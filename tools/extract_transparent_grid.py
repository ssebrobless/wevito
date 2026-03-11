#!/usr/bin/env python3
"""
Extract evenly spaced transparent sprites from a reference sheet.

This is intended for Gemini-generated sheets where:
- each asset is isolated on transparent background
- assets are laid out in a fixed grid
- there may be large transparent margins and uneven asset sizes

The extractor projects non-transparent pixels across each axis to detect the
occupied bands, infers grid cell boundaries, trims transparent padding from
each cell, and optionally fits the sprite into a fixed output canvas.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def find_clusters(values: list[int], threshold: int) -> list[tuple[int, int]]:
    clusters: list[tuple[int, int]] = []
    start: int | None = None
    for index, value in enumerate(values):
        if value > threshold:
            if start is None:
                start = index
        elif start is not None:
            clusters.append((start, index - 1))
            start = None
    if start is not None:
        clusters.append((start, len(values) - 1))
    return clusters


def merge_nearby(clusters: list[tuple[int, int]], max_gap: int) -> list[tuple[int, int]]:
    if not clusters:
        return []
    merged = [clusters[0]]
    for start, end in clusters[1:]:
        prev_start, prev_end = merged[-1]
        if start - prev_end <= max_gap:
            merged[-1] = (prev_start, max(prev_end, end))
        else:
            merged.append((start, end))
    return merged


def axis_clusters(image: Image.Image, axis: str) -> list[tuple[int, int]]:
    alpha = image.getchannel("A")
    width, height = image.size
    values: list[int] = []
    if axis == "x":
        for x in range(width):
            total = 0
            for y in range(height):
                total += 1 if alpha.getpixel((x, y)) > 0 else 0
            values.append(total)
        clusters = find_clusters(values, 0)
        return merge_nearby(clusters, max(1, width // 100))

    for y in range(height):
        total = 0
        for x in range(width):
            total += 1 if alpha.getpixel((x, y)) > 0 else 0
        values.append(total)
    clusters = find_clusters(values, 0)
    return merge_nearby(clusters, max(1, height // 100))


def infer_boundaries(clusters: list[tuple[int, int]], expected: int, axis_size: int) -> list[int]:
    if len(clusters) != expected:
        raise ValueError(f"Expected {expected} occupied bands, found {len(clusters)}")

    bounds = [0]
    for index in range(len(clusters) - 1):
        _, left_end = clusters[index]
        right_start, _ = clusters[index + 1]
        bounds.append(round((left_end + right_start) / 2))
    bounds.append(axis_size)
    return bounds


def fit_to_canvas(sprite: Image.Image, size: tuple[int, int]) -> Image.Image:
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    width, height = sprite.size
    target_width, target_height = size
    scale = min(target_width / width, target_height / height)
    resized = sprite.resize(
        (max(1, round(width * scale)), max(1, round(height * scale))),
        Image.Resampling.LANCZOS,
    )
    offset_x = (target_width - resized.width) // 2
    offset_y = (target_height - resized.height) // 2
    canvas.alpha_composite(resized, (offset_x, offset_y))
    return canvas


def extract_grid(
    source: Path,
    output_dir: Path,
    cols: int,
    rows: int,
    names: list[str],
    out_size: tuple[int, int] | None,
) -> None:
    image = Image.open(source).convert("RGBA")
    width, height = image.size

    x_clusters = axis_clusters(image, "x")
    y_clusters = axis_clusters(image, "y")
    x_bounds = infer_boundaries(x_clusters, cols, width)
    y_bounds = infer_boundaries(y_clusters, rows, height)

    output_dir.mkdir(parents=True, exist_ok=True)
    index = 0
    for row in range(rows):
        for col in range(cols):
            name = names[index]
            index += 1
            left = x_bounds[col]
            right = x_bounds[col + 1]
            top = y_bounds[row]
            bottom = y_bounds[row + 1]
            cell = image.crop((left, top, right, bottom))
            bbox = cell.getbbox()
            if bbox is None:
                raise ValueError(f"No visible sprite found for {name}")
            sprite = cell.crop(bbox)
            if out_size is not None:
                sprite = fit_to_canvas(sprite, out_size)
            sprite.save(output_dir / f"{name}.png")


def parse_size(value: str) -> tuple[int, int]:
    width, height = value.lower().split("x", maxsplit=1)
    return int(width), int(height)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output_dir", type=Path)
    parser.add_argument("--cols", type=int, required=True)
    parser.add_argument("--rows", type=int, required=True)
    parser.add_argument("--names", nargs="+", required=True)
    parser.add_argument("--out-size", type=parse_size)
    args = parser.parse_args()

    expected = args.cols * args.rows
    if len(args.names) != expected:
        raise ValueError(f"Expected {expected} names, got {len(args.names)}")

    extract_grid(
        source=args.source,
        output_dir=args.output_dir,
        cols=args.cols,
        rows=args.rows,
        names=args.names,
        out_size=args.out_size,
    )


if __name__ == "__main__":
    main()
