using System;
using System.Net;

namespace Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var groupEndPoint = new IPEndPoint(IPAddress.Parse("224.168.100.2"), 11000);
            var tcpEndPoint = new IPEndPoint(IPAddress.Loopback, 8888);
            var intermediate = new Intermediate(groupEndPoint, tcpEndPoint);
            Console.ReadKey();
        }
    }
}