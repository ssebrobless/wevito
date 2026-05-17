# C-PHASE 130.003 - Crow Teen Female Direct Frame Repair

## Goal

Drain the third C-PHASE 130 repair queue row, `crow_teen_female`, using the proven deterministic direct-frame path.

## Scope

- Species: `crow`
- Life stage: `teen`
- Gender: `female`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Animation families repaired: `drink`, `groom`
- Runtime files added: 48 PNG frames

## Implemented

- Ran the guarded sprite repair batch runner against `crow_teen_female`.
- Added direct `drink_00..03` frames seeded from the existing `eat_00..03` frames.
- Added direct `groom_00..03` frames seeded from the existing `bathe_00..03` frames.
- Marked `crow_teen_female` as `done` in the C-PHASE 128 repair queue.
- Updated queue totals to `57` queued rows and `3` completed rows.
- Removed temporary generated candidate workspace after the apply completed.

## Safety Boundaries

- No hosted AI calls.
- No network usage.
- No local model usage.
- No source board edits.
- No broad sprite normalization.
- No changes outside the `crow / teen / female` direct runtime frame additions and queue/report artifacts.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/003-crow-teen-female/sprite_repair_batch_summary.json`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/003-crow-teen-female/contact-sheets/crow-teen-female-focused.png`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/003-crow-teen-female/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/003-crow-teen-female/sprite_contract.json`
- Filtered cockpit sweep: `vnext/artifacts/c-phase-130-batches/003-crow-teen-female/cockpit-sweep/visual_qa_manifest.json`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\003-crow-teen-female\sprite_contract.json`
  - Passed with `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\003-crow-teen-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\003-crow-teen-female\runtime_canvas.md --fail-on-mismatch`
  - Passed with `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\003-crow-teen-female\cockpit-sweep -Species crow -Age teen -Gender female`
  - Passed and wrote 6 filtered rows.

## Residual

This continues the direct-frame fallback cleanup for crow rows. It intentionally does not invent bespoke new art, and it leaves harder visual-quality repair rows for later source-quality passes.

