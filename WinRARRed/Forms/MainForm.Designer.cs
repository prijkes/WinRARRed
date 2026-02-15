namespace WinRARRed.Forms;

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
        menuStrip1 = new System.Windows.Forms.MenuStrip();
        tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
        tsmiFileExit = new System.Windows.Forms.ToolStripMenuItem();
        tsmiTools = new System.Windows.Forms.ToolStripMenuItem();
        tsmiToolsFileInspector = new System.Windows.Forms.ToolStripMenuItem();
        tsmiToolsFileCompare = new System.Windows.Forms.ToolStripMenuItem();
        tsmiSettings = new System.Windows.Forms.ToolStripMenuItem();
        tsmiSettingsOptions = new System.Windows.Forms.ToolStripMenuItem();
        statusStrip = new System.Windows.Forms.StatusStrip();
        tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
        tsslProgress = new System.Windows.Forms.ToolStripStatusLabel();
        tsslElapsed = new System.Windows.Forms.ToolStripStatusLabel();
        opStatus2 = new WinRARRed.Controls.OperationProgressStatusUserControl();
        gbInput = new System.Windows.Forms.GroupBox();
        groupBox12 = new System.Windows.Forms.GroupBox();
        label19 = new System.Windows.Forms.Label();
        btnVerificationFileBrowse = new System.Windows.Forms.Button();
        tbVerificationFilePath = new System.Windows.Forms.TextBox();
        groupBox1 = new System.Windows.Forms.GroupBox();
        linkLabel2 = new System.Windows.Forms.LinkLabel();
        btnWinRARDirectoryBrowse = new System.Windows.Forms.Button();
        label2 = new System.Windows.Forms.Label();
        linkLabel1 = new System.Windows.Forms.LinkLabel();
        label1 = new System.Windows.Forms.Label();
        tbWinRARDirectory = new System.Windows.Forms.TextBox();
        groupBox2 = new System.Windows.Forms.GroupBox();
        label3 = new System.Windows.Forms.Label();
        btnReleaseDirectoryBrowse = new System.Windows.Forms.Button();
        tbReleaseDirectory = new System.Windows.Forms.TextBox();
        groupBox3 = new System.Windows.Forms.GroupBox();
        lblOutputWarning = new System.Windows.Forms.Label();
        btnTemporaryDirectoryBrowse = new System.Windows.Forms.Button();
        tbOutputDirectory = new System.Windows.Forms.TextBox();
        label4 = new System.Windows.Forms.Label();
        btnStart = new System.Windows.Forms.Button();
        groupBox11 = new System.Windows.Forms.GroupBox();
        cbAutoScroll = new System.Windows.Forms.CheckBox();
        tabControlLogs = new System.Windows.Forms.TabControl();
        tabPageSystem = new System.Windows.Forms.TabPage();
        rtbLogSystem = new System.Windows.Forms.RichTextBox();
        tabPagePhase1 = new System.Windows.Forms.TabPage();
        rtbLogPhase1 = new System.Windows.Forms.RichTextBox();
        tabPagePhase2 = new System.Windows.Forms.TabPage();
        rtbLogPhase2 = new System.Windows.Forms.RichTextBox();
        btnClearLog = new System.Windows.Forms.Button();
        opStatus1 = new WinRARRed.Controls.OperationProgressStatusUserControl();
        menuStrip1.SuspendLayout();
        statusStrip.SuspendLayout();
        gbInput.SuspendLayout();
        groupBox12.SuspendLayout();
        groupBox1.SuspendLayout();
        groupBox2.SuspendLayout();
        groupBox3.SuspendLayout();
        groupBox11.SuspendLayout();
        tabControlLogs.SuspendLayout();
        tabPageSystem.SuspendLayout();
        tabPagePhase1.SuspendLayout();
        tabPagePhase2.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip1
        // 
        menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiFile, tsmiTools, tsmiSettings });
        menuStrip1.Location = new System.Drawing.Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Size = new System.Drawing.Size(1488, 24);
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
        // tsmiTools
        // 
        tsmiTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { tsmiToolsFileInspector, tsmiToolsFileCompare });
        tsmiTools.Name = "tsmiTools";
        tsmiTools.Size = new System.Drawing.Size(46, 20);
        tsmiTools.Text = "&Tools";
        // 
        // tsmiToolsFileInspector
        // 
        tsmiToolsFileInspector.Name = "tsmiToolsFileInspector";
        tsmiToolsFileInspector.ShortcutKeys = System.Windows.Forms.Keys.F12;
        tsmiToolsFileInspector.Size = new System.Drawing.Size(195, 22);
        tsmiToolsFileInspector.Text = "File &Inspector...";
        tsmiToolsFileInspector.Click += tsmiToolsFileInspector_Click;
        // 
        // tsmiToolsFileCompare
        // 
        tsmiToolsFileCompare.Name = "tsmiToolsFileCompare";
        tsmiToolsFileCompare.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D;
        tsmiToolsFileCompare.Size = new System.Drawing.Size(195, 22);
        tsmiToolsFileCompare.Text = "File &Compare...";
        tsmiToolsFileCompare.Click += tsmiToolsFileCompare_Click;
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
        // statusStrip
        // 
        statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsslStatus, tsslProgress, tsslElapsed });
        statusStrip.Location = new System.Drawing.Point(0, 784);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new System.Drawing.Size(1488, 22);
        statusStrip.TabIndex = 22;
        // 
        // tsslStatus
        // 
        tsslStatus.Name = "tsslStatus";
        tsslStatus.Size = new System.Drawing.Size(39, 17);
        tsslStatus.Text = "Ready";
        // 
        // tsslProgress
        // 
        tsslProgress.Name = "tsslProgress";
        tsslProgress.Size = new System.Drawing.Size(1385, 17);
        tsslProgress.Spring = true;
        tsslProgress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // tsslElapsed
        // 
        tsslElapsed.Name = "tsslElapsed";
        tsslElapsed.Size = new System.Drawing.Size(49, 17);
        tsslElapsed.Text = "00:00:00";
        // 
        // opStatus2
        // 
        opStatus2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        opStatus2.Location = new System.Drawing.Point(765, 233);
        opStatus2.Name = "opStatus2";
        opStatus2.Size = new System.Drawing.Size(707, 200);
        opStatus2.TabIndex = 26;
        opStatus2.Title = "Phase 2 Progress";
        // 
        // gbInput
        // 
        gbInput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        gbInput.Controls.Add(groupBox12);
        gbInput.Controls.Add(groupBox1);
        gbInput.Controls.Add(groupBox2);
        gbInput.Controls.Add(groupBox3);
        gbInput.Controls.Add(btnStart);
        gbInput.Location = new System.Drawing.Point(0, 27);
        gbInput.Name = "gbInput";
        gbInput.Size = new System.Drawing.Size(759, 421);
        gbInput.TabIndex = 24;
        gbInput.TabStop = false;
        gbInput.Text = "Input";
        // 
        // groupBox12
        // 
        groupBox12.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        groupBox12.Controls.Add(label19);
        groupBox12.Controls.Add(btnVerificationFileBrowse);
        groupBox12.Controls.Add(tbVerificationFilePath);
        groupBox12.Location = new System.Drawing.Point(6, 179);
        groupBox12.Name = "groupBox12";
        groupBox12.Size = new System.Drawing.Size(747, 70);
        groupBox12.TabIndex = 12;
        groupBox12.TabStop = false;
        groupBox12.Text = "3. Verification File (SFV/SHA1)";
        // 
        // label19
        // 
        label19.AutoSize = true;
        label19.Location = new System.Drawing.Point(6, 44);
        label19.Name = "label19";
        label19.Size = new System.Drawing.Size(231, 15);
        label19.TabIndex = 9;
        label19.Text = "SFV or SHA1 file for checksum verification.";
        // 
        // btnVerificationFileBrowse
        // 
        btnVerificationFileBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnVerificationFileBrowse.Location = new System.Drawing.Point(666, 17);
        btnVerificationFileBrowse.Name = "btnVerificationFileBrowse";
        btnVerificationFileBrowse.Size = new System.Drawing.Size(75, 23);
        btnVerificationFileBrowse.TabIndex = 8;
        btnVerificationFileBrowse.Text = "Browse";
        btnVerificationFileBrowse.UseVisualStyleBackColor = true;
        // 
        // tbVerificationFilePath
        // 
        tbVerificationFilePath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        tbVerificationFilePath.Location = new System.Drawing.Point(6, 18);
        tbVerificationFilePath.Name = "tbVerificationFilePath";
        tbVerificationFilePath.Size = new System.Drawing.Size(654, 23);
        tbVerificationFilePath.TabIndex = 7;
        tbVerificationFilePath.Text = "D:\\ali.g-dmt.sfv";
        // 
        // groupBox1
        // 
        groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        groupBox1.Controls.Add(linkLabel2);
        groupBox1.Controls.Add(btnWinRARDirectoryBrowse);
        groupBox1.Controls.Add(label2);
        groupBox1.Controls.Add(linkLabel1);
        groupBox1.Controls.Add(label1);
        groupBox1.Controls.Add(tbWinRARDirectory);
        groupBox1.Location = new System.Drawing.Point(6, 22);
        groupBox1.Name = "groupBox1";
        groupBox1.Size = new System.Drawing.Size(747, 74);
        groupBox1.TabIndex = 1;
        groupBox1.TabStop = false;
        groupBox1.Text = "1. WinRAR Installations Directory";
        // 
        // linkLabel2
        // 
        linkLabel2.AutoSize = true;
        linkLabel2.Location = new System.Drawing.Point(403, 48);
        linkLabel2.Name = "linkLabel2";
        linkLabel2.Size = new System.Drawing.Size(147, 15);
        linkLabel2.TabIndex = 5;
        linkLabel2.TabStop = true;
        linkLabel2.Text = "(original files from rar FTP)";
        linkLabel2.LinkClicked += LinkLabel2_LinkClicked;
        // 
        // btnWinRARDirectoryBrowse
        // 
        btnWinRARDirectoryBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnWinRARDirectoryBrowse.Location = new System.Drawing.Point(666, 22);
        btnWinRARDirectoryBrowse.Name = "btnWinRARDirectoryBrowse";
        btnWinRARDirectoryBrowse.Size = new System.Drawing.Size(75, 23);
        btnWinRARDirectoryBrowse.TabIndex = 4;
        btnWinRARDirectoryBrowse.Text = "Browse";
        btnWinRARDirectoryBrowse.UseVisualStyleBackColor = true;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new System.Drawing.Point(277, 48);
        label2.Name = "label2";
        label2.Size = new System.Drawing.Size(28, 15);
        label2.TabIndex = 3;
        label2.Text = "Get:";
        // 
        // linkLabel1
        // 
        linkLabel1.AutoSize = true;
        linkLabel1.Location = new System.Drawing.Point(311, 48);
        linkLabel1.Name = "linkLabel1";
        linkLabel1.Size = new System.Drawing.Size(80, 15);
        linkLabel1.TabIndex = 2;
        linkLabel1.TabStop = true;
        linkLabel1.Text = "extracted files";
        linkLabel1.LinkClicked += LinkLabel1_LinkClicked;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new System.Drawing.Point(6, 48);
        label1.Name = "label1";
        label1.Size = new System.Drawing.Size(248, 15);
        label1.TabIndex = 1;
        label1.Text = "Directory with extracted WinRAR installations.";
        // 
        // tbWinRARDirectory
        // 
        tbWinRARDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        tbWinRARDirectory.Location = new System.Drawing.Point(6, 22);
        tbWinRARDirectory.Name = "tbWinRARDirectory";
        tbWinRARDirectory.Size = new System.Drawing.Size(654, 23);
        tbWinRARDirectory.TabIndex = 0;
        tbWinRARDirectory.Text = "G:\\WinRAR\\extracted";
        // 
        // groupBox2
        // 
        groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        groupBox2.Controls.Add(label3);
        groupBox2.Controls.Add(btnReleaseDirectoryBrowse);
        groupBox2.Controls.Add(tbReleaseDirectory);
        groupBox2.Location = new System.Drawing.Point(6, 102);
        groupBox2.Name = "groupBox2";
        groupBox2.Size = new System.Drawing.Size(747, 71);
        groupBox2.TabIndex = 2;
        groupBox2.TabStop = false;
        groupBox2.Text = "2. Release Directory (files to RAR)";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new System.Drawing.Point(6, 44);
        label3.Name = "label3";
        label3.Size = new System.Drawing.Size(253, 15);
        label3.TabIndex = 3;
        label3.Text = "Directory containing release files to be RARred.";
        // 
        // btnReleaseDirectoryBrowse
        // 
        btnReleaseDirectoryBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnReleaseDirectoryBrowse.Location = new System.Drawing.Point(666, 18);
        btnReleaseDirectoryBrowse.Name = "btnReleaseDirectoryBrowse";
        btnReleaseDirectoryBrowse.Size = new System.Drawing.Size(75, 23);
        btnReleaseDirectoryBrowse.TabIndex = 6;
        btnReleaseDirectoryBrowse.Text = "Browse";
        btnReleaseDirectoryBrowse.UseVisualStyleBackColor = true;
        // 
        // tbReleaseDirectory
        // 
        tbReleaseDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        tbReleaseDirectory.Location = new System.Drawing.Point(6, 18);
        tbReleaseDirectory.Name = "tbReleaseDirectory";
        tbReleaseDirectory.Size = new System.Drawing.Size(654, 23);
        tbReleaseDirectory.TabIndex = 5;
        tbReleaseDirectory.Text = "G:\\WinRAR\\release";
        // 
        // groupBox3
        // 
        groupBox3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        groupBox3.Controls.Add(lblOutputWarning);
        groupBox3.Controls.Add(btnTemporaryDirectoryBrowse);
        groupBox3.Controls.Add(tbOutputDirectory);
        groupBox3.Controls.Add(label4);
        groupBox3.Location = new System.Drawing.Point(6, 255);
        groupBox3.Name = "groupBox3";
        groupBox3.Size = new System.Drawing.Size(747, 87);
        groupBox3.TabIndex = 4;
        groupBox3.TabStop = false;
        groupBox3.Text = "4. Output Directory";
        // 
        // lblOutputWarning
        // 
        lblOutputWarning.AutoSize = true;
        lblOutputWarning.ForeColor = System.Drawing.Color.FromArgb(200, 40, 40);
        lblOutputWarning.Location = new System.Drawing.Point(6, 59);
        lblOutputWarning.Name = "lblOutputWarning";
        lblOutputWarning.Size = new System.Drawing.Size(394, 15);
        lblOutputWarning.TabIndex = 9;
        lblOutputWarning.Text = "WARNING: All existing data in this directory will be deleted when starting!";
        // 
        // btnTemporaryDirectoryBrowse
        // 
        btnTemporaryDirectoryBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnTemporaryDirectoryBrowse.Location = new System.Drawing.Point(666, 17);
        btnTemporaryDirectoryBrowse.Name = "btnTemporaryDirectoryBrowse";
        btnTemporaryDirectoryBrowse.Size = new System.Drawing.Size(75, 23);
        btnTemporaryDirectoryBrowse.TabIndex = 8;
        btnTemporaryDirectoryBrowse.Text = "Browse";
        btnTemporaryDirectoryBrowse.UseVisualStyleBackColor = true;
        // 
        // tbOutputDirectory
        // 
        tbOutputDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        tbOutputDirectory.Location = new System.Drawing.Point(6, 18);
        tbOutputDirectory.Name = "tbOutputDirectory";
        tbOutputDirectory.Size = new System.Drawing.Size(654, 23);
        tbOutputDirectory.TabIndex = 7;
        tbOutputDirectory.Text = "G:\\Temp";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new System.Drawing.Point(6, 44);
        label4.Name = "label4";
        label4.Size = new System.Drawing.Size(281, 15);
        label4.TabIndex = 0;
        label4.Text = "Working directory for temp files and matched RARs.";
        // 
        // btnStart
        // 
        btnStart.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnStart.Location = new System.Drawing.Point(667, 348);
        btnStart.Name = "btnStart";
        btnStart.Size = new System.Drawing.Size(86, 28);
        btnStart.TabIndex = 3;
        btnStart.Text = "Start";
        btnStart.UseVisualStyleBackColor = true;
        // 
        // groupBox11
        // 
        groupBox11.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        groupBox11.Controls.Add(cbAutoScroll);
        groupBox11.Controls.Add(tabControlLogs);
        groupBox11.Controls.Add(btnClearLog);
        groupBox11.Location = new System.Drawing.Point(6, 451);
        groupBox11.Margin = new System.Windows.Forms.Padding(0);
        groupBox11.Name = "groupBox11";
        groupBox11.Size = new System.Drawing.Size(1473, 333);
        groupBox11.TabIndex = 23;
        groupBox11.TabStop = false;
        groupBox11.Text = "Log";
        // 
        // cbAutoScroll
        // 
        cbAutoScroll.AutoSize = true;
        cbAutoScroll.Checked = true;
        cbAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
        cbAutoScroll.Location = new System.Drawing.Point(6, 19);
        cbAutoScroll.Name = "cbAutoScroll";
        cbAutoScroll.Size = new System.Drawing.Size(83, 19);
        cbAutoScroll.TabIndex = 27;
        cbAutoScroll.Text = "Auto scroll";
        cbAutoScroll.UseVisualStyleBackColor = true;
        // 
        // tabControlLogs
        // 
        tabControlLogs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        tabControlLogs.Controls.Add(tabPageSystem);
        tabControlLogs.Controls.Add(tabPagePhase1);
        tabControlLogs.Controls.Add(tabPagePhase2);
        tabControlLogs.Location = new System.Drawing.Point(6, 44);
        tabControlLogs.Name = "tabControlLogs";
        tabControlLogs.SelectedIndex = 0;
        tabControlLogs.Size = new System.Drawing.Size(1461, 283);
        tabControlLogs.TabIndex = 0;
        // 
        // tabPageSystem
        // 
        tabPageSystem.Controls.Add(rtbLogSystem);
        tabPageSystem.Location = new System.Drawing.Point(4, 24);
        tabPageSystem.Name = "tabPageSystem";
        tabPageSystem.Padding = new System.Windows.Forms.Padding(3);
        tabPageSystem.Size = new System.Drawing.Size(1453, 255);
        tabPageSystem.TabIndex = 0;
        tabPageSystem.Text = "System";
        tabPageSystem.UseVisualStyleBackColor = true;
        // 
        // rtbLogSystem
        // 
        rtbLogSystem.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        rtbLogSystem.Dock = System.Windows.Forms.DockStyle.Fill;
        rtbLogSystem.Font = new System.Drawing.Font("Consolas", 9F);
        rtbLogSystem.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
        rtbLogSystem.Location = new System.Drawing.Point(3, 3);
        rtbLogSystem.Name = "rtbLogSystem";
        rtbLogSystem.ReadOnly = true;
        rtbLogSystem.Size = new System.Drawing.Size(1447, 249);
        rtbLogSystem.TabIndex = 0;
        rtbLogSystem.Text = "";
        rtbLogSystem.WordWrap = false;
        // 
        // tabPagePhase1
        // 
        tabPagePhase1.Controls.Add(rtbLogPhase1);
        tabPagePhase1.Location = new System.Drawing.Point(4, 24);
        tabPagePhase1.Name = "tabPagePhase1";
        tabPagePhase1.Padding = new System.Windows.Forms.Padding(3);
        tabPagePhase1.Size = new System.Drawing.Size(1453, 270);
        tabPagePhase1.TabIndex = 1;
        tabPagePhase1.Text = "Phase 1 (Comment)";
        tabPagePhase1.UseVisualStyleBackColor = true;
        // 
        // rtbLogPhase1
        // 
        rtbLogPhase1.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        rtbLogPhase1.Dock = System.Windows.Forms.DockStyle.Fill;
        rtbLogPhase1.Font = new System.Drawing.Font("Consolas", 9F);
        rtbLogPhase1.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
        rtbLogPhase1.Location = new System.Drawing.Point(3, 3);
        rtbLogPhase1.Name = "rtbLogPhase1";
        rtbLogPhase1.ReadOnly = true;
        rtbLogPhase1.Size = new System.Drawing.Size(1447, 264);
        rtbLogPhase1.TabIndex = 0;
        rtbLogPhase1.Text = "";
        rtbLogPhase1.WordWrap = false;
        // 
        // tabPagePhase2
        // 
        tabPagePhase2.Controls.Add(rtbLogPhase2);
        tabPagePhase2.Location = new System.Drawing.Point(4, 24);
        tabPagePhase2.Name = "tabPagePhase2";
        tabPagePhase2.Padding = new System.Windows.Forms.Padding(3);
        tabPagePhase2.Size = new System.Drawing.Size(1453, 270);
        tabPagePhase2.TabIndex = 2;
        tabPagePhase2.Text = "Phase 2 (Full RAR)";
        tabPagePhase2.UseVisualStyleBackColor = true;
        // 
        // rtbLogPhase2
        // 
        rtbLogPhase2.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        rtbLogPhase2.Dock = System.Windows.Forms.DockStyle.Fill;
        rtbLogPhase2.Font = new System.Drawing.Font("Consolas", 9F);
        rtbLogPhase2.ForeColor = System.Drawing.Color.FromArgb(212, 212, 212);
        rtbLogPhase2.Location = new System.Drawing.Point(3, 3);
        rtbLogPhase2.Name = "rtbLogPhase2";
        rtbLogPhase2.ReadOnly = true;
        rtbLogPhase2.Size = new System.Drawing.Size(1447, 264);
        rtbLogPhase2.TabIndex = 0;
        rtbLogPhase2.Text = "";
        rtbLogPhase2.WordWrap = false;
        // 
        // btnClearLog
        // 
        btnClearLog.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        btnClearLog.Location = new System.Drawing.Point(6, 610);
        btnClearLog.Name = "btnClearLog";
        btnClearLog.Size = new System.Drawing.Size(75, 23);
        btnClearLog.TabIndex = 19;
        btnClearLog.Text = "Clear log";
        btnClearLog.UseVisualStyleBackColor = true;
        // 
        // opStatus1
        // 
        opStatus1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        opStatus1.Location = new System.Drawing.Point(765, 27);
        opStatus1.Name = "opStatus1";
        opStatus1.Size = new System.Drawing.Size(707, 200);
        opStatus1.TabIndex = 27;
        opStatus1.Title = "Phase 2 Progress";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1488, 806);
        Controls.Add(opStatus1);
        Controls.Add(opStatus2);
        Controls.Add(gbInput);
        Controls.Add(groupBox11);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip1);
        MainMenuStrip = menuStrip1;
        Name = "MainForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "WinRARRed";
        WindowState = System.Windows.Forms.FormWindowState.Maximized;
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        gbInput.ResumeLayout(false);
        groupBox12.ResumeLayout(false);
        groupBox12.PerformLayout();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        groupBox2.ResumeLayout(false);
        groupBox2.PerformLayout();
        groupBox3.ResumeLayout(false);
        groupBox3.PerformLayout();
        groupBox11.ResumeLayout(false);
        groupBox11.PerformLayout();
        tabControlLogs.ResumeLayout(false);
        tabPageSystem.ResumeLayout(false);
        tabPagePhase1.ResumeLayout(false);
        tabPagePhase2.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
    private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiFileExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiSettings;
        private System.Windows.Forms.ToolStripMenuItem tsmiSettingsOptions;
        private System.Windows.Forms.ToolStripMenuItem tsmiTools;
        private System.Windows.Forms.ToolStripMenuItem tsmiToolsFileInspector;
        private System.Windows.Forms.ToolStripMenuItem tsmiToolsFileCompare;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
        private System.Windows.Forms.ToolStripStatusLabel tsslProgress;
        private System.Windows.Forms.ToolStripStatusLabel tsslElapsed;
    private Controls.OperationProgressStatusUserControl opStatus2;
    private System.Windows.Forms.GroupBox gbInput;
    private System.Windows.Forms.GroupBox groupBox12;
    private System.Windows.Forms.Label label19;
    private System.Windows.Forms.Button btnVerificationFileBrowse;
    private System.Windows.Forms.TextBox tbVerificationFilePath;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Button btnWinRARDirectoryBrowse;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.LinkLabel linkLabel1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox tbWinRARDirectory;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button btnReleaseDirectoryBrowse;
    private System.Windows.Forms.TextBox tbReleaseDirectory;
    private System.Windows.Forms.GroupBox groupBox3;
    private System.Windows.Forms.Button btnTemporaryDirectoryBrowse;
    private System.Windows.Forms.TextBox tbOutputDirectory;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label lblOutputWarning;
    private System.Windows.Forms.Button btnStart;
    private System.Windows.Forms.GroupBox groupBox11;
    private System.Windows.Forms.TabControl tabControlLogs;
    private System.Windows.Forms.TabPage tabPageSystem;
    private System.Windows.Forms.RichTextBox rtbLogSystem;
    private System.Windows.Forms.TabPage tabPagePhase1;
    private System.Windows.Forms.RichTextBox rtbLogPhase1;
    private System.Windows.Forms.TabPage tabPagePhase2;
    private System.Windows.Forms.RichTextBox rtbLogPhase2;
    private System.Windows.Forms.Button btnClearLog;
    private System.Windows.Forms.CheckBox cbAutoScroll;
    private Controls.OperationProgressStatusUserControl opStatus1;
    private System.Windows.Forms.LinkLabel linkLabel2;
}

