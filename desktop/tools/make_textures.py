#!/usr/bin/env python3
"""Builds the desktop version's textures from CC0 photo scans (ambientCG.com, CC0 1.0).
   usage: make_textures.py <dir with unzipped ambientCG sets> <out dir>
   Sets used: PaintedPlaster017, Sponge001, OfficeCeiling001, Chipboard004 (1K)."""
import os, sys, glob, numpy as np
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
ONLY = os.environ.get('DM_ONLY')   # make one track's sheet only (its file name without .json)
HX, HZ, PXM = 5.5, 3.5, 256   # the hall is 11 x 7 m: the 11 x 5 m the layouts may fill, and 2 m of clear floor on the driver's side (+z)
Wp, Hp = int(HX * 2 * PXM), int(HZ * 2 * PXM)
rr = lambda a, b: a + wr.random() * (b - a)
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
# one sheet per track: (data file, how far its layout was moved along z, name suffix, turf and kerbs?)
for TFILE, Z0, SUF, TURF in (('hillside.json', -2.4, '', False), ('infield.json', 1.0, '_infield', True), ('club.json', 1.0, '_club', 'club'), ('club2.json', 1.0, '_club2', 'club2'), ('bigs.json', 1.0, '_bigs', 'bigs')):
    if ONLY and TFILE != ONLY + '.json': continue   # (each layout sits 1 m towards -z of the hall's middle)
    wr = np.random.default_rng(777); tj = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets', TFILE)))
    rgb = np.zeros((Hp, Wp, 3), np.float32); al = np.zeros((Hp, Wp), np.float32); ro = np.zeros((Hp, Wp), np.float32)
    # broad dust haze
    for i in range(60): over(soft(rr(-HX, HX), rr(-HZ, HZ), rr(0.4, 1.5), rr(0.03, 0.09)), (170, 168, 160), 0.10)
    # dust along the walls, where nothing drives
    yy, xx = np.mgrid[0:Hp, 0:Wp]; dw = np.minimum(np.minimum(xx, Wp - 1 - xx), np.minimum(yy, Hp - 1 - yy)) / PXM
    over(np.exp(-dw / 0.22).astype(np.float32) * 0.2, (175, 170, 160), 0.16)
    # the lanes: the cars sweep the dust off and polish the tile
    from scipy.interpolate import splprep, splev
    _l = np.array([(x, z - Z0) for x, z in tj['raceLine']]); _tck, _ = splprep([np.append(_l[:, 0], _l[0, 0]), np.append(_l[:, 1], _l[0, 1])], s=0, per=True)
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
    # tape left from old layouts, some torn short (not on the club's floors: they are laid out like a printed mat, and kept clean)
    for i in range(0 if TURF in ('club', 'club2', 'bigs') else 13):
        im, d = layer(); x, z, a, L = rr(-HX + 0.4, HX - 0.4), rr(-HZ + 0.4, HZ - 0.4), rr(0, 3.14), rr(0.12, 0.7)
        cx, cz = px(x, z); ux, uz = math.cos(a) * L / 2 * PXM, math.sin(a) * L / 2 * PXM
        d.line([(cx - ux, cz - uz), (cx + ux, cz + uz)], fill=255, width=int(0.048 * PXM))
        m = arr(im) * (0.75 + 0.25 * gaussian_filter(wr.random((Hp // 8 + 1, Wp // 8 + 1)).astype(np.float32), 2).repeat(8, 0).repeat(8, 1)[:Hp, :Wp])
        over(m * (1 - 0.6 * lm) * 0.2, (220, 200, 90) if wr.random() < 0.3 else (225, 225, 215), 0.2)
    # faded paint from an old layout: broken arcs
    for i in range(0 if TURF in ('club', 'club2', 'bigs') else 6):
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
    # turf and kerbs (tracks that have them): artificial grass fills the ground between the lanes, and the inside of each
    # tight bend has a red and white kerb. Both are flat on the floor, so they are painted into this sheet.
    if TURF:
        from scipy.spatial import ConvexHull
        cx = np.array(tj['x']); cz = np.array(tj['z']) - Z0; hw = tj['hw']; n = len(cx)
        im, d = layer(); R = (hw + 0.035) * PXM
        for a, b in zip(cx, cz): q = px(a, b); d.ellipse((q[0] - R, q[1] - R, q[0] + R, q[1] + R), fill=255)   # discs along the lane: no gaps at the bends
        lanes = arr(im) > 0.5
        hull = ConvexHull(np.c_[cx, cz]); im, d = layer(); d.polygon([px(*np.c_[cx, cz][i]) for i in hull.vertices], fill=255); inside = arr(im) > 0.5
        grass = inside & ~lanes
        if TURF == 'club':
            q = px(-2.05, 0.15 - Z0); rr2 = (xx - q[0]) ** 2 + (yy - q[1]) ** 2   # turf only outside the big corner, out to the corners of the mat
            grass = (xx < q[0]) & (rr2 > ((2.05 + hw + 0.035) * PXM) ** 2) & (xx > q[0] - 2.55 * PXM) & (np.abs(yy - q[1]) < 2.55 * PXM)
        blue2 = None
        if TURF in ('club2', 'bigs'):   # bare between the lanes but for islands of turf, each with a blue tip (CLUB 2: one inside each tight bend; BIG S: far left, middle and near left)
            isl = np.zeros_like(grass); tip = np.zeros_like(grass)
            for ix, iz, rx, rz, tx in ((2.85, -1.33, 1.5, 0.62, 2.5), (-1.0, -0.17, 1.5, 0.62, 2.5), (2.75, 1.21, 1.5, 0.62, 2.5)) if TURF == 'club2' else ((-3.0, -1.05, 1.05, 0.45, 1.6), (1.85, 0.3, 0.4, 0.75, 0.4), (-3.5, 1.9, 0.6, 0.55, 0.9)):
                q = px(ix, iz - Z0); isl |= ((xx - q[0]) / (rx * PXM)) ** 2 + ((yy - q[1]) / (rz * PXM)) ** 2 < 1; tip |= ((xx - q[0]) / (tx * PXM)) ** 2 + ((yy - q[1]) / (rz * PXM)) ** 2 < 1
            blue2 = grass & tip & ~isl; grass &= isl
        g = wr.random((Hp, Wp)).astype(np.float32); blade = gaussian_filter(g, (0.6, 0.6)); clump = gaussian_filter(wr.random((Hp, Wp)).astype(np.float32), 9)
        tone = 0.78 + 0.5 * (blade - 0.5) * 2.2 + 0.5 * (clump - 0.5) * 3
        for k, base in enumerate((0.16, 0.42, 0.14)): rgb[..., k] = np.where(grass, base * tone, rgb[..., k])
        al[:] = np.where(grass, 1.0, al); ro[:] = np.where(grass, 0.45, ro)
        if blue2 is not None:
            w = np.clip(gaussian_filter(blue2.astype(np.float32), 0.75) * 1.15 - 0.075, 0, 1)
            for k, v in enumerate((0.07, 0.5, 0.86)): rgb[..., k] = rgb[..., k] * (1 - w) + v * w
            al[:] = al * (1 - w) + w; ro[:] = ro * (1 - w) - 0.1 * w
        # kerbs: where the lane bends tighter than about 0.9 m, along its inner edge
        hd = np.arctan2(np.roll(cx, -1) - np.roll(cx, 1), np.roll(cz, -1) - np.roll(cz, 1)); dh = np.roll(hd, -1) - np.roll(hd, 1); kap = np.arctan2(np.sin(dh), np.cos(dh)) / (2 * tj['ds'])
        step = max(1, int(round(0.09 / tj['ds'])))
        for i in range(0, n, step):
            tight = abs(kap[i]) > 1 / 1.25; outer = TURF == 'club' and cx[i] < -2.15   # (the big corner's kerb is on its outside, against the turf)
            if not (tight or outer): continue
            j = (i + step) % n; sgn = (1 if kap[i] > 0 else -1) * (1 if tight else -1)
            def edge(q, o):
                tx, tz = np.cos(hd[q]), -np.sin(hd[q]); return (cx[q] + sgn * tx * o, cz[q] + sgn * tz * o)   # across the lane, to the inside
            quad = [px(*edge(i, hw - 0.005)), px(*edge(j, hw - 0.005)), px(*edge(j, hw - 0.075)), px(*edge(i, hw - 0.075))]
            im, d = layer(); d.polygon(quad, fill=255); m = arr(im) > 0.5; red = (i // step) % 2 == 0
            for k, v in enumerate((0.78, 0.08, 0.07) if red else (0.9, 0.9, 0.88)): rgb[..., k] = np.where(m, v, rgb[..., k])
            al[:] = np.where(m, 1.0, al); ro[:] = np.where(m, 0.1, ro)
    if TURF == 'club':
        def paint(m, col, rough):   # (edges softened over a pixel, so a marking seen close has a clean edge and not steps)
            w = np.clip(gaussian_filter(m.astype(np.float32), 0.75) * 1.15 - 0.075, 0, 1)
            for k, v in enumerate(col): rgb[..., k] = rgb[..., k] * (1 - w) + v * w
            al[:] = al * (1 - w) + w; ro[:] = ro * (1 - w) + rough * w
        def side(q, sg, o): tx, tz = np.cos(hd[q]), -np.sin(hd[q]); return px(cx[q] + sg * tx * o, cz[q] + sg * tz * o)
        # world coordinates of every pixel (x, and z as in the track file)
        q0, q1 = px(0, -Z0), px(1, 1 - Z0); wx = (xx - q0[0]) / (q1[0] - q0[0]); wz = (yy - q0[1]) / (q1[1] - q0[1])
        # the blue sections, where the mat has them. One fills the long island between the near side and the lane coming
        # back through the middle, and runs on up the outside of the roundabout hairpin; the other is a band along the
        # inside of the diagonal.
        dB = np.hypot(wx + 2.05, wz - 0.15); dR = np.hypot(wx + 1.75, wz - 0.155); dH = np.hypot(wx + 0.55, wz - 1.52); dD = np.hypot(wx - 0.5, wz - 0.87)
        isl = (wz > 1.32) & (wz < 1.72) & (wx > -1.75) & (wx < -0.55) | (dH < 0.2) | (wx <= -1.75) & (wz > 0.45) & (dR > 1.17) & (dB < 1.57) & ((wz < 1.72) | (wx < -2.05))
        band = (dD > 0.7) & (dD < 0.92) & (wx > 0.75) & (wz <= 0.87) | (wx > 1.2) & (wx < 1.42) & (wz > 0.87) & (wz < 1.2)
        paint(isl | band, (0.07, 0.5, 0.86), -0.1)
        paint(dR < 0.2, (0.34, 0.34, 0.35), 0.1)   # the middle of the roundabout
        # yellow floor tape (30 mm, clean-edged, a little glossier than the tile): three short lines round the outside of the
        # big corner at its middle, and three arrows across the lane before the line
        im, d = layer(); w = max(3, int(round(0.03 * PXM)))
        for o in (0.13, 0.2, 0.27):
            run = [side(q, -(1 if kap[q] > 0 else -1), hw - o) for q in range(n) if cx[q] < -3.75]
            run.sort(key=lambda p: p[1])
            for a in range(0, len(run) - 1, 4): d.line(run[a:a + 5], fill=255, width=w)
        for back in (0.4, 0.62, 0.84):
            x = cx[0] - back; d.line([px(x + 0.13, cz[0] - 0.24), px(x, cz[0]), px(x + 0.13, cz[0] + 0.24)], fill=255, width=int(round(0.045 * PXM)), joint='curve')
        paint(arr(im) > 0.5, (0.93, 0.76, 0.05), -0.12)
        # the pits, inside the wide end: bays marked out in white
        im, d = layer()
        wl = max(2, int(round(0.02 * PXM)))
        for z in (-1.1, -0.7, -0.3, 0.1): d.line([px(2.78, z - Z0), px(3.42, z - Z0)], fill=255, width=wl)
        d.line([px(3.42, -1.1 - Z0), px(3.42, 0.1 - Z0)], fill=255, width=wl)
        paint(arr(im) > 0.5, (0.9, 0.9, 0.88), -0.05)
    out_rgba = np.dstack([rgb.clip(0, 1), al.clip(0, 1)])
    Image.fromarray((out_rgba * 255).astype(np.uint8), 'RGBA').save(f'{out}/floor_wear{SUF}.png', optimize=True)
    Image.fromarray(((0.5 + ro).clip(0, 1) * 255).astype(np.uint8)).save(f'{out}/floor_wear{SUF}_rough.jpg', quality=85)

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