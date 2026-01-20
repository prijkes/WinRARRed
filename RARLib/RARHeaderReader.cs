using System;
using System.IO;

namespace RARLib
{
    /// <summary>
    /// Parsed service sub-block info (0x7A blocks like CMT, RR, etc.)
    /// </summary>
    public class RARServiceBlockInfo
    {
        /// <summary>Sub-block type name (e.g., "CMT", "RR", "AV").</summary>
        public string SubType { get; set; } = string.Empty;

        /// <summary>Packed (compressed) size of data.</summary>
        public ulong PackedSize { get; set; }

        /// <summary>Unpacked size of data.</summary>
        public ulong UnpackedSize { get; set; }

        /// <summary>Compression method (0x30=Store, 0x33=Normal, etc.).</summary>
        public byte CompressionMethod { get; set; }

        /// <summary>Data CRC.</summary>
        public uint DataCrc { get; set; }

        /// <summary>Offset where the data starts (relative to block start).</summary>
        public int DataOffset { get; set; }

        /// <summary>For CMT blocks: the comment text if extracted.</summary>
        public string? CommentText { get; set; }

        /// <summary>For CMT blocks: raw comment data (may be compressed).</summary>
        public byte[]? RawData { get; set; }

        /// <summary>True if comment is stored (uncompressed), false if compressed.</summary>
        public bool IsStored => CompressionMethod == 0x30;
    }

    /// <summary>
    /// Result of reading a RAR block header.
    /// </summary>
    public class RARBlockReadResult
    {
        /// <summary>Block type (RAR 4.x).</summary>
        public RAR4BlockType BlockType { get; set; }

        /// <summary>Raw flags value.</summary>
        public ushort Flags { get; set; }

        /// <summary>Header size in bytes.</summary>
        public ushort HeaderSize { get; set; }

        /// <summary>Additional data size (from LONG_BLOCK or file headers).</summary>
        public uint AddSize { get; set; }

        /// <summary>Position where the block starts.</summary>
        public long BlockPosition { get; set; }

        /// <summary>Header CRC value.</summary>
        public ushort HeaderCrc { get; set; }

        /// <summary>True if header CRC is valid.</summary>
        public bool CrcValid { get; set; }

        /// <summary>Parsed archive header (if BlockType is ArchiveHeader).</summary>
        public RARArchiveHeader? ArchiveHeader { get; set; }

        /// <summary>Parsed file header (if BlockType is FileHeader).</summary>
        public RARFileHeader? FileHeader { get; set; }

        /// <summary>Parsed service block info (if BlockType is Service).</summary>
        public RARServiceBlockInfo? ServiceBlockInfo { get; set; }
    }

    /// <summary>
    /// Reads RAR 4.x headers from a stream.
    /// </summary>
    public class RARHeaderReader
    {
        private readonly BinaryReader _reader;
        private readonly Stream _stream;

        /// <summary>
        /// Creates a new RAR header reader.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        public RARHeaderReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        }

        /// <summary>
        /// Creates a new RAR header reader using an existing BinaryReader.
        /// </summary>
        /// <param name="reader">BinaryReader to use</param>
        public RARHeaderReader(BinaryReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _stream = reader.BaseStream;
        }

        /// <summary>
        /// Checks if there are enough bytes remaining to read a base header.
        /// </summary>
        public bool CanReadBaseHeader => _stream.Position + 7 <= _stream.Length;

        /// <summary>
        /// Peeks at the next block type without advancing the stream position.
        /// </summary>
        /// <returns>Block type byte, or null if not enough data</returns>
        public byte? PeekBlockType()
        {
            if (_stream.Position + 3 > _stream.Length)
            {
                return null;
            }

            long pos = _stream.Position;
            _stream.Seek(2, SeekOrigin.Current); // Skip CRC
            byte type = _reader.ReadByte();
            _stream.Seek(pos, SeekOrigin.Begin);
            return type;
        }

        /// <summary>
        /// Reads a RAR block header and optionally parses its contents.
        /// </summary>
        /// <param name="parseContents">If true, parse archive/file header contents</param>
        /// <returns>Block read result, or null if not enough data</returns>
        public RARBlockReadResult? ReadBlock(bool parseContents = true)
        {
            if (!CanReadBaseHeader)
            {
                return null;
            }

            long blockStart = _stream.Position;

            // Read base header (7 bytes)
            ushort crc = _reader.ReadUInt16();
            byte typeRaw = _reader.ReadByte();
            ushort flags = _reader.ReadUInt16();
            ushort headerSize = _reader.ReadUInt16();

            if (headerSize < 7 || blockStart + headerSize > _stream.Length)
            {
                return null;
            }

            var result = new RARBlockReadResult
            {
                BlockType = (RAR4BlockType)typeRaw,
                Flags = flags,
                HeaderSize = headerSize,
                BlockPosition = blockStart,
                HeaderCrc = crc
            };

            // Validate CRC by reading entire header
            long currentPos = _stream.Position;
            _stream.Seek(blockStart, SeekOrigin.Begin);
            byte[] headerBytes = _reader.ReadBytes(headerSize);
            result.CrcValid = RARUtils.ValidateHeaderCrc(crc, headerBytes);
            _stream.Seek(currentPos, SeekOrigin.Begin);

            // Read ADD_SIZE for file headers and service blocks (always present even without LONG_BLOCK flag)
            bool hasAddSize = (flags & (ushort)RARFileFlags.LongBlock) != 0 ||
                              result.BlockType == RAR4BlockType.FileHeader ||
                              result.BlockType == RAR4BlockType.Service;

            if (hasAddSize)
            {
                if (_stream.Position + 4 > _stream.Length)
                {
                    return null;
                }
                result.AddSize = _reader.ReadUInt32();
            }

            if (parseContents)
            {
                long headerEnd = blockStart + headerSize;

                switch (result.BlockType)
                {
                    case RAR4BlockType.ArchiveHeader:
                        result.ArchiveHeader = ParseArchiveHeader(result, headerEnd);
                        break;
                    case RAR4BlockType.FileHeader:
                        result.FileHeader = ParseFileHeader(result, headerEnd);
                        break;
                    case RAR4BlockType.Service:
                        result.ServiceBlockInfo = ParseServiceBlock(result, headerEnd);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Skips to the end of the current block (header only, not data).
        /// For file blocks in SRR files, data is not present.
        /// </summary>
        /// <param name="block">Block to skip</param>
        /// <param name="includeData">If true, also skip ADD_SIZE bytes (for non-file blocks)</param>
        public void SkipBlock(RARBlockReadResult block, bool includeData = false)
        {
            long target = block.BlockPosition + block.HeaderSize;
            if (includeData && block.BlockType != RAR4BlockType.FileHeader)
            {
                target += block.AddSize;
            }

            if (target <= block.BlockPosition || target > _stream.Length)
            {
                _stream.Seek(_stream.Length, SeekOrigin.Begin);
            }
            else
            {
                _stream.Seek(target, SeekOrigin.Begin);
            }
        }

        private RARArchiveHeader ParseArchiveHeader(RARBlockReadResult block, long headerEnd)
        {
            return new RARArchiveHeader
            {
                BlockPosition = block.BlockPosition,
                HeaderCrc = block.HeaderCrc,
                HeaderSize = block.HeaderSize,
                Flags = (RARArchiveFlags)block.Flags,
                CrcValid = block.CrcValid
            };
        }

        private RARFileHeader? ParseFileHeader(RARBlockReadResult block, long headerEnd)
        {
            const int minFileHeaderSize = 7 + 4 + 4 + 1 + 4 + 4 + 1 + 1 + 2 + 4; // 32 bytes minimum

            if (block.HeaderSize < minFileHeaderSize)
            {
                return null;
            }

            if (_stream.Position + (minFileHeaderSize - 7 - 4) > headerEnd)
            {
                return null;
            }

            var flags = (RARFileFlags)block.Flags;

            // PACK_SIZE is already in AddSize for file headers
            uint packSize = block.AddSize;

            // Read remaining fields
            uint unpSize = _reader.ReadUInt32();
            byte hostOS = _reader.ReadByte();
            uint fileCrc = _reader.ReadUInt32();
            uint fileTime = _reader.ReadUInt32();
            byte unpVer = _reader.ReadByte();

            // Method is stored as ASCII '0'-'6', subtract 0x30 to get 0-6
            byte methodRaw = _reader.ReadByte();
            byte method = (byte)(methodRaw >= 0x30 ? methodRaw - 0x30 : methodRaw);

            // Read filename
            string? fileName = TryReadFileName(headerEnd, flags, out bool isDirectory, out uint fileAttributes);

            // Handle 64-bit sizes if LHD_LARGE is set
            ulong packedSize = packSize;
            ulong unpackedSize = unpSize;

            // Note: HIGH_PACK_SIZE and HIGH_UNP_SIZE are read in TryReadFileName if LHD_LARGE is set

            // Parse timestamps
            DateTime? modifiedTime = RARUtils.DosDateToDateTime(fileTime);
            DateTime? creationTime = null;
            DateTime? accessTime = null;

            // Skip salt if present
            SkipOptionalSalt(headerEnd, flags);

            // Read extended times
            ReadExtendedTimes(headerEnd, flags, fileTime, ref modifiedTime, ref creationTime, ref accessTime);

            return new RARFileHeader
            {
                BlockPosition = block.BlockPosition,
                HeaderCrc = block.HeaderCrc,
                HeaderSize = block.HeaderSize,
                Flags = flags,
                PackedSize = packedSize,
                UnpackedSize = unpackedSize,
                HostOS = hostOS,
                FileCrc = fileCrc,
                UnpackVersion = unpVer,
                CompressionMethod = method,
                DictionarySizeKB = RARUtils.GetDictionarySize(flags),
                FileAttributes = fileAttributes,
                FileName = fileName ?? string.Empty,
                IsDirectory = isDirectory,
                ModifiedTime = modifiedTime,
                CreationTime = creationTime,
                AccessTime = accessTime,
                CrcValid = block.CrcValid
            };
        }

        private string? TryReadFileName(long headerEnd, RARFileFlags flags, out bool isDirectory, out uint fileAttributes)
        {
            isDirectory = RARUtils.IsDirectory(flags);
            fileAttributes = 0;

            if (_stream.Position + 2 + 4 > headerEnd)
            {
                return null;
            }

            ushort nameSize = _reader.ReadUInt16();
            fileAttributes = _reader.ReadUInt32();

            // Skip HIGH_PACK_SIZE and HIGH_UNP_SIZE if LHD_LARGE is set (64-bit sizes)
            if ((flags & RARFileFlags.Large) != 0)
            {
                if (!TrySkipBytes(headerEnd, 8))
                {
                    return null;
                }
            }

            if (nameSize == 0)
            {
                return null;
            }

            if (_stream.Position + nameSize > headerEnd)
            {
                return null;
            }

            byte[] nameBytes = _reader.ReadBytes(nameSize);
            string? name = RARUtils.DecodeFileName(nameBytes, (flags & RARFileFlags.Unicode) != 0);

            // Check for trailing slash indicating directory
            if (!string.IsNullOrEmpty(name) &&
                (name.EndsWith("\\", StringComparison.Ordinal) || name.EndsWith("/", StringComparison.Ordinal)))
            {
                isDirectory = true;
            }

            return name;
        }

        private void SkipOptionalSalt(long headerEnd, RARFileFlags flags)
        {
            if ((flags & RARFileFlags.Salt) == 0)
            {
                return;
            }

            TrySkipBytes(headerEnd, RARFlagMasks.SaltLength);
        }

        private void ReadExtendedTimes(long headerEnd, RARFileFlags flags, uint baseFileTime,
            ref DateTime? modifiedTime, ref DateTime? creationTime, ref DateTime? accessTime)
        {
            if ((flags & RARFileFlags.ExtTime) == 0)
            {
                return;
            }

            if (!TryReadUInt16(headerEnd, out ushort extFlags))
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                int rmode = (extFlags >> ((3 - i) * 4)) & 0xF;
                if ((rmode & 0x8) == 0)
                {
                    continue;
                }

                // mtime uses base DOS time; ctime/atime have their own DOS time
                uint dosTime = baseFileTime;
                if (i != 0 && !TryReadUInt32(headerEnd, out dosTime))
                {
                    return;
                }

                DateTime? time = RARUtils.DosDateToDateTime(dosTime);
                if ((rmode & 0x4) != 0 && time.HasValue)
                {
                    time = time.Value.AddSeconds(1);
                }

                int count = rmode & 0x3;
                if (!TryReadRemainder(headerEnd, count, out int remainder))
                {
                    return;
                }

                // Remainder is in 100ns units, which map directly to DateTime ticks
                if (time.HasValue && remainder != 0)
                {
                    time = time.Value.AddTicks(remainder);
                }

                switch (i)
                {
                    case 0:
                        modifiedTime = time;
                        break;
                    case 1:
                        creationTime = time;
                        break;
                    case 2:
                        accessTime = time;
                        break;
                }
            }
        }

        private bool TryReadRemainder(long headerEnd, int count, out int remainder)
        {
            remainder = 0;
            if (count <= 0)
            {
                return true;
            }

            if (!TryReadBytes(headerEnd, count, out byte[] bytes))
            {
                return false;
            }

            for (int j = 0; j < count; j++)
            {
                remainder |= bytes[j] << ((j + 3 - count) * 8);
            }

            return true;
        }

        private bool TryReadUInt16(long headerEnd, out ushort value)
        {
            value = 0;
            if (_stream.Position + 2 > headerEnd)
            {
                return false;
            }

            value = _reader.ReadUInt16();
            return true;
        }

        private bool TryReadUInt32(long headerEnd, out uint value)
        {
            value = 0;
            if (_stream.Position + 4 > headerEnd)
            {
                return false;
            }

            value = _reader.ReadUInt32();
            return true;
        }

        private bool TryReadBytes(long headerEnd, int count, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (count < 0)
            {
                return false;
            }

            if (_stream.Position + count > headerEnd)
            {
                return false;
            }

            bytes = _reader.ReadBytes(count);
            return bytes.Length == count;
        }

        private bool TrySkipBytes(long headerEnd, int count)
        {
            if (count <= 0)
            {
                return true;
            }

            long target = _stream.Position + count;
            if (target > headerEnd || target < 0)
            {
                return false;
            }

            _stream.Seek(count, SeekOrigin.Current);
            return true;
        }

        /// <summary>
        /// Parses a service sub-block (0x7A) to extract sub-type and data.
        /// </summary>
        private RARServiceBlockInfo? ParseServiceBlock(RARBlockReadResult block, long headerEnd)
        {
            // Service blocks have a structure similar to file headers:
            // Base header: CRC (2) + Type (1) + Flags (2) + HeaderSize (2) = 7
            // ADD_SIZE (4) = PACK_SIZE (included in HeaderSize)
            // UNP_SIZE (4) + HOST_OS (1) + DATA_CRC (4) + FILE_TIME (4) +
            // UNP_VER (1) + METHOD (1) + NAME_SIZE (2) + ATTR (4) = 21 bytes
            // + NAME (variable, minimum 1 byte)

            const int minServiceHeaderSize = 7 + 4 + 21 + 1; // base + ADD_SIZE + fields + min name

            if (block.HeaderSize < minServiceHeaderSize)
            {
                return null;
            }

            if (_stream.Position + 21 > headerEnd)
            {
                return null;
            }

            var flags = (RARFileFlags)block.Flags;

            // PACK_SIZE is in AddSize for service blocks
            uint packSize = block.AddSize;

            // Read service block fields (same layout as file header)
            uint unpSize = _reader.ReadUInt32();
            byte hostOS = _reader.ReadByte();
            uint dataCrc = _reader.ReadUInt32();
            uint subTime = _reader.ReadUInt32();
            byte unpVer = _reader.ReadByte();
            byte method = _reader.ReadByte();
            ushort nameSize = _reader.ReadUInt16();
            uint subAttr = _reader.ReadUInt32();

            // Handle 64-bit sizes if LHD_LARGE is set
            ulong packedSize = packSize;
            ulong unpackedSize = unpSize;

            if ((flags & RARFileFlags.Large) != 0)
            {
                if (!TrySkipBytes(headerEnd, 8))
                {
                    return null;
                }
                // For simplicity, we skip high size bytes as comments are typically small
            }

            if (nameSize == 0 || _stream.Position + nameSize > headerEnd)
            {
                return null;
            }

            // Read sub-type name (e.g., "CMT", "RR", "AV")
            byte[] nameBytes = _reader.ReadBytes(nameSize);
            string subType = System.Text.Encoding.ASCII.GetString(nameBytes);

            var result = new RARServiceBlockInfo
            {
                SubType = subType,
                PackedSize = packedSize,
                UnpackedSize = unpackedSize,
                CompressionMethod = method,
                DataCrc = dataCrc,
                DataOffset = block.HeaderSize
            };

            return result;
        }

        /// <summary>
        /// Reads the data portion of a service block.
        /// Call this after ReadBlock to get the raw data.
        /// </summary>
        public byte[]? ReadServiceBlockData(RARBlockReadResult block)
        {
            if (block.BlockType != RAR4BlockType.Service || block.ServiceBlockInfo == null)
            {
                return null;
            }

            long dataStart = block.BlockPosition + block.HeaderSize;
            long dataSize = block.AddSize;

            if (dataSize == 0 || dataStart + dataSize > _stream.Length)
            {
                return null;
            }

            _stream.Seek(dataStart, SeekOrigin.Begin);
            return _reader.ReadBytes((int)dataSize);
        }
    }
}
