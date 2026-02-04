using MrezeProjekat;
using MrezeProjekat.Helpers;
using MrezeProjekat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        static async Task Main(string[] args)
        {
            // Create the server socket (TCP)
            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocketTCP.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); 
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, Networking.TcpServerPort);

            serverSocketTCP.Bind(serverEP);
            serverSocketTCP.Listen(10);

            // Create the server socket (UDP)
            Socket serverSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocketUDP.Bind(new IPEndPoint(IPAddress.Loopback, Networking.UdpServerPort));

            // Form the TCP connection with client
            Console.WriteLine("Waiting for client...");
            Socket acceptedSocket = serverSocketTCP.Accept();
            Console.WriteLine($"Client connected: {(acceptedSocket.LocalEndPoint)}");

            // recieve client request
            Request req = RecieveRequestFromClient(acceptedSocket);
            Console.WriteLine($"\nReceived request from: {acceptedSocket.LocalEndPoint as IPEndPoint}");

            // send instructions (client only)
            Console.WriteLine("\nSending instructions to client...");
            Instructions ins = SendInstructionsToClient(acceptedSocket, req);

            // send instructions for every node
            Console.WriteLine("\nSending instructions to nodes...");
            SendInstructionsForNodes(ins, req, serverSocketTCP);

            // recieve last udp package
            // Placeholder za remote endpoint (popuniće se adresom pošiljaoca)
            int lastNodePort = Networking.NodeBasePort + req.NodeNum - 1;
            IPEndPoint lastNodeEP = new IPEndPoint(IPAddress.Loopback, lastNodePort);


            // cancellation token for server UDP listener
            CancellationTokenSource ctsListener = new CancellationTokenSource();
            CancellationToken tokenListener = ctsListener.Token;

            Task listenerTask = Task.Run(() => ListenForNodeResponseUDP(req, ins, serverSocketUDP, lastNodeEP, tokenListener), tokenListener);
         
            // zatvori sve na kraju
            await listenerTask;

            acceptedSocket.Shutdown(SocketShutdown.Both);
            acceptedSocket.Close();
            acceptedSocket.Dispose();

            serverSocketTCP.Close();
            serverSocketTCP.Dispose();

            serverSocketUDP.Close();

            Console.ReadKey();
        }

        public static Request RecieveRequestFromClient(Socket acceptedSocket)
        {
            // primi dužinu (4 bajta)
            byte[] lengthBuffer = new byte[4];
            acceptedSocket.Receive(lengthBuffer);
            int length = BitConverter.ToInt32(lengthBuffer, 0);

            // primi tačno toliko bajtova
            byte[] buffer = new byte[length];
            int totalReceived = 0;
            while (totalReceived < length)
            {
                int received = acceptedSocket.Receive(buffer, totalReceived, length - totalReceived, SocketFlags.None);
                totalReceived += received;
            }

            Request req = Request.FromBytes(buffer);
            return req;
        }

        public static Instructions SendInstructionsToClient(Socket acceptedSocket, Request req)
        {
            Instructions ins = new Instructions(req.NodeNum, null, null);

            // generisi kljuceve
            for (int i = 0; i < req.NodeNum; i++)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 128;
                    aes.GenerateKey();
                    aes.GenerateIV();
                    ins[i] = new CryptoKey(aes.Key, aes.IV);
                }
            }

            byte[] data = ins.ToBytes();

            // prvo pošalji dužinu
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            acceptedSocket.Send(lengthBuffer);

            // zatim pošalji podatke
            acceptedSocket.Send(data);

            return ins;
        }
       
        public static void SendInstructionsForNodes(Instructions ins, Request req, Socket serverSocket)
        {
            // send instructions for every node
            for (int i = 0; i < req.NodeNum; i++)
            {
                // form next and prev IPEndPoint
                IPEndPoint nextEP = (i == (req.NodeNum - 1)) ? null : new IPEndPoint(IPAddress.Loopback, Networking.NodeBasePort + (i + 1));
                IPEndPoint prevEP = (i == 0) ? null : new IPEndPoint(IPAddress.Loopback, Networking.NodeBasePort + (i - 1));
                CryptoKey[] nodeKey = new CryptoKey[] { ins[i] };

                // form Instructions object for the node
                Instructions nodeIns = new Instructions(nodeKey, prevEP, nextEP);
                byte[] data = nodeIns.ToBytes();

                // form IPEndPoint for the node
                IPEndPoint nodeEP = new IPEndPoint(IPAddress.Loopback, Networking.NodeBasePort + i);

                serverSocket.Listen(10);
                Socket nodeSocket = serverSocket.Accept();

                // handshake and send instructions
                Console.WriteLine($"\nWaiting handshake from Node[{Networking.NodeBasePort + i}]:");
                byte[] handshakeData = new byte[5];
                nodeSocket.Receive(handshakeData);

                if(Encoding.UTF8.GetString(handshakeData).Equals("READY"))
                    Console.WriteLine($"Handshake received from Node[{Networking.NodeBasePort + i}]");

                Networking.SendTcp(data, nodeSocket);
                Console.WriteLine($"\nSending Node[{Networking.NodeBasePort + i}] {i + 1}  instructions:");
            }
        }

        public static void ListenForNodeResponseUDP(Request req, Instructions ins, Socket listenerSocket, EndPoint nodeEP, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (token.IsCancellationRequested)
                    break;

                byte[] buffer = Networking.ListenForUDP(listenerSocket, nodeEP);

                Message msg = Message.FromBytes(buffer);
                Console.WriteLine($"[SERVER] recived message from {nodeEP}: {msg.Content}");

                if (msg.Content.Equals(Networking.EndString))
                {
                    buffer = (CryptoHelper.CryptNTimes(Networking.EndString, req.NodeNum, ins, false)).ToBytes();
                    Networking.SendUdp(buffer, listenerSocket, nodeEP);
                    break;
                }

                Array.Clear(buffer, 0, buffer.Length);
                string responseContent = $"SERVER RESPONSE to '{msg.Content}'";
                Message response = CryptoHelper.CryptNTimes(responseContent, req.NodeNum, ins, false);
                
                Console.WriteLine($"[SERVER] sent message {nodeEP}");
                buffer = response.ToBytes();
                Networking.SendUdp(buffer, listenerSocket, nodeEP);
            }
        }

    }
}
