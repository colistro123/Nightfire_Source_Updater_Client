using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nightfire_Source_Updater_Client
{
    public partial class Main : Form
    {
        public static Main CurrentForm;
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            CurrentForm = this;
            var bootStrapper = new Bootstrapper();
            bootStrapper.BeginChecks().Wait(); //async
        }

        public void ChangeLabelText(string text)
        {
            lbl_FileDownload.Invoke((MethodInvoker) delegate 
            {
                // Running on the UI thread
                lbl_FileDownload.Text = text;
                lbl_FileDownload.AutoSize = false;
                lbl_FileDownload.TextAlign = ContentAlignment.MiddleLeft;
            });
        }

        public void UpdateProgBar1(int progress)
        {
            progBar_1.Invoke((MethodInvoker)delegate
            {
                progBar_1.Value = progress;
            });
        }

        public void UpdateProgBar2(int progress)
        {
            progBar_2.Invoke((MethodInvoker)delegate
            {
                progBar_2.Value = progress;
            });
        }
    }
}
