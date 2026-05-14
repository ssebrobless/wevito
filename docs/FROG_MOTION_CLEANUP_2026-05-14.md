# Frog Motion Cleanup

## Goal

Fix the current in-game frog visual problems reported during the May 13/14 playtest: stacked or ghosted frog bodies, mismatched action poses, and rows that looked broken when displayed in the focused home stage.

## Scope

- Runtime sprite rows only: `sprites_runtime/frog/**`
- Added deterministic repair tool: `tools/repair_frog_motion_rows.py`
- No source-board edits
- No prop-anchor edits
- No content-manifest edits
- No hosted AI, browser generation, or external image tool calls

## Method

The repair tool reseeds frog rows from each row's own idle silhouettes and applies small, family-specific in-canvas offsets for runtime readability:

- `idle`: subtle breathing motion
- `walk`: small hop-like offsets that stay inside the row canvas
- `eat`, `happy`, `sick`, `bathe`: conservative pose offsets derived from the local row
- `sad`, `sleep`: left intact when the existing row already matched the rebuilt output

This is a safe runtime cleanup baseline. It removes obvious broken display artifacts, but it is not a final bespoke extended-leg frog-jump authoring pass.

## Evidence

- Dry run: `vnext/artifacts/frog-motion-cleanup-20260514/dry-run/frog-motion-cleanup.md`
- Apply report: `vnext/artifacts/frog-motion-cleanup-20260514/apply/frog-motion-cleanup.md`
- Backup before apply: `vnext/artifacts/frog-motion-cleanup-20260514/apply/backup-before-apply/`
- Visual audit: `vnext/artifacts/frog-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas report: `vnext/artifacts/frog-motion-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/frog-motion-cleanup-20260514/runtime-previews/`

## Results

- Dry run: `288` frog rows scanned, `288` rows would change, `1080` frames would change.
- Final apply after edge-touch correction: `288` rows scanned, `216` rows changed, `360` frames changed.
- Sprite visual quality audit: `0` active findings.
- Runtime canvas audit: `0` mixed-canvas rows, `0` missing/count-mismatch rows, `0` invalid or non-alpha PNG frames.

## Residual

The current frog rows are cleaner and safer to render in the game, but the frog still needs a later high-art animation pass for expressive species-specific motion, especially a more readable jump or leap with extended legs.
