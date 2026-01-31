---
description: RAR format quick reference for header parsing and patching
paths:
  - RARLib/**
  - SRRLib/**
  - WinRARRed/Manager.cs
---

# RAR Technical Quick Reference

> Full documentation: `WinRARRed/docs/rar-format.md`, `rar-cmt-block.md`

## RAR 4.x Block Types

```csharp
// From RARLib/RAR4BlockType.cs
ArchiveHeader = 0x73,  // Main archive header
FileHeader    = 0x74,  // File entry
Service       = 0x7A,  // Service block (CMT, RR, etc.)
EndArchive    = 0x7B   // End of archive marker
```

## RAR 4.x Header Structure (offset from block start)

| Offset | Size | Field |
|--------|------|-------|
| 0 | 2 | CRC16 (lower 16 bits of CRC32 of bytes 2+) |
| 2 | 1 | Block type |
| 3 | 2 | Flags |
| 5 | 2 | Header size |
| 7 | 4 | ADD_SIZE (packed data size, if LONG_BLOCK flag) |
| 11 | 4 | UnpSize |
| 15 | 1 | **Host OS** (0=DOS, 2=Windows, 3=Unix) |
| 16 | 4 | File CRC |
| 20 | 4 | **File Time** (DOS format) |
| 24 | 1 | UnpVer |
| 25 | 1 | **Method** (0x30=Store, 0x31-0x35=Compressed) |
| 26 | 2 | Name size |
| 28 | 4 | **File Attributes** |
| 32 | N | Filename |

## CMT Block (Comment Service Block)

- Block type: `0x7A` (Service)
- Sub-type name: `"CMT"` at offset 32
- Compression: 0x30=Store, 0x31-0x35=LZSS compressed
- Dictionary: Always 64KB window
- Data follows header at `blockStart + headerSize`

## Key Version Differences

| Feature | RAR 2.x | RAR 3.x | RAR 4.x | RAR 5.x |
|---------|---------|---------|---------|---------|
| Timestamp options (-tsm) | No | 3.20+ | Yes | Yes |
| CMT block format | Archive header | Service block | Service block | RAR5 service |
| Default format | RAR4 | RAR4 | RAR4 | **RAR5** (use -ma4) |
| MB dict suffix (-md4m) | Yes | **No** | **No** | Yes |

## Patching Requirements

When brute-forcing on Windows for a Unix-created archive:

1. **Host OS** (offset 15): Patch from `0x02` (Windows) to `0x03` (Unix)
2. **File Attributes** (offset 28): Patch from `0x00000020` (Archive) to Unix mode (e.g., `0x000081B4`)
3. **CMT File Time** (offset 20): May need patching (version-dependent behavior)
4. **Recalculate CRC** after any patch: `CRC32(header[2:]) & 0xFFFF`

## Compression Methods

```
0x30 = Store (no compression)
0x31 = Fastest
0x32 = Fast
0x33 = Normal
0x34 = Good
0x35 = Best
```

Map to command-line: method byte - 0x30 = -m flag value (e.g., 0x35 = -m5)

## Dictionary Sizes (RAR 4.x)

Encoded in file flags bits 5-7 (mask 0x00E0):

| Value | Size | Command |
|-------|------|---------|
| 0 | 64 KB | -md64k |
| 1 | 128 KB | -md128k |
| 2 | 256 KB | -md256k |
| 3 | 512 KB | -md512k |
| 4 | 1024 KB | -md1024k |
| 5 | 2048 KB | -md2048k |
| 6 | 4096 KB | -md4096k |

## Reference Implementation

Unrar source at `E:\unrar` (v7.20):
- `headers.hpp` - Header structures and constants
- `arcread.cpp` - Archive reading logic
- `unpack30.cpp` - RAR 3.x decompression (LZSS)
- `unpack50.cpp` - RAR 5.x decompression
- `model.cpp` - PPMd model
