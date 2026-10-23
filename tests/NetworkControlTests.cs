using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace InputStitch
{
    internal static class NetworkControlTests
    {
        private static int passed;
        private static string executable = System.Reflection.Assembly.GetExecutingAssembly().Location;
        private static NetworkControlOptions Options()
        { return new NetworkControlOptions { Enabled = true, Mode = NetworkBlockMode.Executable, ExecutablePath = executable }; }
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Test(string name, Action action)
        { action(); passed++; Console.WriteLine("PASS " + name); }
        private static void Throws(Action action)
        { bool threw = false; try { action(); } catch (Exception) { threw = true; } Check(threw, "Expected failure"); }
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--pipe-probe") return PipeProbe(args);
            try
            {
                Test("options clone preserves modifiers and startup never blocks", delegate
                {
                    NetworkControlOptions o = Options(); o.Hotkey = new TriggerSpec { Ctrl = true, Alt = true, VirtualKey = 75 };
                    NetworkControlOptions copy = o.Clone(); copy.Hotkey.VirtualKey = 76;
                    Check(o.Hotkey.VirtualKey == 75 && copy.Hotkey.Ctrl && copy.Hotkey.Alt, "hotkey must deep clone");
                    FakeFactory factory = new FakeFactory(); using (NetworkController c = new NetworkController(factory)) Check(factory.Starts == 0 && !c.IsBlocked, "startup must never launch helper");
                    o.Mode = (NetworkBlockMode)99; o.ExecutablePath = null; o.Hotkey.VirtualKey = 17; o.Normalize();
                    Check(o.Mode == NetworkBlockMode.Executable && o.ExecutablePath == "" && o.Hotkey == null, "normalize invalid options");
                    XmlSerializer xml = new XmlSerializer(typeof(NetworkControlOptions));
                    using (StringWriter writer = new StringWriter()) { xml.Serialize(writer, Options()); using (StringReader reader = new StringReader(writer.ToString())) Check(((NetworkControlOptions)xml.Deserialize(reader)).Hotkey == null, "unset shortcut roundtrip"); }
                });
                Test("disabled/missing executable refuses before elevation", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    { Check(!c.Toggle(new NetworkControlOptions()), "disabled"); NetworkControlOptions o = Options(); o.ExecutablePath = "C:\\missing-inputstitch-test.exe"; Check(!c.Toggle(o) && f.Starts == 0 && !c.IsBlocked, "invalid exe"); }
                });
                Test("cancelled UAC and startup failure never report blocked", delegate
                {
                    foreach (Exception error in new Exception[] { new Win32Exception(1223), new IOException("startup failed") })
                    {
                        FakeFactory f = new FakeFactory { StartError = error };
                        using (NetworkController c = new NetworkController(f)) Check(!c.Toggle(Options()) && !c.IsBlocked && c.LastError.Length > 0, "false state on startup failure");
                    }
                });
                Test("toggle restores and reuses authorized idle helper", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    { Check(c.Toggle(Options()) && c.IsBlocked, "first block"); Check(c.Toggle(Options()) && !c.IsBlocked, "toggle restore"); Check(c.Toggle(Options()) && c.IsBlocked && f.Starts == 1, "reuse helper"); Check(c.Restore() && c.Restore() && !c.IsBlocked, "idempotent restore"); }
                });
                Test("independent block is idempotent and restore never starts a helper", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    {
                        Check(c.Restore() && f.Starts == 0, "connected restore requested elevation");
                        Check(c.Block(Options()) && c.Block(Options()) && c.IsBlocked, "repeated block toggled to connected");
                        Check(f.Starts == 1 && f.Connection.Commands.Count == 1, "repeated block submitted duplicate request");
                        Check(c.Restore() && !c.IsBlocked && c.Block(Options()) && c.IsBlocked && f.Starts == 1, "separate commands cannot reuse helper");
                    }
                });
                Test("new hotkeys and independent sound preferences preserve legacy toggle", delegate
                {
                    NetworkControlOptions o = Options(); o.Hotkey = new TriggerSpec { VirtualKey = 75, Ctrl = true };
                    Check(o.BlockShortcut == o.Hotkey && o.RestoreShortcut == o.Hotkey, "legacy chord changed");
                    o.BlockHotkey = new TriggerSpec { VirtualKey = 76 }; Check(o.RestoreShortcut == null, "partial new config used legacy chord");
                    o.RestoreHotkey = new TriggerSpec { VirtualKey = 77, Alt = true }; o.BlockSound = false;
                    XmlSerializer xml = new XmlSerializer(typeof(NetworkControlOptions)); NetworkControlOptions reopened;
                    using (StringWriter w = new StringWriter()) { xml.Serialize(w, o); using (StringReader r = new StringReader(w.ToString())) reopened = (NetworkControlOptions)xml.Deserialize(r); }
                    Check(reopened.BlockShortcut.VirtualKey == 76 && reopened.RestoreShortcut.VirtualKey == 77 && !reopened.BlockSound && reopened.RestoreSound, "settings did not roundtrip");
                    NetworkControlOptions copy = reopened.Clone(); copy.BlockHotkey.VirtualKey = 78; copy.RestoreHotkey.VirtualKey = 79;
                    Check(reopened.BlockHotkey.VirtualKey == 76 && reopened.RestoreHotkey.VirtualKey == 77, "new shortcuts share references");
                    reopened.BlockHotkey.VirtualKey = 17; reopened.Normalize(); Check(reopened.BlockShortcut == null, "invalid block chord survived");
                });
                Test("sounds follow confirmed edges once and respect each mute preference", delegate
                {
                    List<NetworkSoundKind> cues = new List<NetworkSoundKind>(); NetworkStateSounds sounds = new NetworkStateSounds(cues.Add);
                    NetworkControlOptions o = Options(); sounds.Observe(false, o); sounds.Observe(true, o); sounds.Observe(true, o); sounds.Observe(false, o); sounds.Observe(false, o);
                    Check(cues.Count == 2 && cues[0] == NetworkSoundKind.Blocked && cues[1] == NetworkSoundKind.Restored, "duplicate or false startup sound");
                    o.BlockSound = false; sounds.Observe(true, o); o.BlockSound = true; sounds.Observe(true, o);
                    Check(cues.Count == 2, "unmuting played a stale transition");
                    o.RestoreSound = false; sounds.Observe(false, o); Check(cues.Count == 2, "restore mute ignored");
                    new NetworkStateSounds(delegate { throw new InvalidOperationException("no audio device"); }).Observe(true, o);
                });
                Test("embedded cues are small valid PCM waves with opposite pitch direction", delegate
                {
                    byte[] down = NetworkSounds.Wave(NetworkSoundKind.Blocked), up = NetworkSounds.Wave(NetworkSoundKind.Restored);
                    foreach (byte[] wave in new[] { down, up })
                    {
                        Check(wave.Length == 4204 && Encoding.ASCII.GetString(wave, 0, 4) == "RIFF" && Encoding.ASCII.GetString(wave, 8, 8) == "WAVEfmt ", "wave header/size");
                        Check(BitConverter.ToInt32(wave, 4) == wave.Length - 8 && BitConverter.ToInt16(wave, 20) == 1 && BitConverter.ToInt16(wave, 22) == 1 && BitConverter.ToInt32(wave, 24) == 16000 && BitConverter.ToInt16(wave, 34) == 16, "PCM parameters");
                    }
                    Check(Crossings(down, 0) > Crossings(down, 1120) && Crossings(up, 0) < Crossings(up, 1120), "cues have no distinct pitch direction");
                    using (System.Media.SoundPlayer player = new System.Media.SoundPlayer(new MemoryStream(down))) { player.Load(); Check(player.IsLoadCompleted, "native WAV parser refused cue"); }
                });
                Test("false reply/request failure closes connection", delegate
                {
                    foreach (string response in new string[] { "READY", "ERROR\n" + Convert.ToBase64String(Encoding.UTF8.GetBytes("native permission failed")) })
                    {
                        FakeFactory f = new FakeFactory(); f.Connection.Reply = response;
                        using (NetworkController c = new NetworkController(f)) Check(!c.Toggle(Options()) && !c.IsBlocked && f.Connection.Disposed, "unconfirmed state");
                    }
                    FakeFactory broken = new FakeFactory(); broken.Connection.RequestError = new EndOfStreamException();
                    using (NetworkController c = new NetworkController(broken)) Check(!c.Toggle(Options()) && !c.IsBlocked && broken.Connection.Disposed, "pipe failure");
                });
                Test("helper crash clears blocked state and notifies", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    { int notifications = 0; c.StateChanged += delegate { notifications++; }; Check(c.Toggle(Options()), "block"); f.Connection.Crash(); Check(!c.IsBlocked && notifications == 2 && c.LastError.Length > 0, "crash transition"); }
                });
                Test("view exception cannot undo committed block", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    { c.StateChanged += delegate { throw new InvalidOperationException("view unavailable"); }; Check(c.Toggle(Options()) && c.IsBlocked, "UI exception changed state"); }
                });
                Test("restore transport failure forces fail-open pipe teardown", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    { Check(c.Toggle(Options()), "block"); f.Connection.RequestError = new IOException("connection lost"); Check(!c.Restore() && !c.IsBlocked && f.Connection.Disposed && c.LastError.Length > 0, "failure must close helper channel"); }
                });
                Test("Dispose never waits for pending elevation and cannot later block", delegate
                {
                    FakeFactory f = new FakeFactory { StartEntered = new ManualResetEvent(false), StartContinue = new ManualResetEvent(false) };
                    NetworkController c = new NetworkController(f); bool result = true;
                    Thread worker = new Thread(delegate() { result = c.Toggle(Options()); }); worker.Start();
                    Check(f.StartEntered.WaitOne(2000), "start not entered"); Stopwatch elapsed = Stopwatch.StartNew(); c.Dispose();
                    Check(elapsed.ElapsedMilliseconds < 200, "dispose blocked on UAC"); f.StartContinue.Set(); Check(worker.Join(2000), "worker stuck");
                    Check(!result && f.Connection.Disposed && f.Connection.Commands.Count == 0, "pending helper blocked after dispose");
                    f.StartEntered.Dispose(); f.StartContinue.Dispose();
                });
                Test("concurrent toggles serialize to restored state", delegate
                {
                    FakeFactory f = new FakeFactory(); using (NetworkController c = new NetworkController(f))
                    {
                        Thread[] threads = new Thread[20]; for (int i = 0; i < threads.Length; i++) { threads[i] = new Thread(delegate() { Check(c.Toggle(Options()), "concurrent toggle failed"); }); threads[i].Start(); }
                        foreach (Thread thread in threads) Check(thread.Join(5000), "toggle stuck");
                        Check(!c.IsBlocked && f.Starts == 1 && f.Connection.Commands.Count == 20, "toggle operations not serialized");
                    }
                    Check(f.Connection.Disposed, "dispose did not disconnect");
                });
                Test("WFP exact executable applies all four layers in one transaction", delegate
                {
                    FakeWfp api = new FakeWfp(); using (WfpNetworkSession block = WfpNetworkSession.Create(api, NetworkBlockMode.Executable, executable))
                    { Check(api.Filters.Count == 4 && new HashSet<Guid>(api.Filters).Count == 4 && api.AppBound == 4, "v4/v6 inbound/outbound missing"); Check(api.Committed && !api.Closed && api.AppFreed, "commit lifecycle"); }
                    Check(api.Closed && !api.Aborted && api.Closes == 1, "restore session");
                });
                Test("whole-computer block has no executable condition", delegate
                {
                    FakeWfp api = new FakeWfp(); WfpNetworkSession block = WfpNetworkSession.Create(api, NetworkBlockMode.EntireComputer, "");
                    Check(api.AppBound == 0 && api.Filters.Count == 4 && !api.AppRequested, "whole computer scope"); block.Dispose(); block.Dispose(); Check(api.Closes == 1, "double restore");
                });
                Test("each native failure rolls back and frees only owned session", delegate
                {
                    foreach (string failure in new string[] { "appid", "open", "begin", "sublayer", "filter1", "filter2", "filter3", "filter4", "commit" })
                    {
                        FakeWfp api = new FakeWfp { Failure = failure };
                        Throws(delegate { WfpNetworkSession.Create(api, NetworkBlockMode.Executable, executable); });
                        Check(!api.Committed, "failed operation committed: " + failure);
                        Check(!api.Opened || api.Closed, "session left open: " + failure);
                        Check(!api.AppRequested || failure == "appid" || api.AppFreed, "appid leaked: " + failure);
                        Check(!api.Begun || api.Aborted, "transaction not aborted: " + failure);
                    }
                });
                Test("abort failure still closes engine and releases app allocation", delegate
                {
                    FakeWfp api = new FakeWfp { Failure = "filter2", AbortFailure = true };
                    Throws(delegate { WfpNetworkSession.Create(api, NetworkBlockMode.Executable, executable); });
                    Check(api.Closed && api.AppFreed, "nested cleanup failed");
                });
                Test("pipe EOF/parent disappearance restores active session", delegate
                {
                    foreach (bool readThrows in new bool[] { false, true })
                    {
                        Queue<string> commands = new Queue<string>(); commands.Enqueue(BlockCommand()); FakeDisposable block = new FakeDisposable(); List<string> replies = new List<string>();
                        Action run = delegate { NetworkPrivilegeHelper.RunSession(delegate { if (commands.Count > 0) return commands.Dequeue(); if (readThrows) throw new EndOfStreamException("parent lost"); return null; }, replies.Add, delegate { return block; }); };
                        if (readThrows) Throws(run); else run(); Check(block.Disposes == 1 && replies[0] == "BLOCKED", "parent lost left block");
                    }
                });
                Test("helper protocol restores before acknowledgment and is idempotent", delegate
                {
                    Queue<string> commands = new Queue<string>(new string[] { BlockCommand(), "RESTORE", "RESTORE", "QUIT" });
                    FakeDisposable block = new FakeDisposable(); List<string> replies = new List<string>();
                    NetworkPrivilegeHelper.RunSession(commands.Dequeue, delegate(string reply) { if (reply == "RESTORED") Check(block.Disposes == 1, "ack before restore"); replies.Add(reply); }, delegate { return block; });
                    Check(replies.Count == 3 && replies[0] == "BLOCKED" && block.Disposes == 1, "protocol lifecycle");
                });
                Test("native startup failure and invalid scope never acknowledge blocked", delegate
                {
                    foreach (string command in new string[] { BlockCommand(), "BLOCK\n42\n", "BLOCK\n1\nbWlzc2luZw==", "BAD" })
                    {
                        Queue<string> commands = new Queue<string>(new string[] { command, "QUIT" }); List<string> replies = new List<string>();
                        NetworkPrivilegeHelper.RunSession(commands.Dequeue, replies.Add, delegate { throw new Win32Exception(5); });
                        Check(replies.Count == 1 && replies[0].StartsWith("ERROR\n"), "invalid block acknowledged");
                    }
                });
                Test("lost response/restore failure always leaves session scope", delegate
                {
                    FakeDisposable block = new FakeDisposable(); Queue<string> commands = new Queue<string>(new string[] { BlockCommand() });
                    Throws(delegate { NetworkPrivilegeHelper.RunSession(commands.Dequeue, delegate { throw new IOException("parent disappeared during reply"); }, delegate { return block; }); });
                    Check(block.Disposes == 1, "lost ack left session");
                    block = new FakeDisposable { ThrowDispose = true }; commands = new Queue<string>(new string[] { BlockCommand(), "RESTORE" });
                    Throws(delegate { NetworkPrivilegeHelper.RunSession(commands.Dequeue, delegate { }, delegate { return block; }); });
                    Check(block.Disposes == 2, "restore failure did not hit final teardown");
                });
                Test("bounded pipe frames accept unicode and reject invalid size", delegate
                {
                    using (MemoryStream stream = new MemoryStream()) { NetworkPipeProtocol.Write(stream, "中文 EXE / 英文", 1000); stream.Position = 0; Check(NetworkPipeProtocol.Read(stream, 1000) == "中文 EXE / 英文", "unicode roundtrip"); }
                    using (MemoryStream stream = new MemoryStream(BitConverter.GetBytes(32769))) Throws(delegate { NetworkPipeProtocol.Read(stream, 1000); });
                    using (MemoryStream stream = new MemoryStream()) Throws(delegate { NetworkPipeProtocol.Write(stream, new string('x', 32769), 1000); });
                });
                Test("helper verb does not accidentally start macro UI", delegate
                {
                    Check(!NetworkPrivilegeHelper.TryRunHelper(new string[0]), "normal args consumed");
                    Check(NetworkPrivilegeHelper.TryRunHelper(new string[] { NetworkPrivilegeHelper.Argument }), "malformed helper not consumed"); Environment.ExitCode = 0;
                });
                Test("pipe ACL grants only current identity and administrators", delegate
                {
                    PipeSecurity security = NetworkHelperFactory.CreatePipeSecurity();
                    Check(security.AreAccessRulesProtected, "pipe inherits ACL");
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        foreach (PipeAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                            Check(rule.IdentityReference.Equals(identity.User) || rule.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)), "unrelated pipe principal");
                    }
                });
                Test("real named pipe authenticates parent/client and closes child on EOF", delegate { RealPipeTest(false, false); });
                Test("real helper process loss raises disconnection", delegate { RealPipeTest(true, false); });
                Test("real pipe timeout cancels pending IO and tears down child session", delegate { RealPipeTest(false, true); });
                Test("native ABI layout matches Windows SDK", delegate
                {
                    bool x64 = IntPtr.Size == 8;
                    Check(Marshal.SizeOf(typeof(NativeNetworkWfpApi.WfpValue)) == (x64 ? 16 : 8), "FWP_VALUE0 size");
                    Check(Marshal.SizeOf(typeof(NativeNetworkWfpApi.WfpCondition)) == (x64 ? 40 : 28), "FWPM_FILTER_CONDITION0 size");
                    Check(Marshal.SizeOf(typeof(NativeNetworkWfpApi.WfpFilter)) == (x64 ? 200 : 152), "FWPM_FILTER0 size");
                    Check(Marshal.OffsetOf(typeof(NativeNetworkWfpApi.WfpFilter), "context").ToInt32() == (x64 ? 152 : 112), "filter context alignment");
                    Check(Marshal.OffsetOf(typeof(NativeNetworkWfpApi.WfpFilter), "filterId").ToInt32() == (x64 ? 176 : 136), "filterId alignment");
                    Check(Marshal.SizeOf(typeof(NativeNetworkWfpApi.WfpSession)) == (x64 ? 72 : 48), "FWPM_SESSION0 size");
                    Check(Marshal.SizeOf(typeof(NativeNetworkWfpApi.WfpSublayer)) == (x64 ? 72 : 44), "FWPM_SUBLAYER0 size");
                });
                Console.WriteLine("RESULT " + passed + " passed; architecture=" + (IntPtr.Size * 8) + "; native network mutations=0");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        private static int Crossings(byte[] wave, int start)
        { int count = 0; short last = 0; for (int i = start; i < start + 960; i++) { short next = BitConverter.ToInt16(wave, 44 + i * 2); if (last < 0 && next >= 0) count++; last = next; } return count; }
        private static int PipeProbe(string[] args)
        {
            FakeDisposable block = new FakeDisposable();
            try
            {
                using (Process parent = Process.GetProcessById(int.Parse(args[2])))
                using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", NetworkPrivilegeHelper.PipeName(args[1]), PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    NetworkPrivilegeHelper.VerifyParent(parent, long.Parse(args[3]));
                    pipe.Connect(3000); uint server;
                    Check(NetworkPipeNative.GetNamedPipeServerProcessId(pipe.SafePipeHandle, out server) && server == (uint)parent.Id, "parent pipe PID");
                    NetworkPipeProtocol.Write(pipe, "READY", 3000);
                    try { NetworkPrivilegeHelper.RunSession(delegate { return NetworkPipeProtocol.Read(pipe, Timeout.Infinite); }, delegate(string reply) { NetworkPipeProtocol.Write(pipe, reply, 3000); }, delegate { return block; }); }
                    catch (EndOfStreamException) { }
                    return block.Disposes == 1 ? 0 : 5;
                }
            }
            catch { return 7; }
        }
        private static void RealPipeTest(bool crash, bool timeout)
        {
            string id = Guid.NewGuid().ToString("N");
            using (NamedPipeServerStream pipe = new NamedPipeServerStream(NetworkPrivilegeHelper.PipeName(id), PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, NetworkHelperFactory.CreatePipeSecurity()))
            using (Process parent = Process.GetCurrentProcess())
            {
                IAsyncResult accept = pipe.BeginWaitForConnection(null, null);
                ProcessStartInfo launch = new ProcessStartInfo(executable, "--pipe-probe " + id + " " + parent.Id + " " + parent.StartTime.ToUniversalTime().Ticks);
                launch.UseShellExecute = false; launch.CreateNoWindow = true; launch.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process child = Process.Start(launch))
                {
                    try
                    {
                        using (WaitHandle wait = accept.AsyncWaitHandle) { Check(wait.WaitOne(4000), "probe did not connect"); pipe.EndWaitForConnection(accept); } uint client;
                        Check(NetworkPipeNative.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out client) && client == (uint)child.Id && client != (uint)parent.Id, "helper pipe PID");
                        Check(NetworkPipeProtocol.Read(pipe, 3000) == "READY", "probe failed parent identity");
                        using (NetworkPipeConnection connection = new NetworkPipeConnection(pipe, child))
                        using (ManualResetEvent disconnected = new ManualResetEvent(false))
                        {
                            connection.Disconnected += delegate { disconnected.Set(); };
                            Check(connection.Request(BlockCommand()) == "BLOCKED", "real pipe roundtrip failed");
                            if (crash) { child.Kill(); Check(disconnected.WaitOne(3000) && !connection.IsAlive, "child crash not observed"); }
                            else
                            {
                                TimeSpan before = child.TotalProcessorTime; Thread.Sleep(150); child.Refresh();
                                Check((child.TotalProcessorTime - before).TotalMilliseconds < 100, "idle helper polls");
                                // A separate handle remains valid after connection.Dispose releases its Process wrapper.
                                using (Process monitor = Process.GetProcessById(child.Id))
                                {
                                    IntPtr retainedHandle = monitor.Handle; Check(retainedHandle != IntPtr.Zero, "monitor handle");
                                    if (timeout) { Stopwatch timer = Stopwatch.StartNew(); Throws(delegate { NetworkPipeProtocol.Read(pipe, 50); }); Check(timer.ElapsedMilliseconds < 1000, "IO timeout unbounded"); }
                                    connection.Dispose(); Check(monitor.WaitForExit(3000) && monitor.ExitCode == 0, "EOF did not release fake session and exit helper");
                                }
                            }
                        }
                    }
                    finally { try { if (!child.HasExited) { child.Kill(); child.WaitForExit(3000); } } catch (InvalidOperationException) { } }
                }
            }
        }
        private static string BlockCommand() { return "BLOCK\n1\n" + Convert.ToBase64String(Encoding.UTF8.GetBytes(executable)); }

        private sealed class FakeFactory : INetworkHelperFactory
        {
            internal int Starts; internal Exception StartError; internal FakeConnection Connection = new FakeConnection();
            internal ManualResetEvent StartEntered, StartContinue;
            public INetworkHelperConnection Start()
            {
                Starts++; if (StartEntered != null) { StartEntered.Set(); StartContinue.WaitOne(); }
                if (StartError != null) throw StartError; return Connection;
            }
        }
        private sealed class FakeConnection : INetworkHelperConnection
        {
            internal bool Disposed; internal bool Alive = true; internal string Reply; internal Exception RequestError;
            internal List<string> Commands = new List<string>(); public event EventHandler Disconnected;
            public bool IsAlive { get { return Alive && !Disposed; } }
            public string Request(string command)
            {
                Commands.Add(command); if (RequestError != null) throw RequestError;
                return Reply ?? (command == "RESTORE" ? "RESTORED" : "BLOCKED");
            }
            internal void Crash() { Alive = false; if (Disconnected != null) Disconnected(this, EventArgs.Empty); }
            public void Dispose() { Disposed = true; Alive = false; }
        }
        private sealed class FakeDisposable : IDisposable
        {
            internal int Disposes; internal bool ThrowDispose;
            public void Dispose() { Disposes++; if (ThrowDispose) throw new IOException("close failed"); }
        }
        private sealed class FakeWfp : INetworkWfpApi
        {
            internal string Failure; internal bool AbortFailure; internal bool AppRequested, AppFreed, Opened, Closed, Begun, Aborted, Committed; internal int Closes, AppBound;
            internal List<Guid> Filters = new List<Guid>();
            private void Maybe(string operation) { if (Failure == operation) throw new Win32Exception(5, operation); }
            public IntPtr GetApplicationId(string path) { AppRequested = true; Maybe("appid"); return new IntPtr(22); }
            public void FreeApplicationId(ref IntPtr appId) { AppFreed = true; appId = IntPtr.Zero; }
            public IntPtr OpenDynamic() { Maybe("open"); Opened = true; return new IntPtr(11); }
            public void Begin(IntPtr engine) { Maybe("begin"); Begun = true; }
            public void AddSublayer(IntPtr engine, Guid key) { Maybe("sublayer"); Check(key != Guid.Empty, "empty sublayer"); }
            public void AddBlock(IntPtr engine, Guid layer, Guid sublayer, IntPtr appId)
            { Maybe("filter" + (Filters.Count + 1)); Filters.Add(layer); if (appId != IntPtr.Zero) AppBound++; }
            public void Commit(IntPtr engine) { Maybe("commit"); Committed = true; }
            public void Abort(IntPtr engine) { Aborted = true; if (AbortFailure) throw new IOException("abort failed"); }
            public void Close(IntPtr engine) { Closes++; Closed = true; }
        }
    }
}
