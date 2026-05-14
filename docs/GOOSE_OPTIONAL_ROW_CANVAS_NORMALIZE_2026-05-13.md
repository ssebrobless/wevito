# Goose Optional Row Canvas Normalize - 2026-05-13

## Goal

Fix mixed frame geometry in the accepted goose baby/female/blue optional ball rows so playback does not jitter or jump because one frame has a different PNG canvas size than the others.

## Scope

Runtime PNGs only:

- `sprites_runtime/goose/baby/female/blue/play_ball_*.png`
- `sprites_runtime/goose/baby/female/blue/pickup_ball_*.png`
- `sprites_runtime/goose/baby/female/blue/drop_ball_*.png`

No source boards, prop anchors, ball item art, manifests, or other colors/species were changed.

## Method

Added `tools/normalize_runtime_row_canvas.py`.

The tool expands selected rows to the maximum width/height already present in that row and bottom-centers the existing pixels.

It does not repaint, scale, warp, mirror, or synthesize animal pixels.

## Results

Dry run:

- rows selected: `3`
- frames selected for change: `11`

Applied:

- `goose/baby/female/blue/play_ball`: normalized to `102x125`
- `goose/baby/female/blue/pickup_ball`: normalized to `102x125`
- `goose/baby/female/blue/drop_ball`: normalized to `102x125`
- changed frames: `11`

Post-audit impact:

- goose baby/female/blue mixed-geometry findings: `3 -> 0`
- whole-runtime actionable findings: `122 -> 119`

## Evidence

Artifact folder:

- `vnext/artifacts/goose-optional-row-canvas-normalize-20260513/`

Key files:

- `row-canvas-dry-run.md`
- `row-canvas-applied.md`
- `row-canvas-applied.json`
- `post-audit/sprite-visual-quality.md`
- `runtime-canvas-post.md`
- `sprite-contract-post.json`
- `runtime-preview/goose-preview.png`

Backup folder:

- `vnext/artifacts/goose-optional-row-canvas-normalize-20260513/backup-before-normalize/`

## Validation

- `python .\tools\normalize_runtime_row_canvas.py --output-root .\vnext\artifacts\goose-optional-row-canvas-normalize-20260513 --species goose --age baby --gender female --color blue --animations play_ball pickup_ball drop_ball`
- `python .\tools\normalize_runtime_row_canvas.py --output-root .\vnext\artifacts\goose-optional-row-canvas-normalize-20260513 --species goose --age baby --gender female --color blue --animations play_ball pickup_ball drop_ball --apply`
- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\goose-optional-row-canvas-normalize-20260513\post-audit`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\goose-optional-row-canvas-normalize-20260513\runtime-canvas-post.json --markdown .\vnext\artifacts\goose-optional-row-canvas-normalize-20260513\runtime-canvas-post.md --fail-on-mismatch`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\goose-optional-row-canvas-normalize-20260513\sprite-contract-post.json`

## Remaining Work

This fixes canvas consistency only. It does not judge or improve the artistic quality of the goose optional ball pose itself.
