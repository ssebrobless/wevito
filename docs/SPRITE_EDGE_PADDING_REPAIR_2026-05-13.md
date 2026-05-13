# Sprite Edge Padding Repair

## Goal

Fix the visible hard-crop problems called out in the running app without repainting or fabricating animal art.

This pass focuses on the rows where the audit detected top/left/right alpha touching the PNG canvas edge. Bottom-only edge contact was intentionally left alone because it often represents normal ground contact and padding it would make pets float above the floor.

## Applied Repair Scope

Runtime PNGs only:

- `sprites_runtime/deer/**`
- `sprites_runtime/fox/**`
- `sprites_runtime/frog/**`
- `sprites_runtime/pigeon/**`
- `sprites_runtime/snake/**`

Repair method:

- add 8 px transparent padding on left/right when a row has side-edge contact;
- add 8 px transparent padding on top when a row has top-edge contact;
- add 0 px bottom padding;
- apply the same padding to every frame in the affected row so animation timing and row consistency stay stable;
- never repaint, scale, warp, mirror, or synthesize pixels.

## Results

Dry run:

- rows considered: `1440`
- rows selected: `390`
- frames selected: `1440`

Applied:

- rows repaired: `390`
- frames repaired: `1440`

Post-audit impact:

- Snake high-severity edge findings: `48 -> 0`
- Pigeon high-severity edge findings: `162 -> 0`
- Fox high-severity edge findings: `12 -> 0`
- Deer high-severity edge findings: `42 -> 0`
- Frog high-severity edge findings: `126 -> 0`

Remaining findings are mostly bottom-only edge contact, low-motion rows, and the previously known goose optional mixed-geometry rows.

## Evidence

Artifact folder:

`vnext/artifacts/sprite-repair-20260513-edge-padding/`

Key files:

- `canvas-padding-dry-run.md`
- `canvas-padding-applied.md`
- `canvas-padding-applied.json`
- `post-audit/sprite-visual-quality.md`
- `runtime-canvas-post.md`
- `sprite-contract-post.json`

Backup folder created locally during apply:

`vnext/artifacts/sprite-repair-20260513-edge-padding/backup-before-padding/`

The backup folder contains pre-repair PNG copies and is intentionally not treated as new runtime art.

## Still Not Fixed By This Pass

- Squirrel still needs real animation repair/regeneration. Its audit issue is low motion, not severe crop.
- Crow needs manual silhouette review for the flattened-head report because the detector mostly sees bottom contact.
- Goose baby female blue optional ball rows still have mixed geometry and need a separate optional-row normalization/apply pass.
- Some bottom-contact findings remain by design.

## Validation

- `python .\tools\repair_runtime_sprite_canvas_padding.py --output-root .\vnext\artifacts\sprite-repair-20260513-edge-padding --species snake pigeon fox deer frog --runtime-root .\sprites_runtime`
- `python .\tools\repair_runtime_sprite_canvas_padding.py --output-root .\vnext\artifacts\sprite-repair-20260513-edge-padding --species snake pigeon fox deer frog --runtime-root .\sprites_runtime --apply`
- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\sprite-repair-20260513-edge-padding\post-audit`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\sprite-repair-20260513-edge-padding\runtime-canvas-post.json --markdown .\vnext\artifacts\sprite-repair-20260513-edge-padding\runtime-canvas-post.md`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\sprite-repair-20260513-edge-padding\sprite-contract-post.json`
