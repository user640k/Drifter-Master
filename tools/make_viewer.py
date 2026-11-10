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
    between('function tex(', 'const IMG = '), 'const T = {};',
    line('T.blob = tex('), line('T.blob.wrapS'), line('T.halo = tex('),
    line('const lam = '), line('function box('),
    between('const CAR = {', 'let carObj = buildCar();'),
]
car_code = '\n'.join(parts)
tpl = (root / 'tools' / 'viewer_template.html').read_text()
out = tpl.replace('/*__CAR_CODE__*/', car_code)
(root / 'viewer.html').write_text(out)
print('wrote viewer.html', len(out) // 1024, 'KB')
