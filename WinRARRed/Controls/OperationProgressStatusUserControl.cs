using System;
using System.Text;
using System.Windows.Forms;
using WinRARRed.IO;

namespace WinRARRed.Controls;

public partial class OperationProgressStatusUserControl : UserControl
{
        private string title = "Title";

        public string Title
        {
            get => title;
            set
            {
                if (value != title)
                {
                    title = value;

                    gbStatus.Text = value;
                }
            }
        }

        public OperationProgressStatusUserControl()
        {
            InitializeComponent();
        }

        public void OperationStatusChanged(string operation, string? text, OperationStatusChangedEventArgs e)
        {
            StringBuilder sb = new();
            if (!string.IsNullOrEmpty(text))
            {
                sb.AppendLine(text);
            }

            if (e.OldStatus.HasValue)
            {
                sb.Append($"Old status: {e.OldStatus}{Environment.NewLine}");
            }

            sb.Append($"New status: {e.NewStatus}");
            if (e.CompletionStatus.HasValue)
            {
                sb.Append($"{Environment.NewLine}Completion status: {e.CompletionStatus}");
            }

            string line = $"[{operation}] {sb}";

            //Log.Write(this, line);

            tbStatus.Text = line;

            if (e.OldStatus == null && e.NewStatus == OperationStatus.Running)
            {
                Reset();
            }
            else if (e.NewStatus == OperationStatus.Completed && e.CompletionStatus == OperationCompletionStatus.Success)
            {
                SetProgressBarValueImmediate(100);
                lblProgress.Text = "100%";

                lblStatusOperationRemaining.Text = "-";
                lblStatusOperationTimeRemaining.Text = "-";
                lblStatusOperationEstimatedFinishDateTime.Text = DateTime.Now.ToString();
                lblStatusOperationProgress.Text = "100";
            }
        }

        public void OperationProgressChanged(string operation, string? status, OperationProgressEventArgs e)
        {
            SetProgressBarValueImmediate((int)e.Progress);
            lblProgress.Text = $"{(int)e.Progress}%";

            lblStatusOperationSize.Text = e.OperationSize.ToString();
            lblStatusOperationProgressed.Text = e.OperationProgressed.ToString("0.##");
            lblStatusOperationRemaining.Text = e.OperationRemaining.ToString();
            lblStatusOperationStartDateTime.Text = e.StartDateTime.ToString();
            lblStatusOperationTimeElapsed.Text = e.TimeElapsed.ToString(@"hh\:mm\:ss");
            lblStatusOperationTimeRemaining.Text = e.TimeRemaining.ToString(@"hh\:mm\:ss");
            lblStatusOperationSpeed.Text = e.OperationSpeed.ToString();
            lblStatusOperationEstimatedFinishDateTime.Text = e.EstimatedFinishDateTime.ToString();
            lblStatusOperationProgress.Text = e.Progress.ToString("0.##");

            tbStatus.Text = status;
        }

        public void Reset()
        {
            pbProgress.Value = 0;
            lblProgress.Text = "-";

            lblStatusOperationSize.Text = "-";
            lblStatusOperationProgressed.Text = "-";
            lblStatusOperationRemaining.Text = "-";
            lblStatusOperationStartDateTime.Text = "-";
            lblStatusOperationTimeElapsed.Text = "-";
            lblStatusOperationTimeRemaining.Text = "-";
            lblStatusOperationSpeed.Text = "-";
            lblStatusOperationEstimatedFinishDateTime.Text = "-";
            lblStatusOperationProgress.Text = "-";

            tbStatus.Clear();
        }

        /// <summary>
        /// Sets the progress bar value without the Windows visual styles animation lag.
        /// Briefly overshoots the value and sets it back, forcing an immediate repaint.
        /// </summary>
        private void SetProgressBarValueImmediate(int value)
        {
            if (value >= pbProgress.Maximum)
            {
                pbProgress.Maximum = value + 1;
                pbProgress.Value = value + 1;
                pbProgress.Maximum = value;
                pbProgress.Value = value;
            }
            else if (value > 0)
            {
                pbProgress.Value = value + 1;
                pbProgress.Value = value;
            }
            else
            {
                pbProgress.Value = 0;
            }
        }
}
