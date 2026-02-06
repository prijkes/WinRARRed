using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WinRARRed.Controls;
using WinRARRed.Cryptography;
using WinRARRed.Diagnostics;
using WinRARRed.IO;

namespace WinRARRed.Forms;

public partial class MainForm : Form
{
    private const int WM_SETREDRAW = 0x000B;
    private const int WM_USER = 0x0400;
    private const int EM_GETEVENTMASK = WM_USER + 59;
    private const int EM_SETEVENTMASK = WM_USER + 69;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private Manager? Manager = null;

    private readonly SettingsOptionsForm OptionsForm;

    private System.Windows.Forms.Timer? elapsedTimer;
    private DateTime bruteForceStartTime;

    // Buffered logging
    private record LogEntry(string Text, LogTarget Target, DateTime Timestamp, Color Color);
    private readonly ConcurrentQueue<LogEntry> logBuffer = new();
    private System.Windows.Forms.Timer? logFlushTimer;
    private const int LogFlushIntervalMs = 50;
    private const int MaxEntriesPerFlush = 100;

    // Track whether tabs have unseen messages (for visual indicator)
    private bool _phase1HasUnread;
    private bool _phase2HasUnread;
    private const string Phase1TabTitle = "Phase 1 (Comment)";
    private const string Phase2TabTitle = "Phase 2 (Full RAR)";

    public MainForm()
    {
        InitializeComponent();

        OptionsForm = new();
        OptionsForm.Hide();
        OptionsForm.VerificationFileExtracted += OptionsForm_VerificationFileExtracted;

        WinRARRed.Log.Logged += Log_Logged;

        // Initialize buffered log flush timer
        logFlushTimer = new System.Windows.Forms.Timer { Interval = LogFlushIntervalMs };
        logFlushTimer.Tick += (s, e) => FlushLogBuffer();
        logFlushTimer.Start();

        tsmiFileExit.Click += TsmiFileExit_Click;
        tsmiViewCommandLines.Click += TsmiViewCommandLines_Click;
        tsmiSettingsOptions.Click += TsmiSettingsOptions_Click;

        btnWinRARDirectoryBrowse.Click += BtnWinRARDirectoryBrowse_Click;
        btnReleaseDirectoryBrowse.Click += BtnReleaseDirectoryBrowse_Click;
        btnVerificationFileBrowse.Click += BtnVerificationFileBrowse_Click;
        btnTemporaryDirectoryBrowse.Click += BtnTemporaryDirectoryBrowse_Click;
        btnClearLog.Click += BtnClearLog_Click;
        btnStart.Click += BtnStart_Click;
        tabControlLogs.SelectedIndexChanged += TabControlLogs_SelectedIndexChanged;

        FormClosing += MainForm_FormClosing;
    }

    private void TsmiFileExit_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void TsmiViewCommandLines_Click(object? sender, EventArgs e)
    {
        if (Manager == null || Manager.BruteForceOptions == null)
        {
            GUIHelper.ShowError(this, "No brute force has been started yet.");
            return;
        }

        ViewCommandLinesForm form = new(Manager.BruteForceOptions);
        form.Show(this);
    }

    private void TsmiSettingsOptions_Click(object? sender, EventArgs e)
    {
        OptionsForm.ShowDialog(this);
    }

    private void tsmiToolsFileInspector_Click(object? sender, EventArgs e)
    {
        var form = new FileInspectorForm();
        form.Show(this);
    }

    private void tsmiToolsFileCompare_Click(object? sender, EventArgs e)
    {
        var form = new FileCompareForm();
        form.Show(this);
    }

    private void LinkLabel1_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://drive.google.com/file/d/1of053kS2Wxk-foHN_ALRu-u6Tcck58yn/view?usp=drive_link",
            UseShellExecute = true
        });
    }
    
    private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://drive.google.com/file/d/1hvgzSY6YH_ZS3cpy7bHcw2zpjiwuP_Xi/view?usp=drive_link",
            UseShellExecute = true
        });
    }

    private void OptionsForm_VerificationFileExtracted(object? sender, string verificationFilePath)
    {
        if (string.IsNullOrWhiteSpace(verificationFilePath))
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                Invoke((MethodInvoker)delegate { OptionsForm_VerificationFileExtracted(sender, verificationFilePath); });
            }
            catch
            {
            }
            return;
        }

        tbVerificationFilePath.Text = verificationFilePath;
    }

    private void Manager_DirectoryExtractionProgress(object? sender, OperationProgressEventArgs e)
    {
        LogOperationProgressChanged(opStatus1, "Directory extraction", null, e);
    }

    private void Manager_DirectoryExtractionStatusChanged(object? sender, OperationStatusChangedEventArgs e)
    {
        LogOperationStatusChanged(opStatus1, "Directory extraction", null, e);
    }

    private void Manager_FileExtractionProgress(object? sender, FileExtractionProgressEventArgs e)
    {
        LogOperationProgressChanged(opStatus2, "File extraction", e.RARFile.FilePath, e);
    }

    private void Manager_FileExtractionStatusChanged(object? sender, FileExtractionStatusChangedEventArgs e)
    {
        LogOperationStatusChanged(opStatus2, "File extraction", e.RARFile.FilePath, e);
    }

    private void Manager_RARCompressionProgress(object? sender, RARCompressionProgressEventArgs e)
    {
        string text = $"Compressing {e.Process.InputDirectory} to {e.Process.OutputFilePath}{Environment.NewLine}" +
            $"Process file path: {e.Process.ProcessFilePath}{Environment.NewLine}" +
            $"Command line: {string.Join(" ", e.Process.CommandLineOptions)}";

        LogOperationProgressChanged(opStatus2, "RAR compression", text, e);
    }

    private void Manager_RARCompressionStatusChanged(object? sender, RARCompressionStatusChangedEventArgs e)
    {
        LogOperationStatusChanged(opStatus2, "RAR compression", e.Process.ProcessFilePath, e);
    }

    private void Manager_RARProcessOutput(object? sender, RARProcessDataEventArgs e)
    {
        //LogProcessData($"RAR process", e);
    }

    private void Manager_RARProcessStatusChanged(object? sender, RARProcessStatusChangedEventArgs e)
    {
        LogOperationStatusChanged(opStatus2, "RAR process", $"{e.Process.ProcessFilePath} {string.Join(" ", e.Process.CommandLineOptions)}", e);
    }

    private void Manager_BruteForceProgress(object? sender, BruteForceProgressEventArgs e)
    {
        string releaseName = Path.GetFileName(e.ReleaseDirectoryPath);
        string rarVersionDirectoryName = Path.GetFileName(e.RARVersionDirectoryPath);
        string text = $"Release: {releaseName}{Environment.NewLine}" +
            $"RAR version: {Manager.ParseRARVersion(rarVersionDirectoryName)}{Environment.NewLine}" +
            $"Command line arguments: {e.RARCommandLineArguments}";
        LogOperationProgressChanged(opStatus1, "Brute force", text, e);

        // Update status bar with progress info
        double percent = e.OperationSize > 0 ? (double)e.OperationProgressed / e.OperationSize * 100 : 0;
        int versionNum = Manager.ParseRARVersion(rarVersionDirectoryName);
        string versionStr = versionNum >= 100 ? $"{versionNum / 100}.{versionNum % 100:D2}" : $"{versionNum / 10}.{versionNum % 10}";

        // Calculate elapsed and estimated remaining time
        TimeSpan elapsed = DateTime.Now - e.StartDateTime;
        string elapsedStr = FormatTimeSpan(elapsed);
        string remainingStr = "--:--:--";

        if (e.OperationProgressed > 0 && e.OperationSize > 0)
        {
            double rate = e.OperationProgressed / elapsed.TotalSeconds;
            if (rate > 0)
            {
                double remainingSeconds = (e.OperationSize - e.OperationProgressed) / rate;
                TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);
                remainingStr = FormatTimeSpan(remaining);
            }
        }

        string progressText = $"RAR {versionStr} | Test {e.OperationProgressed:N0} of {e.OperationSize:N0} ({percent:F1}%) | Elapsed: {elapsedStr} | Remaining: {remainingStr}";
        UpdateStatusBar("Running", progressText);
    }

    private void Manager_BruteForceStatusChanged(object? sender, BruteForceStatusChangedEventArgs e)
    {
        LogOperationStatusChanged(opStatus1, "Brute force", null, e);
        UpdateStatusBar(e.NewStatus.ToString());
    }

    private void BtnWinRARDirectoryBrowse_Click(object? sender, EventArgs e)
    {
        GUIHelper.BrowseOpenDirectory(tbWinRARDirectory, "Select WinRAR directory");
    }

    private void BtnReleaseDirectoryBrowse_Click(object? sender, EventArgs e)
    {
        GUIHelper.BrowseOpenDirectory(tbReleaseDirectory, "Select release directory");
    }

    private void BtnVerificationFileBrowse_Click(object? sender, EventArgs e)
    {
        string? filePath = GUIHelper.BrowseOpenFile("Select verification file", "Verification files (*.sha1;*.sfv)|*.sha1;*.sfv|All files (*.*)|*.*");
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        tbVerificationFilePath.Text = filePath;
    }

    private void BtnTemporaryDirectoryBrowse_Click(object? sender, EventArgs e)
    {
        GUIHelper.BrowseOpenDirectory(tbOutputDirectory, "Select temporary directory");
    }

    private void BtnClearLog_Click(object? sender, EventArgs e)
    {
        rtbLogSystem.Clear();
        rtbLogPhase1.Clear();
        rtbLogPhase2.Clear();
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (Manager != null)
        {
            btnStart.Enabled = false;
            UpdateStatusBar("Stopping", "Cancelling operation...");
            Manager.Stop();
            return;
        }

        string winRARDirectory = tbWinRARDirectory.Text;
        if (string.IsNullOrEmpty(winRARDirectory))
        {
            GUIHelper.ShowError(tbWinRARDirectory, "Invalid WinRAR directory.");
            return;
        }

        if (!Directory.Exists(winRARDirectory))
        {
            GUIHelper.ShowError(tbWinRARDirectory, "WinRAR directory does not exists.");
            return;
        }

        string inputDirectory = tbReleaseDirectory.Text;
        if (string.IsNullOrEmpty(inputDirectory))
        {
            GUIHelper.ShowError(tbReleaseDirectory, "Invalid release directory.");
            return;
        }

        if (!Directory.Exists(inputDirectory))
        {
            GUIHelper.ShowError(tbReleaseDirectory, "Release directory does not exists.");
            return;
        }

        if (Directory.EnumerateDirectories(inputDirectory).Any() && OptionsForm.RAROptions.DirectoryTimestamps.Count == 0)
        {
            ModifiedDateWarningForm form = new();
            if (form.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }
        }

        string verificationFilePath = tbVerificationFilePath.Text;
        if (string.IsNullOrEmpty(verificationFilePath))
        {
            GUIHelper.ShowError(tbVerificationFilePath, "Invalid verification file path.");
            return;
        }

        if (!File.Exists(verificationFilePath))
        {
            GUIHelper.ShowError(tbVerificationFilePath, "Verification file does not exists.");
            return;
        }

        HashType? hashType = Path.GetExtension(verificationFilePath).ToLower() switch
        {
            ".sha1" => HashType.SHA1,
            ".sfv" => HashType.CRC32,
            _ => null
        };
        if (hashType == null)
        {
            GUIHelper.ShowError(tbVerificationFilePath, "Invalid verification file type.");
            return;
        }
        HashSet<string> hashes = ReadVerificationFile(verificationFilePath, hashType.Value);
        if (hashes.Count == 0)
        {
            GUIHelper.ShowError(tbVerificationFilePath, "No hashes found in verification file.");
            return;
        }

        string outputDirectory = tbOutputDirectory.Text;
        if (string.IsNullOrEmpty(outputDirectory))
        {
            GUIHelper.ShowError(tbOutputDirectory, "Invalid output directory.");
            return;
        }

        if (!Directory.Exists(outputDirectory))
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                GUIHelper.ShowError(tbOutputDirectory, $"Failed to create output directory:{Environment.NewLine}{ex.Message}");
                return;
            }
        }
        else
        {
            // Check if output directory has any contents
            bool hasFiles = Directory.EnumerateFileSystemEntries(outputDirectory).Any();
            if (hasFiles)
            {
                var result = MessageBox.Show(
                    $"The output directory is not empty:{Environment.NewLine}{Environment.NewLine}{outputDirectory}{Environment.NewLine}{Environment.NewLine}Its contents will be deleted before starting. Continue?",
                    "Output Directory Not Empty",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Cancel)
                {
                    return;
                }

                // Delete contents of output directory
                try
                {
                    foreach (string file in Directory.GetFiles(outputDirectory))
                    {
                        File.Delete(file);
                    }
                    foreach (string dir in Directory.GetDirectories(outputDirectory))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch (Exception ex)
                {
                    GUIHelper.ShowError(tbOutputDirectory, $"Failed to clean output directory:{Environment.NewLine}{ex.Message}");
                    return;
                }
            }
        }

        // Disable input controls but keep btnStart enabled so user can stop
        SetInputControlsEnabled(false);

        rtbLogSystem.Clear();
        rtbLogPhase1.Clear();
        rtbLogPhase2.Clear();
        _phase1HasUnread = false;
        _phase2HasUnread = false;
        tabPagePhase1.Text = Phase1TabTitle;
        tabPagePhase2.Text = Phase2TabTitle;
        tabControlLogs.SelectedTab = tabPageSystem;

        btnStart.Text = "Stop";

        UpdateStatusBar("Running", "Starting brute force...");
        StartElapsedTimer();

        Manager = new();

        Manager.DirectoryExtractionProgress += Manager_DirectoryExtractionProgress;
        Manager.DirectoryExtractionStatusChanged += Manager_DirectoryExtractionStatusChanged;
        Manager.FileExtractionProgress += Manager_FileExtractionProgress;
        Manager.FileExtractionStatusChanged += Manager_FileExtractionStatusChanged;
        Manager.RARCompressionProgress += Manager_RARCompressionProgress;
        Manager.RARCompressionStatusChanged += Manager_RARCompressionStatusChanged;
        Manager.RARProcessOutput += Manager_RARProcessOutput;
        Manager.RARProcessStatusChanged += Manager_RARProcessStatusChanged;
        Manager.BruteForceProgress += Manager_BruteForceProgress;
        Manager.BruteForceStatusChanged += Manager_BruteForceStatusChanged;

        OptionsForm.Toggle(false);
        string currentWorkingDirectory = Environment.CurrentDirectory;
        try
        {
            //await Manager.ExtractDirectoryAsync(winRARDirectory, rarInstallationsPath);

            opStatus1.Reset();
            opStatus2.Reset();

            // Set current working directory to output directory
            // Note: Manager.cs will copy files to outputDirectory/input/ as needed
            Environment.CurrentDirectory = outputDirectory;

            BruteForceOptions bruteforceOptions = new(winRARDirectory, inputDirectory, outputDirectory)
            {
                Hashes = hashes,
                RAROptions = OptionsForm.RAROptions,
                HashType = hashType.Value
            };

            bool success = await Manager.BruteForceRARVersionAsync(bruteforceOptions);

            StopElapsedTimer();

            if (success)
            {
                UpdateStatusBar("Success", "Match found!");
                MessageBox.Show("WinRAR version and command line parameters found!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log("WinRAR version and command line parameters found!");
            }
            else
            {
                UpdateStatusBar("No match", "Failed to find matching parameters");
                Log("Failed to find one or more RAR file(s) with a matching checksum for the given release.");
                RARChecksumNotFoundForm form = new();
                form.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            StopElapsedTimer();
            UpdateStatusBar("Error", ex.Message);
            Log(ex.ToString());
            GUIHelper.ShowError(this, $"Failed to start:{Environment.NewLine}{ex.Message}");
        }
        finally
        {
            Environment.CurrentDirectory = currentWorkingDirectory;
            Manager = null;
            OptionsForm.Toggle(true);
        }

        btnStart.Text = "Start";
        btnStart.Enabled = true;
        SetInputControlsEnabled(true);
    }

    /// <summary>
    /// Enables or disables input controls without affecting the Start/Stop button.
    /// </summary>
    private void SetInputControlsEnabled(bool enabled)
    {
        // Disable individual group boxes within gbInput, not gbInput itself
        // This keeps btnStart enabled so user can stop the operation
        foreach (Control control in gbInput.Controls)
        {
            if (control != btnStart)
            {
                control.Enabled = enabled;
            }
        }
    }

    private void TabControlLogs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (tabControlLogs.SelectedTab == tabPagePhase1 && _phase1HasUnread)
        {
            _phase1HasUnread = false;
            tabPagePhase1.Text = Phase1TabTitle;
        }
        else if (tabControlLogs.SelectedTab == tabPagePhase2 && _phase2HasUnread)
        {
            _phase2HasUnread = false;
            tabPagePhase2.Text = Phase2TabTitle;
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        logFlushTimer?.Stop();
        logFlushTimer?.Dispose();
        logFlushTimer = null;

        StopElapsedTimer();
        Manager?.Stop();
    }

    private void Log_Logged(object? sender, LogEventArgs e)
    {
        Log(e.Message, e.Target);
    }

    private static HashSet<string> ReadVerificationFile(string verificationFilePath, HashType type)
    {
        HashSet<string> hashes = [];

        switch (type)
        {
            case HashType.SHA1:
                {
                    SHA1File sha1File = SHA1File.ReadFile(verificationFilePath);
                    foreach (SHA1FileEntry entry in sha1File.Entries)
                    {
                        if (!hashes.Contains(entry.SHA1))
                        {
                            hashes.Add(entry.SHA1);
                        }
                    }
                }
                break;

            case HashType.CRC32:
                {
                    SFVFile sfvFile = SFVFile.ReadFile(verificationFilePath);
                    foreach (SFVFileEntry entry in sfvFile.Entries)
                    {
                        if (!hashes.Contains(entry.CRC))
                        {
                            hashes.Add(entry.CRC);
                        }
                    }
                }
                break;
        }

        return hashes;
    }

    private void LogOperationStatusChanged(OperationProgressStatusUserControl control, string operation, string? text, OperationStatusChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            try
            {
                Invoke((MethodInvoker)delegate { LogOperationStatusChanged(control, operation, text, e); });
            }
            catch
            {
            }
            return;
        }

        control.OperationStatusChanged(operation, text, e);
    }

    private void LogOperationProgressChanged(OperationProgressStatusUserControl control, string operation, string? status, OperationProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            try
            {
                Invoke((MethodInvoker)delegate { LogOperationProgressChanged(control, operation, status, e); });
            }
            catch
            {
            }
            return;
        }

        control.OperationProgressChanged(operation, status, e);
    }

    private void LogProcessData(string operation, ProcessDataEventArgs e)
    {
        if (InvokeRequired)
        {
            try
            {
                Invoke((MethodInvoker)delegate { LogProcessData(operation, e); });
            }
            catch
            {
            }
            return;
        }

        string line = $"[{operation}]{(e.Error ? "[ERROR]" : string.Empty)} {e.Data}";

        WinRARRed.Log.Write(this, line);
    }

    // Log colors for different levels
    private static readonly Color ColorTimestamp = Color.FromArgb(128, 128, 128);  // Gray
    private static readonly Color ColorDebug = Color.FromArgb(128, 128, 128);      // Gray
    private static readonly Color ColorInfo = Color.FromArgb(212, 212, 212);       // Light gray (default)
    private static readonly Color ColorWarning = Color.FromArgb(255, 193, 7);      // Yellow/Orange
    private static readonly Color ColorError = Color.FromArgb(244, 67, 54);        // Red
    private static readonly Color ColorSuccess = Color.FromArgb(76, 175, 80);      // Green

    private void Log(string text) => Log(text, LogTarget.System);

    private void Log(string text, LogTarget target)
    {
        // Determine log color based on level prefix and content
        Color logColor = ColorInfo;
        if (text.Contains("[DEBUG]"))
        {
            logColor = ColorDebug;
        }
        else if (text.Contains("[WARNING]"))
        {
            logColor = ColorWarning;
        }
        else if (text.Contains("[ERROR]") || text.Contains("[FATAL]"))
        {
            logColor = ColorError;
        }
        else if (text.Contains("MATCH FOUND") || text.Contains("SUCCESS"))
        {
            logColor = ColorSuccess;
        }

        // Queue the log entry for batched processing
        logBuffer.Enqueue(new LogEntry(text, target, DateTime.Now, logColor));
    }

    private void FlushLogBuffer()
    {
        if (logBuffer.IsEmpty)
            return;

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke((MethodInvoker)FlushLogBuffer);
            }
            catch
            {
            }
            return;
        }

        // Collect entries to process (up to max per flush)
        var systemEntries = new List<LogEntry>();
        var phase1Entries = new List<LogEntry>();
        var phase2Entries = new List<LogEntry>();
        int processed = 0;

        while (processed < MaxEntriesPerFlush && logBuffer.TryDequeue(out var entry))
        {
            switch (entry.Target)
            {
                case LogTarget.Phase1:
                    phase1Entries.Add(entry);
                    break;
                case LogTarget.Phase2:
                    phase2Entries.Add(entry);
                    break;
                default:
                    systemEntries.Add(entry);
                    break;
            }
            processed++;
        }

        try
        {
            // Process each target's entries in batch
            if (systemEntries.Count > 0)
                FlushEntriesToRichTextBox(rtbLogSystem, systemEntries);
            if (phase1Entries.Count > 0)
            {
                FlushEntriesToRichTextBox(rtbLogPhase1, phase1Entries);
                if (tabControlLogs.SelectedTab != tabPagePhase1)
                {
                    _phase1HasUnread = true;
                    tabPagePhase1.Text = Phase1TabTitle + " *";
                }
            }
            if (phase2Entries.Count > 0)
            {
                FlushEntriesToRichTextBox(rtbLogPhase2, phase2Entries);
                if (tabControlLogs.SelectedTab != tabPagePhase2)
                {
                    _phase2HasUnread = true;
                    tabPagePhase2.Text = Phase2TabTitle + " *";
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void FlushEntriesToRichTextBox(RichTextBox rtbLog, List<LogEntry> entries)
    {
        if (entries.Count == 0)
            return;

        // Check if log has exceeded 1000 lines
        if (rtbLog.Lines.Length > 1000)
        {
            rtbLog.Clear();
        }

        bool autoScroll = cbAutoScroll.Checked;
        int firstVisibleChar = 0;
        int selectionStart = 0;
        int selectionLength = 0;

        // Suspend drawing
        SendMessage(rtbLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        IntPtr eventMask = SendMessage(rtbLog.Handle, EM_GETEVENTMASK, IntPtr.Zero, IntPtr.Zero);

        if (!autoScroll)
        {
            firstVisibleChar = rtbLog.GetCharIndexFromPosition(new Point(0, 0));
            selectionStart = rtbLog.SelectionStart;
            selectionLength = rtbLog.SelectionLength;
        }

        // Append all entries
        foreach (var entry in entries)
        {
            string dateTime = entry.Timestamp.ToString("MM/dd/yyyy HH:mm:ss.fff");
            string[] lines = entry.Text.Split(Environment.NewLine);
            AppendColoredLog(rtbLog, dateTime, lines, entry.Color);
        }

        if (!autoScroll)
        {
            // Restore scroll position
            rtbLog.SelectionStart = selectionStart;
            rtbLog.SelectionLength = selectionLength;
            rtbLog.Select(firstVisibleChar, 0);
            rtbLog.ScrollToCaret();
            rtbLog.SelectionStart = selectionStart;
            rtbLog.SelectionLength = selectionLength;
        }

        // Resume drawing
        SendMessage(rtbLog.Handle, EM_SETEVENTMASK, IntPtr.Zero, eventMask);
        SendMessage(rtbLog.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);

        if (autoScroll)
        {
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        rtbLog.Invalidate();
    }

    private void AppendColoredLog(RichTextBox rtbLog, string dateTime, string[] lines, Color logColor)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            // Append timestamp in gray
            if (i == 0)
            {
                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionColor = ColorTimestamp;
                rtbLog.AppendText($"[{dateTime}] ");
            }
            else
            {
                rtbLog.SelectionStart = rtbLog.TextLength;
                rtbLog.SelectionColor = ColorTimestamp;
                rtbLog.AppendText($"{"",26}");
            }

            // Append the log content in the appropriate color
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = logColor;
            rtbLog.AppendText(lines[i] + Environment.NewLine);
        }

        // Reset selection color
        rtbLog.SelectionColor = rtbLog.ForeColor;
    }

    private void UpdateStatusBar(string status, string? progress = null)
    {
        if (InvokeRequired)
        {
            try
            {
                Invoke((MethodInvoker)delegate { UpdateStatusBar(status, progress); });
            }
            catch
            {
            }
            return;
        }

        tsslStatus.Text = status;
        if (progress != null)
        {
            tsslProgress.Text = progress;
        }
    }

    private void StartElapsedTimer()
    {
        bruteForceStartTime = DateTime.Now;
        elapsedTimer?.Dispose();
        elapsedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        elapsedTimer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - bruteForceStartTime;
            tsslElapsed.Text = FormatTimeSpan(elapsed);
        };
        elapsedTimer.Start();
        tsslElapsed.Text = "00:00";
    }

    private void StopElapsedTimer()
    {
        elapsedTimer?.Stop();
        elapsedTimer?.Dispose();
        elapsedTimer = null;
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
