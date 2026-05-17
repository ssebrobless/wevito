# C-PHASE 130.024 - Frog Adult Male Runtime Repair

## Goal

Drain the C-PHASE 128 queue row `frog_adult_male` through the guarded C-PHASE 130 sprite repair batch process.

## Scope

- Species: `frog`
- Age: `adult`
- Gender: `male`
- Queue row: `frog_adult_male`
- Batch artifact root: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male`

## Result

- Batch runner succeeded.
- Issues processed: `54`
- Runtime mutation: yes, limited to the guarded row repair output.
- Network used: no.
- Hosted AI used: no.
- Local model used: no.
- Queue status after this batch: `done=24`, `queued=36`.
- Frog queue rows are complete.
- Next queued row: `goose_baby_female`.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male/runtime_canvas.md`
- Matrix sweep manifest: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/024-frog-adult-male/contact-sheets/`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\024-frog-adult-male\sprite_contract.json`
  - Passed: `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\024-frog-adult-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\024-frog-adult-male\runtime_canvas.md --fail-on-mismatch`
  - Passed: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\024-frog-adult-male\cockpit-sweep -Species frog -Age adult -Gender male`
  - Passed: wrote `6` matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\024-frog-adult-male\contact-sheets`
  - Passed: wrote sheets for `10` species.

## Safety Notes

The batch preserved the C-PHASE 130 safety posture: no hosted AI, no network calls, no local model calls, and no unguarded asset-prep path. This completes the frog species pass in the current queue while leaving the next species rows untouched.
