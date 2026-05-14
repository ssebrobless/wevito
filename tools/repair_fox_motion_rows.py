#!/usr/bin/env python3
"""Clean fox runtime rows by rebuilding motion from safe local silhouettes.

Fox runtime previews still show row-to-row identity drift and inconsistent
poses. This deterministic pass keeps each age/gender/color folder local,
selects the safest idle frame for that exact folder, and rebuilds common motion
families inside the existing transparent runtime canvas.
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
SPECIES = "fox"
MOTION_PATTERNS = {
    "idle": [(0, 0), (0, -1), (0, 0), (0, 1)],
    "walk": [(-3, 0), (-1, -1), (1, -1), (3, 0), (1, 0), (-1, 0)],
    "eat": [(0, 0), (1, -1), (2, -2), (1, -1)],
    "happy": [(0, 0), (0, -2), (0, -3), (0, -1)],
    "sad": [(-2, 0), (2, -1)],
    "sleep": [(-2, 0), (2, -1)],
    "sick": [(0, 0), (-1, -1), (1, -2), (0, -1)],
    "bathe": [(0, 0), (0, -1), (0, 0), (0, -1)],
}


@dataclass(frozen=True)
class FrameRecord:
    path: str
    source_idle: str
    dx_requested: int
    dy_requested: int
    dx_applied: int
    dy_applied: int
    before_sha256: str
    after_sha256: str
    changed: bool


@dataclass(frozen=True)
class RowRecord:
    row_id: str
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


def idle_score(path: Path) -> tuple[int, int, int]:
    with Image.open(path) as image:
        source = image.convert("RGBA")
    bbox = source.getbbox()
    if bbox is None:
        return (-1, -1, -1)
    left, top, right, bottom = bbox
    margins = [left, top, source.width - right, source.height - bottom]
    area = (right - left) * (bottom - top)
    return (min(margins), -area, top)


def choose_safe_idle_source(idle_paths: list[Path]) -> Path:
    return max(idle_paths, key=idle_score)


def shifted_copy(source_path: Path, requested_dx: int, requested_dy: int) -> tuple[Image.Image, int, int]:
    with Image.open(source_path) as image:
        source = image.convert("RGBA")
    bbox = source.getbbox()
    if bbox is None:
        return source, 0, 0

    left, top, right, bottom = bbox
    body = source.crop(bbox)
    body_width = right - left
    body_height = bottom - top
    min_margin = 2
    base_left = round((source.width - body_width) / 2)
    target_left = max(min_margin, min(source.width - body_width - min_margin, base_left + requested_dx))
    target_top = source.height - min_margin - body_height + requested_dy
    target_top = max(min_margin, min(source.height - body_height - min_margin, target_top))
    output = Image.new("RGBA", source.size, (0, 0, 0, 0))
    output.alpha_composite(body, (target_left, target_top))
    applied_dx = target_left - left
    applied_dy = target_top - top
    return output, applied_dx, applied_dy


def repair_row(runtime_root: Path, backup_root: Path, row_dir: Path, family: str, apply: bool) -> RowRecord:
    row_id = f"{row_dir.relative_to(runtime_root).as_posix()}/{family}"
    target_paths = sorted(row_dir.glob(f"{family}_*.png"))
    idle_paths = sorted(row_dir.glob("idle_*.png"))
    pattern = MOTION_PATTERNS[family]
    if len(target_paths) != len(pattern):
        return RowRecord(row_id, len(target_paths), 0, f"expected_{len(pattern)}_frames", [])
    if not idle_paths:
        return RowRecord(row_id, len(target_paths), 0, "missing_idle_frames", [])

    source_path = choose_safe_idle_source(idle_paths)
    records: list[FrameRecord] = []
    for index, target_path in enumerate(target_paths):
        dx, dy = pattern[index]
        before = sha256(target_path)
        output, applied_dx, applied_dy = shifted_copy(source_path, dx, dy)
        after = before
        changed = True
        if apply:
            backup_path = backup_root / target_path.relative_to(runtime_root)
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(target_path, backup_path)
            output.save(target_path)
            after = sha256(target_path)
            changed = after != before

        records.append(
            FrameRecord(
                path=str(target_path.relative_to(ROOT)),
                source_idle=str(source_path.relative_to(ROOT)),
                dx_requested=dx,
                dy_requested=dy,
                dx_applied=applied_dx,
                dy_applied=applied_dy,
                before_sha256=before,
                after_sha256=after,
                changed=changed,
            )
        )

    return RowRecord(row_id, len(target_paths), sum(1 for frame in records if frame.changed), None, records)


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Fox Motion Cleanup",
        "",
        f"- generated_at: `{payload['generated_at']}`",
        f"- apply: `{payload['apply']}`",
        f"- rows_scanned: `{payload['rows_scanned']}`",
        f"- rows_changed: `{payload['rows_changed']}`",
        f"- frames_changed: `{payload['frames_changed']}`",
        "",
        "## Rows",
    ]
    for row in payload["rows"]:
        status = row["skipped_reason"] or f"{row['changed_count']} changed"
        lines.append(f"- `{row['row_id']}`: {status}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--families", nargs="*", default=list(MOTION_PATTERNS.keys()))
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    args.output_root.mkdir(parents=True, exist_ok=True)
    backup_root = args.output_root / "backup-before-apply"
    species_root = args.runtime_root / SPECIES
    rows: list[RowRecord] = []
    families = [family for family in args.families if family in MOTION_PATTERNS]
    for row_dir in sorted(species_root.glob("*/*/*")):
        if not row_dir.is_dir():
            continue
        for family in families:
            rows.append(repair_row(args.runtime_root, backup_root, row_dir, family, args.apply))

    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "apply": args.apply,
        "runtime_root": str(args.runtime_root),
        "families": families,
        "rows_scanned": len(rows),
        "rows_changed": sum(1 for row in rows if row.changed_count > 0),
        "frames_changed": sum(row.changed_count for row in rows),
        "rows": [asdict(row) for row in rows],
    }
    (args.output_root / "fox-motion-cleanup.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(args.output_root / "fox-motion-cleanup.md", payload)
    print(json.dumps({k: payload[k] for k in ["apply", "rows_scanned", "rows_changed", "frames_changed"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
