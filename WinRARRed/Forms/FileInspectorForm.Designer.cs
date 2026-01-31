using System.Drawing;
using System.Windows.Forms;

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
        splitContainerMain = new SplitContainer();
        treeView = new TreeView();
        contextMenuTree = new ContextMenuStrip();
        exportToolStripMenuItem = new ToolStripMenuItem();
        splitContainerRight = new SplitContainer();
        listView = new ListView();
        columnProperty = new ColumnHeader();
        columnValue = new ColumnHeader();
        groupBoxComment = new GroupBox();
        txtComment = new TextBox();

        menuStrip.SuspendLayout();
        contextMenuTree.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
        splitContainerMain.Panel1.SuspendLayout();
        splitContainerMain.Panel2.SuspendLayout();
        splitContainerMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerRight).BeginInit();
        splitContainerRight.Panel1.SuspendLayout();
        splitContainerRight.Panel2.SuspendLayout();
        splitContainerRight.SuspendLayout();
        groupBoxComment.SuspendLayout();
        SuspendLayout();

        // menuStrip
        menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new Size(900, 24);
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

        // splitContainerMain
        splitContainerMain.Dock = DockStyle.Fill;
        splitContainerMain.Location = new Point(0, 24);
        splitContainerMain.Name = "splitContainerMain";
        splitContainerMain.Panel1.Controls.Add(treeView);
        splitContainerMain.Panel2.Controls.Add(splitContainerRight);
        splitContainerMain.Size = new Size(900, 526);
        splitContainerMain.SplitterDistance = 280;
        splitContainerMain.TabIndex = 1;

        // treeView
        treeView.ContextMenuStrip = contextMenuTree;
        treeView.Dock = DockStyle.Fill;
        treeView.Location = new Point(0, 0);
        treeView.Name = "treeView";
        treeView.Size = new Size(280, 526);
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

        // splitContainerRight
        splitContainerRight.Dock = DockStyle.Fill;
        splitContainerRight.Location = new Point(0, 0);
        splitContainerRight.Name = "splitContainerRight";
        splitContainerRight.Orientation = Orientation.Horizontal;
        splitContainerRight.Panel1.Controls.Add(listView);
        splitContainerRight.Panel2.Controls.Add(groupBoxComment);
        splitContainerRight.Size = new Size(616, 526);
        splitContainerRight.SplitterDistance = 350;
        splitContainerRight.TabIndex = 0;

        // listView
        listView.Columns.AddRange(new ColumnHeader[] { columnProperty, columnValue });
        listView.Dock = DockStyle.Fill;
        listView.FullRowSelect = true;
        listView.GridLines = true;
        listView.Location = new Point(0, 0);
        listView.Name = "listView";
        listView.Size = new Size(616, 350);
        listView.TabIndex = 0;
        listView.UseCompatibleStateImageBehavior = false;
        listView.View = View.Details;

        // columnProperty
        columnProperty.Text = "Property";
        columnProperty.Width = 180;

        // columnValue
        columnValue.Text = "Value";
        columnValue.Width = 400;

        // groupBoxComment
        groupBoxComment.Controls.Add(txtComment);
        groupBoxComment.Dock = DockStyle.Fill;
        groupBoxComment.Location = new Point(0, 0);
        groupBoxComment.Name = "groupBoxComment";
        groupBoxComment.Padding = new Padding(6);
        groupBoxComment.Size = new Size(616, 172);
        groupBoxComment.TabIndex = 0;
        groupBoxComment.TabStop = false;
        groupBoxComment.Text = "Archive Comment";

        // txtComment
        txtComment.Dock = DockStyle.Fill;
        txtComment.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
        txtComment.Location = new Point(6, 22);
        txtComment.Multiline = true;
        txtComment.Name = "txtComment";
        txtComment.ReadOnly = true;
        txtComment.ScrollBars = ScrollBars.Both;
        txtComment.Size = new Size(604, 144);
        txtComment.TabIndex = 0;
        txtComment.WordWrap = false;

        // FileInspectorForm
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 550);
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
        splitContainerMain.Panel1.ResumeLayout(false);
        splitContainerMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
        splitContainerMain.ResumeLayout(false);
        splitContainerRight.Panel1.ResumeLayout(false);
        splitContainerRight.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerRight).EndInit();
        splitContainerRight.ResumeLayout(false);
        groupBoxComment.ResumeLayout(false);
        groupBoxComment.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem openToolStripMenuItem;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripMenuItem exitToolStripMenuItem;
    private SplitContainer splitContainerMain;
    private TreeView treeView;
    private ContextMenuStrip contextMenuTree;
    private ToolStripMenuItem exportToolStripMenuItem;
    private SplitContainer splitContainerRight;
    private ListView listView;
    private ColumnHeader columnProperty;
    private ColumnHeader columnValue;
    private GroupBox groupBoxComment;
    private TextBox txtComment;
}
