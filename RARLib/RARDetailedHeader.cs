using System.Text;

namespace RARLib;

/// <summary>
/// Represents a single field within a RAR header, with its offset and raw/formatted values.
/// </summary>
public class RARHeaderField
{
    /// <summary>Field name (e.g., "Header CRC", "Flags", "Packed Size").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Byte offset from the start of the file.</summary>
    public long Offset { get; set; }

    /// <summary>Length in bytes.</summary>
    public int Length { get; set; }

    /// <summary>Raw bytes of this field.</summary>
    public byte[] RawBytes { get; set; } = [];

    /// <summary>Formatted display value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Additional description or decoded meaning.</summary>
    public string? Description { get; set; }

    /// <summary>Child fields (for nested structures like flags).</summary>
    public List<RARHeaderField> Children { get; set; } = [];

    public override string ToString() => $"{Name}: {Value}";
}

/// <summary>
/// Represents a complete RAR header block with all its fields parsed in detail.
/// </summary>
public class RARDetailedBlock
{
    /// <summary>Block type name.</summary>
    public string BlockType { get; set; } = string.Empty;

    /// <summary>Block type value.</summary>
    public byte BlockTypeValue { get; set; }

    /// <summary>Start offset of this block.</summary>
    public long StartOffset { get; set; }

    /// <summary>Total block size (header + data).</summary>
    public long TotalSize { get; set; }

    /// <summary>Header size only.</summary>
    public int HeaderSize { get; set; }

    /// <summary>All fields in this block header.</summary>
    public List<RARHeaderField> Fields { get; set; } = [];

    /// <summary>True if this block has associated data after the header.</summary>
    public bool HasData { get; set; }

    /// <summary>Size of data after header.</summary>
    public long DataSize { get; set; }

    /// <summary>For file/service blocks: the item name.</summary>
    public string? ItemName { get; set; }
}

/// <summary>
/// Parses RAR files and extracts detailed header information with byte offsets.
/// </summary>
public class RARDetailedParser
{
    /// <summary>
    /// Parses a RAR file and returns all header blocks with detailed field information.
    /// </summary>
    public static List<RARDetailedBlock> Parse(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        return Parse(fs);
    }

    /// <summary>
    /// Parses a RAR stream and returns all header blocks with detailed field information.
    /// </summary>
    public static List<RARDetailedBlock> Parse(Stream stream)
    {
        var blocks = new List<RARDetailedBlock>();

        // Check RAR version
        bool isRAR5 = RAR5HeaderReader.IsRAR5(stream);
        stream.Position = 0;

        if (isRAR5)
        {
            ParseRAR5(stream, blocks);
        }
        else
        {
            ParseRAR4(stream, blocks);
        }

        return blocks;
    }

    #region RAR 4.x Parsing

    private static void ParseRAR4(Stream stream, List<RARDetailedBlock> blocks)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Parse signature
        if (stream.Length >= 7)
        {
            var sigBlock = new RARDetailedBlock
            {
                BlockType = "Signature",
                BlockTypeValue = 0,
                StartOffset = 0,
                TotalSize = 7,
                HeaderSize = 7
            };

            byte[] sig = reader.ReadBytes(7);
            sigBlock.Fields.Add(new RARHeaderField
            {
                Name = "Signature",
                Offset = 0,
                Length = 7,
                RawBytes = sig,
                Value = BitConverter.ToString(sig).Replace("-", " "),
                Description = IsValidRAR4Signature(sig) ? "Valid RAR 4.x signature" : "Invalid signature"
            });

            blocks.Add(sigBlock);
        }

        // Parse remaining blocks
        int maxBlocks = 100000; // Safety limit
        int blockCount = 0;
        while (stream.Position + 7 <= stream.Length && blockCount < maxBlocks)
        {
            long blockStart = stream.Position;

            try
            {
                var block = ParseRAR4Block(reader, stream);
                if (block == null) break;
                blocks.Add(block);
                blockCount++;

                // Calculate next block position: header start + header size + data size
                long nextPos = block.StartOffset + block.TotalSize;

                // Safety check: ensure we're making forward progress
                if (nextPos <= blockStart)
                    break;

                // Check if next position is within file bounds
                if (nextPos > stream.Length)
                    break;

                // Check for end of archive block
                if (block.BlockTypeValue == 0x7B)
                    break;

                stream.Position = nextPos;
            }
            catch
            {
                // Skip to try finding more blocks - move forward by minimum header size
                stream.Position = blockStart + 7;
                if (stream.Position > stream.Length - 7)
                    break;
            }
        }
    }

    private static bool IsValidRAR4Signature(byte[] sig)
    {
        if (sig.Length < 7) return false;
        return sig[0] == 0x52 && sig[1] == 0x61 && sig[2] == 0x72 &&
               sig[3] == 0x21 && sig[4] == 0x1A && sig[5] == 0x07 && sig[6] == 0x00;
    }

    private static RARDetailedBlock? ParseRAR4Block(BinaryReader reader, Stream stream)
    {
        long blockStart = stream.Position;

        if (blockStart + 7 > stream.Length)
            return null;

        var block = new RARDetailedBlock { StartOffset = blockStart };

        // Ensure we're at the right position (in case stream was repositioned)
        if (reader.BaseStream.Position != blockStart)
        {
            reader.BaseStream.Position = blockStart;
        }

        // Read base header
        long pos = blockStart;

        // HEAD_CRC (2 bytes)
        ushort headCrc = reader.ReadUInt16();
        block.Fields.Add(new RARHeaderField
        {
            Name = "Header CRC",
            Offset = pos,
            Length = 2,
            RawBytes = BitConverter.GetBytes(headCrc),
            Value = $"0x{headCrc:X4}"
        });
        pos += 2;

        // HEAD_TYPE (1 byte)
        byte headType = reader.ReadByte();
        block.BlockTypeValue = headType;
        block.BlockType = GetRAR4BlockTypeName(headType);
        block.Fields.Add(new RARHeaderField
        {
            Name = "Block Type",
            Offset = pos,
            Length = 1,
            RawBytes = new[] { headType },
            Value = $"0x{headType:X2}",
            Description = block.BlockType
        });
        pos += 1;

        // HEAD_FLAGS (2 bytes)
        ushort headFlags = reader.ReadUInt16();
        var flagsField = new RARHeaderField
        {
            Name = "Flags",
            Offset = pos,
            Length = 2,
            RawBytes = BitConverter.GetBytes(headFlags),
            Value = $"0x{headFlags:X4}"
        };
        AddRAR4FlagDescriptions(flagsField, headType, headFlags);
        block.Fields.Add(flagsField);
        pos += 2;

        // HEAD_SIZE (2 bytes)
        ushort headSize = reader.ReadUInt16();
        block.HeaderSize = headSize;
        block.Fields.Add(new RARHeaderField
        {
            Name = "Header Size",
            Offset = pos,
            Length = 2,
            RawBytes = BitConverter.GetBytes(headSize),
            Value = $"{headSize} bytes"
        });
        pos += 2;

        if (headSize < 7)
        {
            block.TotalSize = 7;
            return block;
        }

        block.TotalSize = headSize;

        // File headers (0x74), service blocks (0x7A), and any block with LONG_BLOCK flag have ADD_SIZE
        bool hasAddSize = (headFlags & 0x8000) != 0 ||
                          headType == 0x74 || headType == 0x7A;

        // ADD_SIZE (4 bytes) for file headers and blocks with LONG_BLOCK flag
        // Note: For file/service blocks, ADD_SIZE always exists and comes after the base 7-byte header
        uint addSize = 0;
        if (hasAddSize && stream.Position + 4 <= stream.Length)
        {
            // Read ADD_SIZE - it's packed data size that follows the header
            addSize = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Data Size (ADD_SIZE)",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(addSize),
                Value = $"{addSize} bytes"
            });
            pos += 4;
            block.DataSize = addSize;
            block.HasData = addSize > 0;
            block.TotalSize = headSize + addSize;
        }

        // Parse type-specific fields
        switch (headType)
        {
            case 0x73: // Archive header
                ParseRAR4ArchiveHeader(reader, stream, block, pos, blockStart + headSize, headFlags);
                break;
            case 0x74: // File header
                ParseRAR4FileHeader(reader, stream, block, pos, blockStart + headSize, headFlags, addSize);
                break;
            case 0x7A: // Service block (CMT, RR, etc.)
                ParseRAR4ServiceBlock(reader, stream, block, pos, blockStart + headSize, headFlags, addSize);
                break;
            case 0x7B: // End of archive
                ParseRAR4EndBlock(reader, stream, block, pos, blockStart + headSize, headFlags);
                break;
        }

        return block;
    }

    private static string GetRAR4BlockTypeName(byte type) => type switch
    {
        0x72 => "Marker Block",
        0x73 => "Archive Header",
        0x74 => "File Header",
        0x75 => "Comment (old)",
        0x76 => "Extra Info (old)",
        0x77 => "Subblock (old)",
        0x78 => "Recovery Record (old)",
        0x79 => "Auth Info (old)",
        0x7A => "Service Block",
        0x7B => "End of Archive",
        _ => $"Unknown (0x{type:X2})"
    };

    private static void AddRAR4FlagDescriptions(RARHeaderField flagsField, byte blockType, ushort flags)
    {
        // Common flags
        if ((flags & 0x8000) != 0)
            flagsField.Children.Add(new RARHeaderField { Name = "LONG_BLOCK", Value = "Has ADD_SIZE field" });

        if (blockType == 0x73) // Archive header
        {
            if ((flags & 0x0001) != 0) flagsField.Children.Add(new RARHeaderField { Name = "VOLUME", Value = "Multi-volume archive" });
            if ((flags & 0x0002) != 0) flagsField.Children.Add(new RARHeaderField { Name = "COMMENT", Value = "Archive comment present" });
            if ((flags & 0x0004) != 0) flagsField.Children.Add(new RARHeaderField { Name = "LOCK", Value = "Archive is locked" });
            if ((flags & 0x0008) != 0) flagsField.Children.Add(new RARHeaderField { Name = "SOLID", Value = "Solid archive" });
            if ((flags & 0x0010) != 0) flagsField.Children.Add(new RARHeaderField { Name = "NEW_NUMBERING", Value = "New volume naming scheme" });
            if ((flags & 0x0020) != 0) flagsField.Children.Add(new RARHeaderField { Name = "AV", Value = "Authenticity verification present" });
            if ((flags & 0x0040) != 0) flagsField.Children.Add(new RARHeaderField { Name = "PROTECT", Value = "Recovery record present" });
            if ((flags & 0x0080) != 0) flagsField.Children.Add(new RARHeaderField { Name = "PASSWORD", Value = "Headers are encrypted" });
            if ((flags & 0x0100) != 0) flagsField.Children.Add(new RARHeaderField { Name = "FIRST_VOLUME", Value = "First volume" });
        }
        else if (blockType == 0x74 || blockType == 0x7A) // File header or service block
        {
            if ((flags & 0x0001) != 0) flagsField.Children.Add(new RARHeaderField { Name = "SPLIT_BEFORE", Value = "File continued from previous volume" });
            if ((flags & 0x0002) != 0) flagsField.Children.Add(new RARHeaderField { Name = "SPLIT_AFTER", Value = "File continues in next volume" });
            if ((flags & 0x0004) != 0) flagsField.Children.Add(new RARHeaderField { Name = "PASSWORD", Value = "File is encrypted" });
            if ((flags & 0x0008) != 0) flagsField.Children.Add(new RARHeaderField { Name = "COMMENT", Value = "File comment present" });
            if ((flags & 0x0010) != 0) flagsField.Children.Add(new RARHeaderField { Name = "SOLID", Value = "Info from previous files used" });

            int dictBits = (flags >> 5) & 0x7;
            string dictSize = dictBits switch
            {
                0 => "64 KB",
                1 => "128 KB",
                2 => "256 KB",
                3 => "512 KB",
                4 => "1024 KB",
                5 => "2048 KB",
                6 => "4096 KB",
                7 => "Directory",
                _ => "Unknown"
            };
            flagsField.Children.Add(new RARHeaderField { Name = "DICT_SIZE", Value = dictSize });

            if ((flags & 0x0100) != 0) flagsField.Children.Add(new RARHeaderField { Name = "LARGE", Value = "64-bit sizes" });
            if ((flags & 0x0200) != 0) flagsField.Children.Add(new RARHeaderField { Name = "UNICODE", Value = "Unicode filename" });
            if ((flags & 0x0400) != 0) flagsField.Children.Add(new RARHeaderField { Name = "SALT", Value = "Salt present" });
            if ((flags & 0x0800) != 0) flagsField.Children.Add(new RARHeaderField { Name = "VERSION", Value = "File version present" });
            if ((flags & 0x1000) != 0) flagsField.Children.Add(new RARHeaderField { Name = "EXTTIME", Value = "Extended time present" });
        }
        else if (blockType == 0x7B) // End of archive
        {
            if ((flags & 0x0001) != 0) flagsField.Children.Add(new RARHeaderField { Name = "NEXT_VOLUME", Value = "Archive continues in next volume" });
            if ((flags & 0x0002) != 0) flagsField.Children.Add(new RARHeaderField { Name = "DATA_CRC", Value = "Data CRC present" });
            if ((flags & 0x0004) != 0) flagsField.Children.Add(new RARHeaderField { Name = "REV_SPACE", Value = "Reserved space present" });
        }
    }

    private static void ParseRAR4ArchiveHeader(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ushort flags)
    {
        // Reserved1 (2 bytes) - typically 0
        if (pos + 2 <= headerEnd)
        {
            ushort reserved1 = reader.ReadUInt16();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Reserved1",
                Offset = pos,
                Length = 2,
                RawBytes = BitConverter.GetBytes(reserved1),
                Value = $"0x{reserved1:X4}"
            });
            pos += 2;
        }

        // Reserved2 (4 bytes) - typically 0
        if (pos + 4 <= headerEnd)
        {
            uint reserved2 = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Reserved2",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(reserved2),
                Value = $"0x{reserved2:X8}"
            });
            pos += 4;
        }
    }

    private static void ParseRAR4FileHeader(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ushort flags, uint packSize)
    {
        // UNP_SIZE (4 bytes)
        if (pos + 4 <= headerEnd)
        {
            uint unpSize = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Unpacked Size",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(unpSize),
                Value = $"{unpSize:N0} bytes"
            });
            pos += 4;
        }

        // HOST_OS (1 byte)
        if (pos + 1 <= headerEnd)
        {
            byte hostOs = reader.ReadByte();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Host OS",
                Offset = pos,
                Length = 1,
                RawBytes = new[] { hostOs },
                Value = $"0x{hostOs:X2}",
                Description = GetHostOSName(hostOs)
            });
            pos += 1;
        }

        // FILE_CRC (4 bytes)
        if (pos + 4 <= headerEnd)
        {
            uint fileCrc = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "File CRC32",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(fileCrc),
                Value = $"0x{fileCrc:X8}"
            });
            pos += 4;
        }

        // FTIME (4 bytes) - DOS format
        if (pos + 4 <= headerEnd)
        {
            uint ftime = reader.ReadUInt32();
            var dt = RARUtils.DosDateToDateTime(ftime);
            block.Fields.Add(new RARHeaderField
            {
                Name = "File Time (DOS)",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(ftime),
                Value = $"0x{ftime:X8}",
                Description = dt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Invalid"
            });
            pos += 4;
        }

        // UNP_VER (1 byte)
        if (pos + 1 <= headerEnd)
        {
            byte unpVer = reader.ReadByte();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Unpack Version",
                Offset = pos,
                Length = 1,
                RawBytes = new[] { unpVer },
                Value = $"{unpVer}",
                Description = $"RAR {unpVer / 10}.{unpVer % 10}"
            });
            pos += 1;
        }

        // METHOD (1 byte)
        if (pos + 1 <= headerEnd)
        {
            byte method = reader.ReadByte();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Compression Method",
                Offset = pos,
                Length = 1,
                RawBytes = new[] { method },
                Value = $"0x{method:X2}",
                Description = GetCompressionMethodName(method)
            });
            pos += 1;
        }

        // NAME_SIZE (2 bytes)
        ushort nameSize = 0;
        if (pos + 2 <= headerEnd)
        {
            nameSize = reader.ReadUInt16();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Name Size",
                Offset = pos,
                Length = 2,
                RawBytes = BitConverter.GetBytes(nameSize),
                Value = $"{nameSize} bytes"
            });
            pos += 2;
        }

        // ATTR (4 bytes)
        if (pos + 4 <= headerEnd)
        {
            uint attr = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "File Attributes",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(attr),
                Value = $"0x{attr:X8}"
            });
            pos += 4;
        }

        // HIGH_PACK_SIZE (4 bytes) - if LARGE flag set
        if ((flags & 0x0100) != 0 && pos + 4 <= headerEnd)
        {
            uint highPack = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "High Pack Size",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(highPack),
                Value = $"0x{highPack:X8}"
            });
            pos += 4;
        }

        // HIGH_UNP_SIZE (4 bytes) - if LARGE flag set
        if ((flags & 0x0100) != 0 && pos + 4 <= headerEnd)
        {
            uint highUnp = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "High Unpack Size",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(highUnp),
                Value = $"0x{highUnp:X8}"
            });
            pos += 4;
        }

        // FILE_NAME (variable)
        if (nameSize > 0 && pos + nameSize <= headerEnd)
        {
            byte[] nameBytes = reader.ReadBytes(nameSize);
            string fileName = RARUtils.DecodeFileName(nameBytes, (flags & 0x0200) != 0) ?? "";
            block.ItemName = fileName;
            block.Fields.Add(new RARHeaderField
            {
                Name = "File Name",
                Offset = pos,
                Length = nameSize,
                RawBytes = nameBytes,
                Value = fileName,
                Description = (flags & 0x0200) != 0 ? "Unicode encoded" : "OEM encoded"
            });
            pos += nameSize;
        }

        // SALT (8 bytes) - if SALT flag set
        if ((flags & 0x0400) != 0 && pos + 8 <= headerEnd)
        {
            byte[] salt = reader.ReadBytes(8);
            block.Fields.Add(new RARHeaderField
            {
                Name = "Salt",
                Offset = pos,
                Length = 8,
                RawBytes = salt,
                Value = BitConverter.ToString(salt).Replace("-", " ")
            });
            pos += 8;
        }

        // EXT_TIME (variable) - if EXTTIME flag set
        if ((flags & 0x1000) != 0 && pos + 2 <= headerEnd)
        {
            long extTimeStart = pos;
            ushort extFlags = reader.ReadUInt16();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Extended Time Flags",
                Offset = pos,
                Length = 2,
                RawBytes = BitConverter.GetBytes(extFlags),
                Value = $"0x{extFlags:X4}"
            });
            pos += 2;

            // Parse each time field (mtime, ctime, atime, arctime)
            string[] timeNames = { "mtime", "ctime", "atime", "arctime" };
            for (int i = 0; i < 4 && pos < headerEnd; i++)
            {
                int rmode = (extFlags >> ((3 - i) * 4)) & 0xF;
                if ((rmode & 0x8) == 0) continue;

                // Time present
                if (i != 0 && pos + 4 <= headerEnd)
                {
                    uint dosTime = reader.ReadUInt32();
                    block.Fields.Add(new RARHeaderField
                    {
                        Name = $"Ext {timeNames[i]} DOS",
                        Offset = pos,
                        Length = 4,
                        RawBytes = BitConverter.GetBytes(dosTime),
                        Value = $"0x{dosTime:X8}"
                    });
                    pos += 4;
                }

                int count = rmode & 0x3;
                if (count > 0 && pos + count <= headerEnd)
                {
                    byte[] remainder = reader.ReadBytes(count);
                    block.Fields.Add(new RARHeaderField
                    {
                        Name = $"Ext {timeNames[i]} subsec",
                        Offset = pos,
                        Length = count,
                        RawBytes = remainder,
                        Value = BitConverter.ToString(remainder).Replace("-", " ")
                    });
                    pos += count;
                }
            }
        }
    }

    private static void ParseRAR4ServiceBlock(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ushort flags, uint packSize)
    {
        // Service blocks have same structure as file headers
        ParseRAR4FileHeader(reader, stream, block, pos, headerEnd, flags, packSize);
    }

    private static void ParseRAR4EndBlock(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ushort flags)
    {
        // Archive end flags
        if ((flags & 0x0002) != 0 && pos + 4 <= headerEnd)
        {
            uint dataCrc = reader.ReadUInt32();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Archive Data CRC",
                Offset = pos,
                Length = 4,
                RawBytes = BitConverter.GetBytes(dataCrc),
                Value = $"0x{dataCrc:X8}"
            });
            pos += 4;
        }

        if ((flags & 0x0004) != 0 && pos + 2 <= headerEnd)
        {
            ushort volNumber = reader.ReadUInt16();
            block.Fields.Add(new RARHeaderField
            {
                Name = "Volume Number",
                Offset = pos,
                Length = 2,
                RawBytes = BitConverter.GetBytes(volNumber),
                Value = volNumber.ToString()
            });
            pos += 2;
        }
    }

    #endregion

    #region RAR 5.x Parsing

    private static void ParseRAR5(Stream stream, List<RARDetailedBlock> blocks)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Parse signature (8 bytes)
        if (stream.Length >= 8)
        {
            var sigBlock = new RARDetailedBlock
            {
                BlockType = "Signature",
                BlockTypeValue = 0,
                StartOffset = 0,
                TotalSize = 8,
                HeaderSize = 8
            };

            byte[] sig = reader.ReadBytes(8);
            sigBlock.Fields.Add(new RARHeaderField
            {
                Name = "Signature",
                Offset = 0,
                Length = 8,
                RawBytes = sig,
                Value = BitConverter.ToString(sig).Replace("-", " "),
                Description = "RAR 5.x signature"
            });

            blocks.Add(sigBlock);
        }

        // Parse remaining blocks
        while (stream.Position < stream.Length)
        {
            var block = ParseRAR5Block(reader, stream);
            if (block == null) break;
            blocks.Add(block);

            // Skip to next block
            long nextPos = block.StartOffset + block.TotalSize;
            if (nextPos <= stream.Position || nextPos > stream.Length)
                break;
            stream.Position = nextPos;
        }
    }

    private static RARDetailedBlock? ParseRAR5Block(BinaryReader reader, Stream stream)
    {
        long blockStart = stream.Position;

        if (stream.Position + 4 > stream.Length)
            return null;

        var block = new RARDetailedBlock { StartOffset = blockStart };
        long pos = blockStart;

        // HEAD_CRC (4 bytes)
        uint headCrc = reader.ReadUInt32();
        block.Fields.Add(new RARHeaderField
        {
            Name = "Header CRC32",
            Offset = pos,
            Length = 4,
            RawBytes = BitConverter.GetBytes(headCrc),
            Value = $"0x{headCrc:X8}"
        });
        pos += 4;

        // HEAD_SIZE (vint)
        long vintStart = pos;
        ulong headSize = ReadVInt(reader, stream);
        int vintLen = (int)(stream.Position - vintStart);
        block.HeaderSize = (int)headSize + 4 + vintLen; // CRC + vint + header data
        block.Fields.Add(new RARHeaderField
        {
            Name = "Header Size",
            Offset = vintStart,
            Length = vintLen,
            Value = $"{headSize} bytes (vint)"
        });
        pos = stream.Position;

        if (headSize == 0)
        {
            block.TotalSize = pos - blockStart;
            return block;
        }

        // HEAD_TYPE (vint)
        vintStart = pos;
        ulong headType = ReadVInt(reader, stream);
        vintLen = (int)(stream.Position - vintStart);
        block.BlockTypeValue = (byte)headType;
        block.BlockType = GetRAR5BlockTypeName((int)headType);
        block.Fields.Add(new RARHeaderField
        {
            Name = "Header Type",
            Offset = vintStart,
            Length = vintLen,
            Value = $"{headType}",
            Description = block.BlockType
        });
        pos = stream.Position;

        // HEAD_FLAGS (vint)
        vintStart = pos;
        ulong headFlags = ReadVInt(reader, stream);
        vintLen = (int)(stream.Position - vintStart);
        var flagsField = new RARHeaderField
        {
            Name = "Header Flags",
            Offset = vintStart,
            Length = vintLen,
            Value = $"0x{headFlags:X}"
        };
        AddRAR5FlagDescriptions(flagsField, (int)headType, headFlags);
        block.Fields.Add(flagsField);
        pos = stream.Position;

        long headerEnd = blockStart + 4 + (long)headSize + vintLen;
        block.TotalSize = 4 + vintLen + (long)headSize;

        // Extra area size (vint) - if HFL_EXTRA flag
        if ((headFlags & 0x0001) != 0)
        {
            vintStart = pos;
            ulong extraSize = ReadVInt(reader, stream);
            vintLen = (int)(stream.Position - vintStart);
            block.Fields.Add(new RARHeaderField
            {
                Name = "Extra Area Size",
                Offset = vintStart,
                Length = vintLen,
                Value = $"{extraSize} bytes"
            });
            pos = stream.Position;
        }

        // Data size (vint) - if HFL_DATA flag
        if ((headFlags & 0x0002) != 0)
        {
            vintStart = pos;
            ulong dataSize = ReadVInt(reader, stream);
            vintLen = (int)(stream.Position - vintStart);
            block.Fields.Add(new RARHeaderField
            {
                Name = "Data Size",
                Offset = vintStart,
                Length = vintLen,
                Value = $"{dataSize} bytes"
            });
            block.DataSize = (long)dataSize;
            block.HasData = dataSize > 0;
            block.TotalSize += (long)dataSize;
            pos = stream.Position;
        }

        // Parse type-specific fields
        switch ((int)headType)
        {
            case 1: // Main archive header
                ParseRAR5MainHeader(reader, stream, block, pos, headerEnd, headFlags);
                break;
            case 2: // File header
            case 3: // Service header
                ParseRAR5FileHeader(reader, stream, block, pos, headerEnd, headFlags);
                break;
            case 4: // Encryption header
                ParseRAR5EncryptionHeader(reader, stream, block, pos, headerEnd);
                break;
            case 5: // End of archive
                ParseRAR5EndHeader(reader, stream, block, pos, headerEnd, headFlags);
                break;
        }

        return block;
    }

    private static string GetRAR5BlockTypeName(int type) => type switch
    {
        1 => "Main Archive Header",
        2 => "File Header",
        3 => "Service Header",
        4 => "Encryption Header",
        5 => "End of Archive",
        _ => $"Unknown ({type})"
    };

    private static void AddRAR5FlagDescriptions(RARHeaderField flagsField, int blockType, ulong flags)
    {
        if ((flags & 0x0001) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_EXTRA", Value = "Extra area present" });
        if ((flags & 0x0002) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_DATA", Value = "Data area present" });
        if ((flags & 0x0004) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_SKIPIFUNKNOWN", Value = "Skip if unknown" });
        if ((flags & 0x0008) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_SPLITBEFORE", Value = "Split before" });
        if ((flags & 0x0010) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_SPLITAFTER", Value = "Split after" });
        if ((flags & 0x0020) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_CHILD", Value = "Child block" });
        if ((flags & 0x0040) != 0) flagsField.Children.Add(new RARHeaderField { Name = "HFL_INHERITED", Value = "Inherited" });
    }

    private static void ParseRAR5MainHeader(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ulong headFlags)
    {
        // Archive flags (vint)
        if (pos < headerEnd)
        {
            long vintStart = pos;
            ulong archFlags = ReadVInt(reader, stream);
            int vintLen = (int)(stream.Position - vintStart);

            var archFlagsField = new RARHeaderField
            {
                Name = "Archive Flags",
                Offset = vintStart,
                Length = vintLen,
                Value = $"0x{archFlags:X}"
            };

            if ((archFlags & 0x0001) != 0) archFlagsField.Children.Add(new RARHeaderField { Name = "VOLUME", Value = "Multi-volume" });
            if ((archFlags & 0x0002) != 0) archFlagsField.Children.Add(new RARHeaderField { Name = "VOLNUMBER", Value = "Volume number present" });
            if ((archFlags & 0x0004) != 0) archFlagsField.Children.Add(new RARHeaderField { Name = "SOLID", Value = "Solid archive" });
            if ((archFlags & 0x0008) != 0) archFlagsField.Children.Add(new RARHeaderField { Name = "PROTECT", Value = "Recovery record present" });
            if ((archFlags & 0x0010) != 0) archFlagsField.Children.Add(new RARHeaderField { Name = "LOCK", Value = "Locked archive" });

            block.Fields.Add(archFlagsField);
            pos = stream.Position;

            // Volume number (vint) - if VOLNUMBER flag
            if ((archFlags & 0x0002) != 0 && pos < headerEnd)
            {
                vintStart = pos;
                ulong volNum = ReadVInt(reader, stream);
                vintLen = (int)(stream.Position - vintStart);
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Volume Number",
                    Offset = vintStart,
                    Length = vintLen,
                    Value = volNum.ToString()
                });
            }
        }
    }

    private static void ParseRAR5FileHeader(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ulong headFlags)
    {
        // File flags (vint)
        if (pos < headerEnd)
        {
            long vintStart = pos;
            ulong fileFlags = ReadVInt(reader, stream);
            int vintLen = (int)(stream.Position - vintStart);

            var fileFlagsField = new RARHeaderField
            {
                Name = "File Flags",
                Offset = vintStart,
                Length = vintLen,
                Value = $"0x{fileFlags:X}"
            };

            if ((fileFlags & 0x0001) != 0) fileFlagsField.Children.Add(new RARHeaderField { Name = "DIRECTORY", Value = "Directory entry" });
            if ((fileFlags & 0x0002) != 0) fileFlagsField.Children.Add(new RARHeaderField { Name = "UTIME", Value = "Unix time present" });
            if ((fileFlags & 0x0004) != 0) fileFlagsField.Children.Add(new RARHeaderField { Name = "CRC32", Value = "CRC32 present" });
            if ((fileFlags & 0x0008) != 0) fileFlagsField.Children.Add(new RARHeaderField { Name = "UNPSIZE", Value = "Unpacked size unknown" });

            block.Fields.Add(fileFlagsField);
            pos = stream.Position;

            // Unpacked size (vint)
            if (pos < headerEnd)
            {
                vintStart = pos;
                ulong unpSize = ReadVInt(reader, stream);
                vintLen = (int)(stream.Position - vintStart);
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Unpacked Size",
                    Offset = vintStart,
                    Length = vintLen,
                    Value = $"{unpSize:N0} bytes"
                });
                pos = stream.Position;
            }

            // Attributes (vint)
            if (pos < headerEnd)
            {
                vintStart = pos;
                ulong attr = ReadVInt(reader, stream);
                vintLen = (int)(stream.Position - vintStart);
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Attributes",
                    Offset = vintStart,
                    Length = vintLen,
                    Value = $"0x{attr:X}"
                });
                pos = stream.Position;
            }

            // mtime (4 bytes) - if UTIME flag
            if ((fileFlags & 0x0002) != 0 && pos + 4 <= headerEnd)
            {
                uint mtime = reader.ReadUInt32();
                var dt = DateTimeOffset.FromUnixTimeSeconds(mtime).LocalDateTime;
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Modification Time",
                    Offset = pos,
                    Length = 4,
                    RawBytes = BitConverter.GetBytes(mtime),
                    Value = $"{mtime}",
                    Description = dt.ToString("yyyy-MM-dd HH:mm:ss")
                });
                pos += 4;
            }

            // CRC32 (4 bytes) - if CRC32 flag
            if ((fileFlags & 0x0004) != 0 && pos + 4 <= headerEnd)
            {
                uint crc = reader.ReadUInt32();
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Data CRC32",
                    Offset = pos,
                    Length = 4,
                    RawBytes = BitConverter.GetBytes(crc),
                    Value = $"0x{crc:X8}"
                });
                pos += 4;
            }

            // Compression info (vint)
            if (pos < headerEnd)
            {
                vintStart = pos;
                ulong compInfo = ReadVInt(reader, stream);
                vintLen = (int)(stream.Position - vintStart);

                int version = (int)(compInfo & 0x3F);
                bool solid = (compInfo & 0x40) != 0;
                int method = (int)((compInfo >> 7) & 0x7);
                int dictSizeLog = (int)((compInfo >> 10) & 0xF);

                block.Fields.Add(new RARHeaderField
                {
                    Name = "Compression Info",
                    Offset = vintStart,
                    Length = vintLen,
                    Value = $"0x{compInfo:X}",
                    Description = $"Ver={version}, Method={method}, Dict=2^{dictSizeLog + 17}, Solid={solid}"
                });
                pos = stream.Position;
            }

            // Host OS (vint)
            if (pos < headerEnd)
            {
                vintStart = pos;
                ulong hostOs = ReadVInt(reader, stream);
                vintLen = (int)(stream.Position - vintStart);
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Host OS",
                    Offset = vintStart,
                    Length = vintLen,
                    Value = hostOs.ToString(),
                    Description = hostOs == 0 ? "Windows" : "Unix"
                });
                pos = stream.Position;
            }

            // Name length (vint)
            if (pos < headerEnd)
            {
                vintStart = pos;
                ulong nameLen = ReadVInt(reader, stream);
                vintLen = (int)(stream.Position - vintStart);
                block.Fields.Add(new RARHeaderField
                {
                    Name = "Name Length",
                    Offset = vintStart,
                    Length = vintLen,
                    Value = $"{nameLen} bytes"
                });
                pos = stream.Position;

                // Name (UTF-8)
                if (nameLen > 0 && pos + (long)nameLen <= headerEnd)
                {
                    byte[] nameBytes = reader.ReadBytes((int)nameLen);
                    string name = Encoding.UTF8.GetString(nameBytes);
                    block.ItemName = name;
                    block.Fields.Add(new RARHeaderField
                    {
                        Name = "File Name",
                        Offset = pos,
                        Length = (int)nameLen,
                        RawBytes = nameBytes,
                        Value = name,
                        Description = "UTF-8 encoded"
                    });
                }
            }
        }
    }

    private static void ParseRAR5EncryptionHeader(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd)
    {
        // Encryption version (vint)
        if (pos < headerEnd)
        {
            long vintStart = pos;
            ulong encVer = ReadVInt(reader, stream);
            int vintLen = (int)(stream.Position - vintStart);
            block.Fields.Add(new RARHeaderField
            {
                Name = "Encryption Version",
                Offset = vintStart,
                Length = vintLen,
                Value = encVer.ToString()
            });
            pos = stream.Position;
        }

        // Encryption flags (vint)
        if (pos < headerEnd)
        {
            long vintStart = pos;
            ulong encFlags = ReadVInt(reader, stream);
            int vintLen = (int)(stream.Position - vintStart);
            block.Fields.Add(new RARHeaderField
            {
                Name = "Encryption Flags",
                Offset = vintStart,
                Length = vintLen,
                Value = $"0x{encFlags:X}"
            });
            pos = stream.Position;
        }

        // KDF count (1 byte)
        if (pos + 1 <= headerEnd)
        {
            byte kdfCount = reader.ReadByte();
            block.Fields.Add(new RARHeaderField
            {
                Name = "KDF Count",
                Offset = pos,
                Length = 1,
                RawBytes = new[] { kdfCount },
                Value = kdfCount.ToString(),
                Description = $"Iterations = 2^{kdfCount}"
            });
            pos += 1;
        }

        // Salt (16 bytes)
        if (pos + 16 <= headerEnd)
        {
            byte[] salt = reader.ReadBytes(16);
            block.Fields.Add(new RARHeaderField
            {
                Name = "Salt",
                Offset = pos,
                Length = 16,
                RawBytes = salt,
                Value = BitConverter.ToString(salt).Replace("-", " ")
            });
        }
    }

    private static void ParseRAR5EndHeader(BinaryReader reader, Stream stream, RARDetailedBlock block, long pos, long headerEnd, ulong headFlags)
    {
        // End flags (vint)
        if (pos < headerEnd)
        {
            long vintStart = pos;
            ulong endFlags = ReadVInt(reader, stream);
            int vintLen = (int)(stream.Position - vintStart);

            var endFlagsField = new RARHeaderField
            {
                Name = "End Flags",
                Offset = vintStart,
                Length = vintLen,
                Value = $"0x{endFlags:X}"
            };

            if ((endFlags & 0x0001) != 0) endFlagsField.Children.Add(new RARHeaderField { Name = "NEXTVOLUME", Value = "Archive continues" });

            block.Fields.Add(endFlagsField);
        }
    }

    private static ulong ReadVInt(BinaryReader reader, Stream stream)
    {
        ulong result = 0;
        int shift = 0;

        while (stream.Position < stream.Length)
        {
            byte b = reader.ReadByte();
            result |= ((ulong)(b & 0x7F)) << shift;
            if ((b & 0x80) == 0)
                break;
            shift += 7;
            if (shift > 63)
                break;
        }

        return result;
    }

    #endregion

    #region Helpers

    private static string GetHostOSName(byte os) => os switch
    {
        0 => "MS-DOS",
        1 => "OS/2",
        2 => "Windows",
        3 => "Unix",
        4 => "Mac OS",
        5 => "BeOS",
        _ => $"Unknown ({os})"
    };

    private static string GetCompressionMethodName(byte method) => method switch
    {
        0x30 => "Store",
        0x31 => "Fastest",
        0x32 => "Fast",
        0x33 => "Normal",
        0x34 => "Good",
        0x35 => "Best",
        _ when method >= 0 && method <= 5 => new[] { "Store", "Fastest", "Fast", "Normal", "Good", "Best" }[method],
        _ => $"Unknown (0x{method:X2})"
    };

    #endregion
}
