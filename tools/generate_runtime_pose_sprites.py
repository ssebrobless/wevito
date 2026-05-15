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
import math
import shutil
import time
from collections import deque
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image, ImageEnhance
from scipy import ndimage


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "tools" / "incoming_animal_pose_manifest.json"
DEFAULT_SOURCE_ROOT = ROOT / "incoming_sprites"
DEFAULT_OUTPUT_ROOT = ROOT / "sprites_runtime"
CANVAS_SIZE = (72, 64)
SUMMARY_NAME = "generation-summary.json"

FRAME_LAYOUT_OVERRIDES: dict[str, dict[str, dict[str, dict[str, tuple[int, int]]]]] = {
    "snake": {
        "walk": {
            "baby": {
                "canvas_size": (104, 64),
                "target_size": (92, 34),
            },
            "teen": {
                "canvas_size": (112, 64),
                "target_size": (102, 40),
            },
            "adult": {
                "canvas_size": (120, 64),
                "target_size": (112, 48),
            },
        },
    },
}

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
    "baby": (48, 42),
    "teen": (58, 50),
    "adult": (68, 58),
}

TARGET_SIZE_OVERRIDES: dict[str, dict[str, tuple[int, int]]] = {
    "deer": {
        "adult": (48, 52),
        "teen": (44, 48),
    },
    "goose": {
        "adult": (62, 56),
    },
    "raccoon": {
        "adult": (58, 48),
    },
    "squirrel": {
        "adult": (58, 52),
    },
}

GENDER_SCALE_ADJUSTMENTS: dict[tuple[str, str, str], float] = {
    ("deer", "adult", "female"): 0.86,
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
        "max_upscale": 1.22,
    },
    "rodent": {
        "dx_scale": 1.0,
        "dy_scale": 0.9,
        "rotation_scale": 0.9,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 1.02,
        "height_scale": 0.98,
        "max_upscale": 1.18,
    },
    "forager": {
        "dx_scale": 0.95,
        "dy_scale": 0.8,
        "rotation_scale": 0.75,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 0.96,
        "height_scale": 0.98,
        "max_upscale": 1.12,
    },
    "canid": {
        "dx_scale": 1.0,
        "dy_scale": 0.75,
        "rotation_scale": 0.7,
        "brightness_bias": 0.01,
        "baseline_dy": 0.0,
        "width_scale": 1.03,
        "height_scale": 1.0,
        "max_upscale": 1.16,
    },
    "avian": {
        "dx_scale": 0.92,
        "dy_scale": 0.62,
        "rotation_scale": 0.55,
        "brightness_bias": 0.02,
        "baseline_dy": 0.0,
        "width_scale": 0.96,
        "height_scale": 1.08,
        "max_upscale": 1.12,
    },
    "waterfowl": {
        "dx_scale": 0.88,
        "dy_scale": 0.55,
        "rotation_scale": 0.45,
        "brightness_bias": 0.02,
        "baseline_dy": 0.0,
        "width_scale": 0.98,
        "height_scale": 1.12,
        "max_upscale": 1.08,
    },
    "ungulate": {
        "dx_scale": 0.84,
        "dy_scale": 0.45,
        "rotation_scale": 0.35,
        "brightness_bias": 0.01,
        "baseline_dy": 0.0,
        "width_scale": 0.88,
        "height_scale": 1.04,
        "max_upscale": 1.0,
    },
    "amphibian": {
        "dx_scale": 0.82,
        "dy_scale": 1.05,
        "rotation_scale": 0.5,
        "brightness_bias": 0.0,
        "baseline_dy": 0.0,
        "width_scale": 1.08,
        "height_scale": 0.95,
        "max_upscale": 1.14,
    },
    "reptile": {
        "dx_scale": 1.18,
        "dy_scale": 0.25,
        "rotation_scale": 0.25,
        "brightness_bias": -0.01,
        "baseline_dy": 1.0,
        "width_scale": 1.16,
        "height_scale": 0.82,
        "max_upscale": 1.0,
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
    component_crop_overrides: dict[str, CropOverride]
    placement_override: PlacementOverride


def resolve_profile_name(species: str) -> str:
    return SPECIES_PROFILES.get(species, "default")


def resolve_profile(species: str) -> dict[str, float]:
    return PROFILE_SETTINGS.get(resolve_profile_name(species), PROFILE_SETTINGS["default"])


def compose_recipe(
    recipe: dict[str, float],
    profile: dict[str, float],
    profile_name: str,
    animation_name: str,
    frame_index: int,
    frame_count: int,
) -> dict[str, float]:
    adjusted = dict(recipe)
    adjusted["dx"] = recipe.get("dx", 0.0) * profile["dx_scale"]
    adjusted["dy"] = recipe.get("dy", 0.0) * profile["dy_scale"] + profile["baseline_dy"]
    adjusted["rotate"] = recipe.get("rotate", 0.0) * profile["rotation_scale"]
    adjusted["brightness"] = max(0.55, recipe.get("brightness", 1.0) + profile["brightness_bias"])
    adjusted["scale_x"] = recipe.get("scale_x", recipe.get("scale", 1.0))
    adjusted["scale_y"] = recipe.get("scale_y", recipe.get("scale", 1.0))
    phase = (frame_index / max(1, frame_count)) * math.tau
    stride = math.sin(phase)
    lift = math.cos(phase)

    if animation_name == "sleep":
        adjusted["rotate"] *= 0.65
        adjusted["dy"] += 1
    elif animation_name == "walk" and profile_name == "amphibian":
        adjusted["dy"] += 1 if recipe.get("dy", 0.0) >= 0 else -1
    elif animation_name == "walk" and profile_name in {"avian", "waterfowl"}:
        adjusted["dx"] *= 1.05
    elif animation_name == "walk" and profile_name == "reptile":
        adjusted["scale_x"] *= 1.03
        adjusted["scale_y"] *= 0.96

    if profile_name in {"rodent", "forager", "canid"} and animation_name == "walk":
        adjusted["scale_x"] *= 1.045 if stride >= 0 else 0.98
        adjusted["scale_y"] *= 0.965 if stride >= 0 else 1.02
        adjusted["rotate"] += stride * 1.2
        adjusted["tail_shift_x"] = -1.45 if stride >= 0 else 1.45
        adjusted["tail_shift_y"] = -0.85 if lift > 0 else 0.5
        if profile_name == "canid":
            adjusted["front_leg_dx"] = 2.35 if stride >= 0 else -2.35
            adjusted["front_leg_dy"] = -2.75 if stride >= 0 else 1.65
            adjusted["rear_leg_dx"] = -1.95 if stride >= 0 else 1.95
            adjusted["rear_leg_dy"] = 1.55 if stride >= 0 else -2.25
            adjusted["head_shift_x"] = 0.45 if stride >= 0 else -0.25
            adjusted["head_shift_y"] = -1.2 if lift > 0 else 0.65
            adjusted["tail_shift_x"] = -2.6 if stride >= 0 else 2.6
            adjusted["tail_shift_y"] = -0.75 if lift > 0 else 0.95
        elif profile_name == "rodent":
            adjusted["front_leg_dx"] = 1.55 if stride >= 0 else -1.55
            adjusted["front_leg_dy"] = -1.8 if stride >= 0 else 1.15
            adjusted["rear_leg_dx"] = -1.25 if stride >= 0 else 1.25
            adjusted["rear_leg_dy"] = 1.15 if stride >= 0 else -1.45
            adjusted["head_shift_x"] = 0.35 if stride >= 0 else -0.2
            adjusted["head_shift_y"] = -0.8 if lift > 0 else 0.4
            adjusted["tail_shift_x"] = -2.45 if stride >= 0 else 2.45
            adjusted["tail_shift_y"] = -0.55 if lift > 0 else 0.85
        elif profile_name == "forager":
            adjusted["front_leg_dx"] = 1.35 if stride >= 0 else -1.35
            adjusted["front_leg_dy"] = -1.5 if stride >= 0 else 1.05
            adjusted["rear_leg_dx"] = -1.2 if stride >= 0 else 1.2
            adjusted["rear_leg_dy"] = 1.0 if stride >= 0 else -1.3
            adjusted["head_shift_y"] = -0.7 if lift > 0 else 0.35
            adjusted["tail_shift_x"] = -1.8 if stride >= 0 else 1.8
            adjusted["tail_shift_y"] = -0.3 if lift > 0 else 0.55
    elif profile_name == "avian" and animation_name in {"idle", "walk"}:
        adjusted["dy"] += (-0.85 if lift > 0 else 0.55) if animation_name == "walk" else (-0.45 if lift > 0 else 0.22)
        adjusted["rotate"] += stride * 1.8
        adjusted["scale_x"] *= 0.985 if stride >= 0 else 1.015
        adjusted["scale_y"] *= 1.05 if lift > 0 else 0.975
        adjusted["wing_shift_y"] = (-3.4 if lift > 0 else 1.8) if animation_name == "walk" else (-1.55 if lift > 0 else 0.65)
        adjusted["wing_shift_x"] = stride * 1.15
        adjusted["head_shift_y"] = -0.45 if lift > 0 else 0.25
    elif profile_name == "waterfowl" and animation_name in {"idle", "walk"}:
        adjusted["dy"] += (-0.95 if lift > 0 else 0.7) if animation_name == "walk" else (-0.45 if lift > 0 else 0.22)
        adjusted["rotate"] += stride * 1.3
        adjusted["scale_x"] *= 0.975 if stride >= 0 else 1.025
        adjusted["scale_y"] *= 1.06 if lift > 0 else 0.965
        adjusted["wing_shift_y"] = (-1.7 if lift > 0 else 1.0) if animation_name == "walk" else (-0.9 if lift > 0 else 0.35)
        adjusted["wing_shift_x"] = stride * 0.55
        adjusted["head_shift_y"] = -0.35 if lift > 0 else 0.2
    elif profile_name == "ungulate" and animation_name in {"idle", "walk"}:
        adjusted["dy"] += 0.45 if abs(stride) > 0.6 else -0.25
        adjusted["rotate"] += stride * 0.85
        adjusted["scale_x"] *= 1.01 if stride >= 0 else 0.99
        adjusted["scale_y"] *= 0.97 if abs(stride) > 0.6 else 1.01
        if animation_name == "walk":
            adjusted["front_leg_dx"] = 1.3 if stride >= 0 else -1.3
            adjusted["front_leg_dy"] = -1.9 if stride >= 0 else 1.15
            adjusted["rear_leg_dx"] = -1.2 if stride >= 0 else 1.2
            adjusted["rear_leg_dy"] = 1.15 if stride >= 0 else -1.9
            adjusted["head_shift_y"] = -0.65 if lift > 0 else 0.5
    elif profile_name == "amphibian" and animation_name in {"idle", "walk", "happy"}:
        crouch = 1 if abs(stride) > 0.6 else 0
        adjusted["dy"] += 1 if crouch else -0.5
        adjusted["rotate"] += stride * 0.8
        adjusted["scale_x"] *= 1.08 if crouch else 0.96
        adjusted["scale_y"] *= 0.88 if crouch else 1.08
        if animation_name == "walk":
            adjusted["front_leg_dx"] = 1.0 if stride >= 0 else -1.0
            adjusted["front_leg_dy"] = -0.9 if stride >= 0 else 0.7
            adjusted["rear_leg_dx"] = -0.9 if stride >= 0 else 0.9
            adjusted["rear_leg_dy"] = 0.7 if stride >= 0 else -0.9
    elif profile_name == "reptile" and animation_name in {"idle", "walk", "happy", "sad", "sick"}:
        slither_phase = phase if animation_name == "idle" else phase + (math.pi / 2.0)
        slither_stride = math.sin(slither_phase)
        slither_lift = math.cos(slither_phase)
        adjusted["wave_y_amplitude"] = 1.7 if animation_name == "idle" else 3.85
        adjusted["wave_y_phase"] = slither_phase * (0.95 if animation_name == "idle" else 2.15)
        adjusted["wave_y_cycles"] = 1.25 if animation_name == "idle" else 1.95
        adjusted["wave_x_amplitude"] = 1.1 if animation_name == "idle" else 2.45
        adjusted["wave_x_phase"] = slither_phase + (math.pi / 2.0)
        adjusted["wave_x_cycles"] = 1.08 if animation_name == "idle" else 1.75
        adjusted["dx"] += slither_stride * 2.9 if animation_name == "walk" else 0.0
        adjusted["scale_x"] *= 0.94 if animation_name == "idle" else 1.62
        adjusted["scale_y"] *= 1.03 if animation_name == "idle" else 0.58
        adjusted["rotate"] += slither_stride * 0.35
        adjusted["coil_strength"] = 3.9 if animation_name == "idle" else 0.0
        if animation_name == "walk":
            adjusted["head_shift_x"] = 2.6 if slither_stride >= 0 else 2.1
            adjusted["head_shift_y"] = -0.55 if slither_lift > 0 else 0.3
            adjusted["tail_shift_x"] = -3.75 if slither_stride >= 0 else -2.55
            adjusted["tail_shift_y"] = 0.65 if slither_lift > 0 else -0.25

    return adjusted


def get_target_size(species: str, age_stage: str) -> tuple[int, int]:
    species_override = TARGET_SIZE_OVERRIDES.get(species, {}).get(age_stage)
    if species_override is not None:
        return species_override

    base_width, base_height = AGE_TARGETS[age_stage]
    profile = resolve_profile(species)
    width = max(12, min(CANVAS_SIZE[0], round(base_width * profile["width_scale"])))
    height = max(10, min(CANVAS_SIZE[1], round(base_height * profile["height_scale"])))
    return (width, height)


def resolve_frame_layout(species: str, age_stage: str, animation_name: str | None = None) -> tuple[tuple[int, int], tuple[int, int]]:
    canvas_size = CANVAS_SIZE
    target_size = get_target_size(species, age_stage)
    if not animation_name:
        return canvas_size, target_size

    animation_overrides = FRAME_LAYOUT_OVERRIDES.get(species, {}).get(animation_name, {})
    layout = animation_overrides.get(age_stage)
    if layout is None:
        return canvas_size, target_size

    return layout["canvas_size"], layout["target_size"]


def load_manifest(path: Path) -> list[ManifestEntry]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    entries: list[ManifestEntry] = []

    def parse_crop_override(raw: dict[str, Any]) -> CropOverride:
        return CropOverride(
            pad_left=int(raw.get("pad_left", 8)),
            pad_top=int(raw.get("pad_top", 8)),
            pad_right=int(raw.get("pad_right", 8)),
            pad_bottom=int(raw.get("pad_bottom", 8)),
        )

    for item in payload:
        crop_payload = item.get("crop_override", {})
        component_crop_overrides = {
            key: parse_crop_override(value)
            for key, value in item.get("component_crop_overrides", {}).items()
        }
        placement_payload = item.get("placement_override", {})
        entries.append(
            ManifestEntry(
                source=item["source"],
                species=item["species"],
                age_stage=item["age_stage"],
                component_order=tuple(item.get("component_order", ["male", "female"])),
                crop_override=parse_crop_override(crop_payload),
                component_crop_overrides=component_crop_overrides,
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


def is_border_palette_candidate(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return False

    brightness = (r + g + b) / 3
    spread = max(r, g, b) - min(r, g, b)
    return brightness >= 96 and spread <= 42


def quantize_rgb(rgb: tuple[int, int, int], step: int = 8) -> tuple[int, int, int]:
    return tuple(int(round(channel / step) * step) for channel in rgb)


def color_distance(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))


def collect_border_palette(image: Image.Image) -> list[tuple[int, int, int]]:
    pixels = image.load()
    width, height = image.size
    counts: dict[tuple[int, int, int], int] = {}

    def add(px: int, py: int) -> None:
        rgba = pixels[px, py]
        if not is_border_palette_candidate(rgba):
            return
        rgb = quantize_rgb(rgba[:3])
        counts[rgb] = counts.get(rgb, 0) + 1

    corner_span_x = max(6, min(width // 6, 32))
    corner_span_y = max(6, min(height // 6, 32))

    for x in range(corner_span_x):
        add(x, 0)
        add(x, height - 1)
        add(width - 1 - x, 0)
        add(width - 1 - x, height - 1)
    for y in range(corner_span_y):
        add(0, y)
        add(width - 1, y)
        add(0, height - 1 - y)
        add(width - 1, height - 1 - y)

    palette = [color for color, count in sorted(counts.items(), key=lambda item: item[1], reverse=True) if count >= 6]
    return palette[:6]


def collect_neutral_edge_palette(image: Image.Image) -> list[tuple[int, int, int]]:
    pixels = image.load()
    width, height = image.size
    counts: dict[tuple[int, int, int], int] = {}

    def add(px: int, py: int) -> None:
        r, g, b, a = pixels[px, py]
        if a == 0:
            return
        brightness = (r + g + b) / 3
        spread = max(r, g, b) - min(r, g, b)
        if spread > 36 or brightness < 28 or brightness > 232:
            return
        rgb = quantize_rgb((r, g, b))
        counts[rgb] = counts.get(rgb, 0) + 1

    corner_span_x = max(6, min(width // 6, 32))
    corner_span_y = max(6, min(height // 6, 32))
    for x in range(corner_span_x):
        add(x, 0)
        add(x, height - 1)
        add(width - 1 - x, 0)
        add(width - 1 - x, height - 1)
    for y in range(corner_span_y):
        add(0, y)
        add(width - 1, y)
        add(0, height - 1 - y)
        add(width - 1, height - 1 - y)

    palette = [color for color, count in sorted(counts.items(), key=lambda item: item[1], reverse=True) if count >= 6]
    return palette[:8]


def scrub_border_palette_matte(image: Image.Image) -> Image.Image:
    palette = collect_border_palette(image)
    if not palette:
        return image

    work = image.copy()
    source = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def is_background_like(px: int, py: int) -> bool:
        rgba = source[px, py]
        if rgba[3] == 0:
            return True
        rgb = rgba[:3]
        spread = max(rgb) - min(rgb)
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        return nearest <= 56 and spread <= 48

    for x in range(width):
        for y in (0, height - 1):
            if (x, y) in visited or not is_background_like(x, y):
                continue
            visited.add((x, y))
            queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if (x, y) in visited or not is_background_like(x, y):
                continue
            visited.add((x, y))
            queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        source[x, y] = (0, 0, 0, 0)
        for nx, ny in (
            (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
            (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
        ):
            if not (0 <= nx < width and 0 <= ny < height):
                continue
            if (nx, ny) in visited or not is_background_like(nx, ny):
                continue
            visited.add((nx, ny))
            queue.append((nx, ny))

    return work


def strip_palette_like_noise(image: Image.Image) -> Image.Image:
    palette = collect_border_palette(image)
    if not palette:
        return image

    work = image.copy()
    pixels = work.load()
    width, height = work.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            rgb = (r, g, b)
            spread = max(rgb) - min(rgb)
            nearest = min(color_distance(rgb, candidate) for candidate in palette)
            if nearest <= 30 and spread <= 32:
                pixels[x, y] = (0, 0, 0, 0)

    return work


def clear_bright_edge_matte(image: Image.Image) -> Image.Image:
    palette = collect_border_palette(image)
    if not palette:
        return image

    work = image.copy()
    pixels = work.load()
    width, height = work.size
    to_clear: list[tuple[int, int]] = []
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue

            has_transparent_neighbor = False
            for nx, ny in (
                (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
                (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
            ):
                if not (0 <= nx < width and 0 <= ny < height) or pixels[nx, ny][3] == 0:
                    has_transparent_neighbor = True
                    break

            if not has_transparent_neighbor:
                continue

            rgb = (r, g, b)
            bright = (r + g + b) / 3
            spread = max(rgb) - min(rgb)
            nearest = min(color_distance(rgb, candidate) for candidate in palette)
            if bright >= 164 and spread <= 56 and nearest <= 110:
                to_clear.append((x, y))

    for x, y in to_clear:
        pixels[x, y] = (0, 0, 0, 0)

    return work


def scrub_neutral_edge_matte(image: Image.Image) -> Image.Image:
    palette = collect_neutral_edge_palette(image)
    if not palette:
        return image

    work = image.copy()
    source = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def is_background_like(px: int, py: int) -> bool:
        r, g, b, a = source[px, py]
        if a == 0:
            return True
        brightness = (r + g + b) / 3
        spread = max(r, g, b) - min(r, g, b)
        if spread > 42 or brightness < 24 or brightness > 236:
            return False
        nearest = min(color_distance((r, g, b), candidate) for candidate in palette)
        return nearest <= 44

    for x in range(width):
        for y in (0, height - 1):
            if (x, y) in visited or not is_background_like(x, y):
                continue
            visited.add((x, y))
            queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if (x, y) in visited or not is_background_like(x, y):
                continue
            visited.add((x, y))
            queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        source[x, y] = (0, 0, 0, 0)
        for nx, ny in (
            (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
            (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
        ):
            if not (0 <= nx < width and 0 <= ny < height):
                continue
            if (nx, ny) in visited or not is_background_like(nx, ny):
                continue
            visited.add((nx, ny))
            queue.append((nx, ny))

    return work


def clear_transparency_connected_matte(image: Image.Image) -> Image.Image:
    palette = collect_border_palette(image)
    if not palette:
        return image

    work = image.copy()
    pixels = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def is_background_like(px: int, py: int) -> bool:
        r, g, b, a = pixels[px, py]
        if a == 0:
            return True
        rgb = (r, g, b)
        spread = max(rgb) - min(rgb)
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        bright = (r + g + b) / 3
        return nearest <= 74 and spread <= 34 and bright >= 144

    for y in range(height):
        for x in range(width):
            if pixels[x, y][3] == 0:
                visited.add((x, y))
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        for nx, ny in (
            (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
            (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
        ):
            if not (0 <= nx < width and 0 <= ny < height):
                continue
            if (nx, ny) in visited or not is_background_like(nx, ny):
                continue
            visited.add((nx, ny))
            pixels[nx, ny] = (0, 0, 0, 0)
            queue.append((nx, ny))

    return work


def clear_small_background_like_islands(image: Image.Image) -> Image.Image:
    palette = collect_border_palette(image)
    if not palette:
        return image

    work = image.copy()
    pixels = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    removable: set[tuple[int, int]] = set()
    max_component_pixels = max(36, int(width * height * 0.018))

    def is_background_like(px: int, py: int) -> bool:
        r, g, b, a = pixels[px, py]
        if a == 0:
            return False
        rgb = (r, g, b)
        spread = max(rgb) - min(rgb)
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        bright = (r + g + b) / 3
        return nearest <= 78 and spread <= 42 and bright >= 150

    for y in range(height):
        for x in range(width):
            if (x, y) in visited or not is_background_like(x, y):
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited.add((x, y))
            component: set[tuple[int, int]] = set()
            touches_edge = False
            while queue:
                cx, cy = queue.popleft()
                component.add((cx, cy))
                if cx == 0 or cy == 0 or cx == width - 1 or cy == height - 1:
                    touches_edge = True
                for nx, ny in (
                    (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1),
                    (cx - 1, cy - 1), (cx + 1, cy - 1), (cx - 1, cy + 1), (cx + 1, cy + 1),
                ):
                    if not (0 <= nx < width and 0 <= ny < height):
                        continue
                    if (nx, ny) in visited or not is_background_like(nx, ny):
                        continue
                    visited.add((nx, ny))
                    queue.append((nx, ny))

            if not touches_edge and len(component) <= max_component_pixels:
                removable.update(component)

    for x, y in removable:
        pixels[x, y] = (0, 0, 0, 0)

    return work


def clear_lower_background_islands(image: Image.Image) -> Image.Image:
    palette = collect_border_palette(image)
    if not palette:
        return image

    work = image.copy()
    pixels = work.load()
    width, height = work.size
    visited: set[tuple[int, int]] = set()
    removable: set[tuple[int, int]] = set()
    max_component_pixels = max(160, int(width * height * 0.03))

    def is_background_like(px: int, py: int) -> bool:
        r, g, b, a = pixels[px, py]
        if a == 0:
            return False
        rgb = (r, g, b)
        spread = max(rgb) - min(rgb)
        nearest = min(color_distance(rgb, candidate) for candidate in palette)
        bright = (r + g + b) / 3
        return nearest <= 86 and spread <= 50 and bright >= 150

    for y in range(height):
        for x in range(width):
            if (x, y) in visited or not is_background_like(x, y):
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited.add((x, y))
            component: set[tuple[int, int]] = set()
            sum_y = 0
            while queue:
                cx, cy = queue.popleft()
                component.add((cx, cy))
                sum_y += cy
                for nx, ny in (
                    (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1),
                    (cx - 1, cy - 1), (cx + 1, cy - 1), (cx - 1, cy + 1), (cx + 1, cy + 1),
                ):
                    if not (0 <= nx < width and 0 <= ny < height):
                        continue
                    if (nx, ny) in visited or not is_background_like(nx, ny):
                        continue
                    visited.add((nx, ny))
                    queue.append((nx, ny))

            centroid_y = sum_y / max(1, len(component))
            if centroid_y >= height * 0.38 and len(component) <= max_component_pixels:
                removable.update(component)

    for x, y in removable:
        pixels[x, y] = (0, 0, 0, 0)

    return work


def clear_species_artifacts(image: Image.Image, species: str) -> Image.Image:
    work = image.copy()
    pixels = work.load()
    width, height = work.size
    if species in {"crow", "frog"}:
        visited: set[tuple[int, int]] = set()
        removable: set[tuple[int, int]] = set()
        max_component_pixels = max(180, int(width * height * 0.035))

        def is_artifact_like(px: int, py: int) -> bool:
            r, g, b, a = pixels[px, py]
            if a == 0:
                return False
            rgb = (r, g, b)
            spread = max(rgb) - min(rgb)
            bright = (r + g + b) / 3
            return bright >= 178 and spread <= 34

        for y in range(height):
            for x in range(width):
                if (x, y) in visited or not is_artifact_like(x, y):
                    continue

                queue: deque[tuple[int, int]] = deque([(x, y)])
                visited.add((x, y))
                component: set[tuple[int, int]] = set()
                sum_x = 0
                sum_y = 0
                while queue:
                    cx, cy = queue.popleft()
                    component.add((cx, cy))
                    sum_x += cx
                    sum_y += cy
                    for nx, ny in (
                        (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1),
                        (cx - 1, cy - 1), (cx + 1, cy - 1), (cx - 1, cy + 1), (cx + 1, cy + 1),
                    ):
                        if not (0 <= nx < width and 0 <= ny < height):
                            continue
                        if (nx, ny) in visited or not is_artifact_like(nx, ny):
                            continue
                        visited.add((nx, ny))
                        queue.append((nx, ny))

                centroid_x = sum_x / max(1, len(component))
                centroid_y = sum_y / max(1, len(component))
                x_ratio = centroid_x / max(1.0, width)
                y_ratio = centroid_y / max(1.0, height)
                if (
                    0.22 <= x_ratio <= 0.82 and
                    y_ratio >= 0.34 and
                    len(component) <= max_component_pixels
                ):
                    removable.update(component)

        for x, y in removable:
            pixels[x, y] = (0, 0, 0, 0)

    if species in {"rat", "squirrel", "fox", "raccoon"}:
        alpha = np.array(work, dtype=np.uint8)[:, :, 3] > 0
        labels, count = ndimage.label(alpha.astype(np.uint8))
        if count > 1:
            for index in range(1, count + 1):
                ys, xs = np.nonzero(labels == index)
                area = len(xs)
                if area == 0 or area > 6:
                    continue
                left = int(xs.min())
                right = int(xs.max()) + 1
                top = int(ys.min())
                bottom = int(ys.max()) + 1
                near_right_edge = right >= width - 2
                upper_face_band = top >= int(height * 0.45) and bottom <= int(height * 0.78)
                if near_right_edge and upper_face_band:
                    for px, py in zip(xs, ys, strict=False):
                        pixels[int(px), int(py)] = (0, 0, 0, 0)

    return work


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


def build_probable_subject_mask(image: Image.Image) -> np.ndarray | None:
    if image.width == 0 or image.height == 0:
        return None

    data = np.array(image, dtype=np.uint8)
    alpha = data[:, :, 3] > 0
    if not alpha.any():
        return None

    rgb = data[:, :, :3].astype(np.int32)
    luma = (rgb[:, :, 0] * 299 + rgb[:, :, 1] * 587 + rgb[:, :, 2] * 114) // 1000

    luma_values = luma[alpha]
    histogram = np.bincount(luma_values.astype(np.uint8), minlength=256).astype(np.float64)
    total = histogram.sum()
    cumulative_weight = 0.0
    cumulative_sum = 0.0
    total_sum = float(np.dot(np.arange(256), histogram))
    max_variance = -1.0
    otsu_threshold = 128
    for level in range(256):
        cumulative_weight += histogram[level]
        if cumulative_weight == 0:
            continue
        foreground_weight = total - cumulative_weight
        if foreground_weight == 0:
            break
        cumulative_sum += level * histogram[level]
        background_mean = cumulative_sum / cumulative_weight
        foreground_mean = (total_sum - cumulative_sum) / foreground_weight
        variance = cumulative_weight * foreground_weight * (background_mean - foreground_mean) ** 2
        if variance > max_variance:
            max_variance = variance
            otsu_threshold = level

    subject_mask = alpha & (luma <= min(170, otsu_threshold + 10))
    mask = ndimage.binary_closing(subject_mask, structure=np.ones((3, 3), dtype=bool))
    return mask


def isolate_subject_from_opaque_crop(image: Image.Image) -> Image.Image:
    mask = build_probable_subject_mask(image)
    if mask is None or not mask.any():
        return image

    labels, label_count = ndimage.label(mask)
    if label_count <= 0:
        return image

    counts = np.bincount(labels.ravel())
    counts[0] = 0
    largest_label = int(counts.argmax())
    if largest_label == 0 or counts[largest_label] == 0:
        return image

    largest_mask = labels == largest_label
    if largest_mask.sum() < 24:
        return image

    ys, xs = np.nonzero(largest_mask)
    pad = 2
    left = max(0, int(xs.min()) - pad)
    top = max(0, int(ys.min()) - pad)
    right = min(image.width, int(xs.max()) + 1 + pad)
    bottom = min(image.height, int(ys.max()) + 1 + pad)

    cropped = image.crop((left, top, right, bottom)).copy()
    cropped_mask = largest_mask[top:bottom, left:right]
    data = np.array(cropped, dtype=np.uint8)
    data[~cropped_mask] = (0, 0, 0, 0)
    return Image.fromarray(data, "RGBA")


def score_candidate_bbox(
    bbox: tuple[int, int, int, int] | None,
    segment_width: int,
    segment_height: int,
) -> float:
    if bbox is None:
        return float("-inf")

    left, top, right, bottom = bbox
    width = max(0, right - left)
    height = max(0, bottom - top)
    if width <= 0 or height <= 0:
        return float("-inf")

    area_ratio = (width * height) / max(1, segment_width * segment_height)
    touches_left = left <= 4
    touches_top = top <= 4
    touches_right = right >= segment_width - 4
    touches_bottom = bottom >= segment_height - 4
    touched_edges = sum((touches_left, touches_top, touches_right, touches_bottom))
    width_ratio = width / max(1, segment_width)
    height_ratio = height / max(1, segment_height)
    center_x = (left + right) / 2.0
    center_y = (top + bottom) / 2.0
    center_penalty = abs(center_x - (segment_width / 2.0)) / max(1.0, segment_width / 2.0)
    vertical_bias = center_y / max(1.0, segment_height)

    score = 0.0
    if 0.05 <= area_ratio <= 0.9:
        score += 2.4
    else:
        score -= abs(area_ratio - 0.35) * 4.5

    if width_ratio >= 0.18:
        score += 1.2
    else:
        score -= (0.18 - width_ratio) * 5.0

    if height_ratio >= 0.18:
        score += 1.2
    else:
        score -= (0.18 - height_ratio) * 5.0

    score -= touched_edges * 0.9
    if touches_bottom:
        score += 0.45

    if vertical_bias >= 0.38:
        score += 0.6
    else:
        score -= (0.38 - vertical_bias) * 2.0

    score -= center_penalty * 0.8
    return score


def find_pose_components(image: Image.Image) -> list[tuple[int, int, int, int]]:
    mid_x = image.width // 2
    prefer_alpha_only = edge_alpha_is_present(image)
    components: list[tuple[int, int, int, int]] = []
    for left, right in ((0, mid_x), (mid_x, image.width)):
        if prefer_alpha_only:
            components.append(find_largest_component_in_bounds(image, left, 0, right, image.height, True))
            continue

        segment = image.crop((left, 0, right, image.height))
        cleaned = remove_checkerboard_background(segment)
        cleaned = scrub_border_palette_matte(cleaned)
        cleaned = strip_palette_like_noise(cleaned)

        alpha_bbox = cleaned.getchannel("A").getbbox()
        largest_bbox: tuple[int, int, int, int] | None = None
        try:
            largest_bbox = find_largest_component_in_bounds(cleaned, 0, 0, cleaned.width, cleaned.height, True)
        except ValueError:
            largest_bbox = None

        isolated = isolate_subject_from_opaque_crop(segment)
        isolated_bbox = isolated.getchannel("A").getbbox()

        candidates = [candidate for candidate in (alpha_bbox, largest_bbox, isolated_bbox) if candidate is not None]
        if not candidates:
            components.append(find_largest_component_in_bounds(image, left, 0, right, image.height, False))
            continue

        best = max(
            candidates,
            key=lambda candidate: score_candidate_bbox(candidate, segment.width, segment.height))
        components.append((left + best[0], best[1], left + best[2], best[3]))

    return components


def trim_to_alpha(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("Expected non-empty sprite after alpha trim.")
    return image.crop(bbox)


def keep_largest_component(image: Image.Image) -> Image.Image:
    pixels = image.load()
    width, height = image.size
    visited: set[tuple[int, int]] = set()
    largest: set[tuple[int, int]] = set()

    for y in range(height):
        for x in range(width):
            if (x, y) in visited or pixels[x, y][3] == 0:
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited.add((x, y))
            component: set[tuple[int, int]] = set()
            while queue:
                cx, cy = queue.popleft()
                component.add((cx, cy))
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
                    if not (0 <= nx < width and 0 <= ny < height):
                        continue
                    if (nx, ny) in visited or pixels[nx, ny][3] == 0:
                        continue
                    visited.add((nx, ny))
                    queue.append((nx, ny))

            if len(component) > len(largest):
                largest = component

    cleaned = Image.new("RGBA", image.size, (0, 0, 0, 0))
    cleaned_pixels = cleaned.load()
    for x, y in largest:
        cleaned_pixels[x, y] = pixels[x, y]

    return cleaned


def quantize_alpha_edges(image: Image.Image, threshold: int = 72) -> Image.Image:
    alpha = image.getchannel("A")
    extrema = alpha.getextrema()
    if extrema == (0, 0) or extrema == (255, 255):
        return image

    quantized = image.copy()
    pixels = quantized.load()
    for y in range(quantized.height):
        for x in range(quantized.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            pixels[x, y] = (r, g, b, 255 if a >= threshold else 0)
    return quantized


def remove_small_components(image: Image.Image, min_pixels: int) -> Image.Image:
    if min_pixels <= 1:
        return image

    pixels = image.load()
    width, height = image.size
    visited: set[tuple[int, int]] = set()
    kept: set[tuple[int, int]] = set()

    for y in range(height):
        for x in range(width):
            if (x, y) in visited or pixels[x, y][3] == 0:
                continue

            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited.add((x, y))
            component: set[tuple[int, int]] = set()
            while queue:
                cx, cy = queue.popleft()
                component.add((cx, cy))
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
                    if not (0 <= nx < width and 0 <= ny < height):
                        continue
                    if (nx, ny) in visited or pixels[nx, ny][3] == 0:
                        continue
                    visited.add((nx, ny))
                    queue.append((nx, ny))

            if len(component) >= min_pixels:
                kept.update(component)

    cleaned = Image.new("RGBA", image.size, (0, 0, 0, 0))
    cleaned_pixels = cleaned.load()
    for x, y in kept:
        cleaned_pixels[x, y] = pixels[x, y]

    return cleaned


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
            blend_strength = 0.98 if luma >= 84 else 0.9 if luma >= 52 else 0.36
            out_r = round(r * (1 - blend_strength) + tinted_rgb[0] * blend_strength)
            out_g = round(g * (1 - blend_strength) + tinted_rgb[1] * blend_strength)
            out_b = round(b * (1 - blend_strength) + tinted_rgb[2] * blend_strength)
            if luma >= 64:
                out_r = min(255, round(out_r * 1.06))
                out_g = min(255, round(out_g * 1.06))
                out_b = min(255, round(out_b * 1.06))
            target[x, y] = (out_r, out_g, out_b, a)

    return tinted


def apply_vertical_wave(image: Image.Image, amplitude: float, phase: float, cycles: float) -> Image.Image:
    if abs(amplitude) < 0.01:
        return image

    pad = max(1, math.ceil(abs(amplitude)) + 1)
    canvas = Image.new("RGBA", (image.width, image.height + (pad * 2)), (0, 0, 0, 0))
    width_divisor = max(1, image.width - 1)
    for x in range(image.width):
        offset = round(amplitude * math.sin(((x / width_divisor) * cycles * math.tau) + phase))
        column = image.crop((x, 0, x + 1, image.height))
        canvas.alpha_composite(column, (x, pad + offset))
    return trim_to_alpha(canvas)


def apply_horizontal_wave(image: Image.Image, amplitude: float, phase: float, cycles: float) -> Image.Image:
    if abs(amplitude) < 0.01:
        return image

    pad = max(1, math.ceil(abs(amplitude)) + 1)
    canvas = Image.new("RGBA", (image.width + (pad * 2), image.height), (0, 0, 0, 0))
    height_divisor = max(1, image.height - 1)
    for y in range(image.height):
        offset = round(amplitude * math.sin(((y / height_divisor) * cycles * math.tau) + phase))
        row = image.crop((0, y, image.width, y + 1))
        canvas.alpha_composite(row, (pad + offset, y))
    return trim_to_alpha(canvas)


def apply_region_shift(
    image: Image.Image,
    left_ratio: float,
    top_ratio: float,
    right_ratio: float,
    bottom_ratio: float,
    dx: float,
    dy: float,
) -> Image.Image:
    if abs(dx) < 0.01 and abs(dy) < 0.01:
        return image

    left = max(0, min(image.width - 1, round(image.width * left_ratio)))
    top = max(0, min(image.height - 1, round(image.height * top_ratio)))
    right = max(left + 1, min(image.width, round(image.width * right_ratio)))
    bottom = max(top + 1, min(image.height, round(image.height * bottom_ratio)))

    region = image.crop((left, top, right, bottom))
    if region.getchannel("A").getbbox() is None:
        return image

    base = image.copy()
    base_pixels = base.load()
    region_pixels = region.load()
    for ry in range(region.height):
        for rx in range(region.width):
            if region_pixels[rx, ry][3] > 0:
                base_pixels[left + rx, top + ry] = (0, 0, 0, 0)

    offset_x = max(0, min(base.width - region.width, left + round(dx)))
    offset_y = max(0, min(base.height - region.height, top + round(dy)))
    base.alpha_composite(region, (offset_x, offset_y))
    return trim_to_alpha(base)


def apply_center_coil(image: Image.Image, strength: float) -> Image.Image:
    if abs(strength) < 0.01:
        return image

    pad = max(2, math.ceil(abs(strength)) + 2)
    canvas = Image.new("RGBA", (image.width + pad * 2, image.height), (0, 0, 0, 0))
    center_x = image.width / 2.0
    height_divisor = max(1, image.height - 1)
    for y in range(image.height):
        row = image.crop((0, y, image.width, y + 1))
        normalized = abs((y / height_divisor) - 0.5) * 2.0
        inward = round((1.0 - normalized) * strength)
        canvas.alpha_composite(row, (pad + inward, y))
    return trim_to_alpha(canvas)


def apply_recipe(sprite: Image.Image, recipe: dict[str, float]) -> Image.Image:
    work = sprite.copy()

    if recipe.get("brightness", 1.0) != 1.0:
        work = ImageEnhance.Brightness(work).enhance(recipe["brightness"])

    if recipe.get("wash", 0.0) > 0:
        wash = Image.new("RGBA", work.size, (185, 225, 205, round(255 * recipe["wash"])))
        work = Image.alpha_composite(work, wash)

    scale_x = recipe.get("scale_x", recipe.get("scale", 1.0))
    scale_y = recipe.get("scale_y", recipe.get("scale", 1.0))
    if scale_x != 1.0 or scale_y != 1.0:
        scaled_width = max(1, round(work.width * scale_x))
        scaled_height = max(1, round(work.height * scale_y))
        work = work.resize((scaled_width, scaled_height), Image.Resampling.NEAREST)

    if recipe.get("wave_y_amplitude", 0.0):
        work = apply_vertical_wave(
            work,
            recipe["wave_y_amplitude"],
            recipe.get("wave_y_phase", 0.0),
            recipe.get("wave_y_cycles", 1.0),
        )

    if recipe.get("wave_x_amplitude", 0.0):
        work = apply_horizontal_wave(
            work,
            recipe["wave_x_amplitude"],
            recipe.get("wave_x_phase", 0.0),
            recipe.get("wave_x_cycles", 1.0),
        )

    if recipe.get("coil_strength", 0.0):
        work = apply_center_coil(work, recipe["coil_strength"])

    if recipe.get("wing_shift_y", 0.0) or recipe.get("wing_shift_x", 0.0):
        work = apply_region_shift(
            work,
            0.22,
            0.0,
            0.88,
            0.52,
            recipe.get("wing_shift_x", 0.0),
            recipe.get("wing_shift_y", 0.0),
        )

    if recipe.get("head_shift_x", 0.0) or recipe.get("head_shift_y", 0.0):
        work = apply_region_shift(
            work,
            0.56,
            0.0,
            0.94,
            0.44,
            recipe.get("head_shift_x", 0.0),
            recipe.get("head_shift_y", 0.0),
        )

    if recipe.get("front_leg_dx", 0.0) or recipe.get("front_leg_dy", 0.0):
        work = apply_region_shift(
            work,
            0.52,
            0.48,
            0.84,
            1.0,
            recipe.get("front_leg_dx", 0.0),
            recipe.get("front_leg_dy", 0.0),
        )

    if recipe.get("rear_leg_dx", 0.0) or recipe.get("rear_leg_dy", 0.0):
        work = apply_region_shift(
            work,
            0.16,
            0.48,
            0.5,
            1.0,
            recipe.get("rear_leg_dx", 0.0),
            recipe.get("rear_leg_dy", 0.0),
        )

    if recipe.get("tail_shift_x", 0.0) or recipe.get("tail_shift_y", 0.0):
        work = apply_region_shift(
            work,
            0.0,
            0.3,
            0.42,
            0.92,
            recipe.get("tail_shift_x", 0.0),
            recipe.get("tail_shift_y", 0.0),
        )

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
    animation_name: str | None = None,
    gender: str | None = None,
) -> Image.Image:
    def describe_alpha(image: Image.Image) -> tuple[int, int]:
        data = np.array(image, dtype=np.uint8)
        alpha = data[:, :, 3] > 0
        if not alpha.any():
            return 0, 0
        labels, count = ndimage.label(alpha.astype(np.uint8))
        return int(alpha.sum()), int(count)

    canvas_size, target_size = resolve_frame_layout(species, age_stage, animation_name)
    target_width, target_height = target_size
    scale = min(target_width / sprite.width, target_height / sprite.height)
    max_upscale = resolve_profile(species).get("max_upscale", 1.22)
    if scale > 1.0:
        scale = min(scale, max_upscale)
    if gender is not None:
        scale *= GENDER_SCALE_ADJUSTMENTS.get((species, age_stage, gender), 1.0)
    scaled_width = max(1, round(sprite.width * scale))
    scaled_height = max(1, round(sprite.height * scale))
    if scaled_width < sprite.width or scaled_height < sprite.height:
        resized_nearest = sprite.resize((scaled_width, scaled_height), Image.Resampling.NEAREST)
        resized_box = sprite.resize((scaled_width, scaled_height), Image.Resampling.BOX)
        nearest_area, _ = describe_alpha(resized_nearest)
        box_area, _ = describe_alpha(resized_box)
        dense_downscale_species = {"pigeon", "goose", "raccoon", "squirrel", "deer"}
        if species in dense_downscale_species:
            resized = resized_box if box_area > max(nearest_area * 1.08, nearest_area + 8) else resized_nearest
        else:
            resized = resized_box if box_area > max(nearest_area * 1.3, nearest_area + 24) else resized_nearest
    else:
        resized = sprite.resize((scaled_width, scaled_height), Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    offset_x = (canvas_size[0] - scaled_width) // 2 + int(recipe.get("dx", 0)) + placement_override.dx
    offset_y = canvas_size[1] - scaled_height + int(recipe.get("dy", 0)) + placement_override.dy

    offset_x = max(min(offset_x, canvas_size[0] - scaled_width), 0)
    offset_y = max(min(offset_y, canvas_size[1] - scaled_height), 0)
    canvas.alpha_composite(resized, (offset_x, offset_y))

    raw_canvas = canvas.copy()
    gentle = remove_small_components(raw_canvas, 6)
    gentle_area, gentle_components = describe_alpha(gentle)

    if species in {"pigeon", "goose"}:
        return gentle

    cleaned = scrub_border_palette_matte(canvas)
    cleaned = strip_palette_like_noise(cleaned)
    cleaned = clear_bright_edge_matte(cleaned)
    cleaned = clear_transparency_connected_matte(cleaned)
    cleaned = clear_small_background_like_islands(cleaned)
    cleaned = clear_lower_background_islands(cleaned)
    cleaned = clear_species_artifacts(cleaned, species)
    cleaned = keep_largest_component(cleaned)
    cleaned = remove_small_components(cleaned, 10)
    cleaned = quantize_alpha_edges(cleaned)
    cleaned = remove_small_components(cleaned, 6)
    cleaned_area, cleaned_components = describe_alpha(cleaned)

    if gentle_area > 0:
        area_loss_ratio = cleaned_area / gentle_area if gentle_area else 0.0
        if area_loss_ratio < 0.6 or (cleaned_components > max(2, gentle_components + 1) and cleaned_area < gentle_area):
            return gentle

    return cleaned


def extract_gender_pose(image: Image.Image, bbox: tuple[int, int, int, int], crop_override: CropOverride, species: str) -> Image.Image:
    left, top, right, bottom = bbox
    left = max(0, left - crop_override.pad_left)
    top = max(0, top - crop_override.pad_top)
    right = min(image.width, right + crop_override.pad_right)
    bottom = min(image.height, bottom + crop_override.pad_bottom)
    cropped = image.crop((left, top, right, bottom))
    checker_clean = remove_checkerboard_background(cropped)
    checker_clean = scrub_border_palette_matte(checker_clean)
    checker_clean = scrub_neutral_edge_matte(checker_clean)
    checker_clean = strip_palette_like_noise(checker_clean)

    if checker_clean.getchannel("A").getbbox() is not None:
        cropped = checker_clean
    else:
        cropped = isolate_subject_from_opaque_crop(cropped)
        cropped = scrub_border_palette_matte(cropped)
        cropped = scrub_neutral_edge_matte(cropped)
        cropped = strip_palette_like_noise(cropped)

    cropped = clear_bright_edge_matte(cropped)
    cropped = clear_transparency_connected_matte(cropped)
    cropped = clear_small_background_like_islands(cropped)
    cropped = clear_lower_background_islands(cropped)
    cropped = clear_species_artifacts(cropped, species)
    cropped = trim_to_alpha(cropped)
    cropped = keep_largest_component(cropped)
    cropped = remove_small_components(cropped, 10)
    return cropped


def ensure_transparent_frame(image_path: Path) -> None:
    with image_path.open("rb") as stream:
        header = stream.read(33)
    if len(header) < 33 or header[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{image_path} is not a valid PNG.")
    width = int.from_bytes(header[16:20], "big")
    height = int.from_bytes(header[20:24], "big")
    color_type = header[25]
    species = image_path.parents[3].name if len(image_path.parents) >= 4 else ""
    age_stage = image_path.parents[2].name if len(image_path.parents) >= 3 else ""
    animation_name = image_path.stem.rsplit("_", 1)[0]
    expected_canvas, _ = resolve_frame_layout(species, age_stage, animation_name)
    if width != expected_canvas[0] or height != expected_canvas[1]:
        raise ValueError(f"{image_path} expected {expected_canvas[0]}x{expected_canvas[1]}, found {width}x{height}.")
    if color_type not in {4, 6}:
        raise ValueError(f"{image_path} must preserve alpha, color type was {color_type}.")


def save_png_with_retry(frame: Image.Image, out_path: Path, attempts: int = 4, delay_seconds: float = 0.15) -> None:
    last_error: OSError | None = None
    for attempt in range(attempts):
        try:
            frame.save(out_path)
            return
        except OSError as error:
            last_error = error
            if attempt == attempts - 1:
                break
            time.sleep(delay_seconds * (attempt + 1))
    if last_error is not None:
        raise last_error


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
                adjusted_recipe = compose_recipe(recipe, profile, profile_name, animation_name, frame_index, len(frames))
                transformed = apply_recipe(tinted, adjusted_recipe)
                frame = fit_to_canvas(
                    transformed,
                    species,
                    age_stage,
                    adjusted_recipe,
                    placement_override,
                    animation_name,
                    gender,
                )
                out_path = out_dir / f"{animation_name}_{frame_index:02d}.png"
                save_png_with_retry(frame, out_path)
                exported += 1

    return exported


def export_entry(entry: ManifestEntry, source_root: Path, output_root: Path) -> dict[str, Any]:
    source_path = source_root / entry.source
    image = Image.open(source_path).convert("RGBA")
    isolated = remove_checkerboard_background(image)
    components = find_pose_components(isolated)

    if len(components) != 2:
        raise ValueError(f"Expected 2 pose components in {source_path}, found {len(components)}")

    exported = 0
    pose_summaries: list[dict[str, Any]] = []
    for gender, bbox in zip(entry.component_order, components, strict=True):
        crop_override = entry.component_crop_overrides.get(gender, entry.crop_override)
        sprite = extract_gender_pose(isolated, bbox, crop_override, entry.species)
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


def validate_output(output_root: Path, allowed_species: set[str] | None = None) -> dict[str, int]:
    expected_frame_counts = {name: len(frames) for name, frames in ANIMATION_RECIPES.items()}
    counts_by_species: dict[str, int] = {}

    for species_dir in sorted(output_root.iterdir()):
        if not species_dir.is_dir():
            continue
        if allowed_species is not None and species_dir.name.lower() not in allowed_species:
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


def collect_output_counts(output_root: Path) -> dict[str, int]:
    counts_by_species: dict[str, int] = {}
    for species_dir in sorted(output_root.iterdir()):
        if not species_dir.is_dir():
            continue
        counts_by_species[species_dir.name] = sum(1 for _ in species_dir.rglob("*.png"))
    return counts_by_species


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE_ROOT)
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT_ROOT)
    parser.add_argument("--clean", action="store_true")
    parser.add_argument("--species", nargs="*", help="Optional species ids to regenerate.")
    args = parser.parse_args()

    manifest = load_manifest(args.manifest)
    if args.species:
        wanted = {species.lower() for species in args.species}
        manifest = [entry for entry in manifest if entry.species.lower() in wanted]
        if not manifest:
            raise ValueError(f"No manifest entries matched species filter: {', '.join(args.species)}")
    if args.clean:
        clean_output(args.output_root)

    args.output_root.mkdir(parents=True, exist_ok=True)

    exported = 0
    entry_summaries: list[dict[str, Any]] = []
    for entry in manifest:
        entry_summary = export_entry(entry, args.source_root, args.output_root)
        entry_summaries.append(entry_summary)
        exported += int(entry_summary["exported_frames"])

    validated_counts = validate_output(
        args.output_root,
        {species.lower() for species in args.species} if args.species else None,
    )
    counts_by_species = collect_output_counts(args.output_root)
    summary = {
        "canvas_size": {"width": CANVAS_SIZE[0], "height": CANVAS_SIZE[1]},
        "frame_layout_overrides": FRAME_LAYOUT_OVERRIDES,
        "exported_frames": exported,
        "species_counts": counts_by_species,
        "validated_species_counts": validated_counts,
        "entries": entry_summaries,
    }
    (args.output_root / SUMMARY_NAME).write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"exported {exported} runtime frame(s) to {args.output_root}")


if __name__ == "__main__":
    main()
