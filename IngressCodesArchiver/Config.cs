using System;
using System.IO;

namespace IngressCodesArchiver
{
    [Serializable]
    public class Config
    {
        public static readonly Config Instance = Load();

        public string CurrentUrl { get; set; }

        public bool ConvertToPdf { get; set; }

        public Config()
        {
            this.ConvertToPdf = true;
        }

        private static string GetFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IngressCodesArchiver.xml");
        }

        public void Save()
        {
            XmlFile.Serialize(this, GetFilePath());
        }

        public static Config Load()
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                try
                {
                    return XmlFile.Deserialize<Config>(path);
                }
                catch (Exception)
                {
                    return new Config();
                }
            }
            else
            {
                return new Config();
            }
        }
    }
}
