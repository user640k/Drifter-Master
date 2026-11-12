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
# drift-track tile (dark slate grey, a fine grain running one way with sparse short pale streaks, laid with the grain
# turned a quarter on alternate tiles, hairline joints, a satin sheen). Drawn, not scanned: swap in photos later.
from scipy.ndimage import gaussian_filter
N, T = 4, 512
alb = np.zeros((N * T, N * T, 3), np.float32); rough = np.zeros((N * T, N * T), np.float32)
base_col = np.array([0.245, 0.295, 0.318])   # slate with a blue-green cast
for j in range(N):
    for i in range(N):
        n = rng.normal(0, 1, (T, T)).astype(np.float32)
        grain = gaussian_filter(n, (0.7, 14), mode='wrap'); grain /= grain.std()        # long fibres along x
        fine = gaussian_filter(rng.normal(0, 1, (T, T)), 0.6, mode='wrap'); fine /= fine.std()
        cloud = gaussian_filter(rng.normal(0, 1, (T, T)), 60, mode='wrap'); cloud /= cloud.std()
        v = 1 + 0.03 * grain + 0.03 * fine + 0.045 * cloud
        # pale streaks: short dashes along the grain, a few per tile, some in loose rows
        st = np.zeros((T, T), np.float32)
        for _ in range(rng.integers(5, 13)):
            y = rng.integers(8, T - 8); x = rng.integers(0, T); ln = int(rng.integers(10, 70)); th = rng.choice([1, 2, 2, 3]); amp = rng.uniform(0.25, 0.75)
            for k in range(rng.integers(1, 3)):
                xs = (x + k * int(rng.integers(30, 120))) % T; xx = (np.arange(ln) + xs) % T
                prof = np.sin(np.linspace(0, np.pi, ln)) ** 0.5 * amp * rng.uniform(0.5, 1)
                for d in range(th): st[(y + d) % T, xx] = np.maximum(st[(y + d) % T, xx], prof * (rng.random(ln) > 0.15))
        st = gaussian_filter(st, (0.5, 1.2))
        tone = 1 + rng.normal(0, 0.03)
        col = base_col[None, None, :] * (v * tone)[..., None]
        col = col + st[..., None] * np.array([0.5, 0.52, 0.52])
        r = 0.46 + 0.012 * cloud + 0.02 * fine - 0.05 * st + rng.uniform(-0.03, 0.03)
        if (i + j) % 2: col = col.transpose(1, 0, 2); r = r.T                           # quarter turn on alternate tiles
        yy, xx2 = np.mgrid[0:T, 0:T]; d = np.minimum(np.minimum(xx2, T - 1 - xx2), np.minimum(yy, T - 1 - yy)).astype(np.float32)
        seam = np.clip(1 - d / 1.3, 0, 1); edge = np.clip(1 - d / 7, 0, 1)
        col = col * (1 + 0.05 * edge[..., None]) * (1 - 0.45 * seam[..., None])           # worn lighter at the edges, a dark hairline joint
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
print('ok')
