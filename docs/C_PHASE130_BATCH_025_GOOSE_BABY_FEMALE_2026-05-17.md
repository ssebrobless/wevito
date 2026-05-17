# C-PHASE 130.025 - Goose Baby Female Runtime Repair

## Goal

Drain the C-PHASE 128 queue row `goose_baby_female` through the guarded C-PHASE 130 sprite repair batch process.

## Scope

- Species: `goose`
- Age: `baby`
- Gender: `female`
- Queue row: `goose_baby_female`
- Batch artifact root: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female`

## Result

- Batch runner succeeded.
- Issues processed: `60`
- Runtime mutation: yes, limited to the guarded row repair output.
- Network used: no.
- Hosted AI used: no.
- Local model used: no.
- Queue status after this batch: `done=25`, `queued=35`.
- Next queued row: `goose_baby_male`.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female/runtime_canvas.md`
- Matrix sweep manifest: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/025-goose-baby-female/contact-sheets/`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\025-goose-baby-female\sprite_contract.json`
  - Passed: `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\025-goose-baby-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\025-goose-baby-female\runtime_canvas.md --fail-on-mismatch`
  - Passed: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\025-goose-baby-female\cockpit-sweep -Species goose -Age baby -Gender female`
  - Passed: wrote `6` matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\025-goose-baby-female\contact-sheets`
  - Passed: wrote sheets for `10` species.

## Safety Notes

The batch preserved the C-PHASE 130 safety posture: no hosted AI, no network calls, no local model calls, and no unguarded asset-prep path. This starts the goose species queue without touching later goose rows.
