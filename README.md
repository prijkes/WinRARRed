# WinRARRed

WinRARRed is a Windows utility that brute-forces WinRAR command line settings to recreate a bit-perfect RAR archive from its extracted files. It is aimed at preservation workflows where the original archive is missing but the payload files (and a verification hash) are intact.

![main_window](doc/main_window.png)

## How it works

1. Copies the release into a scratch folder (recreated on each run).
2. Iterates over each `rar.exe` under a WinRAR versions root, filtered by the selected version range.
3. Generates archives for each switch combination.
4. Calculates CRC32 or SHA1 and compares it to the verification file.
5. When a match is found, the archive is kept and reported.

## Features

- Brute-force across multiple WinRAR versions (2.x through 6.x) and archive formats (`-ma4`, `-ma5`).
- Switch matrix support: compression level, dictionary size, solid on/off, recursion, timestamp flags, volume sizing, and `-mt` thread counts.
- File attribute toggling (Archive, NotContentIndexed) or `-ai` to ignore attributes.
- SRR import to prefill settings and verify input files (see below).
- Multi-volume handling for `.partXX.rar` and legacy `.r00` naming.
- View generated command lines from the UI.
- Logging to disk (app logs + per-attempt logs).
- Optional cleanup of non-matching archives to control disk usage.

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

## Build

Open `WinRARRed.sln` in Visual Studio 2022 and build `Release` / `x64`.

## Repo contents

- `WinRARRed/` - C# WinForms application.
- `tools/` - helper scripts for SRR inspection (optional).
- `pyrescene/` and `unrar/` - bundled upstream sources/reference material.

## License

MIT - see [LICENSE](LICENSE).
