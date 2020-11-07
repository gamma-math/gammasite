using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace GamMaSite.Services
{
    public class ConfigurationService
    {
        private static ConfigurationService _configurationService = new ConfigurationService();

        private ConcurrentDictionary<string, byte[]> configDictionary = new ConcurrentDictionary<string, byte[]>();

        public static ConfigurationService GetInstance()
        {
            return _configurationService;
        }

        public string Get(string key)
        {
            return Encoding.UTF8.GetString(GetBytes(key));
        }

        public Stream GetStream(string key)
        {
            return new MemoryStream(GetBytes(key));
        }

        public XslCompiledTransform GetTransform(string key)
        {
            XElement element = XElement.Parse(Get(key));
            XslCompiledTransform xsltTransformation = new XslCompiledTransform(false);
            xsltTransformation.Load(element.CreateReader(), new XsltSettings(false, true), new ConfigXmlResolver());
            return xsltTransformation;
        }

        public byte[] GetBytes(string key)
        {
            return configDictionary[key];
        }

        public void Redefine(byte[] zipAsBytes)
        {
            lock(configDictionary)
            {
                configDictionary.Clear();
                var zip = new ZipService().GetZipAsMap(zipAsBytes);
                foreach (string key in zip.Keys)
                {
                    configDictionary[key] = zip[key];
                }
            }
        }
    }

    public class ConfigXmlResolver : XmlUrlResolver
    {
        private ConfigurationService _configurationService = ConfigurationService.GetInstance();

        public ConfigXmlResolver() : base()
        {
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            return _configurationService.GetStream(absoluteUri.ToString());
        }
    }
}
