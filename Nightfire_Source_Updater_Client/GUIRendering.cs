using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nightfire_Source_Updater_Client
{
    class GUIRendering
    {
        static long lasttimeDownload;
        static long bytesLastDownload;

        //GUI related things
        static public void UpdateDownloadProgress(DownloadProgressChangedEventArgs e, string filename)
        {
            long curtime = Environment.TickCount;
            if (curtime - lasttimeDownload >= 1000)
            {
                double speed = Utils.roundToMb(e.BytesReceived - bytesLastDownload);
                string speedLblDesc = String.Format("Downloading File: {0}\n Size: {1} Megabytes of {2} Megabytes\n Speed: {3} MB/s",
                    filename,
                    Math.Abs(Utils.roundToMb(e.BytesReceived)),
                    Math.Abs(Utils.roundToMb(e.TotalBytesToReceive)),
                    Math.Abs(speed));

                Main.CurrentForm.ChangeLabelText(speedLblDesc);

                bytesLastDownload = e.BytesReceived;
                lasttimeDownload = curtime;
            }

            Main.CurrentForm.UpdateProgBar2(e.ProgressPercentage);
            Application.DoEvents();
        }

        /* Updates the changeset progress bar based on how many files were checked / downloaded */
        static public void UpdateChangesetProgressBar(ChangeSets.ChangeSetIterationProgress progBarData)
        {
            double percentageF = ((progBarData.curDataChecked * 1.0 / progBarData.changeSetMgr.GetChangeSetCount(progBarData.changeSetType) * 1.0) * 100.0);
            int percentage = (int)Math.Floor(percentageF + 0.5);
            Main.CurrentForm.UpdateProgBar1(percentage);
        }
        //End of GUI
    }
}
