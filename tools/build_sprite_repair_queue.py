#!/usr/bin/env python3
"""Build a prioritized sprite repair queue from the C-PHASE 127 visual QA manifest."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
SENIOR = "senior"
AGE_ORDER = {"baby": 0, "teen": 1, "adult": 2}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3}
DIRECT_VISIBLE_FAMILIES = {"idle", "walk", "eat", "drink", "sleep", "groom", "bathe", "play", "sad", "happy"}
ACTION_CRITICAL_FAMILIES = {"walk", "eat", "drink", "groom"}


@dataclass
class RepairIssue:
    colorVariant: str
    animationFamily: str
    severity: str
    tags: list[str]
    warnings: list[str]
    repairTool: str
    reason: str
    sourcePath: str | None = None
    capturePath: str | None = None


@dataclass
class RepairQueueRow:
    rowId: str
    speciesId: str
    lifeStage: str
    gender: str
    priority: str
    status: str
    issueCount: int
    colorsAffected: list[str]
    animationsAffected: list[str]
    recommendedTools: list[str]
    issues: list[RepairIssue]


@dataclass
class RepairQueueManifest:
    schemaVersion: str
    generatedAtUtc: str
    sourceManifestPath: str
    sourceManifestGeneratedAtUtc: str
    expectedFamilyCount: int
    rowCount: int
    priorityCounts: dict[str, int]
    tagCounts: dict[str, int]
    structuralAudit: dict[str, Any]
    rows: list[RepairQueueRow] = field(default_factory=list)


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def run_structural_audit(output_path: Path) -> dict[str, Any]:
    command = [
        sys.executable,
        str(ROOT / "tools" / "audit_sprite_contract.py"),
        "--output",
        str(output_path),
    ]
    result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True)
    summary: dict[str, Any] = {
        "command": " ".join(command),
        "exitCode": result.returncode,
        "stdout": result.stdout.strip(),
        "stderr": result.stderr.strip(),
        "outputPath": str(output_path),
    }
    if output_path.exists():
        try:
            payload = load_json(output_path)
            summary["errorCount"] = len(payload.get("errors", []))
            summary["counts"] = payload.get("counts", {})
        except Exception as exc:  # pragma: no cover - defensive evidence capture.
            summary["parseError"] = str(exc)
    return summary


def tool_for(species: str, tags: set[str], family: str) -> str:
    if "missing_frames" in tags:
        return "tools/generate_runtime_pose_sprites.py"
    if "identical_to_idle" in tags:
        species_tool = ROOT / "tools" / f"repair_{species}_motion_rows.py"
        if species_tool.exists():
            return f"tools/repair_{species}_motion_rows.py"
        return "tools/repair_readable_motion_rows.py"
    if tags & {"crop_detected", "box_background", "edge_pixel_touch"}:
        return "tools/repair_runtime_sprite_canvas_padding.py"
    if "palette_drift" in tags:
        return "tools/propagate_authored_colors.py"
    if family in ACTION_CRITICAL_FAMILIES:
        return "tools/generate_runtime_pose_sprites.py"
    return "tools/repair_runtime_sprite_canvas_padding.py"


def severity_for(tags: set[str], family: str) -> str:
    if tags & {"solid_white", "solid_black"}:
        return "P0"
    if "identical_to_idle" in tags and family in {"walk", "eat", "drink"}:
        return "P1"
    if "missing_frames" in tags and family in ACTION_CRITICAL_FAMILIES:
        return "P1"
    if tags & {"crop_detected", "box_background", "palette_drift", "edge_pixel_touch"}:
        return "P2"
    return "P3"


def reason_for(tags: set[str], family: str) -> str:
    if tags & {"solid_white", "solid_black"}:
        return "Frame is effectively blank/solid and needs replacement before runtime use."
    if "identical_to_idle" in tags:
        return f"{family} reads as idle, so the visible action/motion is broken."
    if "missing_frames" in tags:
        return f"{family} has no direct runtime frames and is relying on fallback behavior."
    if "box_background" in tags:
        return "Frame appears to contain a leftover generated image box/background."
    if "crop_detected" in tags:
        return "Opaque pixels touch the canvas edge; the sprite may be cropped in motion."
    if "palette_drift" in tags:
        return "Opaque pixels drift outside the expected coarse species/color palette."
    return "Visual QA heuristic flagged this row for review."


def assert_tool_exists(tool: str) -> None:
    path = ROOT / tool
    if not path.exists():
        raise SystemExit(f"Repair queue references missing repair tool: {tool}")


def build_queue(manifest: dict[str, Any], manifest_path: Path, out_path: Path) -> RepairQueueManifest:
    rows = manifest.get("rows", [])
    species_ids = set(manifest.get("speciesIds", []))
    life_stages = [age for age in manifest.get("lifeStages", []) if age.lower() != SENIOR]
    genders = set(manifest.get("genders", []))
    expected_family_count = len(species_ids) * len(life_stages) * len(genders)

    groups: dict[tuple[str, str, str], list[RepairIssue]] = defaultdict(list)
    tag_counts: Counter[str] = Counter()

    for cell in rows:
        species = str(cell["speciesId"])
        age = str(cell["lifeStage"])
        gender = str(cell["gender"])
        if age.lower() == SENIOR:
            continue
        if species not in species_ids or age not in life_stages or gender not in genders:
            raise SystemExit(f"Manifest contains non-existent cell: {species}/{age}/{gender}")

        for animation in cell.get("animations", []):
            family = str(animation.get("animationFamily", ""))
            tags = set(animation.get("tags", []))
            if "missing_frames" in tags and family not in DIRECT_VISIBLE_FAMILIES:
                # Optional/propped families may be intentionally absent; C-PHASE 127 recorded
                # them as warnings only, so do not inflate the repair queue here.
                continue
            if not tags:
                continue

            priority = severity_for(tags, family)
            if priority == "P3":
                continue

            tool = tool_for(species, tags, family)
            assert_tool_exists(tool)
            ordered_tags = sorted(tags)
            tag_counts.update(ordered_tags)
            groups[(species, age, gender)].append(
                RepairIssue(
                    colorVariant=str(cell["colorVariant"]),
                    animationFamily=family,
                    severity=priority,
                    tags=ordered_tags,
                    warnings=list(animation.get("warnings", [])),
                    repairTool=tool,
                    reason=reason_for(tags, family),
                    sourcePath=animation.get("sourcePath"),
                    capturePath=animation.get("capturePath"),
                )
            )

    output_root = out_path.parent
    structural_audit_path = output_root / "sprite_contract.json"
    structural_audit = run_structural_audit(structural_audit_path)

    queue_rows: list[RepairQueueRow] = []
    for (species, age, gender), issues in groups.items():
        row_priority = min((issue.severity for issue in issues), key=lambda value: PRIORITY_ORDER[value])
        queue_rows.append(
            RepairQueueRow(
                rowId=f"{species}_{age}_{gender}",
                speciesId=species,
                lifeStage=age,
                gender=gender,
                priority=row_priority,
                status="queued",
                issueCount=len(issues),
                colorsAffected=sorted({issue.colorVariant for issue in issues}),
                animationsAffected=sorted({issue.animationFamily for issue in issues}),
                recommendedTools=sorted({issue.repairTool for issue in issues}),
                issues=sorted(
                    issues,
                    key=lambda issue: (
                        PRIORITY_ORDER[issue.severity],
                        issue.colorVariant,
                        issue.animationFamily,
                        ",".join(issue.tags),
                    ),
                ),
            )
        )

    queue_rows.sort(
        key=lambda row: (
            PRIORITY_ORDER[row.priority],
            row.speciesId,
            AGE_ORDER.get(row.lifeStage, 99),
            row.gender,
        )
    )

    p0_count = sum(1 for row in queue_rows if row.priority == "P0")
    if p0_count > 30:
        raise SystemExit(f"Stop gate: P0 count is {p0_count}, which is greater than 30.")

    priority_counts = Counter(row.priority for row in queue_rows)
    return RepairQueueManifest(
        schemaVersion="1.0",
        generatedAtUtc=datetime.now(timezone.utc).isoformat(),
        sourceManifestPath=str(manifest_path),
        sourceManifestGeneratedAtUtc=str(manifest.get("generatedAtUtc", "")),
        expectedFamilyCount=expected_family_count,
        rowCount=len(queue_rows),
        priorityCounts={key: priority_counts.get(key, 0) for key in ["P0", "P1", "P2", "P3"]},
        tagCounts=dict(sorted(tag_counts.items())),
        structuralAudit=structural_audit,
        rows=queue_rows,
    )


def write_markdown(queue: RepairQueueManifest, markdown_path: Path) -> None:
    lines = [
        "# C-PHASE 128 Sprite Repair Queue",
        "",
        f"- generated_at: `{queue.generatedAtUtc}`",
        f"- source_manifest: `{queue.sourceManifestPath}`",
        f"- expected_families: `{queue.expectedFamilyCount}`",
        f"- queued_rows: `{queue.rowCount}`",
        "",
        "## Priority Counts",
        "",
    ]
    for priority, count in queue.priorityCounts.items():
        lines.append(f"- `{priority}`: {count}")
    lines.extend(["", "## Tag Counts", ""])
    for tag, count in queue.tagCounts.items():
        lines.append(f"- `{tag}`: {count}")
    lines.extend(["", "## First 20 Rows", ""])
    for row in queue.rows[:20]:
        lines.append(
            f"- `{row.rowId}` `{row.priority}` issues={row.issueCount} "
            f"animations={','.join(row.animationsAffected)} tools={','.join(row.recommendedTools)}"
        )
    markdown_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--markdown", type=Path, default=None)
    args = parser.parse_args()

    manifest_path = args.manifest.resolve()
    out_path = args.out.resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)
    queue = build_queue(load_json(manifest_path), manifest_path, out_path)
    out_path.write_text(json.dumps(asdict(queue), indent=2), encoding="utf-8")
    write_markdown(queue, args.markdown.resolve() if args.markdown else out_path.with_suffix(".md"))
    print(json.dumps({"rows": queue.rowCount, "priorityCounts": queue.priorityCounts, "tagCounts": queue.tagCounts}, indent=2))


if __name__ == "__main__":
    main()
