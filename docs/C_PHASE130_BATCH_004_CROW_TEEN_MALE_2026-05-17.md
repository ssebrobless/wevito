# C-PHASE 130.004 - Crow Teen Male Direct Frame Repair

## Goal

Drain the fourth C-PHASE 130 repair queue row, `crow_teen_male`, using deterministic direct-frame repair only.

## Scope

- Species: `crow`
- Life stage: `teen`
- Gender: `male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Animation families repaired: `drink`, `groom`
- Runtime files added: 48 PNG frames

## Implemented

- Ran the guarded sprite repair batch runner against `crow_teen_male`.
- Added direct `drink_00..03` frames seeded from the existing `eat_00..03` frames.
- Added direct `groom_00..03` frames seeded from the existing `bathe_00..03` frames.
- Marked `crow_teen_male` as `done` in the C-PHASE 128 repair queue.
- Updated queue totals to `56` queued rows and `4` completed rows.
- Removed temporary generated candidate workspace after the apply completed.

## Safety Boundaries

- No hosted AI calls.
- No network usage.
- No local model usage.
- No source board edits.
- No broad sprite normalization.
- No changes outside the `crow / teen / male` direct runtime frame additions and queue/report artifacts.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/004-crow-teen-male/sprite_repair_batch_summary.json`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/004-crow-teen-male/contact-sheets/crow-teen-male-focused.png`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/004-crow-teen-male/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/004-crow-teen-male/sprite_contract.json`
- Filtered cockpit sweep: `vnext/artifacts/c-phase-130-batches/004-crow-teen-male/cockpit-sweep/visual_qa_manifest.json`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\004-crow-teen-male\sprite_contract.json`
  - Passed with `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\004-crow-teen-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\004-crow-teen-male\runtime_canvas.md --fail-on-mismatch`
  - Passed with `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\004-crow-teen-male\cockpit-sweep -Species crow -Age teen -Gender male`
  - Passed and wrote 6 filtered rows.

## Residual

This batch removes missing-frame fallback behavior for the row. It remains deterministic reuse of existing crow action poses, not bespoke new animation art.

