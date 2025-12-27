using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WinRARRed.IO
{
    public class RARFile(string filePath)
    {
        public string FilePath { get; private set; } = filePath;

        public event EventHandler<OperationProgressEventArgs>? ExtractionProgress;

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

            using FileStream fileStream = File.OpenRead(FilePath);
            using RarArchive archive = RarArchive.Open(fileStream, new()
            {
                LookForHeader = true
            });

            archive.CompressedBytesRead += Archive_CompressedBytesRead;
            archive.EntryExtractionBegin += Archive_EntryExtractionBegin;
            archive.EntryExtractionEnd += Archive_EntryExtractionEnd;
            archive.FilePartExtractionBegin += Archive_FilePartExtractionBegin;

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
        }

        private void Archive_EntryExtractionEnd(object? sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
        }

        private void Archive_EntryExtractionBegin(object? sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
        {
        }

        private void Archive_FilePartExtractionBegin(object? sender, FilePartExtractionBeginEventArgs e)
        {
            if (sender is not RarArchive archive)
            {
                return;
            }
        }

        private void FireExtractionProgress(OperationProgressEventArgs e)
        {
            ExtractionProgress?.Invoke(this, e);
        }
    }
}
