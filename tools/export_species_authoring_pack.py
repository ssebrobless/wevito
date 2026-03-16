#!/usr/bin/env python3
"""
Export an authored-animation pack for a species using the canonical incoming sheets.
"""

from __future__ import annotations

import argparse
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont

from generate_runtime_pose_sprites import (
    CANVAS_SIZE,
    DEFAULT_MANIFEST,
    DEFAULT_OUTPUT_ROOT,
    DEFAULT_SOURCE_ROOT,
    CropOverride,
    extract_gender_pose,
    find_pose_components,
    load_manifest,
    remove_checkerboard_background,
)


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_AUTHORED_ROOT = ROOT / "sprites_authored_verified"
DEFAULT_PACK_ROOT = ROOT / "vnext" / "artifacts" / "authoring-packs"
FRAME_LAYOUT = [
    ["idle_00", "idle_01", "idle_02", "idle_03", "walk_00"],
    ["walk_01", "walk_02", "walk_03", "walk_04", "walk_05"],
    ["eat_00", "eat_01", "eat_02", "eat_03", "happy_00"],
    ["happy_01", "happy_02", "happy_03", "sad_00", "sad_01"],
    ["sleep_00", "sleep_01", "sick_00", "sick_01", "sick_02"],
    ["sick_03", "bathe_00", "bathe_01", "bathe_02", "bathe_03"],
]
ROW_COUNT = len(FRAME_LAYOUT)
COL_COUNT = len(FRAME_LAYOUT[0])
EDIT_CELL_SIZE = (148, 148)
RUNTIME_CELL_SCALE = 5
RUNTIME_CELL_SIZE = (CANVAS_SIZE[0] * RUNTIME_CELL_SCALE, CANVAS_SIZE[1] * RUNTIME_CELL_SCALE)
LABEL_HEIGHT = 24
PADDING = 16
COLORS = ["red", "orange", "yellow", "blue", "indigo", "violet"]
AGE_ORDER = {"baby": 0, "teen": 1, "adult": 2}


def load_font(size: int) -> ImageFont.ImageFont:
    for candidate in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def checkerboard(size: tuple[int, int], a: tuple[int, int, int, int], b: tuple[int, int, int, int], cell: int = 12) -> Image.Image:
    image = Image.new("RGBA", size, b)
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            fill = a if ((x // cell) + (y // cell)) % 2 == 0 else b
            draw.rectangle((x, y, min(size[0], x + cell), min(size[1], y + cell)), fill=fill)
    return image


def get_layout_size(frame_layout: list[list[str | None]]) -> tuple[int, int]:
    row_count = len(frame_layout)
    col_count = max((len(row) for row in frame_layout), default=0)
    return row_count, col_count


def fit_sprite(sprite: Image.Image, target_size: tuple[int, int]) -> Image.Image:
    scale = min(target_size[0] / sprite.width, target_size[1] / sprite.height)
    width = max(1, round(sprite.width * scale))
    height = max(1, round(sprite.height * scale))
    resized = sprite.resize((width, height), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
    x = (target_size[0] - width) // 2
    y = target_size[1] - height
    canvas.alpha_composite(resized, (x, y))
    return canvas


def render_editable_board(base_sprite: Image.Image, title: str, frame_layout: list[list[str | None]] = FRAME_LAYOUT) -> Image.Image:
    row_count, col_count = get_layout_size(frame_layout)
    width = PADDING * 2 + col_count * EDIT_CELL_SIZE[0]
    height = 54 + PADDING * 2 + row_count * (EDIT_CELL_SIZE[1] + LABEL_HEIGHT)
    sheet = checkerboard((width, height), (246, 246, 246, 255), (226, 226, 226, 255), cell=18)
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(22)
    label_font = load_font(13)
    draw.rectangle((0, 0, width, 54), fill=(40, 48, 58, 255))
    draw.text((PADDING, 14), title, fill=(245, 248, 250, 255), font=title_font)

    fitted = fit_sprite(base_sprite, (EDIT_CELL_SIZE[0] - 20, EDIT_CELL_SIZE[1] - 20))
    for row_index, row in enumerate(frame_layout):
        for col_index, frame_name in enumerate(row):
            x = PADDING + col_index * EDIT_CELL_SIZE[0]
            y = 54 + PADDING + row_index * (EDIT_CELL_SIZE[1] + LABEL_HEIGHT)
            draw.rectangle((x, y, x + EDIT_CELL_SIZE[0], y + EDIT_CELL_SIZE[1]), outline=(122, 132, 144, 255), width=2)
            tile = checkerboard((EDIT_CELL_SIZE[0] - 6, EDIT_CELL_SIZE[1] - 6), (250, 250, 250, 255), (235, 235, 235, 255), cell=16)
            if frame_name:
                tile.alpha_composite(fitted, ((tile.width - fitted.width) // 2, (tile.height - fitted.height) // 2))
            sheet.alpha_composite(tile, (x + 3, y + 3))
            draw.rectangle((x, y + EDIT_CELL_SIZE[1], x + EDIT_CELL_SIZE[0], y + EDIT_CELL_SIZE[1] + LABEL_HEIGHT), fill=(250, 250, 250, 230))
            if frame_name:
                draw.text((x + 6, y + EDIT_CELL_SIZE[1] + 4), frame_name, fill=(28, 34, 40, 255), font=label_font)

    return sheet


def resolve_frame(runtime_root: Path, authored_root: Path, species: str, age_stage: str, gender: str, color: str, frame_name: str) -> tuple[Path, bool]:
    authored = authored_root / species / age_stage / gender / color / f"{frame_name}.png"
    if authored.exists():
        return authored, True
    runtime = runtime_root / species / age_stage / gender / color / f"{frame_name}.png"
    return runtime, False


def render_runtime_reference_board(
    runtime_root: Path,
    authored_root: Path,
    species: str,
    age_stage: str,
    gender: str,
    color: str,
    frame_layout: list[list[str | None]] = FRAME_LAYOUT,
) -> Image.Image:
    row_count, col_count = get_layout_size(frame_layout)
    width = PADDING * 2 + col_count * RUNTIME_CELL_SIZE[0]
    height = 54 + PADDING * 2 + row_count * (RUNTIME_CELL_SIZE[1] + LABEL_HEIGHT)
    sheet = checkerboard((width, height), (245, 245, 245, 255), (227, 227, 227, 255), cell=16)
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(22)
    label_font = load_font(13)
    draw.rectangle((0, 0, width, 54), fill=(46, 57, 70, 255))
    draw.text((PADDING, 14), f"{species} {age_stage} {gender} runtime reference ({color})", fill=(245, 248, 250, 255), font=title_font)

    for row_index, row in enumerate(frame_layout):
        for col_index, frame_name in enumerate(row):
            x = PADDING + col_index * RUNTIME_CELL_SIZE[0]
            y = 54 + PADDING + row_index * (RUNTIME_CELL_SIZE[1] + LABEL_HEIGHT)
            if not frame_name:
                continue
            path, authored = resolve_frame(runtime_root, authored_root, species, age_stage, gender, color, frame_name)
            frame = fit_sprite(Image.open(path).convert("RGBA"), RUNTIME_CELL_SIZE)
            tile = checkerboard(RUNTIME_CELL_SIZE, (250, 250, 250, 255), (233, 233, 233, 255), cell=12)
            tile.alpha_composite(frame, (0, 0))
            border = (163, 89, 219, 255) if authored else (122, 132, 144, 255)
            sheet.alpha_composite(tile, (x, y))
            draw.rectangle((x, y, x + RUNTIME_CELL_SIZE[0], y + RUNTIME_CELL_SIZE[1]), outline=border, width=3)
            draw.rectangle((x, y + RUNTIME_CELL_SIZE[1], x + RUNTIME_CELL_SIZE[0], y + RUNTIME_CELL_SIZE[1] + LABEL_HEIGHT), fill=(250, 250, 250, 230))
            draw.text((x + 6, y + RUNTIME_CELL_SIZE[1] + 4), f"{frame_name}{' *' if authored else ''}", fill=(28, 34, 40, 255), font=label_font)

    return sheet


def render_color_strip(runtime_root: Path, authored_root: Path, species: str, age_stage: str, gender: str) -> Image.Image:
    width = PADDING * 2 + len(COLORS) * RUNTIME_CELL_SIZE[0]
    height = 54 + PADDING * 2 + RUNTIME_CELL_SIZE[1] + LABEL_HEIGHT
    sheet = checkerboard((width, height), (246, 246, 246, 255), (226, 226, 226, 255), cell=16)
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(22)
    label_font = load_font(13)
    draw.rectangle((0, 0, width, 54), fill=(54, 68, 82, 255))
    draw.text((PADDING, 14), f"{species} {age_stage} {gender} color references", fill=(245, 248, 250, 255), font=title_font)

    for index, color in enumerate(COLORS):
        x = PADDING + index * RUNTIME_CELL_SIZE[0]
        y = 54 + PADDING
        path, authored = resolve_frame(runtime_root, authored_root, species, age_stage, gender, color, "idle_00")
        frame = Image.open(path).convert("RGBA").resize(RUNTIME_CELL_SIZE, Image.Resampling.NEAREST)
        tile = checkerboard(RUNTIME_CELL_SIZE, (250, 250, 250, 255), (233, 233, 233, 255), cell=12)
        tile.alpha_composite(frame, (0, 0))
        border = (163, 89, 219, 255) if authored else (122, 132, 144, 255)
        sheet.alpha_composite(tile, (x, y))
        draw.rectangle((x, y, x + RUNTIME_CELL_SIZE[0], y + RUNTIME_CELL_SIZE[1]), outline=border, width=3)
        draw.rectangle((x, y + RUNTIME_CELL_SIZE[1], x + RUNTIME_CELL_SIZE[0], y + RUNTIME_CELL_SIZE[1] + LABEL_HEIGHT), fill=(250, 250, 250, 230))
        draw.text((x + 8, y + RUNTIME_CELL_SIZE[1] + 4), color, fill=(28, 34, 40, 255), font=label_font)

    return sheet


def write_brief(path: Path, species: str, age_stage: str, gender: str, files: dict[str, Path]) -> None:
    frame_lines = "\n".join(
        f"- row {row_index + 1}: {', '.join(row)}"
        for row_index, row in enumerate(FRAME_LAYOUT)
    )
    text = f"""# {species} {age_stage} {gender} authored animation brief

Use the canonical pose from:
- `{files["base_pose"]}`

Edit on top of this labeled board:
- `{files["editable_board"]}`

Current runtime reference:
- `{files["runtime_reference"]}`

Color references:
- `{files["color_strip"]}`

Rules:
- preserve the exact character identity from the canonical source pose
- keep transparent background
- keep each frame centered consistently
- keep the feet / ground contact stable across walk, idle, eat, sleep
- pixel art only, crisp edges, no blur, no anti-aliasing
- do not redesign the animal; animate the uploaded look
- male should stay slightly larger and bolder
- female should stay slightly smaller and calmer
- output individual transparent PNG frames after editing

Frame layout:
{frame_lines}

Suggested image-edit workflow:
1. lock the canonical pose as the source look
2. use the editable board as the target canvas
3. edit one action set at a time, starting with idle and walk
4. compare against the runtime reference board, but improve motion rather than copying the synthesized transforms
5. once frames are approved, export transparent PNGs back into:
   `sprites_authored/{species}/{age_stage}/{gender}/<color>/<animation>_<nn>.png`
"""
    path.write_text(text, encoding="utf-8")


def save_image(path: Path, image: Image.Image) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)


def iter_species_entries(manifest: list, species: str) -> Iterable:
    return [entry for entry in manifest if entry.species == species]


def export_pack(species: str, manifest_path: Path, source_root: Path, runtime_root: Path, authored_root: Path, output_root: Path) -> Path:
    manifest = load_manifest(manifest_path)
    species_entries = sorted(iter_species_entries(manifest, species), key=lambda entry: AGE_ORDER[entry.age_stage])
    if not species_entries:
        raise ValueError(f"No manifest entries found for species '{species}'.")

    species_root = output_root / species
    species_root.mkdir(parents=True, exist_ok=True)

    for entry in species_entries:
        source_path = source_root / entry.source
        image = Image.open(source_path).convert("RGBA")
        isolated = remove_checkerboard_background(image)
        components = find_pose_components(image)
        for gender, bbox in zip(entry.component_order, components, strict=True):
            sprite = extract_gender_pose(
                isolated,
                bbox,
                entry.crop_override if hasattr(entry, "crop_override") else CropOverride(),
                entry.species,
            )
            target_dir = species_root / entry.age_stage / gender
            target_dir.mkdir(parents=True, exist_ok=True)

            base_pose_path = target_dir / "base-pose.png"
            editable_board_path = target_dir / "editable-board.png"
            runtime_reference_path = target_dir / "runtime-reference-blue.png"
            color_strip_path = target_dir / "color-strip.png"
            brief_path = target_dir / "authoring-brief.md"

            save_image(base_pose_path, sprite)
            save_image(editable_board_path, render_editable_board(sprite, f"{species} {entry.age_stage} {gender} editable board"))
            save_image(runtime_reference_path, render_runtime_reference_board(runtime_root, authored_root, species, entry.age_stage, gender, "blue"))
            save_image(color_strip_path, render_color_strip(runtime_root, authored_root, species, entry.age_stage, gender))
            write_brief(
                brief_path,
                species,
                entry.age_stage,
                gender,
                {
                    "base_pose": base_pose_path,
                    "editable_board": editable_board_path,
                    "runtime_reference": runtime_reference_path,
                    "color_strip": color_strip_path,
                },
            )

    index_path = species_root / "index.txt"
    index_path.write_text(
        "\n".join(str(path) for path in sorted(species_root.rglob("*")) if path.is_file()),
        encoding="utf-8",
    )
    return species_root


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--species", default="rat")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--authored-root", type=Path, default=DEFAULT_AUTHORED_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_PACK_ROOT)
    args = parser.parse_args()

    pack_root = export_pack(
        args.species,
        args.manifest,
        args.source_root,
        args.runtime_root,
        args.authored_root,
        args.output_root,
    )
    print(pack_root)


if __name__ == "__main__":
    main()
