using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using RARLib;
using SRRLib;
using WinRARRed.Diagnostics;

namespace WinRARRed.Forms;

public partial class SettingsOptionsForm : Form
{
        public RAROptions RAROptions { get; private set; }
        public event EventHandler<string>? VerificationFileExtracted;
        private HashSet<string> ImportedArchiveFiles = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> ImportedArchiveDirectories = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DateTime> ImportedDirectoryTimestamps = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DateTime> ImportedDirectoryCreationTimes = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DateTime> ImportedDirectoryAccessTimes = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DateTime> ImportedFileTimestamps = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DateTime> ImportedFileCreationTimes = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DateTime> ImportedFileAccessTimes = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> ImportedArchiveFileCrcs = new(StringComparer.OrdinalIgnoreCase);
        private string? ImportedArchiveComment = null;
        private byte[]? ImportedArchiveCommentBytes = null;
        private byte[]? ImportedCmtCompressedData = null;
        private byte? ImportedCmtCompressionMethod = null;

        // Host OS patching - exact values from SRR
        private byte? DetectedFileHostOS = null;
        private uint? DetectedFileAttributes = null;
        private byte? DetectedCmtHostOS = null;
        private uint? DetectedCmtFileTime = null;
        private uint? DetectedCmtFileAttributes = null;

        public SettingsOptionsForm()
        {
            InitializeComponent();

            nupSwitchMTEnd.Value = Environment.ProcessorCount;

            cbFileA.CheckedChanged += CbFile_CheckedChanged;
            cbFileI.CheckedChanged += CbFile_CheckedChanged;
            cbSwitchAI.CheckedChanged += CbSwitchAI_CheckedChanged;
            cbSwitchMT.CheckedChanged += CbSwitchMT_CheckedChanged;
            cbSwitchV.CheckedChanged += CbSwitchV_CheckedChanged;
            btnCancel.Click += BtnCancel_Click;
            btnImportSrr.Click += BtnImportSrr_Click;
            btnSave.Click += BtnSave_Click;

            RAROptions = GetRAROptions();

            Shown += OptionsForm_Shown;
        }

        public void Toggle(bool enabled)
        {
            gbRARVersion.Enabled = enabled;
            gbFileOptions.Enabled = enabled;
            gbSwitches.Enabled = enabled;
            cbDeleteRARFiles.Enabled = enabled;
            cbStopOnFirstMatch.Enabled = enabled;
            btnSave.Enabled = enabled;
        }

        private void OptionsForm_Shown(object? sender, EventArgs e)
        {
            cbRARVersion2.Checked = RAROptions.RARVersions.Any(r => r.Start == 200);
            cbRARVersion3.Checked = RAROptions.RARVersions.Any(r => r.Start == 300);
            cbRARVersion4.Checked = RAROptions.RARVersions.Any(r => r.Start == 400);
            cbRARVersion5.Checked = RAROptions.RARVersions.Any(r => r.Start == 500);
            cbRARVersion6.Checked = RAROptions.RARVersions.Any(r => r.Start == 600);
            cbFileA.CheckState = RAROptions.SetFileArchiveAttribute;
            cbFileI.CheckState = RAROptions.SetFileNotContentIndexedAttribute;
            cbSwitchAI.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-ai"));
            cbSwitchM0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m0"));
            cbSwitchM1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m1"));
            cbSwitchM2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m2"));
            cbSwitchM3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m3"));
            cbSwitchM4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m4"));
            cbSwitchM5.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-m5"));
            cbSwitchMA4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-ma4"));
            cbSwitchMA5.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-ma5"));
            cbSwitchMD64K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md64")));
            cbSwitchMD128K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md128")));
            cbSwitchMD256K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md256")));
            cbSwitchMD512K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md512")));
            cbSwitchMD1024K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md1024")));
            cbSwitchMD2048K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md2048")));
            cbSwitchMD4096K.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-md4096")));
            cbSwitchMD8M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md8m"));
            cbSwitchMD16M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md16m"));
            cbSwitchMD32M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md32m"));
            cbSwitchMD64M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md64m"));
            cbSwitchMD128M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md128m"));
            cbSwitchMD256M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md256m"));
            cbSwitchMD512M.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md512m"));
            cbSwitchMD1G.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-md1g"));
            cbSwitchMT.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument.StartsWith("-mt")));
            var start = RAROptions.CommandLineArguments.Select(a => a.FirstOrDefault(ab => ab.Argument.StartsWith("-mt"))).FirstOrDefault();
            nupSwitchMTStart.Value = start != null ? int.Parse(start.Argument[3..]) : 1;
            var end = RAROptions.CommandLineArguments.Select(a => a.LastOrDefault(ab => ab.Argument.StartsWith("-mt"))).LastOrDefault();
            nupSwitchMTEnd.Value = end != null ? int.Parse(end.Argument[3..]) : Environment.ProcessorCount;
            cbSwitchR.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-r"));
            var volumes = RAROptions.CommandLineArguments.Select(a => a.FirstOrDefault(ab => ab.Argument.StartsWith("-v"))).FirstOrDefault();
            cbSwitchV.Checked = volumes != null;
            
            if (volumes != null)
            {
                // Parse the volume size argument (-v followed by size and optional unit)
                // RAR spec: -v<size>[k,b] where no suffix=×1000, k=×1024, b=×1
                string sizeArg = volumes.Argument[2..]; // Remove "-v" prefix
                char lastChar = sizeArg.Length > 0 ? char.ToLower(sizeArg.Last()) : '\0';
                string sizeValueStr = char.IsLetter(lastChar) ? sizeArg[..^1] : sizeArg;
                
                if (!long.TryParse(sizeValueStr, out long sizeValue))
                {
                    sizeValue = 15000;
                }
                
                // Determine unit and convert back to user-friendly display
                switch (lastChar)
                {
                    case 'b':
                        // Exact bytes - just display as-is
                        tbSwitchSize.Text = sizeValue.ToString();
                        rbSwitchSizeBytes.Checked = true;
                        break;
                        
                    case 'k':
                        // KiB (1024-based) - see if it's cleanly divisible by 1024 for larger units
                        if (sizeValue >= 1024 * 1024 && sizeValue % (1024 * 1024) == 0)
                        {
                            // Display as GiB
                            tbSwitchSize.Text = (sizeValue / (1024 * 1024)).ToString();
                            rbSwitchSizeGiB.Checked = true;
                        }
                        else if (sizeValue >= 1024 && sizeValue % 1024 == 0)
                        {
                            // Display as MiB
                            tbSwitchSize.Text = (sizeValue / 1024).ToString();
                            rbSwitchSizeMiB.Checked = true;
                        }
                        else
                        {
                            // Display as KiB
                            tbSwitchSize.Text = sizeValue.ToString();
                            rbSwitchSizeKiB.Checked = true;
                        }
                        break;
                        
                    default:
                        // No suffix = KB (1000-based) - see if it's cleanly divisible by 1000 for larger units
                        if (sizeValue >= 1000 * 1000 && sizeValue % (1000 * 1000) == 0)
                        {
                            // Display as GB
                            tbSwitchSize.Text = (sizeValue / (1000 * 1000)).ToString();
                            rbSwitchSizeGB.Checked = true;
                        }
                        else if (sizeValue >= 1000 && sizeValue % 1000 == 0)
                        {
                            // Display as MB
                            tbSwitchSize.Text = (sizeValue / 1000).ToString();
                            rbSwitchSizeMB.Checked = true;
                        }
                        else
                        {
                            // Display as KB
                            tbSwitchSize.Text = sizeValue.ToString();
                            rbSwitchSizeKB.Checked = true;
                        }
                        break;
                }
            }
            else
            {
                tbSwitchSize.Text = "15000";
                rbSwitchSizeKB.Checked = true;
            }
            cbSwitchTSM0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm0"));
            cbSwitchTSM1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm1"));
            cbSwitchTSM2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm2"));
            cbSwitchTSM3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm3"));
            cbSwitchTSM4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsm4"));
            cbSwitchTSC0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc0"));
            cbSwitchTSC1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc1"));
            cbSwitchTSC2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc2"));
            cbSwitchTSC3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc3"));
            cbSwitchTSC4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsc4"));
            cbSwitchTSA0.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa0"));
            cbSwitchTSA1.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa1"));
            cbSwitchTSA2.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa2"));
            cbSwitchTSA3.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa3"));
            cbSwitchTSA4.Checked = RAROptions.CommandLineArguments.Any(a => a.Any(ab => ab.Argument == "-tsa4"));
            cbDeleteRARFiles.Checked = RAROptions.DeleteRARFiles;
            cbStopOnFirstMatch.Checked = RAROptions.StopOnFirstMatch;
        }

        private void CbFile_CheckedChanged(object? sender, EventArgs e)
        {
            cbSwitchAI.Enabled = !cbFileA.Checked && !cbFileI.Checked;
        }

        private void CbSwitchAI_CheckedChanged(object? sender, EventArgs e)
        {
            cbFileA.Enabled = cbFileI.Enabled = !cbSwitchAI.Checked;
        }

        private void CbSwitchMT_CheckedChanged(object? sender, EventArgs e)
        {
            nupSwitchMTStart.Enabled = nupSwitchMTEnd.Enabled = cbSwitchMT.Checked;
        }

        private void CbSwitchV_CheckedChanged(object? sender, EventArgs e)
        {
            tbSwitchSize.Enabled = rbSwitchSizeBytes.Enabled = rbSwitchSizeKB.Enabled = rbSwitchSizeMB.Enabled = rbSwitchSizeGB.Enabled = rbSwitchSizeKiB.Enabled = rbSwitchSizeMiB.Enabled = rbSwitchSizeGiB.Enabled = cbSwitchV.Checked;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            RAROptions options = GetRAROptions();

            if (options.RARVersions.Count == 0)
            {
                GUIHelper.ShowError(this, "No RAR version(s) have been selected.");
                return;
            }

            RAROptions = options;

            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object? sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void BtnImportSrr_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new();
            ofd.Filter = "SRR Files|*.srr|All Files|*.*";
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                Log.Information(this, $"=== SRR Import: {Path.GetFileName(ofd.FileName)} ===", LogTarget.System);
                Log.Debug(this, $"Full path: {ofd.FileName}", LogTarget.System);

                var srr = SRRFile.Load(ofd.FileName);
                List<string> importedSettings = [];

                Log.Information(this, $"SRR loaded successfully", LogTarget.System);
                ImportedArchiveFiles = new HashSet<string>(srr.ArchivedFiles, StringComparer.OrdinalIgnoreCase);
                ImportedArchiveDirectories = new HashSet<string>(srr.ArchivedDirectories, StringComparer.OrdinalIgnoreCase);
                ImportedDirectoryTimestamps = new Dictionary<string, DateTime>(srr.ArchivedDirectoryTimestamps, StringComparer.OrdinalIgnoreCase);
                ImportedDirectoryCreationTimes = new Dictionary<string, DateTime>(srr.ArchivedDirectoryCreationTimes, StringComparer.OrdinalIgnoreCase);
                ImportedDirectoryAccessTimes = new Dictionary<string, DateTime>(srr.ArchivedDirectoryAccessTimes, StringComparer.OrdinalIgnoreCase);
                ImportedFileTimestamps = new Dictionary<string, DateTime>(srr.ArchivedFileTimestamps, StringComparer.OrdinalIgnoreCase);
                ImportedFileCreationTimes = new Dictionary<string, DateTime>(srr.ArchivedFileCreationTimes, StringComparer.OrdinalIgnoreCase);
                ImportedFileAccessTimes = new Dictionary<string, DateTime>(srr.ArchivedFileAccessTimes, StringComparer.OrdinalIgnoreCase);
                ImportedArchiveFileCrcs = new Dictionary<string, string>(srr.ArchivedFileCrcs, StringComparer.OrdinalIgnoreCase);
                if (ImportedArchiveFiles.Count > 0 || ImportedArchiveDirectories.Count > 0)
                {
                    string dirSuffix = ImportedArchiveDirectories.Count > 0 ? $", {ImportedArchiveDirectories.Count} dirs" : string.Empty;
                    importedSettings.Add($"Archive entries: {ImportedArchiveFiles.Count} files{dirSuffix}");
                    Log.Information(this, $"Archived entries: {ImportedArchiveFiles.Count} files{dirSuffix}", LogTarget.System);
                    foreach (string file in ImportedArchiveFiles.OrderBy(f => f))
                    {
                        Log.Debug(this, $"  File: {file}", LogTarget.System);
                    }
                    foreach (string dir in ImportedArchiveDirectories.OrderBy(d => d))
                    {
                        Log.Debug(this, $"  Dir: {dir}", LogTarget.System);
                    }
                }
                else
                {
                    importedSettings.Add("Archive entries: none (all files will be used)");
                    Log.Information(this, "Archived entries: none (all files will be used)", LogTarget.System);
                }

                if (ImportedDirectoryTimestamps.Count > 0 || ImportedDirectoryCreationTimes.Count > 0 || ImportedDirectoryAccessTimes.Count > 0)
                {
                    importedSettings.Add($"Directory times: mtime {ImportedDirectoryTimestamps.Count}, ctime {ImportedDirectoryCreationTimes.Count}, atime {ImportedDirectoryAccessTimes.Count}");
                }

                if (ImportedFileTimestamps.Count > 0 || ImportedFileCreationTimes.Count > 0 || ImportedFileAccessTimes.Count > 0)
                {
                    importedSettings.Add($"File times: mtime {ImportedFileTimestamps.Count}, ctime {ImportedFileCreationTimes.Count}, atime {ImportedFileAccessTimes.Count}");
                }

                if (ImportedArchiveFileCrcs.Count > 0)
                {
                    importedSettings.Add($"File CRC32 entries: {ImportedArchiveFileCrcs.Count}");
                    Log.Information(this, $"Archived file CRCs: {ImportedArchiveFileCrcs.Count}", LogTarget.System);
                    // Log every CRC entry so users can verify all files were imported.
                    foreach (KeyValuePair<string, string> crcEntry in ImportedArchiveFileCrcs.OrderBy(entry => entry.Key))
                    {
                        Log.Debug(this, $"  CRC: {crcEntry.Key} = {crcEntry.Value}", LogTarget.System);
                    }
                }

                // Store archive comment if present
                ImportedArchiveComment = srr.ArchiveComment;
                ImportedArchiveCommentBytes = srr.ArchiveCommentBytes;
                ImportedCmtCompressedData = srr.CmtCompressedData;
                ImportedCmtCompressionMethod = srr.CmtCompressionMethod;
                if (!string.IsNullOrEmpty(ImportedArchiveComment))
                {
                    int commentLen = ImportedArchiveComment.Length;
                    string truncated = commentLen > 50 ? ImportedArchiveComment[..50] + "..." : ImportedArchiveComment;
                    importedSettings.Add($"Archive comment: {commentLen} chars");
                    Log.Information(this, $"Archive comment: {commentLen} chars", LogTarget.System);
                    Log.Debug(this, $"  Preview: {truncated.Replace("\r", "").Replace("\n", " ")}", LogTarget.System);
                }

                // Log CMT compressed data availability for Phase 1
                if (ImportedCmtCompressedData != null && ImportedCmtCompressedData.Length > 0)
                {
                    string methodName = ImportedCmtCompressionMethod switch
                    {
                        0x30 => "Store",
                        0x31 => "Fastest",
                        0x32 => "Fast",
                        0x33 => "Normal",
                        0x34 => "Good",
                        0x35 => "Best",
                        _ => $"0x{ImportedCmtCompressionMethod:X2}"
                    };
                    importedSettings.Add($"CMT block: {ImportedCmtCompressedData.Length} bytes ({methodName}) - Phase 1 enabled");
                    Log.Information(this, $"CMT compressed data: {ImportedCmtCompressedData.Length} bytes, method={methodName}", LogTarget.System);
                    Log.Information(this, "Phase 1 (comment brute-force) is enabled", LogTarget.System);
                }
                else
                {
                    Log.Information(this, "No CMT data - Phase 1 will be skipped", LogTarget.System);
                }

                // Store detected Host OS and attributes from SRR for patching
                DetectedFileHostOS = srr.DetectedHostOS;
                DetectedFileAttributes = srr.DetectedFileAttributes;
                DetectedCmtHostOS = srr.CmtHostOS;
                DetectedCmtFileTime = srr.CmtFileTimeDOS;
                DetectedCmtFileAttributes = srr.CmtFileAttributes;

                // Display detected Host OS from file headers
                if (srr.DetectedHostOS.HasValue)
                {
                    importedSettings.Add($"Host OS (files): {srr.DetectedHostOSName} (0x{srr.DetectedHostOS:X2})");
                    Log.Information(this, $"Detected Host OS: {srr.DetectedHostOSName} (0x{srr.DetectedHostOS:X2})", LogTarget.System);

                    // Check if Host OS differs from current platform
                    bool isCurrentWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                    bool isRarWindows = srr.DetectedHostOS == 2;
                    bool isRarUnix = srr.DetectedHostOS == 3;
                    bool needsPatching = (isCurrentWindows && isRarUnix) || (!isCurrentWindows && isRarWindows);

                    if (needsPatching)
                    {
                        // Auto-enable patching since Host OS differs from current platform
                        cbEnableHostOSPatching.Checked = true;
                        string targetOS = isRarUnix ? "Unix" : "Windows";
                        importedSettings.Add($"→ Host OS patching enabled (will patch to {targetOS})");
                        Log.Information(this, $"Host OS patching enabled (current: {(isCurrentWindows ? "Windows" : "Unix")}, target: {targetOS})", LogTarget.System);
                    }
                }

                if (srr.DetectedFileAttributes.HasValue)
                {
                    importedSettings.Add($"File Attributes: 0x{srr.DetectedFileAttributes:X8}");
                    Log.Debug(this, $"Detected file attributes: 0x{srr.DetectedFileAttributes:X8}", LogTarget.System);
                }

                // Update the UI to show detected values
                UpdateHostOSDetectedLabel();

                // Display CMT (comment service block) specific metadata
                if (srr.CmtHostOS.HasValue)
                {
                    importedSettings.Add($"CMT Host OS: {srr.CmtHostOSName} (0x{srr.CmtHostOS:X2})");
                }

                // Auto-configure timestamp precision checkboxes from detected values
                // Use file header precision if available, otherwise fall back to CMT precision
                var mtimePrecision = srr.FileMtimePrecision ?? srr.CmtMtimePrecision;
                var ctimePrecision = srr.FileCtimePrecision ?? srr.CmtCtimePrecision;
                var atimePrecision = srr.FileAtimePrecision ?? srr.CmtAtimePrecision;

                if (mtimePrecision.HasValue)
                {
                    SetTimestampPrecisionCheckboxes("TSM", mtimePrecision.Value,
                        cbSwitchTSM0, cbSwitchTSM1, cbSwitchTSM2, cbSwitchTSM3, cbSwitchTSM4);
                    importedSettings.Add($"Mtime precision: -tsm{(int)mtimePrecision.Value}");
                    Log.Information(this, $"Timestamp (mtime): -tsm{(int)mtimePrecision.Value}", LogTarget.System);
                }

                if (ctimePrecision.HasValue)
                {
                    SetTimestampPrecisionCheckboxes("TSC", ctimePrecision.Value,
                        cbSwitchTSC0, cbSwitchTSC1, cbSwitchTSC2, cbSwitchTSC3, cbSwitchTSC4);
                    importedSettings.Add($"Ctime precision: -tsc{(int)ctimePrecision.Value}");
                    Log.Information(this, $"Timestamp (ctime): -tsc{(int)ctimePrecision.Value}", LogTarget.System);
                }

                if (atimePrecision.HasValue)
                {
                    SetTimestampPrecisionCheckboxes("TSA", atimePrecision.Value,
                        cbSwitchTSA0, cbSwitchTSA1, cbSwitchTSA2, cbSwitchTSA3, cbSwitchTSA4);
                    importedSettings.Add($"Atime precision: -tsa{(int)atimePrecision.Value}");
                    Log.Information(this, $"Timestamp (atime): -tsa{(int)atimePrecision.Value}", LogTarget.System);
                }

                if (srr.CmtFileAttributes.HasValue && srr.CmtFileAttributes.Value != 0)
                {
                    importedSettings.Add($"CMT attributes: 0x{srr.CmtFileAttributes:X8}");
                }

                // Set compression method (already 0-5 from RARHeaderReader)
                if (srr.CompressionMethod.HasValue)
                {
                    int method = srr.CompressionMethod.Value;

                    if (method >= 0 && method <= 5)
                    {
                        // Uncheck all compression methods first, then set only the detected one
                        cbSwitchM0.Checked = false;
                        cbSwitchM1.Checked = false;
                        cbSwitchM2.Checked = false;
                        cbSwitchM3.Checked = false;
                        cbSwitchM4.Checked = false;
                        cbSwitchM5.Checked = false;

                        switch (method)
                        {
                            case 0: cbSwitchM0.Checked = true; break;
                            case 1: cbSwitchM1.Checked = true; break;
                            case 2: cbSwitchM2.Checked = true; break;
                            case 3: cbSwitchM3.Checked = true; break;
                            case 4: cbSwitchM4.Checked = true; break;
                            case 5: cbSwitchM5.Checked = true; break;
                        }

                        string[] methodNames = ["Store", "Fastest", "Fast", "Normal", "Good", "Best"];
                        importedSettings.Add($"Compression: -m{method}");
                        Log.Information(this, $"Compression method: -m{method} ({methodNames[method]})", LogTarget.System);
                    }
                    else
                    {
                        GUIHelper.ShowWarning(this, $"SRR file provided unknown compression method: {method}");
                        return;
                    }
                }
                
                // Set dictionary size
                if (srr.DictionarySize.HasValue)
                {
                    // Uncheck all dictionary size options first
                    cbSwitchMD64K.Checked = false;
                    cbSwitchMD128K.Checked = false;
                    cbSwitchMD256K.Checked = false;
                    cbSwitchMD512K.Checked = false;
                    cbSwitchMD1024K.Checked = false;
                    cbSwitchMD2048K.Checked = false;
                    cbSwitchMD4096K.Checked = false;
                    cbSwitchMD8M.Checked = false;
                    cbSwitchMD16M.Checked = false;
                    cbSwitchMD32M.Checked = false;
                    cbSwitchMD64M.Checked = false;
                    cbSwitchMD128M.Checked = false;
                    cbSwitchMD256M.Checked = false;
                    cbSwitchMD512M.Checked = false;
                    cbSwitchMD1G.Checked = false;

                    // Set the matching dictionary size
                    string dictArg = "";
                    switch (srr.DictionarySize.Value)
                    {
                        case 64:
                            cbSwitchMD64K.Checked = true;
                            dictArg = "-md64k";
                            importedSettings.Add("Dictionary: 64 KB");
                            break;
                        case 128:
                            cbSwitchMD128K.Checked = true;
                            dictArg = "-md128k";
                            importedSettings.Add("Dictionary: 128 KB");
                            break;
                        case 256:
                            cbSwitchMD256K.Checked = true;
                            dictArg = "-md256k";
                            importedSettings.Add("Dictionary: 256 KB");
                            break;
                        case 512:
                            cbSwitchMD512K.Checked = true;
                            dictArg = "-md512k";
                            importedSettings.Add("Dictionary: 512 KB");
                            break;
                        case 1024:
                            cbSwitchMD1024K.Checked = true;
                            dictArg = "-md1024k";
                            importedSettings.Add("Dictionary: 1024 KB");
                            break;
                        case 2048:
                            cbSwitchMD2048K.Checked = true;
                            dictArg = "-md2048k";
                            importedSettings.Add("Dictionary: 2048 KB");
                            break;
                        case 4096:
                            cbSwitchMD4096K.Checked = true;
                            dictArg = "-md4096k";
                            importedSettings.Add("Dictionary: 4096 KB");
                            break;
                    }
                    if (!string.IsNullOrEmpty(dictArg))
                    {
                        Log.Information(this, $"Dictionary size: {dictArg} ({srr.DictionarySize.Value} KB)", LogTarget.System);
                    }
                }
                
                // Set solid archive flag
                if (srr.IsSolidArchive.HasValue)
                {
                    // cbSwitchSDash is "Disable solid archiving" so it's the opposite
                    cbSwitchSDash.Checked = !srr.IsSolidArchive.Value;
                    importedSettings.Add(srr.IsSolidArchive.Value ? "Solid: Yes" : "Solid: No (-s-)");
                }

                // Set archive format based on UnpVer
                // UnpVer < 50 = RAR4 format, UnpVer >= 50 = RAR5 format
                if (srr.RARVersion.HasValue)
                {
                    cbSwitchMA4.Checked = false;
                    cbSwitchMA5.Checked = false;

                    if (srr.RARVersion.Value < 50)
                    {
                        // RAR4 format - need -ma4 for RAR 5.50+ to force RAR4 output
                        cbSwitchMA4.Checked = true;
                        importedSettings.Add("Archive format: RAR4 (-ma4)");
                        Log.Information(this, $"Archive format: RAR4 (-ma4) based on UnpVer={srr.RARVersion.Value}", LogTarget.System);
                    }
                    else
                    {
                        // RAR5 format
                        cbSwitchMA5.Checked = true;
                        importedSettings.Add("Archive format: RAR5 (-ma5)");
                        Log.Information(this, $"Archive format: RAR5 (-ma5) based on UnpVer={srr.RARVersion.Value}", LogTarget.System);
                    }
                }

                // Disable file attribute options when importing SRR
                // The attributes from the original archive are detected and will be patched if needed
                cbFileA.CheckState = CheckState.Unchecked;
                cbFileI.CheckState = CheckState.Unchecked;

                // Disable options that add unnecessary combinations
                cbSwitchAI.Checked = false;  // Don't test with/without -ai
                cbSwitchMT.Checked = false;  // Don't test different thread counts

                // Ensure -r (recurse) is enabled for proper directory handling
                cbSwitchR.Checked = true;

                importedSettings.Add("Optimized: Single attribute/thread configuration");
                
                bool volumeSizeApplied = false;
                if (srr.RarFiles.Count > 1 && srr.VolumeSizeBytes.HasValue)
                {
                    string? sizeLabel = ApplyVolumeSizeFromSrr(srr.VolumeSizeBytes.Value);
                    if (!string.IsNullOrEmpty(sizeLabel))
                    {
                        importedSettings.Add("Multi-volume: Yes");
                        importedSettings.Add($"Volume size: {sizeLabel}");
                        volumeSizeApplied = true;
                    }
                }

                // Set volume flag when size can't be inferred.
                if (!volumeSizeApplied && srr.IsVolumeArchive.HasValue && srr.IsVolumeArchive.Value)
                {
                    cbSwitchV.Checked = true;
                    importedSettings.Add("Multi-volume: Yes (size unknown)");
                }
                
                // Set RAR version using MULTI-FACTOR ANALYSIS
                // We use multiple indicators from the SRR to narrow down the version more precisely:
                //   1. UnpVer (compression algorithm version)
                //   2. Feature flags (FirstVolume, Unicode, LargeFile, etc.)
                //   3. Dictionary size constraints
                //
                // Based on unrar source code (headers.hpp, arcread.cpp):
                // Key UnpVer values:
                //   13 = RAR 1.3, 15 = RAR 1.5, 20 = RAR 2.0, 26 = RAR 2.6, 29 = RAR 2.9/3.x/4.x,
                //   36 = RAR 3.6+, 50 = RAR 5.0, 70 = RAR 7.0
                //
                // Key Feature Introduction Versions (from unrar headers.hpp):
                //   MHD_FIRSTVOLUME (0x0100) - RAR 3.0+
                //   LHD_UNICODE (0x0200) - RAR 3.0+
                //   LHD_LARGE (0x0100) - RAR 2.6+ (files >2GB)
                //   MHD_NEWNUMBERING (0x0010) - RAR 2.9+
                //   LHD_EXTTIME (0x1000) - RAR 2.0+
                if (srr.RARVersion.HasValue)
                {
                    int unpVer = srr.RARVersion.Value;
                    
                    // Uncheck all versions first
                    cbRARVersion2.Checked = false;
                    cbRARVersion3.Checked = false;
                    cbRARVersion4.Checked = false;
                    cbRARVersion5.Checked = false;
                    cbRARVersion6.Checked = false;
                    
                    List<string> indicators = [];
                    
                    Log.Information(this, $"RAR version analysis (UnpVer={unpVer}):", LogTarget.System);

                    // PRIMARY: Check UnpVer and dictionary size for RAR 5.0+
                    if (unpVer >= 50 && unpVer < 70)
                    {
                        cbRARVersion5.Checked = true;
                        cbRARVersion6.Checked = true;
                        importedSettings.Add($"✓ RAR 5.0 Format (UNP_VER={unpVer})");
                        importedSettings.Add("Selected: RAR 5.x, 6.x");
                        Log.Information(this, "  RAR 5.0 format detected -> testing RAR 5.x, 6.x", LogTarget.System);
                    }
                    else if (unpVer >= 70)
                    {
                        cbRARVersion6.Checked = true;
                        importedSettings.Add($"✓ RAR 7.0+ Format (UNP_VER={unpVer})");
                        importedSettings.Add("Selected: RAR 6.x+");
                        Log.Information(this, "  RAR 7.0+ format detected -> testing RAR 6.x+", LogTarget.System);
                    }
                    // Check for large dictionaries (RAR 5.0+ exclusive)
                    else if (srr.DictionarySize.HasValue && srr.DictionarySize.Value > 4096)
                    {
                        cbRARVersion5.Checked = true;
                        cbRARVersion6.Checked = true;
                        importedSettings.Add($"✓ Large Dictionary ({srr.DictionarySize}KB) = RAR 5.0+");
                        importedSettings.Add("Selected: RAR 5.x, 6.x");
                        Log.Information(this, $"  Large dictionary ({srr.DictionarySize}KB) -> testing RAR 5.x, 6.x", LogTarget.System);
                    }
                    // SECONDARY: Use feature flags to narrow RAR 2.x/3.x/4.x range
                    // Also include RAR 5.x/6.x since they can create RAR4 format with -ma4 flag
                    else
                    {
                        // Start with UnpVer baseline
                        bool isRar2 = unpVer <= 29;
                        bool isRar3 = unpVer >= 20 && unpVer <= 36;
                        bool isRar4 = unpVer >= 26 && unpVer <= 36;
                        // RAR 5.x and 6.x can create RAR4 format archives with -ma4 flag
                        bool isRar5 = true;
                        bool isRar6 = true;

                        indicators.Add($"UnpVer={unpVer}");
                        indicators.Add("RAR4 format (5.x/6.x can create with -ma4)");

                        // Refine using feature flags
                        if (srr.HasFirstVolumeFlag == true || srr.HasUnicodeNames == true)
                        {
                            // These features were introduced in RAR 3.0
                            isRar2 = false; // Eliminate RAR 2.x
                            if (srr.HasFirstVolumeFlag == true) indicators.Add("FirstVolume flag (RAR 3.0+)");
                            if (srr.HasUnicodeNames == true) indicators.Add("Unicode names (RAR 3.0+)");
                        }

                        if (srr.HasNewVolumeNaming == true && unpVer < 29)
                        {
                            // New volume naming introduced in RAR 2.9
                            indicators.Add("New volume naming (RAR 2.9+)");
                        }

                        if (srr.HasLargeFiles == true && unpVer < 26)
                        {
                            // Large file support introduced in RAR 2.6
                            indicators.Add("Large file support (RAR 2.6+)");
                        }

                        // Special case: UnpVer=36 is RAR 3.6+ specific
                        if (unpVer == 36)
                        {
                            isRar2 = false;
                            isRar3 = true;
                            isRar4 = true;
                            indicators.Add("Alternative hash algorithm (RAR 3.6+)");
                        }

                        // Apply the refined selection
                        cbRARVersion2.Checked = isRar2;
                        cbRARVersion3.Checked = isRar3;
                        cbRARVersion4.Checked = isRar4;
                        cbRARVersion5.Checked = isRar5;
                        cbRARVersion6.Checked = isRar6;

                        // Build description
                        List<string> selectedVersions = [];
                        if (isRar2) selectedVersions.Add("2.x");
                        if (isRar3) selectedVersions.Add("3.x");
                        if (isRar4) selectedVersions.Add("4.x");
                        if (isRar5) selectedVersions.Add("5.x");
                        if (isRar6) selectedVersions.Add("6.x");

                        if (selectedVersions.Count > 0)
                        {
                            importedSettings.Add($"✓ Indicators: {string.Join(", ", indicators)}");
                            importedSettings.Add($"Selected: RAR {string.Join(", ", selectedVersions)}");
                            Log.Information(this, $"  Indicators: {string.Join(", ", indicators)}", LogTarget.System);
                            Log.Information(this, $"  Testing RAR versions: {string.Join(", ", selectedVersions)}", LogTarget.System);
                        }
                        else
                        {
                            // Fallback if somehow nothing selected
                            cbRARVersion2.Checked = true;
                            cbRARVersion3.Checked = true;
                            cbRARVersion4.Checked = true;
                            cbRARVersion5.Checked = true;
                            cbRARVersion6.Checked = true;
                            importedSettings.Add($"⚠ Uncertain version (UNP_VER={unpVer})");
                            importedSettings.Add("Selected: All versions (broad search)");
                            Log.Warning(this, $"  Uncertain version -> testing all RAR versions", LogTarget.System);
                        }
                    }
                }
                
                string? extractedVerificationFilePath = null;
                try
                {
                    extractedVerificationFilePath = TryExtractStoredSfv(ofd.FileName, srr, importedSettings);
                }
                catch (Exception ex)
                {
                    GUIHelper.ShowWarning(this, $"SRR imported, but failed to extract stored SFV: {ex.Message}");
                }

                if (!string.IsNullOrEmpty(extractedVerificationFilePath))
                {
                    VerificationFileExtracted?.Invoke(this, extractedVerificationFilePath);
                }

                if (importedSettings.Count > 0)
                {
                    Log.Information(this, $"=== SRR Import Complete ===", LogTarget.System);
                    string message = "SRR file imported successfully!\n\nSettings applied:\n" +
                                   string.Join("\n", importedSettings);
                    GUIHelper.ShowInfo(this, message);
                }
                else
                {
                    Log.Warning(this, "Could not extract any settings from SRR file", LogTarget.System);
                    GUIHelper.ShowWarning(this, "Could not extract any settings from SRR file.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(this, $"Failed to load SRR file: {ex.Message}", LogTarget.System);
                GUIHelper.ShowError(this, $"Failed to load SRR file: {ex.Message}");
            }
        }

        private static string? TryExtractStoredSfv(string srrFilePath, SRRFile srr, List<string> importedSettings)
        {
            if (srr.StoredFiles.Count == 0)
            {
                return null;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "WinRARRed", "srr-import", $"{Path.GetFileNameWithoutExtension(srrFilePath)}_{Guid.NewGuid():N}");
            string? extractedPath = srr.ExtractStoredFile(srrFilePath, tempDir, fileName =>
                Path.GetExtension(fileName).Equals(".sfv", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(extractedPath))
            {
                importedSettings.Add($"Stored SFV: {Path.GetFileName(extractedPath)}");
            }

            return extractedPath;
        }

        private string? ApplyVolumeSizeFromSrr(long sizeBytes)
        {
            if (sizeBytes <= 0)
            {
                return null;
            }

            cbSwitchV.Checked = true;

            if (sizeBytes % 1_000_000_000 == 0)
            {
                long value = sizeBytes / 1_000_000_000;
                tbSwitchSize.Text = value.ToString();
                rbSwitchSizeGB.Checked = true;
                return $"{value} GB";
            }

            if (sizeBytes % 1_000_000 == 0)
            {
                long value = sizeBytes / 1_000_000;
                tbSwitchSize.Text = value.ToString();
                rbSwitchSizeMB.Checked = true;
                return $"{value} MB";
            }

            if (sizeBytes % 1_000 == 0)
            {
                long value = sizeBytes / 1_000;
                tbSwitchSize.Text = value.ToString();
                rbSwitchSizeKB.Checked = true;
                return $"{value} KB";
            }

            if (sizeBytes % (1024L * 1024 * 1024) == 0)
            {
                long value = sizeBytes / (1024L * 1024 * 1024);
                tbSwitchSize.Text = value.ToString();
                rbSwitchSizeGiB.Checked = true;
                return $"{value} GiB";
            }

            if (sizeBytes % (1024L * 1024) == 0)
            {
                long value = sizeBytes / (1024L * 1024);
                tbSwitchSize.Text = value.ToString();
                rbSwitchSizeMiB.Checked = true;
                return $"{value} MiB";
            }

            if (sizeBytes % 1024 == 0)
            {
                long value = sizeBytes / 1024;
                tbSwitchSize.Text = value.ToString();
                rbSwitchSizeKiB.Checked = true;
                return $"{value} KiB";
            }

            tbSwitchSize.Text = sizeBytes.ToString();
            rbSwitchSizeBytes.Checked = true;
            return $"{sizeBytes} bytes";
        }

        private RAROptions GetRAROptions()
        {
            List<VersionRange> rarVersions = [];
            if (cbRARVersion2.Checked)
            {
                rarVersions.Add(new(200, 300));
            }

            if (cbRARVersion3.Checked)
            {
                rarVersions.Add(new(300, 400));
            }

            if (cbRARVersion4.Checked)
            {
                rarVersions.Add(new(400, 500));
            }

            if (cbRARVersion5.Checked)
            {
                rarVersions.Add(new(500, 600));
            }

            if (cbRARVersion6.Checked)
            {
                rarVersions.Add(new(600, 700));
            }

            return new()
            {
                SetFileArchiveAttribute = cbFileA.CheckState,
                SetFileNotContentIndexedAttribute = cbFileI.CheckState,
                CommandLineArguments = CreateCommandLineArgumentsCombinations(),
                RARVersions = rarVersions,
                DeleteRARFiles = cbDeleteRARFiles.Checked,
                StopOnFirstMatch = cbStopOnFirstMatch.Checked,
                ArchiveFileCrcs = new Dictionary<string, string>(ImportedArchiveFileCrcs, StringComparer.OrdinalIgnoreCase),
                ArchiveFilePaths = new HashSet<string>(ImportedArchiveFiles, StringComparer.OrdinalIgnoreCase),
                ArchiveDirectoryPaths = new HashSet<string>(ImportedArchiveDirectories, StringComparer.OrdinalIgnoreCase),
                DirectoryTimestamps = new Dictionary<string, DateTime>(ImportedDirectoryTimestamps, StringComparer.OrdinalIgnoreCase),
                DirectoryCreationTimes = new Dictionary<string, DateTime>(ImportedDirectoryCreationTimes, StringComparer.OrdinalIgnoreCase),
                DirectoryAccessTimes = new Dictionary<string, DateTime>(ImportedDirectoryAccessTimes, StringComparer.OrdinalIgnoreCase),
                FileTimestamps = new Dictionary<string, DateTime>(ImportedFileTimestamps, StringComparer.OrdinalIgnoreCase),
                FileCreationTimes = new Dictionary<string, DateTime>(ImportedFileCreationTimes, StringComparer.OrdinalIgnoreCase),
                FileAccessTimes = new Dictionary<string, DateTime>(ImportedFileAccessTimes, StringComparer.OrdinalIgnoreCase),
                ArchiveComment = ImportedArchiveComment,
                ArchiveCommentBytes = ImportedArchiveCommentBytes,
                CmtCompressedData = ImportedCmtCompressedData,
                CmtCompressionMethod = ImportedCmtCompressionMethod,
                // Host OS patching - use exact values detected from SRR
                EnableHostOSPatching = cbEnableHostOSPatching.Checked,
                DetectedFileHostOS = DetectedFileHostOS,
                DetectedFileAttributes = DetectedFileAttributes,
                DetectedCmtHostOS = DetectedCmtHostOS,
                DetectedCmtFileTime = DetectedCmtFileTime,
                DetectedCmtFileAttributes = DetectedCmtFileAttributes
            };
        }

        private void UpdateHostOSDetectedLabel()
        {
            if (DetectedFileHostOS.HasValue)
            {
                string osName = DetectedFileHostOS switch
                {
                    0 => "MS-DOS",
                    1 => "OS/2",
                    2 => "Windows",
                    3 => "Unix",
                    4 => "Mac OS",
                    5 => "BeOS",
                    _ => $"Unknown ({DetectedFileHostOS})"
                };
                lblHostOSDetected.Text = $"Detected: {osName} (0x{DetectedFileHostOS:X2})";
                lblHostOSInfo.Text = $"Attrs: 0x{DetectedFileAttributes ?? 0:X8}";
            }
            else
            {
                lblHostOSDetected.Text = "";
                lblHostOSInfo.Text = "Import an SRR to detect Host OS and attributes";
            }
        }

        private static void SetTimestampPrecisionCheckboxes(string prefix, TimestampPrecision precision,
            CheckBox cb0, CheckBox cb1, CheckBox cb2, CheckBox cb3, CheckBox cb4)
        {
            // Set only the checkbox matching the detected precision, uncheck others
            cb0.Checked = precision == TimestampPrecision.NotSaved;
            cb1.Checked = precision == TimestampPrecision.OneSecond;
            cb2.Checked = precision == TimestampPrecision.HighPrecision1;
            cb3.Checked = precision == TimestampPrecision.HighPrecision2;
            cb4.Checked = precision == TimestampPrecision.NtfsPrecision;
        }

        private List<RARCommandLineArgument[]> CreateCommandLineArgumentsCombinations()
        {
            List<RARCommandLineArgument> compressionLevels = [];
            AddSwitch(compressionLevels, cbSwitchM0, new("-m0", 200));
            AddSwitch(compressionLevels, cbSwitchM1, new("-m1", 200));
            AddSwitch(compressionLevels, cbSwitchM2, new("-m2", 200));
            AddSwitch(compressionLevels, cbSwitchM3, new("-m3", 200));
            AddSwitch(compressionLevels, cbSwitchM4, new("-m4", 200));
            AddSwitch(compressionLevels, cbSwitchM5, new("-m5", 200));

            List<RARCommandLineArgument> archivingFormats = [];
            AddSwitch(archivingFormats, cbSwitchMA4, new("-ma4", 500));
            AddSwitch(archivingFormats, cbSwitchMA5, new("-ma5", 500));

            // Dictionary sizes - use 'k' suffix for RAR 3.x-4.x compatibility (works across all versions)
            // RAR 2.x uses no suffix, RAR 3.x-4.x prefer 'k' suffix, RAR 5.x+ accepts both
            List<RARCommandLineArgument> dictionarySizes = [];
            AddSwitch(dictionarySizes, cbSwitchMD64K, new("-md64k", 200, RARArchiveVersion.RAR4));
            AddSwitch(dictionarySizes, cbSwitchMD128K, new("-md128k", 200, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD256K, new("-md256k", 200, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD512K, new("-md512k", 200, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD1024K, new("-md1024k", 200, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD2048K, new("-md2048k", 200, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD4096K, new("-md4096k", 200, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD8M, new("-md8m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD16M, new("-md16m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD32M, new("-md32m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD64M, new("-md64m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD128M, new("-md128m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD256M, new("-md256m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD512M, new("-md512m", 500, RARArchiveVersion.RAR5));
            AddSwitch(dictionarySizes, cbSwitchMD1G, new("-md1g", 500, RARArchiveVersion.RAR5));

            List<RARCommandLineArgument> modificationTimes = [];
            AddSwitch(modificationTimes, cbSwitchTSM0, new("-tsm0", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(modificationTimes, cbSwitchTSM1, new("-tsm1", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(modificationTimes, cbSwitchTSM2, new("-tsm2", 320, RARArchiveVersion.RAR4));
            AddSwitch(modificationTimes, cbSwitchTSM3, new("-tsm3", 320, RARArchiveVersion.RAR4));
            AddSwitch(modificationTimes, cbSwitchTSM4, new("-tsm4", 320, RARArchiveVersion.RAR4));

            List<RARCommandLineArgument> creationTimes = [];
            AddSwitch(creationTimes, cbSwitchTSC0, new("-tsc0", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(creationTimes, cbSwitchTSC1, new("-tsc1", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(creationTimes, cbSwitchTSC2, new("-tsc2", 320, RARArchiveVersion.RAR4));
            AddSwitch(creationTimes, cbSwitchTSC3, new("-tsc3", 320, RARArchiveVersion.RAR4));
            AddSwitch(creationTimes, cbSwitchTSC4, new("-tsc4", 320, RARArchiveVersion.RAR4));

            List<RARCommandLineArgument> accessTimes = [];
            AddSwitch(accessTimes, cbSwitchTSA0, new("-tsa0", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(accessTimes, cbSwitchTSA1, new("-tsa1", 320, RARArchiveVersion.RAR4 | RARArchiveVersion.RAR5));
            AddSwitch(accessTimes, cbSwitchTSA2, new("-tsa2", 320, RARArchiveVersion.RAR4));
            AddSwitch(accessTimes, cbSwitchTSA3, new("-tsa3", 320, RARArchiveVersion.RAR4));
            AddSwitch(accessTimes, cbSwitchTSA4, new("-tsa4", 320, RARArchiveVersion.RAR4));

            List<RARCommandLineArgument[]> switchesList = [];
            for (int a = 0; a < Math.Max(compressionLevels.Count, 1); a++)
            {
                RARCommandLineArgument? compressionLevel = compressionLevels.Count != 0 ? compressionLevels[a] : null;

                for (int b = 0; b < Math.Max(archivingFormats.Count, 1); b++)
                {
                    RARCommandLineArgument? archivingFormat = archivingFormats.Count != 0 ? archivingFormats[b] : null;

                    for (int c = 0; c < Math.Max(dictionarySizes.Count, 1); c++)
                    {
                        RARCommandLineArgument? dictionarySize = dictionarySizes.Count != 0 ? dictionarySizes[c] : null;

                        for (int d = 0; d < Math.Max(modificationTimes.Count, 1); d++)
                        {
                            RARCommandLineArgument? modificationTime = modificationTimes.Count != 0 ? modificationTimes[d] : null;

                            for (int e = 0; e < Math.Max(creationTimes.Count, 1); e++)
                            {
                                RARCommandLineArgument? creationTime = creationTimes.Count != 0 ? creationTimes[e] : null;

                                for (int f = 0; f < Math.Max(accessTimes.Count, 1); f++)
                                {
                                    RARCommandLineArgument? accessTime = accessTimes.Count != 0 ? accessTimes[f] : null;

                                    for (int x = 0; x < (cbSwitchAI.Checked ? 2 : 1); x++)
                                    {
                                        // Thread sweep: inclusive [start, end]; skip the stray iteration that produced no -mt.
                                        for (int z = cbSwitchMT.Checked ? (int)nupSwitchMTStart.Value : 0; z < (cbSwitchMT.Checked ? (int)nupSwitchMTEnd.Value + 1 : 1); z++)
                                        {
                                            List<RARCommandLineArgument> switches =
                                                [
                                                    // a Add files to archive
                                                    new("a", 200)
                                                ];

                                            if (x == 0 && cbSwitchAI.Checked)
                                            {
                                                // ai - Ignore file attributes
                                                switches.Add(new("-ai", 200));
                                            }

                                            if (cbSwitchR.Checked)
                                            {
                                                // -r Recurse subdirectories
                                                switches.Add(new("-r", 200));
                                            }

                                            if (cbSwitchDS.Checked)
                                            {
                                                // -ds Disable name sort for solid archive
                                                switches.Add(new("-ds", 200));
                                            }

                                            if (cbSwitchSDash.Checked)
                                            {
                                                // -s- Disable solid archiving
                                                switches.Add(new("-s-", 200));
                                            }

                                            if (compressionLevel != null)
                                            {
                                                // -m<0..5> Set compression level (0-store...3-default...5-maximal)
                                                switches.Add(compressionLevel);
                                            }

                                            if (archivingFormat != null)
                                            {
                                                // -ma[4|5] Specify a version of archiving format
                                                switches.Add(archivingFormat);
                                            }

                                            if (dictionarySize != null)
                                            {
                                                // -md<n>[k,m,g] Dictionary size in KB, MB or GB
                                                switches.Add(dictionarySize);
                                            }

                                            if (modificationTime != null)
                                            {
                                                // -tsm[N]
                                                switches.Add(modificationTime);
                                            }

                                            if (creationTime != null)
                                            {
                                                // -tsa[N]
                                                switches.Add(creationTime);
                                            }

                                            if (accessTime != null)
                                            {
                                                // -tsa[N]
                                                switches.Add(accessTime);
                                            }

                                            if (cbSwitchV.Checked)
                                            {
                                                // -v<size>[k,b] Create volumes with size=<size>*1000 [*1024, *1]
                                                // Old RAR versions support: (no suffix)=×1000, k=×1024, b=×1
                                                if (!long.TryParse(tbSwitchSize.Text, out long sizeValue))
                                                {
                                                    sizeValue = 15000; // Default to 15000 if parse fails
                                                }
                                                
                                                string volumeArg;
                                                
                                                // Convert to appropriate unit for RAR compatibility
                                                if (rbSwitchSizeBytes.Checked)
                                                {
                                                    // bytes: use 'b' suffix (×1)
                                                    volumeArg = $"-v{sizeValue}b";
                                                }
                                                else if (rbSwitchSizeKB.Checked)
                                                {
                                                    // KB (1000-based): no suffix (×1000)
                                                    volumeArg = $"-v{sizeValue}";
                                                }
                                                else if (rbSwitchSizeMB.Checked)
                                                {
                                                    // MB to KB: convert and use no suffix (×1000)
                                                    long kb = sizeValue * 1000;
                                                    volumeArg = $"-v{kb}";
                                                }
                                                else if (rbSwitchSizeGB.Checked)
                                                {
                                                    // GB to KB: convert and use no suffix (×1000)
                                                    long kb = sizeValue * 1000 * 1000;
                                                    volumeArg = $"-v{kb}";
                                                }
                                                else if (rbSwitchSizeKiB.Checked)
                                                {
                                                    // KiB (1024-based): use 'k' suffix (×1024)
                                                    volumeArg = $"-v{sizeValue}k";
                                                }
                                                else if (rbSwitchSizeMiB.Checked)
                                                {
                                                    // MiB to KiB: convert and use 'k' suffix (×1024)
                                                    long kib = sizeValue * 1024;
                                                    volumeArg = $"-v{kib}k";
                                                }
                                                else if (rbSwitchSizeGiB.Checked)
                                                {
                                                    // GiB to KiB: convert and use 'k' suffix (×1024)
                                                    long kib = sizeValue * 1024 * 1024;
                                                    volumeArg = $"-v{kib}k";
                                                }
                                                else
                                                {
                                                    // Default to KB (no suffix, ×1000)
                                                    volumeArg = $"-v{sizeValue}";
                                                }
                                                
                                                switches.Add(new(volumeArg, 200));
                                            }

                                            // -mt<threads> Set the number of threads
                                            if (cbSwitchMT.Checked)
                                            {
                                                switches.Add(new($"-mt{z}", 360));
                                            }

                                            switchesList.Add([.. switches]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return switchesList;
        }

        private static void AddSwitch(List<RARCommandLineArgument> list, CheckBox checkbox, RARCommandLineArgument argument)
        {
            if (checkbox.Checked)
            {
                list.Add(argument);
        }
    }
}
