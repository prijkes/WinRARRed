using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System;

namespace WinRARRed.IO
{
    public class SRRFile
    {
        public List<SrrRarFileBlock> RarFiles { get; private set; } = [];
        public List<SrrStoredFileBlock> StoredFiles { get; private set; } = [];
        public HashSet<string> ArchivedFiles { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ArchivedDirectories { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ArchivedFileCrcs { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ushort> ArchivedFileCrcFlags { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<long> RarVolumeSizes { get; private set; } = [];
        public long? VolumeSizeBytes { get; private set; }

        private const ushort LHD_LARGE = 0x0100;
        private const ushort LHD_UNICODE = 0x0200;
        private const ushort LHD_DIRECTORY = 0x00E0;
        private const ushort LHD_EXTTIME = 0x1000;
        private const ushort LHD_SPLIT_BEFORE = 0x0001;
        private const ushort LHD_SPLIT_AFTER = 0x0002;

        private static readonly Encoding RarNameEncoding = GetRarNameEncoding();

        // Store the *first* found compression method and dict size
        // This is heuristic: we assume all files in the set use similar settings,
        // or at least the first one does (which is what we brute force).
        public int? CompressionMethod { get; private set; }
        public int? DictionarySize { get; private set; }
        
        // Additional archive settings from Volume Header
        public bool? IsSolidArchive { get; private set; }
        public bool? IsVolumeArchive { get; private set; }
        public bool? HasRecoveryRecord { get; private set; }
        public int? RARVersion { get; private set; }
        
        // Additional version indicators for more precise detection
        public bool? HasNewVolumeNaming { get; private set; }  // MHD_NEWNUMBERING (RAR 2.9+)
        public bool? HasFirstVolumeFlag { get; private set; }  // MHD_FIRSTVOLUME (RAR 3.0+)
        public bool? HasEncryptedHeaders { get; private set; } // MHD_PASSWORD
        public bool? HasLargeFiles { get; private set; }       // LHD_LARGE (files >2GB, RAR 2.6+)
        public bool? HasUnicodeNames { get; private set; }     // LHD_UNICODE (RAR 3.0+)
        public bool? HasExtendedTime { get; private set; }     // LHD_EXTTIME (RAR 2.0+)

        public static SRRFile Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            SRRFile srr = new();
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new(fs);

            // SRR files are a sequence of blocks.
            // We iterate until EOF.
            while (fs.Position < fs.Length)
            {
                // Read 7-byte Base Header
                // CRC (2), Type (1), Flags (2), Size (2)
                if (fs.Position + 7 > fs.Length) break;

                long startPos = fs.Position;
                ushort crc = reader.ReadUInt16();
                SRRBlockType type = (SRRBlockType)reader.ReadByte();
                ushort flags = reader.ReadUInt16();
                ushort headerSize = reader.ReadUInt16();

                uint addSize = 0;
                if ((flags & (ushort)SRRBlockFlags.LongBlock) != 0 || type == SRRBlockType.SrrStoredFile)
                {
                    if (fs.Position + 4 > fs.Length)
                    {
                        break;
                    }
                    addSize = reader.ReadUInt32();
                }

                if (headerSize < 7)
                {
                    break;
                }

                long blockEndPos = startPos + headerSize + addSize;
                if (blockEndPos <= startPos || blockEndPos > fs.Length)
                {
                    break;
                }

                if (type == SRRBlockType.SrrStoredFile)
                {
                    const int minStoredHeaderSize = 7 + 4 + 2;
                    if (headerSize < minStoredHeaderSize)
                    {
                        break;
                    }

                    long headerEnd = startPos + headerSize;
                    if (headerEnd <= startPos || headerEnd > fs.Length)
                    {
                        break;
                    }

                    if (fs.Position + 2 > headerEnd)
                    {
                        break;
                    }
                    ushort nameLen = reader.ReadUInt16();
                    if (fs.Position + nameLen > headerEnd || fs.Position + nameLen > fs.Length)
                    {
                        break;
                    }
                    byte[] nameBytes = reader.ReadBytes(nameLen);
                    string fileName = Encoding.UTF8.GetString(nameBytes);

                    long dataOffset = startPos + headerSize;
                    if (dataOffset < startPos || dataOffset > fs.Length)
                    {
                        break;
                    }

                    SrrStoredFileBlock storedBlock = new()
                    {
                        Crc = crc,
                        RawType = type,
                        Flags = flags,
                        HeaderSize = headerSize,
                        BlockPosition = startPos,
                        AddSize = addSize,
                        FileName = fileName,
                        FileLength = addSize,
                        DataOffset = dataOffset
                    };
                    srr.StoredFiles.Add(storedBlock);

                    fs.Seek(blockEndPos, SeekOrigin.Begin);
                    continue;
                }

                // If it's an SRR RAR File Block, it contains the RAR file name
                if (type == SRRBlockType.SrrRarFile)
                {
                    // For SrrRarFile (0x71), the header contains the file name size and name
                    // [Base 7] [NameSize 2] [Name X]
                    // SrrRarFileBlock has HEADER_LENGTH + len(file_name_data)
                    // It does NOT use AddSize for the name itself, the name is part of the "Header".

                    // Parse name from header
                    // Name length is 2 bytes
                    ushort nameLen = reader.ReadUInt16();
                    if (fs.Position + nameLen > fs.Length)
                    {
                        break;
                    }
                    byte[] nameBytes = reader.ReadBytes(nameLen);
                    string fileName = Encoding.UTF8.GetString(nameBytes);

                    SrrRarFileBlock rarBlock = new()
                    {
                        Crc = crc,
                        RawType = type,
                        Flags = flags,
                        HeaderSize = headerSize,
                        BlockPosition = startPos,
                        AddSize = addSize,
                        FileName = fileName
                    };
                    srr.RarFiles.Add(rarBlock);

                    // After a SrrRarFile block (0x71), RAR blocks follow directly
                    // They continue until we hit another SRR block (0x69-0x71) or EOF
                    // Note: SrrRarFile does NOT use addSize; RAR blocks are not "inside" it
                    long volumeTotalSize = 0;
                    
                    // Parse following RAR blocks
                    while (fs.Position < fs.Length)
                    {
                        // Check if we've hit another SRR block (types 0x69-0x71)
                        // If so, stop parsing RAR blocks and let outer loop handle it
                        if (fs.Position + 7 > fs.Length) break;
                        
                        // Peek at the block type
                        long peekPos = fs.Position;
                        ushort peekCrc = reader.ReadUInt16();
                        byte peekType = reader.ReadByte();
                        fs.Seek(peekPos, SeekOrigin.Begin); // rewind
                        
                        // If it's an SRR block (0x69-0x71), stop parsing RAR blocks
                        if (peekType >= 0x69 && peekType <= 0x71)
                        {
                            break;
                        }
                        
                        // RAR Block Header
                        // CRC(2), Type(1), Flags(2), Size(2)
                        long rBlockStart = fs.Position;
                        ushort rCrc = reader.ReadUInt16();
                        SRRBlockType rType = (SRRBlockType)reader.ReadByte();
                        ushort rFlags = reader.ReadUInt16();
                        ushort rHeadSize = reader.ReadUInt16();

                        if (rHeadSize < 7 || rBlockStart + rHeadSize > fs.Length)
                        {
                            fs.Seek(fs.Length, SeekOrigin.Begin);
                            break;
                        }
                        
                        // For RarPackedFile (0x74) and RarNewSub (0x7A), ADD_SIZE is ALWAYS present
                        // even if LONG_BLOCK flag is not set (BiA releases don't set the flag)
                        uint rAddSize = 0;
                        if ((rFlags & (ushort)SRRBlockFlags.LongBlock) != 0 ||
                            rType == SRRBlockType.RarFile ||
                            rType == (SRRBlockType)0x7A) // RarNewSub
                        {
                            if (fs.Position + 4 > fs.Length)
                            {
                                fs.Seek(fs.Length, SeekOrigin.Begin);
                                break;
                            }
                            rAddSize = reader.ReadUInt32();
                        }

                        volumeTotalSize += rHeadSize + rAddSize;

                        // Check for RarArchiveHeader (0x73) to extract archive flags
                        if (rType == SRRBlockType.RarArchiveHeader)
                        {
                            //Debug.WriteLine($"[SRR] Found RarArchiveHeader block at position 0x{rBlockStart:X}");
                            //Debug.WriteLine($"[SRR]   Flags: 0x{rFlags:X4}");
                            
                            // Volume Header flags (from unrar headers.hpp and pyrescene)
                            const ushort VOLUME = 0x0001;          // Multi-volume archive
                            const ushort SOLID = 0x0008;           // Solid archive
                            const ushort NEW_NUMBERING = 0x0010;   // New volume naming (RAR 2.9+)
                            const ushort PROTECTED = 0x0040;       // Has recovery record
                            const ushort PASSWORD = 0x0080;        // Encrypted headers
                            const ushort FIRST_VOLUME = 0x0100;    // First volume (RAR 3.0+)
                            
                            if (srr.IsVolumeArchive == null)
                            {
                                srr.IsVolumeArchive = (rFlags & VOLUME) != 0;
                                //Debug.WriteLine($"[SRR]   Volume Archive: {srr.IsVolumeArchive}");
                            }
                            
                            if (srr.IsSolidArchive == null)
                            {
                                srr.IsSolidArchive = (rFlags & SOLID) != 0;
                                //Debug.WriteLine($"[SRR]   Solid Archive: {srr.IsSolidArchive}");
                            }
                            
                            if (srr.HasRecoveryRecord == null)
                            {
                                srr.HasRecoveryRecord = (rFlags & PROTECTED) != 0;
                                //Debug.WriteLine($"[SRR]   Has Recovery Record: {srr.HasRecoveryRecord}");
                            }
                            
                            // Capture additional version indicators
                            if (srr.HasNewVolumeNaming == null)
                            {
                                srr.HasNewVolumeNaming = (rFlags & NEW_NUMBERING) != 0;
                                //Debug.WriteLine($"[SRR]   New Volume Naming (RAR 2.9+): {srr.HasNewVolumeNaming}");
                            }
                            
                            if (srr.HasFirstVolumeFlag == null)
                            {
                                srr.HasFirstVolumeFlag = (rFlags & FIRST_VOLUME) != 0;
                                //Debug.WriteLine($"[SRR]   First Volume Flag (RAR 3.0+): {srr.HasFirstVolumeFlag}");
                            }
                            
                            if (srr.HasEncryptedHeaders == null)
                            {
                                srr.HasEncryptedHeaders = (rFlags & PASSWORD) != 0;
                                //Debug.WriteLine($"[SRR]   Encrypted Headers: {srr.HasEncryptedHeaders}");
                            }
                        }
                        
                        // We are looking for RarFile (0x74) which is a File Header
                        if (rType == SRRBlockType.RarFile)
                        {
                            const int minRarFileHeaderSize = 7 + 4 + 4 + 1 + 4 + 4 + 1 + 1 + 2 + 4;
                            long headerEnd = rBlockStart + rHeadSize;
                            if (headerEnd > fs.Length || rHeadSize < minRarFileHeaderSize)
                            {
                                fs.Seek(fs.Length, SeekOrigin.Begin);
                                break;
                            }
                            if (fs.Position + (minRarFileHeaderSize - 7 - 4) > headerEnd)
                            {
                                long skipBlockEnd = rBlockStart + rHeadSize + rAddSize;
                                if (skipBlockEnd <= rBlockStart || skipBlockEnd > fs.Length)
                                {
                                    fs.Seek(fs.Length, SeekOrigin.Begin);
                                    break;
                                }
                                fs.Seek(skipBlockEnd, SeekOrigin.Begin);
                                continue;
                            }

                            //Debug.WriteLine($"[SRR] Found RarFile block at position 0x{rBlockStart:X}");
                            //Debug.WriteLine($"[SRR]   Flags: 0x{rFlags:X4}, HeadSize: {rHeadSize}, AddSize: {rAddSize}");
                            //Debug.WriteLine($"[SRR]   Current stream position: 0x{fs.Position:X}");
                            
                            // IMPORTANT: For RarFile (0x74), the ADD_SIZE field we already read IS the PACK_SIZE!
                            // The RAR File Header structure overlaps with the base header:
                            // Position 7-10:  ADD_SIZE / PACK_SIZE (same 4 bytes!)
                            // Position 11-14: UNP_SIZE
                            // Position 15:    HOST_OS
                            // Position 16-19: FILE_CRC
                            // Position 20-23: FILE_TIME
                            // Position 24:    UNP_VER
                            // Position 25:    METHOD ← This is what we want!
                            
                            //Debug.WriteLine($"[SRR]   PACK_SIZE: {rAddSize} (from ADD_SIZE field)");
                            
                            // Skip UnpSize(4)
                            uint unpSize = reader.ReadUInt32();
                            //Debug.WriteLine($"[SRR]   UNP_SIZE: {unpSize}");
                            
                            // Skip HostOS(1)
                            byte hostOS = reader.ReadByte();
                            //Debug.WriteLine($"[SRR]   HOST_OS: {hostOS}");
                            
                            // Skip FileCRC(4)
                            uint fileCRC = reader.ReadUInt32();
                            //Debug.WriteLine($"[SRR]   FILE_CRC: 0x{fileCRC:X8}");
                            
                            // Skip FileTime(4)
                            uint fileTime = reader.ReadUInt32();
                            //Debug.WriteLine($"[SRR]   FILE_TIME: 0x{fileTime:X8}");
                            
                            // Skip UnpVer(1)
                            byte unpVer = reader.ReadByte();
                            //Debug.WriteLine($"[SRR]   UNP_VER: {unpVer}");
                            
                            // Read Method(1) - THIS IS WHAT WE NEED!
                            byte method = reader.ReadByte();
                            //Debug.WriteLine($"[SRR]   METHOD: 0x{method:X2} ({method} decimal)");

                            bool isDirectoryEntry;
                            string? archivedName = TryReadRarFileName(reader, headerEnd, rFlags, out isDirectoryEntry);
                            if (!string.IsNullOrEmpty(archivedName))
                            {
                                if (archivedName.EndsWith("\\", StringComparison.Ordinal) || archivedName.EndsWith("/", StringComparison.Ordinal))
                                {
                                    isDirectoryEntry = true;
                                }

                                srr.AddArchiveEntry(archivedName, isDirectoryEntry, fileCRC, rFlags);
                            }
                            
                            // Save it!
                            if (srr.CompressionMethod == null)
                            {
                                srr.CompressionMethod = method;
                                
                                // Dictionary size is encoded in rFlags bits 5-7 (mask 0x00E0)
                                // DICT64=0x0000, DICT128=0x0020, DICT256=0x0040, DICT512=0x0060,
                                // DICT1024=0x0080, DICT2048=0x00A0, DICT4096=0x00C0, DIRECTORY=0x00E0
                                int dictFlags = (rFlags & 0x00E0) >> 5;
                                int[] dictSizes = [64, 128, 256, 512, 1024, 2048, 4096, 0];
                                if (dictFlags < 7)
                                {
                                    srr.DictionarySize = dictSizes[dictFlags];
                                }
                                
                                // Extract RAR version (UNP_VER we just read)
                                // Version is encoded as 10 * major + minor (e.g., 29 = RAR 2.9)
                                srr.RARVersion ??= unpVer;
                                
                                // Capture file header flags for version detection
                                if (srr.HasLargeFiles == null)
                                {
                                    srr.HasLargeFiles = (rFlags & LHD_LARGE) != 0;
                                    //Debug.WriteLine($"[SRR]   Large File Flag (RAR 2.6+): {srr.HasLargeFiles}");
                                }
                                
                                if (srr.HasUnicodeNames == null)
                                {
                                    srr.HasUnicodeNames = (rFlags & LHD_UNICODE) != 0;
                                    //Debug.WriteLine($"[SRR]   Unicode Names (RAR 3.0+): {srr.HasUnicodeNames}");
                                }
                                
                                if (srr.HasExtendedTime == null)
                                {
                                    srr.HasExtendedTime = (rFlags & LHD_EXTTIME) != 0;
                                    //Debug.WriteLine($"[SRR]   Extended Time (RAR 2.0+): {srr.HasExtendedTime}");
                                }
                                
                                //Debug.WriteLine($"[SRR] === EXTRACTED VALUES ===");
                                //Debug.WriteLine($"[SRR] Compression Method: 0x{srr.CompressionMethod:X2} ({srr.CompressionMethod} decimal)");
                                //Debug.WriteLine($"[SRR] Dictionary Size: {srr.DictionarySize} KB");
                                //Debug.WriteLine($"[SRR] RAR Version: {srr.RARVersion} ({(srr.RARVersion / 10)}.{(srr.RARVersion % 10)})");
                                //Debug.WriteLine($"[SRR] Solid Archive: {srr.IsSolidArchive}");
                                //Debug.WriteLine($"[SRR] Volume Archive: {srr.IsVolumeArchive}");
                                //Debug.WriteLine($"[SRR] Has Recovery Record: {srr.HasRecoveryRecord}");
                                //Debug.WriteLine($"[SRR] New Volume Naming: {srr.HasNewVolumeNaming}");
                                //Debug.WriteLine($"[SRR] First Volume Flag: {srr.HasFirstVolumeFlag}");
                                //Debug.WriteLine($"[SRR] Large Files: {srr.HasLargeFiles}");
                                //Debug.WriteLine($"[SRR] Unicode Names: {srr.HasUnicodeNames}");
                                //Debug.WriteLine($"[SRR] Extended Time: {srr.HasExtendedTime}");
                                //Debug.WriteLine($"[SRR] =========================");
                            }
                        }
                        
                        // Skip to next block.
                        // In SRR files, packed data is not stored, so RarFile blocks only advance by header size.
                        long innerBlockEnd = rType == SRRBlockType.RarFile
                            ? rBlockStart + rHeadSize
                            : rBlockStart + rHeadSize + rAddSize;
                        if (innerBlockEnd <= rBlockStart || innerBlockEnd > fs.Length)
                        {
                            fs.Seek(fs.Length, SeekOrigin.Begin);
                            break;
                        }
                        fs.Seek(innerBlockEnd, SeekOrigin.Begin);
                    }

                    if (volumeTotalSize > 0)
                    {
                        srr.RarVolumeSizes.Add(volumeTotalSize);
                    }
                }
                else
                {
                    // Skip to next block
                    // Total size = HeaderSize + AddSize
                    long nextPos = startPos + headerSize + addSize;
                    fs.Seek(nextPos, SeekOrigin.Begin);
                }
            }

            if (srr.RarVolumeSizes.Count > 0)
            {
                Dictionary<long, int> counts = new();
                foreach (long size in srr.RarVolumeSizes)
                {
                    counts.TryGetValue(size, out int count);
                    counts[size] = count + 1;
                }

                long bestSize = 0;
                int bestCount = 0;
                foreach (KeyValuePair<long, int> entry in counts)
                {
                    if (entry.Value > bestCount || (entry.Value == bestCount && entry.Key > bestSize))
                    {
                        bestSize = entry.Key;
                        bestCount = entry.Value;
                    }
                }

                if (bestSize > 0)
                {
                    srr.VolumeSizeBytes = bestSize;
                }
            }

            return srr;
        }

        private void AddArchiveEntry(string rawName, bool isDirectory, uint? fileCrc, ushort flags)
        {
            string? normalized = NormalizeArchivePath(rawName);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            if (isDirectory)
            {
                ArchivedDirectories.Add(normalized);
            }
            else
            {
                ArchivedFiles.Add(normalized);
                if (fileCrc.HasValue)
                {
                    string crcString = fileCrc.Value.ToString("x8");
                    bool newHasSplitAfter = (flags & LHD_SPLIT_AFTER) != 0;
                    if (!ArchivedFileCrcs.TryGetValue(normalized, out string? existing))
                    {
                        ArchivedFileCrcs[normalized] = crcString;
                        ArchivedFileCrcFlags[normalized] = flags;
                        return;
                    }

                    ushort existingFlags = ArchivedFileCrcFlags.TryGetValue(normalized, out ushort storedFlags) ? storedFlags : (ushort)0;
                    bool existingHasSplitAfter = (existingFlags & LHD_SPLIT_AFTER) != 0;
                    if (existingHasSplitAfter && !newHasSplitAfter)
                    {
                        ArchivedFileCrcs[normalized] = crcString;
                        ArchivedFileCrcFlags[normalized] = flags;
                        return;
                    }

                    if (existingHasSplitAfter == newHasSplitAfter &&
                        !string.Equals(existing, crcString, StringComparison.OrdinalIgnoreCase))
                    {
                        bool newHasSplitBefore = (flags & LHD_SPLIT_BEFORE) != 0;
                        string splitInfo = newHasSplitAfter
                            ? "split-continued"
                            : (newHasSplitBefore ? "split-end" : "single");
                        //Debug.WriteLine($"[SRR] CRC mismatch for {normalized} ({splitInfo}): existing {existing}, new {crcString}");
                    }
                }
            }
        }

        private static string? NormalizeArchivePath(string path)
        {
            string normalized = path.Trim();
            if (normalized.Length == 0)
            {
                return null;
            }

            normalized = normalized.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            while (normalized.StartsWith("." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                normalized = normalized[2..];
            }
            normalized = normalized.TrimStart(Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            if (normalized.Length == 0)
            {
                return null;
            }

            string[] parts = normalized.Split(Path.DirectorySeparatorChar);
            foreach (string part in parts)
            {
                if (part == "." || part == "..")
                {
                    return null;
                }
            }

            return normalized;
        }

        private static string? TryReadRarFileName(BinaryReader reader, long headerEnd, ushort flags, out bool isDirectory)
        {
            isDirectory = (flags & LHD_DIRECTORY) == LHD_DIRECTORY;

            if (reader.BaseStream.Position + 2 + 4 > headerEnd)
            {
                return null;
            }

            ushort nameSize = reader.ReadUInt16();
            uint fileAttributes = reader.ReadUInt32();
            if ((fileAttributes & 0x10) != 0)
            {
                isDirectory = true;
            }

            if ((flags & LHD_LARGE) != 0)
            {
                if (reader.BaseStream.Position + 8 > headerEnd)
                {
                    return null;
                }

                reader.ReadUInt32();
                reader.ReadUInt32();
            }

            if (nameSize == 0)
            {
                return null;
            }

            if (reader.BaseStream.Position + nameSize > headerEnd)
            {
                return null;
            }

            byte[] nameBytes = reader.ReadBytes(nameSize);
            return DecodeRarFileName(nameBytes, flags);
        }

        private static string? DecodeRarFileName(byte[] nameBytes, ushort flags)
        {
            if (nameBytes.Length == 0)
            {
                return null;
            }

            if ((flags & LHD_UNICODE) != 0)
            {
                int nullIndex = Array.IndexOf(nameBytes, (byte)0);
                if (nullIndex >= 0)
                {
                    byte[] stdName = nameBytes[..nullIndex];
                    byte[] encData = nameBytes[(nullIndex + 1)..];
                    if (encData.Length > 0)
                    {
                        return DecodeRarUnicode(stdName, encData);
                    }

                    if (stdName.Length > 0)
                    {
                        return RarNameEncoding.GetString(stdName);
                    }
                }

                try
                {
                    return Encoding.UTF8.GetString(nameBytes);
                }
                catch (DecoderFallbackException)
                {
                    return RarNameEncoding.GetString(nameBytes);
                }
            }

            return RarNameEncoding.GetString(nameBytes);
        }

        private static string DecodeRarUnicode(byte[] stdName, byte[] encData)
        {
            if (encData.Length == 0)
            {
                return RarNameEncoding.GetString(stdName);
            }

            List<byte> output = new(encData.Length * 2);
            int pos = 0;
            int encPos = 0;
            byte hi = encData[encPos++];
            int flagBits = 0;
            byte flags = 0;

            while (encPos < encData.Length)
            {
                if (flagBits == 0)
                {
                    flags = encData[encPos++];
                    flagBits = 8;
                }

                flagBits -= 2;
                int t = (flags >> flagBits) & 3;

                switch (t)
                {
                    case 0:
                        Put(output, EncByte(encData, ref encPos), 0, ref pos);
                        break;
                    case 1:
                        Put(output, EncByte(encData, ref encPos), hi, ref pos);
                        break;
                    case 2:
                        Put(output, EncByte(encData, ref encPos), EncByte(encData, ref encPos), ref pos);
                        break;
                    default:
                        byte n = EncByte(encData, ref encPos);
                        if ((n & 0x80) != 0)
                        {
                            byte c = EncByte(encData, ref encPos);
                            int count = (n & 0x7f) + 2;
                            for (int i = 0; i < count; i++)
                            {
                                byte lo = (byte)((StdByte(stdName, pos) + c) & 0xFF);
                                Put(output, lo, hi, ref pos);
                            }
                        }
                        else
                        {
                            int count = n + 2;
                            for (int i = 0; i < count; i++)
                            {
                                byte lo = StdByte(stdName, pos);
                                Put(output, lo, 0, ref pos);
                            }
                        }
                        break;
                }
            }

            return Encoding.Unicode.GetString(output.ToArray());
        }

        private static byte EncByte(byte[] data, ref int index)
        {
            if (index >= data.Length)
            {
                return 0;
            }

            return data[index++];
        }

        private static byte StdByte(byte[] data, int index)
        {
            if (index >= data.Length)
            {
                return 0;
            }

            return data[index];
        }

        private static void Put(List<byte> output, byte lo, byte hi, ref int pos)
        {
            output.Add(lo);
            output.Add(hi);
            pos++;
        }

        private static Encoding GetRarNameEncoding()
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8;
            }
            catch (NotSupportedException)
            {
                return Encoding.UTF8;
            }
        }

        public string? ExtractStoredFile(string srrFilePath, string outputDirectory, Func<string, bool> match)
        {
            if (string.IsNullOrWhiteSpace(srrFilePath))
            {
                throw new ArgumentException("SRR file path is required.", nameof(srrFilePath));
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
            }

            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            SrrStoredFileBlock? storedFile = null;
            foreach (SrrStoredFileBlock stored in StoredFiles)
            {
                if (match(stored.FileName))
                {
                    storedFile = stored;
                    break;
                }
            }

            if (storedFile == null)
            {
                return null;
            }

            string safeName = Path.GetFileName(storedFile.FileName);
            if (string.IsNullOrEmpty(safeName))
            {
                return null;
            }

            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, safeName);

            using FileStream fs = new(srrFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long dataOffset = storedFile.DataOffset;
            long dataLength = storedFile.FileLength;

            if (dataOffset < 0 || dataOffset > fs.Length)
            {
                throw new InvalidDataException("Stored file data offset is outside the SRR file bounds.");
            }

            long dataEnd = dataOffset + dataLength;
            if (dataEnd < dataOffset || dataEnd > fs.Length)
            {
                throw new InvalidDataException("Stored file length exceeds SRR file bounds.");
            }

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
                {
                    throw new EndOfStreamException("Unexpected end of SRR file while reading stored file data.");
                }
                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }
    }
}
