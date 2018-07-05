using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using SharpConfig;

namespace Nightfire_Source_Updater_Client
{
    class Bootstrapper
    {
        public static string MainDownloadDir = String.Empty;

        public const string Files_Base_URI = "http://nfsource.mov.re";
        public const string ExpectedModDir = "steamapps/sourcemods/nightfiresource/";
        public const string LocalCachesXMLName = "caches.xml";
        public const string DefaultChannel = "nightfiresource-master";
        public const string Bootstrapper_Path = "bootstrapper/Nightfire_Source_Updater_Client.exe";

        public void BeginChecks()
        {
            IniFileMgr.TryOpenFile(Path.Combine(MakeRelativeModPath(), IniFileMgr.ConfFileName));
            var SteamworksMgr = new SteamWorksMgr();
            string expectedDir = Path.GetFullPath
            (
                    Program.IsDebugRelease ? 
                    Path.Combine( Directory.GetCurrentDirectory(), "data\\") :
                    MakeRelativeModPath()
            );

            string curDir = Path.GetFullPath(Directory.GetCurrentDirectory());
            MainDownloadDir = curDir;

            if (curDir != expectedDir)
                MainDownloadDir = expectedDir; //They didn't put it in the sourcemods folder so workaround it

            string localCachesFullPath = XMLMgr.GetCachesLocalFullPath(LocalCachesXMLName);

            //Initialize XMLMgr and get the state
            var xmlFuncs = new XMLMgr();

            //Check bootstrapper version
            CheckBootstrapper(xmlFuncs);

            XMLMgr.XMLCorrectStates state = xmlFuncs.GetXMLFormatCorrectState(localCachesFullPath);

            if (!File.Exists(localCachesFullPath) || state != XMLMgr.XMLCorrectStates.XML_STATE_FORMAT_VALID)
            {
                downloadCachesXMLFileAndStartIntegrityChecks(LocalCachesXMLName);
            }
            else
            {
                Main.CurrentForm.ChangeLabelText("Found caches.xml, checking for updates...");

                string outID, outVersion;
                int ServerVer, ClientVer;
                xmlFuncs.GetIDAndVersionCachesXML(getMainCachesXMLURI(), out outID, out outVersion); //Get the version on the server

                if (int.TryParse(outVersion, out ServerVer))
                {
                    xmlFuncs.GetIDAndVersionCachesXML(localCachesFullPath, out outID, out outVersion); //Get the one on the client
                    if (int.TryParse(outVersion, out ClientVer))
                    {
                        /*
                         * Evaluate three cases: 
                            1 - they're greater than the server and therefor they somehow got desync'd or we can't trust them since they modified caches.xml.
                            2 - They're on an older version.
                            3 - They never completed any integrity checks. (Internet dc'd, application (crashed?), etc)
                        */
                        if (ServerVer > ClientVer || ClientVer > ServerVer)
                        {
                            if (ClientVer > ServerVer)
                                Main.CurrentForm.ChangeLabelText("Out of sync with the server, re-syncing");
                            else
                                Main.CurrentForm.ChangeLabelText(String.Format("Found a newer version at version {0}, will try to download...", ServerVer));

                            DownloadRemoteCachesXMLFile(ServerVer); 
                        }
                        else
                        {
                            //Todo, versions being the same don't mean anything, their internet could've disconnected.
                            //Instead, keep track on how many files they've downloaded out of the grand total and resume downloads if files are missing.

                            //Temporarily doing this for now
                            if (!IniFileMgr.integritychecks_done)
                            {
                                Main.CurrentForm.ChangeLabelText("Integrity checks were never completed.");
                                DownloadRemoteCachesXMLFile(ServerVer);
                                return;
                            }

                            Main.CurrentForm.ChangeLabelText("You're up to date!");
                        }
                    }
                }
                //Get the caches.xml version and compare against the one on the server
                //If the one on the server is greater
                //Download the new changeset
                //Begin Updating

            }
        }

        public void DownloadRemoteCachesXMLFile(int ServerVer)
        {
            downloadCachesXMLFile(LocalCachesXMLName); //Replace the old one
            DownloadChangeSetFileAndBeginChecks(String.Format("changeset_{0}.xml", ServerVer), ChangeSets.CHANGESET_TYPES.CHANGESET_NEW);
        }

        public void CheckBootstrapper(XMLMgr xmlFuncs)
        {
            string outID, remoteExeHash;
            string xmlURI = getBootstrapperCachesXMLURI();
            string exePathOldExtension = $"{Application.ExecutablePath}.old"; //Append .old extension

            if (File.Exists($"{Application.ExecutablePath}.dev")) //Don't update the bootstrapper
                return;

            //check for old exe's during startup
            if (File.Exists(exePathOldExtension))
                File.Delete(exePathOldExtension);

            xmlFuncs.GetIDAndVersionCachesXML(xmlURI, out outID, out remoteExeHash); //Get the version on the server
            
            if (remoteExeHash == null) //It's not on the server and should be generated
            {
                MessageBox.Show($"Couldn't download/read the bootstrapper integrity file from: {xmlURI}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
                return; //Because it still executes after Application.Exit which is normal...
            }
            
            //This requires implementation in the builder... (Done)
            var hashFuncs = new Hashing();
            string exeHash = File.Exists(Application.ExecutablePath) ? hashFuncs.genFileHash(Application.ExecutablePath) : "0";
            string curDir = Directory.GetCurrentDirectory();

            if (exeHash != remoteExeHash)
            {
                //rename current exe to Nightfire_Source_Updater_Client.exe.old
                File.Move(Application.ExecutablePath, exePathOldExtension); 

                //download the new one
                WebClient client = DownloadFile(curDir, $"{Bootstrapper_Path}.{Compressor.DEFAULT_COMPRESSION_TYPE}", IniFileMgr.prod_channel); //Download it
                client.DownloadProgressChanged += (o, e) =>
                {
                    GUIRendering.UpdateDownloadProgress(e, $"{Bootstrapper_Path}.{Compressor.DEFAULT_COMPRESSION_TYPE}");
                };
                client.DownloadFileCompleted += (o, e) =>
                {
                    //extract it and delete the downloaded gz file
                    invokeDecompressFile($"{Bootstrapper_Path}.{Compressor.DEFAULT_COMPRESSION_TYPE}", false);

                    //Now move the file to the current directory and delete the bootstrapper folder
                    string absolutePathExe = Path.Combine(curDir, "Nightfire_Source_Updater_Client.exe");
                    File.Move(Bootstrapper_Path, absolutePathExe);
                    Directory.Delete(Path.GetFullPath("bootstrapper"), true);

                    //restart the app
                    Process.Start(absolutePathExe);

                    Application.Exit();
                };
            }
        }

        /* makes a full path to the mod install directory */
        static public string MakeRelativeModPath()
        {
            var SteamworksMgr = new SteamWorksMgr();
            return Path.Combine(SteamworksMgr.getSteamInstallPath(), ExpectedModDir);
        }

        public string getMainCachesXMLURI()
        {
            return $"{Files_Base_URI}/{IniFileMgr.prod_channel}/caches.xml";
        }

        public string getBootstrapperCachesXMLURI()
        {
            return $"{Files_Base_URI}/{IniFileMgr.prod_channel}/{IniFileMgr.prod_channel}/bootstrapper/caches.xml";
        }

        public WebClient downloadCachesXMLFile(string filename)
        {
            WebClient client = DownloadFile(MainDownloadDir, filename, IniFileMgr.prod_channel, true); //Download it
            client.DownloadProgressChanged += (o, e) =>
            {
                GUIRendering.UpdateDownloadProgress(e, filename);
            };
            return client;
        }

        public void downloadCachesXMLFileAndStartIntegrityChecks(string filename)
        {
            WebClient client = downloadCachesXMLFile(filename);
            client.DownloadFileCompleted += (o, e) =>
            {
                beginIntegrityChecks();
            };
        }

        public static void BootstrapperUpdateAddedToList(string chSetName, int chSetCountAdded, int chSetCountTotal, string fileName)
        {
            double percentageF = ((chSetCountAdded * 1.0 / chSetCountTotal * 1.0) * 100.0);
            int percentage = (int)Math.Floor(percentageF + 0.5);
            Main.CurrentForm.UpdateProgBar2(percentage);
            Main.CurrentForm.ChangeLabelText("Added: " + fileName);
        }

        public void DoByFileEditMode(ChangeSets chSet, ChangeSets.ChangeSetC item, string FilePath)
        {
            switch (item.mode)
            {
                case "add":
                case "edit":
                    downloadFileIfLocalDiffers(chSet, item, FilePath);
                    break;
                case "delete":
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);
                    break;
            }
        }

        public void DoByDirectoryEditMode(ChangeSets chSet, ChangeSets.ChangeSetC item, string FilePath)
        {
            switch (item.mode)
            {
                case "add-dir":
                    if (!Directory.Exists(FilePath))
                    {
                        if (File.Exists(FilePath))
                            File.Delete(FilePath); //We can't have a file and a directory with the same name
                        Directory.CreateDirectory(FilePath);
                    }
                    break;
                case "delete":
                    Directory.Delete(FilePath, true);
                    break;
            }
        }

        public FileInfo getFileInfoData(string filePath, bool toMainDownloadDir = true)
        {
            filePath = filePath.Replace("nightfiresource/", "");
            return new FileInfo(Path.Combine(toMainDownloadDir ? MainDownloadDir : String.Empty, filePath)/*$"{MainDownloadDir}{filePath}"*/); //Remove 'nightfiresource/' which is just part of the url path.
        }
        public void invokeDecompressFile(string filePath, bool toMainDownloadDir = true)
        {
            Decompressor.Decompress(getFileInfoData(filePath, toMainDownloadDir));
            File.Delete(getFileInfoData(filePath, toMainDownloadDir).ToString());
        }

        public void downloadFileIfLocalDiffers(ChangeSets chSet, ChangeSets.ChangeSetC item, string FilePath)
        {
            var hashFuncs = new Hashing();
            string SHA1Hash = File.Exists(FilePath) ? hashFuncs.genFileHash(FilePath) : "0"; //Generate our sha1 file hash which we'll use later on

            ChangeSets.MatchesResult flags = chSet.DoesCurCHSetFileHashMatchFileInDir(item, MainDownloadDir, FilePath, SHA1Hash);
            bool matches_hash = (flags & ChangeSets.MatchesResult.matches_hash) != 0;
            bool matches_filename = (flags & ChangeSets.MatchesResult.matches_filename) != 0;
            WebClient clientNew;

            //Evaluate if we should apply compression, this is kind of a lazy approach really, but it's simple I guess...
            bool shouldApplyCompType = chSet.DoesFileHaveCompression(item);

            //Replace the nightfiresource path with nothing since it's just leftover url stuff. Todo: Maybe just get rid of it altogether?
            if (shouldApplyCompType)
                item.filename = Compressor.getFilePathAndCompressionAppended(item.filename);

            if (!matches_hash)
            {
                var locker = new object();
                using (clientNew = DownloadFile(MainDownloadDir, item.filename, IniFileMgr.prod_channel))
                {
                    {
                        lock (locker)
                        {
                            clientNew.DownloadProgressChanged += (sender1, e1) =>
                            {
                                GUIRendering.UpdateDownloadProgress(e1, item.filename);
                            };
                            clientNew.DownloadFileCompleted += (o, e) =>
                            {
                                Main.CurrentForm.ChangeLabelText("Downloaded File: \n" + item.filename);

                                if (shouldApplyCompType)
                                {
                                    //Decompress it and delete it after
                                    invokeDecompressFile(item.filename);
                                    Main.CurrentForm.ChangeLabelText("Decompressed File: \n" + item.filename);
                                }

                            };
                            Monitor.PulseAll(locker);
                        }
                    }
                }
                clientNew.Dispose();
            }
            else
            {
                //Also cleanup after old gz files.
                if (shouldApplyCompType)
                {
                    string fPath = getFileInfoData(item.filename).ToString();
                    if (File.Exists(fPath))
                        File.Delete(fPath);
                }

                Main.CurrentForm.ChangeLabelText("Passed integrity check for: \n" + item.filename);
            }
        }

        /* This function can be used to read and download the game contents from any of the integrity or changeset files. */
        public void readChangesetAndBeginDownloading(string xmlchangesetfile, ChangeSets.CHANGESET_TYPES type)
        {
            var curChangeSet = new ChangeSets.ChangeSetIterationProgress();
            curChangeSet.changeSetMgr = new ChangeSets();
            curChangeSet.changeSetType = type;

            if (!curChangeSet.changeSetMgr.LoadChangesetFile(IniFileMgr.prod_channel, xmlchangesetfile, type))
            {
                MessageBox.Show(String.Format("Fatal: Couldn't load {0}, try deleting caches.xml and try again.", xmlchangesetfile));
                Application.Exit();
                return;
            }

            bool changeSetExists = curChangeSet.changeSetMgr.DoesChangeSetExist(type);

            if (changeSetExists)
            {
                try
                {
                    //Make a thread since this will block the gui thread...
                    new Thread(() =>
                    {
                        var watch = Stopwatch.StartNew();

                        foreach (var item in ChangeSets.getAppropriateListForType(type).Select((value, i) => new { i, value }))
                        {
                            var it = item.value;
                            curChangeSet.curDataChecked = item.i;

                            //Update the progress bar...
                            GUIRendering.UpdateChangesetProgressBar(curChangeSet);

                            //Replace the nightfiresource path with nothing since it's just leftover url stuff. Todo: Maybe just get rid of it altogether?
                            string FilePath = $"{MainDownloadDir}{it.filename.Replace("nightfiresource/", "")}";

                            //Is this a directory or a file?
                            switch(it.filetype)
                            {
                                case "directory":
                                    DoByDirectoryEditMode(curChangeSet.changeSetMgr, it, FilePath);
                                    break;
                                case "file":
                                    DoByFileEditMode(curChangeSet.changeSetMgr, it, FilePath);
                                    break;
                            }
                        }

                        watch.Stop();
                        TimeSpan elapsedTime = watch.Elapsed;
                        Main.CurrentForm.ChangeLabelText(String.Format("Updates completed in: {0}:{1}:{2}.", elapsedTime.Hours, elapsedTime.Minutes, elapsedTime.Seconds));

                        IniFileMgr.getIniFileMgrConfigPtr()["General"]["completedintegritychecks"].BoolValue = true;
                        IniFileMgr.SaveConfigFile();
                    }).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Application will now exit, integrity file was never downloaded?");
                Environment.Exit(0);
            }
        }

        public void beginIntegrityChecks()
        {
            var xmlFuncs = new XMLMgr();
            string outID, outVersion;
            xmlFuncs.GetIDAndVersionCachesXML(XMLMgr.GetCachesLocalFullPath(LocalCachesXMLName), out outID, out outVersion);

            if (outID.Length != 0)
            {
                DownloadChangeSetFileAndBeginChecks("integrity.xml", ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT);
                return;
            }
            return;
        }

        //Once it's downloaded
        //Download the latest integrity.xml file from the server
        //Start downloading all the files but verify integrity while going through the changeset so we don't download stuff that isn't needed
        public void DownloadChangeSetFileAndBeginChecks(string filename, ChangeSets.CHANGESET_TYPES type)
        {
            WebClient client = DownloadFile(MainDownloadDir, $"{IniFileMgr.prod_channel}-changesets/{filename}", IniFileMgr.prod_channel, true); //Download it
            client.DownloadProgressChanged += (o, e) =>
            {
                GUIRendering.UpdateDownloadProgress(e, $"{IniFileMgr.prod_channel}-changesets/{filename}");
            };
            client.DownloadFileCompleted += (o, e) =>
            {
                readChangesetAndBeginDownloading(filename, type);
            };
        }

        public WebClient DownloadFile(string toMainTreeDir, string netFilePath, string channel = "", bool fromMainDir = false)
        {
            string fullDlPath = netFilePath;
            fullDlPath = netFilePath.Replace("nightfiresource/", "");
            toMainTreeDir = Path.GetFullPath(Path.Combine(toMainTreeDir, fullDlPath));// $"{toMainTreeDir}/{fullDlPath}");

            try {
                new FileInfo(toMainTreeDir).Directory.Create();
            } catch(Exception ex){}

            WebClient client = new WebClient();
            //channel = channel != "" ? channel : String.Empty;
            channel = !fromMainDir ? $"{channel}/{channel}" : $"{channel}";
            Uri ur = new Uri($"{Files_Base_URI}/{channel}/{Uri.EscapeDataString(netFilePath)}");

            client.Credentials = new NetworkCredential("username", "password");
            client.DownloadProgressChanged += WebClientDownloadProgressChanged;
            client.DownloadFileCompleted += WebClientDownloadCompleted;
            client.DownloadFileAsync(ur, toMainTreeDir);

            if (Program.IsDebugRelease)
                Console.WriteLine($"Downloading {netFilePath}...");

            return client;
        }

        void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) { }
        void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs e){}
    }
}