using System;
using System.Windows.Forms;
using Steamworks;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace Nightfire_Source_Updater_Client
{
    class SteamWorksMgr
    {
        const int SOURCE_SDK_2013_BASE = 243750;
        public void InitAppList()
        {
            m_AppList = new AppId_t[SteamAppList.GetNumInstalledApps()];

            m_SteamAppInstalled = Callback<SteamAppInstalled_t>.Create(OnSteamAppInstalled);
            m_SteamAppUninstalled = Callback<SteamAppUninstalled_t>.Create(OnSteamAppUninstalled);
        }
        public bool TryInit()
        {
            bool retVal = false;
            try
            {
                if (!SteamAPI.Init())
                {
                    Console.WriteLine("SteamAPI.Init() failed!");
                }
                else
                {
                    retVal = true;
                }
            }
            catch (DllNotFoundException e)
            { // We check this here as it will be the first instance of it.
                Console.WriteLine(e);
            }
            if (retVal)
                InitAppList();

            return retVal;
        }

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

        public bool tryInstallSDK2013()
        {
            Process Proc = new Process();
            AppId_t appId = (AppId_t)SOURCE_SDK_2013_BASE;

            Proc.StartInfo.FileName = Path.GetFullPath(getSteamInstallPath() + "\\steam.exe");
            Proc.StartInfo.Arguments = String.Format("-applaunch {0}", appId);
            Proc.StartInfo.UseShellExecute = false;
            Proc.StartInfo.CreateNoWindow = false;
            Proc.Start();

            return true;
        }

        public bool IsSDK2013Installed()
        {
            //SteamAppInstalled_t apps = SteamClient.GetISteamApps(SteamAPI.GetHSteamUser(), SteamAPI.GetHSteamPipe(), Constants.STEAMAPPS_INTERFACE_VERSION);
            //return true;
            /*
            foreach (var app in apps)
            {

            }
            */

            SteamAppList.GetInstalledApps(m_AppList, (uint)m_AppList.Length);

            for (int i = 0; i < m_AppList.Length; i++)
            {
                /*
                string Name;
                int ret = SteamAppList.GetAppName(m_AppList[i], out Name, 256);
                MessageBox.Show("GetAppName(m_AppList[0], out Name, 256) : " + ret + " -- " + Name); 
                */
                if (m_AppList[i].m_AppId == SOURCE_SDK_2013_BASE)
                    return true;
            }
            return false;
        }

        void OnSteamAppInstalled(SteamAppInstalled_t pCallback)
        {
            //Debug.Log("[" + SteamAppInstalled_t.k_iCallback + " - SteamAppInstalled] - " + pCallback.m_nAppID);
        }

        void OnSteamAppUninstalled(SteamAppUninstalled_t pCallback)
        {
            //Debug.Log("[" + SteamAppUninstalled_t.k_iCallback + " - SteamAppUninstalled] - " + pCallback.m_nAppID);
        }

        private AppId_t[] m_AppList;
        protected Callback<SteamAppInstalled_t> m_SteamAppInstalled;
        protected Callback<SteamAppUninstalled_t> m_SteamAppUninstalled;
    }
}
