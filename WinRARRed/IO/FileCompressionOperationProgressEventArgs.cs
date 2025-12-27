using System;

namespace WinRARRed.IO
{
    public class FileCompressionOperationProgressEventArgs(long operationSize, long operationProgressed, DateTime startDateTime, string compressedFilePath) : OperationProgressEventArgs(operationSize, operationProgressed, startDateTime)
    {
        public string CompressedFilePath { get; } = compressedFilePath;

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            Cancelled = true;
        }
    }
}
