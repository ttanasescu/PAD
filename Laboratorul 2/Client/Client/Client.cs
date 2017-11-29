using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Common;
using Common.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Client
{
    public class Client
    {
        private TcpClient _tcpClient;
        private readonly IPAddress _ipAddress;
        private readonly int _port;

        public Client(IPAddress remoteIpAddress, int port)
        {
            _ipAddress = remoteIpAddress;
            _port = port;
        }

        private void NewMethod()
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(new IPEndPoint(_ipAddress, _port));
        }

        public List<T> Get<T>(FilterBy filterBy, OrderBy orderBy) where T : Entity
        {
            var message = new Request
            {
                EntityType = typeof(T).Name,
                FilterBy = filterBy,
                OrderBy = orderBy,
                TimeToLive = 2
            };

            var reqest = message.Serialize();
            Console.WriteLine($"\r\nSent:\r\n{message.SerializeXml(true)}");
            var response = SendRequest(reqest);

            Console.WriteLine($"\r\nCollection is {(IsValidXml<T>(response) ? "" : "not ")}valid.");

            return response.Deserialize<List<T>>();
        }

        private static bool IsValidXml<T>(string xmlString) where T : Entity
        {
            var xdoc = XDocument.Parse(xmlString);
            var schemas = new XmlSchemaSet();

            schemas.Add("", XmlReader.Create(File.OpenRead($"{typeof(T).Name}s.xsd")));

            try
            {
                xdoc.Validate(schemas, null);
            }
            catch (XmlSchemaValidationException)
            {
                return false;
            }

            return true;
        }


        public List<T> Get<T>() where T : Entity
        {
            return Get<T>(null, null);
        }

        public string GetXml<T>() where T : Entity
        {
            return GetXml<T>(null, null);
        }

        public string GetJson<T>(FilterBy filterBy) where T : Entity
        {
            return GetJson<T>(filterBy, null);
        }

        public string GetJson<T>(OrderBy orderBy) where T : Entity
        {
            return GetJson<T>(null, orderBy);
        }

        public string GetJson<T>() where T : Entity
        {
            return GetJson<T>(null, null);
        }

        public string GetXml<T>(FilterBy filterBy, OrderBy orderBy) where T : Entity
        {
            var message = new Request
            {
                EntityType = typeof(T).Name,
                FilterBy = filterBy,
                OrderBy = orderBy,
                TimeToLive = 2
            };

            var reqest = message.Serialize();
            Console.WriteLine($"\r\nSent:\r\n{message.Serialize(true)}");
            var response = SendRequest(reqest);

            return response;
        }

        public string GetJson<T>(FilterBy filterBy, OrderBy orderBy) where T : Entity
        {
            var message = new Request
            {
                EntityType = typeof(T).Name,
                FilterBy = filterBy,
                OrderBy = orderBy,
                ResultAsJson = true,
                TimeToLive = 2
            };

            var reqest = message.Serialize();
            Console.WriteLine($"\r\nSent:\r\n{message.Serialize(true)}");
            var response = SendRequest(reqest);

            using (var file = File.OpenText($"{typeof(T).Name}sSchema.json"))
            using (var reader = new JsonTextReader(file))
            {
                var schema = JSchema.Load(reader);

                var collection = JArray.Parse(response);

                Console.WriteLine($"Collection is {(collection.IsValid(schema) ? "" : "not ")}valid.");
            }

            return response;
        }

        private string SendRequest(string reqest)
        {
            NewMethod();
            var stream = _tcpClient.GetStream();
            try
            {
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.WriteLine(Base64.Encode(reqest));

                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        throw new InvalidOperationException(nameof(line));
                    }
                    var message = Base64.Decode(line);

                    return message;
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Client disconected.");
                return null;
                //throw;
            }
        }
    }
}