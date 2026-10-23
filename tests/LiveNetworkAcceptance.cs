using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace InputStitch
{
    internal static class LiveNetworkAcceptance
    {
        private static readonly string DirectoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string ProbePath { get { return Path.Combine(DirectoryPath, "NetworkProbe.exe"); } }
        private static NetworkControlOptions Options() { return new NetworkControlOptions { Enabled = true, Mode = NetworkBlockMode.Executable, ExecutablePath = ProbePath }; }
        public static int Main(string[] args)
        {
            if (NetworkPrivilegeHelper.TryRunHelper(args)) return Environment.ExitCode;
            if (args.Length == 3 && args[0] == "--live-holder") return Holder(args[1], args[2]);
            if (args.Length != 1 || args[0] != "--allow-process-network-test")
            { Console.WriteLine("Built only. Run Run-LiveNetworkAcceptance.ps1 -Run to authorize tests restricted to NetworkProbe.exe."); return 0; }
            try
            {
                IPAddress v4 = null, v6 = null;
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (UnicastIPAddressInformation unicast in nic.GetIPProperties().UnicastAddresses)
                    {
                        IPAddress address = unicast.Address;
                        if (IPAddress.IsLoopback(address)) continue;
                        if (address.AddressFamily == AddressFamily.InterNetwork && v4 == null) v4 = address;
                        if (address.AddressFamily == AddressFamily.InterNetworkV6 && !address.IsIPv6Multicast && v6 == null) v6 = address;
                    }
                }
                if (v4 == null) throw new InvalidOperationException("A non-loopback local IPv4 address is required.");
                using (EchoServer four = new EchoServer(v4))
                using (EchoServer six = v6 == null ? null : new EchoServer(v6))
                {
                    string probeArgs = four.Arguments + " " + (six == null ? "::1 1 1" : six.Arguments);
                    using (Probe target = new Probe(ProbePath, probeArgs))
                    using (Probe negative = new Probe(Path.Combine(DirectoryPath, "NegativeProbe.exe"), probeArgs))
                    using (NetworkController controller = new NetworkController())
                    {
                        AssertTraffic(target, six != null, true, "baseline"); AssertTraffic(negative, six != null, true, "negative-baseline");
                        Require(target.Command("OPEN4"), true, "open-existing-TCP4");
                        Require(target.Command("UDPOPEN4"), true, "open-existing-UDP4");
                        if (six != null) { Require(target.Command("OPEN6"), true, "open-existing-TCP6"); Require(target.Command("UDPOPEN6"), true, "open-existing-UDP6"); }
                        if (!controller.Toggle(Options()) || !controller.IsBlocked) throw new Exception(controller.LastError);
                        try
                        {
                            AssertTraffic(target, six != null, false, "blocked");
                            Require(target.Command("HELD4"), false, "existing-TCP4-reauthorization");
                            Require(target.Command("UDPHELD4"), false, "existing-UDP4-reauthorization");
                            if (six != null) { Require(target.Command("HELD6"), false, "existing-TCP6-reauthorization"); Require(target.Command("UDPHELD6"), false, "existing-UDP6-reauthorization"); }
                            AssertTraffic(negative, six != null, true, "negative-while-blocked");
                        }
                        finally { if (!controller.Restore()) throw new Exception(controller.LastError); }
                        AssertTraffic(target, six != null, true, "restored");
                        Console.WriteLine("PASS normal NetworkController/helper block and restore");
                        CrashRecovery(target, negative, six != null, "parent");
                        CrashRecovery(target, negative, six != null, "helper");
                    }
                }
                if (v6 == null) Console.WriteLine("SKIP IPv6: no non-loopback local IPv6 address available.");
                Console.WriteLine("RESULT live executable-only acceptance passed; whole-computer blocks=0"); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        private static void AssertTraffic(Probe probe, bool ipv6, bool allowed, string stage)
        {
            foreach (string command in ipv6 ? new string[] { "TCP4", "UDP4", "TCP6", "UDP6" } : new string[] { "TCP4", "UDP4" })
                Require(probe.Command(command), allowed, stage + "/" + command);
        }
        private static void Require(string result, bool allowed, string check)
        {
            bool success = result != null && result.StartsWith("OK ", StringComparison.Ordinal);
            if (success != allowed) throw new Exception(check + " failed: " + result);
            Console.WriteLine("PASS " + check + " " + result);
        }
        private static void CrashRecovery(Probe target, Probe negative, bool ipv6, string kind)
        {
            string id = Guid.NewGuid().ToString("N"), marker = Path.Combine(DirectoryPath, ".live-network-" + id);
            ProcessStartInfo launch = new ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location, "--live-holder " + kind + " " + id);
            launch.WindowStyle = ProcessWindowStyle.Hidden;
            launch.UseShellExecute = kind == "helper";
            if (launch.UseShellExecute) launch.Verb = "runas"; else launch.CreateNoWindow = true;
            using (Process holder = Process.Start(launch))
            {
                try
                {
                    Stopwatch timeout = Stopwatch.StartNew();
                    while (!File.Exists(marker)) { if (holder.HasExited || timeout.ElapsedMilliseconds > 60000) throw new Exception(kind + " holder did not activate"); Thread.Sleep(50); }
                    Require(target.Command("TCP4"), false, kind + "-before-crash");
                    Require(negative.Command("TCP4"), true, kind + "-negative-control");
                    // Acknowledge observation so the fixture cannot terminate before the check.
                    File.WriteAllText(marker + ".observed", "observed");
                    if (!holder.WaitForExit(10000)) throw new Exception(kind + " holder did not exit");
                    if (holder.ExitCode != 33 && holder.ExitCode != 34) throw new Exception(kind + " fixture failed with code " + holder.ExitCode);
                    timeout.Restart(); string result;
                    do { result = target.Command("TCP4"); if (result.StartsWith("OK ", StringComparison.Ordinal)) break; Thread.Sleep(50); } while (timeout.ElapsedMilliseconds < 10000);
                    Require(result, true, kind + "-automatic-recovery"); AssertTraffic(target, ipv6, true, kind + "-all-protocols-restored");
                }
                finally
                {
                    // Only fixture-owned marker files. Never remove WFP objects by enumeration.
                    if (File.Exists(marker)) File.Delete(marker); if (File.Exists(marker + ".observed")) File.Delete(marker + ".observed");
                }
            }
        }
        private static int Holder(string kind, string id)
        {
            Guid parsed;
            if ((kind != "parent" && kind != "helper") || !Guid.TryParseExact(id, "N", out parsed)) return 2;
            string marker = Path.Combine(DirectoryPath, ".live-network-" + id);
            try
            {
                // This fixture has exactly one fixed scope and cannot select EntireComputer.
                if (kind == "parent")
                {
                    using (NetworkController controller = new NetworkController())
                    {
                        if (!controller.Toggle(Options())) return 3;
                        File.WriteAllText(marker, "parent"); WaitForObservation(marker);
                        Environment.Exit(34); // Bypass controller.Dispose, like a terminated macro process.
                    }
                }
                else
                {
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return 4;
                    WfpNetworkSession session = WfpNetworkSession.Create(new NativeNetworkWfpApi(), NetworkBlockMode.Executable, ProbePath);
                    File.WriteAllText(marker, "helper"); WaitForObservation(marker);
                    GC.KeepAlive(session);
                    Environment.Exit(33); // No Dispose/finally: Windows must remove the dynamic session.
                }
                return 5;
            }
            catch { return 6; }
        }
        private static void WaitForObservation(string marker)
        {
            Stopwatch timeout = Stopwatch.StartNew();
            while (!File.Exists(marker + ".observed") && timeout.ElapsedMilliseconds < 10000) Thread.Sleep(50);
        }
        private sealed class Probe : IDisposable
        {
            private readonly Process process;
            internal Probe(string executable, string arguments)
            {
                ProcessStartInfo start = new ProcessStartInfo(executable, arguments);
                start.UseShellExecute = false; start.CreateNoWindow = true; start.WindowStyle = ProcessWindowStyle.Hidden;
                start.RedirectStandardInput = true; start.RedirectStandardOutput = true;
                process = Process.Start(start);
            }
            internal string Command(string command)
            {
                process.StandardInput.WriteLine(command); process.StandardInput.Flush();
                Task<string> reading = process.StandardOutput.ReadLineAsync();
                if (!reading.Wait(4000)) throw new TimeoutException("Probe response timed out: " + command);
                string result = reading.Result;
                if (result == null || (!result.StartsWith("OK ", StringComparison.Ordinal) && !result.StartsWith("BLOCKED ", StringComparison.Ordinal))) throw new IOException("Probe exited unexpectedly");
                return result;
            }
            public void Dispose()
            {
                try { if (!process.HasExited) { process.StandardInput.WriteLine("QUIT"); process.StandardInput.Flush(); if (!process.WaitForExit(2000)) process.Kill(); } }
                finally { process.Dispose(); }
            }
        }
        private sealed class EchoServer : IDisposable
        {
            private readonly TcpListener tcp;
            private readonly UdpClient udp;
            private volatile bool disposed;
            internal string Arguments;
            internal EchoServer(IPAddress address)
            {
                tcp = new TcpListener(address, 0); tcp.Start(); udp = new UdpClient(new IPEndPoint(address, 0));
                Arguments = address.ToString() + " " + ((IPEndPoint)tcp.LocalEndpoint).Port + " " + ((IPEndPoint)udp.Client.LocalEndPoint).Port;
                Thread accept = new Thread(AcceptTcp), receive = new Thread(ReceiveUdp); accept.IsBackground = true; receive.IsBackground = true; accept.Start(); receive.Start();
            }
            private void AcceptTcp()
            {
                while (!disposed)
                {
                    try
                    {
                        TcpClient client = tcp.AcceptTcpClient();
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            using (client) try
                            {
                                NetworkStream stream = client.GetStream(); byte[] buffer = new byte[256]; int count;
                                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0) stream.Write(buffer, 0, count);
                            }
                            catch (IOException) { }
                        });
                    }
                    catch (SocketException) { if (!disposed) throw; }
                    catch (ObjectDisposedException) { return; }
                }
            }
            private void ReceiveUdp()
            {
                while (!disposed)
                {
                    try { IPEndPoint sender = null; byte[] data = udp.Receive(ref sender); udp.Send(data, data.Length, sender); }
                    catch (SocketException) { if (!disposed) throw; }
                    catch (ObjectDisposedException) { return; }
                }
            }
            public void Dispose() { disposed = true; tcp.Stop(); udp.Close(); }
        }
    }
}
