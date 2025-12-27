using System;

namespace WinRARRed.IO
{
    public class FileExtractionProgressEventArgs(RARFile rarFile, long operationSize, long operationProgressed, DateTime startDateTime) : OperationProgressEventArgs(operationSize, operationProgressed, startDateTime)
    {
        public RARFile RARFile { get; private set; } = rarFile;
    }
}
