using System.Collections.Generic;
using System.Xml.Serialization;

namespace Proxy
{
    public class NodeConfig
    {
        [XmlElement]
        public NodeEndPoint CurrentNode { get; set; }

        [XmlArray]
        [XmlArrayItem(typeof(NodeEndPoint))]
        public List<NodeEndPoint> Connections { get; set; }

        public NodeConfig()
        {
        }

        public NodeConfig(NodeEndPoint currentNode, List<NodeEndPoint> connections)
        {
            CurrentNode = currentNode;
            Connections = connections;
        }
    }
}