# WinRARRed

Windows Forms application for brute-forcing RAR archive parameters to reconstruct scene releases from SRR files.

## WHAT - Tech Stack & Structure

- **.NET 8** Windows Forms application
- **C# 12** with nullable reference types, file-scoped namespaces, primary constructors
- **Multi-project solution:**
  - `RARLib/` - RAR parsing and native decompression library
  - `SRRLib/` - SRR file parsing library
  - `WinRARRed/` - Main GUI application
  - `tools/` - Python debugging/testing scripts

## WHY - Architecture Decisions

**Separate libraries (RARLib, SRRLib):** Enables reuse and cleaner testing. RAR format parsing is complex and benefits from isolation.

**Native decompression:** Implemented LZSS and PPMd decompressors (ported from unrar source) to extract compressed comments without external dependencies.

**Two-phase brute-force:** Phase 1 tests comment blocks (fast, small data) to filter RAR versions, Phase 2 uses only matched versions for full RAR creation. Dramatically reduces search space.

**Host OS patching:** RAR stores creator OS in headers. When brute-forcing on Windows for a Unix-created archive, we patch headers post-creation to match original.

## HOW - Commands & Workflow

```bash
# Build
dotnet build WinRARRed/WinRARRed.csproj

# Run
dotnet run --project WinRARRed/WinRARRed.csproj

# Build release
dotnet publish WinRARRed/WinRARRed.csproj -c Release
```

### Python Tools (in `tools/`)
```bash
# Full RAR brute-force from SRR (CLI equivalent of WinRARRed)
python tools/bruteforce_rar.py input.srr --rar-dir E:\WinRAR2 --output-dir output/

# CMT block brute-force only
python tools/bruteforce_cmt.py input.srr --rar-dir E:\WinRAR2

# Inspect SRR/RAR headers
python tools/inspect_srr_headers.py file.srr
python tools/inspect_rar_headers.py file.rar
```

## Key Patterns

- **Async/await** with CancellationToken for all long-running operations
- **Events** for progress reporting (not callbacks)
- **Options classes** (RAROptions, BruteForceOptions) for configuration
- **Result pattern** preferred over exceptions for expected failures

## Patterns to Avoid

- Don't use `Task.Run` for I/O-bound work
- Don't catch generic `Exception` unless re-throwing
- Don't use magic numbers - use constants from `RARLib.RAR4BlockType`, `RARFileFlags`, etc.

## External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Crc32.NET | 1.2.0 | CRC32 calculation |
| CliWrap | 3.10.0 | Process execution wrapper |
| SharpCompress | 0.33.0 | RAR extraction (backup) |
| Serilog | latest | Structured logging |

## Domain Terminology

| Term | Meaning | Code Location |
|------|---------|---------------|
| **SRR** | Scene Release Reconstruction file - contains RAR headers without file data | `SRRLib/SRRFile.cs` |
| **CMT** | Comment service block in RAR archives | `RARLib/RARHeaderReader.cs` |
| **Host OS** | Operating system that created the RAR (stored in header byte 15) | `RAROptions.DetectedFileHostOS` |
| **UnpVer** | Unpack version - indicates RAR format version (29=RAR3, 50=RAR5) | `RARFileHeader.UnpackVersion` |
| **Phase 1** | Comment block brute-force to filter RAR versions | `Manager.BruteForceCommentPhaseAsync` |
| **Phase 2** | Full RAR brute-force with matched versions | `Manager.TryProcessCommandLinesAsync` |

## Git Workflow

- **Branches:** `feature/`, `fix/`, `refactor/`
- **Commits:** `type: description` (feat, fix, refactor, docs, test)
- **No force push** to master
- **Test before commit** - at minimum, verify build succeeds

## Known Issues

- **Skip `wrar30b1`, `wrar39b1`** - buggy beta versions that produce invalid archives
- **RAR 5.50+ defaults to RAR5 format** - always use `-ma4` flag for RAR4 reconstruction
- **RAR 3.0x dictionary options** - `-md128` may fail, use `-md128k` instead
- **CMT File Time varies by version** - RAR 3.10-4.20 use zero, others use current time (must patch)

## Documentation

Detailed technical documentation in `WinRARRed/docs/`:
- `rar-format.md` - RAR4/RAR5 format specification
- `rar-cmt-block.md` - CMT block structure and patching
- `rar-version-capabilities.md` - Version quirks and recommendations
- `rar-version-options.md` - Full option compatibility matrix (281 versions)
- `bruteforce-tools.md` - Python tool documentation

## Rules

@.claude/rules/architecture.md
@.claude/rules/coding-conventions.md
@.claude/rules/rar-technical.md
@.claude/rules/brute-force.md
