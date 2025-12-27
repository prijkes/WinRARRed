using System;

namespace WinRARRed.IO
{
    public class OperationStatusChangedEventArgs(OperationStatus newStatus) : EventArgs
    {
        public OperationStatus? OldStatus { get; private set; }

        public OperationStatus NewStatus { get; private set; } = newStatus;

        public OperationCompletionStatus? CompletionStatus { get; private set; }

        public OperationStatusChangedEventArgs(OperationStatus? oldStatus, OperationStatus newStatus)
            : this(newStatus)
        {
            OldStatus = oldStatus;
        }

        public OperationStatusChangedEventArgs(OperationStatus newStatus, OperationCompletionStatus? completionStatus)
            : this(newStatus)
        {
            CompletionStatus = completionStatus;
        }

        public OperationStatusChangedEventArgs(OperationStatus? oldStatus, OperationStatus newStatus, OperationCompletionStatus? completionStatus)
            : this(oldStatus, newStatus)
        {
            CompletionStatus = completionStatus;
        }
    }
}
