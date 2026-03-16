#!/usr/bin/env python3
"""
Audit Wevito's canonical sprite source contract against the current runtime tree.
"""

from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "tools" / "incoming_animal_pose_manifest.json"
DEFAULT_INCOMING = ROOT / "incoming_sprites"
DEFAULT_RUNTIME = ROOT / "sprites_runtime"

EXPECTED_SPECIES = [
    "rat",
    "crow",
    "fox",
    "snake",
    "deer",
    "frog",
    "pigeon",
    "raccoon",
    "squirrel",
    "goose",
]
EXPECTED_AGES = ["baby", "teen", "adult"]
EXPECTED_GENDERS = ["male", "female"]
EXPECTED_COLORS = ["red", "orange", "yellow", "blue", "indigo", "violet"]
EXPECTED_ANIMATIONS = {
    "idle": 4,
    "walk": 6,
    "eat": 4,
    "happy": 4,
    "sad": 2,
    "sleep": 2,
    "sick": 4,
    "bathe": 4,
}
EXPECTED_SUPPORTING_INPUTS = [
    "egg-hatch-lifecycle.png",
    "environments-A.png",
    "environments-B.png",
    "food-omnivore-scavenger.png",
    "food-herbivore-grazer.png",
    "food-birds-seed.png",
    "food-predator-reptile.png",
    "water-feeding-containers.png",
    "medicine-care.png",
    "toys-enrichment-A.png",
    "toys-enrichment-B.png",
    "utility-shelter-props.png",
    "status-effects.png",
    "large_action_icons.png",
    "small_ui_icons.png",
    "sun.png",
    "moon.png",
]


@dataclass
class AuditCounts:
    source_boards_found: int
    source_boards_expected: int
    supporting_inputs_found: int
    supporting_inputs_expected: int
    runtime_variant_dirs_found: int
    runtime_variant_dirs_expected: int
    runtime_frames_found: int
    runtime_frames_expected: int


def load_manifest(path: Path) -> list[dict[str, Any]]:
    return json.loads(path.read_text(encoding="utf-8"))


def png_dimensions(path: Path) -> tuple[int, int, int]:
    with path.open("rb") as stream:
        header = stream.read(33)
    if len(header) < 33 or header[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path} is not a PNG file.")
    width = int.from_bytes(header[16:20], "big")
    height = int.from_bytes(header[20:24], "big")
    color_type = header[25]
    return width, height, color_type


def is_source_board_entry_valid(entry: dict[str, Any]) -> bool:
    return (
        entry.get("species") in EXPECTED_SPECIES
        and entry.get("age_stage") in EXPECTED_AGES
        and entry.get("component_order") == ["male", "female"]
    )


def audit_source_boards(manifest: list[dict[str, Any]], incoming_root: Path) -> tuple[list[dict[str, Any]], list[str]]:
    results: list[dict[str, Any]] = []
    errors: list[str] = []
    seen_pairs: set[tuple[str, str]] = set()

    for entry in manifest:
        source_name = entry["source"]
        source_path = incoming_root / source_name
        pair = (entry["species"], entry["age_stage"])
        seen_pairs.add(pair)

        if not is_source_board_entry_valid(entry):
            errors.append(f"Manifest entry invalid for {source_name}.")

        if not source_path.exists():
            errors.append(f"Missing source board: {source_name}")
            continue

        width, height, color_type = png_dimensions(source_path)
        results.append(
            {
                "source": source_name,
                "species": entry["species"],
                "age_stage": entry["age_stage"],
                "component_order": entry["component_order"],
                "width": width,
                "height": height,
                "color_type": color_type,
            }
        )

    expected_pairs = {(species, age) for species in EXPECTED_SPECIES for age in EXPECTED_AGES}
    missing_pairs = sorted(expected_pairs - seen_pairs)
    extra_pairs = sorted(seen_pairs - expected_pairs)

    for species, age_stage in missing_pairs:
        errors.append(f"Missing manifest mapping for {species}/{age_stage}.")
    for species, age_stage in extra_pairs:
        errors.append(f"Unexpected manifest mapping for {species}/{age_stage}.")

    return results, errors


def audit_supporting_inputs(incoming_root: Path) -> tuple[list[str], list[str]]:
    found: list[str] = []
    errors: list[str] = []
    for filename in EXPECTED_SUPPORTING_INPUTS:
        path = incoming_root / filename
        if path.exists():
            found.append(filename)
        else:
            errors.append(f"Missing supporting source input: {filename}")
    return found, errors


def audit_runtime(runtime_root: Path) -> tuple[list[dict[str, Any]], list[str], int, int]:
    results: list[dict[str, Any]] = []
    errors: list[str] = []
    variant_dirs_found = 0
    frames_found = 0

    for species in EXPECTED_SPECIES:
        for age_stage in EXPECTED_AGES:
            for gender in EXPECTED_GENDERS:
                for color in EXPECTED_COLORS:
                    variant_dir = runtime_root / species / age_stage / gender / color
                    if not variant_dir.exists():
                        errors.append(f"Missing runtime variant directory: {species}/{age_stage}/{gender}/{color}")
                        continue

                    variant_dirs_found += 1
                    variant_summary = {
                        "species": species,
                        "age_stage": age_stage,
                        "gender": gender,
                        "color": color,
                        "animations": {},
                    }

                    for animation_name, frame_count in EXPECTED_ANIMATIONS.items():
                        frames = sorted(variant_dir.glob(f"{animation_name}_*.png"))
                        variant_summary["animations"][animation_name] = len(frames)

                        if len(frames) != frame_count:
                            errors.append(
                                f"{species}/{age_stage}/{gender}/{color} expected {frame_count} "
                                f"{animation_name} frame(s), found {len(frames)}."
                            )
                            continue

                        for frame_path in frames:
                            width, height, color_type = png_dimensions(frame_path)
                            if width != 28 or height != 24:
                                errors.append(
                                    f"{frame_path} expected 28x24, found {width}x{height}."
                                )
                            if color_type not in {4, 6}:
                                errors.append(
                                    f"{frame_path} lost alpha-capable PNG type, found color type {color_type}."
                                )

                        frames_found += len(frames)

                    results.append(variant_summary)

    variant_dirs_expected = len(EXPECTED_SPECIES) * len(EXPECTED_AGES) * len(EXPECTED_GENDERS) * len(EXPECTED_COLORS)
    frames_expected = variant_dirs_expected * sum(EXPECTED_ANIMATIONS.values())
    return results, errors, variant_dirs_found, frames_found


def build_summary(
    source_results: list[dict[str, Any]],
    supporting_inputs_found: list[str],
    runtime_results: list[dict[str, Any]],
    all_errors: list[str],
    variant_dirs_found: int,
    frames_found: int,
) -> dict[str, Any]:
    counts = AuditCounts(
        source_boards_found=len(source_results),
        source_boards_expected=len(EXPECTED_SPECIES) * len(EXPECTED_AGES),
        supporting_inputs_found=len(supporting_inputs_found),
        supporting_inputs_expected=len(EXPECTED_SUPPORTING_INPUTS),
        runtime_variant_dirs_found=variant_dirs_found,
        runtime_variant_dirs_expected=len(EXPECTED_SPECIES) * len(EXPECTED_AGES) * len(EXPECTED_GENDERS) * len(EXPECTED_COLORS),
        runtime_frames_found=frames_found,
        runtime_frames_expected=len(EXPECTED_SPECIES)
        * len(EXPECTED_AGES)
        * len(EXPECTED_GENDERS)
        * len(EXPECTED_COLORS)
        * sum(EXPECTED_ANIMATIONS.values()),
    )

    return {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "contract": {
            "species": EXPECTED_SPECIES,
            "ages": EXPECTED_AGES,
            "genders": EXPECTED_GENDERS,
            "colors": EXPECTED_COLORS,
            "animations": EXPECTED_ANIMATIONS,
        },
        "counts": asdict(counts),
        "errors": all_errors,
        "source_boards": source_results,
        "supporting_inputs": supporting_inputs_found,
        "runtime_variants": runtime_results,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--incoming-root", type=Path, default=DEFAULT_INCOMING)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME)
    parser.add_argument("--output", type=Path, default=None)
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    source_results, source_errors = audit_source_boards(manifest, args.incoming_root)
    supporting_found, supporting_errors = audit_supporting_inputs(args.incoming_root)
    runtime_results, runtime_errors, variant_dirs_found, frames_found = audit_runtime(args.runtime_root)

    all_errors = source_errors + supporting_errors + runtime_errors
    summary = build_summary(
        source_results,
        supporting_found,
        runtime_results,
        all_errors,
        variant_dirs_found,
        frames_found,
    )

    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(json.dumps(
        {
            "source_boards_found": summary["counts"]["source_boards_found"],
            "source_boards_expected": summary["counts"]["source_boards_expected"],
            "supporting_inputs_found": summary["counts"]["supporting_inputs_found"],
            "supporting_inputs_expected": summary["counts"]["supporting_inputs_expected"],
            "runtime_variant_dirs_found": summary["counts"]["runtime_variant_dirs_found"],
            "runtime_variant_dirs_expected": summary["counts"]["runtime_variant_dirs_expected"],
            "runtime_frames_found": summary["counts"]["runtime_frames_found"],
            "runtime_frames_expected": summary["counts"]["runtime_frames_expected"],
            "error_count": len(all_errors),
        },
        indent=2,
    ))

    if all_errors:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
