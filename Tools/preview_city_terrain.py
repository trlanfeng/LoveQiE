"""Check eight-neighbor tiles against independently rendered whole contours."""
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from city_sidewalk_geometry import NEIGHBORS, BLOB_MASKS, normalize_mask, mask_at, paint_region, sidewalk

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "CityReports/blob47"


def load_tiles():
    image = Image.open(ROOT / "Assets/CityTiles/Textures/city_terrain.png").convert("RGBA")
    manifest = json.loads((ROOT / "Assets/CityTiles/city-atlas.json").read_text())
    result = {}
    for rect in manifest["atlases"][0]["sprites"]:
        x, y = rect["x"], image.height - rect["y"] - 128
        result[rect["name"]] = image.crop((x, y, x + 128, y + 128))
    return result


def assemble(tiles, occupied, w, h):
    image = Image.new("RGBA", (w * 128, h * 128))
    for y in range(h):
        for x in range(w):
            name = f"sidewalk_{normalize_mask(mask_at(occupied, x, y)):02}" if (x, y) in occupied else "asphalt_00"
            image.paste(tiles[name], (x * 128, y * 128))
    return image


def main():
    REPORT.mkdir(parents=True, exist_ok=True)
    tiles = load_tiles()
    assert len(tiles) == 71
    for mask in range(256):
        tile = tiles[f"sidewalk_{normalize_mask(mask):02}"]
        assert tile.size == (128, 128) and tile.getextrema()[3] == (255, 255)
        reference = sidewalk(mask, tiles["asphalt_00"])
        diff = np.abs(np.asarray(tile).astype(int) - np.asarray(reference).astype(int))
        assert diff.max() <= 5, ("Normalized state changes visible contour", mask, diff.max())
    assert tiles["sidewalk_00"].tobytes() != tiles["sidewalk_16"].tobytes()
    # Enumerate every 4x3/3x4 neighborhood around two connected occupied cells.
    pairs = set()
    for vertical in (False, True):
        a, b = (1, 1), ((1, 2) if vertical else (2, 1))
        w, h = (3, 4) if vertical else (4, 3)
        free = [(x, y) for y in range(h) for x in range(w) if (x, y) not in (a, b)]
        for flags in range(1 << len(free)):
            occupied = {a, b} | {p for i, p in enumerate(free) if flags & (1 << i)}
            pairs.add((vertical, mask_at(occupied, *a), mask_at(occupied, *b)))
    max_delta = 0
    for index, (vertical, ma, mb) in enumerate(sorted(pairs)):
        a, b = (1, 1), ((1, 2) if vertical else (2, 1))
        occupied = {a, b}
        for (x, y), mask in ((a, ma), (b, mb)):
            occupied.update((x + dx, y + dy) for dx, dy, bit in NEIGHBORS if not mask & bit)
        # Render a continuous strip across the seam, not two separate tiles.
        bounds = (128, 244, 256, 268) if vertical else (244, 128, 268, 256)
        reference = np.asarray(paint_region(occupied, bounds, tiles["asphalt_00"])).astype(int)
        ta, tb = tiles[f"sidewalk_{normalize_mask(ma):02}"], tiles[f"sidewalk_{normalize_mask(mb):02}"]
        joined = np.concatenate((np.asarray(ta)[116:128], np.asarray(tb)[:12]), axis=0) if vertical else np.concatenate((np.asarray(ta)[:, 116:128], np.asarray(tb)[:, :12]), axis=1)
        delta = int(np.max(np.abs(joined.astype(int) - reference)))
        max_delta = max(max_delta, delta)
        assert delta <= 5, ("Seam differs from global contour", vertical, ma, mb, delta)
        if index % 256 == 0: print(f"Checked {index}/{len(pairs)} joining patterns", flush=True)
    shapes = [
        ("L / concave corner", {(1, 1), (1, 2), (1, 3), (2, 3), (3, 3)}),
        ("T / two concave corners", {(1, 1), (2, 1), (3, 1), (2, 2), (2, 3)}),
        ("Cross / four corners", {(2, 1), (1, 2), (2, 2), (3, 2), (2, 3)}),
        ("Ring / enclosed road", {(x, y) for x in range(1, 4) for y in range(1, 4)} - {(2, 2)}),
        ("Diagonal contacts", {(1, 1), (2, 2), (3, 3)}),
        ("Step / one-cell neck", {(1, 1), (2, 1), (2, 2), (3, 2), (3, 3)})]
    board = Image.new("RGB", (1440, 1020), "#e5e0d2")
    draw = ImageDraw.Draw(board)
    for i, (label, occupied) in enumerate(shapes):
        tiled = assemble(tiles, occupied, 5, 5)
        whole = paint_region(occupied, (0, 0, 640, 640), tiles["asphalt_00"])
        for x, y in occupied:
            rect = (x * 128, y * 128, (x + 1) * 128, (y + 1) * 128)
            diff = np.abs(np.asarray(tiled.crop(rect)).astype(int) - np.asarray(whole.crop(rect)).astype(int))
            assert diff.max() <= 5, (label, x, y, diff.max())
        board.paste(tiled.resize((480, 480), Image.Resampling.LANCZOS), (i % 3 * 480, i // 3 * 510 + 30))
        draw.text((i % 3 * 480 + 12, i // 3 * 510 + 10), label, fill="#35404a")
    board.save(REPORT / "complex-corners.png")
    text = f"PASS: all 256 neighborhoods map to 47 Blob variants without changing their contour; {len(pairs)} compatible tile pairs compared against a continuous global contour; maximum channel difference={max_delta}/255; L/T/cross/ring/diagonal/one-cell-neck assemblies checked; 71 terrain slices.\n"
    (REPORT / "pixel-validation.txt").write_text(text)
    print(text)


if __name__ == "__main__":
    main()
