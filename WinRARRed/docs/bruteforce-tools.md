# RAR Brute-Force Tools

This document describes the Python command-line tools for brute-forcing RAR archives.

## bruteforce_rar.py - Full SRR-based Reconstruction

Command-line equivalent of WinRARRed GUI. Reconstructs RAR archives from SRR files.

### Usage

```bash
python bruteforce_rar.py <input.srr> <release_dir> <rar_versions_dir> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `srr_file` | Path to SRR file (must contain SFV/SHA1/MD5) |
| `release_dir` | Directory containing original release files |
| `rar_versions_dir` | Directory containing RAR version folders |
| `--output-dir` | Output directory (default: ./bruteforce_output) |
| `--max-workers` | Parallel workers (default: 4) |
| `--info` | Show SRR info and exit |
| `-v, --verbose` | Verbose output |

### Hash Extraction

The script extracts expected hashes from stored files in the SRR:

- **SFV files** (`.sfv`) - Parsed for CRC32 hashes
- **SHA1 files** (`.sha1`, `.sha`) - Parsed for SHA1 hashes
- **MD5 files** (`.md5`) - Parsed for MD5 hashes

**The SRR must contain an SFV, SHA1, or MD5 file.** If no hash file is found, the script will error out.

### Example

```bash
# Typical usage
python bruteforce_rar.py release.srr ./release_files G:/WinRAR2

# Show SRR info including extracted hashes
python bruteforce_rar.py release.srr ./files G:/WinRAR2 --info

# Custom output directory
python bruteforce_rar.py release.srr ./files G:/WinRAR2 --output-dir ./output
```

### What It Does

1. **Parses SRR file** to extract:
   - Archived file/directory list
   - File timestamps (mtime, ctime, atime)
   - File CRCs
   - Archive comment (decompresses if needed)
   - Compression method and dictionary size
   - Host OS and file attributes
   - RAR version requirements

2. **Prepares input directory**:
   - Copies only files listed in SRR
   - Applies exact timestamps from SRR
   - Creates comment file if archive has comment

3. **Brute-forces parameters**:
   - Tries detected compression method/dictionary
   - Tries timestamp option combinations
   - Adds `-ma4` for RAR 5.50+

4. **Patches generated archives**:
   - Host OS byte if different platform
   - CMT file time if needed

5. **Compares hash** against expected value

---

## bruteforce_cmt.py - CMT Block Reconstruction

Specialized tool for brute-forcing only the CMT (comment) service block.

### Usage

```bash
python bruteforce_cmt.py <input.rar> <rar_versions_dir> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `input_rar` | Original RAR file with CMT block |
| `rar_versions_dir` | Directory containing RAR versions |
| `--output-dir` | Temp directory (default: ./temp_cmt) |
| `--comment-file` | External comment file (auto-extracts if not provided) |
| `--inspect FILE` | Inspect CMT block in a RAR file |
| `--max-workers` | Parallel workers (default: 4) |
| `-v, --verbose` | Verbose output |

### Example

```bash
# Inspect CMT block
python bruteforce_cmt.py --inspect original.rar

# Brute-force CMT block
python bruteforce_cmt.py original.rar G:/WinRAR2 --output-dir ./temp
```

### What It Does

1. **Extracts CMT block info** from original RAR:
   - Compression method
   - Dictionary size
   - Host OS
   - File time
   - Comment data (decompresses if needed)

2. **Auto-detects parameters** from header:
   - ExtTime flag presence
   - DOS time value
   - Dictionary size bits

3. **Brute-forces versions**:
   - Uses detected method and dictionary
   - Skips incompatible versions
   - Adds `-ma4` for RAR 5.50+

4. **Patches CMT block**:
   - Host OS byte
   - File time

5. **Compares raw block bytes** for exact match

---

## Common Features

### RAR Version Detection

Both tools parse version from folder names:

| Format | Example | Parsed As |
|--------|---------|-----------|
| `wrarXYZ` | wrar380 | 3.80 |
| `winrar-x64-XYZ` | winrar-x64-501 | 5.01 |
| `winrar-x64-XXb` | winrar-x64-55b1 | 5.50 |

### Version Compatibility

| Feature | Minimum Version |
|---------|-----------------|
| Timestamp options (-tsm, -tsc, -tsa) | RAR 3.20 |
| CMT service block creation | RAR 3.00 |
| -ma4 flag needed | RAR 5.50+ |

### Patching

Both tools can patch:
- **Host OS** (offset 15 in file/service headers)
- **File Time** (offset 20-23 in CMT headers)
- **CRC** is recalculated after patching

### Directory Structure

Output directory layout:
```
output/
├── input/              # Prepared input files
├── output/             # Generated RAR files
│   ├── wrar380_m3_md64k_tsc-_tsa-/
│   │   └── test.rar
│   └── ...
├── logs/               # Process logs (optional)
└── comment.txt         # Extracted comment
```

---

## Troubleshooting

### No match found

1. **Check source files** - Files must match exactly
2. **Verify expected hash** - Use correct CRC32/SHA1
3. **Check timestamps** - File times affect compression
4. **Try more combinations** - Expand parameter ranges

### CMT patching fails

1. **Check RAR version** - RAR 5.50+ creates RAR5 by default
2. **Verify -ma4** - Needed for RAR 5.50+ to create RAR4 format
3. **Check format** - Tool expects RAR4 CMT structure

### Slow performance

1. **Reduce workers** - Disk I/O may be bottleneck
2. **Use SSD** - Faster for temp files
3. **Pre-filter versions** - Only test likely candidates
