#!/usr/bin/env python3
"""Create review artifacts for runtime canvas alignment rows.

The input is a normalization report from plan_runtime_canvas_normalization.py.
This tool does not edit sprite PNGs. It groups color-variant rows into source
rows and renders candidate contact sheets for ambiguous top/bottom alignment.
"""

from __future__ import annotations

import argparse
import json
import math
import shutil
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


DEFAULT_INPUT = Path("vnext/artifacts/runtime-canvas-normalization-post-phase4a.json")
DEFAULT_OUTPUT = Path("vnext/artifacts/runtime-canvas-alignment-review-20260504.json")
DEFAULT_MARKDOWN = Path("vnext/artifacts/runtime-canvas-alignment-review-20260504.md")
DEFAULT_SHEET_DIR = Path("vnext/artifacts/runtime-canvas-alignment-review-20260504-sheets")
DEFAULT_APPLY_REPORT = Path("vnext/artifacts/runtime-canvas-alignment-review-apply-20260504.json")
DEFAULT_RECOVERY_ROOT = Path("artifacts/recovery")

ROW_BACKGROUNDS = {
    "original": (34, 44, 56, 255),
    "top_anchor": (34, 58, 46, 255),
    "center_anchor": (52, 45, 74, 255),
    "bottom_anchor": (64, 43, 41, 255),
    "alpha_bottom": (46, 54, 75, 255),
}


@dataclass(frozen=True)
class GroupKey:
    species: str
    age: str
    gender: str
    animation: str

    @property
    def slug(self) -> str:
        return f"{self.species}-{self.age}-{self.gender}-{self.animation}"

    @property
    def label(self) -> str:
        return f"{self.species} / {self.age} / {self.gender} / {self.animation}"


def load_font(size: int = 13) -> ImageFont.ImageFont:
    try:
        return ImageFont.truetype("arial.ttf", size)
    except OSError:
        return ImageFont.load_default()


def checker(size: tuple[int, int], base: tuple[int, int, int, int]) -> Image.Image:
    image = Image.new("RGBA", size, base)
    draw = ImageDraw.Draw(image)
    tile = 8
    tint = tuple(min(255, value + 18) for value in base[:3]) + (255,)
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            if (x // tile + y // tile) % 2 == 0:
                draw.rectangle([x, y, min(x + tile - 1, size[0] - 1), min(y + tile - 1, size[1] - 1)], fill=tint)
    return image


def operation_signature(sequence: dict[str, Any]) -> tuple[Any, ...]:
    signatures = []
    for operation in sequence["operations"]:
        signatures.append(
            (
                operation["file"],
                tuple(operation["current_canvas"].values()),
                tuple(operation["target_canvas"].values()),
                tuple(operation["padding"].values()),
                operation["would_change"],
                tuple(operation["used_alpha_bounds"].values()),
                tuple(operation["transparent_margins"].values()),
            )
        )
    return tuple(signatures)


def choose_representative(sequences: list[dict[str, Any]]) -> dict[str, Any]:
    for sequence in sequences:
        if sequence["color"] == "blue":
            return sequence
    return sequences[0]


def alpha_bottom_values(sequence: dict[str, Any]) -> list[int]:
    values: list[int] = []
    for operation in sequence["operations"]:
        bottom = operation["used_alpha_bounds"]["bottom"]
        if bottom is not None:
            values.append(int(bottom))
    return values


def classify_group(sequence: dict[str, Any]) -> dict[str, Any]:
    bottoms = alpha_bottom_values(sequence)
    bottom_counts = Counter(bottoms)
    mode_bottom = bottom_counts.most_common(1)[0][0] if bottom_counts else None
    all_same_bottom = len(set(bottoms)) <= 1 if bottoms else False

    feasible_alpha_bottom = True
    if mode_bottom is not None:
        for operation in sequence["operations"]:
            bounds = operation["used_alpha_bounds"]
            current = operation["current_canvas"]
            target = operation["target_canvas"]
            missing_height = int(target["height"]) - int(current["height"])
            if bounds["bottom"] is None:
                continue
            required_top = mode_bottom - int(bounds["bottom"])
            if required_top < 0 or required_top > missing_height:
                feasible_alpha_bottom = False
                break

    recommendation = "visual_review"
    reason = "ambiguous vertical alignment; inspect contact sheet before mutation"
    if all_same_bottom:
        recommendation = "preserve_existing_alpha_bottom"
        reason = "all representative frame alpha bottoms already share one y-coordinate"
    elif feasible_alpha_bottom and mode_bottom is not None and bottom_counts[mode_bottom] >= max(2, math.ceil(len(bottoms) / 2)):
        recommendation = "mode_alpha_bottom_possible"
        reason = f"mode alpha bottom y={mode_bottom} is feasible for every changed frame"

    return {
        "alpha_bottom_values": bottoms,
        "alpha_bottom_counts": dict(sorted(bottom_counts.items())),
        "mode_alpha_bottom": mode_bottom,
        "all_same_alpha_bottom": all_same_bottom,
        "mode_alpha_bottom_feasible": feasible_alpha_bottom,
        "recommendation": recommendation,
        "reason": reason,
    }


def candidate_padding(operation: dict[str, Any], candidate: str, mode_bottom: int | None) -> tuple[int, int, int, int]:
    current = operation["current_canvas"]
    target = operation["target_canvas"]
    missing_width = int(target["width"]) - int(current["width"])
    missing_height = int(target["height"]) - int(current["height"])
    left = missing_width // 2
    right = missing_width - left

    if candidate == "top_anchor":
        return left, 0, right, missing_height
    if candidate == "bottom_anchor":
        return left, missing_height, right, 0
    if candidate == "alpha_bottom" and mode_bottom is not None:
        bounds = operation["used_alpha_bounds"]
        if bounds["bottom"] is not None:
            top = max(0, min(missing_height, mode_bottom - int(bounds["bottom"])))
            return left, top, right, missing_height - top
    padding = operation["padding"]
    return int(padding["left"]), int(padding["top"]), int(padding["right"]), int(padding["bottom"])


def render_frame(
    operation: dict[str, Any],
    candidate: str,
    mode_bottom: int | None,
    cell: tuple[int, int],
    font: ImageFont.ImageFont,
) -> Image.Image:
    target = operation["target_canvas"]
    frame = Image.open(operation["path"]).convert("RGBA")
    target_size = (int(target["width"]), int(target["height"]))
    scale = max(1, min((cell[0] - 28) // max(1, target_size[0]), (cell[1] - 52) // max(1, target_size[1])))
    canvas_px = (target_size[0] * scale, target_size[1] * scale)
    tile = checker(canvas_px, ROW_BACKGROUNDS[candidate])
    draw = ImageDraw.Draw(tile)

    if candidate == "original":
        left = top = 0
        original_border = (frame.width * scale, frame.height * scale)
    else:
        left, top, _right, _bottom = candidate_padding(operation, candidate, mode_bottom)
        original_border = None

    sprite = frame.resize((frame.width * scale, frame.height * scale), Image.Resampling.NEAREST)
    tile.alpha_composite(sprite, (left * scale, top * scale))

    draw.rectangle([0, 0, canvas_px[0] - 1, canvas_px[1] - 1], outline=(220, 235, 255, 255), width=1)
    if original_border is not None:
        draw.rectangle([0, 0, original_border[0] - 1, original_border[1] - 1], outline=(255, 210, 80, 255), width=1)

    bounds = operation["used_alpha_bounds"]
    if bounds["left"] is not None:
        bbox_left = (int(bounds["left"]) + left) * scale
        bbox_top = (int(bounds["top"]) + top) * scale
        bbox_right = (int(bounds["right"]) + left) * scale - 1
        bbox_bottom = (int(bounds["bottom"]) + top) * scale - 1
        draw.rectangle([bbox_left, bbox_top, bbox_right, bbox_bottom], outline=(74, 222, 128, 255), width=1)

    output = Image.new("RGBA", cell, (16, 22, 30, 255))
    output.alpha_composite(tile, ((cell[0] - canvas_px[0]) // 2, 22))
    label = f"{operation['file']} {operation['current_canvas']['width']}x{operation['current_canvas']['height']}"
    ImageDraw.Draw(output).text((6, 4), label, fill=(235, 240, 248, 255), font=font)
    return output


def render_sheet(sequence: dict[str, Any], classification: dict[str, Any], output_path: Path) -> None:
    font = load_font()
    label_font = load_font(15)
    candidates = ["original", "top_anchor", "center_anchor", "bottom_anchor", "alpha_bottom"]
    frame_count = len(sequence["operations"])
    cell = (150, 150)
    label_width = 168
    header_height = 72
    row_height = cell[1]
    width = label_width + frame_count * cell[0]
    height = header_height + len(candidates) * row_height
    sheet = Image.new("RGBA", (width, height), (10, 16, 24, 255))
    draw = ImageDraw.Draw(sheet)
    title = f"{sequence['species']} / {sequence['age']} / {sequence['gender']} / {sequence['color']} / {sequence['animation']}"
    subtitle = f"target {sequence['target_canvas']['width']}x{sequence['target_canvas']['height']} | {classification['recommendation']}"
    draw.text((12, 10), title, fill=(255, 255, 255, 255), font=label_font)
    draw.text((12, 34), subtitle, fill=(176, 205, 255, 255), font=font)
    draw.text((12, 52), "green=alpha bbox, yellow=original variable canvas in original row", fill=(176, 205, 255, 255), font=font)

    for row_index, candidate in enumerate(candidates):
        y = header_height + row_index * row_height
        draw.rectangle([0, y, width - 1, y + row_height - 1], outline=(55, 73, 96, 255), width=1)
        draw.text((10, y + 12), candidate, fill=(245, 247, 252, 255), font=label_font)
        if candidate == "alpha_bottom":
            draw.text((10, y + 34), f"mode y={classification['mode_alpha_bottom']}", fill=(176, 205, 255, 255), font=font)
        for column, operation in enumerate(sequence["operations"]):
            frame = render_frame(operation, candidate, classification["mode_alpha_bottom"], cell, font)
            sheet.alpha_composite(frame, (label_width + column * cell[0], y))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)


def build_review(report: dict[str, Any], sheet_dir: Path) -> dict[str, Any]:
    groups: dict[GroupKey, list[dict[str, Any]]] = defaultdict(list)
    for sequence in report["sequences"]:
        key = GroupKey(sequence["species"], sequence["age"], sequence["gender"], sequence["animation"])
        groups[key].append(sequence)

    records: list[dict[str, Any]] = []
    recommendation_counts: Counter[str] = Counter()
    species_counts: Counter[str] = Counter()
    animation_counts: Counter[str] = Counter()

    for key in sorted(groups, key=lambda item: (item.species, item.age, item.gender, item.animation)):
        sequences = sorted(groups[key], key=lambda item: item["color"])
        representative = choose_representative(sequences)
        signatures = {operation_signature(sequence) for sequence in sequences}
        classification = classify_group(representative)
        recommendation_counts[classification["recommendation"]] += 1
        species_counts[key.species] += 1
        animation_counts[key.animation] += 1
        sheet_path = sheet_dir / f"{key.slug}.png"
        render_sheet(representative, classification, sheet_path)
        records.append(
            {
                "key": {
                    "species": key.species,
                    "age": key.age,
                    "gender": key.gender,
                    "animation": key.animation,
                },
                "label": key.label,
                "colors": [sequence["color"] for sequence in sequences],
                "color_row_count": len(sequences),
                "color_signatures": len(signatures),
                "representative_color": representative["color"],
                "target_canvas": representative["target_canvas"],
                "unique_canvas_sizes": representative["unique_canvas_sizes"],
                "changed_frames": sum(1 for operation in representative["operations"] if operation["would_change"]),
                "classification": classification,
                "sheet": str(sheet_path),
            }
        )

    return {
        "source": str(DEFAULT_INPUT),
        "scope": "no-edit review artifacts for review_alignment rows",
        "color_sequence_count": len(report["sequences"]),
        "group_count": len(records),
        "recommendation_counts": dict(sorted(recommendation_counts.items())),
        "groups_by_species": dict(sorted(species_counts.items())),
        "groups_by_animation": dict(sorted(animation_counts.items())),
        "sheet_dir": str(sheet_dir),
        "groups": records,
    }


def apply_preserve_alpha_bottom(
    report: dict[str, Any],
    review: dict[str, Any],
    runtime_root: Path,
    recovery_root: Path,
    stamp: str,
) -> dict[str, Any]:
    allowed_keys = {
        (
            group["key"]["species"],
            group["key"]["age"],
            group["key"]["gender"],
            group["key"]["animation"],
        )
        for group in review["groups"]
        if group["classification"]["recommendation"] == "preserve_existing_alpha_bottom"
    }
    backup_root = recovery_root / f"runtime-canvas-alpha-bottom-{stamp}"
    resolved_runtime_root = runtime_root.resolve()
    changed: list[dict[str, Any]] = []
    skipped: list[dict[str, Any]] = []

    for sequence in report["sequences"]:
        key = (sequence["species"], sequence["age"], sequence["gender"], sequence["animation"])
        if key not in allowed_keys:
            skipped.append(
                {
                    "variant": sequence["variant"],
                    "animation": sequence["animation"],
                    "reason": "group is not preserve_existing_alpha_bottom",
                }
            )
            continue

        for operation in sequence["operations"]:
            if not operation["would_change"]:
                continue
            path = Path(operation["path"])
            resolved_path = path.resolve()
            if not resolved_path.is_relative_to(resolved_runtime_root):
                raise ValueError(f"Refusing to edit path outside runtime root: {path}")

            frame = Image.open(path).convert("RGBA")
            target = operation["target_canvas"]
            target_size = (int(target["width"]), int(target["height"]))
            current = operation["current_canvas"]
            missing_width = int(target["width"]) - int(current["width"])
            missing_height = int(target["height"]) - int(current["height"])
            if missing_width < 0 or missing_height < 0:
                raise ValueError(f"Refusing to shrink frame: {path}")

            left = missing_width // 2
            top = 0
            right = missing_width - left
            bottom = missing_height
            expected_size = (frame.width + left + right, frame.height + top + bottom)
            if expected_size != target_size:
                raise ValueError(f"Padding for {path} would produce {expected_size}, expected {target_size}.")

            relative_path = resolved_path.relative_to(resolved_runtime_root)
            backup_path = backup_root / relative_path
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(path, backup_path)

            canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
            canvas.alpha_composite(frame, (left, top))
            canvas.save(path)
            changed.append(
                {
                    "path": str(path),
                    "backup_path": str(backup_path),
                    "from_canvas": operation["current_canvas"],
                    "to_canvas": operation["target_canvas"],
                    "padding": {"left": left, "top": top, "right": right, "bottom": bottom},
                    "policy": "preserve_existing_alpha_bottom",
                }
            )

    return {
        "applied_at": datetime.now(timezone.utc).isoformat(),
        "scope": "applied preserve_existing_alpha_bottom groups only; transparent padding only",
        "backup_root": str(backup_root),
        "changed_file_count": len(changed),
        "changed_files": changed,
        "skipped_sequence_count": len(skipped),
        "skipped_sequences": skipped,
        "allowed_group_count": len(allowed_keys),
    }


def apply_preserve_original_y(
    report: dict[str, Any],
    runtime_root: Path,
    recovery_root: Path,
    stamp: str,
) -> dict[str, Any]:
    backup_root = recovery_root / f"runtime-canvas-preserve-original-y-{stamp}"
    resolved_runtime_root = runtime_root.resolve()
    changed: list[dict[str, Any]] = []

    for sequence in report["sequences"]:
        for operation in sequence["operations"]:
            if not operation["would_change"]:
                continue
            path = Path(operation["path"])
            resolved_path = path.resolve()
            if not resolved_path.is_relative_to(resolved_runtime_root):
                raise ValueError(f"Refusing to edit path outside runtime root: {path}")

            frame = Image.open(path).convert("RGBA")
            target = operation["target_canvas"]
            target_size = (int(target["width"]), int(target["height"]))
            current = operation["current_canvas"]
            missing_width = int(target["width"]) - int(current["width"])
            missing_height = int(target["height"]) - int(current["height"])
            if missing_width < 0 or missing_height < 0:
                raise ValueError(f"Refusing to shrink frame: {path}")

            left = missing_width // 2
            top = 0
            right = missing_width - left
            bottom = missing_height
            expected_size = (frame.width + left + right, frame.height + top + bottom)
            if expected_size != target_size:
                raise ValueError(f"Padding for {path} would produce {expected_size}, expected {target_size}.")

            relative_path = resolved_path.relative_to(resolved_runtime_root)
            backup_path = backup_root / relative_path
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(path, backup_path)

            canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
            canvas.alpha_composite(frame, (left, top))
            canvas.save(path)
            changed.append(
                {
                    "path": str(path),
                    "backup_path": str(backup_path),
                    "variant": sequence["variant"],
                    "animation": sequence["animation"],
                    "from_canvas": operation["current_canvas"],
                    "to_canvas": operation["target_canvas"],
                    "padding": {"left": left, "top": top, "right": right, "bottom": bottom},
                    "policy": "preserve_original_y",
                }
            )

    return {
        "applied_at": datetime.now(timezone.utc).isoformat(),
        "scope": "applied preserve_original_y to all input rows; transparent padding only",
        "backup_root": str(backup_root),
        "changed_file_count": len(changed),
        "changed_files": changed,
        "sequence_count": len(report["sequences"]),
    }


def write_markdown(review: dict[str, Any], path: Path) -> None:
    lines = [
        "# Runtime Canvas Alignment Review",
        "",
        f"- Scope: {review['scope']}",
        f"- Color rows reviewed: {review['color_sequence_count']}",
        f"- Source groups: {review['group_count']}",
        f"- Sheet directory: `{review['sheet_dir']}`",
        "",
        "## Recommendation Counts",
        "",
    ]
    for name, count in review["recommendation_counts"].items():
        lines.append(f"- `{name}`: {count}")
    lines.extend(["", "## Groups By Species", ""])
    for name, count in review["groups_by_species"].items():
        lines.append(f"- `{name}`: {count}")
    lines.extend(["", "## Groups By Animation", ""])
    for name, count in review["groups_by_animation"].items():
        lines.append(f"- `{name}`: {count}")
    lines.extend(["", "## Review Groups", ""])
    for group in review["groups"]:
        classification = group["classification"]
        lines.append(
            f"- `{group['label']}`: {group['color_row_count']} color rows, "
            f"{group['color_signatures']} color signature(s), "
            f"{group['changed_frames']} changed frame(s), "
            f"`{classification['recommendation']}`; sheet `{group['sheet']}`"
        )

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, default=DEFAULT_INPUT)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--markdown", type=Path, default=DEFAULT_MARKDOWN)
    parser.add_argument("--sheet-dir", type=Path, default=DEFAULT_SHEET_DIR)
    parser.add_argument("--runtime-root", type=Path, default=Path("sprites_runtime"))
    parser.add_argument("--apply-preserve-alpha-bottom", action="store_true")
    parser.add_argument("--apply-preserve-original-y", action="store_true")
    parser.add_argument("--apply-report", type=Path, default=DEFAULT_APPLY_REPORT)
    parser.add_argument("--recovery-root", type=Path, default=DEFAULT_RECOVERY_ROOT)
    parser.add_argument("--stamp", default=datetime.now().strftime("%Y%m%d-%H%M%S"))
    args = parser.parse_args()

    report = json.loads(args.input.read_text(encoding="utf-8"))
    review = build_review(report, args.sheet_dir)
    review["source"] = str(args.input)

    apply_report = None
    if args.apply_preserve_alpha_bottom:
        apply_report = apply_preserve_alpha_bottom(
            report,
            review,
            args.runtime_root,
            args.recovery_root,
            args.stamp,
        )
        review["apply_report"] = apply_report
    if args.apply_preserve_original_y:
        if apply_report is not None:
            raise ValueError("Use only one apply mode at a time.")
        apply_report = apply_preserve_original_y(report, args.runtime_root, args.recovery_root, args.stamp)
        review["apply_report"] = apply_report

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(review, indent=2) + "\n", encoding="utf-8")
    write_markdown(review, args.markdown)
    if apply_report is not None:
        args.apply_report.parent.mkdir(parents=True, exist_ok=True)
        args.apply_report.write_text(json.dumps(apply_report, indent=2) + "\n", encoding="utf-8")

    print(f"color_sequence_count={review['color_sequence_count']}")
    print(f"group_count={review['group_count']}")
    print(f"recommendation_counts={json.dumps(review['recommendation_counts'], sort_keys=True)}")
    print(f"output={args.output}")
    print(f"markdown={args.markdown}")
    print(f"sheet_dir={args.sheet_dir}")
    if apply_report is not None:
        print(f"changed_file_count={apply_report['changed_file_count']}")
        if "allowed_group_count" in apply_report:
            print(f"allowed_group_count={apply_report['allowed_group_count']}")
        if "sequence_count" in apply_report:
            print(f"sequence_count={apply_report['sequence_count']}")
        print(f"backup_root={apply_report['backup_root']}")
        print(f"apply_report={args.apply_report}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
