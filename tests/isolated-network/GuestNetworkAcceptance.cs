using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InputStitch
{
    [Serializable]
    public sealed class GuestFixtureState
    {
        public string State = "";
        public string Scope = "";
        public string Error = "";
        public int ParentId, HelperId;
        public long ParentStartTicks, HelperStartTicks;
        public bool RepeatedBlockStayedBlocked;
    }
    [Serializable]
    public sealed class GuestAcceptanceResult
    {
        public bool Passed;
        public bool ApplicationScopePassed;
        public DateTime StartedUtc, FinishedUtc;
        public int Assertions;
        public string Error = "";
        public string Coverage = "Executables A+B blocked/C remains connected, then EntireComputer; IPv4+IPv6 TCP+UDP new+existing flows; Restore+normal parent exit+TerminateProcess parent+TerminateProcess helper";
    }
    internal static class GuestNetworkAcceptance
    {
        private static StreamWriter evidence;
        private static StreamWriter applicationEvidence;
        private static readonly GuestAcceptanceResult Result = new GuestAcceptanceResult();
        private static string ExecutablePath { get { return Assembly.GetExecutingAssembly().Location; } }
        private static string RunsDirectory { get { return Path.Combine(GuestNetworkGuard.BaseDirectory, "guest-network-evidence"); } }
        private static NetworkControlOptions Options(bool applicationScope)
        {
            return new NetworkControlOptions { Enabled = true, Mode = applicationScope ? NetworkBlockMode.Executable : NetworkBlockMode.EntireComputer,
                ExecutablePath = applicationScope ? Path.Combine(GuestNetworkGuard.BaseDirectory, "GuestNetworkProbeA.exe") : "",
                ExecutablePaths = applicationScope ? new List<string> { Path.Combine(GuestNetworkGuard.BaseDirectory, "GuestNetworkProbeA.exe"), Path.Combine(GuestNetworkGuard.BaseDirectory, "GuestNetworkProbeB.exe") } : null };
        }

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "--self-test") return GuestNetworkGuard.SelfTest();
                if (args.Length == 0)
                { Console.WriteLine("Built only. EntireComputer is available only inside the dedicated marked QEMU guest. See GUEST-RUN.md."); return 0; }
                bool helper = args[0] == NetworkPrivilegeHelper.Argument;
                if (helper) HelperTrace("entry");
                bool fixture = args[0] == "--fixture";
                bool run = args[0] == "--run";
                if (!helper && !fixture && !run) throw new ArgumentException("Unknown test entry point.");
                // This guard precedes EVERY production helper entry and every EntireComputer request.
                GuestNetworkPermission permission = GuestNetworkGuard.Demand(helper ? null : (args.Length > 1 ? args[1] : null), helper);
                if (helper)
                {
                    AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs error) {
                        HelperTrace("production first-chance " + error.Exception);
                    };
                    HelperTrace("guest permission validated; entering unchanged production helper");
                    int exit = NetworkPrivilegeHelper.TryRunHelper(args) ? Environment.ExitCode : 2;
                    HelperTrace("production helper exit=" + exit);
                    return exit;
                }
                RequireAdministrator();
                if (fixture)
                {
                    if (args.Length != 3 && !(args.Length == 4 && args[3] == "appscope")) throw new ArgumentException("Invalid fixture invocation.");
                    return RunFixtureChild(args[2], permission, args.Length == 4);
                }
                if (args.Length != 8) throw new ArgumentException("Usage: --run token peerIPv4 tcp4 udp4 peerIPv6 tcp6 udp6");
                return Run(args, permission);
            }
            catch (Exception ex)
            {
                if (args.Length > 0 && args[0] == NetworkPrivilegeHelper.Argument) HelperTrace("REFUSED " + ex);
                Console.Error.WriteLine("REFUSED/FAILED: " + ex.Message); return 1;
            }
        }

        private static void HelperTrace(string stage)
        {
            File.AppendAllText(Path.Combine(GuestNetworkGuard.BaseDirectory, "guest-helper-startup.txt"),
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " pid=" + Process.GetCurrentProcess().Id + " " + stage + Environment.NewLine);
        }

        private static void RequireAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                    throw new UnauthorizedAccessException("Run the guest acceptance host as Administrator so it can terminate its own elevated test helper.");
        }
        private static int Run(string[] args, GuestNetworkPermission permission)
        {
            IPAddress four = ParsePeer(args[2], AddressFamily.InterNetwork), six = ParsePeer(args[5], AddressFamily.InterNetworkV6);
            int tcp4 = Port(args[3]), udp4 = Port(args[4]), tcp6 = Port(args[6]), udp6 = Port(args[7]);
            string probeArguments = four + " " + tcp4 + " " + udp4 + " " + six + " " + tcp6 + " " + udp6;
            Directory.CreateDirectory(RunsDirectory);
            Result.StartedUtc = DateTime.UtcNow;
            using (evidence = new StreamWriter(Path.Combine(RunsDirectory, "guest-network-acceptance.log"), false))
            {
                evidence.AutoFlush = true;
                try
                {
                    GuestMachineIdentity identity = GuestNetworkGuard.ReadMachine();
                    Log("IDENTITY " + identity.ComputerName + " | " + identity.Manufacturer + " | " + identity.Model + " | " + identity.Bios + " | " + identity.Uuid);
                    Log("PEERS IPv4=" + four + " TCP=" + tcp4 + " UDP=" + udp4 + " IPv6=" + six + " TCP=" + tcp6 + " UDP=" + udp6);
                    Log("CONTROL local disk/stdin/stdout only; maximum fixture lifetime=" + permission.MaximumBlockSeconds + "s; production network sources unchanged");
                    using (Probe a = new Probe("GuestNetworkProbeA.exe", probeArguments))
                    using (Probe b = new Probe("GuestNetworkProbeB.exe", probeArguments))
                    using (Probe c = new Probe("GuestNetworkProbeC.exe", probeArguments))
                    {
                        Assert(a.ProcessId != b.ProcessId && a.ProcessId != c.ProcessId && b.ProcessId != c.ProcessId, "three independent probe process identities");
                        Log("PROBES distinct executables/PIDs A=" + a.ProcessId + " B=" + b.ProcessId + " C=" + c.ProcessId);
                        RunApplicationScope(args[1], a, b, c, permission.MaximumBlockSeconds);
                        foreach (string scenario in new string[] { "restore", "normal-parent-exit", "kill-parent", "kill-helper" })
                            RunScenario(args[1], scenario, a, b, permission.MaximumBlockSeconds);
                    }
                    Result.Passed = true;
                    Log("RESULT PASS assertions=" + Result.Assertions + "; application-scope control and all four whole-computer scenarios; IPv4+IPv6; TCP+UDP; two independent processes; validated guest only");
                    return 0;
                }
                catch (Exception ex) { Result.Error = ex.ToString(); Log("RESULT FAIL " + ex); return 1; }
                finally
                {
                    Result.FinishedUtc = DateTime.UtcNow;
                    WriteXml(Path.Combine(RunsDirectory, "guest-network-result.xml"), Result);
                }
            }
        }

        private static void RunApplicationScope(string token, Probe a, Probe b, Probe c, int maximumSeconds)
        {
            using (StreamWriter separate = new StreamWriter(Path.Combine(RunsDirectory, "application-scope.log"), false))
            {
                separate.AutoFlush = true; applicationEvidence = separate;
                try
                {
                    Log("SCENARIO Executables A+B; C is the negative control; external dual-stack peer");
                    CheckNew(a, true, "appscope/baseline-A"); CheckNew(b, true, "appscope/baseline-B"); CheckNew(c, true, "appscope/baseline-C");
                    OpenExisting(a, "appscope/open-A"); OpenExisting(b, "appscope/open-B"); OpenExisting(c, "appscope/open-C");
                    string id = Guid.NewGuid().ToString("N"), directory = FixtureDirectory(id); Directory.CreateDirectory(directory);
                    ProcessStartInfo launch = new ProcessStartInfo(ExecutablePath, "--fixture " + token + " " + id + " appscope");
                    launch.UseShellExecute = false; launch.CreateNoWindow = true; launch.WindowStyle = ProcessWindowStyle.Hidden;
                    using (Process fixture = Process.Start(launch))
                    {
                        GuestFixtureState state = null;
                        try
                        {
                            Stopwatch ready = Stopwatch.StartNew();
                            while (!File.Exists(Path.Combine(directory, "state.xml")))
                            {
                                if (fixture.HasExited || ready.ElapsedMilliseconds > maximumSeconds * 1000) throw new Exception("Application-scope fixture did not report a committed block.");
                                Thread.Sleep(25);
                            }
                            state = ReadXml<GuestFixtureState>(Path.Combine(directory, "state.xml"));
                            if (state.State != "BLOCKED") throw new Exception("Application-scope block failed: " + state.Error);
                            Assert(state.Scope == "Executable", "appscope committed only the Executable scope");
                            Assert(state.RepeatedBlockStayedBlocked, "appscope repeated independent block remains blocked");
                            Assert(state.ParentId == fixture.Id && state.ParentStartTicks == fixture.StartTime.ToUniversalTime().Ticks, "appscope fixture parent identity");
                            Log("ACTIVE appscope targets=GuestNetworkProbeA.exe+GuestNetworkProbeB.exe parent=" + state.ParentId + " helper=" + state.HelperId);
                            CheckNew(a, false, "appscope/blocked-A"); CheckExisting(a, false, "appscope/existing-blocked-A");
                            CheckNew(b, false, "appscope/blocked-B"); CheckExisting(b, false, "appscope/existing-blocked-B");
                            CheckNew(c, true, "appscope/negative-new-C"); CheckExisting(c, true, "appscope/negative-existing-C");
                            WriteCommand(Path.Combine(directory, "command"), "RESTORE");
                            Assert(fixture.WaitForExit(15000) && fixture.ExitCode == 0, "appscope restore succeeds");
                            Assert(File.Exists(Path.Combine(directory, "normal-cleanup")), "appscope normal cleanup completed");
                            WaitForRecovery(a, "appscope/recovery-A");
                            CheckNew(a, true, "appscope/restored-A"); CheckNew(b, true, "appscope/restored-B"); CheckNew(c, true, "appscope/still-connected-C");
                            Require(a.Command("UDPHELD4"), true, "appscope/A-existing-UDP4-socket-restored");
                            Require(a.Command("UDPHELD6"), true, "appscope/A-existing-UDP6-socket-restored");
                            Require(b.Command("UDPHELD4"), true, "appscope/B-existing-UDP4-socket-restored");
                            Require(b.Command("UDPHELD6"), true, "appscope/B-existing-UDP6-socket-restored");
                            CheckExisting(c, true, "appscope/C-existing-flows-remain-connected");
                            Assert(WaitForHelperExit(state, 10000), "appscope helper exits after restore/parent close");
                            Result.ApplicationScopePassed = true;
                            Log("PASS SCENARIO Executable: A+B blocked atomically; C remained connected on new/existing IPv4+IPv6 TCP+UDP flows; A+B restored");
                        }
                        finally
                        {
                            if (!fixture.HasExited) { fixture.Kill(); fixture.WaitForExit(10000); }
                            if (state != null && state.HelperId > 0 && state.HelperStartTicks > 0 && !WaitForHelperExit(state, 10000))
                                using (Process helper = CheckedHelper(state)) { helper.Kill(); helper.WaitForExit(10000); }
                        }
                    }
                }
                catch (Exception ex) { Log("FAIL SCENARIO Executable " + ex); throw; }
                finally { applicationEvidence = null; }
            }
        }

        private static void RunScenario(string token, string scenario, Probe a, Probe b, int maximumSeconds)
        {
            Log("SCENARIO " + scenario);
            CheckNew(a, true, scenario + "/baseline-A"); CheckNew(b, true, scenario + "/baseline-B");
            OpenExisting(a, scenario + "/open-A"); OpenExisting(b, scenario + "/open-B");
            string id = Guid.NewGuid().ToString("N"), directory = FixtureDirectory(id);
            Directory.CreateDirectory(directory);
            ProcessStartInfo launch = new ProcessStartInfo(ExecutablePath, "--fixture " + token + " " + id);
            launch.UseShellExecute = false; launch.CreateNoWindow = true; launch.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process fixture = Process.Start(launch))
            {
                GuestFixtureState state = null;
                try
                {
                    Stopwatch ready = Stopwatch.StartNew();
                    while (!File.Exists(Path.Combine(directory, "state.xml")))
                    {
                        if (fixture.HasExited || ready.ElapsedMilliseconds > maximumSeconds * 1000) throw new Exception("Fixture failed before reporting a committed block: " + scenario);
                        Thread.Sleep(25);
                    }
                    state = ReadXml<GuestFixtureState>(Path.Combine(directory, "state.xml"));
                    if (state.State != "BLOCKED") throw new Exception("Fixture could not block: " + state.Error);
                    Assert(state.Scope == "EntireComputer", "fixture committed EntireComputer scope");
                    Assert(state.RepeatedBlockStayedBlocked, "repeated independent block remains blocked");
                    Assert(state.ParentId == fixture.Id && state.ParentStartTicks == fixture.StartTime.ToUniversalTime().Ticks, "fixture parent identity");
                    Log("ACTIVE parent=" + state.ParentId + " helper=" + state.HelperId + " scenario=" + scenario);
                    // Both distinct executable paths must be blocked, including flows opened before the change.
                    CheckNew(a, false, scenario + "/blocked-A"); CheckNew(b, false, scenario + "/blocked-B");
                    CheckExisting(a, false, scenario + "/existing-A"); CheckExisting(b, false, scenario + "/existing-B");
                    if (scenario == "kill-parent")
                    {
                        Assert(!fixture.HasExited, "parent is alive before actual TerminateProcess");
                        fixture.Kill(); Assert(fixture.WaitForExit(10000), "terminated parent exits");
                        Assert(fixture.ExitCode != 0 && !File.Exists(Path.Combine(directory, "normal-cleanup")), "parent termination bypasses normal cleanup");
                        Log("TERMINATE parent actual Process.Kill/TerminateProcess exit=" + fixture.ExitCode);
                    }
                    else if (scenario == "kill-helper")
                    {
                        using (Process helper = CheckedHelper(state))
                        {
                            helper.Kill(); Assert(helper.WaitForExit(10000) && helper.ExitCode != 0, "actual helper TerminateProcess");
                            Log("TERMINATE helper actual Process.Kill/TerminateProcess exit=" + helper.ExitCode);
                        }
                        Assert(!fixture.HasExited, "parent remains alive while helper cleanup is verified");
                    }
                    else
                    {
                        WriteCommand(Path.Combine(directory, "command"), scenario == "restore" ? "RESTORE" : "CLOSE");
                        Assert(fixture.WaitForExit(15000) && fixture.ExitCode == 0, "normal restore/close succeeds");
                        Assert(File.Exists(Path.Combine(directory, "normal-cleanup")), "normal cleanup marker exists");
                    }
                    WaitForRecovery(a, scenario + "/recovery-A"); WaitForRecovery(b, scenario + "/recovery-B");
                    CheckNew(a, true, scenario + "/restored-A"); CheckNew(b, true, scenario + "/restored-B");
                    // TCP streams may be torn down on block; re-opened TCP is checked above. UDP should be usable again on the same socket.
                    Require(a.Command("UDPHELD4"), true, scenario + "/UDP4-same-socket-A");
                    Require(a.Command("UDPHELD6"), true, scenario + "/UDP6-same-socket-A");
                    Require(b.Command("UDPHELD4"), true, scenario + "/UDP4-same-socket-B");
                    Require(b.Command("UDPHELD6"), true, scenario + "/UDP6-same-socket-B");
                    if (!fixture.HasExited)
                    { WriteCommand(Path.Combine(directory, "command"), "CLOSE"); Assert(fixture.WaitForExit(10000) && fixture.ExitCode == 0, "remaining parent closes"); }
                    if (state != null) Assert(WaitForHelperExit(state, 10000), "helper exits after parent close");
                    Log("PASS SCENARIO " + scenario);
                }
                finally
                {
                    if (!fixture.HasExited) { fixture.Kill(); fixture.WaitForExit(10000); }
                    if (state != null && state.HelperId > 0 && state.HelperStartTicks > 0 && !WaitForHelperExit(state, 10000))
                        using (Process helper = CheckedHelper(state)) { helper.Kill(); helper.WaitForExit(10000); }
                }
            }
        }

        private static int RunFixtureChild(string id, GuestNetworkPermission permission, bool applicationScope)
        {
            string directory = FixtureDirectory(id); Directory.CreateDirectory(directory);
            GuestFixtureState state = new GuestFixtureState();
            using (Process current = Process.GetCurrentProcess())
            using (Timer watchdog = new Timer(delegate { TerminateProcess(current.Handle, 0xE0000001); }, null, permission.MaximumBlockSeconds * 1000, Timeout.Infinite))
            {
                try
                {
                    using (NetworkController controller = new NetworkController())
                    {
                        if (!controller.Block(Options(applicationScope)) || !controller.IsBlocked) throw new Exception(controller.LastError);
                        state.RepeatedBlockStayedBlocked = controller.Block(Options(applicationScope)) && controller.IsBlocked;
                        if (!state.RepeatedBlockStayedBlocked) throw new Exception("Repeated independent block restored the connection.");
                        Process helper = HelperProcess(controller);
                        state.ParentId = current.Id; state.ParentStartTicks = current.StartTime.ToUniversalTime().Ticks;
                        state.HelperId = helper.Id; state.HelperStartTicks = helper.StartTime.ToUniversalTime().Ticks;
                        state.State = "BLOCKED"; state.Scope = applicationScope ? "Executable" : "EntireComputer";
                        WriteXml(Path.Combine(directory, "state.xml"), state);
                        while (!File.Exists(Path.Combine(directory, "command"))) Thread.Sleep(25);
                        string command = ReadCommand(Path.Combine(directory, "command"));
                        if (command != "RESTORE" && command != "CLOSE") throw new Exception("Invalid disk fixture command.");
                        if (command == "RESTORE" && !controller.Restore()) throw new Exception(controller.LastError);
                        // CLOSE exercises the same Dispose path as a normally closing main window.
                    }
                    File.WriteAllText(Path.Combine(directory, "normal-cleanup"), DateTime.UtcNow.ToString("o"));
                    return 0;
                }
                catch (Exception ex)
                {
                    state.State = "ERROR"; state.Error = ex.ToString(); WriteXml(Path.Combine(directory, "state.xml"), state); return 1;
                }
            }
        }

        private static Process HelperProcess(NetworkController controller)
        {
            object connection = typeof(NetworkController).GetField("connection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            if (connection == null) throw new Exception("Production controller has no helper connection.");
            Process helper = (Process)connection.GetType().GetField("helper", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(connection);
            if (helper == null || helper.HasExited) throw new Exception("Production helper is not alive.");
            return helper;
        }
        private static Process CheckedHelper(GuestFixtureState state)
        {
            Process helper = Process.GetProcessById(state.HelperId);
            try
            {
                IntPtr retained = helper.Handle;
                if (retained == IntPtr.Zero || helper.HasExited || helper.StartTime.ToUniversalTime().Ticks != state.HelperStartTicks ||
                    !string.Equals(Path.GetFullPath(helper.MainModule.FileName), Path.GetFullPath(ExecutablePath), StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Helper identity changed; refusing to terminate an unrelated process.");
                return helper;
            }
            catch { helper.Dispose(); throw; }
        }
        private static bool WaitForHelperExit(GuestFixtureState state, int milliseconds)
        {
            try { using (Process helper = Process.GetProcessById(state.HelperId)) return helper.HasExited || helper.StartTime.ToUniversalTime().Ticks != state.HelperStartTicks || helper.WaitForExit(milliseconds); }
            catch (ArgumentException) { return true; }
            catch (InvalidOperationException) { return true; }
        }
        private static string FixtureDirectory(string id)
        { Guid parsed; if (!Guid.TryParseExact(id, "N", out parsed)) throw new ArgumentException("Invalid fixture id."); return Path.Combine(RunsDirectory, id); }
        private static void CheckNew(Probe probe, bool allowed, string stage)
        { foreach (string command in new string[] { "TCP4", "UDP4", "TCP6", "UDP6" }) Require(probe.Command(command), allowed, stage + "/" + command); }
        private static void OpenExisting(Probe probe, string stage)
        {
            foreach (string command in new string[] { "OPEN4", "UDPOPEN4", "OPEN6", "UDPOPEN6" }) Require(probe.Command(command), true, stage + "/" + command);
            CheckExisting(probe, true, stage + "/confirmed");
        }
        private static void CheckExisting(Probe probe, bool allowed, string stage)
        { foreach (string command in new string[] { "HELD4", "UDPHELD4", "HELD6", "UDPHELD6" }) Require(probe.Command(command), allowed, stage + "/" + command); }
        private static void WaitForRecovery(Probe probe, string stage)
        {
            Stopwatch timer = Stopwatch.StartNew(); string response;
            do { response = probe.Command("TCP4"); if (response.StartsWith("OK ", StringComparison.Ordinal)) break; Thread.Sleep(50); } while (timer.ElapsedMilliseconds < 15000);
            Require(response, true, stage + " latency_ms=" + timer.ElapsedMilliseconds);
        }
        private static void Require(string response, bool allowed, string message)
        { Assert(response.StartsWith(allowed ? "OK " : "BLOCKED ", StringComparison.Ordinal), message + " -> " + response); }
        private static void Assert(bool passed, string message)
        { if (!passed) throw new Exception(message); Result.Assertions++; Log("PASS " + message); }
        private static void Log(string line)
        {
            string entry = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " " + line;
            if (evidence != null) evidence.WriteLine(entry); if (applicationEvidence != null) applicationEvidence.WriteLine(entry); Console.WriteLine(entry);
        }
        private static int Port(string value)
        { int port; if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535) throw new ArgumentException("Invalid peer port."); return port; }
        private static IPAddress ParsePeer(string value, AddressFamily family)
        {
            IPAddress address;
            if (!IPAddress.TryParse(value, out address) || address.AddressFamily != family || IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) ||
                address.Equals(IPAddress.IPv6Any) || address.IsIPv6Multicast || (family == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6))
                throw new ArgumentException("Both peer endpoints must be explicit non-loopback IPv4/IPv6 addresses.");
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                foreach (UnicastIPAddressInformation local in nic.GetIPProperties().UnicastAddresses)
                    if (SameAddressBytes(address, local.Address)) throw new ArgumentException("Peer endpoint resolves to the guest itself; use the external dual-stack peer.");
            return address;
        }
        private static bool SameAddressBytes(IPAddress a, IPAddress b)
        { byte[] left = a.GetAddressBytes(), right = b.GetAddressBytes(); if (left.Length != right.Length) return false; for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false; return true; }
        private static void WriteXml<T>(string path, T value)
        {
            string temporary = path + ".tmp";
            using (FileStream stream = File.Create(temporary)) new XmlSerializer(typeof(T)).Serialize(stream, value);
            if (File.Exists(path)) File.Delete(path); File.Move(temporary, path);
        }
        private static T ReadXml<T>(string path)
        { using (FileStream stream = File.OpenRead(path)) return (T)new XmlSerializer(typeof(T)).Deserialize(stream); }
        private static string ReadCommand(string path)
        {
            // The guest filesystem scanner can briefly retain a just-renamed fixture file.
            Stopwatch wait = Stopwatch.StartNew();
            while (true)
            {
                try { return File.ReadAllText(path).Trim(); }
                catch (IOException) { if (wait.ElapsedMilliseconds >= 3000) throw; Thread.Sleep(25); }
            }
        }
        private static void WriteCommand(string path, string command)
        { string temporary = path + ".tmp"; File.WriteAllText(temporary, command); File.Move(temporary, path); }
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(IntPtr process, uint exitCode);

        private sealed class Probe : IDisposable
        {
            private readonly Process process;
            internal int ProcessId { get { return process.Id; } }
            internal Probe(string name, string arguments)
            {
                string path = Path.Combine(GuestNetworkGuard.BaseDirectory, name);
                ProcessStartInfo start = new ProcessStartInfo(path, arguments);
                start.UseShellExecute = false; start.CreateNoWindow = true; start.WindowStyle = ProcessWindowStyle.Hidden;
                start.RedirectStandardInput = true; start.RedirectStandardOutput = true; process = Process.Start(start);
                try
                {
                    Task<string> ready = process.StandardOutput.ReadLineAsync();
                    if (!ready.Wait(60000) || ready.Result != "READY") throw new TimeoutException("Probe CLR startup did not become ready: " + name);
                }
                catch
                {
                    if (!process.HasExited) { process.Kill(); process.WaitForExit(5000); }
                    process.Dispose();
                    throw;
                }
            }
            internal string Command(string command)
            {
                process.StandardInput.WriteLine(command); process.StandardInput.Flush(); Task<string> pending = process.StandardOutput.ReadLineAsync();
                if (!pending.Wait(15000)) throw new TimeoutException("Probe command timed out: " + command);
                string response = pending.Result;
                if (response == null || (!response.StartsWith("OK " + command, StringComparison.Ordinal) && !response.StartsWith("BLOCKED " + command, StringComparison.Ordinal)))
                    throw new IOException("Invalid probe response: " + response);
                return response;
            }
            public void Dispose()
            {
                try { if (!process.HasExited) { process.StandardInput.WriteLine("QUIT"); process.StandardInput.Flush(); if (!process.WaitForExit(2000)) process.Kill(); } }
                finally { process.Dispose(); }
            }
        }
    }
}
