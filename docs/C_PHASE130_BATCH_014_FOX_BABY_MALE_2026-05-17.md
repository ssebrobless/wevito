# C-PHASE 130.014 - Fox Baby Male Runtime Sprite Repair

## Goal
Repair the next C-PHASE 128 queued runtime row: `fox_baby_male`.

## Scope
- Row: `fox / baby / male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Queue issues processed: `12`
- Runtime frame mutation only; no hosted AI, no asset prep, no source-board edits, no prop-anchor edits, and no content manifest edits.

## Implemented
- Ran the guarded sprite repair batch runner for `fox_baby_male`.
- Processed queued runtime repairs through the existing candidate, dry-run, backup, apply, and post-proof path.
- Removed transient authored candidate and proof-manifest residue after the batch.
- Marked `fox_baby_male` complete in `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.json`.

## Evidence
- Batch summary: `vnext/artifacts/c-phase-130-batches/014-fox-baby-male/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/014-fox-baby-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/014-fox-baby-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/014-fox-baby-male/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/014-fox-baby-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/014-fox-baby-male/contact-sheets/`

## Validation
- `dotnet run --project .\vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj -- --sprite-repair-batch --repo-root . --queue .\vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json --row-id fox_baby_male --out .\vnext\artifacts\c-phase-130-batches\014-fox-baby-male` -> completed 12 repair issues.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\014-fox-baby-male\sprite_contract.json` -> `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\014-fox-baby-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\014-fox-baby-male\runtime_canvas.md --fail-on-mismatch` -> `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\014-fox-baby-male\cockpit-sweep -Species fox -Age baby -Gender male` -> 6 matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\014-fox-baby-male\contact-sheets` -> 10 species sheets.

## Queue State
- Before this batch: `47` queued, `13` done.
- After this batch: `46` queued, `14` done.
- Next queued row: `fox_teen_female`.
