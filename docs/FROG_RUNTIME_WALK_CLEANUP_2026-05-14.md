# Frog Runtime Walk Cleanup

## Goal

Replace visually broken or mismatched frog walk rows with a clean deterministic hop cycle that is safe for the current runtime build.

## Scope

- Changed only `sprites_runtime/frog/**/walk_00..05`.
- Added `tools/repair_frog_runtime_walk_rows.py`.
- Did not mutate source boards, prop anchors, non-frog sprites, or non-walk frog families.
- Did not generate or import new art.

## Method

The first attempted restore from `sprites_authored_verified/frog/**/walk_00..05` produced better motion but exposed source-frame style and damage issues. The final repair therefore derives each walk row from that same row's existing clean idle frames and applies a small squash/stretch hop cycle while preserving the existing runtime canvas and bottom-center anchor.

This is a safe cleanup, not the final hand-authored frog animation pass. It prioritizes no broken silhouettes, no palette drift, no holes, no edge clipping, and stable in-game readability.

## Evidence

- Repair report: `vnext/artifacts/frog-runtime-walk-cleanup-20260514/frog-runtime-walk-cleanup.md`
- JSON report: `vnext/artifacts/frog-runtime-walk-cleanup-20260514/frog-runtime-walk-cleanup.json`
- Visual audit: `vnext/artifacts/frog-runtime-walk-cleanup-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas audit: `vnext/artifacts/frog-runtime-walk-cleanup-20260514/runtime-canvas.md`
- Preview sheet: `vnext/artifacts/frog-runtime-walk-cleanup-20260514/runtime-previews/frog-preview.png`

## Validation

- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\frog-runtime-walk-cleanup-20260514\post-audit`
- `python .\tools\render_runtime_sprite_previews.py --output-root .\vnext\artifacts\frog-runtime-walk-cleanup-20260514\runtime-previews`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\frog-runtime-walk-cleanup-20260514\runtime-canvas.json --markdown .\vnext\artifacts\frog-runtime-walk-cleanup-20260514\runtime-canvas.md --fail-on-mismatch`

Results:

- Visual-quality findings: `0`
- Mixed-canvas rows: `0`
- Missing rows: `0`
- Invalid PNG rows: `0`

## Remaining Art Note

The frog walk is now clean and stable, but still conservative. A later true art pass can replace it with a more expressive hand-authored leap, as long as it preserves row identity, canvas contracts, and in-game readability.
