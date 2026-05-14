# Low Motion Life Pass - 2026-05-13

## Goal

Reduce frozen-looking idle/happy rows without inventing new animal art.

The previous ground-aware audit had `119` remaining findings, all `low_motion_row`.

## Scope

Runtime PNGs only for rows already flagged by the audit:

- `frog` baby/female `happy` rows across all six colors
- `raccoon` flagged `idle` rows across affected age/gender/color folders
- `rat` flagged `idle` rows across all age/gender/color folders
- `snake` baby/male and teen/male `idle` rows across all six colors
- `squirrel` flagged `idle` rows across all age/gender/color folders

No source boards, manifests, prop anchors, item art, or non-flagged animation rows were changed.

## Method

Added `tools/repair_low_motion_rows.py`.

The tool only shifts existing visible pixels upward inside the current transparent canvas:

- `idle`: `0, -1, -2, -1` px breathing lift
- `happy`: `0, -2, -4, -2` px bounce

It does not repaint, scale, warp, mirror, or generate animal pixels.

## Results

Dry run:

- rows selected: `119`
- frames selected for change: `357`

Applied:

- rows repaired: `119`
- frames repaired: `357`

Post-audit:

- total actionable findings: `119 -> 0`
- `low_motion_row`: `119 -> 0`
- mixed canvas: `0`
- missing frames: `0`
- invalid PNGs: `0`

## Evidence

Artifact folder:

- `vnext/artifacts/low-motion-life-pass-20260513/`

Key files:

- `low-motion-dry-run.md`
- `low-motion-applied.md`
- `low-motion-applied.json`
- `post-audit/sprite-visual-quality.md`
- `runtime-canvas-post.md`
- `sprite-contract-post.json`
- `runtime-preview/`

Backup folder:

- `vnext/artifacts/low-motion-life-pass-20260513/backup-before-low-motion-repair/`

## Validation

- `python .\tools\repair_low_motion_rows.py --output-root .\vnext\artifacts\low-motion-life-pass-20260513 --species frog raccoon rat squirrel snake`
- `python .\tools\repair_low_motion_rows.py --output-root .\vnext\artifacts\low-motion-life-pass-20260513 --species frog raccoon rat squirrel snake --apply`
- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\low-motion-life-pass-20260513\post-audit`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\low-motion-life-pass-20260513\runtime-canvas-post.json --markdown .\vnext\artifacts\low-motion-life-pass-20260513\runtime-canvas-post.md --fail-on-mismatch`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\low-motion-life-pass-20260513\sprite-contract-post.json`

## Remaining Artistic Risk

This pass makes low-motion rows less frozen, but it is not a full source-art repair.

Still-needs-human/art-candidate review:

- Squirrel still needs real locomotion/action animation, not just idle breathing.
- Snake still needs a proper slither-extension pass from stronger source art.
- Frog has broad style/pose inconsistency in older runtime rows outside the low-motion happy target.
- The runtime still uses older blue color previews in some QA sheets; that is separate from this motion pass.
