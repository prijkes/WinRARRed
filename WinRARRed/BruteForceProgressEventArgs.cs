using System;
using WinRARRed.IO;

namespace WinRARRed
{
    public class BruteForceProgressEventArgs(string releaseDirectoryPath, string rarVersionDirectoryPath, string rarCommandLineArguments, long operationSize, long operationProgressed, DateTime startDateTime) : OperationProgressEventArgs(operationSize, operationProgressed, startDateTime)
    {
        public string ReleaseDirectoryPath { get; private set; } = releaseDirectoryPath;

        public string RARVersionDirectoryPath { get; private set; } = rarVersionDirectoryPath;

        public string RARCommandLineArguments { get; private set; } = rarCommandLineArguments;
    }
}
