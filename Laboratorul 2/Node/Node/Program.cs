using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Common;
using Common.Entities;
using ServerSideCommons;

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

    public class Node
    {
        private readonly UdpClient _udpClient;
        private NodeConfig _nodeConfig;
        private readonly TcpListener _tcpListener;
        private readonly List<TcpConnectionHandler> _tcpConnectionHandlers;

        private List<Book> _books;
        private List<Movie> _movies;
        private List<Song> _songs;

        public Node(IPAddress groupAddress, int groupPort,string name)
        {
            
            //_books = new List<Book>
            //{
            //    new Book{Author = "Ion Creanga", Title = "Amintiri din cpoilarie", Year = 1850},
            //    new Book{Author = "Ion Creanga", Title = "Ursul pacalit de vulpe", Year = 1861},
            //    new Book{Author = "Mihai Eminescu", Title = "Luceafarul", Year = 1861}
            //};

            //_movies = new List<Movie>
            //{
            //    new Movie{Director = "Steven Spielberg", Title = "Jaws", Year = 1975, Grossing = 260000000},
            //    new Movie{Director = "Steven Spielberg", Title = "E.T.", Year = 1982, Grossing = 359197037},
            //    new Movie{Director = "Steven Spielberg", Title = "Jurrasic Park", Year = 1993, Grossing = 357067947},
            //    new Movie{Director = "James Cameron", Title = "Avatar", Year = 2009, Grossing = 3020000000},
            //    new Movie{Director = "James Cameron", Title = "Titanic", Year = 1997, Grossing = 2516000000}
            //};

            //_songs = new List<Song>
            //{
            //    new Song{Singer = "Bruno Mars", Title = "Uptown Funk", Year = 2014, Duration = "3:10"},
            //    new Song{Singer = "Magic Thompson", Title = "Jet Lag", Year = 2015, Duration = "3:52"},
            //    new Song{Singer = "Elènne", Title = "Ruthless Bloom", Year = 2016, Duration = "4:29"},
            //    new Song{Singer = "Moon Boots", Title = "Gopherit", Year = 2011, Duration = "3:16"}
            //};


            //var orderBy = _songs.OrderBy("Year");
            //var group = _songs.GroupBy("Duration", "Duration");
            //var select = _songs.Where("Year = @0", 2011);
            
            LoadConfiguration(name);

            LoadData(name);

            _tcpConnectionHandlers = new List<TcpConnectionHandler>();

            var tcpIpAddress = IPAddress.Parse(_nodeConfig.CurrentNode.IPAddress);
            var tcpPort = _nodeConfig.CurrentNode.Port;
            _tcpListener = new TcpListener(tcpIpAddress, tcpPort);
            SetUpTcpListener();
            Console.WriteLine($"Accepting TCP connections at {_tcpListener.LocalEndpoint}.");


            _udpClient = new UdpClient { ExclusiveAddressUse = false };

            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);


            var groupEndPoint = new IPEndPoint(groupAddress, groupPort);
            _udpClient.JoinMulticastGroup(groupEndPoint.Address);

            Console.WriteLine($"Joined multicast group {groupEndPoint}");

            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, groupPort));

            new Thread(GetDiscovered).Start();
        }

        private void LoadData(string name)
        {
            var path = $"Books{name.Last()}.xml";

            using (var reader = new StreamReader(path))
            {
                var serializer = new XmlSerializer(typeof(List<Book>));
                _books = serializer.Deserialize(reader) as List<Book>;
            }

            path = $"Songs{name.Last()}.xml";

            using (var reader = new StreamReader(path))
            {
                var serializer = new XmlSerializer(typeof(List<Song>));
                _songs = serializer.Deserialize(reader) as List<Song>;
            }
            path = $"Movies{name.Last()}.xml";

            using (var reader = new StreamReader(path))
            {
                var serializer = new XmlSerializer(typeof(List<Movie>));
                _movies = serializer.Deserialize(reader) as List<Movie>;
            }
        }

        private void LoadConfiguration(string name)
        {
            var path = $"config{name.Last()}.xml";

            using (var reader = new StreamReader(path))
            {
                var serializer = new XmlSerializer(typeof(NodeConfig));
                _nodeConfig = serializer.Deserialize(reader) as NodeConfig;
            }
        }

        private void GetDiscovered()
        {
            while (true)
            {
                var endPoint = new IPEndPoint(IPAddress.Any, 0);
                var data = _udpClient.Receive(ref endPoint);

                string message;
                using (var reader = new StreamReader(new MemoryStream(data), Encoding.ASCII))
                {
                    var line = reader.ReadLine();
                    message = Base64.Decode(line);
                    Console.WriteLine($"\r\nRecieved:\r\n{message}\r\n");
                }

                var udpMessage = message.DeserializeJson<UdpMessage>();
                if (udpMessage.RequestType == RequestType.Discover)
                {
                    data = Encoding.ASCII.GetBytes(Base64.Encode(GetConnections()));
                    _udpClient.Send(data, data.Length, endPoint);
                }
            }
        }

        private string GetConnections()
        {
            return _nodeConfig.SerializeJson();
            //var doc = new XDocument(
            //    new XElement("body",
            //        new XElement("CurrentNode",
            //            new XElement("IP", IPAddress.Loopback),
            //            new XElement("Port", "80001")),
            //        new XElement("Connections",
            //            new XElement("Node",
            //                new XElement("IP", IPAddress.Loopback),
            //                new XElement("Port", "80002"))))
            //            );
            //var writer = new StringWriter();
            //doc.Save(writer);
            //return writer.ToString();
        }

        private void SetUpTcpListener()
        {
            var listenerThread = new Thread(AcceptTcpConnections);
            listenerThread.Start();
        }

        private void AcceptTcpConnections()
        {
            _tcpListener.Start();

            while (true)
            {
                try
                {
                    var tcpClient = _tcpListener.AcceptTcpClient();
                    var tcpClientHandler = new TcpConnectionHandler(tcpClient)
                    {
                        RecievedMessageHandler = ProcessRequest
                    };
                    _tcpConnectionHandlers.Add(tcpClientHandler);
                    tcpClientHandler.StartService();
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine(e);
                    return;
                }
            }

            _tcpListener.Stop();
        }

        //private void ProcessRequest(object sender, MessageArgs e)
        //{
        //    Console.WriteLine($"Message:\r\n{e.Message}");

        //    var message = e.Message.DeserializeJson<Request>();

        //    switch (message.EntityType)
        //    {
        //        case nameof(Book):
        //            e.Response = _books.ApplyFilters(message);
        //            return;
        //        case nameof(Song):
        //            e.Response = _songs.ApplyFilters(message);
        //            return;
        //        case nameof(Movie):
        //            e.Response = _movies.ApplyFilters(message);
        //            return;
        //        default:
        //            throw new ArgumentException(nameof(message.EntityType));
        //    }
        //}

        private void ProcessRequest(object sender, MessageArgs e)
        {
            Console.WriteLine(e.Message);

            var request = e.Message.DeserializeJson<Request>();

            var tasks = new List<Task<string>>(_nodeConfig.Connections.Count);

            var newRequest = (Request) request.Clone();
            newRequest.ResultAsJson = true;
            newRequest.OrderBy = null;

            if (--newRequest.TimeToLive > 0)
            {
                var endPoints = new List<IPEndPoint>(_nodeConfig.Connections.Count);
                endPoints.AddRange(_nodeConfig.Connections.Select(endPoint =>
                    new IPEndPoint(IPAddress.Parse(endPoint.IPAddress), endPoint.Port)));

                tasks.AddRange(endPoints.Select(endPoint =>
                    Task.Run(() => SendRequest(newRequest.SerializeJson(), endPoint))));

                tasks.WaitAll();
            }

            switch (request.EntityType)
            {
                case nameof(Book):

                    var books = new List<Book>();

                    foreach (var task in tasks)
                    {
                        var items = task.Result.DeserializeJson<List<Book>>();
                        books.AddRange(items);
                    }

                    books.AddRange(_books);

                    books.ApplyFilters(request);

                    e.Response = books.Serialize(request.ResultAsJson);
                    return;
                case nameof(Song):
                    var songs = new List<Song>();

                    foreach (var task in tasks)
                    {
                        var items = task.Result.DeserializeJson<List<Song>>();
                        songs.AddRange(items);
                    }

                    songs.AddRange(_songs);

                    songs.ApplyFilters(request);

                    e.Response = songs.Serialize(request.ResultAsJson);
                    return;
                case nameof(Movie):
                    var movies = new List<Movie>();

                    foreach (var task in tasks)
                    {
                        var items = task.Result.DeserializeJson<List<Movie>>();
                        movies.AddRange(items);
                    }

                    movies.AddRange(_movies);

                    movies.ApplyFilters(request);

                    e.Response = movies.Serialize(request.ResultAsJson);
                    return;
                default:
                    throw new ArgumentException(nameof(request.EntityType));
            }
        }

        private string SendRequest(string request, IPEndPoint endPoint)
        {

            string response;
            using (var tcpClient = new TcpClient())
            {
                tcpClient.Connect(endPoint);
                Console.WriteLine($"\r\nConnected to {endPoint}");

                try
                {
                    using (var streamReader = new StreamReader(tcpClient.GetStream()))
                    using (var streamWriter = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true })
                    {
                        streamWriter.WriteLine(Base64.Encode(request));
                        Console.WriteLine($"\r\nSent:\r\n{request}");

                        string line = null;
                        while (line == null)
                        {
                            line = streamReader.ReadLine();
                        }
                        response = Base64.Decode(line);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            return response;
        }

    }
}