using System;
using System.Net;

namespace Node
{
    class Program
    {
        static void Main(string[] args)
        {
            var name = args.Length > 0 ? args[0] : "Node1";
            var unused = new Node(IPAddress.Parse("224.168.100.2"), 11000, name);
            Console.ReadKey();
        }
    }
}