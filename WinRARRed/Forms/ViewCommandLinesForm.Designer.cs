namespace WinRARRed.Forms;

partial class ViewCommandLinesForm
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
        topPanel = new System.Windows.Forms.Panel();
        lblSearch = new System.Windows.Forms.Label();
        txtSearch = new System.Windows.Forms.TextBox();
        btnCopySelected = new System.Windows.Forms.Button();
        btnCopyAll = new System.Windows.Forms.Button();
        lblLineCount = new System.Windows.Forms.Label();
        listViewCommands = new System.Windows.Forms.ListView();
        columnNumber = new System.Windows.Forms.ColumnHeader();
        columnCommandLine = new System.Windows.Forms.ColumnHeader();
        topPanel.SuspendLayout();
        SuspendLayout();

        // topPanel
        topPanel.Controls.Add(lblSearch);
        topPanel.Controls.Add(txtSearch);
        topPanel.Controls.Add(btnCopySelected);
        topPanel.Controls.Add(btnCopyAll);
        topPanel.Controls.Add(lblLineCount);
        topPanel.Dock = System.Windows.Forms.DockStyle.Top;
        topPanel.Location = new System.Drawing.Point(0, 0);
        topPanel.Name = "topPanel";
        topPanel.Padding = new System.Windows.Forms.Padding(6);
        topPanel.Size = new System.Drawing.Size(1000, 36);
        topPanel.TabIndex = 0;

        // lblSearch
        lblSearch.AutoSize = true;
        lblSearch.Location = new System.Drawing.Point(9, 11);
        lblSearch.Name = "lblSearch";
        lblSearch.Size = new System.Drawing.Size(39, 15);
        lblSearch.TabIndex = 0;
        lblSearch.Text = "Filter:";

        // txtSearch
        txtSearch.Location = new System.Drawing.Point(54, 7);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new System.Drawing.Size(300, 23);
        txtSearch.TabIndex = 1;
        txtSearch.PlaceholderText = "Type to filter command lines...";
        txtSearch.TextChanged += TxtSearch_TextChanged;

        // btnCopySelected
        btnCopySelected.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnCopySelected.Location = new System.Drawing.Point(783, 6);
        btnCopySelected.Name = "btnCopySelected";
        btnCopySelected.Size = new System.Drawing.Size(100, 25);
        btnCopySelected.TabIndex = 2;
        btnCopySelected.Text = "Copy Selected";
        btnCopySelected.UseVisualStyleBackColor = true;
        btnCopySelected.Click += BtnCopySelected_Click;

        // btnCopyAll
        btnCopyAll.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        btnCopyAll.Location = new System.Drawing.Point(889, 6);
        btnCopyAll.Name = "btnCopyAll";
        btnCopyAll.Size = new System.Drawing.Size(100, 25);
        btnCopyAll.TabIndex = 3;
        btnCopyAll.Text = "Copy All";
        btnCopyAll.UseVisualStyleBackColor = true;
        btnCopyAll.Click += BtnCopyAll_Click;

        // lblLineCount
        lblLineCount.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        lblLineCount.AutoSize = true;
        lblLineCount.Location = new System.Drawing.Point(600, 11);
        lblLineCount.Name = "lblLineCount";
        lblLineCount.Size = new System.Drawing.Size(100, 15);
        lblLineCount.TabIndex = 4;
        lblLineCount.Text = "0 command lines";
        lblLineCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // listViewCommands
        listViewCommands.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnNumber, columnCommandLine });
        listViewCommands.Dock = System.Windows.Forms.DockStyle.Fill;
        listViewCommands.FullRowSelect = true;
        listViewCommands.GridLines = true;
        listViewCommands.Location = new System.Drawing.Point(0, 36);
        listViewCommands.Name = "listViewCommands";
        listViewCommands.Size = new System.Drawing.Size(1000, 414);
        listViewCommands.TabIndex = 5;
        listViewCommands.UseCompatibleStateImageBehavior = false;
        listViewCommands.View = System.Windows.Forms.View.Details;
        listViewCommands.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);

        // columnNumber
        columnNumber.Text = "#";
        columnNumber.Width = 50;

        // columnCommandLine
        columnCommandLine.Text = "Command Line";
        columnCommandLine.Width = 920;

        // ViewCommandLinesForm
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1000, 450);
        Controls.Add(listViewCommands);
        Controls.Add(topPanel);
        Name = "ViewCommandLinesForm";
        Text = "Command Lines";
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Panel topPanel;
    private System.Windows.Forms.Label lblSearch;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Button btnCopySelected;
    private System.Windows.Forms.Button btnCopyAll;
    private System.Windows.Forms.Label lblLineCount;
    private System.Windows.Forms.ListView listViewCommands;
    private System.Windows.Forms.ColumnHeader columnNumber;
    private System.Windows.Forms.ColumnHeader columnCommandLine;
}
