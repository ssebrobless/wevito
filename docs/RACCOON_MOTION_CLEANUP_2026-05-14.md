# Raccoon Motion Cleanup

## Goal

Clean the raccoon runtime sprite rows that still showed baked ghost bodies, crop/bracket artifacts, static duplicate rows, and mismatched action poses in the current in-game preview sheets.

## Scope

- Runtime-only edits under `sprites_runtime/raccoon/**`.
- Added `tools/repair_raccoon_motion_rows.py` for reproducible repair.
- Families repaired: `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe`.
- No source boards, prop anchors, content manifests, model calls, generation, or hidden learning/training were changed.

## Method

The repair script selects the safest local idle silhouette per species/age/gender/color row, favoring frames with canvas margin and compact alpha bounds so stray bracket/crop lines do not become the seed frame. It then rebuilds each family with small deterministic offsets inside the existing transparent runtime canvas.

This preserves the row's local identity and color while removing action-row afterimages and duplicated silhouettes. It is a conservative runtime cleanup pass, not final bespoke raccoon animation authoring.

## Evidence

- Dry run: `vnext/artifacts/raccoon-motion-cleanup-20260514/dry-run/raccoon-motion-cleanup.md`
- Apply report: `vnext/artifacts/raccoon-motion-cleanup-20260514/apply/raccoon-motion-cleanup.md`
- Post-audit: `vnext/artifacts/raccoon-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas: `vnext/artifacts/raccoon-motion-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/raccoon-motion-cleanup-20260514/runtime-previews/`

## Results

- Rows scanned: 288.
- Rows changed: 288.
- Frames changed after final apply: 1073.
- Sprite visual quality findings after final apply: 0.
- Runtime canvas mismatch/missing/invalid findings: 0.

## Residual

Raccoon is now much cleaner for runtime use, but future visual polish should still create better authored raccoon movement, especially distinct eating/sleeping/action acting rather than small silhouette offsets.
