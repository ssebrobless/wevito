#!/usr/bin/env python3
"""
Generate explicit color portrait variants from neutral age/gender portrait bases.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PORTRAITS_DIR = ROOT / "sprites" / "portraits"

COLORS: dict[str, tuple[int, int, int]] = {
    "red": (255, 107, 107),
    "orange": (255, 168, 77),
    "yellow": (255, 224, 102),
    "blue": (115, 191, 252),
    "indigo": (115, 143, 252),
    "violet": (217, 120, 242),
}


def tint_image(source: Image.Image, tint_rgb: tuple[int, int, int], strength: float = 0.5) -> Image.Image:
    image = source.convert("RGBA")
    pixels = image.load()
    tint_r, tint_g, tint_b = tint_rgb

    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue

            brightness = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0
            target_r = int(tint_r * brightness)
            target_g = int(tint_g * brightness)
            target_b = int(tint_b * brightness)

            new_r = int(round((r * (1.0 - strength)) + (target_r * strength)))
            new_g = int(round((g * (1.0 - strength)) + (target_g * strength)))
            new_b = int(round((b * (1.0 - strength)) + (target_b * strength)))
            pixels[x, y] = (
                max(0, min(255, new_r)),
                max(0, min(255, new_g)),
                max(0, min(255, new_b)),
                a,
            )

    return image


def generate_variants() -> int:
    generated = 0
    for animal_dir in sorted(PORTRAITS_DIR.iterdir()):
        if not animal_dir.is_dir():
            continue
        for base_path in sorted(animal_dir.glob("*.png")):
            stem = base_path.stem
            parts = stem.split("_")
            if len(parts) != 2:
                continue
            age, gender = parts
            if age not in {"baby", "teen", "adult"} or gender not in {"male", "female"}:
                continue

            source = Image.open(base_path).convert("RGBA")
            for color_name, tint_rgb in COLORS.items():
                out_path = animal_dir / f"{age}_{gender}_{color_name}.png"
                tinted = tint_image(source, tint_rgb)
                tinted.save(out_path)
                generated += 1
    return generated


if __name__ == "__main__":
    count = generate_variants()
    print(f"generated {count} portrait variants")
