#!/usr/bin/env python3
"""C-PHASE 203A procedural cleanup sweep for runtime sprite variants.

Owner-approved triage queue (2026-06-12) lane `procedural_cleanup`:
crow, goose, raccoon, rat, squirrel — small cutoffs, silhouette cleanup,
sizing issues, stray pixels between legs (owner-reported), plus the
BUG-007 color-conformance check.

Deliberately conservative, in the repair_crow_runtime_edges.py /
repair_runtime_sprite_canvas_padding.py lineage:
- fringe cleanup: fully removes only near-invisible halo pixels
  (alpha < --fringe-alpha, default 24);
- speck removal: deletes 8-connected alpha components of
  <= --max-speck-pixels (default 8) pixels — stray dots between legs and
  around silhouettes; the main body component is never touched;
- tint conformance (BUG-007): per variant, the circular-mean hue of
  saturated body pixels must sit within --hue-tolerance degrees of the
  variant's expected tint hue (tints imported from
  generate_runtime_pose_sprites.COLOR_VARIANTS); nonconforming variants
  are repaired with the same colorize() used by the tint pipeline, but
  ONLY when --fix-tint is passed;
- edge contact and within-row size outliers are REPORTED only (canvas
  padding stays in repair_runtime_sprite_canvas_padding.py; rescaling
  art is out of scope for a procedural sweep).

Default mode is a dry-run analysis. --apply requires --backup-root and
writes a sha256 backup of every frame it changes before changing it.
"""

from __future__ import annotations

import argparse
import colorsys
import hashlib
import json
import math
import shutil
import sys
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from audit_sprite_contract import EXPECTED_ANIMATIONS  # noqa: E402
from generate_runtime_pose_sprites import COLOR_VARIANTS, colorize  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_RUNTIME_ROOT = ROOT / "sprites_runtime"
EIGHT_CONNECTED = np.ones((3, 3), dtype=bool)
SAT_FLOOR = 0.15
LUMA_FLOOR = 40

# Per-species dark-rim trim settings, calibrated on before/after strips
# (vnext/artifacts/c-phase-203a-procedural-cleanup/inspect/, 2026-06-12).
# A pixel is trimmed when it is opaque, on the silhouette boundary, darker than
# dark_luma, and weakly connected (<= max_neighbors of 8). protect_bottom_frac
# shields the bottom slice of the body bbox — goose legs/feet are legitimately
# dark and 1px thin, and the unprotected filter amputated them.
DARK_RIM_PARAMS: dict[str, dict[str, float]] = {
    "squirrel": {"dark_luma": 56, "max_neighbors": 4, "passes": 2, "protect_bottom_frac": 0.0},
    "rat": {"dark_luma": 56, "max_neighbors": 4, "passes": 2, "protect_bottom_frac": 0.0},
    "raccoon": {"dark_luma": 56, "max_neighbors": 4, "passes": 2, "protect_bottom_frac": 0.0},
    "goose": {"dark_luma": 44, "max_neighbors": 3, "passes": 1, "protect_bottom_frac": 0.28},
    "crow": {"dark_luma": 44, "max_neighbors": 3, "passes": 1, "protect_bottom_frac": 0.0},
}


def clean_dark_rim(rgba: np.ndarray, dark_luma: float, max_neighbors: int, passes: int, protect_bottom_frac: float) -> tuple[np.ndarray, int]:
    out = rgba.copy()
    removed = 0
    neighbor_kernel = np.array([[1, 1, 1], [1, 0, 1], [1, 1, 1]], dtype=np.uint8)
    for _ in range(int(passes)):
        opaque = out[:, :, 3] > 0
        body = np.argwhere(opaque)
        if body.size == 0:
            break
        y_min, y_max = int(body[:, 0].min()), int(body[:, 0].max())
        protect_y = y_max - int((y_max - y_min) * protect_bottom_frac)
        rgb = out[:, :, :3].astype(np.float64)
        luma = rgb[:, :, 0] * 0.299 + rgb[:, :, 1] * 0.587 + rgb[:, :, 2] * 0.114
        neighbors = ndimage.convolve(opaque.astype(np.uint8), neighbor_kernel, mode="constant")
        rows = np.arange(out.shape[0])[:, None] * np.ones(out.shape[1], dtype=int)[None, :]
        target = opaque & (neighbors < 8) & (luma < dark_luma) & (neighbors <= max_neighbors) & (rows < protect_y)
        count = int(target.sum())
        if count == 0:
            break
        out[target] = 0
        removed += count
    return out, removed


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def expected_hue_degrees(color_name: str) -> float:
    r, g, b = (channel / 255 for channel in COLOR_VARIANTS[color_name])
    hue, _, _ = colorsys.rgb_to_hsv(r, g, b)
    return hue * 360.0


def circular_hue_distance(a: float, b: float) -> float:
    delta = abs(a - b) % 360.0
    return min(delta, 360.0 - delta)


@dataclass
class FrameFinding:
    path: str
    fringe_pixels: int
    speck_components: int
    speck_pixels: int
    dark_rim_pixels: int
    edge_sides: list[str]
    bbox: tuple[int, int, int, int] | None
    changed: bool = False
    before_sha256: str | None = None
    after_sha256: str | None = None


@dataclass
class VariantFinding:
    variant: str
    frame_count: int
    fringe_pixels: int
    speck_components: int
    speck_pixels: int
    dark_rim_pixels: int
    edge_touch_frames: int
    size_outlier_frames: list[str]
    measured_hue: float | None
    expected_hue: float
    hue_distance: float | None
    measured_saturation: float | None
    tint_conform: bool
    tint_fixed: bool = False
    frames: list[FrameFinding] = field(default_factory=list)


def analyze_frame(rgba: np.ndarray, fringe_alpha: int, max_speck: int, species: str) -> tuple[np.ndarray, FrameFinding]:
    """Return (cleaned rgba, finding). Cleaning = fringe + speck + dark-rim removal."""
    cleaned = rgba.copy()
    alpha = cleaned[:, :, 3]

    fringe_mask = (alpha > 0) & (alpha < fringe_alpha)
    fringe_pixels = int(fringe_mask.sum())
    cleaned[fringe_mask] = 0

    dark_rim_pixels = 0
    rim_params = DARK_RIM_PARAMS.get(species)
    if rim_params:
        cleaned, dark_rim_pixels = clean_dark_rim(cleaned, **rim_params)

    alpha = cleaned[:, :, 3]
    labels, count = ndimage.label(alpha > 0, structure=EIGHT_CONNECTED)
    speck_components = 0
    speck_pixels = 0
    if count > 1:
        sizes = ndimage.sum_labels(np.ones_like(labels), labels, index=range(1, count + 1))
        for component_index, size in enumerate(sizes, start=1):
            if size <= max_speck and size < sizes.max():
                speck_mask = labels == component_index
                speck_components += 1
                speck_pixels += int(size)
                cleaned[speck_mask] = 0

    alpha = cleaned[:, :, 3]
    height, width = alpha.shape
    edge_sides = []
    if alpha[:, 0].any():
        edge_sides.append("left")
    if alpha[:, width - 1].any():
        edge_sides.append("right")
    if alpha[0, :].any():
        edge_sides.append("top")
    if alpha[height - 1, :].any():
        edge_sides.append("bottom")

    opaque = np.argwhere(alpha > 0)
    bbox = None
    if opaque.size:
        y0, x0 = opaque.min(axis=0)
        y1, x1 = opaque.max(axis=0)
        bbox = (int(x0), int(y0), int(x1) + 1, int(y1) + 1)

    finding = FrameFinding(
        path="",
        fringe_pixels=fringe_pixels,
        speck_components=speck_components,
        speck_pixels=speck_pixels,
        dark_rim_pixels=dark_rim_pixels,
        edge_sides=edge_sides,
        bbox=bbox,
    )
    return cleaned, finding


def measure_variant_tint(frames: list[np.ndarray]) -> tuple[float | None, float | None]:
    """Circular-mean hue (degrees) + mean saturation of saturated body pixels."""
    sin_total = 0.0
    cos_total = 0.0
    sat_total = 0.0
    pixel_count = 0
    for rgba in frames:
        alpha = rgba[:, :, 3]
        body = alpha > 0
        if not body.any():
            continue
        rgb = rgba[body][:, :3].astype(np.float64) / 255.0
        maxc = rgb.max(axis=1)
        minc = rgb.min(axis=1)
        delta = maxc - minc
        luma = rgb[:, 0] * 0.299 + rgb[:, 1] * 0.587 + rgb[:, 2] * 0.114
        with np.errstate(divide="ignore", invalid="ignore"):
            saturation = np.where(maxc > 0, delta / maxc, 0.0)
        keep = (saturation >= SAT_FLOOR) & (luma * 255 >= LUMA_FLOOR) & (delta > 0)
        if not keep.any():
            continue
        r, g, b = rgb[keep, 0], rgb[keep, 1], rgb[keep, 2]
        mx, dl = maxc[keep], delta[keep]
        hue = np.where(
            mx == r,
            ((g - b) / dl) % 6.0,
            np.where(mx == g, (b - r) / dl + 2.0, (r - g) / dl + 4.0),
        ) * 60.0
        radians = np.deg2rad(hue)
        sin_total += float(np.sin(radians).sum())
        cos_total += float(np.cos(radians).sum())
        sat_total += float(saturation[keep].sum())
        pixel_count += int(keep.sum())
    if pixel_count == 0:
        return None, None
    mean_hue = math.degrees(math.atan2(sin_total, cos_total)) % 360.0
    return mean_hue, sat_total / pixel_count


def process_variant(
    variant_dir: Path,
    runtime_root: Path,
    color: str,
    fringe_alpha: int,
    max_speck: int,
    hue_tolerance: float,
    min_saturation: float,
    apply: bool,
    fix_tint: bool,
    backup_root: Path | None,
) -> VariantFinding:
    frame_paths = sorted(variant_dir.glob("*.png"))
    raw_frames: list[np.ndarray] = []
    cleaned_frames: list[np.ndarray] = []
    findings: list[FrameFinding] = []

    species = variant_dir.relative_to(runtime_root).parts[0]
    for frame_path in frame_paths:
        rgba = np.array(Image.open(frame_path).convert("RGBA"), dtype=np.uint8)
        cleaned, finding = analyze_frame(rgba, fringe_alpha, max_speck, species)
        finding.path = str(frame_path.relative_to(runtime_root))
        raw_frames.append(rgba)
        cleaned_frames.append(cleaned)
        findings.append(finding)

    # Within-row size outliers (report only): bbox height vs row median per family.
    rows: dict[str, list[tuple[str, FrameFinding]]] = {}
    for frame_path, finding in zip(frame_paths, findings):
        family = frame_path.stem.rpartition("_")[0]
        rows.setdefault(family, []).append((frame_path.name, finding))
    size_outliers: list[str] = []
    for family, members in rows.items():
        heights = [f.bbox[3] - f.bbox[1] for _, f in members if f.bbox]
        if len(heights) < 2:
            continue
        median = sorted(heights)[len(heights) // 2]
        for name, finding in members:
            if finding.bbox and median > 0:
                height = finding.bbox[3] - finding.bbox[1]
                if abs(height - median) / median > 0.20:
                    size_outliers.append(name)

    # Tint conformance is measured on REQUIRED families only: a variant with
    # correctly tinted optional families can still carry untinted required
    # frames (the original BUG-007 case, goose/baby/female/blue — its blue
    # pilot optional families outvoted the cream required frames in a
    # whole-variant mean). Optional families re-derive from required sources
    # in C-PHASE 199R, so required-frame conformance is the root fix.
    required_cleaned = [
        cleaned
        for frame_path, cleaned in zip(frame_paths, cleaned_frames)
        if frame_path.stem.rpartition("_")[0] in EXPECTED_ANIMATIONS
    ]
    measured_hue, measured_sat = measure_variant_tint(required_cleaned)
    expected = expected_hue_degrees(color)
    hue_distance = circular_hue_distance(measured_hue, expected) if measured_hue is not None else None
    tint_conform = (
        measured_hue is not None
        and measured_sat is not None
        and hue_distance <= hue_tolerance
        and measured_sat >= min_saturation
    )

    tint_fixed = False
    if apply:
        for frame_path, rgba, cleaned, finding in zip(frame_paths, raw_frames, cleaned_frames, findings):
            output = cleaned
            is_required = frame_path.stem.rpartition("_")[0] in EXPECTED_ANIMATIONS
            if fix_tint and not tint_conform and is_required:
                output = np.array(
                    colorize(Image.fromarray(output, "RGBA"), color).convert("RGBA"), dtype=np.uint8
                )
                tint_fixed = True
            if np.array_equal(output, rgba):
                continue
            assert backup_root is not None
            backup_path = backup_root / frame_path.relative_to(runtime_root)
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            if not backup_path.exists():
                shutil.copy2(frame_path, backup_path)
            finding.before_sha256 = sha256(frame_path)
            Image.fromarray(output, "RGBA").save(frame_path)
            finding.after_sha256 = sha256(frame_path)
            finding.changed = True

    return VariantFinding(
        variant=str(variant_dir.relative_to(runtime_root)),
        frame_count=len(frame_paths),
        fringe_pixels=sum(f.fringe_pixels for f in findings),
        speck_components=sum(f.speck_components for f in findings),
        speck_pixels=sum(f.speck_pixels for f in findings),
        dark_rim_pixels=sum(f.dark_rim_pixels for f in findings),
        edge_touch_frames=sum(1 for f in findings if set(f.edge_sides) - {"bottom"}),
        size_outlier_frames=sorted(set(size_outliers)),
        measured_hue=round(measured_hue, 1) if measured_hue is not None else None,
        expected_hue=round(expected, 1),
        hue_distance=round(hue_distance, 1) if hue_distance is not None else None,
        measured_saturation=round(measured_sat, 3) if measured_sat is not None else None,
        tint_conform=tint_conform,
        tint_fixed=tint_fixed,
        frames=findings,
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--species", nargs="+", required=True)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME_ROOT)
    parser.add_argument("--fringe-alpha", type=int, default=24)
    parser.add_argument("--max-speck-pixels", type=int, default=8)
    parser.add_argument("--hue-tolerance", type=float, default=40.0)
    parser.add_argument("--min-saturation", type=float, default=0.15)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--fix-tint", action="store_true")
    parser.add_argument("--backup-root", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    if args.apply and args.backup_root is None:
        parser.error("--apply requires --backup-root")
    if "artifacts" not in args.output.resolve().parts:
        parser.error("--output must live under an artifacts directory")
    if args.backup_root and "artifacts" not in args.backup_root.resolve().parts:
        parser.error("--backup-root must live under an artifacts directory")

    variants: list[VariantFinding] = []
    for species in args.species:
        species_root = args.runtime_root / species
        if not species_root.is_dir():
            parser.error(f"unknown species: {species}")
        for age_dir in sorted(species_root.iterdir()):
            for gender_dir in sorted(d for d in age_dir.iterdir() if d.is_dir()):
                for color_dir in sorted(d for d in gender_dir.iterdir() if d.is_dir()):
                    variants.append(
                        process_variant(
                            color_dir,
                            args.runtime_root,
                            color_dir.name,
                            args.fringe_alpha,
                            args.max_speck_pixels,
                            args.hue_tolerance,
                            args.min_saturation,
                            args.apply,
                            args.fix_tint,
                            args.backup_root,
                        )
                    )

    report = {
        "schemaVersion": "procedural-cleanup/1",
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "mode": "apply" if args.apply else "dry-run",
        "fixTint": args.fix_tint,
        "thresholds": {
            "fringeAlpha": args.fringe_alpha,
            "maxSpeckPixels": args.max_speck_pixels,
            "hueToleranceDegrees": args.hue_tolerance,
            "minSaturation": args.min_saturation,
        },
        "totals": {
            "variants": len(variants),
            "frames": sum(v.frame_count for v in variants),
            "fringePixels": sum(v.fringe_pixels for v in variants),
            "speckComponents": sum(v.speck_components for v in variants),
            "speckPixels": sum(v.speck_pixels for v in variants),
            "darkRimPixels": sum(v.dark_rim_pixels for v in variants),
            "edgeTouchFrames": sum(v.edge_touch_frames for v in variants),
            "tintNonconformVariants": sum(1 for v in variants if not v.tint_conform),
            "tintFixedVariants": sum(1 for v in variants if v.tint_fixed),
            "changedFrames": sum(1 for v in variants for f in v.frames if f.changed),
        },
        "variants": [asdict(v) for v in variants],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report["totals"], indent=2))
    print(f"report: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
