#!/usr/bin/env python3
"""Re-import every staged motion family board through the current import cleaner.

C-PHASE 203C: the C-203B Stage-2 apply was produced with the brightness-flood
cleaner (clean_cell_border_flood), which ate light/white bodies and left
enclosed belly holes + halo specks (C-199R round-2 owner rejection). The import
tool now segments by the drawn dark outline (segment_cell_by_outline); this
driver re-runs every archived board through it so the apply can be rebuilt
without any new Gemini round-trip.

No Gemini, no network: it reads the editable boards already on disk under
incoming_sprites/gemini_handoff_motion/. The sharpness gate is intentionally
skipped — these boards passed it in C-203B and only the segmentation changed.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from import_authored_family_board import import_family_board

# C-203B regen families only (split, anti-blur). Composite boards
# (locomotion/care/expression) are superseded and skipped.
SPLIT_FAMILIES = {
    "locomotion_idle",
    "locomotion_walk_a",
    "locomotion_walk_b",
    "care_eat",
    "expression_happy",
}

# snake/adult/male walk_a: 4 Gemini rolls all returned cropped/zoomed body
# segments (C-203B deferral). The board art is bad — skip so walk stays the old
# runtime row (apply needs both walk halves to assemble, so skipping _a defers
# the whole walk for this row).
DEFERRED = {("snake", "adult", "male", "locomotion_walk_a")}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--packs-root", type=Path, default=Path("incoming_sprites/gemini_handoff_motion"))
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--list-only", action="store_true")
    args = parser.parse_args()

    metas = sorted(args.packs_root.rglob("pack-metadata.json"))
    ledger: list[dict] = []
    imported = skipped = failed = 0

    for meta_path in metas:
        meta = json.loads(meta_path.read_text(encoding="utf-8"))
        family = meta["family"]
        if family not in SPLIT_FAMILIES:
            continue
        sp, age, gender = meta["species"], meta["ageStage"], meta["gender"]
        key = (sp, age, gender, family)
        if key in DEFERRED:
            ledger.append({"row": "/".join(key), "status": "deferred"})
            skipped += 1
            continue
        pack_dir = meta_path.parent
        board = pack_dir / meta["boardImage"]
        cell_size = tuple(meta.get("cellSize", (256, 256)))
        out_dir = args.output_root / sp / age / gender / family
        entry = {"row": "/".join(key), "board": str(board), "cell": list(cell_size)}
        if args.list_only:
            ledger.append({**entry, "status": "would-import"})
            continue
        if not board.exists():
            ledger.append({**entry, "status": "missing-board"})
            failed += 1
            continue
        try:
            outs = import_family_board(
                board, out_dir, sp, age, family,
                cell_size=cell_size, sharpness_reference_dir=None,
            )
            ledger.append({**entry, "status": "imported", "frames": len(outs)})
            imported += 1
        except SystemExit as exc:  # sharpness gate disabled, but be defensive
            ledger.append({**entry, "status": "error", "detail": str(exc)})
            failed += 1

    args.output_root.mkdir(parents=True, exist_ok=True)
    (args.output_root / "reimport-ledger.json").write_text(
        json.dumps(ledger, indent=2), encoding="utf-8"
    )
    print(f"imported={imported} skipped/deferred={skipped} failed={failed} of {len(ledger)} rows")
    if args.list_only:
        for row in ledger[:8]:
            print("  ", row["row"], row["status"])


if __name__ == "__main__":
    main()
