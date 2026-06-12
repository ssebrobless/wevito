#!/usr/bin/env python3
"""
Build the C-PHASE 197 pet-sim completion baseline (read-only).

Merges the sprite contract audit, optional-animation readiness audit, and
source-to-runtime quality audit with two new inventories:

1. Loose-file inventory: every runtime animation file whose family is in neither
   EXPECTED_ANIMATIONS nor OPTIONAL_EXPANDED_ANIMATIONS, classified as:
     - uncontracted_live: family is reachable by the running game. The only known
       member is "groom": PetAnimationState.Groom resolves its primary animation id
       via CurrentAnimationState.ToString().ToLowerInvariant() in
       Wevito.VNext.Shell/SpriteAssetService.cs, so groom_* frames render whenever
       present (the "happy" entry in GetFallbackAnimationId is fallback-only).
       Disposition: REGISTER in EXPECTED_ANIMATIONS (C-PHASE 201).
     - uncontracted_dead: family is unreachable. "play" has no PetAnimationState
       (enum has no Play member) and no action maps to it (actions.json play ->
       animationState happy + optionalAnimationFamily play_ball).
       Disposition: REMOVE (C-PHASE 201, decision D1).
2. Authored-verified coverage matrix: per species x family file counts under
   sprites_authored_verified/.

Output is deterministic: stable ordering everywhere; the only timestamp is the
top-level generated_utc field (excluded from determinism comparisons).

This tool only reads the sprite trees and prior audit artifacts, and only writes
the two output files passed on the command line (which must live under
vnext/artifacts/).
"""

from __future__ import annotations

import argparse
import json
import sys
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from audit_sprite_contract import (  # noqa: E402
    EXPECTED_AGES,
    EXPECTED_ANIMATIONS,
    EXPECTED_COLORS,
    EXPECTED_GENDERS,
    EXPECTED_SPECIES,
    OPTIONAL_EXPANDED_ANIMATIONS,
)

# Families outside both contract lists that the running game can still render.
# See module docstring for the groom rationale.
UNCONTRACTED_LIVE_FAMILIES = {"groom": 4}


def parse_family(filename: str) -> str | None:
    stem = filename.rsplit(".", 1)[0]
    if "_" not in stem:
        return None
    family, _, frame = stem.rpartition("_")
    if not frame.isdigit():
        return None
    return family


def build_loose_inventory(runtime_root: Path) -> dict:
    contracted = set(EXPECTED_ANIMATIONS) | set(OPTIONAL_EXPANDED_ANIMATIONS)
    loose_files: list[dict] = []
    by_family: dict[str, int] = defaultdict(int)
    variants_with_loose: set[str] = set()

    for species in EXPECTED_SPECIES:
        for age in EXPECTED_AGES:
            for gender in EXPECTED_GENDERS:
                for color in EXPECTED_COLORS:
                    variant_dir = runtime_root / species / age / gender / color
                    if not variant_dir.is_dir():
                        continue
                    for png in sorted(variant_dir.glob("*.png")):
                        family = parse_family(png.name)
                        if family is None or family in contracted:
                            continue
                        classification = (
                            "uncontracted_live"
                            if family in UNCONTRACTED_LIVE_FAMILIES
                            else "uncontracted_dead"
                        )
                        loose_files.append(
                            {
                                "path": png.relative_to(ROOT).as_posix(),
                                "variant": f"{species}/{age}/{gender}/{color}",
                                "family": family,
                                "classification": classification,
                            }
                        )
                        by_family[family] += 1
                        variants_with_loose.add(f"{species}/{age}/{gender}/{color}")

    dead = [f for f in loose_files if f["classification"] == "uncontracted_dead"]
    return {
        "total_loose_files": len(loose_files),
        "by_family": dict(sorted(by_family.items())),
        "uncontracted_dead_count": len(dead),
        "uncontracted_dead_variant_count": len(
            {f["variant"] for f in dead}
        ),
        "variants_with_any_loose": len(variants_with_loose),
        "files": loose_files,
    }


def build_authored_coverage(authored_root: Path) -> dict:
    coverage: dict[str, dict[str, int]] = {}
    for species in EXPECTED_SPECIES:
        species_dir = authored_root / species
        per_family: dict[str, int] = defaultdict(int)
        if species_dir.is_dir():
            for png in species_dir.rglob("*.png"):
                family = parse_family(png.name)
                if family is not None:
                    per_family[family] += 1
        coverage[species] = dict(sorted(per_family.items()))
    return coverage


def load_json(path: Path):
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def build_variant_rows(
    readiness: dict, quality: dict, loose: dict
) -> list[dict]:
    missing_by_variant: dict[str, list[str]] = defaultdict(list)
    for target in readiness.get("targets", []):
        if target.get("status") != "runtime_fallback_only":
            continue
        species, age, gender, color, family = target["target_id"].split("|")
        missing_by_variant[f"{species}/{age}/{gender}/{color}"].append(family)

    loose_by_variant: dict[str, list[str]] = defaultdict(list)
    for entry in loose["files"]:
        loose_by_variant[entry["variant"]].append(entry["path"])

    quality_flags_by_row: dict[str, list] = {}
    for row in quality.get("results", []):
        key = "/".join(
            (
                str(row.get("species", "")),
                str(row.get("age_stage", row.get("age", ""))),
                str(row.get("gender", "")),
            )
        )
        quality_flags_by_row[key] = row.get("flags", [])

    rows = []
    for species in EXPECTED_SPECIES:
        for age in EXPECTED_AGES:
            for gender in EXPECTED_GENDERS:
                for color in EXPECTED_COLORS:
                    variant = f"{species}/{age}/{gender}/{color}"
                    quality_key = f"{species}/{age}/{gender}"
                    rows.append(
                        {
                            "variant": variant,
                            "missing_optional_families": sorted(
                                missing_by_variant.get(variant, [])
                            ),
                            "loose_files": sorted(loose_by_variant.get(variant, [])),
                            "quality_row_key": quality_key,
                            "quality_flags": quality_flags_by_row.get(quality_key, []),
                        }
                    )
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifact-root", required=True)
    parser.add_argument("--runtime-root", default=str(ROOT / "sprites_runtime"))
    parser.add_argument(
        "--authored-root", default=str(ROOT / "sprites_authored_verified")
    )
    parser.add_argument("--output", required=True)
    parser.add_argument("--markdown", required=True)
    args = parser.parse_args()

    for out in (args.output, args.markdown):
        resolved = Path(out).resolve()
        if "artifacts" not in resolved.parts:
            raise SystemExit(f"refusing to write outside vnext/artifacts: {resolved}")

    artifact_root = Path(args.artifact_root)
    contract = load_json(artifact_root / "sprite_contract.json")
    readiness = load_json(artifact_root / "optional_readiness.json")
    quality = load_json(artifact_root / "source_to_runtime_quality.json")

    loose = build_loose_inventory(Path(args.runtime_root))
    authored = build_authored_coverage(Path(args.authored_root))
    variant_rows = build_variant_rows(readiness, quality, loose)

    optional_summary = {
        "target_count": readiness.get("target_count"),
        "fallback_only": readiness.get("fallback_only_count"),
        "authored_complete": readiness.get("complete_authored_count"),
        "runtime_only_complete": readiness.get("complete_runtime_only_count"),
        "runtime_prop_anchor_supported": readiness.get(
            "runtime_prop_anchor_supported_count"
        ),
    }

    baseline = {
        "phase_id": "C-PHASE 197",
        "generated_utc": datetime.now(timezone.utc).strftime(
            "%Y-%m-%dT%H:%M:%SZ"
        ),
        "contract_error_count": len(contract.get("errors", [])),
        "optional_readiness_summary": optional_summary,
        "loose_file_inventory": loose,
        "authored_verified_coverage": authored,
        "uncontracted_live_families": UNCONTRACTED_LIVE_FAMILIES,
        "variant_rows": variant_rows,
    }

    output_path = Path(args.output)
    output_path.write_text(
        json.dumps(baseline, indent=1, sort_keys=False) + "\n", encoding="utf-8"
    )

    md_lines = [
        "# C-PHASE 197 Pet-Sim Completion Baseline",
        "",
        f"- Contract error count: {len(contract.get('errors', []))}",
        f"- Optional targets: {optional_summary['target_count']}; fallback-only: {optional_summary['fallback_only']}",
        f"- Prop-anchor supported targets: {optional_summary['runtime_prop_anchor_supported']}",
        f"- Loose files: {loose['total_loose_files']} ({json.dumps(loose['by_family'])})",
        f"- Uncontracted-dead files (C-201 deletion scope): {loose['uncontracted_dead_count']} across {loose['uncontracted_dead_variant_count']} variants",
        "",
        "## Authored-verified coverage (files per species x family)",
        "",
    ]
    for species in EXPECTED_SPECIES:
        fams = authored[species]
        md_lines.append(f"- {species}: {json.dumps(fams) if fams else 'NONE'}")
    Path(args.markdown).write_text("\n".join(md_lines) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "loose_total": loose["total_loose_files"],
                "dead_total": loose["uncontracted_dead_count"],
                "fallback_only": optional_summary["fallback_only"],
                "variant_rows": len(variant_rows),
            }
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
