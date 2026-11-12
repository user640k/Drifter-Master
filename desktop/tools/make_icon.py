#!/usr/bin/env python3
"""Builds the game's icon: the sedan above "DriftMaster" in white, no background.
   usage: make_icon.py <project dir>     writes icon.png (1024), icon_256.png and icon.ico (256 down to 16);
   tools/stamp_icon.py then puts it into the exported exe
   tools/icon_car.png is the game's own sedan, rendered alone by the game:
     DriftMaster -- --bare --at=60 --paint=3 --orbit=38,16,1.15,30 --steer=0.55 --shot=icon_car.png --frames=12
   Lettering: Saira Extra Bold Italic (SIL Open Font License, tools/fonts)."""
import sys, os
from PIL import Image, ImageDraw, ImageFilter, ImageFont
proj = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); N = 1024
car = Image.open(os.path.join(here, 'icon_car.png')).convert('RGBA')
car = car.crop(car.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox()); s = 900 / car.width; car = car.resize((900, int(car.height * s)), Image.LANCZOS)
out = Image.new('RGBA', (N, N), (0, 0, 0, 0)); x = (N - car.width) // 2; y = int(430 - car.height / 2)
sh = Image.new('L', (N, N), 0); ImageDraw.Draw(sh).ellipse((x + car.width * 0.06, y + car.height * 0.74, x + car.width * 0.94, y + car.height * 1.03), fill=110); sh = sh.filter(ImageFilter.GaussianBlur(26))
out.alpha_composite(Image.merge('RGBA', [Image.new('L', (N, N), 0)] * 3 + [sh])); out.alpha_composite(car, (x, y))
d = ImageDraw.Draw(out); size = 150
while True:
    f = ImageFont.truetype(os.path.join(here, 'fonts', 'Saira-ExtraBoldItalic.ttf'), size); bb = d.textbbox((0, 0), 'DriftMaster', font=f)
    if bb[2] - bb[0] <= N * 0.86: break
    size -= 2
d.text(((N - (bb[2] - bb[0])) / 2 - bb[0], 760 - bb[1]), 'DriftMaster', font=f, fill=(255, 255, 255, 255))
out.save(os.path.join(proj, 'icon.png')); out.resize((256, 256), Image.LANCZOS).save(os.path.join(proj, 'icon_256.png'))
# The .ico is written by hand: 32-bit bitmaps WITH the 1-bit mask after the pixels for 16 to 128 px, PNG for 256 px.
# (An icon library wrote the bitmaps without the mask, and Windows then ignored the icon.)
import io, struct
def bmp(n):
    a = out.resize((n, n), Image.LANCZOS); px = a.tobytes('raw', 'BGRA'); rows = [px[y * n * 4:(y + 1) * n * 4] for y in range(n)][::-1]
    mrow = ((n + 31) // 32) * 4
    return struct.pack('<IiiHHIIiiII', 40, n, n * 2, 1, 32, 0, n * n * 4 + mrow * n, 0, 0, 0, 0) + b''.join(rows) + bytes(mrow * n)
def png(n):
    o = io.BytesIO(); out.resize((n, n), Image.LANCZOS).save(o, 'PNG', optimize=True); return o.getvalue()
imgs = [(n, bmp(n)) for n in (16, 24, 32, 48, 64, 128)] + [(256, png(256))]
off = 6 + 16 * len(imgs); ent = b''; body = b''
for n, d in imgs:
    ent += struct.pack('<BBBBHHII', n % 256, n % 256, 0, 0, 1, 32, len(d), off + len(body)); body += d
open(os.path.join(proj, 'icon.ico'), 'wb').write(struct.pack('<HHH', 0, 1, len(imgs)) + ent + body)
print('ok')
