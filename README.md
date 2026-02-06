# WinRARRed

WinRARRed is a Windows utility that brute-forces WinRAR command line settings to recreate a bit-perfect RAR archive from its extracted files. It is aimed at preservation workflows where the original archive is missing but the payload files (and a verification hash) are intact.

![main_window](doc/main_window.png)

## How it works

1. Copies the release into a scratch folder (recreated on each run).
2. Iterates over each `rar.exe` under a WinRAR versions root, filtered by the selected version range.
3. Generates archives for each switch combination.
4. Calculates CRC32 or SHA1 and compares it to the verification file.
5. When a match is found, the archive is kept and reported.

An optional **two-phase** approach speeds things up dramatically:

- **Phase 1** tests only the CMT (comment) block against each RAR version. This is fast because the data is tiny.
- **Phase 2** runs the full RAR creation with only the versions that matched in Phase 1.

## Features

- Brute-force across multiple WinRAR versions (2.x through 6.x) and archive formats (`-ma4`, `-ma5`).
- Switch matrix support: compression level, dictionary size, solid on/off, recursion, timestamp flags, volume sizing, and `-mt` thread counts.
- File attribute toggling (Archive, NotContentIndexed) or `-ai` to ignore attributes.
- Host OS patching: when brute-forcing on Windows for a Unix-created archive, headers are automatically patched to match the original OS, attributes, and timestamps.
- SRR import to prefill settings and verify input files.
- Multi-volume handling for `.partXX.rar` and legacy `.r00` naming.
- View generated command lines from the UI.
- Logging to disk (app logs + per-attempt logs).
- Optional cleanup of non-matching archives to control disk usage.

### File Inspector

A built-in binary inspector for RAR and SRR files with:

- Tree view of all header blocks (signature, archive header, file headers, service blocks, end of archive).
- Property list showing every field in byte order with hex values and decoded descriptions.
- Hex view with highlighting: selecting a field highlights its bytes; selecting a byte highlights the owning field.
- Full RAR 4.x and RAR 5.x support, including extra area records (encryption, file hash, timestamps, locator, metadata, redirection, unix owner) and data areas (stored/compressed comment text).
- SRR block inspection with correct offset tracking for all block types (SRR header, stored files, RAR file references, OSO hashes, RAR padding).
- Drag-and-drop file opening.
- Tree filtering to search blocks by name.
- Export of raw block data via context menu.

## Requirements

- Windows 10/11.
- .NET 8.0 runtime.
- A folder of WinRAR builds, each in its own subdirectory containing `rar.exe`.
  The folder name must include version digits (examples: `winrar-x64-400`, `rar-550`, `winrar-x64-600`).
- The release directory with uncompressed files (must be unmodified).
- A verification file: `.sfv` (CRC32) or `.sha1`.

## Usage

1. Select the WinRAR versions folder.
2. Select the release folder.
3. Select the verification file (`.sfv` or `.sha1`).
4. Pick a temporary output folder (SSD recommended).
5. Open Options to select versions and switch families; optionally import an `.srr`.
6. Start the brute-force run.

Outputs:
- Scratch folders are created under the chosen output path:
  - `input` (copied release files)
  - `output` (generated archives)
  - `logs` (per-attempt RAR output)
- If a match is found, the matching archive is moved to the WinRAR versions root as `<ReleaseName>.rar`.
- App logs are written to `logs` next to the executable.
- If "Delete RAR files" is disabled, `output` keeps the first volume of non-matching attempts.

## SRR import

Use `Options -> Import SRR` to apply metadata from an `.srr`:
- Archive file list and CRC32s (used to validate copied input files).
- Compression method and dictionary size.
- Solid/archive and multi-volume flags plus volume sizing when detectable.
- Candidate RAR version range based on SRR headers.
- Stored `.sfv` extraction to `%TEMP%\WinRARRed\srr-import\...` when present.

## Project structure

```
WinRARRed/
├── RARLib/                     # RAR format library (no UI dependencies)
│   ├── Decompression/          # Native LZSS + PPMd decompressors
│   │   └── PPMd/               # PPMd model, range coder, allocator
│   ├── RARHeaderReader.cs      # RAR 4.x header parsing
│   ├── RAR5HeaderReader.cs     # RAR 5.x header parsing
│   ├── RARDetailedHeader.cs    # Detailed per-field parsing with byte offsets
│   ├── RARPatcher.cs           # Post-creation header patching
│   └── RARUtils.cs             # CRC, date conversion utilities
│
├── SRRLib/                     # SRR format library (depends on RARLib)
│   ├── SRRFile.cs              # Main parser, extracts RAR headers from SRR
│   └── SRRBlock.cs             # Block type definitions
│
├── WinRARRed/                  # Main GUI application
│   ├── Forms/                  # Windows Forms
│   │   ├── MainForm.cs         # Main window, file selection, log display
│   │   ├── FileInspectorForm.cs# RAR/SRR binary inspector with hex view
│   │   ├── FileCompareForm.cs  # SRR/RAR comparison tool
│   │   └── SettingsOptionsForm.cs
│   ├── Controls/               # Custom controls
│   │   └── HexViewControl.cs   # Hex viewer with field highlighting
│   ├── Diagnostics/            # Process management (CliWrap wrapper)
│   ├── IO/                     # File I/O (SFV, SHA1 parsing)
│   ├── Cryptography/           # CRC32, SHA1 hashing
│   ├── Manager.cs              # Core brute-force orchestration
│   ├── RAROptions.cs           # Configuration options
│   └── docs/                   # Technical documentation
│
├── RARLib.Tests/               # xUnit tests for RARLib (170 tests)
├── SRRLib.Tests/               # xUnit tests for SRRLib (43 tests)
│
└── tools/                      # Python CLI scripts
    ├── bruteforce_rar.py       # Full RAR brute-force from SRR
    ├── bruteforce_cmt.py       # CMT block brute-force only
    ├── inspect_rar_headers.py  # Inspect RAR headers
    ├── inspect_srr_headers.py  # Inspect SRR headers
    └── ...
```

## Build

```bash
# Build
dotnet build WinRARRed/WinRARRed.csproj

# Run
dotnet run --project WinRARRed/WinRARRed.csproj

# Build release
dotnet publish WinRARRed/WinRARRed.csproj -c Release

# Run tests
dotnet test RARLib.Tests/RARLib.Tests.csproj
dotnet test SRRLib.Tests/SRRLib.Tests.csproj
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Crc32.NET | 1.2.0 | CRC32 calculation |
| CliWrap | 3.10.0 | Process execution wrapper |
| SharpCompress | 0.33.0 | RAR extraction (backup) |
| Serilog | 4.2.0 | Structured logging |

## License

MIT - see [LICENSE](LICENSE).
