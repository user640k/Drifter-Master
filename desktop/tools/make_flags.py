#!/usr/bin/env python3
"""Draws the trackside flags (our own artwork): two kinds, each in red and in white, saying 640K, DRIFT MASTER,
   SEND IT and WINNERS DON'T USE BRAKES.
   banner_*.png: a small upright banner that hangs in a frame.   feather_*.png: a tall feather flag on a pole (its
   shape is in the alpha).     usage: make_flags.py <out dir>      Lettering: Saira Extra Bold Italic (OFL, tools/fonts)."""
import sys, os
from PIL import Image, ImageDraw, ImageFont
out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); font = lambda s: ImageFont.truetype(os.path.join(here, 'fonts', 'Saira-ExtraBoldItalic.ttf'), s)
RED, WHITE, DARK = (200, 22, 30, 255), (240, 240, 236, 255), (24, 24, 28, 255)
def down(text, size, fill, w, h):   # the words running up the flag, as on a real one
    f = font(size); bb = ImageDraw.Draw(Image.new('L', (4, 4))).textbbox((0, 0), text, font=f); t = Image.new('RGBA', (bb[2] - bb[0] + 8, bb[3] - bb[1] + 8), (0, 0, 0, 0))
    ImageDraw.Draw(t).text((4 - bb[0], 4 - bb[1]), text, font=f, fill=fill); t = t.rotate(90, expand=True)
    if t.size[1] > h: t = t.resize((max(1, int(t.size[0] * h / t.size[1])), h), Image.LANCZOS)
    if t.size[0] > w: t = t.resize((w, max(1, int(t.size[1] * w / t.size[0]))), Image.LANCZOS)
    return t
def feather(name, bg, fg, TF):
    W, H = 160, 640; im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    # the feather: straight down the pole (left), bellied out to the right, round over the top, cut away at the foot
    pts = [(0, 0)] + [(int(W * (0.18 + 0.8 * min(1, (y / 90) ** 0.6)) * (1 - 0.22 * max(0, (y - 110) / (H - 110)))), y) for y in range(0, H - 40, 6)] + [(int(W * 0.08), H - 1), (0, H - 1)]
    d.polygon(pts, fill=fg); m = im.split()[3]; top = Image.new('RGBA', (W, H), bg); cut = Image.new('L', (W, H), 0); ImageDraw.Draw(cut).polygon([(0, 0), (W, 0), (W, 250), (0, 190)], fill=255)
    im.paste(top, (0, 0), Image.composite(cut, Image.new('L', (W, H), 0), m)); d = ImageDraw.Draw(im)
    for k in range(3): d.polygon([(22 + k * 30, 150), (52 + k * 30, 60), (66 + k * 30, 60), (36 + k * 30, 150)], fill=fg)     # three slashes in the top field
    t = down(TF, 72, bg if bg != WHITE else RED, 84, 350); lay = Image.new('RGBA', (W, H), (0, 0, 0, 0)); lay.alpha_composite(t, (14, 262)); im = Image.composite(Image.alpha_composite(im, lay), im, m); im.putalpha(m)
    im.save(os.path.join(out, f'feather_{name}.png'), optimize=True)
for name, bg, fg, TB, TF in (('red', RED, WHITE, '640K', 'DRIFT MASTER'), ('white', WHITE, RED, "WINNERS DON'T USE BRAKES", 'SEND IT')):   # banner wording, feather wording
    W, H = 128, 384; im = Image.new('RGBA', (W, H), bg); d = ImageDraw.Draw(im); d.rectangle((0, 0, W - 1, 22), fill=fg); d.rectangle((0, H - 23, W - 1, H - 1), fill=fg)
    t = down(TB, 84, fg, W - 34, H - 76); im.alpha_composite(t, ((W - t.size[0]) // 2, (H - t.size[1]) // 2)); im.save(os.path.join(out, f'banner_{name}.png'), optimize=True)
    feather(name, bg, fg, TF)
for name, col, TF in (('blue', (28, 86, 200, 255), 'DRIFT MASTER'), ('yellow', (246, 200, 20, 255), 'SEND IT'), ('purple', (122, 48, 172, 255), '640K')): feather(name, col, WHITE, TF)   # the same flag in three more colours
print('ok')
