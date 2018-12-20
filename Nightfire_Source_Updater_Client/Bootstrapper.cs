using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
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
        private static ChangeSets BootStrapperChSetInst = null;

        public async Task BeginChecks()
        {
            BootStrapperChSetInst = ChangeSets.getChangeSetsClassPtr();
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
            await CheckBootstrapper(xmlFuncs);

            XMLMgr.XMLCorrectStates state = xmlFuncs.GetXMLFormatCorrectState(localCachesFullPath);

            if (!File.Exists(localCachesFullPath) || state != XMLMgr.XMLCorrectStates.XML_STATE_FORMAT_VALID)
            {
                await downloadCachesXMLFileAndStartIntegrityChecks(LocalCachesXMLName);
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
                                Main.CurrentForm.ChangeLabelText($"Found a newer version at version {ServerVer}, will try to download...");

                            await DownloadRemoteCachesXMLFile(ServerVer); 
                        }
                        else
                        {
                            //Todo, versions being the same don't mean anything, their internet could've disconnected.
                            //Instead, keep track on how many files they've downloaded out of the grand total and resume downloads if files are missing.

                            //Temporarily doing this for now
                            if (!IniFileMgr.integritychecks_done)
                            {
                                Main.CurrentForm.ChangeLabelText("Integrity checks were never completed... Checking integrity!");
                                await DownloadRemoteCachesXMLFile(ServerVer);
                                return;
                            }

                            Main.CurrentForm.ChangeLabelText("You're up to date!");
                            Main.CurrentForm.UpdateProgBar2(100); //w/e
                        }
                    }
                }
                //Get the caches.xml version and compare against the one on the server
                //If the one on the server is greater
                //Download the new changeset
                //Begin Updating

            }
            await Task.FromResult(0);
        }

        public async Task DownloadRemoteCachesXMLFile(int ServerVer)
        {
            await downloadCachesXMLFile(LocalCachesXMLName); //Replace the old one
            await DownloadChangeSetFileAndBeginChecks($"changeset_{ServerVer}.xml", ChangeSets.CHANGESET_TYPES.CHANGESET_NEW);
        }

        public async Task CheckBootstrapper(XMLMgr xmlFuncs)
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
                WebClient client = await DownloadFile(curDir, $"{Bootstrapper_Path}.{Compressor.DEFAULT_COMPRESSION_TYPE}", IniFileMgr.prod_channel); //Download it
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
            await Task.FromResult(0);
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

        public async Task<WebClient> downloadCachesXMLFile(string filename)
        {
            WebClient client = await DownloadFile(MainDownloadDir, filename, IniFileMgr.prod_channel, true); //Download it
            client.DownloadProgressChanged += (o, e) =>
            {
                GUIRendering.UpdateDownloadProgress(e, filename);
            };
            await Task.FromResult(0);
            return client;
        }

        public async Task downloadCachesXMLFileAndStartIntegrityChecks(string filename)
        {
            WebClient client = await downloadCachesXMLFile(filename);
            client.DownloadFileCompleted += async(o, e) =>
            {
                await beginIntegrityChecks();
            };
        }

        public static void BootstrapperUpdateAddedToList(string chSetName, int chSetCountAdded, int chSetCountTotal, string fileName)
        {
            if (chSetCountTotal == 0) //Don't run this if we've no total since some functions don't pass it
                return;

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
                    addFileToDLQueueFileIfLocalDiffers(chSet, item, FilePath);
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

        public void addFileToDLQueueFileIfLocalDiffers(ChangeSets chSet, ChangeSets.ChangeSetC item, string FilePath)
        {
            var hashFuncs = new Hashing();
            string SHA1Hash = File.Exists(FilePath) ? hashFuncs.genFileHash(FilePath) : "0"; //Generate our sha1 file hash which we'll use later on

            ChangeSets.MatchesResult flags = chSet.DoesCurCHSetFileHashMatchFileInDir(item, MainDownloadDir, FilePath, SHA1Hash);
            bool matches_hash = (flags & ChangeSets.MatchesResult.matches_hash) != 0;
            bool matches_filename = (flags & ChangeSets.MatchesResult.matches_filename) != 0;

            //Evaluate if we should apply compression, this is kind of a lazy approach really, but it's simple I guess...
            bool shouldApplyCompType = chSet.DoesFileHaveCompression(item);

            //Replace the nightfiresource path with nothing since it's just leftover url stuff. Todo: Maybe just get rid of it altogether?
            if (shouldApplyCompType)
                item.filename = Compressor.getFilePathAndCompressionAppended(item.filename);

            if (!matches_hash)
            {
                BootStrapperChSetInst.AddToChangeSet(ChangeSets.CHANGESET_TYPES.CHANGESET_DL_QUEUE, item.hash, item.filename, item.filetype, item.filesize, "download");
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
        public async Task readChangesetAndBeginDownloading(string xmlchangesetfile, ChangeSets.CHANGESET_TYPES type)
        {
            var curChangeSet = new ChangeSets.ChangeSetIterationProgress();
            curChangeSet.changeSetMgr = new ChangeSets();
            curChangeSet.changeSetType = type;

            if (!curChangeSet.changeSetMgr.LoadChangesetFile(IniFileMgr.prod_channel, xmlchangesetfile, type))
            {
                MessageBox.Show($"Fatal: Couldn't load {xmlchangesetfile}, try deleting caches.xml and try again.");
                Application.Exit();
                return;
            }

            bool changeSetExists = curChangeSet.changeSetMgr.DoesChangeSetExist(type);

            if (changeSetExists)
            {
                try
                {
                    var watch = Stopwatch.StartNew();

                    //Todo: re-write this so downloads are queued and downloaded as tasks
                    foreach (var item in ChangeSets.getAppropriateListForType(type).Select((value, i) => new { i, value }))
                    {
                        var it = item.value;
                        curChangeSet.curDataChecked = item.i;

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

                        //Update the progress bar...
                        GUIRendering.UpdateChangesetProgressBar(curChangeSet);
                    }

                    //change the changeset type for the download queue
                    curChangeSet.changeSetType = ChangeSets.CHANGESET_TYPES.CHANGESET_DL_QUEUE;
                    var dlChSet = ChangeSets.getAppropriateListForType(ChangeSets.CHANGESET_TYPES.CHANGESET_DL_QUEUE);

                    //It would be neater if these two were a single lambda expression, but no clue on how that's done in c#, only c++
                    foreach (var item in dlChSet.Select((value, i) => new { i, value }))
                    {
                        var it = item.value;
                        curChangeSet.curDataChecked = item.i;

                        WebClient clientNew = await DownloadFile(MainDownloadDir, it.filename, IniFileMgr.prod_channel);
                        clientNew.DownloadProgressChanged += (sender1, e1) =>
                        {
                            GUIRendering.UpdateDownloadProgress(e1, it.filename);
                        };
                        clientNew.DownloadFileCompleted += (o, e) =>
                        {
                            Main.CurrentForm.ChangeLabelText("Downloaded File: \n" + it.filename);
                            //Evaluate if we should apply compression, this is kind of a lazy approach really, but it's simple I guess...
                            bool shouldApplyCompType = curChangeSet.changeSetMgr.DoesFileHaveCompression(it);

                            if (shouldApplyCompType)
                            {
                                //Decompress it and delete it after
                                invokeDecompressFile(it.filename);
                                Main.CurrentForm.ChangeLabelText("Decompressed File: \n" + it.filename);
                            }

                        };
                        clientNew.Dispose();

                        //Update the progress bar...
                        GUIRendering.UpdateChangesetProgressBar(curChangeSet);
                    }

                    watch.Stop();
                    TimeSpan elapsedTime = watch.Elapsed;
                    Main.CurrentForm.ChangeLabelText($"Updates completed in: {elapsedTime.Hours}:{elapsedTime.Minutes}:{elapsedTime.Seconds}");

                    /* Always force integrity checks for now, until the above is fixed. Async downloads should be queued and waited for as a list of tasks, 
                         * right now it's doing more than one download at once which causes this behaviour where it falsely completes integrity checks. */

                    IniFileMgr.getIniFileMgrConfigPtr()["General"]["completedintegritychecks"].BoolValue = false;
                    IniFileMgr.SaveConfigFile();
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

        public async Task beginIntegrityChecks()
        {
            var xmlFuncs = new XMLMgr();
            string outID, outVersion;
            xmlFuncs.GetIDAndVersionCachesXML(XMLMgr.GetCachesLocalFullPath(LocalCachesXMLName), out outID, out outVersion);

            if (outID.Length != 0)
            {
                await DownloadChangeSetFileAndBeginChecks("integrity.xml", ChangeSets.CHANGESET_TYPES.CHANGESET_INTEGRITY_CURRENT);
                return;
            }
            return;
        }

        //Once it's downloaded
        //Download the latest integrity.xml file from the server
        //Start downloading all the files but verify integrity while going through the changeset so we don't download stuff that isn't needed
        public async Task<WebClient> DownloadChangeSetFileAndBeginChecks(string filename, ChangeSets.CHANGESET_TYPES type)
        {
            WebClient client = await DownloadFile(MainDownloadDir, $"{IniFileMgr.prod_channel}-changesets/{filename}", IniFileMgr.prod_channel, true); //Download it
            client.DownloadProgressChanged += (o, e) =>
            {
                GUIRendering.UpdateDownloadProgress(e, $"{IniFileMgr.prod_channel}-changesets/{filename}");
            };
            client.DownloadFileCompleted += async(o, e) =>
            {
                await readChangesetAndBeginDownloading(filename, type);
            };
            await Task.FromResult(0);
            return client;
        }

        public async Task<WebClient> DownloadFile(string toMainTreeDir, string netFilePath, string channel = "", bool fromMainDir = false)
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

            await Task.FromResult(0);
            return client;
        }

        void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) { }
        void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs e){}
    }
}