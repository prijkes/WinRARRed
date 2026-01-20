#!/usr/bin/env python3
import argparse
from pathlib import Path
import struct
import sys

RAR4_MARKER = b"Rar!\x1a\x07\x00"
RAR5_MARKER = b"Rar!\x1a\x07\x01\x00"


def find_markers(data, marker):
    offsets = []
    start = 0
    while True:
        idx = data.find(marker, start)
        if idx == -1:
            break
        offsets.append(idx)
        start = idx + 1
    return offsets


def format_offsets(offsets, limit=5):
    if not offsets:
        return "none"
    shown = offsets[:limit]
    suffix = ""
    if len(offsets) > limit:
        suffix = f" (+{len(offsets) - limit} more)"
    return ",".join(str(o) for o in shown) + suffix


def parse_srr_blocks(data):
    offset = 0
    blocks = []
    while offset + 7 <= len(data):
        crc, btype, flags, hsize = struct.unpack_from("<HBHH", data, offset)
        if hsize < 7:
            break
        add_size = 0
        if (flags & 0x8000) != 0 or btype == 0x6A:
            if offset + 11 > len(data):
                break
            add_size = struct.unpack_from("<I", data, offset + 7)[0]
        end = offset + hsize + add_size
        if end <= offset or end > len(data):
            break
        blocks.append(btype)
        offset = end
    return blocks


def classify(rar4_offsets, rar5_offsets, has_rar_blocks):
    if not has_rar_blocks:
        if rar4_offsets or rar5_offsets:
            return "RAR markers without SRR RAR blocks"
        return "No RAR blocks"
    if rar5_offsets and not rar4_offsets:
        return "RAR5"
    if rar4_offsets and not rar5_offsets:
        return "RAR4"
    if rar4_offsets and rar5_offsets:
        return "RAR4+RAR5"
    return "RAR blocks (marker not found)"


def inspect_file(path):
    data = path.read_bytes()
    rar4_offsets = find_markers(data, RAR4_MARKER)
    rar5_offsets = find_markers(data, RAR5_MARKER)
    srr_blocks = parse_srr_blocks(data)
    srr_rar_blocks = srr_blocks.count(0x71)
    kind = classify(rar4_offsets, rar5_offsets, srr_rar_blocks > 0)
    return kind, rar4_offsets, rar5_offsets, srr_blocks, srr_rar_blocks


def summarize_blocks(blocks):
    counts = {}
    for btype in blocks:
        counts[btype] = counts.get(btype, 0) + 1

    labels = {
        0x69: "srr_header",
        0x6A: "srr_stored_file",
        0x6B: "srr_oso_hash",
        0x6C: "srr_rar_padding",
        0x71: "srr_rar_file",
        0x72: "rar_marker",
        0x73: "rar_archive_header",
        0x74: "rar_file",
        0x7A: "rar_new_sub",
        0x7B: "rar_archive_end",
    }

    parts = []
    for btype in sorted(counts):
        label = labels.get(btype, f"0x{btype:02x}")
        parts.append(f"{label}={counts[btype]}")
    return ", ".join(parts) if parts else "none"


def main():
    parser = argparse.ArgumentParser(
        description="Detect RAR marker version(s) inside an SRR file."
    )
    parser.add_argument(
        "srr_files",
        nargs="+",
        help="Path(s) to SRR files to inspect.",
    )
    args = parser.parse_args()

    exit_code = 0
    for srr in args.srr_files:
        path = Path(srr)
        if not path.is_file():
            print(f"{srr}: not found", file=sys.stderr)
            exit_code = 1
            continue

        try:
            kind, rar4_offsets, rar5_offsets, srr_blocks, srr_rar_blocks = inspect_file(path)
        except OSError as exc:
            print(f"{srr}: failed to read ({exc})", file=sys.stderr)
            exit_code = 1
            continue

        block_summary = summarize_blocks(srr_blocks)
        print(
            f"{srr}: {kind} "
            f"(rar4={len(rar4_offsets)} at {format_offsets(rar4_offsets)}, "
            f"rar5={len(rar5_offsets)} at {format_offsets(rar5_offsets)}, "
            f"srr_blocks={len(srr_blocks)}, srr_rar_blocks={srr_rar_blocks}, "
            f"blocks=[{block_summary}])"
        )

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
