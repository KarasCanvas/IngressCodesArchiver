using System;
using System.IO;
using System.Xml.Serialization;

namespace IngressCodesArchiver
{
    public static class XmlFile
    {
        private static string CorrectPath(string path, bool createDirectory)
        {
            if (!Path.IsPathRooted(path))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                path = Path.Combine(baseDir, path);
            }
            if (!path.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                path = String.Concat(path, ".xml");
            }
            if (createDirectory)
            {
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            return path;
        }

        public static void Serialize<T>(T instance, string path)
        {
            var xs = new XmlSerializer(typeof(T));
            using (var fs = new FileStream(CorrectPath(path, true), FileMode.Create, FileAccess.ReadWrite))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add(String.Empty, String.Empty);
                xs.Serialize(fs, instance, ns);
            }
        }

        public static T Deserialize<T>(string path)
        {
            var xs = new XmlSerializer(typeof(T));
            using (var fs = new FileStream(CorrectPath(path, false), FileMode.Open, FileAccess.Read))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add(String.Empty, String.Empty);
                return (T)xs.Deserialize(fs);
            }
        }
    }
}
