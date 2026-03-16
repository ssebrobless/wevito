#!/usr/bin/env python3
"""
Report authored sprite coverage against the expected species/age/gender/color/frame contract.
"""

from __future__ import annotations

import json
from pathlib import Path

from authored_motion_specs import MOTION_FAMILIES


ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = ROOT / "tools" / "incoming_animal_pose_manifest.json"
AUTHORED_ROOT = ROOT / "sprites_authored_verified"
OUTPUT_ROOT = ROOT / "vnext" / "artifacts" / "authored-coverage"
FRAME_LAYOUT = [
    ["idle_00", "idle_01", "idle_02", "idle_03", "walk_00"],
    ["walk_01", "walk_02", "walk_03", "walk_04", "walk_05"],
    ["eat_00", "eat_01", "eat_02", "eat_03", "happy_00"],
    ["happy_01", "happy_02", "happy_03", "sad_00", "sad_01"],
    ["sleep_00", "sleep_01", "sick_00", "sick_01", "sick_02"],
    ["sick_03", "bathe_00", "bathe_01", "bathe_02", "bathe_03"],
]
COLORS = ["red", "orange", "yellow", "blue", "indigo", "violet"]


def expected_frames() -> list[str]:
    return [frame for row in FRAME_LAYOUT for frame in row]


def expected_family_frames() -> dict[str, list[str]]:
    families: dict[str, list[str]] = {}
    for family_name, family_spec in MOTION_FAMILIES.items():
        frames = [frame for row in family_spec["frame_layout"] for frame in row if frame]
        families[family_name] = frames
    return families


def load_variants() -> list[dict[str, str]]:
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    variants: list[dict[str, str]] = []
    for entry in manifest:
        for gender in entry["component_order"]:
            for color in COLORS:
                variants.append(
                    {
                        "species": entry["species"],
                        "age": entry["age_stage"],
                        "gender": gender,
                        "color": color,
                    }
                )
    return variants


def main() -> None:
    frames = expected_frames()
    family_frames = expected_family_frames()
    variants = load_variants()
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)

    report: dict[str, object] = {
        "expected_variant_count": len(variants),
        "expected_frame_count_per_variant": len(frames),
        "complete_variants": 0,
        "incomplete_variants": 0,
        "familyCoverage": {},
        "speciesBaseCoverage": {},
        "variants": [],
    }

    complete = 0
    family_complete_counts = {family: 0 for family in family_frames}
    base_variant_status: dict[tuple[str, str, str], dict[str, object]] = {}
    for variant in variants:
        variant_root = AUTHORED_ROOT / variant["species"] / variant["age"] / variant["gender"] / variant["color"]
        missing = [frame for frame in frames if not (variant_root / f"{frame}.png").exists()]
        family_missing = {
            family: [frame for frame in family_expected if not (variant_root / f"{frame}.png").exists()]
            for family, family_expected in family_frames.items()
        }
        if not missing:
            complete += 1
        for family, family_missing_frames in family_missing.items():
            if not family_missing_frames:
                family_complete_counts[family] += 1

        base_key = (variant["species"], variant["age"], variant["gender"])
        base_entry = base_variant_status.setdefault(
            base_key,
            {
                "species": variant["species"],
                "age": variant["age"],
                "gender": variant["gender"],
                "colors_complete": 0,
                "families_complete": {family: 0 for family in family_frames},
            },
        )
        if not missing:
            base_entry["colors_complete"] += 1
        for family, family_missing_frames in family_missing.items():
            if not family_missing_frames:
                base_entry["families_complete"][family] += 1

        report["variants"].append(
            {
                **variant,
                "path": str(variant_root),
                "complete": not missing,
                "missing_frames": missing,
                "family_missing_frames": family_missing,
            }
        )

    report["complete_variants"] = complete
    report["incomplete_variants"] = len(variants) - complete
    report["familyCoverage"] = {
        family: {
            "expected_variants": len(variants),
            "complete_variants": count,
            "incomplete_variants": len(variants) - count,
        }
        for family, count in family_complete_counts.items()
    }
    report["speciesBaseCoverage"] = [
        {
            **entry,
            "all_colors_complete": entry["colors_complete"] == len(COLORS),
            "family_colors_complete": {
                family: count == len(COLORS)
                for family, count in entry["families_complete"].items()
            },
        }
        for entry in sorted(
            base_variant_status.values(),
            key=lambda item: (item["species"], item["age"], item["gender"]),
        )
    ]

    summary_path = OUTPUT_ROOT / "summary.json"
    summary_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(summary_path)


if __name__ == "__main__":
    main()
