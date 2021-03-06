﻿using System;
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
                            DialogResult dialogResult = MessageBox.Show($"Couldn't find a valid Steam.exe in {path}, please provide the correct path to your Steam folder.",
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
