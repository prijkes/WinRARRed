using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WinRARRed.Controls;

/// <summary>
/// A high-performance control for displaying hex data with highlighting support.
/// Uses virtualized custom painting — only visible lines are drawn each frame.
/// </summary>
public class HexViewControl : UserControl
{
    private sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
        }
    }

    private static readonly string[] HexLookup = BuildHexLookup();
    // Static brushes are intentional — they live for the process lifetime to avoid
    // GDI handle churn during OnPaint. Windows reclaims them on process exit.
    private static readonly SolidBrush DiffBrush = new(Color.FromArgb(255, 200, 200));
    private static readonly SolidBrush SelectionBrush = new(Color.FromArgb(51, 153, 255));
    private static readonly Color AddressColor = Color.Gray;
    private static readonly Color HexColor = Color.Black;
    private static readonly Color AsciiColor = Color.FromArgb(0, 100, 0);

    private readonly DoubleBufferedPanel _panel;
    private readonly VScrollBar _scrollBar;
    private readonly Font _font;

    private byte[]? _data;
    private byte[]? _fullData;
    private byte[]? _otherData;
    private byte[]? _otherFullData;
    private int _bytesPerLine = 16;
    private int _selectionStart = -1;
    private int _selectionLength = 0;
    private int _totalFileSize;
    private long _blockStartOffset;
    private bool _blockViewMode;

    // Mouse selection state
    private int _selectionAnchor = -1;
    private bool _isDragging;
    private int _caretIndex = -1;
    private System.Windows.Forms.Timer? _autoScrollTimer;
    private int _autoScrollDirection;
    private ContextMenuStrip? _contextMenu;

    // Reusable StringBuilder to avoid allocations during OnPaint
    private readonly StringBuilder _paintBuffer = new(128);

    // Cached metrics
    private int _charWidth;
    private int _lineHeight;
    private int _bannerLineCount;

    // Column X positions (in pixels)
    private int _addressX;
    private int _hexX;
    private int _asciiX;

    public HexViewControl()
    {
        _font = new Font("Consolas", 9F, FontStyle.Regular);

        _scrollBar = new VScrollBar
        {
            Dock = DockStyle.Right,
            Minimum = 0,
            Value = 0
        };
        _scrollBar.Scroll += OnScrollBarScroll;

        _panel = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Cursor = Cursors.IBeam
        };
        _panel.Paint += OnPanelPaint;
        _panel.MouseDown += OnPanelMouseDown;
        _panel.MouseMove += OnPanelMouseMove;
        _panel.MouseUp += OnPanelMouseUp;
        _panel.Resize += OnPanelResize;

        _contextMenu = BuildContextMenu();
        _panel.ContextMenuStrip = _contextMenu;

        // WinForms docks in reverse Z-order: scrollbar (added last) docks right first,
        // then panel fills the remaining space
        Controls.Add(_panel);
        Controls.Add(_scrollBar);
        BorderStyle = BorderStyle.FixedSingle;

        CacheMetrics();
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
                CacheMetrics();
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
        _otherFullData = null;
        _selectionStart = -1;
        _selectionLength = 0;
        _blockViewMode = false;
        _blockStartOffset = 0;
        ResetMouseState();

        if (data == null)
        {
            _data = null;
            _fullData = null;
            _totalFileSize = 0;
            _scrollBar.Value = 0;
            _panel.Invalidate();
            return;
        }

        _fullData = data;
        _totalFileSize = data.Length;

        _data = data;

        RefreshDisplay();
    }

    /// <summary>
    /// Loads only a specific block/range of data from the file.
    /// The hex view will show offsets relative to the file, not the block.
    /// </summary>
    /// <param name="startOffset">Start offset in the file</param>
    /// <param name="length">Length of block to display</param>
    public void LoadBlockData(long startOffset, int length)
    {
        if (_fullData == null || startOffset < 0 || startOffset >= _fullData.Length)
        {
            _scrollBar.Value = 0;
            _panel.Invalidate();
            return;
        }

        _blockViewMode = true;
        _blockStartOffset = startOffset;
        _selectionStart = -1;
        _selectionLength = 0;
        ResetMouseState();

        int actualLength = (int)Math.Min(length, _fullData.Length - startOffset);

        _data = new byte[actualLength];
        Array.Copy(_fullData, startOffset, _data, 0, actualLength);

        if (_otherFullData != null && startOffset < _otherFullData.Length)
        {
            int otherLength = (int)Math.Min(actualLength, _otherFullData.Length - startOffset);
            _otherData = new byte[otherLength];
            Array.Copy(_otherFullData, startOffset, _otherData, 0, otherLength);
        }
        else
        {
            _otherData = null;
        }

        RefreshDisplay();
    }

    /// <summary>
    /// Resets to showing the full file (exits block view mode).
    /// </summary>
    public void ShowFullFile()
    {
        if (_fullData == null) return;

        _blockViewMode = false;
        _blockStartOffset = 0;
        _selectionStart = -1;
        _selectionLength = 0;
        ResetMouseState();

        _data = _fullData;

        if (_otherFullData != null)
        {
            _otherData = _otherFullData;
        }

        RefreshDisplay();
    }

    /// <summary>
    /// Clears all data and highlights.
    /// </summary>
    public void Clear()
    {
        _data = null;
        _fullData = null;
        _otherData = null;
        _otherFullData = null;
        _totalFileSize = 0;
        _selectionStart = -1;
        _selectionLength = 0;
        ResetMouseState();
        _scrollBar.Value = 0;
        _panel.Invalidate();
    }

    /// <summary>
    /// Sets the selection highlight and scrolls to it.
    /// Accepts absolute file offsets - automatically converts to display-relative offsets in block view mode.
    /// </summary>
    public void SelectRange(long offset, int length)
    {
        if (_data == null || _data.Length == 0)
        {
            _selectionStart = -1;
            _selectionLength = 0;
            return;
        }

        long displayOffset = offset;
        if (_blockViewMode)
        {
            displayOffset = offset - _blockStartOffset;

            if (displayOffset < 0 || displayOffset >= _data.Length)
            {
                _selectionStart = -1;
                _selectionLength = 0;
                return;
            }
        }
        else
        {
            if (displayOffset >= _data.Length)
            {
                _selectionStart = -1;
                _selectionLength = 0;
                return;
            }
        }

        _selectionStart = (int)displayOffset;
        _selectionLength = Math.Min(length, _data.Length - _selectionStart);
        _selectionAnchor = _selectionStart;
        _caretIndex = _selectionStart;
        _isDragging = false;
        StopAutoScroll();
        RefreshDisplay();
        ScrollToOffset(displayOffset);
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
            ResetMouseState();
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

        int targetLine = (int)(offset / _bytesPerLine) + _bannerLineCount;
        int visibleLines = GetVisibleLineCount();

        // Center the target line in the viewport
        int scrollTo = Math.Max(0, targetLine - visibleLines / 2);
        int maxScroll = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
        scrollTo = Math.Min(scrollTo, Math.Max(0, maxScroll));

        _scrollBar.Value = scrollTo;
        _panel.Invalidate();
    }

    /// <summary>
    /// Sets the other file's data for comparison highlighting.
    /// </summary>
    public void SetComparisonData(byte[]? otherData)
    {
        _otherFullData = otherData;

        if (otherData == null)
        {
            _otherData = null;
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
        RecalculateScrollParameters();
        _panel.Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_scrollBar.Maximum == 0) return;

        int delta = SystemInformation.MouseWheelScrollLines;
        int newValue = _scrollBar.Value - (e.Delta > 0 ? delta : -delta);
        int maxScroll = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
        newValue = Math.Clamp(newValue, 0, Math.Max(0, maxScroll));

        _scrollBar.Value = newValue;
        _panel.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoScrollTimer?.Dispose();
            _contextMenu?.Dispose();
            _font.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnScrollBarScroll(object? sender, ScrollEventArgs e)
    {
        _panel.Invalidate();
    }

    private void OnPanelMouseDown(object? sender, MouseEventArgs e)
    {
        _panel.Focus();

        if (e.Button == MouseButtons.Left)
        {
            int byteIndex = HitTestByte(e.Location);
            if (byteIndex < 0)
            {
                _selectionStart = -1;
                _selectionLength = 0;
                ResetMouseState();
                _panel.Invalidate();
                return;
            }

            if ((Control.ModifierKeys & Keys.Shift) != 0 && _selectionAnchor >= 0)
            {
                // Shift+click: extend selection from anchor
                ExtendSelectionTo(byteIndex);
            }
            else
            {
                // Normal click: start new selection at this byte
                _selectionAnchor = byteIndex;
                _caretIndex = byteIndex;
                _selectionStart = byteIndex;
                _selectionLength = 1;
                _panel.Invalidate();
            }

            _isDragging = true;
        }
        else if (e.Button == MouseButtons.Right)
        {
            int byteIndex = HitTestByte(e.Location);
            if (byteIndex >= 0 && !IsByteSelected(byteIndex))
            {
                // Right-click outside selection: move caret there
                _selectionAnchor = byteIndex;
                _caretIndex = byteIndex;
                _selectionStart = byteIndex;
                _selectionLength = 1;
                _panel.Invalidate();
            }
        }
    }

    private void OnPanelMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging || _data == null) return;

        // Check if mouse is above or below the panel for auto-scroll
        if (e.Y < 0)
        {
            _autoScrollDirection = -1;
            StartAutoScroll();
        }
        else if (e.Y >= _panel.ClientSize.Height)
        {
            _autoScrollDirection = 1;
            StartAutoScroll();
        }
        else
        {
            StopAutoScroll();
        }

        int byteIndex = HitTestByteClamp(e.Location);
        if (byteIndex >= 0)
        {
            ExtendSelectionTo(byteIndex);
        }
    }

    private void OnPanelMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            StopAutoScroll();
        }
    }

    private void OnPanelResize(object? sender, EventArgs e)
    {
        RecalculateScrollParameters();
    }

    /// <summary>
    /// Maps a pixel position to a byte index within the current data.
    /// Returns -1 if the position is outside the hex or ASCII columns.
    /// </summary>
    private int HitTestByte(Point pt)
    {
        if (_data == null || _data.Length == 0 || _lineHeight <= 0) return -1;

        int firstVisibleLine = _scrollBar.Value;
        int lineIdx = firstVisibleLine + pt.Y / _lineHeight;

        if (lineIdx < _bannerLineCount) return -1;

        int dataLine = lineIdx - _bannerLineCount;
        int lineOffset = dataLine * _bytesPerLine;
        if (lineOffset >= _data.Length) return -1;

        int count = Math.Min(_bytesPerLine, _data.Length - lineOffset);
        int byteInLine = -1;

        // Check hex column
        int hexEndX = _hexX + _bytesPerLine * 3 * _charWidth;
        if (pt.X >= _hexX && pt.X < hexEndX)
        {
            byteInLine = (pt.X - _hexX) / (3 * _charWidth);
        }

        // Check ASCII column (skip leading pipe)
        int pipeWidth = _charWidth;
        int asciiStartX = _asciiX + pipeWidth;
        int asciiEndX = asciiStartX + _bytesPerLine * _charWidth;
        if (pt.X >= asciiStartX && pt.X < asciiEndX)
        {
            byteInLine = (pt.X - asciiStartX) / _charWidth;
        }

        if (byteInLine < 0 || byteInLine >= count) return -1;

        return lineOffset + byteInLine;
    }

    /// <summary>
    /// Like HitTestByte but clamps to the nearest valid byte for drag-beyond-edges.
    /// </summary>
    private int HitTestByteClamp(Point pt)
    {
        if (_data == null || _data.Length == 0 || _lineHeight <= 0) return -1;

        int firstVisibleLine = _scrollBar.Value;
        int lineIdx = firstVisibleLine + pt.Y / _lineHeight;

        // Clamp to data range
        lineIdx = Math.Max(_bannerLineCount, lineIdx);

        int dataLine = lineIdx - _bannerLineCount;
        int lineOffset = dataLine * _bytesPerLine;

        if (lineOffset >= _data.Length)
        {
            return _data.Length - 1;
        }

        int count = Math.Min(_bytesPerLine, _data.Length - lineOffset);

        int byteInLine;
        // Determine byte-in-line from X, clamped to [0, count-1]
        int hexEndX = _hexX + _bytesPerLine * 3 * _charWidth;
        int pipeWidth = _charWidth;
        int asciiStartX = _asciiX + pipeWidth;
        int asciiEndX = asciiStartX + _bytesPerLine * _charWidth;

        if (pt.X >= asciiStartX && pt.X < asciiEndX)
        {
            byteInLine = (pt.X - asciiStartX) / _charWidth;
        }
        else if (pt.X >= _hexX && pt.X < hexEndX)
        {
            byteInLine = (pt.X - _hexX) / (3 * _charWidth);
        }
        else if (pt.X < _hexX)
        {
            byteInLine = 0;
        }
        else
        {
            byteInLine = count - 1;
        }

        byteInLine = Math.Clamp(byteInLine, 0, count - 1);
        return lineOffset + byteInLine;
    }

    private void ExtendSelectionTo(int byteIndex)
    {
        if (_selectionAnchor < 0) return;

        _caretIndex = byteIndex;
        _selectionStart = Math.Min(_selectionAnchor, byteIndex);
        _selectionLength = Math.Abs(_selectionAnchor - byteIndex) + 1;
        _panel.Invalidate();
    }

    private void StartAutoScroll()
    {
        if (_autoScrollTimer != null) return;

        _autoScrollTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _autoScrollTimer.Tick += OnAutoScrollTick;
        _autoScrollTimer.Start();
    }

    private void StopAutoScroll()
    {
        if (_autoScrollTimer == null) return;

        _autoScrollTimer.Stop();
        _autoScrollTimer.Tick -= OnAutoScrollTick;
        _autoScrollTimer.Dispose();
        _autoScrollTimer = null;
        _autoScrollDirection = 0;
    }

    private void OnAutoScrollTick(object? sender, EventArgs e)
    {
        if (_data == null || !_isDragging)
        {
            StopAutoScroll();
            return;
        }

        int maxScroll = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
        int newValue = Math.Clamp(_scrollBar.Value + _autoScrollDirection, 0, Math.Max(0, maxScroll));

        if (newValue != _scrollBar.Value)
        {
            _scrollBar.Value = newValue;

            // Extend selection to the edge byte in the scroll direction
            int firstVisibleLine = _scrollBar.Value;
            int visibleLines = GetVisibleLineCount();

            int edgeLine;
            if (_autoScrollDirection < 0)
            {
                edgeLine = firstVisibleLine - _bannerLineCount;
            }
            else
            {
                edgeLine = firstVisibleLine + visibleLines - 1 - _bannerLineCount;
            }

            int edgeOffset = edgeLine * _bytesPerLine;
            if (_autoScrollDirection > 0)
            {
                edgeOffset += _bytesPerLine - 1;
            }

            edgeOffset = Math.Clamp(edgeOffset, 0, _data.Length - 1);
            ExtendSelectionTo(edgeOffset);
        }
    }

    private void ResetMouseState()
    {
        _selectionAnchor = -1;
        _caretIndex = -1;
        _isDragging = false;
        StopAutoScroll();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.C))
        {
            CopySelectionAsHex();
            return true;
        }

        if (keyData == (Keys.Control | Keys.A))
        {
            SelectAll();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SelectAll()
    {
        if (_data == null || _data.Length == 0) return;

        _selectionAnchor = 0;
        _caretIndex = _data.Length - 1;
        _selectionStart = 0;
        _selectionLength = _data.Length;
        _panel.Invalidate();
    }

    private void CopySelectionAsHex()
    {
        if (_data == null || _selectionStart < 0 || _selectionLength <= 0) return;

        var sb = new StringBuilder(_selectionLength * 3);
        int end = Math.Min(_selectionStart + _selectionLength, _data.Length);
        for (int i = _selectionStart; i < end; i++)
        {
            if (i > _selectionStart) sb.Append(' ');
            sb.Append(HexLookup[_data[i]]);
        }

        SetClipboardText(sb.ToString());
    }

    private void CopySelectionAsText()
    {
        if (_data == null || _selectionStart < 0 || _selectionLength <= 0) return;

        var sb = new StringBuilder(_selectionLength);
        int end = Math.Min(_selectionStart + _selectionLength, _data.Length);
        for (int i = _selectionStart; i < end; i++)
        {
            byte b = _data[i];
            sb.Append((b >= 32 && b < 127) ? (char)b : '.');
        }

        SetClipboardText(sb.ToString());
    }

    private static void SetClipboardText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (ExternalException)
        {
            // Clipboard may be locked by another process
        }
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var copyHex = new ToolStripMenuItem("Copy as Hex", null, (_, _) => CopySelectionAsHex())
        {
            ShortcutKeyDisplayString = "Ctrl+C"
        };
        var copyText = new ToolStripMenuItem("Copy as Text", null, (_, _) => CopySelectionAsText());
        var selectAll = new ToolStripMenuItem("Select All", null, (_, _) => SelectAll())
        {
            ShortcutKeyDisplayString = "Ctrl+A"
        };

        menu.Items.Add(copyHex);
        menu.Items.Add(copyText);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(selectAll);

        menu.Opening += (_, _) =>
        {
            bool hasSelection = _selectionStart >= 0 && _selectionLength > 0;
            bool hasData = _data != null && _data.Length > 0;
            copyHex.Enabled = hasSelection;
            copyText.Enabled = hasSelection;
            selectAll.Enabled = hasData;
        };

        return menu;
    }

    private void CacheMetrics()
    {
        // Measure a single character to get monospace dimensions
        Size measured = TextRenderer.MeasureText("W", _font, new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        _charWidth = measured.Width;
        _lineHeight = measured.Height;

        // Layout:  ADDRESS  (gap)  HEX BYTES  (gap)  |ASCII|
        // ADDRESS: 8 chars
        // gap: 2 chars
        // HEX: bytesPerLine * 3 - 1 chars (each byte "XX " except last)
        // gap: 2 chars
        // ASCII: 1 + bytesPerLine + 1 chars (|...|)
        _addressX = 0;
        _hexX = (8 + 2) * _charWidth;
        _asciiX = (8 + 2 + _bytesPerLine * 3 - 1 + 2) * _charWidth;
    }

    private int GetVisibleLineCount()
    {
        if (_lineHeight <= 0) return 1;
        return Math.Max(1, _panel.ClientSize.Height / _lineHeight);
    }

    private int GetTotalLineCount()
    {
        if (_data == null || _data.Length == 0) return 0;
        return _bannerLineCount + (_data.Length + _bytesPerLine - 1) / _bytesPerLine;
    }

    private void RecalculateScrollParameters()
    {
        // Calculate banner lines
        _bannerLineCount = 0;
        if (_data != null)
        {
            if (_blockViewMode)
                _bannerLineCount = 2;
        }

        int totalLines = GetTotalLineCount();
        int visibleLines = GetVisibleLineCount();

        if (totalLines <= visibleLines)
        {
            _scrollBar.Enabled = false;
            _scrollBar.Maximum = 0;
            _scrollBar.Value = 0;
        }
        else
        {
            _scrollBar.Enabled = true;
            _scrollBar.LargeChange = visibleLines;
            _scrollBar.SmallChange = 1;
            // VScrollBar quirk: actual scrollable range is Maximum - LargeChange + 1
            _scrollBar.Maximum = totalLines - 1 + (visibleLines - 1);

            // Clamp current value
            int maxScroll = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
            if (_scrollBar.Value > maxScroll)
                _scrollBar.Value = Math.Max(0, maxScroll);
        }
    }

    private void OnPanelPaint(object? sender, PaintEventArgs e)
    {
        if (_data == null || _data.Length == 0) return;

        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int firstVisibleLine = _scrollBar.Value;
        int visibleLines = GetVisibleLineCount();
        int totalLines = GetTotalLineCount();
        int lastVisibleLine = Math.Min(firstVisibleLine + visibleLines, totalLines);

        var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        for (int lineIdx = firstVisibleLine; lineIdx < lastVisibleLine; lineIdx++)
        {
            int y = (lineIdx - firstVisibleLine) * _lineHeight;

            // Banner lines
            if (lineIdx < _bannerLineCount)
            {
                if (lineIdx == 0)
                {
                    string banner = GetBannerText();
                    TextRenderer.DrawText(g, banner, _font, new Point(0, y), AddressColor, flags);
                }
                // lineIdx == 1 is blank separator
                continue;
            }

            int dataLine = lineIdx - _bannerLineCount;
            int offset = dataLine * _bytesPerLine;
            int count = Math.Min(_bytesPerLine, _data.Length - offset);

            // Address
            long displayAddr = _blockViewMode ? _blockStartOffset + offset : offset;
            TextRenderer.DrawText(g, displayAddr.ToString("X8"), _font,
                new Point(_addressX, y), AddressColor, flags);

            // Hex bytes with run-batching
            DrawHexBytes(g, offset, count, y, flags);

            // ASCII with run-batching
            DrawAscii(g, offset, count, y, flags);
        }
    }

    private string GetBannerText()
    {
        if (_blockViewMode)
        {
            return $"[Block view: offset 0x{_blockStartOffset:X8}, {_data!.Length} bytes]";
        }

        return string.Empty;
    }

    private void DrawHexBytes(Graphics g, int offset, int count, int y, TextFormatFlags flags)
    {
        int i = 0;
        while (i < _bytesPerLine)
        {
            if (i < count)
            {
                int byteIndex = offset + i;
                bool isDiff = IsByteDifferent(byteIndex);
                bool isSelected = IsByteSelected(byteIndex);

                // Find run of consecutive bytes with same highlight state
                int runEnd = i + 1;
                while (runEnd < count && runEnd < _bytesPerLine)
                {
                    int nextIndex = offset + runEnd;
                    if (IsByteDifferent(nextIndex) != isDiff || IsByteSelected(nextIndex) != isSelected)
                        break;
                    runEnd++;
                }

                // Build the hex string for this run
                _paintBuffer.Clear();
                for (int j = i; j < runEnd; j++)
                {
                    _paintBuffer.Append(HexLookup[_data![offset + j]]);
                    if (j < _bytesPerLine - 1)
                        _paintBuffer.Append(' ');
                }

                int x = _hexX + i * 3 * _charWidth;
                string text = _paintBuffer.ToString();

                // Draw background rect if highlighted
                if (isSelected || isDiff)
                {
                    var brush = isSelected ? SelectionBrush : DiffBrush;
                    Size textSize = TextRenderer.MeasureText(g, text, _font, new Size(int.MaxValue, int.MaxValue), flags);
                    g.FillRectangle(brush, x, y, textSize.Width, _lineHeight);
                }

                Color fg = isSelected ? Color.White : HexColor;
                TextRenderer.DrawText(g, text, _font, new Point(x, y), fg, flags);

                i = runEnd;
            }
            else
            {
                // Padding for incomplete lines — skip, just leave blank
                i++;
            }
        }

        // Draw caret outline in hex column
        if (_caretIndex >= 0 && _caretIndex >= offset && _caretIndex < offset + count)
        {
            int caretCol = _caretIndex - offset;
            int cx = _hexX + caretCol * 3 * _charWidth;
            int cw = 2 * _charWidth; // "XX" width (no trailing space)
            DrawCaretOutline(g, cx, y, cw, _lineHeight);
        }
    }

    private void DrawAscii(Graphics g, int offset, int count, int y, TextFormatFlags flags)
    {
        // Draw leading pipe
        TextRenderer.DrawText(g, "|", _font, new Point(_asciiX, y), AsciiColor, flags);

        int pipeWidth = _charWidth;
        int i = 0;
        while (i < _bytesPerLine)
        {
            if (i < count)
            {
                int byteIndex = offset + i;
                bool isDiff = IsByteDifferent(byteIndex);
                bool isSelected = IsByteSelected(byteIndex);

                int runEnd = i + 1;
                while (runEnd < count && runEnd < _bytesPerLine)
                {
                    int nextIndex = offset + runEnd;
                    if (IsByteDifferent(nextIndex) != isDiff || IsByteSelected(nextIndex) != isSelected)
                        break;
                    runEnd++;
                }

                _paintBuffer.Clear();
                for (int j = i; j < runEnd; j++)
                {
                    byte b = _data![offset + j];
                    _paintBuffer.Append((b >= 32 && b < 127) ? (char)b : '.');
                }

                int x = _asciiX + pipeWidth + i * _charWidth;
                string text = _paintBuffer.ToString();

                if (isSelected || isDiff)
                {
                    var brush = isSelected ? SelectionBrush : DiffBrush;
                    Size textSize = TextRenderer.MeasureText(g, text, _font, new Size(int.MaxValue, int.MaxValue), flags);
                    g.FillRectangle(brush, x, y, textSize.Width, _lineHeight);
                }

                Color fg = isSelected ? Color.White : AsciiColor;
                TextRenderer.DrawText(g, text, _font, new Point(x, y), fg, flags);

                i = runEnd;
            }
            else
            {
                // Padding space for incomplete lines
                i++;
            }
        }

        // Draw caret outline in ASCII column
        if (_caretIndex >= 0 && _caretIndex >= offset && _caretIndex < offset + count)
        {
            int caretCol = _caretIndex - offset;
            int cx = _asciiX + pipeWidth + caretCol * _charWidth;
            DrawCaretOutline(g, cx, y, _charWidth, _lineHeight);
        }

        // Draw trailing pipe at fixed position (end of full line width)
        int closePipeX = _asciiX + pipeWidth + _bytesPerLine * _charWidth;
        TextRenderer.DrawText(g, "|", _font, new Point(closePipeX, y), AsciiColor, flags);
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

    private static void DrawCaretOutline(Graphics g, int x, int y, int width, int height)
    {
        using var pen = new Pen(Color.Black, 1);
        g.DrawRectangle(pen, x, y, width - 1, height - 1);
    }

    private static string[] BuildHexLookup()
    {
        var table = new string[256];
        for (int i = 0; i < 256; i++)
        {
            table[i] = i.ToString("X2");
        }
        return table;
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
