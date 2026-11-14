#!/usr/bin/env python3
"""The skin of the tyre arch over CLUB 1: make_arch.py <out dir>. One sheet wrapped round the tyre's section: across it (u) runs
the arch from its left foot to its right as the driver sees it; down it (v) goes round the section: back sidewall at the
top and bottom edges, the yellow underside at a quarter, the front sidewall with DRIFT MASTER in the middle, the tread at
three quarters. Up on the arch is down on the sheet, so the lettering is drawn upside down here. Original artwork."""
import sys, os, numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import gaussian_filter
W, H = 4096, 1024; out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); rng = np.random.default_rng(7)
font = ImageFont.truetype(os.path.join(here, 'fonts', 'Saira-ExtraBoldItalic.ttf'), 176)
rub = 0.05 + 0.012 * gaussian_filter(rng.normal(0, 1, (H, W)), 3)                      # rubber: near black, a little uneven
img = np.dstack([rub, rub, rub * 1.03]).astype(np.float32); rough = np.full((H, W), 0.82, np.float32)
im = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im)
# tread (v 0.625 to 0.875): two straight grooves round the tyre and a herringbone of cuts between and outside them
t0, t1 = int(0.63 * H), int(0.87 * H)
for y in (int(0.71 * H), int(0.79 * H)): d.line([(0, y), (W, y)], fill=255, width=9)
for x in range(-200, W + 200, 46):
    d.line([(x, t0), (x + 50, int(0.705 * H))], fill=255, width=6); d.line([(x + 25, int(0.715 * H)), (x - 25, int(0.785 * H))], fill=255, width=6); d.line([(x, int(0.795 * H)), (x + 50, t1)], fill=255, width=6)
g = np.asarray(im, np.float32) / 255; img *= (1 - 0.75 * g)[..., None]; rough += 0.1 * g
# a raised rib where each sidewall meets the tread and the underside, and fine rings on the sidewalls
im = Image.new('L', (W, H), 0); d = ImageDraw.Draw(im)
for v in (0.385, 0.615, 0.115, 0.885): d.line([(0, int(v * H)), (W, int(v * H))], fill=255, width=5)
for v in (0.41, 0.59, 0.09, 0.91): d.line([(0, int(v * H)), (W, int(v * H))], fill=120, width=2)
g = np.asarray(im, np.float32) / 255; img += 0.05 * g[..., None]
# the underside: a yellow panel along the middle of the arch
im = Image.new('L', (W, H), 0); ImageDraw.Draw(im).rounded_rectangle((int(0.16 * W), int(0.155 * H), int(0.84 * W), int(0.345 * H)), 30, fill=255)
g = gaussian_filter(np.asarray(im, np.float32) / 255, 1.2)
for k, c in enumerate((0.93, 0.74, 0.04)): img[..., k] = img[..., k] * (1 - g) + c * g
rough = rough * (1 - g) + 0.45 * g
# the lettering on the front sidewall: letters spaced out along the arch, squeezed across (the sheet is finer along the arch than round the section), upside down
txt = 'DRIFT MASTER'; track = 150; ws = [font.getlength(ch) for ch in txt]; tw = int(sum(ws) + track * (len(txt) - 1)) + 80
ti = Image.new('L', (tw, 260), 0); td = ImageDraw.Draw(ti); x = 40
for ch, w in zip(txt, ws): td.text((x, 20), ch, font=font, fill=255, stroke_width=9, stroke_fill=255); x += w + track
bb = ti.getbbox(); ti = ti.crop(bb); ti = ti.resize((int(ti.width * 0.66), 150), Image.LANCZOS).transpose(Image.FLIP_TOP_BOTTOM)
lay = Image.new('L', (W, H), 0); lay.paste(ti, ((W - ti.width) // 2, int(0.5 * H) - ti.height // 2))
g = np.asarray(lay, np.float32) / 255
for k, c in enumerate((0.93, 0.93, 0.9)): img[..., k] = img[..., k] * (1 - g) + c * g
rough = rough * (1 - g) + 0.6 * g
Image.fromarray((img.clip(0, 1) * 255).astype(np.uint8)).save(f'{out}/arch.jpg', quality=90)
Image.fromarray((rough.clip(0, 1) * 255).astype(np.uint8)).resize((W // 2, H // 2)).save(f'{out}/arch_rough.jpg', quality=85)
print('ok', ti.size)
