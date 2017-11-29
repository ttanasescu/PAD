using System.Xml.Serialization;

namespace Proxy
{
    public class NodeEndPoint
    {
        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string IPAddress { get; set; }

        [XmlElement]
        public int Port { get; set; }

        public NodeEndPoint()
        {
        }

        public NodeEndPoint(string ipAddress, int port, string name = "Node1")
        {
            IPAddress = ipAddress;
            Port = port;
            Name = name;
        }
    }
}