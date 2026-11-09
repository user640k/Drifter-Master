#!/usr/bin/env python3
"""Package the game for itch.io as an HTML5 upload: dist/drift-master-itch.zip

index.html is written as a page fragment (it is also published as a claude.ai artifact), so this
wraps it in a full HTML document and swaps the three.js CDN script for a bundled local copy,
so the game loads even if a CDN is blocked. Upload the zip on itch.io as "HTML" and tick
"This file will be played in the browser"; set the embed size to 1280 x 760.
"""
import pathlib, zipfile
root = pathlib.Path(__file__).resolve().parent.parent
page = (root / 'index.html').read_text()
cdn = '<script src="https://cdnjs.cloudflare.com/ajax/libs/three.js/r128/three.min.js"></script>'
assert cdn in page
page = page.replace(cdn, '<script src="three.min.js"></script>')
doc = ('<!doctype html>\n<html lang="en">\n<head>\n<meta charset="utf-8">\n'
       '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n'
       '<style>html,body{margin:0}</style>\n</head>\n<body>\n' + page + '\n</body>\n</html>\n')
dist = root / 'dist'; dist.mkdir(exist_ok=True)
out = dist / 'drift-master-itch.zip'
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as z:
    z.writestr('index.html', doc)
    z.write(root / 'tools' / 'vendor' / 'three.r128.min.js', 'three.min.js')
    z.write(root / 'tools' / 'vendor' / 'THREE-LICENSE.txt', 'THREE-LICENSE.txt')
print('wrote', out.relative_to(root), out.stat().st_size // 1024, 'KB')
