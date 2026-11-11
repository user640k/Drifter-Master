#!/usr/bin/env python3
"""Build parts.html (the Parts Lab: the original car's parts on a turntable) from the PARTS section of index.html.

The parts are written once, in the game file between the PARTS markers, and this page shows them as they are:
    python3 tools/make_parts.py
"""
import pathlib, re

root = pathlib.Path(__file__).resolve().parent.parent
game = (root / 'index.html').read_text()

def between(start, end, inclusive_end=False):
    i = game.index(start); j = game.index(end, i)
    return game[i:j + (len(end) if inclusive_end else 0)]

def line(prefix):
    return re.search(r'^' + re.escape(prefix) + r'.*$', game, re.M).group(0)

code = '\n'.join([
    line('const clamp = '), line('function rng(seed)'),
    between('function tex(', 'const IMG = '),
    line('const BODY_ASSET = '),  # the owner's car model (tools/car_from_usdz.py)
    between('/* ======== PARTS:', '/* ======== END PARTS ======== */', True),
])
out = (root / 'tools' / 'parts_template.html').read_text().replace('/*__PARTS_CODE__*/', code)
(root / 'parts.html').write_text(out)
print('wrote parts.html', len(out) // 1024, 'KB')
