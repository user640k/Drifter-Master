"""Shrink the downloaded CC0 / public-domain photos to small early-2000s style textures
(tools/textures/*.jpg) and embed them into index.html as `const IMG = {...}` (one line).

Usage: python3 tools/make_textures.py <download dir>
The download dir holds the source files named as in SOURCES below (see tools/textures/SOURCES.md)."""
import base64, io, json, pathlib, re, sys
from PIL import Image, ImageDraw, ImageEnhance, ImageOps

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / 'tools' / 'textures'
src = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else None

def load(name): return Image.open(src / name).convert('RGB')
def square(im, size): s = min(im.size); w, h = im.size; return im.crop(((w - s) // 2, (h - s) // 2, (w + s) // 2, (h + s) // 2)).resize((size, size), Image.LANCZOS)
def tint(im, rgb, amount=1.0):
    g = ImageOps.grayscale(im); col = ImageOps.colorize(g, (0, 0, 0), rgb).convert('RGB'); return Image.blend(im, col, amount)
def crunch(im):  # early-2000s look: a touch more contrast and saturation, posterised slightly
    im = ImageEnhance.Contrast(im).enhance(1.12); im = ImageEnhance.Color(im).enhance(1.15)
    return ImageOps.posterize(im, 6)

def grey(im): return ImageOps.grayscale(im).convert('RGB')
JOBS = {
    'curtain': lambda: crunch(ImageEnhance.Brightness(tint(square(load('velour_velvet.jpg'), 128), (140, 26, 34), 0.85)).enhance(0.9)),
    # RC arena
    'arTurf': lambda: crunch(ImageEnhance.Color(ImageEnhance.Brightness(square(load('Grass001_1K-JPG_Color.jpg'), 128)).enhance(1.35)).enhance(1.2)),
    'arEpoxy': lambda: crunch(ImageEnhance.Contrast(ImageEnhance.Brightness(grey(square(load('concrete_floor_01.jpg'), 256))).enhance(0.42)).enhance(1.5)),
    'arWall': lambda: crunch(tint(square(load('painted_plaster_wall.jpg'), 128), (150, 156, 168), 0.9)),
    'arConcrete': lambda: crunch(ImageEnhance.Brightness(grey(square(load('concrete_wall_004.jpg'), 256))).enhance(1.05)),
    'arFloor': lambda: crunch(ImageEnhance.Brightness(grey(square(load('hangar_concrete_floor.jpg'), 256))).enhance(2.3)),
    # neon blacklight track
    'neCarpet': lambda: crunch(ImageEnhance.Brightness(square(load('Carpet012_1K-JPG_Color.jpg'), 128)).enhance(0.55)),
    'nePlate': lambda: crunch(square(load('DiamondPlate001_1K-JPG_Color.jpg'), 128)),
}

if src:
    OUT.mkdir(exist_ok=True)
    for k, f in JOBS.items(): f().save(OUT / f'{k}.jpg', quality=78, optimize=True)
img = {k: base64.b64encode((OUT / f'{k}.jpg').read_bytes()).decode() for k in JOBS}
line = 'const IMG = ' + json.dumps(img, separators=(',', ':')) + ';'
html = (ROOT / 'index.html').read_text()
if re.search(r'^const IMG = .*;$', html, flags=re.M): html = re.sub(r'^const IMG = .*;$', lambda m: line, html, count=1, flags=re.M)
else: html = html.replace("const T = {};", line + "\nconst T = {};", 1)
(ROOT / 'index.html').write_text(html)
print('embedded', len(img), 'textures,', sum(len(v) for v in img.values()) // 1024, 'KB base64')
