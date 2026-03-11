#!/usr/bin/env python3
"""
Generate explicit baby/teen/adult animation sheets from the existing working sprite sets.

Source layout:
    sprites/<animal>/<gender>/<color>/<animation>_<frame>.png

Output layout:
    sprites/<animal>/<age>/<gender>/<color>/<animation>_<frame>.png
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SPRITES_DIR = ROOT / "sprites"

ANIMATIONS = ["idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe"]
AGES = {
    "baby": 0.78,
    "teen": 0.9,
    "adult": 1.0,
}


def fit_to_same_canvas(source: Image.Image, scale: float) -> Image.Image:
    image = source.convert("RGBA")
    bbox = image.getbbox()
    canvas = Image.new("RGBA", image.size, (0, 0, 0, 0))
    if bbox is None:
        return canvas

    sprite = image.crop(bbox)
    if scale != 1.0:
        sprite = sprite.resize(
            (
                max(1, round(sprite.width * scale)),
                max(1, round(sprite.height * scale)),
            ),
            Image.Resampling.NEAREST,
        )

    bbox_w = bbox[2] - bbox[0]
    bbox_h = bbox[3] - bbox[1]
    orig_center_x = bbox[0] + (bbox_w / 2.0)
    orig_bottom_y = bbox[3]

    paste_x = round(orig_center_x - (sprite.width / 2.0))
    paste_y = round(orig_bottom_y - sprite.height)

    paste_x = max(min(paste_x, image.width - sprite.width), 0)
    paste_y = max(min(paste_y, image.height - sprite.height), 0)
    canvas.alpha_composite(sprite, (paste_x, paste_y))
    return canvas


def generate() -> int:
    generated = 0
    for animal_dir in sorted(SPRITES_DIR.iterdir()):
        if not animal_dir.is_dir():
            continue
        gender_dirs = [p for p in animal_dir.iterdir() if p.is_dir() and p.name in {"male", "female"}]
        if not gender_dirs:
            continue

        for gender_dir in gender_dirs:
            for color_dir in sorted(p for p in gender_dir.iterdir() if p.is_dir()):
                frames = sorted(color_dir.glob("*.png"))
                if not frames:
                    continue

                for age_name, scale in AGES.items():
                    out_dir = animal_dir / age_name / gender_dir.name / color_dir.name
                    out_dir.mkdir(parents=True, exist_ok=True)
                    for frame_path in frames:
                        image = Image.open(frame_path).convert("RGBA")
                        output = fit_to_same_canvas(image, scale)
                        output.save(out_dir / frame_path.name)
                        generated += 1
    return generated


if __name__ == "__main__":
    count = generate()
    print(f"generated {count} age animation frames")
