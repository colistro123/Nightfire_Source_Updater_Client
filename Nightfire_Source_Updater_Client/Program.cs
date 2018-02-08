using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nightfire_Source_Updater_Client
{
    static class Program
    {

        public static bool IsDebugRelease
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var steamWorksMgr = new SteamWorksMgr();
            bool steamWorksStarted = steamWorksMgr.TryInit();
            if (!steamWorksStarted)
            {
                MessageBox.Show("Error while trying to start Steamworks, please launch the Steam Client (if it's closed) or install it (if it's not installed).");
                return;
            }

            bool foundSteamDir = false;
            if (!steamWorksMgr.IsSDK2013Installed())
            {
                if (steamWorksMgr.getSteamInstallPath() == "")
                {
                    do
                    {
                        string path = steamWorksMgr.searchForSteamDir();
                        string pathExe = Path.GetFullPath(path + "\\Steam.exe");
                        if (!File.Exists(pathExe))
                        {
                            DialogResult dialogResult = MessageBox.Show(String.Format("Couldn't find a valid Steam.exe in {0}, please provide the correct path to your Steam folder.", path),
                                "Nightfire: Source Updater", MessageBoxButtons.OKCancel, MessageBoxIcon.None,
                                MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000);

                            if (dialogResult == DialogResult.Cancel)
                                return;
                        }
                        else
                        {
                            foundSteamDir = true;
                        }
                    } while (!foundSteamDir);
                }
                steamWorksMgr.tryInstallSDK2013();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
