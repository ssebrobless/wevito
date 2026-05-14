#!/usr/bin/env python3
"""Apply conservative readable-motion repairs to weak runtime sprite rows.

This is a deterministic runtime-art repair, not generation. It keeps each
frame's canvas size and alpha semantics, records hashes, and only touches the
species/animations listed in TARGET_ANIMATIONS.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RUNTIME_ROOT = ROOT / "sprites_runtime"
DEFAULT_OUTPUT_ROOT = ROOT / "vnext" / "artifacts" / "readable-motion-repair-20260513"

TARGET_ANIMATIONS = {
    "snake": {"idle", "walk", "happy"},
    "squirrel": {"idle", "walk", "happy"},
}

SQUIRREL_FRAME_OFFSETS = {
    2: [(0, 0), (0, -2)],
    4: [(0, 0), (1, -2), (0, 0), (-1, 2)],
    6: [(0, 0), (2, -2), (1, 0), (-1, 2), (-2, 0), (1, -2)],
}


@dataclass(frozen=True)
class RepairRecord:
    path: str
    species: str
    animation: str
    frame_index: int
    width: int
    height: int
    before_sha256: str
    after_sha256: str
    operation: str


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_animation(path: Path) -> tuple[str, int] | None:
    stem = path.stem
    if "_" not in stem:
        return None
    animation, frame_text = stem.rsplit("_", 1)
    if not frame_text.isdigit():
        return None
    return animation, int(frame_text)


def shift_image(image: Image.Image, dx: int, dy: int) -> Image.Image:
    if dx == 0 and dy == 0:
        return image.copy()

    output = Image.new("RGBA", image.size, (0, 0, 0, 0))
    output.alpha_composite(image, (dx, dy))
    return output


def warp_snake(image: Image.Image, frame_index: int, frame_count: int) -> Image.Image:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    alpha_bbox = rgba.getchannel("A").getbbox()
    if alpha_bbox is None:
        return rgba

    phase = (frame_index / max(1, frame_count)) * math.tau
    amplitude = 3 if width >= 54 else 2
    period = max(18.0, height * 0.72)
    output = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    pixels = rgba.load()
    out_pixels = output.load()

    for y in range(height):
        row_shift = int(round(math.sin((y / period) * math.tau + phase) * amplitude))
        # A tiny forward/back phase offset makes walk rows read as slithering instead of shimmering.
        forward = int(round(math.sin(phase) * 2))
        shift = row_shift + forward
        for x in range(width):
            target_x = x + shift
            if 0 <= target_x < width:
                out_pixels[target_x, y] = pixels[x, y]

    return output


def repair_squirrel(image: Image.Image, frame_index: int, frame_count: int) -> Image.Image:
    offsets = SQUIRREL_FRAME_OFFSETS.get(frame_count) or SQUIRREL_FRAME_OFFSETS[4]
    dx, dy = offsets[frame_index % len(offsets)]
    return shift_image(image.convert("RGBA"), dx, dy)


def iter_target_rows(runtime_root: Path) -> list[tuple[str, str, list[Path]]]:
    rows: dict[tuple[str, str], list[Path]] = {}
    for species, animations in TARGET_ANIMATIONS.items():
        species_root = runtime_root / species
        if not species_root.exists():
            continue
        for path in sorted(species_root.glob("*/*/*/*.png")):
            parsed = parse_animation(path)
            if parsed is None:
                continue
            animation, _ = parsed
            if animation not in animations:
                continue
            rows.setdefault((species, animation), []).append(path)
    return [(species, animation, paths) for (species, animation), paths in sorted(rows.items())]


def repair(runtime_root: Path, output_root: Path, dry_run: bool) -> dict:
    records: list[RepairRecord] = []
    output_root.mkdir(parents=True, exist_ok=True)
    backup_root = output_root / "before"
    backup_root.mkdir(parents=True, exist_ok=True)

    for species, animation, paths in iter_target_rows(runtime_root):
        grouped: dict[Path, list[Path]] = {}
        for path in paths:
            grouped.setdefault(path.parent, []).append(path)

        for _, row_paths in sorted(grouped.items()):
            ordered = sorted(row_paths, key=lambda path: parse_animation(path)[1] if parse_animation(path) else 0)
            frame_count = len(ordered)
            for path in ordered:
                parsed = parse_animation(path)
                if parsed is None:
                    continue
                _, frame_index = parsed
                before_hash = sha256(path)
                relative = path.relative_to(runtime_root)
                backup_path = backup_root / relative
                backup_path.parent.mkdir(parents=True, exist_ok=True)
                if not backup_path.exists():
                    backup_path.write_bytes(path.read_bytes())

                with Image.open(path) as image:
                    if species == "snake":
                        repaired = warp_snake(image, frame_index, frame_count)
                        operation = "sine_row_warp"
                    elif species == "squirrel":
                        repaired = repair_squirrel(image, frame_index, frame_count)
                        operation = "readable_bounce_shift"
                    else:
                        continue

                if dry_run:
                    after_hash = before_hash
                else:
                    repaired.save(path)
                    after_hash = sha256(path)

                if before_hash != after_hash or dry_run:
                    records.append(
                        RepairRecord(
                            path=str(relative).replace("\\", "/"),
                            species=species,
                            animation=animation,
                            frame_index=frame_index,
                            width=repaired.width,
                            height=repaired.height,
                            before_sha256=before_hash,
                            after_sha256=after_hash,
                            operation=operation,
                        )
                    )

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "runtime_root": str(runtime_root),
        "dry_run": dry_run,
        "records": [asdict(record) for record in records],
        "summary": {
            "records": len(records),
            "species": sorted({record.species for record in records}),
            "animations": sorted({record.animation for record in records}),
        },
    }
    (output_root / "repair-report.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    lines = [
        "# Readable Motion Repair",
        "",
        f"- generated_at_utc: `{payload['generated_at_utc']}`",
        f"- dry_run: `{dry_run}`",
        f"- records: `{len(records)}`",
        f"- species: `{', '.join(payload['summary']['species'])}`",
        f"- animations: `{', '.join(payload['summary']['animations'])}`",
        "",
        "## Scope",
        "",
        "- `snake`: deterministic sine row warp for idle/walk/happy rows.",
        "- `squirrel`: stronger whole-sprite bounce/lean shifts for idle/walk/happy rows.",
        "",
        "## Notes",
        "",
        "- Canvas dimensions are preserved.",
        "- Backups are stored under `before/` with the same relative paths.",
        "- This improves readability but does not replace the need for true authored animation art.",
    ]
    (output_root / "repair-report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return payload


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    payload = repair(args.runtime_root, args.output_root, args.dry_run)
    print(json.dumps(payload["summary"], indent=2))


if __name__ == "__main__":
    main()
