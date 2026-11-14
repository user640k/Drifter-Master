#!/usr/bin/env python3
"""Lights out: the light the LED strips throw on the floor, baked into one sheet over the hall for each track
(make_led.py <assets dir>; writes tex/led*.png). Each strip is drawn where it is fixed and spread as light falls off
from a line: tight and bright beside it, a wide faint wash further out. The same strips are built as glowing ribbons
by scripts/Decor.cs. Every track has strips on its barriers, under its kerbs and across its line; CLUB 1 has more."""
import sys, json, numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import gaussian_filter
A = sys.argv[1]; P = 96
ICE, PINK, RED, WHITE, AMB, BLUE = (0.25, 0.8, 1.0), (1.0, 0.2, 0.62), (1.0, 0.1, 0.06), (1.0, 0.97, 0.9), (1.0, 0.62, 0.1), (0.1, 0.42, 1.0)
for FILE, Z0, OUT, BOARDS, HX, HZ in (('club', 1.0, 'led_club', True, 5.5, 3.5), ('club2', 1.0, 'led_club2', True, 5.5, 3.5), ('infield', 1.0, 'led_infield', True, 5.5, 3.5), ('hillside', -2.4, 'led', False, 5.5, 3.5), ('bigs', 1.0, 'led_bigs', True, 5.5, 3.5)):
    W, H = int(2 * HX * P), int(2 * HZ * P)
    def px(x, z): return ((x + HX) * P, (z + HZ) * P)
    CLUB1 = FILE == 'club'; tj = json.load(open(f'{A}/{FILE}.json')); X = np.array(tj['x']); Z = np.array(tj['z']) - Z0; hw = tj['hw']; n = len(X)
    tx = np.roll(X, -1) - np.roll(X, 1); tz = np.roll(Z, -1) - np.roll(Z, 1); l = np.hypot(tx, tz); tx /= l; tz /= l; nx, nz = tz, -tx
    hd = np.arctan2(tx, tz); dh = np.roll(hd, -1) - np.roll(hd, 1); kap = np.arctan2(np.sin(dh), np.cos(dh)) / (2 * tj['ds'])
    def edge(i, s, o): return px(X[i] + s * nx[i] * o, Z[i] + s * nz[i] * o)
    out = np.zeros((H, W, 3), np.float32)
    def light(draw, col, power=1.0, spread=(0.035, 0.13, 0.42), weight=(1.0, 0.5, 0.2)):
        im = Image.new('L', (W, H), 0); draw(ImageDraw.Draw(im)); a = np.asarray(im, np.float32) / 255; tot = np.zeros_like(a)
        for s, w in zip(spread, weight):
            b = gaussian_filter(a, s * P); tot += w * b / max(1e-6, b.max())
        out[:] += tot[..., None] * np.array(col, np.float32) * power
    light(lambda d: d.line([edge(i, 1, hw + 0.006) for i in range(n)] + [edge(0, 1, hw + 0.006)], fill=255, width=3), ICE, 0.8)
    light(lambda d: d.line([edge(i, -1, hw + 0.006) for i in range(n)] + [edge(0, -1, hw + 0.006)], fill=255, width=3), PINK, 0.8)
    def kerbs(d):
        run = []
        for i in list(range(n)) + [0]:
            tight = abs(kap[i]) > 1 / 1.25; outer = CLUB1 and X[i] < -2.15
            if tight or outer: run.append(edge(i, (1 if kap[i] > 0 else -1) * (1 if tight else -1), hw - 0.08))
            else:
                if len(run) > 2: d.line(run, fill=255, width=4)
                run = []
    if BOARDS: light(kerbs, RED, 0.9, (0.03, 0.09, 0.25), (1.0, 0.5, 0.15))
    light(lambda d: d.line([edge(0, -1, hw), edge(0, 1, hw)], fill=255, width=5), WHITE, 0.9, (0.03, 0.1, 0.3))
    if CLUB1:
        light(lambda d: [d.line([px(X[0] - b + 0.13, Z[0] - 0.24), px(X[0] - b, Z[0]), px(X[0] - b + 0.13, Z[0] + 0.24)], fill=255, width=5) for b in (0.4, 0.62, 0.84)], AMB, 0.8, (0.02, 0.07, 0.2))
        light(lambda d: d.line([px(3.6, -0.4), px(4.6, -0.4)], fill=255, width=8), WHITE, 1.3, (0.08, 0.25, 0.6), (1.0, 0.7, 0.3))                  # under the arch
        def bays(d):
            for z in (-2.1, -1.7, -1.3, -0.9): d.line([px(2.78, z), px(3.42, z)], fill=255, width=3)
            d.line([px(3.42, -2.1), px(3.42, -0.9)], fill=255, width=3)
        light(bays, (0.85, 0.93, 1.0), 0.7, (0.03, 0.12, 0.35))
        # the blue sections glow all over: lit from strips along their edges
        yy, xx = np.mgrid[0:H, 0:W]; wx = xx / P - HX; wz = yy / P - HZ + 1.0
        dB = np.hypot(wx + 2.05, wz - 0.15); dR = np.hypot(wx + 1.75, wz - 0.155); dH = np.hypot(wx + 0.55, wz - 1.52); dD = np.hypot(wx - 0.5, wz - 0.87)
        blue = (wz > 1.32) & (wz < 1.72) & (wx > -1.75) & (wx < -0.55) | (dH < 0.2) | (wx <= -1.75) & (wz > 0.45) & (dR > 1.17) & (dB < 1.57) & ((wz < 1.72) | (wx < -2.05))
        blue |= (dD > 0.7) & (dD < 0.92) & (wx > 0.75) & (wz <= 0.87) | (wx > 1.2) & (wx < 1.42) & (wz > 0.87) & (wz < 1.2)
        b = blue.astype(np.float32); out[:] += (gaussian_filter(b, 0.03 * P) * 0.75 + gaussian_filter(b, 0.2 * P) * 0.3)[..., None] * np.array(BLUE, np.float32)
        light(lambda d: d.ellipse((px(-1.95, -1.045)[0], px(-1.95, -1.045)[1], px(-1.55, -0.645)[0], px(-1.55, -0.645)[1]), outline=255, width=3), AMB, 0.35, (0.03, 0.1, 0.3))
        light(lambda d: d.line([px(-0.7, 2.45), px(0.6, 2.45)], fill=255, width=3), AMB, 0.35, (0.05, 0.2, 0.5))
    Image.fromarray((np.clip(out / 1.6, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8)).save(f'{A}/tex/{OUT}.png')
    print('ok', OUT)
