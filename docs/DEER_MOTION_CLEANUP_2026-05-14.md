# Deer Motion Cleanup

## Goal

Fix the current deer runtime rows where preview sheets showed duplicated bodies, mismatched scale, and action frames that no longer read like the same animal moving through a sequence.

## Scope

- Runtime sprite rows only: `sprites_runtime/deer/**`
- Added deterministic repair tool: `tools/repair_deer_motion_rows.py`
- No source-board edits
- No prop-anchor edits
- No content-manifest edits
- No hosted AI, browser generation, or external image tool calls

## Method

The repair tool rebuilds each deer row from its own safest local idle silhouette. It selects the idle source with the most canvas margin so antlers, ears, legs, and tails do not touch the canvas edge, then applies conservative in-canvas offsets by animation family.

This keeps male/female, age, antler, color, and size traits tied to the existing row while removing obvious broken action-artifacts.

## Evidence

- Dry run: `vnext/artifacts/deer-motion-cleanup-20260514/dry-run/deer-motion-cleanup.md`
- Apply report: `vnext/artifacts/deer-motion-cleanup-20260514/apply/deer-motion-cleanup.md`
- Backup before apply: `vnext/artifacts/deer-motion-cleanup-20260514/apply/backup-before-apply/`
- Visual audit: `vnext/artifacts/deer-motion-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas report: `vnext/artifacts/deer-motion-cleanup-20260514/runtime-canvas.md`
- Runtime previews: `vnext/artifacts/deer-motion-cleanup-20260514/runtime-previews/`

## Results

- Dry run: `288` deer rows scanned, `288` rows would change, `1080` frames would change.
- Final apply after margin-safe source selection: `288` rows scanned, `288` rows changed, `1074` frames changed.
- Sprite visual quality audit: `0` active findings.
- Runtime canvas audit: `0` mixed-canvas rows, `0` missing/count-mismatch rows, `0` invalid or non-alpha PNG frames.

## Residual

The current deer rows are readable and safe for runtime use. A later high-art authored pass should add more natural walking, grazing, and rest motion once the whole runtime set is no longer visibly broken.
