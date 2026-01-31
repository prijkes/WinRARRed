using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinRARRed.Controls;

/// <summary>
/// A high-performance control for displaying hex data with highlighting support.
/// Uses direct RTF generation instead of slow Selection* APIs.
/// Limits display to first MaxDisplayBytes to prevent memory issues.
/// </summary>
public class HexViewControl : UserControl
{
    private readonly RichTextBox _richTextBox;
    private byte[]? _data;
    private byte[]? _otherData; // Store reference for comparison instead of copying
    private int _bytesPerLine = 16;
    private int _selectionStart = -1;
    private int _selectionLength = 0;
    private int _totalFileSize;

    /// <summary>
    /// Maximum bytes to display in hex view (default 64KB).
    /// </summary>
    public const int MaxDisplayBytes = 65536;

    public HexViewControl()
    {
        _richTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9F, FontStyle.Regular),
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            DetectUrls = false
        };

        Controls.Add(_richTextBox);
        BorderStyle = BorderStyle.FixedSingle;
    }

    /// <summary>
    /// Gets or sets the number of bytes displayed per line.
    /// </summary>
    public int BytesPerLine
    {
        get => _bytesPerLine;
        set
        {
            if (value > 0 && value != _bytesPerLine)
            {
                _bytesPerLine = value;
                RefreshDisplay();
            }
        }
    }

    /// <summary>
    /// Loads data to display in the hex view.
    /// </summary>
    public void LoadData(byte[]? data)
    {
        _otherData = null;
        _selectionStart = -1;
        _selectionLength = 0;

        if (data == null)
        {
            _data = null;
            _totalFileSize = 0;
            _richTextBox.Clear();
            return;
        }

        _totalFileSize = data.Length;

        // Limit display size to prevent memory issues
        if (data.Length > MaxDisplayBytes)
        {
            _data = new byte[MaxDisplayBytes];
            Array.Copy(data, _data, MaxDisplayBytes);
        }
        else
        {
            _data = data;
        }

        RefreshDisplay();
    }

    /// <summary>
    /// Clears all data and highlights.
    /// </summary>
    public void Clear()
    {
        _data = null;
        _otherData = null;
        _totalFileSize = 0;
        _selectionStart = -1;
        _selectionLength = 0;
        _richTextBox.Clear();
    }

    /// <summary>
    /// Sets the selection highlight and scrolls to it.
    /// </summary>
    public void SelectRange(long offset, int length)
    {
        if (offset >= MaxDisplayBytes)
        {
            // Selection is beyond displayed range
            _selectionStart = -1;
            _selectionLength = 0;
            return;
        }

        _selectionStart = (int)offset;
        _selectionLength = Math.Min(length, MaxDisplayBytes - _selectionStart);
        RefreshDisplay();
        ScrollToOffset(offset);
    }

    /// <summary>
    /// Clears the selection highlight.
    /// </summary>
    public void ClearSelection()
    {
        if (_selectionStart >= 0)
        {
            _selectionStart = -1;
            _selectionLength = 0;
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Scrolls the view to show the specified byte offset.
    /// </summary>
    public void ScrollToOffset(long offset)
    {
        if (_data == null || _data.Length == 0) return;
        if (offset >= _data.Length) return;

        int lineNumber = (int)(offset / _bytesPerLine);
        int charIndex = GetCharIndexForLine(lineNumber);

        if (charIndex >= 0 && charIndex < _richTextBox.TextLength)
        {
            _richTextBox.SelectionStart = charIndex;
            _richTextBox.SelectionLength = 0;
            _richTextBox.ScrollToCaret();
        }
    }

    private int GetCharIndexForLine(int lineNumber)
    {
        // Approximate - each line is roughly the same length
        int lineLength = 8 + 2 + (_bytesPerLine * 3 - 1) + 3 + _bytesPerLine + 2;
        return lineNumber * lineLength;
    }

    /// <summary>
    /// Sets the other file's data for comparison highlighting.
    /// </summary>
    public void SetComparisonData(byte[]? otherData)
    {
        if (otherData == null)
        {
            _otherData = null;
        }
        else if (otherData.Length > MaxDisplayBytes)
        {
            _otherData = new byte[MaxDisplayBytes];
            Array.Copy(otherData, _otherData, MaxDisplayBytes);
        }
        else
        {
            _otherData = otherData;
        }

        RefreshDisplay();
    }

    /// <summary>
    /// Refreshes the hex display with current data and highlights.
    /// </summary>
    public void RefreshDisplay()
    {
        if (_data == null || _data.Length == 0)
        {
            _richTextBox.Clear();
            return;
        }

        string rtf = BuildRtf();
        _richTextBox.Rtf = rtf;
    }

    private string BuildRtf()
    {
        int displayLength = _data!.Length;
        int totalLines = (displayLength + _bytesPerLine - 1) / _bytesPerLine;

        // Estimate capacity: ~80 chars per line base + extra for highlights
        var rtf = new StringBuilder(totalLines * 100);

        // RTF header with color table
        rtf.Append(@"{\rtf1\ansi\deff0");
        rtf.Append(@"{\fonttbl{\f0\fmodern Consolas;}}");
        rtf.Append(@"{\colortbl;");
        rtf.Append(@"\red0\green0\blue0;");           // 1: Black (default)
        rtf.Append(@"\red128\green128\blue128;");     // 2: Gray (addresses)
        rtf.Append(@"\red0\green100\blue0;");         // 3: Dark green (ASCII)
        rtf.Append(@"\red255\green200\blue200;");     // 4: Light red (diff background)
        rtf.Append(@"\red100\green149\blue237;");     // 5: Cornflower blue (selection bg)
        rtf.Append(@"\red255\green255\blue255;");     // 6: White (selection fg)
        rtf.Append('}');
        rtf.Append(@"\f0\fs18 ");  // Font and size

        // Show truncation notice if file is larger than display limit
        if (_totalFileSize > MaxDisplayBytes)
        {
            rtf.Append(@"\cf2 [Showing first ");
            rtf.Append(MaxDisplayBytes / 1024);
            rtf.Append(@" KB of ");
            rtf.Append(_totalFileSize / 1024);
            rtf.Append(@" KB]\cf1\par\par ");
        }

        for (int line = 0; line < totalLines; line++)
        {
            int offset = line * _bytesPerLine;
            int count = Math.Min(_bytesPerLine, displayLength - offset);

            // Address (gray)
            rtf.Append(@"\cf2 ");
            rtf.Append(offset.ToString("X8"));
            rtf.Append(@"\cf1   ");

            // Hex bytes - batch consecutive same-state bytes
            int i = 0;
            while (i < _bytesPerLine)
            {
                if (i < count)
                {
                    int byteIndex = offset + i;
                    bool isDiff = IsByteDifferent(byteIndex);
                    bool isSelected = IsByteSelected(byteIndex);

                    // Find run of bytes with same state
                    int runEnd = i + 1;
                    while (runEnd < count && runEnd < _bytesPerLine)
                    {
                        int nextIndex = offset + runEnd;
                        bool nextDiff = IsByteDifferent(nextIndex);
                        bool nextSel = IsByteSelected(nextIndex);
                        if (nextDiff != isDiff || nextSel != isSelected)
                            break;
                        runEnd++;
                    }

                    // Apply formatting for run
                    if (isSelected)
                    {
                        rtf.Append(@"\highlight5\cf6 ");
                    }
                    else if (isDiff)
                    {
                        rtf.Append(@"\highlight4\cf1 ");
                    }

                    // Output bytes in run
                    for (int j = i; j < runEnd; j++)
                    {
                        rtf.Append(_data[offset + j].ToString("X2"));
                        if (j < _bytesPerLine - 1)
                            rtf.Append(' ');
                    }

                    if (isSelected || isDiff)
                    {
                        rtf.Append(@"\highlight0\cf1 ");
                    }

                    i = runEnd;
                }
                else
                {
                    rtf.Append("  ");
                    if (i < _bytesPerLine - 1)
                        rtf.Append(' ');
                    i++;
                }
            }

            // ASCII (dark green)
            rtf.Append(@"  \cf3 |");

            i = 0;
            while (i < _bytesPerLine)
            {
                if (i < count)
                {
                    int byteIndex = offset + i;
                    bool isDiff = IsByteDifferent(byteIndex);
                    bool isSelected = IsByteSelected(byteIndex);

                    // Find run of bytes with same state
                    int runEnd = i + 1;
                    while (runEnd < count && runEnd < _bytesPerLine)
                    {
                        int nextIndex = offset + runEnd;
                        bool nextDiff = IsByteDifferent(nextIndex);
                        bool nextSel = IsByteSelected(nextIndex);
                        if (nextDiff != isDiff || nextSel != isSelected)
                            break;
                        runEnd++;
                    }

                    // Apply formatting for run
                    if (isSelected)
                    {
                        rtf.Append(@"\highlight5\cf6 ");
                    }
                    else if (isDiff)
                    {
                        rtf.Append(@"\highlight4\cf3 ");
                    }

                    // Output ASCII chars in run
                    for (int j = i; j < runEnd; j++)
                    {
                        byte b = _data[offset + j];
                        char c = (b >= 32 && b < 127) ? (char)b : '.';

                        // Escape special RTF characters
                        if (c == '\\' || c == '{' || c == '}')
                        {
                            rtf.Append('\\');
                        }
                        rtf.Append(c);
                    }

                    if (isSelected || isDiff)
                    {
                        rtf.Append(@"\highlight0\cf3 ");
                    }

                    i = runEnd;
                }
                else
                {
                    rtf.Append(' ');
                    i++;
                }
            }

            rtf.Append(@"|\cf1\par ");
        }

        rtf.Append('}');
        return rtf.ToString();
    }

    private bool IsByteDifferent(int index)
    {
        if (_otherData == null) return false;
        if (index >= _data!.Length) return true;
        if (index >= _otherData.Length) return true;
        return _data[index] != _otherData[index];
    }

    private bool IsByteSelected(int index)
    {
        return _selectionStart >= 0 &&
               index >= _selectionStart &&
               index < _selectionStart + _selectionLength;
    }
}

/// <summary>
/// Represents a byte range in a file with a description.
/// </summary>
public class ByteRange
{
    public string PropertyName { get; set; } = string.Empty;
    public long Offset { get; set; }
    public int Length { get; set; }
}
