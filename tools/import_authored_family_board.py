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
    if cell_size != EDIT_CELL_SIZE:
        # 256px-cell anti-blur era (C-203B+): segment by the drawn dark OUTLINE,
        # not by colour. The earlier brightness-flood (clean_cell_border_flood)
        # could not tell a white body (pigeon) from the light checker and ate
        # the body (C-199R round-2 rejection: holes / missing chunks / faint
        # white birds). segment_cell_by_outline treats the dark silhouette
        # outline as a sealed boundary, floods the EXTERIOR from the canvas
        # border, and keeps everything the flood cannot reach — colour-agnostic,
        # so white bodies survive, enclosed belly gaps fill, between-the-legs
        # gaps (open to the border) stay transparent, and detached halo specks
        # are dropped by keep-largest-component.
        return segment_cell_by_outline(crop.convert("RGBA"))
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


def segment_cell_by_outline(
    crop: Image.Image,
    dark_threshold: int = 150,
    seal: int = 2,
    ring: int = 6,
) -> Image.Image:
    """Isolate the subject by treating its dark outline as a sealed boundary.

    The board renderer draws each frame as a colour fill bounded by a dark
    silhouette outline, on a light-gray checkerboard, with a 2px mid-gray
    cell-outline rectangle at the cell edge. This segmenter:

    1. marks dark pixels (the drawn outline AND any dark body) as a barrier and
       dilates it by ``seal`` to close hairline gaps in the outline;
    2. clears the outer ``ring`` so the cell-outline rectangle is removed and the
       checker connects to the canvas border;
    3. flood-labels the non-barrier ("free") space and calls every component
       touching the border the EXTERIOR (checker + open gaps such as between the
       legs, which drain to the border);
    4. body = everything not exterior, then ``binary_fill_holes`` fills any
       checker pocket fully enclosed by the outline (belly holes), and
       keep-largest-component drops detached halo specks.

    Colour plays no role, so a white body on a near-white checker (pigeon) is
    recovered as reliably as a dark one — fixing the C-199R round-2 rejection
    where the brightness flood ate light bodies.
    """
    from scipy import ndimage  # local import keeps module load light

    arr = np.array(crop.convert("RGBA"), dtype=np.uint8)
    height, width = arr.shape[:2]
    brightness = arr[:, :, :3].astype(np.int16).mean(axis=2)

    barrier = ndimage.binary_dilation(brightness <= dark_threshold, iterations=seal)
    barrier[:ring, :] = barrier[-ring:, :] = barrier[:, :ring] = barrier[:, -ring:] = False

    labels, _ = ndimage.label(~barrier)
    border_labels = set(labels[0, :]) | set(labels[-1, :]) | set(labels[:, 0]) | set(labels[:, -1])
    border_labels.discard(0)
    exterior = np.isin(labels, sorted(border_labels))

    body = ndimage.binary_fill_holes(~exterior)
    comp, count = ndimage.label(body)
    if count > 1:
        sizes = ndimage.sum(np.ones_like(comp), comp, range(1, count + 1))
        body = comp == (1 + int(np.argmax(sizes)))

    out = np.zeros_like(arr)
    out[:, :, :3] = arr[:, :, :3]
    out[:, :, 3] = np.where(body, 255, 0)
    return Image.fromarray(out, "RGBA")


def clean_cell_border_flood(crop: Image.Image, channel_spread: int = 14, brightness_floor: int = 210) -> Image.Image:
    """Strip background as the border-connected near-gray/near-white region.

    Keeps every pixel not reachable from the canvas border through
    background-like colors — light body interiors are preserved by
    construction. A final 1px defringe drops boundary pixels that are
    themselves background-colored (anti-matte).

    The board renderer draws a 2px mid-gray cell-outline rectangle at each
    cell edge (render_editable_board, outline (122,132,144)). That outline is
    darker than the checker and would wall the checkerboard off from the canvas
    border, leaving the whole background enclosed (counted as one giant hole).
    The sprite is centered with a wide inset, so the outer ring of the cell is
    always background — we force it transparent first, which removes the outline
    and lets the checker flood from the border.
    """
    from scipy import ndimage  # local import keeps module load light

    arr = np.array(crop.convert("RGBA"), dtype=np.uint8)
    height, width = arr.shape[:2]
    rgb = arr[:, :, :3].astype(np.int16)
    spread = rgb.max(axis=2) - rgb.min(axis=2)
    brightness = rgb.mean(axis=2)
    background_like = (spread <= channel_spread) & (brightness >= brightness_floor)  # checker grays + white
    background_like |= arr[:, :, 3] == 0

    # The subject is drawn with a dark silhouette outline. White/light body
    # interiors (e.g. the pigeon chest) are background-colored too, so only the
    # outline separates them from the exterior. Treat the outline as a flood
    # barrier and dilate it to seal hairline gaps, otherwise the flood leaks
    # through and eats the body (C-199R round-1: missing chunks).
    dark = brightness <= 140
    barrier = ndimage.binary_dilation(dark, iterations=2)
    floodable = background_like & ~barrier

    # Force the outer ring floodable so the cell-outline rectangle is removed
    # and the checker becomes border-connected.
    ring = max(3, min(height, width) // 32)
    floodable[:ring, :] = True
    floodable[-ring:, :] = True
    floodable[:, :ring] = True
    floodable[:, -ring:] = True

    # 4-connectivity so the flood cannot squeeze through diagonal pinholes.
    labels, count = ndimage.label(floodable)
    border_labels = set(labels[0, :]) | set(labels[-1, :]) | set(labels[:, 0]) | set(labels[:, -1])
    border_labels.discard(0)
    background = np.isin(labels, sorted(border_labels))

    out = arr.copy()
    out[background] = 0

    # 1px defringe: opaque boundary pixels that are themselves background-colored
    opaque = out[:, :, 3] > 0
    neighbor_kernel = np.array([[1, 1, 1], [1, 0, 1], [1, 1, 1]], dtype=np.uint8)
    boundary = opaque & (ndimage.convolve(opaque.astype(np.uint8), neighbor_kernel, mode="constant") < 8)
    fringe = boundary & background_like
    out[fringe] = 0
    return Image.fromarray(out, "RGBA")


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

    # Boards extracted from a Gemini result pack come back at the result's
    # resolution, not the prep geometry — normalize so cell slicing lines up.
    row_count = len(layout)
    col_count = max(len(row) for row in layout)
    expected = (
        PADDING * 2 + col_count * cell_size[0],
        HEADER_HEIGHT + PADDING * 2 + row_count * (cell_size[1] + LABEL_HEIGHT),
    )
    if board.size != expected:
        board = board.resize(expected, Image.Resampling.LANCZOS)

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
