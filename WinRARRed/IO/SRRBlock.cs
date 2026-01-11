using System;

namespace WinRARRed.IO
{
    public enum SRRBlockType : byte
    {
        SrrHeader = 0x69,
        SrrStoredFile = 0x6A,
        SrrOsoHash = 0x6B,
        SrrRarPadding = 0x6C,
        SrrRarFile = 0x71,
        RarMarker = 0x72,
        RarArchiveHeader = 0x73,
        RarFile = 0x74,
        RarArchiveEnd = 0x7B
    }

    [Flags]
    public enum SRRHeaderFlags : ushort
    {
        AppNamePresent = 0x0001
    }
    
    [Flags]
    public enum SRRBlockFlags : ushort
    {
        LongBlock = 0x8000,
        SkipIfUnknown = 0x4000
    }

    public class SRRBlock
    {
        public ushort Crc { get; set; }
        public SRRBlockType RawType { get; set; }
        public ushort Flags { get; set; }
        public ushort HeaderSize { get; set; }
        public long BlockPosition { get; set; }
        public byte[] RawData { get; set; } = [];

        // For Long Blocks
        public uint AddSize { get; set; }
    }

    public class SrrRarFileBlock : SRRBlock
    {
        public string FileName { get; set; } = string.Empty;
    }

    public class SrrStoredFileBlock : SRRBlock
    {
        public string FileName { get; set; } = string.Empty;
        public uint FileLength { get; set; }
        public long DataOffset { get; set; }
    }
}
