using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MrezeProjekat;
using MrezeProjekat.Helpers;
using MrezeProjekat.Models;

namespace Client
{
    public class Client
    {
        static async Task Main(string[] args)
        {
            // Create the client socket (TCP)
            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocketTCP.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Create the client socket (UDP)// UDP klijent socket
            Socket clientSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocketUDP.Bind(new IPEndPoint(IPAddress.Loopback, Networking.UdpClientPort));

            // Servern EndPoint
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, Networking.TcpServerPort);
            clientSocketTCP.Connect(serverEP);
            Console.WriteLine($"{(clientSocketTCP.RemoteEndPoint as IPEndPoint).Port}");

            // form the request (coming soon)
            // send request to the server
            // umesto new Request napraviceo f-ju gde korisnik formira zahtev
            Request req = new Request(clientSocketTCP.LocalEndPoint as IPEndPoint, 3, 5);
            Console.WriteLine("Sending request to server...");
            SendRequest(clientSocketTCP, req);

            // recieve client instructions 
            Console.WriteLine("Recieving instructions from server...");
            Instructions ins = RecieveInstructions(clientSocketTCP);
            Console.Write(ins.ToString() + "\n\n");

            // form the Onion nodes
            List<OnionNode> onionNodes = new List<OnionNode>();
            for (int i = 0; i < req.NodeNum; i++)
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, Networking.NodeBasePort + i);
                OnionNode node = new OnionNode(localEP, Networking.NodeBasePort + i);
                
                onionNodes.Add(node);
            }

            // recieve instructions for the nodes
            List<Task> nodeTasks = new List<Task>();
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.ReceiveInstructionsForNode()));
                nodeTasks.Last().Wait();
            }

            // crypt the message N times
            Message message = CryptoHelper.CryptNTimes("medjed", req.NodeNum, ins, true);
            Console.WriteLine($"\nClient formed the message to send: {message.Content}");
            Console.WriteLine();

            OnionNode firstNode = onionNodes.First();
            IPEndPoint nodeEP = new IPEndPoint(IPAddress.Loopback, firstNode.NodePort);
            bool stop = false;
            Task listenerTask = Task.Run(() =>
            {

                while (!stop)
                {
                    byte[] buffer = Networking.ListenForUDP(clientSocketUDP, nodeEP);

                    Message msg = Message.FromBytes(buffer);
                    Console.WriteLine($"Primljen paket od {nodeEP}: {msg.Content}");

                    Console.WriteLine($"[CLIENT] Primljen paket od {nodeEP}: {msg.Content}");
                    stop = true;
                }
            });

            //start every Onion node
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            nodeTasks.Clear();
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.RunNode(token), token));
            }

            // send the message to the first node
            int sent = Networking.SendUdp(message.ToBytes(), clientSocketUDP, nodeEP);
            Console.WriteLine($"\nUDP sending {sent} bytes to first node [{nodeEP.Address}:{nodeEP.Port}]...");


            await listenerTask;

            // wait for response from server
            Message message1 = CryptoHelper.CryptNTimes("pingvin", req.NodeNum, ins, true);
            Networking.SendUdp(message1.ToBytes(), clientSocketUDP, nodeEP);
            Console.WriteLine($"\nSecond sending {sent} bytes to first node [{nodeEP.Address}:{nodeEP.Port}]...");
         

            // closing of the socket
            clientSocketTCP.Shutdown(SocketShutdown.Both);
            clientSocketTCP.Close();
            clientSocketTCP.Dispose();

            await listenerTask;
            clientSocketUDP.Close();
            clientSocketUDP.Dispose();

            cts.Cancel();
            try
            {
                await Task.WhenAll(nodeTasks);
                stop = true;
                await listenerTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOnion nodes have been stopped.");
            }


            Console.ReadKey();
        }

        public static void SendRequest(Socket clientSocket, Request request)
        {
            IPEndPoint localEP = clientSocket.LocalEndPoint as IPEndPoint;
            request.Sender = localEP;

            byte[] data = request.ToBytes();

            // sending length of data
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            clientSocket.Send(lengthBuffer);

            // sending data
            clientSocket.Send(data);
        }
        public static Instructions RecieveInstructions(Socket clientSocket)
        {
            // recieve the data length
            byte[] lengthBuffer = new byte[4];
            int readLen = clientSocket.Receive(lengthBuffer);
            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

            // recieve data
            byte[] buffer = new byte[dataLength];
            int totalReceived = 0;
            while (totalReceived < dataLength)
            {
                int received = clientSocket.Receive(buffer, totalReceived, dataLength - totalReceived, SocketFlags.None);
                if (received == 0) break; // CLOSE THE CONNECTION
                totalReceived += received;
            }

            // Deserialize the Instructons object
            Instructions ins = Instructions.FromBytes(buffer);

            return ins;
        }



    }
}
