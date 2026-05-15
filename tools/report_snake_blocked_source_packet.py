#!/usr/bin/env python3
"""Create an evidence packet for snake rows that are still source-blocked.

This tool is intentionally report-only. It does not synthesize, repair, or
mutate runtime sprites. It collects the current runtime frames and the available
source boards for each blocked snake row so the next art pass has exact targets.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
RUNTIME_ROOT = ROOT / "sprites_runtime" / "snake"
GENERAL_SOURCE_ROOT = ROOT / "incoming_sprites" / "gemini_handoff" / "snake"
MOTION_SOURCE_ROOT = ROOT / "incoming_sprites" / "gemini_handoff_motion" / "snake"
DEFAULT_OUTPUT = ROOT / "vnext" / "artifacts" / "snake-blocked-source-packet-20260514"

FRAME_COUNTS = {
    "idle": 4,
    "walk": 6,
    "eat": 4,
    "sleep": 2,
    "happy": 4,
    "sad": 2,
    "sick": 4,
    "bathe": 4,
}

BLOCKED_ROWS = [
    {
        "age": "baby",
        "gender": "female",
        "family": "sad",
        "reason": "sad_01 source extracts as a tail fragment",
        "source_need": "new complete sad row for baby female snake; both frames must show the same full body",
    },
    {
        "age": "baby",
        "gender": "female",
        "family": "sick",
        "reason": "sick_02 source extracts as a partial body fragment",
        "source_need": "new complete sick row for baby female snake; four full-body low-profile frames",
    },
    {
        "age": "teen",
        "gender": "female",
        "family": "happy",
        "reason": "happy_00 source extracts as a partial body fragment",
        "source_need": "new complete happy row for teen female snake; consistent full body and no cropped head/tail",
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "idle",
        "reason": "editable-board idle row contains grid fragments and a boxed partial source frame",
        "source_need": "review current runtime first; if replacing, use a clean low-profile idle row rather than upright coil poses",
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "walk",
        "reason": "editable-board walk row contains partial body fragments",
        "source_need": "review current runtime first; if replacing, use a clean slither row with body extension, not coil bobbing",
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "eat",
        "reason": "eat_02 source extracts as a partial body fragment",
        "source_need": "new complete eat row for adult female snake or explicit approval to use the coiled care-board source",
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "happy",
        "reason": "happy_03 source extracts as a partial body fragment",
        "source_need": "new complete happy row for adult female snake; no body fragments or copied unrelated pose",
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "sick",
        "reason": "sick_00 source extracts as a partial body fragment",
        "source_need": "new complete sick row for adult female snake; four low/resting frames with consistent scale",
    },
    {
        "age": "adult",
        "gender": "female",
        "family": "bathe",
        "reason": "bathe_01 source extracts as a partial body fragment",
        "source_need": "new complete bathe row for adult female snake; no detached water/noise chunks",
    },
    {
        "age": "adult",
        "gender": "male",
        "family": "walk",
        "reason": "editable-board walk row contains partial body fragments",
        "source_need": "review current runtime first; if replacing, use a clean slither row with body extension, not coil bobbing",
    },
    {
        "age": "adult",
        "gender": "male",
        "family": "sick",
        "reason": "sick_01 and sick_02 source extracts are line/body fragments",
        "source_need": "new complete sick row for adult male snake; four full-body frames",
    },
]


@dataclass(frozen=True)
class RuntimeFrame:
    frame_id: str
    path: str
    sha256: str | None
    exists: bool


@dataclass(frozen=True)
class SourceReference:
    kind: str
    path: str
    exists: bool
    note: str


@dataclass(frozen=True)
class BlockedRowRecord:
    age: str
    gender: str
    family: str
    reason: str
    source_need: str
    runtime_frames: list[RuntimeFrame]
    source_references: list[SourceReference]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def rel(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def font(size: int = 14) -> ImageFont.ImageFont:
    for candidate in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            continue
    return ImageFont.load_default()


def checker(size: tuple[int, int], cell: int = 8) -> Image.Image:
    image = Image.new("RGBA", size, (245, 245, 245, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(225, 225, 225, 255))
    return image


def runtime_frames(age: str, gender: str, family_name: str) -> list[RuntimeFrame]:
    frames: list[RuntimeFrame] = []
    for index in range(FRAME_COUNTS[family_name]):
        frame_id = f"{family_name}_{index:02d}"
        path = RUNTIME_ROOT / age / gender / "blue" / f"{frame_id}.png"
        frames.append(
            RuntimeFrame(
                frame_id=frame_id,
                path=rel(path),
                sha256=sha256(path) if path.exists() else None,
                exists=path.exists(),
            )
        )
    return frames


def first_existing(paths: Iterable[Path]) -> Path | None:
    for path in paths:
        if path.exists():
            return path
    return None


def source_references(age: str, gender: str, family_name: str) -> list[SourceReference]:
    refs: list[SourceReference] = []
    general_dir = GENERAL_SOURCE_ROOT / age / gender / "5-save-edited-board-here"
    general = first_existing(sorted(general_dir.glob("*gemini-result*.png")) if general_dir.exists() else [])
    if general is not None:
        refs.append(
            SourceReference(
                "general_editable_board",
                rel(general),
                True,
                "source board used by the previous guarded extraction pass; row is blocked because one or more frames are fragments",
            )
        )

    motion_family = "locomotion" if family_name in {"idle", "walk"} else "care" if family_name in {"eat", "sleep"} else None
    if motion_family:
        folder = MOTION_SOURCE_ROOT / age / gender / motion_family
        board = folder / f"{motion_family}-editable-board.png"
        gemini_dir = folder / "5-save-edited-board-here"
        gemini = first_existing(sorted(gemini_dir.glob("*.png")) if gemini_dir.exists() else [])
        refs.append(
            SourceReference(
                f"{motion_family}_editable_board",
                rel(board),
                board.exists(),
                "alternate dedicated motion/care board; useful only if visual style and pose intent match the target row",
            )
        )
        if gemini is not None:
            refs.append(
                SourceReference(
                    f"{motion_family}_gemini_packet",
                    rel(gemini),
                    True,
                    "full Gemini handoff packet containing reference and editable sections",
                )
            )
    return refs


def collect() -> list[BlockedRowRecord]:
    records: list[BlockedRowRecord] = []
    for item in BLOCKED_ROWS:
        records.append(
            BlockedRowRecord(
                age=item["age"],
                gender=item["gender"],
                family=item["family"],
                reason=item["reason"],
                source_need=item["source_need"],
                runtime_frames=runtime_frames(item["age"], item["gender"], item["family"]),
                source_references=source_references(item["age"], item["gender"], item["family"]),
            )
        )
    return records


def paste_runtime_strip(sheet: Image.Image, draw: ImageDraw.ImageDraw, record: BlockedRowRecord, x: int, y: int) -> None:
    label_font = font(11)
    tile_w, tile_h = 70, 58
    for i, frame in enumerate(record.runtime_frames):
        tile = checker((tile_w, tile_h), 7)
        path = ROOT / frame.path
        if path.exists():
            sprite = Image.open(path).convert("RGBA")
            sprite.thumbnail((tile_w - 8, tile_h - 18), Image.Resampling.NEAREST)
            tile.alpha_composite(sprite, ((tile_w - sprite.width) // 2, 4 + (tile_h - 20 - sprite.height) // 2))
        else:
            ImageDraw.Draw(tile).text((8, 16), "MISSING", fill=(150, 20, 20, 255), font=label_font)
        sheet.alpha_composite(tile, (x + i * tile_w, y))
        draw.text((x + i * tile_w + 3, y + tile_h - 14), frame.frame_id, fill=(20, 28, 35), font=label_font)


def paste_source_thumbs(sheet: Image.Image, draw: ImageDraw.ImageDraw, record: BlockedRowRecord, x: int, y: int) -> None:
    label_font = font(11)
    thumb_w, thumb_h = 126, 58
    for i, ref in enumerate(record.source_references[:3]):
        tile = checker((thumb_w, thumb_h), 7)
        path = ROOT / ref.path
        if path.exists():
            image = Image.open(path).convert("RGBA")
            image.thumbnail((thumb_w - 8, thumb_h - 18), Image.Resampling.NEAREST)
            tile.alpha_composite(image, ((thumb_w - image.width) // 2, 4 + (thumb_h - 20 - image.height) // 2))
        else:
            ImageDraw.Draw(tile).text((8, 18), "NO SOURCE", fill=(150, 20, 20, 255), font=label_font)
        sheet.alpha_composite(tile, (x + i * thumb_w, y))
        draw.text((x + i * thumb_w + 3, y + thumb_h - 14), ref.kind[:18], fill=(20, 28, 35), font=label_font)


def write_contact_sheet(records: list[BlockedRowRecord], output: Path) -> None:
    title_font = font(16)
    body_font = font(12)
    width = 1180
    row_h = 96
    height = 48 + row_h * len(records)
    sheet = Image.new("RGBA", (width, height), (236, 241, 245, 255))
    draw = ImageDraw.Draw(sheet)
    draw.rectangle((0, 0, width, 38), fill=(31, 40, 50, 255))
    draw.text((12, 9), "snake blocked-row source packet - current runtime plus available source boards", fill=(245, 248, 252), font=title_font)

    y = 44
    for index, record in enumerate(records):
        fill = (224, 232, 238, 255) if index % 2 else (214, 224, 232, 255)
        draw.rectangle((0, y - 4, width, y + row_h - 8), fill=fill)
        draw.text((10, y + 4), f"{record.age}/{record.gender}/{record.family}", fill=(15, 24, 32), font=body_font)
        draw.text((10, y + 22), record.reason[:60], fill=(110, 44, 32), font=body_font)
        draw.text((10, y + 42), record.source_need[:64], fill=(35, 63, 86), font=body_font)
        paste_runtime_strip(sheet, draw, record, 360, y + 4)
        paste_source_thumbs(sheet, draw, record, 810, y + 4)
        y += row_h

    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(output)


def write_markdown(records: list[BlockedRowRecord], output: Path, json_name: str, sheet_name: str) -> None:
    lines = [
        "# Snake Blocked Row Source Packet",
        "",
        f"- generated_at_utc: `{datetime.now(timezone.utc).isoformat()}`",
        "- policy: report-only; no PNG mutation, no source-board mutation, no procedural drawing",
        f"- rows needing source decision: `{len(records)}`",
        f"- json: `{json_name}`",
        f"- contact_sheet: `{sheet_name}`",
        "",
        "## Decision Summary",
        "",
        "The current runtime is structurally valid, but these rows remain risky for visual perfection because the available source row contains partial bodies, fragments, or a pose family that does not match the intended snake motion. The safe next move is targeted source creation/review for these exact rows, then a normal backup/hash/rollback apply pass.",
        "",
        "## Blocked Rows",
        "",
    ]
    for record in records:
        lines.extend(
            [
                f"### `{record.age} / {record.gender} / {record.family}`",
                "",
                f"- blocker: {record.reason}",
                f"- needed source: {record.source_need}",
                "- current blue runtime frames:",
            ]
        )
        for frame in record.runtime_frames:
            status = "exists" if frame.exists else "missing"
            digest = frame.sha256[:12] if frame.sha256 else "n/a"
            lines.append(f"  - `{frame.frame_id}` `{status}` `{digest}` `{frame.path}`")
        lines.append("- available source references:")
        for ref in record.source_references:
            status = "exists" if ref.exists else "missing"
            lines.append(f"  - `{ref.kind}` `{status}` `{ref.path}` - {ref.note}")
        lines.append("")

    lines.extend(
        [
            "## Recommended Next Apply Rules",
            "",
            "- Do not use the alternate coiled locomotion/care boards to overwrite low-profile slither rows unless reviewed and explicitly approved.",
            "- Do not generate local procedural snake art as a substitute for missing source rows.",
            "- For each newly accepted row, apply the whole row across all six colors only after source frames are complete and style-consistent.",
            "- Keep the normal apply gate: dry-run scope, backup hashes, post-proof, rollback drill, then re-apply.",
            "",
        ]
    )
    output.write_text("\n".join(lines), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    output_root = args.output_root
    output_root.mkdir(parents=True, exist_ok=True)
    records = collect()

    json_path = output_root / "snake-blocked-source-packet.json"
    md_path = output_root / "snake-blocked-source-packet.md"
    sheet_path = output_root / "snake-blocked-source-contact-sheet.png"

    payload = {
        "generated_at_utc": datetime.now(timezone.utc).isoformat(),
        "mutation_policy": "report_only",
        "blocked_row_count": len(records),
        "records": [asdict(record) for record in records],
    }
    json_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    write_contact_sheet(records, sheet_path)
    write_markdown(records, md_path, json_path.name, sheet_path.name)
    print(f"Wrote {json_path}")
    print(f"Wrote {md_path}")
    print(f"Wrote {sheet_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
