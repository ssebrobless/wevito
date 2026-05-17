# C-PHASE 130.022 - Frog Teen Male Runtime Repair

## Goal

Repair the queued `frog / teen / male` runtime sprite row across all six color variants while preserving the guarded C-PHASE 130 mutation contract.

## Scope

- Row: `frog_teen_male`
- Species: `frog`
- Age: `teen`
- Gender: `male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Queue issues processed: `54`
- Runtime mutation scope: `sprites_runtime/frog/teen/male/**`

## Implemented

- Ran the C-PHASE 130 sprite repair batch runner for `frog_teen_male`.
- Repaired missing direct action rows and cropped runtime frames listed by the queue.
- Preserved source-board, prop-anchor, content-manifest, and hosted-AI boundaries.
- Marked `frog_teen_male` complete in `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.json`.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/022-frog-teen-male/sprite_repair_batch_summary.json`
- Runtime canvas proof: `vnext/artifacts/c-phase-130-batches/022-frog-teen-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/022-frog-teen-male/runtime_canvas.md`
- Sprite contract proof: `vnext/artifacts/c-phase-130-batches/022-frog-teen-male/sprite_contract.json`
- Cockpit sweep manifest: `vnext/artifacts/c-phase-130-batches/022-frog-teen-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/022-frog-teen-male/contact-sheets/`

## Safety Boundaries

- `didUseNetwork`: `false`
- `didUseHostedAi`: `false`
- `didUseLocalModel`: `false`
- `didMutate`: `true`
- No asset prep was run.
- No source boards were edited.
- No `prop_anchors.json` edits were made.
- No content manifest edits were made.

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\022-frog-teen-male\sprite_contract.json`
  - Result: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\022-frog-teen-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\022-frog-teen-male\runtime_canvas.md --fail-on-mismatch`
  - Result: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\022-frog-teen-male\cockpit-sweep -Species frog -Age teen -Gender male`
  - Result: `6` matrix rows written.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\022-frog-teen-male\contact-sheets`
  - Result: `10` species sheets written.

## Queue State

- Before batch: `39 queued / 21 done`
- After batch: `38 queued / 22 done`
- Next queued row: `frog_adult_female`
