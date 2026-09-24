"""Prepare a clean star and a redistributable Chinese font for the supplied UI."""
from pathlib import Path
import urllib.request
from PIL import Image
from fontTools import subset

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/UI/Prepared'
OUT.mkdir(parents=True, exist_ok=True)

# The center-star image includes slivers of its neighbours. Retain only the
# largest alpha-connected component; do not redraw the user's artwork.
image = Image.open(ROOT / 'Assets/UI/star_center.png').convert('RGBA')
alpha = image.getchannel('A')
pixels = alpha.load()
seen = set()
largest = []
for y in range(image.height):
    for x in range(image.width):
        if (x, y) in seen or pixels[x, y] < 16:
            continue
        component, stack = [], [(x, y)]
        seen.add((x, y))
        while stack:
            point = stack.pop()
            component.append(point)
            px, py = point
            for nx, ny in ((px-1, py), (px+1, py), (px, py-1), (px, py+1)):
                if 0 <= nx < image.width and 0 <= ny < image.height and (nx, ny) not in seen and pixels[nx, ny] >= 16:
                    seen.add((nx, ny))
                    stack.append((nx, ny))
        if len(component) > len(largest):
            largest = component
mask = Image.new('L', image.size)
for point in largest:
    mask.putpixel(point, pixels[point])
image.putalpha(mask)
image.crop(mask.getbbox()).save(OUT / 'star_clean.png')

cache = ROOT / 'UIReports/font-source'
cache.mkdir(parents=True, exist_ok=True)
font_path = cache / 'NotoSansSC-Regular.otf'
if not font_path.exists():
    urllib.request.urlretrieve('https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/SubsetOTF/SC/NotoSansSC-Regular.otf', font_path)
license_path = OUT / 'OFL.txt'
if not license_path.exists():
    urllib.request.urlretrieve('https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/LICENSE', license_path)
text = ''.join(path.read_text(encoding='utf-8-sig') for path in (ROOT / 'Assets/_Scripts/UI').glob('*.cs'))
text += ''.join(chr(i) for i in range(32, 127)) + '←↑↓→★≤'
options = subset.Options()
options.name_IDs = ['*']
font = subset.load_font(str(font_path), options)
subsetter = subset.Subsetter(options=options)
subsetter.populate(text=text)
subsetter.subset(font)
subset.save_font(font, str(OUT / 'JourneyUI.otf'), options)
(OUT / 'README.txt').write_text('JourneyUI.otf: UI character subset of Noto Sans SC Regular (SIL OFL 1.1).\nSource: https://github.com/notofonts/noto-cjk\nRegenerate with python Tools/prepare_game_ui.py after adding UI text.\nstar_clean.png: largest connected star from ../star_center.png.\n', encoding='utf-8')
print('Prepared clean star and bundled Chinese font.')
