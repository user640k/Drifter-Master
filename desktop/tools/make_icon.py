#!/usr/bin/env python3
"""Builds the game's icon: the sedan above "DriftMaster" in white, no background.
   usage: make_icon.py <project dir>     writes icon.png (1024), icon_256.png and icon.ico (256 down to 16)
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
# plain bitmaps inside the .ico, not PNG: Windows Explorer and the window icon loader only take PNG for the 256 px
# entry, and showed no icon at all when the small ones were PNG too
out.save(os.path.join(proj, 'icon.ico'), sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)], bitmap_format='bmp')
print('ok')
