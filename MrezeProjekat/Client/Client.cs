using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat;
using MrezeProjekat.Helpers;
using MrezeProjekat.Models;

namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            // Create the client socket
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);

            clientSocket.Connect(serverEP);

            // form the request (coming soon)
            // send request to the server
            // umesto new Request napraviceo f-ju gde korisnik formira zahtev
            Request req = new Request(null, 3, 5);
            SendRequest(clientSocket, req);

            // recieve instructions 
            Instructions ins = RecieveInstructions(clientSocket);
            Console.Write(ins.ToString());

            // crypt the message N times
            Message message = CryptoHelper.CryptNTimes("gas", 3 , ins);

            // closing of the socket
            clientSocket.Close();

            Console.ReadKey();
        }

        public static void SendRequest(Socket clientSocket, Request request)
        {
            IPEndPoint localEP = clientSocket.LocalEndPoint as IPEndPoint;
            request.Sender = localEP;

            byte[] data = request.ToBytes();

            // sending length of data
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            clientSocket.Send(lengthBuffer);

            // sending data
            clientSocket.Send(data);
        }
        public static Instructions RecieveInstructions(Socket clientSocket)
        {
            // recieve the data length
            byte[] lengthBuffer = new byte[4];
            int readLen = clientSocket.Receive(lengthBuffer);
            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

            // recieve data
            byte[] buffer = new byte[dataLength];
            int totalReceived = 0;
            while (totalReceived < dataLength)
            {
                int received = clientSocket.Receive(buffer, totalReceived, dataLength - totalReceived, SocketFlags.None);
                if (received == 0) break; // CLOSE THE CONNECTION
                totalReceived += received;
            }

            // Deserialize the Instructons object
            Instructions ins = Instructions.FromBytes(buffer);

            return ins;
        }

    }
}
