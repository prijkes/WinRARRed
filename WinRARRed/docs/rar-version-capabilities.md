# RAR Version Capabilities

This document describes the capabilities and quirks of different RAR versions relevant to scene release reconstruction.

## Version Naming Conventions

RAR versions appear in various naming formats:

| Format | Example | Version |
|--------|---------|---------|
| wrarXYZ | wrar380 | 3.80 |
| winrar-x32-XYZ | winrar-x32-420 | 4.20 |
| winrar-x64-XYZ | winrar-x64-540 | 5.40 |
| WinRAR X.YZ | WinRAR 3.80 | 3.80 |

## Command-Line Option Support

### Timestamp Options

| Option | Description | Minimum Version |
|--------|-------------|-----------------|
| -tsm- | Disable mtime preservation | RAR 3.20 |
| -tsc- | Disable ctime preservation | RAR 3.20 |
| -tsa- | Disable atime preservation | RAR 3.20 |
| -tsm+ | Enable mtime (default) | RAR 3.20 |

RAR versions before 3.20 ignore these options silently.

### Dictionary Size Options

| Option | Size | Notes |
|--------|------|-------|
| -md64k | 64 KB | Minimum |
| -md128k | 128 KB | |
| -md256k | 256 KB | |
| -md512k | 512 KB | |
| -md1024k / -md1m | 1 MB | |
| -md2048k / -md2m | 2 MB | |
| -md4096k / -md4m | 4 MB | Maximum for RAR4 |

### Compression Method Options

| Option | Method | Description |
|--------|--------|-------------|
| -m0 | Store | No compression |
| -m1 | Fastest | Minimal compression |
| -m2 | Fast | Light compression |
| -m3 | Normal | Standard (default) |
| -m4 | Good | Better compression |
| -m5 | Best | Maximum compression |

## Archive Format by Version

| Version | Default Format | Can Create RAR4? | Notes |
|---------|---------------|------------------|-------|
| RAR 2.x | RAR2 | Yes (implicit) | Comments in archive header |
| RAR 3.x | RAR4 | Yes | CMT service block |
| RAR 4.x | RAR4 | Yes | CMT service block |
| RAR 5.0-5.40 | RAR4 | Yes (default) | CMT service block |
| RAR 5.50+ | RAR5 | Yes (use -ma4) | Requires -ma4 for CMT block |

To force RAR4 format on RAR 5.50+:
```
rar a -ma4 archive.rar files
```

The `bruteforce_cmt.py` script automatically adds `-ma4` for RAR 5.50+ versions.

## Comment Handling by Version

### Comment Creation

| Version | Comment Location | -z Option |
|---------|-----------------|-----------|
| RAR 2.x | Archive header | Supported |
| RAR 3.x+ | CMT service block | Supported |
| RAR 5.50+ | RAR5 service block | Supported |

### CMT Block Quirks

| Version | File Time | Host OS |
|---------|-----------|---------|
| RAR 3.00-3.0x | Current time | System OS |
| RAR 3.10-4.20 | Zero | System OS |
| RAR 5.0-5.40 | Current time | System OS |

Both fields must be patched post-creation to match originals.

## Version Detection

To identify which RAR version created an archive, check:

1. **Signature**: RAR4 (7 bytes) vs RAR5 (8 bytes)
2. **Archive header flags**: Feature flags indicate capabilities
3. **Unpack version**: In file headers, indicates minimum required version
4. **CMT block File Time**: Zero suggests 3.10-4.20

## Tested Version Count

From G:\WinRAR2 collection (281 versions):

| Category | Count |
|----------|-------|
| Creates CMT block with zero time | 107 |
| Creates CMT block with current time | 90 |
| No CMT block (old format) | 53 |
| No CMT block (RAR5 format) | 31 |

## Recommended Versions for Brute-Forcing

### Best Choices
- **RAR 3.80 - 4.20**: Stable, well-tested, CMT time = 0
- **RAR 3.50 - 3.71**: Good compatibility, CMT time = 0

### To Avoid
- **RAR 2.x**: Different comment format
- **RAR 3.00-3.0x**: CMT time = current (needs patching)
- **RAR 5.50+**: Default RAR5 format, requires -ma4 flag

### Version Selection Strategy

1. Start with RAR versions matching the original's unpack version
2. If ExtTime flag is absent in original, use older versions
3. Match dictionary size to original (check file flags bits 5-7)
4. If Host OS differs, any version works (patch after)
