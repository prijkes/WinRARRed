using WinRARRed.IO;

namespace WinRARRed
{
    public class BruteForceStatusChangedEventArgs : OperationStatusChangedEventArgs
    {
        public BruteForceStatusChangedEventArgs(OperationStatus newStatus) : base(newStatus)
        {
        }

        public BruteForceStatusChangedEventArgs(OperationStatus? oldStatus, OperationStatus newStatus) : base(oldStatus, newStatus)
        {
        }

        public BruteForceStatusChangedEventArgs(OperationStatus newStatus, OperationCompletionStatus? completionStatus) : base(newStatus, completionStatus)
        {
        }

        public BruteForceStatusChangedEventArgs(OperationStatus? oldStatus, OperationStatus newStatus, OperationCompletionStatus? completionStatus) : base(oldStatus, newStatus, completionStatus)
        {
        }
    }
}
