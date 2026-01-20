
namespace WinRARRed.Forms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tbWinRARDirectory = new System.Windows.Forms.TextBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            btnWinRARDirectoryBrowse = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            linkLabel1 = new System.Windows.Forms.LinkLabel();
            label1 = new System.Windows.Forms.Label();
            groupBox2 = new System.Windows.Forms.GroupBox();
            label3 = new System.Windows.Forms.Label();
            btnReleaseDirectoryBrowse = new System.Windows.Forms.Button();
            tbReleaseDirectory = new System.Windows.Forms.TextBox();
            btnStart = new System.Windows.Forms.Button();
            groupBox3 = new System.Windows.Forms.GroupBox();
            btnTemporaryDirectoryBrowse = new System.Windows.Forms.Button();
            tbOutputDirectory = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            groupBox11 = new System.Windows.Forms.GroupBox();
            tbLog = new System.Windows.Forms.TextBox();
            gbInput = new System.Windows.Forms.GroupBox();
            groupBox12 = new System.Windows.Forms.GroupBox();
            label19 = new System.Windows.Forms.Label();
            btnVerificationFileBrowse = new System.Windows.Forms.Button();
            tbVerificationFilePath = new System.Windows.Forms.TextBox();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            tsmiFileExit = new System.Windows.Forms.ToolStripMenuItem();
            tsmiView = new System.Windows.Forms.ToolStripMenuItem();
            tsmiViewCommandLines = new System.Windows.Forms.ToolStripMenuItem();
            tsmiSettings = new System.Windows.Forms.ToolStripMenuItem();
            tsmiSettingsOptions = new System.Windows.Forms.ToolStripMenuItem();
            opStatus2 = new WinRARRed.Controls.OperationProgressStatusUserControl();
            opStatus1 = new WinRARRed.Controls.OperationProgressStatusUserControl();
            btnClearLog = new System.Windows.Forms.Button();
            cbAutoScroll = new System.Windows.Forms.CheckBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox11.SuspendLayout();
            gbInput.SuspendLayout();
            groupBox12.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tbWinRARDirectory
            // 
            tbWinRARDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbWinRARDirectory.Location = new System.Drawing.Point(6, 22);
            tbWinRARDirectory.Name = "tbWinRARDirectory";
            tbWinRARDirectory.Size = new System.Drawing.Size(755, 23);
            tbWinRARDirectory.TabIndex = 0;
            tbWinRARDirectory.Text = "G:\\WinRAR2";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnWinRARDirectoryBrowse);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(linkLabel1);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(tbWinRARDirectory);
            groupBox1.Location = new System.Drawing.Point(6, 22);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(848, 90);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "WinRAR directory";
            // 
            // btnWinRARDirectoryBrowse
            // 
            btnWinRARDirectoryBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnWinRARDirectoryBrowse.Location = new System.Drawing.Point(767, 22);
            btnWinRARDirectoryBrowse.Name = "btnWinRARDirectoryBrowse";
            btnWinRARDirectoryBrowse.Size = new System.Drawing.Size(75, 23);
            btnWinRARDirectoryBrowse.TabIndex = 4;
            btnWinRARDirectoryBrowse.Text = "Browse";
            btnWinRARDirectoryBrowse.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 63);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(205, 15);
            label2.TabIndex = 3;
            label2.Text = "Get the WinRAR installation files here:";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new System.Drawing.Point(217, 63);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new System.Drawing.Size(218, 15);
            linkLabel1.TabIndex = 2;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "http://rescene.wikidot.com/rar-versions";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 48);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(740, 15);
            label1.TabIndex = 1;
            label1.Text = "This is the directory that contains the extracted WinRAR installation files. Each WinRAR installation should be extracted to it's own directory.";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(btnReleaseDirectoryBrowse);
            groupBox2.Controls.Add(tbReleaseDirectory);
            groupBox2.Location = new System.Drawing.Point(6, 118);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(848, 71);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "Release directory";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(6, 48);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(654, 15);
            label3.TabIndex = 3;
            label3.Text = "This is the directory that contains the release files and directories. Anything in this directory will be rarred for the checksum.";
            // 
            // btnReleaseDirectoryBrowse
            // 
            btnReleaseDirectoryBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnReleaseDirectoryBrowse.Location = new System.Drawing.Point(767, 22);
            btnReleaseDirectoryBrowse.Name = "btnReleaseDirectoryBrowse";
            btnReleaseDirectoryBrowse.Size = new System.Drawing.Size(75, 23);
            btnReleaseDirectoryBrowse.TabIndex = 6;
            btnReleaseDirectoryBrowse.Text = "Browse";
            btnReleaseDirectoryBrowse.UseVisualStyleBackColor = true;
            // 
            // tbReleaseDirectory
            // 
            tbReleaseDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbReleaseDirectory.Location = new System.Drawing.Point(6, 22);
            tbReleaseDirectory.Name = "tbReleaseDirectory";
            tbReleaseDirectory.Size = new System.Drawing.Size(755, 23);
            tbReleaseDirectory.TabIndex = 5;
            tbReleaseDirectory.Text = "E:\\temp3";
            // 
            // btnStart
            // 
            btnStart.Location = new System.Drawing.Point(800, 385);
            btnStart.Name = "btnStart";
            btnStart.Size = new System.Drawing.Size(75, 23);
            btnStart.TabIndex = 3;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(btnTemporaryDirectoryBrowse);
            groupBox3.Controls.Add(tbOutputDirectory);
            groupBox3.Controls.Add(label4);
            groupBox3.Location = new System.Drawing.Point(6, 272);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(848, 72);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "Output directory";
            // 
            // btnTemporaryDirectoryBrowse
            // 
            btnTemporaryDirectoryBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnTemporaryDirectoryBrowse.Location = new System.Drawing.Point(767, 22);
            btnTemporaryDirectoryBrowse.Name = "btnTemporaryDirectoryBrowse";
            btnTemporaryDirectoryBrowse.Size = new System.Drawing.Size(75, 23);
            btnTemporaryDirectoryBrowse.TabIndex = 8;
            btnTemporaryDirectoryBrowse.Text = "Browse";
            btnTemporaryDirectoryBrowse.UseVisualStyleBackColor = true;
            // 
            // tbOutputDirectory
            // 
            tbOutputDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbOutputDirectory.Location = new System.Drawing.Point(6, 22);
            tbOutputDirectory.Name = "tbOutputDirectory";
            tbOutputDirectory.Size = new System.Drawing.Size(755, 23);
            tbOutputDirectory.TabIndex = 7;
            tbOutputDirectory.Text = "G:\\Temp\\temp";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(6, 48);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(439, 15);
            label4.TabIndex = 0;
            label4.Text = "The work directory to save temp files. The original release directory is not touched.";
            // 
            // groupBox11
            // 
            groupBox11.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox11.Controls.Add(tbLog);
            groupBox11.Location = new System.Drawing.Point(12, 437);
            groupBox11.Name = "groupBox11";
            groupBox11.Size = new System.Drawing.Size(1577, 312);
            groupBox11.TabIndex = 3;
            groupBox11.TabStop = false;
            groupBox11.Text = "Log";
            // 
            // tbLog
            // 
            tbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            tbLog.Font = new System.Drawing.Font("Consolas", 9F);
            tbLog.Location = new System.Drawing.Point(3, 19);
            tbLog.Multiline = true;
            tbLog.Name = "tbLog";
            tbLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            tbLog.Size = new System.Drawing.Size(1571, 290);
            tbLog.TabIndex = 0;
            // 
            // gbInput
            // 
            gbInput.Controls.Add(groupBox12);
            gbInput.Controls.Add(groupBox1);
            gbInput.Controls.Add(groupBox2);
            gbInput.Controls.Add(groupBox3);
            gbInput.Location = new System.Drawing.Point(12, 27);
            gbInput.Name = "gbInput";
            gbInput.Size = new System.Drawing.Size(863, 352);
            gbInput.TabIndex = 7;
            gbInput.TabStop = false;
            gbInput.Text = "Input";
            // 
            // groupBox12
            // 
            groupBox12.Controls.Add(label19);
            groupBox12.Controls.Add(btnVerificationFileBrowse);
            groupBox12.Controls.Add(tbVerificationFilePath);
            groupBox12.Location = new System.Drawing.Point(6, 195);
            groupBox12.Name = "groupBox12";
            groupBox12.Size = new System.Drawing.Size(848, 71);
            groupBox12.TabIndex = 12;
            groupBox12.TabStop = false;
            groupBox12.Text = "Verification file";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new System.Drawing.Point(6, 48);
            label19.Name = "label19";
            label19.Size = new System.Drawing.Size(356, 15);
            label19.TabIndex = 9;
            label19.Text = "The file to check whether the rar file(s) have the correct checksum.";
            // 
            // btnVerificationFileBrowse
            // 
            btnVerificationFileBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnVerificationFileBrowse.Location = new System.Drawing.Point(767, 22);
            btnVerificationFileBrowse.Name = "btnVerificationFileBrowse";
            btnVerificationFileBrowse.Size = new System.Drawing.Size(75, 23);
            btnVerificationFileBrowse.TabIndex = 8;
            btnVerificationFileBrowse.Text = "Browse";
            btnVerificationFileBrowse.UseVisualStyleBackColor = true;
            // 
            // tbVerificationFilePath
            // 
            tbVerificationFilePath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbVerificationFilePath.Location = new System.Drawing.Point(6, 22);
            tbVerificationFilePath.Name = "tbVerificationFilePath";
            tbVerificationFilePath.Size = new System.Drawing.Size(755, 23);
            tbVerificationFilePath.TabIndex = 7;
            tbVerificationFilePath.Text = "D:\\ali.g-dmt.sfv";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiFile, tsmiView, tsmiSettings });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(1596, 24);
            menuStrip1.TabIndex = 8;
            menuStrip1.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiFileExit });
            tsmiFile.Name = "tsmiFile";
            tsmiFile.Size = new System.Drawing.Size(37, 20);
            tsmiFile.Text = "&File";
            // 
            // tsmiFileExit
            // 
            tsmiFileExit.Name = "tsmiFileExit";
            tsmiFileExit.Size = new System.Drawing.Size(93, 22);
            tsmiFileExit.Text = "E&xit";
            // 
            // tsmiView
            // 
            tsmiView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiViewCommandLines });
            tsmiView.Name = "tsmiView";
            tsmiView.Size = new System.Drawing.Size(44, 20);
            tsmiView.Text = "&View";
            // 
            // tsmiViewCommandLines
            // 
            tsmiViewCommandLines.Name = "tsmiViewCommandLines";
            tsmiViewCommandLines.Size = new System.Drawing.Size(161, 22);
            tsmiViewCommandLines.Text = "&Command Lines";
            // 
            // tsmiSettings
            // 
            tsmiSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiSettingsOptions });
            tsmiSettings.Name = "tsmiSettings";
            tsmiSettings.Size = new System.Drawing.Size(61, 20);
            tsmiSettings.Text = "&Settings";
            // 
            // tsmiSettingsOptions
            // 
            tsmiSettingsOptions.Name = "tsmiSettingsOptions";
            tsmiSettingsOptions.Size = new System.Drawing.Size(116, 22);
            tsmiSettingsOptions.Text = "&Options";
            // 
            // opStatus2
            // 
            opStatus2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            opStatus2.Location = new System.Drawing.Point(881, 232);
            opStatus2.Name = "opStatus2";
            opStatus2.Size = new System.Drawing.Size(708, 199);
            opStatus2.TabIndex = 18;
            opStatus2.Title = "Status 2";
            // 
            // opStatus1
            // 
            opStatus1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            opStatus1.Location = new System.Drawing.Point(881, 27);
            opStatus1.Name = "opStatus1";
            opStatus1.Size = new System.Drawing.Size(708, 199);
            opStatus1.TabIndex = 17;
            opStatus1.Title = "Status 1";
            // 
            // btnClearLog
            // 
            btnClearLog.Location = new System.Drawing.Point(12, 408);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new System.Drawing.Size(75, 23);
            btnClearLog.TabIndex = 19;
            btnClearLog.Text = "Clear log";
            btnClearLog.UseVisualStyleBackColor = true;
            // 
            // cbAutoScroll
            // 
            cbAutoScroll.AutoSize = true;
            cbAutoScroll.Checked = true;
            cbAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            cbAutoScroll.Location = new System.Drawing.Point(93, 410);
            cbAutoScroll.Name = "cbAutoScroll";
            cbAutoScroll.Size = new System.Drawing.Size(83, 19);
            cbAutoScroll.TabIndex = 20;
            cbAutoScroll.Text = "Auto scroll";
            cbAutoScroll.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1596, 761);
            Controls.Add(cbAutoScroll);
            Controls.Add(btnClearLog);
            Controls.Add(opStatus2);
            Controls.Add(opStatus1);
            Controls.Add(groupBox11);
            Controls.Add(gbInput);
            Controls.Add(menuStrip1);
            Controls.Add(btnStart);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "WinRARRed";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox11.ResumeLayout(false);
            groupBox11.PerformLayout();
            gbInput.ResumeLayout(false);
            groupBox12.ResumeLayout(false);
            groupBox12.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox tbWinRARDirectory;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnWinRARDirectoryBrowse;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnReleaseDirectoryBrowse;
        private System.Windows.Forms.TextBox tbReleaseDirectory;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnTemporaryDirectoryBrowse;
        private System.Windows.Forms.TextBox tbOutputDirectory;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox11;
        private System.Windows.Forms.TextBox tbLog;
        private System.Windows.Forms.GroupBox gbInput;
        private System.Windows.Forms.GroupBox groupBox12;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button btnVerificationFileBrowse;
        private System.Windows.Forms.TextBox tbVerificationFilePath;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiSettings;
        private System.Windows.Forms.ToolStripMenuItem tsmiSettingsOptions;
        private Controls.OperationProgressStatusUserControl opStatus2;
        private Controls.OperationProgressStatusUserControl opStatus1;
        private System.Windows.Forms.ToolStripMenuItem tsmiView;
        private System.Windows.Forms.ToolStripMenuItem tsmiViewCommandLines;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.CheckBox cbAutoScroll;
    }
}

