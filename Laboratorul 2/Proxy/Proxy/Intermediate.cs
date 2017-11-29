using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Common;
using Common.Entities;
using QuickGraph;
using QuickGraph.Algorithms;
using ServerSideCommons;

namespace Proxy
{
    public class Intermediate
    {
        private readonly UdpClient _groupUdpClient;
        private readonly IPEndPoint _groupEndPoint;

        private readonly ConcurrentBag<NodeConfig> _configs;
        private readonly List<NodeConfig> _mavens;
        private readonly List<IPEndPoint> _mavenEndPoints;

        private readonly TcpListener _tcpListener;
        private readonly List<TcpClientHandler> _tcpConnectionHandlers;
        private bool _isActive;

        public Intermediate(IPEndPoint groupEndPoint, IPEndPoint tcpEndPoint)
        {
            _tcpListener = new TcpListener(tcpEndPoint);
            _tcpConnectionHandlers = new List<TcpClientHandler>();

            _groupUdpClient = new UdpClient(2000, AddressFamily.InterNetwork);
            _configs = new ConcurrentBag<NodeConfig>();

            _groupEndPoint = groupEndPoint;
            _groupUdpClient.JoinMulticastGroup(_groupEndPoint.Address);


            DiscoverNodes();

            if (!_configs.IsEmpty)
            {
                _mavens = DetermineMavens();
                Console.WriteLine("Mavens:");
                foreach (var maven in _mavens)
                {
                    Console.WriteLine(maven.CurrentNode.Name);
                }
                _mavenEndPoints = GetMaveEndPoints();
                SetUpTcpListener();
            }
            else
            {
                Console.WriteLine("No nodes found.");
            }
        }


        #region Discovery

        private void DiscoverNodes()
        {
            var data = Encoding.ASCII.GetBytes($"{Base64.Encode(Discover())}\r\n");

            _groupUdpClient.Send(data, data.Length, _groupEndPoint);

            var ts = new CancellationTokenSource();
            Task.Factory.StartNew(WaitForNodes, ts.Token);

            Thread.Sleep(1000);
            _groupUdpClient.Close();
            ts.Cancel();
            Console.WriteLine("Waiting timed out.");
        }

        private void WaitForNodes()
        {
            Console.WriteLine("Waiting...");
            try
            {
                while (true)
                {
                    var endPoint = new IPEndPoint(IPAddress.Any, 50);
                    var bytes = _groupUdpClient.Receive(ref endPoint);
                    if (bytes == null || bytes.Length == 0)
                        continue;
                    using (var reader = new StreamReader(new MemoryStream(bytes), Encoding.ASCII))
                    {
                        var line = reader.ReadLine();
                        var message = Base64.Decode(line);
                        Task.Run(() => ProcessMessage(message));
                    }
                }
            }
            catch (SocketException e) when (e.ErrorCode == 10004)
            {
                Console.WriteLine("Finished waiting.");
            }
        }

        private void ProcessMessage(string message)
        {
            Console.WriteLine($"Recieved:\r\n{message}\r\n");
            var serializer = new JavaScriptSerializer();
            var nodeConfig = serializer.Deserialize<NodeConfig>(message);
            _configs.Add(nodeConfig);
            //}
        }

        private string Discover()
        {
            return new UdpMessage(RequestType.Discover).SerializeJson();
            //var doc = new XDocument(new XElement("request", "discover"));
            //var writer = new StringWriter();
            //doc.Save(writer);
            //return writer.ToString();
        }


        private List<NodeConfig> DetermineMavens()
        {
            var mavens = new List<NodeConfig>();

            var graph = new UndirectedGraph<string, Edge<string>>();
            foreach (var config in _configs)
            {
                graph.AddVertex(config.CurrentNode.Name);
                foreach (var connection in config.Connections)
                {
                    graph.AddVertex(connection.Name);
                    graph.AddEdge(new Edge<string>(config.CurrentNode.Name, connection.Name));
                }
            }

            var components = new Dictionary<string, int>();
            graph.ConnectedComponents(components);

            foreach (var i in components.Values.Distinct())
            {
                var names = components.Where(p => p.Value == i).Select(p => p.Key);

                var node = _configs.Where(config => names.Contains(config.CurrentNode.Name))
                    .OrderByDescending(x => x.Connections.Count).First();

                mavens.Add(node);
            }

            return mavens;
        }

        private List<IPEndPoint> GetMaveEndPoints()
        {
            return _mavens.Select(maven =>
                new IPEndPoint(IPAddress.Parse(maven.CurrentNode.IPAddress), maven.CurrentNode.Port)).ToList();
        }

        #endregion


        private void SetUpTcpListener()
        {
            _isActive = true;
            var listenerThread = new Thread(AcceptTcpConnections);
            listenerThread.Start();
        }

        private void AcceptTcpConnections()
        {
            _tcpListener.Start();

            while (_isActive)
            {
                try
                {
                    var tcpClient = _tcpListener.AcceptTcpClient();
                    var tcpClientHandler = new TcpClientHandler(tcpClient)
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

            var request = e.Message.Deserialize<Request>();
            var newRequest = (Request) request.Clone();
            newRequest.ResultAsJson = true;
            newRequest.OrderBy = null;

            var tasks = new List<Task<string>>(_mavenEndPoints.Count);
            foreach (var endPoint in _mavenEndPoints)
            {
                var task = Task.Run(() => SendRequest(newRequest.SerializeJson(), endPoint));
                tasks.Add(task);
            }

            tasks.WaitAll();

            var results = tasks.Select(task => task.Result).ToList();

            switch (request.EntityType)
            {
                case nameof(Book):
                    e.Response = GetResponse<Book>(request, results);
                    return;
                case nameof(Song):
                    e.Response = GetResponse<Song>(request, results);
                    return;
                case nameof(Movie):
                    e.Response = GetResponse<Movie>(request, results);
                    return;
                default:
                    throw new ArgumentException(nameof(request.EntityType));
            }
        }

        private static string GetResponse<T>(Request request, List<string> tasks)
        {
            var items = new List<T>();

            foreach (var task in tasks)
            {
                items.AddRange(task.DeserializeJson<List<T>>());
            }

            items = items.ApplyFilters(request);

            return items.Serialize(request.ResultAsJson);
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
                    using (var streamWriter = new StreamWriter(tcpClient.GetStream()) {AutoFlush = true})
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