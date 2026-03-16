#!/usr/bin/env python3
"""
Compare canonical source poses to runtime sprite outputs and flag likely crop/noise issues.
"""

from __future__ import annotations

import argparse
import json
from dataclasses import asdict
from datetime import datetime
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

from export_species_authoring_pack import AGE_ORDER, fit_sprite, load_font
from generate_runtime_pose_sprites import (
    CANVAS_SIZE,
    DEFAULT_MANIFEST,
    DEFAULT_OUTPUT_ROOT,
    DEFAULT_SOURCE_ROOT,
    CropOverride,
    extract_gender_pose,
    find_pose_components,
    fit_to_canvas,
    load_manifest,
    remove_checkerboard_background,
)


ROOT = Path(__file__).resolve().parents[1]
ARTIFACT_ROOT = ROOT / "vnext" / "artifacts" / "source-runtime-audit"
COLORS = ["red", "orange", "yellow", "blue", "indigo", "violet"]
AGES = ["baby", "teen", "adult"]
GENDERS = ["male", "female"]
RUNTIME_AUDIT_COLOR = "blue"
TILE_SIZE = (180, 140)
PADDING = 20


def checkerboard(size: tuple[int, int], cell: int = 10) -> Image.Image:
    image = Image.new("RGBA", size, (238, 238, 238, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            fill = (246, 246, 246, 255) if ((x // cell) + (y // cell)) % 2 == 0 else (226, 226, 226, 255)
            draw.rectangle((x, y, min(size[0], x + cell), min(size[1], y + cell)), fill=fill)
    return image


def alpha_mask(image: Image.Image) -> np.ndarray:
    array = np.asarray(image.convert("RGBA"))
    return array[:, :, 3] > 0


def bbox_from_mask(mask: np.ndarray) -> list[int] | None:
    if not np.any(mask):
        return None
    ys, xs = np.nonzero(mask)
    return [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]


def component_areas(mask: np.ndarray) -> list[int]:
    if not np.any(mask):
        return []
    labels, count = ndimage.label(mask.astype(np.uint8))
    if count == 0:
        return []
    return sorted(
        [int(np.sum(labels == index)) for index in range(1, count + 1)],
        reverse=True,
    )


def count_tiny_components(mask: np.ndarray, max_pixels: int = 6) -> int:
    return sum(1 for area in component_areas(mask) if area <= max_pixels)


def bbox_touches_edge(bbox: list[int] | None, size: tuple[int, int], margin: int = 0) -> bool:
    if bbox is None:
        return False
    left, top, right, bottom = bbox
    width, height = size
    return (
        left <= margin
        or top <= margin
        or right >= width - 1 - margin
        or bottom >= height - 1 - margin
    )


def load_source_pose(entry: Any, gender: str, source_root: Path) -> Image.Image:
    source_path = source_root / entry.source
    image = Image.open(source_path).convert("RGBA")
    isolated = remove_checkerboard_background(image)
    components = find_pose_components(isolated)
    mapping = dict(zip(entry.component_order, components, strict=True))
    bbox = mapping[gender]
    crop_override = entry.component_crop_overrides.get(gender, entry.crop_override)
    return extract_gender_pose(
        isolated,
        bbox,
        crop_override if hasattr(entry, "crop_override") else CropOverride(),
        entry.species,
    )


def normalize_source_pose(entry: Any, gender: str, sprite: Image.Image) -> Image.Image:
    placement = getattr(entry, "placement_override", None)
    return fit_to_canvas(
        sprite,
        entry.species,
        entry.age_stage,
        {},
        placement,
        None,
        gender,
    )


def load_runtime_frame(
    runtime_root: Path, species: str, age_stage: str, gender: str, color: str, frame_name: str
) -> Image.Image | None:
    path = runtime_root / species / age_stage / gender / color / f"{frame_name}.png"
    if not path.exists():
        return None
    return Image.open(path).convert("RGBA")


def metric_block(image: Image.Image | None) -> dict[str, Any]:
    if image is None:
        return {
            "present": False,
            "bbox": None,
            "area": 0,
            "component_count": 0,
            "tiny_component_count": 0,
            "touches_edge": False,
            "size": None,
        }
    mask = alpha_mask(image)
    bbox = bbox_from_mask(mask)
    areas = component_areas(mask)
    return {
        "present": True,
        "bbox": bbox,
        "area": int(np.count_nonzero(mask)),
        "component_count": len(areas),
        "tiny_component_count": count_tiny_components(mask),
        "touches_edge": bbox_touches_edge(bbox, image.size),
        "size": list(image.size),
    }


def compare_metrics(source_norm: dict[str, Any], runtime: dict[str, Any], allow_wide: bool = False) -> dict[str, Any]:
    flags: list[str] = []
    if not runtime["present"]:
        flags.append("missing")
        return {"flags": flags, "severity": "critical"}

    if runtime["area"] == 0:
        flags.append("empty")
    else:
        source_area = max(1, source_norm["area"])
        area_ratio = runtime["area"] / source_area
        if area_ratio < 0.42:
            flags.append("too-small")
        elif area_ratio > 1.85 and not allow_wide:
            flags.append("too-large")

    if runtime["component_count"] > 4:
        flags.append("fragmented")
    if runtime["tiny_component_count"] > 2:
        flags.append("halo-noise")
    if runtime["touches_edge"] and not allow_wide:
        flags.append("edge-touch")

    severity = "ok"
    if any(flag in flags for flag in ("missing", "empty", "too-small", "fragmented", "edge-touch")):
        severity = "warning"
    if any(flag in flags for flag in ("missing", "empty")):
        severity = "critical"
    return {"flags": flags, "severity": severity}


def render_tile(label: str, image: Image.Image | None) -> Image.Image:
    tile = Image.new("RGBA", TILE_SIZE, (246, 246, 246, 255))
    draw = ImageDraw.Draw(tile)
    draw.rectangle((0, 0, TILE_SIZE[0] - 1, TILE_SIZE[1] - 1), outline=(80, 80, 80, 255), width=1)
    font = load_font(15)
    draw.text((10, 8), label, fill=(30, 30, 30, 255), font=font)
    preview_rect = (10, 34, TILE_SIZE[0] - 10, TILE_SIZE[1] - 10)
    preview = checkerboard((preview_rect[2] - preview_rect[0], preview_rect[3] - preview_rect[1]))
    tile.alpha_composite(preview, (preview_rect[0], preview_rect[1]))
    draw.rectangle(preview_rect, outline=(190, 190, 190, 255), width=1)
    if image is not None:
        preview = fit_sprite(image, (preview_rect[2] - preview_rect[0], preview_rect[3] - preview_rect[1]))
        x = preview_rect[0] + ((preview_rect[2] - preview_rect[0]) - preview.width) // 2
        y = preview_rect[1] + ((preview_rect[3] - preview_rect[1]) - preview.height) // 2
        tile.alpha_composite(preview, (x, y))
    return tile


def render_variant_row(
    species: str,
    age_stage: str,
    gender: str,
    source_pose: Image.Image,
    source_norm: Image.Image,
    idle_frame: Image.Image | None,
    walk_frame: Image.Image | None,
    flags: list[str],
) -> Image.Image:
    label_width = 220
    tile_gap = 14
    row_width = label_width + 4 * TILE_SIZE[0] + 3 * tile_gap
    row_height = TILE_SIZE[1]
    row = Image.new("RGBA", (row_width, row_height), (255, 255, 255, 255))
    draw = ImageDraw.Draw(row)
    header_font = load_font(18)
    text_font = load_font(14)
    draw.rectangle((0, 0, row_width - 1, row_height - 1), outline=(90, 90, 90, 255), width=1)
    draw.text((16, 16), f"{species} / {age_stage} / {gender}", fill=(24, 24, 24, 255), font=header_font)
    flag_text = ", ".join(flags) if flags else "ok"
    flag_fill = (154, 43, 43, 255) if flags else (38, 114, 55, 255)
    draw.text((16, 50), f"flags: {flag_text}", fill=flag_fill, font=text_font)
    x = label_width
    for label, image in [
        ("source", source_pose),
        ("source-norm", source_norm),
        ("idle_00", idle_frame),
        ("walk_00", walk_frame),
    ]:
        row.alpha_composite(render_tile(label, image), (x, 0))
        x += TILE_SIZE[0] + tile_gap
    return row


def compose_board(rows: list[Image.Image], title: str) -> Image.Image:
    if not rows:
        return Image.new("RGBA", (100, 100), (255, 255, 255, 255))
    width = max(row.width for row in rows) + PADDING * 2
    header_height = 72
    height = header_height + len(rows) * (rows[0].height + 10) + PADDING
    board = Image.new("RGBA", (width, height), (245, 245, 245, 255))
    draw = ImageDraw.Draw(board)
    title_font = load_font(28)
    subtitle_font = load_font(16)
    draw.text((PADDING, 16), title, fill=(30, 30, 30, 255), font=title_font)
    draw.text(
        (PADDING, 48),
        "Source pose vs normalized source vs runtime idle/walk. Flags highlight likely crop or noise issues.",
        fill=(72, 72, 72, 255),
        font=subtitle_font,
    )
    y = header_height
    for row in rows:
        board.alpha_composite(row, (PADDING, y))
        y += row.height + 10
    return board


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--species", nargs="*", default=None)
    parser.add_argument("--age-stages", nargs="*", default=None)
    parser.add_argument("--genders", nargs="*", default=None)
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    species_filter = set(args.species or [])
    age_filter = set(args.age_stages or [])
    gender_filter = set(args.genders or GENDERS)
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output_root = ARTIFACT_ROOT / timestamp
    output_root.mkdir(parents=True, exist_ok=True)

    manifest_entries = sorted(
        [
            entry
            for entry in manifest
            if (not species_filter or entry.species in species_filter)
            and (not age_filter or entry.age_stage in age_filter)
        ],
        key=lambda entry: (entry.species, AGE_ORDER[entry.age_stage]),
    )

    rows: list[Image.Image] = []
    results: list[dict[str, Any]] = []
    for entry in manifest_entries:
        for gender in GENDERS:
            if gender not in gender_filter:
                continue
            source_pose = load_source_pose(entry, gender, args.source_root)
            source_norm = normalize_source_pose(entry, gender, source_pose)
            idle_frame = load_runtime_frame(
                args.runtime_root, entry.species, entry.age_stage, gender, RUNTIME_AUDIT_COLOR, "idle_00"
            )
            walk_frame = load_runtime_frame(
                args.runtime_root, entry.species, entry.age_stage, gender, RUNTIME_AUDIT_COLOR, "walk_00"
            )
            source_metrics = metric_block(source_norm)
            idle_metrics = metric_block(idle_frame)
            walk_metrics = metric_block(walk_frame)
            idle_eval = compare_metrics(source_metrics, idle_metrics)
            walk_eval = compare_metrics(source_metrics, walk_metrics, allow_wide=(entry.species == "snake"))
            flags = sorted(set(idle_eval["flags"] + [f"walk:{flag}" for flag in walk_eval["flags"]]))
            row = render_variant_row(
                entry.species,
                entry.age_stage,
                gender,
                source_pose,
                source_norm,
                idle_frame,
                walk_frame,
                flags,
            )
            rows.append(row)
            results.append(
                {
                    "species": entry.species,
                    "age_stage": entry.age_stage,
                    "gender": gender,
                    "source": entry.source,
                    "source_metrics": source_metrics,
                    "idle_metrics": idle_metrics,
                    "walk_metrics": walk_metrics,
                    "flags": flags,
                }
            )

    board = compose_board(rows, "Wevito Source-To-Runtime Sprite Audit")
    board_path = output_root / "source-runtime-audit-board.png"
    board.save(board_path)

    summary = {
        "captured_at": datetime.now().isoformat(timespec="seconds"),
        "runtime_root": str(args.runtime_root),
        "source_root": str(args.source_root),
        "board": str(board_path),
        "flagged_variants": sum(1 for result in results if result["flags"]),
        "results": results,
    }
    summary_path = output_root / "summary.json"
    summary_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(summary_path)


if __name__ == "__main__":
    main()
