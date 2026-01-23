using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MrezeProjekat.Models
{
    internal class Request
    {
        private IPEndPoint _sender;
        private int _nodeNum;
        private int _maxMessages; // Maksimalni broj poruka, nakon kog se lanac prekida 

        public Request() { }
        public Request(IPEndPoint senderPoint, int nodeNumber, int maxMessagesNum)
        {
            this._sender = senderPoint;
            this._nodeNum = nodeNumber;
            this._maxMessages = maxMessagesNum;
        }

        public IPEndPoint Sender => this._sender;
        public int NodeNum => this._nodeNum;
        public int MaxMessages => this._maxMessages;

    }
}
