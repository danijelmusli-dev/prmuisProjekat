using MrezeProjekat.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    public class OnionNode
    {
        public int NodePort  { get; set; }
        public Instructions NodeInstructions { get; set; }

        private IPEndPoint _localEP;

        private Socket _udpSocket;
        private Socket _tcpSocket;

        public OnionNode() { }
        public OnionNode(IPEndPoint nodeEP, int nodePort)
        {
            this._localEP = nodeEP;
            this.NodePort = nodePort;

            // nakon prijema Instrukcija pokreni UDP socket
            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //this._udpSocket.Blocking = false;
            this._udpSocket.Bind(this._localEP);
        }

        public void RunNode()
        {
            Console.WriteLine($"[NODE {this.NodePort}] Starting polling loop...");

            while (true)
            {
                // awaiting for data (Message object)
                if (this._udpSocket.Poll(1_000_000, SelectMode.SelectRead))
                {
                    Message message = this.ReceiveFromPrevNode();
                    Console.WriteLine($"[NODE {this.NodePort}] recieved message {message.Content}");
                    
                    this.SendToNextNode(message);
                    Console.WriteLine($"[NODE {this.NodePort}] send message");
                }
                else
                {
                    Console.WriteLine($"[NODE {this.NodePort}] waiting...");
                }
            }

        }

        public void SendToNextNode(Message message)
        {
            // send the instance of Message 
            string decryptedMessage = Encoding.UTF8.GetString(this.PeelOffLayer(message.Content));
            Message passNext = new Message(decryptedMessage);

            byte[] data = passNext.ToBytes(); 
            Networking.SendUdp(data, this._udpSocket, this.NodeInstructions.NextNode as IPEndPoint);
        }

        public Message ReceiveFromPrevNode()
        {
            // recieving the instance of Message
            EndPoint prevEP = this.NodeInstructions.PrevNode ?? new IPEndPoint(IPAddress.Any, Networking.UdpClientPort);

            byte[] data = Networking.ListenForUPD(this._udpSocket, prevEP);
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

        }

        private byte[] PeelOffLayer(string message) // removes one layer of encryption from message
        {
            byte[] cryptedMessage = Encoding.UTF8.GetBytes(message);

            byte[] key = this.NodeInstructions[0].Key;
            byte[] iv  = this.NodeInstructions[0].IV;

            string decryptedMessage = CryptoHelper.DecryptStringFromBytes(cryptedMessage, key, iv);
            byte[] data = Encoding.UTF8.GetBytes(decryptedMessage);
            return data;
        }

    }
}
