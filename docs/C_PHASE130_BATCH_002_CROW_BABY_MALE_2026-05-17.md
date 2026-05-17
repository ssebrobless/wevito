# C-PHASE 130.002 - Crow Baby Male Direct Frame Repair

## Goal

Drain the second C-PHASE 130 repair queue row, `crow_baby_male`, without introducing generated art or expanding into harder source-quality repairs.

## Scope

- Species: `crow`
- Life stage: `baby`
- Gender: `male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Animation families repaired: `drink`, `groom`
- Runtime files added: 48 PNG frames

## Implemented

- Ran the guarded sprite repair batch runner against `crow_baby_male`.
- Added direct `drink_00..03` frames seeded from the existing `eat_00..03` frames.
- Added direct `groom_00..03` frames seeded from the existing `bathe_00..03` frames.
- Marked `crow_baby_male` as `done` in the C-PHASE 128 repair queue.
- Updated queue totals to `58` queued rows and `2` completed rows.
- Removed temporary generated candidate workspace after the apply completed.

## Safety Boundaries

- No hosted AI calls.
- No network usage.
- No local model usage.
- No source board edits.
- No broad sprite normalization.
- No changes outside the `crow / baby / male` direct runtime frame additions and queue/report artifacts.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/002-crow-baby-male/sprite_repair_batch_summary.json`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/002-crow-baby-male/contact-sheets/crow-baby-male-focused.png`
- Crow contact sheet: `vnext/artifacts/c-phase-130-batches/002-crow-baby-male/contact-sheets/crow.png`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/002-crow-baby-male/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/002-crow-baby-male/sprite_contract.json`
- Filtered cockpit sweep: `vnext/artifacts/c-phase-130-batches/002-crow-baby-male/cockpit-sweep/visual_qa_manifest.json`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\002-crow-baby-male\sprite_contract.json`
  - Passed with `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\002-crow-baby-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\002-crow-baby-male\runtime_canvas.md --fail-on-mismatch`
  - Passed with `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\002-crow-baby-male\cockpit-sweep -Species crow -Age baby -Gender male`
  - Passed and wrote 6 filtered rows.

## Residual

This is a deterministic direct-frame repair that removes missing-frame fallback behavior. It does not create bespoke crow drinking or grooming art. The harder rows in the queue, especially species with crop, background, and source-quality issues, still need stronger source repair or regeneration workflows before safe broad application.

