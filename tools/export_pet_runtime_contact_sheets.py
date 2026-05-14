#!/usr/bin/env python3
"""Export per-species contact sheets for Wevito runtime pet sprites.

This is read-only. It gives the visual QA cockpit a broad evidence surface for
human review when structural audits pass but sprites still look bad in-game.
"""

from __future__ import annotations

import argparse
import json
import re
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


FRAME_RE = re.compile(r"^(?P<animation>.+)_(?P<frame>\d+)\.png$", re.IGNORECASE)


@dataclass(frozen=True)
class ContactSheetRecord:
    species: str
    path: str
    rows: int
    frames: int


def iter_rows(runtime_root: Path):
    groups: dict[tuple[str, str, str, str, str], list[Path]] = {}
    for path in sorted(runtime_root.glob("*/*/*/*/*.png")):
        match = FRAME_RE.match(path.name)
        if match is None:
            continue
        species, age, gender, color = path.relative_to(runtime_root).parts[:4]
        groups.setdefault((species, age, gender, color, match.group("animation")), []).append(path)
    for key, paths in sorted(groups.items()):
        yield key, sorted(paths)


def load_thumb(path: Path, size: int) -> Image.Image:
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        rgba.thumbnail((size, size), Image.Resampling.NEAREST)
        tile = Image.new("RGBA", (size, size), (245, 245, 245, 255))
        left = (size - rgba.width) // 2
        top = (size - rgba.height) // 2
        tile.alpha_composite(rgba, (left, top))
        return tile.convert("RGB")


def draw_sheet(species: str, rows: list[tuple[tuple[str, str, str, str, str], list[Path]]], output_path: Path) -> ContactSheetRecord:
    thumb = 40
    label_w = 260
    row_h = 46
    max_frames = max((len(paths) for _, paths in rows), default=1)
    width = label_w + max_frames * (thumb + 4) + 16
    height = max(80, 42 + len(rows) * row_h)
    sheet = Image.new("RGB", (width, height), (236, 240, 242))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.text((12, 12), f"{species} runtime sprite contact sheet", fill=(20, 26, 32), font=font)

    frame_count = 0
    y = 38
    for index, (key, paths) in enumerate(rows):
        _, age, gender, color, animation = key
        fill = (226, 232, 236) if index % 2 == 0 else (216, 224, 230)
        draw.rectangle((0, y - 2, width, y + row_h - 3), fill=fill)
        draw.text((10, y + 12), f"{age}/{gender}/{color}/{animation}", fill=(20, 26, 32), font=font)
        x = label_w
        for path in paths:
            tile = load_thumb(path, thumb)
            sheet.paste(tile, (x, y))
            x += thumb + 4
            frame_count += 1
        y += row_h

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)
    return ContactSheetRecord(species, str(output_path).replace("\\", "/"), len(rows), frame_count)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime-root", default="sprites_runtime")
    parser.add_argument("--output-root", required=True)
    args = parser.parse_args()

    runtime_root = Path(args.runtime_root).resolve()
    output_root = Path(args.output_root)
    grouped: dict[str, list[tuple[tuple[str, str, str, str, str], list[Path]]]] = {}
    for key, paths in iter_rows(runtime_root):
        grouped.setdefault(key[0], []).append((key, paths))

    sheets = [
        draw_sheet(species, rows, output_root / f"{species}.png")
        for species, rows in sorted(grouped.items())
    ]
    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "runtime_root": str(runtime_root),
        "species_count": len(sheets),
        "sheets": [asdict(sheet) for sheet in sheets],
    }
    output_root.mkdir(parents=True, exist_ok=True)
    (output_root / "contact-sheets.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    lines = [
        "# Runtime Pet Contact Sheets",
        "",
        f"- generated_at: `{payload['generated_at_utc']}`",
        f"- runtime_root: `{payload['runtime_root']}`",
        f"- species_count: `{len(sheets)}`",
        "",
    ]
    for sheet in sheets:
        lines.append(f"- `{sheet.species}`: `{sheet.path}` ({sheet.rows} rows, {sheet.frames} frames)")
    (output_root / "contact-sheets.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(json.dumps({"species_count": len(sheets), "sheets": [sheet.species for sheet in sheets]}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
