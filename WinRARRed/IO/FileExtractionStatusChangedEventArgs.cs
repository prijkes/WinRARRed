namespace WinRARRed.IO;

public class FileExtractionStatusChangedEventArgs : OperationStatusChangedEventArgs
{
    public RARFile RARFile { get; private set; }

    public FileExtractionStatusChangedEventArgs(RARFile rarFile, OperationStatus newStatus)
        : base(newStatus)
    {
        RARFile = rarFile;
    }

    public FileExtractionStatusChangedEventArgs(RARFile rarFile, OperationStatus? oldStatus, OperationStatus newStatus)
        : base(oldStatus, newStatus)
    {
        RARFile = rarFile;
    }

    public FileExtractionStatusChangedEventArgs(RARFile rarFile, OperationStatus newStatus, OperationCompletionStatus completionStatus)
        : base(newStatus, completionStatus)
    {
        RARFile = rarFile;
    }

    public FileExtractionStatusChangedEventArgs(RARFile rarFile, OperationStatus? oldStatus, OperationStatus newStatus, OperationCompletionStatus? completionStatus)
        : base(oldStatus, newStatus, completionStatus)
    {
        RARFile = rarFile;
    }
}
