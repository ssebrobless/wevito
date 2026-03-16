#!/usr/bin/env python3
"""
Prepare a simple Gemini handoff folder with numbered upload assets and a prompt.
"""

from __future__ import annotations

import argparse
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PACK_ROOT = ROOT / "vnext" / "artifacts" / "authoring-packs"
DEFAULT_OUTPUT_ROOT = ROOT / "incoming_sprites" / "gemini_handoff"

AGE_ORDER = ["adult", "teen", "baby"]
GENDER_ORDER = ["male", "female"]


def load_font(size: int) -> ImageFont.ImageFont:
    for candidate in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def checkerboard(size: tuple[int, int], cell: int = 18) -> Image.Image:
    a = (247, 247, 247, 255)
    b = (231, 231, 231, 255)
    image = Image.new("RGBA", size, b)
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            fill = a if ((x // cell) + (y // cell)) % 2 == 0 else b
            draw.rectangle((x, y, min(size[0], x + cell), min(size[1], y + cell)), fill=fill)
    return image


def build_upload_pack(source_dir: Path, species: str, age_stage: str, gender: str) -> Image.Image:
    base = Image.open(source_dir / "base-pose.png").convert("RGBA")
    board = Image.open(source_dir / "editable-board.png").convert("RGBA")
    runtime = Image.open(source_dir / "runtime-reference-blue.png").convert("RGBA")

    margin = 18
    title_height = 56
    gap = 18
    panel_width = max(base.width, board.width, runtime.width) + margin * 2
    total_width = panel_width
    total_height = title_height + (base.height + runtime.height + board.height) + gap * 2 + margin * 2

    canvas = checkerboard((total_width, total_height), cell=20)
    draw = ImageDraw.Draw(canvas)
    title_font = load_font(22)
    label_font = load_font(16)

    draw.rectangle((0, 0, total_width, title_height), fill=(38, 47, 59, 255))
    draw.text((margin, 14), f"{species} {age_stage} {gender} upload pack", fill=(245, 248, 250, 255), font=title_font)

    y = title_height + margin
    sections = [
        ("canonical base pose", base),
        ("current runtime reference", runtime),
        ("editable labeled board", board),
    ]
    for label, image in sections:
        draw.text((margin, y), label, fill=(34, 40, 48, 255), font=label_font)
        y += 24
        x = (total_width - image.width) // 2
        canvas.alpha_composite(image, (x, y))
        draw.rectangle((x - 2, y - 2, x + image.width + 2, y + image.height + 2), outline=(120, 129, 140, 255), width=2)
        y += image.height + gap

    return canvas


def build_prompt(species: str, age_stage: str, gender: str) -> str:
    size_line = {
        "male": "male should stay slightly larger and bolder",
        "female": "female should stay slightly smaller and calmer",
    }[gender]
    age_line = {
        "adult": "adult motion should feel mature, grounded, and controlled",
        "teen": "teen motion should feel a little lighter and more alert than adult",
        "baby": "baby motion should feel smaller, softer, and slightly less coordinated",
    }[age_stage]
    species_motion_line = {
        "snake": (
            "Walk must become a real side-view slither: the body should extend horizontally, flow through a readable S-curve, "
            "and show traveling body motion across walk_00 through walk_05. Idle should return to the calm neutral coiled pose. "
            "Each walk cell must contain a complete full-body snake pose inside that labeled cell; do not draw one continuous snake stretched across multiple walk cells."
        ),
        "pigeon": (
            "Walk should read as a grounded hop-step with subtle head bob and tiny wing balance motion. "
            "Preserve the full chest, belly, wing, and tail silhouette in every frame with no missing body chunks."
        ),
        "crow": (
            "Walk should read as a grounded hop-step with tiny wing balance motion. "
            "Keep the leg gap clean and remove any bright outline noise around the body."
        ),
        "fox": (
            "Walk should clearly use the front and rear legs instead of a body bounce, with a subtle tail wag and a fully preserved muzzle silhouette."
        ),
        "rat": (
            "Walk should clearly use the front and rear paws instead of a body bounce, with a subtle tail wag and fully preserved whisker and nose silhouette."
        ),
    }.get(species, "Walk should read as grounded, species-appropriate side-view motion with stable baseline and a fully preserved silhouette.")
    species_repair_line = {
        "snake": "Repair any squashed or overly zoomed-in body framing. Do not let the snake read as only a head and neck.",
        "pigeon": "Repair any cropped or missing torso, chest, belly, wing, or tail pixels anywhere on the board.",
        "crow": "Remove any checkerboard or bright noise around the outline and between the legs.",
        "fox": "Repair any muzzle or snout clipping immediately. The full fox head silhouette must stay intact.",
        "rat": "Repair any nose, whisker, or face clipping immediately. The full rat head silhouette must stay intact.",
    }.get(species, "Repair any cropped silhouette, detached body chunks, or leftover background residue on the board.")
    return f"""Generate an IMAGE ONLY.

Do not answer with text.
Do not explain anything.
Do not provide JSON.
Do not provide a table.

You are editing an existing pixel-art animation board for a small desktop virtual pet game.

Use the uploaded {age_stage} {gender} {species} reference pack as the design anchor.
The single uploaded image contains:
- the canonical base pose
- the current runtime reference
- the editable labeled animation board

Preserve the exact uploaded {species} identity, proportions, face, silhouette, outline weight, paws/feet placement language, and shading style.
Do not redesign the {species}.

Treat the editable labeled board panel inside the uploaded reference pack as the board to edit.
Keep the board layout, frame positions, transparency, and labels intact.
Do not remove labels.
Do not add labels.
Do not add extra frames.
Do not change the board dimensions.

Only improve these frames:
- idle_00
- idle_01
- idle_02
- idle_03
- walk_00
- walk_01
- walk_02
- walk_03
- walk_04
- walk_05

Also repair any frame anywhere on the board that has:
- cropped body parts
- missing silhouette chunks
- bright outline specks
- checkerboard/background residue

Leave all other frames visually unchanged.

Idle goals:
- subtle breathing
- tiny head motion
- tiny ear / whisker / blink variation only when appropriate
- no exaggerated bounce

Walk goals:
- grounded side-view walk
- clear foot alternation
- slight body weight shift
- stable baseline
- no jitter
- no sliding
- no exaggerated vertical bobbing
- {species_motion_line}

Style goals:
- pixel art only
- crisp edges
- no blur
- no anti-aliasing
- slightly moody, cute, vulnerable tone
- preserve the original uploaded {species} appearance exactly

Important:
- {size_line}
- {age_line}
- {species_repair_line}
- background must be fully transparent anywhere outside the animal and board labels
- do not leave checkerboard residue, matte halos, or bright cleanup specks
- do not repaint untouched frames
- do not change palette relationships
- do not make it look like a different artist

Return only the edited image.
"""


def build_readme(species: str, age_stage: str, gender: str) -> str:
    slug = f"{species}-{age_stage}-{gender}"
    return f"""Wevito Gemini handoff
=====================

Variant
- species: {species}
- age: {age_stage}
- gender: {gender}

What to do
1. Open Gemini image editing.
2. Drag this PNG into Gemini:
   - 1-upload-pack.png
3. Paste the text from 4-prompt.txt
4. Ask Gemini to return an edited image only.
5. Save the returned edited PNG into:
   - 5-save-edited-board-here\\{slug}-edited-board.png
6. Tell Codex the saved file path.

Why there is 1 upload pack
- it combines the canonical base pose, runtime reference, and editable board into one image
- this is more reliable for automation and keeps Gemini anchored to the existing sprite look
"""


def stage_variant(species_root: Path, output_root: Path, species: str, age_stage: str, gender: str) -> None:
    source_dir = species_root / age_stage / gender
    if not source_dir.exists():
        return

    target_dir = output_root / species / age_stage / gender
    target_dir.mkdir(parents=True, exist_ok=True)
    save_dir = target_dir / "5-save-edited-board-here"
    save_dir.mkdir(parents=True, exist_ok=True)

    file_map = {
        "base-pose.png": "1-base-pose.png",
        "editable-board.png": "2-editable-board.png",
        "runtime-reference-blue.png": "3-runtime-reference-blue.png",
        "color-strip.png": "optional-color-strip.png",
    }

    for source_name, target_name in file_map.items():
        source_path = source_dir / source_name
        if source_path.exists():
            shutil.copy2(source_path, target_dir / target_name)

    build_upload_pack(source_dir, species, age_stage, gender).save(target_dir / "1-upload-pack.png")

    (target_dir / "4-prompt.txt").write_text(build_prompt(species, age_stage, gender), encoding="utf-8")
    (target_dir / "README.txt").write_text(build_readme(species, age_stage, gender), encoding="utf-8")


def prepare_species(species: str, pack_root: Path, output_root: Path) -> Path:
    species_root = pack_root / species
    if not species_root.exists():
        raise FileNotFoundError(f"Authoring pack not found: {species_root}")

    target_root = output_root / species
    target_root.mkdir(parents=True, exist_ok=True)

    for age_stage in AGE_ORDER:
        for gender in GENDER_ORDER:
            stage_variant(species_root, output_root, species, age_stage, gender)

    index_lines: list[str] = []
    for age_stage in AGE_ORDER:
        for gender in GENDER_ORDER:
            variant_dir = target_root / age_stage / gender
            if variant_dir.exists():
                index_lines.append(str(variant_dir))
    (target_root / "index.txt").write_text("\n".join(index_lines) + "\n", encoding="utf-8")
    return target_root


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--species", default="rat")
    parser.add_argument("--pack-root", type=Path, default=DEFAULT_PACK_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    args = parser.parse_args()

    result = prepare_species(args.species, args.pack_root, args.output_root)
    print(result)


if __name__ == "__main__":
    main()
