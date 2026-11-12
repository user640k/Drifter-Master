#!/usr/bin/env python3
"""Puts the game's icon into an exported DriftMaster.exe, from Linux, without Windows tools.
   usage: stamp_icon.py <exe> <icon.png (square, 1024)>
   The exported exe still carries Godot's icon: six images (16, 32, 48, 64, 128 as bitmaps, 256 as PNG) in one icon
   group. Each of ours is written over the one of the same size, in exactly that form: 32-bit bitmaps WITH the 1-bit
   mask Windows insists on after the pixels (an icon library left it out, and Windows then ignored the icon), and
   the 256 px one as PNG. Ours are never larger than Godot's, so nothing in the file moves; only the six images,
   their sizes in the resource table and the sizes in the icon group change."""
import sys, io, struct
import pefile
from PIL import Image
exe, src = sys.argv[1], sys.argv[2]
im = Image.open(src).convert('RGBA')
def bmp(n):
    a = im.resize((n, n), Image.LANCZOS); px = a.tobytes('raw', 'BGRA'); rows = [px[y * n * 4:(y + 1) * n * 4] for y in range(n)][::-1]  # bottom row first
    mrow = ((n + 31) // 32) * 4                                                   # mask rows are padded to 4 bytes
    hdr = struct.pack('<IiiHHIIiiII', 40, n, n * 2, 1, 32, 0, n * n * 4 + mrow * n, 0, 0, 0, 0)
    return hdr + b''.join(rows) + bytes(mrow * n)                                 # mask all 0: the alpha channel decides
def png(n, limit):
    a = im.resize((n, n), Image.LANCZOS)
    for bits in (8, 7, 6, 5):                                                     # fewer colour steps until it fits Godot's slot
        b = a if bits == 8 else Image.merge('RGBA', [c.point(lambda v, s=8 - bits: (v >> s) << s) for c in a.split()])
        o = io.BytesIO(); b.save(o, 'PNG', optimize=True, compress_level=9)
        if len(o.getvalue()) <= limit: return o.getvalue()
    raise SystemExit('256 px icon does not fit')
pe = pefile.PE(exe); raw = bytearray(open(exe, 'rb').read()); icons = {}; group = None
for e in pe.DIRECTORY_ENTRY_RESOURCE.entries:
    for n in e.directory.entries:
        for l in n.directory.entries:
            if e.id == 3: icons[n.id] = l.data.struct
            if e.id == 14 and group is None: group = l.data.struct
goff = pe.get_offset_from_rva(group.OffsetToData); g = raw[goff:goff + group.Size]; cnt = struct.unpack_from('<H', g, 4)[0]
for i in range(cnt):
    w, h, cc, r, planes, bpp, size, iid = struct.unpack_from('<BBBBHHIH', g, 6 + 14 * i); n = w or 256; st = icons[iid]
    off = pe.get_offset_from_rva(st.OffsetToData); old = raw[off:off + st.Size]; new = png(n, st.Size) if old[:4] == b'\x89PNG' else bmp(n)
    assert len(new) <= st.Size, (n, len(new), st.Size)
    raw[off:off + st.Size] = new + bytes(st.Size - len(new))                      # the image, in the same place
    struct.pack_into('<I', raw, st.get_file_offset() + 4, len(new))               # its size in the resource table (after the RVA)
    struct.pack_into('<BBBBHHIH', raw, goff + 6 + 14 * i, w, h, 0, 0, 1, 32, len(new), iid)   # and in the icon group
    print('  %3d px: %s, %d bytes' % (n, 'PNG' if new[:4] == b'\x89PNG' else 'bitmap + mask', len(new)))
pe.close(); open(exe, 'wb').write(raw); print('stamped', exe)
