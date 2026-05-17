#!/usr/bin/env python3
"""Pad runtime sprite rows that are visibly hard-cropped.

The repair is deliberately conservative:
- it only adds transparent canvas space;
- it never repaints, scales, warps, mirrors, or invents animal pixels;
- it ignores bottom-only edge contact because that often represents ground contact;
- it writes backup copies and before/after hashes for every changed PNG.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image


FRAME_RE = re.compile(r"^(?P<animation>.+)_(?P<frame>\d+)\.png$", re.IGNORECASE)


@dataclass(frozen=True)
class RepairRecord:
    path: str
    backup_path: str
    before_sha256: str
    after_sha256: str
    before_size: tuple[int, int]
    after_size: tuple[int, int]
    pad_left: int
    pad_top: int
    pad_right: int
    pad_bottom: int


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def frame_edges(path: Path) -> set[str]:
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        alpha = rgba.getchannel("A")
        bbox = alpha.getbbox()
        if bbox is None:
            return set()

        left, top, right, bottom = bbox
        width, height = rgba.size
        edges: set[str] = set()
        if left <= 0:
            edges.add("left")
        if top <= 0:
            edges.add("top")
        if right >= width:
            edges.add("right")
        if bottom >= height:
            edges.add("bottom")
        return edges


def iter_rows(root: Path):
    rows: dict[tuple[str, str, str, str, str], list[Path]] = {}
    for path in sorted(root.glob("*/*/*/*/*.png")):
        match = FRAME_RE.match(path.name)
        if match is None:
            continue
        species, age, gender, color = path.relative_to(root).parts[:4]
        rows.setdefault((species, age, gender, color, match.group("animation")), []).append(path)

    for key, paths in sorted(rows.items()):
        yield key, sorted(paths, key=lambda item: item.name)


def should_repair_row(paths: list[Path]) -> tuple[bool, set[str]]:
    all_edges: set[str] = set()
    for path in paths:
        all_edges.update(frame_edges(path))

    repair_edges = all_edges.intersection({"left", "right", "top"})
    return bool(repair_edges), repair_edges


def pad_frame(path: Path, backup_root: Path, runtime_root: Path, repair_edges: set[str], dry_run: bool) -> RepairRecord:
    rel = path.relative_to(runtime_root)
    backup_path = backup_root / rel
    before_hash = sha256(path)

    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        width, height = rgba.size
        pad_left = 8 if repair_edges.intersection({"left", "right"}) else 0
        pad_right = pad_left
        pad_top = 8 if "top" in repair_edges else 0
        pad_bottom = 0
        new_width = width + pad_left + pad_right
        new_height = height + pad_top + pad_bottom

        if not dry_run:
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, backup_path)
            canvas = Image.new("RGBA", (new_width, new_height), (0, 0, 0, 0))
            canvas.alpha_composite(rgba, (pad_left, pad_top))
            canvas.save(path)

    after_hash = before_hash if dry_run else sha256(path)
    return RepairRecord(
        path=str(rel).replace("\\", "/"),
        backup_path=str(backup_path),
        before_sha256=before_hash,
        after_sha256=after_hash,
        before_size=(width, height),
        after_size=(new_width, new_height),
        pad_left=pad_left,
        pad_top=pad_top,
        pad_right=pad_right,
        pad_bottom=pad_bottom,
    )


def padded_image(path: Path, repair_edges: set[str]) -> tuple[Image.Image, dict[str, int | tuple[int, int]]]:
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        width, height = rgba.size
        pad_left = 8 if repair_edges.intersection({"left", "right"}) else 0
        pad_right = pad_left
        pad_top = 8 if "top" in repair_edges else 0
        pad_bottom = 0
        canvas = Image.new("RGBA", (width + pad_left + pad_right, height + pad_top + pad_bottom), (0, 0, 0, 0))
        canvas.alpha_composite(rgba, (pad_left, pad_top))
        return canvas, {
            "before_size": (width, height),
            "after_size": canvas.size,
            "pad_left": pad_left,
            "pad_top": pad_top,
            "pad_right": pad_right,
            "pad_bottom": pad_bottom,
        }


def run_candidate_mode(args: argparse.Namespace) -> int:
    species = args.species[0] if isinstance(args.species, list) and args.species else args.species
    required = {
        "--repo-root": args.repo_root,
        "--species": species,
        "--age": args.age,
        "--gender": args.gender,
        "--color": args.color,
        "--animation": args.animation,
        "--out-dir": args.out_dir,
    }
    missing = [name for name, value in required.items() if not value]
    if missing:
        raise SystemExit(f"candidate mode missing required arguments: {', '.join(missing)}")

    repo_root = Path(args.repo_root).resolve()
    runtime_root = Path(args.runtime_root)
    if not runtime_root.is_absolute():
        runtime_root = repo_root / runtime_root
    runtime_root = runtime_root.resolve()
    source_row = runtime_root / species / args.age / args.gender / args.color
    out_dir = Path(args.out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    paths = sorted(source_row.glob(f"{args.animation}_*.png"))
    if not paths:
        raise SystemExit(f"no runtime frames found for {species}/{args.age}/{args.gender}/{args.color}/{args.animation}")

    should_repair, repair_edges = should_repair_row(paths)

    records = []
    for path in paths:
        output_path = out_dir / path.name
        before_hash = sha256(path)
        if should_repair:
            canvas, geometry = padded_image(path, repair_edges)
            canvas.save(output_path)
        else:
            shutil.copy2(path, output_path)
            with Image.open(path) as image:
                geometry = {
                    "before_size": image.size,
                    "after_size": image.size,
                    "pad_left": 0,
                    "pad_top": 0,
                    "pad_right": 0,
                    "pad_bottom": 0,
                }
        records.append(
            {
                "source_path": str(path),
                "candidate_path": str(output_path),
                "before_sha256": before_hash,
                "candidate_sha256": sha256(output_path),
                **geometry,
            }
        )

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "mode": "candidate",
        "row_id": args.row_id,
        "target": {
            "species": species,
            "age": args.age,
            "gender": args.gender,
            "color": args.color,
            "animation": args.animation,
        },
        "repair_edges": sorted(repair_edges),
        "candidate_mode_note": "bottom-only or no edge contact copies frames unchanged" if not should_repair else "",
        "frame_count": len(records),
        "records": records,
    }
    summary_path = out_dir / "candidate-summary.json"
    summary_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    print(f"exported {len(records)} padded candidate frame(s) to {out_dir}")
    return 0


def write_markdown(path: Path, payload: dict) -> None:
    records = payload["records"]
    lines = [
        "# Runtime Sprite Canvas Padding Repair",
        "",
        f"- generated_at: `{payload['generated_at_utc']}`",
        f"- dry_run: `{payload['dry_run']}`",
        f"- runtime_root: `{payload['runtime_root']}`",
        f"- backup_root: `{payload['backup_root']}`",
        f"- rows_considered: `{payload['summary']['rows_considered']}`",
        f"- rows_repaired: `{payload['summary']['rows_repaired']}`",
        f"- frames_repaired: `{payload['summary']['frames_repaired']}`",
        "",
        "## Scope",
        "",
        "This repair only adds transparent canvas padding to rows with top/left/right edge contact.",
        "Bottom-only edge contact is intentionally ignored because it is usually ground contact.",
        "",
        "## Repaired Rows",
        "",
    ]
    for row in payload["repaired_rows"][:120]:
        lines.append(
            f"- `{row['species']} / {row['age']} / {row['gender']} / {row['color']} / {row['animation']}` edges={','.join(row['edges'])} frames={row['frame_count']}"
        )
    if len(payload["repaired_rows"]) > 120:
        lines.append(f"- ... {len(payload['repaired_rows']) - 120} additional rows in JSON")

    lines.extend(["", "## First Frame Records", ""])
    for record in records[:80]:
        lines.append(
            f"- `{record['path']}` {record['before_size']} -> {record['after_size']} sha `{record['before_sha256'][:12]}` -> `{record['after_sha256'][:12]}`"
        )

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime-root", default="sprites_runtime")
    parser.add_argument("--output-root")
    parser.add_argument("--species", nargs="*", default=None, help="Optional species allowlist.")
    parser.add_argument("--apply", action="store_true", help="Actually write repairs. Omit for dry-run.")
    parser.add_argument("--repo-root", help="Repo root for single-row candidate mode.")
    parser.add_argument("--row-id", help="Queue row id for single-row candidate mode.")
    parser.add_argument("--age", help="Age stage for single-row candidate mode.")
    parser.add_argument("--gender", help="Gender for single-row candidate mode.")
    parser.add_argument("--color", help="Color variant for single-row candidate mode.")
    parser.add_argument("--animation", help="Animation family for single-row candidate mode.")
    parser.add_argument("--out-dir", help="Candidate output folder for single-row candidate mode.")
    args = parser.parse_args()

    if args.out_dir:
        return run_candidate_mode(args)

    if not args.output_root:
        parser.error("the following arguments are required: --output-root")

    runtime_root = Path(args.runtime_root).resolve()
    output_root = Path(args.output_root)
    output_root.mkdir(parents=True, exist_ok=True)
    backup_root = output_root / "backup-before-padding"
    species_filter = {item.lower() for item in args.species} if args.species else None
    dry_run = not args.apply

    rows_considered = 0
    repaired_rows: list[dict] = []
    records: list[RepairRecord] = []
    for key, paths in iter_rows(runtime_root):
        species, age, gender, color, animation = key
        if species_filter is not None and species.lower() not in species_filter:
            continue

        rows_considered += 1
        should_repair, repair_edges = should_repair_row(paths)
        if not should_repair:
            continue

        repaired_rows.append(
            {
                "species": species,
                "age": age,
                "gender": gender,
                "color": color,
                "animation": animation,
                "edges": sorted(repair_edges),
                "frame_count": len(paths),
            }
        )
        for path in paths:
            records.append(pad_frame(path, backup_root, runtime_root, repair_edges, dry_run))

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "dry_run": dry_run,
        "runtime_root": str(runtime_root),
        "backup_root": str(backup_root),
        "summary": {
            "rows_considered": rows_considered,
            "rows_repaired": len(repaired_rows),
            "frames_repaired": len(records),
        },
        "repaired_rows": repaired_rows,
        "records": [asdict(record) for record in records],
    }

    suffix = "dry-run" if dry_run else "applied"
    json_path = output_root / f"canvas-padding-{suffix}.json"
    md_path = output_root / f"canvas-padding-{suffix}.md"
    json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_markdown(md_path, payload)
    print(json.dumps(payload["summary"], indent=2))
    print(md_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
