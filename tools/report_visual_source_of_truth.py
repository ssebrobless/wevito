#!/usr/bin/env python3
"""
Report Wevito visual source-of-truth status without mutating assets.

This C-PHASE 55 reporter intentionally composes existing audit logic instead
of introducing a second validation contract. It writes a deterministic JSON
and markdown recovery map for runtime assets, authored verified sprites,
source boards, optional families, and the next visual review queues.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from audit_optional_animation_readiness import (
    DEFAULT_AUTHORED,
    DEFAULT_MANIFEST as DEFAULT_OPTIONAL_MANIFEST,
    DEFAULT_PROP_ANCHORS,
    DEFAULT_RUNTIME,
    audit as audit_optional_readiness,
)
from audit_sprite_contract import (
    DEFAULT_INCOMING,
    DEFAULT_MANIFEST,
    DEFAULT_RUNTIME as DEFAULT_CONTRACT_RUNTIME,
    EXPECTED_AGES,
    EXPECTED_COLORS,
    EXPECTED_GENDERS,
    EXPECTED_SPECIES,
    audit_runtime,
    audit_source_boards,
    audit_supporting_inputs,
    build_summary as build_contract_summary,
    load_manifest,
)
from report_authored_sprite_coverage import (
    AUTHORED_ROOT,
    expected_family_frames,
    expected_frames,
    load_variants,
)


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_ROOT = ROOT / "vnext" / "artifacts" / "c-phase-55-visual-source-of-truth"


def rel(path: Path) -> str:
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return str(path)


def frame_count(root: Path, variant: dict[str, str], family: str) -> int:
    variant_root = root / variant["species"] / variant["age"] / variant["gender"] / variant["color"]
    return len(list(variant_root.glob(f"{family}_*.png"))) if variant_root.exists() else 0


def authored_coverage(authored_root: Path) -> dict[str, Any]:
    frames = expected_frames()
    family_frames = expected_family_frames()
    variants = load_variants()
    complete_variants = 0
    family_complete_counts = {family: 0 for family in family_frames}
    family_missing_counter: dict[str, Counter[str]] = {
        family: Counter() for family in family_frames
    }
    variant_reports: list[dict[str, Any]] = []

    for variant in variants:
        variant_root = authored_root / variant["species"] / variant["age"] / variant["gender"] / variant["color"]
        missing_frames = [frame for frame in frames if not (variant_root / f"{frame}.png").exists()]
        family_missing = {
            family: [frame for frame in expected if not (variant_root / f"{frame}.png").exists()]
            for family, expected in family_frames.items()
        }

        if not missing_frames:
            complete_variants += 1

        for family, missing in family_missing.items():
            if not missing:
                family_complete_counts[family] += 1
            else:
                family_missing_counter[family].update(missing)

        variant_reports.append(
            {
                **variant,
                "path": rel(variant_root),
                "complete": not missing_frames,
                "missingFrameCount": len(missing_frames),
                "missingFrames": missing_frames,
                "familyMissingFrames": family_missing,
            }
        )

    family_coverage = {
        family: {
            "expectedVariants": len(variants),
            "completeVariants": complete,
            "incompleteVariants": len(variants) - complete,
            "topMissingFrames": [
                {"frame": frame, "count": count}
                for frame, count in missing_counter.most_common(12)
            ],
        }
        for family, complete in family_complete_counts.items()
        for missing_counter in [family_missing_counter[family]]
    }

    return {
        "root": rel(authored_root),
        "expectedVariantCount": len(variants),
        "expectedFrameCountPerVariant": len(frames),
        "completeVariants": complete_variants,
        "incompleteVariants": len(variants) - complete_variants,
        "familyCoverage": family_coverage,
        "variants": variant_reports,
    }


def source_board_report(manifest_path: Path, incoming_root: Path) -> dict[str, Any]:
    manifest = load_manifest(manifest_path)
    source_results, source_errors = audit_source_boards(manifest, incoming_root)
    supporting_found, supporting_errors = audit_supporting_inputs(incoming_root)
    source_by_species: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for result in source_results:
        source_by_species[result["species"]].append(result)

    return {
        "manifest": rel(manifest_path),
        "incomingRoot": rel(incoming_root),
        "sourceBoardsFound": len(source_results),
        "sourceBoardsExpected": len(EXPECTED_SPECIES) * len(EXPECTED_AGES),
        "supportingInputsFound": len(supporting_found),
        "errors": source_errors + supporting_errors,
        "sourceBoardsBySpecies": {
            species: sorted(rows, key=lambda row: row["age_stage"])
            for species, rows in sorted(source_by_species.items())
        },
        "supportingInputs": supporting_found,
    }


def runtime_required_report(manifest_path: Path, incoming_root: Path, runtime_root: Path) -> dict[str, Any]:
    manifest = load_manifest(manifest_path)
    source_results, source_errors = audit_source_boards(manifest, incoming_root)
    supporting_found, supporting_errors = audit_supporting_inputs(incoming_root)
    runtime_results, runtime_errors, variant_dirs_found, frames_found = audit_runtime(runtime_root)
    contract = build_contract_summary(
        source_results,
        supporting_found,
        runtime_results,
        source_errors + supporting_errors + runtime_errors,
        variant_dirs_found,
        frames_found,
    )

    species_counts: dict[str, dict[str, int]] = {
        species: {"variantDirs": 0, "frames": 0}
        for species in EXPECTED_SPECIES
    }
    for row in runtime_results:
        species = str(row["species"])
        species_counts[species]["variantDirs"] += 1
        species_counts[species]["frames"] += sum(int(count) for count in row["animations"].values())

    return {
        "root": rel(runtime_root),
        "contract": contract["contract"],
        "counts": contract["counts"],
        "errorCount": len(contract["errors"]),
        "errors": contract["errors"],
        "speciesCounts": species_counts,
    }


def optional_family_report(
    manifest_path: Path,
    runtime_root: Path,
    authored_root: Path,
    prop_anchor_path: Path,
) -> dict[str, Any]:
    optional = audit_optional_readiness(manifest_path, runtime_root, authored_root, prop_anchor_path)
    prop_anchor_rows = optional.get("prop_anchor_rows", 0)
    families = json.loads(manifest_path.read_text(encoding="utf-8"))
    return {
        "manifest": rel(manifest_path),
        "runtimeRoot": rel(runtime_root),
        "authoredRoot": rel(authored_root),
        "propAnchorMetadata": rel(prop_anchor_path),
        "propAnchorRows": prop_anchor_rows,
        "passed": optional["passed"],
        "targetCount": optional["target_count"],
        "statusCounts": optional["status_counts"],
        "familyCounts": optional["family_counts"],
        "families": families,
        "targets": optional["targets"],
        "pendingGenerationTargets": optional["pending_generation_targets"],
        "errors": optional["errors"],
    }


def build_priority_queues(authored: dict[str, Any], optional: dict[str, Any]) -> dict[str, Any]:
    missing_care = []
    missing_expression = []
    needs_visual_review = []

    for variant in authored["variants"]:
        care_missing = variant["familyMissingFrames"].get("care", [])
        expression_missing = variant["familyMissingFrames"].get("expression", [])
        if care_missing:
            missing_care.append(
                {
                    "species": variant["species"],
                    "age": variant["age"],
                    "gender": variant["gender"],
                    "color": variant["color"],
                    "missingFrameCount": len(care_missing),
                    "missingFrames": care_missing,
                    "path": variant["path"],
                }
            )
        if expression_missing:
            missing_expression.append(
                {
                    "species": variant["species"],
                    "age": variant["age"],
                    "gender": variant["gender"],
                    "color": variant["color"],
                    "missingFrameCount": len(expression_missing),
                    "missingFrames": expression_missing,
                    "path": variant["path"],
                }
            )

        if variant["missingFrameCount"]:
            needs_visual_review.append(
                {
                    "reason": "authored_verified_incomplete",
                    "species": variant["species"],
                    "age": variant["age"],
                    "gender": variant["gender"],
                    "color": variant["color"],
                    "missingFrameCount": variant["missingFrameCount"],
                    "path": variant["path"],
                }
            )

    optional_fallback_only = [
        target for target in optional["targets"]
        if target["status"] == "runtime_fallback_only"
    ]
    runtime_only_optional = [
        target for target in optional["targets"]
        if target["status"] == "runtime_only_complete"
    ]

    for target in runtime_only_optional:
        needs_visual_review.append(
            {
                "reason": "runtime_only_optional_needs_source_provenance",
                "species": target["species"],
                "age": target["age"],
                "gender": target["gender"],
                "color": target["color"],
                "family": target["family"],
                "runtimeFrameCount": target["runtime_frame_count"],
            }
        )

    sort_key = lambda item: (
        item.get("species", ""),
        item.get("age", ""),
        item.get("gender", ""),
        item.get("color", ""),
        item.get("family", ""),
    )
    return {
        "missing_authored_care": sorted(missing_care, key=sort_key),
        "missing_authored_expression": sorted(missing_expression, key=sort_key),
        "optional_fallback_only": sorted(optional_fallback_only, key=sort_key),
        "runtime_only_optional": sorted(runtime_only_optional, key=sort_key),
        "needs_visual_review": sorted(needs_visual_review, key=sort_key),
    }


def summarize_queues(priority_queues: dict[str, list[dict[str, Any]]]) -> dict[str, int]:
    return {name: len(items) for name, items in priority_queues.items()}


def write_markdown(report: dict[str, Any], output: Path) -> None:
    queues = report["priorityQueues"]
    authored = report["authoredVerified"]
    runtime = report["runtimeRequired"]
    optional = report["optionalFamilies"]
    source = report["sourceBoards"]

    lines = [
        "# Visual Source-Of-Truth Recovery Report",
        "",
        f"- Generated at UTC: `{report['generatedAtUtc']}`",
        f"- Mutation mode: `{report['mutationMode']}`",
        f"- Runtime root: `{runtime['root']}`",
        f"- Authored verified root: `{authored['root']}`",
        f"- Incoming/source root: `{source['incomingRoot']}`",
        "",
        "## Summary",
        "",
        "| Area | Result |",
        "| --- | ---: |",
        f"| Runtime variant dirs | {runtime['counts']['runtime_variant_dirs_found']} / {runtime['counts']['runtime_variant_dirs_expected']} |",
        f"| Runtime frames | {runtime['counts']['runtime_frames_found']} / {runtime['counts']['runtime_frames_expected']} |",
        f"| Runtime contract errors | {runtime['errorCount']} |",
        f"| Source boards | {source['sourceBoardsFound']} / {source['sourceBoardsExpected']} |",
        f"| Supporting source inputs | {source['supportingInputsFound']} |",
        f"| Authored complete variants | {authored['completeVariants']} / {authored['expectedVariantCount']} |",
        f"| Authored incomplete variants | {authored['incompleteVariants']} |",
        f"| Optional targets | {optional['targetCount']} |",
        f"| Optional fallback-only | {optional['statusCounts'].get('runtime_fallback_only', 0)} |",
        f"| Runtime-only optional | {optional['statusCounts'].get('runtime_only_complete', 0)} |",
        f"| Invalid optional art | {optional['statusCounts'].get('invalid_optional_art', 0)} |",
        "",
        "## Priority Queues",
        "",
        "| Queue | Count |",
        "| --- | ---: |",
    ]

    for name, count in report["priorityQueueCounts"].items():
        lines.append(f"| `{name}` | {count} |")

    lines.extend(
        [
            "",
            "## Authored Family Coverage",
            "",
            "| Family | Complete | Incomplete | Top missing frames |",
            "| --- | ---: | ---: | --- |",
        ]
    )
    for family, row in authored["familyCoverage"].items():
        top_missing = ", ".join(f"{item['frame']}={item['count']}" for item in row["topMissingFrames"][:6])
        lines.append(
            f"| `{family}` | {row['completeVariants']} | {row['incompleteVariants']} | {top_missing or 'none'} |"
        )

    lines.extend(
        [
            "",
            "## Optional Family Counts",
            "",
            "| Family | Status counts |",
            "| --- | --- |",
        ]
    )
    for family, counts in optional["familyCounts"].items():
        count_text = ", ".join(f"{key}={value}" for key, value in sorted(counts.items()))
        lines.append(f"| `{family}` | {count_text} |")

    lines.extend(
        [
            "",
            "## First Optional Fallback Targets",
            "",
        ]
    )
    for target in queues["optional_fallback_only"][:30]:
        lines.append(
            "- `{species}|{age}|{gender}|{color}|{family}` -> fallback `{fallback}`".format(
                species=target["species"],
                age=target["age"],
                gender=target["gender"],
                color=target["color"],
                family=target["family"],
                fallback=" / ".join(target["fallback_animations"]),
            )
        )

    lines.extend(
        [
            "",
            "## First Missing Authored Care Targets",
            "",
        ]
    )
    for target in queues["missing_authored_care"][:30]:
        lines.append(
            f"- `{target['species']}|{target['age']}|{target['gender']}|{target['color']}` missing {target['missingFrameCount']} care frame(s)"
        )

    lines.extend(
        [
            "",
            "## First Missing Authored Expression Targets",
            "",
        ]
    )
    for target in queues["missing_authored_expression"][:30]:
        lines.append(
            f"- `{target['species']}|{target['age']}|{target['gender']}|{target['color']}` missing {target['missingFrameCount']} expression frame(s)"
        )

    lines.extend(
        [
            "",
            "## Source Board Errors",
            "",
        ]
    )
    if source["errors"]:
        lines.extend(f"- {error}" for error in source["errors"])
    else:
        lines.append("- none")

    lines.extend(
        [
            "",
            "## Runtime Contract Errors",
            "",
        ]
    )
    if runtime["errors"]:
        lines.extend(f"- {error}" for error in runtime["errors"])
    else:
        lines.append("- none")

    lines.extend(
        [
            "",
            "## Mutation Statement",
            "",
            "This report is read-only. It does not mutate PNGs, source boards, runtime folders, prop anchors, or content manifests.",
        ]
    )

    output.write_text("\n".join(lines) + "\n", encoding="utf-8")


def build_report(
    runtime_root: Path,
    authored_root: Path,
    incoming_root: Path,
    manifest_path: Path,
    optional_manifest_path: Path,
    prop_anchor_path: Path,
) -> dict[str, Any]:
    runtime = runtime_required_report(manifest_path, incoming_root, runtime_root)
    authored = authored_coverage(authored_root)
    source_boards = source_board_report(manifest_path, incoming_root)
    optional = optional_family_report(optional_manifest_path, runtime_root, authored_root, prop_anchor_path)
    queues = build_priority_queues(authored, optional)
    return {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "mutationMode": "report_only_no_asset_mutation",
        "runtimeRequired": runtime,
        "authoredVerified": authored,
        "sourceBoards": source_boards,
        "optionalFamilies": optional,
        "priorityQueues": queues,
        "priorityQueueCounts": summarize_queues(queues),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME)
    parser.add_argument("--authored-root", type=Path, default=DEFAULT_AUTHORED)
    parser.add_argument("--incoming-root", type=Path, default=DEFAULT_INCOMING)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--optional-manifest", type=Path, default=DEFAULT_OPTIONAL_MANIFEST)
    parser.add_argument("--prop-anchor-metadata", type=Path, default=DEFAULT_PROP_ANCHORS)
    args = parser.parse_args()

    args.output_root.mkdir(parents=True, exist_ok=True)
    report = build_report(
        args.runtime_root,
        args.authored_root,
        args.incoming_root,
        args.manifest,
        args.optional_manifest,
        args.prop_anchor_metadata,
    )

    json_path = args.output_root / "visual-source-of-truth.json"
    markdown_path = args.output_root / "visual-source-of-truth.md"
    json_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    write_markdown(report, markdown_path)

    print(json_path)
    print(markdown_path)
    print(
        json.dumps(
            {
                "runtime_errors": report["runtimeRequired"]["errorCount"],
                "source_errors": len(report["sourceBoards"]["errors"]),
                "authored_incomplete": report["authoredVerified"]["incompleteVariants"],
                "optional_fallback_only": report["priorityQueueCounts"]["optional_fallback_only"],
                "runtime_only_optional": report["priorityQueueCounts"]["runtime_only_optional"],
                "mutation_mode": report["mutationMode"],
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
