using System.Collections.Generic;
using WinRARRed.Cryptography;

namespace WinRARRed
{
    public class BruteForceOptions(string rarInstallationsDirectoryPath, string releaseDirectoryPath, string outputDirectoryPath)
    {
        /// <summary>
        /// Gets or sets the directory to which the RAR installation files have been extracted to.
        /// </summary>
        /// <value>
        /// The directory to which the RAR installation files have been extracted to.
        /// </value>
        public string RARInstallationsDirectoryPath { get; set; } = rarInstallationsDirectoryPath;

        /// <summary>
        /// Gets or sets the release directory which contains the files to RAR.
        /// </summary>
        /// <value>
        /// The release directory.
        /// </value>
        public string ReleaseDirectoryPath { get; set; } = releaseDirectoryPath;

        /// <summary>
        /// Gets or sets the output directory to save the temp RAR files.
        /// </summary>
        /// <value>
        /// The output directory to save the temp RAR files.
        /// </value>
        public string OutputDirectoryPath { get; set; } = outputDirectoryPath;

        /// <summary>
        /// Gets or sets the hashes which contain the expected hash of the RAR file(s).
        /// </summary>
        /// <value>
        /// The hashes.
        /// </value>
        public HashSet<string> Hashes { get; set; } = [];

        /// <summary>
        /// Gets or sets the type of the hash in the <see cref="Hashes"/> set.
        /// </summary>
        /// <value>
        /// The type of the hash.
        /// </value>
        public HashType HashType { get; set; }

        /// <summary>
        /// Gets or sets the RAR options.
        /// </summary>
        /// <value>
        /// The RAR options.
        /// </value>
        public RAROptions RAROptions { get; set; } = new();
    }
}
