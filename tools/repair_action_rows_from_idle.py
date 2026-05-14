#!/usr/bin/env python3
"""Reseed visibly broken action rows from each row's clean idle frames.

This is intentionally conservative: it does not generate or repaint animal
pixels. It copies the existing idle frames for the same species/age/gender/color
and applies tiny whole-sprite vertical offsets so action rows read as alive
without inheriting malformed action art.
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
DEFAULT_SPECIES = ["pigeon", "crow", "fox"]
DEFAULT_FAMILIES = ["eat", "happy", "sad", "sleep", "sick", "bathe"]
ACTION_PATTERNS = {
    "eat": [0, 1, 2, 1],
    "happy": [0, -2, -4, -2],
    "sad": [1, 2],
    "sleep": [0, 1],
    "sick": [0, 1, 0, 1],
    "bathe": [0, -1, 0, -1],
}


@dataclass(frozen=True)
class FrameRecord:
    path: str
    source_idle: str
    dy_requested: int
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


def shifted_copy(source_path: Path, requested_dy: int) -> tuple[Image.Image, int]:
    with Image.open(source_path) as image:
        source = image.convert("RGBA")
    bbox = source.getbbox()
    if bbox is None:
        return source, 0

    applied_dy = requested_dy
    if requested_dy < 0:
        applied_dy = -min(abs(requested_dy), bbox[1])
    elif requested_dy > 0:
        bottom_margin = source.height - bbox[3]
        applied_dy = min(requested_dy, bottom_margin)

    output = Image.new("RGBA", source.size, (0, 0, 0, 0))
    output.alpha_composite(source, (0, applied_dy))
    return output, applied_dy


def repair_row(
    runtime_root: Path,
    backup_root: Path,
    row_dir: Path,
    family: str,
    apply: bool,
) -> RowRecord:
    rel_parent = row_dir.relative_to(runtime_root).as_posix()
    row_id = f"{rel_parent}/{family}"
    target_paths = sorted(row_dir.glob(f"{family}_*.png"))
    idle_paths = sorted(row_dir.glob("idle_*.png"))
    pattern = ACTION_PATTERNS[family]
    if len(target_paths) != len(pattern):
        return RowRecord(row_id, len(target_paths), 0, f"expected_{len(pattern)}_frames", [])
    if len(idle_paths) < 1:
        return RowRecord(row_id, len(target_paths), 0, "missing_idle_frames", [])

    records: list[FrameRecord] = []
    for index, target_path in enumerate(target_paths):
        source_path = idle_paths[index % len(idle_paths)]
        before_sha = sha256(target_path)
        output, applied_dy = shifted_copy(source_path, pattern[index])
        changed = True
        after_sha = before_sha
        if apply:
            backup_path = backup_root / target_path.relative_to(runtime_root)
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(target_path, backup_path)
            output.save(target_path)
            after_sha = sha256(target_path)
            changed = after_sha != before_sha

        records.append(
            FrameRecord(
                path=str(target_path.relative_to(ROOT)),
                source_idle=str(source_path.relative_to(ROOT)),
                dy_requested=pattern[index],
                dy_applied=applied_dy,
                before_sha256=before_sha,
                after_sha256=after_sha,
                changed=changed,
            )
        )

    return RowRecord(row_id, len(target_paths), sum(1 for record in records if record.changed), None, records)


def iter_row_dirs(runtime_root: Path, species: list[str]):
    for species_id in species:
        species_root = runtime_root / species_id
        if not species_root.exists():
            continue
        for row_dir in sorted(species_root.glob("*/*/*")):
            if row_dir.is_dir():
                yield row_dir


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Action Row Idle Reseed Report",
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
    parser.add_argument("--species", nargs="*", default=DEFAULT_SPECIES)
    parser.add_argument("--families", nargs="*", default=DEFAULT_FAMILIES)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    families = [family for family in args.families if family in ACTION_PATTERNS]
    args.output_root.mkdir(parents=True, exist_ok=True)
    backup_root = args.output_root / "backup-before-apply"

    rows: list[RowRecord] = []
    for row_dir in iter_row_dirs(args.runtime_root, args.species):
        for family in families:
            rows.append(repair_row(args.runtime_root, backup_root, row_dir, family, args.apply))

    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "apply": args.apply,
        "runtime_root": str(args.runtime_root),
        "species": args.species,
        "families": families,
        "rows_scanned": len(rows),
        "rows_changed": sum(1 for row in rows if row.changed_count > 0),
        "frames_changed": sum(row.changed_count for row in rows),
        "rows": [asdict(row) for row in rows],
    }
    (args.output_root / "action-row-idle-reseed.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(args.output_root / "action-row-idle-reseed.md", payload)
    print(json.dumps({k: payload[k] for k in ["apply", "rows_scanned", "rows_changed", "frames_changed"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
