---
description: Project structure, module responsibilities, and canonical examples
---

# Architecture Rules

## Project Structure

```
WinRARRed/
├── RARLib/                     # RAR format library (no UI dependencies)
│   ├── Decompression/          # Native LZSS + PPMd decompressors
│   │   └── PPMd/               # PPMd-specific classes
│   ├── RAR4BlockType.cs        # Block type enums
│   ├── RARFlags.cs             # Flag enums (RARFileFlags, RARArchiveFlags)
│   ├── RARHeaderReader.cs      # RAR 4.x header parsing
│   ├── RAR5HeaderReader.cs     # RAR 5.x header parsing
│   ├── RARPatcher.cs           # Post-creation header patching
│   └── RARUtils.cs             # CRC, date conversion utilities
│
├── SRRLib/                     # SRR format library
│   ├── SRRFile.cs              # Main parser, extracts RAR headers from SRR
│   └── SRRBlock.cs             # Block type definitions
│
├── WinRARRed/                  # Main application
│   ├── Forms/                  # Windows Forms UI
│   │   ├── MainForm.cs         # Main window, file selection, log display
│   │   ├── SettingsOptionsForm.cs  # Brute-force options configuration
│   │   └── FileCompareForm.cs  # SRR/RAR comparison tool
│   ├── Diagnostics/            # Process management
│   │   ├── RARProcess.cs       # CliWrap wrapper for rar.exe
│   │   └── *EventArgs.cs       # Progress/status event arguments
│   ├── IO/                     # File I/O utilities
│   ├── Cryptography/           # CRC32, SHA1 hash utilities
│   ├── Manager.cs              # Core brute-force orchestration
│   ├── RAROptions.cs           # Configuration options
│   └── docs/                   # Technical documentation
│
└── tools/                      # Python scripts (not part of build)
```

## Module Responsibilities

### RARLib
- Parse RAR 4.x and 5.x headers
- Native decompression (LZSS, PPMd) for comment extraction
- Header patching (Host OS, attributes, CRC recalculation)
- **No dependencies on WinRARRed or SRRLib**

### SRRLib
- Parse SRR file format
- Extract embedded RAR headers
- Extract stored files (SFV, NFO)
- **Depends on RARLib** for header parsing

### WinRARRed
- UI and user interaction
- Brute-force orchestration (Manager.cs)
- Process management (rar.exe execution)
- **Depends on RARLib and SRRLib**

## Canonical Examples

**Event-based progress reporting:**
See `Manager.cs:BruteForceProgress` event and `BruteForceProgressEventArgs`

**Options pattern:**
See `RAROptions.cs` - properties with XML doc comments, computed properties like `CanUseCommentPhase`

**RAR header parsing:**
See `RARHeaderReader.ReadBlock()` - reads header, validates CRC, returns structured result

**Async with cancellation:**
See `Manager.cs:BruteForceRARVersionAsync()` - uses CancellationTokenSource, passes token to child operations
