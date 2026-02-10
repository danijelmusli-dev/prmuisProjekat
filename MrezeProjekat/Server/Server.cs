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
using System.Security.Cryptography;
using System.Security.Policy;
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
            Socket serverSocketTCP = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocketTCP.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); 
            IPEndPoint serverTCP_EP = new IPEndPoint(IPAddress.Any, Networking.TcpServerPort);

            serverSocketTCP.Bind(serverTCP_EP);
            serverSocketTCP.Listen(10);

            // Create the server socket (UDP)
            Socket serverSocketUDP = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverUDP_EP = new IPEndPoint(IPAddress.Any, Networking.UdpServerPort);

            serverSocketUDP.Bind(serverUDP_EP);

            // Enabling the UTF8 for the Dashboard
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            bool endStringReceived = false;

            Menu.OnionLogo();

            #region Connecting with client
            // Form the TCP connection with client
            AnsiConsole.MarkupLine("[yellow]Waiting for client...[/]");
            Socket acceptedSocket = serverSocketTCP.Accept();
            IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
            AnsiConsole.MarkupLine($"[green]Client connected: {(acceptedSocket.RemoteEndPoint)}[/]\n");
            #endregion

            #region Client Request
            // recieve client request
            Request req = RecieveRequestFromClient(acceptedSocket);
            AnsiConsole.MarkupLine($"[yellow]Received request from: {acceptedSocket.LocalEndPoint as IPEndPoint}[/]");
            #endregion

            #region Instructions
            // send instructions (client only)
            AnsiConsole.MarkupLine("[orange1]Sending instructions to client...[/]");
            Instructions ins = SendInstructionsToClient(acceptedSocket, req);

            // send instructions for every node
            AnsiConsole.MarkupLine("[yellow]Sending instructions to nodes...[/]");
            SendInstructionsForNodes(ins, req, serverSocketTCP);
            #endregion

            #region Last Node
            // get the laststNode endpoint
            int lastNodePort = Networking.NodeBasePort + req.NodeNum - 1;
            IPEndPoint lastNodeEP = new IPEndPoint(IPAddress.Parse(clientEP.Address.ToString()), lastNodePort);
            #endregion

            // cancellation token for server UDP listener
            CancellationTokenSource ctsListener = new CancellationTokenSource();
            CancellationToken tokenListener = ctsListener.Token;

            //Task listenerTask = Task.Run(() => ListenForNodeResponseUDP(req, ins, serverSocketUDP, lastNodeEP, tokenListener, dashboard), tokenListener);

            while (!endStringReceived)
            {

                byte[] buffer = Networking.ListenForUDP(serverSocketUDP, lastNodeEP);
                if (buffer.Length == 0) continue;

                Message msg = Message.FromBytes(buffer);
                AnsiConsole.MarkupLineInterpolated($"\t[Cyan][[SERVER]] recived message from [white]{lastNodeEP}[/]: [white italic]{msg.Content}[/][/]");

                if (msg.Content.Equals(Networking.EndString))
                {
                    AnsiConsole.MarkupLine("[red]Client Disconnected[/]");
                    AnsiConsole.MarkupLine("[yellow]Stopping Server...[/]");
                    break;
                }

                Array.Clear(buffer, 0, buffer.Length);
                string responseContent = $"SERVER RESPONSE to '{msg.Content}'";
                Message response = CryptoHelper.CryptNTimes(responseContent, req.NodeNum, ins, false);

                AnsiConsole.MarkupLineInterpolated($"\t[Cyan][[SERVER]] sent message to [white]{lastNodeEP}[/][/]");
                buffer = response.ToBytes();
                Networking.SendUdp(buffer, serverSocketUDP, lastNodeEP);
            }


            //Thread.Sleep(1000);

            AnsiConsole.MarkupLine("[red]Onion nodes have been stopped.[/]");

            #region Socket closing and cleanup
            acceptedSocket.Shutdown(SocketShutdown.Both);
            acceptedSocket.Close();
            acceptedSocket.Dispose();

            serverSocketTCP.Close();
            serverSocketTCP.Dispose();

            serverSocketUDP.Close();
            #endregion

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
            serverSocket.Listen(10);
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

                // accept the node socket
                Socket nodeSocket = serverSocket.Accept();

                // handshake and send instructions
                AnsiConsole.MarkupLine($"[yellow1]Waiting handshake from [white]Node[[{Networking.NodeBasePort + i}]][/] [/]");
                byte[] handshakeData = new byte[5];
                nodeSocket.Receive(handshakeData);

                // check if the message is handshake
                if(Encoding.UTF8.GetString(handshakeData).Equals("READY"))
                    AnsiConsole.MarkupLine($"[yellow1]Handshake received from [white]Node[[{Networking.NodeBasePort + i}]][/] [/]");

                // send instructions
                AnsiConsole.MarkupLine($"[yellow1]Sending instructions to [white]Node[[{Networking.NodeBasePort + i}]][/] [/]");
                Networking.SendTcp(data, nodeSocket);
            }
        }

        public static void ListenForNodeResponseUDP(Request req, Instructions ins, Socket listenerSocket, EndPoint nodeEP, CancellationToken token, ServerDashboard dash)
        {
            while (!token.IsCancellationRequested)
            {
                if (token.IsCancellationRequested)
                    break;

                byte[] buffer = Networking.ListenForUDP(listenerSocket, nodeEP);
                if (buffer.Length == 0) continue;

                Message msg = Message.FromBytes(buffer);
                AnsiConsole.MarkupLine($"[[SERVER]] recived message from {nodeEP}: {msg.Content}");

                if (msg.Content.Equals(Networking.EndString))
                    break;

                Array.Clear(buffer, 0, buffer.Length);
                string responseContent = $"SERVER RESPONSE to '{msg.Content}'";
                Message response = CryptoHelper.CryptNTimes(responseContent, req.NodeNum, ins, false);
                
                AnsiConsole.MarkupLine($"[[SERVER]] sent message {nodeEP}");
                buffer = response.ToBytes();
                Networking.SendUdp(buffer, listenerSocket, nodeEP);
            }
        }

    }
}
