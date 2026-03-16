from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Remove tiny detached particle remnants from runtime pet frames.")
    parser.add_argument("--root", default="sprites_runtime", help="Runtime pet sprite root.")
    parser.add_argument("--max-area", type=int, default=8, help="Largest detached component area to remove.")
    parser.add_argument("--min-gap", type=int, default=1, help="Required pixel gap from the main body before removing a component.")
    return parser.parse_args()


def connected_components(image: Image.Image) -> list[list[tuple[int, int]]]:
    width, height = image.size
    pixels = image.load()
    seen = [[False] * width for _ in range(height)]
    components: list[list[tuple[int, int]]] = []

    for y in range(height):
        for x in range(width):
            if seen[y][x]:
                continue

            seen[y][x] = True
            if pixels[x, y][3] == 0:
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            cells: list[tuple[int, int]] = []
            while queue:
                cx, cy = queue.popleft()
                cells.append((cx, cy))
                for nx, ny in (
                    (cx - 1, cy),
                    (cx + 1, cy),
                    (cx, cy - 1),
                    (cx, cy + 1),
                    (cx - 1, cy - 1),
                    (cx + 1, cy - 1),
                    (cx - 1, cy + 1),
                    (cx + 1, cy + 1),
                ):
                    if 0 <= nx < width and 0 <= ny < height and not seen[ny][nx]:
                        seen[ny][nx] = True
                        if pixels[nx, ny][3] > 0:
                            queue.append((nx, ny))

            components.append(cells)

    return components


def bbox(cells: list[tuple[int, int]]) -> tuple[int, int, int, int]:
    xs = [cell[0] for cell in cells]
    ys = [cell[1] for cell in cells]
    return min(xs), min(ys), max(xs), max(ys)


def gap_to_main(main_bbox: tuple[int, int, int, int], child_bbox: tuple[int, int, int, int]) -> tuple[int, int]:
    mx0, my0, mx1, my1 = main_bbox
    x0, y0, x1, y1 = child_bbox
    return max(mx0 - x1, x0 - mx1, 0), max(my0 - y1, y0 - my1, 0)


def scrub_frame(path: Path, max_area: int, min_gap: int) -> dict | None:
    image = Image.open(path).convert("RGBA")
    components = connected_components(image)
    if len(components) <= 1:
        return None

    components.sort(key=len, reverse=True)
    main_bbox = bbox(components[0])
    pixels = image.load()
    removed_components = 0
    removed_pixels = 0

    for component in components[1:]:
        area = len(component)
        if area > max_area:
            continue
        component_bbox = bbox(component)
        gap_x, gap_y = gap_to_main(main_bbox, component_bbox)
        if gap_x < min_gap and gap_y < min_gap:
            continue

        for x, y in component:
            rgba = pixels[x, y]
            if rgba[3] == 0:
                continue
            pixels[x, y] = (rgba[0], rgba[1], rgba[2], 0)
            removed_pixels += 1
        removed_components += 1

    if removed_components == 0:
        return None

    image.save(path)
    return {
        "file": str(path),
        "removed_components": removed_components,
        "removed_pixels": removed_pixels,
    }


def main() -> int:
    args = parse_args()
    root = Path(args.root).resolve()
    results: list[dict] = []
    for path in root.rglob("*.png"):
        result = scrub_frame(path, args.max_area, args.min_gap)
        if result is not None:
            results.append(result)

    summary = {
        "root": str(root),
        "files_scanned": sum(1 for _ in root.rglob("*.png")),
        "files_cleaned": len(results),
        "removed_components": sum(result["removed_components"] for result in results),
        "removed_pixels": sum(result["removed_pixels"] for result in results),
        "results": results,
    }
    summary_path = root / "cleanup-summary.json"
    summary_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"summary={summary_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
