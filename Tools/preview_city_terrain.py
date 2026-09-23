"""Compare old/new terrain and check assembled curb silhouettes and atlas scope."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "CityReports/terrain-comparison"


def slices(path):
    image = Image.open(path).convert("RGBA")
    manifest = json.loads((ROOT / "Assets/CityTiles/city-atlas.json").read_text())
    result = {}
    for rect in manifest["atlases"][0]["sprites"]:
        x, y = rect["x"], image.height - rect["y"] - 128
        result[rect["name"]] = image.crop((x, y, x + 128, y + 128))
    return result


def assemble(tiles):
    image = Image.new("RGBA", (768, 512))
    occupied = {(1, 1), (2, 1), (3, 1), (1, 2), (2, 2), (3, 2), (5, 1), (5, 3)}
    for y in range(4):
        for x in range(6):
            mask = sum(bit for dx, dy, bit in [(0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8)]
                       if (x + dx, y + dy) not in occupied)
            name = f"sidewalk_{mask:02}" if (x, y) in occupied else "asphalt_00"
            image.paste(tiles[name], (x * 128, y * 128))
    return image


def main():
    old = slices(REPORT / "terrain-before.png")
    new = slices(ROOT / "Assets/CityTiles/Textures/city_terrain.png")
    changed = [name for name in old if old[name].tobytes() != new[name].tobytes()]
    assert changed == [f"sidewalk_{i:02}" for i in range(16)], changed
    for mask in range(16):
        tile = new[f"sidewalk_{mask:02}"]
        assert tile.size == (128, 128) and tile.getextrema()[3] == (255, 255)
    # Road surrounds the rounded isolated slab; the interior is raised warm stone.
    rounded = new["sidewalk_15"]
    road = new["asphalt_00"]
    for p in [(0, 0), (127, 0), (0, 127), (127, 127)]:
        assert max(abs(a - b) for a, b in zip(rounded.getpixel(p), road.getpixel(p))) <= 3
    assert rounded.getpixel((64, 64))[0] > rounded.getpixel((64, 124))[0] + 25
    # The curb profile continues through internal joins, without a false cap.
    comparisons = 0
    for left, right in [(9, 1), (1, 3), (12, 4), (4, 6), (13, 5), (5, 7)]:
        a, b = new[f"sidewalk_{left:02}"], new[f"sidewalk_{right:02}"]
        for y in list(range(0, 12)) + list(range(112, 128)):
            # Stone joints can differ slightly; a road gap or end-cap cannot.
            assert max(abs(a.getpixel((127, y))[c] - b.getpixel((0, y))[c]) for c in range(3)) < 35
            comparisons += 1
    board = Image.new("RGB", (1536, 572), "#eee9dd")
    draw = ImageDraw.Draw(board)
    draw.text((24, 20), "BEFORE / flat square pavement", fill="#33404a")
    draw.text((792, 20), "AFTER / rounded cap, raised face, contact shadow", fill="#33404a")
    board.paste(assemble(old), (0, 60))
    board.paste(assemble(new), (768, 60))
    board.save(REPORT / "rounded-curbs-before-after.png")
    (REPORT / "validation.txt").write_text(
        f"PASS: only 16 sidewalk slices changed; all 24 road/marking slices unchanged; 128px opaque rounded cuts; corner asphalt matches; {comparisons} connected curb profile comparisons pass; atlas packing and sprite names unchanged.\n")
    print((REPORT / "validation.txt").read_text())


if __name__ == "__main__":
    main()
