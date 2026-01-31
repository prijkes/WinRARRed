using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WinRARRed.IO;

public class RARFile(string filePath)
{
    public string FilePath { get; private set; } = filePath;

    public event EventHandler<OperationProgressEventArgs>? ExtractionProgress;

    private long _totalSize;
    private long _bytesRead;
    private DateTime _startTime;

    public Task ExtractAsync(string outputDirectory, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ExtractFile(FilePath, outputDirectory, cancellationToken), cancellationToken);
    }

    private void ExtractFile(string filePath, string outputDirectory, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        _startTime = DateTime.Now;
        _bytesRead = 0;

        using FileStream fileStream = File.OpenRead(FilePath);
        _totalSize = fileStream.Length;

        using RarArchive archive = RarArchive.Open(fileStream, new()
        {
            LookForHeader = true
        });

        archive.CompressedBytesRead += Archive_CompressedBytesRead;

        foreach (RarArchiveEntry entry in archive.Entries.Where(e => !e.IsDirectory))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            entry.WriteToDirectory(outputDirectory, new()
            {
                ExtractFullPath = true,
                Overwrite = false
            });
        }
    }

    private void Archive_CompressedBytesRead(object? sender, CompressedBytesReadEventArgs e)
    {
        _bytesRead += e.CompressedBytesRead;

        if (_totalSize > 0 && ExtractionProgress != null)
        {
            var progressArgs = new OperationProgressEventArgs(
                _totalSize,
                Math.Min(_bytesRead, _totalSize),
                _startTime);
            ExtractionProgress.Invoke(this, progressArgs);
        }
    }
}
