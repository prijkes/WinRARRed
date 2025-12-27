using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WinRARRed.IO;

namespace WinRARRed.Diagnostics
{
    public sealed partial class RARProcess : IDisposable
    {
        public event EventHandler<ProcessDataEventArgs>? ProcessOutput;

        public event EventHandler<OperationStatusChangedEventArgs>? ProcessStatusChanged;

        public event EventHandler<FileCompressionOperationProgressEventArgs>? CompressionProgress;

        public event EventHandler<FileCompressionOperationStatusChangedEventArgs>? CompressionStatusChanged;

        public string ProcessFilePath { get; private set; }

        public string InputDirectory { get; private set; }

        public string OutputFilePath { get; private set; }

        public string[] CommandLineOptions { get; private set; }

        private Process Process { get; set; }

        private StringBuilder StringBuilder { get; set; } = new();

        private struct ArchiveItem
        {
            public string FileName { get; set; }

            public int Progress { get; set; }

            public bool Done { get; set; }
        };

        private struct Archive(string archiveFileName)
        {
            public string ArchiveFileName { get; set; } = archiveFileName;

            public List<ArchiveItem> ArchiveItems { get; set; } = [];
        }

        private Archive ArchiveFile = new();

        [GeneratedRegex("[\\s^]*(?<filename>[A-Z]{1}:.+\\.[^\\s.]*)[\\b\\s]+(?<progress>[0-9]+)%", RegexOptions.Compiled)]
        private static partial Regex GeneratedRegex();
        private static readonly Regex ProgressRegex = GeneratedRegex();

        public RARProcess(string processFilePath, string inputDirectory, string outputFilePath, IEnumerable<string> commandLineOptions)
        {
            if (!File.Exists(processFilePath))
            {
                throw new FileNotFoundException(processFilePath);
            }

            ProcessFilePath = processFilePath;
            InputDirectory = inputDirectory;
            OutputFilePath = outputFilePath;

            List<string> options =
            [
                .. commandLineOptions,
                // Output file
                outputFilePath,

                // Input directory/files
                // RAR 2.00 does not like just '.' as input directory
                ".\\*"
            ];

            // Save new options
            CommandLineOptions = [.. options];

            Process = new()
            {
                StartInfo = new()
                {
                    FileName = ProcessFilePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = InputDirectory
                },
                EnableRaisingEvents = true
            };

            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.ErrorDataReceived += Process_ErrorDataReceived;
            Process.Exited += Process_Exited;
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            // Command line options
            foreach (string commandLineOption in CommandLineOptions)
            {
                Process.StartInfo.ArgumentList.Add(commandLineOption);
            }

            // Start process
            try
            {
                Process.Start();

                FireProcessStatusChanged(new(OperationStatus.Running));

                // Asynchronously read the standard output of the spawned process.
                // This raises OutputDataReceived and ErrorDataReceived events for each line of output.
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                // Wait for it to exit
                await Process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                FireProcessStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, OperationCompletionStatus.Cancelled));

                //Process.Kill();
            }
            catch
            {
                throw;
            }

            //return Process.ExitCode;
            return 1;
        }

        public void Dispose()
        {
            Process.OutputDataReceived -= Process_OutputDataReceived;
            Process.ErrorDataReceived -= Process_ErrorDataReceived;
            Process.Exited -= Process_Exited;

            Process?.Dispose();
        }

        private void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (sender is not Process process)
            {
                return;
            }

            ProcessOutput?.Invoke(this, new(e.Data));

            if (!string.IsNullOrEmpty(e.Data))
            {
                ParseProcessOutputData(e.Data, process.StartTime);
            }
        }

        private void Process_ErrorDataReceived(object? sender, DataReceivedEventArgs e)
        {
            ProcessOutput?.Invoke(this, new(e.Data, true));
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            if (sender is not Process process)
            {
                return;
            }

            FireProcessStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, process.ExitCode switch
            {
                0 => OperationCompletionStatus.Success,
                _ => OperationCompletionStatus.Error
            }));
        }

        /*
            G:\Temp\temp>G:\WinRAR2\winrar-x64-400\rar.exe a -r -ds -s- -m0 -tsm4 -tsc0 -tsa0 G:\Temp\temp\archived-winrar-x64-400-a-r-ds-s--m0-tsm4-tsc0-tsa0.rar .\*

            RAR 4.00   Copyright (c) 1993-2011 Alexander Roshal   2 Mar 2011
            Shareware version         Type RAR -? for help

            Evaluation copy. Please register.

            Creating archive G:\Temp\temp\archived-winrar-x64-400-a-r-ds-s--m0-tsm4-tsc0-tsa0.rar

            Adding    .\SW_DVD5_Proofing_Tools_2016_64Bit_MultiLang_ComplKit_MLF_X20-42861.ISO  OK
            Adding    .\sw_dvd5_proofing_tools_2016_64bit_multilang_complkit_mlf_x20-42861.r12  OK
            Done
        */
        private void ParseProcessOutputData(string output, DateTime startDateTime)
        {
            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            StringBuilder.Append(output);

            string s = StringBuilder.ToString();

            if (string.IsNullOrEmpty(ArchiveFile.ArchiveFileName))
            {
                // Try parsing current file
                // Creating archive G:\Temp\temp\archived-winrar-x64-401-a-r-ds-s--m0-tsm4-tsc0-tsa0.rar
                Match m = ProgressRegex.Match(s);
                if (!m.Success)
                {
                    // Can't do anything
                    return;
                }

                ArchiveFile .ArchiveFileName = m.Groups["filename"].Value;
            }

            MatchCollection progressMatches = ProgressRegex.Matches(s);
            foreach (Match m in progressMatches.Cast<Match>())
            {
                string progressStr = m.Groups["progress"].Value;
                if (!int.TryParse(progressStr, out int progress))
                {
                    continue;
                }

                FireCompressionProgress(new(100, progress, startDateTime, string.Empty));
            }
        }

        private void FireProcessStatusChanged(OperationStatusChangedEventArgs e)
        {
            ProcessStatusChanged?.Invoke(this, e);
        }

        private void FireCompressionStatusChanged(FileCompressionOperationStatusChangedEventArgs e)
        {
            CompressionStatusChanged?.Invoke(this, e);
        }

        private void FireCompressionProgress(FileCompressionOperationProgressEventArgs e)
        {
            if (e.OperationProgressed == 100)
            {
                // Current file progressed to 100%
                //CurrentFileCreationFilePath = null;
            }

            CompressionProgress?.Invoke(this, e);
        }
    }
}
