using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private readonly Dictionary<RARProcess, StreamWriter> ProcessLogWriters = [];
        private readonly HashSet<RARProcess> ActiveProcesses = [];
        private readonly object ProcessLock = new();
        private string? CommentFilePath = null;

        [GeneratedRegex("(?:win)?(?:rar|wr)(?:-x64|-x32)?-?(\\d+)(?:b\\d+)?", RegexOptions.IgnoreCase)]
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
            for (int i = 0; i < rarFiles.Length; i++)
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
            Log.Information(this, $"Starting brute force operation. Release: {options.ReleaseDirectoryPath}, Output: {options.OutputDirectoryPath}");
            Log.Information(this, $"Expected {options.HashType} hash(es) for first volume: {string.Join(", ", options.Hashes)}");
            BruteForceOptions = options;

            DateTime bruteForceStartDateTime = DateTime.Now;

            BruteForceStatusChangedEventArgs status = new(OperationStatus.Running);
            FireBruteForceStatusChanged(status);

            string[] rarVersionDirectories = Directory.GetDirectories(options.RARInstallationsDirectoryPath);
            Log.Debug(this, $"Found {rarVersionDirectories.Length} RAR version directories in {options.RARInstallationsDirectoryPath}");

            if (rarVersionDirectories.Length == 0)
            {
                Log.Warning(this, "No RAR executables found in WinRAR directory or sub directories");
                return false;
            }

            string inputFilesDir = PrepareInputDirectory(options);

            int totalProgressSize = CalculateBruteForceProgressSize(options);
            int currentProgress = 0;

            DirectoryInfo directoryInfo = new(inputFilesDir);
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
                        SetFileAttributes(fileInfos, FileAttributes.Archive, true);
                    }
                    else
                    {
                        // Remove archive attribute on second run
                        SetFileAttributes(fileInfos, FileAttributes.Archive, false);
                    }
                }

                for (int b = 0; b < (options.RAROptions.SetFileNotContentIndexedAttribute == CheckState.Checked ? 2 : 1) && !found; b++)
                {
                    if (options.RAROptions.SetFileNotContentIndexedAttribute != CheckState.Unchecked)
                    {
                        if (b == 0)
                        {
                            // Set not content indexed attribute on first run
                            SetFileAttributes(fileInfos, FileAttributes.NotContentIndexed, true);
                        }
                        else
                        {
                            // Remove not content indexed attribute on second run
                            SetFileAttributes(fileInfos, FileAttributes.NotContentIndexed, false);
                        }
                    }

                    var validRarDirectories = GetValidRarDirectories(rarVersionDirectories, options);

                    foreach (var (rarVersionDirectoryPath, version) in validRarDirectories)
                    {
                        if (CancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var (foundCombination, newProgress) = await TryProcessCommandLinesAsync(options, version, rarVersionDirectoryPath, inputFilesDir, totalProgressSize, currentProgress, bruteForceStartDateTime, fileHashes, a, b);
                        currentProgress = newProgress;
                        if (foundCombination)
                        {
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
            Log.Information(this, "Stopping brute force operation and cancelling all RAR processes");
            CancellationTokenSource.Cancel();
            
            // CliWrap will automatically kill processes when the cancellation token is cancelled
            // The processes will clean themselves up in Process_ProcessStatusChanged
            
            lock (ProcessLock)
            {
                Log.Information(this, $"Active processes count: {ActiveProcesses.Count}");
                ActiveProcesses.Clear();
            }
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
                                (!a.MaximumVersion.HasValue || version <= a.MaximumVersion.Value) &&
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
            RARProcess process = new(rarExeFilePath, inputDirectory, outputFilePath, commandLineOptions);

            // Initialize streaming log writer for this process
            if (BruteForceOptions != null)
            {
                // Create log file path
                string logsDir = Path.Combine(BruteForceOptions.OutputDirectoryPath, "logs");
                Directory.CreateDirectory(logsDir);

                string logFileName = $"{Path.GetFileNameWithoutExtension(outputFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                string logFilePath = Path.Combine(logsDir, logFileName);

                // Open StreamWriter with AutoFlush enabled for immediate writes
                StreamWriter writer = new(logFilePath, append: false)
                {
                    AutoFlush = true
                };
                ProcessLogWriters[process] = writer;
                
                Log.Write(this, $"Opened log file for streaming: {logFilePath}");
            }

            process.ProcessStatusChanged += Process_ProcessStatusChanged;
            process.ProcessOutput += Process_ProcessOutput;
            process.CompressionStatusChanged += Process_CompressionStatusChanged;
            process.CompressionProgress += Process_CompressionProgress;
            
            // Create a linked cancellation token for early termination
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Start monitoring for second volume (for early termination optimization)
            Task monitorTask = MonitorForSecondVolumeAsync(outputFilePath, linkedCts);
            
            // Run the RAR process
            Task<int> processTask = process.RunAsync(linkedCts.Token);
            
            // Wait for either process completion or early termination
            await Task.WhenAny(processTask, monitorTask);
            
            // If monitoring detected second volume, cancel the process
            if (monitorTask.IsCompleted && !processTask.IsCompleted)
            {
                Log.Debug(this, $"Second volume detected, terminating RAR process early for: {outputFilePath}");
                linkedCts.Cancel();
                // Wait a bit for graceful cancellation
                await Task.WhenAny(processTask, Task.Delay(1000));
            }
            
            // Return the exit code if available, otherwise return success (0) since we terminated early
            return processTask.IsCompleted ? await processTask : 0;
        }

        private async Task MonitorForSecondVolumeAsync(string expectedRarFilePath, CancellationTokenSource cts)
        {
            try
            {
                string directory = Path.GetDirectoryName(expectedRarFilePath) ?? string.Empty;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(expectedRarFilePath);
                
                // Determine which second volume filename to look for
                // Check .part02.rar (zero-padded)
                string secondVolumePart02 = Path.Combine(directory, $"{fileNameWithoutExtension}.part02.rar");
                // Check .part2.rar (non-padded)
                string secondVolumePart2 = Path.Combine(directory, $"{fileNameWithoutExtension}.part2.rar");
                // Check .r00 (old format)
                string secondVolumeR00 = Path.Combine(directory, $"{fileNameWithoutExtension}.r00");
                
                // Poll for second volume existence
                while (!cts.Token.IsCancellationRequested)
                {
                    if (File.Exists(secondVolumePart02) || File.Exists(secondVolumePart2) || File.Exists(secondVolumeR00))
                    {
                        // Second volume detected! Return to trigger early termination
                        return;
                    }
                    
                    // Wait a bit before checking again (100ms polling interval)
                    await Task.Delay(100, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, ignore
            }
            catch (ObjectDisposedException)
            {
                // CTS was disposed after the process completed, ignore
            }
            catch (Exception ex)
            {
                Log.Debug(this, $"Error monitoring for second volume: {ex.Message}");
            }
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

            // When process completes, close and dispose the log writer
            if (e.NewStatus == OperationStatus.Completed)
            {
                if (ProcessLogWriters.TryGetValue(process, out StreamWriter? writer))
                {
                    try
                    {
                        writer.Close();
                        writer.Dispose();
                        Log.Write(this, $"Process log file closed");
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to close process log writer: {ex.Message}");
                    }
                    finally
                    {
                        // Clean up tracking dictionary
                        ProcessLogWriters.Remove(process);
                    }
                }
            }
        }

        private void Process_ProcessOutput(object? sender, ProcessDataEventArgs e)
        {
            if (sender is not RARProcess process)
            {
                return;
            }

            // Stream output directly to log file (auto-flushed)
            if (ProcessLogWriters.TryGetValue(process, out StreamWriter? writer))
            {
                try
                {
                    writer.WriteLine(e.Data);
                    // AutoFlush is enabled, so data is immediately written to disk
                }
                catch (Exception ex)
                {
                    Log.Write(this, $"Failed to write to process log: {ex.Message}");
                }
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

        private void SetFileAttributes(IEnumerable<FileInfo> files, FileAttributes attribute, bool add)
        {
            foreach (FileInfo fileInfo in files)
            {
                if (add)
                {
                    fileInfo.Attributes |= attribute;
                    Log.Write(this, $"Added {attribute} attribute to {fileInfo}");
                }
                else
                {
                    fileInfo.Attributes &= ~attribute;
                    Log.Write(this, $"Removed {attribute} attribute from {fileInfo}");
                }
            }
        }

        private List<(string Path, int Version)> GetValidRarDirectories(string[] directories, BruteForceOptions options)
        {
            var validDirectories = new List<(string Path, int Version)>();

            foreach (var dir in directories)
            {
                string rarExeFilePath = Path.Combine(dir, "rar.exe");
                if (!File.Exists(rarExeFilePath))
                {
                    Log.Write(this, $"rar.exe not found in {dir}");
                    continue;
                }

                string dirName = Path.GetFileName(dir);
                int version = ParseRARVersion(dirName);

                if (options.RAROptions.RARVersions.Any(r => r.InRange(version)))
                {
                    validDirectories.Add((dir, version));
                }
            }

            return validDirectories;
        }


        private async Task<(bool Found, int NewProgress)> TryProcessCommandLinesAsync(
            BruteForceOptions options,
            int version,
            string rarVersionDirectoryPath,
            string inputFilesDir,
            int totalProgressSize,
            int currentProgress,
            DateTime bruteForceStartDateTime,
            HashSet<string> fileHashes,
            int archiveAttributeIteration,
            int notContentAttributeIteration)
        {
            string rarExeFilePath = Path.Combine(rarVersionDirectoryPath, "rar.exe");
            string rarVersionDirectoryName = Path.GetFileName(rarVersionDirectoryPath);

            // Create subdirectory structure:
            // - inputFilesDir: Contains copy of input files (working directory for RAR)
            // - rarOutputDir: Contains generated RAR files
            string rarOutputDir = Path.Combine(options.OutputDirectoryPath, "output");

            Log.Debug(this, $"Input files directory: {inputFilesDir}");
            Log.Debug(this, $"RAR output directory: {rarOutputDir}");

            if (!Directory.Exists(rarOutputDir))
            {
                Directory.CreateDirectory(rarOutputDir);
            }

            for (int j = 0; j < options.RAROptions.CommandLineArguments.Count; j++)
            {
                RARCommandLineArgument[] commandLineArguments = options.RAROptions.CommandLineArguments[j];
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return (false, currentProgress);
                }

                RARArchiveVersion archiveVersion = ParseRARArchiveVersion(commandLineArguments, version);

                // Filter arguments by RAR version and RAR archive version
                IEnumerable<string> filteredArguments = commandLineArguments.Where(
                    a => version >= a.MinimumVersion &&
                    (!a.MaximumVersion.HasValue || version <= a.MaximumVersion.Value) &&
                    (!a.ArchiveVersion.HasValue || a.ArchiveVersion.Value.HasFlag(archiveVersion))
                ).Select(a => a.Argument);

                string joinedArguments = string.Join("", filteredArguments);
                string archiveAttribute = options.RAROptions.SetFileArchiveAttribute != CheckState.Unchecked && archiveAttributeIteration == 0 ? "archived-" : string.Empty;
                string notContentIndexedAttribute = options.RAROptions.SetFileNotContentIndexedAttribute != CheckState.Unchecked && notContentAttributeIteration == 0 ? "notcontentindexed-" : string.Empty;
                // Output RAR file to the rarOutputDir subdirectory
                string rarFilePath = Path.Combine(rarOutputDir, $"{archiveAttribute}{notContentIndexedAttribute}{rarVersionDirectoryName}-{joinedArguments}.rar");

                if (File.Exists(rarFilePath))
                {
                    // Throw error? Overwrite?
                    Log.Debug(this, $"RAR file already exists, skipping: {rarFilePath}");
                    continue;
                }

                FireBruteForceProgress(new(options.ReleaseDirectoryPath, rarVersionDirectoryPath, joinedArguments, totalProgressSize, currentProgress, bruteForceStartDateTime));

                // Build final arguments list, including comment option if available
                List<string> finalArguments = [.. filteredArguments];
                if (!string.IsNullOrEmpty(CommentFilePath))
                {
                    // Add comment option: -z<commentfile>
                    finalArguments.Add($"-z{CommentFilePath}");
                }

                // Run RAR with inputFilesDir as working directory, output to rarOutputDir
                await RARCompressDirectoryAsync(rarExeFilePath, inputFilesDir, rarFilePath, finalArguments, CancellationTokenSource.Token);

                currentProgress++;
                FireBruteForceProgress(new(options.ReleaseDirectoryPath, rarVersionDirectoryPath, joinedArguments, totalProgressSize, currentProgress, bruteForceStartDateTime));

                // Check if RAR file or volume files were created
                string? actualRarFilePath = FindCreatedRARFile(rarFilePath);
                if (actualRarFilePath == null)
                {
                    Log.Write(this, $"RAR file was not created: {rarFilePath}");
                    continue;
                }

                // Log what file was actually created (may be different from expected if volumes were created)
                if (actualRarFilePath != rarFilePath)
                {
                    Log.Debug(this, $"Actual file created: {actualRarFilePath} (expected: {Path.GetFileName(rarFilePath)})");
                }

                string hash = options.HashType switch
                {
                    HashType.SHA1 => SHA1.Calculate(actualRarFilePath),
                    HashType.CRC32 => CRC32.Calculate(actualRarFilePath),
                    _ => throw new IndexOutOfRangeException(nameof(options.HashType))
                };

                Log.Write(this, $"Hash for {actualRarFilePath}: {hash} (match: {options.Hashes.Contains(hash)})");

                // Track if we've seen this hash before (to avoid creating duplicates)
                bool isDuplicateHash = fileHashes.Contains(hash);
                fileHashes.Add(hash);

                if (!options.Hashes.Contains(hash))
                {
                    if (options.RAROptions.DeleteRARFiles)
                    {
                        // Delete all volumes including first
                        DeleteRARFileAndVolumes(actualRarFilePath);
                    }
                    // If DeleteRARFiles is false, keep all files for debugging

                    continue;
                }

                string releaseName = Path.GetFileName(options.ReleaseDirectoryPath);
                string releaseRarFileName = $"{releaseName}.rar";
                string releaseRarFilePath = Path.Combine(options.RARInstallationsDirectoryPath, releaseRarFileName);
                if (!File.Exists(releaseRarFilePath))
                {
                    File.Move(actualRarFilePath, releaseRarFilePath);
                }

                return (true, currentProgress);
            }

            return (false, currentProgress);
        }

        private static string? FindCreatedRARFile(string expectedRarFilePath)
        {
            // Check if the expected file exists (non-volume case)
            if (File.Exists(expectedRarFilePath))
            {
                return expectedRarFilePath;
            }

            // Check for volume files
            string directory = Path.GetDirectoryName(expectedRarFilePath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(expectedRarFilePath);

            // Check for RAR5 volume format with zero-padded numbers: filename.part01.rar, filename.part02.rar, etc.
            string part01File = Path.Combine(directory, $"{fileNameWithoutExtension}.part01.rar");
            if (File.Exists(part01File))
            {
                return part01File;
            }

            // Check for RAR5 volume format without zero-padding: filename.part1.rar, filename.part2.rar, etc.
            string part1File = Path.Combine(directory, $"{fileNameWithoutExtension}.part1.rar");
            if (File.Exists(part1File))
            {
                return part1File;
            }

            // Check for older RAR volume formats: filename.rar + filename.r00, filename.r01, etc.
            // In this case, the first volume keeps the .rar extension
            string firstVolumeOldFormat = Path.Combine(directory, $"{fileNameWithoutExtension}.rar");
            string secondVolumeOldFormat = Path.Combine(directory, $"{fileNameWithoutExtension}.r00");
            if (File.Exists(firstVolumeOldFormat) && File.Exists(secondVolumeOldFormat))
            {
                return firstVolumeOldFormat;
            }

            // Check if only the first volume exists (very small archive that fits in one volume)
            if (File.Exists(firstVolumeOldFormat))
            {
                return firstVolumeOldFormat;
            }

            // No RAR file found
            return null;
        }

        private void DeleteExtraVolumes(string rarFilePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(rarFilePath) ?? string.Empty;
                string fileName = Path.GetFileName(rarFilePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(rarFilePath);
                
                // Determine the base name (remove .partXX.rar or .rXX suffix if present)
                string baseName = fileNameWithoutExtension;
                
                // Check if this is a volume file and extract base name
                if (fileName.Contains(".part") && fileName.EndsWith(".rar"))
                {
                    // Format: filename.part01.rar - remove .part01
                    int partIndex = fileName.IndexOf(".part");
                    if (partIndex > 0)
                    {
                        baseName = fileName.Substring(0, partIndex);
                    }
                }
                
                // Delete extra volume files (keep the first volume)
                // Pattern 1: filename.partXX.rar (zero-padded) - keep part01, delete part02 onwards
                string[] partFiles = Directory.GetFiles(directory, $"{baseName}.part*.rar");
                foreach (string file in partFiles)
                {
                    string partFileName = Path.GetFileName(file);
                    // Skip the first volume (.part01.rar or .part1.rar)
                    if (partFileName.Contains(".part01.rar") || partFileName.Contains(".part1.rar"))
                    {
                        continue;
                    }
                    
                    try
                    {
                        File.Delete(file);
                        Log.Debug(this, $"Deleted extra volume file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to delete extra volume file {file}: {ex.Message}");
                    }
                }
                
                // Pattern 2: filename.rXX (old format - r00, r01, r02, etc.)
                // In old format, first volume is .rar, second is .r00, third is .r01, etc.
                string[] rxxFiles = Directory.GetFiles(directory, $"{baseName}.r??");
                foreach (string file in rxxFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Log.Debug(this, $"Deleted extra volume file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to delete extra volume file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(this, $"Failed to delete extra volumes: {rarFilePath}{Environment.NewLine}{ex.Message}");
            }
        }

        private void DeleteRARFileAndVolumes(string rarFilePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(rarFilePath) ?? string.Empty;
                string fileName = Path.GetFileName(rarFilePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(rarFilePath);
                
                // Determine the base name (remove .partXX.rar or .rXX suffix if present)
                string baseName = fileNameWithoutExtension;
                
                // Check if this is a volume file and extract base name
                if (fileName.Contains(".part") && fileName.EndsWith(".rar"))
                {
                    // Format: filename.part01.rar - remove .part01
                    int partIndex = fileName.IndexOf(".part");
                    if (partIndex > 0)
                    {
                        baseName = fileName.Substring(0, partIndex);
                    }
                }
                
                // Delete all related volume files using pattern matching
                // Pattern 1: filename.partXX.rar (zero-padded)
                string[] partFiles = Directory.GetFiles(directory, $"{baseName}.part*.rar");
                foreach (string file in partFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Log.Debug(this, $"Deleted volume file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to delete volume file {file}: {ex.Message}");
                    }
                }
                
                // Pattern 2: filename.rXX (old format - r00, r01, r02, etc.)
                string[] rxxFiles = Directory.GetFiles(directory, $"{baseName}.r??");
                foreach (string file in rxxFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Log.Debug(this, $"Deleted volume file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to delete volume file {file}: {ex.Message}");
                    }
                }
                
                // Also delete the main .rar file if it exists (old format first volume)
                string mainRarFile = Path.Combine(directory, $"{baseName}.rar");
                if (File.Exists(mainRarFile))
                {
                    try
                    {
                        File.Delete(mainRarFile);
                        Log.Debug(this, $"Deleted main RAR file: {mainRarFile}");
                    }
                    catch (Exception ex)
                    {
                        Log.Write(this, $"Failed to delete main RAR file {mainRarFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(this, $"Failed to delete RAR file and volumes: {rarFilePath}{Environment.NewLine}{ex.Message}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            // Create destination directory
            Directory.CreateDirectory(destDir);

            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Copy all subdirectories recursively
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
            }
        }

        private void CopySelectedEntries(string sourceDir, string destDir, HashSet<string> filePaths, HashSet<string> directoryPaths)
        {
            string sourceRoot = Path.GetFullPath(sourceDir);
            string destRoot = Path.GetFullPath(destDir);
            int missingFiles = 0;
            int skippedEntries = 0;

            foreach (string directory in directoryPaths)
            {
                if (!TryResolveRelativePath(sourceRoot, directory, out string relativeDir))
                {
                    skippedEntries++;
                    continue;
                }

                string destPath = Path.Combine(destRoot, relativeDir);
                Directory.CreateDirectory(destPath);
            }

            foreach (string file in filePaths)
            {
                if (!TryResolveRelativePath(sourceRoot, file, out string relativeFile))
                {
                    skippedEntries++;
                    continue;
                }

                string sourcePath = Path.Combine(sourceRoot, relativeFile);
                if (!File.Exists(sourcePath))
                {
                    missingFiles++;
                    Log.Warning(this, $"SRR entry not found on disk: {relativeFile}");
                    continue;
                }

                string destPath = Path.Combine(destRoot, relativeFile);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy(sourcePath, destPath, true);
            }

            if (skippedEntries > 0)
            {
                Log.Warning(this, $"Skipped {skippedEntries} SRR entries due to invalid paths.");
            }

            if (missingFiles > 0)
            {
                Log.Warning(this, $"Missing {missingFiles} SRR file entries on disk.");
            }
        }

        private static bool TryResolveRelativePath(string baseFullPath, string entryPath, out string relativePath)
        {
            relativePath = string.Empty;
            if (string.IsNullOrWhiteSpace(entryPath))
            {
                return false;
            }

            string normalized = entryPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            while (normalized.StartsWith("." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                normalized = normalized[2..];
            }
            normalized = normalized.TrimStart(Path.DirectorySeparatorChar);

            if (normalized.Length == 0)
            {
                return false;
            }

            string basePath = Path.GetFullPath(baseFullPath);
            string fullPath = Path.GetFullPath(Path.Combine(basePath, normalized));

            string basePrefix = basePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? basePath
                : basePath + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            relativePath = Path.GetRelativePath(basePath, fullPath);
            return !string.IsNullOrWhiteSpace(relativePath) && relativePath != ".";
        }

        private void ValidateArchiveFileCrcs(string inputDirectory, Dictionary<string, string> expectedCrcs)
        {
            if (expectedCrcs.Count == 0)
            {
                return;
            }

            List<string> missing = [];
            List<string> mismatched = [];

            foreach (KeyValuePair<string, string> entry in expectedCrcs)
            {
                string relativePath = entry.Key;
                string expectedCrc = entry.Value;
                string filePath = Path.Combine(inputDirectory, relativePath);

                if (!File.Exists(filePath))
                {
                    missing.Add(relativePath);
                    continue;
                }

                string actualCrc = CRC32.Calculate(filePath);
                if (!string.Equals(actualCrc, expectedCrc, StringComparison.OrdinalIgnoreCase))
                {
                    mismatched.Add($"{relativePath} (expected {expectedCrc}, got {actualCrc})");
                }
            }

            if (missing.Count == 0 && mismatched.Count == 0)
            {
                Log.Information(this, $"SRR CRC32 validation passed for {expectedCrcs.Count} file(s).");
                return;
            }

            foreach (string entry in missing)
            {
                Log.Error(this, $"SRR CRC32 validation missing file: {entry}");
            }

            foreach (string entry in mismatched)
            {
                Log.Error(this, $"SRR CRC32 validation mismatch: {entry}");
            }

            int issueCount = missing.Count + mismatched.Count;
            throw new InvalidDataException($"SRR CRC32 validation failed for {issueCount} file(s).");
        }

        private string PrepareInputDirectory(BruteForceOptions options)
        {
            string inputFilesDir = Path.Combine(options.OutputDirectoryPath, "input");
            if (Directory.Exists(inputFilesDir))
            {
                Log.Information(this, $"Cleaning existing input directory: {inputFilesDir}");
                Directory.Delete(inputFilesDir, true);
            }

            Log.Information(this, $"Creating input directory and copying files from {options.ReleaseDirectoryPath} to {inputFilesDir}");
            Directory.CreateDirectory(inputFilesDir);
            if (options.RAROptions.HasArchiveFileList)
            {
                Log.Information(this, $"Using SRR file list: {options.RAROptions.ArchiveFilePaths.Count} files, {options.RAROptions.ArchiveDirectoryPaths.Count} dirs");
                CopySelectedEntries(options.ReleaseDirectoryPath, inputFilesDir, options.RAROptions.ArchiveFilePaths, options.RAROptions.ArchiveDirectoryPaths);
            }
            else
            {
                CopyDirectory(options.ReleaseDirectoryPath, inputFilesDir);
            }
            Log.Information(this, $"Finished copying {Directory.GetFiles(inputFilesDir, "*.*", SearchOption.AllDirectories).Length} files to input directory");

            if (options.RAROptions.ArchiveFileCrcs.Count > 0)
            {
                Log.Information(this, $"Validating SRR CRC32 entries: {options.RAROptions.ArchiveFileCrcs.Count} file(s)");
                ValidateArchiveFileCrcs(inputFilesDir, options.RAROptions.ArchiveFileCrcs);
            }

            int fileTimestampCount = options.RAROptions.FileTimestamps.Count;
            int fileCreationCount = options.RAROptions.FileCreationTimes.Count;
            int fileAccessCount = options.RAROptions.FileAccessTimes.Count;
            // Apply file timestamps before directory timestamps to keep directory times authoritative.
            if (fileTimestampCount + fileCreationCount + fileAccessCount > 0)
            {
                Log.Information(this, $"Applying file timestamps: mtime {fileTimestampCount}, ctime {fileCreationCount}, atime {fileAccessCount}");
                ApplyFileTimestamps(inputFilesDir, options.RAROptions.FileTimestamps, options.RAROptions.FileCreationTimes, options.RAROptions.FileAccessTimes);
            }

            int dirTimestampCount = options.RAROptions.DirectoryTimestamps.Count;
            int dirCreationCount = options.RAROptions.DirectoryCreationTimes.Count;
            int dirAccessCount = options.RAROptions.DirectoryAccessTimes.Count;
            if (dirTimestampCount + dirCreationCount + dirAccessCount > 0)
            {
                Log.Information(this, $"Applying directory timestamps: mtime {dirTimestampCount}, ctime {dirCreationCount}, atime {dirAccessCount}");
                ApplyDirectoryTimestamps(inputFilesDir, options.RAROptions.DirectoryTimestamps, options.RAROptions.DirectoryCreationTimes, options.RAROptions.DirectoryAccessTimes);
            }

            // Create comment file if archive has a comment
            CommentFilePath = null;
            if (!string.IsNullOrEmpty(options.RAROptions.ArchiveComment))
            {
                string commentFilePath = Path.Combine(options.OutputDirectoryPath, "comment.txt");
                try
                {
                    File.WriteAllText(commentFilePath, options.RAROptions.ArchiveComment, Encoding.UTF8);
                    CommentFilePath = commentFilePath;
                    Log.Information(this, $"Created comment file: {commentFilePath} ({options.RAROptions.ArchiveComment.Length} chars)");
                }
                catch (Exception ex)
                {
                    Log.Warning(this, $"Failed to create comment file: {ex.Message}");
                }
            }

            return inputFilesDir;
        }

        private void ApplyFileTimestamps(string inputDirectory, Dictionary<string, DateTime> modifiedTimes, Dictionary<string, DateTime> creationTimes, Dictionary<string, DateTime> accessTimes)
        {
            // Order matters: set creation/access first so modified time ends up as the final write.
            ApplyFileTimestampEntries(inputDirectory, creationTimes, File.SetCreationTime, "creation");
            ApplyFileTimestampEntries(inputDirectory, accessTimes, File.SetLastAccessTime, "access");
            ApplyFileTimestampEntries(inputDirectory, modifiedTimes, File.SetLastWriteTime, "modified");
        }

        private void ApplyFileTimestampEntries(string inputDirectory, Dictionary<string, DateTime> timestamps, Action<string, DateTime> setter, string label)
        {
            foreach (KeyValuePair<string, DateTime> entry in timestamps)
            {
                string relativePath = entry.Key;
                string filePath = Path.Combine(inputDirectory, relativePath);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                try
                {
                    setter(filePath, entry.Value);
                }
                catch (Exception ex)
                {
                    Log.Warning(this, $"Failed to set {label} timestamp for file {relativePath}: {ex.Message}");
                }
            }
        }

        private void ApplyDirectoryTimestamps(string inputDirectory, Dictionary<string, DateTime> modifiedTimes, Dictionary<string, DateTime> creationTimes, Dictionary<string, DateTime> accessTimes)
        {
            // Order matters: set creation/access first so modified time ends up as the final write.
            ApplyDirectoryTimestampEntries(inputDirectory, creationTimes, Directory.SetCreationTime, "creation");
            ApplyDirectoryTimestampEntries(inputDirectory, accessTimes, Directory.SetLastAccessTime, "access");
            ApplyDirectoryTimestampEntries(inputDirectory, modifiedTimes, Directory.SetLastWriteTime, "modified");
        }

        private void ApplyDirectoryTimestampEntries(string inputDirectory, Dictionary<string, DateTime> timestamps, Action<string, DateTime> setter, string label)
        {
            foreach (KeyValuePair<string, DateTime> entry in timestamps)
            {
                string relativePath = entry.Key;
                string dirPath = Path.Combine(inputDirectory, relativePath);
                if (!Directory.Exists(dirPath))
                {
                    continue;
                }

                try
                {
                    setter(dirPath, entry.Value);
                }
                catch (Exception ex)
                {
                    Log.Warning(this, $"Failed to set {label} timestamp for directory {relativePath}: {ex.Message}");
                }
            }
        }
    }
}
