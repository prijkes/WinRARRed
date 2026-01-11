using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using WinRARRed.IO;

namespace WinRARRed.Diagnostics
{
    public sealed partial class RARProcess
    {
        public event EventHandler<ProcessDataEventArgs>? ProcessOutput;

        public event EventHandler<OperationStatusChangedEventArgs>? ProcessStatusChanged;

        public event EventHandler<FileCompressionOperationProgressEventArgs>? CompressionProgress;

        public event EventHandler<FileCompressionOperationStatusChangedEventArgs>? CompressionStatusChanged;

        public string ProcessFilePath { get; private set; }

        public string InputDirectory { get; private set; }

        public string OutputFilePath { get; private set; }

        public string[] CommandLineOptions { get; private set; }

        private struct ArchiveItem
        {
            public string FileName { get; set; }

            public int Progress { get; set; }

            public bool Done { get; set; }
        };

        private struct Archive
        {
            public string ArchiveFileName { get; set; }

            public List<ArchiveItem> ArchiveItems { get; set; }

            public Archive()
            {
                ArchiveFileName = string.Empty;
                ArchiveItems = [];
            }
        }

        private Archive ArchiveFile = new();

        private static readonly Encoding OutputEncoding = GetOutputEncoding();

        // Matches lines with filename and percentage, language-independent
        // Examples: "Adding    .\file.txt    0%" or "Ajout    .\file.txt    100%  OK"
        // Captures any filename pattern between the action word and progress columns.
        [GeneratedRegex(@"^\s*\S+\s+(?<filename>.+?)\s{2,}(?<progress>\d+)%", RegexOptions.Compiled)]
        private static partial Regex GeneratedProgressRegex();
        private static readonly Regex ProgressRegex = GeneratedProgressRegex();

        [GeneratedRegex(@"^\s*\S+\s+(?<filename>.+?)\s{2,}OK\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex GeneratedOkRegex();
        private static readonly Regex OkRegex = GeneratedOkRegex();

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
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            // Start process
            try
            {
                Log.Debug(this, $"Starting RAR process: {ProcessFilePath}");
                Log.Debug(this, $"Working Directory: {InputDirectory}");
                Log.Debug(this, $"Output File: {OutputFilePath}");
                Log.Debug(this, $"Arguments: {string.Join(" ", CommandLineOptions)}");
                
                FireProcessStatusChanged(new(OperationStatus.Running));
                FireCompressionStatusChanged(new(OperationStatus.Running, OutputFilePath));

                var startTime = DateTime.Now;

                // Create custom streams to capture output as it arrives (including \r updates)
                var stdOutStream = new OutStream(data => HandleOutputData(data, startTime), OutputEncoding);
                var stdErrStream = new OutStream(HandleErrorData, OutputEncoding);

                var result = await Cli.Wrap(ProcessFilePath)
                    .WithArguments(CommandLineOptions)
                    .WithWorkingDirectory(InputDirectory)
                    .WithStandardOutputPipe(PipeTarget.ToStream(stdOutStream))
                    .WithStandardErrorPipe(PipeTarget.ToStream(stdErrStream))
                    .WithValidation(CommandResultValidation.None) // Manually handle exit codes
                    .ExecuteAsync(cancellationToken);
                
                int exitCode = result.ExitCode;
                var elapsed = DateTime.Now - startTime;

                Log.Information(this, $"RAR process completed. Exit Code: {exitCode}, Duration: {elapsed.TotalSeconds:F2}s, Output: {OutputFilePath}");

                OperationCompletionStatus completionStatus = exitCode == 0 ? OperationCompletionStatus.Success : OperationCompletionStatus.Error;

                FireProcessStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, completionStatus));
                FireCompressionStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, completionStatus, OutputFilePath));

                return exitCode;
            }
            catch (OperationCanceledException)
            {
                Log.Warning(this, $"RAR process cancelled: {OutputFilePath}");
                FireProcessStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, OperationCompletionStatus.Cancelled));
                FireCompressionStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, OperationCompletionStatus.Cancelled, OutputFilePath));
                // CliWrap handles getting killed on cancellation automatically
            }
            catch (Exception ex)
            {
                Log.Error(this, ex, $"RAR process failed: {OutputFilePath}");
                FireCompressionStatusChanged(new(OperationStatus.Running, OperationStatus.Completed, OperationCompletionStatus.Error, OutputFilePath));
                throw;
            }

            return 1;
        }

        // Custom stream class to capture data as it arrives (byte-by-byte)
        private class OutStream : Stream
        {
            private readonly StringBuilder _buffer = new();
            private readonly Action<string> _onData;
            private readonly Encoding _encoding;

            public OutStream(Action<string> onData, Encoding encoding)
            {
                _onData = onData;
                _encoding = encoding;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                // Decode bytes to string
                string data = _encoding.GetString(buffer, offset, count);
                
                // Process each character
                foreach (char c in data)
                {
                    if (c == '\n')
                    {
                        // Newline - flush current buffer and notify
                        _onData(_buffer.ToString());
                        _buffer.Clear();
                    }
                    else if (c == '\r')
                    {
                        // Carriage return - notify with current buffer but don't clear
                        // This allows progress updates on the same line
                        _onData(_buffer.ToString());
                        _buffer.Clear();
                    }
                    else
                    {
                        // Regular character - append to buffer
                        _buffer.Append(c);
                    }
                }
            }
        }


        private void HandleOutputData(string data, DateTime startTime)
        {
            ProcessOutput?.Invoke(this, new(data));

            if (!string.IsNullOrEmpty(data))
            {
                ParseProcessOutputData(data, startTime);
            }
        }

        private void HandleErrorData(string data)
        {
            ProcessOutput?.Invoke(this, new(data, true));
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

            if (string.IsNullOrEmpty(ArchiveFile.ArchiveFileName))
            {
                // Use the OutputFilePath property which is already known from constructor
                // This is reliable across all languages and archive extensions (.rar, .r00, .r01, etc.)
                ArchiveFile.ArchiveFileName = OutputFilePath;
            }

            // Parse the current line for progress information.
            // Lines like: Adding    .\ali.g-dmt.avi                                          0%  0%  0%
            // Or done:    Adding    .\file name.txt  OK
            Match lineMatch = ProgressRegex.Match(output);
            if (lineMatch.Success)
            {
                string filename = NormalizeOutputFileName(lineMatch.Groups["filename"].Value);
                string progressStr = lineMatch.Groups["progress"].Value;

                if (!string.IsNullOrEmpty(filename) && int.TryParse(progressStr, out int progress))
                {
                    // Find or create the archive item for this file
                    int itemIndex = ArchiveFile.ArchiveItems.FindIndex(item => item.FileName == filename);
                    
                    if (itemIndex >= 0)
                    {
                        // Update existing item
                        var item = ArchiveFile.ArchiveItems[itemIndex];
                        item.Progress = progress;
                        item.Done = progress >= 100;
                        ArchiveFile.ArchiveItems[itemIndex] = item;
                    }
                    else
                    {
                        // Add new item
                        ArchiveFile.ArchiveItems.Add(new ArchiveItem
                        {
                            FileName = filename,
                            Progress = progress,
                            Done = progress >= 100
                        });
                    }

                    // Fire overall compression progress using the latest progress value
                    string filePath = Path.IsPathRooted(filename) ? filename : Path.Combine(InputDirectory, filename);
                    FireCompressionProgress(new(100, progress, startDateTime, filePath));
                }
            }
            else if (output.Contains("OK", StringComparison.OrdinalIgnoreCase))
            {
                // Handle lines like: Adding    .\file.txt  OK (language-independent)
                // Use regex to extract filename from "OK" completion lines
                Match okMatch = OkRegex.Match(output);
                if (okMatch.Success)
                {
                    string filename = NormalizeOutputFileName(okMatch.Groups["filename"].Value);
                    
                    // Mark this file as done
                    int itemIndex = ArchiveFile.ArchiveItems.FindIndex(item => item.FileName == filename);
                    
                    if (itemIndex >= 0)
                    {
                        var item = ArchiveFile.ArchiveItems[itemIndex];
                        item.Progress = 100;
                        item.Done = true;
                        ArchiveFile.ArchiveItems[itemIndex] = item;
                    }
                    else
                    {
                        // File completed without progress updates (small file)
                        ArchiveFile.ArchiveItems.Add(new ArchiveItem
                        {
                            FileName = filename,
                            Progress = 100,
                            Done = true
                        });
                    }

                    string filePath = Path.IsPathRooted(filename) ? filename : Path.Combine(InputDirectory, filename);
                    FireCompressionProgress(new(100, 100, startDateTime, filePath));
                }
            }
        }

        private static string NormalizeOutputFileName(string filename)
        {
            string trimmed = filename.Trim();
            if (trimmed.StartsWith(".\\", StringComparison.Ordinal) || trimmed.StartsWith("./", StringComparison.Ordinal))
            {
                return trimmed[2..];
            }

            return trimmed;
        }

        private static Encoding GetOutputEncoding()
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8;
            }
            catch (NotSupportedException)
            {
                return Encoding.UTF8;
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
