#!/usr/bin/env python3
"""Build viewer.html (a rotatable model of the car) from the car code in index.html.

The viewer reuses buildCar() and the textures it needs straight from the game, so any change to the
car in index.html shows up in the viewer after re-running:  python3 tools/make_viewer.py
"""
import base64, pathlib, re

root = pathlib.Path(__file__).resolve().parent.parent
game = (root / 'index.html').read_text()

def between(start, end, inclusive_end=False):
    i = game.index(start); j = game.index(end, i)
    return game[i:j + (len(end) if inclusive_end else 0)]

def line(prefix):
    m = re.search(r'^' + re.escape(prefix) + r'.*$', game, re.M)
    return m.group(0)

parts = [
    line('const clamp = '), line('function rng(seed)'),
    between('function tex(', 'const T = {};', True),
    line('T.blob = tex('), line('T.blob.wrapS'), line('T.tire = tex('),
    between('T.fivespoke = tex(', 'T.fivespoke.wrapS = T.fivespoke.wrapT = THREE.ClampToEdgeWrapping;', True),
    line('const lam = '), line('function box('),
    between('const CAR = {', 'const carObj = buildCar();'),
]
car_code = '\n'.join(parts)
ref = base64.b64encode((root / 'tools' / 'reference-s14.jpg').read_bytes()).decode()
tpl = (root / 'tools' / 'viewer_template.html').read_text()
out = tpl.replace('/*__CAR_CODE__*/', car_code).replace('__REF_JPG__', ref)
(root / 'viewer.html').write_text(out)
print('wrote viewer.html', len(out) // 1024, 'KB')
