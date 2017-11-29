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