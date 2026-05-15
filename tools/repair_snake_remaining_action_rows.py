#!/usr/bin/env python3
"""Repair snake rows from existing editable Gemini boards.

This pass only extracts already-generated snake art from the saved editable
boards. It applies whole animation rows only when every source frame in that
row survives cleanup as a complete snake body. Fragment rows are recorded as
blocked instead of being silently patched with bad art.
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
from scipy import ndimage

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
SOURCE_ROOT = ROOT / "incoming_sprites" / "gemini_handoff" / "snake"
RUNTIME_ROOT = ROOT / "sprites_runtime" / "snake"
ARTIFACT_ROOT = ROOT / "vnext" / "artifacts" / "snake-remaining-action-source-20260514"
AGES = ("baby", "teen", "adult")
GENDERS = ("female", "male")
TARGET_FAMILIES = {
    "idle": 4,
    "walk": 6,
    "eat": 4,
    "sleep": 2,
    "happy": 4,
    "sad": 2,
    "sick": 4,
    "bathe": 4,
}
FRAME_LAYOUT = [
    ("idle_00", "idle_01", "idle_02", "idle_03", "walk_00"),
    ("walk_01", "walk_02", "walk_03", "walk_04", "walk_05"),
    ("eat_00", "eat_01", "eat_02", "eat_03", "happy_00"),
    ("happy_01", "happy_02", "happy_03", "sad_00", "sad_01"),
    ("sleep_00", "sleep_01", "sick_00", "sick_01", "sick_02"),
    ("sick_03", "bathe_00", "bathe_01", "bathe_02", "bathe_03"),
]
BLOCKED_ROWS = {
    ("adult", "female", "idle"): "editable-board idle row contains grid fragments and one boxed partial source frame",
    ("adult", "female", "walk"): "editable-board walk row contains partial body fragments",
    ("adult", "female", "eat"): "editable-board eat_02 extracts as a partial body fragment",
    ("baby", "female", "sad"): "sad_01 source extracts as a tail fragment",
    ("baby", "female", "sick"): "sick_02 source extracts as a partial body fragment",
    ("teen", "female", "happy"): "happy_00 source extracts as a partial body fragment",
    ("adult", "female", "happy"): "happy_03 source extracts as a partial body fragment",
    ("adult", "female", "sick"): "sick_00 source extracts as a partial body fragment",
    ("adult", "female", "bathe"): "bathe_01 source extracts as a partial body fragment",
    ("adult", "male", "walk"): "editable-board walk row contains partial body fragments",
    ("adult", "male", "sick"): "sick_01 and sick_02 source extracts are line/body fragments",
}
KNOWN_RESIDUALS: dict[tuple[str, str, str], str] = {}


@dataclass(frozen=True)
class FrameRecord:
    age: str
    gender: str
    color: str
    family: str
    frame: str
    source: str
    target: str
    candidate: str
    pre_sha256: str
    post_sha256: str
    changed: bool


@dataclass(frozen=True)
class RowDecision:
    age: str
    gender: str
    family: str
    applied: bool
    reason: str | None
    frames: list[str]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_font(size: int = 13) -> ImageFont.ImageFont:
    for candidate in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def groups(values: np.ndarray, min_len: int = 1) -> list[tuple[int, int]]:
    raw = [int(value) for value in values]
    if not raw:
        return []
    result: list[tuple[int, int]] = []
    start = previous = raw[0]
    for value in raw[1:]:
        if value <= previous + 1:
            previous = value
            continue
        if previous - start + 1 >= min_len:
            result.append((start, previous))
        start = previous = value
    if previous - start + 1 >= min_len:
        result.append((start, previous))
    return result


def find_editable_board_header(image: Image.Image) -> tuple[int, int, int, int]:
    arr = np.asarray(image.convert("RGBA"))
    r = arr[:, :, 0]
    g = arr[:, :, 1]
    b = arr[:, :, 2]
    a = arr[:, :, 3]
    spread = np.maximum.reduce([r, g, b]) - np.minimum.reduce([r, g, b])
    dark_mask = (a > 200) & (r < 100) & (g < 115) & (b < 130) & (spread < 90)
    y_counts = dark_mask.sum(axis=1)
    candidates: list[tuple[int, int, int, int, int]] = []
    y_values = np.where((np.arange(image.height) > image.height * 0.35) & (y_counts > image.width * 0.12))[0]
    for y0, y1 in groups(y_values, min_len=5):
        x_counts = dark_mask[y0 : y1 + 1, :].sum(axis=0)
        x_values = np.where(x_counts > max(2, (y1 - y0 + 1) * 0.45))[0]
        x_groups = groups(x_values, min_len=30)
        if not x_groups:
            continue
        x0, x1 = max(x_groups, key=lambda group: group[1] - group[0])
        width = x1 - x0 + 1
        if width > image.width * 0.50:
            candidates.append((x0, x1, y0, y1, width))
    if not candidates:
        raise ValueError("Could not locate editable snake board header.")
    x0, x1, y0, y1, _ = max(candidates, key=lambda item: item[2])
    return x0, x1, y0, y1


def keep_primary_component(image: Image.Image) -> Image.Image:
    arr = np.array(image.convert("RGBA"))
    labels, count = ndimage.label(arr[:, :, 3] > 0)
    components: list[tuple[int, int, int, int, int, int, int, int, int]] = []
    for index in range(1, count + 1):
        ys, xs = np.where(labels == index)
        if len(xs) == 0:
            continue
        area = len(xs)
        x0 = int(xs.min())
        x1 = int(xs.max())
        y0 = int(ys.min())
        y1 = int(ys.max())
        width = x1 - x0 + 1
        height = y1 - y0 + 1
        if area < 12:
            continue
        if height <= 3 and width >= 12:
            continue
        if width <= 3 and height >= 12:
            continue
        components.append((area, width * height, width, height, x0, x1, y0, y1, index))
    if not components:
        return image
    components.sort(reverse=True)
    keep = labels == components[0][-1]
    out = arr.copy()
    out[:, :, 3] = np.where(keep, arr[:, :, 3], 0).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


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
    cleaned = keep_primary_component(cleaned)
    if cleaned.getchannel("A").getbbox() is None:
        raise ValueError("Extracted frame was empty after cleanup.")
    return trim_to_alpha(cleaned)


def source_board_path(age: str, gender: str) -> Path:
    matches = sorted((SOURCE_ROOT / age / gender / "5-save-edited-board-here").glob("*gemini-result*.png"))
    if not matches:
        raise FileNotFoundError(f"No editable board found for snake/{age}/{gender}")
    return matches[0]


def extract_source_frames(age: str, gender: str) -> dict[str, Image.Image]:
    path = source_board_path(age, gender)
    image = Image.open(path).convert("RGBA")
    x0, x1, _header_top, header_bottom = find_editable_board_header(image)
    grid_top = header_bottom + 1
    grid_bottom = image.height
    col_width = (x1 - x0 + 1) / 5
    row_height = (grid_bottom - grid_top) / 6

    frames: dict[str, Image.Image] = {}
    for row_index, row in enumerate(FRAME_LAYOUT):
        for col_index, frame_id in enumerate(row):
            family = frame_id.rsplit("_", 1)[0]
            if family not in TARGET_FAMILIES:
                continue
            left = round(x0 + col_index * col_width) + 2
            right = round(x0 + (col_index + 1) * col_width) - 2
            top = max(grid_top, round(grid_top + row_index * row_height) - 8)
            bottom = min(image.height, round(grid_top + (row_index + 1.45) * row_height))
            frames[frame_id] = clean_frame_crop(image.crop((left, top, right, bottom)))
    return frames


def source_quality_issue(image: Image.Image, age: str) -> str | None:
    alpha = np.asarray(image.getchannel("A"))
    ys, xs = np.where(alpha > 0)
    if len(xs) == 0:
        return "empty after cleanup"
    area = len(xs)
    width = int(xs.max() - xs.min() + 1)
    height = int(ys.max() - ys.min() + 1)
    min_area = {"baby": 130, "teen": 170, "adult": 220}[age]
    min_width = {"baby": 22, "teen": 26, "adult": 30}[age]
    min_height = {"baby": 14, "teen": 16, "adult": 18}[age]
    if area < min_area:
        return f"too few art pixels ({area} < {min_area})"
    if width < min_width or height < min_height:
        return f"bbox too small ({width}x{height})"
    return None


def to_runtime_frame(source: Image.Image, age: str, gender: str, color: str, frame_id: str) -> Image.Image:
    family = frame_id.rsplit("_", 1)[0]
    return fit_to_canvas(
        colorize(source, color),
        "snake",
        age,
        {},
        PlacementOverride(),
        family,
        gender,
    )


def frame_ids_for_family(family: str) -> list[str]:
    return [f"{family}_{index:02d}" for index in range(TARGET_FAMILIES[family])]


def build_candidates(output_root: Path) -> tuple[list[RowDecision], list[FrameRecord], dict[tuple[str, str, str], Image.Image]]:
    decisions: list[RowDecision] = []
    records: list[FrameRecord] = []
    preview: dict[tuple[str, str, str], Image.Image] = {}
    candidate_root = output_root / "candidate-frames"
    for age in AGES:
        for gender in GENDERS:
            source_frames = extract_source_frames(age, gender)
            for family in TARGET_FAMILIES:
                frame_ids = frame_ids_for_family(family)
                blocked_reason = BLOCKED_ROWS.get((age, gender, family))
                issues: list[str] = []
                for frame_id in frame_ids:
                    source = source_frames.get(frame_id)
                    if source is None:
                        issues.append(f"{frame_id}: missing source")
                        continue
                    issue = source_quality_issue(source, age)
                    if issue:
                        issues.append(f"{frame_id}: {issue}")
                if blocked_reason:
                    issues.append(blocked_reason)
                if issues:
                    decisions.append(RowDecision(age, gender, family, False, "; ".join(issues), frame_ids))
                    continue

                decisions.append(RowDecision(age, gender, family, True, None, frame_ids))
                for frame_id in frame_ids:
                    preview[(age, gender, frame_id)] = to_runtime_frame(source_frames[frame_id], age, gender, "blue", frame_id)
                    for color in COLOR_VARIANTS:
                        candidate = to_runtime_frame(source_frames[frame_id], age, gender, color, frame_id)
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
                                family=family,
                                frame=frame_id,
                                source=str(source_board_path(age, gender).relative_to(ROOT)).replace("\\", "/"),
                                target=str(target.relative_to(ROOT)).replace("\\", "/"),
                                candidate=str(out_path.relative_to(ROOT)).replace("\\", "/"),
                                pre_sha256=sha256(target),
                                post_sha256=sha256(out_path),
                                changed=sha256(target) != sha256(out_path),
                            )
                        )
    return decisions, records, preview


def apply_records(records: list[FrameRecord], output_root: Path) -> Path:
    backup_root = output_root / "backup-before-apply" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    for record in records:
        target = ROOT / record.target
        candidate = ROOT / record.candidate
        if not target.exists():
            raise FileNotFoundError(target)
        expected_canvas, _ = resolve_frame_layout("snake", record.age, record.family)
        with Image.open(candidate) as image:
            if image.size != expected_canvas:
                raise ValueError(f"{candidate} expected canvas {expected_canvas}, got {image.size}")
        backup = backup_root / record.target
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(target, backup)
        shutil.copy2(candidate, target)
        if sha256(target) != record.post_sha256:
            raise ValueError(f"Post-apply hash mismatch for {target}")
    return backup_root


def rollback(records: list[FrameRecord], backup_root: Path) -> None:
    for record in records:
        target = ROOT / record.target
        backup = backup_root / record.target
        shutil.copy2(backup, target)
        if sha256(target) != record.pre_sha256:
            raise ValueError(f"Rollback hash mismatch for {target}")


def reapply(records: list[FrameRecord]) -> None:
    for record in records:
        target = ROOT / record.target
        candidate = ROOT / record.candidate
        shutil.copy2(candidate, target)
        if sha256(target) != record.post_sha256:
            raise ValueError(f"Reapply hash mismatch for {target}")


def write_contact_sheet(
    decisions: list[RowDecision],
    preview: dict[tuple[str, str, str], Image.Image],
    output: Path,
) -> None:
    frame_ids = [frame for family in TARGET_FAMILIES for frame in frame_ids_for_family(family)]
    tile_w, tile_h = 126, 82
    label_w = 148
    width = label_w + len(frame_ids) * tile_w + 10
    height = 42 + len(AGES) * len(GENDERS) * tile_h
    sheet = Image.new("RGB", (width, height), (235, 238, 240))
    draw = ImageDraw.Draw(sheet)
    font = load_font(12)
    draw.text((10, 12), "snake remaining action candidates from editable Gemini boards", fill=(12, 18, 24), font=font)
    blocked = {(decision.age, decision.gender, decision.family): decision.reason for decision in decisions if not decision.applied}
    y = 38
    for age in AGES:
        for gender in GENDERS:
            draw.rectangle((0, y, width, y + tile_h - 3), fill=(245, 247, 249) if y % 2 else (228, 234, 239))
            draw.text((10, y + 28), f"{age}/{gender}", fill=(12, 18, 24), font=font)
            x = label_w
            for frame_id in frame_ids:
                family = frame_id.rsplit("_", 1)[0]
                cell = Image.new("RGBA", (tile_w, tile_h - 18), (248, 248, 248, 255))
                image = preview.get((age, gender, frame_id))
                if image is not None:
                    thumb = image.copy()
                    thumb.thumbnail((tile_w - 8, tile_h - 24), Image.Resampling.NEAREST)
                    cell.alpha_composite(thumb, ((tile_w - thumb.width) // 2, (tile_h - 18 - thumb.height) // 2))
                else:
                    cell_draw = ImageDraw.Draw(cell)
                    reason = blocked.get((age, gender, family), "blocked")
                    cell_draw.rectangle((4, 4, tile_w - 4, tile_h - 22), outline=(164, 70, 70, 255), width=2)
                    cell_draw.text((8, 22), "BLOCKED", fill=(120, 24, 24, 255), font=font)
                    cell_draw.text((8, 38), reason[:18], fill=(120, 24, 24, 255), font=font)
                sheet.paste(cell.convert("RGB"), (x, y))
                draw.text((x + 2, y + tile_h - 17), frame_id, fill=(12, 18, 24), font=font)
                x += tile_w
            y += tile_h
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)


def write_report(
    decisions: list[RowDecision],
    records: list[FrameRecord],
    output_root: Path,
    apply: bool,
    backup_root: Path | None,
    rollback_drill: bool,
) -> None:
    payload: dict[str, Any] = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "species": "snake",
        "scope": "clean complete snake rows from editable Gemini boards",
        "applied": apply,
        "backup_root": str(backup_root.relative_to(ROOT)).replace("\\", "/") if backup_root else None,
        "rollback_drill": rollback_drill,
        "row_decisions": [asdict(decision) for decision in decisions],
        "record_count": len(records),
        "changed_count": sum(1 for record in records if record.changed),
        "records": [asdict(record) for record in records],
        "known_residuals": [
            {"age": age, "gender": gender, "family": family, "reason": reason}
            for (age, gender, family), reason in KNOWN_RESIDUALS.items()
        ],
    }
    output_root.mkdir(parents=True, exist_ok=True)
    (output_root / "snake-remaining-action-apply-report.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")

    lines = [
        "# Snake Editable Board Source Pass",
        "",
        f"- generated_at_utc: `{payload['generated_at_utc']}`",
        "- source: existing `incoming_sprites/gemini_handoff/snake/*/*/5-save-edited-board-here/*gemini-result*.png` editable boards",
        "- mutation policy: apply whole animation rows only when all frames are complete source-grounded snakes",
        f"- applied: `{apply}`",
        f"- changed frames: `{payload['changed_count']}` / `{payload['record_count']}`",
        f"- backup_root: `{payload['backup_root']}`",
        f"- rollback_drill: `{rollback_drill}`",
        "",
        "## Row Decisions",
        "",
    ]
    for decision in decisions:
        status = "applied" if decision.applied else "blocked"
        reason = "" if decision.reason is None else f" - {decision.reason}"
        lines.append(f"- `{decision.age}/{decision.gender}/{decision.family}`: `{status}`{reason}")
    lines.extend(["", "## Known Residuals", ""])
    for item in payload["known_residuals"]:
        lines.append(f"- `snake/{item['age']}/{item['gender']}/{item['family']}`: {item['reason']}")
    lines.extend(["", "## Changed Targets", ""])
    for record in records:
        if record.changed:
            lines.append(f"- `{record.target}` `{record.pre_sha256[:12]}` -> `{record.post_sha256[:12]}`")
    (output_root / "snake-remaining-action-apply-report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", default=str(ARTIFACT_ROOT))
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--rollback-drill", action="store_true")
    args = parser.parse_args()

    output_root = Path(args.output_root).resolve()
    decisions, records, preview = build_candidates(output_root)
    write_contact_sheet(decisions, preview, output_root / "snake-remaining-action-candidate-contact-sheet.png")
    backup_root = apply_records(records, output_root) if args.apply else None
    if args.apply and args.rollback_drill:
        assert backup_root is not None
        rollback(records, backup_root)
        reapply(records)
    write_report(decisions, records, output_root, args.apply, backup_root, args.apply and args.rollback_drill)
    print(
        json.dumps(
            {
                "rows_applied": sum(1 for decision in decisions if decision.applied),
                "rows_blocked": sum(1 for decision in decisions if not decision.applied),
                "records": len(records),
                "changed": sum(1 for record in records if record.changed),
                "applied": args.apply,
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
