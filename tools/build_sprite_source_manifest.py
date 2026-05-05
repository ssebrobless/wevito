from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFilter, ImageStat


SPECIES = [
    "crow",
    "deer",
    "fox",
    "frog",
    "goose",
    "pigeon",
    "raccoon",
    "rat",
    "snake",
    "squirrel",
]
AGES = ["baby", "teen", "adult"]
GENDERS = ["female", "male"]

TOP_LEVEL_NAME_CANDIDATES = {
    ("crow", "baby"): ["crow_baby.png", "crow-baby.png", "crow baby.png"],
    ("deer", "baby"): ["deer baby.png", "deer-baby.png", "deer_baby.png"],
    ("fox", "baby"): ["fox_baby.png", "fox-baby.png", "fox baby.png"],
    ("frog", "baby"): ["frog-baby.png", "frog_baby.png", "frog baby.png"],
    ("goose", "baby"): ["goose-baby.png", "goose_baby.png", "goose baby.png"],
    ("pigeon", "baby"): ["pigeon-baby.png", "pigeon_baby.png", "pigeon baby.png"],
    ("raccoon", "baby"): ["raccoon-baby.png", "raccoon_baby.png", "raccoon baby.png"],
    ("rat", "baby"): ["rat_baby.png", "rat-baby.png", "rat baby.png"],
    ("snake", "baby"): ["snake baby.png", "snake-baby.png", "snake_baby.png"],
    ("squirrel", "baby"): ["squirrel-baby.png", "squirrel_baby.png", "squirrel baby.png"],
}


@dataclass
class ImageRecord:
    source_class: str
    species: str
    age: str
    gender: str | None
    path: str | None
    exists: bool
    width: int | None = None
    height: int | None = None
    file_bytes: int | None = None
    mode: str | None = None
    has_alpha: bool | None = None
    opaque_bbox: list[int] | None = None
    opaque_width: int | None = None
    opaque_height: int | None = None
    transparent_percent: float | None = None
    partial_alpha_percent: float | None = None
    sharpness_proxy: float | None = None
    flags: list[str] | None = None


def top_level_candidates(species: str, age: str) -> list[str]:
    if (species, age) in TOP_LEVEL_NAME_CANDIDATES:
        return TOP_LEVEL_NAME_CANDIDATES[(species, age)]
    return [
        f"{species}-{age}.png",
        f"{species}_{age}.png",
        f"{species} {age}.png",
    ]


def locate_first(root: Path, names: Iterable[str]) -> Path | None:
    for name in names:
        path = root / name
        if path.exists():
            return path
    return None


def image_record(source_class: str, species: str, age: str, gender: str | None, path: Path | None) -> ImageRecord:
    if path is None or not path.exists():
        return ImageRecord(
            source_class=source_class,
            species=species,
            age=age,
            gender=gender,
            path=str(path) if path else None,
            exists=False,
            flags=["missing"],
        )

    flags: list[str] = []
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        width, height = rgba.size
        alpha = rgba.getchannel("A")
        alpha_stat = ImageStat.Stat(alpha)
        total = width * height
        alpha_histogram = alpha.histogram()
        transparent = alpha_histogram[0]
        partial = sum(alpha_histogram[1:255])
        bbox = alpha.point(lambda value: 255 if value > 16 else 0).getbbox()

        crop = rgba.crop(bbox) if bbox else rgba
        gray = crop.convert("L")
        edges = gray.filter(ImageFilter.FIND_EDGES)
        edge_rms = float(ImageStat.Stat(edges).rms[0])

        if source_class == "handoff_base_pose" and min(width, height) < 700:
            flags.append("small_base_pose_reference")
        if source_class == "top_level_original" and (width < 2000 or height < 1000):
            flags.append("smaller_than_expected_original")
        if source_class == "handoff_base_pose" and alpha_stat.extrema[0][0] == 255:
            flags.append("no_transparency_channel_effect")
        if edge_rms < 5:
            flags.append("low_sharpness_proxy_check")

        opaque_width = None
        opaque_height = None
        if bbox:
            opaque_width = bbox[2] - bbox[0]
            opaque_height = bbox[3] - bbox[1]

        return ImageRecord(
            source_class=source_class,
            species=species,
            age=age,
            gender=gender,
            path=str(path),
            exists=True,
            width=width,
            height=height,
            file_bytes=path.stat().st_size,
            mode=image.mode,
            has_alpha="A" in image.getbands(),
            opaque_bbox=list(bbox) if bbox else None,
            opaque_width=opaque_width,
            opaque_height=opaque_height,
            transparent_percent=round(transparent / total * 100, 3),
            partial_alpha_percent=round(partial / total * 100, 3),
            sharpness_proxy=round(edge_rms, 3),
            flags=flags,
        )


def make_contact_sheet(records: list[ImageRecord], output_path: Path, title: str, columns: int) -> None:
    cell_w = 260
    cell_h = 230
    header_h = 50
    rows = (len(records) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * cell_w, header_h + rows * cell_h), (28, 34, 42))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 14), title, fill=(245, 248, 255))

    checker_light = (214, 221, 230)
    checker_dark = (174, 184, 197)

    for index, record in enumerate(records):
        col = index % columns
        row = index // columns
        x = col * cell_w
        y = header_h + row * cell_h
        tile = Image.new("RGB", (cell_w - 16, cell_h - 44), checker_light)
        tile_draw = ImageDraw.Draw(tile)
        square = 12
        for cy in range(0, tile.height, square):
            for cx in range(0, tile.width, square):
                if (cx // square + cy // square) % 2:
                    tile_draw.rectangle((cx, cy, cx + square - 1, cy + square - 1), fill=checker_dark)

        if record.exists and record.path:
            with Image.open(record.path) as image:
                rgba = image.convert("RGBA")
                rgba.thumbnail((tile.width - 16, tile.height - 16), Image.Resampling.LANCZOS)
                px = (tile.width - rgba.width) // 2
                py = (tile.height - rgba.height) // 2
                tile.paste(rgba, (px, py), rgba)
        else:
            tile_draw.text((24, 58), "MISSING", fill=(130, 20, 20))

        sheet.paste(tile, (x + 8, y + 8))
        label = f"{record.species} {record.age}"
        if record.gender:
            label += f" {record.gender}"
        if record.flags:
            label += f" [{', '.join(record.flags)}]"
        draw.text((x + 10, y + cell_h - 30), label[:44], fill=(235, 241, 250))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)


def count_motion_sources(motion_root: Path) -> dict[str, object]:
    image_files = [
        path
        for path in motion_root.rglob("*")
        if path.is_file() and path.suffix.lower() in {".png", ".jpg", ".jpeg", ".webp"}
    ]
    family_counts: dict[str, int] = {}
    for path in image_files:
        parts = [part.lower() for part in path.relative_to(motion_root).parts]
        for family in ("locomotion", "care", "expression"):
            if family in parts or any(part.startswith(family) for part in parts):
                family_counts[family] = family_counts.get(family, 0) + 1
                break
    return {
        "root": str(motion_root),
        "image_file_count": len(image_files),
        "family_image_counts": family_counts,
    }


def write_markdown(
    output_path: Path,
    top_records: list[ImageRecord],
    base_records: list[ImageRecord],
    motion_summary: dict[str, object],
    contact_sheets: dict[str, str],
) -> None:
    missing_top = [r for r in top_records if not r.exists]
    missing_base = [r for r in base_records if not r.exists]
    flagged = [r for r in top_records + base_records if r.flags]
    top_dims = sorted({(r.width, r.height) for r in top_records if r.exists})
    base_min_w = min((r.width or 0) for r in base_records if r.exists)
    base_min_h = min((r.height or 0) for r in base_records if r.exists)
    base_max_w = max((r.width or 0) for r in base_records if r.exists)
    base_max_h = max((r.height or 0) for r in base_records if r.exists)

    lines = [
        "# Wevito Phase 0 Original Source Reset",
        "",
        "This manifest is the guardrail for restarting sprite work from clean sources only.",
        "",
        "## Source Rules",
        "",
        "- Primary identity references are the top-level original generated animal PNGs in `incoming_sprites`.",
        "- Per-row cutout anchors are only `incoming_sprites/gemini_handoff/{species}/{age}/{gender}/1-base-pose.png`.",
        "- Motion/action references may use saved Gemini motion boards, but runtime/authored/variant sprites must not be used as source truth.",
        "- Blue/color variants are excluded from approval references until canonical natural-color animation is approved.",
        "- Old Sprite Workflow App requests from March are considered stale and must stay archived until a new queue is generated from this manifest.",
        "",
        "## Counts",
        "",
        f"- Top-level original generated animal references: {len([r for r in top_records if r.exists])}/30",
        f"- Per-gender base-pose anchors: {len([r for r in base_records if r.exists])}/60",
        f"- Missing top-level originals: {len(missing_top)}",
        f"- Missing base-pose anchors: {len(missing_base)}",
        f"- Automated source-screen flags: {len(flagged)}",
        f"- Motion source image files: {motion_summary['image_file_count']}",
        "",
        "## Dimensions",
        "",
        f"- Top-level original dimensions found: {top_dims}",
        f"- Base-pose anchor width range: {base_min_w}-{base_max_w}",
        f"- Base-pose anchor height range: {base_min_h}-{base_max_h}",
        "",
        "## Contact Sheets",
        "",
        f"- Top-level originals: `{contact_sheets['top_level_originals']}`",
        f"- Base-pose anchors: `{contact_sheets['base_poses']}`",
        "",
        "## Automated Flags",
        "",
    ]

    if flagged:
        for record in flagged:
            gender = record.gender if record.gender else "-"
            lines.append(
                f"- {record.source_class}: {record.species} | {record.age} | {gender} | "
                f"{', '.join(record.flags or [])} | `{record.path}`"
            )
    else:
        lines.append("- None from automated source screen.")

    lines.extend(
        [
            "",
            "## Phase 1 Entry Criteria",
            "",
            "- Generate the next repair queue only after this manifest is current.",
            "- Every queued repair must cite a `source_reference_path` from this manifest.",
            "- Every queued repair must cite a motion board when the issue is animation, pose, or sequence timing.",
            "- Repairs must preserve natural motion extents instead of shrinking poses into old square bounds.",
        ]
    )
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument("--stamp", default="latest")
    args = parser.parse_args()

    project_root = Path(args.project_root).resolve()
    incoming = project_root / "incoming_sprites"
    handoff = incoming / "gemini_handoff"
    motion = incoming / "gemini_handoff_motion"
    artifact_dir = project_root / "vnext" / "artifacts" / f"source-reset-{args.stamp}"
    artifact_dir.mkdir(parents=True, exist_ok=True)

    top_records: list[ImageRecord] = []
    for species in SPECIES:
        for age in AGES:
            path = locate_first(incoming, top_level_candidates(species, age))
            top_records.append(image_record("top_level_original", species, age, None, path))

    base_records: list[ImageRecord] = []
    for species in SPECIES:
        for age in AGES:
            for gender in GENDERS:
                path = handoff / species / age / gender / "1-base-pose.png"
                base_records.append(image_record("handoff_base_pose", species, age, gender, path))

    motion_summary = count_motion_sources(motion)
    top_sheet = artifact_dir / "top-level-original-generated-sprites.png"
    base_sheet = artifact_dir / "handoff-1-base-pose-anchors.png"
    make_contact_sheet(top_records, top_sheet, "Top-Level Original Generated Animal References", columns=5)
    make_contact_sheet(base_records, base_sheet, "Per-Row Handoff 1-Base-Pose Anchors", columns=6)

    manifest = {
        "schema_version": 1,
        "project_root": str(project_root),
        "expected_shape": {
            "species": SPECIES,
            "ages": AGES,
            "genders": GENDERS,
            "canonical_rows": len(SPECIES) * len(AGES) * len(GENDERS),
            "canonical_frames_before_color_variants": 1800,
            "runtime_frames_after_six_color_variants": 10800,
        },
        "source_policy": {
            "allowed_identity_sources": [
                "incoming_sprites/*.png original animal references",
                "incoming_sprites/gemini_handoff/{species}/{age}/{gender}/1-base-pose.png",
            ],
            "allowed_motion_sources": [
                "incoming_sprites/gemini_handoff_motion",
                "incoming_sprites/gemini_handoff/{species}/{age}/{gender}/2-editable-board.png",
            ],
            "excluded_as_source_truth": [
                "sprites_runtime",
                "sprites_authored",
                "sprites_authored_verified",
                "3-runtime-reference-blue.png",
                "optional-color-strip.png",
                ".sprite-workflow archived requests",
            ],
        },
        "top_level_originals": [asdict(record) for record in top_records],
        "handoff_base_poses": [asdict(record) for record in base_records],
        "motion_sources": motion_summary,
        "contact_sheets": {
            "top_level_originals": str(top_sheet),
            "base_poses": str(base_sheet),
        },
    }

    json_path = artifact_dir / "original-source-manifest.json"
    md_path = artifact_dir / "original-source-manifest.md"
    json_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    write_markdown(
        md_path,
        top_records,
        base_records,
        motion_summary,
        {
            "top_level_originals": str(top_sheet),
            "base_poses": str(base_sheet),
        },
    )

    print(json.dumps({"manifest": str(json_path), "report": str(md_path)}, indent=2))


if __name__ == "__main__":
    main()
