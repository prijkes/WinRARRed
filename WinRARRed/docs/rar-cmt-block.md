# RAR CMT Block Technical Reference

This document describes the behavior of the CMT (Comment) service block in RAR archives, based on testing across 281 RAR versions.

## CMT Block Header Structure (RAR 4.x)

```
Offset  Size  Field
------  ----  -----
0       2     Header CRC (lower 16 bits of CRC32 of bytes 2 to header end)
2       1     Block Type (0x7A = Service block)
3       2     Flags
5       2     Header Size
7       4     Packed Size (compressed comment data size)
11      4     Unpacked Size
15      1     Host OS
16      4     File CRC (CRC32 of uncompressed comment)
20      4     File Time (DOS format)
24      1     Unpack Version
25      1     Method (0x30=Store, 0x31-0x35=Compressed)
26      2     Name Size (always 3 for "CMT")
28      4     File Attributes
32      3     Sub-type name ("CMT")
35+     var   Compressed/stored comment data
```

## File Time (DOS) Field Behavior

**Tested: 2026-01-28 across 281 RAR versions**

The File Time field at offset 20-23 in the CMT block header is NOT controllable via command-line options.

### By RAR Version

| Version Range | File Time Value | Notes |
|---------------|-----------------|-------|
| RAR 2.x | N/A | No CMT block - comments stored in archive header |
| RAR 3.00, 3.0x betas | Current system time | 8 versions tested |
| RAR 3.10 - 4.20 | Zero (0x00000000) | 107 versions tested |
| RAR 5.00 - 5.40 | Current system time | 82 versions tested |
| RAR 5.50+ | N/A | RAR5 format - different block structure |

### What Does NOT Affect File Time

- **Comment file's mtime**: RAR ignores the modification time of the file passed to `-z`
- **Timestamp options**: `-tsm-`, `-tsc-`, `-tsa-` only affect file entry timestamps, not CMT block
- **Any other command-line option**: No known parameter controls this field

### Solution for Matching Original Archives

Post-creation patching is required:
1. Create RAR with comment using any compatible RAR version
2. Patch offset 20-23 in the CMT block header with target value
3. Recalculate header CRC (bytes 2 to header end) and update offset 0-1

## Host OS Field Behavior

The Host OS field at offset 15 reflects the operating system where RAR was executed:

| Value | OS |
|-------|-----|
| 0 | MS-DOS |
| 1 | OS/2 |
| 2 | Windows |
| 3 | Unix |
| 4 | Mac OS |
| 5 | BeOS |

This field is also not controllable via command-line and must be patched post-creation if needed.

## Timestamp Options Support

| RAR Version | -tsm/-tsc/-tsa Support |
|-------------|------------------------|
| RAR < 3.20 | Not supported |
| RAR 3.20+ | Fully supported |

Note: These options affect file entry timestamps only, not CMT block fields.

## Comment Storage Formats

### RAR 2.x (Old Format)
- Comments stored in Archive Header block (0x73)
- Archive header flag 0x0002 indicates comment present
- Not compatible with CMT service block approach

### RAR 3.x/4.x (Target Format)
- Comments stored in separate CMT service block (0x7A)
- 115 versions create this format
- Compatible with brute-force reconstruction

### RAR 5.x (New Format)
- RAR 5.0-5.40: Creates CMT service block (different structure)
- RAR 5.50+: RAR5 format with vint encoding, incompatible with RAR4

## Compression Methods

| Method | Value | Description |
|--------|-------|-------------|
| Store | 0x30 | Uncompressed |
| Fastest | 0x31 | Minimal compression |
| Fast | 0x32 | Light compression |
| Normal | 0x33 | Standard compression |
| Good | 0x34 | Better compression |
| Best | 0x35 | Maximum compression |

Comments typically use 64KB dictionary window regardless of method.

## CRC Calculation

The header CRC covers bytes from offset 2 to the end of the header (before data):
```python
header_crc = crc32(header_bytes[2:header_size]) & 0xFFFF
```

When patching any header field, the CRC must be recalculated.
