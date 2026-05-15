#!/usr/bin/env python3
"""Import the Gemini snake clean-source board into guarded runtime rows.

The input image is a generated source board supplied by the user. This script
extracts the blue snake bodies, creates candidate runtime frames, and can apply
accepted whole rows across all six runtime colors with backup/provenance.
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
    colorize,
    fit_to_canvas,
    remove_small_components,
    trim_to_alpha,
)


ROOT = Path(__file__).resolve().parents[1]
RUNTIME_ROOT = ROOT / "sprites_runtime" / "snake"
SOURCE_PACKET_ROOT = ROOT / "incoming_sprites" / "gemini_handoff" / "snake" / "clean-source-20260514"
ARTIFACT_ROOT = ROOT / "vnext" / "artifacts" / "snake-gemini-clean-source-apply-20260514"

ROWS: list[dict[str, Any]] = [
    {
        "age": "baby",
        "gender": "female",
        "family": "sad",
        "frames": ["sad_00", "sad_01"],
        "boxes": [(81, 126, 174, 212), (317, 120, 408, 212)],
    },
    {
        "age": "baby",
        "gender": "female",
        "family": "sick",
        "frames": ["sick_00", "sick_01", "sick_02", "sick_03"],
        "boxes": [(81, 380, 174, 466), (313, 382, 408, 466), (551, 385, 647, 465), (780, 382, 882, 465)],
    },
    {
        "age": "teen",
        "gender": "female",
        "family": "happy",
        "frames": ["happy_00", "happy_01", "happy_02", "happy_03"],
        "boxes": [(70, 593, 189, 725), (296, 584, 423, 725), (529, 579, 655, 725), (765, 579, 887, 725)],
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "idle",
        "frames": ["idle_00", "idle_01", "idle_02", "idle_03"],
        "boxes": [(42, 838, 216, 979), (277, 838, 450, 979), (510, 838, 686, 979), (740, 838, 905, 979)],
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "walk",
        "frames": ["walk_00", "walk_01", "walk_02", "walk_03", "walk_04", "walk_05"],
        "boxes": [(35, 1142, 219, 1232), (266, 1118, 455, 1230), (499, 1104, 694, 1232), (732, 1100, 929, 1232), (967, 1096, 1165, 1232), (1216, 1133, 1397, 1232)],
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "eat",
        "frames": ["eat_00", "eat_01", "eat_02", "eat_03"],
        "boxes": [(1914, 332, 2019, 470), (2148, 333, 2256, 469), (2382, 321, 2497, 469), (2623, 321, 2751, 468)],
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "happy",
        "frames": ["happy_00", "happy_01", "happy_02", "happy_03"],
        "boxes": [(1853, 596, 2034, 721), (2102, 592, 2277, 721), (2348, 583, 2508, 725), (2586, 584, 2741, 725)],
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "sick",
        "frames": ["sick_00", "sick_01", "sick_02", "sick_03"],
        "boxes": [(1856, 836, 2024, 979), (2112, 836, 2256, 979), (2349, 852, 2495, 979), (2587, 855, 2770, 979)],
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "bathe",
        "frames": ["bathe_00", "bathe_01", "bathe_02", "bathe_03"],
        "boxes": [(1855, 1096, 1989, 1233), (2104, 1093, 2234, 1233), (2346, 1081, 2457, 1234), (2584, 1083, 2718, 1232)],
    },
    {
        "age": "adult",
        "gender": "male",
        "family": "walk",
        "frames": ["walk_00", "walk_01", "walk_02", "walk_03", "walk_04", "walk_05"],
        "boxes": [(43, 1357, 231, 1488), (268, 1356, 467, 1488), (498, 1344, 693, 1488), (746, 1352, 933, 1488), (968, 1342, 1166, 1488), (1217, 1361, 1397, 1488)],
    },
    {
        "age": "adult",
        "gender": "male",
        "family": "sick",
        "frames": ["sick_00", "sick_01", "sick_02", "sick_03"],
        "boxes": [(1859, 1352, 2021, 1488), (2105, 1341, 2264, 1488), (2347, 1339, 2506, 1488), (2584, 1338, 2772, 1488)],
    },
]


@dataclass(frozen=True)
class FrameRecord:
    age: str
    gender: str
    color: str
    family: str
    frame: str
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


def rel(path: Path) -> str:
    absolute = path if path.is_absolute() else (ROOT / path)
    return absolute.resolve().relative_to(ROOT.resolve()).as_posix()


def load_font(size: int = 13) -> ImageFont.ImageFont:
    for candidate in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def body_mask(crop: Image.Image) -> np.ndarray:
    arr = np.asarray(crop.convert("RGBA"))
    r = arr[:, :, 0].astype(np.int16)
    g = arr[:, :, 1].astype(np.int16)
    b = arr[:, :, 2].astype(np.int16)
    a = arr[:, :, 3]
    luma = (r + g + b) / 3
    spread = np.maximum.reduce([r, g, b]) - np.minimum.reduce([r, g, b])

    blue_seed = (a > 128) & (b > 110) & (g > 60) & (r < 135) & ((b - r) > 30) & ((b - g) > 5)
    near_seed = ndimage.binary_dilation(blue_seed, iterations=7)
    dark_outline = (a > 128) & (luma < 92)
    blue_body = (a > 128) & (b > 85) & (g > 45) & (r < 150) & ((b - r) > 15)
    attached_highlight = (a > 128) & (luma > 135) & (spread < 95)
    candidate = near_seed & (blue_body | dark_outline | attached_highlight)

    labels, count = ndimage.label(candidate)
    keep = np.zeros(candidate.shape, dtype=bool)
    for index in range(1, count + 1):
        component = labels == index
        if np.any(component & blue_seed):
            keep |= component
    return keep


def isolate_snake(source: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    pad = 10
    left, top, right, bottom = box
    crop = source.crop(
        (
            max(0, left - pad),
            max(0, top - pad),
            min(source.width, right + pad),
            min(source.height, bottom + pad),
        )
    ).convert("RGBA")
    mask = body_mask(crop)
    arr = np.asarray(crop).copy()
    arr[:, :, 3] = np.where(mask, arr[:, :, 3], 0).astype(np.uint8)
    isolated = trim_to_alpha(Image.fromarray(arr, "RGBA"))
    isolated = remove_small_components(isolated, 8)
    if isolated.getchannel("A").getbbox() is None:
        raise ValueError(f"empty extraction for box {box}")
    return trim_to_alpha(isolated)


def quality_issue(image: Image.Image, age: str) -> str | None:
    alpha = np.asarray(image.getchannel("A"))
    ys, xs = np.where(alpha > 0)
    if len(xs) == 0:
        return "empty"
    area = len(xs)
    width = int(xs.max() - xs.min() + 1)
    height = int(ys.max() - ys.min() + 1)
    min_area = {"baby": 220, "teen": 520, "adult": 620}[age]
    min_width = {"baby": 28, "teen": 45, "adult": 55}[age]
    min_height = {"baby": 28, "teen": 50, "adult": 48}[age]
    labels, count = ndimage.label(alpha > 0)
    large_components = 0
    for index in range(1, count + 1):
        if int((labels == index).sum()) >= 24:
            large_components += 1
    if area < min_area:
        return f"too few art pixels ({area} < {min_area})"
    if width < min_width:
        return f"bbox width too small ({width} < {min_width})"
    if height < min_height:
        return f"bbox height too small ({height} < {min_height})"
    if large_components > 3:
        return f"too many large components ({large_components})"
    return None


def runtime_frame(source: Image.Image, age: str, gender: str, color: str, family: str) -> Image.Image:
    tinted = colorize(source, color)
    return fit_to_canvas(tinted, "snake", age, {}, PlacementOverride(), family, gender)


def write_contact_sheet(candidates: dict[tuple[str, str, str, str], Image.Image], issues: dict[tuple[str, str, str], str], output: Path) -> None:
    tile_w, tile_h = 96, 82
    label_w = 210
    rows = [(row["age"], row["gender"], row["family"], row["frames"]) for row in ROWS]
    width = label_w + max(len(frames) for _, _, _, frames in rows) * tile_w + 260
    height = 44 + len(rows) * tile_h
    sheet = Image.new("RGBA", (width, height), (232, 238, 242, 255))
    draw = ImageDraw.Draw(sheet)
    title = load_font(15)
    small = load_font(11)
    draw.rectangle((0, 0, width, 36), fill=(31, 40, 50, 255))
    draw.text((12, 9), "snake Gemini clean-source candidates", fill=(245, 248, 252), font=title)
    y = 42
    for row_index, (age, gender, family, frames) in enumerate(rows):
        fill = (220, 229, 236, 255) if row_index % 2 else (210, 221, 230, 255)
        draw.rectangle((0, y - 3, width, y + tile_h - 6), fill=fill)
        draw.text((10, y + 8), f"{age}/{gender}/{family}", fill=(20, 28, 35), font=small)
        row_issue = "; ".join(f"{frame}: {issues[(age, gender, frame)]}" for frame in frames if (age, gender, frame) in issues)
        if row_issue:
            draw.text((10, y + 26), "BLOCKED", fill=(148, 34, 34), font=small)
            draw.text((10, y + 42), row_issue[:32], fill=(148, 34, 34), font=small)
        else:
            draw.text((10, y + 26), "candidate OK", fill=(28, 92, 58), font=small)
        x = label_w
        for frame in frames:
            canvas = Image.new("RGBA", (tile_w, tile_h - 8), (246, 246, 246, 255))
            image = candidates.get((age, gender, family, frame))
            if image is not None:
                thumb = image.copy()
                thumb.thumbnail((tile_w - 10, tile_h - 24), Image.Resampling.NEAREST)
                canvas.alpha_composite(thumb, ((tile_w - thumb.width) // 2, 4 + (tile_h - 24 - thumb.height) // 2))
            ImageDraw.Draw(canvas).text((4, tile_h - 21), frame, fill=(20, 28, 35), font=small)
            sheet.alpha_composite(canvas, (x, y))
            x += tile_w
        y += tile_h
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(output)


def copy_source(input_path: Path) -> Path:
    SOURCE_PACKET_ROOT.mkdir(parents=True, exist_ok=True)
    target = SOURCE_PACKET_ROOT / "snake-clean-source-gemini-20260514.png"
    shutil.copy2(input_path, target)
    return target


def build_candidates(source_path: Path, output_root: Path) -> tuple[dict[tuple[str, str, str, str], Image.Image], dict[tuple[str, str, str], str]]:
    source = Image.open(source_path).convert("RGBA")
    candidates: dict[tuple[str, str, str, str], Image.Image] = {}
    issues: dict[tuple[str, str, str], str] = {}
    for row in ROWS:
        age = row["age"]
        gender = row["gender"]
        family = row["family"]
        for frame, box in zip(row["frames"], row["boxes"], strict=True):
            extracted = isolate_snake(source, box)
            issue = quality_issue(extracted, age)
            if issue:
                issues[(age, gender, frame)] = issue
            for color in COLOR_VARIANTS:
                frame_image = runtime_frame(extracted, age, gender, color, family)
                if color == "blue":
                    candidates[(age, gender, family, frame)] = frame_image
                candidate_path = output_root / "candidate-frames" / age / gender / color / f"{frame}.png"
                candidate_path.parent.mkdir(parents=True, exist_ok=True)
                frame_image.save(candidate_path)
    return candidates, issues


def backup_targets(output_root: Path) -> Path:
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    backup_root = output_root / "backup-before-apply" / stamp
    for row in ROWS:
        age = row["age"]
        gender = row["gender"]
        for color in COLOR_VARIANTS:
            target_dir = RUNTIME_ROOT / age / gender / color
            for frame in row["frames"]:
                source = target_dir / f"{frame}.png"
                target = backup_root / rel(source)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
    return backup_root


def apply_candidates(output_root: Path) -> tuple[list[FrameRecord], Path]:
    backup_root = backup_targets(output_root)
    records: list[FrameRecord] = []
    for row in ROWS:
        age = row["age"]
        gender = row["gender"]
        family = row["family"]
        for color in COLOR_VARIANTS:
            for frame in row["frames"]:
                target = RUNTIME_ROOT / age / gender / color / f"{frame}.png"
                candidate = output_root / "candidate-frames" / age / gender / color / f"{frame}.png"
                pre = sha256(target)
                post = sha256(candidate)
                if pre != post:
                    shutil.copy2(candidate, target)
                records.append(
                    FrameRecord(
                        age=age,
                        gender=gender,
                        color=color,
                        family=family,
                        frame=frame,
                        target=rel(target),
                        candidate=rel(candidate),
                        pre_sha256=pre,
                        post_sha256=post,
                        changed=pre != post,
                    )
                )
    return records, backup_root


def rollback(records: list[FrameRecord], backup_root: Path) -> None:
    for record in records:
        target = ROOT / record.target
        backup = backup_root / record.target
        shutil.copy2(backup, target)


def write_report(output_root: Path, source_path: Path, copied_source: Path, issues: dict[tuple[str, str, str], str], records: list[FrameRecord], backup_root: Path | None, applied: bool, rollback_drill: bool) -> None:
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "input_source": str(source_path),
        "copied_source": rel(copied_source),
        "applied": applied,
        "blocked_frame_issues": {"/".join(key): value for key, value in sorted(issues.items())},
        "backup_root": rel(backup_root) if backup_root else None,
        "rollback_drill": rollback_drill,
        "changed_count": sum(1 for record in records if record.changed),
        "records": [asdict(record) for record in records],
    }
    (output_root / "snake-gemini-clean-source-apply-report.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    lines = [
        "# Snake Gemini Clean Source Apply Report",
        "",
        f"- generated_at_utc: `{payload['generated_at_utc']}`",
        f"- input_source: `{source_path}`",
        f"- copied_source: `{rel(copied_source)}`",
        f"- applied: `{applied}`",
        f"- changed_count: `{payload['changed_count']}`",
        f"- backup_root: `{payload['backup_root']}`",
        f"- rollback_drill: `{rollback_drill}`",
        "",
        "## Blocked Frame Issues",
        "",
    ]
    if issues:
        for key, value in sorted(issues.items()):
            lines.append(f"- `{'/'.join(key)}`: {value}")
    else:
        lines.append("- None.")
    lines.extend(["", "## Changed Targets", ""])
    for record in records:
        if record.changed:
            lines.append(f"- `{record.target}` `{record.pre_sha256[:12]}` -> `{record.post_sha256[:12]}`")
    (output_root / "snake-gemini-clean-source-apply-report.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, default=ARTIFACT_ROOT)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--allow-blocked", action="store_true", help="Apply even if extraction quality heuristics report issues.")
    args = parser.parse_args()

    output_root = args.output_root
    output_root.mkdir(parents=True, exist_ok=True)
    copied_source = copy_source(args.input)
    candidates, issues = build_candidates(copied_source, output_root)
    write_contact_sheet(candidates, issues, output_root / "snake-gemini-clean-source-contact-sheet.png")

    records: list[FrameRecord] = []
    backup_root: Path | None = None
    rollback_drill = False
    if args.apply:
        if issues and not args.allow_blocked:
            raise SystemExit(f"Refusing apply because extraction issues were found: {issues}")
        records, backup_root = apply_candidates(output_root)
        rollback(records, backup_root)
        rollback_ok = all(sha256(ROOT / record.target) == record.pre_sha256 for record in records)
        if not rollback_ok:
            raise SystemExit("Rollback drill failed.")
        for record in records:
            shutil.copy2(ROOT / record.candidate, ROOT / record.target)
        reapply_ok = all(sha256(ROOT / record.target) == record.post_sha256 for record in records)
        if not reapply_ok:
            raise SystemExit("Re-apply after rollback failed.")
        rollback_drill = True

    write_report(output_root, args.input, copied_source, issues, records, backup_root, args.apply, rollback_drill)
    print(f"source={copied_source}")
    print(f"contact_sheet={output_root / 'snake-gemini-clean-source-contact-sheet.png'}")
    print(f"issues={len(issues)}")
    print(f"applied={args.apply}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
