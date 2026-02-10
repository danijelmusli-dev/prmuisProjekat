using MrezeProjekat;
using MrezeProjekat.Helpers;
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client
{
    public class Client
    {
        static async Task Main(string[] args)
        {
            // Create the client socket (TCP)
            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocketTCP.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Servern EndPoint (TCP)
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(Networking.ServerIPAdress), Networking.TcpServerPort);
            clientSocketTCP.Connect(serverEP);

            // Create the client socket (UDP)
            Socket clientSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocketUDP.Bind(new IPEndPoint(IPAddress.Loopback, Networking.UdpClientPort));

            // Conditions for stopping the interaction
            int nodeNum = Menu.GetIntegerInput(1, 10, "Node Number");
            int maxMessages = Menu.GetIntegerInput(1, 20, "Max Messages");
            bool endStringReceived = false;

            Console.OutputEncoding = Encoding.UTF8;
            AnsiConsole.Clear();

            // form the request (coming soon)
            // send request to the server
            // umesto new Request napraviceo f-ju gde korisnik formira zahtev
            Request req = new Request(clientSocketTCP.LocalEndPoint as IPEndPoint, nodeNum, maxMessages);
            AnsiConsole.MarkupLine("[yellow]Sending request to server...[/]");
            SendRequest(clientSocketTCP, req);
            Menu.PrintRequest(req);

            // recieve client instructions 
            AnsiConsole.MarkupLine("\t[yellow]Recieving instructions from server...[/]");
            Instructions ins = RecieveInstructions(clientSocketTCP);

            #region Forming Onion Nodes
            // form the Onion nodes
            List<OnionNode> onionNodes = new List<OnionNode>();
            for (int i = 0; i < req.NodeNum; i++)
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, Networking.NodeBasePort + i);
                OnionNode node = new OnionNode(localEP, Networking.NodeBasePort + i);
                onionNodes.Add(node);
            }
            #endregion

            #region Node Instructions
            // recieve instructions for the nodes
            List<Task> nodeTasks = new List<Task>();
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.ReceiveInstructionsForNode()));
                nodeTasks.Last().Wait();
            }
            #endregion

            #region First Node
            // get the firstNode endpoint
            OnionNode firstNode = onionNodes.First();
            IPEndPoint firstNodeEP = new IPEndPoint(IPAddress.Loopback, firstNode.NodePort);
            #endregion

            #region Client UDP listener tast
            // cancelation token for listener
            CancellationTokenSource ctsListener = new CancellationTokenSource();
            CancellationToken tokenListener = ctsListener.Token;

            // start server listener for UDP response from last node
            Task listenerTask = Task.Run(() => ListenForNodeResponseUDP(clientSocketUDP, firstNodeEP, tokenListener), tokenListener);
            #endregion

            #region Nodes listener task
            // cancellation token for nodes
            CancellationTokenSource ctsNodes = new CancellationTokenSource();
            CancellationToken tokenNodes = ctsNodes.Token;

            // start every Onion node
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.RunNode(tokenNodes), tokenNodes));
            }
            #endregion

            #region Main Loop
            while (!endStringReceived && (maxMessages != 0))
            {
                string input = AnsiConsole.Ask<string>("[bold italic]>> Type your message here: [/]");

                // crypt the message N times
                Message nextMessage = CryptoHelper.CryptNTimes(input, req.NodeNum, ins, true);
                int sent = Networking.SendUdp(nextMessage.ToBytes(), clientSocketUDP, firstNodeEP);

                Thread.Sleep(500); // wait for response

                maxMessages -= 1;
                endStringReceived = input.Equals(Networking.EndString);

                if (endStringReceived || (maxMessages == 0))
                {
                    sent = Networking.SendUdp(CryptoHelper.CryptNTimes(Networking.EndString, req.NodeNum, ins, true).ToBytes(), clientSocketUDP, firstNodeEP);
                    break;
                }   
            }

            #region Task cancellation and cleanup
            ctsNodes.Cancel();
            ctsListener.Cancel();
            try
            {
                await Task.WhenAll(nodeTasks);
                //await listenerTask;
            }
            catch (Exception ex) { }
            #endregion
            #endregion

            #region Socket closing and cleanup
            // closing of the socket
            clientSocketTCP.Shutdown(SocketShutdown.Both);
            clientSocketTCP.Close();
            clientSocketTCP.Dispose();

            clientSocketUDP.Close();
            clientSocketUDP.Dispose();
            #endregion

            AnsiConsole.MarkupLine("[indianred]Client stopping...[/]");
            // Console.ReadKey();
        }

        public static void SendRequest(Socket clientSocket, Request request)
        {
            // get the local endpoint of the client socket and set it as the sender in the request
            IPEndPoint localEP = clientSocket.LocalEndPoint as IPEndPoint;
            request.Sender = localEP;

            // serialize the request to bytes
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

        public static void ListenForNodeResponseUDP(Socket listenerSocket, EndPoint nodeEP, CancellationToken token) 
        {
            // looping while listener task is not cancelled
            while (!token.IsCancellationRequested)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    byte[] buffer = Networking.ListenForUDP(listenerSocket, nodeEP);

                    Message msg = Message.FromBytes(buffer);
                    AnsiConsole.MarkupLineInterpolated($"\t[yellow]Recieved message from {nodeEP}: {msg.Content} [/]\n\n");

                    if (msg.Content.Equals(Networking.EndString))
                        break;
                }
                catch (SocketException ex)
                {
                    if (token.IsCancellationRequested) break;

                }
            }

            AnsiConsole.MarkupLine("[indianred]Client UDP listener stopping...[/]");
        }

    }
}
