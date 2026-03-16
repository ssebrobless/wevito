#!/usr/bin/env python3
"""
Render visual contact sheets for generated runtime pet sprites.

This gives a quick visual QA layer over the canonical runtime tree so broken
extractions or obviously bad procedural animations show up before launching the
shell.
"""

from __future__ import annotations

import argparse
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SPRITE_ROOT = ROOT / "sprites_runtime"
DEFAULT_AUTHORED_ROOT = ROOT / "sprites_authored_verified"
DEFAULT_OUTPUT_ROOT = ROOT / "vnext" / "artifacts" / "sprite-previews"
FRAME_SCALE = 6
FRAME_SIZE = (28 * FRAME_SCALE, 24 * FRAME_SCALE)
ROW_LABEL_WIDTH = 128
HEADER_HEIGHT = 42
ROW_HEIGHT = FRAME_SIZE[1] + 12
PADDING = 10
ANIMATION_ORDER = [
    ("idle", 4),
    ("walk", 6),
    ("eat", 4),
    ("happy", 4),
    ("sad", 2),
    ("sleep", 2),
    ("sick", 4),
    ("bathe", 4),
]
ROW_ORDER = [
    ("baby", "male"),
    ("baby", "female"),
    ("teen", "male"),
    ("teen", "female"),
    ("adult", "male"),
    ("adult", "female"),
]
COLORS = ["red", "orange", "yellow", "blue", "indigo", "violet"]


def load_font(size: int) -> ImageFont.ImageFont:
    for candidate in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def checkerboard(size: tuple[int, int]) -> Image.Image:
    image = Image.new("RGBA", size, (0, 0, 0, 255))
    draw = ImageDraw.Draw(image)
    cell = 12
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            fill = (238, 238, 238, 255) if ((x // cell) + (y // cell)) % 2 == 0 else (204, 204, 204, 255)
            draw.rectangle((x, y, min(size[0], x + cell), min(size[1], y + cell)), fill=fill)
    return image


def resolve_animation_frames(runtime_directory: Path, authored_directory: Path, animation_name: str, count: int) -> list[tuple[Path, bool]]:
    runtime = sorted(runtime_directory.glob(f"{animation_name}_*.png"))
    authored = sorted(authored_directory.glob(f"{animation_name}_*.png"))

    if getattr(resolve_animation_frames, "prefer_authored", False) and len(authored) == count:
        return [(path, True) for path in authored]

    if len(runtime) == count:
        return [(path, False) for path in runtime]

    return [(path, True) for path in authored]


def frame_paths_for(runtime_directory: Path, authored_directory: Path) -> list[tuple[Path, bool]]:
    frames: list[tuple[Path, bool]] = []
    for animation_name, count in ANIMATION_ORDER:
        frames.extend(resolve_animation_frames(runtime_directory, authored_directory, animation_name, count))
    return frames


def render_species_sheet(sprite_root: Path, authored_root: Path, output_root: Path, species: str) -> Path:
    frames_per_row = sum(count for _, count in ANIMATION_ORDER)
    width = PADDING + ROW_LABEL_WIDTH + (frames_per_row * FRAME_SIZE[0]) + PADDING
    height = HEADER_HEIGHT + PADDING + (len(ROW_ORDER) * ROW_HEIGHT) + PADDING
    sheet = checkerboard((width, height)).convert("RGBA")
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(20)
    label_font = load_font(14)
    draw.rectangle((0, 0, width, HEADER_HEIGHT), fill=(39, 45, 54, 255))
    draw.text((PADDING, 10), f"{species} sprite runtime preview", fill=(245, 248, 250, 255), font=title_font)

    header_x = PADDING + ROW_LABEL_WIDTH
    cursor_x = header_x
    for animation_name, count in ANIMATION_ORDER:
        block_width = count * FRAME_SIZE[0]
        draw.text((cursor_x + 8, HEADER_HEIGHT - 24), animation_name, fill=(24, 31, 40, 255), font=label_font)
        draw.line((cursor_x, HEADER_HEIGHT, cursor_x, height - PADDING), fill=(112, 120, 128, 255), width=2)
        cursor_x += block_width
    draw.line((cursor_x, HEADER_HEIGHT, cursor_x, height - PADDING), fill=(112, 120, 128, 255), width=2)

    for row_index, (age_stage, gender) in enumerate(ROW_ORDER):
        y = HEADER_HEIGHT + PADDING + (row_index * ROW_HEIGHT)
        draw.text((PADDING, y + 10), f"{age_stage}/{gender}", fill=(24, 31, 40, 255), font=label_font)
        directory = sprite_root / species / age_stage / gender / "blue"
        authored_directory = authored_root / species / age_stage / gender / "blue"
        for frame_index, (frame_path, authored) in enumerate(frame_paths_for(directory, authored_directory)):
            frame = Image.open(frame_path).convert("RGBA")
            frame = frame.resize(FRAME_SIZE, Image.Resampling.NEAREST)
            frame_x = header_x + (frame_index * FRAME_SIZE[0])
            frame_y = y
            tile = checkerboard(FRAME_SIZE)
            tile.alpha_composite(frame, (0, 0))
            sheet.alpha_composite(tile, (frame_x, frame_y))
            if authored:
                draw.rectangle((frame_x, frame_y, frame_x + FRAME_SIZE[0], frame_y + FRAME_SIZE[1]), outline=(163, 89, 219, 255), width=3)

    output_root.mkdir(parents=True, exist_ok=True)
    out_path = output_root / f"{species}-preview.png"
    sheet.save(out_path)
    return out_path


def render_color_sheet(sprite_root: Path, authored_root: Path, output_root: Path, species: str) -> Path:
    width = PADDING + (len(COLORS) * FRAME_SIZE[0]) + PADDING
    height = HEADER_HEIGHT + PADDING + (len(ROW_ORDER) * ROW_HEIGHT) + PADDING
    sheet = checkerboard((width, height)).convert("RGBA")
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(20)
    label_font = load_font(14)
    draw.rectangle((0, 0, width, HEADER_HEIGHT), fill=(49, 60, 74, 255))
    draw.text((PADDING, 10), f"{species} color variants", fill=(245, 248, 250, 255), font=title_font)

    for index, color in enumerate(COLORS):
        x = PADDING + (index * FRAME_SIZE[0])
        draw.text((x + 8, HEADER_HEIGHT - 24), color, fill=(24, 31, 40, 255), font=label_font)

    for row_index, (age_stage, gender) in enumerate(ROW_ORDER):
        y = HEADER_HEIGHT + PADDING + (row_index * ROW_HEIGHT)
        draw.text((PADDING, y + 10), f"{age_stage}/{gender}", fill=(24, 31, 40, 255), font=label_font)
        for color_index, color in enumerate(COLORS):
            directory = sprite_root / species / age_stage / gender / color
            authored_directory = authored_root / species / age_stage / gender / color
            frame_path, authored = resolve_animation_frames(directory, authored_directory, "idle", 4)[0]
            frame = Image.open(frame_path).convert("RGBA")
            frame = frame.resize(FRAME_SIZE, Image.Resampling.NEAREST)
            frame_x = PADDING + (color_index * FRAME_SIZE[0])
            tile = checkerboard(FRAME_SIZE)
            tile.alpha_composite(frame, (0, 0))
            sheet.alpha_composite(tile, (frame_x, y))
            if authored:
                draw.rectangle((frame_x, y, frame_x + FRAME_SIZE[0], y + FRAME_SIZE[1]), outline=(163, 89, 219, 255), width=3)

    output_root.mkdir(parents=True, exist_ok=True)
    out_path = output_root / f"{species}-colors.png"
    sheet.save(out_path)
    return out_path


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sprite-root", type=Path, default=DEFAULT_SPRITE_ROOT)
    parser.add_argument("--authored-root", type=Path, default=DEFAULT_AUTHORED_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--prefer-authored", action="store_true")
    args = parser.parse_args()
    resolve_animation_frames.prefer_authored = args.prefer_authored

    species_ids = sorted(path.name for path in args.sprite_root.iterdir() if path.is_dir())
    args.output_root.mkdir(parents=True, exist_ok=True)
    outputs: list[Path] = []
    for species in species_ids:
        outputs.append(render_species_sheet(args.sprite_root, args.authored_root, args.output_root, species))
        outputs.append(render_color_sheet(args.sprite_root, args.authored_root, args.output_root, species))

    index_path = args.output_root / "index.txt"
    index_path.write_text("\n".join(str(path) for path in outputs), encoding="utf-8")
    print(f"rendered {len(outputs)} preview sheet(s) to {args.output_root}")


if __name__ == "__main__":
    main()
