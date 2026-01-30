using MrezeProjekat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Helpers
{
    public static class Networking
    {
        // Base port for onion nodes
        public const int NodeBasePort  = 5501;

        // Server ports
        public const int TcpServerPort = 50001;
        public const int UdpServerPort = 60001;

        // Client ports
        public const int TcpClientPort = 50000;
        public const int UdpClientPort = 60000;

        // UPD send and receive
        public static byte[] ListenForUPD(Socket listenerSocket, EndPoint remoteEP)
        {
            byte[] buffer = new byte[4096];

            try 
            {
                int recieved = listenerSocket.ReceiveFrom(buffer, SocketFlags.None, ref remoteEP);

                byte[] data = new byte[recieved];
                Array.Copy(buffer, data, recieved);

                Console.WriteLine($"UDP received {data.Length} bytes from {remoteEP}");
                Console.WriteLine(Message.FromBytes(data).Content);
                return data;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
                return null;
            }

        }
        public static void SendUdp(byte[] data, Socket acceptedSocket, EndPoint remoteEP)
        {
            acceptedSocket.SendTo(data, remoteEP);
            Console.WriteLine($"UDP sent {data.Length} bytes to {remoteEP}");
        }


        // TCP send and receive
        public static byte[] ListenForTcp(Socket listenerSocket)
        {

            if (!listenerSocket.Connected)
                throw new InvalidOperationException($"Socket {listenerSocket.RemoteEndPoint} is not connected.");

            // primi dužinu
            byte[] lengthBuffer = new byte[4];
            int read = listenerSocket.Receive(lengthBuffer);
            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

            // primi podatke
            byte[] buffer = new byte[dataLength];
            int totalReceived = 0;
            while (totalReceived < dataLength)
            {
                int received = listenerSocket.Receive(buffer, totalReceived, dataLength - totalReceived, SocketFlags.None);
                totalReceived += received;
            }

            Console.WriteLine($"TCP received {buffer.Length} bytes from {listenerSocket.RemoteEndPoint}");

            return buffer;
        }
        public static void SendTcp(byte[] data, Socket acceptedSocket)
        {
            if (!acceptedSocket.Connected)
                throw new InvalidOperationException($"Socket {acceptedSocket.RemoteEndPoint} is not connected.");

            // send length first
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            acceptedSocket.Send(lengthBuffer);

            // send data
            int totalSent = 0;
            while (totalSent < data.Length)
            {
                int sent = acceptedSocket.Send(data, totalSent, data.Length - totalSent, SocketFlags.None);
                totalSent += sent;
            }

        }
        
        // Handshake
        public static bool NodeHandshake(int port, Socket nodeSocket)
        {
            Console.WriteLine($"[NODE {port}] sending handshake to server");
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, Networking.TcpServerPort);

            nodeSocket.Connect(serverEP);
            int sent = nodeSocket.Send(Encoding.UTF8.GetBytes("READY"));
            
            Console.WriteLine($"[NODE {port}] waiting for server response");
            return (sent == 5);
        }

    }
}
