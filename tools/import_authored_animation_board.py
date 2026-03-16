#!/usr/bin/env python3
"""
Slice an edited authoring board back into authored runtime PNG frames.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

from export_species_authoring_pack import (
    EDIT_CELL_SIZE,
    FRAME_LAYOUT,
    LABEL_HEIGHT,
    PADDING,
)
from generate_runtime_pose_sprites import (
    PlacementOverride,
    clear_bright_edge_matte,
    clear_lower_background_islands,
    clear_small_background_like_islands,
    clear_transparency_connected_matte,
    fit_to_canvas,
    isolate_subject_from_opaque_crop,
    remove_checkerboard_background,
    scrub_border_palette_matte,
    strip_palette_like_noise,
    trim_to_alpha,
)


HEADER_HEIGHT = 54


def cell_top_left(row_index: int, col_index: int) -> tuple[int, int]:
    x = PADDING + col_index * EDIT_CELL_SIZE[0]
    y = HEADER_HEIGHT + PADDING + row_index * (EDIT_CELL_SIZE[1] + LABEL_HEIGHT)
    return x, y


def extract_frame_tile(board: Image.Image, row_index: int, col_index: int) -> Image.Image:
    x, y = cell_top_left(row_index, col_index)
    crop = board.crop((x, y, x + EDIT_CELL_SIZE[0], y + EDIT_CELL_SIZE[1]))
    cleaned = remove_checkerboard_background(crop.convert("RGBA"))
    bbox = cleaned.getbbox()
    if bbox is None:
        cleaned = isolate_subject_from_opaque_crop(crop.convert("RGBA"))
    else:
        bbox_width = bbox[2] - bbox[0]
        bbox_height = bbox[3] - bbox[1]
        if bbox_width >= crop.width * 0.82 or bbox_height >= crop.height * 0.82:
            cleaned = isolate_subject_from_opaque_crop(crop.convert("RGBA"))

    cleaned = scrub_border_palette_matte(cleaned)
    cleaned = strip_palette_like_noise(cleaned)
    cleaned = clear_bright_edge_matte(cleaned)
    cleaned = clear_transparency_connected_matte(cleaned)
    cleaned = clear_small_background_like_islands(cleaned)
    cleaned = clear_lower_background_islands(cleaned)
    return cleaned


def normalize_frame(frame: Image.Image, species: str, age_stage: str, frame_name: str) -> Image.Image:
    trimmed = trim_to_alpha(frame)
    animation_name = frame_name.rsplit("_", 1)[0]
    return fit_to_canvas(trimmed, species, age_stage, {"dx": 0.0, "dy": 0.0}, PlacementOverride(), animation_name)


def derive_fallback_dir(output_dir: Path, runtime_root: Path) -> Path | None:
    parts = output_dir.parts
    if len(parts) < 4:
        return None
    color = parts[-1]
    gender = parts[-2]
    age_stage = parts[-3]
    species = parts[-4]
    candidate = runtime_root / species / age_stage / gender / color
    return candidate if candidate.exists() else None


def import_board(board_path: Path, output_dir: Path, species: str, age_stage: str, runtime_root: Path) -> list[Path]:
    board = Image.open(board_path).convert("RGBA")
    output_dir.mkdir(parents=True, exist_ok=True)
    outputs: list[Path] = []
    fallback_dir = derive_fallback_dir(output_dir, runtime_root)

    for row_index, row in enumerate(FRAME_LAYOUT):
        for col_index, frame_name in enumerate(row):
            frame = extract_frame_tile(board, row_index, col_index)
            try:
                normalized = normalize_frame(frame, species, age_stage, frame_name)
            except ValueError:
                if fallback_dir is None:
                    raise
                fallback_path = fallback_dir / f"{frame_name}.png"
                if not fallback_path.exists():
                    raise
                normalized = Image.open(fallback_path).convert("RGBA")
            out_path = output_dir / f"{frame_name}.png"
            normalized.save(out_path)
            outputs.append(out_path)

    return outputs


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--board", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--species", required=True)
    parser.add_argument("--age-stage", required=True)
    parser.add_argument("--runtime-root", type=Path, default=Path(__file__).resolve().parents[1] / "sprites_runtime")
    args = parser.parse_args()

    outputs = import_board(args.board, args.output_dir, args.species, args.age_stage, args.runtime_root)
    print(f"imported {len(outputs)} frame(s) to {args.output_dir}")


if __name__ == "__main__":
    main()
