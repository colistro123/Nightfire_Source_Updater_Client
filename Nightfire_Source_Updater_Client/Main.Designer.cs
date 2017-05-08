namespace Nightfire_Source_Updater_Client
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.progBar_1 = new System.Windows.Forms.ProgressBar();
            this.lbl_FileDownload = new System.Windows.Forms.Label();
            this.progBar_2 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // progBar_1
            // 
            this.progBar_1.BackColor = System.Drawing.SystemColors.Control;
            this.progBar_1.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.progBar_1.Location = new System.Drawing.Point(15, 94);
            this.progBar_1.Name = "progBar_1";
            this.progBar_1.Size = new System.Drawing.Size(349, 10);
            this.progBar_1.TabIndex = 0;
            this.progBar_1.Value = 50;
            // 
            // lbl_FileDownload
            // 
            this.lbl_FileDownload.Location = new System.Drawing.Point(12, 9);
            this.lbl_FileDownload.Name = "lbl_FileDownload";
            this.lbl_FileDownload.Size = new System.Drawing.Size(349, 82);
            this.lbl_FileDownload.TabIndex = 1;
            this.lbl_FileDownload.Text = "Checking for caches.xml...";
            // 
            // progBar_2
            // 
            this.progBar_2.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.progBar_2.Location = new System.Drawing.Point(15, 110);
            this.progBar_2.Name = "progBar_2";
            this.progBar_2.Size = new System.Drawing.Size(349, 10);
            this.progBar_2.TabIndex = 2;
            this.progBar_2.Value = 50;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 132);
            this.Controls.Add(this.progBar_2);
            this.Controls.Add(this.lbl_FileDownload);
            this.Controls.Add(this.progBar_1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "Main";
            this.Text = "Nightfire: Source Updater";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.Label lbl_FileDownload;
        public System.Windows.Forms.ProgressBar progBar_1;
        public System.Windows.Forms.ProgressBar progBar_2;
    }
}

