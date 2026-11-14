#!/usr/bin/env python3
"""Draws what hangs on the room's walls (our own artwork, no one's brands) and the door's wood.
   usage: make_room.py <wood colour jpg (ambientCG Wood026, CC0)> <out dir>
   wall_banner_1..9.png: vinyl banners in the manner of a club's sponsor wall, saying 640K, DRIFT MASTER, SEND IT and
   WINNERS DON'T USE BRAKES.   door.jpg: a flush brown door.   caution.png: a yellow notice.   blinds.png: a window with
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
def finish(im, name):   # a hem, an eyelet in each corner, and the slight unevenness of printed vinyl
    W, H = im.size; d = ImageDraw.Draw(im); d.rectangle((0, 0, W - 1, H - 1), outline=(0, 0, 0, 70), width=5)
    for x in (22, W - 22):
        for y in (22, H - 22): d.ellipse((x - 9, y - 9, x + 9, y + 9), fill=(170, 172, 176, 255)); d.ellipse((x - 4, y - 4, x + 4, y + 4), fill=(40, 40, 44, 255))
    a = np.asarray(im).astype(np.float32); sh = 1 + 0.05 * (np.asarray(Image.fromarray((rng.random((H // 40 + 2, W // 40 + 2)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)).astype(np.float32) / 255 - 0.5)
    a[..., :3] = np.clip(a[..., :3] * sh[..., None], 0, 255); Image.fromarray(a.astype(np.uint8)).convert('RGB').save(os.path.join(out, name), quality=88)
def new(w, h, bg): im = Image.new('RGBA', (w, h), bg); return im, ImageDraw.Draw(im)
BK, WH, RD, YL, BL, OR, GN, PK = (18, 18, 22, 255), (242, 242, 238, 255), (206, 24, 32, 255), (248, 206, 24, 255), (18, 70, 190, 255), (228, 120, 36, 255), (20, 92, 60, 255), (226, 30, 130, 255)
# 1  DRIFT MASTER, the big one: white on black, a red bar breaking into speed lines
im, d = new(1200, 480, BK); words(d, 'DRIFT MASTER', saira, (50, 60, 1150, 310), WH); d.polygon([(70, 340), (820, 340), (800, 372), (50, 372)], fill=RD)
for k in range(5): d.polygon([(850 + k * 62, 340), (890 + k * 62 - k * 6, 340), (870 + k * 62 - k * 6, 372), (830 + k * 62, 372)], fill=RD)
words(d, '1/10 RC DRIFT', saira, (60, 388, 700, 440), (200, 200, 200, 255)); finish(im, 'wall_banner_1.jpg')
# 2  640K: white on orange, as a maker's banner
im, d = new(1000, 440, OR); words(d, '640K', saira, (60, 30, 940, 320), WH, 6, (150, 70, 10, 255)); words(d, 'TINY WHOOP  ·  FPV  ·  RC', saira, (80, 330, 920, 400), WH); finish(im, 'wall_banner_2.jpg')
# 3  SEND IT: script on pink with a white slash
im, d = new(900, 420, PK); d.polygon([(0, 300), (900, 180), (900, 250), (0, 370)], fill=WH); words(d, 'Send it', script, (60, 10, 840, 330), WH, 3, WH); finish(im, 'wall_banner_3.jpg')
# 4  WINNERS DON'T USE BRAKES: red and black on yellow
im, d = new(1000, 440, YL); d.rectangle((0, 0, 1000, 70), fill=BK); d.rectangle((0, 370, 1000, 440), fill=BK); words(d, "WINNERS DON'T\nUSE BRAKES", saira, (40, 84, 960, 356), RD, 4, BK)
for k in range(12): d.polygon([(k * 90, 0), (k * 90 + 45, 0), (k * 90 + 15, 70), (k * 90 - 30, 70)], fill=YL)
finish(im, 'wall_banner_4.jpg')
# 5  640K again: white on blue with a swoosh
im, d = new(760, 360, BL); d.ellipse((-200, 210, 960, 560), fill=(240, 196, 30, 255)); d.ellipse((-200, 232, 960, 600), fill=BL); words(d, '640K', saira, (40, 20, 720, 250), WH); finish(im, 'wall_banner_5.jpg')
# 6  DRIFT MASTER over a chequered flag
im, d = new(1000, 420, (60, 62, 66, 255))
for y in range(0, 420, 42):
    for x in range(0, 1000, 42):
        if (x // 42 + y // 42) % 2 == 0: d.rectangle((x, y, x + 41, y + 41), fill=(84, 86, 92, 255))
words(d, 'DRIFT\nMASTER', saira, (60, 30, 940, 390), WH, 5, BK); finish(im, 'wall_banner_6.jpg')
# 7  SEND IT in block capitals on green
im, d = new(820, 380, GN); words(d, 'SEND IT', saira, (50, 50, 770, 270), WH); d.rectangle((60, 292, 760, 306), fill=YL[:3] + (255,)); words(d, 'NO LIFT', saira, (60, 312, 400, 356), YL); finish(im, 'wall_banner_7.jpg')
# 8  WINNERS DON'T USE BRAKES, white on black with a red band
im, d = new(1100, 380, BK); d.rectangle((0, 150, 1100, 250), fill=RD); words(d, "WINNERS DON'T", saira, (40, 22, 1060, 140), WH); words(d, 'USE BRAKES', saira, (40, 158, 1060, 244), WH); words(d, 'DRIFT MASTER', saira, (300, 270, 800, 350), (190, 190, 190, 255)); finish(im, 'wall_banner_8.jpg')
# 9  DRIFT MASTER, white on red
im, d = new(900, 300, RD); words(d, 'DRIFT MASTER', saira, (40, 40, 860, 260), WH, 3, (120, 10, 14, 255)); finish(im, 'wall_banner_9.jpg')
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
