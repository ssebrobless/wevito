#!/usr/bin/env python3
"""Normalize PNG canvas size within selected runtime animation rows.

The repair is intentionally conservative: visible pixels are not repainted,
scaled, mirrored, or warped. Each selected row is expanded to the maximum width
and height already present in that row, with content bottom-centered so floor
contact stays stable.
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


@dataclass(frozen=True)
class FrameRecord:
    path: str
    before_size: tuple[int, int]
    after_size: tuple[int, int]
    before_sha256: str
    after_sha256: str
    changed: bool


@dataclass(frozen=True)
class RowRecord:
    row_id: str
    target_size: tuple[int, int]
    frame_count: int
    changed_count: int
    frames: list[FrameRecord]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def normalize_frame(image: Image.Image, target_size: tuple[int, int]) -> Image.Image:
    width, height = image.size
    target_width, target_height = target_size
    if width > target_width or height > target_height:
        raise ValueError(f"Cannot shrink frame {image.size} into {target_size}")

    output = Image.new("RGBA", target_size, (0, 0, 0, 0))
    x = (target_width - width) // 2
    y = target_height - height
    output.alpha_composite(image.convert("RGBA"), (x, y))
    return output


def normalize_row(row_dir: Path, animation: str, backup_root: Path, apply: bool) -> RowRecord:
    paths = sorted(row_dir.glob(f"{animation}_*.png"))
    if not paths:
        raise FileNotFoundError(f"No frames found for {row_dir} {animation}")

    sizes = []
    for path in paths:
        with Image.open(path) as image:
            sizes.append(image.size)

    target_size = (max(width for width, _ in sizes), max(height for _, height in sizes))
    frame_records: list[FrameRecord] = []

    for path in paths:
        before_sha = sha256(path)
        with Image.open(path) as image:
            before_size = image.size
            normalized = normalize_frame(image.convert("RGBA"), target_size)

        changed = before_size != target_size
        after_sha = before_sha
        if apply and changed:
            backup_path = backup_root / path.relative_to(DEFAULT_RUNTIME_ROOT)
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(path, backup_path)
            normalized.save(path)
            after_sha = sha256(path)

        frame_records.append(
            FrameRecord(
                path=str(path.relative_to(DEFAULT_RUNTIME_ROOT)),
                before_size=before_size,
                after_size=target_size,
                before_sha256=before_sha,
                after_sha256=after_sha,
                changed=changed,
            )
        )

    return RowRecord(
        row_id=str(row_dir.relative_to(DEFAULT_RUNTIME_ROOT)).replace("\\", "/") + f"/{animation}",
        target_size=target_size,
        frame_count=len(paths),
        changed_count=sum(1 for record in frame_records if record.changed),
        frames=frame_records,
    )


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Runtime Row Canvas Normalization",
        "",
        f"- generated_at: `{payload['generated_at']}`",
        f"- dry_run: `{payload['dry_run']}`",
        f"- runtime_root: `{payload['runtime_root']}`",
        f"- backup_root: `{payload['backup_root']}`",
        f"- rows: `{payload['row_count']}`",
        f"- changed_frames: `{payload['changed_frame_count']}`",
        "",
        "## Scope",
        "",
        "Selected rows only. The repair expands canvas with transparent pixels and bottom-centers existing art.",
        "",
        "## Rows",
        "",
    ]
    for row in payload["rows"]:
        lines.append(
            f"- `{row['row_id']}` target={row['target_size'][0]}x{row['target_size'][1]} "
            f"frames={row['frame_count']} changed={row['changed_count']}"
        )
    lines.extend(["", "## Changed Frames", ""])
    for row in payload["rows"]:
        for frame in row["frames"]:
            if frame["changed"]:
                lines.append(
                    f"- `{frame['path']}` {tuple(frame['before_size'])} -> {tuple(frame['after_size'])} "
                    f"sha `{frame['before_sha256'][:12]}` -> `{frame['after_sha256'][:12]}`"
                )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    global DEFAULT_RUNTIME_ROOT

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--species", required=True)
    parser.add_argument("--age", required=True)
    parser.add_argument("--gender", required=True)
    parser.add_argument("--color", required=True)
    parser.add_argument("--animations", nargs="+", required=True)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    DEFAULT_RUNTIME_ROOT = args.runtime_root.resolve()

    output_root = args.output_root
    output_root.mkdir(parents=True, exist_ok=True)
    backup_root = output_root / "backup-before-normalize"
    row_dir = DEFAULT_RUNTIME_ROOT / args.species / args.age / args.gender / args.color

    rows = [normalize_row(row_dir, animation, backup_root, args.apply) for animation in args.animations]
    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "dry_run": not args.apply,
        "runtime_root": str(DEFAULT_RUNTIME_ROOT),
        "backup_root": str(backup_root),
        "row_count": len(rows),
        "changed_frame_count": sum(row.changed_count for row in rows),
        "rows": [asdict(row) for row in rows],
    }

    stem = "row-canvas-applied" if args.apply else "row-canvas-dry-run"
    json_path = output_root / f"{stem}.json"
    markdown_path = output_root / f"{stem}.md"
    json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(markdown_path, payload)
    print(json.dumps({"rows": payload["row_count"], "changed_frames": payload["changed_frame_count"]}, indent=2))
    print(markdown_path)


if __name__ == "__main__":
    main()
