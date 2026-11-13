#!/usr/bin/env python3
"""The van's detail sheet. Its model came with a weathered texture (rust, grime) and a paint mask; an RC shell is freshly
   painted, so only the real details are kept (lamps, grille, handles, seals, panel lines) and everything else is paint.
   usage: make_van_detail.py <diffuse.png> <paint mask.png> <emit.png> <out.png>   (the three sheets from the web asset)
   Output: RGBA in the model's texture layout: colour of the detail, alpha where it covers the paint."""
import sys, numpy as np
from PIL import Image
from scipy import ndimage
dif = np.asarray(Image.open(sys.argv[1]).convert('RGB')).astype(np.float32) / 255
msk = np.asarray(Image.open(sys.argv[2]).convert('RGB').resize(dif.shape[1::-1])).astype(np.float32) / 255
emi = np.asarray(Image.open(sys.argv[3]).convert('L').resize(dif.shape[1::-1], Image.BILINEAR)).astype(np.float32) / 255
R, G, B = dif[..., 0], dif[..., 1], dif[..., 2]; mx = dif.max(-1)
paint = msk[..., 1] > 0.45; sat = mx - dif.min(-1)
lamp = ndimage.binary_dilation(emi > 0.2, iterations=3)                                  # lit lamps
lens = ndimage.binary_opening((sat < 0.16) & (mx > 0.5), iterations=2)                    # clear lenses, reflectors, chrome rims
lab, n = ndimage.label(lens); sz = ndimage.sum(lens, lab, range(1, n + 1)); lens = np.isin(lab, 1 + np.flatnonzero(sz > 80))
# the painted panels: the mask with its holes closed. Holes up to the size of a rust patch are paint; the big ones are
# the window openings and stay
body = ndimage.binary_closing(paint, iterations=5); holes = ndimage.binary_fill_holes(body) & ~body
lab, n = ndimage.label(holes); sz = ndimage.sum(holes, lab, range(1, n + 1)); small = np.isin(lab, 1 + np.flatnonzero(sz < 2500))
p = body | small
# the nose panel keeps its dark details: the grille slots and the seam under the windscreen
H, W = mx.shape; nose = np.zeros_like(p); nose[int(H * 0.49):int(H * 0.715), int(W * 0.195):int(W * 0.372)] = True
slots = ndimage.binary_opening(nose & (mx < 0.2), iterations=1)
p = p & ~lamp & ~lens & ~slots
p = p | (ndimage.binary_dilation((B > R * 1.12) & (B > 0.22) & (sat > 0.1), iterations=2) & ~(emi > 0.3))     # any of the old blue left over is paint
a = 1 - ndimage.gaussian_filter(p.astype(np.float32), 0.8)
det = dif.copy(); grey = det.mean(-1, keepdims=True)
det = np.where(slots[..., None], 0.03, det); det = np.where(((det.max(-1, keepdims=True) - det.min(-1, keepdims=True)) > 0.12) & ~lamp[..., None] & ~lens[..., None], grey * 0.5, det)                          # leftover rust in the trim: to dark grey
out = np.dstack([det, a]); Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).transpose(Image.FLIP_TOP_BOTTOM).save(sys.argv[4], optimize=True); print('ok', a.mean())
