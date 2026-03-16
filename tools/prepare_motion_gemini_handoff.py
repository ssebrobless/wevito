#!/usr/bin/env python3
"""
Prepare focused motion-family Gemini handoff packs for species animation work.
"""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw

from authored_motion_specs import MOTION_FAMILIES, get_family_layout, get_species_note
from export_species_authoring_pack import (
    AGE_ORDER,
    COLORS,
    DEFAULT_AUTHORED_ROOT,
    DEFAULT_MANIFEST,
    DEFAULT_OUTPUT_ROOT,
    DEFAULT_PACK_ROOT,
    DEFAULT_SOURCE_ROOT,
    PADDING,
    RUNTIME_CELL_SIZE,
    checkerboard,
    export_pack,
    fit_sprite,
    load_font,
    render_editable_board,
    render_runtime_reference_board,
)


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_HANDOFF_ROOT = ROOT / "incoming_sprites" / "gemini_handoff_motion"
DEFAULT_SPECIES = ["rat", "crow", "fox", "snake", "deer", "frog", "pigeon", "raccoon", "squirrel", "goose"]
GENDER_ORDER = ["male", "female"]


def scale_down(image: Image.Image, max_width: int, max_height: int) -> Image.Image:
    if image.width <= max_width and image.height <= max_height:
        return image
    scale = min(max_width / image.width, max_height / image.height)
    width = max(1, round(image.width * scale))
    height = max(1, round(image.height * scale))
    return image.resize((width, height), Image.Resampling.NEAREST)


def build_motion_upload_pack(source_dir: Path, species: str, age_stage: str, gender: str, family: str) -> Image.Image:
    family_layout = get_family_layout(family)
    base_path = source_dir / "base-pose.png"
    if not base_path.exists():
        base_path = source_dir / "1-base-pose.png"
    board_path = source_dir / f"{family}-editable-board.png"
    if not board_path.exists():
        board_path = source_dir / "2-editable-board.png"
    runtime_path = source_dir / f"{family}-runtime-reference-blue.png"
    if not runtime_path.exists():
        runtime_path = source_dir / "3-runtime-reference-blue.png"
    approved_reference_path = source_dir / "approved-motion-reference.png"

    base = scale_down(Image.open(base_path).convert("RGBA"), 760, 520)
    board = scale_down(Image.open(board_path).convert("RGBA"), 760, 520)
    runtime = scale_down(Image.open(runtime_path).convert("RGBA"), 760, 360)
    approved_reference = scale_down(Image.open(approved_reference_path).convert("RGBA"), 760, 420) if approved_reference_path.exists() else None

    margin = 18
    title_height = 56
    gap = 18
    label_height = 24
    panel_width = max(
        image.width for image in [base, board, runtime, approved_reference] if image is not None
    ) + margin * 2
    total_width = panel_width
    section_images = [base, runtime]
    if approved_reference is not None:
        section_images.append(approved_reference)
    section_images.append(board)
    total_height = (
        title_height
        + sum(image.height for image in section_images)
        + (label_height * len(section_images))
        + gap * (len(section_images) - 1)
        + margin * 2
    )

    canvas = checkerboard((total_width, total_height), cell=20, a=(247, 247, 247, 255), b=(231, 231, 231, 255))
    draw = ImageDraw.Draw(canvas)
    title_font = load_font(22)
    label_font = load_font(16)

    draw.rectangle((0, 0, total_width, title_height), fill=(38, 47, 59, 255))
    draw.text((margin, 14), f"{species} {age_stage} {gender} {family} pack", fill=(245, 248, 250, 255), font=title_font)

    y = title_height + margin
    sections = [
        ("canonical base pose", base),
        ("current motion reference", runtime),
    ]
    if approved_reference is not None:
        sections.append(("approved slither motion reference", approved_reference))
    sections.append(("editable motion board", board))
    for label, image in sections:
        draw.text((margin, y), label, fill=(34, 40, 48, 255), font=label_font)
        y += label_height
        x = (total_width - image.width) // 2
        canvas.alpha_composite(image, (x, y))
        draw.rectangle((x - 2, y - 2, x + image.width + 2, y + image.height + 2), outline=(120, 129, 140, 255), width=2)
        y += image.height + gap

    return canvas


def build_prompt(species: str, age_stage: str, gender: str, family: str, has_approved_reference: bool) -> str:
    family_spec = MOTION_FAMILIES[family]
    size_line = "male should stay slightly larger and bolder" if gender == "male" else "female should stay slightly smaller and calmer"
    age_line = {
        "adult": "adult motion should feel mature, grounded, and controlled",
        "teen": "teen motion should feel a little lighter and more alert than adult",
        "baby": "baby motion should feel smaller, softer, and slightly less coordinated",
    }[age_stage]
    frame_names = [name for row in get_family_layout(family) for name in row if name]
    frame_list = "\n".join(f"- {name}" for name in frame_names)
    focus_lines = "\n".join(f"- {line}" for line in family_spec["prompt_focus"])
    species_note = get_species_note(species, family)
    reference_block = """The single uploaded image contains:
- the canonical base pose
- the current motion reference
- the editable labeled motion board""" if not has_approved_reference else """The single uploaded image contains:
- the canonical base pose
- the current motion reference
- an approved motion reference showing the desired walk language
- the editable labeled motion board"""

    snake_reference_block = ""
    if has_approved_reference and species == "snake" and family == "locomotion":
        snake_reference_block = """
- Walk frames must follow the approved slither reference panel in the uploaded pack.
- Do NOT leave the walk frames coiled.
- Do NOT draw one continuous snake stretched across the whole board.
- If any walk frame remains coiled, the edit is incorrect."""

    interior_fill_block = ""
    if species in {"crow", "pigeon", "goose"}:
        interior_fill_block = """
- Preserve a fully filled chest and body mass in every frame.
- Do NOT hollow out the torso, chest, wing interior, or belly.
- Keep the internal shading and solid silhouette volume intact."""

    return f"""Generate an IMAGE ONLY.

Do not answer with text.
Do not explain anything.
Do not provide JSON.
Do not provide a table.

You are editing a focused pixel-art motion board for a desktop virtual pet game.

Use the uploaded {age_stage} {gender} {species} motion pack as the design anchor.
{reference_block}

Preserve the exact uploaded {species} identity, proportions, face, silhouette, outline weight, and shading style.
Do not redesign the {species}.

Treat the editable motion board panel inside the uploaded pack as the board to edit.
Keep the board layout, frame positions, transparency, and labels intact.
Do not remove labels.
Do not add labels.
Do not add extra frames.
Do not change the board dimensions.

Only improve these frames:
{frame_list}

Leave all other frames in this board unchanged.

Goals:
{focus_lines}
- {species_note}
{snake_reference_block}

Style goals:
- pixel art only
- crisp edges
- no blur
- no anti-aliasing
- preserve the original uploaded {species} appearance exactly

Important:
- {size_line}
- {age_line}
- background must be fully transparent anywhere outside the animal and board labels
- each labeled frame cell must contain a complete readable pose for that frame
- do not leave checkerboard residue, matte halos, clipped silhouettes, or bright cleanup specks
- do not change palette relationships
- do not make it look like a different artist
{interior_fill_block}

Return only the edited image.
"""


def build_readme(species: str, age_stage: str, gender: str, family: str) -> str:
    slug = f"{species}-{age_stage}-{gender}-{family}"
    return f"""Wevito Gemini motion handoff
============================

Variant
- species: {species}
- age: {age_stage}
- gender: {gender}
- family: {family}

What to do
1. Open Gemini image editing.
2. Drag this PNG into Gemini:
   - 1-upload-pack.png
3. Paste the text from 4-prompt.txt
4. Ask Gemini to return an edited image only.
5. Save the returned edited PNG into:
   - 5-save-edited-board-here\\{slug}-edited-board.png
6. Tell Codex the saved file path.
""" 


def resolve_approved_reference_path(species: str, family: str, handoff_root: Path) -> Path | None:
    if species == "snake" and family.startswith("locomotion"):
        candidate = (
            handoff_root
            / species
            / "adult"
            / "male"
            / "locomotion"
            / "5-save-edited-board-here"
            / f"{species}-adult-male-locomotion-extracted-board-import.png"
        )
        if candidate.exists():
            return candidate
    return None


def write_family_assets(species_pack_dir: Path, handoff_root: Path, species: str, age_stage: str, gender: str, family: str, runtime_root: Path, authored_root: Path) -> None:
    target_dir = handoff_root / species / age_stage / gender / family
    target_dir.mkdir(parents=True, exist_ok=True)
    save_dir = target_dir / "5-save-edited-board-here"
    save_dir.mkdir(parents=True, exist_ok=True)

    source_dir = species_pack_dir / age_stage / gender
    family_layout = get_family_layout(family)
    family_board = render_editable_board(
        Image.open(source_dir / "base-pose.png").convert("RGBA"),
        f"{species} {age_stage} {gender} {family} board",
        family_layout,
    )
    family_board_path = target_dir / f"{family}-editable-board.png"
    family_board.save(family_board_path)

    runtime_board = render_runtime_reference_board(
        runtime_root,
        authored_root,
        species,
        age_stage,
        gender,
        "blue",
        family_layout,
    )
    runtime_board_path = target_dir / f"{family}-runtime-reference-blue.png"
    runtime_board.save(runtime_board_path)

    shutil.copy2(source_dir / "base-pose.png", target_dir / "1-base-pose.png")
    shutil.copy2(family_board_path, target_dir / "2-editable-board.png")
    shutil.copy2(runtime_board_path, target_dir / "3-runtime-reference-blue.png")
    approved_reference_path = resolve_approved_reference_path(species, family, handoff_root)
    if approved_reference_path is not None:
        shutil.copy2(approved_reference_path, target_dir / "approved-motion-reference.png")
    if (source_dir / "color-strip.png").exists():
        shutil.copy2(source_dir / "color-strip.png", target_dir / "optional-color-strip.png")

    build_motion_upload_pack(target_dir, species, age_stage, gender, family).save(target_dir / "1-upload-pack.png")
    (target_dir / "4-prompt.txt").write_text(
        build_prompt(species, age_stage, gender, family, (target_dir / "approved-motion-reference.png").exists()),
        encoding="utf-8",
    )
    (target_dir / "README.txt").write_text(build_readme(species, age_stage, gender, family), encoding="utf-8")
    (target_dir / "pack-metadata.json").write_text(
        json.dumps(
            {
                "species": species,
                "ageStage": age_stage,
                "gender": gender,
                "family": family,
                "boardImage": f"{family}-editable-board.png",
                "runtimeReferenceImage": f"{family}-runtime-reference-blue.png",
                "frameLayout": family_layout,
                "colors": COLORS,
            },
            indent=2,
        ),
        encoding="utf-8",
    )


def prepare_species(species: str, family: str, pack_root: Path, source_root: Path, runtime_root: Path, authored_root: Path, output_root: Path) -> Path:
    species_pack_dir = export_pack(species, DEFAULT_MANIFEST, source_root, runtime_root, authored_root, pack_root)
    for age_stage in sorted((path.name for path in species_pack_dir.iterdir() if path.is_dir()), key=lambda age: AGE_ORDER[age]):
        for gender in GENDER_ORDER:
            if not (species_pack_dir / age_stage / gender).exists():
                continue
            write_family_assets(species_pack_dir, output_root, species, age_stage, gender, family, runtime_root, authored_root)
    return output_root / species


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--family", choices=sorted(MOTION_FAMILIES.keys()), default="locomotion")
    parser.add_argument("--species", nargs="*", default=DEFAULT_SPECIES)
    parser.add_argument("--pack-root", type=Path, default=DEFAULT_PACK_ROOT)
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--authored-root", type=Path, default=DEFAULT_AUTHORED_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_HANDOFF_ROOT)
    args = parser.parse_args()

    prepared: list[str] = []
    for species in args.species:
        prepare_species(species, args.family, args.pack_root, args.source_root, args.runtime_root, args.authored_root, args.output_root)
        prepared.append(species)

    print(json.dumps({"family": args.family, "species": prepared, "outputRoot": str(args.output_root)}, indent=2))


if __name__ == "__main__":
    main()
