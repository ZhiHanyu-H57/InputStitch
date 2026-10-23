using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InputStitch.NetworkAcceptance
{
    // Deliberately a separate executable path. Only this process is blocked in live tests.
    internal static class NetworkProbe
    {
        private static TcpClient held4, held6;
        private static UdpClient heldUdp4, heldUdp6;
        public static int Main(string[] args)
        {
            if (args.Length != 6) return 2;
            string line;
            while ((line = Console.ReadLine()) != null && line != "QUIT")
            {
                try
                {
                    bool six = line.EndsWith("6", StringComparison.Ordinal);
                    IPAddress address = IPAddress.Parse(args[six ? 3 : 0]);
                    int tcpPort = int.Parse(args[six ? 4 : 1]), udpPort = int.Parse(args[six ? 5 : 2]);
                    if (line.StartsWith("OPEN", StringComparison.Ordinal))
                    {
                        TcpClient opened = Connect(address, tcpPort);
                        if (six) { if (held6 != null) held6.Close(); held6 = opened; }
                        else { if (held4 != null) held4.Close(); held4 = opened; }
                        Echo(opened);
                    }
                    else if (line.StartsWith("HELD", StringComparison.Ordinal)) Echo(six ? held6 : held4);
                    else if (line.StartsWith("TCP", StringComparison.Ordinal)) using (TcpClient client = Connect(address, tcpPort)) Echo(client);
                    else if (line.StartsWith("UDPOPEN", StringComparison.Ordinal))
                    {
                        UdpClient opened = OpenUdp(address, udpPort);
                        if (six) { if (heldUdp6 != null) heldUdp6.Close(); heldUdp6 = opened; }
                        else { if (heldUdp4 != null) heldUdp4.Close(); heldUdp4 = opened; }
                        EchoUdp(opened);
                    }
                    else if (line.StartsWith("UDPHELD", StringComparison.Ordinal)) EchoUdp(six ? heldUdp6 : heldUdp4);
                    else if (line.StartsWith("UDP", StringComparison.Ordinal)) using (UdpClient udp = OpenUdp(address, udpPort)) EchoUdp(udp);
                    else throw new ArgumentException("Unknown probe command");
                    Console.WriteLine("OK " + line);
                }
                catch (Exception ex) { Console.WriteLine("BLOCKED " + line + " " + ex.GetType().Name); }
                Console.Out.Flush();
            }
            if (held4 != null) held4.Close(); if (held6 != null) held6.Close();
            if (heldUdp4 != null) heldUdp4.Close(); if (heldUdp6 != null) heldUdp6.Close();
            return 0;
        }
        private static UdpClient OpenUdp(IPAddress address, int port)
        {
            UdpClient udp = new UdpClient(address.AddressFamily);
            udp.Client.ReceiveTimeout = 700; udp.Client.SendTimeout = 700; udp.Connect(address, port); return udp;
        }
        private static void EchoUdp(UdpClient udp)
        {
            if (udp == null) throw new IOException("No existing UDP flow");
            byte[] ping = Encoding.ASCII.GetBytes("IS01"); udp.Send(ping, ping.Length);
            IPEndPoint sender = null;
            if (Encoding.ASCII.GetString(udp.Receive(ref sender)) != "IS01") throw new IOException("UDP echo mismatch");
        }
        private static TcpClient Connect(IPAddress address, int port)
        {
            TcpClient client = new TcpClient(address.AddressFamily);
            try
            {
                client.ReceiveTimeout = 700; client.SendTimeout = 700;
                IAsyncResult connecting = client.BeginConnect(address, port, null, null);
                using (System.Threading.WaitHandle wait = connecting.AsyncWaitHandle)
                { if (!wait.WaitOne(700)) throw new TimeoutException("TCP connection timeout"); client.EndConnect(connecting); }
                return client;
            }
            catch { client.Close(); throw; }
        }
        private static void Echo(TcpClient client)
        {
            if (client == null) throw new IOException("No existing TCP connection");
            byte[] ping = Encoding.ASCII.GetBytes("IS01"), result = new byte[4];
            NetworkStream stream = client.GetStream(); stream.Write(ping, 0, ping.Length);
            int offset = 0; while (offset < 4) { int read = stream.Read(result, offset, 4 - offset); if (read == 0) throw new EndOfStreamException(); offset += read; }
            if (Encoding.ASCII.GetString(result) != "IS01") throw new IOException("TCP echo mismatch");
        }
    }
}
