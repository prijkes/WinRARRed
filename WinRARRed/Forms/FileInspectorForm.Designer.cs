using System.Drawing;
using System.Windows.Forms;
using WinRARRed.Controls;

namespace WinRARRed.Forms;

partial class FileInspectorForm
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
        openToolStripMenuItem = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        exitToolStripMenuItem = new ToolStripMenuItem();
        viewToolStripMenuItem = new ToolStripMenuItem();
        showHexViewToolStripMenuItem = new ToolStripMenuItem();
        splitContainerMain = new SplitContainer();
        treeFilterPanel = new Panel();
        txtTreeFilter = new TextBox();
        lblTreeFilter = new Label();
        lblTreeFilterCount = new Label();
        treeView = new TreeView();
        contextMenuTree = new ContextMenuStrip();
        exportToolStripMenuItem = new ToolStripMenuItem();
        splitContainerVertical = new SplitContainer();
        listView = new ListView();
        columnProperty = new ColumnHeader();
        columnValue = new ColumnHeader();
        groupBoxHex = new GroupBox();
        hexView = new HexViewControl();

        menuStrip.SuspendLayout();
        contextMenuTree.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
        splitContainerMain.Panel1.SuspendLayout();
        splitContainerMain.Panel2.SuspendLayout();
        splitContainerMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerVertical).BeginInit();
        splitContainerVertical.Panel1.SuspendLayout();
        splitContainerVertical.Panel2.SuspendLayout();
        splitContainerVertical.SuspendLayout();
        groupBoxHex.SuspendLayout();
        SuspendLayout();

        // menuStrip
        menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(1000, 24);
        menuStrip.TabIndex = 0;

        // fileToolStripMenuItem
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new Size(37, 20);
        fileToolStripMenuItem.Text = "&File";

        // openToolStripMenuItem
        openToolStripMenuItem.Name = "openToolStripMenuItem";
        openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        openToolStripMenuItem.Size = new Size(155, 22);
        openToolStripMenuItem.Text = "&Open...";
        openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;

        // toolStripSeparator1
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(152, 6);

        // exitToolStripMenuItem
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        exitToolStripMenuItem.Size = new Size(155, 22);
        exitToolStripMenuItem.Text = "E&xit";
        exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;

        // viewToolStripMenuItem
        viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { showHexViewToolStripMenuItem });
        viewToolStripMenuItem.Name = "viewToolStripMenuItem";
        viewToolStripMenuItem.Size = new Size(44, 20);
        viewToolStripMenuItem.Text = "&View";

        // showHexViewToolStripMenuItem
        showHexViewToolStripMenuItem.Checked = true;
        showHexViewToolStripMenuItem.CheckOnClick = true;
        showHexViewToolStripMenuItem.CheckState = CheckState.Checked;
        showHexViewToolStripMenuItem.Name = "showHexViewToolStripMenuItem";
        showHexViewToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.H;
        showHexViewToolStripMenuItem.Size = new Size(200, 22);
        showHexViewToolStripMenuItem.Text = "Show &Hex View";
        showHexViewToolStripMenuItem.Click += ShowHexViewToolStripMenuItem_Click;

        // splitContainerMain
        splitContainerMain.Dock = DockStyle.Fill;
        splitContainerMain.Location = new Point(0, 24);
        splitContainerMain.Name = "splitContainerMain";
        splitContainerMain.Panel1.Controls.Add(treeView);
        splitContainerMain.Panel1.Controls.Add(treeFilterPanel);
        splitContainerMain.Panel2.Controls.Add(splitContainerVertical);
        splitContainerMain.Size = new Size(1000, 626);
        splitContainerMain.SplitterDistance = 280;
        splitContainerMain.TabIndex = 1;

        // treeFilterPanel
        treeFilterPanel.Controls.Add(txtTreeFilter);
        treeFilterPanel.Controls.Add(lblTreeFilter);
        treeFilterPanel.Controls.Add(lblTreeFilterCount);
        treeFilterPanel.Dock = DockStyle.Top;
        treeFilterPanel.Location = new Point(0, 0);
        treeFilterPanel.Name = "treeFilterPanel";
        treeFilterPanel.Padding = new Padding(4, 4, 4, 2);
        treeFilterPanel.Size = new Size(280, 30);
        treeFilterPanel.TabIndex = 1;

        // lblTreeFilter
        lblTreeFilter.AutoSize = true;
        lblTreeFilter.Location = new Point(7, 8);
        lblTreeFilter.Name = "lblTreeFilter";
        lblTreeFilter.Size = new Size(33, 15);
        lblTreeFilter.TabIndex = 0;
        lblTreeFilter.Text = "Find:";

        // txtTreeFilter
        txtTreeFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtTreeFilter.Location = new Point(44, 4);
        txtTreeFilter.Name = "txtTreeFilter";
        txtTreeFilter.Size = new Size(168, 23);
        txtTreeFilter.TabIndex = 1;
        txtTreeFilter.PlaceholderText = "Filter blocks...";
        txtTreeFilter.TextChanged += TxtTreeFilter_TextChanged;

        // lblTreeFilterCount
        lblTreeFilterCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblTreeFilterCount.AutoSize = true;
        lblTreeFilterCount.ForeColor = SystemColors.GrayText;
        lblTreeFilterCount.Location = new Point(218, 8);
        lblTreeFilterCount.Name = "lblTreeFilterCount";
        lblTreeFilterCount.Size = new Size(55, 15);
        lblTreeFilterCount.TabIndex = 2;
        lblTreeFilterCount.Text = "";

        // treeView
        treeView.ContextMenuStrip = contextMenuTree;
        treeView.Dock = DockStyle.Fill;
        treeView.Location = new Point(0, 30);
        treeView.Name = "treeView";
        treeView.Size = new Size(280, 596);
        treeView.TabIndex = 0;
        treeView.AfterSelect += treeView_AfterSelect;
        treeView.NodeMouseClick += treeView_NodeMouseClick;

        // contextMenuTree
        contextMenuTree.Items.AddRange(new ToolStripItem[] { exportToolStripMenuItem });
        contextMenuTree.Name = "contextMenuTree";
        contextMenuTree.Size = new Size(120, 26);
        contextMenuTree.Opening += contextMenuTree_Opening;

        // exportToolStripMenuItem
        exportToolStripMenuItem.Name = "exportToolStripMenuItem";
        exportToolStripMenuItem.Size = new Size(119, 22);
        exportToolStripMenuItem.Text = "Export...";
        exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;

        // splitContainerVertical
        splitContainerVertical.Dock = DockStyle.Fill;
        splitContainerVertical.Location = new Point(0, 0);
        splitContainerVertical.Name = "splitContainerVertical";
        splitContainerVertical.Orientation = Orientation.Horizontal;
        splitContainerVertical.Panel1.Controls.Add(listView);
        splitContainerVertical.Panel2.Controls.Add(groupBoxHex);
        splitContainerVertical.Size = new Size(716, 626);
        splitContainerVertical.SplitterDistance = 350;
        splitContainerVertical.TabIndex = 0;

        // listView
        listView.Columns.AddRange(new ColumnHeader[] { columnProperty, columnValue });
        listView.Dock = DockStyle.Fill;
        listView.FullRowSelect = true;
        listView.GridLines = true;
        listView.Location = new Point(0, 0);
        listView.Name = "listView";
        listView.Size = new Size(716, 220);
        listView.TabIndex = 0;
        listView.UseCompatibleStateImageBehavior = false;
        listView.View = View.Details;
        listView.SelectedIndexChanged += listView_SelectedIndexChanged;

        // columnProperty
        columnProperty.Text = "Property";
        columnProperty.Width = 180;

        // columnValue
        columnValue.Text = "Value";
        columnValue.Width = 500;

        // groupBoxHex
        groupBoxHex.Controls.Add(hexView);
        groupBoxHex.Dock = DockStyle.Fill;
        groupBoxHex.Location = new Point(0, 0);
        groupBoxHex.Name = "groupBoxHex";
        groupBoxHex.Padding = new Padding(3);
        groupBoxHex.Size = new Size(716, 272);
        groupBoxHex.TabIndex = 0;
        groupBoxHex.TabStop = false;
        groupBoxHex.Text = "Hex View";

        // hexView
        hexView.Dock = DockStyle.Fill;
        hexView.Location = new Point(3, 19);
        hexView.Name = "hexView";
        hexView.Size = new Size(710, 250);
        hexView.TabIndex = 0;

        // FileInspectorForm
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 650);
        Controls.Add(splitContainerMain);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "FileInspectorForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "File Inspector";
        DragDrop += FileInspectorForm_DragDrop;
        DragEnter += FileInspectorForm_DragEnter;

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        contextMenuTree.ResumeLayout(false);
        treeFilterPanel.ResumeLayout(false);
        treeFilterPanel.PerformLayout();
        splitContainerMain.Panel1.ResumeLayout(false);
        splitContainerMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
        splitContainerMain.ResumeLayout(false);
        splitContainerVertical.Panel1.ResumeLayout(false);
        splitContainerVertical.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerVertical).EndInit();
        splitContainerVertical.ResumeLayout(false);
        groupBoxHex.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem openToolStripMenuItem;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem exitToolStripMenuItem;
    private ToolStripMenuItem viewToolStripMenuItem;
    private ToolStripMenuItem showHexViewToolStripMenuItem;
    private SplitContainer splitContainerMain;
    private Panel treeFilterPanel;
    private TextBox txtTreeFilter;
    private Label lblTreeFilter;
    private Label lblTreeFilterCount;
    private TreeView treeView;
    private ContextMenuStrip contextMenuTree;
    private ToolStripMenuItem exportToolStripMenuItem;
    private SplitContainer splitContainerVertical;
    private ListView listView;
    private ColumnHeader columnProperty;
    private ColumnHeader columnValue;
    private GroupBox groupBoxHex;
    private HexViewControl hexView;
}
