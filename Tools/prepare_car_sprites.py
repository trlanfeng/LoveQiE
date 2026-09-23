"""Extract the AI car artwork, then make registered four-frame driving loops.

Run with Pillow: python Tools/prepare_car_sprites.py
The generated source and exact generation prompt live in output/imagegen.
No API credentials are used or stored by this post-processing script.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output/imagegen"
ASSETS = ROOT / "Assets/Images/Cars"


def extract(image):
    image = image.convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            # Magenta screen, including antialiased edge pixels. Despill the rim.
            spill = min(r, b) - g
            if spill > 90 and r > 150 and b > 150:
                pixels[x, y] = (0, 0, 0, 0)
            elif spill > 45:
                pixels[x, y] = (min(r, g + 35), g, min(b, g + 35), a)
    bbox = image.getbbox()
    if bbox is None:
        raise ValueError("No car found in source image")
    return image.crop(bbox)


def make_frames(car):
    car.thumbnail((82, 108), Image.Resampling.LANCZOS)
    frames = []
    for phase in range(4):
        # Subtle suspension compression, using the same artwork to prevent AI frame drift.
        compression = (0, 1, 0, -1)[phase]
        body = car.resize((car.width, car.height + compression), Image.Resampling.LANCZOS)
        px = body.load()
        for y in range(body.height):
            for x in range(body.width):
                r, g, b, a = px[x, y]
                # Only dark outer tire rubber receives the scrolling tread highlight.
                if a > 220 and (x < body.width * 0.19 or x > body.width * 0.81) and max(r, g, b) < 85:
                    if (y + phase * 2) % 8 < 2:
                        px[x, y] = (min(110, r + 23), min(110, g + 25), min(110, b + 27), a)
        frame = Image.new("RGBA", (128, 128))
        frame.alpha_composite(body, ((128 - body.width) // 2, (128 - body.height) // 2))
        frames.append(frame)
    return frames


def main():
    ASSETS.mkdir(parents=True, exist_ok=True)
    source = Image.open(OUTPUT / "cars-source.png")
    sheet = Image.new("RGBA", (512, 256))
    sequences = []
    for row, color in enumerate(("red", "green")):
        half = source.crop((row * source.width // 2, 0, (row + 1) * source.width // 2, source.height))
        frames = make_frames(extract(half))
        sequences.append(frames)
        for i, frame in enumerate(frames):
            assert frame.getbbox() and frame.getpixel((0, 0))[3] == 0
            frame.save(ASSETS / f"car_{color}_{i:02}.png")
            sheet.alpha_composite(frame, (128 * i, 128 * row))
        assert len({f.tobytes() for f in frames}) == 4
    sheet.save(OUTPUT / "cars-spritesheet.png")
    previews = []
    for i in range(4):
        preview = Image.new("RGB", (512, 320), "#dce6ca")
        draw = ImageDraw.Draw(preview)
        for col, (label, frames) in enumerate(zip(("RED", "GREEN"), sequences)):
            frame = frames[i].resize((256, 256), Image.Resampling.NEAREST)
            preview.paste(frame, (col * 256, 18), frame)
            draw.text((col * 256 + 108, 284), label, fill="#314934")
        previews.append(preview)
    previews[0].save(OUTPUT / "cars-preview.png")
    previews[0].save(OUTPUT / "cars-driving.gif", save_all=True, append_images=previews[1:], duration=50, loop=0)
    print("PASS: 8 distinct 128x128 RGBA frames with transparent margins; sprite sheet and animated preview saved.")


if __name__ == "__main__":
    main()
