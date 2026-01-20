import struct
import sys
from pathlib import Path


def read_base_header(data, offset):
    # Returns (crc, type, flags, head_size, add_size, next_offset)
    crc, typ, flags, head_size = struct.unpack_from("<HBHH", data, offset)
    pos = offset + 7
    add_size = 0
    if flags & 0x8000:  # LONG_BLOCK
        add_size = struct.unpack_from("<I", data, pos)[0]
        pos += 4
    next_offset = offset + head_size + add_size
    return crc, typ, flags, head_size, add_size, next_offset


def decode_rar_file_header(data, offset, add_size):
    # Assumes we've already read the 7-byte base header and optional ADD_SIZE (pos at offset+7 or offset+11)
    crc, typ, flags, head_size = struct.unpack_from("<HBHH", data, offset)
    pos = offset + 7  # start right after the base header (CRC, type, flags, head_size)

    # Field layout for RAR 3.x FILE header: pack size is always present immediately after the base header.
    pack_size_field = struct.unpack_from("<I", data, pos)[0]; pos += 4
    pack_size = add_size or pack_size_field

    unp_size = struct.unpack_from("<I", data, pos)[0]; pos += 4
    host_os = data[pos]; pos += 1
    file_crc = struct.unpack_from("<I", data, pos)[0]; pos += 4
    file_time = struct.unpack_from("<I", data, pos)[0]; pos += 4
    unp_ver = data[pos]; pos += 1
    method = data[pos]; pos += 1
    name_size = struct.unpack_from("<H", data, pos)[0]; pos += 2
    file_attr = struct.unpack_from("<I", data, pos)[0]; pos += 4

    if flags & 0x0100:  # LHD_LARGE
        high_pack, high_unp = struct.unpack_from("<II", data, pos)
        pos += 8
        pack_size |= high_pack << 32
        unp_size |= high_unp << 32

    name_bytes = data[pos:pos + name_size]
    pos += name_size

    # Prefer header flag for directory detection; file_attr is unreliable in SRR-only headers.
    is_directory = (flags & 0x00E0) == 0x00E0
    name = name_bytes.decode("utf-8", errors="replace")
    return {
        "name": name,
        "is_dir": is_directory,
        "pack_size": pack_size,
        "unp_size": unp_size,
        "flags": flags,
        "file_attr": file_attr,
    }


def list_srr_entries(srr_path: Path):
    data = srr_path.read_bytes()
    offset = 0
    files = []
    dirs = []
    stored = []

    while offset < len(data):
        if offset + 7 > len(data):
            break
        crc, typ, flags, head_size, add_size, next_offset = read_base_header(data, offset)
        if typ == 0x6A:  # srr_stored_file
            # Stored file: [Base 7][ADD_SIZE 4][NameLen 2][Name][Data...]
            pos = offset + 7
            if flags & 0x8000:
                pos += 4  # skip ADD_SIZE already accounted for
            if pos + 2 <= offset + head_size:
                name_len = struct.unpack_from("<H", data, pos)[0]; pos += 2
                name = data[pos:pos + name_len].decode("utf-8", errors="replace")
                stored.append(name)
            offset = next_offset
            continue

        if typ == 0x71:  # srr_rar_file
            # name length stored in header after base header: 2 bytes name len + name
            pos = offset + 7
            name_len = struct.unpack_from("<H", data, pos)[0]; pos += 2
            rar_name = data[pos:pos + name_len].decode("utf-8", errors="replace")
            # RAR blocks start immediately after this header
            rar_offset = offset + head_size
            # Walk RAR blocks until we reach the next SRR block
            rpos = rar_offset
            while rpos + 7 <= len(data):
                r_crc, r_typ, r_flags, r_head_size, r_add, r_next = read_base_header(data, rpos)
                if 0x69 <= r_typ <= 0x71:  # next SRR block; stop
                    break
                if r_typ == 0x74:  # file header
                    entry = decode_rar_file_header(data, rpos, r_add)
                    if entry["is_dir"]:
                        dirs.append(entry["name"])
                    else:
                        files.append(entry["name"])
                # In SRR files, packed data bytes are not present, so advance by header size for FILE blocks.
                r_next = rpos + r_head_size if r_typ == 0x74 else r_next
                # Stop if parsing would loop
                if r_next <= rpos:
                    break
                rpos = r_next
            # Continue SRR scanning from where we stopped inside the RAR stream
            offset = rpos
            continue
        offset = next_offset

    stored = list(dict.fromkeys(stored))
    files = list(dict.fromkeys(files))
    dirs = list(dict.fromkeys(dirs))

    print(f"Stored files ({len(stored)}):")
    for s in stored:
        print(f"  {s}")
    print(f"Files ({len(files)}):")
    for f in files:
        print(f"  {f}")
    print(f"Dirs ({len(dirs)}):")
    for d in dirs:
        print(f"  {d}")


def main():
    if len(sys.argv) != 2:
        print("Usage: python list_srr_entries.py <file.srr>")
        sys.exit(1)
    list_srr_entries(Path(sys.argv[1]))


if __name__ == "__main__":
    main()
