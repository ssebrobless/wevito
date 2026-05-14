#!/usr/bin/env python3
"""Repair snake eat/sleep runtime rows from dedicated Gemini care boards.

The source boards already contain generated snake care poses. This script only
extracts, cleans, colorizes, backs up, and applies those source-grounded frames.
It does not synthesize new snake art.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image, ImageDraw, ImageFont

from generate_runtime_pose_sprites import (
    COLOR_VARIANTS,
    PlacementOverride,
    clear_bright_edge_matte,
    clear_lower_background_islands,
    clear_small_background_like_islands,
    clear_species_artifacts,
    clear_transparency_connected_matte,
    colorize,
    fit_to_canvas,
    remove_checkerboard_background,
    remove_small_components,
    resolve_frame_layout,
    scrub_border_palette_matte,
    scrub_neutral_edge_matte,
    strip_palette_like_noise,
    trim_to_alpha,
)


ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = ROOT / "incoming_sprites" / "gemini_handoff_motion" / "snake"
RUNTIME_ROOT = ROOT / "sprites_runtime" / "snake"
ARTIFACT_ROOT = ROOT / "vnext" / "artifacts" / "snake-care-action-source-quality-20260514"
AGES = ("baby", "teen", "adult")
GENDERS = ("female", "male")
TARGET_FAMILIES = ("eat", "sleep")
BLOCKED_FRAMES = {
    # The dedicated adult/female care source labels these as sleep, but the
    # visual pose is upright/alert rather than resting or coiled.
    ("adult", "female", "sleep_00"): "source pose is not visually sleeping",
    ("adult", "female", "sleep_01"): "source pose is not visually sleeping",
}


@dataclass(frozen=True)
class FrameRecord:
    age: str
    gender: str
    color: str
    frame: str
    source: str
    target: str
    candidate: str
    pre_sha256: str
    post_sha256: str
    changed: bool


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_font(size: int = 14) -> ImageFont.ImageFont:
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default()


def line_mask(image: Image.Image) -> np.ndarray:
    arr = np.asarray(image.convert("RGBA"))
    r = arr[:, :, 0].astype(np.int16)
    g = arr[:, :, 1].astype(np.int16)
    b = arr[:, :, 2].astype(np.int16)
    a = arr[:, :, 3]
    bright = (r + g + b) / 3
    spread = np.maximum.reduce([r, g, b]) - np.minimum.reduce([r, g, b])
    purple = (b > 145) & (r > 90) & (g < 175) & ((b - g) > 15)
    gray = (bright > 70) & (bright < 170) & (spread < 38)
    return (a > 200) & (purple | gray)


def group_centers(values: np.ndarray) -> list[int]:
    raw = [int(value) for value in values]
    if not raw:
        return []
    groups: list[tuple[int, int]] = []
    start = previous = raw[0]
    for value in raw[1:]:
        if value == previous + 1:
            previous = value
            continue
        groups.append((start, previous))
        start = previous = value
    groups.append((start, previous))
    return [(start + end) // 2 for start, end in groups]


def detect_grid(image: Image.Image) -> tuple[list[int], list[int]]:
    mask = line_mask(image)
    x_counts = mask[55:, :].sum(axis=0)
    y_counts = mask.sum(axis=1)
    x_centers = group_centers(np.where(x_counts > 250)[0])
    y_centers = group_centers(np.where(y_counts > 250)[0])
    if len(x_centers) < 6 or len(y_centers) < 4:
        raise ValueError(f"Could not detect care board grid: x={x_centers}, y={y_centers}")

    x_lines = sorted(x_centers[:6])
    y0 = y_centers[0]
    row0_bottom = max(center for center in y_centers if center < image.height * 0.52)
    row1_top = min(center for center in y_centers if center > row0_bottom + 12)
    row1_bottom = y_centers[-1]
    return x_lines, [y0, row0_bottom, row1_top, row1_bottom]


def clean_frame_crop(crop: Image.Image) -> Image.Image:
    cleaned = remove_checkerboard_background(crop.convert("RGBA"))
    cleaned = scrub_border_palette_matte(cleaned)
    cleaned = scrub_neutral_edge_matte(cleaned)
    cleaned = strip_palette_like_noise(cleaned)
    cleaned = clear_bright_edge_matte(cleaned)
    cleaned = clear_transparency_connected_matte(cleaned)
    cleaned = clear_small_background_like_islands(cleaned)
    cleaned = clear_lower_background_islands(cleaned)
    cleaned = clear_species_artifacts(cleaned, "snake")
    cleaned = remove_small_components(cleaned, 5)
    if cleaned.getchannel("A").getbbox() is None:
        raise ValueError("Extracted care frame was empty after cleanup.")
    return trim_to_alpha(cleaned)


def load_metadata(care_dir: Path) -> dict[str, Any]:
    return json.loads((care_dir / "pack-metadata.json").read_text(encoding="utf-8"))


def source_frames(age: str, gender: str) -> dict[str, Image.Image]:
    care_dir = SOURCE_ROOT / age / gender / "care"
    metadata = load_metadata(care_dir)
    source_path = care_dir / metadata["runtimeReferenceImage"]
    image = Image.open(source_path).convert("RGBA")
    x_lines, y_lines = detect_grid(image)
    rows = [(y_lines[0], y_lines[1]), (y_lines[2], y_lines[3])]

    frames: dict[str, Image.Image] = {}
    for row_index, row in enumerate(metadata["frameLayout"]):
        top, bottom = rows[row_index]
        for col_index, frame_id in enumerate(row):
            if frame_id is None:
                continue
            if not frame_id.startswith(TARGET_FAMILIES):
                continue
            if (age, gender, frame_id) in BLOCKED_FRAMES:
                continue
            left = x_lines[col_index]
            right = x_lines[col_index + 1]
            crop = image.crop((left + 4, top + 4, right - 4, bottom - 4))
            frames[frame_id] = clean_frame_crop(crop)
    return frames


def to_runtime_frame(source: Image.Image, age: str, gender: str, color: str, frame_id: str) -> Image.Image:
    family = frame_id.rsplit("_", 1)[0]
    tinted = colorize(source, color)
    return fit_to_canvas(
        tinted,
        "snake",
        age,
        {},
        PlacementOverride(),
        family,
        gender,
    )


def write_contact_sheet(frames: dict[tuple[str, str, str], Image.Image], output: Path) -> None:
    tile_w, tile_h = 128, 88
    label_w = 190
    keys = sorted(frames)
    width = label_w + len(TARGET_FAMILIES) * 4 * tile_w + 32
    height = 40 + len(AGES) * len(GENDERS) * tile_h
    sheet = Image.new("RGB", (width, height), (232, 238, 242))
    draw = ImageDraw.Draw(sheet)
    font = load_font(13)
    draw.text((12, 12), "snake care/action candidates from dedicated source boards", fill=(20, 28, 35), font=font)
    y = 36
    for age in AGES:
        for gender in GENDERS:
            draw.rectangle((0, y, width, y + tile_h - 4), fill=(222, 230, 236) if (y // tile_h) % 2 else (212, 222, 230))
            draw.text((10, y + 32), f"{age}/{gender}/blue", fill=(20, 28, 35), font=font)
            x = label_w
            for frame_id in ("eat_00", "eat_01", "eat_02", "eat_03", "sleep_00", "sleep_01"):
                canvas = Image.new("RGBA", (tile_w, tile_h - 8), (246, 246, 246, 255))
                frame = frames.get((age, gender, frame_id))
                if frame is None:
                    blocked = BLOCKED_FRAMES.get((age, gender, frame_id), "blocked")
                    block_draw = ImageDraw.Draw(canvas)
                    block_draw.rectangle((4, 4, tile_w - 4, tile_h - 14), outline=(168, 78, 78, 255), width=2)
                    block_draw.text((10, 24), "BLOCKED", fill=(126, 24, 24, 255), font=font)
                    block_draw.text((10, 40), blocked[:18], fill=(126, 24, 24, 255), font=font)
                else:
                    thumb = frame.copy()
                    thumb.thumbnail((tile_w - 12, tile_h - 22), Image.Resampling.NEAREST)
                    canvas.alpha_composite(thumb, ((tile_w - thumb.width) // 2, (tile_h - 12 - thumb.height) // 2))
                sheet.paste(canvas.convert("RGB"), (x, y + 4))
                draw.text((x + 4, y + tile_h - 18), frame_id, fill=(20, 28, 35), font=font)
                x += tile_w
            y += tile_h
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)


def build_candidates(output_root: Path) -> tuple[list[FrameRecord], dict[tuple[str, str, str], Image.Image]]:
    candidate_root = output_root / "candidate-frames"
    source_preview: dict[tuple[str, str, str], Image.Image] = {}
    records: list[FrameRecord] = []
    for age in AGES:
        for gender in GENDERS:
            extracted = source_frames(age, gender)
            for frame_id, source in extracted.items():
                source_preview[(age, gender, frame_id)] = to_runtime_frame(source, age, gender, "blue", frame_id)
                for color in COLOR_VARIANTS:
                    candidate = to_runtime_frame(source, age, gender, color, frame_id)
                    out_dir = candidate_root / age / gender / color
                    out_dir.mkdir(parents=True, exist_ok=True)
                    out_path = out_dir / f"{frame_id}.png"
                    candidate.save(out_path)
                    target = RUNTIME_ROOT / age / gender / color / f"{frame_id}.png"
                    records.append(
                        FrameRecord(
                            age=age,
                            gender=gender,
                            color=color,
                            frame=frame_id,
                            source=str((SOURCE_ROOT / age / gender / "care" / "3-runtime-reference-blue.png").relative_to(ROOT)).replace("\\", "/"),
                            target=str(target.relative_to(ROOT)).replace("\\", "/"),
                            candidate=str(out_path.relative_to(ROOT)).replace("\\", "/"),
                            pre_sha256=sha256(target),
                            post_sha256=sha256(out_path),
                            changed=sha256(target) != sha256(out_path),
                        )
                    )
    return records, source_preview


def apply_records(records: list[FrameRecord], output_root: Path) -> Path:
    backup_root = output_root / "backup-before-apply" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    for record in records:
        target = ROOT / record.target
        candidate = ROOT / record.candidate
        if not target.exists():
            raise FileNotFoundError(target)
        backup = backup_root / record.target
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(target, backup)
        expected_canvas, _ = resolve_frame_layout("snake", record.age, record.frame.rsplit("_", 1)[0])
        with Image.open(candidate) as image:
            if image.size != expected_canvas:
                raise ValueError(f"{candidate} expected canvas {expected_canvas}, got {image.size}")
        shutil.copy2(candidate, target)
        if sha256(target) != record.post_sha256:
            raise ValueError(f"Post-apply hash mismatch for {target}")
    return backup_root


def apply_records_without_backup(records: list[FrameRecord]) -> None:
    for record in records:
        target = ROOT / record.target
        candidate = ROOT / record.candidate
        shutil.copy2(candidate, target)
        if sha256(target) != record.post_sha256:
            raise ValueError(f"Post-reapply hash mismatch for {target}")


def rollback(records: list[FrameRecord], backup_root: Path) -> None:
    for record in records:
        backup = backup_root / record.target
        target = ROOT / record.target
        shutil.copy2(backup, target)
        if sha256(target) != record.pre_sha256:
            raise ValueError(f"Rollback hash mismatch for {target}")


def write_report(records: list[FrameRecord], output_root: Path, apply: bool, backup_root: Path | None, rollback_drill: bool) -> None:
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "species": "snake",
        "scope": "care/action source-quality pass for eat/sleep only",
        "applied": apply,
        "backup_root": str(backup_root.relative_to(ROOT)).replace("\\", "/") if backup_root else None,
        "rollback_drill": rollback_drill,
        "record_count": len(records),
        "changed_count": sum(1 for record in records if record.changed),
        "records": [asdict(record) for record in records],
    }
    output_root.mkdir(parents=True, exist_ok=True)
    (output_root / "snake-care-action-apply-report.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")

    lines = [
        "# Snake Care/Action Source Quality Pass",
        "",
        f"- generated_at_utc: `{payload['generated_at_utc']}`",
        "- scope: `snake` eat/sleep rows from dedicated Gemini care boards",
        f"- applied: `{apply}`",
        f"- changed frames: `{payload['changed_count']}` / `{payload['record_count']}`",
        f"- backup_root: `{payload['backup_root']}`",
        f"- rollback_drill: `{rollback_drill}`",
        "",
        "## Row Decision",
        "",
        "- `eat_00..03` and `sleep_00..01` have dedicated source boards for every snake age/gender and were safe to process.",
        "- `adult/female/sleep_00..01` was intentionally skipped because the source board pose is not visually sleeping.",
        "- `happy`, `sad`, `sick`, and `bathe` do not have clean dedicated source rows in this packet; they remain blocked for a separate source/art pass.",
        "",
        "## Changed Targets",
        "",
    ]
    for record in records:
        if record.changed:
            lines.append(f"- `{record.target}` `{record.pre_sha256[:12]}` -> `{record.post_sha256[:12]}`")
    (output_root / "snake-care-action-apply-report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", default=str(ARTIFACT_ROOT))
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--rollback-drill", action="store_true")
    args = parser.parse_args()

    output_root = Path(args.output_root).resolve()
    records, source_preview = build_candidates(output_root)
    write_contact_sheet(source_preview, output_root / "snake-care-action-candidate-contact-sheet.png")
    backup_root = apply_records(records, output_root) if args.apply else None
    if args.apply and args.rollback_drill:
        assert backup_root is not None
        rollback(records, backup_root)
        apply_records_without_backup(records)
    write_report(records, output_root, args.apply, backup_root, args.apply and args.rollback_drill)
    print(json.dumps({"records": len(records), "changed": sum(1 for record in records if record.changed), "applied": args.apply}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
