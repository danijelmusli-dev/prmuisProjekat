using MrezeProjekat.Dashboards;
using MrezeProjekat.Helpers;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    public class OnionNode
    {
        public int NodePort                  { get; set; }
        public bool ClientToServer           { get; set; }
        public Instructions NodeInstructions { get; set; }

        private IPEndPoint _localEP; 

        private Socket _udpSocket;
        private Socket _tcpSocket;

        private static readonly Random _rand = new Random(); // generating node colors

        string nodeColor;

        public OnionNode() { }
        public OnionNode(IPEndPoint nodeEP, int nodePort)
        {
            this._localEP = nodeEP;
            this.NodePort = nodePort;
            this.ClientToServer = true;

            this.nodeColor = (Dashboard.nodeColors[_rand.Next(0, Dashboard.nodeColors.Length)]);

            // After we receive instructions from the server for the node (TCP)
            // we will initialize the UDP socket and bind it to the local endpoint.
            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this._udpSocket.Blocking = false;
            this._udpSocket.Bind(this._localEP);
        }

        public void RunNode(CancellationToken token)
        {
           //AnsiConsole.MarkupLineInterpolated($"[white][[NODE {this.NodePort}]] Starting polling loop...[/]");

            // node main loop
            // node is looping until the cancelatin from client is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // check for cancellation before polling
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] Cancellation requested, stopping node...[/]");
                    break;
                }

                // awaiting for data (Message object)
                if (this._udpSocket.Poll(1_000_000, SelectMode.SelectRead))
                {

                    AnsiConsole.MarkupLineInterpolated($"\t\t[{this.nodeColor}][[NODE {this.NodePort}]] recieved message [/]");
                    Message message = this.ReceiveFromPrevNode();

                    // validate the message (checksum and length)
                    if (!CheckMessage(message))
                    {
                        AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] invalid message, discarding...[/]");
                        continue;
                    }

                    // validate sending 
                    try
                    {
                        AnsiConsole.MarkupLineInterpolated($"\t\t[{this.nodeColor}][[NODE {this.NodePort}]] send message[/]");
                        this.SendToNextNode(message);
                    }
                    catch (Exception) { AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] nodes coliding[/]"); }
                    AnsiConsole.MarkupLine("\n");
                }
            }

            // closing the node socket
            this._udpSocket.Close();

        }

        public void SendToNextNode(Message message)
        {
            // Determine next endpoint based on current mode
            EndPoint nextEP = (ClientToServer) ?
                (this.NodeInstructions.NextNode ?? new IPEndPoint(IPAddress.Parse(Networking.ServerIPAdress), Networking.UdpServerPort))  // Client -> Server
              : (this.NodeInstructions.PrevNode ?? new IPEndPoint(IPAddress.Loopback, Networking.UdpClientPort)); // Server -> Client

            // peel off one layer of encryption from message
            string decryptedMessage = Encoding.UTF8.GetString(this.PeelOffLayer(message.Content));
            Message passNext = new Message(decryptedMessage);

            // send the instance of Message 
            byte[] data = passNext.ToBytes(); 
            Networking.SendUdp(data, this._udpSocket, nextEP);

            // switching the mode after sending
            this.ClientToServer = !this.ClientToServer;
        }

        public Message ReceiveFromPrevNode()
        {
            // Determine previous endpoint based on current mode
            EndPoint prevEP = (ClientToServer) ?
                (this.NodeInstructions.PrevNode ?? new IPEndPoint(IPAddress.Loopback, Networking.UdpClientPort))  // Client -> Server
              : (this.NodeInstructions.NextNode ?? new IPEndPoint(IPAddress.Parse(Networking.ServerIPAdress), Networking.UdpServerPort)); // Server -> Client

            // receive the instance of Message
            byte[] data = Networking.ListenForUDP(this._udpSocket, prevEP);
            if (data == null || data.Length == 0)
                // no data received (transient).
                // Return null or loop caller should handle null.
                return null;

            return Message.FromBytes(data);
        }

        public void ReceiveInstructionsForNode()
        {
            #region Node TCP Connection and Handshake
            this._tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //this._tcpSocket.Bind(this._localEP);

            while (!Networking.NodeHandshake(this.NodePort, this._tcpSocket))
            {
                AnsiConsole.MarkupLineInterpolated($"\t\t[indianred1][[NODE {this.NodePort}]] Handshake failed, retrying...[/]");
            }
            #endregion

            byte[] data = Networking.ListenForTcp(this._tcpSocket);
            this.NodeInstructions = Instructions.FromBytes(data);

            AnsiConsole.MarkupLineInterpolated($"\t\t[{this.nodeColor}][[NODE {this.NodePort}]] recieved instructions:[/]");
            Menu.PrintInstructions(this.NodeInstructions);

            #region TCP Socket Cleanup
            this._tcpSocket.Shutdown(SocketShutdown.Both);
            this._tcpSocket.Close();
            #endregion
        }

        private byte[] PeelOffLayer(string message) // removes one layer of encryption from message
        {
            // message is base64 string of the encrypted data,
            // so we need to convert it back to byte array before decryption

            if (message.Length % 4 != 0)
                throw new FormatException();

            byte[] cryptedMessage = Convert.FromBase64String(message);
            
            byte[] key = this.NodeInstructions[0].Key; // get the key given to this node for decryption
            byte[] iv  = this.NodeInstructions[0].IV;  // get the IV  given to this node for decryption

            // decrypt the message
            string decryptedMessage = CryptoHelper.DecryptStringFromBytes(cryptedMessage, key, iv);

            AnsiConsole.MarkupLineInterpolated($"\t\t[{this.nodeColor}][[NODE {this.NodePort}]] peeled off layer: [/][#A8A8A8]{new string(decryptedMessage.Take(30).ToArray())}...[/]");

            // return the decrypted message as byte array for the next node
            byte[] data = Encoding.UTF8.GetBytes(decryptedMessage);
            return data;
        }

        private bool CheckMessage(Message msg)
        {
            if (msg == null)
            {
                AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] message is null[/]");
                return false;
            }

            if (string.IsNullOrEmpty(msg.Content))
            {
                AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] message content is null or empty[/]");
                return false;
            }

            int sum = msg.Content.Sum(c => (int)c); // calculate the checksum of the message content (ASCII sum)
            if (msg.CheckSum != sum)
            {
                AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] message checksum mismatch {msg.CheckSum} : {sum}[/]");
                return false;
            }

            int len = msg.Content.Length; // calculate the length of the message content
            if (msg.MessageLenght != len)
            {
                AnsiConsole.MarkupLineInterpolated($"\t\t[red][[NODE {this.NodePort}]] message length mismatch {msg.MessageLenght} : {len}[/]");
                return false;
            }

            return true;
        }

    }
}