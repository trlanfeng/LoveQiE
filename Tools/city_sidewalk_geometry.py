"""Render world-space Blob contours using five compatible quarter-cell shapes.

Full 8-neighbor road mask: N1 E2 S4 W8 NE16 SE32 SW64 NW128.
Diagonal road bits are relevant only when both adjacent cardinals are land.
This produces the 47 Blob states. Requires numpy/scipy.
"""
import numpy as np
from PIL import Image

NEIGHBORS = [(0, -1, 1), (1, 0, 2), (0, 1, 4), (-1, 0, 8),
             (1, -1, 16), (1, 1, 32), (-1, 1, 64), (-1, -1, 128)]
SCALE = 2


def normalize_mask(mask):
    for adjacent, diagonal in ((3, 16), (6, 32), (12, 64), (9, 128)):
        if mask & adjacent:
            mask |= diagonal
    return mask


BLOB_MASKS = sorted({normalize_mask(mask) for mask in range(256)})
assert len(BLOB_MASKS) == 47


def mask_at(occupied, x, y):
    return sum(bit for dx, dy, bit in NEIGHBORS if (x + dx, y + dy) not in occupied)


def paint_region(occupied, bounds, asphalt, transparent=False):
    """Bounds are pixel coordinates in a continuous grid (y down).

    Match straight, convex and concave contour tangents at shared cell edges.
    The five quarter-cell shapes produce 47 full-cell combinations. A continuous
    padded field keeps bevel lighting and antialiasing consistent at tile seams.
    """
    x0, y0, x1, y1 = bounds
    pad = 48
    xs = np.arange((x0 - pad) * SCALE, (x1 + pad) * SCALE) / SCALE + 0.25
    ys = np.arange((y0 - pad) * SCALE, (y1 + pad) * SCALE) / SCALE + 0.25
    gx, gy = np.meshgrid(xs, ys)
    distance = np.full(gx.shape, -64., dtype=float)
    for x, y in occupied:
        local_x, local_y = gx - x * 128, gy - y * 128
        inside = (local_x >= 0) & (local_x < 128) & (local_y >= 0) & (local_y < 128)
        mask = mask_at(occupied, x, y)
        # Five quarter-cell shapes: full, straight x/y, convex, concave.
        # Their edge tangents match exactly; irrelevant diagonal islands cannot
        # change this cell. Keep lighting in world orientation (never rotate art).
        for east, south, horizontal, vertical, diagonal in (
            (False, False, 8, 1, 128), (True, False, 2, 1, 16),
            (True, True, 2, 4, 32), (False, True, 8, 4, 64)):
            u = 128 - local_x if east else local_x
            v = 128 - local_y if south else local_y
            quarter = inside & (u <= 64) & (v <= 64)
            a, b = bool(mask & horizontal), bool(mask & vertical)
            if a and b:
                # Rounded box SDF: 9px road gutter, 18px convex radius.
                qx, qy = 27 - u, 27 - v
                d = 18 - np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) - np.minimum(np.maximum(qx, qy), 0)
            elif a:
                d = u - 9
            elif b:
                d = v - 9
            elif mask & diagonal:
                d = np.hypot(u, v) - 9
            else:
                d = np.full(gx.shape, 64.)
            distance[quarter] = d[quarter]
    top = np.clip(distance + 0.5, 0, 1)
    inner = np.clip(distance - 8.5, 0, 1)
    background = np.asarray(asphalt.convert("RGB"))[np.floor(gy).astype(int) % 128, np.floor(gx).astype(int) % 128].astype(float)
    opacity = np.ones(gx.shape, dtype=float)
    if transparent:
        background[:] = 0
        opacity[:] = 0

    def over(color, alpha):
        nonlocal background, opacity
        a = alpha[..., None]
        background = background * (1 - a) + np.asarray(color) * a
        opacity = opacity * (1 - alpha) + alpha

    # Bevel bands provide height without a shifted shadow spilling into another
    # terrain cell. Lighting follows the contour normal, not tile rotations.
    dy, dx = np.gradient(distance, 1 / SCALE)
    light = np.clip((dx + dy) * 0.7, -1, 1)
    outside = np.clip(-distance + 0.5, 0, 1)
    over((22, 30, 34), np.exp(-np.maximum(-distance, 0) ** 2 / 8) * outside * 0.35)
    over((123, 117, 105), np.clip(distance + 3.5, 0, 1))
    over((159, 152, 136), np.clip(distance + 1.5, 0, 1))
    over((202, 196, 177), top)
    bevel = top * np.clip(2.5 - distance, 0, 1)
    over((247, 237, 215), bevel * np.maximum(light, 0))
    over((145, 137, 122), bevel * np.maximum(-light, 0))
    over((158, 150, 132), np.clip(distance - 7.5, 0, 1))
    # Continuous 32px stone pattern, identical on either side of every tile seam.
    py = np.floor(gy).astype(int) % 32
    px = (np.floor(gx).astype(int) + (np.floor(gy / 32).astype(int) % 2) * 16) % 32
    stone = np.empty_like(background)
    stone[:] = (193, 185, 167)
    stone[(px == 0) | (py == 0)] = (170, 161, 141)
    stone[(py == 1) & (px > 0)] = (208, 200, 181)
    over(stone, inner)
    # Stone joint marks on the rim, shared across both straight and curved edges.
    rim = np.clip(top - np.clip(distance - 7.5, 0, 1), 0, 1)
    joints = (((np.floor(gx).astype(int) % 32 == 0) | (np.floor(gy).astype(int) % 32 == 0))).astype(float)
    over((121, 116, 104), rim * joints * 0.28)
    if transparent:
        background = background / np.maximum(opacity[..., None], 1e-8)
    rgba = np.concatenate((np.uint8(np.clip(background, 0, 255)), np.uint8(np.clip(opacity[..., None] * 255, 0, 255))), axis=2)
    crop = rgba[pad * SCALE:-pad * SCALE, pad * SCALE:-pad * SCALE]
    return Image.fromarray(crop).resize((x1 - x0, y1 - y0), Image.Resampling.LANCZOS)


def sidewalk(mask, asphalt):
    occupied = {(0, 0)}
    occupied.update((dx, dy) for dx, dy, bit in NEIGHBORS if not mask & bit)
    return paint_region(occupied, (0, 0, 128, 128), asphalt)
