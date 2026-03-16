#!/usr/bin/env python3
"""
Crop the editable-board section back out of a Gemini upload-pack result image.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


TITLE_HEIGHT = 56
MARGIN = 18
LABEL_HEIGHT = 24
GAP = 18


def scale_down(image: Image.Image, max_width: int, max_height: int) -> Image.Image:
    if image.width <= max_width and image.height <= max_height:
        return image
    scale = min(max_width / image.width, max_height / image.height)
    width = max(1, round(image.width * scale))
    height = max(1, round(image.height * scale))
    return image.resize((width, height), Image.Resampling.NEAREST)


def compute_source_pack_layout(
    base: Image.Image,
    runtime: Image.Image,
    board: Image.Image,
    approved_reference: Image.Image | None = None,
) -> dict[str, int]:
    section_images = [base, runtime]
    if approved_reference is not None:
        section_images.append(approved_reference)
    section_images.append(board)
    panel_width = max(image.width for image in section_images) + MARGIN * 2
    total_width = panel_width
    total_height = TITLE_HEIGHT + MARGIN * 2
    total_height += sum(image.height + LABEL_HEIGHT for image in section_images)
    total_height += GAP * (len(section_images) - 1)

    y = TITLE_HEIGHT + MARGIN
    y += LABEL_HEIGHT
    base_top = y
    base_left = (total_width - base.width) // 2
    y += base.height + GAP

    y += LABEL_HEIGHT
    runtime_top = y
    runtime_left = (total_width - runtime.width) // 2
    y += runtime.height + GAP

    approved_top = None
    approved_left = None
    if approved_reference is not None:
        y += LABEL_HEIGHT
        approved_top = y
        approved_left = (total_width - approved_reference.width) // 2
        y += approved_reference.height + GAP

    y += LABEL_HEIGHT
    board_top = y
    board_left = (total_width - board.width) // 2

    layout = {
        "total_width": total_width,
        "total_height": total_height,
        "base_left": base_left,
        "base_top": base_top,
        "runtime_left": runtime_left,
        "runtime_top": runtime_top,
        "board_left": board_left,
        "board_top": board_top,
    }
    if approved_top is not None and approved_left is not None:
        layout["approved_left"] = approved_left
        layout["approved_top"] = approved_top
    return layout


def extract_board(result_pack_path: Path, source_dir: Path, output_path: Path, board_image_name: str = "editable-board.png") -> Path:
    base_path = source_dir / "base-pose.png"
    if not base_path.exists():
        base_path = source_dir / "1-base-pose.png"
    runtime_path = source_dir / "runtime-reference-blue.png"
    if not runtime_path.exists():
        runtime_path = source_dir / "3-runtime-reference-blue.png"
    board_path = source_dir / board_image_name
    if not board_path.exists():
        board_path = source_dir / "2-editable-board.png"
    approved_reference_path = source_dir / "approved-motion-reference.png"

    base = Image.open(base_path).convert("RGBA")
    runtime = Image.open(runtime_path).convert("RGBA")
    board = Image.open(board_path).convert("RGBA")
    approved_reference = Image.open(approved_reference_path).convert("RGBA") if approved_reference_path.exists() else None
    result = Image.open(result_pack_path).convert("RGBA")

    base = scale_down(base, 760, 520)
    runtime = scale_down(runtime, 760, 360)
    board = scale_down(board, 760, 520)
    if approved_reference is not None:
        approved_reference = scale_down(approved_reference, 760, 420)

    layout = compute_source_pack_layout(base, runtime, board, approved_reference)
    scale_x = result.width / layout["total_width"]
    scale_y = result.height / layout["total_height"]
    board_left = round(layout["board_left"] * scale_x)
    board_top = round(layout["board_top"] * scale_y)
    board_width = round(board.width * scale_x)
    board_height = round(board.height * scale_y)
    cropped = result.crop((board_left, board_top, board_left + board_width, board_top + board_height))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(output_path)
    return output_path


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--result-pack", type=Path, required=True)
    parser.add_argument("--source-dir", type=Path, required=True)
    parser.add_argument("--output-board", type=Path, required=True)
    parser.add_argument("--board-image-name", default="editable-board.png")
    args = parser.parse_args()

    output = extract_board(args.result_pack, args.source_dir, args.output_board, args.board_image_name)
    print(output)


if __name__ == "__main__":
    main()
