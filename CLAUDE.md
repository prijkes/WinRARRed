# WinRARRed Project Memory Bank

## Project Overview

**WinRARRed** is a Windows Forms application for brute-forcing RAR archive parameters to match expected checksums, primarily used for scene release reconstruction from SRR (Scene Release Reconstruction) files.

## Repository Structure

```
E:\Projects\WinRARRed\
├── RARLib/                     # RAR parsing/decompression library
│   ├── RARBlockType.cs         # RAR 4.x and 5.x block type enums
│   ├── RARFlags.cs             # Archive, file, and end archive flags
│   ├── RARFileHeader.cs        # RARFileHeader, RARArchiveHeader classes
│   ├── RARHeaderReader.cs      # Header parsing, RARServiceBlockInfo
│   ├── RARUtils.cs             # CRC, DOS dates, filename decoding
│   └── Decompression/          # Native RAR decompression (NEW)
│       ├── BitInput.cs         # Bit-level stream reading
│       ├── PackDef.cs          # Compression constants
│       ├── DecodeTable.cs      # Huffman decode tables
│       ├── HuffmanDecoder.cs   # Huffman encoding/decoding
│       ├── Unpack29.cs         # RAR 2.9/3.x LZSS decompressor
│       ├── Unpack50.cs         # RAR 5.x LZSS decompressor
│       ├── RARDecompressor.cs  # Facade class for decompression
│       └── PPMd/               # PPMd implementation
│           ├── RangeCoder.cs   # Carryless range coder
│           ├── SubAllocator.cs # Memory sub-allocator
│           └── ModelPPM.cs     # PPMd prediction model
├── WinRARRed/                  # Main Windows Forms application
│   ├── Forms/                  # UI forms
│   ├── IO/                     # File I/O classes (SRRFile, RARFile, etc.)
│   ├── Diagnostics/            # RARProcess, logging, event args
│   └── Manager.cs              # Core brute force orchestration
├── tools/                      # Python scripts for debugging
│   ├── inspect_srr_headers.py  # SRR file header inspector
│   └── inspect_rar_headers.py  # RAR file header inspector
└── unrar/                      # Unrar 7.20 source code (reference)
```

## Current State

### Completed Features
- SRR file parsing with full RAR header extraction
- Brute force RAR parameter combinations
- Archive comment extraction from CMT sub-blocks
  - Stored comments (method 0x30): native
  - Compressed comments (0x31-0x35): native LZSS + PPMd with fallback to unrar.exe
- Auto-scroll checkbox for log output
- Log line limit (1000 lines)
- CRC validation and logging
- File/directory timestamp preservation from SRR
- **Native RAR decompression library (LZSS + PPMd)** ✓

### Decompression Implementation Status

**COMPLETED:**
- BitInput - Bit-level stream reading (ported from `getbits.hpp`)
- DecodeTable - Huffman decoding tables (ported from `unpack.cpp`)
- HuffmanDecoder - Huffman encoding/decoding
- Unpack29 - RAR 2.9/3.x LZSS decompressor (ported from `unpack30.cpp`)
- Unpack50 - RAR 5.x LZSS decompressor (ported from `unpack50.cpp`)
- RangeCoder - PPMd carryless range coder (ported from `coder.cpp`)
- SubAllocator - PPMd memory allocation (ported from `suballoc.cpp`)
- ModelPPM - PPMd prediction model (ported from `model.cpp`)
- RARDecompressor - Facade class with automatic algorithm selection
- Integration into SRRFile comment extraction

**PENDING TESTING:**
- Real-world compressed comment decompression
- PPMd edge cases

## Key Technical Details

### RAR Comment Block Structure (RAR 4.x)
- Block type: 0x7A (Service)
- Sub-type name: "CMT"
- Compression methods:
  - 0x30 = Store (uncompressed)
  - 0x31-0x35 = Compressed (Fastest to Best)
- Comments use 64KB dictionary window
- CRC is 16-bit (lower 16 bits of CRC32)

### RAR Compression Algorithms
- **LZSS**: Huffman + LZ77 sliding window
  - RAR 2.9/3.x: `Unpack29.cs`
  - RAR 5.x: `Unpack50.cs`
- **PPMd**: Prediction by Partial Matching with range coding
  - `ModelPPM.cs` - Context modeling and symbol decoding
  - `RangeCoder.cs` - Arithmetic coding
  - `SubAllocator.cs` - Memory management

### Decompression Architecture
```
RARDecompressor.Decompress(data, size, method, version)
  ├── Store (0x30): Direct copy
  ├── RAR 2.9/3.x (0x31-0x35):
  │   ├── Try LZSS (Unpack29)
  │   └── Fallback to PPMd (ModelPPM)
  └── RAR 5.x:
      └── LZSS (Unpack50)
```

### Unrar Source Reference
Located at `E:\unrar` (version 7.20)

Key files for decompression:
- `getbits.hpp` - BitInput class
- `unpack.hpp/cpp` - Main Unpack class, DecodeTable
- `unpack30.cpp` - RAR 3.x (LZSS branch)
- `unpack50.cpp` - RAR 5.x
- `model.hpp/cpp` - PPMd model
- `coder.hpp/cpp` - Range coder
- `suballoc.hpp/cpp` - PPMd memory allocation

## Dependencies

### RARLib
- `Crc32.NET` (1.2.0) - CRC32 calculation

### WinRARRed
- `RARLib` (project reference)
- `CliWrap` (3.10.0) - Process execution
- `Crc32.NET` (1.2.0)
- `SharpCompress` (0.33.0) - RAR extraction (not used for comments)
- `Serilog` + sinks - Logging

## Recent Changes

### Session 2026-01-19 (Continued)
1. Implemented full native RAR decompression in RARLib/Decompression:
   - BitInput.cs - Bit-level stream reading
   - PackDef.cs - RAR compression constants
   - DecodeTable.cs - Huffman decode table structures
   - HuffmanDecoder.cs - MakeDecodeTables and DecodeNumber
   - Unpack29.cs - RAR 2.9/3.x LZSS decompressor
   - Unpack50.cs - RAR 5.x LZSS decompressor
   - PPMd/RangeCoder.cs - Carryless range coder
   - PPMd/SubAllocator.cs - Memory sub-allocator
   - PPMd/ModelPPM.cs - PPMd prediction model
   - RARDecompressor.cs - Facade with algorithm auto-selection
2. Integrated native decompression into SRRFile.cs
   - TryNativeDecompressComment() tries native first
   - Falls back to unrar.exe if native fails

### Session 2025-01-19
1. Added archive comment extraction from SRR files
2. Created RARLib as separate class library
3. Moved RAR parsing code from WinRARRed to RARLib
4. Updated namespaces and project references

### Previous Session
1. Fixed CancellationTokenSource disposal error
2. Added auto-scroll checkbox
3. Added expected CRC logging
4. Added 1000-line log limit
5. Created Python inspector scripts
6. Fixed RAR file deletion bug

## Notes

- Scene releases typically use RAR 4.x format
- Most comments are stored (0x30) or use LZSS compression
- PPMd is rare for comments but implemented for 100% coverage
- Native decompression is attempted first, with unrar.exe fallback
- SharpCompress has RAR decompression but doesn't expose comment extraction API
