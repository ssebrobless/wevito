#!/usr/bin/env python3
"""
Import a focused motion-family board back into authored runtime frames.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

from authored_motion_specs import get_family_layout
from export_species_authoring_pack import EDIT_CELL_SIZE, LABEL_HEIGHT, PADDING
from generate_runtime_pose_sprites import (
    PlacementOverride,
    clear_bright_edge_matte,
    clear_lower_background_islands,
    clear_small_background_like_islands,
    clear_species_artifacts,
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


def import_family_board(board_path: Path, output_dir: Path, species: str, age_stage: str, family: str) -> list[Path]:
    board = Image.open(board_path).convert("RGBA")
    output_dir.mkdir(parents=True, exist_ok=True)
    outputs: list[Path] = []
    layout = get_family_layout(family)
    for row_index, row in enumerate(layout):
        for col_index, frame_name in enumerate(row):
            if not frame_name:
                continue
            frame = extract_frame_tile(board, row_index, col_index)
            normalized = normalize_frame(frame, species, age_stage, frame_name)
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
    parser.add_argument("--family", required=True)
    args = parser.parse_args()

    outputs = import_family_board(args.board, args.output_dir, args.species, args.age_stage, args.family)
    print(f"imported {len(outputs)} frame(s) to {args.output_dir}")


if __name__ == "__main__":
    main()
