from __future__ import annotations

import argparse
import os
import json
import math
import shutil
from collections import Counter, deque
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

from PIL import Image
import numpy as np


DEFAULT_CLEAN_FOLDERS = (
    "environment",
    "items",
    "status",
)

DEFAULT_COPY_FOLDERS = (
    "icons",
    "celestial",
    "portraits",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Clean shared Wevito sprite assets into a runtime-ready transparent tree.")
    parser.add_argument("--source", default="sprites", help="Source shared sprite root.")
    parser.add_argument("--output", default="sprites_shared_runtime", help="Output cleaned sprite root.")
    parser.add_argument(
        "--clean-folders",
        nargs="*",
        default=list(DEFAULT_CLEAN_FOLDERS),
        help="Folders under the source root that should go through background cleanup.",
    )
    parser.add_argument(
        "--copy-folders",
        nargs="*",
        default=list(DEFAULT_COPY_FOLDERS),
        help="Folders under the source root that should be copied through untouched.",
    )
    parser.add_argument(
        "--workers",
        type=int,
        default=max(1, (os.cpu_count() or 2) - 1),
        help="Number of worker processes for cleanup work.",
    )
    return parser.parse_args()


def quantize(rgb: tuple[int, int, int], step: int = 8) -> tuple[int, int, int]:
    return tuple(int(round(channel / step) * step) for channel in rgb)


def color_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def saturation(rgb: tuple[int, int, int]) -> int:
    return max(rgb) - min(rgb)


def brightness(rgb: tuple[int, int, int]) -> float:
    return (rgb[0] + rgb[1] + rgb[2]) / 3


def is_neutral(rgb: tuple[int, int, int], max_saturation: int = 28) -> bool:
    return saturation(rgb) <= max_saturation


def collect_border_pixels(image: Image.Image) -> list[tuple[int, int, int]]:
    width, height = image.size
    pixels = image.load()
    colors: list[tuple[int, int, int]] = []
    for x in range(width):
        for y in (0, height - 1):
            rgba = pixels[x, y]
            if rgba[3] > 0:
                colors.append(rgba[:3])
    for y in range(height):
        for x in (0, width - 1):
            rgba = pixels[x, y]
            if rgba[3] > 0:
                colors.append(rgba[:3])
    return colors


def resolve_background_palette(border_pixels: list[tuple[int, int, int]]) -> list[tuple[int, int, int]]:
    if not border_pixels:
        return []

    quantized_counts = Counter(quantize(color) for color in border_pixels)
    palette: list[tuple[int, int, int]] = []
    for color, count in quantized_counts.most_common(8):
        if count < 4:
            continue
        if saturation(color) > 42 and count < 24:
            continue
        if not is_neutral(color) and count < 16:
            continue
        palette.append(color)
    return palette[:6]


def build_background_mask(image: Image.Image, palette: list[tuple[int, int, int]]) -> list[list[bool]]:
    width, height = image.size
    pixels = image.load()
    mask = [[False for _ in range(width)] for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()

    def is_background_like(px: int, py: int) -> bool:
        rgba = pixels[px, py]
        if rgba[3] == 0:
            return True
        rgb = rgba[:3]
        if not palette:
            return False
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        threshold = 52 if is_neutral(rgb) else 38
        return nearest <= threshold

    for x in range(width):
        for y in (0, height - 1):
            if not mask[y][x] and is_background_like(x, y):
                mask[y][x] = True
                queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if not mask[y][x] and is_background_like(x, y):
                mask[y][x] = True
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1), (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1)):
            if nx < 0 or ny < 0 or nx >= width or ny >= height or mask[ny][nx]:
                continue
            if is_background_like(nx, ny):
                mask[ny][nx] = True
                queue.append((nx, ny))

    return mask


def apply_mask(image: Image.Image, mask: list[list[bool]], palette: list[tuple[int, int, int]]) -> tuple[Image.Image, int]:
    width, height = image.size
    pixels = image.load()
    cleaned = image.copy()
    cleaned_pixels = cleaned.load()
    removed = 0

    for y in range(height):
        for x in range(width):
            if mask[y][x]:
                rgba = cleaned_pixels[x, y]
                if rgba[3] != 0:
                    cleaned_pixels[x, y] = (rgba[0], rgba[1], rgba[2], 0)
                    removed += 1

    for y in range(height):
        for x in range(width):
            if mask[y][x]:
                continue
            rgba = cleaned_pixels[x, y]
            if rgba[3] == 0 or not palette:
                continue

            near_background = min(color_distance(rgba[:3], candidate) for candidate in palette)
            if near_background > 46:
                continue

            touching_transparent = False
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1), (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1)):
                if nx < 0 or ny < 0 or nx >= width or ny >= height:
                    touching_transparent = True
                    break
                if mask[ny][nx]:
                    touching_transparent = True
                    break
            if not touching_transparent:
                continue

            if near_background <= 24:
                cleaned_pixels[x, y] = (rgba[0], rgba[1], rgba[2], 0)
                removed += 1
            else:
                scaled_alpha = max(0, min(rgba[3], int((near_background - 24) / 22 * rgba[3])))
                cleaned_pixels[x, y] = (rgba[0], rgba[1], rgba[2], scaled_alpha)

    return cleaned, removed


def prune_border_connected_background(image: Image.Image, palette: list[tuple[int, int, int]]) -> tuple[Image.Image, int]:
    if not palette:
        return image, 0

    width, height = image.size
    source_pixels = image.load()
    cleaned = image.copy()
    cleaned_pixels = cleaned.load()
    visited = [[False for _ in range(width)] for _ in range(height)]
    queue: deque[tuple[int, int]] = deque()
    removed = 0

    def is_background_residue(px: int, py: int) -> bool:
        rgba = source_pixels[px, py]
        if rgba[3] == 0:
            return False
        rgb = rgba[:3]
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        return (
            nearest <= 64
            and is_neutral(rgb, max_saturation=34)
            and brightness(rgb) <= 196
        )

    for x in range(width):
        for y in (0, height - 1):
            if not visited[y][x] and is_background_residue(x, y):
                visited[y][x] = True
                queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if not visited[y][x] and is_background_residue(x, y):
                visited[y][x] = True
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        rgba = cleaned_pixels[x, y]
        if rgba[3] != 0:
            cleaned_pixels[x, y] = (rgba[0], rgba[1], rgba[2], 0)
            removed += 1

        for nx, ny in (
            (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
            (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
        ):
            if nx < 0 or ny < 0 or nx >= width or ny >= height or visited[ny][nx]:
                continue
            if not is_background_residue(nx, ny):
                continue
            visited[ny][nx] = True
            queue.append((nx, ny))

    return cleaned, removed


def scrub_checker_matte(image: Image.Image, palette: list[tuple[int, int, int]]) -> tuple[Image.Image, int]:
    if not palette:
        return image, 0

    width, height = image.size
    source = image.load()
    cleaned = image.copy()
    target = cleaned.load()
    removed = 0

    def matches_checker(rgb: tuple[int, int, int]) -> bool:
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        return nearest <= 58 and is_neutral(rgb, max_saturation=34)

    for y in range(height):
        for x in range(width):
            rgba = source[x, y]
            if rgba[3] == 0 or not matches_checker(rgba[:3]):
                continue

            background_neighbors = 0
            solid_neighbors = 0
            for nx, ny in (
                (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
                (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
            ):
                if nx < 0 or ny < 0 or nx >= width or ny >= height:
                    background_neighbors += 1
                    continue
                neighbor = source[nx, ny]
                if neighbor[3] == 0 or matches_checker(neighbor[:3]):
                    background_neighbors += 1
                else:
                    solid_neighbors += 1

            if background_neighbors >= 5 and solid_neighbors <= 3:
                target[x, y] = (rgba[0], rgba[1], rgba[2], 0)
                removed += 1
            elif background_neighbors >= 4 and solid_neighbors <= 2:
                target[x, y] = (rgba[0], rgba[1], rgba[2], max(0, int(rgba[3] * 0.35)))

    return cleaned, removed


def strip_palette_background(image: Image.Image, palette: list[tuple[int, int, int]]) -> tuple[Image.Image, int]:
    if not palette:
        return image, 0

    width, height = image.size
    source = image.load()
    cleaned = image.copy()
    target = cleaned.load()
    removed = 0

    for y in range(height):
        for x in range(width):
            rgba = source[x, y]
            if rgba[3] == 0:
                continue

            rgb = rgba[:3]
            nearest = min(color_distance(rgb, candidate) for candidate in palette)
            if nearest <= 34 and is_neutral(rgb, max_saturation=36):
                target[x, y] = (rgba[0], rgba[1], rgba[2], 0)
                removed += 1

    return cleaned, removed


def prune_edge_noise(image: Image.Image) -> tuple[Image.Image, int]:
    width, height = image.size
    pixels = image.load()
    visited = [[False for _ in range(width)] for _ in range(height)]
    cleaned = image.copy()
    cleaned_pixels = cleaned.load()
    removed = 0
    max_border_component = max(32, int(width * height * 0.008))
    max_interior_speck = max(12, int(width * height * 0.0002))

    for y in range(height):
        for x in range(width):
            if visited[y][x] or pixels[x, y][3] <= 4:
                continue

            stack = [(x, y)]
            visited[y][x] = True
            component: list[tuple[int, int]] = []
            touches_border = False

            while stack:
                cx, cy = stack.pop()
                component.append((cx, cy))
                if cx == 0 or cy == 0 or cx == width - 1 or cy == height - 1:
                    touches_border = True

                for nx, ny in (
                    (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1),
                    (cx - 1, cy - 1), (cx + 1, cy - 1), (cx - 1, cy + 1), (cx + 1, cy + 1),
                ):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height or visited[ny][nx]:
                        continue
                    if pixels[nx, ny][3] <= 4:
                        continue
                    visited[ny][nx] = True
                    stack.append((nx, ny))

            limit = max_border_component if touches_border else max_interior_speck
            if len(component) > limit:
                continue

            for cx, cy in component:
                rgba = cleaned_pixels[cx, cy]
                if rgba[3] == 0:
                    continue
                cleaned_pixels[cx, cy] = (rgba[0], rgba[1], rgba[2], 0)
                removed += 1

    return cleaned, removed


def otsu_threshold(values: np.ndarray) -> int:
    if values.size == 0:
        return 0

    hist, _ = np.histogram(values, bins=256, range=(0, 256))
    total = values.size
    weighted_sum = float(np.dot(np.arange(256), hist))
    background_weight = 0.0
    background_sum = 0.0
    best_threshold = 0
    best_variance = -1.0

    for value in range(256):
        background_weight += hist[value]
        if background_weight <= 0:
            continue

        foreground_weight = total - background_weight
        if foreground_weight <= 0:
            break

        background_sum += value * hist[value]
        mean_background = background_sum / background_weight
        mean_foreground = (weighted_sum - background_sum) / foreground_weight
        variance = background_weight * foreground_weight * ((mean_background - mean_foreground) ** 2)
        if variance > best_variance:
            best_variance = variance
            best_threshold = value

    return best_threshold


def dilate(mask: np.ndarray, iterations: int = 1) -> np.ndarray:
    result = mask.copy()
    for _ in range(iterations):
        padded = np.pad(result, 1, constant_values=False)
        grown = result.copy()
        for dy in range(3):
            for dx in range(3):
                grown |= padded[dy:dy + result.shape[0], dx:dx + result.shape[1]]
        result = grown
    return result


def erode(mask: np.ndarray, iterations: int = 1) -> np.ndarray:
    result = mask.copy()
    for _ in range(iterations):
        padded = np.pad(result, 1, constant_values=False)
        shrunk = result.copy()
        for dy in range(3):
            for dx in range(3):
                shrunk &= padded[dy:dy + result.shape[0], dx:dx + result.shape[1]]
        result = shrunk
    return result


def fill_holes(mask: np.ndarray) -> np.ndarray:
    height, width = mask.shape
    visited = np.zeros_like(mask, dtype=bool)
    queue: deque[tuple[int, int]] = deque()

    def enqueue(x: int, y: int) -> None:
        if 0 <= x < width and 0 <= y < height and not mask[y, x] and not visited[y, x]:
            visited[y, x] = True
            queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            enqueue(nx, ny)

    return mask | (~mask & ~visited)


def connected_components(mask: np.ndarray) -> list[list[tuple[int, int]]]:
    height, width = mask.shape
    visited = np.zeros_like(mask, dtype=bool)
    components: list[list[tuple[int, int]]] = []

    for y in range(height):
        for x in range(width):
            if not mask[y, x] or visited[y, x]:
                continue
            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited[y, x] = True
            component: list[tuple[int, int]] = []
            while queue:
                cx, cy = queue.popleft()
                component.append((cx, cy))
                for nx, ny in (
                    (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1),
                    (cx - 1, cy - 1), (cx + 1, cy - 1), (cx - 1, cy + 1), (cx + 1, cy + 1),
                ):
                    if 0 <= nx < width and 0 <= ny < height and mask[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        queue.append((nx, ny))
            components.append(component)

    return components


def isolate_opaque_subject(image: Image.Image, palette: list[tuple[int, int, int]] | None = None) -> tuple[Image.Image, int]:
    rgba = image.convert("RGBA")
    arr = np.array(rgba)
    alpha = arr[:, :, 3]
    border_alpha = (
        np.count_nonzero(alpha[0, :])
        + np.count_nonzero(alpha[-1, :])
        + np.count_nonzero(alpha[:, 0])
        + np.count_nonzero(alpha[:, -1])
    )
    if border_alpha < max(8, (rgba.width + rgba.height) // 16):
        return rgba, 0

    rgb = arr[:, :, :3].astype(np.float32)
    if palette:
        palette_arr = np.array(palette, dtype=np.float32)
        deltas = rgb[:, :, None, :] - palette_arr[None, None, :, :]
        nearest = np.sqrt(np.sum(deltas * deltas, axis=3)).min(axis=2)
        sat = np.max(rgb, axis=2) - np.min(rgb, axis=2)
        subject_mask = (alpha > 0) & ((nearest >= 34) | (sat >= 18))
    else:
        luma = rgb[:, :, 0] * 0.299 + rgb[:, :, 1] * 0.587 + rgb[:, :, 2] * 0.114
        threshold = min(176, otsu_threshold(luma[alpha > 0]) + 10)
        subject_mask = (alpha > 0) & (luma <= threshold)
    subject_mask = dilate(erode(subject_mask, 1), 1)
    subject_mask = fill_holes(subject_mask)

    components = connected_components(subject_mask)
    if not components:
        return rgba, 0

    width = rgba.width
    height = rgba.height

    def score(component: list[tuple[int, int]]) -> tuple[float, int]:
        xs = [point[0] for point in component]
        ys = [point[1] for point in component]
        touches_border = any(x in (0, width - 1) or y in (0, height - 1) for x, y in component)
        bbox_area = (max(xs) - min(xs) + 1) * (max(ys) - min(ys) + 1)
        compactness = len(component) / max(1, bbox_area)
        border_penalty = 0.35 if touches_border else 1.0
        center_x = sum(xs) / len(xs)
        center_y = sum(ys) / len(ys)
        center_distance = abs(center_x - (width / 2)) + abs(center_y - (height / 2))
        center_bonus = 1.0 / (1.0 + (center_distance / max(width, height)))
        return (len(component) * compactness * border_penalty * center_bonus, len(component))

    best_component = max(components, key=score)
    if len(best_component) < max(64, (width * height) // 200):
        return rgba, 0

    final_mask = np.zeros((height, width), dtype=bool)
    for x, y in best_component:
        final_mask[y, x] = True
    final_mask = fill_holes(dilate(final_mask, 1))

    removed = int(np.count_nonzero((alpha > 0) & ~final_mask))
    if removed <= 0:
        return rgba, 0

    cleaned = arr.copy()
    cleaned[~final_mask, 3] = 0
    return Image.fromarray(cleaned, mode="RGBA"), removed


def trim_transparent_border(image: Image.Image, padding: int = 2) -> Image.Image:
    alpha = image.getchannel("A")
    width, height = image.size

    def find_bounds(threshold: int) -> tuple[int, int, int, int] | None:
        min_x = width
        min_y = height
        max_x = -1
        max_y = -1
        for y in range(height):
            for x in range(width):
                if alpha.getpixel((x, y)) <= threshold:
                    continue
                if x < min_x:
                    min_x = x
                if y < min_y:
                    min_y = y
                if x > max_x:
                    max_x = x
                if y > max_y:
                    max_y = y
        if max_x < 0 or max_y < 0:
            return None
        return min_x, min_y, max_x + 1, max_y + 1

    bounds = find_bounds(24) or find_bounds(4)
    if bounds is None:
        return image

    left, top, right, bottom = bounds
    left = max(0, left - padding)
    top = max(0, top - padding)
    right = min(width, right + padding)
    bottom = min(height, bottom + padding)

    if left == 0 and top == 0 and right == width and bottom == height:
        return image

    return image.crop((left, top, right, bottom))


def process_image(source_path: Path, output_path: Path) -> dict:
    image = Image.open(source_path).convert("RGBA")
    original_size = list(image.size)
    border_pixels = collect_border_pixels(image)
    palette = resolve_background_palette(border_pixels)
    normalized_path = source_path.as_posix().lower()
    opaque_subject_required = "/items/" in normalized_path or "/status/" in normalized_path

    if not palette:
        isolated_removed = 0
        if opaque_subject_required:
            image, isolated_removed = isolate_opaque_subject(image, palette)
            image = trim_transparent_border(image)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        image.save(output_path)
        return {
            "file": str(source_path),
            "cleaned": isolated_removed > 0,
            "removed_pixels": isolated_removed,
            "palette": [],
            "original_size": original_size,
            "output_size": list(image.size),
        }

    mask = build_background_mask(image, palette)
    cleaned, removed = apply_mask(image, mask, palette)
    cleaned, border_pruned = prune_border_connected_background(cleaned, palette)
    cleaned, matte_removed = scrub_checker_matte(cleaned, palette)
    cleaned, pruned = prune_edge_noise(cleaned)
    isolated_removed = 0
    if opaque_subject_required:
        cleaned, palette_removed = strip_palette_background(cleaned, palette)
        cleaned, isolated_removed = isolate_opaque_subject(cleaned, palette)
    else:
        palette_removed = 0
    trimmed = trim_transparent_border(cleaned)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    trimmed.save(output_path)
    return {
        "file": str(source_path),
        "cleaned": removed + border_pruned + matte_removed + pruned + palette_removed + isolated_removed > 0 or trimmed.size != image.size,
        "removed_pixels": removed + border_pruned + matte_removed + pruned + palette_removed + isolated_removed,
        "palette": palette,
        "original_size": original_size,
        "output_size": list(trimmed.size),
    }


def process_image_job(job: tuple[Path, Path]) -> dict:
    source_path, output_path = job
    return process_image(source_path, output_path)


def copy_tree(source_root: Path, output_root: Path, folder: str) -> list[dict]:
    source_folder = source_root / folder
    if not source_folder.exists():
        return []

    results: list[dict] = []
    for path in source_folder.rglob("*"):
        relative = path.relative_to(source_root)
        destination = output_root / relative
        if path.is_dir():
            destination.mkdir(parents=True, exist_ok=True)
            continue

        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, destination)
        if path.suffix.lower() == ".png":
            results.append({
                "file": str(path),
                "cleaned": False,
                "removed_pixels": 0,
                "palette": [],
                "mode": "copied",
            })
    return results


def main() -> int:
    args = parse_args()
    source_root = Path(args.source).resolve()
    output_root = Path(args.output).resolve()

    if output_root.exists():
        shutil.rmtree(output_root)
    output_root.mkdir(parents=True, exist_ok=True)

    results: list[dict] = []
    clean_folders = tuple(dict.fromkeys(args.clean_folders))
    copy_folders = tuple(folder for folder in dict.fromkeys(args.copy_folders) if folder not in clean_folders)

    clean_jobs: list[tuple[Path, Path]] = []
    for folder in clean_folders:
        source_folder = source_root / folder
        if not source_folder.exists():
            continue
        for path in source_folder.rglob("*"):
            relative = path.relative_to(source_root)
            destination = output_root / relative
            if path.is_dir():
                destination.mkdir(parents=True, exist_ok=True)
                continue
            if path.suffix.lower() != ".png":
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, destination)
                continue
            clean_jobs.append((path, destination))

    if clean_jobs:
        with ProcessPoolExecutor(max_workers=max(1, args.workers)) as executor:
            for result in executor.map(process_image_job, clean_jobs, chunksize=4):
                results.append(result)

    for folder in copy_folders:
        results.extend(copy_tree(source_root, output_root, folder))

    summary = {
        "source_root": str(source_root),
        "output_root": str(output_root),
        "clean_folders": clean_folders,
        "copy_folders": copy_folders,
        "files_processed": len(results),
        "files_cleaned": sum(1 for result in results if result["cleaned"]),
        "total_removed_pixels": sum(result["removed_pixels"] for result in results),
        "results": results,
    }
    summary_path = output_root / "cleanup-summary.json"
    summary_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"summary={summary_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
