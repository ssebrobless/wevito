# Pigeon, Crow, And Fox Action Consistency Repair - 2026-05-13

## Goal

Remove obvious action-row visual breakage for pigeon, crow, and fox without inventing new art or depending on external AI.

## Scope

- Species: `pigeon`, `crow`, `fox`
- Families: `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe`
- Ages/genders/colors: all runtime variants present under each target species

## Implemented

- Added `tools/repair_action_rows_from_idle.py`.
- Ran a dry-run report, then applied the repair with backups.
- Each repaired action frame is reseeded from the same row's own `idle_*` frame and receives only tiny whole-sprite vertical offsets.
- This removes broken/cropped/transformed action poses while preserving each pet's current runtime identity, palette, canvas safety, and row count.

## Evidence

- Dry-run report: `vnext/artifacts/pigeon-crow-fox-action-consistency-20260513/dry-run/action-row-idle-reseed.md`
- Apply report: `vnext/artifacts/pigeon-crow-fox-action-consistency-20260513/apply/action-row-idle-reseed.md`
- Backup root: `vnext/artifacts/pigeon-crow-fox-action-consistency-20260513/apply/backup-before-apply/`
- Runtime preview sheets: `vnext/artifacts/pigeon-crow-fox-action-consistency-20260513/runtime-previews/`
- Visual quality audit: `vnext/artifacts/pigeon-crow-fox-action-consistency-20260513/post-audit/sprite-visual-quality.md`
- Canvas audit: `vnext/artifacts/pigeon-crow-fox-action-consistency-20260513/runtime-canvas.md`

## Results

- Rows scanned: `648`
- Rows changed: `648`
- Frames changed: `2160`
- Post-repair visual-quality findings: `0`
- Post-repair mixed-canvas rows: `0`

## Tradeoff

This is a conservative rescue pass, not final authored action animation. The repaired rows are now clean and consistent, but action-specific behaviors are subtle. A later visual-authoring pass can replace these rows with richer generated/action-specific animation using the normal candidate/proof/rollback pipeline.
