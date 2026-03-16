#!/usr/bin/env python3
"""
Propagate one authored color set across all configured color variants.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image

from generate_runtime_pose_sprites import COLOR_VARIANTS, colorize


def propagate(source_dir: Path, output_root: Path) -> list[Path]:
    frame_paths = sorted(source_dir.glob("*.png"))
    if not frame_paths:
        raise ValueError(f"No authored PNG frames found in {source_dir}")

    parts = source_dir.parts
    if len(parts) < 4:
        raise ValueError("Source directory must end with species/age/gender/color")

    color_name = parts[-1]
    gender = parts[-2]
    age_stage = parts[-3]
    species = parts[-4]

    outputs: list[Path] = []
    for target_color in COLOR_VARIANTS:
        target_dir = output_root / species / age_stage / gender / target_color
        target_dir.mkdir(parents=True, exist_ok=True)
        for frame_path in frame_paths:
            image = Image.open(frame_path).convert("RGBA")
            result = image if target_color == color_name else colorize(image, target_color)
            out_path = target_dir / frame_path.name
            result.save(out_path)
            outputs.append(out_path)

    return outputs


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, required=True)
    args = parser.parse_args()

    outputs = propagate(args.source_dir, args.output_root)
    print(f"wrote {len(outputs)} frame(s)")


if __name__ == "__main__":
    main()
