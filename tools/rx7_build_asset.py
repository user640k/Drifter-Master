"""Run tools/rx7_parse_obj.py <RX7_OBJ.obj> first (writes rx7_parts.pkl), then this script.
The source model and textures are not in the repo (51 MB OBJ); they were supplied as free to use.
Convert the RX-7 OBJ into a compact, game-ready asset (JSON with base64 typed arrays).
Parts are grouped by material role; each wheel is split into a spinning assembly and a fixed caliper.
Coordinates go to the game's model space (+x forward, +y up, +z right) at 1/10.2 scale."""
import pickle, json, base64, numpy as np, fast_simplification as fs, sys
D = pickle.load(open('rx7_parts.pkl', 'rb')); V, VT, parts = D['V'], D['VT'], D['parts']
S = 0.257 / 2.52                       # real wheelbase 2.52 m -> RC wheelbase 257 mm
WHEELS = {'RL': (0.72, -1.18), 'RR': (-0.72, -1.18), 'FL': (0.72, 1.34), 'FR': (-0.72, 1.34)}  # (obj x, obj z)
HUB_Y = 0.34
def to_model(P):  # obj (x,y,z) -> model (x fwd, y up, z right)
    return np.stack([P[:, 2], P[:, 1], -P[:, 0]], 1) * S
def role(p):
    g, m = p['g'], p['m']; c = (p['bmin'] + p['bmax']) / 2
    # wheel parts are the v_cross_hood_wheel_* pieces and the 'te' rims; match those names only
    # (a plain 'Cylinder' match also caught the exhaust, pCylinder7, which then spun with a wheel)
    wheelish = 'v_cross_hood_wheel' in g or m.startswith('wheel_cosmic')
    if wheelish:
        w = min(WHEELS, key=lambda k: (WHEELS[k][0] - c[0]) ** 2 + (WHEELS[k][1] - c[2]) ** 2)
        if 'Support' in g: return ('caliper', w)
        if 'Tire' in g: return ('tire', w)
        if 'Cylinder' in g: return ('disc', w)
        if m.startswith('wheel_cosmic'): return ('rim', w)
        return ('lug', w)
    if m == 'aiStandardSurface1SG':
        if 'polySurface526' in g: return ('wing', None)
        return ('paint', None)
    if m == 'aiStandardSurface33SG': return ('glass', None)
    if m in ('aiStandardSurface4SG', 'aiStandardSurface54SG', 'lambert8SG', 'blinn9SG'): return ('trim', None)
    if m == 'aiStandardSurface39SG': return ('mirror', None)
    if m == 'aiStandardSurface10SG': return ('chrome', None)
    if m in ('aiStandardSurface6SG', 'aiStandardSurface7SG'): return ('grille', None)
    if m in ('aiStandardSurface52SG', 'aiStandardSurface55SG'): return ('plate', None)
    if m == 'blinn7SG': return ('amber', None)
    front = c[2] > 1.0
    if m == 'aiStandardSurface16SG': return ('headlens', None)
    if m == 'aiStandardSurface18SG': return ('reflector', None)
    if m in ('aiStandardSurface14SG', 'aiStandardSurface15SG'): return ('paint', None) if c[1] > 0.6 else ('lenscorner', None)  # closed pop-up covers are body colour
    if m == 'aiStandardSurface19SG': return ('amber', None)
    if m == 'blinn10SG': return ('lenscorner', None) if front else ('taillamp', None)
    if m in ('aiStandardSurface12SG', 'aiStandardSurface13SG'): return ('tailbar', None)
    return ('trim', None)
# keep ratio per role (fraction of triangles kept), with a floor
KEEP = {'paint': 0.8, 'wing': 0.9, 'glass': 0.6, 'trim': 0.6, 'mirror': 0.5, 'chrome': 0.35, 'grille': 1, 'plate': 1, 'amber': 0.6,
        'headlens': 0.4, 'reflector': 0.5, 'housing': 0.6, 'lenscorner': 0.5, 'taillamp': 0.5, 'tailbar': 0.35,
        'tire': 0.08, 'rim': 0.38, 'disc': 0.08, 'caliper': 0.04, 'lug': 0.25}
UVROLES = {'taillamp', 'tailbar'}
groups = {}
for p in parts:
    r, w = role(p); groups.setdefault((r, w), []).append(p)
out = {'scale': S, 'meshes': []}
tot = 0
allP = to_model(V)
lo, hi = allP.min(0) - 0.001, allP.max(0) + 0.001
for (r, w), ps in sorted(groups.items(), key=lambda kv: (kv[0][0], str(kv[0][1]))):
    F = np.concatenate([p['f'] for p in ps]); UVI = np.concatenate([p['uv'] for p in ps])
    uniq, inv = np.unique(F.reshape(-1), return_inverse=True)
    P = allP[uniq]; Fl = inv.reshape(-1, 3)
    uv = None
    if r in UVROLES:
        # per-vertex uv from the first face corner that uses it
        uvv = np.zeros((len(uniq), 2), np.float32); flat = UVI.reshape(-1)
        uvv[inv] = VT[np.maximum(flat, 0)]; uv = uvv
    keep = KEEP.get(r, 0.5)
    if keep < 0.999 and len(Fl) > 60 and uv is None:
        P2, F2 = fs.simplify(P.astype(np.float32), Fl.astype(np.int32), target_reduction=1 - keep)
        if len(F2) > 8: P, Fl = P2, F2
    q = np.round((P - lo) / (hi - lo) * 65535).astype(np.uint16)
    ent = {'role': r, 'wheel': w, 'pos': base64.b64encode(q.tobytes()).decode(), 'idx': base64.b64encode(Fl.astype(np.uint16 if len(P) < 65536 else np.uint32).tobytes()).decode(), 'i32': len(P) >= 65536}
    if w is not None: ent['hub'] = (to_model(np.array([[WHEELS[w][0], HUB_Y, WHEELS[w][1]]], np.float32))[0]).round(5).tolist()
    if uv is not None: ent['uv'] = base64.b64encode(np.clip(np.round(uv * 65535), 0, 65535).astype(np.uint16).tobytes()).decode()
    out['meshes'].append(ent); tot += len(Fl)
    print(f"{r:10s} {str(w):4s} parts {len(ps):3d} tris {len(F):6d} -> {len(Fl):6d}")
out['lo'] = lo.round(6).tolist(); out['hi'] = hi.round(6).tolist()
js = json.dumps(out, separators=(',', ':'))
open('rx7_asset.json', 'w').write(js)
print('total tris', tot, 'json KB', len(js) // 1024)
