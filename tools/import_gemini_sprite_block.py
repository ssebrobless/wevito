#!/usr/bin/env python3
"""
Convert a Gemini-generated labeled sprite block into game-ready 28x24 frames.

Expected input shape:
- 5 columns x 6 rows
- dark separator lines between cells
- sprite art above a baked label band

The importer:
- detects the cell grid from separator lines
- crops away the label band
- flood-fills connected gray/white background from cell edges
- scales the sprite into a 28x24 transparent frame
"""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


FRAME_ORDER = [
    "idle_00",
    "idle_01",
    "idle_02",
    "idle_03",
    "walk_00",
    "walk_01",
    "walk_02",
    "walk_03",
    "walk_04",
    "walk_05",
    "eat_00",
    "eat_01",
    "eat_02",
    "eat_03",
    "happy_00",
    "happy_01",
    "happy_02",
    "happy_03",
    "sad_00",
    "sad_01",
    "sleep_00",
    "sleep_01",
    "sick_00",
    "sick_01",
    "sick_02",
    "sick_03",
    "bathe_00",
    "bathe_01",
    "bathe_02",
    "bathe_03",
]


def find_dark_clusters(values: list[int], threshold: int) -> list[tuple[int, int]]:
    clusters: list[tuple[int, int]] = []
    start: int | None = None
    for i, value in enumerate(values):
        if value >= threshold:
            if start is None:
                start = i
        elif start is not None:
            clusters.append((start, i - 1))
            start = None
    if start is not None:
        clusters.append((start, len(values) - 1))
    return clusters


def is_dark_grid_pixel(pixel: tuple[int, int, int, int], threshold: int = 120) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return False
    return (r + g + b) / 3 <= threshold


def is_blue_gray_grid_pixel(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a < 200:
        return False

    return 95 <= r <= 150 and abs((g - r) - 10) <= 8 and abs((b - g) - 12) <= 8


def cluster_indices(indices: list[int]) -> list[tuple[int, int]]:
    clusters: list[tuple[int, int]] = []
    start: int | None = None
    previous: int | None = None
    for value in indices:
        if start is None:
            start = previous = value
        elif previous is not None and value == previous + 1:
            previous = value
        else:
            clusters.append((start, previous if previous is not None else start))
            start = previous = value

    if start is not None:
        clusters.append((start, previous if previous is not None else start))

    return clusters


def detect_grid_boundaries(image: Image.Image) -> tuple[list[int], list[int]]:
    width, height = image.size
    pixels = image.load()

    dark_cols: list[int] = []
    for x in range(width):
        dark = 0
        for y in range(height):
            if is_dark_grid_pixel(pixels[x, y]):
                dark += 1
        dark_cols.append(dark)

    dark_rows: list[int] = []
    for y in range(height):
        dark = 0
        for x in range(width):
            if is_dark_grid_pixel(pixels[x, y]):
                dark += 1
        dark_rows.append(dark)

    x_clusters = find_dark_clusters(dark_cols, int(height * 0.8))
    y_clusters = find_dark_clusters(dark_rows, int(width * 0.8))

    if len(x_clusters) != 4:
        raise ValueError(f"Expected 4 interior vertical separators, found {len(x_clusters)}")
    if len(y_clusters) < 5:
        raise ValueError(f"Expected at least 5 interior horizontal separators, found {len(y_clusters)}")

    x_bounds = [0] + [round((start + end) / 2) for start, end in x_clusters] + [width]
    filtered_y_clusters = [(start, end) for start, end in y_clusters if start > 0]
    y_bounds = [0] + [round((start + end) / 2) for start, end in filtered_y_clusters] + [height]

    if len(y_bounds) != 7:
        raise ValueError(f"Expected 7 row boundaries, found {len(y_bounds)}")

    return x_bounds, y_bounds


def detect_editable_board_cells(image: Image.Image) -> list[tuple[int, int, int, int]]:
    width, height = image.size
    pixels = image.load()
    scan_top = min(55, height)

    separator_cols: list[int] = []
    for x in range(width):
        line_pixels = 0
        for y in range(scan_top, height):
            if is_blue_gray_grid_pixel(pixels[x, y]):
                line_pixels += 1
        if line_pixels >= int((height - scan_top) * 0.25):
            separator_cols.append(x)

    separator_rows: list[int] = []
    for y in range(scan_top, height):
        line_pixels = 0
        for x in range(width):
            if is_blue_gray_grid_pixel(pixels[x, y]):
                line_pixels += 1
        if line_pixels >= int(width * 0.8):
            separator_rows.append(y)

    x_clusters = cluster_indices(separator_cols)
    y_clusters = cluster_indices(separator_rows)
    if len(x_clusters) != 6 or len(y_clusters) < 12:
        raise ValueError(
            f"Expected editable-board separators, found {len(x_clusters)} vertical and {len(y_clusters)} horizontal"
        )

    x_bounds = [round((start + end) / 2) for start, end in x_clusters]
    row_lines = [round((start + end) / 2) for start, end in y_clusters]

    cells: list[tuple[int, int, int, int]] = []
    for row in range(6):
        top = row_lines[row * 2]
        bottom = row_lines[row * 2 + 1]
        for col in range(5):
            cells.append((x_bounds[col] + 2, top + 2, x_bounds[col + 1] - 2, bottom - 2))

    return cells


def is_connected_background(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return True
    spread = max(r, g, b) - min(r, g, b)
    return spread <= 8 and max(r, g, b) >= 170


def strip_background(work_region: Image.Image) -> Image.Image:
    work = work_region.copy()
    pixels = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    edge_points: list[tuple[int, int]] = []
    for x in range(width):
        edge_points.append((x, 0))
        edge_points.append((x, height - 1))
    for y in range(height):
        edge_points.append((0, y))
        edge_points.append((width - 1, y))

    for start_x, start_y in edge_points:
        if (start_x, start_y) in visited:
            continue
        if not is_connected_background(pixels[start_x, start_y]):
            continue

        queue.append((start_x, start_y))
        visited.add((start_x, start_y))

        while queue:
            x, y = queue.popleft()
            pixels[x, y] = (0, 0, 0, 0)
            for next_x, next_y in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if not (0 <= next_x < width and 0 <= next_y < height):
                    continue
                if (next_x, next_y) in visited:
                    continue
                if not is_connected_background(pixels[next_x, next_y]):
                    continue
                visited.add((next_x, next_y))
                queue.append((next_x, next_y))

    return work


def fit_sprite_to_frame(sprite: Image.Image, frame_size: tuple[int, int]) -> Image.Image:
    frame_width, frame_height = frame_size
    width, height = sprite.size
    scale = min(frame_width / width, frame_height / height)
    scaled_width = max(1, round(width * scale))
    scaled_height = max(1, round(height * scale))
    resized = sprite.resize((scaled_width, scaled_height), Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", frame_size, (0, 0, 0, 0))
    offset_x = (frame_width - scaled_width) // 2
    offset_y = frame_height - scaled_height
    canvas.alpha_composite(resized, (offset_x, offset_y))
    return canvas


def import_block(source: Path, output_dir: Path, label_reserve: int = 70) -> None:
    image = Image.open(source).convert("RGBA")
    editable_cells: list[tuple[int, int, int, int]] | None = None
    try:
        x_bounds, y_bounds = detect_grid_boundaries(image)
    except ValueError:
        editable_cells = detect_editable_board_cells(image)
        x_bounds = []
        y_bounds = []

    output_dir.mkdir(parents=True, exist_ok=True)

    for index, frame_name in enumerate(FRAME_ORDER):
        if editable_cells is not None:
            cell = image.crop(editable_cells[index])
            work_region = cell
        else:
            row = index // 5
            col = index % 5

            left = x_bounds[col] + 2
            right = x_bounds[col + 1] - 2
            top = y_bounds[row] + 2
            bottom = y_bounds[row + 1] - 2

            cell = image.crop((left, top, right, bottom))
            crop_bottom = max(1, cell.height - label_reserve)
            work_region = cell.crop((0, 0, cell.width, crop_bottom))

        work_region = strip_background(work_region)

        bbox = work_region.getbbox()
        if bbox is None:
            raise ValueError(f"No sprite content found for {frame_name}")

        pad = 4
        bx0, by0, bx1, by1 = bbox
        bx0 = max(0, bx0 - pad)
        by0 = max(0, by0 - pad)
        bx1 = min(cell.width, bx1 + pad)
        by1 = min(work_region.height, by1 + pad)

        sprite = work_region.crop((bx0, by0, bx1, by1))
        frame = fit_sprite_to_frame(sprite, (28, 24))
        frame.save(output_dir / f"{frame_name}.png")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Gemini PNG block to import")
    parser.add_argument("output_dir", type=Path, help="Destination sprite folder")
    parser.add_argument(
        "--label-reserve",
        type=int,
        default=70,
        help="Pixels reserved at the bottom of each cell for baked labels",
    )
    args = parser.parse_args()

    import_block(args.source, args.output_dir, label_reserve=args.label_reserve)


if __name__ == "__main__":
    main()
