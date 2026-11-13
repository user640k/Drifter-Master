#!/usr/bin/env python3
"""Draws the door stickers: four separate cut-vinyl stickers with no backing, stacked in a column the way sponsor
   decals run down a door under the mirror: DRIFT MASTER, SEND IT, 640kFPV, NO FEAR. White with a thin black
   contour, whatever the paint.     usage: make_stickers.py <out dir>
   Lettering: Saira Extra Bold Italic and Sacramento (SIL Open Font License, tools/fonts). Plain words in our own
   lettering: no one's logo."""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter
out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); W, H = 640, 896
im = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im)   # the white shapes first; the contour is grown from them after
saira = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Saira-ExtraBoldItalic.ttf'), s)
script = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Sacramento-Regular.ttf'), s)
def fit(text, font, width, start, dd=None):
    s = start
    while True:
        f = font(s); bb = (dd or d).textbbox((0, 0), text, font=f)
        if bb[2] - bb[0] <= width: return f, bb
        s -= 2
def centred(text, font, width, start, y):
    f, bb = fit(text, font, width, start); x = (W - (bb[2] - bb[0])) // 2; d.text((x - bb[0], y - bb[1]), text, font=f, fill=255); return x, bb[2] - bb[0], bb[3] - bb[1]
# 1  DRIFT MASTER: two lines, with a bar under them that breaks into speed lines
x, w, h = centred('DRIFT', saira, 520, 150, 34); x, w, h2 = centred('MASTER', saira, 560, 150, 34 + h + 22); y = 34 + h + 22 + h2 + 22
d.polygon([(x + 10, y), (x + w * 0.66, y), (x + w * 0.66 - 10, y + 16), (x, y + 16)], fill=255)
for k, fx in enumerate((0.70, 0.80, 0.88, 0.95)): xx = x + w * fx; d.polygon([(xx, y), (xx + 40 - k * 9, y), (xx + 30 - k * 9, y + 16), (xx - 10, y + 16)], fill=255)
# 2  SEND IT: script, leaning up, with an underline flick
lay = Image.new('L', (W, 260), 0); ld = ImageDraw.Draw(lay); f, bb = fit('Send it', script, 470, 230, ld); lx = (W - (bb[2] - bb[0])) // 2
ld.text((lx - bb[0], 20 - bb[1]), 'Send it', font=f, fill=255, stroke_width=4, stroke_fill=255); ld.line([(lx + 10, 212), (lx + 190, 202), (lx + 470, 170)], fill=255, width=11, joint='curve')
im.paste(255, (0, 300), lay.rotate(4, resample=Image.BICUBIC))
# 3  640kFPV: a plate of slanted block letters
centred('640kFPV', saira, 540, 130, 590)
# 4  NO FEAR: block capitals between two rules
x, w, h = centred('NO FEAR', saira, 470, 110, 748); d.rectangle((x - 4, 748 - 30, x + w + 10, 748 - 18), fill=255); d.rectangle((x - 14, 748 + h + 18, x + w, 748 + h + 30), fill=255)
# the black contour: the shapes grown by a few pixels, under the white
edge = im.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(1.2)); res = Image.new('RGBA', (W, H), (0, 0, 0, 0))
res.putalpha(edge); res.paste((255, 255, 255, 255), (0, 0), im.filter(ImageFilter.GaussianBlur(0.6)))
a = Image.composite(Image.new('L', (W, H), 255), edge, im.point(lambda v: 255 if v > 127 else 0)); res.putalpha(a)
res.save(os.path.join(out, 'side_stickers.png'), optimize=True); print('ok')
