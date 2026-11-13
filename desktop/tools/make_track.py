#!/usr/bin/env python3
"""Lays out the INFIELD track and writes assets/infield.json (same form as hillside.json: 480 points along the
   centre of the lane, evenly spaced, with the lane's half width).
   After a photo of a club track: an oval round the outside with a twisting S inside it, turf between the lanes.
   It is 2 m longer than the first version (the hall was widened to 11 m for it): the long straight runs past the pilot, a hairpin at the right-hand end turns
   back into the infield, an S through two more hairpins, then the far straight and the left-hand end bring it home."""
import json, sys, numpy as np
from scipy.ndimage import gaussian_filter1d
N = 600; HW = 0.45
def line(a, b, n=200): t = np.linspace(0, 1, n, endpoint=False)[:, None]; return np.array(a) + (np.array(b) - np.array(a)) * t
def arc(c, r, a0, a1, n=200): a = np.radians(np.linspace(a0, a1, n, endpoint=False)); return np.c_[c[0] + r * np.cos(a), c[1] + r * np.sin(a)]
path = np.vstack([
    line((-3.05, 1.85), (4.2, 1.85)),            # the long straight, past the pilot
    arc((4.2, 1.25), 0.6, 90, -90),              # right-hand end: hairpin into the infield
    line((4.2, 0.65), (-2.4, 0.65)),             # back along the middle
    arc((-2.4, 0.0), 0.65, 90, 270),             # the S: hairpin
    line((-2.4, -0.65), (4.2, -0.65)),           # and out again
    arc((4.2, -1.25), 0.6, 90, -90),             # hairpin onto the far straight
    line((4.2, -1.85), (-3.05, -1.85)),          # the far straight
    arc((-3.05, 0.0), 1.85, 270, 90),            # the wide left-hand end, all the way round
])
# even steps, the joints between straights and arcs eased (a car cannot change curvature in no distance)
d = np.concatenate([[0], np.cumsum(np.hypot(*np.diff(np.vstack([path, path[:1]]), axis=0).T))]); L = d[-1]
m = 6000; sm = np.linspace(0, L, m, endpoint=False); px = np.interp(sm, d, np.append(path[:, 0], path[0, 0])); pz = np.interp(sm, d, np.append(path[:, 1], path[0, 1]))
sig = 0.14 / (L / m); px = gaussian_filter1d(px, sig, mode='wrap'); pz = gaussian_filter1d(pz, sig, mode='wrap')
d = np.concatenate([[0], np.cumsum(np.hypot(np.diff(np.append(px, px[0])), np.diff(np.append(pz, pz[0]))))]); L = d[-1]
s = np.linspace(0, L, N, endpoint=False); X = np.interp(s, d, np.append(px, px[0])); Z = np.interp(s, d, np.append(pz, pz[0]))
i0 = int(np.argmin((X + 0.6) ** 2 + (Z - 1.9) ** 2))                      # the line: on the long straight, in front of the pilot
X = np.roll(X, -i0); Z = np.roll(Z, -i0); si = N - 30
X2, Z2 = np.append(X, X[:2]), np.append(Z, Z[:2])
out = {'name': 'INFIELD', 'hw': HW, 'ds': L / N, 'n': N, 'x': [round(float(v), 5) for v in X], 'z': [round(float(v), 5) for v in Z], 'e': [0.0] * N,
       'start': [float(X[si]), float(Z[si]), float(np.arctan2(X2[si + 1] - X2[si - 1], Z2[si + 1] - Z2[si - 1]))],
       'raceLine': [[round(float(X[i]), 4), round(float(Z[i]), 4)] for i in range(0, N, 8)]}
json.dump(out, open(sys.argv[1], 'w'))
# tightest bend, and the nearest two stretches of lane come to each other
hd = np.unwrap(np.arctan2(np.roll(X, -1) - np.roll(X, 1), np.roll(Z, -1) - np.roll(Z, 1))); k = np.abs(np.roll(hd, -1) - np.roll(hd, 1))[2:-2] / (2 * L / N)
dm = np.hypot(X[:, None] - X[None, :], Z[:, None] - Z[None, :]); idx = np.abs(np.arange(N)[:, None] - np.arange(N)[None, :]); idx = np.minimum(idx, N - idx); dm[idx < 60] = 9
print('lap %.1f m, tightest radius %.2f m, lanes no nearer than %.2f m centre to centre, reach x %.2f..%.2f z %.2f..%.2f' % (L, 1 / k.max(), dm.min(), X.min(), X.max(), Z.min(), Z.max()))
