#!/usr/bin/env python3
"""Repair frog runtime walk rows with a clean local hop cycle.

Some verified authored frog walk frames contain useful motion ideas, but they
are too visually inconsistent for a safe bulk runtime restore. This tool repairs
only `sprites_runtime/frog/**/walk_00..05` by deriving a readable hop cycle from
each row's own existing idle frames. It does not generate, import, edit source
boards, or touch non-walk families.
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
FRAME_COUNT = 6
MIN_MARGIN = 2
REFERENCE_IDLE_COUNT = 4
HOP_POSES = [
    (1.06, 0.94, 0, 1),
    (0.98, 1.04, 2, -2),
    (0.96, 1.08, 4, -5),
    (1.00, 1.02, 5, -3),
    (1.07, 0.93, 3, 1),
    (1.00, 1.00, 1, 0),
]


@dataclass(frozen=True)
class FrameRecord:
    target_path: str
    before_sha256: str
    idle_source_path: str
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


def hop_frame_from_idle(source_path: Path, canvas_path: Path, pose: tuple[float, float, int, int]) -> Image.Image:
    with Image.open(source_path) as source_image:
        source = source_image.convert("RGBA")
    with Image.open(canvas_path) as canvas_image:
        canvas_size = canvas_image.size

    bbox = source.getbbox()
    if bbox is None:
        return Image.new("RGBA", canvas_size, (0, 0, 0, 0))

    sprite = source.crop(bbox)
    scale_x, scale_y, offset_x, offset_y = pose
    scaled_size = (
        max(1, round(sprite.width * scale_x)),
        max(1, round(sprite.height * scale_y)),
    )
    sprite = sprite.resize(scaled_size, Image.Resampling.LANCZOS)

    max_w = max(1, canvas_size[0] - (MIN_MARGIN * 2))
    max_h = max(1, canvas_size[1] - (MIN_MARGIN * 2))
    scale = min(1.0, max_w / sprite.width, max_h / sprite.height)
    if scale < 1.0:
        size = (max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale)))
        sprite = sprite.resize(size, Image.Resampling.LANCZOS)

    target_x = ((canvas_size[0] - sprite.width) // 2) + offset_x
    target_y = canvas_size[1] - sprite.height - MIN_MARGIN + offset_y
    target_x = max(MIN_MARGIN, min(canvas_size[0] - sprite.width - MIN_MARGIN, target_x))
    target_y = max(MIN_MARGIN, min(canvas_size[1] - sprite.height - MIN_MARGIN, target_y))

    output = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    output.alpha_composite(sprite, (target_x, target_y))
    return output


def restore_row(runtime_root: Path, backup_root: Path, runtime_row_dir: Path, apply: bool) -> RowRecord:
    row_id = runtime_row_dir.relative_to(runtime_root).as_posix()
    idle_paths = [runtime_row_dir / f"idle_{index % REFERENCE_IDLE_COUNT:02d}.png" for index in range(FRAME_COUNT)]
    target_paths = [runtime_row_dir / f"walk_{index:02d}.png" for index in range(FRAME_COUNT)]

    missing_idles = [path.name for path in idle_paths if not path.exists()]
    if missing_idles:
        return RowRecord(row_id, 0, 0, f"missing_idle_sources:{','.join(missing_idles)}", [])
    missing_targets = [path.name for path in target_paths if not path.exists()]
    if missing_targets:
        return RowRecord(row_id, 0, 0, f"missing_targets:{','.join(missing_targets)}", [])

    records: list[FrameRecord] = []
    for index, (idle_path, target_path) in enumerate(zip(idle_paths, target_paths)):
        backup_path = backup_root / target_path.relative_to(runtime_root)
        before = sha256(target_path)
        canvas_path = backup_path if backup_path.exists() else target_path
        output = hop_frame_from_idle(idle_path, canvas_path, HOP_POSES[index])
        output_bytes_path = backup_root / ".tmp-normalized-frame.png"
        output_bytes_path.parent.mkdir(parents=True, exist_ok=True)
        output.save(output_bytes_path)
        normalized_sha = sha256(output_bytes_path)
        output_bytes_path.unlink()
        after = before
        changed = before != normalized_sha
        if apply and changed:
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(target_path, backup_path)
            output.save(target_path)
            after = sha256(target_path)

        records.append(
            FrameRecord(
                target_path=str(target_path.relative_to(ROOT)),
                before_sha256=before,
                idle_source_path=str(idle_path.relative_to(ROOT)),
                after_sha256=after,
                changed=changed,
            )
        )

    return RowRecord(row_id, FRAME_COUNT, sum(1 for record in records if record.changed), None, records)


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Frog Runtime Walk Cleanup",
        "",
        f"- generated_at: `{payload['generated_at']}`",
        f"- apply: `{payload['apply']}`",
        f"- rows_scanned: `{payload['rows_scanned']}`",
        f"- rows_changed: `{payload['rows_changed']}`",
        f"- frames_changed: `{payload['frames_changed']}`",
        "",
        "## Method",
        "",
        "Rebuilds runtime frog `walk_00..05` frames from each row's own clean idle sprites.",
        "Applies a small squash/stretch hop cycle with bottom-center anchoring on the existing runtime canvas.",
        "No generation, imports, source-board edits, prop-anchor edits, or non-walk family edits.",
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
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    args.output_root.mkdir(parents=True, exist_ok=True)
    backup_root = args.output_root / "backup-before-apply"
    frog_runtime_root = args.runtime_root / "frog"

    rows: list[RowRecord] = []
    for runtime_row_dir in sorted(frog_runtime_root.glob("*/*/*")):
        if runtime_row_dir.is_dir():
            rows.append(restore_row(args.runtime_root, backup_root, runtime_row_dir, args.apply))

    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "apply": args.apply,
        "runtime_root": str(args.runtime_root),
        "rows_scanned": len(rows),
        "rows_changed": sum(1 for row in rows if row.changed_count > 0),
        "frames_changed": sum(row.changed_count for row in rows),
        "rows": [asdict(row) for row in rows],
    }
    (args.output_root / "frog-runtime-walk-cleanup.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(args.output_root / "frog-runtime-walk-cleanup.md", payload)
    print(json.dumps({k: payload[k] for k in ["apply", "rows_scanned", "rows_changed", "frames_changed"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
