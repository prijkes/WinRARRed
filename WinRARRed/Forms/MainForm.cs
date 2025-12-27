using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WinRARRed.Controls;
using WinRARRed.Cryptography;
using WinRARRed.Diagnostics;
using WinRARRed.IO;

namespace WinRARRed.Forms
{
    public partial class MainForm : Form
    {
        private Manager? Manager = null;

        private readonly SettingsOptionsForm OptionsForm;

        public MainForm()
        {
            InitializeComponent();

            OptionsForm = new();
            OptionsForm.Hide();

            WinRARRed.Log.Logged += Log_Logged;

            tsmiFileExit.Click += TsmiFileExit_Click;
            tsmiViewCommandLines.Click += TsmiViewCommandLines_Click;
            tsmiSettingsOptions.Click += TsmiSettingsOptions_Click;

            btnWinRARDirectoryBrowse.Click += BtnWinRARDirectoryBrowse_Click;
            btnReleaseDirectoryBrowse.Click += BtnReleaseDirectoryBrowse_Click;
            btnVerificationFileBrowse.Click += BtnVerificationFileBrowse_Click;
            btnTemporaryDirectoryBrowse.Click += BtnTemporaryDirectoryBrowse_Click;
            btnClearLog.Click += BtnClearLog_Click;
            btnStart.Click += BtnStart_Click;

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
        }

        private void Manager_BruteForceStatusChanged(object? sender, BruteForceStatusChangedEventArgs e)
        {
            LogOperationStatusChanged(opStatus1, "Brute force", null, e);
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
            tbLog.Clear();
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (Manager != null)
            {
                btnStart.Enabled = false;
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

            if (Directory.EnumerateDirectories(inputDirectory).Any())
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
            if (!hashes.Any())
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

            gbInput.Enabled = false;

            tbLog.Clear();

            btnStart.Text = "Stop";

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

                // Copy release files recursively
                CopyFilesRecursively(inputDirectory, outputDirectory);

                // Set current working directory to base directory of release.
                // rar.exe won't properly add directories otherwise (it will add the whole path instead...)
                Environment.CurrentDirectory = outputDirectory;

                BruteForceOptions bruteforceOptions = new(winRARDirectory, outputDirectory, outputDirectory)
                {
                    Hashes = hashes,
                    RAROptions = OptionsForm.RAROptions,
                    HashType = hashType.Value
                };

                bool success = await Manager.BruteForceRARVersionAsync(bruteforceOptions);

                // Restore current working directory.
                Environment.CurrentDirectory = currentWorkingDirectory;

                if (success)
                {
                    MessageBox.Show("WinRAR version and command line parameters found!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Log("WinRAR version and command line parameters found!");
                }
                else
                {
                    Log("Failed to find one or more RAR file(s) with a matching checksum for the given release.");
                    RARChecksumNotFoundForm form = new();
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                GUIHelper.ShowError(this, $"Failed to start:{Environment.NewLine}{ex.Message}");
            }
            finally
            {
                Manager = null;
                OptionsForm.Toggle(true);
            }

            btnStart.Text = "Start";

            btnStart.Enabled = true;

            gbInput.Enabled = true;
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Manager?.Stop();
        }

        private void Log_Logged(object? sender, string e)
        {
            Log(e);
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

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string dstPath = newPath.Replace(sourcePath, targetPath);
                if (!File.Exists(dstPath))
                {
                    File.Copy(newPath, dstPath);
                }
            }
        }

        private void Log(string text)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke((MethodInvoker)delegate { Log(text); });
                }
                catch
                {
                }
                return;
            }

            StringBuilder strBuilder = new();
            string dateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
            string[] lines = text.Split(Environment.NewLine);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    strBuilder.Append($"[{dateTime}] ");
                }
                else
                {
                    strBuilder.Append($"{"",40}");
                }

                string line = lines[i];
                strBuilder.AppendLine(line);
            }

            try
            {
                tbLog.AppendText(strBuilder.ToString());
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
