using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.ComponentModel;

namespace MrezeProjekat.Models
{
    public class Request
    {
        private IPEndPoint _sender; // IP endpoint of the client sending the request
        private int _nodeNum;       // Number of nodes in the path (N)
        private int _maxMessages;   // Number of messages before the connection is closed 

        public Request() { }
        public Request(IPEndPoint senderPoint, int nodeNumber, int maxMessagesNum)
        {
            this._sender = senderPoint;
            this._nodeNum = nodeNumber;
            this._maxMessages = maxMessagesNum;
        }

        public IPEndPoint Sender
        { 
            get => _sender;
            set => _sender = value;
        }
        public int NodeNum => this._nodeNum;
        public int MaxMessages => this._maxMessages;

        public byte[] ToBytes()
        {
            byte[] addrBytes = this._sender.Address.GetAddressBytes();
            byte[] portBytes = BitConverter.GetBytes(this._sender.Port);

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                #region Field serialization
                // lenght IP & IP
                bw.Write(addrBytes.Length);
                bw.Write(addrBytes);
                
                // Port
                bw.Write(portBytes);

                // nodeNum & maxMessages
                bw.Write(this._nodeNum);
                bw.Write(this._maxMessages);
                
                return ms.ToArray();
                #endregion
            }
        }

        public static Request FromBytes(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                #region Field deserialisation
                // lenght IP & IP
                int addrLen = br.ReadInt32();
                byte[] addrBytes = br.ReadBytes(addrLen);

                // Port
                int port = br.ReadInt32();

                IPAddress ip = new IPAddress(addrBytes);
                IPEndPoint endPoint = new IPEndPoint(ip, port);

                // nodeNum & maxMessages
                int nodeNum = br.ReadInt32();
                int maxMessages = br.ReadInt32();

                return new Request(endPoint, nodeNum, maxMessages);
                #endregion
            }
        }

    }
}
