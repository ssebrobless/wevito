#!/usr/bin/env python3
"""Build the 10 per-species ball-candidate review sheets (C-199R / C-199R2).

One row per variant (age/gender/color). Columns: play_ball x6, carry_ball_run
x6, then the first frame of pickup_ball / hold_ball / drop_ball / carry_ball_walk
so the owner can spot-check every optional family at a glance, same layout as the
C-199R round-2 sheets.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw

AGES = ["baby", "teen", "adult"]
GENDERS = ["female", "male"]
COLORS = ["red", "orange", "yellow", "blue", "indigo", "violet"]

COLUMNS: list[tuple[str, list[str]]] = [
    ("play_ball", [f"play_ball_{i:02d}" for i in range(6)]),
    ("carry_ball_run", [f"carry_ball_run_{i:02d}" for i in range(6)]),
    ("pickup", ["pickup_ball_00"]),
    ("hold", ["hold_ball_00"]),
    ("drop", ["drop_ball_00"]),
    ("carry_walk", ["carry_ball_walk_00"]),
]

CELL = 72
LABEL_W = 150
HEAD = 28
GROUP_GAP = 10
BG = (248, 248, 248, 255)
GRID = (210, 210, 210, 255)


def variant_rows() -> list[tuple[str, str, str]]:
    rows = []
    for age in AGES:
        for gender in GENDERS:
            for color in COLORS:
                rows.append((age, gender, color))
    return rows


def thumb(path: Path) -> Image.Image:
    tile = Image.new("RGBA", (CELL, CELL), BG)
    if path.exists():
        im = Image.open(path).convert("RGBA")
        im.thumbnail((CELL - 6, CELL - 6), Image.LANCZOS)
        tile.alpha_composite(im, ((CELL - im.width) // 2, (CELL - im.height) // 2))
    else:
        d = ImageDraw.Draw(tile)
        d.line((4, 4, CELL - 4, CELL - 4), fill=(200, 80, 80, 255), width=2)
        d.line((4, CELL - 4, CELL - 4, 4), fill=(200, 80, 80, 255), width=2)
    return tile


def build_species_sheet(species: str, root: Path) -> Image.Image:
    flat_cols = [(grp, frame) for grp, frames in COLUMNS for frame in frames]
    n_groups = len(COLUMNS)
    width = LABEL_W + len(flat_cols) * CELL + n_groups * GROUP_GAP
    rows = variant_rows()
    height = HEAD + len(rows) * CELL
    sheet = Image.new("RGBA", (width, height), BG)
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 8), f"{species} — ball-candidate review (C-199R2)", fill=(20, 20, 20, 255))

    for r, (age, gender, color) in enumerate(rows):
        y = HEAD + r * CELL
        draw.text((6, y + CELL // 2 - 6), f"{age}/{gender}/{color}", fill=(40, 40, 40, 255))
        vdir = root / species / age / gender / color
        x = LABEL_W
        gi = 0
        col_in_group = 0
        for grp, frames_in_group in COLUMNS:
            for frame in frames_in_group:
                sheet.alpha_composite(thumb(vdir / f"{frame}.png"), (x, y))
                draw.rectangle((x, y, x + CELL, y + CELL), outline=GRID)
                x += CELL
            x += GROUP_GAP
    return sheet


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--candidates-root", type=Path, required=True)
    parser.add_argument("--out-dir", type=Path, required=True)
    args = parser.parse_args()
    args.out_dir.mkdir(parents=True, exist_ok=True)
    species = sorted(d.name for d in args.candidates_root.iterdir() if d.is_dir() and not d.name.startswith("_"))
    for sp in species:
        sheet = build_species_sheet(sp, args.candidates_root)
        out = args.out_dir / f"{sp}.png"
        sheet.save(out)
        print(f"{sp}: {sheet.size[0]}x{sheet.size[1]} -> {out}")


if __name__ == "__main__":
    main()
