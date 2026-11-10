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

# ---- textures sampled from the reference photo of the arena (ref_arena.jpg, supplied by the project owner) ----
def flatten(im, radius=40):
    # take out the photo's lighting: divide by a heavy blur, keep the mean colour
    from PIL import ImageFilter, ImageStat
    blur = im.filter(ImageFilter.GaussianBlur(radius)); mean = ImageStat.Stat(im).mean
    a, b = im.load(), blur.load(); out = Image.new('RGB', im.size); o = out.load()
    for y in range(im.height):
        for x in range(im.width):
            o[x, y] = tuple(max(0, min(255, int(a[x, y][c] / max(8, b[x, y][c]) * mean[c]))) for c in range(3))
    return out
def seamless(im):
    # blend with a half-shifted copy so opposite edges meet (classic offset-and-feather tiling)
    from PIL import ImageChops
    w, h = im.size; rolled = ImageChops.offset(im, w // 2, h // 2)
    mask = Image.new('L', (w, h)); m = mask.load()
    for y in range(h):
        for x in range(w): m[x, y] = int(255 * min(1, 2.2 * min(x, w - 1 - x) / w * 2) * min(1, 2.2 * min(y, h - 1 - y) / h * 2))
    return Image.composite(im, rolled, mask)
def ref(box, size, flat=40, tile=True, sharpen=1.3):
    im = load('ref_arena.jpg').crop(box).resize(size, Image.LANCZOS)
    if flat: im = flatten(im, flat)
    if tile: im = seamless(im)
    return ImageEnhance.Sharpness(im).enhance(sharpen)
def grainy(im, amp=10, seed=3):
    import random; r = random.Random(seed); px = im.load()
    for y in range(im.height):
        for x in range(im.width): n = int((r.random() - 0.5) * amp); px[x, y] = tuple(max(0, min(255, c + n)) for c in px[x, y])
    return im
def grey(im): return ImageOps.grayscale(im).convert('RGB')
JOBS = {
    # RC arena
    'arFloor': lambda: crunch(ImageEnhance.Brightness(grey(square(load('hangar_concrete_floor.jpg'), 256))).enhance(2.3)),
    # sampled from the reference photo
    'phTrack': lambda: crunch(ref((660, 1020, 1060, 1300), (256, 256), 30)),
    'phPad': lambda: crunch(grainy(ref((742, 572, 803, 633), (128, 128), 14), 14, 4)),
    'phTurf': lambda: crunch(ImageEnhance.Color(ref((770, 672, 1000, 740), (256, 256), 20)).enhance(0.95)),
    'phWall': lambda: crunch(grainy(ref((1262, 322, 1438, 418), (128, 128), 25), 9, 5)),
    'phConcrete': lambda: crunch(ref((560, 2, 790, 80), (256, 128), 30, True, 1.2)),
    'phRed': lambda: crunch(ref((1285, 158, 1495, 184), (128, 64), 15)),
    'phCurtain': lambda: crunch(ref((520, 420, 800, 500), (256, 128), 0, False, 1.2)),
    'phDoors': lambda: crunch(ref((1098, 338, 1245, 515), (128, 160), 0, False, 1.2)),
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
