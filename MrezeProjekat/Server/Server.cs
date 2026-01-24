using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat;
using MrezeProjekat.Models;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 50001);

            serverSocket.Bind(serverEP);
            serverSocket.Listen(10);

            Socket acceptedSocket = serverSocket.Accept();

            IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
            
            byte[] buffer = new byte[1024];
            int recieved = acceptedSocket.Receive(buffer);

            Request req = Request.FromBytes(buffer.Take(recieved).ToArray());

            acceptedSocket.Close();
            serverSocket.Close();

        }
    }
}
