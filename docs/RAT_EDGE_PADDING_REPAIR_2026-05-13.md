# Rat Edge Padding Repair - 2026-05-13

## Goal

Fix rat frames where the visible body touches the left canvas edge, which can create a cropped or border-hugging look in-game.

## Scope

Runtime PNGs only:

- `sprites_runtime/rat/adult/female/*/bathe_*.png`
- `sprites_runtime/rat/adult/female/*/happy_*.png`
- `sprites_runtime/rat/adult/female/*/walk_*.png`
- `sprites_runtime/rat/adult/male/*/bathe_*.png`
- `sprites_runtime/rat/adult/male/*/happy_*.png`
- `sprites_runtime/rat/adult/male/*/walk_*.png`

Colors covered:

- `blue`
- `indigo`
- `orange`
- `red`
- `violet`
- `yellow`

No source boards, prop anchors, manifests, or non-rat runtime files were changed.

## Method

Used `tools/repair_runtime_sprite_canvas_padding.py` to add transparent left/right canvas padding to each affected row.

This does not repaint, scale, warp, mirror, or synthesize animal pixels. It only gives the existing rat silhouette enough transparent canvas margin so the body no longer hugs the left edge.

## Results

Dry run:

- rows considered: `288`
- rows selected: `36`
- frames selected: `168`

Applied:

- rows repaired: `36`
- frames repaired: `168`

Post-audit impact for rat:

- `edge_touch`: cleared from the ground-aware actionable list
- remaining rat findings: `36` `low_motion_row`

## Evidence

Artifact folder:

- `vnext/artifacts/rat-edge-padding-repair-20260513/`

Key files:

- `canvas-padding-dry-run.md`
- `canvas-padding-applied.md`
- `canvas-padding-applied.json`
- `post-audit/sprite-visual-quality.md`
- `runtime-canvas-post.md`
- `sprite-contract-post.json`
- `runtime-preview/rat-preview.png`

Backup folder:

- `vnext/artifacts/rat-edge-padding-repair-20260513/backup-before-padding/`

## Validation

- `python .\tools\repair_runtime_sprite_canvas_padding.py --output-root .\vnext\artifacts\rat-edge-padding-repair-20260513 --species rat --runtime-root .\sprites_runtime`
- `python .\tools\repair_runtime_sprite_canvas_padding.py --output-root .\vnext\artifacts\rat-edge-padding-repair-20260513 --species rat --runtime-root .\sprites_runtime --apply`
- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\rat-edge-padding-repair-20260513\post-audit`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\rat-edge-padding-repair-20260513\runtime-canvas-post.json --markdown .\vnext\artifacts\rat-edge-padding-repair-20260513\runtime-canvas-post.md --fail-on-mismatch`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\rat-edge-padding-repair-20260513\sprite-contract-post.json`

## Remaining Rat Work

The adult rat edge-crop issue is fixed. The remaining rat issue is low idle motion, which needs an animation/art pass rather than transparent padding.
