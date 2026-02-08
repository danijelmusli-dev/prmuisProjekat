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

namespace Client
{
    public class Client
    {
        static async Task Main(string[] args)
        {
            // Create the client socket (TCP)
            Socket clientSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocketTCP.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Create the client socket (UDP)
            Socket clientSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocketUDP.Bind(new IPEndPoint(IPAddress.Loopback, Networking.UdpClientPort));

            // Servern EndPoint
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, Networking.TcpServerPort);
            clientSocketTCP.Connect(serverEP);


            // Conditions for stopping the interaction
            int  maxMessages = 5;
            bool endStringReceived = false;

            #region spectre

            ClientDashboard dashboard = new ClientDashboard();

            bool pauseDrawing = false;
            Task dasgTask = Task.Run(() =>
            {
                AnsiConsole.Live(dashboard.Root)
                .Start(ctx =>
                {
                    while (!endStringReceived)
                    {
                        if (!pauseDrawing)
                        {
                            dashboard.RefreshPanels();
                            ctx.Refresh();
                        }
                        // osveži prikaz
                        Thread.Sleep(100);          // delay između refresh-a
                    }
                });

            });

            #endregion

            // form the request (coming soon)
            // send request to the server
            // umesto new Request napraviceo f-ju gde korisnik formira zahtev
            Request req = new Request(clientSocketTCP.LocalEndPoint as IPEndPoint, 5, maxMessages);
            dashboard.AddClient("[yellow]Sending request to server...[/]");
            SendRequest(clientSocketTCP, req);
            Menu.PrintRequest(dashboard, req);

            // recieve client instructions 
            dashboard.AddClient("[yellow]Recieving instructions from server...[/]");
            Instructions ins = RecieveInstructions(clientSocketTCP);

            // form the Onion nodes
            List<OnionNode> onionNodes = new List<OnionNode>();
            for (int i = 0; i < req.NodeNum; i++)
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, Networking.NodeBasePort + i);
                OnionNode node = new OnionNode(localEP, Networking.NodeBasePort + i);
                node.Dashboard = dashboard;
                onionNodes.Add(node);
            }

            // recieve instructions for the nodes
            List<Task> nodeTasks = new List<Task>();
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.ReceiveInstructionsForNode()));
                nodeTasks.Last().Wait();
            }

            // get the firstNode endpoint
            OnionNode  firstNode = onionNodes.First();
            IPEndPoint firstNodeEP = new IPEndPoint(IPAddress.Loopback, firstNode.NodePort);

            // cancelation token for listener
            CancellationTokenSource ctsListener = new CancellationTokenSource();
            CancellationToken tokenListener = ctsListener.Token;

            // start server listener for UDP response from last node
            Task listenerTask = Task.Run(() => ListenForNodeResponseUDP(clientSocketUDP, firstNodeEP, tokenListener, dashboard), tokenListener);


            // cancellation token for nodes
            CancellationTokenSource ctsNodes = new CancellationTokenSource();
            CancellationToken tokenNodes = ctsNodes.Token;

            // start every Onion node
            foreach (OnionNode node in onionNodes)
            {
                nodeTasks.Add(Task.Run(() => node.RunNode(tokenNodes), tokenNodes));
            }

            while (!endStringReceived)
            {
                string input = Console.ReadLine();
                pauseDrawing = true;
                dashboard.AddInput($"[bold]{input}[/]");
                pauseDrawing = false;

                // crypt the message N times
                Message nextMessage = CryptoHelper.CryptNTimes(input, req.NodeNum, ins, true);
                
                int sent = Networking.SendUdp(nextMessage.ToBytes(), clientSocketUDP, firstNodeEP, dashboard);

                Thread.Sleep(500); // wait for response
                endStringReceived = input.Equals(Networking.EndString);

            }

            ctsListener.Cancel();
            ctsNodes.Cancel();
            try
            {
                await Task.WhenAll(nodeTasks);
                await listenerTask;
                await dasgTask;
            }
            catch (OperationCanceledException)
            {
                dashboard.AddClient("[red]Onion nodes have been stopped.[/]");
            }

            // closing of the socket
            clientSocketTCP.Shutdown(SocketShutdown.Both);
            clientSocketTCP.Close();
            clientSocketTCP.Dispose();

            clientSocketUDP.Close();
            clientSocketUDP.Dispose();

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

        public static void ListenForNodeResponseUDP(Socket listenerSocket, EndPoint nodeEP, CancellationToken token, ClientDashboard dash) 
        {
            while (!token.IsCancellationRequested)
            {
                byte[] buffer = Networking.ListenForUDP(listenerSocket, nodeEP, dash);

                Message msg = Message.FromBytes(buffer);
                dash.AddServer($"[yellow]Recieved message from {nodeEP}: {msg.Content} [/]");

                if (msg.Content.Equals("END"))
                    break;
            }
            dash.AddServer("[indianred]Client UDP listener stopping...[/]");
        }

    }
}
