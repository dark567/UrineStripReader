using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppClient
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("172.16.0.77"), 8234);
            string text = System.IO.File.ReadAllText(@"proba.txt");

            try
            {
                client.Connect(serverEndPoint);
                NetworkStream clientStream = client.GetStream();

                ASCIIEncoding encoder = new ASCIIEncoding();
                //byte[] buffer = encoder.GetBytes("Hello Server!");
                byte[] buffer = encoder.GetBytes(text);

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
            catch (SocketException)
            {
                Console.WriteLine("Ip not available");
            }

            Thread.Sleep(1000);
        }
    }
}
