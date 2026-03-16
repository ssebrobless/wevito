from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


CELL_PADDING = 18
HEADER_HEIGHT = 34
LABEL_HEIGHT = 28
BACKGROUND = (20, 24, 28, 255)
PANEL = (36, 42, 47, 255)
OUTLINE = (82, 92, 99, 255)
TEXT = (235, 239, 242, 255)
SUBTEXT = (181, 190, 198, 255)


def load_summary(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_font(size: int) -> ImageFont.ImageFont:
    try:
        return ImageFont.truetype("C:/Windows/Fonts/consola.ttf", size)
    except OSError:
        return ImageFont.load_default()


def normalize_image(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def fit_image(image: Image.Image, max_width: int, max_height: int) -> Image.Image:
    scale = min(max_width / image.width, max_height / image.height)
    size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    return image.resize(size, Image.Resampling.NEAREST)


def compose_board(summary_path: Path, output_path: Path, columns: int) -> Path:
    summary = load_summary(summary_path)
    results = summary.get("results", [])
    if not results:
        raise ValueError(f"No results found in {summary_path}")

    captures: list[tuple[str, Image.Image]] = []
    max_width = 0
    max_height = 0
    for item in results:
        label = item["species"]
        image = normalize_image(Path(item["focused_home"]))
        captures.append((label, image))
        max_width = max(max_width, image.width)
        max_height = max(max_height, image.height)

    cell_width = max_width + (CELL_PADDING * 2)
    cell_height = HEADER_HEIGHT + max_height + LABEL_HEIGHT + CELL_PADDING
    rows = (len(captures) + columns - 1) // columns
    board = Image.new(
        "RGBA",
        (cell_width * columns, cell_height * rows),
        BACKGROUND,
    )
    draw = ImageDraw.Draw(board)
    header_font = load_font(16)
    label_font = load_font(14)

    for index, (label, image) in enumerate(captures):
        row = index // columns
        column = index % columns
        origin_x = column * cell_width
        origin_y = row * cell_height

        panel_rect = (
            origin_x + 8,
            origin_y + 8,
            origin_x + cell_width - 8,
            origin_y + cell_height - 8,
        )
        draw.rounded_rectangle(panel_rect, radius=14, fill=PANEL, outline=OUTLINE, width=1)

        draw.text((origin_x + 18, origin_y + 14), label.upper(), font=header_font, fill=TEXT)

        fitted = fit_image(image, max_width, max_height)
        image_x = origin_x + ((cell_width - fitted.width) // 2)
        image_y = origin_y + HEADER_HEIGHT
        board.alpha_composite(fitted, (image_x, image_y))

        draw.text(
            (origin_x + 18, origin_y + cell_height - LABEL_HEIGHT - 4),
            Path(summary_path).stem,
            font=label_font,
            fill=SUBTEXT,
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    board.save(output_path)
    return output_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Compose a single audit board from a sprite audit summary.")
    parser.add_argument("summary", type=Path, help="Path to sprite audit summary.json")
    parser.add_argument("--output", type=Path, required=True, help="Output PNG path")
    parser.add_argument("--columns", type=int, default=3, help="Number of columns in the board")
    args = parser.parse_args()

    output = compose_board(args.summary, args.output, max(1, args.columns))
    print(output)


if __name__ == "__main__":
    main()
