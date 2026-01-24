using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat;
using MrezeProjekat.Models;

namespace Client
{
    internal class Client
    {
        static void Main(string[] args)
        {
           




            Console.ReadKey();
        }


        public void SendRequest()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);

            clientSocket.Connect(serverEP);

            IPEndPoint localEP = clientSocket.LocalEndPoint as IPEndPoint;
            Request req = new Request(localEP, 12, 10);

            byte[] data = req.ToBytes();

            // prvo pošalji dužinu
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            clientSocket.Send(lengthBuffer);

            // zatim pošalji podatke
            clientSocket.Send(data);

            clientSocket.Close();
        }
    }
}
