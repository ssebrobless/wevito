# C-PHASE 130.009 - Deer Teen Female Runtime Repair

## Goal

Repair the next queued C-PHASE 128 sprite row: `deer_teen_female`.

## Scope

- Target row: `deer / teen / female`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Queue movement: `52 queued / 8 done` to `51 queued / 9 done`
- Mutated runtime scope: `sprites_runtime/deer/teen/female/**`

## Implemented

- Completed 30 repair issue(s) for `deer_teen_female`.
- Added direct missing runtime frames where the row previously relied on fallback behavior.
- Processed crop-detected families through guarded candidate generation, dry-run apply, backup, apply, and post-proof.
- Preserved the C-PHASE 130 no-hosted-AI/no-asset-prep boundaries.

## Evidence

- Batch root: `vnext/artifacts/c-phase-130-batches/009-deer-teen-female`
- Batch summary: `vnext/artifacts/c-phase-130-batches/009-deer-teen-female/sprite_repair_batch_summary.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/009-deer-teen-female/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/009-deer-teen-female/sprite_contract.json`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/009-deer-teen-female/cockpit-sweep/visual_qa_manifest.md`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/009-deer-teen-female/contact-sheets/deer-teen-female-focused.png`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\009-deer-teen-female\sprite_contract.json`
  - Result: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\009-deer-teen-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\009-deer-teen-female\runtime_canvas.md --fail-on-mismatch`
  - Result: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\009-deer-teen-female\cockpit-sweep -Species deer -Age teen -Gender female`
  - Result: 6 matrix rows

## Safety Boundaries

- No hosted AI calls.
- No asset prep.
- No source board edits.
- No prop anchor edits.
- No content manifest edits.
- No unrelated species/age/gender rows edited.

## Next Row

Next queued row after this batch is `deer_teen_male`.
