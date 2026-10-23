using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace InputStitch
{
    internal static class NetworkPrivilegeHelper
    {
        internal const string Argument = "--inputstitch-network-helper";
        internal const int RequestTimeout = 10000;

        // Returns true for the helper verb even when arguments/authentication fail:
        // malformed helper invocations must never open a second macro window.
        internal static bool TryRunHelper(string[] args)
        {
            if (args == null || args.Length == 0 || args[0] != Argument) return false;
            try
            {
                int parentId;
                long startTicks;
                Guid pipeId;
                if (args.Length != 5 || !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out parentId) ||
                    !long.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out startTicks) ||
                    !Guid.TryParseExact(args[3], "N", out pipeId) || !ValidToken(args[4]))
                    throw new ArgumentException("Invalid network helper arguments.");
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                        throw new UnauthorizedAccessException("The network helper requires administrator approval.");

                using (Process parent = Process.GetProcessById(parentId))
                {
                    VerifyParent(parent, startTicks);
                    using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", PipeName(args[3]), PipeDirection.InOut, PipeOptions.Asynchronous))
                    {
                        pipe.Connect(RequestTimeout);
                        uint serverId;
                        if (!NetworkPipeNative.GetNamedPipeServerProcessId(pipe.SafePipeHandle, out serverId) || serverId != (uint)parentId)
                            throw new UnauthorizedAccessException("Network helper parent does not own the pipe.");
                        VerifyParent(parent, startTicks);
                        // Process exit wakes a blocked read, without a polling timer or heartbeat.
                        EventHandler parentExited = delegate { try { pipe.Dispose(); } catch (ObjectDisposedException) { } };
                        parent.Exited += parentExited;
                        parent.EnableRaisingEvents = true;
                        try
                        {
                            string hello = NetworkPipeProtocol.Read(pipe, RequestTimeout);
                            if (hello != "HELLO\n" + args[4]) throw new UnauthorizedAccessException("Invalid network helper handshake.");
                            NetworkPipeProtocol.Write(pipe, "READY", RequestTimeout);
                            RunSession(delegate { return NetworkPipeProtocol.Read(pipe, Timeout.Infinite); },
                                delegate(string reply) { NetworkPipeProtocol.Write(pipe, reply, RequestTimeout); },
                                delegate(NetworkBlockMode mode, string path) { return WfpNetworkSession.Create(new NativeNetworkWfpApi(), mode, path); });
                        }
                        finally { parent.Exited -= parentExited; }
                    }
                }
            }
            catch (Exception) { Environment.ExitCode = 1; }
            return true;
        }

        private static bool ValidToken(string token)
        {
            if (token == null || token.Length != 64) return false;
            for (int i = 0; i < token.Length; i++) if (!((token[i] >= '0' && token[i] <= '9') || (token[i] >= 'a' && token[i] <= 'f'))) return false;
            return true;
        }

        internal static void VerifyParent(Process parent, long startTicks)
        {
            if (parent.Id == Process.GetCurrentProcess().Id || parent.HasExited || parent.StartTime.ToUniversalTime().Ticks != startTicks ||
                !string.Equals(Path.GetFullPath(parent.MainModule.FileName), Path.GetFullPath(Assembly.GetExecutingAssembly().Location), StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Network helper parent identity is invalid.");
        }

        internal static string PipeName(string id) { return "InputStitch.Network." + id; }

        internal static string ValidateExecutable(NetworkBlockMode mode, string path)
        {
            if (!Enum.IsDefined(typeof(NetworkBlockMode), mode)) throw new ArgumentException("Invalid network scope.");
            if (mode == NetworkBlockMode.EntireComputer) return "";
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path.IndexOf('\n') >= 0 || path.IndexOf('\r') >= 0)
                throw new ArgumentException("Choose an executable file first.");
            string full = Path.GetFullPath(path);
            if (!string.Equals(Path.GetExtension(full), ".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
                throw new ArgumentException("The selected executable could not be found.");
            return full;
        }

        internal static void RequireResponse(string response, string expected)
        {
            if (response == expected) return;
            if (response != null && response.StartsWith("ERROR\n", StringComparison.Ordinal))
                throw new InvalidOperationException(Encoding.UTF8.GetString(Convert.FromBase64String(response.Substring(6))));
            throw new IOException("The network helper did not confirm the requested state.");
        }

        internal static void RunSession(Func<string> read, Action<string> write, Func<NetworkBlockMode, string, IDisposable> create)
        {
            IDisposable block = null;
            try
            {
                while (true)
                {
                    string command = read();
                    if (command == null || command == "QUIT") return;
                    try
                    {
                        if (command == "RESTORE")
                        {
                            if (block != null) { block.Dispose(); block = null; }
                            write("RESTORED");
                        }
                        else
                        {
                            string[] parts = command.Split('\n');
                            int scope;
                            if (parts.Length != 3 || parts[0] != "BLOCK" || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out scope) ||
                                !Enum.IsDefined(typeof(NetworkBlockMode), scope)) throw new ArgumentException("Invalid network command.");
                            if (block != null) throw new InvalidOperationException("Restore the existing block before changing scope.");
                            NetworkBlockMode mode = (NetworkBlockMode)scope;
                            string path = ValidateExecutable(mode, Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])));
                            block = create(mode, path);
                            if (block == null) throw new InvalidOperationException("The network block was not created.");
                            write("BLOCKED");
                        }
                    }
                    catch (Exception ex)
                    {
                        // A failed restore ends the helper so RPC rundown still releases the dynamic session.
                        write("ERROR\n" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ex.Message)));
                        if (block != null) return;
                    }
                }
            }
            finally { if (block != null) block.Dispose(); }
        }
    }

    internal sealed class NetworkHelperFactory : INetworkHelperFactory
    {
        public INetworkHelperConnection Start()
        {
            string id = Guid.NewGuid().ToString("N");
            string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            NamedPipeServerStream pipe = new NamedPipeServerStream(NetworkPrivilegeHelper.PipeName(id), PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 4096, 4096, CreatePipeSecurity());
            Process helper = null;
            try
            {
                IAsyncResult accepting = pipe.BeginWaitForConnection(null, null);
                using (Process parent = Process.GetCurrentProcess())
                {
                    ProcessStartInfo launch = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,
                        NetworkPrivilegeHelper.Argument + " " + parent.Id.ToString(CultureInfo.InvariantCulture) + " " +
                        parent.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture) + " " + id + " " + token);
                    launch.UseShellExecute = true;
                    launch.Verb = "runas";
                    launch.WindowStyle = ProcessWindowStyle.Hidden;
                    helper = Process.Start(launch);
                }
                if (helper == null) throw new IOException("Windows did not start the network helper.");
                using (WaitHandle wait = accepting.AsyncWaitHandle)
                {
                    if (!wait.WaitOne(NetworkPrivilegeHelper.RequestTimeout))
                    {
                        pipe.Dispose();
                        try { pipe.EndWaitForConnection(accepting); } catch (IOException) { } catch (ObjectDisposedException) { }
                        throw new TimeoutException("The network helper did not connect.");
                    }
                    pipe.EndWaitForConnection(accepting);
                }
                uint clientId;
                if (!NetworkPipeNative.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out clientId) || clientId != (uint)helper.Id)
                    throw new UnauthorizedAccessException("An unrelated process connected to the network pipe.");
                NetworkPipeProtocol.Write(pipe, "HELLO\n" + token, NetworkPrivilegeHelper.RequestTimeout);
                NetworkPrivilegeHelper.RequireResponse(NetworkPipeProtocol.Read(pipe, NetworkPrivilegeHelper.RequestTimeout), "READY");
                return new NetworkPipeConnection(pipe, helper);
            }
            catch { pipe.Dispose(); if (helper != null) helper.Dispose(); throw; }
        }

        internal static PipeSecurity CreatePipeSecurity()
        {
            PipeSecurity security = new PipeSecurity();
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                security.AddAccessRule(new PipeAccessRule(identity.User, PipeAccessRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
            security.SetAccessRuleProtection(true, false);
            return security;
        }
    }

    internal sealed class NetworkPipeConnection : INetworkHelperConnection
    {
        private readonly NamedPipeServerStream pipe;
        private readonly Process helper;
        private int disposed;
        public event EventHandler Disconnected;

        internal NetworkPipeConnection(NamedPipeServerStream pipe, Process helper)
        {
            this.pipe = pipe; this.helper = helper;
            helper.Exited += OnExited;
            helper.EnableRaisingEvents = true;
        }
        public bool IsAlive
        {
            get { try { return Interlocked.CompareExchange(ref disposed, 0, 0) == 0 && !helper.HasExited; } catch (InvalidOperationException) { return false; } }
        }
        private void OnExited(object sender, EventArgs args)
        {
            EventHandler handler = Disconnected;
            if (handler != null) handler(this, EventArgs.Empty);
        }
        public string Request(string command)
        {
            if (!IsAlive) throw new IOException("The network helper is no longer running.");
            NetworkPipeProtocol.Write(pipe, command, NetworkPrivilegeHelper.RequestTimeout);
            return NetworkPipeProtocol.Read(pipe, NetworkPrivilegeHelper.RequestTimeout);
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            helper.Exited -= OnExited;
            pipe.Dispose();
            helper.Dispose();
        }
    }

    internal static class NetworkPipeProtocol
    {
        private const int MaxFrame = 32768;
        internal static void Write(Stream pipe, string text, int timeout)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            if (data.Length > MaxFrame) throw new InvalidDataException("Network message is too long.");
            byte[] frame = new byte[4 + data.Length];
            Array.Copy(BitConverter.GetBytes(data.Length), frame, 4);
            Array.Copy(data, 0, frame, 4, data.Length);
            IAsyncResult writing = pipe.BeginWrite(frame, 0, frame.Length, null, null);
            using (WaitHandle wait = writing.AsyncWaitHandle)
            {
                if (!wait.WaitOne(timeout))
                {
                    pipe.Dispose();
                    try { pipe.EndWrite(writing); } catch (IOException) { } catch (ObjectDisposedException) { }
                    throw new TimeoutException("Network helper write timed out.");
                }
                pipe.EndWrite(writing);
            }
        }
        internal static string Read(Stream pipe, int timeout)
        {
            byte[] header = ReadBytes(pipe, 4, timeout);
            int size = BitConverter.ToInt32(header, 0);
            if (size < 0 || size > MaxFrame) throw new InvalidDataException("Invalid network message length.");
            return Encoding.UTF8.GetString(ReadBytes(pipe, size, timeout));
        }
        private static byte[] ReadBytes(Stream pipe, int size, int timeout)
        {
            byte[] data = new byte[size];
            int offset = 0;
            Stopwatch elapsed = Stopwatch.StartNew();
            while (offset < size)
            {
                IAsyncResult reading = pipe.BeginRead(data, offset, size - offset, null, null);
                int left = timeout == Timeout.Infinite ? Timeout.Infinite : Math.Max(0, timeout - (int)elapsed.ElapsedMilliseconds);
                int read;
                using (WaitHandle wait = reading.AsyncWaitHandle)
                {
                    if (!wait.WaitOne(left))
                    {
                        pipe.Dispose();
                        try { pipe.EndRead(reading); } catch (IOException) { } catch (ObjectDisposedException) { }
                        throw new TimeoutException("Network helper read timed out.");
                    }
                    read = pipe.EndRead(reading);
                }
                if (read == 0) throw new EndOfStreamException("Network helper connection closed.");
                offset += read;
            }
            return data;
        }
    }

    internal static class NetworkPipeNative
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint processId);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint processId);
    }

    // Native calls are injectable: fault/recovery tests never change the machine's networking.
    internal interface INetworkWfpApi
    {
        IntPtr OpenDynamic();
        void Begin(IntPtr engine);
        void AddSublayer(IntPtr engine, Guid key);
        IntPtr GetApplicationId(string path);
        void FreeApplicationId(ref IntPtr appId);
        void AddBlock(IntPtr engine, Guid layer, Guid sublayer, IntPtr appId);
        void Commit(IntPtr engine);
        void Abort(IntPtr engine);
        void Close(IntPtr engine);
    }

    internal sealed class WfpNetworkSession : IDisposable
    {
        internal static readonly Guid[] Layers = {
            new Guid("c38d57d1-05a7-4c33-904f-7fbceee60e82"),
            new Guid("4a72393b-319f-44bc-84c3-ba54dcb3b6b4"),
            new Guid("e1cd9fe7-f4b5-4273-96c0-592e487b8650"),
            new Guid("a3b42c97-9f04-4672-b87e-cee9c483257f") };
        private readonly INetworkWfpApi api;
        private IntPtr engine;
        private WfpNetworkSession(INetworkWfpApi api, IntPtr engine) { this.api = api; this.engine = engine; }

        internal static WfpNetworkSession Create(INetworkWfpApi api, NetworkBlockMode mode, string path)
        {
            if (api == null) throw new ArgumentNullException("api");
            path = NetworkPrivilegeHelper.ValidateExecutable(mode, path);
            IntPtr engine = IntPtr.Zero, app = IntPtr.Zero;
            bool transaction = false;
            try
            {
                if (mode == NetworkBlockMode.Executable) app = api.GetApplicationId(path);
                engine = api.OpenDynamic();
                if (engine == IntPtr.Zero) throw new IOException("The filtering session could not be opened.");
                api.Begin(engine); transaction = true;
                Guid sublayer = Guid.NewGuid();
                api.AddSublayer(engine, sublayer);
                foreach (Guid layer in Layers) api.AddBlock(engine, layer, sublayer, app);
                api.Commit(engine); transaction = false;
                WfpNetworkSession result = new WfpNetworkSession(api, engine);
                engine = IntPtr.Zero;
                return result;
            }
            finally
            {
                try { if (engine != IntPtr.Zero && transaction) api.Abort(engine); }
                finally
                {
                    try { if (engine != IntPtr.Zero) api.Close(engine); }
                    finally { if (app != IntPtr.Zero) api.FreeApplicationId(ref app); }
                }
            }
        }

        public void Dispose()
        {
            IntPtr closing = Interlocked.Exchange(ref engine, IntPtr.Zero);
            if (closing != IntPtr.Zero) api.Close(closing);
        }
    }

    internal sealed class NativeNetworkWfpApi : INetworkWfpApi
    {
        private static readonly Guid AppIdCondition = new Guid("d78e1e87-8644-4ea5-9437-d809ecefc971");
        private static void Check(uint error, string operation)
        { if (error != 0) throw new Win32Exception(unchecked((int)error), operation + " failed (0x" + error.ToString("X8", CultureInfo.InvariantCulture) + ")."); }
        public IntPtr OpenDynamic()
        {
            WfpSession session = new WfpSession();
            session.sessionKey = Guid.NewGuid();
            session.displayData = new WfpDisplay { name = "InputStitch temporary network control" };
            session.flags = 1; // FWPM_SESSION_FLAG_DYNAMIC; never persistent or boot-time.
            session.txnWaitTimeoutInMSec = 5000;
            IntPtr engine;
            Check(FwpmEngineOpen0(null, 10, IntPtr.Zero, ref session, out engine), "Opening temporary network session");
            return engine;
        }
        public void Begin(IntPtr engine) { Check(FwpmTransactionBegin0(engine, 0), "Beginning network change"); }
        public void Commit(IntPtr engine) { Check(FwpmTransactionCommit0(engine), "Committing network change"); }
        public void Abort(IntPtr engine) { Check(FwpmTransactionAbort0(engine), "Reverting incomplete network change"); }
        public void Close(IntPtr engine) { Check(FwpmEngineClose0(engine), "Restoring network"); }
        public void AddSublayer(IntPtr engine, Guid key)
        {
            WfpSublayer sublayer = new WfpSublayer();
            sublayer.key = key;
            sublayer.displayData = new WfpDisplay { name = "InputStitch temporary network block" };
            sublayer.weight = ushort.MaxValue;
            Check(FwpmSubLayerAdd0(engine, ref sublayer, IntPtr.Zero), "Creating temporary network scope");
        }
        public IntPtr GetApplicationId(string path)
        {
            IntPtr app;
            Check(FwpmGetAppIdFromFileName0(path, out app), "Resolving executable network identity");
            if (app == IntPtr.Zero) throw new IOException("Windows returned an empty executable network identity.");
            return app;
        }
        public void FreeApplicationId(ref IntPtr appId) { FwpmFreeMemory0(ref appId); }
        public void AddBlock(IntPtr engine, Guid layer, Guid sublayer, IntPtr appId)
        {
            WfpFilter filter = new WfpFilter();
            filter.key = Guid.NewGuid();
            filter.displayData = new WfpDisplay { name = "InputStitch temporary network block" };
            filter.layer = layer; filter.sublayer = sublayer;
            filter.action = new WfpAction { type = 0x1001 }; // FWP_ACTION_BLOCK | terminating.
            // FWP_EMPTY selects automatic weight; our sublayer contains only our own blocks.
            IntPtr condition = IntPtr.Zero;
            try
            {
                if (appId != IntPtr.Zero)
                {
                    WfpCondition app = new WfpCondition { field = AppIdCondition, match = 0,
                        value = new WfpValue { type = 12, pointer = appId } }; // FWP_BYTE_BLOB_TYPE
                    condition = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WfpCondition)));
                    Marshal.StructureToPtr(app, condition, false);
                    filter.conditionCount = 1; filter.conditions = condition;
                }
                ulong id;
                Check(FwpmFilterAdd0(engine, ref filter, IntPtr.Zero, out id), "Adding temporary network filter");
            }
            finally { if (condition != IntPtr.Zero) Marshal.FreeHGlobal(condition); }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WfpDisplay { [MarshalAs(UnmanagedType.LPWStr)] public string name; [MarshalAs(UnmanagedType.LPWStr)] public string description; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpSession { public Guid sessionKey; public WfpDisplay displayData; public uint flags, txnWaitTimeoutInMSec, processId; public IntPtr sid, username; public int kernelMode; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpBlob { public uint size; public IntPtr data; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpSublayer { public Guid key; public WfpDisplay displayData; public uint flags; public IntPtr provider; public WfpBlob providerData; public ushort weight; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpValue { public uint type; public IntPtr pointer; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpCondition { public Guid field; public uint match; public WfpValue value; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpAction { public uint type; public Guid key; }
        [StructLayout(LayoutKind.Explicit)]
        internal struct WfpContext { [FieldOffset(0)] public ulong raw; [FieldOffset(0)] public Guid key; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WfpFilter
        {
            public Guid key; public WfpDisplay displayData; public uint flags; public IntPtr provider;
            public WfpBlob providerData; public Guid layer, sublayer; public WfpValue weight;
            public uint conditionCount; public IntPtr conditions; public WfpAction action;
            public WfpContext context; public IntPtr reserved; public ulong filterId; public WfpValue effectiveWeight;
        }
        [DllImport("fwpuclnt.dll", CharSet = CharSet.Unicode)]
        private static extern uint FwpmEngineOpen0(string server, uint authentication, IntPtr identity, ref WfpSession session, out IntPtr engine);
        [DllImport("fwpuclnt.dll")] private static extern uint FwpmEngineClose0(IntPtr engine);
        [DllImport("fwpuclnt.dll")] private static extern uint FwpmTransactionBegin0(IntPtr engine, uint flags);
        [DllImport("fwpuclnt.dll")] private static extern uint FwpmTransactionCommit0(IntPtr engine);
        [DllImport("fwpuclnt.dll")] private static extern uint FwpmTransactionAbort0(IntPtr engine);
        [DllImport("fwpuclnt.dll")] private static extern uint FwpmSubLayerAdd0(IntPtr engine, ref WfpSublayer sublayer, IntPtr security);
        [DllImport("fwpuclnt.dll")] private static extern uint FwpmFilterAdd0(IntPtr engine, ref WfpFilter filter, IntPtr security, out ulong filterId);
        [DllImport("fwpuclnt.dll", CharSet = CharSet.Unicode)] private static extern uint FwpmGetAppIdFromFileName0(string path, out IntPtr appId);
        [DllImport("fwpuclnt.dll")] private static extern void FwpmFreeMemory0(ref IntPtr memory);
    }
}
