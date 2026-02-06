using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RARLib;
using SRRLib;
using WinRARRed.Controls;

namespace WinRARRed.Forms;

public partial class FileCompareForm : Form
{
    // Colors for difference highlighting (color-blind accessible: blue/orange instead of red/green)
    private static readonly Color ColorAdded = Color.FromArgb(200, 220, 255);    // Light blue
    private static readonly Color ColorRemoved = Color.FromArgb(255, 220, 180);  // Light orange
    private static readonly Color ColorModified = Color.FromArgb(255, 245, 200); // Light amber
    private static readonly Color ColorMatch = Color.White;

    private string? _leftFilePath;
    private string? _rightFilePath;
    private object? _leftData;
    private object? _rightData;
    private CompareResult? _compareResult;

    // Raw file bytes for hex view
    private byte[]? _leftFileBytes;
    private byte[]? _rightFileBytes;

    // Detailed parsed headers
    private List<RARDetailedBlock>? _leftDetailedBlocks;
    private List<RARDetailedBlock>? _rightDetailedBlocks;

    // Property offset mappings for hex highlighting
    private Dictionary<string, ByteRange> _leftPropertyOffsets = [];
    private Dictionary<string, ByteRange> _rightPropertyOffsets = [];

    // Currently selected node data for property offset lookup
    private CompareNodeData? _currentLeftNodeData;
    private CompareNodeData? _currentRightNodeData;

    public FileCompareForm()
    {
        InitializeComponent();
    }

    public FileCompareForm(string leftFile, string rightFile) : this()
    {
        LoadLeftFile(leftFile);
        LoadRightFile(rightFile);
    }

    #region Menu Handlers

    private void OpenLeftToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        BrowseAndLoadFile(true);
    }

    private void OpenRightToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        BrowseAndLoadFile(false);
    }

    private void SwapToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        SwapFiles();
    }

    private void RefreshToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        RefreshComparison();
    }

    private void ExitToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void ShowHexViewToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        splitContainerVertical.Panel2Collapsed = !showHexViewToolStripMenuItem.Checked;
    }

    private void ShowAllHexDataToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        // Toggle ShowAllData property on both hex views
        hexViewLeft.ShowAllData = showAllHexDataToolStripMenuItem.Checked;
        hexViewRight.ShowAllData = showAllHexDataToolStripMenuItem.Checked;

        // Reload the current data to apply the change
        hexViewLeft.LoadData(_leftFileBytes);
        hexViewRight.LoadData(_rightFileBytes);
        RefreshHexComparison();
    }

    #endregion

    #region Button Handlers

    private void BtnBrowseLeft_Click(object? sender, EventArgs e)
    {
        BrowseAndLoadFile(true);
    }

    private void BtnBrowseRight_Click(object? sender, EventArgs e)
    {
        BrowseAndLoadFile(false);
    }

    #endregion

    #region TreeView Handlers

    private void TreeViewLeft_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is CompareNodeData nodeData)
        {
            _currentLeftNodeData = nodeData;

            // ShowPropertyComparison handles offset building for detailed blocks
            if (nodeData.NodeType != CompareNodeType.DetailedBlock)
            {
                _leftPropertyOffsets = BuildPropertyOffsets(nodeData, _leftFileBytes);
            }

            ShowPropertyComparison(nodeData, listViewLeft, true);
            SyncTreeSelection(treeViewLeft, treeViewRight, e.Node);

            // Highlight the entire block in hex view
            HighlightBlockInHexView(nodeData, hexViewLeft);
        }
    }

    private void TreeViewRight_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is CompareNodeData nodeData)
        {
            _currentRightNodeData = nodeData;

            // ShowPropertyComparison handles offset building for detailed blocks
            if (nodeData.NodeType != CompareNodeType.DetailedBlock)
            {
                _rightPropertyOffsets = BuildPropertyOffsets(nodeData, _rightFileBytes);
            }

            ShowPropertyComparison(nodeData, listViewRight, false);
            SyncTreeSelection(treeViewRight, treeViewLeft, e.Node);

            // Highlight the entire block in hex view
            HighlightBlockInHexView(nodeData, hexViewRight);
        }
    }

    private static void HighlightBlockInHexView(CompareNodeData nodeData, HexViewControl hexView)
    {
        if (nodeData.NodeType == CompareNodeType.DetailedBlock && nodeData.Data is RARDetailedBlock block)
        {
            // Show only this block's data in hex view
            hexView.LoadBlockData(block.StartOffset, (int)Math.Min(block.TotalSize, int.MaxValue));
        }
        else
        {
            // Show full file for non-block nodes
            hexView.ShowFullFile();
        }
    }

    #endregion

    #region ListView Handlers

    private void ListViewLeft_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (listViewLeft.SelectedItems.Count > 0)
        {
            var item = listViewLeft.SelectedItems[0];
            string propertyName = item.Text;

            if (_leftPropertyOffsets.TryGetValue(propertyName, out var range))
            {
                hexViewLeft.SelectRange(range.Offset, range.Length);
            }
            else
            {
                hexViewLeft.ClearSelection();
            }

            // Sync selection to right ListView if same property exists
            SyncListViewSelection(listViewLeft, listViewRight, propertyName, item.Index);
        }
    }

    private void ListViewRight_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (listViewRight.SelectedItems.Count > 0)
        {
            var item = listViewRight.SelectedItems[0];
            string propertyName = item.Text;

            if (_rightPropertyOffsets.TryGetValue(propertyName, out var range))
            {
                hexViewRight.SelectRange(range.Offset, range.Length);
            }
            else
            {
                hexViewRight.ClearSelection();
            }

            // Sync selection to left ListView if same property exists
            SyncListViewSelection(listViewRight, listViewLeft, propertyName, item.Index);
        }
    }

    private static void SyncListViewSelection(ListView source, ListView target, string propertyName, int sourceIndex)
    {
        // First, try to find an item at the same index with the same name (most likely match)
        if (sourceIndex < target.Items.Count && target.Items[sourceIndex].Text == propertyName)
        {
            var item = target.Items[sourceIndex];
            if (!item.Selected)
            {
                target.SelectedItems.Clear();
                item.Selected = true;
                item.EnsureVisible();
            }
            return;
        }

        // Fall back to finding the first item with matching name
        foreach (ListViewItem item in target.Items)
        {
            if (item.Text == propertyName)
            {
                if (!item.Selected)
                {
                    target.SelectedItems.Clear();
                    item.Selected = true;
                    item.EnsureVisible();
                }
                break;
            }
        }
    }

    #endregion

    #region Drag and Drop

    private void FileCompareForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void FileCompareForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;

        // Get drop position to determine left vs right
        Point dropPoint = PointToClient(new Point(e.X, e.Y));
        bool isLeftSide = dropPoint.X < (ClientSize.Width / 2);

        if (files.Length == 1)
        {
            if (isLeftSide)
                LoadLeftFile(files[0]);
            else
                LoadRightFile(files[0]);
        }
        else if (files.Length >= 2)
        {
            LoadLeftFile(files[0]);
            LoadRightFile(files[1]);
        }
    }

    #endregion

    #region File Loading

    private void BrowseAndLoadFile(bool isLeft)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "RAR/SRR Files|*.rar;*.srr|RAR Files|*.rar|SRR Files|*.srr|All Files|*.*",
            Title = isLeft ? "Open Left File" : "Open Right File"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            if (isLeft)
                LoadLeftFile(ofd.FileName);
            else
                LoadRightFile(ofd.FileName);
        }
    }

    private void LoadLeftFile(string filePath)
    {
        try
        {
            _leftFilePath = filePath;
            txtLeftFile.Text = filePath;
            _leftFileBytes = File.ReadAllBytes(filePath);
            _leftData = LoadFileData(filePath);
            _leftPropertyOffsets.Clear();

            // Parse detailed headers for RAR files (SRR detailed blocks are in SRRFileData)
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".rar")
            {
                try
                {
                    _leftDetailedBlocks = RARDetailedParser.Parse(filePath);
                }
                catch
                {
                    _leftDetailedBlocks = null;
                }
            }
            else
            {
                _leftDetailedBlocks = null;
            }

            hexViewLeft.LoadData(_leftFileBytes);
            groupBoxHexLeft.Text = $"Hex View - {Path.GetFileName(filePath)}";
            RefreshComparison();
            RefreshHexComparison();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading left file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _leftFilePath = null;
            _leftData = null;
            _leftFileBytes = null;
            _leftDetailedBlocks = null;
            txtLeftFile.Clear();
            hexViewLeft.Clear();
            groupBoxHexLeft.Text = "Hex View - Left File";
        }
    }

    private void LoadRightFile(string filePath)
    {
        try
        {
            _rightFilePath = filePath;
            txtRightFile.Text = filePath;
            _rightFileBytes = File.ReadAllBytes(filePath);
            _rightData = LoadFileData(filePath);
            _rightPropertyOffsets.Clear();

            // Parse detailed headers for RAR files (SRR detailed blocks are in SRRFileData)
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".rar")
            {
                try
                {
                    _rightDetailedBlocks = RARDetailedParser.Parse(filePath);
                }
                catch
                {
                    _rightDetailedBlocks = null;
                }
            }
            else
            {
                _rightDetailedBlocks = null;
            }

            hexViewRight.LoadData(_rightFileBytes);
            groupBoxHexRight.Text = $"Hex View - {Path.GetFileName(filePath)}";
            RefreshComparison();
            RefreshHexComparison();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading right file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _rightFilePath = null;
            _rightData = null;
            _rightFileBytes = null;
            _rightDetailedBlocks = null;
            txtRightFile.Clear();
            hexViewRight.Clear();
            groupBoxHexRight.Text = "Hex View - Right File";
        }
    }

    private void RefreshHexComparison()
    {
        // Set comparison data (each view compares against the other's data)
        hexViewLeft.SetComparisonData(_rightFileBytes);
        hexViewRight.SetComparisonData(_leftFileBytes);
    }

    private static object? LoadFileData(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".srr")
        {
            return SRRFileData.Load(filePath);
        }
        else
        {
            return LoadRARFileData(filePath);
        }
    }

    private static RARFileData LoadRARFileData(string filePath)
    {
        var data = new RARFileData { FilePath = filePath };

        using var fs = File.OpenRead(filePath);
        using var reader = new BinaryReader(fs);

        data.IsRAR5 = RAR5HeaderReader.IsRAR5(fs);
        fs.Position = 0;

        if (data.IsRAR5)
        {
            LoadRAR5Data(fs, data);
        }
        else
        {
            LoadRAR4Data(reader, data);
        }

        return data;
    }

    private static void LoadRAR4Data(BinaryReader reader, RARFileData data)
    {
        var headerReader = new RARHeaderReader(reader);

        while (headerReader.CanReadBaseHeader)
        {
            var block = headerReader.ReadBlock(parseContents: true);
            if (block == null) break;

            if (block.ArchiveHeader != null)
            {
                data.ArchiveHeader = block.ArchiveHeader;
            }

            if (block.FileHeader != null)
            {
                data.FileHeaders.Add(block.FileHeader);
            }

            if (block.ServiceBlockInfo != null && block.ServiceBlockInfo.SubType == "CMT")
            {
                var commentData = headerReader.ReadServiceBlockData(block);
                if (commentData != null)
                {
                    data.Comment = block.ServiceBlockInfo.IsStored
                        ? System.Text.Encoding.UTF8.GetString(commentData)
                        : RARLib.Decompression.RARDecompressor.DecompressComment(
                            commentData,
                            (int)block.ServiceBlockInfo.UnpackedSize,
                            block.ServiceBlockInfo.CompressionMethod,
                            isRAR5: false);
                }
            }

            headerReader.SkipBlock(block, includeData: block.BlockType != RAR4BlockType.FileHeader);
        }
    }

    private static void LoadRAR5Data(Stream stream, RARFileData data)
    {
        stream.Seek(8, SeekOrigin.Begin);
        var headerReader = new RAR5HeaderReader(stream);

        while (headerReader.CanReadBaseHeader)
        {
            var block = headerReader.ReadBlock();
            if (block == null) break;

            if (block.ArchiveInfo != null)
            {
                data.RAR5ArchiveInfo = block.ArchiveInfo;
            }

            if (block.FileInfo != null)
            {
                data.RAR5FileInfos.Add(block.FileInfo);
            }

            if (block.ServiceBlockInfo != null && block.ServiceBlockInfo.SubType == "CMT")
            {
                var commentData = headerReader.ReadServiceBlockData(block);
                if (commentData != null)
                {
                    data.Comment = block.ServiceBlockInfo.IsStored
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
    }

    #endregion

    #region Comparison Logic

    private void SwapFiles()
    {
        (_leftFilePath, _rightFilePath) = (_rightFilePath, _leftFilePath);
        (_leftData, _rightData) = (_rightData, _leftData);
        (_leftFileBytes, _rightFileBytes) = (_rightFileBytes, _leftFileBytes);
        (txtLeftFile.Text, txtRightFile.Text) = (txtRightFile.Text, txtLeftFile.Text);

        // Update hex views
        hexViewLeft.LoadData(_leftFileBytes);
        hexViewRight.LoadData(_rightFileBytes);
        groupBoxHexLeft.Text = _leftFilePath != null ? $"Hex View - {Path.GetFileName(_leftFilePath)}" : "Hex View - Left File";
        groupBoxHexRight.Text = _rightFilePath != null ? $"Hex View - {Path.GetFileName(_rightFilePath)}" : "Hex View - Right File";

        RefreshComparison();
        RefreshHexComparison();
    }

    private void RefreshComparison()
    {
        treeViewLeft.Nodes.Clear();
        treeViewRight.Nodes.Clear();
        listViewLeft.Items.Clear();
        listViewRight.Items.Clear();

        if (_leftData != null)
        {
            PopulateTreeView(treeViewLeft, _leftData, true);
        }

        if (_rightData != null)
        {
            PopulateTreeView(treeViewRight, _rightData, false);
        }

        if (_leftData != null && _rightData != null)
        {
            _compareResult = CompareFiles();
            ApplyComparisonHighlighting();
            UpdateStatusBar();
        }
        else
        {
            _compareResult = null;
            statusLabel.Text = "Load files on both sides to compare.";
        }
    }

    private CompareResult CompareFiles()
    {
        var result = new CompareResult();

        // Compare based on file types
        if (_leftData is SRRFileData leftSrrData && _rightData is SRRFileData rightSrrData)
        {
            CompareSRRFiles(leftSrrData.SrrFile, rightSrrData.SrrFile, result);
        }
        else if (_leftData is RARFileData leftRar && _rightData is RARFileData rightRar)
        {
            CompareRARFiles(leftRar, rightRar, result);
        }
        else
        {
            // Mixed types - compare what we can
            result.ArchiveDifferences.Add(new PropertyDifference
            {
                PropertyName = "File Type",
                LeftValue = GetFileTypeName(_leftData),
                RightValue = GetFileTypeName(_rightData)
            });
        }

        return result;
    }

    private static string GetFileTypeName(object? data) => data switch
    {
        SRRFileData => "SRR File",
        RARFileData r => r.IsRAR5 ? "RAR 5.x" : "RAR 4.x",
        _ => "Unknown"
    };

    private void CompareSRRFiles(SRRFile left, SRRFile right, CompareResult result)
    {
        // Compare archive-level properties
        CompareProperty(result.ArchiveDifferences, "App Name", left.HeaderBlock?.AppName, right.HeaderBlock?.AppName);
        CompareProperty(result.ArchiveDifferences, "RAR Version", FormatRARVersion(left.RARVersion), FormatRARVersion(right.RARVersion));
        CompareProperty(result.ArchiveDifferences, "Compression Method", GetCompressionMethodName(left.CompressionMethod), GetCompressionMethodName(right.CompressionMethod));
        CompareProperty(result.ArchiveDifferences, "Dictionary Size", FormatDictionarySize(left.DictionarySize), FormatDictionarySize(right.DictionarySize));
        CompareProperty(result.ArchiveDifferences, "Solid Archive", FormatBool(left.IsSolidArchive), FormatBool(right.IsSolidArchive));
        CompareProperty(result.ArchiveDifferences, "Volume Archive", FormatBool(left.IsVolumeArchive), FormatBool(right.IsVolumeArchive));
        CompareProperty(result.ArchiveDifferences, "Recovery Record", FormatBool(left.HasRecoveryRecord), FormatBool(right.HasRecoveryRecord));
        CompareProperty(result.ArchiveDifferences, "Encrypted Headers", FormatBool(left.HasEncryptedHeaders), FormatBool(right.HasEncryptedHeaders));
        CompareProperty(result.ArchiveDifferences, "Has Comment", FormatBool(!string.IsNullOrEmpty(left.ArchiveComment)), FormatBool(!string.IsNullOrEmpty(right.ArchiveComment)));
        CompareProperty(result.ArchiveDifferences, "RAR Volumes Count", left.RarFiles.Count.ToString(), right.RarFiles.Count.ToString());
        CompareProperty(result.ArchiveDifferences, "Stored Files Count", left.StoredFiles.Count.ToString(), right.StoredFiles.Count.ToString());
        CompareProperty(result.ArchiveDifferences, "Archived Files Count", left.ArchivedFiles.Count.ToString(), right.ArchivedFiles.Count.ToString());
        CompareProperty(result.ArchiveDifferences, "Header CRC Errors", left.HeaderCrcMismatches.ToString(), right.HeaderCrcMismatches.ToString());

        // Compare archived files
        var leftFiles = new HashSet<string>(left.ArchivedFiles, StringComparer.OrdinalIgnoreCase);
        var rightFiles = new HashSet<string>(right.ArchivedFiles, StringComparer.OrdinalIgnoreCase);

        foreach (var file in leftFiles.Union(rightFiles).OrderBy(f => f))
        {
            bool inLeft = leftFiles.Contains(file);
            bool inRight = rightFiles.Contains(file);

            var fileDiff = new FileDifference { FileName = file };

            if (inLeft && !inRight)
            {
                fileDiff.Type = DifferenceType.Removed;
            }
            else if (!inLeft && inRight)
            {
                fileDiff.Type = DifferenceType.Added;
            }
            else
            {
                // Compare file properties
                left.ArchivedFileCrcs.TryGetValue(file, out var leftCrc);
                right.ArchivedFileCrcs.TryGetValue(file, out var rightCrc);

                if (!string.Equals(leftCrc, rightCrc, StringComparison.OrdinalIgnoreCase))
                {
                    fileDiff.Type = DifferenceType.Modified;
                    fileDiff.PropertyDifferences.Add(new PropertyDifference
                    {
                        PropertyName = "CRC",
                        LeftValue = leftCrc ?? "N/A",
                        RightValue = rightCrc ?? "N/A"
                    });
                }

                left.ArchivedFileTimestamps.TryGetValue(file, out var leftTime);
                right.ArchivedFileTimestamps.TryGetValue(file, out var rightTime);

                if (leftTime != rightTime)
                {
                    fileDiff.Type = DifferenceType.Modified;
                    fileDiff.PropertyDifferences.Add(new PropertyDifference
                    {
                        PropertyName = "Modified Time",
                        LeftValue = leftTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        RightValue = rightTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            if (fileDiff.Type != DifferenceType.None)
            {
                result.FileDifferences.Add(fileDiff);
            }
        }

        // Compare stored files
        var leftStored = left.StoredFiles.Select(s => s.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightStored = right.StoredFiles.Select(s => s.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in leftStored.Union(rightStored).OrderBy(f => f))
        {
            bool inLeft = leftStored.Contains(file);
            bool inRight = rightStored.Contains(file);

            if (inLeft != inRight)
            {
                result.StoredFileDifferences.Add(new FileDifference
                {
                    FileName = file,
                    Type = inLeft ? DifferenceType.Removed : DifferenceType.Added
                });
            }
        }
    }

    private void CompareRARFiles(RARFileData left, RARFileData right, CompareResult result)
    {
        // Compare format
        CompareProperty(result.ArchiveDifferences, "Format", left.IsRAR5 ? "RAR 5.x" : "RAR 4.x", right.IsRAR5 ? "RAR 5.x" : "RAR 4.x");

        if (!left.IsRAR5 && !right.IsRAR5)
        {
            // Both RAR4
            if (left.ArchiveHeader != null && right.ArchiveHeader != null)
            {
                CompareProperty(result.ArchiveDifferences, "Volume", FormatBool(left.ArchiveHeader.IsVolume), FormatBool(right.ArchiveHeader.IsVolume));
                CompareProperty(result.ArchiveDifferences, "Solid", FormatBool(left.ArchiveHeader.IsSolid), FormatBool(right.ArchiveHeader.IsSolid));
                CompareProperty(result.ArchiveDifferences, "Recovery Record", FormatBool(left.ArchiveHeader.HasRecoveryRecord), FormatBool(right.ArchiveHeader.HasRecoveryRecord));
                CompareProperty(result.ArchiveDifferences, "Locked", FormatBool(left.ArchiveHeader.IsLocked), FormatBool(right.ArchiveHeader.IsLocked));
                CompareProperty(result.ArchiveDifferences, "Encrypted Headers", FormatBool(left.ArchiveHeader.HasEncryptedHeaders), FormatBool(right.ArchiveHeader.HasEncryptedHeaders));
            }

            // Compare file headers
            var leftDict = left.FileHeaders.ToDictionary(f => f.FileName, StringComparer.OrdinalIgnoreCase);
            var rightDict = right.FileHeaders.ToDictionary(f => f.FileName, StringComparer.OrdinalIgnoreCase);

            foreach (var fileName in leftDict.Keys.Union(rightDict.Keys).OrderBy(f => f))
            {
                leftDict.TryGetValue(fileName, out var leftFile);
                rightDict.TryGetValue(fileName, out var rightFile);

                var fileDiff = new FileDifference { FileName = fileName };

                if (leftFile != null && rightFile == null)
                {
                    fileDiff.Type = DifferenceType.Removed;
                }
                else if (leftFile == null && rightFile != null)
                {
                    fileDiff.Type = DifferenceType.Added;
                }
                else if (leftFile != null && rightFile != null)
                {
                    CompareRAR4FileHeaders(leftFile, rightFile, fileDiff);
                }

                if (fileDiff.Type != DifferenceType.None)
                {
                    result.FileDifferences.Add(fileDiff);
                }
            }
        }
        else if (left.IsRAR5 && right.IsRAR5)
        {
            // Both RAR5
            if (left.RAR5ArchiveInfo != null && right.RAR5ArchiveInfo != null)
            {
                CompareProperty(result.ArchiveDifferences, "Volume", FormatBool(left.RAR5ArchiveInfo.IsVolume), FormatBool(right.RAR5ArchiveInfo.IsVolume));
                CompareProperty(result.ArchiveDifferences, "Solid", FormatBool(left.RAR5ArchiveInfo.IsSolid), FormatBool(right.RAR5ArchiveInfo.IsSolid));
                CompareProperty(result.ArchiveDifferences, "Recovery Record", FormatBool(left.RAR5ArchiveInfo.HasRecoveryRecord), FormatBool(right.RAR5ArchiveInfo.HasRecoveryRecord));
                CompareProperty(result.ArchiveDifferences, "Locked", FormatBool(left.RAR5ArchiveInfo.IsLocked), FormatBool(right.RAR5ArchiveInfo.IsLocked));
            }

            // Compare file headers
            var leftDict = left.RAR5FileInfos.ToDictionary(f => f.FileName, StringComparer.OrdinalIgnoreCase);
            var rightDict = right.RAR5FileInfos.ToDictionary(f => f.FileName, StringComparer.OrdinalIgnoreCase);

            foreach (var fileName in leftDict.Keys.Union(rightDict.Keys).OrderBy(f => f))
            {
                leftDict.TryGetValue(fileName, out var leftFile);
                rightDict.TryGetValue(fileName, out var rightFile);

                var fileDiff = new FileDifference { FileName = fileName };

                if (leftFile != null && rightFile == null)
                {
                    fileDiff.Type = DifferenceType.Removed;
                }
                else if (leftFile == null && rightFile != null)
                {
                    fileDiff.Type = DifferenceType.Added;
                }
                else if (leftFile != null && rightFile != null)
                {
                    CompareRAR5FileHeaders(leftFile, rightFile, fileDiff);
                }

                if (fileDiff.Type != DifferenceType.None)
                {
                    result.FileDifferences.Add(fileDiff);
                }
            }
        }

        CompareProperty(result.ArchiveDifferences, "Has Comment", FormatBool(!string.IsNullOrEmpty(left.Comment)), FormatBool(!string.IsNullOrEmpty(right.Comment)));
    }

    private static void CompareRAR4FileHeaders(RARFileHeader left, RARFileHeader right, FileDifference diff)
    {
        if (left.FileCrc != right.FileCrc)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "CRC",
                LeftValue = left.FileCrc.ToString("X8"),
                RightValue = right.FileCrc.ToString("X8")
            });
        }

        if (left.UnpackedSize != right.UnpackedSize)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "Unpacked Size",
                LeftValue = left.UnpackedSize.ToString("N0"),
                RightValue = right.UnpackedSize.ToString("N0")
            });
        }

        if (left.PackedSize != right.PackedSize)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "Packed Size",
                LeftValue = left.PackedSize.ToString("N0"),
                RightValue = right.PackedSize.ToString("N0")
            });
        }

        if (left.CompressionMethod != right.CompressionMethod)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "Compression Method",
                LeftValue = GetCompressionMethodName(left.CompressionMethod),
                RightValue = GetCompressionMethodName(right.CompressionMethod)
            });
        }

        if (left.ModifiedTime != right.ModifiedTime)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "Modified Time",
                LeftValue = left.ModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                RightValue = right.ModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            });
        }
    }

    private static void CompareRAR5FileHeaders(RAR5FileInfo left, RAR5FileInfo right, FileDifference diff)
    {
        if (left.FileCrc != right.FileCrc)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "CRC",
                LeftValue = left.FileCrc?.ToString("X8") ?? "N/A",
                RightValue = right.FileCrc?.ToString("X8") ?? "N/A"
            });
        }

        if (left.UnpackedSize != right.UnpackedSize)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "Unpacked Size",
                LeftValue = left.UnpackedSize.ToString("N0"),
                RightValue = right.UnpackedSize.ToString("N0")
            });
        }

        if (left.CompressionMethod != right.CompressionMethod)
        {
            diff.Type = DifferenceType.Modified;
            diff.PropertyDifferences.Add(new PropertyDifference
            {
                PropertyName = "Compression Method",
                LeftValue = GetCompressionMethodName(left.CompressionMethod),
                RightValue = GetCompressionMethodName(right.CompressionMethod)
            });
        }
    }

    private static void CompareProperty(List<PropertyDifference> diffs, string name, string? leftValue, string? rightValue)
    {
        if (!string.Equals(leftValue ?? "", rightValue ?? "", StringComparison.Ordinal))
        {
            diffs.Add(new PropertyDifference
            {
                PropertyName = name,
                LeftValue = leftValue ?? "N/A",
                RightValue = rightValue ?? "N/A"
            });
        }
    }

    #endregion

    #region Tree Population

    private void PopulateTreeView(TreeView tree, object data, bool isLeft)
    {
        tree.BeginUpdate();
        tree.Nodes.Clear();

        // Use detailed blocks if available and they contain meaningful data
        var detailedBlocks = isLeft ? _leftDetailedBlocks : _rightDetailedBlocks;

        // Check if detailed blocks have file headers (indicates successful parsing)
        bool hasFileHeaders = detailedBlocks != null &&
                              detailedBlocks.Any(b => b.BlockType == "File Header" || b.BlockType == "Service Block");

        if (detailedBlocks != null && detailedBlocks.Count > 0 && hasFileHeaders)
        {
            PopulateDetailedTree(tree, detailedBlocks, isLeft);
        }
        else if (data is SRRFileData srrData)
        {
            PopulateSRRTree(tree, srrData, isLeft);
        }
        else if (data is RARFileData rar)
        {
            // Fall back to old method if detailed parsing didn't find file headers
            PopulateRARTree(tree, rar, isLeft);
        }

        tree.EndUpdate();

        if (tree.Nodes.Count > 0)
        {
            tree.Nodes[0].Expand();
        }
    }

    private void PopulateDetailedTree(TreeView tree, List<RARDetailedBlock> blocks, bool isLeft)
    {
        bool isRAR5 = blocks.Count > 0 && blocks[0].BlockType == "Signature" &&
                      blocks[0].Fields.Count > 0 && blocks[0].Fields[0].Value.StartsWith("52 61 72 21 1A 07 01");

        string rootName = isRAR5 ? $"RAR 5.x Archive ({blocks.Count} blocks)" : $"RAR 4.x Archive ({blocks.Count} blocks)";

        var rootNode = tree.Nodes.Add(rootName);
        rootNode.Tag = new CompareNodeData { NodeType = CompareNodeType.Root, IsLeft = isLeft };

        // Add all blocks in order
        for (int i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];

            // Build block label
            string blockLabel = $"[{i}] {block.BlockType}";

            // Add item name for file headers and service blocks
            if (!string.IsNullOrEmpty(block.ItemName))
            {
                blockLabel = $"[{i}] {block.BlockType}: {block.ItemName}";
            }

            var blockNode = rootNode.Nodes.Add(blockLabel);
            blockNode.Tag = new CompareNodeData
            {
                NodeType = CompareNodeType.DetailedBlock,
                Data = block,
                FileName = block.ItemName,
                IsLeft = isLeft
            };
        }

        rootNode.Expand();
    }

    private void PopulateSRRTree(TreeView tree, SRRFileData srrData, bool isLeft)
    {
        var srr = srrData.SrrFile;

        var rootNode = tree.Nodes.Add("SRR File");
        rootNode.Tag = new CompareNodeData { NodeType = CompareNodeType.Root, Data = srr, IsLeft = isLeft };

        var archiveNode = rootNode.Nodes.Add("Archive Info");
        archiveNode.Tag = new CompareNodeData { NodeType = CompareNodeType.ArchiveInfo, Data = srr, IsLeft = isLeft };

        if (srr.RarFiles.Count > 0)
        {
            var volumesNode = rootNode.Nodes.Add($"RAR Volumes ({srr.RarFiles.Count})");
            volumesNode.Tag = new CompareNodeData { NodeType = CompareNodeType.RarVolumes, Data = srr.RarFiles, IsLeft = isLeft };
            foreach (var rar in srr.RarFiles)
            {
                var volNode = volumesNode.Nodes.Add(rar.FileName);
                volNode.Tag = new CompareNodeData { NodeType = CompareNodeType.RarVolume, Data = rar, IsLeft = isLeft };

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
                        blockNode.Tag = new CompareNodeData
                        {
                            NodeType = CompareNodeType.DetailedBlock,
                            Data = block,
                            FileName = block.ItemName,
                            IsLeft = isLeft
                        };
                    }
                }
            }
        }

        if (srr.StoredFiles.Count > 0)
        {
            var storedNode = rootNode.Nodes.Add($"Stored Files ({srr.StoredFiles.Count})");
            storedNode.Tag = new CompareNodeData { NodeType = CompareNodeType.StoredFiles, Data = srr.StoredFiles, IsLeft = isLeft };
            foreach (var stored in srr.StoredFiles)
            {
                var fileNode = storedNode.Nodes.Add(stored.FileName);
                fileNode.Tag = new CompareNodeData { NodeType = CompareNodeType.StoredFile, Data = stored, FileName = stored.FileName, IsLeft = isLeft };
            }
        }

        if (srr.ArchivedFiles.Count > 0)
        {
            var archivedNode = rootNode.Nodes.Add($"Archived Files ({srr.ArchivedFiles.Count})");
            archivedNode.Tag = new CompareNodeData { NodeType = CompareNodeType.ArchivedFiles, Data = srr, IsLeft = isLeft };
            foreach (var file in srr.ArchivedFiles.OrderBy(f => f))
            {
                string displayName = file;
                if (srr.ArchivedFileCrcs.TryGetValue(file, out var crc))
                {
                    displayName = $"{file} [CRC: {crc}]";
                }
                var fileNode = archivedNode.Nodes.Add(displayName);
                fileNode.Tag = new CompareNodeData { NodeType = CompareNodeType.ArchivedFile, Data = srr, FileName = file, IsLeft = isLeft };
            }
        }

        if (srr.OsoHashBlocks.Count > 0)
        {
            var osoNode = rootNode.Nodes.Add($"OSO Hashes ({srr.OsoHashBlocks.Count})");
            osoNode.Tag = new CompareNodeData { NodeType = CompareNodeType.OsoHashes, Data = srr.OsoHashBlocks, IsLeft = isLeft };
        }

        rootNode.Expand();
    }

    private void PopulateRARTree(TreeView tree, RARFileData rar, bool isLeft)
    {
        int fileCount = rar.IsRAR5 ? rar.RAR5FileInfos.Count : rar.FileHeaders.Count;
        // Estimate block count: signature + archive header + files + end archive + possibly CMT
        int blockCount = 2 + fileCount + 1 + (string.IsNullOrEmpty(rar.Comment) ? 0 : 1);

        var rootNode = tree.Nodes.Add(rar.IsRAR5 ? $"RAR 5.x Archive (~{blockCount} blocks)" : $"RAR 4.x Archive (~{blockCount} blocks)");
        rootNode.Tag = new CompareNodeData { NodeType = CompareNodeType.Root, Data = rar, IsLeft = isLeft };

        int blockIndex = 0;

        // Signature block
        var sigNode = rootNode.Nodes.Add($"[{blockIndex++}] Signature");
        sigNode.Tag = new CompareNodeData { NodeType = CompareNodeType.Root, IsLeft = isLeft };

        // Archive header
        var archNode = rootNode.Nodes.Add($"[{blockIndex++}] Archive Header");
        archNode.Tag = new CompareNodeData { NodeType = CompareNodeType.ArchiveInfo, Data = rar, IsLeft = isLeft };

        // File headers
        if (rar.IsRAR5)
        {
            foreach (var file in rar.RAR5FileInfos)
            {
                var fileNode = rootNode.Nodes.Add($"[{blockIndex++}] File Header: {file.FileName}");
                fileNode.Tag = new CompareNodeData { NodeType = CompareNodeType.ArchivedFile, Data = file, FileName = file.FileName, IsLeft = isLeft };
            }
        }
        else
        {
            foreach (var file in rar.FileHeaders)
            {
                var fileNode = rootNode.Nodes.Add($"[{blockIndex++}] File Header: {file.FileName}");
                fileNode.Tag = new CompareNodeData { NodeType = CompareNodeType.ArchivedFile, Data = file, FileName = file.FileName, IsLeft = isLeft };
            }
        }

        // Comment block if present
        if (!string.IsNullOrEmpty(rar.Comment))
        {
            var cmtNode = rootNode.Nodes.Add($"[{blockIndex++}] Service Block: CMT");
            cmtNode.Tag = new CompareNodeData { NodeType = CompareNodeType.Root, IsLeft = isLeft };
        }

        // End archive
        var endNode = rootNode.Nodes.Add($"[{blockIndex++}] End Archive");
        endNode.Tag = new CompareNodeData { NodeType = CompareNodeType.Root, IsLeft = isLeft };

        rootNode.Expand();
    }

    private void ApplyComparisonHighlighting()
    {
        if (_compareResult == null) return;

        // Build sets of different files for quick lookup
        var addedFiles = _compareResult.FileDifferences
            .Where(d => d.Type == DifferenceType.Added)
            .Select(d => d.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removedFiles = _compareResult.FileDifferences
            .Where(d => d.Type == DifferenceType.Removed)
            .Select(d => d.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var modifiedFiles = _compareResult.FileDifferences
            .Where(d => d.Type == DifferenceType.Modified)
            .Select(d => d.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedStoredFiles = _compareResult.StoredFileDifferences
            .Where(d => d.Type == DifferenceType.Added)
            .Select(d => d.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removedStoredFiles = _compareResult.StoredFileDifferences
            .Where(d => d.Type == DifferenceType.Removed)
            .Select(d => d.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Apply highlighting to left tree
        ApplyTreeHighlighting(treeViewLeft, removedFiles, addedFiles, modifiedFiles, removedStoredFiles, addedStoredFiles, true);

        // Apply highlighting to right tree
        ApplyTreeHighlighting(treeViewRight, addedFiles, removedFiles, modifiedFiles, addedStoredFiles, removedStoredFiles, false);
    }

    private static void ApplyTreeHighlighting(TreeView tree, HashSet<string> removed, HashSet<string> added, HashSet<string> modified, HashSet<string> storedRemoved, HashSet<string> storedAdded, bool isLeft)
    {
        foreach (TreeNode node in tree.Nodes)
        {
            ApplyNodeHighlighting(node, removed, added, modified, storedRemoved, storedAdded, isLeft);
        }
    }

    private static void ApplyNodeHighlighting(TreeNode node, HashSet<string> removed, HashSet<string> added, HashSet<string> modified, HashSet<string> storedRemoved, HashSet<string> storedAdded, bool isLeft)
    {
        if (node.Tag is CompareNodeData data)
        {
            if (data.NodeType == CompareNodeType.ArchivedFile || data.NodeType == CompareNodeType.StoredFile)
            {
                var fileName = data.FileName ?? "";

                if (data.NodeType == CompareNodeType.StoredFile)
                {
                    if (storedRemoved.Contains(fileName))
                    {
                        node.BackColor = isLeft ? ColorRemoved : ColorAdded;
                        node.Text = isLeft ? $"{GetBaseNodeText(node)} [REMOVED]" : $"{GetBaseNodeText(node)} [NEW]";
                    }
                    else if (storedAdded.Contains(fileName))
                    {
                        node.BackColor = isLeft ? ColorAdded : ColorRemoved;
                    }
                }
                else
                {
                    if (removed.Contains(fileName))
                    {
                        node.BackColor = isLeft ? ColorRemoved : ColorAdded;
                        node.Text = isLeft ? $"{GetBaseNodeText(node)} [REMOVED]" : $"{GetBaseNodeText(node)} [NEW]";
                    }
                    else if (added.Contains(fileName))
                    {
                        node.BackColor = isLeft ? ColorAdded : ColorRemoved;
                    }
                    else if (modified.Contains(fileName))
                    {
                        node.BackColor = ColorModified;
                        node.Text = $"{GetBaseNodeText(node)} [DIFF]";
                    }
                }
            }
        }

        foreach (TreeNode child in node.Nodes)
        {
            ApplyNodeHighlighting(child, removed, added, modified, storedRemoved, storedAdded, isLeft);
        }
    }

    private static string GetBaseNodeText(TreeNode node)
    {
        string text = node.Text;
        int bracketIndex = text.LastIndexOf(" [");
        if (bracketIndex > 0 && (text.EndsWith("[REMOVED]") || text.EndsWith("[NEW]") || text.EndsWith("[DIFF]")))
        {
            return text[..bracketIndex];
        }
        return text;
    }

    private void SyncTreeSelection(TreeView source, TreeView target, TreeNode selectedNode)
    {
        if (selectedNode.Tag is not CompareNodeData sourceData)
            return;

        // Find matching node in target tree
        var matchingNode = FindMatchingNode(target.Nodes, sourceData);
        if (matchingNode != null && target.SelectedNode != matchingNode)
        {
            target.SelectedNode = matchingNode;
        }
    }

    private static TreeNode? FindMatchingNode(TreeNodeCollection nodes, CompareNodeData sourceData)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is CompareNodeData nodeData)
            {
                // For DetailedBlock nodes, match by block type and item name
                if (nodeData.NodeType == CompareNodeType.DetailedBlock && sourceData.NodeType == CompareNodeType.DetailedBlock)
                {
                    if (nodeData.Data is RARDetailedBlock nodeBlock && sourceData.Data is RARDetailedBlock sourceBlock)
                    {
                        // Match by block type and item name (e.g., "Service Block" + "CMT")
                        if (nodeBlock.BlockType == sourceBlock.BlockType &&
                            nodeBlock.ItemName == sourceBlock.ItemName)
                        {
                            return node;
                        }
                    }
                }
                // For other node types, match by NodeType and FileName
                else if (nodeData.NodeType == sourceData.NodeType && nodeData.FileName == sourceData.FileName)
                {
                    return node;
                }
            }

            var child = FindMatchingNode(node.Nodes, sourceData);
            if (child != null)
                return child;
        }
        return null;
    }

    #endregion

    #region Property Display

    private void ShowPropertyComparison(CompareNodeData nodeData, ListView listView, bool isLeft)
    {
        listView.Items.Clear();

        switch (nodeData.NodeType)
        {
            case CompareNodeType.DetailedBlock:
                if (nodeData.Data is RARDetailedBlock detailedBlock)
                    ShowDetailedBlockProperties(listView, detailedBlock, isLeft);
                break;

            case CompareNodeType.ArchiveInfo:
                if (nodeData.Data is SRRFile srr)
                    ShowSRRArchiveProperties(listView, srr, isLeft);
                else if (nodeData.Data is RARFileData rar)
                    ShowRARArchiveProperties(listView, rar, isLeft);
                break;

            case CompareNodeType.ArchivedFile:
                if (nodeData.Data is RARFileHeader fileHeader)
                    ShowRAR4FileProperties(listView, fileHeader, isLeft);
                else if (nodeData.Data is RAR5FileInfo fileInfo)
                    ShowRAR5FileProperties(listView, fileInfo, isLeft);
                else if (nodeData.Data is SRRFile srrFile && nodeData.FileName != null)
                    ShowSRRArchivedFileProperties(listView, srrFile, nodeData.FileName, isLeft);
                break;

            case CompareNodeType.StoredFile:
                if (nodeData.Data is SrrStoredFileBlock stored)
                    ShowStoredFileProperties(listView, stored, isLeft);
                break;

            case CompareNodeType.RarVolume:
                if (nodeData.Data is SrrRarFileBlock rarFile)
                    ShowRarVolumeProperties(listView, rarFile, isLeft);
                break;
        }
    }

    private void ShowDetailedBlockProperties(ListView listView, RARDetailedBlock block, bool isLeft)
    {
        // Store offsets for hex highlighting
        var offsets = isLeft ? _leftPropertyOffsets : _rightPropertyOffsets;
        offsets.Clear();

        // Add block summary info
        AddPropertyItem(listView, "Block Type", block.BlockType, isLeft, null);
        AddPropertyItem(listView, "Start Offset", $"0x{block.StartOffset:X}", isLeft, null);
        AddPropertyItem(listView, "Header Size", $"{block.HeaderSize} bytes", isLeft, null);
        AddPropertyItem(listView, "Total Size", $"{block.TotalSize:N0} bytes", isLeft, null);

        if (block.HasData)
        {
            AddPropertyItem(listView, "Data Size", $"{block.DataSize:N0} bytes", isLeft, null);
        }

        // Add separator
        listView.Items.Add(new ListViewItem("--- Fields ---") { BackColor = Color.LightGray });

        // Add all fields with their offsets
        foreach (var field in block.Fields)
        {
            string value = field.Value;
            if (!string.IsNullOrEmpty(field.Description) && field.Description != field.Value)
            {
                value = $"{field.Value} ({field.Description})";
            }

            var item = new ListViewItem(field.Name);
            item.SubItems.Add(value);
            item.Tag = field; // Store field for hex highlighting

            // Store offset for hex view
            offsets[field.Name] = new ByteRange
            {
                PropertyName = field.Name,
                Offset = field.Offset,
                Length = field.Length
            };

            // Check for differences if comparing
            if (_compareResult != null)
            {
                var otherBlock = FindMatchingDetailedBlock(block, isLeft);
                if (otherBlock != null)
                {
                    var otherField = otherBlock.Fields.FirstOrDefault(f => f.Name == field.Name);
                    if (otherField != null && otherField.Value != field.Value)
                    {
                        item.BackColor = ColorModified;
                    }
                }
            }

            listView.Items.Add(item);

            // Add child fields (like flag details)
            foreach (var child in field.Children)
            {
                var childItem = new ListViewItem($"    {child.Name}");
                childItem.SubItems.Add(child.Value);
                childItem.ForeColor = Color.DarkBlue;
                listView.Items.Add(childItem);
            }
        }
    }

    /// <summary>
    /// Finds a matching detailed block on the other side for comparison.
    /// Searches both RAR file detailed blocks and SRR volume detailed blocks.
    /// </summary>
    private RARDetailedBlock? FindMatchingDetailedBlock(RARDetailedBlock block, bool isLeft)
    {
        // First check RAR file detailed blocks
        var otherBlocks = isLeft ? _rightDetailedBlocks : _leftDetailedBlocks;
        if (otherBlocks != null)
        {
            var match = otherBlocks.FirstOrDefault(b =>
                b.BlockType == block.BlockType &&
                b.ItemName == block.ItemName);
            if (match != null) return match;
        }

        // Then check SRR volume detailed blocks
        var otherData = isLeft ? _rightData : _leftData;
        if (otherData is SRRFileData otherSrrData)
        {
            foreach (var volumeBlocks in otherSrrData.VolumeDetailedBlocks.Values)
            {
                var match = volumeBlocks.FirstOrDefault(b =>
                    b.BlockType == block.BlockType &&
                    b.ItemName == block.ItemName);
                if (match != null) return match;
            }
        }

        return null;
    }

    private void ShowSRRArchiveProperties(ListView listView, SRRFile srr, bool isLeft)
    {
        AddPropertyItem(listView, "App Name", srr.HeaderBlock?.AppName ?? "N/A", isLeft, "App Name");
        AddPropertyItem(listView, "RAR Version", FormatRARVersion(srr.RARVersion), isLeft, "RAR Version");
        AddPropertyItem(listView, "Compression Method", GetCompressionMethodName(srr.CompressionMethod), isLeft, "Compression Method");
        AddPropertyItem(listView, "Dictionary Size", FormatDictionarySize(srr.DictionarySize), isLeft, "Dictionary Size");
        AddPropertyItem(listView, "Solid Archive", FormatBool(srr.IsSolidArchive), isLeft, "Solid Archive");
        AddPropertyItem(listView, "Volume Archive", FormatBool(srr.IsVolumeArchive), isLeft, "Volume Archive");
        AddPropertyItem(listView, "Recovery Record", FormatBool(srr.HasRecoveryRecord), isLeft, "Recovery Record");
        AddPropertyItem(listView, "Encrypted Headers", FormatBool(srr.HasEncryptedHeaders), isLeft, "Encrypted Headers");
        AddPropertyItem(listView, "RAR Volumes", srr.RarFiles.Count.ToString(), isLeft, "RAR Volumes Count");
        AddPropertyItem(listView, "Stored Files", srr.StoredFiles.Count.ToString(), isLeft, "Stored Files Count");
        AddPropertyItem(listView, "Archived Files", srr.ArchivedFiles.Count.ToString(), isLeft, "Archived Files Count");
        AddPropertyItem(listView, "Header CRC Errors", srr.HeaderCrcMismatches.ToString(), isLeft, "Header CRC Errors");
        AddPropertyItem(listView, "Has Comment", FormatBool(!string.IsNullOrEmpty(srr.ArchiveComment)), isLeft, "Has Comment");

        // Host OS and CMT properties for reconstruction hints
        listView.Items.Add(new ListViewItem("--- Reconstruction Hints ---") { BackColor = Color.LightGray });

        if (srr.DetectedHostOS.HasValue)
        {
            AddPropertyItem(listView, "Host OS (files)", $"{srr.DetectedHostOSName} (0x{srr.DetectedHostOS:X2})", isLeft, "Host OS");
        }

        if (srr.CmtHostOS.HasValue)
        {
            AddPropertyItem(listView, "CMT Host OS", $"{srr.CmtHostOSName} (0x{srr.CmtHostOS:X2})", isLeft, "CMT Host OS");
        }

        if (srr.CmtFileTimeDOS.HasValue)
        {
            string timeMode = srr.CmtHasZeroedFileTime
                ? "Zeroed (0x00000000)"
                : $"0x{srr.CmtFileTimeDOS:X8}";
            AddPropertyItem(listView, "CMT Timestamp", timeMode, isLeft, "CMT Timestamp");
            AddPropertyItem(listView, "CMT Time Mode", srr.CmtTimestampMode, isLeft, "CMT Time Mode");
        }

        if (srr.CmtFileAttributes.HasValue)
        {
            AddPropertyItem(listView, "CMT Attributes", $"0x{srr.CmtFileAttributes:X8}", isLeft, "CMT Attributes");
        }
    }

    private void ShowRARArchiveProperties(ListView listView, RARFileData rar, bool isLeft)
    {
        AddPropertyItem(listView, "Format", rar.IsRAR5 ? "RAR 5.x" : "RAR 4.x", isLeft, "Format");

        if (rar.IsRAR5 && rar.RAR5ArchiveInfo != null)
        {
            AddPropertyItem(listView, "Volume", FormatBool(rar.RAR5ArchiveInfo.IsVolume), isLeft, "Volume");
            AddPropertyItem(listView, "Solid", FormatBool(rar.RAR5ArchiveInfo.IsSolid), isLeft, "Solid");
            AddPropertyItem(listView, "Recovery Record", FormatBool(rar.RAR5ArchiveInfo.HasRecoveryRecord), isLeft, "Recovery Record");
            AddPropertyItem(listView, "Locked", FormatBool(rar.RAR5ArchiveInfo.IsLocked), isLeft, "Locked");
            AddPropertyItem(listView, "File Count", rar.RAR5FileInfos.Count.ToString(), isLeft, "File Count");
        }
        else if (!rar.IsRAR5 && rar.ArchiveHeader != null)
        {
            AddPropertyItem(listView, "Volume", FormatBool(rar.ArchiveHeader.IsVolume), isLeft, "Volume");
            AddPropertyItem(listView, "Solid", FormatBool(rar.ArchiveHeader.IsSolid), isLeft, "Solid");
            AddPropertyItem(listView, "Recovery Record", FormatBool(rar.ArchiveHeader.HasRecoveryRecord), isLeft, "Recovery Record");
            AddPropertyItem(listView, "Locked", FormatBool(rar.ArchiveHeader.IsLocked), isLeft, "Locked");
            AddPropertyItem(listView, "Encrypted Headers", FormatBool(rar.ArchiveHeader.HasEncryptedHeaders), isLeft, "Encrypted Headers");
            AddPropertyItem(listView, "File Count", rar.FileHeaders.Count.ToString(), isLeft, "File Count");
        }

        AddPropertyItem(listView, "Has Comment", FormatBool(!string.IsNullOrEmpty(rar.Comment)), isLeft, "Has Comment");
    }

    private void ShowRAR4FileProperties(ListView listView, RARFileHeader header, bool isLeft)
    {
        AddPropertyItem(listView, "File Name", header.FileName, isLeft, null);
        AddPropertyItem(listView, "Type", header.IsDirectory ? "Directory" : "File", isLeft, null);
        AddPropertyItem(listView, "Unpacked Size", $"{header.UnpackedSize:N0} bytes", isLeft, "Unpacked Size");
        AddPropertyItem(listView, "Packed Size", $"{header.PackedSize:N0} bytes", isLeft, "Packed Size");
        AddPropertyItem(listView, "CRC32", header.FileCrc.ToString("X8"), isLeft, "CRC");
        AddPropertyItem(listView, "Compression Method", GetCompressionMethodName(header.CompressionMethod), isLeft, "Compression Method");
        AddPropertyItem(listView, "Dictionary Size", $"{header.DictionarySizeKB} KB", isLeft, null);
        AddPropertyItem(listView, "Modified Time", header.ModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A", isLeft, "Modified Time");
        AddPropertyItem(listView, "Split Before", FormatBool(header.IsSplitBefore), isLeft, null);
        AddPropertyItem(listView, "Split After", FormatBool(header.IsSplitAfter), isLeft, null);
    }

    private void ShowRAR5FileProperties(ListView listView, RAR5FileInfo info, bool isLeft)
    {
        AddPropertyItem(listView, "File Name", info.FileName, isLeft, null);
        AddPropertyItem(listView, "Type", info.IsDirectory ? "Directory" : "File", isLeft, null);
        AddPropertyItem(listView, "Unpacked Size", $"{info.UnpackedSize:N0} bytes", isLeft, "Unpacked Size");
        AddPropertyItem(listView, "CRC32", info.FileCrc?.ToString("X8") ?? "N/A", isLeft, "CRC");
        AddPropertyItem(listView, "Compression Method", GetCompressionMethodName(info.CompressionMethod), isLeft, "Compression Method");
        AddPropertyItem(listView, "Dictionary Size", $"{info.DictionarySizeKB} KB", isLeft, null);
        if (info.ModificationTime.HasValue)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(info.ModificationTime.Value).LocalDateTime;
            AddPropertyItem(listView, "Modified Time", dt.ToString("yyyy-MM-dd HH:mm:ss"), isLeft, null);
        }
        AddPropertyItem(listView, "Split Before", FormatBool(info.IsSplitBefore), isLeft, null);
        AddPropertyItem(listView, "Split After", FormatBool(info.IsSplitAfter), isLeft, null);
    }

    private void ShowSRRArchivedFileProperties(ListView listView, SRRFile srr, string fileName, bool isLeft)
    {
        AddPropertyItem(listView, "File Name", fileName, isLeft, null);

        if (srr.ArchivedFileCrcs.TryGetValue(fileName, out var crc))
            AddPropertyItem(listView, "CRC32", crc, isLeft, "CRC");

        if (srr.ArchivedFileTimestamps.TryGetValue(fileName, out var modTime))
            AddPropertyItem(listView, "Modified Time", modTime.ToString("yyyy-MM-dd HH:mm:ss"), isLeft, "Modified Time");

        if (srr.ArchivedFileCreationTimes.TryGetValue(fileName, out var createTime))
            AddPropertyItem(listView, "Creation Time", createTime.ToString("yyyy-MM-dd HH:mm:ss"), isLeft, null);

        if (srr.ArchivedFileAccessTimes.TryGetValue(fileName, out var accessTime))
            AddPropertyItem(listView, "Access Time", accessTime.ToString("yyyy-MM-dd HH:mm:ss"), isLeft, null);
    }

    private void ShowStoredFileProperties(ListView listView, SrrStoredFileBlock stored, bool isLeft)
    {
        AddPropertyItem(listView, "File Name", stored.FileName, isLeft, null);
        AddPropertyItem(listView, "File Size", $"{stored.FileLength:N0} bytes", isLeft, null);
        AddPropertyItem(listView, "Data Offset", $"0x{stored.DataOffset:X}", isLeft, null);
    }

    private void ShowRarVolumeProperties(ListView listView, SrrRarFileBlock rarFile, bool isLeft)
    {
        AddPropertyItem(listView, "Volume Name", rarFile.FileName, isLeft, null);
        AddPropertyItem(listView, "Block Position", $"0x{rarFile.BlockPosition:X}", isLeft, null);
        AddPropertyItem(listView, "Header CRC", $"0x{rarFile.Crc:X4}", isLeft, null);
    }

    private void AddPropertyItem(ListView listView, string property, string value, bool isLeft, string? diffPropertyName)
    {
        var item = new ListViewItem(property);
        item.SubItems.Add(value);

        // Apply highlighting if this property differs
        if (diffPropertyName != null && _compareResult != null)
        {
            var diff = _compareResult.ArchiveDifferences.FirstOrDefault(d => d.PropertyName == diffPropertyName);
            if (diff != null)
            {
                item.BackColor = ColorModified;
            }
        }

        listView.Items.Add(item);
    }

    #endregion

    #region Status Bar

    private void UpdateStatusBar()
    {
        if (_compareResult == null)
        {
            statusLabel.Text = "Load files on both sides to compare.";
            diffSummaryPanel.Visible = false;
            return;
        }

        int archiveDiffs = _compareResult.ArchiveDifferences.Count;
        int added = _compareResult.FileDifferences.Count(d => d.Type == DifferenceType.Added);
        int removed = _compareResult.FileDifferences.Count(d => d.Type == DifferenceType.Removed);
        int modified = _compareResult.FileDifferences.Count(d => d.Type == DifferenceType.Modified);
        int storedAdded = _compareResult.StoredFileDifferences.Count(d => d.Type == DifferenceType.Added);
        int storedRemoved = _compareResult.StoredFileDifferences.Count(d => d.Type == DifferenceType.Removed);

        int totalDiffs = archiveDiffs + added + removed + modified + storedAdded + storedRemoved;

        if (totalDiffs == 0)
        {
            statusLabel.Text = "Files are identical.";
            diffSummaryPanel.Visible = true;
            diffSummaryPanel.BackColor = Color.FromArgb(220, 240, 220);
            lblDiffSummary.Text = "No differences found - files are identical.";
        }
        else
        {
            var parts = new List<string>();
            if (archiveDiffs > 0) parts.Add($"{archiveDiffs} archive property change(s)");
            if (added > 0) parts.Add($"{added} file(s) added");
            if (removed > 0) parts.Add($"{removed} file(s) removed");
            if (modified > 0) parts.Add($"{modified} file(s) modified");
            if (storedAdded > 0) parts.Add($"{storedAdded} stored file(s) added");
            if (storedRemoved > 0) parts.Add($"{storedRemoved} stored file(s) removed");

            statusLabel.Text = $"{totalDiffs} difference(s) found: {string.Join(", ", parts)}";

            diffSummaryPanel.Visible = true;
            diffSummaryPanel.BackColor = Color.FromArgb(255, 245, 220);
            lblDiffSummary.Text = $"{totalDiffs} difference(s): {string.Join(" | ", parts)}";
        }
    }

    #endregion

    #region Helper Methods

    private static string FormatRARVersion(int? version) => version switch
    {
        null => "Unknown",
        50 => "RAR 5.0",
        _ => $"RAR {version / 10}.{version % 10}"
    };

    private static string GetCompressionMethodName(int? method) => method switch
    {
        null => "Unknown",
        0x00 or 0x30 => "Store",
        0x01 or 0x31 => "Fastest",
        0x02 or 0x32 => "Fast",
        0x03 or 0x33 => "Normal",
        0x04 or 0x34 => "Good",
        0x05 or 0x35 => "Best",
        _ => $"Unknown ({method})"
    };

    private static string GetCompressionMethodName(byte method) => GetCompressionMethodName((int?)method);

    private static string GetCompressionMethodName(int method) => GetCompressionMethodName((int?)method);

    private static string FormatDictionarySize(int? size) => size switch
    {
        null => "Unknown",
        _ => $"{size} KB"
    };

    private static string FormatBool(bool? value) => value switch
    {
        null => "Unknown",
        true => "Yes",
        false => "No"
    };

    #endregion

    #region Property Offset Mapping

    /// <summary>
    /// Builds a dictionary mapping property names to their byte offsets in the file.
    /// </summary>
    private static Dictionary<string, ByteRange> BuildPropertyOffsets(CompareNodeData nodeData, byte[]? fileBytes)
    {
        var offsets = new Dictionary<string, ByteRange>();
        if (fileBytes == null) return offsets;

        switch (nodeData.NodeType)
        {
            case CompareNodeType.ArchiveInfo:
                if (nodeData.Data is RARFileData rarData)
                {
                    BuildRARArchiveOffsets(offsets, fileBytes, rarData.IsRAR5);
                }
                else if (nodeData.Data is SRRFile)
                {
                    BuildSRRArchiveOffsets(offsets, fileBytes);
                }
                break;

            case CompareNodeType.ArchivedFile:
                if (nodeData.Data is RARFileHeader fileHeader)
                {
                    BuildRAR4FileOffsets(offsets, fileBytes, fileHeader);
                }
                else if (nodeData.Data is RAR5FileInfo fileInfo)
                {
                    BuildRAR5FileOffsets(offsets, fileBytes, fileInfo);
                }
                break;

            case CompareNodeType.StoredFile:
                if (nodeData.Data is SrrStoredFileBlock storedFile)
                {
                    BuildSRRStoredFileOffsets(offsets, storedFile);
                }
                break;

            case CompareNodeType.RarVolume:
                if (nodeData.Data is SrrRarFileBlock rarFile)
                {
                    BuildSRRRarVolumeOffsets(offsets, rarFile);
                }
                break;
        }

        return offsets;
    }

    private static void BuildRARArchiveOffsets(Dictionary<string, ByteRange> offsets, byte[] fileBytes, bool isRAR5)
    {
        if (isRAR5)
        {
            // RAR 5.x signature
            offsets["Format"] = new ByteRange { PropertyName = "Format", Offset = 0, Length = 8 };

            // Main archive header typically starts at offset 8
            // CRC32 (4 bytes) + HeaderSize (vint) + HeaderType (vint) + Flags (vint)
            if (fileBytes.Length > 12)
            {
                offsets["Volume"] = new ByteRange { PropertyName = "Volume", Offset = 8, Length = 10 };
                offsets["Solid"] = new ByteRange { PropertyName = "Solid", Offset = 8, Length = 10 };
                offsets["Recovery Record"] = new ByteRange { PropertyName = "Recovery Record", Offset = 8, Length = 10 };
                offsets["Locked"] = new ByteRange { PropertyName = "Locked", Offset = 8, Length = 10 };
            }
        }
        else
        {
            // RAR 4.x signature: Rar!\x1a\x07\x00
            offsets["Format"] = new ByteRange { PropertyName = "Format", Offset = 0, Length = 7 };

            // Marker block (7 bytes) followed by archive header
            // Archive header: CRC (2) + Type (1) + Flags (2) + Size (2) + Reserved1 (2) + Reserved2 (4)
            if (fileBytes.Length > 20)
            {
                offsets["Volume"] = new ByteRange { PropertyName = "Volume", Offset = 10, Length = 2 };
                offsets["Solid"] = new ByteRange { PropertyName = "Solid", Offset = 10, Length = 2 };
                offsets["Recovery Record"] = new ByteRange { PropertyName = "Recovery Record", Offset = 10, Length = 2 };
                offsets["Locked"] = new ByteRange { PropertyName = "Locked", Offset = 10, Length = 2 };
                offsets["Encrypted Headers"] = new ByteRange { PropertyName = "Encrypted Headers", Offset = 10, Length = 2 };
            }
        }
    }

    private static void BuildSRRArchiveOffsets(Dictionary<string, ByteRange> offsets, byte[] fileBytes)
    {
        // SRR header block starts at offset 0
        // CRC (2) + Type (1) + Flags (2) + Size (2) + AppNameLen (2) + AppName (variable)
        if (fileBytes.Length > 7)
        {
            offsets["App Name"] = new ByteRange { PropertyName = "App Name", Offset = 7, Length = Math.Min(32, fileBytes.Length - 7) };
        }
    }

    private static void BuildRAR4FileOffsets(Dictionary<string, ByteRange> offsets, byte[] fileBytes, RARFileHeader header)
    {
        // Find the file header in the raw bytes by searching for the filename
        long offset = FindRAR4FileHeader(fileBytes, header.FileName);
        if (offset < 0) return;

        // RAR 4.x file header structure:
        // CRC (2) + Type (1) + Flags (2) + Size (2) + PackedSize (4) + UnpackedSize (4) + Host (1) + CRC32 (4) +
        // Time (4) + Version (1) + Method (1) + NameLen (2) + Attr (4) + [HighPackSize (4)] + [HighUnpSize (4)] + Name

        offsets["File Name"] = new ByteRange { PropertyName = "File Name", Offset = offset, Length = (int)header.HeaderSize };

        if (offset + 25 <= fileBytes.Length)
        {
            offsets["Packed Size"] = new ByteRange { PropertyName = "Packed Size", Offset = offset + 7, Length = 4 };
            offsets["Unpacked Size"] = new ByteRange { PropertyName = "Unpacked Size", Offset = offset + 11, Length = 4 };
            offsets["CRC32"] = new ByteRange { PropertyName = "CRC32", Offset = offset + 16, Length = 4 };
            offsets["Modified Time"] = new ByteRange { PropertyName = "Modified Time", Offset = offset + 20, Length = 4 };
            offsets["Compression Method"] = new ByteRange { PropertyName = "Compression Method", Offset = offset + 25, Length = 1 };
        }
    }

    private static void BuildRAR5FileOffsets(Dictionary<string, ByteRange> offsets, byte[] fileBytes, RAR5FileInfo fileInfo)
    {
        // Find the file header in the raw bytes
        long offset = FindRAR5FileHeader(fileBytes, fileInfo.FileName);
        if (offset < 0) return;

        // RAR 5.x file header: CRC32 (4) + HeaderSize (vint) + Type (vint) + Flags (vint) + ...
        offsets["File Name"] = new ByteRange { PropertyName = "File Name", Offset = offset, Length = 50 };

        if (offset + 20 <= fileBytes.Length)
        {
            offsets["CRC32"] = new ByteRange { PropertyName = "CRC32", Offset = offset, Length = 4 };
            offsets["Unpacked Size"] = new ByteRange { PropertyName = "Unpacked Size", Offset = offset + 10, Length = 8 };
            offsets["Compression Method"] = new ByteRange { PropertyName = "Compression Method", Offset = offset + 20, Length = 4 };
        }
    }

    private static void BuildSRRStoredFileOffsets(Dictionary<string, ByteRange> offsets, SrrStoredFileBlock storedFile)
    {
        // Block position points to the header
        offsets["File Name"] = new ByteRange { PropertyName = "File Name", Offset = storedFile.BlockPosition, Length = storedFile.HeaderSize };
        offsets["File Size"] = new ByteRange { PropertyName = "File Size", Offset = storedFile.BlockPosition, Length = storedFile.HeaderSize };
        offsets["Data Offset"] = new ByteRange { PropertyName = "Data Offset", Offset = storedFile.DataOffset, Length = (int)Math.Min(64, storedFile.FileLength) };
    }

    private static void BuildSRRRarVolumeOffsets(Dictionary<string, ByteRange> offsets, SrrRarFileBlock rarFile)
    {
        offsets["Volume Name"] = new ByteRange { PropertyName = "Volume Name", Offset = rarFile.BlockPosition, Length = rarFile.HeaderSize };
        offsets["Block Position"] = new ByteRange { PropertyName = "Block Position", Offset = rarFile.BlockPosition, Length = rarFile.HeaderSize };
        offsets["Header CRC"] = new ByteRange { PropertyName = "Header CRC", Offset = rarFile.BlockPosition, Length = 2 };
    }

    private static long FindRAR4FileHeader(byte[] fileBytes, string fileName)
    {
        // Search for the filename bytes in the file
        byte[] nameBytes = System.Text.Encoding.GetEncoding(866).GetBytes(fileName);
        if (nameBytes.Length == 0) return -1;

        for (int i = 0; i < fileBytes.Length - nameBytes.Length - 25; i++)
        {
            // Check if this could be a file header (type 0x74)
            if (i >= 2 && fileBytes[i + 2] == 0x74)
            {
                // Try to find the filename after the fixed header fields
                int nameOffset = i + 25; // Approximate offset to name length field
                if (nameOffset + 2 < fileBytes.Length)
                {
                    int nameLen = fileBytes[nameOffset] | (fileBytes[nameOffset + 1] << 8);
                    if (nameLen == nameBytes.Length && nameOffset + 2 + nameLen <= fileBytes.Length)
                    {
                        bool match = true;
                        for (int j = 0; j < nameLen && match; j++)
                        {
                            if (fileBytes[nameOffset + 2 + j] != nameBytes[j])
                                match = false;
                        }
                        if (match) return i;
                    }
                }
            }
        }
        return -1;
    }

    private static long FindRAR5FileHeader(byte[] fileBytes, string fileName)
    {
        // Search for the UTF-8 filename bytes in the file
        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
        if (nameBytes.Length == 0) return -1;

        for (int i = 8; i < fileBytes.Length - nameBytes.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < nameBytes.Length && match; j++)
            {
                if (fileBytes[i + j] != nameBytes[j])
                    match = false;
            }
            if (match)
            {
                // Go back to find the start of the header (CRC32)
                // Typically the header starts 20-50 bytes before the filename
                return Math.Max(0, i - 30);
            }
        }
        return -1;
    }

    #endregion
}

#region Data Models

public class RARFileData
{
    public string FilePath { get; set; } = string.Empty;
    public bool IsRAR5 { get; set; }
    public RARArchiveHeader? ArchiveHeader { get; set; }
    public RAR5ArchiveInfo? RAR5ArchiveInfo { get; set; }
    public List<RARFileHeader> FileHeaders { get; set; } = [];
    public List<RAR5FileInfo> RAR5FileInfos { get; set; } = [];
    public string? Comment { get; set; }
}

public class CompareResult
{
    public List<PropertyDifference> ArchiveDifferences { get; set; } = [];
    public List<FileDifference> FileDifferences { get; set; } = [];
    public List<FileDifference> StoredFileDifferences { get; set; } = [];
    public int TotalDifferences => ArchiveDifferences.Count + FileDifferences.Count + StoredFileDifferences.Count;
}

public class PropertyDifference
{
    public string PropertyName { get; set; } = string.Empty;
    public string LeftValue { get; set; } = string.Empty;
    public string RightValue { get; set; } = string.Empty;
}

public class FileDifference
{
    public string FileName { get; set; } = string.Empty;
    public DifferenceType Type { get; set; } = DifferenceType.None;
    public List<PropertyDifference> PropertyDifferences { get; set; } = [];
}

public enum DifferenceType
{
    None,
    Added,
    Removed,
    Modified
}

public enum CompareNodeType
{
    Root,
    ArchiveInfo,
    RarVolumes,
    RarVolume,
    StoredFiles,
    StoredFile,
    ArchivedFiles,
    ArchivedFile,
    OsoHashes,
    DetailedBlock
}

public class CompareNodeData
{
    public CompareNodeType NodeType { get; set; }
    public object? Data { get; set; }
    public string? FileName { get; set; }
    public bool IsLeft { get; set; }
}

#endregion
