#!/usr/bin/env python3
"""Report runtime sprite animation sequences with inconsistent PNG canvases.

This is intentionally non-mutating. It exists to make
SpriteRuntimeCoverageTests and visual-policy failures actionable without
normalizing or rewriting any sprite assets.
"""

from __future__ import annotations

import argparse
import json
import struct
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RUNTIME_ROOT = ROOT / "sprites_runtime"
DEFAULT_OUTPUT = ROOT / "vnext" / "artifacts" / "runtime-canvas-mismatches.json"

REQUIRED_ANIMATIONS: dict[str, int] = {
    "idle": 4,
    "walk": 6,
    "eat": 4,
    "happy": 4,
    "sad": 2,
    "sleep": 2,
    "sick": 4,
    "bathe": 4,
}


def read_png_size(path: Path) -> tuple[int, int, int]:
    with path.open("rb") as handle:
        header = handle.read(33)
    if len(header) < 33 or header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise ValueError(f"Invalid PNG header: {path}")
    width, height = struct.unpack(">II", header[16:24])
    color_type = header[25]
    return width, height, color_type


def is_asset_dir(path: Path) -> bool:
    name = path.name
    return not name.startswith("_") and not name.startswith(".")


def iter_variant_dirs(runtime_root: Path) -> list[Path]:
    variants: list[Path] = []
    for species_dir in sorted((path for path in runtime_root.iterdir() if path.is_dir() and is_asset_dir(path)), key=lambda p: p.name):
        for age_dir in sorted((path for path in species_dir.iterdir() if path.is_dir() and is_asset_dir(path)), key=lambda p: p.name):
            for gender_dir in sorted((path for path in age_dir.iterdir() if path.is_dir() and is_asset_dir(path)), key=lambda p: p.name):
                for color_dir in sorted((path for path in gender_dir.iterdir() if path.is_dir() and is_asset_dir(path)), key=lambda p: p.name):
                    variants.append(color_dir)
    return variants


def frame_index(path: Path) -> int:
    try:
        return int(path.stem.rsplit("_", 1)[1])
    except (IndexError, ValueError):
        return 10_000


def resolve_expected_canvas(species: str, age: str, animation: str) -> tuple[int, int]:
    if species.lower() == "snake" and animation.lower() == "walk":
        return {
            "baby": (104, 64),
            "teen": (112, 64),
        }.get(age.lower(), (120, 64))

    return 72, 64


def inspect_runtime(runtime_root: Path) -> dict[str, Any]:
    mismatches: list[dict[str, Any]] = []
    canonical_mismatches: list[dict[str, Any]] = []
    missing: list[dict[str, Any]] = []
    invalid: list[dict[str, Any]] = []
    checked_sequences = 0
    checked_frames = 0
    animation_counts: Counter[str] = Counter()
    species_counts: Counter[str] = Counter()
    canonical_animation_counts: Counter[str] = Counter()
    canonical_species_counts: Counter[str] = Counter()
    summary_path = runtime_root / "generation-summary.json"
    summary_exists = summary_path.exists()

    for variant_dir in iter_variant_dirs(runtime_root):
        relative_parts = variant_dir.relative_to(runtime_root).parts
        species, age, gender, color = relative_parts
        for animation, expected_count in REQUIRED_ANIMATIONS.items():
            checked_sequences += 1
            frames = sorted(variant_dir.glob(f"{animation}_*.png"), key=frame_index)
            if len(frames) != expected_count:
                missing.append(
                    {
                        "variant": str(variant_dir.relative_to(runtime_root)),
                        "species": species,
                        "age": age,
                        "gender": gender,
                        "color": color,
                        "animation": animation,
                        "expected_count": expected_count,
                        "actual_count": len(frames),
                    }
                )

            frame_records: list[dict[str, Any]] = []
            sizes: list[tuple[int, int]] = []
            expected_width, expected_height = resolve_expected_canvas(species, age, animation)
            for frame in frames:
                checked_frames += 1
                try:
                    width, height, color_type = read_png_size(frame)
                except ValueError as exc:
                    invalid.append({"path": str(frame), "error": str(exc)})
                    continue
                sizes.append((width, height))
                if color_type not in {4, 6}:
                    invalid.append(
                        {
                            "path": str(frame),
                            "error": f"PNG is not alpha-capable; found color type {color_type}.",
                            "color_type": color_type,
                        }
                    )
                frame_records.append(
                    {
                        "path": str(frame),
                        "file": frame.name,
                        "width": width,
                        "height": height,
                        "color_type": color_type,
                    }
                )
                if width != expected_width or height != expected_height:
                    canonical_animation_counts[animation] += 1
                    canonical_species_counts[species] += 1
                    canonical_mismatches.append(
                        {
                            "variant": str(variant_dir.relative_to(runtime_root)),
                            "species": species,
                            "age": age,
                            "gender": gender,
                            "color": color,
                            "animation": animation,
                            "file": frame.name,
                            "path": str(frame),
                            "expected_width": expected_width,
                            "expected_height": expected_height,
                            "actual_width": width,
                            "actual_height": height,
                            "color_type": color_type,
                        }
                    )

            if len(set(sizes)) > 1:
                animation_counts[animation] += 1
                species_counts[species] += 1
                mismatches.append(
                    {
                        "variant": str(variant_dir.relative_to(runtime_root)),
                        "species": species,
                        "age": age,
                        "gender": gender,
                        "color": color,
                        "animation": animation,
                        "unique_sizes": [
                            {"width": width, "height": height}
                            for width, height in sorted(set(sizes))
                        ],
                        "frames": frame_records,
                    }
                )

    return {
        "captured_at": datetime.now(timezone.utc).isoformat(),
        "runtime_root": str(runtime_root),
        "generation_summary_path": str(summary_path),
        "generation_summary_exists": summary_exists,
        "required_animations": REQUIRED_ANIMATIONS,
        "checked_sequences": checked_sequences,
        "checked_frames": checked_frames,
        "mismatch_count": len(mismatches),
        "canonical_mismatch_count": len(canonical_mismatches),
        "missing_count": len(missing),
        "invalid_count": len(invalid),
        "mismatches_by_animation": dict(sorted(animation_counts.items())),
        "mismatches_by_species": dict(sorted(species_counts.items())),
        "canonical_mismatches_by_animation": dict(sorted(canonical_animation_counts.items())),
        "canonical_mismatches_by_species": dict(sorted(canonical_species_counts.items())),
        "mismatches": mismatches,
        "canonical_mismatches": canonical_mismatches,
        "missing": missing,
        "invalid": invalid,
    }


def write_markdown(report: dict[str, Any], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "# Runtime Canvas Mismatch Report",
        "",
        f"- Runtime root: `{report['runtime_root']}`",
        f"- Generation summary exists: {report['generation_summary_exists']}",
        f"- Checked sequences: {report['checked_sequences']}",
        f"- Checked frames: {report['checked_frames']}",
        f"- Mixed-canvas sequences: {report['mismatch_count']}",
        f"- Legacy fixed-canvas diagnostic mismatches: {report['canonical_mismatch_count']}",
        f"- Missing/count-mismatch sequences: {report['missing_count']}",
        f"- Invalid or non-alpha PNG frames: {report['invalid_count']}",
        "",
        "The current runtime policy allows natural per-sequence canvases. Legacy fixed-canvas mismatches are diagnostic only unless `--fail-on-canonical-mismatch` is used.",
        "",
        "## Mixed Canvas By Animation",
        "",
    ]
    if report["mismatches_by_animation"]:
        for animation, count in report["mismatches_by_animation"].items():
            lines.append(f"- `{animation}`: {count}")
    else:
        lines.append("- none")

    lines.extend(["", "## Mixed Canvas By Species", ""])
    if report["mismatches_by_species"]:
        for species, count in report["mismatches_by_species"].items():
            lines.append(f"- `{species}`: {count}")
    else:
        lines.append("- none")

    lines.extend(["", "## First 50 Mismatches", ""])
    for item in report["mismatches"][:50]:
        sizes = ", ".join(f"{size['width']}x{size['height']}" for size in item["unique_sizes"])
        lines.append(f"- `{item['variant']}` `{item['animation']}`: {sizes}")
    if len(report["mismatches"]) > 50:
        lines.append(f"- ... {len(report['mismatches']) - 50} additional mismatches omitted from markdown summary.")

    lines.extend(["", "## Canonical Canvas By Animation", ""])
    if report["canonical_mismatches_by_animation"]:
        for animation, count in report["canonical_mismatches_by_animation"].items():
            lines.append(f"- `{animation}`: {count}")
    else:
        lines.append("- none")

    lines.extend(["", "## Canonical Canvas By Species", ""])
    if report["canonical_mismatches_by_species"]:
        for species, count in report["canonical_mismatches_by_species"].items():
            lines.append(f"- `{species}`: {count}")
    else:
        lines.append("- none")

    lines.extend(["", "## First 50 Canonical Canvas Mismatches", ""])
    for item in report["canonical_mismatches"][:50]:
        lines.append(
            f"- `{item['variant']}` `{item['animation']}` `{item['file']}`: "
            f"expected {item['expected_width']}x{item['expected_height']}, "
            f"found {item['actual_width']}x{item['actual_height']}"
        )
    if len(report["canonical_mismatches"]) > 50:
        lines.append(f"- ... {len(report['canonical_mismatches']) - 50} additional canonical mismatches omitted from markdown summary.")

    if report["missing"]:
        lines.extend(["", "## Missing Or Count Mismatches", ""])
        for item in report["missing"][:50]:
            lines.append(
                f"- `{item['variant']}` `{item['animation']}`: expected {item['expected_count']}, found {item['actual_count']}"
            )
        if len(report["missing"]) > 50:
            lines.append(f"- ... {len(report['missing']) - 50} additional count mismatches omitted from markdown summary.")

    if report["invalid"]:
        lines.extend(["", "## Invalid Or Non-Alpha PNG Frames", ""])
        for item in report["invalid"][:50]:
            lines.append(f"- `{item['path']}`: {item['error']}")
        if len(report["invalid"]) > 50:
            lines.append(f"- ... {len(report['invalid']) - 50} additional invalid frames omitted from markdown summary.")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--markdown", type=Path)
    parser.add_argument("--fail-on-mismatch", action="store_true", help="Fail on sequence canvas mismatches, missing/count mismatches, invalid PNGs, or non-alpha PNGs.")
    parser.add_argument("--fail-on-canonical-mismatch", action="store_true", help="Also fail on legacy fixed-canvas diagnostic mismatches.")
    parser.add_argument("--fail-on-missing-summary", action="store_true", help="Also fail when generation-summary.json is absent.")
    args = parser.parse_args()

    if not args.runtime_root.exists():
        raise FileNotFoundError(args.runtime_root)

    report = inspect_runtime(args.runtime_root)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

    if args.markdown:
        write_markdown(report, args.markdown)

    print(f"checked_sequences={report['checked_sequences']}")
    print(f"checked_frames={report['checked_frames']}")
    print(f"mismatch_count={report['mismatch_count']}")
    print(f"canonical_mismatch_count={report['canonical_mismatch_count']}")
    print(f"missing_count={report['missing_count']}")
    print(f"invalid_count={report['invalid_count']}")
    print(f"output={args.output}")
    if args.markdown:
        print(f"markdown={args.markdown}")

    should_fail = False
    if args.fail_on_mismatch and (report["mismatch_count"] or report["missing_count"] or report["invalid_count"]):
        should_fail = True
    if args.fail_on_canonical_mismatch and report["canonical_mismatch_count"]:
        should_fail = True
    if args.fail_on_missing_summary and not report["generation_summary_exists"]:
        should_fail = True
    if should_fail:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
