using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace IngressCodesArchiver
{
    internal static class XmlUtility
    {
        public static string Serialize<T>(T instance)
        {
            var xs = new XmlSerializer(typeof(T));
            using (var s = new MemoryStream())
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add(String.Empty, String.Empty);
                xs.Serialize(s, instance, ns);
                s.Seek(0, SeekOrigin.Begin);
                using (var sr = new StreamReader(s))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static T Deserialize<T>(string xml)
        {
            var xs = new XmlSerializer(typeof(T));
            using (var s = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add(String.Empty, String.Empty);
                return (T)xs.Deserialize(s);
            }
        }
    }
}
