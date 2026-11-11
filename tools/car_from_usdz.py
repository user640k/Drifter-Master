#!/usr/bin/env python3
"""Convert the owner's car model (a USDZ, e.g. BMW_2002ti.usdz unzipped to scene.usdc + textures) into the
compact BODY_ASSET embedded in index.html.

    python3 tools/car_from_usdz.py <path/to/scene.usdc> [<textures dir>]

Needs:  pip install usd-core fast-simplification numpy

- The shell is never reshaped: it is only scaled uniformly to true 1/10 of the real car (the model's wheelbase
  maps to REAL_WB / 10) and turned into the game's frame (x right, y up, z forward), with the wheelbase centred on
  z = 0 and the tyres on the ground. Everything else in the game (chassis, wheels, posts, physics) fits to it.
- Dense parts are decimated to keep the game light (the shape stays; mostly fine window seals lose triangles).
- Parts are grouped by material role; wheels are split per corner (relative to their hub) so they can steer and
  spin; the side mirrors are split left / right so each can break off on its own.
- Material colours come from the model's UsdPreviewSurface shaders.
"""
import base64, json, pathlib, re, sys
import numpy as np
from pxr import Usd, UsdGeom, UsdShade
import fast_simplification

REAL_WB = 2.500            # m, real car's wheelbase (BMW 2002: 2500 mm)
TARGET_MAX_FACES = 4200    # per part before decimation kicks in
ROLE = {  # material -> role in the game
    'CP_Body_Orange': 'paint', 'CP_Chromium': 'chrome', 'CP_Black': 'black', 'CP_White': 'lens', 'CP_Glass': 'glass',
    'Front_Red_Light': 'redlight', 'Emblem': 'emblem', 'BMW_2002__0': 'badge', 'disk': 'disk', 'balata': 'caliper',
    'jant': 'rim', 'Lastik': 'tire',
}
src = pathlib.Path(sys.argv[1]); texdir = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else src.parent
root = pathlib.Path(__file__).resolve().parent.parent
st = Usd.Stage.Open(str(src))

def mat_info(m):
    out = {'color': [0.7, 0.7, 0.7], 'metal': 0.0, 'rough': 0.5, 'opacity': 1.0, 'tex': None}
    for sh in m.GetPrim().GetChildren():
        s = UsdShade.Shader(sh)
        if not s or s.GetIdAttr().Get() != 'UsdPreviewSurface': continue
        for name, key in (('diffuseColor', 'color'), ('metallic', 'metal'), ('roughness', 'rough'), ('opacity', 'opacity')):
            i = s.GetInput(name)
            if not i: continue
            if i.HasConnectedSource():
                src_sh = i.GetConnectedSource()[0].GetPrim()
                f = UsdShade.Shader(src_sh).GetInput('file')
                if key == 'color' and f and f.Get(): out['tex'] = pathlib.Path(str(f.Get().path)).stem
                continue
            v = i.Get()
            if v is not None: out[key] = [float(c) for c in v] if key == 'color' else float(v)
    return out

parts, mats = [], {}
for p in st.Traverse():
    if not p.IsA(UsdGeom.Mesh): continue
    mesh = UsdGeom.Mesh(p)
    pts = np.array(mesh.GetPointsAttr().Get(), dtype=np.float64)
    counts = mesh.GetFaceVertexCountsAttr().Get(); idx = mesh.GetFaceVertexIndicesAttr().Get()
    xf = np.array(UsdGeom.Xformable(p).ComputeLocalToWorldTransform(Usd.TimeCode.Default()), dtype=np.float64)
    pts = (np.c_[pts, np.ones(len(pts))] @ xf)[:, :3]
    tris, uvsrc, k = [], None, 0
    for c in counts:
        for j in range(1, c - 1): tris.append([idx[k], idx[k + j], idx[k + j + 1]])
        k += c
    tris = np.array(tris, dtype=np.int64)
    m = UsdShade.MaterialBindingAPI(p).ComputeBoundMaterial()[0]
    mname = m.GetPath().name if m else 'none'
    if mname not in mats: mats[mname] = mat_info(m) if m else {'color': [0.5, 0.5, 0.5]}
    uv = None
    if mats[mname].get('tex'):
        pv = UsdGeom.PrimvarsAPI(p).GetPrimvar('st')
        if pv and pv.Get() is not None:
            vals = np.array(pv.Get(), dtype=np.float64); interp = pv.GetInterpolation(); ind = pv.GetIndices()
            if ind: vals = vals[np.array(ind)]
            if interp == 'faceVarying':  # unweld so each corner keeps its uv
                fv = []; k = 0
                for c in counts:
                    for j in range(1, c - 1): fv.append([k, k + j, k + j + 1])
                    k += c
                fv = np.array(fv); pts = pts[tris.reshape(-1)]; uv = vals[fv.reshape(-1)]; tris = np.arange(len(pts)).reshape(-1, 3)
            else: uv = vals
    parts.append({'name': p.GetName(), 'mat': mname, 'pts': pts, 'tris': tris, 'uv': uv})

# frame: model x is the length (front at -x), y up, z the width -> game x = model z, y = y, z = -model x
for q in parts: q['pts'] = np.c_[q['pts'][:, 2], q['pts'][:, 1], -q['pts'][:, 0]]
tyres = [q for q in parts if ROLE.get(q['mat']) == 'tire']
cent = [q['pts'].mean(0) for q in tyres]
front = [c for c in cent if c[2] > np.mean([c[2] for c in cent])]; rear = [c for c in cent if c[2] <= np.mean([c[2] for c in cent])]
zf, zr = np.mean([c[2] for c in front]), np.mean([c[2] for c in rear])
scale = REAL_WB / 10 / (zf - zr)
ground = min(q['pts'][:, 1].min() for q in tyres)
off = np.array([0, ground, (zf + zr) / 2])
for q in parts: q['pts'] = (q['pts'] - off) * scale

# wheels per corner: tyre bbox centre is the hub; rims, discs and calipers go to the nearest hub
hubs = {}
for q in tyres:
    c = (q['pts'].min(0) + q['pts'].max(0)) / 2
    key = ('F' if c[2] > 0 else 'R') + ('R' if c[0] > 0 else 'L'); hubs[key] = c
def nearest_hub(c): return min(hubs, key=lambda k: np.linalg.norm(hubs[k] - c))
# split meshes whose triangles sit on both sides of the car into L / R where useful (mirrors)
out_parts = []
for q in parts:
    role = ROLE.get(q['mat'], 'black')
    if q['name'].startswith('side_mirrors'):
        for side, keep in (('R', lambda x: x > 0), ('L', lambda x: x <= 0)):
            cx = q['pts'][q['tris']].mean(1)[:, 0]; t = q['tris'][keep(cx)]
            out_parts.append({**q, 'tris': t, 'name': 'mirror_' + side, 'role': role})
        continue
    out_parts.append({**q, 'role': role})

def weld(q):  # merge duplicate vertices so the simplifier can collapse across seams
    key = np.round(q['pts'] / 2e-6).astype(np.int64)
    _, first, inv = np.unique(key, axis=0, return_index=True, return_inverse=True)
    return {**q, 'pts': q['pts'][first], 'tris': inv.reshape(-1)[q['tris']]}

CLUSTER = 0.0006  # m: vertex-clustering grid for the dense trim parts (they are thousands of tiny separate pieces)
def decimate(q):
    n = len(q['tris'])
    if n <= TARGET_MAX_FACES or q['uv'] is not None or q['role'] in ('paint', 'glass', 'tire', 'rim', 'disk', 'caliper'): return q
    q = weld(q); t = q['tris']
    # quadric reduction first, then vertex clustering, which also merges neighbouring pieces
    p2, t2 = fast_simplification.simplify(q['pts'].astype(np.float32), t.astype(np.int64), target_reduction=0.5, agg=8)
    pts, tris = p2.astype(np.float64), t2.astype(np.int64)
    if len(tris) > TARGET_MAX_FACES:
        key = np.floor(pts / CLUSTER).astype(np.int64); _, inv, cnt = np.unique(key, axis=0, return_inverse=True, return_counts=True)
        inv = inv.reshape(-1); cen = np.zeros((len(cnt), 3)); np.add.at(cen, inv, pts); cen /= cnt[:, None]
        tris = inv[tris]; tris = tris[(tris[:, 0] != tris[:, 1]) & (tris[:, 1] != tris[:, 2]) & (tris[:, 0] != tris[:, 2])]
        tris = np.unique(np.sort(tris, axis=1), axis=0, return_index=True)[1]; tris = inv_tris = None
        t3 = inv[t2.astype(np.int64)]; ok = (t3[:, 0] != t3[:, 1]) & (t3[:, 1] != t3[:, 2]) & (t3[:, 0] != t3[:, 2]); t3 = t3[ok]
        _, first = np.unique(np.sort(t3, axis=1), axis=0, return_index=True); t3 = t3[np.sort(first)]
        pts, tris = cen, t3
    return {**q, 'pts': pts, 'tris': tris}

def compact(q):
    used = np.unique(q['tris']); remap = -np.ones(len(q['pts']), dtype=np.int64); remap[used] = np.arange(len(used))
    return {**q, 'pts': q['pts'][used], 'tris': remap[q['tris']], 'uv': None if q['uv'] is None else q['uv'][used]}

meshes, faces = [], 0
allp = np.concatenate([q['pts'] for q in out_parts]); lo, hi = allp.min(0), allp.max(0)
for q in out_parts:
    if len(q['tris']) == 0: continue
    q = compact(decimate(compact(q)))
    wheel = None; hub = None
    if q['role'] in ('tire', 'rim', 'disk', 'caliper'): wheel = nearest_hub((q['pts'].min(0) + q['pts'].max(0)) / 2); hub = hubs[wheel].tolist()
    qp = np.round((q['pts'] - lo) / (hi - lo) * 65535).astype(np.uint16)
    big = len(q['pts']) > 65535
    ib = q['tris'].astype(np.uint32 if big else np.uint16).reshape(-1)
    e = {'name': q['name'], 'role': q['role'], 'mat': q['mat'], 'wheel': wheel, 'hub': hub, 'n32': big,
         'pos': base64.b64encode(qp.tobytes()).decode(), 'idx': base64.b64encode(ib.tobytes()).decode()}
    if q['uv'] is not None: e['uv'] = base64.b64encode(np.round(np.clip(q['uv'], 0, 1) * 65535).astype(np.uint16).tobytes()).decode()
    meshes.append(e); faces += len(q['tris'])
tex = {}
for mname, mi in mats.items():
    if mi.get('tex'):
        f = next((x for x in texdir.rglob('*') if x.stem.startswith(mi['tex']) or mi['tex'].startswith(x.stem)), None)
        if f: tex[mi['tex']] = base64.b64encode(f.read_bytes()).decode()
asset = {'scale': scale, 'lo': lo.round(6).tolist(), 'hi': hi.round(6).tolist(), 'hubs': {k: v.round(5).tolist() for k, v in hubs.items()},
         'mats': {k: {kk: vv for kk, vv in v.items()} for k, v in mats.items()}, 'tex': tex, 'meshes': meshes}
line = 'const BODY_ASSET = ' + json.dumps(asset, separators=(',', ':')) + ';'
html = (root / 'index.html').read_text()
if re.search(r'^const BODY_ASSET = .*;$', html, flags=re.M): html = re.sub(r'^const BODY_ASSET = .*;$', lambda m: line, html, count=1, flags=re.M)
else: html = html.replace('/* ======== PARTS:', line + '\n/* ======== PARTS:', 1)
(root / 'index.html').write_text(html)
print(f'{len(meshes)} meshes, {faces} triangles, {len(line) // 1024} KB; scale {scale:.6f} m per model unit')
print('hubs', {k: [round(x, 4) for x in v] for k, v in hubs.items()}); print('bounds', lo.round(4).tolist(), hi.round(4).tolist())
