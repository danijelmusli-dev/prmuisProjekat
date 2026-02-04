using MrezeProjekat.Helpers;
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
        public int NodePort  { get; set; }
        public bool ClientToServer { get; set; }
        public Instructions NodeInstructions { get; set; }

        private IPEndPoint _localEP;

        private Socket _udpSocket;
        private Socket _tcpSocket;

        public OnionNode() { }
        public OnionNode(IPEndPoint nodeEP, int nodePort)
        {
            this._localEP = nodeEP;
            this.NodePort = nodePort;
            this.ClientToServer = true;

            // nakon prijema Instrukcija pokreni UDP socket
            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this._udpSocket.Blocking = false;
            this._udpSocket.Bind(this._localEP);
        }

        public void RunNode(CancellationToken token)
        {
            Console.WriteLine($"[NODE {this.NodePort}] Starting polling loop...");

            // node main loop
            // node is looping until the cancelatin from client is requested
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"\n[NODE {this.NodePort}] Cancellation requested, stopping node...");
                    break;
                }

                // awaiting for data (Message object)
                if (this._udpSocket.Poll(1_000_000, SelectMode.SelectRead))
                {

                    Message message = this.ReceiveFromPrevNode();
                    Console.WriteLine($"[NODE {this.NodePort}] recieved message");

                    if(!CheckMessage(message))
                    {                        
                        Console.WriteLine($"[NODE {this.NodePort}] invalid message, discarding...");
                        continue;
                    }

                    this.SendToNextNode(message);
                    Console.WriteLine($"[NODE {this.NodePort}] send message");
                }
                else
                {
                    Console.WriteLine($"[NODE {this.NodePort}] waiting...");
                }
            }

            this._udpSocket.Close();

        }

        public void SendToNextNode(Message message)
        {
            // Determine next endpoint based on current mode
            EndPoint nextEP = (ClientToServer) ?
                (this.NodeInstructions.NextNode ?? new IPEndPoint(IPAddress.Loopback, Networking.UdpServerPort))  // Client -> Server
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
                (this.NodeInstructions.PrevNode ?? new IPEndPoint(IPAddress.Any, Networking.UdpClientPort))  // Client -> Server
              : (this.NodeInstructions.NextNode ?? new IPEndPoint(IPAddress.Any, Networking.UdpServerPort)); // Server -> Client

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
            this._tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this._tcpSocket.Bind(this._localEP);

            while (!Networking.NodeHandshake(this.NodePort, this._tcpSocket))
            {
                Console.WriteLine($"[NODE {this.NodePort}] Handshake failed, retrying...");
            }

            byte[] data = Networking.ListenForTcp(this._tcpSocket);
            NodeInstructions = Instructions.FromBytes(data);
            
            Console.WriteLine($"\n[NODE {this.NodePort}] recieved instructions:");
            Console.WriteLine(this.NodeInstructions.ToString() + '\n');

            this._tcpSocket.Shutdown(SocketShutdown.Both);
            this._tcpSocket.Close();
        }

        private byte[] PeelOffLayer(string message) // removes one layer of encryption from message
        {

            byte[] cryptedMessage = Convert.FromBase64String(message);
            
            byte[] key = this.NodeInstructions[0].Key;
            byte[] iv  = this.NodeInstructions[0].IV;

            string decryptedMessage = CryptoHelper.DecryptStringFromBytes(cryptedMessage, key, iv);
            Console.WriteLine($"[NODE {this.NodePort}] peeled off layer: {decryptedMessage}");
            byte[] data = Encoding.UTF8.GetBytes(decryptedMessage);
            return data;
        }

        private bool CheckMessage(Message msg)
        {
            if (msg == null)
            {
                Console.WriteLine($"[NODE {this.NodePort}] message is null");
                return false;
            }

            if (string.IsNullOrEmpty(msg.Content))
            {
                Console.WriteLine($"[NODE {this.NodePort}] message content is null or empty");
                return false;
            }

            int sum = msg.Content.Sum(c => (int)c);
            if (msg.CheckSum != sum)
            {
                Console.WriteLine($"[NODE {this.NodePort}] message checksum mismatch {msg.CheckSum} : {sum}");
                return false;
            }

            int len = msg.Content.Length;
            if (msg.MessageLenght != len)
            {
                Console.WriteLine($"[NODE {this.NodePort}] message length mismatch {msg.MessageLenght} : {len}");
                return false;
            }

            return true;
        }

    }
}
