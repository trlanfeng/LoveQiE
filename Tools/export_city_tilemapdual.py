"""Export rounded city curbs in the installed TileMapDual Standard 4x4 order."""
import json
import re
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from city_sidewalk_geometry import paint_region

ROOT = Path(__file__).resolve().parents[1]
TARGET = Path('D:/game/knowledge-train')
OUT = ROOT / 'output/tilemapdual-city'
CORNERS = [(0, 0, 1), (1, 0, 2), (0, 1, 4), (1, 1, 8)]


def standard_order():
    # Read the plugin's mask -> coordinate mapping; do not manually translate
    # between clockwise bits (NW NE SE SW) and row-major bits (NW NE SW SE).
    source = (TARGET / 'addons/TileMapDual/terrain_preset.gd').read_text(encoding='utf-8')
    block = source.split("'layers': [", 1)[1].split('],', 1)[0]
    coordinates = [tuple(map(int, pair)) for pair in re.findall(r'Vector2i\((\d+),\s*(\d+)\)', block)]
    assert len(coordinates) == len(set(coordinates)) == 16
    result = [None] * 16
    for mask, (x, y) in enumerate(coordinates):
        result[y * 4 + x] = mask
    return result


def pixel_corner_mask(tile):
    alpha = np.asarray(tile.convert('RGBA'))[:, :, 3]
    n = tile.width
    pad = max(1, n // 16)
    corners = [alpha[pad:3*pad, pad:3*pad], alpha[pad:3*pad, -3*pad:-pad],
               alpha[-3*pad:-pad, pad:3*pad], alpha[-3*pad:-pad, -3*pad:-pad]]
    return sum(1 << i for i, pixels in enumerate(corners) if pixels.mean() > 128)


def verify_packed_atlas(image, order):
    n = image.width // 4
    assert image.size == (n * 4, n * 4)
    for i, expected in enumerate(order):
        tile = image.crop((i % 4 * n, i // 4 * n, (i % 4 + 1) * n, (i // 4 + 1) * n))
        actual = pixel_corner_mask(tile)
        assert actual == expected, ('Wrong packed corner artwork', (i % 4, i // 4), expected, actual)
    assert image.crop((0, n * 3, n, n * 4)).getbbox() is None
    assert image.crop((n * 2, n, n * 3, n * 2)).getextrema()[3] == (255, 255)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    reference = Image.open(TARGET / 'assets/tileset_grass.png')
    assert reference.size == (128, 128)
    order = standard_order()
    verify_packed_atlas(reference, order)
    manifest = json.loads((ROOT / 'Assets/CityTiles/city-atlas.json').read_text())
    atlas = Image.open(ROOT / 'Assets/CityTiles/Textures/city_terrain.png').convert('RGBA')
    rect = manifest['atlases'][0]['sprites'][0]
    x, y = rect['x'], atlas.height - rect['y'] - 128
    asphalt = atlas.crop((x, y, x + 128, y + 128))
    # The display grid is offset half a world cell. Each tile sees FOUR world
    # cells, unlike the centered eight-neighbor Blob atlas used by Unity.
    tiles = {}
    for mask in range(16):
        occupied = {(cx, cy) for cx, cy, bit in CORNERS if mask & bit}
        tiles[mask] = paint_region(occupied, (64, 64, 192, 192), asphalt, transparent=True)
    for size, suffix in ((32, ''), (128, '_hd')):
        result = Image.new('RGBA', (size * 4, size * 4))
        for i, mask in enumerate(order):
            tile = tiles[mask].resize((size, size), Image.Resampling.LANCZOS)
            result.paste(tile, (i % 4 * size, i // 4 * size))
        filename = f'tileset_city_sidewalk{suffix}.png'
        result.save(OUT / filename)
        result.save(TARGET / 'assets' / filename)
        # Reopen the delivered PNG, so a packing error cannot pass a test that
        # checks only the unarranged mask dictionary or generated rule metadata.
        verify_packed_atlas(Image.open(TARGET / 'assets' / filename), order)
    # Labeled sheet: compare the reference image and actual packed pixels.
    comparison = Image.new('RGB', (960, 570), '#e5e0d2')
    pen = ImageDraw.Draw(comparison)
    packed = Image.open(TARGET / 'assets/tileset_city_sidewalk_hd.png').convert('RGBA')
    for side, (label, source) in enumerate((('REFERENCE GRASS', reference), ('CORRECTED CITY / Standard', packed))):
        pen.text((side * 480 + 16, 12), label, fill='#35404a')
        size = source.width // 4
        for i, mask in enumerate(order):
            x, y = side * 480 + (i % 4) * 116 + 8, 42 + (i // 4) * 130
            tile = source.crop((i % 4 * size, i // 4 * size, (i % 4 + 1) * size, (i // 4 + 1) * size))
            pen.rectangle((x, y, x + 108, y + 108), fill='#48565f')
            tile = tile.resize((108, 108), Image.Resampling.NEAREST)
            comparison.paste(tile, (x, y), tile)
            pen.text((x, y + 111), f'({i%4},{i//4}) mask {mask:02}', fill='#35404a')
    comparison.save(OUT / 'standard-layout-check.png')
    asphalt.resize((32, 32), Image.Resampling.LANCZOS).save(OUT / 'city_asphalt_base.png')
    asphalt.resize((32, 32), Image.Resampling.LANCZOS).save(TARGET / 'assets/city_asphalt_base.png')
    assert tiles[0].getbbox() is None
    assert tiles[15].getextrema()[3] == (255, 255)
    # Every legal pair has identical boundary topology and color/alpha continuity.
    # Test the continuous strip against separately sampled atlas tiles.
    count, maximum = 0, 0
    for vertical in (False, True):
        w, h = (2, 3) if vertical else (3, 2)
        for flags in range(64):
            occupied = {(xx, yy) for yy in range(h) for xx in range(w) if flags & (1 << (yy*w+xx))}
            def mask_at(xx, yy):
                return sum(bit for dx, dy, bit in CORNERS if (xx+dx, yy+dy) in occupied)
            a, b = tiles[mask_at(0, 0)], tiles[mask_at(0 if vertical else 1, 1 if vertical else 0)]
            bounds = (64, 180, 192, 204) if vertical else (180, 64, 204, 192)
            whole = paint_region(occupied, bounds, asphalt, transparent=True)
            joined = np.concatenate((np.asarray(a)[116:], np.asarray(b)[:12]), 0) if vertical else np.concatenate((np.asarray(a)[:,116:], np.asarray(b)[:,:12]), 1)
            def premult(p):
                p = p.astype(float)
                return np.concatenate((p[:,:,:3]*p[:,:,3:]/255,p[:,:,3:]),2)
            delta = float(np.max(abs(premult(joined)-premult(np.asarray(whole)))))
            maximum=max(maximum,delta)
            assert delta <= 5, (vertical, flags, delta)
            count += 1
    shapes = [
        ('L', {(1,1),(1,2),(1,3),(2,3),(3,3)}),
        ('T', {(1,1),(2,1),(3,1),(2,2),(2,3)}),
        ('Cross', {(2,1),(1,2),(2,2),(3,2),(2,3)}),
        ('Ring', {(x,y) for x in range(1,4) for y in range(1,4)}-{(2,2)}),
        ('Diagonal', {(1,1),(2,2),(3,3)}),
        ('Narrow bridge', {(1,1),(2,1),(2,2),(3,2),(3,3)})]
    board=Image.new('RGB',(960,688),'#ded8c7'); draw=ImageDraw.Draw(board)
    for i,(label,occupied) in enumerate(shapes):
        canvas=Image.new('RGBA',(640,640))
        for yy in range(5):
            for xx in range(5): canvas.paste(asphalt,(xx*128,yy*128))
        for yy in range(6):
            for xx in range(6):
                mask=sum(bit for dx,dy,bit in CORNERS if (xx-1+dx,yy-1+dy) in occupied)
                canvas.alpha_composite(tiles[mask],(xx*128-64,yy*128-64))
        x,y=i%3*320,i//3*344
        draw.text((x+10,y+7),label,fill='#35404a')
        board.paste(canvas.resize((320,320),Image.Resampling.LANCZOS),(x,y+24))
    board.save(OUT/'dual-preview.png')
    spec=dict(atlas_size=[128,128],tile_size=[32,32],columns=4,rows=4,margin=0,spacing=0,
              preset='TileMapDual / Standard',corner_bits=dict(NW=1,NE=2,SW=4,SE=8),
              row_major_masks=order,foreground=[2,1],background=[0,3],hd_tile_size=[128,128])
    (OUT/'layout.json').write_text(json.dumps(spec,indent=2),encoding='utf-8')
    report=f'PASS: plugin Standard coordinates AND actual PNG corner pixels match the reference for all 16 cells in both 32px and 128px versions; transparent background (0,3); full foreground (2,1); {count} legal neighboring pairs; max premultiplied RGBA error {maximum:.2f}/255.\n'
    (OUT/'validation.txt').write_text(report,encoding='utf-8')
    print(report)


if __name__ == '__main__': main()
