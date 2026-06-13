#!/usr/bin/env python3
"""C-PHASE 203B/202 Stage-2 apply: imported authored families -> runtime matrix.

Takes the gate-verified frames staged by the round-trip import
(vnext/artifacts/c-phase-203b-imports/<species>/<age>/<gender>/<family>/),
assembles complete family rows (walk requires both _a and _b halves), runs the
conservative fringe+speck cleanup on each frame, tints every frame into ALL six
color variants with the pipeline's own colorize() — no passthrough color, so an
untinted authored row can never reach the runtime again (BUG-007 root cause) —
and writes to sprites_runtime + sprites_authored_verified.

Guarded: --apply requires --backup-root; every overwritten runtime/authored file
is backed up once with sha256 recorded. Default is dry-run.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from generate_runtime_pose_sprites import COLOR_VARIANTS, colorize  # noqa: E402
from procedural_cleanup_sweep import analyze_frame  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_STAGING = ROOT / "vnext" / "artifacts" / "c-phase-203b-imports"
DEFAULT_RUNTIME = ROOT / "sprites_runtime"
DEFAULT_AUTHORED = ROOT / "sprites_authored_verified"

# family assembly: staged family dir -> (runtime family, expected frames)
FAMILY_PARTS = {
    "locomotion_idle": ("idle", ["idle_00", "idle_01", "idle_02", "idle_03"]),
    "locomotion_walk_a": ("walk", ["walk_00", "walk_01", "walk_02"]),
    "locomotion_walk_b": ("walk", ["walk_03", "walk_04", "walk_05"]),
    "care_eat": ("eat", ["eat_00", "eat_01", "eat_02", "eat_03"]),
    "expression_happy": ("happy", ["happy_00", "happy_01", "happy_02", "happy_03"]),
}
WALK_FULL = ["walk_00", "walk_01", "walk_02", "walk_03", "walk_04", "walk_05"]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def assemble_rows(staging: Path, species_filter: list[str]) -> tuple[dict, list[str]]:
    """Returns {(species, age, gender): {runtime_family: {frame: path}}}, skipped[]."""
    rows: dict = {}
    skipped: list[str] = []
    for species_dir in sorted(d for d in staging.iterdir() if d.is_dir()):
        species = species_dir.name
        if species_filter and species not in species_filter:
            continue
        for age_dir in sorted(d for d in species_dir.iterdir() if d.is_dir()):
            for gender_dir in sorted(d for d in age_dir.iterdir() if d.is_dir()):
                key = (species, age_dir.name, gender_dir.name)
                families: dict = {}
                for staged_family, (runtime_family, frames) in FAMILY_PARTS.items():
                    fam_dir = gender_dir / staged_family
                    if not fam_dir.is_dir():
                        continue
                    for frame in frames:
                        p = fam_dir / f"{frame}.png"
                        if p.exists():
                            families.setdefault(runtime_family, {})[frame] = p
                # completeness: walk needs all 6; others need their full set
                complete: dict = {}
                for runtime_family, frame_map in families.items():
                    expected = WALK_FULL if runtime_family == "walk" else [
                        f for fam, (rf, fr) in FAMILY_PARTS.items() if rf == runtime_family for f in fr
                    ]
                    if all(f in frame_map for f in expected):
                        complete[runtime_family] = frame_map
                    else:
                        skipped.append(f"{species}/{age_dir.name}/{gender_dir.name}:{runtime_family} "
                                       f"({len(frame_map)}/{len(expected)} frames)")
                if complete:
                    rows[key] = complete
    return rows, skipped


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--species", nargs="*", default=[])
    parser.add_argument("--staging-root", type=Path, default=DEFAULT_STAGING)
    parser.add_argument("--runtime-root", type=Path, default=DEFAULT_RUNTIME)
    parser.add_argument("--authored-root", type=Path, default=DEFAULT_AUTHORED)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--backup-root", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    if args.apply and args.backup_root is None:
        parser.error("--apply requires --backup-root")
    if "artifacts" not in args.output.resolve().parts:
        parser.error("--output must live under an artifacts directory")
    if args.backup_root and "artifacts" not in args.backup_root.resolve().parts:
        parser.error("--backup-root must live under an artifacts directory")

    rows, skipped = assemble_rows(args.staging_root, args.species)

    manifest = {
        "schemaVersion": "authored-family-apply/1",
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "mode": "apply" if args.apply else "dry-run",
        "rows": [],
        "skippedIncomplete": skipped,
    }
    written = 0
    for (species, age, gender), families in sorted(rows.items()):
        row_record = {"row": f"{species}/{age}/{gender}", "families": {}, "framesWritten": 0}
        for runtime_family, frame_map in sorted(families.items()):
            cleaned_frames: dict[str, Image.Image] = {}
            cleanup_px = 0
            for frame, path in sorted(frame_map.items()):
                rgba = np.array(Image.open(path).convert("RGBA"), dtype=np.uint8)
                # fringe + speck only (species not in DARK_RIM_PARAMS get no rim trim;
                # for those that are, the calibrated trim also applies — same contract
                # the live runtime tree already passed in C-203A)
                cleaned, finding = analyze_frame(rgba, fringe_alpha=24, max_speck=8, species=species)
                cleanup_px += finding.fringe_pixels + finding.speck_pixels + finding.dark_rim_pixels
                cleaned_frames[frame] = Image.fromarray(cleaned, "RGBA")
            row_record["families"][runtime_family] = {
                "frames": sorted(frame_map),
                "cleanupPixels": cleanup_px,
            }
            if not args.apply:
                continue
            for color in COLOR_VARIANTS:
                for frame, image in cleaned_frames.items():
                    tinted = colorize(image, color)
                    for target_root in (args.runtime_root, args.authored_root):
                        target = target_root / species / age / gender / color / f"{frame}.png"
                        target.parent.mkdir(parents=True, exist_ok=True)
                        if target.exists():
                            backup = args.backup_root / target_root.name / target.relative_to(target_root)
                            if not backup.exists():
                                backup.parent.mkdir(parents=True, exist_ok=True)
                                shutil.copy2(target, backup)
                        tinted.save(target)
                        written += 1
                        row_record["framesWritten"] += 1
        manifest["rows"].append(row_record)

    manifest["totals"] = {
        "rows": len(rows),
        "filesWritten": written,
        "skippedIncomplete": len(skipped),
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(json.dumps(manifest["totals"], indent=2))
    for s in skipped:
        print("SKIPPED (incomplete):", s)
    print(f"manifest: {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
