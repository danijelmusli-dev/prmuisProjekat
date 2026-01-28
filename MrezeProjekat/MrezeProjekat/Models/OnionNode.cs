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
        private TcpListener _tcpListener;
        private Socket _udpSocket;

        public OnionNode() { }
        public OnionNode(IPEndPoint ep, int nodePort)
        {
            this._localEP = ep;
            this.NodePort = nodePort;
            this._tcpListener = new TcpListener(this._localEP);
            this._tcpListener.Start();
        }

        public OnionNode(Instructions instructions, IPEndPoint localEP)
        { 
            this.NodeInstructions = instructions;
            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            this._udpSocket.Blocking = false; // neblokirajuci rezim
            this._udpSocket.Bind(localEP);    // bindujemo jer cemo i slusati i slati
        }

        public void RunNode()
        {

            // nakon prijema Instrukcija pokreni UDP socket
            this._udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this._udpSocket.Blocking = false;
            this._udpSocket.Bind(new IPEndPoint(IPAddress.Loopback, this.NodePort));

            Console.WriteLine($"[NODE {this.NodePort}] Starting polling loop...");

            while (true)
            {
                // awaiting for data (Message object)
                if (this._udpSocket.Poll(1_000_000, SelectMode.SelectRead))
                {
                    Message message = this.ReceiveFromPrevNode();
                    Console.WriteLine($"[NODE {this.NodePort}] recieved message");
                    
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

            IPEndPoint nextEP = this.NodeInstructions.NextNode ?? new IPEndPoint(IPAddress.Parse("127.0.0.1"), 60001);

            byte[] data = passNext.ToBytes();
            this._udpSocket.SendTo(data, 0, data.Length, SocketFlags.None, nextEP);
        }

        public Message ReceiveFromPrevNode()
        {
            // recieving the instance of Message
            EndPoint prevEP = this.NodeInstructions.PrevNode ?? new IPEndPoint(IPAddress.Any, 60000);

            byte[] buffer = new byte[4096]; // 4096 stand UDP size
            int bytesReceived = this._udpSocket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref prevEP);

            // filtering data
            byte[] actualData = new byte[bytesReceived];
            Array.Copy(buffer, actualData, bytesReceived);

            return Message.FromBytes(actualData);
        }

        public void ReceiveInstructionsForNode()
        {
            using (TcpClient client = this._tcpListener.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            {
                // recieve length first
                byte[] lengthBuffer = new byte[4];
                stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                //recieve the data
                byte[] buffer = new byte[dataLength];
                int totalreceived = 0;
                while (totalreceived < dataLength)
                {
                    int received = stream.Read(buffer, totalreceived, dataLength - totalreceived);
                    totalreceived += received;
                }

                NodeInstructions = Instructions.FromBytes(buffer);
            }
        }
        public Message ReceiveMessageForNode()
        {
            using (TcpClient client = this._tcpListener.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            {
                // recieve length first
                byte[] lengthBuffer = new byte[4];
                stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                //recieve the data
                byte[] buffer = new byte[dataLength];
                int totalreceived = 0;
                while (totalreceived < dataLength)
                {
                    int received = stream.Read(buffer, totalreceived, dataLength - totalreceived);
                    totalreceived += received;
                }

                return Message.FromBytes(buffer);
            }
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
