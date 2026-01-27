using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat;
using MrezeProjekat.Helpers;
using MrezeProjekat.Models;

namespace Client
{
    public class Client
    {
        const int TcpServerPort = 50001;
        const int UdpServerPort = 60001;
        const int NodeBasePort = 5501;
        static void Main(string[] args)
        {
            // Create the client socket (TCP)
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, TcpServerPort);

            clientSocket.Connect(serverEP);

            // form the request (coming soon)
            // send request to the server
            // umesto new Request napraviceo f-ju gde korisnik formira zahtev
            Request req = new Request(null, 3, 5);
            SendRequest(clientSocket, req);

            // recieve client instructions 
            Instructions ins = RecieveInstructions(clientSocket);
            //Console.Write(ins.ToString());

            // form the Onion nodes
            List<OnionNode> onionNodes = new List<OnionNode>();
            for (int i = 0; i < req.NodeNum; i++)
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, NodeBasePort + i);
                OnionNode node = new OnionNode(localEP);
                
                onionNodes.Add(node);
            }

            // recieve instructions for the nodes
            List<Task> nodeTasks = new List<Task>();
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.ReceiveInstructionsForNode()));
            }
            Task.WaitAll(nodeTasks.ToArray());

            foreach (OnionNode node in onionNodes)
            {
                Console.WriteLine(node.NodeInstructions.ToString());
            }

            // crypt the message N times
            Message message = CryptoHelper.CryptNTimes("gas", 3 , ins);


            // closing of the socket
            clientSocket.Close();

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
