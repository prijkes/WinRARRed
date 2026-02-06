using System.Collections.Generic;
using System;
using System.Windows.Forms;
using WinRARRed.Diagnostics;

namespace WinRARRed;

/// <summary>
/// Configuration options for RAR archive creation and brute-force operations.
/// Contains all parameters needed to reconstruct a RAR archive from SRR metadata.
/// </summary>
public class RAROptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to set the archive attribute on files before compressing.
    /// </summary>
    public CheckState SetFileArchiveAttribute { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the not content indexed attribute on files before compressing.
    /// </summary>
    public CheckState SetFileNotContentIndexedAttribute { get; set; }

    /// <summary>
    /// Gets or sets the command line arguments.
    /// </summary>
    /// <value>
    /// The command line arguments.
    /// </value>
    public List<RARCommandLineArgument[]> CommandLineArguments { get; set; } = [];

    /// <summary>
    /// Gets or sets the RAR versions to try.
    /// </summary>
    /// <value>
    /// A combination of bit flags of the RAR versions to try.
    /// </value>
    public List<VersionRange> RARVersions { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to delete the RAR files if checksum does not match.
    /// </summary>
    /// <value>
    ///   <c>true</c> if RAR files should be deleted when checksum does not match; otherwise, <c>false</c>.
    /// </value>
    public bool DeleteRARFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete duplicate CRC files.
    /// When enabled, only the first occurrence of each unique CRC is kept.
    /// </summary>
    /// <value>
    ///   <c>true</c> to delete files with duplicate CRCs; otherwise, <c>false</c>.
    /// </value>
    public bool DeleteDuplicateCRCFiles { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to stop brute-forcing on the first successful match.
    /// </summary>
    /// <value>
    ///   <c>true</c> to stop after finding the first matching RAR; <c>false</c> to continue testing all combinations.
    /// </value>
    public bool StopOnFirstMatch { get; set; } = true;

    /// <summary>
    /// Gets or sets expected CRC32 values for archived files (relative paths).
    /// </summary>
    public Dictionary<string, string> ArchiveFileCrcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the file paths to include when an SRR file list is present.
    /// </summary>
    public HashSet<string> ArchiveFilePaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the directory paths to include when an SRR file list is present.
    /// </summary>
    public HashSet<string> ArchiveDirectoryPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets directory modified times (mtime) keyed by relative path.
    /// </summary>
    public Dictionary<string, DateTime> DirectoryTimestamps { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets directory creation times (ctime) keyed by relative path.
    /// </summary>
    public Dictionary<string, DateTime> DirectoryCreationTimes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets directory access times (atime) keyed by relative path.
    /// </summary>
    public Dictionary<string, DateTime> DirectoryAccessTimes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets file modified times (mtime) keyed by relative path.
    /// </summary>
    public Dictionary<string, DateTime> FileTimestamps { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets file creation times (ctime) keyed by relative path.
    /// </summary>
    public Dictionary<string, DateTime> FileCreationTimes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets file access times (atime) keyed by relative path.
    /// </summary>
    public Dictionary<string, DateTime> FileAccessTimes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the SRR file list restricts inputs.
    /// </summary>
    public bool HasArchiveFileList => ArchiveFilePaths.Count > 0 || ArchiveDirectoryPaths.Count > 0;

    /// <summary>
    /// Gets or sets the archive comment to include when creating RAR files.
    /// Extracted from the CMT sub-block in the SRR file.
    /// </summary>
    public string? ArchiveComment { get; set; }

    /// <summary>
    /// Gets or sets the raw archive comment bytes for exact reconstruction.
    /// This preserves exact byte content including trailing nulls and line endings.
    /// </summary>
    public byte[]? ArchiveCommentBytes { get; set; }

    /// <summary>
    /// Gets or sets the raw CMT block compressed data from the SRR.
    /// Used for Phase 1 brute-force to compare generated CMT blocks.
    /// </summary>
    public byte[]? CmtCompressedData { get; set; }

    /// <summary>
    /// Gets or sets the CMT block compression method from the SRR.
    /// 0x30 = Store, 0x31-0x35 = Compressed (Fastest to Best).
    /// </summary>
    public byte? CmtCompressionMethod { get; set; }

    /// <summary>
    /// Gets a value indicating whether Phase 1 (comment brute-force) can be used.
    /// Requires CMT compressed data to be available.
    /// </summary>
    public bool CanUseCommentPhase => CmtCompressedData != null && CmtCompressedData.Length > 0;

    /// <summary>
    /// Gets or sets whether to enable Host OS patching.
    /// When true, patches the brute-forced RAR to match the original SRR values.
    /// </summary>
    public bool EnableHostOSPatching { get; set; }

    /// <summary>
    /// Gets or sets the detected Host OS from file headers in the SRR.
    /// This is the exact value we need to patch into the brute-forced RAR.
    /// </summary>
    public byte? DetectedFileHostOS { get; set; }

    /// <summary>
    /// Gets or sets the detected file attributes from file headers in the SRR.
    /// This is the exact value we need to patch into the brute-forced RAR file headers.
    /// </summary>
    public uint? DetectedFileAttributes { get; set; }

    /// <summary>
    /// Gets or sets the detected Host OS from CMT service block in the SRR.
    /// </summary>
    public byte? DetectedCmtHostOS { get; set; }

    /// <summary>
    /// Gets or sets the detected file time (DOS format) from CMT service block in the SRR.
    /// </summary>
    public uint? DetectedCmtFileTime { get; set; }

    /// <summary>
    /// Gets or sets the detected file attributes from CMT service block in the SRR.
    /// </summary>
    public uint? DetectedCmtFileAttributes { get; set; }

    /// <summary>
    /// Gets or sets whether to use old volume naming scheme (.rar, .r00, .r01) instead of new (.part01.rar).
    /// When true, adds the -vn flag to RAR command line to disable new numbering.
    /// Auto-detected from SRR when the NEW_NUMBERING archive flag (0x0010) is NOT set.
    /// </summary>
    public bool UseOldVolumeNaming { get; set; }

    /// <summary>
    /// Returns true if patching is needed (Host OS differs from current platform).
    /// </summary>
    public bool NeedsHostOSPatching
    {
        get
        {
            if (!EnableHostOSPatching || !DetectedFileHostOS.HasValue)
                return false;

            // Check if detected Host OS differs from current platform
            bool isCurrentWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            byte currentHostOS = isCurrentWindows ? (byte)2 : (byte)3;
            return DetectedFileHostOS.Value != currentHostOS;
        }
    }
}
