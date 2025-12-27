using System.Collections.Generic;
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
    }
}
