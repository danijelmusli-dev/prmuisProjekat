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
        const int TcpPort = 50001;
        const int UdpPort = 60001;
        const int NodeBasePort = 5501;
        static void Main(string[] args)
        {
            // Create the server socket (TCP)
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, TcpPort);  

            serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serverSocket.Bind(serverEP);
            serverSocket.Listen(10);

            // Create a task to listen for UDP messages
            // we do this because UPD is blocking so we put it in a separate thread
            Task.Run(() => ListenForUPD());

            Console.WriteLine("Waiting for client...");
            Socket acceptedSocket = serverSocket.Accept();
            Console.WriteLine("Client connected.");

            // recieve client request
            Request req = RecieveRequest(acceptedSocket);

            // send instructions (client only)
            Instructions ins = SendInstructions(acceptedSocket, req);

            // send instructions for every node
            SendInstructionsForNodes(ins, req);


            // zatvori sve na kraju
            acceptedSocket.Close();
            serverSocket.Close();

            Console.ReadKey();
        }

        public static Request RecieveRequest(Socket acceptedSocket)
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

        public static Instructions SendInstructions(Socket acceptedSocket, Request req)
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
        public static void SendInstructionsForNodes(Instructions ins, Request req)
        {
            // send instructions for every node
            for (int i = 0; i < req.NodeNum; i++)
            {
                IPEndPoint nextEP = (i == (req.NodeNum - 1)) ? null : new IPEndPoint(IPAddress.Loopback, NodeBasePort + (i + 1));
                IPEndPoint prevEP = (i == 0) ? null : new IPEndPoint(IPAddress.Loopback, NodeBasePort + (i - 1));
                CryptoKey[] nodeKey = new CryptoKey[] { ins[i] };

                Instructions nodeIns = new Instructions(nodeKey,prevEP,nextEP);
                byte[] data = nodeIns.ToBytes();

                TcpClient node = new TcpClient();
                node.Connect(IPAddress.Loopback, NodeBasePort + i);

                NetworkStream stream = node.GetStream();

                // first send length
                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                stream.Write(lengthBuffer, 0, lengthBuffer.Length);

                // then send data    
                stream.Write(data, 0, data.Length);

                stream.Flush();
                node.Close();
            }
        }

        public static void ListenForUPD()
        {
            using (UdpClient client = new UdpClient(UdpPort))
            {
                while (true)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = client.Receive(ref remoteEP);

                    Console.WriteLine($"Received {data.Length} bytes from {remoteEP}");
                    Console.WriteLine(Message.FromBytes(data).Content);
                }
            }
        }

    }
}
