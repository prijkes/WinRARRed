using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinRARRed.Cryptography;
using WinRARRed.Diagnostics;
using WinRARRed.IO;

namespace WinRARRed
{
    public partial class Manager
    {
        public event EventHandler<OperationProgressEventArgs>? DirectoryExtractionProgress;

        public event EventHandler<OperationStatusChangedEventArgs>? DirectoryExtractionStatusChanged;

        public event EventHandler<FileExtractionProgressEventArgs>? FileExtractionProgress;

        public event EventHandler<FileExtractionStatusChangedEventArgs>? FileExtractionStatusChanged;

        public event EventHandler<RARProcessDataEventArgs>? RARProcessOutput;

        public event EventHandler<RARProcessStatusChangedEventArgs>? RARProcessStatusChanged;

        public event EventHandler<RARCompressionProgressEventArgs>? RARCompressionProgress;

        public event EventHandler<RARCompressionStatusChangedEventArgs>? RARCompressionStatusChanged;

        public event EventHandler<BruteForceProgressEventArgs>? BruteForceProgress;

        public event EventHandler<BruteForceStatusChangedEventArgs>? BruteForceStatusChanged;

        public BruteForceOptions? BruteForceOptions { get; private set; }

        private readonly CancellationTokenSource CancellationTokenSource = new();

        [GeneratedRegex("(?:win)?(?:rar|wr)(?:-x64|-x32)?-?(\\d+)(?:b\\d+)?", RegexOptions.IgnoreCase, "ja-JP")]
        private static partial Regex GeneratedRARVersionRegex();
        private readonly static Regex RARVersionRegex = GeneratedRARVersionRegex();

        public static int ParseRARVersion(string rarVersionDirectoryName)
        {
            Match versionMatch = RARVersionRegex.Match(rarVersionDirectoryName);
            if (!versionMatch.Success)
            {
                throw new FormatException($"WinRAR version not found in directory name:{Environment.NewLine}{rarVersionDirectoryName}");
            }

            string versionNumberStr = versionMatch.Groups[1].Value;
            if (!int.TryParse(versionNumberStr, out int versionNumber))
            {
                throw new InvalidDataException($"WinRAR version found in directory name is invalid:{Environment.NewLine}{versionNumberStr}");
            }

            return versionNumber switch
            {
                < 100 => versionNumber * 10,
                _ => versionNumber
            };
        }

        public static RARArchiveVersion ParseRARArchiveVersion(RARCommandLineArgument[] commandLineArguments, int version)
        {
            RARCommandLineArgument? archiveVersionCommandLine = commandLineArguments.FirstOrDefault(a => a.Argument == "-ma4" || a.Argument == "-ma5");
            if (archiveVersionCommandLine != null)
            {
                return archiveVersionCommandLine.Argument switch
                {
                    "-ma4" => RARArchiveVersion.RAR4,
                    "-ma5" => RARArchiveVersion.RAR5,
                    _ => throw new IndexOutOfRangeException($"RAR archive version command line argument out of range: {archiveVersionCommandLine.Argument}")
                };
            }

            return version switch
            {
                < 500 => RARArchiveVersion.RAR4,
                >= 500 => RARArchiveVersion.RAR5
            };
        }

        public async Task ExtractDirectoryAsync(string inputDirectory, string outputDirectory)
        {
            OperationCompletionStatus completionStatus = OperationCompletionStatus.Success;

            OperationStatusChangedEventArgs status = new(OperationStatus.Running);
            FireDirectoryExtractionStatusChanged(status);

            DateTime startDateTime = DateTime.Now;

            string[] rarFiles = Directory.GetFiles(inputDirectory, "*.*");
            for ( int i = 0; i < rarFiles.Length; i++)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                string rarFile = rarFiles[i];
                string winrarOutputDirectory = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(rarFile));
                if (!Directory.Exists(winrarOutputDirectory))
                {
                    Directory.CreateDirectory(winrarOutputDirectory);

                    if (!await ExtractRARFileAsync(rarFile, winrarOutputDirectory, CancellationTokenSource.Token))
                    {
                        completionStatus = OperationCompletionStatus.Error;

                        Directory.Delete(winrarOutputDirectory, true);
                    }
                }

                OperationProgressEventArgs progress = new(rarFiles.Length, i + 1, startDateTime);
                FireDirectoryExtractionProgress(progress);
            }

            status = new(OperationStatus.Running, OperationStatus.Completed, CancellationTokenSource.IsCancellationRequested ? OperationCompletionStatus.Cancelled : completionStatus);
            FireDirectoryExtractionStatusChanged(status);
        }

        public async Task<bool> BruteForceRARVersionAsync(BruteForceOptions options)
        {
            BruteForceOptions = options;

            DateTime bruteForceStartDateTime = DateTime.Now;

            BruteForceStatusChangedEventArgs status = new(OperationStatus.Running);
            FireBruteForceStatusChanged(status);

            string[] rarVersionDirectories = Directory.GetDirectories(options.RARInstallationsDirectoryPath);
            if (!rarVersionDirectories.Any())
            {
                Log.Write(this, "No RAR executables found in WinRAR directory or sub directories");
                return false;
            }

            int totalProgressSize = CalculateBruteForceProgressSize(options);
            int currentProgress = 0;

            DirectoryInfo directoryInfo = new(options.ReleaseDirectoryPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

            // Save file attributes
            Dictionary<FileInfo, FileAttributes> fileInfoAttributes = fileInfos.Select(f => new KeyValuePair<FileInfo, FileAttributes>(f, f.Attributes)).ToDictionary(f => f.Key, f => f.Value);

            // Save file hash
            HashSet<string> fileHashes = [];

            bool found = false;
            for (int a = 0; a < (options.RAROptions.SetFileArchiveAttribute == CheckState.Checked ? 2 : 1) && !found; a++)
            {
                if (options.RAROptions.SetFileArchiveAttribute != CheckState.Unchecked)
                {
                    if (a == 0)
                    {
                        // Set archive attribute on first run
                        foreach (FileInfo fileInfo in fileInfos)
                        {
                            fileInfo.Attributes |= FileAttributes.Archive;
                            Log.Write(this, $"Added {nameof(FileAttributes.Archive)} attribute to {fileInfo}");
                        }
                    }
                    else
                    {
                        // Remove archive attribute on second run
                        foreach (FileInfo fileInfo in fileInfos)
                        {
                            fileInfo.Attributes &= ~FileAttributes.Archive;
                            Log.Write(this, $"Removed {nameof(FileAttributes.Archive)} attribute from {fileInfo}");
                        }
                    }
                }

                for (int b = 0; b < (options.RAROptions.SetFileNotContentIndexedAttribute == CheckState.Checked ? 2 : 1) && !found; b++)
                {
                    if (options.RAROptions.SetFileNotContentIndexedAttribute != CheckState.Unchecked)
                    {
                        if (b == 0)
                        {
                            // Set not content indexed attribute on first run
                            foreach (FileInfo fileInfo in fileInfos)
                            {
                                fileInfo.Attributes |= FileAttributes.NotContentIndexed;
                                Log.Write(this, $"Added {nameof(FileAttributes.NotContentIndexed)} attribute to {fileInfo}");
                            }
                        }
                        else
                        {
                            // Remove not content indexed attribute on second run
                            foreach (FileInfo fileInfo in fileInfos)
                            {
                                fileInfo.Attributes &= ~FileAttributes.NotContentIndexed;
                                Log.Write(this, $"Removed {nameof(FileAttributes.NotContentIndexed)} attribute from {fileInfo}");
                            }
                        }
                    }

                    for (int i = 0; i < rarVersionDirectories.Length && !found; i++)
                    {
                        string rarVersionDirectoryPath = rarVersionDirectories[i];
                        if (CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        string rarExeFilePath = Path.Combine(rarVersionDirectoryPath, "rar.exe");
                        if (!File.Exists(rarExeFilePath))
                        {
                            Log.Write(this, $"rar.exe not found in {rarVersionDirectoryPath}");
                            continue;
                        }

                        string rarVersionDirectoryName = Path.GetFileName(rarVersionDirectoryPath);
                        int version = ParseRARVersion(rarVersionDirectoryName);
                        if (!options.RAROptions.RARVersions.Any(r => r.InRange(version)))
                        {
                            continue;
                        }

                        for (int j = 0; j < options.RAROptions.CommandLineArguments.Count; j++)
                        {
                            RARCommandLineArgument[] commandLineArguments = options.RAROptions.CommandLineArguments[j];
                            if (CancellationTokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            RARArchiveVersion archiveVersion = ParseRARArchiveVersion(commandLineArguments, version);

                            // Filter arguments by RAR version and RAR archive version
                            IEnumerable<string> filteredArguments = commandLineArguments.Where(
                                a => version >= a.MinimumVersion &&
                                (!a.ArchiveVersion.HasValue || a.ArchiveVersion.Value.HasFlag(archiveVersion))
                            ).Select(a => a.Argument);

                            string joinedArguments = string.Join("", filteredArguments);
                            string archiveAttribute = options.RAROptions.SetFileArchiveAttribute != CheckState.Unchecked && a == 0 ? "archived-" : string.Empty;
                            string notContentIndexedAttribute = options.RAROptions.SetFileNotContentIndexedAttribute != CheckState.Unchecked && a == 0 ? "notcontentindexed-" : string.Empty;
                            string rarFilePath = Path.Combine(options.OutputDirectoryPath, $"{archiveAttribute}{notContentIndexedAttribute}{rarVersionDirectoryName}-{joinedArguments}.rar");
                            if (File.Exists(rarFilePath))
                            {
                                // Throw error? Overwrite?
                                continue;
                            }

                            FireBruteForceProgress(new(options.ReleaseDirectoryPath, rarVersionDirectoryPath, joinedArguments, totalProgressSize, currentProgress, bruteForceStartDateTime));

                            await RARCompressDirectoryAsync(rarExeFilePath, options.ReleaseDirectoryPath, rarFilePath, filteredArguments, CancellationTokenSource.Token);

                            currentProgress++;
                            FireBruteForceProgress(new(options.ReleaseDirectoryPath, rarVersionDirectoryPath, joinedArguments, totalProgressSize, currentProgress, bruteForceStartDateTime));

                            if (!File.Exists(rarFilePath))
                            {
                                continue;
                            }

                            string hash = options.HashType switch
                            {
                                HashType.SHA1 => SHA1.Calculate(rarFilePath),
                                HashType.CRC32 => CRC32.Calculate(rarFilePath),
                                _ => throw new IndexOutOfRangeException(nameof(options.HashType))
                            };

                            Log.Write(this, $"Hash for {rarFilePath}: {hash} (match: {options.Hashes.Contains(hash)})");

                            bool deleteFile = options.RAROptions.DeleteRARFiles || fileHashes.Contains(hash);
                            if (!fileHashes.Contains(hash))
                            {
                                fileHashes.Add(hash);
                            }

                            if (!options.Hashes.Contains(hash))
                            {
                                if (deleteFile)
                                {
                                    try
                                    {
                                        File.Delete(rarFilePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Write(this, $"Failed to delete RAR file: {rarFilePath}{Environment.NewLine}{ex.Message}");
                                    }
                                }

                                continue;
                            }

                            string releaseName = Path.GetFileName(options.ReleaseDirectoryPath);
                            string releaseRarFileName = $"{releaseName}.rar";
                            string releaseRarFilePath = Path.Combine(options.RARInstallationsDirectoryPath, releaseRarFileName);
                            if (!File.Exists(releaseRarFilePath))
                            {
                                File.Move(rarFilePath, releaseRarFilePath);
                            }

                            string rarVersion = Path.GetFileName(rarVersionDirectoryPath);
                            Log.Write(this, $"Found RAR version: {rarVersion}{Environment.NewLine}Command line: {joinedArguments}{Environment.NewLine}File: {releaseRarFilePath}");

                            found = true;
                            break;
                        }
                    }
                }
            }

            if (options.RAROptions.SetFileArchiveAttribute != CheckState.Unchecked ||
                options.RAROptions.SetFileNotContentIndexedAttribute != CheckState.Unchecked)
            {
                // Restore file attributes
                foreach (FileInfo fileInfo in fileInfos)
                {
                    fileInfo.Attributes = fileInfoAttributes[fileInfo];
                }
            }

            status = new(OperationStatus.Running, OperationStatus.Completed, CancellationTokenSource.IsCancellationRequested ? OperationCompletionStatus.Cancelled : OperationCompletionStatus.Success);
            FireBruteForceStatusChanged(status);
            return found;
        }

        public void Stop()
        {
            CancellationTokenSource.Cancel();
        }

        private static int CalculateBruteForceProgressSize(BruteForceOptions options)
        {
            int size = 0;

            DirectoryInfo directoryInfo = new(options.ReleaseDirectoryPath);
            string[] rarVersionDirectories = Directory.GetDirectories(options.RARInstallationsDirectoryPath);
            for (int a = 0; a < (options.RAROptions.SetFileArchiveAttribute == CheckState.Checked ? 2 : 1); a++)
            {
                for (int b = 0; b < (options.RAROptions.SetFileNotContentIndexedAttribute == CheckState.Checked ? 2 : 1); b++)
                {
                    Parallel.ForEach(rarVersionDirectories, (rarVersionDirectoryPath, s, i) =>
                    {
                        string rarExeFilePath = Path.Combine(rarVersionDirectoryPath, "rar.exe");
                        if (!File.Exists(rarExeFilePath))
                        {
                            return;
                        }

                        string rarVersionDirectoryName = Path.GetFileName(rarVersionDirectoryPath);
                        int version = ParseRARVersion(rarVersionDirectoryName);
                        if (!options.RAROptions.RARVersions.Any(r => r.InRange(version)))
                        {
                            return;
                        }

                        Parallel.ForEach(options.RAROptions.CommandLineArguments, (commandLineArguments, s2, j) =>
                        {
                            RARArchiveVersion archiveVersion = ParseRARArchiveVersion(commandLineArguments, version);

                            // Filter arguments by RAR version and RAR archive version
                            IEnumerable<string> filteredArguments = commandLineArguments.Where(
                                a => version >= a.MinimumVersion &&
                                (!a.ArchiveVersion.HasValue || a.ArchiveVersion.Value.HasFlag(archiveVersion))
                            ).Select(a => a.Argument);

                            string joinedArguments = string.Join("", filteredArguments);
                            string rarFilePath = Path.Combine(options.RARInstallationsDirectoryPath, $"{rarVersionDirectoryName}-{joinedArguments}.rar");
                            if (File.Exists(rarFilePath))
                            {
                                // Throw error? Overwrite?
                                return;
                            }

                            Interlocked.Increment(ref size);
                        });
                    });
                }
            }

            return size;
        }

        private async Task<int> RARCompressDirectoryAsync(string rarExeFilePath, string inputDirectory, string outputFilePath, IEnumerable<string> commandLineOptions, CancellationToken cancellationToken)
        {
            using RARProcess process = new(rarExeFilePath, inputDirectory, outputFilePath, commandLineOptions);
            process.ProcessStatusChanged += Process_ProcessStatusChanged;
            process.ProcessOutput += Process_ProcessOutput;
            process.CompressionStatusChanged += Process_CompressionStatusChanged;
            process.CompressionProgress += Process_CompressionProgress;
            return await process.RunAsync(cancellationToken);
        }

        private async Task<bool> ExtractRARFileAsync(string file, string outputDirectory, CancellationToken cancellationToken)
        {
            RARFile rarFile = new(file);

            rarFile.ExtractionProgress += RarFile_ExtractionProgress;

            FileExtractionStatusChangedEventArgs status = new(rarFile, OperationStatus.Running);
            FireFileExtractionStatusChanged(status);

            OperationCompletionStatus completionStatus = OperationCompletionStatus.Success;
            try
            {
                await rarFile.ExtractAsync(outputDirectory, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                completionStatus = OperationCompletionStatus.Cancelled;
            }
            catch (Exception ex)
            {
                completionStatus = OperationCompletionStatus.Error;

                Log.Write(this, $"Failed to extract RAR file: {rarFile}{Environment.NewLine}Message: {ex.Message}");
            }

            status = new(rarFile, OperationStatus.Running, OperationStatus.Completed, completionStatus);
            FireFileExtractionStatusChanged(status);

            return status.CompletionStatus == OperationCompletionStatus.Success;
        }

        private void Process_ProcessStatusChanged(object? sender, OperationStatusChangedEventArgs e)
        {
            if (sender is not RARProcess process)
            {
                return;
            }

            RARProcessStatusChanged?.Invoke(this, new(process, e.OldStatus, e.NewStatus, e.CompletionStatus));
        }

        private void Process_ProcessOutput(object? sender, ProcessDataEventArgs e)
        {
            if (sender is not RARProcess process)
            {
                return;
            }

            RARProcessOutput?.Invoke(this, new(process, e.Data));
        }

        private void Process_CompressionStatusChanged(object? sender, OperationStatusChangedEventArgs e)
        {
            if (sender is not RARProcess process)
            {
                return;
            }

            RARCompressionStatusChanged?.Invoke(this, new(process, e.OldStatus, e.NewStatus, e.CompletionStatus));
        }

        private void Process_CompressionProgress(object? sender, FileCompressionOperationProgressEventArgs e)
        {
            if (sender is not RARProcess process)
            {
                return;
            }

            RARCompressionProgress?.Invoke(this, new(process, e.OperationSize, e.OperationProgressed, e.StartDateTime));

            if (BruteForceOptions == null || e.OperationRemaining > 0)
            {
                return;
            }

            // Calculate hash for the current compressed file
            string hash = BruteForceOptions.HashType switch
            {
                HashType.SHA1 => SHA1.Calculate(e.CompressedFilePath),
                HashType.CRC32 => CRC32.Calculate(e.CompressedFilePath),
                _ => throw new IndexOutOfRangeException(nameof(BruteForceOptions.HashType))
            };

            // See if the hash for the current file is known
            if (!BruteForceOptions.Hashes.Contains(hash))
            {
                if (BruteForceOptions.RAROptions.DeleteRARFiles)
                {
                    try
                    {
                        File.Delete(e.CompressedFilePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to delete RAR file: {e.CompressedFilePath}{Environment.NewLine}{ex.Message}");
                    }
                }

                // Hash for the current file is not known; no need to compress any more files for the current input
                e.Cancel();
            }
        }

        private void RarFile_ExtractionProgress(object? sender, OperationProgressEventArgs e)
        {
            if (sender is not RARFile rarFile)
            {
                return;
            }

            FileExtractionProgress?.Invoke(this, new(rarFile, e.OperationSize, e.OperationProgressed, e.StartDateTime));
        }

        private void FireDirectoryExtractionStatusChanged(OperationStatusChangedEventArgs e)
            => DirectoryExtractionStatusChanged?.Invoke(this, e);

        private void FireDirectoryExtractionProgress(OperationProgressEventArgs e)
            => DirectoryExtractionProgress?.Invoke(this, e);

        private void FireFileExtractionStatusChanged(FileExtractionStatusChangedEventArgs e)
            => FileExtractionStatusChanged?.Invoke(this, e);

        private void FireBruteForceProgress(BruteForceProgressEventArgs e)
            => BruteForceProgress?.Invoke(this, e);

        private void FireBruteForceStatusChanged(BruteForceStatusChangedEventArgs e)
            => BruteForceStatusChanged?.Invoke(this, e);
    }
}
