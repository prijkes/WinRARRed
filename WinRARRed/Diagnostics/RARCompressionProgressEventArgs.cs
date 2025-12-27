using System;
using WinRARRed.IO;

namespace WinRARRed.Diagnostics
{
    public class RARCompressionProgressEventArgs(RARProcess process, long operationSize, long operationProgressed, DateTime startDateTime) : OperationProgressEventArgs(operationSize, operationProgressed, startDateTime)
    {
        public RARProcess Process { get; private set; } = process;
    }
}
