#!/usr/bin/env python3
"""Builds the desktop version's textures from CC0 photo scans (ambientCG.com, CC0 1.0).
   usage: make_textures.py <dir with unzipped ambientCG sets> <out dir>
   Sets used: PaintedPlaster017, Sponge001, OfficeCeiling001, Chipboard004 (1K)."""
import sys, glob, numpy as np
from PIL import Image, ImageFilter
src, out = sys.argv[1], sys.argv[2]
rng = np.random.default_rng(7)
def load(name, kind='Color'):
    f = glob.glob(f'{src}/**/{name}*_{kind}.jpg', recursive=True)
    f.sort(key=lambda p: -Image.open(p).size[0]); return Image.open(f[0]).convert('RGB')

# ---- P-tile: 4 x 4 vinyl composition tiles (304.8 mm each) in one 2048 px texture, after the owner's photo of real
# drift-track tile (black, a fine grain running one way with sparse short pale streaks, laid with the grain
# turned a quarter on alternate tiles, hairline joints, a satin sheen). Drawn, not scanned: swap in photos later.
from scipy.ndimage import gaussian_filter
N, T = 4, 512
alb = np.zeros((N * T, N * T, 3), np.float32); rough = np.zeros((N * T, N * T), np.float32)
base_col = np.array([0.050, 0.056, 0.062])   # black, with the faintest cool cast
for j in range(N):
    for i in range(N):
        n = rng.normal(0, 1, (T, T)).astype(np.float32)
        grain = gaussian_filter(n, (0.7, 14), mode='wrap'); grain /= grain.std()        # long fibres along x
        fine = gaussian_filter(rng.normal(0, 1, (T, T)), 0.6, mode='wrap'); fine /= fine.std()
        cloud = gaussian_filter(rng.normal(0, 1, (T, T)), 60, mode='wrap'); cloud /= cloud.std()
        v = 1 + 0.10 * grain + 0.08 * fine + 0.10 * cloud
        # pale streaks: short dashes along the grain, a few per tile, some in loose rows
        st = np.zeros((T, T), np.float32)
        for _ in range(rng.integers(5, 13)):
            y = rng.integers(8, T - 8); x = rng.integers(0, T); ln = int(rng.integers(10, 70)); th = rng.choice([1, 2, 2, 3]); amp = rng.uniform(0.25, 0.75)
            for k in range(rng.integers(1, 3)):
                xs = (x + k * int(rng.integers(30, 120))) % T; xx = (np.arange(ln) + xs) % T
                prof = np.sin(np.linspace(0, np.pi, ln)) ** 0.5 * amp * rng.uniform(0.5, 1)
                for d in range(th): st[(y + d) % T, xx] = np.maximum(st[(y + d) % T, xx], prof * (rng.random(ln) > 0.15))
        st = gaussian_filter(st, (0.5, 1.2))
        tone = 1 + rng.normal(0, 0.07)
        col = base_col[None, None, :] * (v * tone)[..., None]
        col = col + st[..., None] * np.array([0.30, 0.31, 0.31])
        r = 0.36 + 0.012 * cloud + 0.02 * fine + 0.06 * st + rng.uniform(-0.03, 0.03)
        if (i + j) % 2: col = col.transpose(1, 0, 2); r = r.T                           # quarter turn on alternate tiles
        yy, xx2 = np.mgrid[0:T, 0:T]; d = np.minimum(np.minimum(xx2, T - 1 - xx2), np.minimum(yy, T - 1 - yy)).astype(np.float32)
        seam = np.clip(1 - d / 1.3, 0, 1); edge = np.clip(1 - d / 7, 0, 1)
        col = col * (1 + 0.12 * edge[..., None]) + 0.035 * seam[..., None]                # worn lighter at the edges, dust in the hairline joint
        alb[j * T:(j + 1) * T, i * T:(i + 1) * T] = col; rough[j * T:(j + 1) * T, i * T:(i + 1) * T] = r + 0.3 * seam
Image.fromarray((alb.clip(0, 1) * 255).astype(np.uint8)).save(f'{out}/ptile_albedo.jpg', quality=92)
Image.fromarray((rough.clip(0, 1) * 255).astype(np.uint8)).save(f'{out}/ptile_rough.jpg', quality=88)

def simple(name, outname, size, tint=None, gray=False, q=88):
    im = load(name).resize((size, size), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32) / 255
    if gray: g = a.mean(axis=2, keepdims=True); a = np.repeat(g / g.mean() * 0.78, 3, axis=2)
    if tint is not None: a = a * np.array(tint)
    Image.fromarray((a.clip(0, 1) * 255).astype(np.uint8)).save(f'{out}/{outname}', quality=q)
simple('PaintedPlaster017', 'wall.jpg', 1024, tint=[1.06, 1.05, 1.02])
simple('Sponge001', 'foam.jpg', 512, gray=True)      # grey detail; the block colour is set per block
simple('OfficeCeiling001', 'ceiling.jpg', 1024)
simple('Chipboard004', 'board.jpg', 1024)

# ---- floor wear: one sheet over the whole hall floor (about 4 mm a pixel; 2304 x 1280, sizes the graphics card can compress), after the browser version's floorWear/paintWear,
# retuned for black tile, where wear shows as dust, dullness and pale marks rather than dark stains:
# dust haze and dusty edges, lanes the cars keep clean and polished, dried spills, chips, cracked corners,
# tape and faded paint from old layouts, fine scratches, faint tyre lines.
# floor_wear.png is colour + how much of it covers the tile; floor_wear_rough.jpg shifts the gloss (grey = no change).
import json, os, math
from PIL import ImageDraw
HX, HZ, PXM = 4.5, 2.5, 256
Wp, Hp = int(HX * 2 * PXM), int(HZ * 2 * PXM)
wr = np.random.default_rng(777); rr = lambda a, b: a + wr.random() * (b - a)
rgb = np.zeros((Hp, Wp, 3), np.float32); al = np.zeros((Hp, Wp), np.float32); ro = np.zeros((Hp, Wp), np.float32)
def over(mask, col, rough=0.0):
    global rgb, al, ro
    m = mask[..., None]; c = np.array(col, np.float32) / 255
    na = al + mask * (1 - al); rgb[:] = np.where(na[..., None] > 1e-6, (rgb * al[..., None] * (1 - m) + c * m) / np.maximum(na[..., None], 1e-6), rgb); al[:] = na; ro[:] += rough * mask / max(mask.max(), 1e-6) if rough else 0
def px(x, z): return ((x + HX) * PXM, (z + HZ) * PXM)
def soft(x, z, rad, a):
    m = np.zeros((Hp, Wp), np.float32); cx, cz = px(x, z); R = rad * PXM
    x0, x1, y0, y1 = int(max(0, cx - R)), int(min(Wp, cx + R)), int(max(0, cz - R)), int(min(Hp, cz + R))
    if x1 <= x0 or y1 <= y0: return m
    yy, xx = np.mgrid[y0:y1, x0:x1]; d = np.sqrt((xx - cx) ** 2 + (yy - cz) ** 2) / R; m[y0:y1, x0:x1] = np.clip(1 - d, 0, 1) ** 1.5 * a; return m
def layer(): im = Image.new('L', (Wp, Hp), 0); return im, ImageDraw.Draw(im)
def arr(im, blur=0): 
    a = np.asarray(im).astype(np.float32) / 255
    return gaussian_filter(a, blur) if blur else a
# broad dust haze
for i in range(60): over(soft(rr(-HX, HX), rr(-HZ, HZ), rr(0.4, 1.5), rr(0.03, 0.09)), (170, 168, 160), 0.10)
# dust along the walls, where nothing drives
yy, xx = np.mgrid[0:Hp, 0:Wp]; dw = np.minimum(np.minimum(xx, Wp - 1 - xx), np.minimum(yy, Hp - 1 - yy)) / PXM
over(np.exp(-dw / 0.22).astype(np.float32) * 0.2, (175, 170, 160), 0.16)
# the lanes: the cars sweep the dust off and polish the tile
tj = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets', 'hillside.json')))
from scipy.interpolate import splprep, splev
_l = np.array([(x, z + 3.4) for x, z in tj['raceLine']]); _tck, _ = splprep([np.append(_l[:, 0], _l[0, 0]), np.append(_l[:, 1], _l[0, 1])], s=0, per=True)
line = list(zip(*splev(np.linspace(0, 1, 600, endpoint=False), _tck)))  # the racing line, smooth
def lane(width, off=0.0, wob=0.0):
    im, d = layer(); pts = []
    n = len(line)
    for i, (x, z) in enumerate(line):
        qx, qz = line[(i + 1) % n]; dx, dz = qx - x, qz - z; l = math.hypot(dx, dz) or 1; o = off + math.sin(i * 0.04 + off * 9) * wob
        pts.append(px(x - dz / l * o, z + dx / l * o))
    d.line(pts + [pts[0]], fill=255, width=max(1, int(width * PXM)), joint='curve'); return im
lm = arr(lane(0.62), 22); lm = lm / lm.max()
al *= (1 - 0.75 * lm); ro -= 0.10 * lm
# dried spills: a dull patch with a paler ring; a few stay glossy
for i in range(14):
    x, z, s = rr(-HX + 0.4, HX - 0.4), rr(-HZ + 0.4, HZ - 0.4), rr(0.05, 0.2); blot = np.zeros((Hp, Wp), np.float32)
    for k in range(5): blot = np.maximum(blot, soft(x + rr(-s, s), z + rr(-s, s) * 0.7, s * rr(0.5, 1.1), 1))
    core = np.clip(blot * 3, 0, 1); ring = np.clip(1 - np.abs(blot - 0.25) / 0.12, 0, 1) * (blot > 0.05)
    glossy = wr.random() < 0.3
    ro += (-0.16 if glossy else 0.22) * core; over(gaussian_filter(ring.astype(np.float32), 2.5) * 0.06, (150, 148, 140))
# chips: pale specks where the surface is gone
im, d = layer()
for i in range(650):
    cx, cz = px(rr(-HX, HX), rr(-HZ, HZ)); s = rr(0.0015, 0.007) * PXM; d.ellipse((cx - s, cz - s, cx + s, cz + s), fill=int(rr(90, 255)))
over(arr(im, 0.5) * 0.45, (150, 150, 146), 0.15)
# cracked tiles: short jagged hairlines
im, d = layer()
for i in range(12):
    x, z, a = rr(-HX, HX), rr(-HZ, HZ), rr(0, 6.28); pts = [px(x, z)]
    for k in range(int(rr(4, 12))): a += rr(-0.7, 0.7); s = rr(0.02, 0.07); x += math.cos(a) * s; z += math.sin(a) * s; pts.append(px(x, z))
    d.line(pts, fill=255, width=1)
over(arr(im, 0.4) * 0.5, (120, 120, 116), 0.2)
# tape left from old layouts, some torn short
for i in range(13):
    im, d = layer(); x, z, a, L = rr(-HX + 0.4, HX - 0.4), rr(-HZ + 0.4, HZ - 0.4), rr(0, 3.14), rr(0.12, 0.7)
    cx, cz = px(x, z); ux, uz = math.cos(a) * L / 2 * PXM, math.sin(a) * L / 2 * PXM
    d.line([(cx - ux, cz - uz), (cx + ux, cz + uz)], fill=255, width=int(0.048 * PXM))
    m = arr(im) * (0.75 + 0.25 * gaussian_filter(wr.random((Hp // 8 + 1, Wp // 8 + 1)).astype(np.float32), 2).repeat(8, 0).repeat(8, 1)[:Hp, :Wp])
    over(m * (1 - 0.6 * lm) * 0.2, (220, 200, 90) if wr.random() < 0.3 else (225, 225, 215), 0.2)
# faded paint from an old layout: broken arcs
for i in range(6):
    im, d = layer(); cx, cz = px(rr(-HX + 1, HX - 1), rr(-HZ + 1, HZ - 1)); R = rr(0.6, 2.0) * PXM; a0 = rr(0, 360); a = a0
    while a < a0 + rr(60, 170):
        if wr.random() < 0.75: d.arc((cx - R, cz - R, cx + R, cz + R), a, a + 11, fill=255, width=int(0.022 * PXM))
        a += 14
    over(arr(im, 0.6) * 0.17, (230, 230, 220) if wr.random() < 0.5 else (225, 190, 40))
# fine scratches
im, d = layer()
for i in range(1000):
    x, z, a, l = rr(-HX, HX), rr(-HZ, HZ), rr(0, 6.28), rr(0.03, 0.3); d.line([px(x, z), px(x + math.cos(a) * l, z + math.sin(a) * l)], fill=int(rr(60, 200)), width=1)
over(arr(im, 0.35) * 0.16, (210, 210, 205), 0.06)
# tyre lines round the lap
for k in range(10): over(arr(lane(0.014, rr(-0.24, 0.24), 0.06), 0.8) * 0.07, (170, 170, 168))
out_rgba = np.dstack([rgb.clip(0, 1), al.clip(0, 1)])
Image.fromarray((out_rgba * 255).astype(np.uint8), 'RGBA').save(f'{out}/floor_wear.png', optimize=True)
Image.fromarray(((0.5 + ro).clip(0, 1) * 255).astype(np.uint8)).save(f'{out}/floor_wear_rough.jpg', quality=85)

# ---- neon sign "Drift Master": glass tube lettering in a single-stroke script (Sacramento, SIL OFL), "Drift" in pink and
# "Master" in ice blue. neon_tube.png is the lit tube (white-hot down the middle, colour at the glass, a tight halo);
# neon_glow.png is the wash of colour it throws on the light wall behind (colour + how strongly it tints).
from PIL import ImageFont
NW, NH = 2048, 640
font = ImageFont.truetype(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'fonts', 'Sacramento-Regular.ttf'), 372)
def word_mask(txt_all, part):
    im = Image.new('L', (NW, NH), 0); d = ImageDraw.Draw(im); bb = d.textbbox((0, 0), txt_all, font=font)
    ox, oy = (NW - (bb[2] - bb[0])) / 2 - bb[0], (NH - (bb[3] - bb[1])) / 2 - bb[1]
    pre = txt_all[:txt_all.index(part)]; d.text((ox + d.textlength(pre, font=font), oy), part, font=font, fill=255)
    return np.asarray(im.filter(ImageFilter.MaxFilter(3))).astype(np.float32) / 255
tube = np.zeros((NH, NW, 4), np.float32); glow = np.zeros((NH * 2, NW * 2, 3), np.float32); gsum = np.zeros((NH * 2, NW * 2), np.float32)
for part, col in (('Drift', (1.0, 0.05, 0.45)), ('Master', (0.0, 0.62, 0.95))):
    m = word_mask('Drift Master', part); c = np.array(col, np.float32)
    core = np.clip(gaussian_filter(m, 2.2) * 1.5 - 0.8, 0, 1)                 # hottest along the middle of the tube
    halo = gaussian_filter(m, 7) * 0.9 + gaussian_filter(m, 18) * 0.5
    rgbt = c[None, None, :] * (1 - core[..., None]) + core[..., None] * (0.45 + 0.55 * c[None, None, :]) + core[..., None] * 0.25
    a = np.clip(np.maximum(m, halo * 0.75), 0, 1)
    tube[..., :3] = np.where(a[..., None] > tube[..., 3:4], np.where(m[..., None] > 0.02, rgbt, c[None, None, :]), tube[..., :3]); tube[..., 3] = np.maximum(tube[..., 3], a)
    big = np.zeros((NH * 2, NW * 2), np.float32); big[NH // 2:NH // 2 + NH, NW // 2:NW // 2 + NW] = m
    g = gaussian_filter(big, 45) * 2.2 + gaussian_filter(big, 130) * 3.2
    glow += g[..., None] * c[None, None, :]; gsum += g
Image.fromarray((tube.clip(0, 1) * 255).astype(np.uint8), 'RGBA').save(f'{out}/neon_tube.png', optimize=True)
gcol = glow / np.maximum(gsum[..., None], 1e-4); ga = np.clip(gsum * 0.55, 0, 0.8)
Image.fromarray((np.dstack([gcol.clip(0, 1), ga]) * 255).astype(np.uint8), 'RGBA').resize((1024, 320), Image.LANCZOS).save(f'{out}/neon_glow.png', optimize=True)
print('ok')