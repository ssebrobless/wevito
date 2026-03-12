#!/usr/bin/env python3
"""
Generate the full runtime animal sprite tree from incoming pose boards.

The incoming animal PNGs are not animation sheets. Each file contains a pair of
high-resolution poses for a single species/age stage. This tool extracts those
poses, normalizes them to the runtime contract, and procedurally synthesizes a
complete transparent animation set for every supported color.
"""

from __future__ import annotations

import argparse
import json
import shutil
from collections import deque
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from PIL import Image, ImageEnhance


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "tools" / "incoming_animal_pose_manifest.json"
DEFAULT_SOURCE_ROOT = ROOT / "incoming_sprites"
DEFAULT_OUTPUT_ROOT = ROOT / "sprites_runtime"
CANVAS_SIZE = (28, 24)
SUMMARY_NAME = "generation-summary.json"

ANIMATION_RECIPES: dict[str, list[dict[str, float]]] = {
    "idle": [
        {"dx": 0, "dy": 0, "rotate": 0, "scale": 1.00, "brightness": 1.00},
        {"dx": 0, "dy": -1, "rotate": 0, "scale": 1.01, "brightness": 1.01},
        {"dx": 0, "dy": 0, "rotate": 0, "scale": 1.00, "brightness": 1.00},
        {"dx": 0, "dy": 1, "rotate": 0, "scale": 0.99, "brightness": 0.99},
    ],
    "walk": [
        {"dx": -1, "dy": 0, "rotate": -3, "scale": 1.00, "brightness": 1.00},
        {"dx": 0, "dy": -1, "rotate": 0, "scale": 1.01, "brightness": 1.00},
        {"dx": 1, "dy": 0, "rotate": 3, "scale": 1.00, "brightness": 1.00},
        {"dx": 0, "dy": 1, "rotate": 1, "scale": 0.99, "brightness": 0.99},
        {"dx": -1, "dy": 0, "rotate": -2, "scale": 1.00, "brightness": 1.00},
        {"dx": 0, "dy": -1, "rotate": 2, "scale": 1.01, "brightness": 1.01},
    ],
    "eat": [
        {"dx": 0, "dy": 1, "rotate": -4, "scale": 0.98, "brightness": 0.98},
        {"dx": 1, "dy": 2, "rotate": -7, "scale": 0.97, "brightness": 0.97},
        {"dx": 1, "dy": 2, "rotate": -5, "scale": 0.98, "brightness": 0.99},
        {"dx": 0, "dy": 1, "rotate": -2, "scale": 0.99, "brightness": 1.00},
    ],
    "happy": [
        {"dx": -1, "dy": -2, "rotate": -5, "scale": 1.03, "brightness": 1.05},
        {"dx": 1, "dy": -3, "rotate": 5, "scale": 1.04, "brightness": 1.06},
        {"dx": -1, "dy": -1, "rotate": -4, "scale": 1.02, "brightness": 1.04},
        {"dx": 1, "dy": 0, "rotate": 4, "scale": 1.01, "brightness": 1.03},
    ],
    "sad": [
        {"dx": 0, "dy": 2, "rotate": -5, "scale": 0.96, "brightness": 0.86},
        {"dx": 0, "dy": 3, "rotate": -7, "scale": 0.95, "brightness": 0.82},
    ],
    "sleep": [
        {"dx": 0, "dy": 4, "rotate": -9, "scale": 0.94, "brightness": 0.78},
        {"dx": 0, "dy": 4, "rotate": -10, "scale": 0.94, "brightness": 0.75},
    ],
    "sick": [
        {"dx": 0, "dy": 2, "rotate": -4, "scale": 0.96, "brightness": 0.82, "wash": 0.18},
        {"dx": 0, "dy": 3, "rotate": -2, "scale": 0.95, "brightness": 0.80, "wash": 0.22},
        {"dx": 1, "dy": 2, "rotate": 2, "scale": 0.96, "brightness": 0.83, "wash": 0.18},
        {"dx": 0, "dy": 3, "rotate": -3, "scale": 0.95, "brightness": 0.80, "wash": 0.24},
    ],
    "bathe": [
        {"dx": -1, "dy": 0, "rotate": -2, "scale": 1.01, "brightness": 1.08},
        {"dx": 1, "dy": 0, "rotate": 2, "scale": 1.02, "brightness": 1.10},
        {"dx": -1, "dy": -1, "rotate": -1, "scale": 1.01, "brightness": 1.09},
        {"dx": 1, "dy": 0, "rotate": 1, "scale": 1.02, "brightness": 1.10},
    ],
}

COLOR_VARIANTS: dict[str, tuple[int, int, int]] = {
    "red": (196, 112, 116),
    "orange": (207, 145, 92),
    "yellow": (211, 192, 104),
    "blue": (112, 153, 210),
    "indigo": (122, 116, 190),
    "violet": (178, 124, 193),
}

AGE_TARGETS: dict[str, tuple[int, int]] = {
    "baby": (18, 16),
    "teen": (21, 18),
    "adult": (24, 20),
}

SPECIES_PROFILES: dict[str, str] = {
    "rat": "rodent",
    "crow": "avian",
    "fox": "canid",
    "snake": "reptile",
    "deer": "ungulate",
    "frog": "amphibian",
    "pigeon": "avian",
    "raccoon": "forager",
    "squirrel": "rodent",
    "goose": "waterfowl",
}

PROFILE_SETTINGS: dict[str, dict[str, float]] = {
    "default": {
        "dx_scale": 1.0,
        "dy_scale": 1.0,
        "rotation_scale": 1.0,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 1.0,
        "height_scale": 1.0,
    },
    "rodent": {
        "dx_scale": 1.0,
        "dy_scale": 0.9,
        "rotation_scale": 0.9,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 1.02,
        "height_scale": 0.98,
    },
    "forager": {
        "dx_scale": 0.95,
        "dy_scale": 0.8,
        "rotation_scale": 0.75,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 1.0,
        "height_scale": 1.0,
    },
    "canid": {
        "dx_scale": 1.0,
        "dy_scale": 0.75,
        "rotation_scale": 0.7,
        "brightness_bias": 0.01,
        "baseline_dy": 0.0,
        "width_scale": 1.03,
        "height_scale": 1.0,
    },
    "avian": {
        "dx_scale": 0.92,
        "dy_scale": 0.62,
        "rotation_scale": 0.55,
        "brightness_bias": 0.02,
        "baseline_dy": 0.0,
        "width_scale": 0.96,
        "height_scale": 1.08,
    },
    "waterfowl": {
        "dx_scale": 0.88,
        "dy_scale": 0.55,
        "rotation_scale": 0.45,
        "brightness_bias": 0.02,
        "baseline_dy": 0.0,
        "width_scale": 0.98,
        "height_scale": 1.12,
    },
    "ungulate": {
        "dx_scale": 0.84,
        "dy_scale": 0.45,
        "rotation_scale": 0.35,
        "brightness_bias": 0.01,
        "baseline_dy": 0.0,
        "width_scale": 0.92,
        "height_scale": 1.12,
    },
    "amphibian": {
        "dx_scale": 0.82,
        "dy_scale": 1.05,
        "rotation_scale": 0.5,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 1.08,
        "height_scale": 0.95,
    },
    "reptile": {
        "dx_scale": 1.18,
        "dy_scale": 0.25,
        "rotation_scale": 0.25,
        "brightness_bias": -0.01,
        "baseline_dy": 1.0,
        "width_scale": 1.16,
        "height_scale": 0.82,
    },
}


@dataclass(frozen=True)
class CropOverride:
    pad_left: int = 8
    pad_top: int = 8
    pad_right: int = 8
    pad_bottom: int = 8


@dataclass(frozen=True)
class PlacementOverride:
    dx: int = 0
    dy: int = 0


@dataclass(frozen=True)
class ManifestEntry:
    source: str
    species: str
    age_stage: str
    component_order: tuple[str, str]
    crop_override: CropOverride
    placement_override: PlacementOverride


def resolve_profile_name(species: str) -> str:
    return SPECIES_PROFILES.get(species, "default")


def resolve_profile(species: str) -> dict[str, float]:
    return PROFILE_SETTINGS.get(resolve_profile_name(species), PROFILE_SETTINGS["default"])


def compose_recipe(recipe: dict[str, float], profile: dict[str, float], profile_name: str, animation_name: str) -> dict[str, float]:
    adjusted = dict(recipe)
    adjusted["dx"] = recipe.get("dx", 0.0) * profile["dx_scale"]
    adjusted["dy"] = recipe.get("dy", 0.0) * profile["dy_scale"] + profile["baseline_dy"]
    adjusted["rotate"] = recipe.get("rotate", 0.0) * profile["rotation_scale"]
    adjusted["brightness"] = max(0.55, recipe.get("brightness", 1.0) + profile["brightness_bias"])

    if animation_name == "sleep":
        adjusted["rotate"] *= 0.65
        adjusted["dy"] += 1
    elif animation_name == "walk" and profile_name == "amphibian":
        adjusted["dy"] += 1 if recipe.get("dy", 0.0) >= 0 else -1
    elif animation_name == "walk" and profile_name in {"avian", "waterfowl"}:
        adjusted["dx"] *= 1.05
    elif animation_name == "walk" and profile_name == "reptile":
        adjusted["scale"] = recipe.get("scale", 1.0) * 1.02

    return adjusted


def get_target_size(species: str, age_stage: str) -> tuple[int, int]:
    base_width, base_height = AGE_TARGETS[age_stage]
    profile = resolve_profile(species)
    width = max(12, min(CANVAS_SIZE[0], round(base_width * profile["width_scale"])))
    height = max(10, min(CANVAS_SIZE[1], round(base_height * profile["height_scale"])))
    return (width, height)


def load_manifest(path: Path) -> list[ManifestEntry]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    entries: list[ManifestEntry] = []
    for item in payload:
        crop_payload = item.get("crop_override", {})
        placement_payload = item.get("placement_override", {})
        entries.append(
            ManifestEntry(
                source=item["source"],
                species=item["species"],
                age_stage=item["age_stage"],
                component_order=tuple(item.get("component_order", ["male", "female"])),
                crop_override=CropOverride(
                    pad_left=int(crop_payload.get("pad_left", 8)),
                    pad_top=int(crop_payload.get("pad_top", 8)),
                    pad_right=int(crop_payload.get("pad_right", 8)),
                    pad_bottom=int(crop_payload.get("pad_bottom", 8)),
                ),
                placement_override=PlacementOverride(
                    dx=int(placement_payload.get("dx", 0)),
                    dy=int(placement_payload.get("dy", 0)),
                ),
            )
        )
    return entries


def edge_alpha_is_present(image: Image.Image) -> bool:
    alpha = image.getchannel("A")
    width, height = image.size
    transparent_edge_pixels = 0
    total_edge_pixels = (width * 2) + (height * 2) - 4

    for x in range(width):
        transparent_edge_pixels += 1 if alpha.getpixel((x, 0)) == 0 else 0
        transparent_edge_pixels += 1 if alpha.getpixel((x, height - 1)) == 0 else 0
    for y in range(1, height - 1):
        transparent_edge_pixels += 1 if alpha.getpixel((0, y)) == 0 else 0
        transparent_edge_pixels += 1 if alpha.getpixel((width - 1, y)) == 0 else 0

    return transparent_edge_pixels / max(1, total_edge_pixels) > 0.65


def is_background(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return True

    brightness = (r + g + b) / 3
    spread = max(r, g, b) - min(r, g, b)
    return brightness >= 168 and spread <= 20


def remove_checkerboard_background(image: Image.Image) -> Image.Image:
    if edge_alpha_is_present(image):
        return image.copy()

    work = image.copy()
    pixels = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    edge_points: list[tuple[int, int]] = []
    for x in range(width):
        edge_points.append((x, 0))
        edge_points.append((x, height - 1))
    for y in range(height):
        edge_points.append((0, y))
        edge_points.append((width - 1, y))

    for start_x, start_y in edge_points:
        if (start_x, start_y) in visited or not is_background(pixels[start_x, start_y]):
            continue

        queue.append((start_x, start_y))
        visited.add((start_x, start_y))

        while queue:
            x, y = queue.popleft()
            pixels[x, y] = (0, 0, 0, 0)
            for next_x, next_y in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if not (0 <= next_x < width and 0 <= next_y < height):
                    continue
                if (next_x, next_y) in visited or not is_background(pixels[next_x, next_y]):
                    continue
                visited.add((next_x, next_y))
                queue.append((next_x, next_y))

    return work


def is_subject_pixel(pixel: tuple[int, int, int, int], prefer_alpha_only: bool) -> bool:
    if prefer_alpha_only:
        return pixel[3] > 0
    return not is_background(pixel)


def find_largest_component_in_bounds(
    image: Image.Image,
    left: int,
    top: int,
    right: int,
    bottom: int,
    prefer_alpha_only: bool,
) -> tuple[int, int, int, int]:
    pixels = image.load()
    visited: set[tuple[int, int]] = set()
    best_component: tuple[int, int, int, int, int] | None = None

    for y in range(top, bottom):
        for x in range(left, right):
            if (x, y) in visited or not is_subject_pixel(pixels[x, y], prefer_alpha_only):
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited.add((x, y))
            min_x = max_x = x
            min_y = max_y = y
            area = 0

            while queue:
                current_x, current_y = queue.popleft()
                area += 1
                min_x = min(min_x, current_x)
                max_x = max(max_x, current_x)
                min_y = min(min_y, current_y)
                max_y = max(max_y, current_y)

                for next_x, next_y in (
                    (current_x + 1, current_y),
                    (current_x - 1, current_y),
                    (current_x, current_y + 1),
                    (current_x, current_y - 1),
                ):
                    if not (left <= next_x < right and top <= next_y < bottom):
                        continue
                    if (next_x, next_y) in visited or not is_subject_pixel(pixels[next_x, next_y], prefer_alpha_only):
                        continue
                    visited.add((next_x, next_y))
                    queue.append((next_x, next_y))

            candidate = (area, min_x, min_y, max_x + 1, max_y + 1)
            if best_component is None or candidate[0] > best_component[0]:
                best_component = candidate

    if best_component is None:
        raise ValueError(f"No subject component found in bounds {left},{top},{right},{bottom}.")

    _, min_x, min_y, max_x, max_y = best_component
    return (min_x, min_y, max_x, max_y)


def find_pose_components(image: Image.Image) -> list[tuple[int, int, int, int]]:
    mid_x = image.width // 2
    prefer_alpha_only = edge_alpha_is_present(image)
    return [
        find_largest_component_in_bounds(image, 0, 0, mid_x, image.height, prefer_alpha_only),
        find_largest_component_in_bounds(image, mid_x, 0, image.width, image.height, prefer_alpha_only),
    ]


def trim_to_alpha(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("Expected non-empty sprite after alpha trim.")
    return image.crop(bbox)


def colorize(sprite: Image.Image, variant_name: str) -> Image.Image:
    tint = COLOR_VARIANTS[variant_name]
    tinted = Image.new("RGBA", sprite.size, (0, 0, 0, 0))
    source = sprite.load()
    target = tinted.load()

    for y in range(sprite.height):
        for x in range(sprite.width):
            r, g, b, a = source[x, y]
            if a == 0:
                target[x, y] = (0, 0, 0, 0)
                continue

            luma = round(r * 0.299 + g * 0.587 + b * 0.114)
            tinted_rgb = tuple((luma * channel) // 255 for channel in tint)
            blend_strength = 0.55
            out_r = round(r * (1 - blend_strength) + tinted_rgb[0] * blend_strength)
            out_g = round(g * (1 - blend_strength) + tinted_rgb[1] * blend_strength)
            out_b = round(b * (1 - blend_strength) + tinted_rgb[2] * blend_strength)
            target[x, y] = (out_r, out_g, out_b, a)

    return tinted


def apply_recipe(sprite: Image.Image, recipe: dict[str, float]) -> Image.Image:
    work = sprite.copy()

    if recipe.get("brightness", 1.0) != 1.0:
        work = ImageEnhance.Brightness(work).enhance(recipe["brightness"])

    if recipe.get("wash", 0.0) > 0:
        wash = Image.new("RGBA", work.size, (185, 225, 205, round(255 * recipe["wash"])))
        work = Image.alpha_composite(work, wash)

    scale = recipe.get("scale", 1.0)
    if scale != 1.0:
        scaled_width = max(1, round(work.width * scale))
        scaled_height = max(1, round(work.height * scale))
        work = work.resize((scaled_width, scaled_height), Image.Resampling.NEAREST)

    rotation = recipe.get("rotate", 0.0)
    if rotation:
        work = work.rotate(rotation, resample=Image.Resampling.NEAREST, expand=True)

    return trim_to_alpha(work)


def fit_to_canvas(
    sprite: Image.Image,
    species: str,
    age_stage: str,
    recipe: dict[str, float],
    placement_override: PlacementOverride,
) -> Image.Image:
    target_width, target_height = get_target_size(species, age_stage)
    scale = min(target_width / sprite.width, target_height / sprite.height)
    scaled_width = max(1, round(sprite.width * scale))
    scaled_height = max(1, round(sprite.height * scale))
    resized = sprite.resize((scaled_width, scaled_height), Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    offset_x = (CANVAS_SIZE[0] - scaled_width) // 2 + int(recipe.get("dx", 0)) + placement_override.dx
    offset_y = CANVAS_SIZE[1] - scaled_height + int(recipe.get("dy", 0)) + placement_override.dy

    offset_x = max(min(offset_x, CANVAS_SIZE[0] - scaled_width), 0)
    offset_y = max(min(offset_y, CANVAS_SIZE[1] - scaled_height), 0)
    canvas.alpha_composite(resized, (offset_x, offset_y))
    return canvas


def extract_gender_pose(image: Image.Image, bbox: tuple[int, int, int, int], crop_override: CropOverride) -> Image.Image:
    left, top, right, bottom = bbox
    left = max(0, left - crop_override.pad_left)
    top = max(0, top - crop_override.pad_top)
    right = min(image.width, right + crop_override.pad_right)
    bottom = min(image.height, bottom + crop_override.pad_bottom)
    return trim_to_alpha(image.crop((left, top, right, bottom)))


def ensure_transparent_frame(image_path: Path) -> None:
    with image_path.open("rb") as stream:
        header = stream.read(33)
    if len(header) < 33 or header[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{image_path} is not a valid PNG.")
    width = int.from_bytes(header[16:20], "big")
    height = int.from_bytes(header[20:24], "big")
    color_type = header[25]
    if width != CANVAS_SIZE[0] or height != CANVAS_SIZE[1]:
        raise ValueError(f"{image_path} expected {CANVAS_SIZE[0]}x{CANVAS_SIZE[1]}, found {width}x{height}.")
    if color_type not in {4, 6}:
        raise ValueError(f"{image_path} must preserve alpha, color type was {color_type}.")


def export_animation_set(
    base_sprite: Image.Image,
    species: str,
    age_stage: str,
    gender: str,
    placement_override: PlacementOverride,
    output_root: Path,
) -> int:
    exported = 0
    profile_name = resolve_profile_name(species)
    profile = resolve_profile(species)
    for color_name in COLOR_VARIANTS:
        tinted = colorize(base_sprite, color_name)
        out_dir = output_root / species / age_stage / gender / color_name
        out_dir.mkdir(parents=True, exist_ok=True)

        for animation_name, frames in ANIMATION_RECIPES.items():
            for frame_index, recipe in enumerate(frames):
                adjusted_recipe = compose_recipe(recipe, profile, profile_name, animation_name)
                transformed = apply_recipe(tinted, adjusted_recipe)
                frame = fit_to_canvas(transformed, species, age_stage, adjusted_recipe, placement_override)
                out_path = out_dir / f"{animation_name}_{frame_index:02d}.png"
                frame.save(out_path)
                exported += 1

    return exported


def export_entry(entry: ManifestEntry, source_root: Path, output_root: Path) -> dict[str, Any]:
    source_path = source_root / entry.source
    image = Image.open(source_path).convert("RGBA")
    isolated = remove_checkerboard_background(image)
    components = find_pose_components(image)

    if len(components) != 2:
        raise ValueError(f"Expected 2 pose components in {source_path}, found {len(components)}")

    exported = 0
    pose_summaries: list[dict[str, Any]] = []
    for gender, bbox in zip(entry.component_order, components, strict=True):
        sprite = extract_gender_pose(isolated, bbox, entry.crop_override)
        exported += export_animation_set(sprite, entry.species, entry.age_stage, gender, entry.placement_override, output_root)
        pose_summaries.append(
            {
                "gender": gender,
                "bbox": {"left": bbox[0], "top": bbox[1], "right": bbox[2], "bottom": bbox[3]},
                "source_size": {"width": sprite.width, "height": sprite.height},
            }
        )

    return {
        "source": entry.source,
        "species": entry.species,
        "age_stage": entry.age_stage,
        "exported_frames": exported,
        "poses": pose_summaries,
    }


def clean_output(path: Path) -> None:
    if path.exists():
        shutil.rmtree(path)


def validate_output(output_root: Path) -> dict[str, int]:
    expected_frame_counts = {name: len(frames) for name, frames in ANIMATION_RECIPES.items()}
    counts_by_species: dict[str, int] = {}

    for species_dir in sorted(output_root.iterdir()):
        if not species_dir.is_dir():
            continue
        species_total = 0
        for age_dir in sorted(species_dir.iterdir()):
            for gender_dir in sorted(age_dir.iterdir()):
                for color_dir in sorted(gender_dir.iterdir()):
                    for animation_name, expected_count in expected_frame_counts.items():
                        frames = sorted(color_dir.glob(f"{animation_name}_*.png"))
                        if len(frames) != expected_count:
                            raise ValueError(
                                f"{color_dir} expected {expected_count} frame(s) for {animation_name}, found {len(frames)}."
                            )
                        for frame_path in frames:
                            ensure_transparent_frame(frame_path)
                        species_total += len(frames)
        counts_by_species[species_dir.name] = species_total

    return counts_by_species


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--clean", action="store_true")
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    if args.clean:
        clean_output(args.output_root)

    args.output_root.mkdir(parents=True, exist_ok=True)

    exported = 0
    entry_summaries: list[dict[str, Any]] = []
    for entry in manifest:
        entry_summary = export_entry(entry, args.source_root, args.output_root)
        entry_summaries.append(entry_summary)
        exported += int(entry_summary["exported_frames"])

    counts_by_species = validate_output(args.output_root)
    summary = {
        "canvas_size": {"width": CANVAS_SIZE[0], "height": CANVAS_SIZE[1]},
        "exported_frames": exported,
        "species_counts": counts_by_species,
        "entries": entry_summaries,
    }
    (args.output_root / SUMMARY_NAME).write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"exported {exported} runtime frame(s) to {args.output_root}")


if __name__ == "__main__":
    main()
