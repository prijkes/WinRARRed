using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RARLib;
using SRRLib;
using WinRARRed.Controls;

namespace WinRARRed.Forms;

public partial class FileInspectorForm : Form
{
    private static readonly Color PropertyHasOffsetColor = Color.FromArgb(232, 245, 255);

    private string? _currentFilePath;
    private byte[]? _fileBytes;
    private Dictionary<string, ByteRange> _propertyOffsets = [];
    private RARDetailedBlock? _currentDetailedBlock;
    private List<TreeNode>? _allTreeNodes;

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

    private void ShowHexViewToolStripMenuItem_Click(object sender, EventArgs e)
    {
        splitContainerVertical.Panel2Collapsed = !showHexViewToolStripMenuItem.Checked;
    }

    private void LoadFile(string filePath)
    {
        treeView.Nodes.Clear();
        listView.Items.Clear();

        hexView.Clear();
        txtTreeFilter.Clear();
        _allTreeNodes = null;
        lblTreeFilterCount.Text = "";
        _currentFilePath = filePath;
        _propertyOffsets.Clear();
        _currentDetailedBlock = null;

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            // Load file bytes for hex view
            _fileBytes = File.ReadAllBytes(filePath);
            hexView.LoadData(_fileBytes);
            groupBoxHex.Text = $"Hex View - {Path.GetFileName(filePath)}";

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
            _fileBytes = null;
            hexView.Clear();
            groupBoxHex.Text = "Hex View";
        }
    }

    private void TxtTreeFilter_TextChanged(object? sender, EventArgs e)
    {
        ApplyTreeFilter();
    }

    private void ApplyTreeFilter()
    {
        string filter = txtTreeFilter.Text.Trim();

        if (string.IsNullOrEmpty(filter))
        {
            // Restore all nodes if filter cleared
            if (_allTreeNodes != null)
            {
                RestoreAllNodes();
                _allTreeNodes = null;
            }
            lblTreeFilterCount.Text = "";
            return;
        }

        // On first filter, snapshot all nodes
        if (_allTreeNodes == null)
        {
            _allTreeNodes = SnapshotNodes();
        }

        // Rebuild tree showing only matching nodes and their ancestors
        treeView.BeginUpdate();
        treeView.Nodes.Clear();

        int matchCount = 0;
        foreach (var rootSnapshot in _allTreeNodes)
        {
            var filtered = FilterNode(rootSnapshot, filter, ref matchCount);
            if (filtered != null)
            {
                treeView.Nodes.Add(filtered);
            }
        }
        treeView.ExpandAll();
        treeView.EndUpdate();

        lblTreeFilterCount.Text = $"{matchCount} found";
    }

    private List<TreeNode> SnapshotNodes()
    {
        var result = new List<TreeNode>();
        foreach (TreeNode node in treeView.Nodes)
        {
            result.Add(CloneNode(node));
        }
        return result;
    }

    private static TreeNode CloneNode(TreeNode source)
    {
        var clone = new TreeNode(source.Text) { Tag = source.Tag };
        foreach (TreeNode child in source.Nodes)
        {
            clone.Nodes.Add(CloneNode(child));
        }
        return clone;
    }

    private void RestoreAllNodes()
    {
        if (_allTreeNodes == null) return;
        treeView.BeginUpdate();
        treeView.Nodes.Clear();
        foreach (var node in _allTreeNodes)
        {
            treeView.Nodes.Add(CloneNode(node));
        }
        // Expand root nodes
        foreach (TreeNode node in treeView.Nodes)
        {
            node.Expand();
        }
        treeView.EndUpdate();
    }

    private static TreeNode? FilterNode(TreeNode source, string filter, ref int matchCount)
    {
        bool selfMatches = source.Text.Contains(filter, StringComparison.OrdinalIgnoreCase);

        // Check children
        var matchedChildren = new List<TreeNode>();
        foreach (TreeNode child in source.Nodes)
        {
            var filtered = FilterNode(child, filter, ref matchCount);
            if (filtered != null)
                matchedChildren.Add(filtered);
        }

        if (selfMatches)
        {
            matchCount++;
            // Include this node with all its original children
            var result = new TreeNode(source.Text) { Tag = source.Tag };
            foreach (TreeNode child in source.Nodes)
            {
                result.Nodes.Add(CloneNode(child));
            }
            return result;
        }

        if (matchedChildren.Count > 0)
        {
            // Include this node as ancestor with only matched children
            var result = new TreeNode(source.Text) { Tag = source.Tag };
            foreach (var child in matchedChildren)
            {
                result.Nodes.Add(child);
            }
            return result;
        }

        return null;
    }

    private void LoadRARFile(string filePath)
    {
        using var fs = File.OpenRead(filePath);

        var rootNode = treeView.Nodes.Add("RAR Archive");
        rootNode.Tag = filePath;

        var detailedBlocks = RARDetailedParser.Parse(fs);

        int fileCount = 0;
        for (int i = 0; i < detailedBlocks.Count; i++)
        {
            var block = detailedBlocks[i];
            string blockLabel = $"[{i}] {block.BlockType}";
            if (!string.IsNullOrEmpty(block.ItemName))
                blockLabel = $"[{i}] {block.BlockType}: {block.ItemName}";

            var blockNode = rootNode.Nodes.Add(blockLabel);
            blockNode.Tag = block;

            if (block.BlockType.Contains("File"))
                fileCount++;
        }

        rootNode.Text = $"RAR Archive ({detailedBlocks.Count} blocks, {fileCount} files)";
        rootNode.Expand();
    }


    private void LoadSRRFile(string filePath)
    {
        var srrData = SRRFileData.Load(filePath);
        var srr = srrData.SrrFile;

        var rootNode = treeView.Nodes.Add("SRR File");
        rootNode.Tag = "root"; // Use string tag to identify root node

        if (srr.HeaderBlock != null)
        {
            var headerNode = rootNode.Nodes.Add("SRR Header");
            headerNode.Tag = srr.HeaderBlock;
        }

        // Only show RAR Archive Info if the SRR contains RAR blocks
        if (srr.RarFiles.Count > 0)
        {
            var archiveNode = rootNode.Nodes.Add("RAR Archive Info");
            archiveNode.Tag = srr;
        }

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

                // Add detailed RAR block child nodes from pre-parsed data
                if (srrData.VolumeDetailedBlocks.TryGetValue(rar.FileName, out var detailedBlocks))
                {
                    for (int i = 0; i < detailedBlocks.Count; i++)
                    {
                        var block = detailedBlocks[i];
                        string blockLabel = $"[{i}] {block.BlockType}";
                        if (!string.IsNullOrEmpty(block.ItemName))
                            blockLabel = $"[{i}] {block.BlockType}: {block.ItemName}";

                        var blockNode = volNode.Nodes.Add(blockLabel);
                        blockNode.Tag = block;
                    }
                }
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


    private void AddSRRInfoToList(SRRFile srr)
    {
        listView.Items.Clear();


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
        // Tint rows that have hex offset mappings so user knows they're clickable
        if (_propertyOffsets.ContainsKey(property))
        {
            item.BackColor = PropertyHasOffsetColor;
        }
        listView.Items.Add(item);
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
        _propertyOffsets.Clear();
        _currentDetailedBlock = null;

        if (e.Node?.Tag is RARDetailedBlock detailedBlock)
        {
            _currentDetailedBlock = detailedBlock;
            ShowDetailedBlockInfo(detailedBlock);
            HighlightBlockInHexView(detailedBlock.StartOffset, (int)detailedBlock.TotalSize);
        }
        else if (e.Node?.Tag is SrrHeaderBlock srrHeader)
        {
            ShowSrrHeaderBlockDetails(srrHeader);
            HighlightBlockInHexView(srrHeader.BlockPosition, srrHeader.HeaderSize);
        }
        else if (e.Node?.Tag is SrrOsoHashBlock osoHash)
        {
            ShowOsoHashBlockDetails(osoHash);
            HighlightBlockInHexView(osoHash.BlockPosition, osoHash.HeaderSize);
        }
        else if (e.Node?.Tag is SrrRarPaddingBlock rarPadding)
        {
            ShowRarPaddingBlockDetails(rarPadding);
            HighlightBlockInHexView(rarPadding.BlockPosition, rarPadding.HeaderSize + rarPadding.AddSize);
        }
        else if (e.Node?.Tag is SrrStoredFileBlock storedFile)
        {
            ShowStoredFileDetails(storedFile);
            HighlightBlockInHexView(storedFile.BlockPosition, storedFile.HeaderSize + storedFile.AddSize);
        }
        else if (e.Node?.Tag is SrrRarFileBlock rarFile)
        {
            ShowRarFileBlockDetails(rarFile);
            HighlightBlockInHexView(rarFile.BlockPosition, rarFile.HeaderSize + rarFile.AddSize);
        }
        else if (e.Node?.Tag is SRRFile srr)
        {
            // Only show info for "RAR Archive Info" node, not for root "SRR File" node
            AddSRRInfoToList(srr);
            hexView.ShowFullFile();
        }
        else if (e.Node?.Tag is string tagStr && tagStr == "root")
        {
            // Root "SRR File" node - just clear and show nothing
            listView.Items.Clear();
    
            hexView.ShowFullFile();
        }
        else
        {
            // Unknown or container node - clear the view
            listView.Items.Clear();
            hexView.ShowFullFile();
        }
    }

    private void HighlightBlockInHexView(long offset, long size)
    {
        if (_fileBytes == null) return;
        hexView.LoadBlockData(offset, (int)Math.Min(size, int.MaxValue));
    }

    private void listView_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (listView.SelectedItems.Count > 0)
        {
            var item = listView.SelectedItems[0];
            string propertyName = item.Text;

            if (_propertyOffsets.TryGetValue(propertyName, out var range))
            {
                hexView.SelectRange(range.Offset, range.Length);
            }
            else
            {
                hexView.ClearSelection();
            }
        }
    }    private static string GetCompressionMethodName(byte method)
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

    private void ShowStoredFileDetails(SrrStoredFileBlock stored)
    {
        listView.Items.Clear();

        _propertyOffsets.Clear();

        long pos = stored.BlockPosition;
        long p = pos;

        // Fields in byte order: CRC(0), Type(2), Flags(3), Size(5), AddSize(7), NameLen(11), FileName(13)
        _propertyOffsets["Header CRC"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Header CRC", $"0x{stored.Crc:X4}");
        _propertyOffsets["Block Type"] = new ByteRange { Offset = p + 2, Length = 1 };
        AddListItem("Block Type", $"0x{(byte)stored.BlockType:X2} ({stored.BlockType})");
        _propertyOffsets["Flags"] = new ByteRange { Offset = p + 3, Length = 2 };
        AddListItem("Flags", $"0x{stored.Flags:X4}");
        _propertyOffsets["Header Size"] = new ByteRange { Offset = p + 5, Length = 2 };
        AddListItem("Header Size", $"{stored.HeaderSize} bytes");
        p += 7;

        // StoredFile always has AddSize (file data length)
        _propertyOffsets["Add Size"] = new ByteRange { Offset = p, Length = 4 };
        AddListItem("Add Size", $"{stored.AddSize} bytes");
        p += 4;

        int nameLen = Encoding.UTF8.GetByteCount(stored.FileName);
        _propertyOffsets["Name Length"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Name Length", $"{nameLen} bytes");
        p += 2;
        _propertyOffsets["File Name"] = new ByteRange { Offset = p, Length = nameLen };
        AddListItem("File Name", stored.FileName);

        // Stored file data follows the header
        if (stored.FileLength > 0)
        {
            _propertyOffsets["File Data"] = new ByteRange { Offset = stored.DataOffset, Length = (int)Math.Min(stored.FileLength, int.MaxValue) };
            AddListItem("File Data", $"{stored.FileLength:N0} bytes ({FormatSize(stored.FileLength)})");
        }
    }

    private void ShowRarFileBlockDetails(SrrRarFileBlock rar)
    {
        listView.Items.Clear();

        _propertyOffsets.Clear();

        long p = rar.BlockPosition;

        // Fields in byte order: CRC(0), Type(2), Flags(3), Size(5), [AddSize(7)], NameLen, RarFileName
        _propertyOffsets["Header CRC"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Header CRC", $"0x{rar.Crc:X4}");
        _propertyOffsets["Block Type"] = new ByteRange { Offset = p + 2, Length = 1 };
        AddListItem("Block Type", $"0x{(byte)rar.BlockType:X2} ({rar.BlockType})");
        _propertyOffsets["Flags"] = new ByteRange { Offset = p + 3, Length = 2 };
        AddListItem("Flags", $"0x{rar.Flags:X4}");
        _propertyOffsets["Header Size"] = new ByteRange { Offset = p + 5, Length = 2 };
        AddListItem("Header Size", $"{rar.HeaderSize} bytes");
        p += 7;

        if ((rar.Flags & (ushort)SRRBlockFlags.LongBlock) != 0)
        {
            _propertyOffsets["Add Size"] = new ByteRange { Offset = p, Length = 4 };
            AddListItem("Add Size", $"{rar.AddSize} bytes");
            p += 4;
        }

        int nameLen = Encoding.UTF8.GetByteCount(rar.FileName);
        _propertyOffsets["Name Length"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Name Length", $"{nameLen} bytes");
        p += 2;
        _propertyOffsets["RAR Volume"] = new ByteRange { Offset = p, Length = nameLen };
        AddListItem("RAR Volume", rar.FileName);
    }

    private void ShowDetailedBlockInfo(RARDetailedBlock block)
    {
        listView.Items.Clear();

        _propertyOffsets.Clear();

        // All fields in byte order
        foreach (var field in block.Fields)
        {
            string value = field.Value;
            if (!string.IsNullOrEmpty(field.Description) && field.Description != field.Value)
                value = $"{field.Value} ({field.Description})";

            // Store offset for hex view highlighting before AddListItem so tinting works
            if (field.Length > 0)
            {
                _propertyOffsets[field.Name] = new ByteRange
                {
                    PropertyName = field.Name,
                    Offset = field.Offset,
                    Length = field.Length
                };
            }

            AddListItem(field.Name, value);

            // Flag children indented
            foreach (var child in field.Children)
            {
                AddListItem($"  {child.Name}", child.Value);
            }
        }
    }

    private void ShowSrrHeaderBlockDetails(SrrHeaderBlock header)
    {
        listView.Items.Clear();

        _propertyOffsets.Clear();

        long pos = header.BlockPosition;

        // Fields in byte order: CRC(0), Type(2), Flags(3), Size(5), [AppNameLen+AppName(7)]
        _propertyOffsets["Header CRC"] = new ByteRange { Offset = pos, Length = 2 };
        AddListItem("Header CRC", $"0x{header.Crc:X4}");
        _propertyOffsets["Block Type"] = new ByteRange { Offset = pos + 2, Length = 1 };
        AddListItem("Block Type", $"0x{(byte)header.BlockType:X2} ({header.BlockType})");
        _propertyOffsets["Flags"] = new ByteRange { Offset = pos + 3, Length = 2 };
        AddListItem("Flags", $"0x{header.Flags:X4}");
        _propertyOffsets["Header Size"] = new ByteRange { Offset = pos + 5, Length = 2 };
        AddListItem("Header Size", $"{header.HeaderSize} bytes");
        if (!string.IsNullOrEmpty(header.AppName))
        {
            _propertyOffsets["App Name"] = new ByteRange { Offset = pos + 7, Length = header.AppName.Length + 2 };
            AddListItem("App Name", header.AppName);
        }
    }

    private void ShowOsoHashBlockDetails(SrrOsoHashBlock oso)
    {
        listView.Items.Clear();

        _propertyOffsets.Clear();

        long p = oso.BlockPosition;

        // Fields in byte order: CRC(0), Type(2), Flags(3), Size(5), [AddSize(7)], NameLen, Name, FileSize, OsoHash
        _propertyOffsets["Header CRC"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Header CRC", $"0x{oso.Crc:X4}");
        _propertyOffsets["Block Type"] = new ByteRange { Offset = p + 2, Length = 1 };
        AddListItem("Block Type", $"0x{(byte)oso.BlockType:X2} ({oso.BlockType})");
        _propertyOffsets["Flags"] = new ByteRange { Offset = p + 3, Length = 2 };
        AddListItem("Flags", $"0x{oso.Flags:X4}");
        _propertyOffsets["Header Size"] = new ByteRange { Offset = p + 5, Length = 2 };
        AddListItem("Header Size", $"{oso.HeaderSize} bytes");
        p += 7;

        if ((oso.Flags & (ushort)SRRBlockFlags.LongBlock) != 0)
        {
            _propertyOffsets["Add Size"] = new ByteRange { Offset = p, Length = 4 };
            AddListItem("Add Size", $"{oso.AddSize} bytes");
            p += 4;
        }

        int nameLen = Encoding.UTF8.GetByteCount(oso.FileName);
        _propertyOffsets["Name Length"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Name Length", $"{nameLen} bytes");
        p += 2;
        _propertyOffsets["File Name"] = new ByteRange { Offset = p, Length = nameLen };
        AddListItem("File Name", oso.FileName);
        p += nameLen;

        _propertyOffsets["File Size"] = new ByteRange { Offset = p, Length = 8 };
        AddListItem("File Size", $"{oso.FileSize:N0} bytes ({FormatSize((long)oso.FileSize)})");
        _propertyOffsets["OSO Hash"] = new ByteRange { Offset = p + 8, Length = 8 };
        AddListItem("OSO Hash", BitConverter.ToString(oso.OsoHash).Replace("-", ""));
    }

    private void ShowRarPaddingBlockDetails(SrrRarPaddingBlock padding)
    {
        listView.Items.Clear();

        _propertyOffsets.Clear();

        long p = padding.BlockPosition;

        // Fields in byte order: CRC(0), Type(2), Flags(3), Size(5), [AddSize(7)], NameLen, RarFileName
        _propertyOffsets["Header CRC"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Header CRC", $"0x{padding.Crc:X4}");
        _propertyOffsets["Block Type"] = new ByteRange { Offset = p + 2, Length = 1 };
        AddListItem("Block Type", $"0x{(byte)padding.BlockType:X2} ({padding.BlockType})");
        _propertyOffsets["Flags"] = new ByteRange { Offset = p + 3, Length = 2 };
        AddListItem("Flags", $"0x{padding.Flags:X4}");
        _propertyOffsets["Header Size"] = new ByteRange { Offset = p + 5, Length = 2 };
        AddListItem("Header Size", $"{padding.HeaderSize} bytes");
        p += 7;

        if ((padding.Flags & (ushort)SRRBlockFlags.LongBlock) != 0)
        {
            _propertyOffsets["Add Size"] = new ByteRange { Offset = p, Length = 4 };
            AddListItem("Add Size", $"{padding.AddSize} bytes");
            p += 4;
        }

        int nameLen = Encoding.UTF8.GetByteCount(padding.RarFileName);
        _propertyOffsets["Name Length"] = new ByteRange { Offset = p, Length = 2 };
        AddListItem("Name Length", $"{nameLen} bytes");
        p += 2;
        _propertyOffsets["RAR File Name"] = new ByteRange { Offset = p, Length = nameLen };
        AddListItem("RAR File Name", padding.RarFileName);
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
