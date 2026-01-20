#!/usr/bin/env python3
import struct
import sys

def inspect_rar(filepath):
    with open(filepath, 'rb') as f:
        data = f.read()

    print(f'File size: {len(data)} bytes')
    print()

    # RAR marker (7 bytes)
    print('RAR Marker:', data[:7].hex())

    pos = 7
    block_num = 0
    while pos < len(data):
        block_num += 1
        if pos + 7 > len(data):
            break

        crc = struct.unpack('<H', data[pos:pos+2])[0]
        block_type = data[pos+2]
        flags = struct.unpack('<H', data[pos+3:pos+5])[0]
        header_size = struct.unpack('<H', data[pos+5:pos+7])[0]

        type_names = {
            0x72: 'Marker',
            0x73: 'ArchiveHeader',
            0x74: 'FileHeader',
            0x75: 'Comment',
            0x7A: 'Service',
            0x7B: 'EndArchive'
        }
        type_name = type_names.get(block_type, 'Unknown')

        print(f'Block {block_num}:')
        print(f'  Position: 0x{pos:x} ({pos})')
        print(f'  CRC: 0x{crc:04x}')
        print(f'  Type: 0x{block_type:02x} ({type_name})')
        print(f'  Flags: 0x{flags:04x}')
        print(f'  HeaderSize: {header_size}')

        # Check for ADD_SIZE - present in file headers, service blocks, or LONG_BLOCK flag
        add_size = 0
        has_add_size = block_type in [0x74, 0x7A] or (flags & 0x8000)
        if has_add_size and pos + 7 + 4 <= len(data):
            add_size = struct.unpack('<I', data[pos+7:pos+11])[0]
        print(f'  AddSize: {add_size}')

        # Show header hex
        header_end = min(pos + header_size, len(data))
        header_hex = data[pos:header_end].hex()
        print(f'  Header (hex): {header_hex[:80]}{"..." if len(header_hex) > 80 else ""}')

        # For service blocks, show sub-type
        if block_type == 0x7A and pos + 32 <= len(data):
            # File header structure: base(7) + ADD_SIZE(4) + UNP_SIZE(4) + HOST_OS(1) + FILE_CRC(4) + FTIME(4) + UNP_VER(1) + METHOD(1) + NAME_SIZE(2) = 28
            # Then NAME
            name_size_pos = pos + 7 + 4 + 4 + 1 + 4 + 4 + 1 + 1
            name_size = struct.unpack('<H', data[name_size_pos:name_size_pos+2])[0]
            name_pos = name_size_pos + 2
            if name_pos + name_size <= len(data):
                name = data[name_pos:name_pos+name_size].decode('utf-8', errors='replace')
                print(f'  SubType: {name}')

        # Show data content if it's comment data
        if add_size > 0 and block_type == 0x7A:
            data_start = pos + header_size
            data_end = min(data_start + add_size, len(data))
            data_hex = data[data_start:data_end].hex()
            print(f'  Data (hex): {data_hex[:80]}{"..." if len(data_hex) > 80 else ""}')

        pos += header_size + add_size
        print()

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: python inspect_rar.py <rarfile>")
        sys.exit(1)
    inspect_rar(sys.argv[1])
