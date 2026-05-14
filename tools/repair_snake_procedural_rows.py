#!/usr/bin/env python3
"""Replace muddy snake runtime rows with crisp deterministic pixel snakes.

The current snake rows have fully opaque baked afterimages and mushy action
poses. This tool redraws only snake runtime PNGs with simple local procedural
pixel art while preserving the existing folder layout, filenames, frame counts,
and transparent PNG contract.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
import shutil
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RUNTIME_ROOT = ROOT / "sprites_runtime"
FRAME_COUNTS = {
    "idle": 4,
    "walk": 6,
    "eat": 4,
    "happy": 4,
    "sad": 2,
    "sleep": 2,
    "sick": 4,
    "bathe": 4,
}
COLOR_VARIANTS = {
    "blue": ((42, 76, 103), (86, 126, 153), (16, 30, 42)),
    "indigo": ((54, 58, 116), (100, 102, 169), (22, 23, 52)),
    "orange": ((141, 87, 39), (199, 132, 69), (62, 34, 17)),
    "red": ((122, 47, 42), (181, 84, 77), (51, 20, 19)),
    "violet": ((93, 62, 126), (147, 105, 178), (40, 27, 58)),
    "yellow": ((142, 122, 45), (208, 184, 83), (64, 52, 20)),
}
AGE_SCALE = {
    "baby": 0.72,
    "teen": 0.90,
    "adult": 1.10,
}


@dataclass(frozen=True)
class FrameRecord:
    path: str
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


def image_sha256(image: Image.Image) -> str:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG")
    return hashlib.sha256(buffer.getvalue()).hexdigest()


def age_dimensions(age: str) -> tuple[int, int, float]:
    scale = AGE_SCALE.get(age, 1.0)
    width = int(round(138 * scale))
    height = int(round(58 * scale))
    return max(width, 92), 64, scale


def body_points(family: str, frame_index: int, frame_count: int, width: int, height: int, scale: float) -> list[tuple[float, float]]:
    phase = (frame_index / max(1, frame_count)) * math.tau
    base_y = height * 0.62
    length = width * 0.62
    start_x = width * 0.13
    points: list[tuple[float, float]] = []

    if family in {"idle", "eat", "happy", "sick", "bathe"}:
        coils = 2.25 if family != "happy" else 2.55
        radius_x = length / (coils * 2.35)
        amp = height * (0.09 if family != "sick" else 0.055)
        for i in range(28):
            t = i / 27
            x = start_x + t * length
            wave = math.sin(t * math.tau * coils + phase)
            y = base_y + wave * amp
            if i > 21:
                y -= (i - 21) * height * 0.035
            points.append((x, y))
        if family == "eat":
            points[-1] = (points[-1][0] + 2 * scale, points[-1][1] - 5 * scale)
        return points

    if family == "walk":
        amp = height * 0.115
        for i in range(32):
            t = i / 31
            x = start_x + t * (width * 0.66)
            y = base_y + math.sin(t * math.tau * 2.8 + phase) * amp
            points.append((x, y))
        return points

    if family == "sleep":
        breath = math.sin(phase) * height * 0.012
        for i in range(28):
            t = i / 27
            x = start_x + t * length
            y = base_y + breath + math.sin(t * math.tau * 1.8 + phase * 0.15) * height * 0.045
            points.append((x, y))
        return points

    if family == "sad":
        for i in range(28):
            t = i / 27
            x = start_x + t * (width * 0.68)
            y = base_y + math.sin(t * math.tau * 1.5 + phase * 0.4) * height * 0.06
            if i > 22:
                y += (i - 22) * height * 0.018
            points.append((x, y))
        return points

    return body_points("idle", frame_index, frame_count, width, height, scale)


def draw_polyline(draw: ImageDraw.ImageDraw, points: list[tuple[float, float]], width: int, fill: tuple[int, int, int, int]) -> None:
    int_points = [(int(round(x)), int(round(y))) for x, y in points]
    if len(int_points) >= 2:
        draw.line(int_points, fill=fill, width=width, joint="curve")
    radius = max(1, width // 2)
    for x, y in int_points:
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def render_frame(age: str, gender: str, color: str, family: str, frame_index: int, frame_count: int) -> Image.Image:
    width, height, scale = age_dimensions(age)
    base, highlight, outline = COLOR_VARIANTS.get(color, COLOR_VARIANTS["blue"])
    body_w = max(5, int(round(9 * scale)))
    outline_w = body_w + max(2, int(round(3 * scale)))
    points = body_points(family, frame_index, frame_count, width, height, scale)

    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    draw_polyline(draw, points, outline_w, (*outline, 255))
    draw_polyline(draw, points, body_w, (*base, 255))

    # Dorsal highlights: short staggered pixels along the back.
    for i, (x, y) in enumerate(points[3:-3:3], start=3):
        if i % 2 == 0:
            draw.rectangle(
                (
                    int(round(x - 1 * scale)),
                    int(round(y - body_w * 0.45)),
                    int(round(x + 3 * scale)),
                    int(round(y - body_w * 0.18)),
                ),
                fill=(*highlight, 255),
            )

    head_x, head_y = points[-1]
    head_r = max(4, int(round(6 * scale)))
    if gender == "male":
        head_r += 1
    head_box = (
        int(round(head_x - head_r)),
        int(round(head_y - head_r * 0.95)),
        int(round(head_x + head_r * 1.15)),
        int(round(head_y + head_r * 0.95)),
    )
    draw.ellipse(head_box, fill=(*outline, 255))
    inner_head = (
        head_box[0] + 2,
        head_box[1] + 2,
        head_box[2] - 2,
        head_box[3] - 2,
    )
    draw.ellipse(inner_head, fill=(*base, 255))
    eye_y = int(round(head_y - head_r * 0.32))
    draw.rectangle((int(round(head_x + head_r * 0.35)), eye_y, int(round(head_x + head_r * 0.35)) + 1, eye_y + 1), fill=(232, 238, 220, 255))
    draw.point((int(round(head_x + head_r * 0.35)) + 1, eye_y + 1), fill=(5, 8, 10, 255))

    if family == "eat" and frame_index % 2 == 1:
        tongue_x = int(round(head_x + head_r * 1.1))
        tongue_y = int(round(head_y))
        draw.line((tongue_x, tongue_y, tongue_x + 5, tongue_y - 1), fill=(214, 65, 82, 255), width=1)
        draw.point((tongue_x + 6, tongue_y - 2), fill=(214, 65, 82, 255))
        draw.point((tongue_x + 6, tongue_y), fill=(214, 65, 82, 255))

    return image


def repair_row(runtime_root: Path, backup_root: Path, row_dir: Path, family: str, apply: bool) -> RowRecord:
    rel_parts = row_dir.relative_to(runtime_root).parts
    if len(rel_parts) != 4:
        return RowRecord(str(row_dir), 0, 0, "unexpected_path", [])
    species, age, gender, color = rel_parts
    row_id = f"{species}/{age}/{gender}/{color}/{family}"
    paths = sorted(row_dir.glob(f"{family}_*.png"))
    expected_count = FRAME_COUNTS[family]
    if len(paths) != expected_count:
        return RowRecord(row_id, len(paths), 0, f"expected_{expected_count}_frames", [])

    records: list[FrameRecord] = []
    for index, path in enumerate(paths):
        before = sha256(path)
        image = render_frame(age, gender, color, family, index, expected_count)
        after = image_sha256(image)
        if apply:
            backup_path = backup_root / path.relative_to(runtime_root)
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(path, backup_path)
            image.save(path)
            after = sha256(path)
        records.append(FrameRecord(str(path.relative_to(ROOT)), before, after, before != after))

    return RowRecord(row_id, len(paths), sum(1 for frame in records if frame.changed), None, records)


def write_markdown(path: Path, payload: dict) -> None:
    lines = [
        "# Snake Procedural Runtime Repair",
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
    parser.add_argument("--families", nargs="*", default=list(FRAME_COUNTS.keys()))
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()

    args.output_root.mkdir(parents=True, exist_ok=True)
    backup_root = args.output_root / "backup-before-apply"
    snake_root = args.runtime_root / "snake"
    rows: list[RowRecord] = []
    for row_dir in sorted(snake_root.glob("*/*/*")):
        if not row_dir.is_dir():
            continue
        for family in args.families:
            if family in FRAME_COUNTS:
                rows.append(repair_row(args.runtime_root, backup_root, row_dir, family, args.apply))

    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "apply": args.apply,
        "runtime_root": str(args.runtime_root),
        "families": args.families,
        "rows_scanned": len(rows),
        "rows_changed": sum(1 for row in rows if row.changed_count > 0),
        "frames_changed": sum(row.changed_count for row in rows),
        "rows": [asdict(row) for row in rows],
    }
    (args.output_root / "snake-procedural-repair.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(args.output_root / "snake-procedural-repair.md", payload)
    print(json.dumps({k: payload[k] for k in ["apply", "rows_scanned", "rows_changed", "frames_changed"]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
