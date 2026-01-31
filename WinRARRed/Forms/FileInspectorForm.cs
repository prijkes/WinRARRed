using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RARLib;
using SRRLib;

namespace WinRARRed.Forms;

public partial class FileInspectorForm : Form
{
    private string? _currentFilePath;

    public FileInspectorForm()
    {
        InitializeComponent();
    }

    public FileInspectorForm(string filePath) : this()
    {
        LoadFile(filePath);
    }

    private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "RAR/SRR Files|*.rar;*.srr|RAR Files|*.rar|SRR Files|*.srr|All Files|*.*",
            Title = "Open RAR or SRR File"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            LoadFile(ofd.FileName);
        }
    }

    private void LoadFile(string filePath)
    {
        treeView.Nodes.Clear();
        listView.Items.Clear();
        txtComment.Clear();
        _currentFilePath = filePath;

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            if (ext == ".srr")
            {
                LoadSRRFile(filePath);
            }
            else
            {
                LoadRARFile(filePath);
            }

            Text = $"File Inspector - {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadRARFile(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var reader = new BinaryReader(fs);

        var rootNode = treeView.Nodes.Add("RAR Archive");
        rootNode.Tag = filePath;

        bool isRar5 = RAR5HeaderReader.IsRAR5(fs);
        fs.Position = 0;

        if (isRar5)
        {
            LoadRAR5File(fs, rootNode);
        }
        else
        {
            LoadRAR4File(reader, rootNode);
        }

        rootNode.Expand();
    }

    private void LoadRAR4File(BinaryReader reader, TreeNode rootNode)
    {
        var headerReader = new RARHeaderReader(reader);
        int blockIndex = 0;

        while (headerReader.CanReadBaseHeader)
        {
            var block = headerReader.ReadBlock(parseContents: true);
            if (block == null) break;

            // Build block label
            string blockName = GetBlockTypeName(block.BlockType);
            string blockLabel = $"[{blockIndex}] {blockName}";

            // Add sub-type info for service blocks
            if (block.ServiceBlockInfo != null)
            {
                blockLabel = $"[{blockIndex}] {blockName} ({block.ServiceBlockInfo.SubType})";
            }
            else if (block.FileHeader != null)
            {
                blockLabel = $"[{blockIndex}] {blockName}: {block.FileHeader.FileName}";
            }

            var blockNode = rootNode.Nodes.Add(blockLabel);
            blockNode.Tag = block;
            blockIndex++;

            // Extract and store comment if present (will be shown when block is selected)
            if (block.ServiceBlockInfo != null && block.ServiceBlockInfo.SubType == "CMT")
            {
                var commentData = headerReader.ReadServiceBlockData(block);
                if (commentData != null)
                {
                    block.ServiceBlockInfo.CommentText = block.ServiceBlockInfo.IsStored
                        ? System.Text.Encoding.UTF8.GetString(commentData).TrimEnd('\0')
                        : RARLib.Decompression.RARDecompressor.DecompressComment(
                            commentData,
                            (int)block.ServiceBlockInfo.UnpackedSize,
                            block.ServiceBlockInfo.CompressionMethod,
                            isRAR5: false);
                }
            }

            headerReader.SkipBlock(block, includeData: block.BlockType != RAR4BlockType.FileHeader);
        }

        rootNode.Text = $"RAR Archive ({blockIndex} blocks)";
    }

    private static string GetBlockTypeName(RAR4BlockType blockType)
    {
        return blockType switch
        {
            RAR4BlockType.Marker => "Marker",
            RAR4BlockType.ArchiveHeader => "Archive Header",
            RAR4BlockType.FileHeader => "File Header",
            RAR4BlockType.Comment => "Comment (Old)",
            RAR4BlockType.AuthInfo => "Auth Info",
            RAR4BlockType.OldService => "Old Service",
            RAR4BlockType.Protect => "Protection/Recovery",
            RAR4BlockType.Sign => "Signature",
            RAR4BlockType.Service => "Service Block",
            RAR4BlockType.EndArchive => "End Archive",
            _ => $"Unknown (0x{(byte)blockType:X2})"
        };
    }

    private void LoadRAR5File(Stream stream, TreeNode rootNode)
    {
        stream.Seek(8, SeekOrigin.Begin);

        var headerReader = new RAR5HeaderReader(stream);
        var archiveNode = rootNode.Nodes.Add("Archive Info (RAR5)");
        var filesNode = rootNode.Nodes.Add("Files");

        var fileInfos = new List<RAR5FileInfo>();

        while (headerReader.CanReadBaseHeader)
        {
            var block = headerReader.ReadBlock();
            if (block == null) break;

            if (block.ArchiveInfo != null)
            {
                AddRAR5ArchiveInfoToList(block.ArchiveInfo);
            }

            if (block.FileInfo != null)
            {
                fileInfos.Add(block.FileInfo);
                var fileNode = filesNode.Nodes.Add(block.FileInfo.FileName);
                fileNode.Tag = block.FileInfo;
            }

            // Extract and store comment if present (will be shown when block is selected)
            if (block.ServiceBlockInfo != null && block.ServiceBlockInfo.SubType == "CMT")
            {
                var commentData = headerReader.ReadServiceBlockData(block);
                if (commentData != null)
                {
                    block.ServiceBlockInfo.CommentText = block.ServiceBlockInfo.IsStored
                        ? System.Text.Encoding.UTF8.GetString(commentData).TrimEnd('\0')
                        : RARLib.Decompression.RARDecompressor.DecompressComment(
                            commentData,
                            (int)block.ServiceBlockInfo.UnpackedSize,
                            (byte)(block.ServiceBlockInfo.CompressionMethod == 0 ? 0x30 : 0x30 + block.ServiceBlockInfo.CompressionMethod),
                            isRAR5: true);
                }
            }

            headerReader.SkipBlock(block);
        }

        filesNode.Text = $"Files ({fileInfos.Count})";
        filesNode.Expand();
    }

    private void LoadSRRFile(string filePath)
    {
        var srr = SRRFile.Load(filePath);

        var rootNode = treeView.Nodes.Add("SRR File");
        rootNode.Tag = srr;

        if (srr.HeaderBlock != null)
        {
            var headerNode = rootNode.Nodes.Add("SRR Header");
            headerNode.Tag = srr.HeaderBlock;
        }

        var archiveNode = rootNode.Nodes.Add("RAR Archive Info");
        archiveNode.Tag = srr;

        if (srr.OsoHashBlocks.Count > 0)
        {
            var osoNode = rootNode.Nodes.Add($"OSO Hashes ({srr.OsoHashBlocks.Count})");
            foreach (var oso in srr.OsoHashBlocks)
            {
                var node = osoNode.Nodes.Add(oso.FileName);
                node.Tag = oso;
            }
            osoNode.Expand();
        }

        if (srr.RarPaddingBlocks.Count > 0)
        {
            var paddingNode = rootNode.Nodes.Add($"RAR Padding ({srr.RarPaddingBlocks.Count})");
            foreach (var padding in srr.RarPaddingBlocks)
            {
                var node = paddingNode.Nodes.Add(padding.RarFileName);
                node.Tag = padding;
            }
        }

        if (srr.RarFiles.Count > 0)
        {
            var volumesNode = rootNode.Nodes.Add($"RAR Volumes ({srr.RarFiles.Count})");
            foreach (var rar in srr.RarFiles)
            {
                var volNode = volumesNode.Nodes.Add(rar.FileName);
                volNode.Tag = rar;
            }
            volumesNode.Expand();
        }

        if (srr.StoredFiles.Count > 0)
        {
            var storedNode = rootNode.Nodes.Add($"Stored Files ({srr.StoredFiles.Count})");
            foreach (var stored in srr.StoredFiles)
            {
                var fileNode = storedNode.Nodes.Add(stored.FileName);
                fileNode.Tag = stored;
            }
            storedNode.Expand();
        }

        if (srr.ArchivedFiles.Count > 0)
        {
            var archivedNode = rootNode.Nodes.Add($"Archived Files ({srr.ArchivedFiles.Count})");
            foreach (var file in srr.ArchivedFiles.OrderBy(f => f))
            {
                var fileNode = archivedNode.Nodes.Add(file);
                if (srr.ArchivedFileCrcs.TryGetValue(file, out var crc))
                {
                    fileNode.Text = $"{file} [CRC: {crc}]";
                }
            }
        }

        SetComment(srr.ArchiveComment);
        rootNode.Expand();
    }

    private void AddArchiveInfoToList(RARArchiveHeader header)
    {
        listView.Items.Clear();
        AddListItem("Format", "RAR 4.x");
        AddListItem("Block Position", $"0x{header.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{header.HeaderCrc:X4}");
        AddListItem("Header Size", $"{header.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{(ushort)header.Flags:X4}");
        AddListItem("CRC Valid", header.CrcValid ? "Yes" : "No");
        AddListItem("Volume", header.IsVolume ? "Yes" : "No");
        AddListItem("Solid", header.IsSolid ? "Yes" : "No");
        AddListItem("Recovery Record", header.HasRecoveryRecord ? "Yes" : "No");
        AddListItem("Locked", header.IsLocked ? "Yes" : "No");
        AddListItem("First Volume", header.IsFirstVolume ? "Yes" : "No");
        AddListItem("New Volume Naming", header.HasNewVolumeNaming ? "Yes" : "No");
        AddListItem("Encrypted Headers", header.HasEncryptedHeaders ? "Yes" : "No");
    }

    private void AddRAR5ArchiveInfoToList(RAR5ArchiveInfo info)
    {
        listView.Items.Clear();
        AddListItem("Format", "RAR 5.0");
        AddListItem("Archive Flags (Raw)", $"0x{info.ArchiveFlags:X}");
        AddListItem("Volume", info.IsVolume ? "Yes" : "No");
        AddListItem("Has Volume Number", info.HasVolumeNumber ? "Yes" : "No");
        if (info.VolumeNumber.HasValue)
            AddListItem("Volume Number", info.VolumeNumber.Value.ToString());
        AddListItem("Solid", info.IsSolid ? "Yes" : "No");
        AddListItem("Recovery Record", info.HasRecoveryRecord ? "Yes" : "No");
        AddListItem("Locked", info.IsLocked ? "Yes" : "No");
    }

    private void AddSRRInfoToList(SRRFile srr)
    {
        listView.Items.Clear();
        txtComment.Clear();

        // Show archive comment if present for SRR files
        if (!string.IsNullOrEmpty(srr.ArchiveComment))
        {
            SetComment(srr.ArchiveComment);
        }

        AddListItem("RAR Version", srr.RARVersion.HasValue ? (srr.RARVersion == 50 ? "RAR 5.0" : $"RAR {srr.RARVersion.Value / 10}.{srr.RARVersion.Value % 10}") : "Unknown");

        if (srr.CompressionMethod.HasValue)
            AddListItem("Compression Method", GetCompressionMethodName((byte)srr.CompressionMethod.Value));
        if (srr.DictionarySize.HasValue)
            AddListItem("Dictionary Size", $"{srr.DictionarySize.Value} KB");

        AddListItem("Solid Archive", srr.IsSolidArchive.HasValue ? (srr.IsSolidArchive.Value ? "Yes" : "No") : "Unknown");
        AddListItem("Volume Archive", srr.IsVolumeArchive.HasValue ? (srr.IsVolumeArchive.Value ? "Yes" : "No") : "Unknown");
        AddListItem("Recovery Record", srr.HasRecoveryRecord.HasValue ? (srr.HasRecoveryRecord.Value ? "Yes" : "No") : "Unknown");
        AddListItem("Encrypted Headers", srr.HasEncryptedHeaders.HasValue ? (srr.HasEncryptedHeaders.Value ? "Yes" : "No") : "Unknown");
        AddListItem("New Volume Naming", srr.HasNewVolumeNaming.HasValue ? (srr.HasNewVolumeNaming.Value ? "Yes" : "No") : "Unknown");
        AddListItem("First Volume Flag", srr.HasFirstVolumeFlag.HasValue ? (srr.HasFirstVolumeFlag.Value ? "Yes" : "No") : "Unknown");
        AddListItem("Large Files (64-bit)", srr.HasLargeFiles.HasValue ? (srr.HasLargeFiles.Value ? "Yes" : "No") : "Unknown");
        AddListItem("Unicode Names", srr.HasUnicodeNames.HasValue ? (srr.HasUnicodeNames.Value ? "Yes" : "No") : "Unknown");
        AddListItem("Extended Time", srr.HasExtendedTime.HasValue ? (srr.HasExtendedTime.Value ? "Yes" : "No") : "Unknown");

        if (srr.VolumeSizeBytes.HasValue)
            AddListItem("Volume Size", $"{srr.VolumeSizeBytes.Value:N0} bytes ({FormatSize(srr.VolumeSizeBytes.Value)})");
        if (srr.RarVolumeSizes.Count > 0)
        {
            AddListItem("Volume Sizes Count", srr.RarVolumeSizes.Count.ToString());
            var uniqueSizes = srr.RarVolumeSizes.Distinct().OrderByDescending(s => s).ToList();
            for (int i = 0; i < Math.Min(uniqueSizes.Count, 5); i++)
            {
                AddListItem($"  Unique Size {i + 1}", $"{uniqueSizes[i]:N0} bytes ({FormatSize(uniqueSizes[i])})");
            }
            if (uniqueSizes.Count > 5)
                AddListItem("  ...", $"({uniqueSizes.Count - 5} more)");
        }

        AddListItem("RAR Volumes", srr.RarFiles.Count.ToString());
        AddListItem("Stored Files", srr.StoredFiles.Count.ToString());
        AddListItem("Archived Files", srr.ArchivedFiles.Count.ToString());
        AddListItem("Archived Directories", srr.ArchivedDirectories.Count.ToString());

        AddListItem("File Timestamps", srr.ArchivedFileTimestamps.Count.ToString());
        AddListItem("File Creation Times", srr.ArchivedFileCreationTimes.Count.ToString());
        AddListItem("File Access Times", srr.ArchivedFileAccessTimes.Count.ToString());
        AddListItem("Dir Timestamps", srr.ArchivedDirectoryTimestamps.Count.ToString());
        AddListItem("Dir Creation Times", srr.ArchivedDirectoryCreationTimes.Count.ToString());
        AddListItem("Dir Access Times", srr.ArchivedDirectoryAccessTimes.Count.ToString());

        AddListItem("File CRCs", srr.ArchivedFileCrcs.Count.ToString());
        AddListItem("Header CRC Errors", srr.HeaderCrcMismatches.ToString());

        AddListItem("Has Comment", !string.IsNullOrEmpty(srr.ArchiveComment) ? "Yes" : "No");
    }

    private void AddListItem(string property, string value)
    {
        var item = new ListViewItem(property);
        item.SubItems.Add(value);
        listView.Items.Add(item);
    }

    private void SetComment(string? comment)
    {
        if (string.IsNullOrEmpty(comment))
        {
            txtComment.Clear();
            return;
        }

        comment = comment.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        txtComment.Text = comment;
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.##} {suffixes[i]}";
    }

    private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is RARBlockReadResult blockResult)
        {
            ShowBlockDetails(blockResult);
        }
        else if (e.Node?.Tag is RARFileHeader fileHeader)
        {
            ShowFileHeaderDetails(fileHeader);
        }
        else if (e.Node?.Tag is RAR5FileInfo fileInfo)
        {
            ShowRAR5FileDetails(fileInfo);
        }
        else if (e.Node?.Tag is SrrHeaderBlock srrHeader)
        {
            ShowSrrHeaderBlockDetails(srrHeader);
        }
        else if (e.Node?.Tag is SrrOsoHashBlock osoHash)
        {
            ShowOsoHashBlockDetails(osoHash);
        }
        else if (e.Node?.Tag is SrrRarPaddingBlock rarPadding)
        {
            ShowRarPaddingBlockDetails(rarPadding);
        }
        else if (e.Node?.Tag is SrrStoredFileBlock storedFile)
        {
            ShowStoredFileDetails(storedFile);
        }
        else if (e.Node?.Tag is SrrRarFileBlock rarFile)
        {
            ShowRarFileBlockDetails(rarFile);
        }
        else if (e.Node?.Tag is SRRFile srr)
        {
            AddSRRInfoToList(srr);
        }
    }

    private void ShowBlockDetails(RARBlockReadResult block)
    {
        listView.Items.Clear();
        txtComment.Clear();

        // Common block info
        AddListItem("Block Type", $"0x{(byte)block.BlockType:X2} ({GetBlockTypeName(block.BlockType)})");
        AddListItem("Block Position", $"0x{block.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{block.HeaderCrc:X4}");
        AddListItem("CRC Valid", block.CrcValid ? "Yes" : "No");
        AddListItem("Header Size", $"{block.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{block.Flags:X4}");
        AddListItem("Add Size", $"{block.AddSize} bytes");
        AddListItem("Total Size", $"{block.HeaderSize + block.AddSize} bytes");

        // Archive header details
        if (block.ArchiveHeader != null)
        {
            AddListItem("", "--- Archive Header ---");
            AddListItem("Volume", block.ArchiveHeader.IsVolume ? "Yes" : "No");
            AddListItem("Solid", block.ArchiveHeader.IsSolid ? "Yes" : "No");
            AddListItem("Recovery Record", block.ArchiveHeader.HasRecoveryRecord ? "Yes" : "No");
            AddListItem("Locked", block.ArchiveHeader.IsLocked ? "Yes" : "No");
            AddListItem("First Volume", block.ArchiveHeader.IsFirstVolume ? "Yes" : "No");
            AddListItem("New Volume Naming", block.ArchiveHeader.HasNewVolumeNaming ? "Yes" : "No");
            AddListItem("Encrypted Headers", block.ArchiveHeader.HasEncryptedHeaders ? "Yes" : "No");
        }

        // File header details
        if (block.FileHeader != null)
        {
            AddListItem("", "--- File Header ---");
            AddListItem("File Name", block.FileHeader.FileName);
            AddListItem("Type", block.FileHeader.IsDirectory ? "Directory" : "File");
            AddListItem("Unpacked Size", $"{block.FileHeader.UnpackedSize:N0} bytes");
            AddListItem("Packed Size", $"{block.FileHeader.PackedSize:N0} bytes");
            AddListItem("Host OS", GetHostOSName(block.FileHeader.HostOS));
            AddListItem("File CRC32", $"0x{block.FileHeader.FileCrc:X8}");
            AddListItem("Compression Method", GetCompressionMethodName(block.FileHeader.CompressionMethod));
            AddListItem("Dictionary Size", $"{block.FileHeader.DictionarySizeKB} KB");
            AddListItem("Unpack Version", $"{block.FileHeader.UnpackVersion / 10}.{block.FileHeader.UnpackVersion % 10}");
        }

        // Service block details (CMT, RR, etc.)
        if (block.ServiceBlockInfo != null)
        {
            var svc = block.ServiceBlockInfo;
            AddListItem("", "--- Service Block ---");
            AddListItem("Sub-Type", svc.SubType);
            AddListItem("Packed Size", $"{svc.PackedSize:N0} bytes");
            AddListItem("Unpacked Size", $"{svc.UnpackedSize:N0} bytes");
            AddListItem("Compression Method", $"0x{svc.CompressionMethod:X2} ({GetCompressionMethodName(svc.CompressionMethod)})");
            AddListItem("Host OS", $"{svc.HostOS} ({GetHostOSName(svc.HostOS)})");
            AddListItem("Data CRC", $"0x{svc.DataCrc:X8}");
            AddListItem("File Time (DOS)", $"0x{svc.FileTimeDOS:X8}");
            AddListItem("File Time (Parsed)", svc.FileTimeDOS == 0 ? "(not set)" : RARUtils.DosDateToDateTime(svc.FileTimeDOS)?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(invalid)");
            AddListItem("File Attributes", $"0x{svc.FileAttributes:X8}");
            AddListItem("Unpack Version", $"{svc.UnpackVersion / 10}.{svc.UnpackVersion % 10}");
            AddListItem("Is Stored", svc.IsStored ? "Yes" : "No");

            // Timestamp precision info
            AddListItem("", "--- Timestamp Precision ---");
            AddListItem("Mtime Precision", GetPrecisionName(svc.MtimePrecision));
            AddListItem("Ctime Precision", GetPrecisionName(svc.CtimePrecision));
            AddListItem("Atime Precision", GetPrecisionName(svc.AtimePrecision));

            // Decode flags for service blocks
            var flags = (RARFileFlags)block.Flags;
            AddListItem("", "--- Decoded Flags ---");
            AddListItem("Has Extended Time", (flags & RARFileFlags.ExtTime) != 0 ? "Yes" : "No");
            AddListItem("Dictionary Index", $"{((block.Flags & 0xE0) >> 5)} ({RARUtils.GetDictionarySize(flags)} KB)");

            // Show comment text if this is a CMT block
            if (svc.SubType == "CMT" && !string.IsNullOrEmpty(svc.CommentText))
            {
                SetComment(svc.CommentText);
            }
        }
    }

    private static string GetPrecisionName(TimestampPrecision precision)
    {
        return precision switch
        {
            TimestampPrecision.NotSaved => "Not Saved (-ts-)",
            TimestampPrecision.OneSecond => "1 Second (DOS)",
            TimestampPrecision.HighPrecision1 => "High Precision 1",
            TimestampPrecision.HighPrecision2 => "High Precision 2",
            TimestampPrecision.NtfsPrecision => "NTFS (100ns)",
            _ => $"Unknown ({precision})"
        };
    }

    private void ShowFileHeaderDetails(RARFileHeader header)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("File Name", header.FileName);
        AddListItem("Type", header.IsDirectory ? "Directory" : "File");
        AddListItem("Block Position", $"0x{header.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{header.HeaderCrc:X4}");
        AddListItem("Header Size", $"{header.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{(ushort)header.Flags:X4}");
        AddListItem("CRC Valid", header.CrcValid ? "Yes" : "No");
        AddListItem("Unpacked Size", $"{header.UnpackedSize:N0} bytes ({FormatSize((long)header.UnpackedSize)})");
        AddListItem("Packed Size", $"{header.PackedSize:N0} bytes ({FormatSize((long)header.PackedSize)})");
        AddListItem("File CRC32", header.FileCrc.ToString("X8"));
        AddListItem("Host OS", GetHostOSName(header.HostOS));
        AddListItem("Unpack Version", $"{header.UnpackVersion / 10}.{header.UnpackVersion % 10}");
        AddListItem("Compression Method", GetCompressionMethodName(header.CompressionMethod));
        AddListItem("Dictionary Size", $"{header.DictionarySizeKB} KB");
        AddListItem("File Attributes", $"0x{header.FileAttributes:X8}");
        if (header.ModifiedTime.HasValue)
            AddListItem("Modified Time", header.ModifiedTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (header.CreationTime.HasValue)
            AddListItem("Creation Time", header.CreationTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (header.AccessTime.HasValue)
            AddListItem("Access Time", header.AccessTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        AddListItem("Split Before", header.IsSplitBefore ? "Yes" : "No");
        AddListItem("Split After", header.IsSplitAfter ? "Yes" : "No");
        AddListItem("Encrypted", header.IsEncrypted ? "Yes" : "No");
        AddListItem("Unicode Name", header.HasUnicodeName ? "Yes" : "No");
        AddListItem("Extended Time", header.HasExtendedTime ? "Yes" : "No");
        AddListItem("Large File (64-bit)", header.HasLargeSize ? "Yes" : "No");
    }

    private static string GetHostOSName(byte hostOS)
    {
        return hostOS switch
        {
            0 => "MS-DOS",
            1 => "OS/2",
            2 => "Windows",
            3 => "Unix",
            4 => "Mac OS",
            5 => "BeOS",
            _ => $"Unknown ({hostOS})"
        };
    }

    private static string GetCompressionMethodName(byte method)
    {
        return method switch
        {
            0x00 or 0x30 => "Store",
            0x01 or 0x31 => "Fastest",
            0x02 or 0x32 => "Fast",
            0x03 or 0x33 => "Normal",
            0x04 or 0x34 => "Good",
            0x05 or 0x35 => "Best",
            _ => $"Unknown (0x{method:X2})"
        };
    }

    private void ShowRAR5FileDetails(RAR5FileInfo info)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("File Name", info.FileName);
        AddListItem("Type", info.IsDirectory ? "Directory" : "File");
        AddListItem("File Flags (Raw)", $"0x{info.FileFlags:X}");
        AddListItem("Unpacked Size", $"{info.UnpackedSize:N0} bytes ({FormatSize((long)info.UnpackedSize)})");
        AddListItem("File Attributes", $"0x{info.Attributes:X}");
        if (info.FileCrc.HasValue)
            AddListItem("File CRC32", info.FileCrc.Value.ToString("X8"));
        AddListItem("Compression Info (Raw)", $"0x{info.CompressionInfo:X}");
        AddListItem("Compression Method", info.IsStored ? "Store (0)" : $"Method {info.CompressionMethod}");
        AddListItem("Dict Size Power", info.DictSizePower.ToString());
        AddListItem("Dictionary Size", $"{info.DictionarySizeKB} KB");
        AddListItem("Host OS", GetRAR5HostOSName(info.HostOS));
        if (info.ModificationTime.HasValue)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(info.ModificationTime.Value).LocalDateTime;
            AddListItem("Modification Time", dt.ToString("yyyy-MM-dd HH:mm:ss"));
            AddListItem("Modification Time (Unix)", info.ModificationTime.Value.ToString());
        }
        AddListItem("Split Before", info.IsSplitBefore ? "Yes" : "No");
        AddListItem("Split After", info.IsSplitAfter ? "Yes" : "No");
    }

    private static string GetRAR5HostOSName(ulong hostOS)
    {
        return hostOS switch
        {
            0 => "Windows",
            1 => "Unix",
            _ => $"Unknown ({hostOS})"
        };
    }

    private void ShowStoredFileDetails(SrrStoredFileBlock stored)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("File Name", stored.FileName);
        AddListItem("Block Type", $"0x{(byte)stored.BlockType:X2} ({stored.BlockType})");
        AddListItem("Block Position", $"0x{stored.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{stored.Crc:X4}");
        AddListItem("Header Size", $"{stored.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{stored.Flags:X4}");
        AddListItem("Add Size", $"{stored.AddSize} bytes");
        AddListItem("File Length", $"{stored.FileLength:N0} bytes ({FormatSize(stored.FileLength)})");
        AddListItem("Data Offset", $"0x{stored.DataOffset:X}");
    }

    private void ShowRarFileBlockDetails(SrrRarFileBlock rar)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("RAR Volume", rar.FileName);
        AddListItem("Block Type", $"0x{(byte)rar.BlockType:X2} ({rar.BlockType})");
        AddListItem("Block Position", $"0x{rar.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{rar.Crc:X4}");
        AddListItem("Header Size", $"{rar.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{rar.Flags:X4}");
        AddListItem("Add Size", $"{rar.AddSize} bytes");
    }

    private void ShowSrrHeaderBlockDetails(SrrHeaderBlock header)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("Block Type", $"0x{(byte)header.BlockType:X2} ({header.BlockType})");
        AddListItem("Block Position", $"0x{header.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{header.Crc:X4}");
        AddListItem("Header Size", $"{header.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{header.Flags:X4}");
        AddListItem("Has App Name", header.HasAppName ? "Yes" : "No");
        if (!string.IsNullOrEmpty(header.AppName))
            AddListItem("App Name", header.AppName);
    }

    private void ShowOsoHashBlockDetails(SrrOsoHashBlock oso)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("File Name", oso.FileName);
        AddListItem("Block Type", $"0x{(byte)oso.BlockType:X2} ({oso.BlockType})");
        AddListItem("Block Position", $"0x{oso.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{oso.Crc:X4}");
        AddListItem("Header Size", $"{oso.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{oso.Flags:X4}");
        AddListItem("File Size", $"{oso.FileSize:N0} bytes ({FormatSize((long)oso.FileSize)})");
        AddListItem("OSO Hash", BitConverter.ToString(oso.OsoHash).Replace("-", ""));
    }

    private void ShowRarPaddingBlockDetails(SrrRarPaddingBlock padding)
    {
        listView.Items.Clear();
        txtComment.Clear();
        AddListItem("RAR File Name", padding.RarFileName);
        AddListItem("Block Type", $"0x{(byte)padding.BlockType:X2} ({padding.BlockType})");
        AddListItem("Block Position", $"0x{padding.BlockPosition:X}");
        AddListItem("Header CRC", $"0x{padding.Crc:X4}");
        AddListItem("Header Size", $"{padding.HeaderSize} bytes");
        AddListItem("Flags (Raw)", $"0x{padding.Flags:X4}");
        AddListItem("Add Size", $"{padding.AddSize} bytes");
        AddListItem("Padding Size", $"{padding.PaddingSize:N0} bytes");
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void FileInspectorForm_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void FileInspectorForm_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            LoadFile(files[0]);
        }
    }

    private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            treeView.SelectedNode = e.Node;
        }
    }

    private void contextMenuTree_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
        bool canExport = treeView.SelectedNode?.Tag is SrrStoredFileBlock && _currentFilePath != null;
        exportToolStripMenuItem.Enabled = canExport;

        if (!canExport)
        {
            e.Cancel = true;
        }
    }

    private void exportToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (treeView.SelectedNode?.Tag is not SrrStoredFileBlock storedFile || _currentFilePath == null)
            return;

        using var sfd = new SaveFileDialog
        {
            FileName = Path.GetFileName(storedFile.FileName),
            Title = "Export Stored File",
            Filter = "All Files|*.*"
        };

        if (sfd.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            ExportStoredFile(storedFile, sfd.FileName);
            MessageBox.Show($"File exported successfully to:\n{sfd.FileName}", "Export Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting file: {ex.Message}", "Export Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportStoredFile(SrrStoredFileBlock storedFile, string outputPath)
    {
        if (_currentFilePath == null)
            throw new InvalidOperationException("No SRR file loaded.");

        using var fs = File.OpenRead(_currentFilePath);
        fs.Seek(storedFile.DataOffset, SeekOrigin.Begin);

        using var output = File.Create(outputPath);
        byte[] buffer = new byte[81920];
        long remaining = storedFile.FileLength;

        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = fs.Read(buffer, 0, toRead);
            if (read <= 0)
                throw new EndOfStreamException("Unexpected end of SRR file.");
            output.Write(buffer, 0, read);
            remaining -= read;
        }
    }
}
