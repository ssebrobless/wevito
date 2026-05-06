#!/usr/bin/env python3
"""
Audit optional animation readiness without pretending fallbacks are finished art.

The base sprite contract proves the required animations. This audit tracks the
expanded families that the runtime can play through fallbacks until real,
high-confidence source art is authored.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any

from audit_sprite_contract import (
    EXPECTED_AGES,
    EXPECTED_COLORS,
    EXPECTED_GENDERS,
    EXPECTED_SPECIES,
    OPTIONAL_EXPANDED_ANIMATIONS,
    png_dimensions,
)


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "vnext" / "content" / "optional_animation_families.json"
DEFAULT_RUNTIME = ROOT / "sprites_runtime"
DEFAULT_AUTHORED = ROOT / "sprites_authored_verified"
DEFAULT_ARTIFACT_ROOT = ROOT / "vnext" / "artifacts" / "workflow-runs"
DEFAULT_PROP_ANCHORS = ROOT / "sprites_runtime" / "_metadata" / "prop_anchors.json"
BALL_PROP_ANIMATION_FAMILIES = {
    "drink",
    "play_ball",
    "hold_ball",
    "carry_ball_walk",
    "carry_ball_run",
    "pickup_ball",
    "drop_ball",
}

RUNTIME_FALLBACKS = {
    "drink": ["eat", "idle"],
    "play_ball": ["happy", "walk", "idle"],
    "hold_ball": ["happy", "idle"],
    "carry_ball_walk": ["walk", "idle"],
    "carry_ball_run": ["walk", "idle"],
    "pickup_ball": ["happy", "idle"],
    "drop_ball": ["happy", "idle"],
}


@dataclass(frozen=True)
class OptionalTarget:
    species: str
    age: str
    gender: str
    color: str
    family: str

    @property
    def target_id(self) -> str:
        return f"{self.species}|{self.age}|{self.gender}|{self.color}|{self.family}"


def load_manifest(path: Path) -> list[dict[str, Any]]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, list):
        raise ValueError(f"{path} must contain a JSON array.")
    return data


def load_prop_anchor_metadata(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError(f"{path} must contain a JSON object.")
    return data


def frames_for(root: Path, target: OptionalTarget) -> list[Path]:
    directory = root / target.species / target.age / target.gender / target.color
    if not directory.exists():
        return []
    return sorted(
        path
        for path in directory.glob(f"{target.family}_*.png")
        if not path.name.endswith(".import")
    )


def validate_frames(frames: list[Path], expected_count: int) -> list[str]:
    errors: list[str] = []
    if not frames:
        return errors
    if len(frames) != expected_count:
        errors.append(f"expected {expected_count} frame(s), found {len(frames)}")
        return errors
    for frame in frames:
        width, height, color_type = png_dimensions(frame)
        if width <= 0 or height <= 0:
            errors.append(f"{frame} has invalid dimensions {width}x{height}")
        if width > 192 or height > 160:
            errors.append(f"{frame} exceeds optional canvas limit, found {width}x{height}")
        if color_type not in {4, 6}:
            errors.append(f"{frame} lost alpha-capable PNG type, found color type {color_type}")
    return errors


def prop_anchor_status(target: OptionalTarget, metadata: dict[str, Any]) -> tuple[bool, str]:
    if target.family not in BALL_PROP_ANIMATION_FAMILIES:
        return False, ""
    anchors = metadata.get("anchors", {})
    if not isinstance(anchors, dict):
        return False, "prop anchor metadata has no anchors object"
    key = f"{target.species}/{target.age}/{target.gender}/{target.color}"
    row = anchors.get(key)
    if not isinstance(row, dict):
        return False, f"missing prop anchor row {key}"
    if target.family == "drink":
        drink = row.get("drink")
        if not isinstance(drink, dict):
            return False, f"missing drink payload for {key}"
        merged = drink.get("default")
        if not isinstance(merged, dict):
            return False, f"missing drink default payload for {key}"
        norm = merged.get("anchor_norm", {})
        if not isinstance(norm, dict):
            return False, f"missing drink anchor_norm for {target.target_id}"
        x = float(norm.get("x", -1.0))
        y = float(norm.get("y", -1.0))
        scale = float(merged.get("scale", 0.0))
        if not (0.0 <= x <= 1.0 and 0.0 <= y <= 1.0 and 0.18 <= scale <= 0.42):
            return False, f"invalid drink anchor values for {target.target_id}: anchor={norm} scale={scale}"
        return True, key

    ball = row.get("ball")
    if not isinstance(ball, dict):
        return False, f"missing ball payload for {key}"
    default = ball.get("default")
    families = ball.get("families")
    if not isinstance(default, dict) or not isinstance(families, dict):
        return False, f"incomplete ball payload for {key}"
    family = families.get(target.family)
    if not isinstance(family, dict):
        return False, f"missing ball family {target.family} for {key}"
    merged = dict(default)
    merged.update(family)
    norm = merged.get("anchor_norm", {})
    if not isinstance(norm, dict):
        return False, f"missing anchor_norm for {target.target_id}"
    x = float(norm.get("x", -1.0))
    y = float(norm.get("y", -1.0))
    scale = float(merged.get("scale", 0.0))
    if not (0.0 <= x <= 1.0 and 0.0 <= y <= 1.0 and 0.25 <= scale <= 1.25):
        return False, f"invalid prop anchor values for {target.target_id}: anchor={norm} scale={scale}"
    return True, key


def build_target_report(
    target: OptionalTarget,
    manifest_by_family: dict[str, dict[str, Any]],
    runtime_root: Path,
    authored_root: Path,
    prop_anchor_metadata: dict[str, Any],
) -> dict[str, Any]:
    expected_count = int(manifest_by_family[target.family]["frameCount"])
    runtime_frames = frames_for(runtime_root, target)
    authored_frames = frames_for(authored_root, target)
    runtime_errors = validate_frames(runtime_frames, expected_count)
    authored_errors = validate_frames(authored_frames, expected_count)

    if authored_errors or runtime_errors:
        status = "invalid_optional_art"
        prop_status_detail = ""
    elif len(authored_frames) == expected_count:
        status = "authored_complete"
        prop_status_detail = ""
    elif len(runtime_frames) == expected_count:
        status = "runtime_only_complete"
        prop_status_detail = ""
    else:
        prop_supported, prop_status_detail = prop_anchor_status(target, prop_anchor_metadata)
        status = "runtime_prop_anchor_supported" if prop_supported else "runtime_fallback_only"

    manifest_entry = manifest_by_family[target.family]
    return {
        "target_id": target.target_id,
        "species": target.species,
        "age": target.age,
        "gender": target.gender,
        "color": target.color,
        "family": target.family,
        "expected_frame_count": expected_count,
        "authored_frame_count": len(authored_frames),
        "runtime_frame_count": len(runtime_frames),
        "status": status,
        "prop_anchor_supported": status == "runtime_prop_anchor_supported",
        "prop_anchor_detail": prop_status_detail,
        "fallback_animations": manifest_entry.get("fallbackAnimations", RUNTIME_FALLBACKS[target.family]),
        "prop_requirement": manifest_entry.get("propRequirement", ""),
        "quality_gate": manifest_entry.get("qualityGate", ""),
        "errors": authored_errors + runtime_errors,
    }


def audit(manifest_path: Path, runtime_root: Path, authored_root: Path, prop_anchor_path: Path) -> dict[str, Any]:
    manifest = load_manifest(manifest_path)
    prop_anchor_metadata = load_prop_anchor_metadata(prop_anchor_path)
    manifest_by_family = {str(entry["id"]): entry for entry in manifest}
    errors: list[str] = []

    expected_families = set(OPTIONAL_EXPANDED_ANIMATIONS)
    manifest_families = set(manifest_by_family)
    for missing in sorted(expected_families - manifest_families):
        errors.append(f"Missing optional family manifest entry: {missing}")
    for extra in sorted(manifest_families - expected_families):
        errors.append(f"Unexpected optional family manifest entry: {extra}")

    for family, expected_count in OPTIONAL_EXPANDED_ANIMATIONS.items():
        entry = manifest_by_family.get(family)
        if not entry:
            continue
        if int(entry.get("frameCount", -1)) != expected_count:
            errors.append(f"{family} manifest frameCount must be {expected_count}.")
        fallback = entry.get("fallbackAnimations", [])
        if not isinstance(fallback, list) or not fallback:
            errors.append(f"{family} must define at least one fallback animation.")

    target_reports: list[dict[str, Any]] = []
    if not errors:
        for species in EXPECTED_SPECIES:
            for age in EXPECTED_AGES:
                for gender in EXPECTED_GENDERS:
                    for color in EXPECTED_COLORS:
                        for family in OPTIONAL_EXPANDED_ANIMATIONS:
                            target = OptionalTarget(species, age, gender, color, family)
                            report = build_target_report(target, manifest_by_family, runtime_root, authored_root, prop_anchor_metadata)
                            target_reports.append(report)
                            errors.extend(f"{target.target_id}: {error}" for error in report["errors"])

    status_counts = Counter(report["status"] for report in target_reports)
    family_counts: dict[str, Counter[str]] = {}
    for report in target_reports:
        family_counts.setdefault(report["family"], Counter())
        family_counts[report["family"]][report["status"]] += 1

    pending = [
        report
        for report in target_reports
        if report["status"] == "runtime_fallback_only"
    ]

    return {
        "audited_at": datetime.now().isoformat(timespec="seconds"),
        "passed": len(errors) == 0,
        "manifest": str(manifest_path),
        "runtime_root": str(runtime_root),
        "authored_root": str(authored_root),
        "prop_anchor_metadata": str(prop_anchor_path),
        "prop_anchor_rows": len(prop_anchor_metadata.get("anchors", {})) if isinstance(prop_anchor_metadata.get("anchors", {}), dict) else 0,
        "target_count": len(target_reports),
        "complete_authored_count": status_counts.get("authored_complete", 0),
        "complete_runtime_only_count": status_counts.get("runtime_only_complete", 0),
        "runtime_prop_anchor_supported_count": status_counts.get("runtime_prop_anchor_supported", 0),
        "fallback_only_count": status_counts.get("runtime_fallback_only", 0),
        "invalid_optional_art_count": status_counts.get("invalid_optional_art", 0),
        "error_count": len(errors),
        "errors": errors,
        "status_counts": dict(status_counts),
        "family_counts": {family: dict(counts) for family, counts in sorted(family_counts.items())},
        "pending_generation_targets": pending,
        "targets": target_reports,
    }


def write_markdown(summary: dict[str, Any], path: Path) -> None:
    lines = [
        "# Optional Animation Readiness Audit",
        "",
        f"- Passed: `{summary['passed']}`",
        f"- Targets: `{summary['target_count']}`",
        f"- Authored complete: `{summary['complete_authored_count']}`",
        f"- Runtime-only complete: `{summary['complete_runtime_only_count']}`",
        f"- Runtime prop-anchor supported: `{summary['runtime_prop_anchor_supported_count']}`",
        f"- Fallback-only pending: `{summary['fallback_only_count']}`",
        f"- Invalid optional art: `{summary['invalid_optional_art_count']}`",
        f"- Errors: `{summary['error_count']}`",
        f"- Prop-anchor rows: `{summary['prop_anchor_rows']}`",
        "",
        "## Family Counts",
        "",
    ]
    for family, counts in summary["family_counts"].items():
        count_text = ", ".join(f"{key}={value}" for key, value in sorted(counts.items()))
        lines.append(f"- `{family}`: {count_text}")
    lines.extend(["", "## Errors", ""])
    if summary["errors"]:
        lines.extend(f"- {error}" for error in summary["errors"])
    else:
        lines.append("- none")
    lines.extend(
        [
            "",
            "## First Pending Targets",
            "",
        ]
    )
    for target in summary["pending_generation_targets"][:40]:
        lines.append(
            "- `{species}|{age}|{gender}|{color}|{family}` -> fallback `{fallback}`; gate: {gate}".format(
                species=target["species"],
                age=target["age"],
                gender=target["gender"],
                color=target["color"],
                family=target["family"],
                fallback=" / ".join(target["fallback_animations"]),
                gate=target["quality_gate"],
            )
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME)
    parser.add_argument("--authored-root", type=Path, default=DEFAULT_AUTHORED)
    parser.add_argument("--prop-anchor-metadata", type=Path, default=DEFAULT_PROP_ANCHORS)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--markdown", type=Path)
    args = parser.parse_args()

    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output = args.output or DEFAULT_ARTIFACT_ROOT / f"{timestamp}-optional-animation-readiness.json"
    markdown = args.markdown or output.with_suffix(".md")
    output.parent.mkdir(parents=True, exist_ok=True)
    markdown.parent.mkdir(parents=True, exist_ok=True)

    summary = audit(args.manifest, args.runtime_root, args.authored_root, args.prop_anchor_metadata)
    output.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    write_markdown(summary, markdown)

    print(output)
    print(markdown)
    print(
        json.dumps(
            {
                "passed": summary["passed"],
                "target_count": summary["target_count"],
                "authored_complete": summary["complete_authored_count"],
                "runtime_prop_anchor_supported": summary["runtime_prop_anchor_supported_count"],
                "fallback_only": summary["fallback_only_count"],
                "invalid_optional_art": summary["invalid_optional_art_count"],
                "error_count": summary["error_count"],
            },
            indent=2,
        )
    )
    return 0 if summary["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
