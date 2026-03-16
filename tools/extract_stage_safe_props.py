from __future__ import annotations

import argparse
import json
from collections import Counter, deque
from pathlib import Path

from PIL import Image


CELL_COLUMNS = 3
CELL_ROWS = 3


PROP_MAP = {
    "utility-shelter-props.png": {
        (0, 2): ("items", "toys_b", "rock_basking_spot"),
        (2, 1): ("items", "toys_b", "moss_bed"),
        (2, 2): ("items", "toys_b", "blanket_mat"),
    },
    "water-feeding-containers.png": {
        (0, 0): ("items", "containers", "water_bowl"),
        (0, 1): ("items", "containers", "shallow_water_dish"),
        (1, 0): ("items", "containers", "seed_tray"),
        (1, 1): ("items", "containers", "hanging_feeder"),
        (1, 2): ("items", "containers", "pond_dish"),
        (2, 1): ("items", "containers", "treat_cup"),
    },
    "toys-enrichment-A.png": {
        (1, 0): ("items", "toys_a", "bell_toy"),
        (1, 1): ("items", "toys_a", "branch_perch"),
        (2, 1): ("items", "toys_a", "mirror_trinket"),
        (2, 2): ("items", "toys_a", "leaf_pile"),
        (0, 2): ("items", "toys_a", "rope_toy"),
    },
    "toys-enrichment-B.png": {
        (0, 0): ("items", "toys_b", "nest_bed"),
        (0, 1): ("items", "toys_b", "crate_hideout"),
        (1, 1): ("items", "toys_b", "stump_perch"),
        (2, 0): ("items", "toys_b", "hay_bed"),
        (2, 1): ("items", "toys_b", "log_shelter"),
    },
}

CELL_TRIM_OVERRIDES = {
    "water_bowl": (0.08, 0.04, 0.92, 0.54),
    "shallow_water_dish": (0.08, 0.06, 0.92, 0.42),
    "seed_tray": (0.12, 0.18, 0.88, 0.82),
    "hanging_feeder": (0.26, 0.08, 0.74, 0.88),
    "pond_dish": (0.12, 0.14, 0.88, 0.70),
    "treat_cup": (0.28, 0.18, 0.72, 0.90),
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Extract clean stage-safe shared props from original incoming boards.")
    parser.add_argument("--source", default="incoming_sprites", help="Directory containing the original prop boards.")
    parser.add_argument("--output", default="sprites_shared_runtime", help="Shared runtime asset root.")
    return parser.parse_args()


def iter_components(alpha: list[list[int]]) -> list[list[tuple[int, int]]]:
    height = len(alpha)
    width = len(alpha[0]) if height else 0
    visited = [[False for _ in range(width)] for _ in range(height)]
    components: list[list[tuple[int, int]]] = []

    for y in range(height):
        for x in range(width):
            if visited[y][x] or alpha[y][x] == 0:
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited[y][x] = True
            component: list[tuple[int, int]] = []
            while queue:
                cx, cy = queue.popleft()
                component.append((cx, cy))
                for nx, ny in (
                    (cx - 1, cy),
                    (cx + 1, cy),
                    (cx, cy - 1),
                    (cx, cy + 1),
                    (cx - 1, cy - 1),
                    (cx + 1, cy - 1),
                    (cx - 1, cy + 1),
                    (cx + 1, cy + 1),
                ):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height or visited[ny][nx] or alpha[ny][nx] == 0:
                        continue
                    visited[ny][nx] = True
                    queue.append((nx, ny))

            components.append(component)

    return components


def quantize(rgb: tuple[int, int, int], step: int = 8) -> tuple[int, int, int]:
    return tuple(int(round(channel / step) * step) for channel in rgb)


def color_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    dr = a[0] - b[0]
    dg = a[1] - b[1]
    db = a[2] - b[2]
    return (dr * dr + dg * dg + db * db) ** 0.5


def saturation(rgb: tuple[int, int, int]) -> int:
    return max(rgb) - min(rgb)


def is_neutral(rgb: tuple[int, int, int], max_saturation: int = 34) -> bool:
    return saturation(rgb) <= max_saturation


def collect_border_pixels(image: Image.Image) -> list[tuple[int, int, int]]:
    width, height = image.size
    pixels = image.load()
    colors: list[tuple[int, int, int]] = []
    for x in range(width):
        for y in (0, height - 1):
            rgba = pixels[x, y]
            if rgba[3] > 0:
                colors.append(rgba[:3])
    for y in range(height):
        for x in (0, width - 1):
            rgba = pixels[x, y]
            if rgba[3] > 0:
                colors.append(rgba[:3])
    return colors


def resolve_background_palette(border_pixels: list[tuple[int, int, int]]) -> list[tuple[int, int, int]]:
    counts = Counter(quantize(color) for color in border_pixels)
    palette: list[tuple[int, int, int]] = []
    for color, count in counts.most_common(8):
        if count < 8:
            continue
        if not is_neutral(color) and count < 18:
            continue
        palette.append(color)
    return palette[:6]


def build_background_mask(image: Image.Image, palette: list[tuple[int, int, int]]) -> list[list[bool]]:
    width, height = image.size
    pixels = image.load()
    mask = [[False for _ in range(width)] for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()

    def is_background_like(px: int, py: int) -> bool:
        rgba = pixels[px, py]
        if rgba[3] == 0:
            return True
        rgb = rgba[:3]
        nearest = min(color_distance(rgb, candidate) for candidate in palette) if palette else 999
        brightness = (rgb[0] + rgb[1] + rgb[2]) / 3
        return (
            (nearest <= 72 and is_neutral(rgb, max_saturation=44))
            or (is_neutral(rgb, max_saturation=28) and brightness <= 190)
        )

    for x in range(width):
        for y in (0, height - 1):
            if not mask[y][x] and is_background_like(x, y):
                mask[y][x] = True
                queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if not mask[y][x] and is_background_like(x, y):
                mask[y][x] = True
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        for nx, ny in (
            (x - 1, y),
            (x + 1, y),
            (x, y - 1),
            (x, y + 1),
            (x - 1, y - 1),
            (x + 1, y - 1),
            (x - 1, y + 1),
            (x + 1, y + 1),
        ):
            if nx < 0 or ny < 0 or nx >= width or ny >= height or mask[ny][nx]:
                continue
            if is_background_like(nx, ny):
                mask[ny][nx] = True
                queue.append((nx, ny))

    return mask


def strip_background(image: Image.Image) -> tuple[Image.Image, list[tuple[int, int, int]]]:
    rgba = image.convert("RGBA")
    palette = resolve_background_palette(collect_border_pixels(rgba))
    if not palette:
        return rgba, []

    width, height = rgba.size
    pixels = rgba.load()
    cleaned = rgba.copy()
    target = cleaned.load()
    mask = build_background_mask(rgba, palette)

    for y in range(height):
        for x in range(width):
            if mask[y][x]:
                target[x, y] = (0, 0, 0, 0)

    for y in range(height):
        for x in range(width):
            rgba_pixel = target[x, y]
            if rgba_pixel[3] == 0:
                continue
            rgb = rgba_pixel[:3]
            nearest = min(color_distance(rgb, candidate) for candidate in palette)
            if nearest <= 26 and is_neutral(rgb, max_saturation=44):
                target[x, y] = (0, 0, 0, 0)

    return cleaned, palette


def tighten_outline(image: Image.Image, palette: list[tuple[int, int, int]]) -> Image.Image:
    if not palette:
        return image

    rgba = image.convert("RGBA")
    width, height = rgba.size
    source = rgba.load()
    cleaned = rgba.copy()
    target = cleaned.load()

    for y in range(height):
        for x in range(width):
            rgba_pixel = source[x, y]
            if rgba_pixel[3] == 0:
                continue

            rgb = rgba_pixel[:3]
            nearest = min(color_distance(rgb, candidate) for candidate in palette)
            if nearest > 70 or not is_neutral(rgb, max_saturation=44):
                continue

            touching_transparent = False
            solid_neighbors = 0
            for nx, ny in (
                (x - 1, y),
                (x + 1, y),
                (x, y - 1),
                (x, y + 1),
                (x - 1, y - 1),
                (x + 1, y - 1),
                (x - 1, y + 1),
                (x + 1, y + 1),
            ):
                if nx < 0 or ny < 0 or nx >= width or ny >= height:
                    touching_transparent = True
                    continue
                neighbor = source[nx, ny]
                if neighbor[3] == 0:
                    touching_transparent = True
                else:
                    solid_neighbors += 1

            if not touching_transparent:
                continue

            if nearest <= 42 and solid_neighbors <= 4:
                target[x, y] = (0, 0, 0, 0)
            else:
                target[x, y] = (rgba_pixel[0], rgba_pixel[1], rgba_pixel[2], max(0, int(rgba_pixel[3] * 0.55)))

    bbox = cleaned.getbbox()
    return cleaned.crop(bbox) if bbox is not None else cleaned


def crop_cell(board: Image.Image, row: int, col: int, asset_id: str) -> Image.Image:
    width, height = board.size
    x_edges = [round(width * index / CELL_COLUMNS) for index in range(CELL_COLUMNS + 1)]
    y_edges = [round(height * index / CELL_ROWS) for index in range(CELL_ROWS + 1)]
    left = x_edges[col] + 2
    top = y_edges[row] + 2
    right = x_edges[col + 1] - 2
    bottom = y_edges[row + 1] - 2
    cell = board.crop((left, top, right, bottom))
    trim = CELL_TRIM_OVERRIDES.get(asset_id)
    if trim is None:
        return cell

    trim_left = round(cell.width * trim[0])
    trim_top = round(cell.height * trim[1])
    trim_right = round(cell.width * trim[2])
    trim_bottom = round(cell.height * trim[3])
    trim_right = max(trim_left + 1, trim_right)
    trim_bottom = max(trim_top + 1, trim_bottom)
    return cell.crop((trim_left, trim_top, trim_right, trim_bottom))


def isolate_subject(image: Image.Image) -> Image.Image:
    rgba, palette = strip_background(image)
    width, height = rgba.size
    pixels = rgba.load()
    alpha = [[pixels[x, y][3] for x in range(width)] for y in range(height)]
    components = iter_components(alpha)
    if not components:
        return rgba

    center_x = width / 2.0
    center_y = height / 2.0

    def score(component: list[tuple[int, int]]) -> float:
        xs = [p[0] for p in component]
        ys = [p[1] for p in component]
        cx = (min(xs) + max(xs)) / 2.0
        cy = (min(ys) + max(ys)) / 2.0
        distance = abs(cx - center_x) + abs(cy - center_y)
        return len(component) - distance * 0.85

    primary = max(components, key=score)
    keep: set[tuple[int, int]] = set(primary)
    primary_xs = [p[0] for p in primary]
    primary_ys = [p[1] for p in primary]
    primary_bounds = (min(primary_xs), min(primary_ys), max(primary_xs), max(primary_ys))

    for component in components:
        if component is primary or len(component) < 36:
            continue
        xs = [p[0] for p in component]
        ys = [p[1] for p in component]
        bounds = (min(xs), min(ys), max(xs), max(ys))
        horizontal_gap = max(0, max(primary_bounds[0] - bounds[2], bounds[0] - primary_bounds[2]))
        vertical_gap = max(0, max(primary_bounds[1] - bounds[3], bounds[1] - primary_bounds[3]))
        if horizontal_gap <= 44 and vertical_gap <= 36:
            keep.update(component)

    cleaned = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    cleaned_pixels = cleaned.load()
    for x, y in keep:
        cleaned_pixels[x, y] = pixels[x, y]

    bbox = cleaned.getbbox()
    cropped = cleaned.crop(bbox) if bbox is not None else cleaned
    return remove_small_components(tighten_outline(cropped, palette), 10)


def remove_small_components(image: Image.Image, min_pixels: int) -> Image.Image:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()
    alpha = [[pixels[x, y][3] for x in range(width)] for y in range(height)]
    components = iter_components(alpha)
    if not components:
        return rgba

    cleaned = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    target = cleaned.load()
    for component in components:
        if len(component) < min_pixels:
            continue
        for x, y in component:
            target[x, y] = pixels[x, y]

    bbox = cleaned.getbbox()
    return cleaned.crop(bbox) if bbox is not None else cleaned


def main() -> None:
    args = parse_args()
    source_root = Path(args.source)
    output_root = Path(args.output)
    extracted: list[dict[str, str]] = []

    for board_name, mappings in PROP_MAP.items():
        board_path = source_root / board_name
        if not board_path.exists():
            continue

        board = Image.open(board_path).convert("RGBA")
        for (row, col), (root_folder, category, asset_id) in mappings.items():
            subject = isolate_subject(crop_cell(board, row, col, asset_id))
            target_path = output_root / root_folder / category / f"{asset_id}.png"
            target_path.parent.mkdir(parents=True, exist_ok=True)
            subject.save(target_path)
            extracted.append(
                {
                    "board": board_name,
                    "row": str(row),
                    "col": str(col),
                    "target": str(target_path),
                    "size": f"{subject.size[0]}x{subject.size[1]}",
                }
            )

    summary_path = output_root / "stage-prop-extraction-summary.json"
    summary_path.write_text(json.dumps({"count": len(extracted), "items": extracted}, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
