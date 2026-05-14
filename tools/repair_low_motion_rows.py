#!/usr/bin/env python3
"""Add conservative life-motion to low-motion runtime rows.

This tool is intentionally limited to rows where small whole-sprite motion is
appropriate: idle breathing and happy bounce. It never repaints, scales, mirrors,
or generates animal pixels. It only shifts existing visible pixels upward inside
the existing transparent canvas.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RUNTIME_ROOT = ROOT / "sprites_runtime"
DEFAULT_AUDIT_JSON = ROOT / "vnext" / "artifacts" / "sprite-visual-quality-audit-20260513-after-goose-rat" / "sprite-visual-quality.json"
SUPPORTED_PATTERNS = {
    "idle": [0, -1, -2, -1],
    "happy": [0, -2, -4, -2],
}


@dataclass(frozen=True)
class FrameRecord:
    path: str
    dy_requested: int
    dy_applied: int
    before_sha256: str
    after_sha256: str
    changed: bool


@dataclass(frozen=True)
class RowRecord:
    row_id: str
    animation: str
    frame_count: int
    changed_count: int
    skipped_reason: str | None
    frames: list[FrameRecord]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def shifted_frame(image: Image.Image, requested_dy: int) -> tuple[Image.Image, int]:
    source = image.convert("RGBA")
    bbox = source.getbbox()
    if bbox is None:
        return source, 0

    _, top, _, _ = bbox
    applied_dy = requested_dy
    if requested_dy < 0:
        applied_dy = -min(abs(requested_dy), top)
    elif requested_dy > 0:
        bottom_margin = source.height - bbox[3]
        applied_dy = min(requested_dy, bottom_margin)

    if applied_dy == 0:
        return source.copy(), 0

    output = Image.new("RGBA", source.size, (0, 0, 0, 0))
    output.alpha_composite(source, (0, applied_dy))
    return output, applied_dy


def row_from_finding(finding: dict) -> tuple[str, str, str, str, str]:
    return (
        finding["species"],
        finding["age"],
        finding["gender"],
        finding["color"],
        finding["animation"],
    )


def repair_row(runtime_root: Path, backup_root: Path, row: tuple[str, str, str, str, str], apply: bool) -> RowRecord:
    species, age, gender, color, animation = row
    pattern = SUPPORTED_PATTERNS.get(animation)
    row_id = f"{species}/{age}/{gender}/{color}/{animation}"
    if pattern is None:
        return RowRecord(row_id, animation, 0, 0, "unsupported_animation", [])

    row_dir = runtime_root / species / age / gender / color
    paths = sorted(row_dir.glob(f"{animation}_*.png"))
    if len(paths) != len(pattern):
        return RowRecord(row_id, animation, len(paths), 0, f"expected_{len(pattern)}_frames", [])

    records: list[FrameRecord] = []
    for path, requested_dy in zip(paths, pattern):
        before_sha = sha256(path)
        with Image.open(path) as image:
            shifted, applied_dy = shifted_frame(image, requested_dy)

        changed = applied_dy != 0
        after_sha = before_sha
        if apply and changed:
            backup_path = backup_root / path.relative_to(runtime_root)
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(path, backup_path)
            shifted.save(path)
            after_sha = sha256(path)

        records.append(
            FrameRecord(
                path=str(path.relative_to(runtime_root)),
                dy_requested=requested_dy,
                dy_applied=applied_dy,
                before_sha256=before_sha,
                after_sha256=after_sha,
                changed=changed,
            )
        )

    return RowRecord(
        row_id=row_id,
        animation=animation,
        frame_count=len(paths),
        changed_count=sum(1 for record in records if record.changed),
        skipped_reason=None,
        frames=records,
    )


def load_rows(audit_json: Path, species_filter: set[str] | None) -> list[tuple[str, str, str, str, str]]:
    payload = json.loads(audit_json.read_text(encoding="utf-8"))
    rows = []
    for finding in payload["findings"]:
        if finding["issue"] != "low_motion_row":
            continue
        if species_filter and finding["species"] not in species_filter:
            continue
        row = row_from_finding(finding)
        if row[-1] in SUPPORTED_PATTERNS:
            rows.append(row)
    return sorted(set(rows))


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Low Motion Runtime Repair",
        "",
        f"- generated_at: `{payload['generated_at']}`",
        f"- dry_run: `{payload['dry_run']}`",
        f"- runtime_root: `{payload['runtime_root']}`",
        f"- audit_json: `{payload['audit_json']}`",
        f"- backup_root: `{payload['backup_root']}`",
        f"- rows: `{payload['row_count']}`",
        f"- changed_frames: `{payload['changed_frame_count']}`",
        "",
        "## Method",
        "",
        "Adds transparent-canvas whole-sprite motion only. No repainting, scaling, mirroring, or generated pixels.",
        "",
        "Patterns:",
        "",
        "- `idle`: 0, -1, -2, -1 px vertical breathing lift",
        "- `happy`: 0, -2, -4, -2 px vertical bounce",
        "",
        "## Rows",
        "",
    ]
    for row in payload["rows"]:
        suffix = f" skipped={row['skipped_reason']}" if row["skipped_reason"] else ""
        lines.append(
            f"- `{row['row_id']}` frames={row['frame_count']} changed={row['changed_count']}{suffix}"
        )
    lines.extend(["", "## Changed Frames", ""])
    for row in payload["rows"]:
        for frame in row["frames"]:
            if frame["changed"]:
                lines.append(
                    f"- `{frame['path']}` dy={frame['dy_applied']} "
                    f"sha `{frame['before_sha256'][:12]}` -> `{frame['after_sha256'][:12]}`"
                )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--audit-json", type=Path, default=DEFAULT_AUDIT_JSON)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--species", nargs="*")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    runtime_root = args.runtime_root.resolve()
    output_root = args.output_root
    output_root.mkdir(parents=True, exist_ok=True)
    backup_root = output_root / "backup-before-low-motion-repair"
    species_filter = set(args.species) if args.species else None

    rows = load_rows(args.audit_json, species_filter)
    records = [repair_row(runtime_root, backup_root, row, args.apply) for row in rows]
    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "dry_run": not args.apply,
        "runtime_root": str(runtime_root),
        "audit_json": str(args.audit_json),
        "backup_root": str(backup_root),
        "row_count": len(records),
        "changed_frame_count": sum(row.changed_count for row in records),
        "rows": [asdict(row) for row in records],
    }

    stem = "low-motion-applied" if args.apply else "low-motion-dry-run"
    json_path = output_root / f"{stem}.json"
    markdown_path = output_root / f"{stem}.md"
    json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(markdown_path, payload)
    print(json.dumps({"rows": payload["row_count"], "changed_frames": payload["changed_frame_count"]}, indent=2))
    print(markdown_path)


if __name__ == "__main__":
    main()
