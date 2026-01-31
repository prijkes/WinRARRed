namespace WinRARRed.Forms;

partial class ViewCommandLinesForm
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
            this.tbCommandLines = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tbCommandLines
            // 
            this.tbCommandLines.BackColor = System.Drawing.Color.White;
            this.tbCommandLines.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbCommandLines.Location = new System.Drawing.Point(0, 0);
            this.tbCommandLines.Multiline = true;
            this.tbCommandLines.Name = "tbCommandLines";
            this.tbCommandLines.ReadOnly = true;
            this.tbCommandLines.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbCommandLines.Size = new System.Drawing.Size(800, 450);
            this.tbCommandLines.TabIndex = 0;
            // 
            // ViewCommandLinesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tbCommandLines);
            this.Name = "ViewCommandLinesForm";
            this.Text = "Command Lines";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

    private System.Windows.Forms.TextBox tbCommandLines;
}