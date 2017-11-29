using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace ServerSideCommons
{
    class UdpMesasageConverter : JavaScriptConverter
    {
        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            var p = new UdpMessage();
            foreach (string key in dictionary.Keys)
            {
                switch (key)
                {
                    case "Request":
                        p.RequestType = (RequestType)dictionary[key];
                        break;
                }
            }
            return p;
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var p = (UdpMessage)obj;
            IDictionary<string, object> serialized = new Dictionary<string, object>
            {
                ["Request"] = p.RequestType.ToString()
            };
            return serialized;
        }

        public override IEnumerable<Type> SupportedTypes => new List<Type> { typeof(UdpMessage) };
    }
}