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
        static void Main(string[] args)
        {
            // Create the server socket (TCP)
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, Networking.TcpServerPort);

            serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serverSocket.Bind(serverEP);
            serverSocket.Listen(10);

            // Create a task to listen for UDP messages
            // we do this because UPD is blocking so we put it in a separate thread
            //Task.Run(() => { while (true) Networking.ListenForUPD(Networking.UdpServerPort, serverEP); });

            Console.WriteLine("Waiting for client...");
            Socket acceptedSocket = serverSocket.Accept();
            Console.WriteLine("Client connected.");

            // recieve client request
            Request req = RecieveRequestFromClient(acceptedSocket);
            Console.WriteLine($"\nReceived request from: {acceptedSocket.LocalEndPoint as IPEndPoint}");

            // send instructions (client only)
            Console.WriteLine("\nSending instructions to client...");
            Instructions ins = SendInstructionsToClient(acceptedSocket, req);

            // send instructions for every node
            Console.WriteLine("\nSending instructions to nodes...");
            SendInstructionsForNodes(ins, req, serverSocket);

            // zatvori sve na kraju
            acceptedSocket.Shutdown(SocketShutdown.Both);
            acceptedSocket.Close();
            serverSocket.Close();

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

    }
}
