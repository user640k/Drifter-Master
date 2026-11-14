#!/usr/bin/env python3
"""Lays out a track and writes its file: make_track.py <out.json> [infield|club|club2|bigs]. First INFIELD (assets/infield.json (same form as hillside.json: 480 points along the
   centre of the lane, evenly spaced, with the lane's half width).
   After a photo of a club track: an oval round the outside with a twisting S inside it, turf between the lanes.
   It is 2 m longer than the first version (the hall was widened to 11 m for it): the long straight runs past the pilot, a hairpin at the right-hand end turns
   back into the infield, an S through two more hairpins, then the far straight and the left-hand end bring it home."""
import json, sys, numpy as np
from scipy.ndimage import gaussian_filter1d
N = 600; HW = 0.45
def line(a, b, n=200): t = np.linspace(0, 1, n, endpoint=False)[:, None]; return np.array(a) + (np.array(b) - np.array(a)) * t
def arc(c, r, a0, a1, n=200): a = np.radians(np.linspace(a0, a1, n, endpoint=False)); return np.c_[c[0] + r * np.cos(a), c[1] + r * np.sin(a)]
which = sys.argv[2] if len(sys.argv) > 2 else 'infield'
if which == 'infield':
  path = np.vstack([
    line((-3.05, 1.85), (4.2, 1.85)),            # the long straight, past the pilot
    arc((4.2, 1.25), 0.6, 90, -90),              # right-hand end: hairpin into the infield
    line((4.2, 0.65), (-2.4, 0.65)),             # back along the middle
    arc((-2.4, 0.0), 0.65, 90, 270),             # the S: hairpin
    line((-2.4, -0.65), (4.2, -0.65)),           # and out again
    arc((4.2, -1.25), 0.6, 90, -90),             # hairpin onto the far straight
    line((4.2, -1.85), (-3.05, -1.85)),          # the far straight
    arc((-3.05, 0.0), 1.85, 270, 90),            # the wide left-hand end, all the way round
  ]); NAME, START = 'INFIELD', (-0.6, 1.9)
elif which == 'bigs':
  # BIG S: the line drawn over a photo of a club's track, laid out to fit the Cave. From the line on the right-hand side:
  # up to the far wall and all along it, down the left, back along under the far-left island and up in an S onto a
  # second run to the right, down round a hook, up to a hairpin (the finger), down the middle of the room, left along
  # the near side, a step up, out to the left, the big left-hander, and the long near straight back to the line.
  # It is given as its corners, each rounded with its own radius.
  C = [((4.3, 1.6), 0), ((4.3, -2.0), 1.25), ((-4.5, -2.0), 1.0), ((-4.5, -0.1), 0.9), ((-1.6, -0.1), 0.5), ((-1.6, -1.05), 0.45), ((2.7, -1.05), 0.9),
       ((2.7, 1.35), 0.85), ((1.0, 1.35), 0.85), ((1.0, -0.05), 0.55), ((-0.1, -0.05), 0.55), ((-0.1, 1.9), 0.75), ((-2.3, 1.9), 0.5), ((-2.3, 0.9), 0.5),
       ((-4.55, 0.9), 1.0), ((-4.55, 2.9), 1.0), ((4.3, 2.9), 1.15)]
  pts = []
  for k in range(len(C)):
    a, (b, R), c = np.array(C[k - 1][0]), (np.array(C[k][0]), C[k][1]), np.array(C[(k + 1) % len(C)][0])
    if R <= 0: pts.append(b[None, :]); continue
    u = (a - b) / np.linalg.norm(a - b); v = (c - b) / np.linalg.norm(c - b); phi = np.arccos(np.clip(u @ v, -1, 1)); tl = R / np.tan(phi / 2)
    cen = b + (u + v) / np.linalg.norm(u + v) * R / np.sin(phi / 2); p1, p2 = b + u * tl, b + v * tl
    a1 = np.arctan2(*(p1 - cen)[::-1]); a2 = np.arctan2(*(p2 - cen)[::-1]); d = (a2 - a1 + np.pi) % (2 * np.pi) - np.pi; ang = a1 + d * np.linspace(0, 1, 60)
    pts.append(np.c_[cen[0] + R * np.cos(ang), cen[1] + R * np.sin(ang)])
  dense = [];
  for k in range(len(pts)):
    q0, q1 = pts[k][-1], pts[(k + 1) % len(pts)][0]; dense.append(pts[k]); dense.append(line(tuple(q0), tuple(q1), max(2, int(np.linalg.norm(q1 - q0) / 0.02)))[1:])
  path = np.vstack(dense); N = 820; NAME, START = 'BIG S', (4.3, 0.9)
elif which == 'club':
  # CLUB 1: the route drawn in red on a picture of a printed drift mat. Along the far side to the left with the line on
  # it, the big corner all the way round, back along the near side, a left hairpin round the tip of the long island,
  # back to the left through the middle, a right hairpin round the roundabout, then the long diagonal sweeping right
  # and down across the mat, a wide left hairpin under the pits, up the right-hand side and left onto the far side.
  path = np.vstack([
    line((3.2, -1.9), (-2.05, -1.9)),            # the far straight, with the line
    arc((-2.05, 0.15), 2.05, -90, -270),         # the big corner
    line((-2.05, 2.2), (-0.55, 2.2)),            # the near side
    arc((-0.55, 1.52), 0.68, 90, -90),           # left hairpin round the tip of the island
    line((-0.55, 0.84), (-1.75, 0.84)),          # back to the left
    arc((-1.75, 0.155), 0.685, 90, 270),         # right hairpin round the roundabout
    line((-1.75, -0.53), (0.5, -0.53)),
    arc((0.5, 0.87), 1.4, -90, 0),               # the diagonal: a long right-hander down across the mat
    line((1.9, 0.87), (1.9, 1.1)),
    arc((3.0, 1.1), 1.1, 180, 0),                # the wide left hairpin under the pits
    line((4.1, 1.1), (4.1, -1.0)),               # up the right-hand side
    arc((3.2, -1.0), 0.9, 0, -90),               # left onto the far straight
  ]); NAME, START = 'CLUB 1', (-0.6, -1.9)
else:
  # CLUB 2: after a photo of a club's track, seen from a corner of its room. Nothing in it is straight: a run along the
  # far wall to a hairpin in the corner by the door, back through the middle in a long S round two islands, a wide
  # bend at the door end onto the near side, and a big sweeper round the far end. A smooth curve through these points:
  from scipy.interpolate import splprep, splev
  def bend(cx, cz, r, a0, a1, k=4): return [(cx + r * np.cos(np.radians(a)), cz + r * np.sin(np.radians(a))) for a in np.linspace(a0, a1, k)]
  P = ([(-2.9, -1.94), (-1.2, -2.0), (1.0, -1.9), (2.9, -1.96)]                    # along the far wall
       + bend(3.85, -1.33, 0.62, -62, 62)                                          # the hairpin by the door
       + [(2.9, -0.72), (1.4, -0.9), (-0.2, -0.72), (-1.5, -0.86)]                 # back through the middle
       + bend(-2.0, -0.17, 0.69, 242, 118)                                         # the hairpin round the first island
       + [(-1.2, 0.52), (0.4, 0.4), (2.0, 0.56), (3.2, 0.44)]                      # out again, the other way
       + bend(3.75, 1.21, 0.77, -62, 62)                                           # the wide bend at the door end
       + [(2.7, 1.98), (0.8, 1.9), (-1.0, 2.0), (-2.6, 1.94)]                      # the near side, past the driver
       + bend(-2.85, 0.0, 1.92, 108, 252, 5))                                      # the big sweeper round the far end
  P = np.array(P); tck, _ = splprep([np.append(P[:, 0], P[0, 0]), np.append(P[:, 1], P[0, 1])], s=0, per=True)
  path = np.array(splev(np.linspace(0, 1, 4000, endpoint=False), tck)).T; NAME, START = 'CLUB 2', (0.6, 1.92)
# even steps, the joints between straights and arcs eased (a car cannot change curvature in no distance)
d = np.concatenate([[0], np.cumsum(np.hypot(*np.diff(np.vstack([path, path[:1]]), axis=0).T))]); L = d[-1]
m = 6000; sm = np.linspace(0, L, m, endpoint=False); px = np.interp(sm, d, np.append(path[:, 0], path[0, 0])); pz = np.interp(sm, d, np.append(path[:, 1], path[0, 1]))
sig = 0.14 / (L / m); px = gaussian_filter1d(px, sig, mode='wrap'); pz = gaussian_filter1d(pz, sig, mode='wrap')
d = np.concatenate([[0], np.cumsum(np.hypot(np.diff(np.append(px, px[0])), np.diff(np.append(pz, pz[0]))))]); L = d[-1]
s = np.linspace(0, L, N, endpoint=False); X = np.interp(s, d, np.append(px, px[0])); Z = np.interp(s, d, np.append(pz, pz[0]))
i0 = int(np.argmin((X - START[0]) ** 2 + (Z - START[1]) ** 2))                      # the line: on the long straight, in front of the pilot
X = np.roll(X, -i0); Z = np.roll(Z, -i0); si = N - 30
X2, Z2 = np.append(X, X[:2]), np.append(Z, Z[:2])
out = {'name': NAME, 'hw': HW, 'ds': L / N, 'n': N, 'x': [round(float(v), 5) for v in X], 'z': [round(float(v), 5) for v in Z], 'e': [0.0] * N,
       'start': [float(X[si]), float(Z[si]), float(np.arctan2(X2[si + 1] - X2[si - 1], Z2[si + 1] - Z2[si - 1]))],
       'raceLine': [[round(float(X[i]), 4), round(float(Z[i]), 4)] for i in range(0, N, 8)]}
json.dump(out, open(sys.argv[1], 'w'))
# tightest bend, and the nearest two stretches of lane come to each other
hd = np.unwrap(np.arctan2(np.roll(X, -1) - np.roll(X, 1), np.roll(Z, -1) - np.roll(Z, 1))); k = np.abs(np.roll(hd, -1) - np.roll(hd, 1))[2:-2] / (2 * L / N)
dm = np.hypot(X[:, None] - X[None, :], Z[:, None] - Z[None, :]); idx = np.abs(np.arange(N)[:, None] - np.arange(N)[None, :]); idx = np.minimum(idx, N - idx); dm[idx < 60] = 9
print('lap %.1f m, tightest radius %.2f m, lanes no nearer than %.2f m centre to centre, reach x %.2f..%.2f z %.2f..%.2f' % (L, 1 / k.max(), dm.min(), X.min(), X.max(), Z.min(), Z.max()))
