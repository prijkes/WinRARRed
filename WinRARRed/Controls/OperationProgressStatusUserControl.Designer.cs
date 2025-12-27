
namespace WinRARRed.Controls
{
    partial class OperationProgressStatusUserControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbStatus = new System.Windows.Forms.GroupBox();
            this.lblStatusOperationProgress = new System.Windows.Forms.Label();
            this.lblStatusOperationTimeRemaining = new System.Windows.Forms.Label();
            this.lblStatusOperationTimeElapsed = new System.Windows.Forms.Label();
            this.lblStatusOperationStartDateTime = new System.Windows.Forms.Label();
            this.lblStatusOperationSize = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.lblStatusOperationProgressed = new System.Windows.Forms.Label();
            this.lblStatusOperationRemaining = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.lblStatusOperationSpeed = new System.Windows.Forms.Label();
            this.lblStatusOperationEstimatedFinishDateTime = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblProgress = new System.Windows.Forms.Label();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.label6 = new System.Windows.Forms.Label();
            this.tbStatus = new System.Windows.Forms.TextBox();
            this.gbStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbStatus
            // 
            this.gbStatus.Controls.Add(this.lblStatusOperationProgress);
            this.gbStatus.Controls.Add(this.lblStatusOperationTimeRemaining);
            this.gbStatus.Controls.Add(this.lblStatusOperationTimeElapsed);
            this.gbStatus.Controls.Add(this.lblStatusOperationStartDateTime);
            this.gbStatus.Controls.Add(this.lblStatusOperationSize);
            this.gbStatus.Controls.Add(this.label9);
            this.gbStatus.Controls.Add(this.label11);
            this.gbStatus.Controls.Add(this.label10);
            this.gbStatus.Controls.Add(this.label12);
            this.gbStatus.Controls.Add(this.label13);
            this.gbStatus.Controls.Add(this.label14);
            this.gbStatus.Controls.Add(this.lblStatusOperationProgressed);
            this.gbStatus.Controls.Add(this.lblStatusOperationRemaining);
            this.gbStatus.Controls.Add(this.label15);
            this.gbStatus.Controls.Add(this.label16);
            this.gbStatus.Controls.Add(this.label17);
            this.gbStatus.Controls.Add(this.lblStatusOperationSpeed);
            this.gbStatus.Controls.Add(this.lblStatusOperationEstimatedFinishDateTime);
            this.gbStatus.Controls.Add(this.label5);
            this.gbStatus.Controls.Add(this.lblProgress);
            this.gbStatus.Controls.Add(this.pbProgress);
            this.gbStatus.Controls.Add(this.label6);
            this.gbStatus.Controls.Add(this.tbStatus);
            this.gbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbStatus.Location = new System.Drawing.Point(0, 0);
            this.gbStatus.Name = "gbStatus";
            this.gbStatus.Size = new System.Drawing.Size(708, 199);
            this.gbStatus.TabIndex = 6;
            this.gbStatus.TabStop = false;
            this.gbStatus.Text = "Archiving release status";
            // 
            // lblStatusOperationProgress
            // 
            this.lblStatusOperationProgress.Location = new System.Drawing.Point(582, 84);
            this.lblStatusOperationProgress.Name = "lblStatusOperationProgress";
            this.lblStatusOperationProgress.Size = new System.Drawing.Size(120, 15);
            this.lblStatusOperationProgress.TabIndex = 47;
            this.lblStatusOperationProgress.Text = "-";
            // 
            // lblStatusOperationTimeRemaining
            // 
            this.lblStatusOperationTimeRemaining.Location = new System.Drawing.Point(301, 84);
            this.lblStatusOperationTimeRemaining.Name = "lblStatusOperationTimeRemaining";
            this.lblStatusOperationTimeRemaining.Size = new System.Drawing.Size(120, 15);
            this.lblStatusOperationTimeRemaining.TabIndex = 41;
            this.lblStatusOperationTimeRemaining.Text = "-";
            // 
            // lblStatusOperationTimeElapsed
            // 
            this.lblStatusOperationTimeElapsed.Location = new System.Drawing.Point(301, 69);
            this.lblStatusOperationTimeElapsed.Name = "lblStatusOperationTimeElapsed";
            this.lblStatusOperationTimeElapsed.Size = new System.Drawing.Size(120, 15);
            this.lblStatusOperationTimeElapsed.TabIndex = 40;
            this.lblStatusOperationTimeElapsed.Text = "-";
            // 
            // lblStatusOperationStartDateTime
            // 
            this.lblStatusOperationStartDateTime.Location = new System.Drawing.Point(301, 54);
            this.lblStatusOperationStartDateTime.Name = "lblStatusOperationStartDateTime";
            this.lblStatusOperationStartDateTime.Size = new System.Drawing.Size(120, 15);
            this.lblStatusOperationStartDateTime.TabIndex = 39;
            this.lblStatusOperationStartDateTime.Text = "-";
            // 
            // lblStatusOperationSize
            // 
            this.lblStatusOperationSize.Location = new System.Drawing.Point(136, 54);
            this.lblStatusOperationSize.Name = "lblStatusOperationSize";
            this.lblStatusOperationSize.Size = new System.Drawing.Size(60, 15);
            this.lblStatusOperationSize.TabIndex = 36;
            this.lblStatusOperationSize.Text = "-";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 54);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(85, 15);
            this.label9.TabIndex = 30;
            this.label9.Text = "Operation size:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 84);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(120, 15);
            this.label11.TabIndex = 32;
            this.label11.Text = "Operation remaining:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 69);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(124, 15);
            this.label10.TabIndex = 31;
            this.label10.Text = "Operation progressed:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(202, 54);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(89, 15);
            this.label12.TabIndex = 33;
            this.label12.Text = "Start date/time:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(202, 69);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(79, 15);
            this.label13.TabIndex = 34;
            this.label13.Text = "Time elapsed:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(202, 84);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(93, 15);
            this.label14.TabIndex = 35;
            this.label14.Text = "Time remaining:";
            // 
            // lblStatusOperationProgressed
            // 
            this.lblStatusOperationProgressed.Location = new System.Drawing.Point(136, 69);
            this.lblStatusOperationProgressed.Name = "lblStatusOperationProgressed";
            this.lblStatusOperationProgressed.Size = new System.Drawing.Size(60, 15);
            this.lblStatusOperationProgressed.TabIndex = 37;
            this.lblStatusOperationProgressed.Text = "-";
            // 
            // lblStatusOperationRemaining
            // 
            this.lblStatusOperationRemaining.Location = new System.Drawing.Point(136, 84);
            this.lblStatusOperationRemaining.Name = "lblStatusOperationRemaining";
            this.lblStatusOperationRemaining.Size = new System.Drawing.Size(60, 15);
            this.lblStatusOperationRemaining.TabIndex = 38;
            this.lblStatusOperationRemaining.Text = "-";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(427, 54);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(97, 15);
            this.label15.TabIndex = 42;
            this.label15.Text = "Operation speed:";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(427, 69);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(149, 15);
            this.label16.TabIndex = 43;
            this.label16.Text = "Estimated finish date/time:";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(427, 84);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(55, 15);
            this.label17.TabIndex = 44;
            this.label17.Text = "Progress:";
            // 
            // lblStatusOperationSpeed
            // 
            this.lblStatusOperationSpeed.Location = new System.Drawing.Point(582, 54);
            this.lblStatusOperationSpeed.Name = "lblStatusOperationSpeed";
            this.lblStatusOperationSpeed.Size = new System.Drawing.Size(120, 15);
            this.lblStatusOperationSpeed.TabIndex = 45;
            this.lblStatusOperationSpeed.Text = "-";
            // 
            // lblStatusOperationEstimatedFinishDateTime
            // 
            this.lblStatusOperationEstimatedFinishDateTime.Location = new System.Drawing.Point(582, 69);
            this.lblStatusOperationEstimatedFinishDateTime.Name = "lblStatusOperationEstimatedFinishDateTime";
            this.lblStatusOperationEstimatedFinishDateTime.Size = new System.Drawing.Size(120, 15);
            this.lblStatusOperationEstimatedFinishDateTime.TabIndex = 46;
            this.lblStatusOperationEstimatedFinishDateTime.Text = "-";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 27);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(55, 15);
            this.label5.TabIndex = 9;
            this.label5.Text = "Progress:";
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(654, 27);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(35, 15);
            this.lblProgress.TabIndex = 8;
            this.lblProgress.Text = "100%";
            // 
            // pbProgress
            // 
            this.pbProgress.Location = new System.Drawing.Point(70, 22);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(578, 23);
            this.pbProgress.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 113);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 15);
            this.label6.TabIndex = 2;
            this.label6.Text = "Status:";
            // 
            // tbStatus
            // 
            this.tbStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbStatus.Location = new System.Drawing.Point(56, 113);
            this.tbStatus.Multiline = true;
            this.tbStatus.Name = "tbStatus";
            this.tbStatus.ReadOnly = true;
            this.tbStatus.Size = new System.Drawing.Size(646, 80);
            this.tbStatus.TabIndex = 29;
            this.tbStatus.Text = "Waiting to start...";
            // 
            // OperationProgressStatusUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbStatus);
            this.Name = "OperationProgressStatusUserControl";
            this.Size = new System.Drawing.Size(708, 199);
            this.gbStatus.ResumeLayout(false);
            this.gbStatus.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbStatus;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.ProgressBar pbProgress;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbStatus;
        private System.Windows.Forms.Label lblStatusOperationProgress;
        private System.Windows.Forms.Label lblStatusOperationTimeRemaining;
        private System.Windows.Forms.Label lblStatusOperationTimeElapsed;
        private System.Windows.Forms.Label lblStatusOperationStartDateTime;
        private System.Windows.Forms.Label lblStatusOperationSize;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label lblStatusOperationProgressed;
        private System.Windows.Forms.Label lblStatusOperationRemaining;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label lblStatusOperationSpeed;
        private System.Windows.Forms.Label lblStatusOperationEstimatedFinishDateTime;
    }
}
