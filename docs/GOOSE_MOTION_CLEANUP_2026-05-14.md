# Goose Motion Cleanup

## Goal

Fix the current in-game goose visual problems reported during playtest and visible in the runtime preview sheet: duplicated bodies, puddle/base leftovers, and action rows that looked like the goose transformed mid-animation.

## Scope

- Runtime sprite rows only: `sprites_runtime/goose/**`
- Added deterministic repair tool: `tools/repair_goose_motion_rows.py`
- No source-board edits
- No prop-anchor edits
- No content-manifest edits
- No hosted AI, browser generation, or external image tool calls

## Method

The repair tool rebuilds goose runtime rows from each row's own local idle silhouettes and applies small, safe in-canvas offsets by animation family:

- `idle`: subtle breathing motion
- `walk`: gentle foot/body shifts
- `eat`, `happy`, `sick`, `bathe`: conservative local pose offsets
- `sad`, `sleep`: rebuilt from the local idle pair to remove mismatched external artifacts

This removes obvious runtime artifacts while preserving species, age, gender, and color identity. It is intentionally not a final high-art authored goose motion pass.

## Evidence

- Dry run: `vnext/artifacts/goose-motion-cleanup-20260514/dry-run/goose-motion-cleanup.md`
- Apply report: `vnext/artifacts/goose-motion-cleanup-20260514/apply/goose-motion-cleanup.md`
- Backup before apply: `vnext/artifacts/goose-motion-cleanup-20260514/apply/backup-before-apply/`
- Visual audit: `vnext/artifacts/goose-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas report: `vnext/artifacts/goose-motion-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/goose-motion-cleanup-20260514/runtime-previews/`

## Results

- Dry run: `288` goose rows scanned, `288` rows would change, `1080` frames would change.
- Apply: `288` rows scanned, `288` rows changed, `972` frames changed.
- Sprite visual quality audit: `0` active findings.
- Runtime canvas audit: `0` mixed-canvas rows, `0` missing/count-mismatch rows, `0` invalid or non-alpha PNG frames.

## Residual

The current goose rows are readable and safer in-game, but the goose still needs a later authored animation pass for more natural bird-specific motion.
