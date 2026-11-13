#!/usr/bin/env python3
"""Draws the door stickers: four separate cut-vinyl stickers with no backing, stacked in a column the way sponsor
   decals run down a door under the mirror: DRIFT MASTER, Send it, 640kFPV, SEND IT. White with a thin black
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
# 4  SEND IT again, in block capitals between two rules
x, w, h = centred('SEND IT', saira, 470, 110, 748); d.rectangle((x - 4, 748 - 30, x + w + 10, 748 - 18), fill=255); d.rectangle((x - 14, 748 + h + 18, x + w, 748 + h + 30), fill=255)
# the black contour: the shapes grown by a few pixels, under the white
edge = im.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(1.2)); res = Image.new('RGBA', (W, H), (0, 0, 0, 0))
res.putalpha(edge); res.paste((255, 255, 255, 255), (0, 0), im.filter(ImageFilter.GaussianBlur(0.6)))
a = Image.composite(Image.new('L', (W, H), 255), edge, im.point(lambda v: 255 if v > 127 else 0)); res.putalpha(a)
res.save(os.path.join(out, 'side_stickers.png'), optimize=True)
# A sticker is a thin sheet lying on the shell: a normal map of its edges (the vinyl's step up from the paint, and the
# white print's step up from its black backing) lets the light catch them
import numpy as np
from scipy import ndimage
def relief(rgba, name, k=2.2):
    al = np.asarray(rgba)[..., 3].astype(np.float32) / 255; wh = np.asarray(rgba)[..., 0].astype(np.float32) / 255 * al
    h = ndimage.gaussian_filter(al, 1.6) + 0.35 * ndimage.gaussian_filter(wh, 1.2)
    gy, gx = np.gradient(h); n = np.stack([-gx * k, gy * k, np.ones_like(h)], -1); n /= np.linalg.norm(n, axis=-1, keepdims=True)
    Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(os.path.join(out, name), optimize=True)
relief(res, 'side_stickers_n.png')
# the windscreen banner: DRIFT MASTER across the sun strip (the strip itself is tint in the glass, see glass.gdshader)
BW, BH = 1024, 128; bm = Image.new('L', (BW, BH), 0); bd = ImageDraw.Draw(bm); f, bb = fit('DRIFT MASTER', saira, 940, 110, bd)
bd.text(((BW - (bb[2] - bb[0])) // 2 - bb[0], (BH - (bb[3] - bb[1])) // 2 - bb[1]), 'DRIFT MASTER', font=f, fill=255)
edge = bm.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(1.0)); ban = Image.new('RGBA', (BW, BH), (0, 0, 0, 0))
ban.paste((255, 255, 255, 255), (0, 0), bm.filter(ImageFilter.GaussianBlur(0.6))); ban.putalpha(Image.composite(Image.new('L', (BW, BH), 255), edge, bm.point(lambda v: 255 if v > 127 else 0)))
ban.save(os.path.join(out, 'banner.png'), optimize=True); relief(ban, 'banner_n.png')
# the tyre sidewall lettering: DRIFTMASTER in white on a blue band with a yellow flash at its head, a strip that is bent
# round the sidewall twice (top and bottom). Stored upside down, as the wheel model's own strip was
TW, TH = 640, 72; ty = Image.new('RGBA', (TW, TH), (0, 0, 0, 0)); td = ImageDraw.Draw(ty)
td.rounded_rectangle((14, 8, TW - 14, TH - 8), 6, fill=(16, 150, 222, 255)); td.polygon([(14, 8), (96, 8), (78, TH - 8), (14, TH - 8)], fill=(250, 208, 20, 255))
for k in range(3): td.polygon([(26 + k * 18, TH - 16), (38 + k * 18, 16), (46 + k * 18, 16), (34 + k * 18, TH - 16)], fill=(20, 20, 24, 255))
f, bb = fit('DRIFTMASTER', saira, 500, 60, td); td.text((104 + (510 - (bb[2] - bb[0])) // 2 - bb[0], (TH - (bb[3] - bb[1])) // 2 - bb[1]), 'DRIFTMASTER', font=f, fill=(255, 255, 255, 255))
ty.transpose(Image.FLIP_TOP_BOTTOM).save(os.path.join(out, 'tyre_letter.png'), optimize=True); print('ok')
