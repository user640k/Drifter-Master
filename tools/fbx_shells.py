#!/usr/bin/env python3
"""Convert the owner's FBX car models into RC drift shells embedded in index.html (one `const <KEY>_ASSET = ...;` line each).

    uv venv -p 3.11 bpyenv && uv pip install -p bpyenv/bin/python bpy numpy pillow
    bpyenv/bin/python tools/fbx_shells.py <key> <model.fbx> [<texture dir>]      keys: muscle, ute, coupe, pickup

Same rules as tools/zuk_from_blend.py: the shape is never edited, only scaled uniformly to true 1/10 of the real car (by its
wheelbase) and turned into the game's frame (x = the car's left, y up, z forward), wheelbase centred on z = 0, tyres on the
ground. It is emptied into an RC body: the wheels, the floor pan and underbody, the interior and the maker's badges and
plates are left out (the game's own chassis, wheels and 640kFPV plate go on). What is left is split by role, so the game
can give each its material: paint (recoloured by the car screen), satin (the chrome trim, as a chrome delete, like the
sedan), black, glass, lens (headlamps), redlight, amber, and for a textured model one textured body whose paint is
recoloured through a paint mask. Mirrors become their own parts (they break off).
The asset also carries `meta`: measured points the game fits things to (wheelbase, width, roof, windscreen, mirrors, lamps,
plate), so nothing is placed by hand.
"""
import base64, io, json, os, pathlib, re, sys, collections
import bpy, bmesh
import numpy as np
from PIL import Image, ImageFilter

CFG = {
    # a 1970s four-door muscle sedan: detailed, untextured paint; separate objects
    'muscle': dict(wb=2.946, fwd='+y', decimate=0.55, wheel=dict(objects=['wheels']),
                   drop_objects=['ground', 'badge3', 'badges', 'badges 2', 'Circle.002', 'screws', 'plate'],
                   mats={'M_Paint': 'paint', 'M_Black': 'black', 'M_Chrome': 'satin', 'M_Glass': 'glass', 'M_Lights': 'lamp',
                         'M_License': 'black', 'M_Badge': None, 'M_Wheel': None, 'M_Dome': None}),
    # a coupe-utility (car-based pickup), low poly
    'ute': dict(wb=2.946, fwd='-y', wheel=dict(lowpoly=True),
                mats={'Paint': 'paint', 'Paint2': 'paint', 'Chrome': 'satin', 'Black': 'black', 'Plastic': 'black', 'Glass': 'glass',
                      'Lights': 'lamp', 'Taillights': 'redlight', 'Indicator': 'amber', 'Plate': None, 'Logo': None}),
    # a 1990s sports coupe with a wide body kit, low poly
    'coupe': dict(wb=2.55, fwd='-y', wheel=dict(lowpoly=True),
                  mats={'Paint': 'paint', 'Chrome': 'satin', 'Black': 'black', 'Plastic': 'black', 'Glass': 'glass',
                        'Lights': 'lamp', 'Taillights': 'redlight', 'Indicator': 'amber'}),
    # a 1960s pickup truck, weathered two-tone paint in texture atlases (Front, Back): kept, the teal recoloured
    'pickup': dict(wb=2.921, fwd='-x', decimate=0.4, wheel=dict(mats=['Wheel']),
                   mats={'Front': 'tex', 'Back': 'tex', 'Glass': 'glass', 'Bottom': None, 'Inside': None, 'Wheel': None},
                   atlas=[('Front', 'T_Front_C.tga.png'), ('Back', 'T_Back_C.tga.png')]),
}

args = [a for a in sys.argv[sys.argv.index('--') + 1:]] if '--' in sys.argv else sys.argv[1:]
key, src = args[0], args[1]; texdir = pathlib.Path(args[2]) if len(args) > 2 else None
C = CFG[key]; root = pathlib.Path(__file__).resolve().parent.parent

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
objs = [o for o in bpy.data.objects if o.type == 'MESH' and o.name not in C.get('drop_objects', [])]

# model frame -> (left, up, forward) before scaling
def turn(P):
    P = np.asarray(P, dtype=np.float64)
    if C['fwd'] == '-y': return np.stack([P[..., 0], P[..., 2], -P[..., 1]], -1)
    if C['fwd'] == '+y': return np.stack([-P[..., 0], P[..., 2], P[..., 1]], -1)
    if C['fwd'] == '-x': return np.stack([-P[..., 1], P[..., 2], -P[..., 0]], -1)
    raise ValueError(C['fwd'])

# ---- pass 1: every triangle with its object, material, loose part and corners (world space, turned)
def grab(o):
    bm = bmesh.new(); bm.from_mesh(o.data); bm.transform(o.matrix_world)
    bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    uvl = bm.loops.layers.uv.active; mats = [m.name if m else '?' for m in o.data.materials]
    parent = {}
    def find(a):
        while parent.setdefault(a, a) != a: parent[a] = parent[parent[a]]; a = parent[a]
        return a
    for f in bm.faces:
        vs = [v.index for v in f.verts]
        for v in vs[1:]: parent[find(v)] = find(vs[0])
    out = []
    for f in bm.faces:
        out.append(dict(obj=o.name, mat=mats[f.material_index] if f.material_index < len(mats) else '?', part=(o.name, find(f.verts[0].index)),
                        vid=[(o.name, l.vert.index) for l in f.loops], P=turn([l.vert.co[:] for l in f.loops]),
                        uv=[tuple(l[uvl].uv) if uvl else (0.0, 0.0) for l in f.loops]))
    bm.free(); return out

def decimate(ratio, drop_mats):
    for o in objs:
        me = o.data; bm = bmesh.new(); bm.from_mesh(me)
        dm = {i for i, m in enumerate(me.materials) if m and C['mats'].get(m.name, 'x') is None}
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index in dm], context='FACES'); bm.to_mesh(me); bm.free()
        mod = o.modifiers.new('dec', 'DECIMATE'); mod.ratio = ratio
        bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='dec')

tris = [t for o in objs for t in grab(o)]

# ---- wheels and hubs
W = C['wheel']
if 'objects' in W: wheel_tri = lambda t: t['obj'] in W['objects']
elif 'mats' in W: wheel_tri = lambda t: t['mat'] in W['mats']
else: wheel_tri = None
if wheel_tri is None:  # low poly: the tyres are the loose parts touching the ground at the four corners
    parts = collections.defaultdict(list)
    for t in tris: parts[t['part']].append(t)
    pb = {k: (np.min([t['P'].min(0) for t in v], 0), np.max([t['P'].max(0) for t in v], 0)) for k, v in parts.items()}
    tyre_parts = [k for k, (lo, hi) in pb.items() if lo[1] < 0.06 and hi[1] > 0.4 and abs(lo[0] + hi[0]) / 2 > 0.4 and len(parts[k]) > 100]
    wheel_set = set(tyre_parts)
    wheel_tri = lambda t: t['part'] in wheel_set
wt = [t for t in tris if wheel_tri(t)]
WP = np.concatenate([t['P'] for t in wt])
ground = WP[:, 1].min()
hubs_m = {}
for fr in (1, -1):
    for lr in (1, -1):
        sel = WP[(np.sign(WP[:, 2] - WP[:, 2].mean()) == fr) & (np.sign(WP[:, 0]) == lr)]
        hubs_m[('F' if fr > 0 else 'R') + ('R' if lr > 0 else 'L')] = (sel.min(0) + sel.max(0)) / 2   # key R = +x (as in the 2002 asset)
zF = (hubs_m['FR'][2] + hubs_m['FL'][2]) / 2; zR = (hubs_m['RR'][2] + hubs_m['RL'][2]) / 2
s = C['wb'] / 10 / (zF - zR); zc = (zF + zR) / 2
def game(P): return (np.asarray(P) - np.array([0, ground, zc])) * s
hubs = {k: [round(float(x), 5) for x in game(v)] for k, v in hubs_m.items()}
R = float(np.mean([(WP[:, 1].max() - ground) / 2])) * s     # tyre radius at 1/10
if wheel_tri is not None and 'tyre_parts' in dir():         # low poly: also everything inside the wheel (rims, discs, caps)
    for k, (lo, hi) in pb.items():
        c = game((lo + hi) / 2); ext = (hi - lo) * s
        if any(abs(c[2] - h[2]) < R * 1.1 and abs(c[1] - h[1]) < R * 1.1 and ext[1] < 2.3 * R and abs(c[0]) > 0.04 for h in hubs.values()): wheel_set.add(k)

# ---- decimate the dense models now that the hubs are known (dropped materials go first)
if C.get('decimate'):
    decimate(C['decimate'], None)
    tris = [t for o in objs for t in grab(o)]
    if 'objects' in W: tris = [t for t in tris if t['obj'] not in W['objects']]

# ---- roles, underbody, mirrors
for t in tris: t['G'] = game(t['P']); t['c'] = t['G'].mean(0)
def role_of(t):
    if wheel_tri(t): return None
    r = C['mats'].get(t['mat'], 'black')
    if r == 'lamp': r = 'lens' if t['c'][2] > 0 else 'redlight'
    return r
keep = [t for t in tris if role_of(t)]
for t in keep: t['role'] = role_of(t)
allc = np.array([t['c'] for t in keep]); allP = np.concatenate([t['G'] for t in keep])
halfW_all = np.abs(allP[:, 0]).max(); roof = allP[:, 1].max()
hy = np.mean([h[1] for h in hubs.values()]); hzF = hubs['FR'][2]; hzR = hubs['RR'][2]
def nrm(t):
    n = np.cross(t['G'][1] - t['G'][0], t['G'][2] - t['G'][0]); L = np.linalg.norm(n); return n / L if L > 0 else n
under = 0
def is_under(t):
    c = t['c']; inboard = abs(c[0]) < 0.8 * halfW_all
    floor = inboard and c[1] < hy * 1.3 and hzR + R * 0.6 < c[2] < hzF - R * 0.6       # floor pan between the axles
    belly = inboard and c[1] < hy * 1.6 and nrm(t)[1] < -0.7                             # anything facing down underneath
    return floor or belly
k2 = []
for t in keep:
    if is_under(t): under += 1
    else: k2.append(t)
keep = k2
# mirrors: small loose parts sticking out widest at shoulder height, near the middle of the car
parts = collections.defaultdict(list)
for t in keep: parts[t['part']].append(t)
pbx = {k: (np.min([t['G'].min(0) for t in v], 0), np.max([t['G'].max(0) for t in v], 0), len(v)) for k, v in parts.items()}
mirror_of = {}
for side in (1, -1):
    cands = [(k, lo, hi) for k, (lo, hi, n) in pbx.items() if n < 1500 and side * (lo[0] + hi[0]) / 2 > 0.5 * halfW_all
             and 0.5 * roof < (lo[1] + hi[1]) / 2 < 0.9 * roof and abs((lo[2] + hi[2]) / 2) < 0.6 * (hzF - hzR) and (hi[2] - lo[2]) < 0.3 * (hzF - hzR)]
    if not cands: continue
    best = max(cands, key=lambda c: max(abs(c[1][0]), abs(c[2][0])))
    if max(abs(best[1][0]), abs(best[2][0])) < 0.96 * halfW_all: continue
    blo, bhi = best[1] - 0.004, best[2] + 0.004
    for k, (lo, hi, n) in pbx.items():
        if n < 1500 and np.all(lo >= blo) and np.all(hi <= bhi): mirror_of[k] = 'mirror_R' if side > 0 else 'mirror_L'
body_nm = [t for t in keep if t['part'] not in mirror_of]
bP = np.concatenate([t['G'] for t in body_nm]); halfW = np.abs(bP[:, 0]).max()

# ---- meshes: one per role (all the paint in one 'body' mesh), mirrors separate
meshes = collections.defaultdict(list)
for t in keep:
    name = mirror_of.get(t['part']) or ('body' if t['role'] in ('paint', 'tex') else t['role'])
    meshes[name].append(t)

# texture atlases (pickup): stack them in one image and move each material's uvs into its band
atlas_band = {}
tex = {}
if C.get('atlas'):
    ims = [(m, Image.open(texdir / f).convert('RGB')) for m, f in C['atlas']]
    Wd = 2048; ims = [(m, im.resize((Wd, int(Wd * im.size[1] / im.size[0])), Image.LANCZOS)) for m, im in ims]
    H = sum(im.size[1] for _, im in ims); big = Image.new('RGB', (Wd, H)); y = 0
    for m, im in ims: big.paste(im, (0, y)); atlas_band[m] = (y / H, im.size[1] / H); y += im.size[1]
    # clean it up: no rust, grime or dents, like a freshly painted RC body. Each pixel is sorted into teal paint, white
    # paint, or trim (dark, chrome, glass, lamps) by its colour; rust and grime are filled from the nearest clean pixel,
    # specks are smoothed out of the class map, and the paint is laid back flat (lighting does the shading)
    from scipy import ndimage
    A = np.asarray(big).astype(np.float32) / 255; mx, mn = A.max(2), A.min(2); sat = (mx - mn) / np.maximum(mx, 1e-3); R_, G_, B_ = A[..., 0], A[..., 1], A[..., 2]
    teal = (B_ > R_ + 0.05) & (G_ > R_ + 0.03) & (sat > 0.12) & (mx > 0.22)
    white = (mx > 0.45) & (sat < 0.22) & ~teal & (R_ - B_ < 0.12)
    dark = ndimage.uniform_filter((mx < 0.2).astype(np.float32), 15) > 0.6                 # coherent black trim
    grey = ndimage.uniform_filter(((sat < 0.08) & (mx > 0.22) & (mx < 0.5)).astype(np.float32), 15) > 0.55  # chrome / grey trim
    cls = np.full(mx.shape, -1, np.int8); cls[teal] = 0; cls[white & ~grey] = 1; cls[(dark & (mx < 0.3)) | (grey & (sat < 0.1))] = 3
    unknown = cls < 0                                                                       # rust, grime, stains
    _, (iy, ix) = ndimage.distance_transform_edt(unknown, return_indices=True)
    cls = cls[iy, ix]; clean = A[iy, ix]                                                   # -> nearest clean pixel
    sm = np.stack([ndimage.uniform_filter((cls == k).astype(np.float32), 11) for k in (0, 1, 3)], -1)
    cls = np.array([0, 1, 3], np.int8)[sm.argmax(-1)]
    paintm = (cls == 0) | (cls == 1)   # teal and the white primer showing through where it flaked: all one paint now
    cls[cls == 1] = 0
    out = clean.copy(); cream = np.array([0.91, 0.90, 0.86], np.float32)
    out[cls == 1] = cream; out[cls == 0] = np.array([0.30, 0.66, 0.64], np.float32)
    trim = cls == 3; tl = ndimage.median_filter(clean, size=(5, 5, 1)); out[trim] = tl[trim]   # trim: specks smoothed
    big = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8))
    mask = Image.fromarray((paintm * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.0))
    shade = Image.new('L', big.size, 225)                                                   # flat paint: lighting shades it
    if os.environ.get('CLEAN_CHECK'): big.resize((1024, int(1024 * big.size[1] / big.size[0]))).save(os.environ['CLEAN_CHECK'])
    def jpg(im, w, q):
        b = io.BytesIO(); im.resize((w, int(w * im.size[1] / im.size[0])), Image.LANCZOS).save(b, 'JPEG', quality=q); return base64.b64encode(b.getvalue()).decode()
    tex = {'dif': jpg(big, 1024, 82), 'paint': jpg(Image.merge('RGB', (shade, mask, Image.new('L', big.size, 0))), 1024, 86)}

def uv_of(t, i):
    u, v = t['uv'][i]
    if t['mat'] in atlas_band:
        y0, h = atlas_band[t['mat']]; u = u % 1.0; v = v % 1.0
        return (u, 1 - (y0 + (1 - v) * h))    # image rows from the top; uv v from the bottom
    return (u, v)

out, allp = [], []
built = []
for name, ts in meshes.items():
    vmap, pos, uvs, idx = {}, [], [], []
    for t in ts:
        corner = []
        for i in range(3):
            uv = uv_of(t, i); kk = (t['vid'][i], round(uv[0], 5), round(uv[1], 5))
            if kk not in vmap: vmap[kk] = len(pos); pos.append(t['G'][i]); uvs.append(uv)
            corner.append(vmap[kk])
        idx.append(corner)
    role = ts[0]['role'] if name != 'body' else ('tex' if any(t['role'] == 'tex' for t in ts) else 'paint')
    if name.startswith('mirror'): role = collections.Counter(t['role'] for t in ts).most_common(1)[0][0]
    built.append((name, role, np.array(pos), np.array(uvs), np.array(idx))); allp.append(np.array(pos))
allp = np.concatenate(allp); lo, hi = allp.min(0), allp.max(0)
for name, role, pos, uvs, idx in built:
    qp = np.round((pos - lo) / (hi - lo) * 65535).astype(np.uint16); n32 = len(pos) > 65535
    m = {'name': name, 'role': role, 'mat': role, 'wheel': None, 'hub': None, 'n32': n32,
         'pos': base64.b64encode(qp.tobytes()).decode(), 'idx': base64.b64encode(idx.astype(np.uint32 if n32 else np.uint16).reshape(-1).tobytes()).decode()}
    if role == 'tex': m['uv'] = base64.b64encode(np.round(np.clip(np.c_[uvs[:, 0], 1 - uvs[:, 1]], 0, 1) * 65535).astype(np.uint16).tobytes()).decode()
    out.append(m)

# ---- measured points
def faces(pred): return [t for t in keep if pred(t)]
glass = faces(lambda t: t['role'] == 'glass')
ws = [t for t in glass if nrm(t)[2] * np.sign(nrm(t)[1] or 1) > 0.25 and abs(nrm(t)[1]) > 0.3 and t['c'][2] > -0.02]
def plane(ts, front):
    P = np.concatenate([t['G'] for t in ts]); n = np.mean([nrm(t) * np.sign(nrm(t)[1]) for t in ts], 0); n /= np.linalg.norm(n)
    top, base = P[:, 1].max(), P[:, 1].min()
    zt = P[P[:, 1] > top - 0.003][:, 2].mean(); zb = P[P[:, 1] < base + 0.003][:, 2].mean()
    return dict(top=[0, float(top), float(zt)], base=[0, float(base), float(zb)], n=n.round(4).tolist(), w=float(2 * np.abs(P[:, 0]).max()))
meta = dict(wbReal=C['wb'], halfW=float(halfW), roof=float(roof), z0=float(lo[2]), z1=float(hi[2]), R=R)
if ws: meta['ws'] = plane(ws, True)
rw = [t for t in glass if nrm(t)[2] * np.sign(nrm(t)[1] or 1) < -0.25 and abs(nrm(t)[1]) > 0.3 and t['c'][2] < 0]
if rw: meta['rw'] = plane(rw, False)
lens = faces(lambda t: t['role'] == 'lens')
if lens: Lp = np.concatenate([t['G'] for t in lens]); meta['lamp'] = [float(np.abs(Lp[:, 0]).mean()), float(Lp[:, 1].mean()), float(Lp[:, 2].max())]
plate_ts = [t for t in tris if t['mat'] in ('Plate', 'plate', 'M_License') and t['G'].mean(0)[2] < 0] if key != 'pickup' else []
if key == 'muscle': plate_ts = [t for t in tris if t['obj'] == 'plate' and t['G'].mean(0)[2] < 0]
mir = {v: None for v in mirror_of.values()}
for k, v in mirror_of.items():
    lo_, hi_ = pbx[k][0], pbx[k][1]; c = (lo_ + hi_) / 2
    mir[v] = c if mir[v] is None else (mir[v] + c) / 2
meta['mirrors'] = {k: [round(float(x), 4) for x in v] for k, v in mir.items() if v is not None}
# belt (shoulder) line: the highest paint at the door, below the glass
door = [t for t in body_nm if t['role'] in ('paint', 'tex') and abs(t['c'][2]) < 0.04 and abs(t['c'][0]) > 0.85 * halfW]
meta['belt'] = float(np.max([t['c'][1] for t in door])) if door else float(0.6 * roof)
asset = {'scale': s, 'lo': lo.round(6).tolist(), 'hi': hi.round(6).tolist(), 'hubs': hubs, 'mats': {}, 'tex': tex, 'meta': meta, 'meshes': out}
# the plate on the original model (we place our own 640kFPV plate there)
P_src = [t for t in tris if (t['mat'] in ('Plate',) or t['obj'] == 'plate') and t['G'].mean(0)[2] < 0]
if P_src:
    Pp = np.concatenate([t['G'] for t in P_src]); meta['plate'] = [0, float((Pp[:, 1].min() + Pp[:, 1].max()) / 2), float(Pp[:, 2].min()) - 0.0005,
                                                                 float(Pp[:, 0].max() - Pp[:, 0].min()), float(Pp[:, 1].max() - Pp[:, 1].min())]
name = key.upper() + '_ASSET'
line = f'const {name} = ' + json.dumps(asset, separators=(',', ':')) + ';'
html = (root / 'index.html').read_text()
if re.search(rf'^const {name} = .*;$', html, flags=re.M): html = re.sub(rf'^const {name} = .*;$', lambda m: line, html, count=1, flags=re.M)
else: html = html.replace('/* ======== PARTS:', line + '\n/* ======== PARTS:', 1)
(root / 'index.html').write_text(html)
print(f'{key}: {sum(len(b[4]) for b in built)} triangles, {under} underbody dropped, {len(line) // 1024} KB, scale {s:.5f}')
print(' meshes', {b[0]: (b[1], len(b[4])) for b in built})
print(' hubs', hubs, 'R', round(R, 4)); print(' bounds', lo.round(4).tolist(), hi.round(4).tolist())
print(' meta', json.dumps({k: v for k, v in meta.items()}, default=float)[:900])
