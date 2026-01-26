using MrezeProjekat.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MrezeProjekat.Models
{
    public class OnionNode
    {
        public int NodeId  { get; set; }
        public int Timeout { get; set; }
        public Instructions NodeInstructions { get; set; }

        private Socket _udpSocket;

        public OnionNode() { }
        public OnionNode(Instructions instructions, IPEndPoint localEP, int timeout)
        { 
            this.NodeInstructions = instructions;
            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            this._udpSocket.Blocking = false; // neblokirajuci rezim
            this.Timeout = timeout;           // pool timeout

            this._udpSocket.Bind(localEP);    // bindujemo jer cemo i slusati i slati
        }

        void RunNode()
        {
            Console.WriteLine("[NODE] Starting polling loop...");

            while (true)
            {
                // awaiting for data (Message object)
                if (this._udpSocket.Poll(1_000_000, SelectMode.SelectRead))
                {
                    Message message = this.ReceiveFromPrevNode();
                    Console.WriteLine($"[NODE {this.NodeId}] recieved message");
                    
                    this.SendToNextNode(message);
                    Console.WriteLine($"[NODE {this.NodeId}] send message");
                }
                else
                {
                    Console.WriteLine($"[NODE {this.NodeId}] waiting...");
                }
            }

        }

        public void SendToNextNode(Message message)
        {
            // send the instance of Message 
            string decryptedMessage = Encoding.UTF8.GetString(this.PeelOffLayer(message.Content));
            Message passNext = new Message(decryptedMessage);

            IPEndPoint nextEP = this.NodeInstructions.NextNode ?? new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60000);

            byte[] data = passNext.ToBytes();
            this._udpSocket.SendTo(data, 0, data.Length, SocketFlags.None, nextEP);
        }

        public Message ReceiveFromPrevNode()
        {
            // recieving the instance of Message
            EndPoint prevEP = this.NodeInstructions.PrevNode ?? new IPEndPoint(IPAddress.Any, 0);

            byte[] buffer = new byte[4096]; // 4096 stand UDP size
            int bytesReceived = this._udpSocket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref prevEP);

            // filtering data
            byte[] actualData = new byte[bytesReceived];
            Array.Copy(buffer, actualData, bytesReceived);

            return Message.FromBytes(actualData);
        }

        private byte[] PeelOffLayer(string message) // removes one layer of encryption from message
        {
            byte[] cryptedMessage = Encoding.UTF8.GetBytes(message);

            byte[] key = this.NodeInstructions[this.NodeId].Key;
            byte[] iv  = this.NodeInstructions[this.NodeId].IV;

            string decryptedMessage = CryptoHelper.DecryptStringFromBytes(cryptedMessage, key, iv);
            byte[] data = Encoding.UTF8.GetBytes(decryptedMessage);
            return data;
        }

    }
}
