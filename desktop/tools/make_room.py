#!/usr/bin/env python3
"""Draws what hangs on the room's walls (our own artwork, no one's brands) and the door's wood.
   usage: make_room.py <wood colour jpg (ambientCG Wood026, CC0)> <out dir>
   wall_banner_640k.jpg, wall_banner_dm.jpg: two printed PVC vinyl banners (640K in black and white, DRIFT MASTER in
   colour) and wall_banner_n.png, the folds they hang in.   door.jpg: a flush brown door.   caution.png: a yellow notice.   blinds.png: a window with
   its slatted blind down, daylight behind.     Lettering: Saira Extra Bold Italic and Sacramento (OFL, tools/fonts)."""
import sys, os, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
wood, out = sys.argv[1], sys.argv[2]; here = os.path.dirname(os.path.abspath(__file__)); rng = np.random.default_rng(5)
saira = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Saira-ExtraBoldItalic.ttf'), s)
script = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Sacramento-Regular.ttf'), s)
def fit(d, text, font, w, h, start=400):
    s = start
    while True:
        f = font(s); bb = d.multiline_textbbox((0, 0), text, font=f, align='center', spacing=s // 8)
        if bb[2] - bb[0] <= w and bb[3] - bb[1] <= h: return f, bb
        s -= 4
def words(d, text, font, box, fill, stroke=0, sfill=None):
    x0, y0, x1, y1 = box; f, bb = fit(d, text, font, x1 - x0, y1 - y0)
    d.multiline_text((x0 + (x1 - x0 - (bb[2] - bb[0])) // 2 - bb[0], y0 + (y1 - y0 - (bb[3] - bb[1])) // 2 - bb[1]), text, font=f, fill=fill, align='center', spacing=f.size // 8, stroke_width=stroke, stroke_fill=sfill)
# ---- two banners of printed PVC vinyl, 2 : 1, hemmed, with eyelets along the edges. The sheet hangs from its corners, so
# it is never quite flat: broad soft folds run in from the corners and a little sag shows along the top. The folds are
# shaded into the print (gently) and also written out as a normal map, so the room's lights slide over them.
BW_, BH_ = 1600, 800
from scipy.ndimage import gaussian_filter
yy, xx = np.mgrid[0:BH_, 0:BW_].astype(np.float32); hgt = np.zeros((BH_, BW_), np.float32)
for cx, cy in ((0, 0), (BW_, 0), (0, BH_), (BW_, BH_)):       # folds fanning in from each corner tie
    ang = np.arctan2(yy - cy, xx - cx); dist = np.hypot(xx - cx, yy - cy); hgt += 0.9 * np.sin(ang * 9 + cx * 0.01) * np.exp(-dist / 620.0)
hgt += 0.5 * gaussian_filter(rng.normal(0, 1, (BH_, BW_)).astype(np.float32), 70) * 22 + 0.35 * np.sin(xx / 150.0 + 1.3) * np.exp(-yy / 260.0)   # slow billows, sag under the top hem
hgt = gaussian_filter(hgt, 6); gy, gx = np.gradient(hgt); k = 5.0; nrm = np.stack([-gx * k, gy * k, np.ones_like(hgt)], -1); nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
Image.fromarray(((nrm * 0.5 + 0.5) * 255).astype(np.uint8)).save(os.path.join(out, 'wall_banner_n.png'), optimize=True)
fold = np.clip(1 + (-gx * 0.9 - gy * 0.6) * 0.5, 0.86, 1.1)                                # light from the upper left
def vinyl(im, name):
    d = ImageDraw.Draw(im); W, H = im.size
    d.rectangle((14, 14, W - 15, H - 15), outline=(0, 0, 0, 40), width=3)                  # the hem's stitch line
    for x, y in [(34, 34), (W // 2, 30), (W - 34, 34), (34, H - 34), (W // 2, H - 30), (W - 34, H - 34), (30, H // 2), (W - 30, H // 2)]:
        d.ellipse((x - 15, y - 15, x + 15, y + 15), fill=(196, 198, 202, 255), outline=(90, 92, 96, 255), width=2); d.ellipse((x - 7, y - 7, x + 7, y + 7), fill=(30, 30, 34, 255))
    a = np.asarray(im.convert('RGB')).astype(np.float32) * fold[..., None]; Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(out, name), quality=90)
def new(w, h, bg): im = Image.new('RGBA', (w, h), bg); return im, ImageDraw.Draw(im)
# 1  640K: black and white
im, d = new(BW_, BH_, (14, 14, 16, 255)); words(d, '640K', saira, (150, 90, 1450, 560), (244, 244, 240, 255))
d.rectangle((150, 596, 1450, 612), fill=(244, 244, 240, 255)); words(d, 'TINY WHOOP  ·  FPV  ·  RC', saira, (150, 630, 1450, 720), (244, 244, 240, 255)); vinyl(im, 'wall_banner_640k.jpg')
# 2  DRIFT MASTER: black on a white sheet
im, d = new(BW_, BH_, (244, 244, 240, 255)); words(d, 'DRIFT\nMASTER', saira, (120, 70, 1480, 730), (14, 14, 16, 255)); vinyl(im, 'wall_banner_dm.jpg')
BK, YL = (18, 18, 22, 255), (248, 206, 24, 255)
# the door: the wood with its grain running up the door, a little darker towards the edges
w = Image.open(wood).convert('RGB').rotate(90, expand=True).resize((512, 1024), Image.LANCZOS); a = np.asarray(w).astype(np.float32); yy, xx = np.mgrid[0:1024, 0:512]
edge = np.minimum(np.minimum(xx, 511 - xx) / 40, 1) * 0.12 + 0.88; a *= edge[..., None]; Image.fromarray(np.clip(a * 0.92, 0, 255).astype(np.uint8)).save(os.path.join(out, 'door.jpg'), quality=88)
# a yellow notice
im, d = new(400, 300, YL); d.rectangle((0, 0, 399, 96), fill=BK); words(d, 'CAUTION', saira, (30, 12, 370, 86), YL); words(d, 'WATCH YOUR STEP\nCARS ON TRACK', saira, (30, 112, 370, 280), BK); im.convert('RGB').save(os.path.join(out, 'caution.jpg'), quality=88)
# the window: daylight behind a slatted blind, a paler band where the lower sash is, cords down one side
W, H = 768, 640; a = np.zeros((H, W, 3), np.float32); yy, xx = np.mgrid[0:H, 0:W]
day = 0.86 + 0.1 * np.exp(-((xx - W * 0.45) / (W * 0.5)) ** 2) - 0.1 * (yy > H * 0.62) * (1 - 0.5 * np.exp(-((xx - W * 0.5) / 200.0) ** 2))
slat = 0.5 + 0.5 * np.cos(2 * np.pi * yy / 16.0); shade = 0.74 + 0.26 * slat ** 0.6
for k, t in enumerate((0.93, 0.97, 1.0)): a[..., k] = np.clip(day * shade * t, 0, 1)
im = Image.fromarray((a * 255).astype(np.uint8)); d = ImageDraw.Draw(im)
for x in (W * 0.33, W * 0.9): d.line([(x, 0), (x, H)], fill=(190, 196, 200), width=2)
d.rectangle((0, 0, W - 1, 14), fill=(214, 216, 216)); im.filter(ImageFilter.GaussianBlur(0.6)).save(os.path.join(out, 'blinds.jpg'), quality=90)
print('ok')
