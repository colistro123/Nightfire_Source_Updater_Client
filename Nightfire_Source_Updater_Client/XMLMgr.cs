using System;
using System.IO;
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
            if (File.Exists(file) || Utils.RemoteFileExists(file))
            {
                try
                {
                    foreach (XElement level1Element in XElement.Load(@file).Elements("Cache"))
                    {
                        id = level1Element.Attribute("ID").Value;
                        version = level1Element.Attribute("Version").Value;
                        cacheList.Add(id, version);
                    }
                } catch (Exception ex)
                {
                    Console.WriteLine($"Caches file is invalid or non-existing: {ex.ToString()}");
                    cacheList.Add("-1", "invalid");
                }
            }
            return cacheList;
        }

        public static string GetCachesLocalFullPath(string cacheName)
        {
            return Path.Combine(Bootstrapper.MainDownloadDir, cacheName);
        }

        public void GetIDAndVersionCachesXML(string cachePath, out string outID, out string outVersion)
        {
            Dictionary<String, String> cacheList = ReadFromCacheFile(cachePath); //Read our caches file, provided it is there
            var firstElement = cacheList.FirstOrDefault();
            outID = firstElement.Key;
            outVersion = firstElement.Value;
            return;
        }

        public bool IsXMLFormatCorrect(string file)
        {
            GetIDAndVersionCachesXML(file, out string outID, out string outVersion); //Get the one on the client
            return (outVersion != "invalid" && outID != "-1");
        }

        public enum XMLCorrectStates
        {
            XML_STATE_FORMAT_VALID = 1,
            XML_STATE_FORMAT_INVALID = 2,
            XML_STATE_FULLY_INVALID = 3,
            XML_STATE_DOESNT_EXIST = 4
        };

        public XMLCorrectStates GetXMLFormatCorrectState(string file)
        {

            if (!File.Exists(file))
                return XMLCorrectStates.XML_STATE_DOESNT_EXIST;

            if (IsXMLValid(file))
            {
                if (IsXMLFormatCorrect(file))
                    return XMLCorrectStates.XML_STATE_FORMAT_VALID;
                else
                    return XMLCorrectStates.XML_STATE_FORMAT_INVALID;
            }
            else
            {
                return XMLCorrectStates.XML_STATE_FULLY_INVALID;
            }
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