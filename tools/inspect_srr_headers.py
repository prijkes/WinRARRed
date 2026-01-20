#!/usr/bin/env python3
import argparse
from pathlib import Path
import struct
import sys
import subprocess
import shutil
import tempfile
from datetime import datetime

SRR_BLOCK_TYPES = {0x69, 0x6A, 0x6B, 0x6C, 0x71}

BLOCK_NAMES = {
    0x69: "srr_header",
    0x6A: "srr_stored_file",
    0x6B: "srr_oso_hash",
    0x6C: "srr_rar_padding",
    0x71: "srr_rar_file",
    0x72: "rar_marker",
    0x73: "rar_archive_header",
    0x74: "rar_file",
    0x75: "rar_old_comment",
    0x76: "rar_old_auth",
    0x77: "rar_old_sub",
    0x78: "rar_old_recovery",
    0x79: "rar_old_auth2",
    0x7A: "rar_new_sub",
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

LHD_LARGE = 0x0100
LHD_UNICODE = 0x0200
FLAG_LONG_BLOCK = 0x8000


def find_unrar():
    """Find unrar executable."""
    unrar_paths = [
        shutil.which('unrar'),
        shutil.which('UnRAR'),
        r'C:Program FilesWinRARUnRAR.exe',
        r'C:Program Files (x86)WinRARUnRAR.exe',
        str(Path(__file__).parent.parent / 'unrar' / 'unrar.exe'),
        str(Path(__file__).parent.parent / 'unrar' / 'unrar'),
    ]
    
    for p in unrar_paths:
        if p and Path(p).is_file():
            return p
    return None


def extract_comment_from_rar_data(rar_data):
    """Extract comment by creating a temp RAR file and using unrar or rarfile library."""
    # Create a temporary RAR file first (needed for both methods)
    try:
        with tempfile.NamedTemporaryFile(suffix='.rar', delete=False) as tmp:
            tmp.write(rar_data)
            tmp_path = tmp.name
    except Exception as e:
        return None, f"Failed to create temp file: {e}"
    
    try:
        # First, try using rarfile library
        try:
            import rarfile
            with rarfile.RarFile(tmp_path) as rf:
                comment = rf.comment
                if comment:
                    return comment, None
        except ImportError:
            pass  # rarfile not installed
        except Exception:
            pass  # rarfile failed, try unrar
        
        # Try unrar executable
        unrar_exe = find_unrar()
        if not unrar_exe:
            return None, "unrar not found (install WinRAR or run: pip install rarfile)"
        
        try:
            result = subprocess.run(
                [unrar_exe, 'cw', '-inul', tmp_path],
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
    finally:
        # Clean up temp file
        try:
            Path(tmp_path).unlink()
        except:
            pass


def read_u16(data, offset):
    return struct.unpack_from("<H", data, offset)[0]


def read_u32(data, offset):
    return struct.unpack_from("<I", data, offset)[0]


def read_base_header(data, offset):
    if offset + 7 > len(data):
        return None
    crc, btype, flags, hsize = struct.unpack_from("<HBHH", data, offset)
    if hsize < 7:
        return None
    add_size = 0
    if (flags & FLAG_LONG_BLOCK) != 0 or btype in (0x6A, 0x74, 0x7A):
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


def block_name(btype):
    return BLOCK_NAMES.get(btype, f"0x{btype:02x}")


def decode_rar_unicode(std_name, enc_data):
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
    if (flags & LHD_UNICODE) != 0:
        try:
            null_index = name_bytes.index(0)
        except ValueError:
            null_index = -1
        if null_index >= 0:
            std_name = name_bytes[:null_index]
            enc_data = name_bytes[null_index + 1 :]
            if enc_data:
                return decode_rar_unicode(std_name, enc_data)
            return std_name.decode("cp437", errors="replace")
        try:
            return name_bytes.decode("utf-8")
        except UnicodeDecodeError:
            return name_bytes.decode("cp437", errors="replace")
    return name_bytes.decode("cp437", errors="replace")


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


def get_flag_names(block):
    """Get human-readable flag names for a block."""
    flag_names = []
    flags = block["flags"]
    btype = block["type"]

    if btype == 0x74:  # rar_file
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
        dict_flag = flags & 0x00E0
        dict_map = {
            0x0000: "dict64",
            0x0020: "dict128",
            0x0040: "dict256",
            0x0060: "dict512",
            0x0080: "dict1024",
            0x00A0: "dict2048",
            0x00C0: "dict4096",
            0x00E0: "dir_entry",
        }
        if dict_flag in dict_map:
            flag_names.append(dict_map[dict_flag])
    elif btype == 0x73:  # rar_archive_header
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
    elif btype == 0x69:  # srr_header
        if (flags & 0x0001) != 0:
            flag_names.append("has_app_name")
    elif btype == 0x6A:  # srr_stored_file
        if (flags & 0x8000) != 0:
            flag_names.append("long_block")
    elif btype == 0x71:  # srr_rar_file
        if (flags & 0x0001) != 0:
            flag_names.append("has_path_separator")
    elif btype == 0x7B:  # rar_archive_end
        if (flags & 0x0001) != 0:
            flag_names.append("next_volume")
        if (flags & 0x0002) != 0:
            flag_names.append("data_crc_present")
        if (flags & 0x0004) != 0:
            flag_names.append("rev_space")

    if (flags & 0x8000) != 0:
        flag_names.append("long_block")

    return flag_names


def parse_rar_archive_header(data, block):
    """Parse RAR archive header (0x73) fields."""
    offset = block["offset"]
    hsize = block["hsize"]
    pos = offset + 7

    result = {}

    # Reserved fields (if present)
    if pos + 6 <= offset + hsize:
        reserved1 = read_u16(data, pos)
        pos += 2
        reserved2 = read_u32(data, pos)
        pos += 4
        result["reserved1"] = f"0x{reserved1:04x}"
        result["reserved2"] = f"0x{reserved2:08x}"

    return result


def parse_rar_file_header(data, block):
    offset = block["offset"]
    flags = block["flags"]
    hsize = block["hsize"]
    header_end = offset + hsize
    pos = offset + 7

    if pos + 25 > header_end:
        return None

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

    name_bytes = data[pos : pos + name_size]
    file_name = decode_name(name_bytes, flags)

    return {
        "file_name": file_name,
        "file_crc": f"{file_crc:08x}",
        "pack_size": pack_size,
        "unp_size": unp_size,
        "host_os": host_os,
        "host_os_name": HOST_OS_NAMES.get(host_os, f"Unknown({host_os})"),
        "unp_ver": unp_ver,
        "unp_ver_str": f"{unp_ver // 10}.{unp_ver % 10}",
        "method": method,
        "method_name": METHOD_NAMES.get(method, f"Unknown(0x{method:02x})"),
        "file_attr": file_attr,
        "file_attr_hex": f"0x{file_attr:08x}",
        "file_time": file_time,
        "file_time_str": decode_dos_datetime(file_time),
        "name_size": name_size,
        "high_pack": high_pack,
        "high_unp": high_unp,
    }


def is_recovery_newsub(data, block):
    offset = block["offset"]
    hsize = block["hsize"]
    if hsize <= 34:
        return False
    if offset + 34 > len(data):
        return False
    name_len = read_u16(data, offset + 26)
    if name_len != 2:
        return False
    return data[offset + 32 : offset + 34] == b"RR"


def parse_rar_sub_block(data, block, rar_start_offset=None):
    """Parse RAR sub-block (0x7A) and extract sub-type."""
    offset = block["offset"]
    hsize = block["hsize"]
    flags = block["flags"]
    pos = offset + 7

    result = {}

    if pos + 25 > offset + hsize:
        return result

    # Sub-block has similar structure to file header
    pack_size = read_u32(data, pos)
    pos += 4
    unp_size = read_u32(data, pos)
    pos += 4
    host_os = data[pos]
    pos += 1
    data_crc = read_u32(data, pos)
    pos += 4
    sub_time = read_u32(data, pos)
    pos += 4
    unp_ver = data[pos]
    pos += 1
    method = data[pos]
    pos += 1
    name_size = read_u16(data, pos)
    pos += 2
    sub_attr = read_u32(data, pos)
    pos += 4

    # Handle large file flag
    if (flags & 0x0100) != 0:
        if pos + 8 <= offset + hsize:
            high_pack = read_u32(data, pos)
            high_unp = read_u32(data, pos + 4)
            pos += 8
            pack_size += high_pack << 32
            unp_size += high_unp << 32

    result["pack_size"] = pack_size
    result["unp_size"] = unp_size
    result["host_os"] = host_os
    result["host_os_name"] = HOST_OS_NAMES.get(host_os, f"Unknown({host_os})")
    result["data_crc"] = f"{data_crc:08x}"
    result["sub_time"] = f"0x{sub_time:08x}"
    result["unp_ver"] = unp_ver
    result["method"] = f"0x{method:02x}"
    result["sub_attr"] = f"0x{sub_attr:08x}"
    result["name_size"] = name_size

    # Extract sub-type name
    if pos + name_size <= offset + hsize:
        sub_type_bytes = data[pos:pos + name_size]
        sub_type = sub_type_bytes.decode("ascii", errors="replace")
        result["sub_type"] = sub_type
        result["sub_type_hex"] = sub_type_bytes.hex()
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
                    # Compressed - try to extract using unrar on the RAR section
                    if rar_start_offset is not None:
                        # Extract RAR data from start to end of this sub-block
                        rar_end = data_end
                        rar_section = data[rar_start_offset:rar_end]
                        comment_text, error = extract_comment_from_rar_data(rar_section)
                        if comment_text:
                            result["comment_text"] = comment_text
                        else:
                            result["comment_note"] = f"Compressed (method 0x{method:02x}), extraction failed: {error}"

    return result


def rar_block_total_size(data, block):
    btype = block["type"]
    add_size = block["add_size"]
    hsize = block["hsize"]
    is_recovery = btype == 0x78 or (btype == 0x7A and is_recovery_newsub(data, block))
    if btype == 0x74 or is_recovery:
        return hsize
    return hsize + add_size


def rar_block_on_disk_size(block, file_info=None):
    btype = block["type"]
    if btype == 0x74 and file_info is not None:
        return block["hsize"] + file_info["pack_size"]
    if btype in (0x74, 0x7A, 0x78):
        return block["hsize"] + block["add_size"]
    if (block["flags"] & FLAG_LONG_BLOCK) != 0:
        return block["hsize"] + block["add_size"]
    return block["hsize"]


def parse_srr_header(data, block):
    if (block["flags"] & 0x0001) == 0:
        return None
    offset = block["offset"]
    hsize = block["hsize"]
    if offset + 9 > len(data) or offset + hsize > len(data):
        return None
    name_len = read_u16(data, offset + 7)
    name_start = offset + 9
    name_end = name_start + name_len
    if name_end > offset + hsize:
        return None
    return data[name_start:name_end].decode("utf-8", errors="replace")


def parse_srr_name(data, block, has_add_size):
    offset = block["offset"]
    hsize = block["hsize"]
    base = offset + 7 + (4 if has_add_size else 0)
    if base + 2 > len(data) or offset + hsize > len(data):
        return None
    name_len = read_u16(data, base)
    name_start = base + 2
    name_end = name_start + name_len
    if name_end > offset + hsize:
        return None
    return data[name_start:name_end].decode("utf-8", errors="replace")


def parse_srr_oso_hash(data, block):
    """Parse OSO hash block (0x6B)."""
    offset = block["offset"]
    hsize = block["hsize"]
    pos = offset + 7

    if pos + 16 > offset + hsize:
        return None

    file_size = struct.unpack_from("<Q", data, pos)[0]
    pos += 8
    oso_hash = struct.unpack_from("<Q", data, pos)[0]
    pos += 8

    # Name follows
    if pos + 2 <= offset + hsize:
        name_len = read_u16(data, pos)
        pos += 2
        if pos + name_len <= offset + hsize:
            name = data[pos:pos + name_len].decode("utf-8", errors="replace")
            return {
                "file_size": file_size,
                "oso_hash": f"{oso_hash:016x}",
                "file_name": name,
            }

    return {
        "file_size": file_size,
        "oso_hash": f"{oso_hash:016x}",
    }


def parse_srr_rar_padding(data, block):
    """Parse RAR padding block (0x6C)."""
    offset = block["offset"]
    hsize = block["hsize"]
    add_size = block["add_size"]

    return {
        "padding_size": add_size,
    }


def print_block(indent, block, extra=None, rar_relative_offset=None):
    flag_names = get_flag_names(block)

    print(f"{indent}--- Block: {block_name(block['type'])} ---")
    print(f"{indent}  offset:     0x{block['offset']:08x} ({block['offset']})")
    if rar_relative_offset is not None:
        print(f"{indent}  rar_offset: 0x{rar_relative_offset:08x} ({rar_relative_offset})")
    print(f"{indent}  type:       0x{block['type']:02x} ({block_name(block['type'])})")
    print(f"{indent}  crc:        0x{block['crc']:04x}")
    print(f"{indent}  flags:      0x{block['flags']:04x}")
    if flag_names:
        print(f"{indent}  flag_names: [{', '.join(flag_names)}]")
    print(f"{indent}  hsize:      {block['hsize']}")
    print(f"{indent}  add_size:   {block['add_size']}")
    if extra:
        print(f"{indent}  {extra}")


def print_rar_file_info(indent, file_info):
    """Print all RAR file header fields."""
    print(f"{indent}  --- RAR File Fields ---")
    print(f"{indent}    file_name:    {file_info['file_name']}")
    print(f"{indent}    file_crc:     {file_info['file_crc']}")
    print(f"{indent}    pack_size:    {file_info['pack_size']}")
    print(f"{indent}    unp_size:     {file_info['unp_size']}")
    print(f"{indent}    host_os:      {file_info['host_os']} ({file_info['host_os_name']})")
    print(f"{indent}    unp_ver:      {file_info['unp_ver']} (RAR {file_info['unp_ver_str']})")
    print(f"{indent}    method:       0x{file_info['method']:02x} ({file_info['method_name']})")
    print(f"{indent}    file_attr:    {file_info['file_attr_hex']}")
    print(f"{indent}    file_time:    0x{file_info['file_time']:08x} ({file_info['file_time_str']})")
    print(f"{indent}    name_size:    {file_info['name_size']}")
    if file_info['high_pack'] or file_info['high_unp']:
        print(f"{indent}    high_pack:    {file_info['high_pack']}")
        print(f"{indent}    high_unp:     {file_info['high_unp']}")


def parse_rar_blocks(data, offset, indent):
    total_size = 0
    rar_start_offset = offset  # Remember where RAR section starts
    while offset + 7 <= len(data):
        btype = data[offset + 2]
        if btype in SRR_BLOCK_TYPES:
            break
        block = read_base_header(data, offset)
        if not block:
            break
        # Calculate relative offset within RAR section
        rar_relative_offset = offset - rar_start_offset
        print_block(indent, block, rar_relative_offset=rar_relative_offset)
        file_info = None

        if block["type"] == 0x73:  # rar_archive_header
            archive_info = parse_rar_archive_header(data, block)
            if archive_info:
                print(f"{indent}  --- RAR Archive Header Fields ---")
                for key, value in archive_info.items():
                    print(f"{indent}    {key}: {value}")

        elif block["type"] == 0x74:  # rar_file
            file_info = parse_rar_file_header(data, block)
            if file_info:
                print_rar_file_info(indent, file_info)

        elif block["type"] == 0x7A:  # rar_new_sub (sub-block)
            sub_info = parse_rar_sub_block(data, block, rar_start_offset)
            if sub_info:
                print(f"{indent}  --- RAR Sub-Block Fields ---")
                for key, value in sub_info.items():
                    print(f"{indent}    {key}: {value}")

        elif block["type"] == 0x7B:  # rar_archive_end
            print(f"{indent}  --- RAR Archive End ---")

        total_size += rar_block_on_disk_size(block, file_info)
        offset = offset + rar_block_total_size(data, block)
    return offset, total_size


def parse_srr_file(path):
    data = path.read_bytes()
    offset = 0
    print("=" * 60)
    print(f"SRR File: {path}")
    print(f"Size: {len(data)} bytes")
    print("=" * 60)

    while offset + 7 <= len(data):
        block = read_base_header(data, offset)
        if not block:
            break
        if block["type"] not in SRR_BLOCK_TYPES:
            print(f"Unexpected block at 0x{offset:08x} type=0x{block['type']:02x}")
            break

        print_block("[SRR] ", block)

        if block["type"] == 0x69:  # srr_header
            app_name = parse_srr_header(data, block)
            print(f"[SRR]   --- SRR Header Fields ---")
            if app_name:
                print(f"[SRR]     app_name: {app_name}")
            else:
                print(f"[SRR]     app_name: (none)")

        elif block["type"] == 0x6A:  # srr_stored_file
            name = parse_srr_name(data, block, has_add_size=True)
            print(f"[SRR]   --- SRR Stored File Fields ---")
            print(f"[SRR]     file_name: {name}")
            print(f"[SRR]     file_size: {block['add_size']}")

        elif block["type"] == 0x6B:  # srr_oso_hash
            oso_info = parse_srr_oso_hash(data, block)
            print(f"[SRR]   --- SRR OSO Hash Fields ---")
            if oso_info:
                for key, value in oso_info.items():
                    print(f"[SRR]     {key}: {value}")

        elif block["type"] == 0x6C:  # srr_rar_padding
            padding_info = parse_srr_rar_padding(data, block)
            print(f"[SRR]   --- SRR RAR Padding Fields ---")
            if padding_info:
                for key, value in padding_info.items():
                    print(f"[SRR]     {key}: {value}")

        elif block["type"] == 0x71:  # srr_rar_file
            name = parse_srr_name(data, block, has_add_size=False)
            print(f"[SRR]   --- SRR RAR File Fields ---")
            print(f"[SRR]     rar_file_name: {name}")

        block_end = offset + block["hsize"] + block["add_size"]
        offset = block_end

        if block["type"] == 0x71:
            print(f"[SRR]   --- Embedded RAR Headers ---")
            offset, total_size = parse_rar_blocks(data, offset, "  [RAR] ")
            print(f"[SRR]   rar_file_total_on_disk_size: {total_size}")

        print()  # Blank line between blocks

    if offset < len(data):
        print(f"Trailing data at 0x{offset:08x} ({len(data) - offset} bytes)")


def main():
    parser = argparse.ArgumentParser(
        description="Inspect SRR headers and embedded RAR headers with full field details."
    )
    parser.add_argument("srr_files", nargs="+", help="Path(s) to SRR files.")
    args = parser.parse_args()

    exit_code = 0
    for srr in args.srr_files:
        path = Path(srr)
        if not path.is_file():
            print(f"{srr}: not found", file=sys.stderr)
            exit_code = 1
            continue
        try:
            parse_srr_file(path)
        except (OSError, ValueError, struct.error) as exc:
            print(f"{srr}: failed to parse ({exc})", file=sys.stderr)
            exit_code = 1
        print("")

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
