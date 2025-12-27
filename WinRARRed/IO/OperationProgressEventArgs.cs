using System;

namespace WinRARRed.IO
{
    public class OperationProgressEventArgs : EventArgs
    {
        public long OperationSize { get; private set; }

        public long OperationProgressed { get; private set; }

        public long OperationRemaining { get; private set; }

        public DateTime StartDateTime { get; private set; }

        public TimeSpan TimeElapsed { get; private set; }

        public TimeSpan TimeRemaining { get; private set; }

        public long OperationSpeed { get; private set; }

        public DateTime EstimatedFinishDateTime { get; private set; }

        public double Progress { get; private set; }

        public OperationProgressEventArgs(long operationSize, long operationProgressed, DateTime startDateTime)
        {
            if (operationSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(operationSize));
            }

            if (operationProgressed > operationSize)
            {
                throw new ArgumentOutOfRangeException(nameof(operationProgressed));
            }

            OperationSize = operationSize;
            OperationProgressed = operationProgressed;
            OperationRemaining = OperationSize - OperationProgressed;
            StartDateTime = startDateTime;
            TimeElapsed = DateTime.Now.Subtract(StartDateTime);
            TimeRemaining = OperationProgressed < 1 ? default : TimeSpan.FromSeconds(TimeElapsed.TotalSeconds / OperationProgressed * OperationRemaining);
            OperationSpeed = TimeElapsed.TotalSeconds < 1 ? OperationProgressed : (long)(OperationProgressed / TimeElapsed.TotalSeconds);
            EstimatedFinishDateTime = DateTime.Now.Add(TimeRemaining);
            Progress = 100.0 / OperationSize * OperationProgressed;
        }
    }
}
