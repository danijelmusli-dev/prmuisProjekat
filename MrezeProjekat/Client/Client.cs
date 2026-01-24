using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using MrezeProjekat;
using MrezeProjekat.Models;

namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            // napravi klijentski socket jednom
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);

            clientSocket.Connect(serverEP);

            // koristi funkcije sa prosleđenim socketom
            SendRequest(clientSocket);

            Instructions ins = RecieveInstructions(clientSocket);

            // zatvori sve na kraju
            clientSocket.Close();

            Console.ReadKey();
        }

        public static void SendRequest(Socket clientSocket)
        {
            IPEndPoint localEP = clientSocket.LocalEndPoint as IPEndPoint;
            Request req = new Request(localEP, 12, 10);

            byte[] data = req.ToBytes();

            // prvo pošalji dužinu
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            clientSocket.Send(lengthBuffer);

            // zatim pošalji podatke
            clientSocket.Send(data);
        }

        public static Instructions RecieveInstructions(Socket clientSocket)
        {
            // prvo primi dužinu
            byte[] lengthBuffer = new byte[4];
            int readLen = clientSocket.Receive(lengthBuffer);
            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

            // zatim primi podatke
            byte[] buffer = new byte[dataLength];
            int totalReceived = 0;
            while (totalReceived < dataLength)
            {
                int received = clientSocket.Receive(buffer, totalReceived, dataLength - totalReceived, SocketFlags.None);
                if (received == 0) break; // konekcija zatvorena
                totalReceived += received;
            }

            // rekonstruiši Instructions objekat
            Instructions ins = Instructions.FromBytes(buffer);

            return ins;
        }

    }
}
