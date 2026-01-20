using System.Collections.Generic;
using System;
using System.Windows.Forms;
using WinRARRed.Diagnostics;

namespace WinRARRed
{
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
    }
}
