#!/usr/bin/env python3
"""
Synthesize body-pose candidate frames for the six optional ball families
(C-PHASE 199; reference implementation: the C-PHASE 198 pilot transform).

The ball is a RUNTIME OVERLAY (PropOverlayKind.Ball) — no ball pixels are ever
baked into frames. Each family is therefore a body-pose loop derived
deterministically from the variant's own existing on-contract frames, chosen so
the body motion semantically matches the family:

  family           frames  source row      transform
  ---------------  ------  --------------  -------------------------------------------
  play_ball        6       happy           [h0,h1,h2,h3,h2,h1] + bounce lift [0,1,2,1,0,1]
  hold_ball        4       idle            [i0,i1,i2,i3] + settle lift [0,1,1,0]
  pickup_ball      4       eat             [e0,e1,e2,e3] (head lowers toward ground)
  drop_ball        4       eat reversed    [e3,e2,e1,e0] (head returns up)
  carry_ball_walk  6       walk            [w0..w5] unchanged (carry = walk; overlay
                                           attaches at the hold anchor)
  carry_ball_run   6       walk            [w0..w5] + run-bounce lift [0,1,2,1,0,2]
                                           (the C-PHASE 198 pilot transform)

"Lift" pastes the source frame N pixels higher inside the SAME canvas, clamped to
the frame's transparent top margin so no body pixel is ever cropped (rows flagged
edge-touch by the quality audit get lift 0 on affected frames). No repaint, no
scale, no mirror, no warp. Output canvases equal the source row's canvas, so the
per-row canvas-consistency contract holds by construction.

Safety:
- Reads sprites_runtime only. Writes ONLY under --out-root, which must contain an
  "artifacts" path segment (refuses anything else). Never touches sprites_runtime.
- Deterministic: PNGs are re-encoded through a fixed PIL save path; --hash-only
  re-synthesizes in memory and emits the manifest without writing frames, so two
  runs can be compared for byte-identity.
- Skips any family the variant already has in runtime by default (e.g. the
  C-PHASE 198 pilot) and records the skip in the manifest. Review-sheet rebuilds
  can pass --include-existing-runtime-families to copy those existing frames into
  the candidate artifact tree so already-authored rows stay visible for review.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

from audit_sprite_contract import (  # noqa: E402
    EXPECTED_AGES,
    EXPECTED_COLORS,
    EXPECTED_GENDERS,
    EXPECTED_SPECIES,
    OPTIONAL_EXPANDED_ANIMATIONS,
)

# family -> (source_family, [(source_frame_index, lift_px), ...])
TRANSFORMS: dict[str, tuple[str, list[tuple[int, int]]]] = {
    "play_ball": ("happy", [(0, 0), (1, 1), (2, 2), (3, 1), (2, 0), (1, 1)]),
    "hold_ball": ("idle", [(0, 0), (1, 1), (2, 1), (3, 0)]),
    "pickup_ball": ("eat", [(0, 0), (1, 0), (2, 0), (3, 0)]),
    "drop_ball": ("eat", [(3, 0), (2, 0), (1, 0), (0, 0)]),
    "carry_ball_walk": ("walk", [(0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0)]),
    "carry_ball_run": ("walk", [(0, 0), (1, 1), (2, 2), (3, 1), (4, 0), (5, 2)]),
}

BALL_FAMILIES = list(TRANSFORMS)


def transparent_top_margin(image: Image.Image) -> int:
    alpha = image.getchannel("A")
    width, height = image.size
    for y in range(height):
        row = alpha.crop((0, y, width, y + 1))
        if row.getbbox() is not None:
            return y
    return height


def encode_png(image: Image.Image) -> bytes:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=False)
    return buffer.getvalue()


def synthesize_family(
    variant_dir: Path, family: str, include_existing_runtime_families: bool
) -> tuple[str, list[tuple[str, bytes]]] | str:
    """Returns (status, [(filename, png_bytes)]) or a string skip/error reason."""
    source_family, plan = TRANSFORMS[family]
    expected = OPTIONAL_EXPANDED_ANIMATIONS[family]
    if len(plan) != expected:
        raise AssertionError(f"{family} plan length != contract frame count")

    existing = sorted(variant_dir.glob(f"{family}_*.png"))
    if existing and include_existing_runtime_families:
        if len(existing) != expected:
            return f"error_existing_runtime_family_count:{family}:{len(existing)}"
        return (
            "copied_existing_runtime_family",
            [(p.name, p.read_bytes()) for p in existing],
        )
    if existing:
        return "skip_existing_runtime_family"

    sources = sorted(variant_dir.glob(f"{source_family}_*.png"))
    if not sources:
        return f"error_missing_source_row:{source_family}"

    frames = [Image.open(p).convert("RGBA") for p in sources]
    canvas_w = max(f.width for f in frames)
    canvas_h = max(f.height for f in frames)

    out: list[tuple[str, bytes]] = []
    for out_index, (src_index, lift) in enumerate(plan):
        if src_index >= len(frames):
            return f"error_source_frame_missing:{source_family}_{src_index:02d}"
        src = frames[src_index]
        safe_lift = min(lift, transparent_top_margin(src))
        canvas = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
        # bottom-centered like normalize_runtime_row_canvas, then lifted
        x = (canvas_w - src.width) // 2
        y = canvas_h - src.height - safe_lift
        canvas.paste(src, (x, max(y, 0)), src)
        out.append((f"{family}_{out_index:02d}.png", encode_png(canvas)))
    return ("generated", out)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runtime-root", default=str(ROOT / "sprites_runtime"))
    parser.add_argument("--out-root", required=True)
    parser.add_argument("--species", nargs="*", default=None)
    parser.add_argument("--families", nargs="*", default=None)
    parser.add_argument(
        "--hash-only",
        action="store_true",
        help="synthesize in memory and write only the manifest (determinism check)",
    )
    parser.add_argument("--manifest-name", default="candidate-manifest.json")
    parser.add_argument(
        "--include-existing-runtime-families",
        action="store_true",
        help=(
            "copy existing runtime optional-family frames into the artifact tree "
            "instead of showing them as missing in review sheets"
        ),
    )
    args = parser.parse_args()

    out_root = Path(args.out_root).resolve()
    if "artifacts" not in out_root.parts:
        raise SystemExit(f"refusing out-root outside an artifacts path: {out_root}")
    runtime_root = Path(args.runtime_root)

    species_filter = set(args.species or EXPECTED_SPECIES)
    family_filter = set(args.families or BALL_FAMILIES)
    unknown = family_filter - set(BALL_FAMILIES)
    if unknown:
        raise SystemExit(f"unknown families: {sorted(unknown)}")

    manifest: dict = {"tool": "generate_optional_ball_families", "species": {}}
    generated = copied_existing = skipped = errors = 0

    for species in EXPECTED_SPECIES:
        if species not in species_filter:
            continue
        species_entry: dict = {"variants": {}}
        for age in EXPECTED_AGES:
            for gender in EXPECTED_GENDERS:
                for color in EXPECTED_COLORS:
                    variant = f"{age}/{gender}/{color}"
                    variant_dir = runtime_root / species / age / gender / color
                    variant_entry: dict = {}
                    for family in BALL_FAMILIES:
                        if family not in family_filter:
                            continue
                        result = synthesize_family(
                            variant_dir,
                            family,
                            args.include_existing_runtime_families,
                        )
                        if isinstance(result, str):
                            variant_entry[family] = {"status": result}
                            if result.startswith("error"):
                                errors += 1
                            else:
                                skipped += 1
                            continue
                        status, frames = result
                        frames_entry = {}
                        for name, blob in frames:
                            digest = hashlib.sha256(blob).hexdigest()
                            frames_entry[name] = digest
                            if not args.hash_only:
                                dest = out_root / species / age / gender / color
                                dest.mkdir(parents=True, exist_ok=True)
                                (dest / name).write_bytes(blob)
                            if status == "copied_existing_runtime_family":
                                copied_existing += 1
                            else:
                                generated += 1
                        variant_entry[family] = {
                            "status": status,
                            "sha256": frames_entry,
                        }
                    species_entry["variants"][variant] = variant_entry
        manifest["species"][species] = species_entry

    manifest["summary"] = {
        "frames_generated": generated,
        "frames_copied_existing_runtime_family": copied_existing,
        "families_skipped_existing": skipped,
        "errors": errors,
    }
    out_root.mkdir(parents=True, exist_ok=True)
    manifest_path = out_root / args.manifest_name
    manifest_path.write_text(
        json.dumps(manifest, indent=1, sort_keys=True) + "\n", encoding="utf-8"
    )
    print(json.dumps(manifest["summary"]))
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
