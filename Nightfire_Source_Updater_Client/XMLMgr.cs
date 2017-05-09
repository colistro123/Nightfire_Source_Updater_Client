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

        /* Everything below this line needs to be massively refactored, this looks horrible. */
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

        public void GetIDAndVersionCachesXML(string cacheName, out string outID, out string outVersion)
        {
            Dictionary<String, String> cacheList = ReadFromCacheFile(cacheName); //Read our caches file, provided it is there
            var firstElement = cacheList.FirstOrDefault();
            outID = firstElement.Key;
            outVersion = firstElement.Value;
            return;
        }

        public static bool IsXMLValid(string file)
        {
            bool valid = false;
            try
            {
                XDocument xd1 = new XDocument();
                xd1 = XDocument.Load(file);
                valid = true;
            }
            catch (XmlException exception){}
            return valid;
        }
    }
}