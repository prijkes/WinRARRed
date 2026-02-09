using Force.Crc32;

namespace RARLib;

/// <summary>
/// Result of a patching operation on a single block.
/// </summary>
public class PatchResult
{
    public long BlockPosition { get; set; }
    public RAR4BlockType BlockType { get; set; }
    public string? FileName { get; set; }
    public byte OriginalHostOS { get; set; }
    public byte NewHostOS { get; set; }
    public uint OriginalAttributes { get; set; }
    public uint NewAttributes { get; set; }
    public ushort OriginalCrc { get; set; }
    public ushort NewCrc { get; set; }
}

/// <summary>
/// Options for patching RAR files. Supports exact values detected from SRR headers.
/// </summary>
public class PatchOptions
{
    // ===== File Header Options =====

    /// <summary>
    /// Target Host OS value for file headers (0=MS-DOS, 1=OS/2, 2=Windows, 3=Unix, 4=Mac OS, 5=BeOS).
    /// </summary>
    public byte? FileHostOS { get; set; }

    /// <summary>
    /// Target file attributes for file headers.
    /// </summary>
    public uint? FileAttributes { get; set; }

    // ===== Service Block (CMT) Options =====

    /// <summary>
    /// If true, also patch service blocks (like CMT).
    /// </summary>
    public bool PatchServiceBlocks { get; set; } = true;

    /// <summary>
    /// Target Host OS value for service blocks (CMT). If null, uses FileHostOS.
    /// </summary>
    public byte? ServiceBlockHostOS { get; set; }

    /// <summary>
    /// Target file attributes for service blocks (CMT). If null, uses FileAttributes.
    /// </summary>
    public uint? ServiceBlockAttributes { get; set; }

    /// <summary>
    /// Target file time (DOS format) for service blocks. If null, time is not patched.
    /// </summary>
    public uint? ServiceBlockFileTime { get; set; }

    // ===== Computed Properties =====

    /// <summary>
    /// Gets the Host OS to use for file headers. Returns null if no patching needed.
    /// </summary>
    public byte? GetFileHostOS() => FileHostOS;

    /// <summary>
    /// Gets the Host OS to use for service blocks. Falls back to FileHostOS if not set.
    /// </summary>
    public byte? GetServiceBlockHostOS() => ServiceBlockHostOS ?? FileHostOS;

    /// <summary>
    /// Gets the file attributes to use for file headers.
    /// </summary>
    public uint? GetFileAttributes() => FileAttributes;

    /// <summary>
    /// Gets the file attributes to use for service blocks. Falls back to FileAttributes if not set.
    /// </summary>
    public uint? GetServiceBlockAttributes() => ServiceBlockAttributes ?? FileAttributes;
}

/// <summary>
/// Patches RAR 4.x files to modify Host OS, attributes, and other header fields
/// while maintaining valid CRCs.
/// </summary>
public static class RARPatcher
{
    // RAR 4.x header field offsets (from block start)
    private const int OffsetCrc = 0;
    private const int OffsetType = 2;
    private const int OffsetFlags = 3;
    private const int OffsetHeaderSize = 5;
    private const int OffsetAddSize = 7;       // For file headers and service blocks
    private const int OffsetUnpSize = 11;
    private const int OffsetHostOS = 15;
    private const int OffsetFileCrc = 16;
    private const int OffsetFileTime = 20;
    private const int OffsetUnpVer = 24;
    private const int OffsetMethod = 25;
    private const int OffsetNameSize = 26;
    private const int OffsetAttr = 28;

    /// <summary>
    /// Host OS name lookup.
    /// </summary>
    public static string GetHostOSName(byte hostOS) => hostOS switch
    {
        0 => "MS-DOS",
        1 => "OS/2",
        2 => "Windows",
        3 => "Unix",
        4 => "Mac OS",
        5 => "BeOS",
        _ => $"Unknown ({hostOS})"
    };

    /// <summary>
    /// Patches a RAR file in-place to change Host OS and optionally attributes.
    /// </summary>
    /// <param name="filePath">Path to the RAR file to patch</param>
    /// <param name="options">Patching options</param>
    /// <returns>List of patch results for each modified block</returns>
    public static List<PatchResult> PatchFile(string filePath, PatchOptions options)
    {
        var results = new List<PatchResult>();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        PatchStream(stream, options, results);

        return results;
    }

    /// <summary>
    /// Patches a RAR file stream to change Host OS and optionally attributes.
    /// </summary>
    /// <param name="stream">Stream with read/write access</param>
    /// <param name="options">Patching options</param>
    /// <param name="results">List to add patch results to</param>
    public static void PatchStream(Stream stream, PatchOptions options, List<PatchResult> results)
    {
        // Skip RAR signature (7 bytes for RAR 4.x)
        stream.Position = 7;

        // Track End of Archive block for Archive Data CRC patching
        long endArchivePosition = -1;
        ushort endArchiveFlags = 0;
        ushort endArchiveHeaderSize = 0;

        while (stream.Position + 7 <= stream.Length)
        {
            long blockStart = stream.Position;

            // Read base header
            byte[] baseHeader = new byte[7];
            if (stream.Read(baseHeader, 0, 7) != 7)
                break;

            byte blockType = baseHeader[OffsetType];
            ushort headerSize = BitConverter.ToUInt16(baseHeader, OffsetHeaderSize);

            if (headerSize < 7 || blockStart + headerSize > stream.Length)
                break;

            // Check if this is a file header or service block
            bool isFileHeader = blockType == (byte)RAR4BlockType.FileHeader;
            bool isServiceBlock = blockType == (byte)RAR4BlockType.Service;

            if ((isFileHeader || (isServiceBlock && options.PatchServiceBlocks)) && headerSize >= 32)
            {
                // Read full header
                stream.Position = blockStart;
                byte[] fullHeader = new byte[headerSize];
                if (stream.Read(fullHeader, 0, headerSize) != headerSize)
                    break;

                // Extract current values
                ushort originalCrc = BitConverter.ToUInt16(fullHeader, OffsetCrc);
                byte originalHostOS = fullHeader[OffsetHostOS];
                uint originalAttr = BitConverter.ToUInt32(fullHeader, OffsetAttr);
                uint originalFileTime = BitConverter.ToUInt32(fullHeader, OffsetFileTime);

                // Extract filename for logging
                ushort nameSize = BitConverter.ToUInt16(fullHeader, OffsetNameSize);
                string? fileName = null;
                if (nameSize > 0 && 32 + nameSize <= headerSize)
                {
                    fileName = System.Text.Encoding.ASCII.GetString(fullHeader, 32, Math.Min(nameSize, headerSize - 32));
                }

                bool modified = false;

                // Determine target values based on block type
                byte? targetHostOS = isFileHeader ? options.GetFileHostOS() : options.GetServiceBlockHostOS();
                uint? targetAttr = isFileHeader ? options.GetFileAttributes() : options.GetServiceBlockAttributes();
                uint? targetFileTime = isServiceBlock ? options.ServiceBlockFileTime : null;

                // Patch Host OS if target value is set
                byte newHostOS = originalHostOS;
                if (targetHostOS.HasValue && fullHeader[OffsetHostOS] != targetHostOS.Value)
                {
                    fullHeader[OffsetHostOS] = targetHostOS.Value;
                    newHostOS = targetHostOS.Value;
                    modified = true;
                }

                // Patch attributes if target value is set
                uint newAttr = originalAttr;
                if (targetAttr.HasValue && originalAttr != targetAttr.Value)
                {
                    newAttr = targetAttr.Value;
                    byte[] attrBytes = BitConverter.GetBytes(newAttr);
                    Array.Copy(attrBytes, 0, fullHeader, OffsetAttr, 4);
                    modified = true;
                }

                // Patch service block file time if target value is set
                uint newFileTime = originalFileTime;
                if (targetFileTime.HasValue && originalFileTime != targetFileTime.Value)
                {
                    newFileTime = targetFileTime.Value;
                    byte[] timeBytes = BitConverter.GetBytes(newFileTime);
                    Array.Copy(timeBytes, 0, fullHeader, OffsetFileTime, 4);
                    modified = true;
                }

                if (modified)
                {
                    // Recalculate CRC (CRC32 of header bytes excluding CRC field, take lower 16 bits)
                    uint crc32 = Crc32Algorithm.Compute(fullHeader, 2, fullHeader.Length - 2);
                    ushort newCrc = (ushort)(crc32 & 0xFFFF);

                    // Update CRC in header
                    byte[] crcBytes = BitConverter.GetBytes(newCrc);
                    Array.Copy(crcBytes, 0, fullHeader, OffsetCrc, 2);

                    // Write modified header back
                    stream.Position = blockStart;
                    stream.Write(fullHeader, 0, fullHeader.Length);

                    results.Add(new PatchResult
                    {
                        BlockPosition = blockStart,
                        BlockType = (RAR4BlockType)blockType,
                        FileName = fileName,
                        OriginalHostOS = originalHostOS,
                        NewHostOS = newHostOS,
                        OriginalAttributes = originalAttr,
                        NewAttributes = newAttr,
                        OriginalCrc = originalCrc,
                        NewCrc = newCrc
                    });
                }

                // Move to next block (header + data for file/service blocks)
                uint addSize = BitConverter.ToUInt32(fullHeader, OffsetAddSize);
                stream.Position = blockStart + headerSize + addSize;
            }
            else
            {
                // Track End of Archive block position
                if (blockType == (byte)RAR4BlockType.EndArchive)
                {
                    endArchivePosition = blockStart;
                    endArchiveFlags = BitConverter.ToUInt16(baseHeader, OffsetFlags);
                    endArchiveHeaderSize = headerSize;
                }

                // Skip this block
                // For blocks with LONG_BLOCK flag or file headers, read ADD_SIZE
                ushort flags = BitConverter.ToUInt16(baseHeader, OffsetFlags);
                bool hasAddSize = (flags & (ushort)RARFileFlags.LongBlock) != 0 ||
                                  blockType == (byte)RAR4BlockType.FileHeader ||
                                  blockType == (byte)RAR4BlockType.Service;

                uint addSize = 0;
                if (hasAddSize && stream.Position + 4 <= stream.Length)
                {
                    byte[] addSizeBytes = new byte[4];
                    stream.Read(addSizeBytes, 0, 4);
                    addSize = BitConverter.ToUInt32(addSizeBytes, 0);
                }

                stream.Position = blockStart + headerSize + addSize;
            }

            // Safety check: prevent infinite loop
            if (stream.Position <= blockStart)
                break;
        }

        // After all header patching, update End of Archive's Archive Data CRC if needed.
        // The Archive Data CRC covers all bytes from offset 0 to the End of Archive block,
        // so it becomes stale after patching any header bytes within that range.
        if (results.Count > 0 && endArchivePosition >= 0 &&
            (endArchiveFlags & (ushort)RAREndArchiveFlags.DataCrc) != 0 &&
            endArchiveHeaderSize >= 11) // 7 base + 4 Archive Data CRC minimum
        {
            PatchEndOfArchiveCrc(stream, endArchivePosition, endArchiveHeaderSize);
        }
    }

    /// <summary>
    /// Analyzes a RAR file and returns information about blocks that would be patched.
    /// Does not modify the file.
    /// </summary>
    /// <param name="filePath">Path to the RAR file</param>
    /// <param name="options">Patching options to simulate</param>
    /// <returns>List of blocks that would be modified</returns>
    public static List<PatchResult> AnalyzeFile(string filePath, PatchOptions options)
    {
        var results = new List<PatchResult>();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // Skip RAR signature (7 bytes for RAR 4.x)
        stream.Position = 7;

        while (stream.Position + 7 <= stream.Length)
        {
            long blockStart = stream.Position;

            // Read base header
            byte[] baseHeader = new byte[7];
            if (stream.Read(baseHeader, 0, 7) != 7)
                break;

            byte blockType = baseHeader[OffsetType];
            ushort headerSize = BitConverter.ToUInt16(baseHeader, OffsetHeaderSize);

            if (headerSize < 7 || blockStart + headerSize > stream.Length)
                break;

            bool isFileHeader = blockType == (byte)RAR4BlockType.FileHeader;
            bool isServiceBlock = blockType == (byte)RAR4BlockType.Service;

            if ((isFileHeader || (isServiceBlock && options.PatchServiceBlocks)) && headerSize >= 32)
            {
                // Read full header
                stream.Position = blockStart;
                byte[] fullHeader = new byte[headerSize];
                if (stream.Read(fullHeader, 0, headerSize) != headerSize)
                    break;

                ushort originalCrc = BitConverter.ToUInt16(fullHeader, OffsetCrc);
                byte originalHostOS = fullHeader[OffsetHostOS];
                uint originalAttr = BitConverter.ToUInt32(fullHeader, OffsetAttr);

                ushort nameSize = BitConverter.ToUInt16(fullHeader, OffsetNameSize);
                string? fileName = null;
                if (nameSize > 0 && 32 + nameSize <= headerSize)
                {
                    fileName = System.Text.Encoding.ASCII.GetString(fullHeader, 32, Math.Min(nameSize, headerSize - 32));
                }

                // Determine target values based on block type
                byte? targetHostOS = isFileHeader ? options.GetFileHostOS() : options.GetServiceBlockHostOS();
                uint? targetAttr = isFileHeader ? options.GetFileAttributes() : options.GetServiceBlockAttributes();

                bool wouldModify = (targetHostOS.HasValue && originalHostOS != targetHostOS.Value) ||
                                  (targetAttr.HasValue && originalAttr != targetAttr.Value);

                if (wouldModify)
                {
                    results.Add(new PatchResult
                    {
                        BlockPosition = blockStart,
                        BlockType = (RAR4BlockType)blockType,
                        FileName = fileName,
                        OriginalHostOS = originalHostOS,
                        NewHostOS = targetHostOS ?? originalHostOS,
                        OriginalAttributes = originalAttr,
                        NewAttributes = targetAttr ?? originalAttr,
                        OriginalCrc = originalCrc,
                        NewCrc = 0 // Not calculated in analysis mode
                    });
                }

                uint addSize = BitConverter.ToUInt32(fullHeader, OffsetAddSize);
                stream.Position = blockStart + headerSize + addSize;
            }
            else
            {
                ushort flags = BitConverter.ToUInt16(baseHeader, OffsetFlags);
                bool hasAddSize = (flags & (ushort)RARFileFlags.LongBlock) != 0 ||
                                  blockType == (byte)RAR4BlockType.FileHeader ||
                                  blockType == (byte)RAR4BlockType.Service;

                uint addSize = 0;
                if (hasAddSize && stream.Position + 4 <= stream.Length)
                {
                    byte[] addSizeBytes = new byte[4];
                    stream.Read(addSizeBytes, 0, 4);
                    addSize = BitConverter.ToUInt32(addSizeBytes, 0);
                }

                stream.Position = blockStart + headerSize + addSize;
            }

            if (stream.Position <= blockStart)
                break;
        }

        return results;
    }

    /// <summary>
    /// Recalculates the Archive Data CRC in the End of Archive block.
    /// The Archive Data CRC is a CRC32 of all bytes from offset 0 to the start of the End of Archive block.
    /// </summary>
    private static void PatchEndOfArchiveCrc(Stream stream, long endArchivePosition, ushort headerSize)
    {
        // Compute CRC32 of all bytes from offset 0 to the End of Archive block
        stream.Position = 0;
        byte[] buffer = new byte[81920];
        long remaining = endArchivePosition;
        uint archiveDataCrc = 0;

        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = stream.Read(buffer, 0, toRead);
            if (read <= 0) break;
            archiveDataCrc = Crc32Algorithm.Append(archiveDataCrc, buffer, 0, read);
            remaining -= read;
        }

        // Read the End of Archive header
        stream.Position = endArchivePosition;
        byte[] endHeader = new byte[headerSize];
        if (stream.Read(endHeader, 0, headerSize) != headerSize)
            return;

        // Update Archive Data CRC at offset 7 (immediately after the 7-byte base header)
        BitConverter.GetBytes(archiveDataCrc).CopyTo(endHeader, 7);

        // Recalculate the End of Archive header's own CRC
        uint crc32 = Crc32Algorithm.Compute(endHeader, 2, endHeader.Length - 2);
        ushort newHeaderCrc = (ushort)(crc32 & 0xFFFF);
        BitConverter.GetBytes(newHeaderCrc).CopyTo(endHeader, OffsetCrc);

        // Write back
        stream.Position = endArchivePosition;
        stream.Write(endHeader, 0, endHeader.Length);
    }
}
