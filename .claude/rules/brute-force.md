---
description: Two-phase brute-force algorithm, patching, and progress events
paths:
  - WinRARRed/Manager.cs
  - WinRARRed/RAROptions.cs
  - WinRARRed/BruteForceOptions.cs
  - tools/bruteforce_*.py
---

# Brute-Force Algorithm Rules

## Two-Phase Approach

### Phase 1: Comment Block Brute-Force (Fast)
**Purpose:** Narrow down RAR versions by testing only the CMT block

1. Create temporary directory with small dummy file
2. For each RAR version × compression method × dictionary size:
   - Create test RAR with comment using `-z` flag
   - Extract CMT block compressed data from generated RAR
   - Compare with original CMT compressed data from SRR
3. Collect versions that produce matching CMT blocks
4. Pass only matched versions to Phase 2

**Skip conditions:**
- No CMT data in SRR (`CmtCompressedData == null`)
- Archive has no comment

### Phase 2: Full RAR Brute-Force
**Purpose:** Test full RAR creation with matched versions

1. Prepare input directory (copy files, apply timestamps)
2. For each matched version × all parameter combinations:
   - Create RAR with full file set
   - Apply Host OS patching if needed
   - Calculate hash (CRC32 or SHA1)
   - Compare with expected hash from SFV/SRR
3. On match: move RAR to output, optionally stop or continue

## Parameter Space

| Parameter | Values | Notes |
|-----------|--------|-------|
| RAR version | 2.x - 6.x | ~281 versions tested |
| Compression | -m0 to -m5 | Method stored in header |
| Dictionary | 64k - 4096k | Encoded in flags |
| Timestamps | -tsm0-4, -tsc0-4, -tsa0-4 | Requires RAR 3.20+ |
| Archive format | -ma4, -ma5 | RAR 5.50+ defaults to RAR5 |
| File attributes | Archive, NotContentIndexed | Windows-specific |

## Post-Processing (Patching)

When `NeedsHostOSPatching` is true:

```csharp
// File headers
PatchOptions.FileHostOS = DetectedFileHostOS;        // e.g., 0x03 for Unix
PatchOptions.FileAttributes = DetectedFileAttributes; // e.g., 0x000081B4

// CMT service block (may differ from file headers)
PatchOptions.ServiceBlockHostOS = DetectedCmtHostOS;
PatchOptions.ServiceBlockFileTime = DetectedCmtFileTime;
PatchOptions.ServiceBlockAttributes = DetectedCmtFileAttributes;

// After patching any field, recalculate header CRC
ushort newCrc = (ushort)(Crc32Algorithm.Compute(header, 2, header.Length - 2) & 0xFFFF);
```

## Key Classes

| Class | Responsibility |
|-------|----------------|
| `Manager.BruteForceRARVersionAsync()` | Main orchestration |
| `Manager.BruteForceCommentPhaseAsync()` | Phase 1 implementation |
| `Manager.TryProcessCommandLinesAsync()` | Phase 2 per-version testing |
| `RARPatcher.PatchFile()` | Header patching with CRC recalc |
| `RAROptions` | All brute-force configuration |

## Progress Events

```csharp
// Subscribe in UI
manager.BruteForceProgress += (s, e) => {
    // e.TotalSize, e.CurrentProgress, e.StartDateTime
    // e.ReleaseDirectory, e.RARVersionDirectory, e.Arguments
};

manager.BruteForceStatusChanged += (s, e) => {
    // e.OldStatus, e.NewStatus, e.CompletionStatus
};
```

## Cancellation

All brute-force operations respect `CancellationToken`:

```csharp
// Manager creates internal CancellationTokenSource
// Call manager.Stop() to cancel all operations
// CliWrap automatically terminates child processes
```
