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

def wallpaper():
    base = tint(square(load('Wallpaper001A_1K-JPG_Color.jpg'), 256), (236, 222, 186), 0.85)
    d = ImageDraw.Draw(base, 'RGBA')
    for x in range(0, 256, 64):  # regency stripes: wide cream band, thin sage pinstripes
        d.rectangle([x + 4, 0, x + 27, 255], fill=(150, 160, 112, 70))
        d.rectangle([x + 31, 0, x + 32, 255], fill=(120, 110, 70, 110)); d.rectangle([x + 36, 0, x + 37, 255], fill=(120, 110, 70, 110))
        for y in range(16, 256, 64):  # small fleur dots in the cream band
            cx = x + 48; d.ellipse([cx - 3, y - 3, cx + 3, y + 3], fill=(170, 140, 80, 120)); d.ellipse([cx - 1, y + 32 - 1, cx + 1, y + 32 + 1], fill=(170, 140, 80, 120))
    return base

JOBS = {
    'whMetal': lambda: crunch(ImageEnhance.Brightness(square(load('corrugated_iron_02.jpg'), 256)).enhance(1.35)),
    'whBlock': lambda: crunch(ImageEnhance.Brightness(tint(square(load('concrete_block_wall.jpg'), 256), (205, 208, 212), 0.75)).enhance(2.6)),
    'whRoof': lambda: crunch(ImageEnhance.Brightness(tint(square(load('box_profile_metal_sheet.jpg'), 256), (190, 194, 200), 1.0)).enhance(2.4)),
    'sofa': lambda: crunch(ImageEnhance.Brightness(tint(square(load('floral_jacquard.jpg'), 256), (176, 132, 84), 0.9)).enhance(1.6)),
    'chair': lambda: crunch(square(load('wool_boucle.jpg'), 128)),
    'curtain': lambda: crunch(ImageEnhance.Brightness(tint(square(load('velour_velvet.jpg'), 128), (140, 26, 34), 0.85)).enhance(0.9)),
    'darkwood': lambda: crunch(square(load('dark_wood.jpg'), 256)),
    'floorwood': lambda: crunch(square(load('laminate_floor_02.jpg'), 256)),
    'rug': lambda: crunch((lambda im: im.crop((int(im.width * 0.045), int(im.height * 0.03), int(im.width * 0.955), int(im.height * 0.97))))(load('Ardabil_Carpet.jpg')).resize((192, 368), Image.LANCZOS)),
    'wallpaper': lambda: crunch(wallpaper()),
    'paint1': lambda: crunch(load('Tsunami_by_hokusai_19th_century.jpg').resize((256, 172), Image.LANCZOS)),
    'paint2': lambda: crunch(load('Claude_Monet_-_Water_Lilies_-_1906,_Ryerson.jpg').resize((200, 192), Image.LANCZOS)),
    'tvscreen': lambda: crunch(Image.open(ROOT / 'media' / 'key-art-drift-master.png').convert('RGB').resize((192, 108), Image.LANCZOS)),
}

if src:
    OUT.mkdir(exist_ok=True)
    for k, f in JOBS.items(): f().save(OUT / f'{k}.jpg', quality=78, optimize=True)
img = {p.stem: base64.b64encode(p.read_bytes()).decode() for p in sorted(OUT.glob('*.jpg'))}
line = 'const IMG = ' + json.dumps(img, separators=(',', ':')) + ';'
html = (ROOT / 'index.html').read_text()
if re.search(r'^const IMG = .*;$', html, flags=re.M): html = re.sub(r'^const IMG = .*;$', lambda m: line, html, count=1, flags=re.M)
else: html = html.replace("const T = {};", line + "\nconst T = {};", 1)
(ROOT / 'index.html').write_text(html)
print('embedded', len(img), 'textures,', sum(len(v) for v in img.values()) // 1024, 'KB base64')
