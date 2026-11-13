#!/usr/bin/env python3
"""Draws the door stickers: four separate cut-vinyl stickers with no backing (white on clear; the game tints them
   to show on the paint): DRIFT MASTER, SEND IT, 640kFPV, NO FEAR.     usage: make_stickers.py <out dir>
   Lettering: Saira Extra Bold Italic and Sacramento (SIL Open Font License, tools/fonts). Plain words in our own
   lettering: no one's logo."""
import sys, os
from PIL import Image, ImageDraw, ImageFont
out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); W, H = 1024, 512
im = Image.new('RGBA', (W, H), (255, 255, 255, 0)); d = ImageDraw.Draw(im)
saira = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Saira-ExtraBoldItalic.ttf'), s)
script = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Sacramento-Regular.ttf'), s)
def fit(text, font, width, start):
    s = start
    while True:
        f = font(s); bb = d.textbbox((0, 0), text, font=f)
        if bb[2] - bb[0] <= width: return f, bb
        s -= 2
white = (255, 255, 255, 255)
# 1  DRIFT MASTER: the big one across the top, with a bar under it that breaks into speed lines
f, bb = fit('DRIFT MASTER', saira, 960, 150); d.text((32 - bb[0], 14 - bb[1]), 'DRIFT MASTER', font=f, fill=white); y = 14 + bb[3] - bb[1] + 12
d.polygon([(40, y), (700, y), (690, y + 14), (30, y + 14)], fill=white)
for k, x in enumerate((720, 800, 868, 924)): d.polygon([(x, y), (x + 60 - k * 12, y), (x + 50 - k * 12, y + 14), (x - 10, y + 14)], fill=white)
# 2  SEND IT: script, leaning up, with an underline flick
f, bb = fit('Send it', script, 400, 190); lay = Image.new('RGBA', (460, 220), (255, 255, 255, 0)); ld = ImageDraw.Draw(lay)
ld.text((20 - bb[0], 10 - bb[1]), 'Send it', font=f, fill=white, stroke_width=3, stroke_fill=white); ld.line([(40, 190), (200, 180), (410, 150)], fill=white, width=9, joint='curve')
im.alpha_composite(lay.rotate(5, resample=Image.BICUBIC, expand=False), (20, 190))
# 3  640kFPV: outline letters only
f, bb = fit('640kFPV', saira, 470, 130); d.text((520 - bb[0], 215 - bb[1]), '640kFPV', font=f, fill=(255, 255, 255, 0), stroke_width=5, stroke_fill=white)
# 4  NO FEAR: wide block capitals between two rules
f, bb = fit('NO FEAR', saira, 520, 110); x0 = 470; yy = 380
d.text((x0 - bb[0], yy - bb[1]), 'NO FEAR', font=f, fill=white); wdt = bb[2] - bb[0]; hgt = bb[3] - bb[1]
d.rectangle((x0 - 6, yy - 18, x0 + wdt + 10, yy - 10), fill=white); d.rectangle((x0 - 16, yy + hgt + 12, x0 + wdt, yy + hgt + 20), fill=white)
im.save(os.path.join(out, 'side_stickers.png'), optimize=True); print('ok')
