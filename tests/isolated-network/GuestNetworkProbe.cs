using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InputStitch.NetworkAcceptance
{
    internal static class GuestNetworkProbe
    {
        private static TcpClient tcp4, tcp6;
        private static UdpClient udp4, udp6;
        private static string[] endpoints;
        public static int Main(string[] args)
        {
            if (args.Length != 6) return 2;
            endpoints = args;
            Console.WriteLine("READY");
            Console.Out.Flush();
            string command;
            while ((command = Console.ReadLine()) != null && command != "QUIT")
            {
                try
                {
                    bool six = command.EndsWith("6", StringComparison.Ordinal);
                    IPAddress address = IPAddress.Parse(endpoints[six ? 3 : 0]);
                    int tcpPort = int.Parse(endpoints[six ? 4 : 1]), udpPort = int.Parse(endpoints[six ? 5 : 2]);
                    if (command == "TCP4" || command == "TCP6") using (TcpClient client = ConnectTcp(address, tcpPort)) EchoTcp(client);
                    else if (command == "UDP4" || command == "UDP6") using (UdpClient client = ConnectUdp(address, udpPort)) EchoUdp(client);
                    else if (command == "OPEN4" || command == "OPEN6")
                    {
                        TcpClient client = ConnectTcp(address, tcpPort);
                        if (six) { if (tcp6 != null) tcp6.Close(); tcp6 = client; } else { if (tcp4 != null) tcp4.Close(); tcp4 = client; }
                        EchoTcp(client);
                    }
                    else if (command == "UDPOPEN4" || command == "UDPOPEN6")
                    {
                        UdpClient client = ConnectUdp(address, udpPort);
                        if (six) { if (udp6 != null) udp6.Close(); udp6 = client; } else { if (udp4 != null) udp4.Close(); udp4 = client; }
                        EchoUdp(client);
                    }
                    else if (command == "HELD4" || command == "HELD6") EchoTcp(six ? tcp6 : tcp4);
                    else if (command == "UDPHELD4" || command == "UDPHELD6") EchoUdp(six ? udp6 : udp4);
                    else throw new ArgumentException("Unknown probe command.");
                    Console.WriteLine("OK " + command);
                }
                catch (Exception ex)
                {
                    SocketException socket = ex as SocketException;
                    if (socket == null && ex.InnerException is SocketException) socket = (SocketException)ex.InnerException;
                    bool networkFailure = socket != null || ex is TimeoutException || ex is EndOfStreamException;
                    Console.WriteLine((networkFailure ? "BLOCKED " : "ERROR ") + command + " " + ex.GetType().Name +
                        (socket == null ? "" : " native=" + socket.NativeErrorCode) +
                        (networkFailure ? "" : " " + ex.ToString().Replace('\r', ' ').Replace('\n', ' ')));
                }
                Console.Out.Flush();
            }
            if (tcp4 != null) tcp4.Close(); if (tcp6 != null) tcp6.Close(); if (udp4 != null) udp4.Close(); if (udp6 != null) udp6.Close();
            return 0;
        }
        private static TcpClient ConnectTcp(IPAddress address, int port)
        {
            TcpClient client = new TcpClient(address.AddressFamily);
            try
            {
                client.SendTimeout = 1000; client.ReceiveTimeout = 1000;
                // Retain the socket: TcpClient.Close clears Client, so calling its
                // EndConnect afterwards can throw NullReferenceException on .NET Framework.
                Socket socket = client.Client;
                IAsyncResult pending = socket.BeginConnect(address, port, null, null);
                using (System.Threading.WaitHandle wait = pending.AsyncWaitHandle)
                {
                    if (!wait.WaitOne(1000))
                    {
                        socket.Close();
                        try { socket.EndConnect(pending); } catch (SocketException) { } catch (ObjectDisposedException) { }
                        throw new TimeoutException("TCP timeout");
                    }
                    socket.EndConnect(pending);
                }
                return client;
            }
            catch { client.Close(); throw; }
        }
        private static UdpClient ConnectUdp(IPAddress address, int port)
        { UdpClient client = new UdpClient(address.AddressFamily); client.Client.SendTimeout = 1000; client.Client.ReceiveTimeout = 1000; client.Connect(address, port); return client; }
        private static byte[] Payload() { return Encoding.ASCII.GetBytes("InputStitch:" + Guid.NewGuid().ToString("N")); }
        private static void EchoTcp(TcpClient client)
        {
            if (client == null) throw new IOException("No TCP flow was opened.");
            byte[] outgoing = Payload(), incoming = new byte[outgoing.Length]; NetworkStream stream = client.GetStream();
            stream.Write(outgoing, 0, outgoing.Length);
            int offset = 0; while (offset < incoming.Length) { int count = stream.Read(incoming, offset, incoming.Length - offset); if (count == 0) throw new EndOfStreamException(); offset += count; }
            if (Encoding.ASCII.GetString(outgoing) != Encoding.ASCII.GetString(incoming)) throw new IOException("TCP echo nonce mismatch.");
        }
        private static void EchoUdp(UdpClient client)
        {
            if (client == null) throw new IOException("No UDP flow was opened.");
            byte[] outgoing = Payload(); client.Send(outgoing, outgoing.Length); IPEndPoint peer = null;
            if (Encoding.ASCII.GetString(client.Receive(ref peer)) != Encoding.ASCII.GetString(outgoing)) throw new IOException("UDP echo nonce mismatch.");
        }
    }
}
