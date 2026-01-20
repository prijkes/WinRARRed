using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RARLib;
using RARLib.Decompression;

namespace WinRARRed.IO
{
    /// <summary>
    /// Parser for SRR (Scene Release Reconstruction) files.
    /// </summary>
    public class SRRFile
    {
        public List<SrrRarFileBlock> RarFiles { get; private set; } = [];
        public List<SrrStoredFileBlock> StoredFiles { get; private set; } = [];
        public HashSet<string> ArchivedFiles { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ArchivedDirectories { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> ArchivedDirectoryTimestamps { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> ArchivedDirectoryCreationTimes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> ArchivedDirectoryAccessTimes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> ArchivedFileTimestamps { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> ArchivedFileCreationTimes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> ArchivedFileAccessTimes { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ArchivedFileCrcs { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, RARFileFlags> ArchivedFileCrcFlags { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<long> RarVolumeSizes { get; private set; } = [];
        public long? VolumeSizeBytes { get; private set; }

        // Archive metadata extracted from RAR headers
        public int? CompressionMethod { get; private set; }
        public int? DictionarySize { get; private set; }
        public bool? IsSolidArchive { get; private set; }
        public bool? IsVolumeArchive { get; private set; }
        public bool? HasRecoveryRecord { get; private set; }
        public int? RARVersion { get; private set; }

        // Version indicators from RAR headers
        public bool? HasNewVolumeNaming { get; private set; }
        public bool? HasFirstVolumeFlag { get; private set; }
        public bool? HasEncryptedHeaders { get; private set; }
        public bool? HasLargeFiles { get; private set; }
        public bool? HasUnicodeNames { get; private set; }
        public bool? HasExtendedTime { get; private set; }

        // CRC validation tracking
        public int HeaderCrcMismatches { get; private set; }

        // Archive comment extracted from CMT sub-block
        public string? ArchiveComment { get; private set; }

        // Raw RAR data from embedded headers (needed for comment extraction)
        private byte[]? RawRarData { get; set; }

        public static SRRFile Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            SRRFile srr = new();
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new(fs);

            while (fs.Position < fs.Length)
            {
                if (fs.Position + 7 > fs.Length) break;

                long startPos = fs.Position;
                ushort crc = reader.ReadUInt16();
                byte typeRaw = reader.ReadByte();
                ushort flags = reader.ReadUInt16();
                ushort headerSize = reader.ReadUInt16();

                // Check if this is an SRR block type
                if (!IsSrrBlockType(typeRaw))
                {
                    // Unknown block, skip it
                    fs.Seek(startPos, SeekOrigin.Begin);
                    break;
                }

                SRRBlockType type = (SRRBlockType)typeRaw;

                uint addSize = 0;
                if ((flags & (ushort)SRRBlockFlags.LongBlock) != 0 || type == SRRBlockType.StoredFile)
                {
                    if (fs.Position + 4 > fs.Length) break;
                    addSize = reader.ReadUInt32();
                }

                if (headerSize < 7) break;

                long blockEndPos = startPos + headerSize + addSize;
                if (blockEndPos <= startPos || blockEndPos > fs.Length) break;

                if (type == SRRBlockType.StoredFile)
                {
                    var storedBlock = ParseStoredFileBlock(reader, fs, startPos, crc, type, flags, headerSize, addSize);
                    if (storedBlock == null) break;
                    srr.StoredFiles.Add(storedBlock);
                    fs.Seek(blockEndPos, SeekOrigin.Begin);
                    continue;
                }

                if (type == SRRBlockType.RarFile)
                {
                    var rarBlock = ParseRarFileBlock(reader, fs, startPos, crc, type, flags, headerSize, addSize);
                    if (rarBlock == null) break;
                    srr.RarFiles.Add(rarBlock);

                    // Parse embedded RAR headers that follow
                    long volumeTotalSize = ParseEmbeddedRarHeaders(reader, fs, srr);

                    if (volumeTotalSize > 0)
                    {
                        srr.RarVolumeSizes.Add(volumeTotalSize);
                    }
                }
                else
                {
                    // Skip other SRR block types
                    fs.Seek(blockEndPos, SeekOrigin.Begin);
                }
            }

            srr.CalculateVolumeSizeBytes();
            return srr;
        }

        private static bool IsSrrBlockType(byte type)
        {
            return type >= 0x69 && type <= 0x71;
        }

        private static SrrStoredFileBlock? ParseStoredFileBlock(BinaryReader reader, FileStream fs,
            long startPos, ushort crc, SRRBlockType type, ushort flags, ushort headerSize, uint addSize)
        {
            const int minStoredHeaderSize = 7 + 4 + 2;
            if (headerSize < minStoredHeaderSize) return null;

            long headerEnd = startPos + headerSize;
            if (headerEnd <= startPos || headerEnd > fs.Length) return null;

            if (fs.Position + 2 > headerEnd) return null;
            ushort nameLen = reader.ReadUInt16();
            if (fs.Position + nameLen > headerEnd || fs.Position + nameLen > fs.Length) return null;

            byte[] nameBytes = reader.ReadBytes(nameLen);
            string fileName = Encoding.UTF8.GetString(nameBytes);

            long dataOffset = startPos + headerSize;
            if (dataOffset < startPos || dataOffset > fs.Length) return null;

            return new SrrStoredFileBlock
            {
                Crc = crc,
                BlockType = type,
                Flags = flags,
                HeaderSize = headerSize,
                BlockPosition = startPos,
                AddSize = addSize,
                FileName = fileName,
                FileLength = addSize,
                DataOffset = dataOffset
            };
        }

        private static SrrRarFileBlock? ParseRarFileBlock(BinaryReader reader, FileStream fs,
            long startPos, ushort crc, SRRBlockType type, ushort flags, ushort headerSize, uint addSize)
        {
            ushort nameLen = reader.ReadUInt16();
            if (fs.Position + nameLen > fs.Length) return null;

            byte[] nameBytes = reader.ReadBytes(nameLen);
            string fileName = Encoding.UTF8.GetString(nameBytes);

            return new SrrRarFileBlock
            {
                Crc = crc,
                BlockType = type,
                Flags = flags,
                HeaderSize = headerSize,
                BlockPosition = startPos,
                AddSize = addSize,
                FileName = fileName
            };
        }

        private static long ParseEmbeddedRarHeaders(BinaryReader reader, FileStream fs, SRRFile srr)
        {
            // Check if this is RAR5 format by looking for the marker
            if (IsRar5Marker(fs))
            {
                return ParseEmbeddedRar5Headers(reader, fs, srr);
            }

            return ParseEmbeddedRar4Headers(reader, fs, srr);
        }

        private static bool IsRar5Marker(FileStream fs)
        {
            if (fs.Position + 8 > fs.Length)
                return false;

            long pos = fs.Position;
            byte[] marker = new byte[8];
            fs.Read(marker, 0, 8);
            fs.Position = pos;

            // RAR5 marker: Rar!\x1a\x07\x01\x00
            return marker[0] == 0x52 && marker[1] == 0x61 && marker[2] == 0x72 && marker[3] == 0x21 &&
                   marker[4] == 0x1A && marker[5] == 0x07 && marker[6] == 0x01 && marker[7] == 0x00;
        }

        private static long ParseEmbeddedRar4Headers(BinaryReader reader, FileStream fs, SRRFile srr)
        {
            long volumeTotalSize = 0;
            var rarReader = new RARHeaderReader(reader);

            while (fs.Position < fs.Length)
            {
                // Check if we've hit another SRR block
                byte? peekType = rarReader.PeekBlockType();
                if (peekType == null) break;
                if (IsSrrBlockType(peekType.Value)) break;

                // Read the RAR block
                var block = rarReader.ReadBlock(parseContents: true);
                if (block == null)
                {
                    fs.Seek(fs.Length, SeekOrigin.Begin);
                    break;
                }

                // Track CRC mismatches
                if (!block.CrcValid)
                {
                    srr.HeaderCrcMismatches++;
                }

                // Calculate actual RAR volume size (header + packed data for all blocks)
                // For file headers, AddSize = PackedSize (compressed data size)
                volumeTotalSize += block.HeaderSize + block.AddSize;

                // Process archive header
                if (block.ArchiveHeader != null)
                {
                    srr.ProcessArchiveHeader(block.ArchiveHeader);
                }

                // Process file header
                if (block.FileHeader != null)
                {
                    srr.ProcessFileHeader(block.FileHeader);
                }

                // Process service block (CMT comment, RR recovery, etc.)
                if (block.ServiceBlockInfo != null)
                {
                    srr.ProcessServiceBlock(block, rarReader);
                }

                // Skip to next block (headers only for file blocks in SRR)
                rarReader.SkipBlock(block, includeData: block.BlockType != RAR4BlockType.FileHeader);
            }

            return volumeTotalSize;
        }

        private static long ParseEmbeddedRar5Headers(BinaryReader reader, FileStream fs, SRRFile srr)
        {
            long volumeTotalSize = 0;

            // Skip RAR5 marker (8 bytes)
            fs.Seek(8, SeekOrigin.Current);
            volumeTotalSize += 8;

            var rarReader = new RAR5HeaderReader(fs);
            srr.RARVersion = 50; // RAR 5.0

            while (fs.Position < fs.Length)
            {
                // Check if we've hit another SRR block (RAR5 block types are 0-5, SRR blocks are 0x69-0x71)
                byte? peekType = rarReader.PeekBlockType();
                if (peekType == null) break;
                // SRR blocks have types in 0x69-0x71 range, RAR5 blocks are 0-5
                if (peekType.Value >= 0x69 && peekType.Value <= 0x71) break;

                // Read the RAR5 block
                var block = rarReader.ReadBlock();
                if (block == null)
                {
                    fs.Seek(fs.Length, SeekOrigin.Begin);
                    break;
                }

                // Track CRC mismatches
                if (!block.CrcValid)
                {
                    srr.HeaderCrcMismatches++;
                }

                // Calculate RAR5 volume size (CRC + header size vint + header + data)
                // Approximate: 4 (CRC) + 1 (header size vint) + header + data
                volumeTotalSize += 4 + 1 + (long)block.HeaderSize + (long)block.DataSize;

                // Process archive header
                if (block.ArchiveInfo != null)
                {
                    srr.ProcessRar5ArchiveHeader(block.ArchiveInfo);
                }

                // Process file header
                if (block.FileInfo != null)
                {
                    srr.ProcessRar5FileHeader(block.FileInfo, block.DataSize);
                }

                // Process service block (CMT comment, RR recovery, etc.)
                if (block.ServiceBlockInfo != null)
                {
                    srr.ProcessRar5ServiceBlock(block, rarReader);
                }

                // Skip to next block (headers only for file blocks in SRR)
                // In SRR, file data is not present, so we skip data for non-file blocks
                if (block.BlockType != RAR5BlockType.File)
                {
                    rarReader.SkipBlock(block);
                }
                else
                {
                    // For file blocks in SRR, only skip header (data is not present)
                    long target = block.BlockPosition + (long)block.HeaderSize;
                    if (target <= fs.Length)
                        fs.Position = target;
                }
            }

            return volumeTotalSize;
        }

        private void ProcessArchiveHeader(RARArchiveHeader header)
        {
            IsVolumeArchive ??= header.IsVolume;
            IsSolidArchive ??= header.IsSolid;
            HasRecoveryRecord ??= header.HasRecoveryRecord;
            HasNewVolumeNaming ??= header.HasNewVolumeNaming;
            HasFirstVolumeFlag ??= header.IsFirstVolume;
            HasEncryptedHeaders ??= header.HasEncryptedHeaders;
        }

        private void ProcessFileHeader(RARFileHeader header)
        {
            // Store first file's compression settings
            if (CompressionMethod == null)
            {
                CompressionMethod = header.CompressionMethod;
                DictionarySize = header.DictionarySizeKB;
                RARVersion = header.UnpackVersion;
                HasLargeFiles = header.HasLargeSize;
                HasUnicodeNames = header.HasUnicodeName;
                HasExtendedTime = header.HasExtendedTime;
            }

            // Add archive entry
            if (!string.IsNullOrEmpty(header.FileName))
            {
                AddArchiveEntry(
                    header.FileName,
                    header.IsDirectory,
                    header.FileCrc,
                    header.Flags,
                    header.ModifiedTime,
                    header.CreationTime,
                    header.AccessTime);
            }
        }

        private void ProcessServiceBlock(RARBlockReadResult block, RARHeaderReader rarReader)
        {
            var serviceInfo = block.ServiceBlockInfo;
            if (serviceInfo == null)
            {
                return;
            }

            // Only process CMT (comment) blocks
            if (!string.Equals(serviceInfo.SubType, "CMT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Read the comment data
            byte[]? commentData = rarReader.ReadServiceBlockData(block);
            if (commentData == null || commentData.Length == 0)
            {
                return;
            }

            serviceInfo.RawData = commentData;

            // Try to extract comment text
            if (serviceInfo.IsStored)
            {
                // Stored (uncompressed) comment - decode directly
                try
                {
                    ArchiveComment = Encoding.UTF8.GetString(commentData);
                }
                catch
                {
                    // Try with default encoding
                    try
                    {
                        ArchiveComment = Encoding.Default.GetString(commentData);
                    }
                    catch
                    {
                        // Failed to decode
                    }
                }
            }
                else
            {
                // Compressed comment - use native decompression
                ArchiveComment = TryNativeDecompressComment(serviceInfo, commentData);
            }
        }

        private static string? TryNativeDecompressComment(RARServiceBlockInfo serviceInfo, byte[] compressedData)
        {
            try
            {
                // Get the uncompressed size from the service block info
                int uncompressedSize = (int)serviceInfo.UnpackedSize;
                if (uncompressedSize <= 0 || uncompressedSize > 1024 * 1024) // Sanity check: max 1MB
                    return null;

                // Use native decompressor
                string? comment = RARDecompressor.DecompressComment(
                    compressedData,
                    uncompressedSize,
                    serviceInfo.CompressionMethod,
                    isRAR5: false); // SRR files typically use RAR4 format

                return comment;
            }
            catch
            {
                // Native decompression failed
                return null;
            }
        }

        private void ProcessRar5ArchiveHeader(RAR5ArchiveInfo info)
        {
            IsVolumeArchive ??= info.IsVolume;
            IsSolidArchive ??= info.IsSolid;
            HasRecoveryRecord ??= info.HasRecoveryRecord;
            HasNewVolumeNaming ??= info.HasVolumeNumber; // RAR5 uses volume number field
        }

        private void ProcessRar5FileHeader(RAR5FileInfo info, ulong dataSize)
        {
            // Store first file's compression settings
            if (CompressionMethod == null)
            {
                // RAR5 compression method: 0=store, 1-5=compression levels
                CompressionMethod = info.CompressionMethod == 0 ? 0x30 : 0x30 + info.CompressionMethod;
                DictionarySize = info.DictionarySizeKB;
                RARVersion = 50;
            }

            // Add archive entry
            if (!string.IsNullOrEmpty(info.FileName))
            {
                // RAR5 uses split before/after flags in header flags, not file flags
                var flags = RARFileFlags.None;
                if (info.IsSplitBefore) flags |= RARFileFlags.SplitBefore;
                if (info.IsSplitAfter) flags |= RARFileFlags.SplitAfter;

                // Convert Unix timestamp to DateTime if present
                DateTime? modifiedTime = null;
                if (info.ModificationTime.HasValue)
                {
                    modifiedTime = DateTimeOffset.FromUnixTimeSeconds(info.ModificationTime.Value).LocalDateTime;
                }

                AddArchiveEntry(
                    info.FileName,
                    info.IsDirectory,
                    info.FileCrc,
                    flags,
                    modifiedTime,
                    null, // RAR5 doesn't store creation time in basic header
                    null); // RAR5 doesn't store access time in basic header
            }
        }

        private void ProcessRar5ServiceBlock(RAR5BlockReadResult block, RAR5HeaderReader rarReader)
        {
            var serviceInfo = block.ServiceBlockInfo;
            if (serviceInfo == null)
            {
                return;
            }

            // Only process CMT (comment) blocks
            if (!string.Equals(serviceInfo.SubType, "CMT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Read the comment data
            byte[]? commentData = rarReader.ReadServiceBlockData(block);
            if (commentData == null || commentData.Length == 0)
            {
                return;
            }

            // Try to extract comment text
            if (serviceInfo.IsStored)
            {
                // Stored (uncompressed) comment - decode directly
                try
                {
                    ArchiveComment = Encoding.UTF8.GetString(commentData).TrimEnd('\0');
                }
                catch
                {
                    // Try with default encoding
                    try
                    {
                        ArchiveComment = Encoding.Default.GetString(commentData).TrimEnd('\0');
                    }
                    catch
                    {
                        // Failed to decode
                    }
                }
            }
            else
            {
                // Compressed comment - use native RAR5 decompression
                ArchiveComment = TryNativeDecompressRar5Comment(serviceInfo, commentData);
            }
        }

        private static string? TryNativeDecompressRar5Comment(RAR5ServiceBlockInfo serviceInfo, byte[] compressedData)
        {
            try
            {
                // Get the uncompressed size from the service block info
                int uncompressedSize = (int)serviceInfo.UnpackedSize;
                if (uncompressedSize <= 0 || uncompressedSize > 1024 * 1024) // Sanity check: max 1MB
                    return null;

                // Map RAR5 method to RARMethod enum (RAR5 method 0=store, 1-5=compression)
                byte method = (byte)(serviceInfo.CompressionMethod == 0 ? 0x30 : 0x30 + serviceInfo.CompressionMethod);

                // Use native decompressor with RAR5 version
                string? comment = RARDecompressor.DecompressComment(
                    compressedData,
                    uncompressedSize,
                    method,
                    isRAR5: true);

                return comment?.TrimEnd('\0');
            }
            catch
            {
                // Native decompression failed
                return null;
            }
        }

        private void AddArchiveEntry(string rawName, bool isDirectory, uint? fileCrc, RARFileFlags flags,
            DateTime? modifiedTime, DateTime? creationTime, DateTime? accessTime)
        {
            string? normalized = NormalizeArchivePath(rawName);
            if (string.IsNullOrEmpty(normalized)) return;

            if (isDirectory)
            {
                ArchivedDirectories.Add(normalized);
                SetDirectoryTimes(normalized, modifiedTime, creationTime, accessTime);
                return;
            }

            ArchivedFiles.Add(normalized);

            bool overwriteTimes = false;
            if (fileCrc.HasValue)
            {
                string crcString = fileCrc.Value.ToString("x8");
                bool newHasSplitAfter = (flags & RARFileFlags.SplitAfter) != 0;
                if (!ArchivedFileCrcs.TryGetValue(normalized, out _))
                {
                    ArchivedFileCrcs[normalized] = crcString;
                    ArchivedFileCrcFlags[normalized] = flags;
                    overwriteTimes = true;
                }
                else
                {
                    var existingFlags = ArchivedFileCrcFlags.TryGetValue(normalized, out var storedFlags) ? storedFlags : RARFileFlags.None;
                    bool existingHasSplitAfter = (existingFlags & RARFileFlags.SplitAfter) != 0;

                    if (existingHasSplitAfter && !newHasSplitAfter)
                    {
                        ArchivedFileCrcs[normalized] = crcString;
                        ArchivedFileCrcFlags[normalized] = flags;
                        overwriteTimes = true;
                    }
                }
            }

            SetFileTimes(normalized, modifiedTime, creationTime, accessTime, overwriteTimes);
        }

        private void SetDirectoryTimes(string path, DateTime? modifiedTime, DateTime? creationTime, DateTime? accessTime)
        {
            if (modifiedTime.HasValue)
                ArchivedDirectoryTimestamps[path] = modifiedTime.Value;
            if (creationTime.HasValue)
                ArchivedDirectoryCreationTimes[path] = creationTime.Value;
            if (accessTime.HasValue)
                ArchivedDirectoryAccessTimes[path] = accessTime.Value;
        }

        private void SetFileTimes(string path, DateTime? modifiedTime, DateTime? creationTime, DateTime? accessTime, bool overwrite)
        {
            if (modifiedTime.HasValue && (overwrite || !ArchivedFileTimestamps.ContainsKey(path)))
                ArchivedFileTimestamps[path] = modifiedTime.Value;
            if (creationTime.HasValue && (overwrite || !ArchivedFileCreationTimes.ContainsKey(path)))
                ArchivedFileCreationTimes[path] = creationTime.Value;
            if (accessTime.HasValue && (overwrite || !ArchivedFileAccessTimes.ContainsKey(path)))
                ArchivedFileAccessTimes[path] = accessTime.Value;
        }

        private static string? NormalizeArchivePath(string path)
        {
            string normalized = path.Trim();
            if (normalized.Length == 0) return null;

            normalized = normalized.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            while (normalized.StartsWith("." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                normalized = normalized[2..];
            }
            normalized = normalized.TrimStart(Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            if (normalized.Length == 0) return null;

            string[] parts = normalized.Split(Path.DirectorySeparatorChar);
            foreach (string part in parts)
            {
                if (part == "." || part == "..") return null;
            }

            return normalized;
        }

        private void CalculateVolumeSizeBytes()
        {
            if (RarVolumeSizes.Count == 0) return;

            Dictionary<long, int> counts = [];
            foreach (long size in RarVolumeSizes)
            {
                counts.TryGetValue(size, out int count);
                counts[size] = count + 1;
            }

            long bestSize = 0;
            int bestCount = 0;
            foreach (var entry in counts)
            {
                if (entry.Value > bestCount || (entry.Value == bestCount && entry.Key > bestSize))
                {
                    bestSize = entry.Key;
                    bestCount = entry.Value;
                }
            }

            if (bestSize > 0)
            {
                VolumeSizeBytes = bestSize;
            }
        }

        public string? ExtractStoredFile(string srrFilePath, string outputDirectory, Func<string, bool> match)
        {
            if (string.IsNullOrWhiteSpace(srrFilePath))
                throw new ArgumentException("SRR file path is required.", nameof(srrFilePath));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
            ArgumentNullException.ThrowIfNull(match);

            SrrStoredFileBlock? storedFile = null;
            foreach (var stored in StoredFiles)
            {
                if (match(stored.FileName))
                {
                    storedFile = stored;
                    break;
                }
            }

            if (storedFile == null) return null;

            string safeName = Path.GetFileName(storedFile.FileName);
            if (string.IsNullOrEmpty(safeName)) return null;

            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, safeName);

            using FileStream fs = new(srrFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long dataOffset = storedFile.DataOffset;
            long dataLength = storedFile.FileLength;

            if (dataOffset < 0 || dataOffset > fs.Length)
                throw new InvalidDataException("Stored file data offset is outside the SRR file bounds.");

            long dataEnd = dataOffset + dataLength;
            if (dataEnd < dataOffset || dataEnd > fs.Length)
                throw new InvalidDataException("Stored file length exceeds SRR file bounds.");

            fs.Seek(dataOffset, SeekOrigin.Begin);
            using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            CopyBytes(fs, output, dataLength);

            return outputPath;
        }

        private static void CopyBytes(Stream input, Stream output, long bytesToCopy)
        {
            byte[] buffer = new byte[81920];
            long remaining = bytesToCopy;

            while (remaining > 0)
            {
                int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0)
                    throw new EndOfStreamException("Unexpected end of SRR file while reading stored file data.");
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }
    }
}
