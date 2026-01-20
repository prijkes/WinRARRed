#!/usr/bin/env python3
"""
Inspect RAR file headers and display all fields and values.
Supports RAR 1.5 - 4.x format.
"""
import argparse
from pathlib import Path
import struct
import sys
import subprocess
import shutil

BLOCK_NAMES = {
    0x72: "rar_marker",
    0x73: "rar_archive_header",
    0x74: "rar_file_header",
    0x75: "rar_comment_header",
    0x76: "rar_old_auth_info",
    0x77: "rar_old_sub_block",
    0x78: "rar_old_recovery_record",
    0x79: "rar_old_auth_info2",
    0x7A: "rar_sub_block",
    0x7B: "rar_archive_end",
}

HOST_OS_NAMES = {
    0: "MS-DOS",
    1: "OS/2",
    2: "Windows",
    3: "Unix",
    4: "Mac OS",
    5: "BeOS",
}

METHOD_NAMES = {
    0x30: "Store",
    0x31: "Fastest",
    0x32: "Fast",
    0x33: "Normal",
    0x34: "Good",
    0x35: "Best",
}

SUB_BLOCK_TYPES = {
    "RR": "Recovery Record - Error correction data for repairing damaged archives",
    "AV": "Authenticity Verification - Digital signature/authentication info",
    "CMT": "Comment - Archive or file comment",
    "ACL": "Access Control List - Windows security permissions",
    "STM": "NTFS Stream - Alternate data streams",
    "EA": "Extended Attributes - OS/2 and Unix extended attributes",
    "UOW": "Unix Owner - Unix user/group ownership info",
    "QO": "NTFS Quota - NTFS quota information",
    "OS2EA": "OS/2 Extended Attributes",
    "LNKS": "Symbolic Link - Unix symbolic link target",
    "LNKH": "Hard Link - Hard link info",
}

# RAR signature bytes
RAR_SIGNATURE = b'\x52\x61\x72\x21\x1a\x07\x00'  # Rar!\x1a\x07\x00
RAR5_SIGNATURE = b'\x52\x61\x72\x21\x1a\x07\x01\x00'  # Rar!\x1a\x07\x01\x00

# Flag constants
LHD_LARGE = 0x0100
LHD_UNICODE = 0x0200
FLAG_LONG_BLOCK = 0x8000
FLAG_SKIP_IF_UNKNOWN = 0x4000

# Global to track current archive path for comment extraction
_current_archive_path = None


def extract_comment_with_unrar(archive_path):
    """Extract archive comment using unrar command-line tool or rarfile library."""
    # First, try using rarfile library
    try:
        import rarfile
        with rarfile.RarFile(str(archive_path)) as rf:
            comment = rf.comment
            if comment:
                return comment, None
    except ImportError:
        pass  # rarfile not installed
    except Exception as e:
        pass  # rarfile failed, try unrar
    
    # Try to find unrar executable
    unrar_paths = [
        shutil.which('unrar'),
        shutil.which('UnRAR'),
        # Common Windows paths
        r'C:Program FilesWinRARUnRAR.exe',
        r'C:Program Files (x86)WinRARUnRAR.exe',
        # Check relative to script
        str(Path(__file__).parent.parent / 'unrar' / 'unrar.exe'),
        str(Path(__file__).parent.parent / 'unrar' / 'unrar'),
    ]
    
    unrar_exe = None
    for p in unrar_paths:
        if p and Path(p).is_file():
            unrar_exe = p
            break
    
    if not unrar_exe:
        return None, "unrar not found (install WinRAR or run: pip install rarfile)"
    
    try:
        # Use 'cw' command to write comment to stdout
        result = subprocess.run(
            [unrar_exe, 'cw', '-inul', str(archive_path)],
            capture_output=True,
            text=True,
            timeout=10
        )
        if result.returncode == 0 and result.stdout.strip():
            return result.stdout.strip(), None
        return None, f"unrar returned code {result.returncode}"
    except subprocess.TimeoutExpired:
        return None, "unrar timed out"
    except Exception as e:
        return None, str(e)


def read_u16(data, offset):
    return struct.unpack_from("<H", data, offset)[0]


def read_u32(data, offset):
    return struct.unpack_from("<I", data, offset)[0]


def read_u64(data, offset):
    return struct.unpack_from("<Q", data, offset)[0]


def block_name(btype):
    return BLOCK_NAMES.get(btype, f"unknown_0x{btype:02x}")


def decode_dos_datetime(dos_time):
    """Decode DOS datetime format to readable string."""
    try:
        second = (dos_time & 0x1F) * 2
        minute = (dos_time >> 5) & 0x3F
        hour = (dos_time >> 11) & 0x1F
        day = (dos_time >> 16) & 0x1F
        month = (dos_time >> 21) & 0x0F
        year = ((dos_time >> 25) & 0x7F) + 1980
        return f"{year:04d}-{month:02d}-{day:02d} {hour:02d}:{minute:02d}:{second:02d}"
    except:
        return f"0x{dos_time:08x}"


def decode_rar_unicode(std_name, enc_data):
    """Decode RAR unicode filename encoding."""
    if not enc_data:
        return std_name.decode("cp437", errors="replace")
    out = bytearray()
    pos = 0
    enc_pos = 0
    hi = enc_data[enc_pos]
    enc_pos += 1
    flag_bits = 0
    flags = 0
    while enc_pos < len(enc_data):
        if flag_bits == 0:
            flags = enc_data[enc_pos]
            enc_pos += 1
            flag_bits = 8
        flag_bits -= 2
        t = (flags >> flag_bits) & 3
        if t == 0:
            lo = enc_data[enc_pos]
            enc_pos += 1
            out.extend([lo, 0])
            pos += 1
        elif t == 1:
            lo = enc_data[enc_pos]
            enc_pos += 1
            out.extend([lo, hi])
            pos += 1
        elif t == 2:
            lo = enc_data[enc_pos]
            hi2 = enc_data[enc_pos + 1] if enc_pos + 1 < len(enc_data) else 0
            enc_pos += 2
            out.extend([lo, hi2])
            pos += 1
        else:
            n = enc_data[enc_pos]
            enc_pos += 1
            if (n & 0x80) != 0:
                c = enc_data[enc_pos]
                enc_pos += 1
                count = (n & 0x7F) + 2
                for _ in range(count):
                    lo = (std_name[pos] + c) & 0xFF if pos < len(std_name) else c
                    out.extend([lo, hi])
                    pos += 1
            else:
                count = n + 2
                for _ in range(count):
                    lo = std_name[pos] if pos < len(std_name) else 0
                    out.extend([lo, 0])
                    pos += 1
    return out.decode("utf-16le", errors="replace")


def decode_name(name_bytes, flags):
    """Decode filename from RAR header."""
    if (flags & LHD_UNICODE) != 0:
        try:
            null_index = name_bytes.index(0)
        except ValueError:
            null_index = -1
        if null_index >= 0:
            std_name = name_bytes[:null_index]
            enc_data = name_bytes[null_index + 1:]
            if enc_data:
                return decode_rar_unicode(std_name, enc_data)
            return std_name.decode("cp437", errors="replace")
        try:
            return name_bytes.decode("utf-8")
        except UnicodeDecodeError:
            return name_bytes.decode("cp437", errors="replace")
    return name_bytes.decode("cp437", errors="replace")


def get_archive_flags(flags):
    """Get human-readable flag names for archive header."""
    flag_names = []
    if (flags & 0x0001) != 0:
        flag_names.append("volume")
    if (flags & 0x0002) != 0:
        flag_names.append("has_comment")
    if (flags & 0x0004) != 0:
        flag_names.append("locked")
    if (flags & 0x0008) != 0:
        flag_names.append("solid")
    if (flags & 0x0010) != 0:
        flag_names.append("new_volume_naming")
    if (flags & 0x0020) != 0:
        flag_names.append("has_auth_info")
    if (flags & 0x0040) != 0:
        flag_names.append("has_recovery")
    if (flags & 0x0080) != 0:
        flag_names.append("block_encrypted")
    if (flags & 0x0100) != 0:
        flag_names.append("first_volume")
    if (flags & FLAG_SKIP_IF_UNKNOWN) != 0:
        flag_names.append("skip_if_unknown")
    if (flags & FLAG_LONG_BLOCK) != 0:
        flag_names.append("long_block")
    return flag_names


def get_file_flags(flags):
    """Get human-readable flag names for file header."""
    flag_names = []
    if (flags & 0x0001) != 0:
        flag_names.append("split_before")
    if (flags & 0x0002) != 0:
        flag_names.append("split_after")
    if (flags & 0x0004) != 0:
        flag_names.append("encrypted")
    if (flags & 0x0008) != 0:
        flag_names.append("has_comment")
    if (flags & 0x0010) != 0:
        flag_names.append("solid")

    # Dictionary size
    dict_flag = flags & 0x00E0
    dict_map = {
        0x0000: "dict64k",
        0x0020: "dict128k",
        0x0040: "dict256k",
        0x0060: "dict512k",
        0x0080: "dict1024k",
        0x00A0: "dict2048k",
        0x00C0: "dict4096k",
        0x00E0: "directory",
    }
    if dict_flag in dict_map:
        flag_names.append(dict_map[dict_flag])

    if (flags & 0x0100) != 0:
        flag_names.append("large_file")
    if (flags & 0x0200) != 0:
        flag_names.append("unicode_name")
    if (flags & 0x0400) != 0:
        flag_names.append("has_salt")
    if (flags & 0x0800) != 0:
        flag_names.append("is_old_version")
    if (flags & 0x1000) != 0:
        flag_names.append("ext_time")
    if (flags & FLAG_SKIP_IF_UNKNOWN) != 0:
        flag_names.append("skip_if_unknown")
    if (flags & FLAG_LONG_BLOCK) != 0:
        flag_names.append("long_block")
    return flag_names


def get_end_flags(flags):
    """Get human-readable flag names for archive end block."""
    flag_names = []
    if (flags & 0x0001) != 0:
        flag_names.append("next_volume")
    if (flags & 0x0002) != 0:
        flag_names.append("data_crc_present")
    if (flags & 0x0004) != 0:
        flag_names.append("rev_space")
    if (flags & FLAG_SKIP_IF_UNKNOWN) != 0:
        flag_names.append("skip_if_unknown")
    if (flags & FLAG_LONG_BLOCK) != 0:
        flag_names.append("long_block")
    return flag_names


def get_subblock_flags(flags):
    """Get human-readable flag names for sub block."""
    flag_names = []
    if (flags & 0x0001) != 0:
        flag_names.append("split_before")
    if (flags & 0x0002) != 0:
        flag_names.append("split_after")
    if (flags & FLAG_SKIP_IF_UNKNOWN) != 0:
        flag_names.append("skip_if_unknown")
    if (flags & FLAG_LONG_BLOCK) != 0:
        flag_names.append("long_block")
    return flag_names


def parse_ext_time(data, offset, flags, mtime):
    """Parse extended time fields."""
    result = {}
    pos = offset

    if pos + 2 > len(data):
        return result, pos

    ext_flags = read_u16(data, pos)
    pos += 2
    result["ext_time_flags"] = f"0x{ext_flags:04x}"

    # mtime, ctime, atime, arctime - each has 4 bits
    times = ["mtime", "ctime", "atime", "arctime"]
    base_times = [mtime, 0, 0, 0]

    for i, time_name in enumerate(times):
        time_flags = (ext_flags >> ((3 - i) * 4)) & 0x0F
        if time_flags == 0:
            continue

        # Bit 3: time is present
        if (time_flags & 0x08) != 0:
            count = 3 - (time_flags & 0x03)
            if i == 0:
                # mtime uses the base file time
                base = base_times[i]
            else:
                # Other times need to read 4 bytes
                if pos + 4 <= len(data):
                    base = read_u32(data, pos)
                    pos += 4
                else:
                    continue

            # Read additional precision bytes
            nanoseconds = 0
            for j in range(count):
                if pos < len(data):
                    nanoseconds |= data[pos] << (j * 8)
                    pos += 1

            result[f"{time_name}_dos"] = f"0x{base:08x}"
            result[f"{time_name}_str"] = decode_dos_datetime(base)
            if count > 0:
                result[f"{time_name}_extra"] = f"0x{nanoseconds:06x}"

    return result, pos


def read_base_header(data, offset):
    """Read the base RAR block header."""
    if offset + 7 > len(data):
        return None

    crc, btype, flags, hsize = struct.unpack_from("<HBHH", data, offset)

    if hsize < 7:
        return None

    add_size = 0
    if (flags & FLAG_LONG_BLOCK) != 0 or btype in (0x74, 0x7A):
        if offset + 11 > len(data):
            return None
        add_size = read_u32(data, offset + 7)

    return {
        "offset": offset,
        "crc": crc,
        "type": btype,
        "flags": flags,
        "hsize": hsize,
        "add_size": add_size,
    }


def parse_marker_block(data, offset):
    """Parse RAR marker/signature block."""
    # The marker block is typically 7 bytes: Rar!\x1a\x07\x00
    # But it's presented as a standard block header
    return {
        "signature": data[offset:offset+7].hex(),
        "signature_str": repr(data[offset:offset+7]),
    }


def parse_archive_header(data, block):
    """Parse RAR archive header (0x73)."""
    offset = block["offset"]
    hsize = block["hsize"]
    pos = offset + 7

    result = {}

    # Reserved fields
    if pos + 2 <= offset + hsize:
        reserved1 = read_u16(data, pos)
        pos += 2
        result["reserved1"] = f"0x{reserved1:04x}"

    if pos + 4 <= offset + hsize:
        reserved2 = read_u32(data, pos)
        pos += 4
        result["reserved2"] = f"0x{reserved2:08x}"

    # If encrypted, there may be salt
    if (block["flags"] & 0x0080) != 0:
        if pos + 8 <= offset + hsize:
            salt = data[pos:pos+8]
            pos += 8
            result["encryption_salt"] = salt.hex()

    return result


def parse_file_header(data, block):
    """Parse RAR file header (0x74)."""
    offset = block["offset"]
    flags = block["flags"]
    hsize = block["hsize"]
    header_end = offset + hsize
    pos = offset + 7

    if pos + 25 > header_end:
        return None

    # Read fixed fields
    pack_size = read_u32(data, pos)
    pos += 4
    unp_size = read_u32(data, pos)
    pos += 4
    host_os = data[pos]
    pos += 1
    file_crc = read_u32(data, pos)
    pos += 4
    file_time = read_u32(data, pos)
    pos += 4
    unp_ver = data[pos]
    pos += 1
    method = data[pos]
    pos += 1
    name_size = read_u16(data, pos)
    pos += 2
    file_attr = read_u32(data, pos)
    pos += 4

    high_pack = 0
    high_unp = 0
    if (flags & LHD_LARGE) != 0:
        if pos + 8 <= header_end:
            high_pack = read_u32(data, pos)
            high_unp = read_u32(data, pos + 4)
            pos += 8
            pack_size += high_pack << 32
            unp_size += high_unp << 32

    if pos + name_size > header_end:
        return None

    name_bytes = data[pos:pos + name_size]
    file_name = decode_name(name_bytes, flags)
    pos += name_size

    result = {
        "file_name": file_name,
        "file_name_bytes": name_bytes.hex(),
        "name_size": name_size,
        "pack_size_low": pack_size & 0xFFFFFFFF,
        "pack_size": pack_size,
        "unp_size_low": unp_size & 0xFFFFFFFF,
        "unp_size": unp_size,
        "host_os": host_os,
        "host_os_name": HOST_OS_NAMES.get(host_os, f"Unknown({host_os})"),
        "file_crc": f"{file_crc:08x}",
        "file_time": f"0x{file_time:08x}",
        "file_time_str": decode_dos_datetime(file_time),
        "unp_ver": unp_ver,
        "unp_ver_str": f"{unp_ver // 10}.{unp_ver % 10}",
        "method": f"0x{method:02x}",
        "method_name": METHOD_NAMES.get(method, f"Unknown(0x{method:02x})"),
        "file_attr": f"0x{file_attr:08x}",
    }

    if high_pack or high_unp:
        result["high_pack"] = high_pack
        result["high_unp"] = high_unp

    # Salt (if encrypted)
    if (flags & 0x0400) != 0:
        if pos + 8 <= header_end:
            salt = data[pos:pos+8]
            pos += 8
            result["salt"] = salt.hex()

    # Extended time
    if (flags & 0x1000) != 0:
        ext_time, pos = parse_ext_time(data, pos, flags, file_time)
        result.update(ext_time)

    return result


def parse_sub_block(data, block):
    """Parse RAR sub block (0x7A) - used for various metadata."""
    offset = block["offset"]
    hsize = block["hsize"]
    pos = offset + 7

    result = {}

    # Sub blocks have a structure similar to file headers
    if pos + 25 > offset + hsize:
        return result

    pack_size = read_u32(data, pos)
    pos += 4
    unp_size = read_u32(data, pos)
    pos += 4
    host_os = data[pos]
    pos += 1
    data_crc = read_u32(data, pos)
    pos += 4
    file_time = read_u32(data, pos)
    pos += 4
    unp_ver = data[pos]
    pos += 1
    method = data[pos]
    pos += 1
    name_size = read_u16(data, pos)
    pos += 2
    file_attr = read_u32(data, pos)
    pos += 4

    result["pack_size"] = pack_size
    result["unp_size"] = unp_size
    result["host_os"] = host_os
    result["data_crc"] = f"{data_crc:08x}"
    result["file_time"] = f"0x{file_time:08x}"
    result["unp_ver"] = unp_ver
    result["method"] = f"0x{method:02x}"
    result["name_size"] = name_size
    result["file_attr"] = f"0x{file_attr:08x}"

    if pos + name_size <= offset + hsize:
        name_bytes = data[pos:pos + name_size]
        sub_type = name_bytes.decode("ascii", errors="replace")
        result["sub_type"] = sub_type
        result["sub_type_hex"] = name_bytes.hex()
        pos += name_size
        
        # Look up description
        sub_desc = SUB_BLOCK_TYPES.get(sub_type, "Unknown sub-block type")
        result["sub_type_description"] = sub_desc

        # Extract comment data if this is a CMT block
        if sub_type == "CMT" and pack_size > 0:
            data_start = offset + hsize
            data_end = data_start + pack_size
            if data_end <= len(data):
                comment_data = data[data_start:data_end]
                result["comment_data_hex"] = comment_data[:64].hex() + ("..." if len(comment_data) > 64 else "")
                
                if method == 0x30:  # Store (uncompressed)
                    try:
                        comment_text = comment_data.decode("utf-8", errors="replace")
                        result["comment_text"] = comment_text
                    except:
                        result["comment_text"] = "(failed to decode)"
                else:
                    # Compressed - try to extract using unrar
                    if _current_archive_path:
                        comment_text, error = extract_comment_with_unrar(_current_archive_path)
                        if comment_text:
                            result["comment_text"] = comment_text
                        else:
                            result["comment_note"] = f"Compressed (method 0x{method:02x}), extraction failed: {error}"

    return result


def parse_end_block(data, block):
    """Parse RAR archive end block (0x7B)."""
    offset = block["offset"]
    hsize = block["hsize"]
    flags = block["flags"]
    pos = offset + 7

    result = {}

    # Archive end may have a data CRC
    if (flags & 0x0002) != 0:
        if pos + 4 <= offset + hsize:
            data_crc = read_u32(data, pos)
            pos += 4
            result["archive_data_crc"] = f"{data_crc:08x}"

    # Volume number for multi-volume archives
    if pos + 2 <= offset + hsize:
        vol_number = read_u16(data, pos)
        pos += 2
        result["volume_number"] = vol_number

    return result


def print_block(block, extra_fields=None, flag_names=None):
    """Print block header and fields."""
    print(f"\n{'='*60}")
    print(f"Block: {block_name(block['type'])}")
    print(f"{'='*60}")
    print(f"  offset:     0x{block['offset']:08x} ({block['offset']})")
    print(f"  type:       0x{block['type']:02x} ({block_name(block['type'])})")
    print(f"  header_crc: 0x{block['crc']:04x}")
    print(f"  flags:      0x{block['flags']:04x}")
    if flag_names:
        print(f"  flag_names: [{', '.join(flag_names)}]")
    print(f"  hsize:      {block['hsize']}")
    print(f"  add_size:   {block['add_size']}")
    print(f"  total_size: {block['hsize'] + block['add_size']}")

    if extra_fields:
        print(f"\n  --- {block_name(block['type'])} Fields ---")
        for key, value in extra_fields.items():
            print(f"    {key}: {value}")


def parse_rar_file(path):
    """Parse a RAR file and display all headers."""
    global _current_archive_path
    _current_archive_path = path
    data = path.read_bytes()
    offset = 0

    print("=" * 60)
    print(f"RAR File: {path}")
    print(f"Size: {len(data)} bytes")
    print("=" * 60)

    # Check for RAR signature
    if data[:7] == RAR_SIGNATURE:
        print(f"Format: RAR 1.5 - 4.x")
    elif data[:8] == RAR5_SIGNATURE:
        print(f"Format: RAR 5.x (not fully supported by this tool)")
    else:
        print(f"Warning: Unknown or missing RAR signature")
        print(f"First 8 bytes: {data[:8].hex()}")

    block_count = 0

    while offset + 7 <= len(data):
        block = read_base_header(data, offset)
        if not block:
            print(f"\nFailed to read block at offset 0x{offset:08x}")
            break

        block_count += 1
        extra_fields = None
        flag_names = None

        if block["type"] == 0x72:  # Marker
            extra_fields = parse_marker_block(data, offset)
            flag_names = []

        elif block["type"] == 0x73:  # Archive header
            extra_fields = parse_archive_header(data, block)
            flag_names = get_archive_flags(block["flags"])

        elif block["type"] == 0x74:  # File header
            extra_fields = parse_file_header(data, block)
            flag_names = get_file_flags(block["flags"])

        elif block["type"] == 0x7A:  # Sub block
            extra_fields = parse_sub_block(data, block)
            flag_names = get_subblock_flags(block["flags"])

        elif block["type"] == 0x7B:  # Archive end
            extra_fields = parse_end_block(data, block)
            flag_names = get_end_flags(block["flags"])

        else:
            flag_names = []
            if (block["flags"] & FLAG_SKIP_IF_UNKNOWN) != 0:
                flag_names.append("skip_if_unknown")
            if (block["flags"] & FLAG_LONG_BLOCK) != 0:
                flag_names.append("long_block")

        print_block(block, extra_fields, flag_names)

        # Calculate next block offset
        if block["type"] == 0x72:
            # Marker block is always 7 bytes
            offset += 7
        elif block["type"] == 0x74:
            # File header: skip header + packed data
            offset += block["hsize"] + block["add_size"]
        elif block["type"] == 0x7A:
            # Sub block: skip header + data
            offset += block["hsize"] + block["add_size"]
        elif block["type"] == 0x7B:
            # Archive end - stop parsing
            break
        else:
            # Other blocks: header + optional add_size
            if (block["flags"] & FLAG_LONG_BLOCK) != 0:
                offset += block["hsize"] + block["add_size"]
            else:
                offset += block["hsize"]

    print(f"\n{'='*60}")
    print(f"Total blocks parsed: {block_count}")
    if offset < len(data):
        print(f"Remaining data at 0x{offset:08x}: {len(data) - offset} bytes")
    print("=" * 60)


def main():
    parser = argparse.ArgumentParser(
        description="Inspect RAR file headers and display all fields and values."
    )
    parser.add_argument("rar_files", nargs="+", help="Path(s) to RAR files.")
    args = parser.parse_args()

    exit_code = 0
    for rar in args.rar_files:
        path = Path(rar)
        if not path.is_file():
            print(f"{rar}: not found", file=sys.stderr)
            exit_code = 1
            continue
        try:
            parse_rar_file(path)
        except (OSError, ValueError, struct.error) as exc:
            print(f"{rar}: failed to parse ({exc})", file=sys.stderr)
            exit_code = 1
        print("")

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
