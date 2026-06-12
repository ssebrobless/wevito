#!/usr/bin/env python3
"""
Import a focused motion-family board back into authored runtime frames.

C-PHASE 203B additions (Amendment R2b anti-blur policy):
- board cell geometry comes from the pack's pack-metadata.json ("cellSize")
  when --pack-metadata is given (handoff packs may use enlarged 256px cells);
- a deterministic sharpness gate compares each imported frame's edge energy
  (variance of the Laplacian over body pixels) against the current runtime
  frame for the same species/age/gender; frames below --sharpness-ratio of
  the reference are rejected and NOTHING is written (re-roll the board).
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
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


def cell_top_left(row_index: int, col_index: int, cell_size: tuple[int, int]) -> tuple[int, int]:
    x = PADDING + col_index * cell_size[0]
    y = HEADER_HEIGHT + PADDING + row_index * (cell_size[1] + LABEL_HEIGHT)
    return x, y


def edge_energy(image: Image.Image) -> float:
    """Variance of a 4-neighbor Laplacian over body (alpha>0) pixels."""
    rgba = np.array(image.convert("RGBA"), dtype=np.float64)
    alpha = rgba[:, :, 3] > 0
    if alpha.sum() < 16:
        return 0.0
    luma = rgba[:, :, 0] * 0.299 + rgba[:, :, 1] * 0.587 + rgba[:, :, 2] * 0.114
    lap = (
        -4.0 * luma
        + np.roll(luma, 1, axis=0)
        + np.roll(luma, -1, axis=0)
        + np.roll(luma, 1, axis=1)
        + np.roll(luma, -1, axis=1)
    )
    return float(lap[alpha].var())


def extract_frame_tile(board: Image.Image, row_index: int, col_index: int, cell_size: tuple[int, int] = EDIT_CELL_SIZE) -> Image.Image:
    x, y = cell_top_left(row_index, col_index, cell_size)
    crop = board.crop((x, y, x + cell_size[0], y + cell_size[1]))
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


def import_family_board(
    board_path: Path,
    output_dir: Path,
    species: str,
    age_stage: str,
    family: str,
    cell_size: tuple[int, int] = EDIT_CELL_SIZE,
    sharpness_reference_dir: Path | None = None,
    sharpness_ratio: float = 0.55,
) -> list[Path]:
    board = Image.open(board_path).convert("RGBA")
    layout = get_family_layout(family)

    staged: list[tuple[str, Image.Image]] = []
    rejections: list[str] = []
    metrics: dict[str, dict[str, float]] = {}
    for row_index, row in enumerate(layout):
        for col_index, frame_name in enumerate(row):
            if not frame_name:
                continue
            frame = extract_frame_tile(board, row_index, col_index, cell_size)
            normalized = normalize_frame(frame, species, age_stage, frame_name)
            candidate_energy = edge_energy(normalized)
            entry = {"candidateEdgeEnergy": round(candidate_energy, 2)}
            if sharpness_reference_dir is not None:
                reference_path = sharpness_reference_dir / f"{frame_name}.png"
                if reference_path.exists():
                    reference_energy = edge_energy(Image.open(reference_path))
                    entry["referenceEdgeEnergy"] = round(reference_energy, 2)
                    entry["ratio"] = round(candidate_energy / reference_energy, 3) if reference_energy else None
                    if reference_energy > 0 and candidate_energy < sharpness_ratio * reference_energy:
                        rejections.append(
                            f"{frame_name}: edge energy {candidate_energy:.1f} < "
                            f"{sharpness_ratio:.2f} x reference {reference_energy:.1f} (blurry — re-roll the board)"
                        )
            metrics[frame_name] = entry
            staged.append((frame_name, normalized))

    if rejections:
        details = "\n  ".join(rejections)
        raise SystemExit(f"SHARPNESS GATE FAILED — nothing imported:\n  {details}")

    output_dir.mkdir(parents=True, exist_ok=True)
    outputs: list[Path] = []
    for frame_name, normalized in staged:
        out_path = output_dir / f"{frame_name}.png"
        normalized.save(out_path)
        outputs.append(out_path)
    metrics_path = output_dir / f"{family}-sharpness-metrics.json"
    metrics_path.write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    return outputs


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--board", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--species", required=True)
    parser.add_argument("--age-stage", required=True)
    parser.add_argument("--family", required=True)
    parser.add_argument("--pack-metadata", type=Path, help="pack-metadata.json from the handoff pack (supplies cellSize)")
    parser.add_argument("--sharpness-reference-dir", type=Path, help="directory of current runtime frames for the same row; enables the sharpness gate")
    parser.add_argument("--sharpness-ratio", type=float, default=0.55)
    args = parser.parse_args()

    cell_size = EDIT_CELL_SIZE
    if args.pack_metadata is not None:
        metadata = json.loads(args.pack_metadata.read_text(encoding="utf-8"))
        if "cellSize" in metadata:
            cell_size = tuple(metadata["cellSize"])

    outputs = import_family_board(
        args.board,
        args.output_dir,
        args.species,
        args.age_stage,
        args.family,
        cell_size=cell_size,
        sharpness_reference_dir=args.sharpness_reference_dir,
        sharpness_ratio=args.sharpness_ratio,
    )
    print(f"imported {len(outputs)} frame(s) to {args.output_dir}")


if __name__ == "__main__":
    main()
