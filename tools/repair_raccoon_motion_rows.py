#!/usr/bin/env python3
"""Clean raccoon runtime rows by rebuilding motion from safe idle silhouettes.

Raccoon action rows still contain baked ghost bodies and stray crop/bracket
artifacts. This tool keeps each row's own age/gender/color identity by choosing
the safest local idle frame, then generating conservative readable motion inside
the existing transparent runtime canvas.
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
MOTION_PATTERNS = {
    "idle": [(0, 0), (0, -1), (0, 0), (0, 1)],
    "walk": [(-2, 0), (-1, -1), (1, -1), (2, 0), (1, 0), (-1, 0)],
    "eat": [(0, 0), (1, 1), (1, 2), (0, 1)],
    "happy": [(0, 0), (0, -2), (0, -3), (0, -1)],
    "sad": [(-1, 0), (1, 0)],
    "sleep": [(-1, 0), (1, 0)],
    "sick": [(0, 0), (0, 1), (0, 0), (0, 1)],
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


def alpha_bbox(path: Path) -> tuple[int, int, int, int] | None:
    with Image.open(path) as image:
        return image.convert("RGBA").getbbox()


def idle_score(path: Path) -> tuple[int, int, int]:
    bbox = alpha_bbox(path)
    if bbox is None:
        return (-1, -1, -1)
    with Image.open(path) as image:
        source = image.convert("RGBA")
    left, top, right, bottom = bbox
    margins = [left, top, source.width - right, source.height - bottom]
    area = (right - left) * (bottom - top)
    # Prefer frames with safe canvas margins; then prefer compact silhouettes so
    # stray bracket/crop lines do not become the source of every repaired row.
    return (min(margins), -area, top)


def choose_safe_idle_source(idle_paths: list[Path]) -> Path:
    return max(idle_paths, key=idle_score)


def shifted_copy(source_path: Path, requested_dx: int, requested_dy: int) -> tuple[Image.Image, int, int]:
    with Image.open(source_path) as image:
        source = image.convert("RGBA")
    bbox = source.getbbox()
    if bbox is None:
        return source, 0, 0

    if requested_dx < 0:
        applied_dx = -min(abs(requested_dx), bbox[0])
    elif requested_dx > 0:
        applied_dx = min(requested_dx, source.width - bbox[2])
    else:
        applied_dx = 0

    if requested_dy < 0:
        applied_dy = -min(abs(requested_dy), bbox[1])
    elif requested_dy > 0:
        applied_dy = min(requested_dy, source.height - bbox[3])
    else:
        applied_dy = 0

    output = Image.new("RGBA", source.size, (0, 0, 0, 0))
    output.alpha_composite(source, (applied_dx, applied_dy))
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
        "# Raccoon Motion Cleanup",
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
    raccoon_root = args.runtime_root / "raccoon"
    rows: list[RowRecord] = []
    families = [family for family in args.families if family in MOTION_PATTERNS]
    for row_dir in sorted(raccoon_root.glob("*/*/*")):
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
    (args.output_root / "raccoon-motion-cleanup.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(args.output_root / "raccoon-motion-cleanup.md", payload)
    print(json.dumps({k: payload[k] for k in ["apply", "rows_scanned", "rows_changed", "frames_changed"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
