using System.Drawing;
using System.Windows.Forms;
using WinRARRed.Controls;

namespace WinRARRed.Forms;

partial class FileCompareForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        menuStrip = new MenuStrip();
        fileToolStripMenuItem = new ToolStripMenuItem();
        openLeftToolStripMenuItem = new ToolStripMenuItem();
        openRightToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        swapToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator2 = new ToolStripSeparator();
        exitToolStripMenuItem = new ToolStripMenuItem();
        viewToolStripMenuItem = new ToolStripMenuItem();
        refreshToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator3 = new ToolStripSeparator();
        showHexViewToolStripMenuItem = new ToolStripMenuItem();
        showAllHexDataToolStripMenuItem = new ToolStripMenuItem();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        topPanel = new Panel();
        btnBrowseRight = new Button();
        txtRightFile = new TextBox();
        lblRightFile = new Label();
        btnBrowseLeft = new Button();
        txtLeftFile = new TextBox();
        lblLeftFile = new Label();
        diffSummaryPanel = new Panel();
        lblDiffSummary = new Label();
        splitContainerVertical = new SplitContainer();
        splitContainerMain = new SplitContainer();
        splitContainerLeft = new SplitContainer();
        treeViewLeft = new TreeView();
        listViewLeft = new ListView();
        columnLeftProperty = new ColumnHeader();
        columnLeftValue = new ColumnHeader();
        splitContainerRight = new SplitContainer();
        treeViewRight = new TreeView();
        listViewRight = new ListView();
        columnRightProperty = new ColumnHeader();
        columnRightValue = new ColumnHeader();
        splitContainerHex = new SplitContainer();
        groupBoxHexLeft = new GroupBox();
        hexViewLeft = new HexViewControl();
        groupBoxHexRight = new GroupBox();
        hexViewRight = new HexViewControl();
        menuStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        topPanel.SuspendLayout();
        diffSummaryPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerVertical).BeginInit();
        splitContainerVertical.Panel1.SuspendLayout();
        splitContainerVertical.Panel2.SuspendLayout();
        splitContainerVertical.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
        splitContainerMain.Panel1.SuspendLayout();
        splitContainerMain.Panel2.SuspendLayout();
        splitContainerMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerLeft).BeginInit();
        splitContainerLeft.Panel1.SuspendLayout();
        splitContainerLeft.Panel2.SuspendLayout();
        splitContainerLeft.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerRight).BeginInit();
        splitContainerRight.Panel1.SuspendLayout();
        splitContainerRight.Panel2.SuspendLayout();
        splitContainerRight.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerHex).BeginInit();
        splitContainerHex.Panel1.SuspendLayout();
        splitContainerHex.Panel2.SuspendLayout();
        splitContainerHex.SuspendLayout();
        groupBoxHexLeft.SuspendLayout();
        groupBoxHexRight.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip
        // 
        menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(1400, 24);
        menuStrip.TabIndex = 0;
        // 
        // fileToolStripMenuItem
        // 
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openLeftToolStripMenuItem, openRightToolStripMenuItem, toolStripSeparator1, swapToolStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new Size(37, 20);
        fileToolStripMenuItem.Text = "&File";
        // 
        // openLeftToolStripMenuItem
        // 
        openLeftToolStripMenuItem.Name = "openLeftToolStripMenuItem";
        openLeftToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.L;
        openLeftToolStripMenuItem.Size = new Size(184, 22);
        openLeftToolStripMenuItem.Text = "Open &Left...";
        openLeftToolStripMenuItem.Click += OpenLeftToolStripMenuItem_Click;
        // 
        // openRightToolStripMenuItem
        // 
        openRightToolStripMenuItem.Name = "openRightToolStripMenuItem";
        openRightToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.R;
        openRightToolStripMenuItem.Size = new Size(184, 22);
        openRightToolStripMenuItem.Text = "Open &Right...";
        openRightToolStripMenuItem.Click += OpenRightToolStripMenuItem_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(181, 6);
        // 
        // swapToolStripMenuItem
        // 
        swapToolStripMenuItem.Name = "swapToolStripMenuItem";
        swapToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.W;
        swapToolStripMenuItem.Size = new Size(184, 22);
        swapToolStripMenuItem.Text = "S&wap Files";
        swapToolStripMenuItem.Click += SwapToolStripMenuItem_Click;
        // 
        // toolStripSeparator2
        // 
        toolStripSeparator2.Name = "toolStripSeparator2";
        toolStripSeparator2.Size = new Size(181, 6);
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        exitToolStripMenuItem.Size = new Size(184, 22);
        exitToolStripMenuItem.Text = "E&xit";
        exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
        // 
        // viewToolStripMenuItem
        // 
        viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { refreshToolStripMenuItem, toolStripSeparator3, showHexViewToolStripMenuItem, showAllHexDataToolStripMenuItem });
        viewToolStripMenuItem.Name = "viewToolStripMenuItem";
        viewToolStripMenuItem.Size = new Size(44, 20);
        viewToolStripMenuItem.Text = "&View";
        // 
        // refreshToolStripMenuItem
        // 
        refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
        refreshToolStripMenuItem.ShortcutKeys = Keys.F5;
        refreshToolStripMenuItem.Size = new Size(213, 22);
        refreshToolStripMenuItem.Text = "&Refresh";
        refreshToolStripMenuItem.Click += RefreshToolStripMenuItem_Click;
        // 
        // toolStripSeparator3
        // 
        toolStripSeparator3.Name = "toolStripSeparator3";
        toolStripSeparator3.Size = new Size(210, 6);
        // 
        // showHexViewToolStripMenuItem
        // 
        showHexViewToolStripMenuItem.Checked = true;
        showHexViewToolStripMenuItem.CheckOnClick = true;
        showHexViewToolStripMenuItem.CheckState = CheckState.Checked;
        showHexViewToolStripMenuItem.Name = "showHexViewToolStripMenuItem";
        showHexViewToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.H;
        showHexViewToolStripMenuItem.Size = new Size(213, 22);
        showHexViewToolStripMenuItem.Text = "Show &Hex View";
        showHexViewToolStripMenuItem.Click += ShowHexViewToolStripMenuItem_Click;
        // 
        // showAllHexDataToolStripMenuItem
        // 
        showAllHexDataToolStripMenuItem.CheckOnClick = true;
        showAllHexDataToolStripMenuItem.Name = "showAllHexDataToolStripMenuItem";
        showAllHexDataToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.A;
        showAllHexDataToolStripMenuItem.Size = new Size(213, 22);
        showAllHexDataToolStripMenuItem.Text = "Show &All Hex Data";
        showAllHexDataToolStripMenuItem.ToolTipText = "Show all hex data without 64KB limit (may be slow for large files)";
        showAllHexDataToolStripMenuItem.Click += ShowAllHexDataToolStripMenuItem_Click;
        // 
        // statusStrip
        // 
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Location = new Point(0, 828);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new Size(1400, 22);
        statusStrip.TabIndex = 1;
        // 
        // statusLabel
        // 
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(1385, 17);
        statusLabel.Spring = true;
        statusLabel.Text = "Ready. Drag and drop files or use File menu to open.";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // topPanel
        // 
        topPanel.Controls.Add(btnBrowseRight);
        topPanel.Controls.Add(txtRightFile);
        topPanel.Controls.Add(lblRightFile);
        topPanel.Controls.Add(btnBrowseLeft);
        topPanel.Controls.Add(txtLeftFile);
        topPanel.Controls.Add(lblLeftFile);
        topPanel.Dock = DockStyle.Top;
        topPanel.Location = new Point(0, 24);
        topPanel.Name = "topPanel";
        topPanel.Padding = new Padding(6);
        topPanel.Size = new Size(1400, 36);
        topPanel.TabIndex = 2;
        // 
        // btnBrowseRight
        // 
        btnBrowseRight.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseRight.Location = new Point(1300, 6);
        btnBrowseRight.Name = "btnBrowseRight";
        btnBrowseRight.Size = new Size(75, 25);
        btnBrowseRight.TabIndex = 5;
        btnBrowseRight.Text = "Browse...";
        btnBrowseRight.UseVisualStyleBackColor = true;
        btnBrowseRight.Click += BtnBrowseRight_Click;
        // 
        // txtRightFile
        // 
        txtRightFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        txtRightFile.Location = new Point(744, 7);
        txtRightFile.Name = "txtRightFile";
        txtRightFile.ReadOnly = true;
        txtRightFile.Size = new Size(550, 23);
        txtRightFile.TabIndex = 4;
        // 
        // lblRightFile
        // 
        lblRightFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblRightFile.AutoSize = true;
        lblRightFile.Location = new Point(700, 11);
        lblRightFile.Name = "lblRightFile";
        lblRightFile.Size = new Size(38, 15);
        lblRightFile.TabIndex = 3;
        lblRightFile.Text = "Right:";
        // 
        // btnBrowseLeft
        // 
        btnBrowseLeft.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseLeft.Location = new Point(599, 6);
        btnBrowseLeft.Name = "btnBrowseLeft";
        btnBrowseLeft.Size = new Size(75, 25);
        btnBrowseLeft.TabIndex = 2;
        btnBrowseLeft.Text = "Browse...";
        btnBrowseLeft.UseVisualStyleBackColor = true;
        btnBrowseLeft.Click += BtnBrowseLeft_Click;
        // 
        // txtLeftFile
        // 
        txtLeftFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtLeftFile.Location = new Point(43, 7);
        txtLeftFile.Name = "txtLeftFile";
        txtLeftFile.ReadOnly = true;
        txtLeftFile.Size = new Size(550, 23);
        txtLeftFile.TabIndex = 1;
        // 
        // lblLeftFile
        // 
        lblLeftFile.AutoSize = true;
        lblLeftFile.Location = new Point(9, 11);
        lblLeftFile.Name = "lblLeftFile";
        lblLeftFile.Size = new Size(30, 15);
        lblLeftFile.TabIndex = 0;
        lblLeftFile.Text = "Left:";
        // 
        // diffSummaryPanel
        // 
        diffSummaryPanel.Controls.Add(lblDiffSummary);
        diffSummaryPanel.Dock = DockStyle.Top;
        diffSummaryPanel.Location = new Point(0, 60);
        diffSummaryPanel.Name = "diffSummaryPanel";
        diffSummaryPanel.Padding = new Padding(8, 4, 8, 4);
        diffSummaryPanel.Size = new Size(1400, 26);
        diffSummaryPanel.TabIndex = 4;
        diffSummaryPanel.Visible = false;
        // 
        // lblDiffSummary
        // 
        lblDiffSummary.AutoSize = true;
        lblDiffSummary.Dock = DockStyle.Fill;
        lblDiffSummary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblDiffSummary.Location = new Point(8, 4);
        lblDiffSummary.Name = "lblDiffSummary";
        lblDiffSummary.Size = new Size(0, 15);
        lblDiffSummary.TabIndex = 0;
        // 
        // splitContainerVertical
        // 
        splitContainerVertical.Dock = DockStyle.Fill;
        splitContainerVertical.Location = new Point(0, 86);
        splitContainerVertical.Name = "splitContainerVertical";
        splitContainerVertical.Orientation = Orientation.Horizontal;
        // 
        // splitContainerVertical.Panel1
        // 
        splitContainerVertical.Panel1.Controls.Add(splitContainerMain);
        // 
        // splitContainerVertical.Panel2
        // 
        splitContainerVertical.Panel2.Controls.Add(splitContainerHex);
        splitContainerVertical.Size = new Size(1400, 742);
        splitContainerVertical.SplitterDistance = 452;
        splitContainerVertical.TabIndex = 3;
        // 
        // splitContainerMain
        // 
        splitContainerMain.Dock = DockStyle.Fill;
        splitContainerMain.Location = new Point(0, 0);
        splitContainerMain.Name = "splitContainerMain";
        // 
        // splitContainerMain.Panel1
        // 
        splitContainerMain.Panel1.Controls.Add(splitContainerLeft);
        // 
        // splitContainerMain.Panel2
        // 
        splitContainerMain.Panel2.Controls.Add(splitContainerRight);
        splitContainerMain.Size = new Size(1400, 452);
        splitContainerMain.SplitterDistance = 698;
        splitContainerMain.TabIndex = 0;
        // 
        // splitContainerLeft
        // 
        splitContainerLeft.Dock = DockStyle.Fill;
        splitContainerLeft.Location = new Point(0, 0);
        splitContainerLeft.Name = "splitContainerLeft";
        splitContainerLeft.Orientation = Orientation.Horizontal;
        // 
        // splitContainerLeft.Panel1
        // 
        splitContainerLeft.Panel1.Controls.Add(treeViewLeft);
        // 
        // splitContainerLeft.Panel2
        // 
        splitContainerLeft.Panel2.Controls.Add(listViewLeft);
        splitContainerLeft.Size = new Size(698, 452);
        splitContainerLeft.SplitterDistance = 270;
        splitContainerLeft.TabIndex = 0;
        // 
        // treeViewLeft
        // 
        treeViewLeft.Dock = DockStyle.Fill;
        treeViewLeft.Location = new Point(0, 0);
        treeViewLeft.Name = "treeViewLeft";
        treeViewLeft.Size = new Size(698, 270);
        treeViewLeft.TabIndex = 0;
        treeViewLeft.AfterSelect += TreeViewLeft_AfterSelect;
        // 
        // listViewLeft
        // 
        listViewLeft.Columns.AddRange(new ColumnHeader[] { columnLeftProperty, columnLeftValue });
        listViewLeft.Dock = DockStyle.Fill;
        listViewLeft.FullRowSelect = true;
        listViewLeft.GridLines = true;
        listViewLeft.Location = new Point(0, 0);
        listViewLeft.Name = "listViewLeft";
        listViewLeft.Size = new Size(698, 178);
        listViewLeft.TabIndex = 0;
        listViewLeft.UseCompatibleStateImageBehavior = false;
        listViewLeft.View = View.Details;
        listViewLeft.SelectedIndexChanged += ListViewLeft_SelectedIndexChanged;
        // 
        // columnLeftProperty
        // 
        columnLeftProperty.Text = "Property";
        columnLeftProperty.Width = 180;
        // 
        // columnLeftValue
        // 
        columnLeftValue.Text = "Value";
        columnLeftValue.Width = 480;
        // 
        // splitContainerRight
        // 
        splitContainerRight.Dock = DockStyle.Fill;
        splitContainerRight.Location = new Point(0, 0);
        splitContainerRight.Name = "splitContainerRight";
        splitContainerRight.Orientation = Orientation.Horizontal;
        // 
        // splitContainerRight.Panel1
        // 
        splitContainerRight.Panel1.Controls.Add(treeViewRight);
        // 
        // splitContainerRight.Panel2
        // 
        splitContainerRight.Panel2.Controls.Add(listViewRight);
        splitContainerRight.Size = new Size(698, 452);
        splitContainerRight.SplitterDistance = 270;
        splitContainerRight.TabIndex = 0;
        // 
        // treeViewRight
        // 
        treeViewRight.Dock = DockStyle.Fill;
        treeViewRight.Location = new Point(0, 0);
        treeViewRight.Name = "treeViewRight";
        treeViewRight.Size = new Size(698, 270);
        treeViewRight.TabIndex = 0;
        treeViewRight.AfterSelect += TreeViewRight_AfterSelect;
        // 
        // listViewRight
        // 
        listViewRight.Columns.AddRange(new ColumnHeader[] { columnRightProperty, columnRightValue });
        listViewRight.Dock = DockStyle.Fill;
        listViewRight.FullRowSelect = true;
        listViewRight.GridLines = true;
        listViewRight.Location = new Point(0, 0);
        listViewRight.Name = "listViewRight";
        listViewRight.Size = new Size(698, 178);
        listViewRight.TabIndex = 0;
        listViewRight.UseCompatibleStateImageBehavior = false;
        listViewRight.View = View.Details;
        listViewRight.SelectedIndexChanged += ListViewRight_SelectedIndexChanged;
        // 
        // columnRightProperty
        // 
        columnRightProperty.Text = "Property";
        columnRightProperty.Width = 180;
        // 
        // columnRightValue
        // 
        columnRightValue.Text = "Value";
        columnRightValue.Width = 480;
        // 
        // splitContainerHex
        // 
        splitContainerHex.Dock = DockStyle.Fill;
        splitContainerHex.Location = new Point(0, 0);
        splitContainerHex.Name = "splitContainerHex";
        // 
        // splitContainerHex.Panel1
        // 
        splitContainerHex.Panel1.Controls.Add(groupBoxHexLeft);
        // 
        // splitContainerHex.Panel2
        // 
        splitContainerHex.Panel2.Controls.Add(groupBoxHexRight);
        splitContainerHex.Size = new Size(1400, 286);
        splitContainerHex.SplitterDistance = 698;
        splitContainerHex.TabIndex = 0;
        // 
        // groupBoxHexLeft
        // 
        groupBoxHexLeft.Controls.Add(hexViewLeft);
        groupBoxHexLeft.Dock = DockStyle.Fill;
        groupBoxHexLeft.Location = new Point(0, 0);
        groupBoxHexLeft.Name = "groupBoxHexLeft";
        groupBoxHexLeft.Size = new Size(698, 286);
        groupBoxHexLeft.TabIndex = 0;
        groupBoxHexLeft.TabStop = false;
        groupBoxHexLeft.Text = "Hex View - Left File";
        // 
        // hexViewLeft
        // 
        hexViewLeft.BorderStyle = BorderStyle.FixedSingle;
        hexViewLeft.BytesPerLine = 16;
        hexViewLeft.Dock = DockStyle.Fill;
        hexViewLeft.Location = new Point(3, 19);
        hexViewLeft.Name = "hexViewLeft";
        hexViewLeft.ShowAllData = false;
        hexViewLeft.Size = new Size(692, 264);
        hexViewLeft.TabIndex = 0;
        // 
        // groupBoxHexRight
        // 
        groupBoxHexRight.Controls.Add(hexViewRight);
        groupBoxHexRight.Dock = DockStyle.Fill;
        groupBoxHexRight.Location = new Point(0, 0);
        groupBoxHexRight.Name = "groupBoxHexRight";
        groupBoxHexRight.Size = new Size(698, 286);
        groupBoxHexRight.TabIndex = 0;
        groupBoxHexRight.TabStop = false;
        groupBoxHexRight.Text = "Hex View - Right File";
        // 
        // hexViewRight
        // 
        hexViewRight.BorderStyle = BorderStyle.FixedSingle;
        hexViewRight.BytesPerLine = 16;
        hexViewRight.Dock = DockStyle.Fill;
        hexViewRight.Location = new Point(3, 19);
        hexViewRight.Name = "hexViewRight";
        hexViewRight.ShowAllData = false;
        hexViewRight.Size = new Size(692, 264);
        hexViewRight.TabIndex = 0;
        // 
        // FileCompareForm
        // 
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1400, 850);
        Controls.Add(splitContainerVertical);
        Controls.Add(diffSummaryPanel);
        Controls.Add(topPanel);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "FileCompareForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "File Compare";
        DragDrop += FileCompareForm_DragDrop;
        DragEnter += FileCompareForm_DragEnter;
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        diffSummaryPanel.ResumeLayout(false);
        diffSummaryPanel.PerformLayout();
        splitContainerVertical.Panel1.ResumeLayout(false);
        splitContainerVertical.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerVertical).EndInit();
        splitContainerVertical.ResumeLayout(false);
        splitContainerMain.Panel1.ResumeLayout(false);
        splitContainerMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
        splitContainerMain.ResumeLayout(false);
        splitContainerLeft.Panel1.ResumeLayout(false);
        splitContainerLeft.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerLeft).EndInit();
        splitContainerLeft.ResumeLayout(false);
        splitContainerRight.Panel1.ResumeLayout(false);
        splitContainerRight.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerRight).EndInit();
        splitContainerRight.ResumeLayout(false);
        splitContainerHex.Panel1.ResumeLayout(false);
        splitContainerHex.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerHex).EndInit();
        splitContainerHex.ResumeLayout(false);
        groupBoxHexLeft.ResumeLayout(false);
        groupBoxHexRight.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem openLeftToolStripMenuItem;
    private ToolStripMenuItem openRightToolStripMenuItem;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem swapToolStripMenuItem;
    private ToolStripSeparator toolStripSeparator2;
    private ToolStripMenuItem exitToolStripMenuItem;
    private ToolStripMenuItem viewToolStripMenuItem;
    private ToolStripMenuItem refreshToolStripMenuItem;
    private ToolStripSeparator toolStripSeparator3;
    private ToolStripMenuItem showHexViewToolStripMenuItem;
    private ToolStripMenuItem showAllHexDataToolStripMenuItem;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
    private Panel topPanel;
    private Panel diffSummaryPanel;
    private Label lblDiffSummary;
    private Button btnBrowseRight;
    private TextBox txtRightFile;
    private Label lblRightFile;
    private Button btnBrowseLeft;
    private TextBox txtLeftFile;
    private Label lblLeftFile;
    private SplitContainer splitContainerVertical;
    private SplitContainer splitContainerMain;
    private SplitContainer splitContainerLeft;
    private TreeView treeViewLeft;
    private ListView listViewLeft;
    private ColumnHeader columnLeftProperty;
    private ColumnHeader columnLeftValue;
    private SplitContainer splitContainerRight;
    private TreeView treeViewRight;
    private ListView listViewRight;
    private ColumnHeader columnRightProperty;
    private ColumnHeader columnRightValue;
    private SplitContainer splitContainerHex;
    private GroupBox groupBoxHexLeft;
    private HexViewControl hexViewLeft;
    private GroupBox groupBoxHexRight;
    private HexViewControl hexViewRight;
}
