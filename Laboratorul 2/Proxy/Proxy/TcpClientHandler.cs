using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Common;

namespace Proxy
{
    internal class TcpClientHandler
    {
        private readonly TcpClient _tcpClient;
        private bool _isServing;
        public EventHandler<MessageArgs> RecievedMessageHandler;

        public TcpClientHandler(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public void StartService()
        {
            var worker = new Thread(Serve);
            worker.Start();
        }

        public void StopService()
        {
            _isServing = false;
        }

        private void Serve()
        {
            _isServing = true;
            Console.WriteLine($"Serving {_tcpClient.Client.RemoteEndPoint}" +
                              $" on {_tcpClient.Client.LocalEndPoint}");
            var stream = _tcpClient.GetStream();
            //try
            {
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    while (_isServing)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                        {
                            Console.WriteLine("Client disconnected.");
                            return;
                        }
                        var message = Base64.Decode(line);

                        var args = new MessageArgs(message);
                        RecievedMessageHandler(null, args);
                        var response = Base64.Encode(args.Response);

                        writer.WriteLine(response);
                    }
                }
            }
            //catch (InvalidOperationException e)
            {
                //Debug.WriteLine(e.Message);
                Console.WriteLine("Client disconnected.");
                // ClientDisconectedHandler(this, EventArgs.Empty);
            }
        }
    }
}