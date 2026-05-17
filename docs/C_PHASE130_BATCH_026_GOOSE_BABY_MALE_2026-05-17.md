# C-PHASE 130.026 - Goose Baby Male Runtime Sprite Repair

## Goal

Repair the queued runtime sprite row `goose_baby_male` through the guarded C-PHASE 130 batch pipeline.

## Scope

- Species: goose
- Age: baby
- Gender: male
- Colors: red, orange, yellow, blue, indigo, violet
- Runtime root: `sprites_runtime/goose/baby/male`
- Queue row: `goose_baby_male`
- Artifact root: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male`

## Batch Result

- Batch summary: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male/sprite_repair_batch_summary.json`
- Issues processed: 60
- Result: succeeded
- Mutated runtime PNGs: yes, scoped to `sprites_runtime/goose/baby/male`
- Network used: false
- Hosted AI used: false
- Local model used: false

## Proof Artifacts

- Sprite contract: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/026-goose-baby-male/contact-sheets`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\026-goose-baby-male\sprite_contract.json`
  - Passed: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\026-goose-baby-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\026-goose-baby-male\runtime_canvas.md --fail-on-mismatch`
  - Passed: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\026-goose-baby-male\cockpit-sweep -Species goose -Age baby -Gender male`
  - Passed: wrote 6 matrix rows
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\026-goose-baby-male\contact-sheets`
  - Passed: wrote 10 species contact sheets

## Queue State

- Marked `goose_baby_male` done.
- Queue counts after update: `done=26`, `queued=34`, `awaiting_review=0`, `paused=0`.
- Next queued row: `goose_teen_female`.

## Notes

The batch runner timed out at the shell command boundary but continued in the background and completed successfully. The summary confirms the batch did not use network, hosted AI, or a local model.
