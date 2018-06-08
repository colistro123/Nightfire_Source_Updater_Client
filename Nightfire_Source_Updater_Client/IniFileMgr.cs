using System;
using System.IO;
using SharpConfig;

namespace Nightfire_Source_Updater_Client
{
    /* Not sure about this class name... */
    class IniFileMgr
    {
        static public string prod_channel { get; set; }
        static public Configuration config;

        /* Create (if null) or get the config instance */
        static public Configuration getIniFileMgrConfigPtr()
        {
            if (config == null)
                config = new Configuration();
            return config;
        }
        /* Gets data from a section that has already been loaded, otherwise returns null */
        static public Section getSection(string section)
        {
            getIniFileMgrConfigPtr();
            return config[section];
        }
        /* Inits a cfg file for the first time */
        static public void InitFile(string fileName)
        {
            getIniFileMgrConfigPtr();
            config["General"]["channel"].StringValue = Bootstrapper.DefaultChannel; //By default
            config.SaveToFile(fileName);
        }
        /* Reads a section from a file, assigns it to config and returns it */
        static public Section ReadFromFile(string FileName, string section)
        {
            config = Configuration.LoadFromFile(FileName);
            return config[section];
        }
        static public void TryOpenFile(string fileName)
        {
            if (!File.Exists(fileName))
                InitFile(fileName);

            //If for whatever reason the file is empty since it exists and they edited it...
            if (ReadFromFile(fileName, "General")["channel"].StringValue == String.Empty)
                InitFile(fileName);

            prod_channel = getSection("General")["channel"].StringValue;
        }
    }
}
