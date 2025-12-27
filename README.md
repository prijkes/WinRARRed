# WinRARRed

**WinRARRed** is a specialized utility designed to reconstruct exact, bit-perfect copies of RAR archives from their uncompressed contents. It is particularly useful for digital preservation and verifying the integrity of "scene" releases where the original archive file is missing or corrupted, but the contained files are intact.

## How It Works

WinRARRed uses a **brute-force** approach to identify the exact compression parameters used to create the original archive. It iteratively compresses the source files using:

*   **Multiple WinRAR Versions**: It cycles through different versions of `rar.exe` (e.g., v4.x, v5.x) to match the compression algorithm used originally.
*   **Command Line Switches**: It tests various permutations of compression arguments, including:
    *   Compression methods (`-m0` to `-m5`)
    *   Dictionary sizes (`-md64k` to `-md1g`)
    *   Time stamp behavior (`-ts`)
    *   File attributes (`-ai`, `-r`, etc.)
*   **File Attribute Toggling**: It attempts to match file attributes (Archive, NotContentIndexed) that affect the binary output.

For each attempt, it calculates the hash (CRC32 or SHA1) of the generated archive and compares it against a target hash provided by a verification file (`.sfv` or `.sha1`).

## Features

*   **Brute-Force Engine**: Automated testing of thousands of parameter combinations.
*   **Multi-Threading**: Utilizes multiple CPU cores to speed up the brute-force process.
*   **Hash Verification**: Supports matching against:
    *   **SFV** (CRC32)
    *   **SHA1**
*   **CLI Parameter Discovery**: Once a match is found, it reports the exact WinRAR version and command-line arguments used.
*   **Clean UI**: Windows Forms interface for easy configuration and progress monitoring.

## Prerequisites

To use WinRARRed, you need:

1.  **Source Files**: The uncompressed files from the original release (must be unmodified).
2.  **WinRAR Installations**: A directory containing multiple versions of `rar.exe` (WinRAR executables) in separate subdirectories (e.g., `WinRAR Versions/5.50/rar.exe`, `WinRAR Versions/6.00/rar.exe`).
3.  **Verification File**: An `.sfv` or `.sha1` file containing the hash of the original RAR archive(s).
4.  **.NET 8.0 Runtime**: The application is built on .NET 8.

## Usage

1.  **WinRAR Directory**: Select the folder containing your collection of `rar.exe` versions.
2.  **Release Directory**: Select the folder containing the uncompressed source files.
3.  **Verification File**: Select the `.sfv` or `.sha1` file with the expected hashes.
4.  **Temporary Directory**: A folder where temporary RAR files will be created (SSD recommended for speed).
5.  **Settings**: Configure which switches and versions to test via the "Options" menu.
6.  **Start**: Click start to begin the brute-force process.

## Build

Open `WinRARRed.sln` in Visual Studio 2022 and build for `Release` / `x64`.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
