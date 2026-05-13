#!/usr/bin/env python3
"""Report likely visual-quality issues in Wevito runtime sprite rows.

This is intentionally read-only. It scans runtime PNG rows and flags the kinds
of issues that are visible in-game: static animation rows, cropped/edge-touching
silhouettes, empty frames, sparse silhouettes, and mixed frame geometry.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageChops, ImageStat


FRAME_RE = re.compile(r"^(?P<animation>.+)_(?P<frame>\d+)\.png$", re.IGNORECASE)


@dataclass(frozen=True)
class FrameMetrics:
    path: str
    frame_id: str
    sha256: str
    width: int
    height: int
    bbox: tuple[int, int, int, int] | None
    alpha_pixels: int
    coverage: float
    edge_touch: bool
    edge_sides: list[str]


@dataclass(frozen=True)
class RowFinding:
    severity: str
    issue: str
    species: str
    age: str
    gender: str
    color: str
    animation: str
    detail: str
    frames: list[str]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def iter_sprite_rows(root: Path) -> Iterable[tuple[tuple[str, str, str, str, str], list[Path]]]:
    groups: dict[tuple[str, str, str, str, str], list[Path]] = {}
    for path in sorted(root.glob("*/*/*/*/*.png")):
        match = FRAME_RE.match(path.name)
        if match is None:
            continue

        species, age, gender, color = path.relative_to(root).parts[:4]
        key = (species, age, gender, color, match.group("animation"))
        groups.setdefault(key, []).append(path)

    for key, paths in sorted(groups.items()):
        yield key, sorted(paths, key=lambda path: path.name)


def frame_metrics(path: Path, root: Path) -> FrameMetrics:
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        alpha = rgba.getchannel("A")
        bbox = alpha.getbbox()
        alpha_pixels = 0
        if bbox is not None:
            alpha_pixels = sum(1 for value in alpha.tobytes() if value > 0)
        width, height = rgba.size
        coverage = alpha_pixels / max(1, width * height)
        edge_sides: list[str] = []
        if bbox is not None:
            left, top, right, bottom = bbox
            if left <= 0:
                edge_sides.append("left")
            if top <= 0:
                edge_sides.append("top")
            if right >= width:
                edge_sides.append("right")
            if bottom >= height:
                edge_sides.append("bottom")

    return FrameMetrics(
        path=str(path.relative_to(root)).replace("\\", "/"),
        frame_id=path.stem,
        sha256=sha256(path),
        width=width,
        height=height,
        bbox=bbox,
        alpha_pixels=alpha_pixels,
        coverage=round(coverage, 6),
        edge_touch=bool(edge_sides),
        edge_sides=edge_sides,
    )


def thumbnail_diff(a: Path, b: Path) -> float:
    with Image.open(a) as left, Image.open(b) as right:
        left_thumb = left.convert("RGBA").resize((24, 24), Image.Resampling.NEAREST)
        right_thumb = right.convert("RGBA").resize((24, 24), Image.Resampling.NEAREST)
        diff = ImageChops.difference(left_thumb, right_thumb)
        stat = ImageStat.Stat(diff)
        return float(sum(stat.mean) / len(stat.mean))


def bbox_jitter(metrics: list[FrameMetrics]) -> float:
    boxes = [metric.bbox for metric in metrics if metric.bbox is not None]
    if len(boxes) <= 1:
        return 0.0

    centers = [((box[0] + box[2]) / 2.0, (box[1] + box[3]) / 2.0, box[2] - box[0], box[3] - box[1]) for box in boxes]
    averages = [sum(values[index] for values in centers) / len(centers) for index in range(4)]
    variances = [
        sum((values[index] - averages[index]) ** 2 for values in centers) / len(centers)
        for index in range(4)
    ]
    return math.sqrt(sum(variances))


def analyze_row(root: Path, key: tuple[str, str, str, str, str], paths: list[Path]) -> tuple[list[FrameMetrics], list[RowFinding]]:
    species, age, gender, color, animation = key
    metrics = [frame_metrics(path, root) for path in paths]
    findings: list[RowFinding] = []
    dimensions = {(metric.width, metric.height) for metric in metrics}
    unique_hash_count = len({metric.sha256 for metric in metrics})
    edge_metrics = [metric for metric in metrics if metric.edge_touch]
    edge_frames = [metric.frame_id for metric in edge_metrics]
    empty_frames = [metric.frame_id for metric in metrics if metric.alpha_pixels == 0]
    sparse_frames = [metric.frame_id for metric in metrics if 0 < metric.coverage < 0.012]
    diffs = [thumbnail_diff(paths[index], paths[index + 1]) for index in range(len(paths) - 1)]
    mean_diff = sum(diffs) / len(diffs) if diffs else 0.0
    jitter = bbox_jitter(metrics)
    row_id = (species, age, gender, color, animation)

    def add(severity: str, issue: str, detail: str, frames: list[str]) -> None:
        findings.append(RowFinding(severity, issue, *row_id, detail, frames))

    if len(dimensions) > 1:
        add("critical", "mixed_geometry", f"row has {len(dimensions)} frame dimensions: {sorted(dimensions)}", [m.frame_id for m in metrics])
    if empty_frames:
        add("critical", "empty_frame", f"{len(empty_frames)} frame(s) have no visible alpha", empty_frames)
    if edge_frames:
        touched_sides = sorted({side for metric in edge_metrics for side in metric.edge_sides})
        severe_sides = [side for side in touched_sides if side in {"left", "top", "right"}]
        severity = "high" if severe_sides and len(edge_frames) >= 2 else "medium"
        add(severity, "edge_touch", f"{len(edge_frames)} frame(s) touch canvas edge(s): {', '.join(touched_sides)}", edge_frames)
    if sparse_frames:
        add("medium", "sparse_frame", f"{len(sparse_frames)} frame(s) have unusually low visible coverage", sparse_frames)
    if len(paths) > 1 and unique_hash_count <= 1:
        add("high", "static_duplicate_row", "all frames have identical hashes; animation is visually static", [m.frame_id for m in metrics])
    elif len(paths) > 2 and mean_diff < 1.0 and jitter < 1.0:
        add("medium", "low_motion_row", f"very low adjacent-frame motion: mean_pixel_diff={mean_diff:.2f}, bbox_jitter={jitter:.2f}", [m.frame_id for m in metrics])

    return metrics, findings


def write_markdown(path: Path, payload: dict) -> None:
    findings = payload["findings"]
    by_species: dict[str, int] = {}
    by_issue: dict[str, int] = {}
    for finding in findings:
        by_species[finding["species"]] = by_species.get(finding["species"], 0) + 1
        by_issue[finding["issue"]] = by_issue.get(finding["issue"], 0) + 1

    lines = [
        "# Sprite Visual Quality Audit",
        "",
        f"- generated_at: `{payload['generated_at_utc']}`",
        f"- runtime_root: `{payload['runtime_root']}`",
        f"- rows_scanned: `{payload['summary']['rows_scanned']}`",
        f"- frames_scanned: `{payload['summary']['frames_scanned']}`",
        f"- findings: `{len(findings)}`",
        "",
        "## Findings By Issue",
        "",
    ]
    for issue, count in sorted(by_issue.items(), key=lambda item: (-item[1], item[0])):
        lines.append(f"- `{issue}`: {count}")

    lines.extend(["", "## Findings By Species", ""])
    for species, count in sorted(by_species.items(), key=lambda item: (-item[1], item[0])):
        lines.append(f"- `{species}`: {count}")

    lines.extend(["", "## Top Findings", ""])
    for finding in findings[:80]:
        row = f"{finding['species']} / {finding['age']} / {finding['gender']} / {finding['color']} / {finding['animation']}"
        frames = ", ".join(finding["frames"][:8])
        lines.append(f"- `{finding['severity']}` `{finding['issue']}` `{row}`: {finding['detail']} ({frames})")

    if len(findings) > 80:
        lines.append("")
        lines.append(f"_Only the first 80 findings are shown here. See JSON for all {len(findings)} findings._")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime-root", default="sprites_runtime", help="Runtime sprite root to scan.")
    parser.add_argument("--output-root", required=True, help="Folder for JSON and Markdown outputs.")
    args = parser.parse_args()

    runtime_root = Path(args.runtime_root).resolve()
    output_root = Path(args.output_root)
    output_root.mkdir(parents=True, exist_ok=True)

    all_findings: list[RowFinding] = []
    rows_scanned = 0
    frames_scanned = 0
    for key, paths in iter_sprite_rows(runtime_root):
        rows_scanned += 1
        frames_scanned += len(paths)
        _, findings = analyze_row(runtime_root, key, paths)
        all_findings.extend(findings)

    severity_order = {"critical": 0, "high": 1, "medium": 2, "low": 3}
    all_findings.sort(key=lambda finding: (
        severity_order.get(finding.severity, 99),
        finding.species,
        finding.age,
        finding.gender,
        finding.color,
        finding.animation,
        finding.issue,
    ))

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "runtime_root": str(runtime_root),
        "summary": {
            "rows_scanned": rows_scanned,
            "frames_scanned": frames_scanned,
            "findings": len(all_findings),
        },
        "findings": [asdict(finding) for finding in all_findings],
    }

    json_path = output_root / "sprite-visual-quality.json"
    md_path = output_root / "sprite-visual-quality.md"
    json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(md_path, payload)
    print(json.dumps(payload["summary"], indent=2))
    print(md_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
