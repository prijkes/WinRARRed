using System;

namespace WinRARRed.IO
{
    /// <summary>
    /// SRR-specific block types (0x69-0x71).
    /// </summary>
    public enum SRRBlockType : byte
    {
        Header = 0x69,          // SRR file header
        StoredFile = 0x6A,      // Stored file block
        OsoHash = 0x6B,         // OSO hash block
        RarPadding = 0x6C,      // RAR padding block
        RarFile = 0x71,         // RAR file reference (followed by RAR headers)
    }

    /// <summary>
    /// SRR header block flags.
    /// </summary>
    [Flags]
    public enum SRRHeaderFlags : ushort
    {
        None = 0x0000,
        AppNamePresent = 0x0001
    }

    /// <summary>
    /// Generic SRR block flags.
    /// </summary>
    [Flags]
    public enum SRRBlockFlags : ushort
    {
        None = 0x0000,
        SkipIfUnknown = 0x4000,
        LongBlock = 0x8000,
    }

    /// <summary>
    /// Base class for SRR blocks.
    /// </summary>
    public class SRRBlock
    {
        public ushort Crc { get; set; }
        public SRRBlockType BlockType { get; set; }
        public ushort Flags { get; set; }
        public ushort HeaderSize { get; set; }
        public long BlockPosition { get; set; }
        public uint AddSize { get; set; }
    }

    /// <summary>
    /// SRR RAR file reference block (0x71).
    /// Contains the RAR filename and is followed by embedded RAR headers.
    /// </summary>
    public class SrrRarFileBlock : SRRBlock
    {
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// SRR stored file block (0x6A).
    /// Contains a file embedded within the SRR.
    /// </summary>
    public class SrrStoredFileBlock : SRRBlock
    {
        public string FileName { get; set; } = string.Empty;
        public uint FileLength { get; set; }
        public long DataOffset { get; set; }
    }
}
