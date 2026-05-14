#!/usr/bin/env python3
"""Clean crow runtime edges without replacing the crow art.

The crow rows are mostly readable, but several adult frames touch the bottom of
their runtime canvas and some previews show tiny detached dark specks around
feet. This tool keeps the original pixels, removes only very small disconnected
alpha components, and repacks each frame with a small bottom margin inside its
existing canvas.
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
SPECIES = "crow"
FAMILIES = ("idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe")
MOTION_OFFSETS = {
    "idle": [(0, 0), (0, -1), (0, 0), (0, 1)],
    "walk": [(-3, 0), (-2, -1), (0, -1), (2, 0), (3, 0), (1, 0)],
    "eat": [(0, 0), (1, 1), (2, 2), (1, 1)],
    "happy": [(0, 0), (0, -2), (0, -3), (0, -1)],
    "sad": [(-2, 1), (2, 1)],
    "sleep": [(-2, 0), (2, 0)],
    "sick": [(0, 0), (-2, 2), (2, -1), (0, 2)],
    "bathe": [(0, 0), (0, -2), (0, -1), (0, 1)],
}


@dataclass(frozen=True)
class FrameRecord:
    path: str
    before_sha256: str
    after_sha256: str
    changed: bool
    before_bbox: tuple[int, int, int, int] | None
    after_bbox: tuple[int, int, int, int] | None
    removed_components: int
    removed_pixels: int
    dx_applied: int
    dy_applied: int


@dataclass(frozen=True)
class RowRecord:
    row_id: str
    frame_count: int
    changed_count: int
    frames: list[FrameRecord]


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def connected_alpha_components(image: Image.Image) -> list[list[tuple[int, int]]]:
    alpha = image.getchannel("A")
    width, height = image.size
    visited: set[tuple[int, int]] = set()
    components: list[list[tuple[int, int]]] = []
    for y in range(height):
        for x in range(width):
            if (x, y) in visited or alpha.getpixel((x, y)) == 0:
                continue
            stack = [(x, y)]
            visited.add((x, y))
            component: list[tuple[int, int]] = []
            while stack:
                px, py = stack.pop()
                component.append((px, py))
                for nx in (px - 1, px, px + 1):
                    for ny in (py - 1, py, py + 1):
                        if nx == px and ny == py:
                            continue
                        if nx < 0 or ny < 0 or nx >= width or ny >= height:
                            continue
                        if (nx, ny) in visited or alpha.getpixel((nx, ny)) == 0:
                            continue
                        visited.add((nx, ny))
                        stack.append((nx, ny))
            components.append(component)
    return components


def remove_tiny_specks(image: Image.Image, max_component_pixels: int) -> tuple[Image.Image, int, int]:
    if max_component_pixels <= 0:
        return image.copy(), 0, 0
    output = image.copy()
    pixels = output.load()
    removed_components = 0
    removed_pixels = 0
    for component in connected_alpha_components(image):
        if len(component) > max_component_pixels:
            continue
        removed_components += 1
        removed_pixels += len(component)
        for x, y in component:
            pixels[x, y] = (0, 0, 0, 0)
    return output, removed_components, removed_pixels


def repack_with_safe_margin(image: Image.Image, bottom_margin: int, top_margin: int, motion_offset: tuple[int, int]) -> tuple[Image.Image, int, int]:
    bbox = image.getbbox()
    if bbox is None:
        return image.copy(), 0, 0

    left, top, right, bottom = bbox
    width, height = image.size
    body_width = right - left
    body_height = bottom - top
    target_left = max(0, min(width - body_width, round((width - body_width) / 2) + motion_offset[0]))
    target_top = height - bottom_margin - body_height
    target_top = max(top_margin, min(height - bottom_margin - body_height, target_top + motion_offset[1]))
    dx = target_left - left
    dy = target_top - top
    if dx == 0 and dy == 0:
        return image.copy(), 0, 0

    crop = image.crop(bbox)
    output = Image.new("RGBA", image.size, (0, 0, 0, 0))
    output.alpha_composite(crop, (target_left, target_top))
    return output, dx, dy


def clean_frame(path: Path, backup_root: Path, runtime_root: Path, apply: bool, bottom_margin: int, top_margin: int, max_speck_pixels: int, family: str, frame_index: int) -> FrameRecord:
    before_hash = sha256(path)
    with Image.open(path) as raw:
        original = raw.convert("RGBA")
    before_bbox = original.getbbox()
    without_specks, removed_components, removed_pixels = remove_tiny_specks(original, max_speck_pixels)
    offsets = MOTION_OFFSETS.get(family, [(0, 0)])
    cleaned, dx, dy = repack_with_safe_margin(without_specks, bottom_margin, top_margin, offsets[frame_index % len(offsets)])
    after_bbox = cleaned.getbbox()

    changed = cleaned.tobytes() != original.tobytes()
    after_hash = before_hash
    if apply and changed:
        backup_path = backup_root / path.relative_to(runtime_root)
        backup_path.parent.mkdir(parents=True, exist_ok=True)
        if not backup_path.exists():
            shutil.copy2(path, backup_path)
        cleaned.save(path)
        after_hash = sha256(path)
    elif apply:
        after_hash = sha256(path)

    return FrameRecord(
        path=str(path.relative_to(ROOT)),
        before_sha256=before_hash,
        after_sha256=after_hash,
        changed=changed,
        before_bbox=before_bbox,
        after_bbox=after_bbox,
        removed_components=removed_components,
        removed_pixels=removed_pixels,
        dx_applied=dx,
        dy_applied=dy,
    )


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Crow Runtime Edge Cleanup",
        "",
        f"- generated_at: `{payload['generated_at']}`",
        f"- apply: `{payload['apply']}`",
        f"- rows_scanned: `{payload['rows_scanned']}`",
        f"- rows_changed: `{payload['rows_changed']}`",
        f"- frames_changed: `{payload['frames_changed']}`",
        f"- removed_components: `{payload['removed_components']}`",
        f"- removed_pixels: `{payload['removed_pixels']}`",
        "",
        "## Rows",
    ]
    for row in payload["rows"]:
        lines.append(f"- `{row['row_id']}`: {row['changed_count']} changed")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--bottom-margin", type=int, default=2)
    parser.add_argument("--top-margin", type=int, default=2)
    parser.add_argument("--max-speck-pixels", type=int, default=3)
    args = parser.parse_args()

    args.output_root.mkdir(parents=True, exist_ok=True)
    backup_root = args.output_root / "backup-before-apply"
    crow_root = args.runtime_root / SPECIES
    rows: list[RowRecord] = []
    for row_dir in sorted(crow_root.glob("*/*/*")):
        if not row_dir.is_dir():
            continue
        for family in FAMILIES:
            paths = sorted(row_dir.glob(f"{family}_*.png"))
            frames = [
                clean_frame(path, backup_root, args.runtime_root, args.apply, args.bottom_margin, args.top_margin, args.max_speck_pixels, family, index)
                for index, path in enumerate(paths)
            ]
            rows.append(
                RowRecord(
                    row_id=f"{row_dir.relative_to(args.runtime_root).as_posix()}/{family}",
                    frame_count=len(frames),
                    changed_count=sum(1 for frame in frames if frame.changed),
                    frames=frames,
                )
            )

    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "apply": args.apply,
        "runtime_root": str(args.runtime_root),
        "rows_scanned": len(rows),
        "rows_changed": sum(1 for row in rows if row.changed_count > 0),
        "frames_changed": sum(row.changed_count for row in rows),
        "removed_components": sum(frame.removed_components for row in rows for frame in row.frames),
        "removed_pixels": sum(frame.removed_pixels for row in rows for frame in row.frames),
        "rows": [asdict(row) for row in rows],
    }
    (args.output_root / "crow-runtime-edge-cleanup.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(args.output_root / "crow-runtime-edge-cleanup.md", payload)
    print(json.dumps({k: payload[k] for k in ["apply", "rows_scanned", "rows_changed", "frames_changed", "removed_components", "removed_pixels"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
