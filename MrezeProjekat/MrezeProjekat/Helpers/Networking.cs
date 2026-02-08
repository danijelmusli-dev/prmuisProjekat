using MrezeProjekat.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat.Dashboards;

namespace MrezeProjekat.Helpers
{
    public static class Networking
    {
        // Base port for onion nodes
        public const int NodeBasePort  = 5501;

        // Server ports
        public const int TcpServerPort = 50001;
        public const int UdpServerPort = 50002;

        // Client ports
        // TcpClientPort not declared because of operating system TIME_WAIT period 
        // Client gets his TCP port dynamically by Operating System
        public const  int UdpClientPort = 60002;

        // String for ending the communication
        public const string EndString = "END";

        // UPD send and receive
        public static byte[] ListenForUDP(Socket listenerSocket, EndPoint senderEP, Dashboard dash)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[4096];

            try
            {
                int recieved = listenerSocket.ReceiveFrom(buffer, SocketFlags.None, ref remoteEP);

                byte[] data = new byte[recieved];
                Array.Copy(buffer, data, recieved);

                dash.AddNetwork($"[green1]UDP[/] [italic white]{listenerSocket.LocalEndPoint}[/] [LightSteelBlue]received[/] [yellow2]{data.Length}[/] bytes from [white bold italic]{senderEP}[/]");
                return data;

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
            return (Array.Empty<byte>());
        }
        public static int SendUdp(byte[] data, Socket acceptedSocket, EndPoint remoteEP, Dashboard dash)
        {
            int sent = acceptedSocket.SendTo(data, remoteEP); 
            dash.AddNetwork( $"[green1]UDP[/] [italic white]{acceptedSocket.LocalEndPoint}[/] [LightCoral]sent[/] [yellow2]{data.Length}[/] bytes to [white bold italic]{remoteEP}[/]");
            return sent;
        }


        // TCP send and receive
        public static byte[] ListenForTcp(Socket listenerSocket, Dashboard dash)
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

            dash.AddNetwork($"[cyan1]TCP[/] [white italic]{listenerSocket.LocalEndPoint}[/] [LightSteelBlue]received[/] [yellow2]{buffer.Length}[/] bytes from [white bold italic]{listenerSocket.RemoteEndPoint}[/]");
            
            return buffer;
        }
        public static void SendTcp(byte[] data, Socket acceptedSocket, Dashboard dash)
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

            dash.AddNetwork($"[cyan1]TCP[/] [black on white]{acceptedSocket.LocalEndPoint}[/] [LightCoral]sent[/] [black on yellow]{totalSent}[/] bytes to [white bold italic]{acceptedSocket.RemoteEndPoint}[/]");
        }

        // Handshake
        public static bool NodeHandshake(int port, Socket nodeSocket, Dashboard dash)
        {
            dash.AddServer($"[yellow][[NODE {port}]] sending handshake to server [/]");
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, Networking.TcpServerPort);

            nodeSocket.Connect(serverEP);
            int sent = nodeSocket.Send(Encoding.UTF8.GetBytes("READY"));
            
            dash.AddServer($"[yellow][[NODE {port}]] waiting for server response [/]");
            return (sent == 5);
        }

    }
}
