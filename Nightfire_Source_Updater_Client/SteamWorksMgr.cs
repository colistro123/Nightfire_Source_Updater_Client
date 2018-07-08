using System;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace Nightfire_Source_Updater_Client
{
    class SteamWorksMgr
    {
        const int SOURCE_SDK_2013_BASE = 243750;

        public string getSteamInstallPath()
        {
            RegistryKey regKey = Registry.CurrentUser;
            regKey = regKey.OpenSubKey(@"Software\Valve\Steam");

            if (regKey != null)
                return regKey.GetValue("SteamPath").ToString();

            return "";
        }

        public string searchForSteamDir()
        {
            FolderBrowserDialog browser = new FolderBrowserDialog();
            string tempPath = "";

            if (browser.ShowDialog() == DialogResult.OK)
            {
                tempPath = browser.SelectedPath; // prints path
            }

            return tempPath;
        }

        public bool IsSDK2013Installed()
        {
            bool installed = false;
            RegistryKey regKey = Registry.CurrentUser;
            regKey = regKey.OpenSubKey($@"Software\Valve\Steam\Apps\{SOURCE_SDK_2013_BASE}");

            if (regKey != null)
                installed = regKey.GetValue("Installed").ToString() != "0";

            return installed;
        }

        public bool tryInstallSDK2013()
        {
            Process Proc = new Process();

            Proc.StartInfo.FileName = Path.GetFullPath(getSteamInstallPath() + "\\steam.exe");
            Proc.StartInfo.Arguments = String.Format("-applaunch {0}", SOURCE_SDK_2013_BASE);
            Proc.StartInfo.UseShellExecute = false;
            Proc.StartInfo.CreateNoWindow = false;
            Proc.Start();

            return true;
        }
    }
}
