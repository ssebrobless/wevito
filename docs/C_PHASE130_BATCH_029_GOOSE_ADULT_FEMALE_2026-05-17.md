# C-PHASE 130.029 - Goose Adult Female Runtime Sprite Repair

## Goal

Repair the queued runtime sprite row `goose_adult_female` through the guarded C-PHASE 130 batch pipeline.

## Scope

- Species: goose
- Age: adult
- Gender: female
- Colors: red, orange, yellow, blue, indigo, violet
- Runtime root: `sprites_runtime/goose/adult/female`
- Queue row: `goose_adult_female`
- Artifact root: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female`

## Batch Result

- Batch summary: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female/sprite_repair_batch_summary.json`
- Issues processed: 60
- Result: succeeded
- Mutated runtime PNGs: yes, scoped to `sprites_runtime/goose/adult/female`
- Network used: false
- Hosted AI used: false
- Local model used: false

## Proof Artifacts

- Sprite contract: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/029-goose-adult-female/contact-sheets`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\029-goose-adult-female\sprite_contract.json`
  - Passed: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\029-goose-adult-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\029-goose-adult-female\runtime_canvas.md --fail-on-mismatch`
  - Passed: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\029-goose-adult-female\cockpit-sweep -Species goose -Age adult -Gender female`
  - Passed: wrote 6 matrix rows
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\029-goose-adult-female\contact-sheets`
  - Passed: wrote 10 species contact sheets

## Queue State

- Marked `goose_adult_female` done.
- Queue counts after update: `done=29`, `queued=31`, `awaiting_review=0`, `paused=0`.
- Next queued row: `goose_adult_male`.

## Notes

The batch runner timed out at the shell command boundary but continued in the background and completed successfully. The summary confirms the batch did not use network, hosted AI, or a local model.
