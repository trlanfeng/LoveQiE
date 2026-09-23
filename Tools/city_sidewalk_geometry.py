"""Render a shared world-space curb contour before cutting individual tiles.

Full 8-neighbor road mask: N1 E2 S4 W8 NE16 SE32 SW64 NW128.
All 256 states are retained, including diagonal contacts. Requires numpy/scipy.
"""
import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt, gaussian_filter

NEIGHBORS = [(0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8),
             (1, -1, 16), (1, 1, 32), (-1, 1, 64), (-1, -1, 128)]
SCALE = 2


def mask_at(occupied, x, y):
    return sum(bit for dx, dy, bit in NEIGHBORS if (x + dx, y + dy) not in occupied)


def paint_region(occupied, bounds, asphalt):
    """Bounds are pixel coordinates in a continuous grid (y down).

    Round the union, not independent tile rectangles. Distance transforms extend
    over a padded neighborhood, so shading/curves never stop at a tile seam.
    """
    x0, y0, x1, y1 = bounds
    pad = 48
    xs = np.arange((x0 - pad) * SCALE, (x1 + pad) * SCALE) / SCALE + 0.25
    ys = np.arange((y0 - pad) * SCALE, (y1 + pad) * SCALE) / SCALE + 0.25
    gx, gy = np.meshgrid(xs, ys)
    land = np.zeros(gx.shape, dtype=bool)
    for x, y in occupied:
        land |= (gx >= x * 128) & (gx < (x + 1) * 128) & (gy >= y * 128) & (gy < (y + 1) * 128)
    # Euclidean opening rounds convex corners without breaking one-cell bridges.
    eroded = distance_transform_edt(land) > 18 * SCALE
    rounded = distance_transform_edt(~eroded) <= 18 * SCALE
    distance = distance_transform_edt(rounded) / SCALE
    top = np.clip(distance - 4.5, 0, 1)
    inner = np.clip(distance - 12.5, 0, 1)
    background = np.asarray(asphalt.convert("RGB"))[np.floor(gy).astype(int) % 128, np.floor(gx).astype(int) % 128].astype(float)

    def shift(a, dx, dy):
        result = np.roll(a, (round(dy * SCALE), round(dx * SCALE)), axis=(0, 1))
        return result

    def over(color, alpha):
        nonlocal background
        a = alpha[..., None]
        background = background * (1 - a) + np.asarray(color) * a

    over((22, 30, 34), gaussian_filter(shift(top, 1, 4), 2 * SCALE) * 0.48)
    over((108, 103, 94), shift(top, 0, 5))
    over((155, 148, 133), shift(top, 0, 3))
    over((202, 196, 177), top)
    over((247, 237, 215), np.clip(top - shift(top, 1, 1.5), 0, 1))
    over((159, 153, 138), np.clip(top - shift(top, -1, -1), 0, 1))
    over((158, 150, 132), np.clip(distance - 11.5, 0, 1))
    # Continuous 32px stone pattern, identical on either side of every tile seam.
    py = np.floor(gy).astype(int) % 32
    px = (np.floor(gx).astype(int) + (np.floor(gy / 32).astype(int) % 2) * 16) % 32
    stone = np.empty_like(background)
    stone[:] = (193, 185, 167)
    stone[(px == 0) | (py == 0)] = (170, 161, 141)
    stone[(py == 1) & (px > 0)] = (208, 200, 181)
    over(stone, inner)
    # Stone joint marks on the rim, shared across both straight and curved edges.
    rim = np.clip(top - np.clip(distance - 11.5, 0, 1), 0, 1)
    joints = (((np.floor(gx).astype(int) % 32 == 0) | (np.floor(gy).astype(int) % 32 == 0))).astype(float)
    over((121, 116, 104), rim * joints * 0.28)
    rgba = np.concatenate((np.uint8(np.clip(background, 0, 255)), np.full((*land.shape, 1), 255, dtype=np.uint8)), axis=2)
    crop = rgba[pad * SCALE:-pad * SCALE, pad * SCALE:-pad * SCALE]
    return Image.fromarray(crop).resize((x1 - x0, y1 - y0), Image.Resampling.LANCZOS)


def sidewalk(mask, asphalt):
    occupied = {(0, 0)}
    occupied.update((dx, dy) for dx, dy, bit in NEIGHBORS if not mask & bit)
    return paint_region(occupied, (0, 0, 128, 128), asphalt)
