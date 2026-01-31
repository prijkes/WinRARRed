using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RARLib;
using WinRARRed.Cryptography;
using WinRARRed.Diagnostics;
using WinRARRed.IO;

namespace WinRARRed;

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
        Log.Information(this, $"=== Starting Brute-Force ===", LogTarget.System);
        Log.Information(this, $"Release: {options.ReleaseDirectoryPath}", LogTarget.System);
        Log.Information(this, $"Output: {options.OutputDirectoryPath}", LogTarget.System);
        Log.Information(this, $"Expected {options.HashType}: {string.Join(", ", options.Hashes)}", LogTarget.System);
        Log.Information(this, $"Stop on first match: {options.RAROptions.StopOnFirstMatch}", LogTarget.System);
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

        // Get all valid RAR directories first
        var allValidRarDirectories = GetValidRarDirectories(rarVersionDirectories, options);
        Log.Information(this, $"Found {allValidRarDirectories.Count} valid RAR versions matching configured version ranges");

        // === PHASE 1: Comment Block Brute-Force ===
        // If CMT compressed data is available, first brute-force the comment to narrow down versions
        List<(string Path, int Version)> versionsToUse;
        if (options.RAROptions.CanUseCommentPhase)
        {
            versionsToUse = await BruteForceCommentPhaseAsync(options, allValidRarDirectories);
            Log.Information(this, $"Phase 1 complete: {versionsToUse.Count} matching version(s)", LogTarget.System);
            Log.Information(this, $"=== PHASE 2: Full RAR Brute-Force with {versionsToUse.Count} version(s) ===", LogTarget.Phase2);
        }
        else
        {
            versionsToUse = allValidRarDirectories;
            Log.Information(this, "Phase 1 skipped (no CMT data)", LogTarget.System);
            Log.Information(this, "Phase 1 skipped (no CMT data) - using all versions for brute-force", LogTarget.Phase1);
        }

        string inputFilesDir = PrepareInputDirectory(options);

        int totalProgressSize = CalculateBruteForceProgressSize(options, versionsToUse.Count, allValidRarDirectories.Count);
        int currentProgress = 0;

        DirectoryInfo directoryInfo = new(inputFilesDir);
        FileInfo[] fileInfos = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

        // Save file attributes
        Dictionary<FileInfo, FileAttributes> fileInfoAttributes = fileInfos.Select(f => new KeyValuePair<FileInfo, FileAttributes>(f, f.Attributes)).ToDictionary(f => f.Key, f => f.Value);

        // Save file hash
        HashSet<string> fileHashes = [];

        bool found = false;
        bool stopOnFirstMatch = options.RAROptions.StopOnFirstMatch;
        for (int a = 0; a < (options.RAROptions.SetFileArchiveAttribute == CheckState.Checked ? 2 : 1) && !(found && stopOnFirstMatch); a++)
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

            for (int b = 0; b < (options.RAROptions.SetFileNotContentIndexedAttribute == CheckState.Checked ? 2 : 1) && !(found && stopOnFirstMatch); b++)
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

                // Use versions filtered by Phase 1 (or all versions if Phase 1 was skipped)
                foreach (var (rarVersionDirectoryPath, version) in versionsToUse)
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
                        if (stopOnFirstMatch)
                        {
                            Log.Information(this, "Match found - stopping brute force (StopOnFirstMatch is enabled)", LogTarget.Phase2);
                            break;
                        }
                        else
                        {
                            Log.Information(this, "Match found - continuing to test remaining versions (StopOnFirstMatch is disabled)", LogTarget.Phase2);
                        }
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

        // Log completion summary to System tab
        var elapsed = DateTime.Now - bruteForceStartDateTime;
        if (CancellationTokenSource.IsCancellationRequested)
        {
            Log.Information(this, $"=== Brute-force CANCELLED after {elapsed.TotalSeconds:F1}s ===", LogTarget.System);
        }
        else if (found)
        {
            Log.Information(this, $"=== Brute-force SUCCESS in {elapsed.TotalSeconds:F1}s ===", LogTarget.System);
        }
        else
        {
            Log.Warning(this, $"=== Brute-force FAILED - no match found after {elapsed.TotalSeconds:F1}s ===", LogTarget.System);
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

    private static int CalculateBruteForceProgressSize(BruteForceOptions options, int filteredVersionCount = 0, int totalVersionCount = 0)
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

        // If Phase 1 filtered the versions, scale the progress accordingly
        if (filteredVersionCount > 0 && totalVersionCount > 0 && filteredVersionCount < totalVersionCount)
        {
            // Scale the size based on the ratio of filtered versions to total versions
            size = (int)((long)size * filteredVersionCount / totalVersionCount);
        }

        return size;
    }

    private async Task<int> RARCompressDirectoryAsync(string rarExeFilePath, string inputDirectory, string outputFilePath, IEnumerable<string> commandLineOptions, CancellationToken cancellationToken)
    {
        RARProcess process = new(rarExeFilePath, inputDirectory, outputFilePath, commandLineOptions)
        {
            LogTarget = LogTarget.Phase2
        };

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

            Log.Write(this, $"Opened log file for streaming: {logFilePath}", LogTarget.Phase2);
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
            Log.Debug(this, $"Second volume detected, terminating RAR process early for: {outputFilePath}", LogTarget.Phase2);
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
            Log.Debug(this, $"Error monitoring for second volume: {ex.Message}", LogTarget.Phase2);
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

            Log.Write(this, $"Failed to extract RAR file: {rarFile}{Environment.NewLine}Message: {ex.Message}", LogTarget.Phase2);
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
                    Log.Write(this, $"Process log file closed", LogTarget.Phase2);
                }
                catch (Exception ex)
                {
                    Log.Write(this, $"Failed to close process log writer: {ex.Message}", LogTarget.Phase2);
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
                Log.Write(this, $"Failed to write to process log: {ex.Message}", LogTarget.Phase2);
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
                Log.Write(this, $"Added {attribute} attribute to {fileInfo}", LogTarget.Phase2);
            }
            else
            {
                fileInfo.Attributes &= ~attribute;
                Log.Write(this, $"Removed {attribute} attribute from {fileInfo}", LogTarget.Phase2);
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

        Log.Debug(this, $"Input files directory: {inputFilesDir}", LogTarget.Phase2);
        Log.Debug(this, $"RAR output directory: {rarOutputDir}", LogTarget.Phase2);

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
                Log.Debug(this, $"RAR file already exists, skipping: {rarFilePath}", LogTarget.Phase2);
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
                Log.Write(this, $"RAR file was not created: {rarFilePath}", LogTarget.Phase2);
                continue;
            }

            // Log what file was actually created (may be different from expected if volumes were created)
            if (actualRarFilePath != rarFilePath)
            {
                Log.Debug(this, $"Actual file created: {actualRarFilePath} (expected: {Path.GetFileName(rarFilePath)})", LogTarget.Phase2);
            }

            // Apply Host OS patching if needed (Host OS differs from current platform)
            if (options.RAROptions.NeedsHostOSPatching)
            {
                PatchRARFilesHostOS(actualRarFilePath, options.RAROptions);
            }

            string hash = options.HashType switch
            {
                HashType.SHA1 => SHA1.Calculate(actualRarFilePath),
                HashType.CRC32 => CRC32.Calculate(actualRarFilePath),
                _ => throw new IndexOutOfRangeException(nameof(options.HashType))
            };

            Log.Write(this, $"Hash for {actualRarFilePath}: {hash} (match: {options.Hashes.Contains(hash)})", LogTarget.Phase2);

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

            // Log match to System tab for visibility
            string patchedNote = options.RAROptions.NeedsHostOSPatching ? " (patched)" : "";
            Log.Information(this, $"*** MATCH FOUND{patchedNote}! ***", LogTarget.System);
            Log.Information(this, $"  Version: {rarVersionDirectoryName}", LogTarget.System);
            Log.Information(this, $"  Params:  {joinedArguments}", LogTarget.System);
            Log.Information(this, $"  Hash:    {hash}", LogTarget.System);
            Log.Information(this, $"  RAR:     {actualRarFilePath}", LogTarget.System);

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
                    Log.Debug(this, $"Deleted volume file: {file}", LogTarget.Phase2);
                }
                catch (Exception ex)
                {
                    Log.Write(this, $"Failed to delete volume file {file}: {ex.Message}", LogTarget.Phase2);
                }
            }

            // Pattern 2: filename.rXX (old format - r00, r01, r02, etc.)
            string[] rxxFiles = Directory.GetFiles(directory, $"{baseName}.r??");
            foreach (string file in rxxFiles)
            {
                try
                {
                    File.Delete(file);
                    Log.Debug(this, $"Deleted volume file: {file}", LogTarget.Phase2);
                }
                catch (Exception ex)
                {
                    Log.Write(this, $"Failed to delete volume file {file}: {ex.Message}", LogTarget.Phase2);
                }
            }

            // Also delete the main .rar file if it exists (old format first volume)
            string mainRarFile = Path.Combine(directory, $"{baseName}.rar");
            if (File.Exists(mainRarFile))
            {
                try
                {
                    File.Delete(mainRarFile);
                    Log.Debug(this, $"Deleted main RAR file: {mainRarFile}", LogTarget.Phase2);
                }
                catch (Exception ex)
                {
                    Log.Write(this, $"Failed to delete main RAR file {mainRarFile}: {ex.Message}", LogTarget.Phase2);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Write(this, $"Failed to delete RAR file and volumes: {rarFilePath}{Environment.NewLine}{ex.Message}", LogTarget.Phase2);
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
        if (options.RAROptions.ArchiveCommentBytes != null && options.RAROptions.ArchiveCommentBytes.Length > 0)
        {
            // Use raw bytes for exact reconstruction
            string commentFilePath = Path.Combine(options.OutputDirectoryPath, "comment.txt");
            try
            {
                File.WriteAllBytes(commentFilePath, options.RAROptions.ArchiveCommentBytes);
                CommentFilePath = commentFilePath;
                Log.Information(this, $"Created comment file: {commentFilePath} ({options.RAROptions.ArchiveCommentBytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Log.Warning(this, $"Failed to create comment file: {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(options.RAROptions.ArchiveComment))
        {
            // Fallback to string (for manually entered comments)
            string commentFilePath = Path.Combine(options.OutputDirectoryPath, "comment.txt");
            try
            {
                // Use UTF-8 without BOM
                File.WriteAllText(commentFilePath, options.RAROptions.ArchiveComment, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                CommentFilePath = commentFilePath;
                Log.Information(this, $"Created comment file: {commentFilePath} ({options.RAROptions.ArchiveComment.Length} chars, fallback)");
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

    private void PatchRARFilesHostOS(string rarFilePath, RAROptions rarOptions)
    {
        if (!rarOptions.EnableHostOSPatching || !rarOptions.DetectedFileHostOS.HasValue)
        {
            return;
        }

        try
        {
            // Collect all volume files to patch
            List<string> filesToPatch = GetAllVolumeFiles(rarFilePath);
            string hostOSName = RARPatcher.GetHostOSName(rarOptions.DetectedFileHostOS.Value);

            Log.Information(this, $"Patching to match SRR: Host OS={hostOSName} (0x{rarOptions.DetectedFileHostOS.Value:X2}), Attrs=0x{rarOptions.DetectedFileAttributes ?? 0:X8} for {filesToPatch.Count} file(s)", LogTarget.Phase2);

            // Use exact values detected from SRR headers
            var patchOptions = new PatchOptions
            {
                // File header values
                FileHostOS = rarOptions.DetectedFileHostOS,
                FileAttributes = rarOptions.DetectedFileAttributes,
                // Service block (CMT) values - use CMT-specific values if available, otherwise fall back to file values
                PatchServiceBlocks = true,
                ServiceBlockHostOS = rarOptions.DetectedCmtHostOS ?? rarOptions.DetectedFileHostOS,
                ServiceBlockAttributes = rarOptions.DetectedCmtFileAttributes ?? rarOptions.DetectedFileAttributes,
                ServiceBlockFileTime = rarOptions.DetectedCmtFileTime
            };

            int totalPatched = 0;
            foreach (string filePath in filesToPatch)
            {
                try
                {
                    var results = RARPatcher.PatchFile(filePath, patchOptions);
                    totalPatched += results.Count;

                    foreach (var result in results)
                    {
                        string blockDesc = result.BlockType == RAR4BlockType.Service
                            ? $"Service ({result.FileName ?? "?"})"
                            : $"File ({result.FileName ?? "?"})";
                        Log.Debug(this, $"Patched {blockDesc}: Host OS 0x{result.OriginalHostOS:X2} -> 0x{result.NewHostOS:X2}, Attrs 0x{result.OriginalAttributes:X8} -> 0x{result.NewAttributes:X8}, CRC 0x{result.OriginalCrc:X4} -> 0x{result.NewCrc:X4}", LogTarget.Phase2);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(this, $"Failed to patch {filePath}: {ex.Message}", LogTarget.Phase2);
                }
            }

            Log.Information(this, $"Patched {totalPatched} block(s) in {filesToPatch.Count} file(s)", LogTarget.Phase2);
        }
        catch (Exception ex)
        {
            Log.Warning(this, $"Host OS patching failed: {ex.Message}", LogTarget.Phase2);
        }
    }

    private static List<string> GetAllVolumeFiles(string firstVolumePath)
    {
        var files = new List<string>();
        string directory = Path.GetDirectoryName(firstVolumePath) ?? string.Empty;
        string fileName = Path.GetFileName(firstVolumePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(firstVolumePath);

        // Determine the base name (remove .partXX if present)
        string baseName = fileNameWithoutExtension;
        if (fileName.Contains(".part") && fileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
        {
            int partIndex = fileName.IndexOf(".part", StringComparison.OrdinalIgnoreCase);
            if (partIndex > 0)
            {
                baseName = fileName.Substring(0, partIndex);
            }
        }

        // Check for partXX.rar format volumes
        string[] partFiles = Directory.GetFiles(directory, $"{baseName}.part*.rar");
        if (partFiles.Length > 0)
        {
            files.AddRange(partFiles.OrderBy(f => f));
            return files;
        }

        // Check for .rar + .rXX format volumes
        string mainRar = Path.Combine(directory, $"{baseName}.rar");
        if (File.Exists(mainRar))
        {
            files.Add(mainRar);
        }

        string[] rxxFiles = Directory.GetFiles(directory, $"{baseName}.r??");
        if (rxxFiles.Length > 0)
        {
            files.AddRange(rxxFiles.OrderBy(f => f));
        }

        // If no volumes found, just return the original file
        if (files.Count == 0 && File.Exists(firstVolumePath))
        {
            files.Add(firstVolumePath);
        }

        return files;
    }

    /// <summary>
    /// Extracts the CMT block compressed data from a RAR file.
    /// </summary>
    private static byte[]? ExtractCmtCompressedData(string rarFilePath)
    {
        try
        {
            using var fs = new FileStream(rarFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);

            // Skip RAR signature (7 bytes for RAR 4.x)
            if (fs.Length < 7)
                return null;

            fs.Position = 7;

            while (fs.Position + 7 <= fs.Length)
            {
                long blockStart = fs.Position;

                // Read base header
                ushort crc = reader.ReadUInt16();
                byte blockType = reader.ReadByte();
                ushort flags = reader.ReadUInt16();
                ushort headerSize = reader.ReadUInt16();

                if (headerSize < 7 || blockStart + headerSize > fs.Length)
                    break;

                // Check if this is a service block (0x7A)
                if (blockType == 0x7A && headerSize >= 32)
                {
                    // Read ADD_SIZE (4 bytes at offset 7)
                    uint addSize = reader.ReadUInt32();

                    // Read to get the sub-type name
                    // Skip: UnpSize(4), HostOS(1), FileCRC(4), FileTime(4), UnpVer(1), Method(1), NameSize(2), Attr(4) = 21 bytes
                    fs.Position = blockStart + 7 + 4 + 4 + 1 + 4 + 4 + 1 + 1;
                    ushort nameSize = reader.ReadUInt16();

                    // Skip Attr (4 bytes)
                    fs.Position += 4;

                    // Read the sub-type name
                    if (nameSize > 0 && fs.Position + nameSize <= fs.Length)
                    {
                        byte[] nameBytes = reader.ReadBytes(nameSize);
                        string subType = Encoding.ASCII.GetString(nameBytes);

                        if (string.Equals(subType, "CMT", StringComparison.OrdinalIgnoreCase))
                        {
                            // Found CMT block - read the compressed data
                            long dataStart = blockStart + headerSize;
                            if (dataStart + addSize <= fs.Length && addSize > 0)
                            {
                                fs.Position = dataStart;
                                byte[] data = reader.ReadBytes((int)addSize);
                                return data;
                            }
                        }
                    }

                    // Skip this block
                    fs.Position = blockStart + headerSize + addSize;
                }
                else
                {
                    // Skip this block
                    bool hasAddSize = (flags & 0x8000) != 0 || blockType == 0x74 || blockType == 0x7A;
                    uint addSize = 0;
                    if (hasAddSize)
                    {
                        fs.Position = blockStart + 7;
                        if (fs.Position + 4 <= fs.Length)
                        {
                            addSize = reader.ReadUInt32();
                        }
                    }
                    fs.Position = blockStart + headerSize + addSize;
                }

                // Safety check
                if (fs.Position <= blockStart)
                    break;
            }
        }
        catch
        {
            // Failed to extract CMT data
        }

        return null;
    }

    /// <summary>
    /// Phase 1: Brute-force RAR versions using only the comment block.
    /// Returns a list of (directoryPath, version) tuples that produced matching CMT blocks.
    /// </summary>
    private async Task<List<(string Path, int Version)>> BruteForceCommentPhaseAsync(
        BruteForceOptions options,
        List<(string Path, int Version)> allRarDirectories)
    {
        var matchedVersions = new List<(string Path, int Version)>();
        byte[]? expectedCmtData = options.RAROptions.CmtCompressedData;

        if (expectedCmtData == null || expectedCmtData.Length == 0)
        {
            Log.Information(this, "Phase 1 skipped: No CMT compressed data available", LogTarget.Phase1);
            return allRarDirectories; // Return all versions if no CMT data
        }

        Log.Information(this, $"=== PHASE 1: Comment Block Brute-Force ===", LogTarget.Phase1);
        Log.Information(this, $"Expected CMT compressed data: {expectedCmtData.Length} bytes", LogTarget.Phase1);

        // Create a temporary directory for Phase 1 testing
        string phase1Dir = Path.Combine(options.OutputDirectoryPath, "phase1_temp");
        if (Directory.Exists(phase1Dir))
        {
            Directory.Delete(phase1Dir, true);
        }
        Directory.CreateDirectory(phase1Dir);

        // Create a small dummy file for testing
        string dummyInputDir = Path.Combine(phase1Dir, "input");
        Directory.CreateDirectory(dummyInputDir);
        string dummyFilePath = Path.Combine(dummyInputDir, "dummy.txt");
        File.WriteAllText(dummyFilePath, "dummy");

        // Create comment file
        string commentFilePath = Path.Combine(phase1Dir, "comment.txt");
        if (options.RAROptions.ArchiveCommentBytes != null)
        {
            File.WriteAllBytes(commentFilePath, options.RAROptions.ArchiveCommentBytes);
        }
        else if (!string.IsNullOrEmpty(options.RAROptions.ArchiveComment))
        {
            File.WriteAllText(commentFilePath, options.RAROptions.ArchiveComment, new UTF8Encoding(false));
        }

        string outputDir = Path.Combine(phase1Dir, "output");
        Directory.CreateDirectory(outputDir);

        int totalTests = 0;
        int matchCount = 0;

        // For Phase 1, use the CMT compression method (not file compression method)
        // CMT CompressionMethod is stored as raw 0x30-0x35, convert to 0-5 for -m flag
        string cmtMethodArg;
        if (options.RAROptions.CmtCompressionMethod.HasValue)
        {
            byte rawMethod = options.RAROptions.CmtCompressionMethod.Value;
            int cmtMethod = rawMethod >= 0x30 ? rawMethod - 0x30 : rawMethod;
            if (cmtMethod >= 0 && cmtMethod <= 5)
            {
                cmtMethodArg = $"-m{cmtMethod}";
            }
            else
            {
                cmtMethodArg = "-m3"; // Default to normal
            }
        }
        else
        {
            cmtMethodArg = "-m3"; // Default to normal compression
        }

        // For Phase 1, always use -md64k (comments always use 64KB dictionary window)
        string cmtDictArg = "-md64k";

        Log.Information(this, $"CMT compression method: {cmtMethodArg}", LogTarget.Phase1);
        Log.Information(this, $"CMT dictionary size: {cmtDictArg}", LogTarget.Phase1);

        foreach (var (rarVersionDir, version) in allRarDirectories)
        {
            if (CancellationTokenSource.IsCancellationRequested)
                break;

            string rarExePath = Path.Combine(rarVersionDir, "rar.exe");
            string versionName = Path.GetFileName(rarVersionDir);

            totalTests++;

            // Build arguments using CMT-specific compression method and dictionary
            List<string> args = ["a", "-r", cmtMethodArg, cmtDictArg, $"-z{commentFilePath}"];

            // Add -ma4 for RAR 5.50+ to create RAR4 format
            if (version >= 550)
            {
                args.Add("-ma4");
            }

            // Add timestamp options if RAR version supports them (3.20+)
            // For CMT blocks, we typically disable ctime and atime
            if (version >= 320)
            {
                args.Add("-tsc-");
                args.Add("-tsa-");
            }

            string testRarPath = Path.Combine(outputDir, $"test_{versionName}_{cmtMethodArg}_{cmtDictArg}.rar"
                .Replace("-", "").Replace("/", ""));

            try
            {
                // Create the test RAR
                var process = new RARProcess(rarExePath, dummyInputDir, testRarPath, args)
                {
                    LogTarget = LogTarget.Phase1
                };
                await process.RunAsync(CancellationTokenSource.Token);

                if (!File.Exists(testRarPath))
                {
                    continue;
                }

                // Extract CMT data from generated RAR
                byte[]? generatedCmtData = ExtractCmtCompressedData(testRarPath);

                // Clean up test file
                try { File.Delete(testRarPath); } catch { }

                if (generatedCmtData == null)
                {
                    continue;
                }

                // Compare CMT compressed data
                if (generatedCmtData.SequenceEqual(expectedCmtData))
                {
                    matchCount++;
                    if (!matchedVersions.Any(v => v.Path == rarVersionDir))
                    {
                        matchedVersions.Add((rarVersionDir, version));
                        Log.Information(this, $"Phase 1 MATCH: {versionName} {cmtMethodArg} {cmtDictArg}", LogTarget.Phase1);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Debug(this, $"Phase 1 test failed for {versionName}: {ex.Message}", LogTarget.Phase1);
            }
        }

        // Clean up Phase 1 temp directory
        try
        {
            Directory.Delete(phase1Dir, true);
        }
        catch { }

        Log.Information(this, $"Phase 1 complete: {totalTests} tests, {matchCount} matches, {matchedVersions.Count} unique versions", LogTarget.Phase1);

        if (matchedVersions.Count == 0)
        {
            Log.Warning(this, "Phase 1 found no matches - falling back to all versions for Phase 2", LogTarget.Phase1);
            return allRarDirectories;
        }

        return matchedVersions;
    }
}
