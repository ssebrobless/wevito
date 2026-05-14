# Raccoon Adult Male Runtime Consistency - 2026-05-14

## Goal

Replace the visibly broken raccoon adult male runtime rows with a clean, in-game-safe raccoon body.

## Scope

- Changed only `sprites_runtime/raccoon/adult/male/<color>/*.png`.
- Covered all six colors: `red`, `orange`, `yellow`, `blue`, `indigo`, `violet`.
- Replaced 180 runtime frames.
- Did not touch source boards, authored verified sprites, prop anchors, manifests, or generated/import folders.
- Did not generate new art.

## Method

The adult male raccoon runtime rows were a visible species/style outlier: tall, skinny, upright, and inconsistent with the cleaner adult raccoon rows. The safest deterministic runtime cleanup was to use the existing adult female raccoon runtime rows as the adult male fallback body. This keeps the species readable and avoids using the damaged raccoon handoff sheets that contain checkerboard residue and broken runtime references.

## Evidence

- Repair report: `vnext/artifacts/raccoon-adult-male-runtime-consistency-20260514/raccoon-adult-male-runtime-consistency.md`
- Hash report: `vnext/artifacts/raccoon-adult-male-runtime-consistency-20260514/raccoon-adult-male-runtime-consistency.json`
- Preview: `vnext/artifacts/raccoon-adult-male-runtime-consistency-20260514/runtime-previews/raccoon-preview.png`
- Visual-quality audit: `vnext/artifacts/raccoon-adult-male-runtime-consistency-20260514/post-audit/sprite-visual-quality.md`
- Runtime canvas audit: `vnext/artifacts/raccoon-adult-male-runtime-consistency-20260514/runtime-canvas.md`

## Validation

- `python .\tools\report_sprite_visual_quality.py --output-root .\vnext\artifacts\raccoon-adult-male-runtime-consistency-20260514\post-audit`
  - `findings=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\raccoon-adult-male-runtime-consistency-20260514\runtime-canvas.json --markdown .\vnext\artifacts\raccoon-adult-male-runtime-consistency-20260514\runtime-canvas.md --fail-on-mismatch`
  - `mismatch_count=0`
  - `missing_count=0`
  - `invalid_count=0`

## Remaining Visual Note

This is a cleanup fallback, not final hand-authored sex dimorphism. Adult male and adult female raccoon now share the cleaner adult raccoon body. A later high-quality source-generation pass can restore a distinct male variant once clean transparent source art exists.
