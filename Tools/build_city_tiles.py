"""Pack AI-painted city sprites and assemble edge-matched terrain for Unity.

python Tools/build_city_tiles.py
Requires Pillow. AI sources/prompts are retained in output/imagegen; no credentials.
128 px = one world cell, 2 px extruded padding, 132 px atlas pitch.
"""
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps, ImageFilter, ImageChops
from prepare_car_sprites import extract as chroma_extract, make_frames

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "output/imagegen"
OUT = ROOT / "Assets/CityTiles/Textures"
REPORT = ROOT / "CityReports"
SIZE, PAD, PITCH = 128, 2, 132


def extract(image):
    # This provider returned alpha already. Remove near-invisible alpha specks
    # before measuring bounds, or the visible car is scaled much too small.
    if image.mode == "RGBA" and image.getextrema()[3][0] == 0:
        image = image.copy()
        image.putalpha(image.getchannel("A").point(lambda a: 0 if a < 32 else a))
        return image.crop(image.getbbox())
    return chroma_extract(image)


def pack(name, entries, columns):
    rows = math.ceil(len(entries) / columns)
    sheet = Image.new("RGBA", (columns * PITCH, rows * PITCH))
    records = []
    for index, (label, tile) in enumerate(entries):
        x, y = index % columns * PITCH + PAD, index // columns * PITCH + PAD
        sheet.paste(tile, (x, y))
        # Extrude the outer texels into the gutter to prevent bilinear atlas bleeding.
        sheet.paste(tile.crop((0, 0, SIZE, 1)).resize((SIZE, PAD)), (x, y - PAD))
        sheet.paste(tile.crop((0, SIZE - 1, SIZE, SIZE)).resize((SIZE, PAD)), (x, y + SIZE))
        sheet.paste(tile.crop((0, 0, 1, SIZE)).resize((PAD, SIZE)), (x - PAD, y))
        sheet.paste(tile.crop((SIZE - 1, 0, SIZE, SIZE)).resize((PAD, SIZE)), (x + SIZE, y))
        for dx, dy, tx, ty in [(-PAD, -PAD, 0, 0), (SIZE, -PAD, 127, 0), (-PAD, SIZE, 0, 127), (SIZE, SIZE, 127, 127)]:
            sheet.paste(tile.getpixel((tx, ty)), (x + dx, y + dy, x + dx + PAD, y + dy + PAD))
        records.append(dict(name=label, x=x, y=sheet.height - y - SIZE, width=SIZE, height=SIZE))
    sheet.save(OUT / f"{name}.png")
    return dict(file=f"{name}.png", sprites=records)


def raised_sidewalk(mask, asphalt):
    """A continuous raised slab. Only road-facing edges are inset/rounded.

    Unexposed sides extend past the tile, so long blocks have no false end caps.
    Render at 4x for smooth curves; keep road-colored pixels under the round cuts.
    """
    scale = 4
    size = SIZE * scale

    def silhouette(inset=0, dx=0, dy=0):
        bounds = [5 + inset if mask & 8 else -64,
                  4 + inset if mask & 1 else -64,
                  123 - inset if mask & 2 else 192,
                  120 - inset if mask & 4 else 192]
        bounds = [(v + (dx if i % 2 == 0 else dy)) * scale for i, v in enumerate(bounds)]
        shape = Image.new("L", (size, size))
        ImageDraw.Draw(shape).rounded_rectangle(bounds, radius=max(1, 22 - inset) * scale, fill=255)
        return shape

    tile = asphalt.resize((size, size), Image.Resampling.BICUBIC)
    top = silhouette()
    shadow = silhouette(dx=1, dy=4).filter(ImageFilter.GaussianBlur(2 * scale))
    tile.paste((22, 30, 34, 255), (0, 0, size, size), shadow.point(lambda a: int(a * 0.48)))
    # Five-pixel south-facing fascia under the cap gives the sidewalk its height.
    tile.paste((108, 103, 94, 255), (0, 0, size, size), silhouette(dy=5))
    tile.paste((155, 148, 133, 255), (0, 0, size, size), silhouette(dy=3))
    tile.paste((202, 196, 177, 255), (0, 0, size, size), top)
    light = ImageChops.subtract(top, silhouette(dx=1.2, dy=1.5))
    tile.paste((247, 237, 215, 255), (0, 0, size, size), light)
    shade = ImageChops.subtract(top, silhouette(dx=-1, dy=-1))
    tile.paste((159, 153, 138, 255), (0, 0, size, size), shade)

    # Large warm paving slabs with restrained seams and subtle stone variation.
    paving = Image.new("RGBA", (size, size), "#c1b9a6")
    draw = ImageDraw.Draw(paving)
    for y in range(0, 128, 32):
        for x in range(-32 if y % 64 else 0, 128, 32):
            tone = ((x // 32 * 3 + y // 32 * 5) % 5) - 2
            draw.rectangle((x * scale, y * scale, (x + 32) * scale, (y + 32) * scale),
                           fill=(193 + tone, 185 + tone, 167 + tone, 255))
            draw.line((x * scale, y * scale, (x + 32) * scale, y * scale), fill="#a89f8d", width=scale)
            draw.line((x * scale, y * scale, x * scale, (y + 32) * scale), fill="#ada490", width=scale)
            draw.line((x * scale + scale, y * scale + scale, (x + 32) * scale - scale, y * scale + scale), fill="#d0c8b5", width=scale)
    # Thin inner groove, then the paving inset. Both follow the same round corner.
    tile.paste((158, 150, 132, 255), (0, 0, size, size), silhouette(inset=7))
    tile.paste(paving, (0, 0), silhouette(inset=8))
    # Joints between curb stones are confined to the cap; no square lines cut corners.
    cap = ImageChops.subtract(top, silhouette(inset=7))
    joints = Image.new("L", (size, size))
    d = ImageDraw.Draw(joints)
    for p in (32, 64, 96):
        if mask & 1: d.line((p * scale, 0, p * scale, 12 * scale), fill=110, width=scale)
        if mask & 4: d.line((p * scale, 112 * scale, p * scale, size), fill=110, width=scale)
        if mask & 8: d.line((0, p * scale, 13 * scale, p * scale), fill=110, width=scale)
        if mask & 2: d.line((115 * scale, p * scale, size, p * scale), fill=110, width=scale)
    tile.paste((121, 116, 104, 255), (0, 0, size, size), ImageChops.multiply(joints, cap))
    result = tile.resize((128, 128), Image.Resampling.LANCZOS)
    assert result.getextrema()[3] == (255, 255)
    return result


def terrain():
    # Sample actual asphalt from the approved AI-painted city, then mirror its
    # boundaries so all four variants share identical edge pixels.
    reference = Image.open(SOURCE / "city-game-preview.png").convert("RGBA")
    patch = reference.crop((450, 565, 482, 597)).resize((64, 64), Image.Resampling.BICUBIC)
    asphalt = Image.new("RGBA", (128, 128))
    asphalt.paste(patch, (0, 0))
    asphalt.paste(ImageOps.mirror(patch), (64, 0))
    asphalt.paste(ImageOps.flip(patch), (0, 64))
    asphalt.paste(ImageOps.flip(ImageOps.mirror(patch)), (64, 64))
    entries = []
    for i in range(4):
        tile = asphalt.copy()
        # Interior-only wear keeps shared seams unchanged.
        wear = Image.new("RGBA", (128, 128))
        draw = ImageDraw.Draw(wear)
        if i:
            draw.line([(30 + i * 11, 40), (42 + i * 9, 58), (38 + i * 8, 66)], fill=(165, 176, 174, 12), width=1)
        tile.alpha_composite(wear)
        assert tile.getextrema()[3] == (255, 255)
        entries.append((f"asphalt_{i:02}", tile))
    for mask in range(16):
        entries.append((f"sidewalk_{mask:02}", raised_sidewalk(mask, asphalt)))
    for mask in range(16):
        tile = Image.new("RGBA", (128, 128))
        draw = ImageDraw.Draw(tile)
        # Dashes have the same phase at every cell boundary. Keep junction center clear.
        for bit, points in [(1, [(63, y, 65, y + 9) for y in (0, 24, 48)]),
                            (4, [(63, y, 65, min(y + 9, 127)) for y in (72, 96, 120)]),
                            (8, [(x, 63, x + 9, 65) for x in (0, 24, 48)]),
                            (2, [(x, 63, min(x + 9, 127), 65) for x in (72, 96, 120)])]:
            if mask & bit:
                for p in points:
                    draw.rectangle(p, fill=(221, 210, 175, 155))
        entries.append((f"lane_{mask:02}", tile))
    crossing = Image.new("RGBA", (128, 128))
    draw = ImageDraw.Draw(crossing)
    for x in range(20, 112, 16):
        draw.rectangle((x, 40, x + 8, 88), fill=(234, 224, 195, 218))
    entries.extend([("crosswalk_ns", crossing), ("crosswalk_ew", crossing.rotate(90))])
    for color, tint in [("red", "#bc7465"), ("green", "#8da276")]:
        tile = Image.new("RGBA", (128, 128))
        draw = ImageDraw.Draw(tile)
        draw.line([(30, 32), (30, 114), (98, 114), (98, 32)], fill=tint, width=3)
        entries.append((f"parking_{color}", tile))
    # All asphalt variants must join each other without a discontinuity.
    for _, a in entries[:4]:
        for _, b in entries[:4]:
            assert a.crop((127, 0, 128, 128)).tobytes() == b.crop((0, 0, 1, 128)).tobytes()
            assert a.crop((0, 127, 128, 128)).tobytes() == b.crop((0, 0, 128, 1)).tobytes()
    return entries


def props_and_buildings():
    source = Image.open(SOURCE / "city-buildings-source.png")
    names = ["house_terracotta", "house_slate", "house_sage", "apartment_cream", "cafe", "bakery", "clinic", "shop", "tree", "streetlamp", "bench", "planter"]
    result = []
    for i, name in enumerate(names):
        w, h = source.width // 4, source.height // 3
        cell = source.crop((i % 4 * w, i // 4 * h, (i % 4 + 1) * w, (i // 4 + 1) * h))
        cutout = extract(cell)
        bound = 110 if i < 8 else (98 if name == "tree" else 90)
        cutout.thumbnail((bound, bound), Image.Resampling.LANCZOS)
        tile = Image.new("RGBA", (128, 128))
        tile.alpha_composite(cutout, ((128 - cutout.width) // 2, (128 - cutout.height) // 2))
        assert tile.getbbox() and tile.getpixel((0, 0))[3] == 0
        result.append((name, tile))
    return result[:8], result[8:]


def cars():
    source = Image.open(SOURCE / "city-cars-source.png")
    entries = []
    for index, color in enumerate(("red", "green")):
        cutout = extract(source.crop((index * source.width // 2, 0, (index + 1) * source.width // 2, source.height)))
        frames = make_frames(cutout)
        assert len({f.tobytes() for f in frames}) == 4
        for n, frame in enumerate(frames):
            entries.append((f"car_{color}_{n:02}", frame))
    return entries


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    REPORT.mkdir(exist_ok=True)
    ground = terrain()
    buildings, props = props_and_buildings()
    vehicles = cars()
    manifest = dict(tileSize=SIZE, padding=PAD, pixelsPerUnit=128, atlases=[
        pack("city_terrain", ground, 8), pack("city_buildings", buildings, 4),
        pack("city_props", props, 4), pack("city_cars", vehicles, 4)])
    (OUT.parent / "city-atlas.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    # A labeled contact sheet for choosing tiles, separate from the runtime atlases.
    all_tiles = ground + buildings + props + vehicles
    board = Image.new("RGB", (8 * 152, math.ceil(len(all_tiles) / 8) * 154 + 60), "#e5e0d2")
    draw = ImageDraw.Draw(board)
    draw.text((20, 20), "CITY TILESET / 128px tiles / 2px padding / N1 E2 S4 W8", fill="#38434a")
    for i, (name, tile) in enumerate(all_tiles):
        x, y = i % 8 * 152 + 12, i // 8 * 154 + 52
        draw.rectangle((x, y, x + 127, y + 127), fill="#626f76")
        board.paste(tile, (x, y), tile)
        draw.text((x, y + 130), name, fill="#38434a")
    board.save(REPORT / "city-atlas-overview.png")
    # Preview the softened car art against the actual terrain and building sprites.
    animated = []
    for phase in range(4):
        frame = Image.new("RGB", (640, 384), "#c8c6b5")
        for row in range(3):
            for col in range(5):
                frame.paste(ground[0][1], (col * 128, row * 128))
        for col, variant in enumerate((0, 1, 2, 4, 6)):
            frame.paste(ground[4 + 4][1], (col * 128, 0))
            tile = buildings[variant][1]
            frame.paste(tile, (col * 128, 0), tile)
        for x, offset in [(160, 0), (352, 4)]:
            tile = vehicles[offset + phase][1]
            frame.paste(tile, (x, 180), tile)
        animated.append(frame)
    animated[0].save(REPORT / "city-style-preview.png")
    animated[0].save(REPORT / "city-cars-driving.gif", save_all=True, append_images=animated[1:], duration=50, loop=0)
    (REPORT / "image-validation.txt").write_text("PASS: 60 sprites (40 terrain, 8 buildings, 4 props, 8 car frames); 128x128; 2px extruded gutters; transparent objects; 16 pairs of asphalt variants share exact edge pixels; four unique frames per car.\n", encoding="utf-8")
    print("PASS: 4 atlases, 60 sprites; exact asphalt seams, transparent cutouts, car loops and manifest.")


if __name__ == "__main__":
    main()
