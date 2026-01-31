# RAR File Format Reference

## File Structure Overview

RAR files consist of a signature followed by a sequence of blocks.

## RAR 1.x - 4.x Format

### Signature
```
52 61 72 21 1A 07 00    (7 bytes: "Rar!" + 0x1A + 0x07 + 0x00)
```

### Block Types

| Type | Value | Description |
|------|-------|-------------|
| Marker | 0x72 | Archive marker (part of signature) |
| Archive Header | 0x73 | Main archive header with flags |
| File Header | 0x74 | File entry with metadata and data |
| Comment (Old) | 0x75 | Old-style comment (RAR 1.x) |
| Extra Info | 0x76 | Old-style extra info |
| Subblock | 0x77 | Old-style subblock |
| Recovery Record | 0x78 | Recovery record data |
| Auth Info | 0x79 | Authentication info (obsolete) |
| Service Block | 0x7A | Service block (CMT, ACL, etc.) |
| End Archive | 0x7B | End of archive marker |

### Base Block Header (7 bytes)

```
Offset  Size  Field
------  ----  -----
0       2     Header CRC (CRC32 & 0xFFFF)
2       1     Block Type
3       2     Flags
5       2     Header Size
```

### Common Flags

| Flag | Value | Description |
|------|-------|-------------|
| LONG_BLOCK | 0x8000 | Block has additional 4-byte size field |
| SKIP_IF_UNKNOWN | 0x4000 | Skip block if type unknown |

### Archive Header Flags (0x73)

| Flag | Value | Description |
|------|-------|-------------|
| VOLUME | 0x0001 | Multi-volume archive |
| COMMENT | 0x0002 | Archive comment present (old style) |
| LOCK | 0x0004 | Archive is locked |
| SOLID | 0x0008 | Solid archive |
| NEW_NUMBERING | 0x0010 | New volume naming scheme |
| AUTH | 0x0020 | Authenticity info present |
| RECOVERY | 0x0040 | Recovery record present |
| ENCRYPTED | 0x0080 | Block headers are encrypted |
| FIRST_VOLUME | 0x0100 | First volume |

### File Header Flags (0x74)

| Flag | Value | Description |
|------|-------|-------------|
| SPLIT_BEFORE | 0x0001 | File continues from previous volume |
| SPLIT_AFTER | 0x0002 | File continues in next volume |
| PASSWORD | 0x0004 | File is encrypted |
| COMMENT | 0x0008 | File comment present |
| SOLID | 0x0010 | Info from previous files used |
| DIRECTORY | 0x00E0 | Dictionary size (bits 5-7) |
| LARGE_FILE | 0x0100 | Large file (>2GB) |
| UNICODE | 0x0200 | Filename is Unicode |
| SALT | 0x0400 | Salt present for encryption |
| VERSION | 0x0800 | File version field present |
| EXTTIME | 0x1000 | Extended time field present |
| EXTFLAGS | 0x2000 | Extra flags field present |

### Dictionary Size Encoding (File Flags bits 5-7)

| Value | Size |
|-------|------|
| 0 | 64 KB |
| 1 | 128 KB |
| 2 | 256 KB |
| 3 | 512 KB |
| 4 | 1024 KB |
| 5 | 2048 KB |
| 6 | 4096 KB |

## RAR 5.x Format

### Signature
```
52 61 72 21 1A 07 01 00    (8 bytes: "Rar!" + 0x1A + 0x07 + 0x01 + 0x00)
```

### Key Differences from RAR 4.x

1. **Variable-length integers (vint)**: Most sizes use vint encoding
2. **Different block structure**: No base 7-byte header
3. **CRC32 instead of CRC16**: Full 32-bit CRC in headers
4. **UTF-8 filenames**: Always UTF-8, no encoding flag
5. **Unix timestamps**: mtime stored as Unix time, not DOS time

### Block Types (RAR 5.x)

| Type | Value | Description |
|------|-------|-------------|
| Main | 1 | Main archive header |
| File | 2 | File header |
| Service | 3 | Service block (CMT, etc.) |
| Encryption | 4 | Encryption header |
| End | 5 | End of archive |

### VInt Encoding

Variable-length integer with continuation bit:
- Bits 0-6: Data bits
- Bit 7: Continuation flag (1 = more bytes follow)

Example: 0x80 0x01 = 128 (0x80 & 0x7F = 0, plus 0x01 << 7 = 128)

### RAR 5.x Header Structure

```
CRC32 (4 bytes)
Header Size (vint) - size from type field onwards
Header Type (vint)
Header Flags (vint)
[Extra Area Size (vint)] - if flag 0x0001 set
[Data Size (vint)] - if flag 0x0002 set
... type-specific fields ...
```

## Version Compatibility Matrix

| Feature | RAR 2.x | RAR 3.x | RAR 4.x | RAR 5.0-5.40 | RAR 5.50+ |
|---------|---------|---------|---------|--------------|-----------|
| CMT Service Block | No | Yes | Yes | Yes* | No |
| -ts Options | No | 3.20+ | Yes | Yes | Yes |
| RAR4 Format | Yes | Yes | Yes | Yes | No |
| RAR5 Format | No | No | No | Optional | Default |

*RAR 5.0-5.40 create CMT blocks but with current time in File Time field.

## Recommended Versions for Scene Reconstruction

For brute-forcing CMT blocks to match original scene releases:

- **Best**: RAR 3.10 - 4.20 (107 versions)
  - Creates CMT service blocks
  - File Time = 0 (matches most originals)
  - Full timestamp option support (3.20+)

- **Avoid**: RAR 2.x (no CMT block), RAR 5.50+ (RAR5 format)
