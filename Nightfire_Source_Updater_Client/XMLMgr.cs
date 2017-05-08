using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace Nightfire_Source_Updater_Client
{
    class XMLMgr
    {
        public static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "\t",
            //NewLineOnAttributes = true
        };

        public Dictionary<String, String> ReadFromCacheFile(string file)
        {
            StringBuilder result = new StringBuilder();
            var id = String.Empty;
            var version = String.Empty;
            Dictionary<String, String> cacheList = new Dictionary<string, string>();
            foreach (XElement level1Element in XElement.Load(@file).Elements("Cache"))
            {
                id = level1Element.Attribute("ID").Value;
                version = level1Element.Attribute("Version").Value;
                cacheList.Add(id, version);
            }
            return cacheList;
        }
    }
}