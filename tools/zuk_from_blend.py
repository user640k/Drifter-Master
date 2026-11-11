#!/usr/bin/env python3
"""Convert the owner's Żuk van model (a .blend from a "free zuk 3d model" zip) into ZUK_ASSET, embedded in index.html.

    uv venv -p 3.11 bpyenv && uv pip install -p bpyenv/bin/python bpy numpy pillow
    bpyenv/bin/python tools/zuk_from_blend.py <unzipped>/source/<name>.blend <unzipped>/textures

Same rules as tools/car_from_usdz.py: the shell is never reshaped, only scaled uniformly to true 1/10 of the real van
(the Żuk's 2700 mm wheelbase becomes 270 mm) and turned into the game's frame (x = the car's left, y up, z forward),
wheelbase centred on z = 0, tyres on the ground. To make it an RC drift body it is emptied: the wheels, the floor pan,
the inner wheel-arch liners, axles and exhaust are left out (the game's own chassis and wheels go underneath).
Parts are split by role: the body (one textured mesh), the glass (window parts) and the two mirrors (they break off).
Textures: the model's colour atlas (with its number plate replaced by a 640kFPV plate and the oil-brand sun strip
by a DRIFT MASTER one), a paint texture (R: the model's own clean paint shading, G: where the blue paint is, so the
game's paint schemes can recolour the body and keep the rust and trim), and the lamp glow texture.
"""
import base64, io, json, pathlib, re, sys
import bpy, bmesh
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

REAL_WB = 2.700
src, texdir = sys.argv[-2], pathlib.Path(sys.argv[-1])
root = pathlib.Path(__file__).resolve().parent.parent
bpy.ops.wm.open_mainfile(filepath=src, load_ui=False)
ob = bpy.data.objects['Plane.018']
bm = bmesh.new(); bm.from_mesh(ob.data); bm.transform(ob.matrix_world); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
uvl = bm.loops.layers.uv.active

# loose parts
parent = {}
def find(a):
    while parent.setdefault(a, a) != a:
        parent[a] = parent[parent[a]]; a = parent[a]
    return a
for f in bm.faces:
    vs = [v.index for v in f.verts]
    for v in vs[1:]: parent[find(v)] = find(vs[0])
parts = {}
for f in bm.faces: parts.setdefault(find(f.verts[0].index), []).append(f)

dif = Image.open(texdir / 'dif_optimised.jpeg').convert('RGB'); DW, DH = dif.size; difA = np.asarray(dif).astype(np.float32) / 255
def sample(u, v): return difA[min(DH - 1, max(0, int((1 - v) * DH))), min(DW - 1, max(0, int(u * DW)))]

def info(fs):
    P = np.array([v.co[:] for f in fs for v in f.verts]); U = np.array([l[uvl].uv[:] for f in fs for l in f.loops])
    return P.min(0), P.max(0), P.mean(0), U.mean(0)

# the tyres give the hubs: the four biggest identical parts low down at the sides
tyres = [fs for fs in parts.values() if len(fs) == 1120]
hubs_b = [((lo + hi) / 2) for lo, hi, _, _ in map(info, tyres)]
yf = np.mean([h[1] for h in hubs_b if h[1] < 0]); yr = np.mean([h[1] for h in hubs_b if h[1] > 0])
ground = min(info(fs)[0][2] for fs in tyres)
s = REAL_WB / 10 / (yr - yf); yc = (yf + yr) / 2
def game(p): return np.array([p[0], p[2] - ground, -(p[1] - yc)]) * s   # x stays (the car's left), z up -> y, -y forward -> z

body_lo, body_hi = info(max(parts.values(), key=len))[:2]
groups = {'body': [], 'glass': [], 'mirror_R': [], 'mirror_L': []}
dropped = 0
for fs in parts.values():
    lo, hi, c, uvc = info(fs)
    wheel = len(fs) in (1120, 160)                                   # tyres and rims: the game brings its own
    under = hi[2] < 0.1 and abs(c[0]) < 0.24 and -0.70 < c[1] < 0.59  # floor pan, arch liners, axles, exhaust
    if wheel or under: dropped += len(fs); continue
    if abs(c[0]) > 0.255 and c[2] > 0.25 and c[1] < -0.4:            # the mirrors and their arms
        groups['mirror_R' if c[0] > 0 else 'mirror_L'].extend(fs); continue
    col = sample(*uvc)
    if col.mean() < 0.16 and 0.1 < uvc[1] < 0.66 and hi[2] > 0.24:   # dark window panels up in the glasshouse
        groups['glass'].extend(fs); continue
    groups['body'].extend(fs)

meshes, allp = [], []
for name, fs in groups.items():
    if not fs: continue
    vmap, pos, uvs, idx = {}, [], [], []
    for f in fs:
        corner = []
        for l in f.loops:
            key = (l.vert.index, round(l[uvl].uv[0], 5), round(l[uvl].uv[1], 5))
            if key not in vmap: vmap[key] = len(pos); pos.append(game(l.vert.co)); uvs.append(l[uvl].uv[:])
            corner.append(vmap[key])
        for j in range(1, len(corner) - 1): idx.append([corner[0], corner[j], corner[j + 1]])
    meshes.append((name, np.array(pos), np.array(uvs), np.array(idx)))
    allp.append(np.array(pos))
allp = np.concatenate(allp); lo, hi = allp.min(0), allp.max(0)

out = []
for name, pos, uvs, idx in meshes:
    qp = np.round((pos - lo) / (hi - lo) * 65535).astype(np.uint16); big = len(pos) > 65535
    role = 'glass' if name == 'glass' else 'zbody'
    out.append({'name': name, 'role': role, 'mat': role, 'wheel': None, 'hub': None, 'n32': big,
                'pos': base64.b64encode(qp.tobytes()).decode(), 'idx': base64.b64encode(idx.astype(np.uint32 if big else np.uint16).reshape(-1).tobytes()).decode(),
                # stored with v flipped, as the game's decoder flips it back (USD convention)
                'uv': base64.b64encode(np.round(np.clip(np.c_[uvs[:, 0], 1 - uvs[:, 1]], 0, 1) * 65535).astype(np.uint16).tobytes()).decode()})

# hubs in the game frame (key: F/R + R/L, R = +x as in the 2002 asset)
hubs = {}
for h in hubs_b:
    g = game(h); hubs[('F' if g[2] > 0 else 'R') + ('R' if g[0] > 0 else 'L')] = [round(float(x), 5) for x in g]

# textures: colour atlas with a 640kFPV plate and a DRIFT MASTER sun strip
def font(px):
    for f in ['/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf']:
        if pathlib.Path(f).exists(): return ImageFont.truetype(f, px)
    return ImageFont.load_default()
d = dif.copy(); g = ImageDraw.Draw(d)
glass = tuple(int(x) for x in np.asarray(dif.crop((140, 840, 200, 880))).reshape(-1, 3).mean(0))
g.rectangle((230, 765, 455, 825), fill=glass)                          # the brand logo goes
g.rectangle((150, 770, 560, 818), fill=(18, 18, 20))                   # sun strip
fb = font(40); w = g.textlength('DRIFT MASTER', font=fb); g.text((355 - w / 2, 772), 'DRIFT MASTER', fill=(240, 240, 236), font=fb)
g.rectangle((52, 1018, 322, 1082), fill=(26, 26, 28)); g.rectangle((57, 1023, 317, 1077), fill=(238, 238, 232))  # white plate
fp = font(46); w = g.textlength('640kFPV', font=fp); g.text((187 - w / 2, 1024), '640kFPV', fill=(18, 18, 20), font=fp)
def jpg(im, size, q):
    b = io.BytesIO(); im.resize((size, size), Image.LANCZOS).save(b, 'JPEG', quality=q); return base64.b64encode(b.getvalue()).decode()
# paint texture: R = the model's clean paint shading, G = where the blue paint is (soft edge)
A = np.asarray(dif).astype(np.float32) / 255; mx, mn = A.max(2), A.min(2); sat = (mx - mn) / np.maximum(mx, 1e-3)
blue = (A[..., 2] > A[..., 0] + 0.06) & (A[..., 2] >= A[..., 1] - 0.02) & (sat > 0.22) & (mx > 0.18)
mask = Image.fromarray((blue * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1.5))
shade = Image.open(texdir / '69d055c305ec4e4f91d45507db270e77.jpeg').convert('L').resize(dif.size, Image.LANCZOS)
paint = Image.merge('RGB', (shade, mask, Image.new('L', dif.size, 0)))
tex = {'zukDif': jpg(d, 1024, 82), 'zukPaint': jpg(paint, 1024, 88), 'zukEmit': jpg(Image.open(texdir / 'emit.jpg').convert('RGB'), 512, 80)}

asset = {'scale': s, 'lo': lo.round(6).tolist(), 'hi': hi.round(6).tolist(), 'hubs': hubs, 'mats': {}, 'tex': tex, 'meshes': out}
line = 'const ZUK_ASSET = ' + json.dumps(asset, separators=(',', ':')) + ';'
html = (root / 'index.html').read_text()
if re.search(r'^const ZUK_ASSET = .*;$', html, flags=re.M): html = re.sub(r'^const ZUK_ASSET = .*;$', lambda m: line, html, count=1, flags=re.M)
else: html = html.replace('/* ======== PARTS:', line + '\n/* ======== PARTS:', 1)
(root / 'index.html').write_text(html)
import os
if os.environ.get('ZUK_CHECK'): d.resize((1024, 1024)).save(os.environ['ZUK_CHECK'])
print(f'{sum(len(m[3]) for m in meshes)} triangles kept ({dropped} faces dropped), {len(line) // 1024} KB; scale {s:.5f}')
print('groups', {k: len(v) for k, v in groups.items()}); print('hubs', hubs); print('bounds', lo.round(4).tolist(), hi.round(4).tolist())
