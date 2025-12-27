namespace WinRARRed.Diagnostics
{
    public class RARCommandLineArgument(string argument, int minimumVersion, RARArchiveVersion? archiveVersion = null)
    {
        public string Argument { get; set; } = argument;

        public int MinimumVersion { get; set; } = minimumVersion;

        public int? MaximumVersion { get; set; }

        public RARArchiveVersion? ArchiveVersion { get; set; } = archiveVersion;

        public RARCommandLineArgument(string argument, int minimumVersion, int maximumVersion, RARArchiveVersion? archiveVersion = null)
            : this(argument, minimumVersion, archiveVersion)
        {
            MaximumVersion = maximumVersion;
        }
    }
}
