# C-PHASE 130.057 - Squirrel Teen Female Runtime Repair

## Goal

Repair the queued `squirrel_teen_female` runtime sprite row using the guarded C-PHASE 130 batch path.

## Scope

- Target row: `squirrel_teen_female`
- Runtime mutation scope: `sprites_runtime/squirrel/teen/female`
- Issues completed: 12
- Artifact root: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female`
- Queue state after completion: `done=57`, `queued=3`

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female/summary.json`
- Sprite contract proof: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female/sprite_contract.json`
- Runtime canvas proof: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/057-squirrel-teen-female/contact-sheets`

## Safety Boundaries

- No hosted AI was used.
- No local model was used.
- No network access was used.
- No asset prep was run.
- Runtime mutation was limited to the intended squirrel teen female row.

## Validation

- `dotnet run --project .\vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj -- --sprite-repair-batch --repo-root . --queue .\vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json --row-id squirrel_teen_female --out .\vnext\artifacts\c-phase-130-batches\057-squirrel-teen-female` passed.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\057-squirrel-teen-female\sprite_contract.json` passed with `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\057-squirrel-teen-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\057-squirrel-teen-female\runtime_canvas.md --fail-on-mismatch` passed with `mismatch_count=0`, `missing_count=0`, and `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\057-squirrel-teen-female\cockpit-sweep -Species squirrel -Age teen -Gender female` passed and wrote 6 matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\057-squirrel-teen-female\contact-sheets` passed and wrote all 10 species sheets.

## Next Row

The next queued row is `squirrel_teen_male`.
