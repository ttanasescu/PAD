using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Common
{
    public static class Extension
    {
        public static void WaitAll<T>(this List<Task<T>> tasks)
        {
            foreach (var item in tasks)
            {
                item.Wait();
            }
        }

        public static string Serialize<T>(this T value, bool asJson = false)
        {
            if (value == null)
                return string.Empty;

            return asJson ? SerializeJson(value) : SerializeXml(value);
        }

        public static string SerializeXml<T>(this T value, bool indent = false)
        {
            if (value == null)
                return string.Empty;

            var xmlserializer = new XmlSerializer(typeof(T));
            using (var stringWriter = new StringWriter())
            {
                var settings = new XmlWriterSettings { Indent = indent };
                using (var writer = XmlWriter.Create(stringWriter, settings))
                {
                    xmlserializer.Serialize(writer, value);
                    return stringWriter.ToString();
                }
            }
        }
        public static T Deserialize<T>(this string value) where T:class 
        {
            if (string.IsNullOrEmpty(value))
                return default(T);

            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(value))
            {
                return serializer.Deserialize(reader) as T;
            }
        }

        public static string SerializeJson<T>(this T value)
        {
            if (value == null)
                return string.Empty;

            var serializer = new JavaScriptSerializer();
            //serializer.RegisterConverters(new[] { new UdpMesasageConverter() });

            return serializer.Serialize(value);
        }

        public static T DeserializeJson<T>(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return default(T);

            var serializer = new JavaScriptSerializer();
            //serializer.RegisterConverters(new[] { new UdpMesasageConverter() });

            return serializer.Deserialize<T>(value);
        }
    }
}