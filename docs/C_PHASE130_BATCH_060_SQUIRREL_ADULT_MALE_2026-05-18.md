# C-PHASE 130.060 - Squirrel Adult Male Runtime Repair

## Goal

Repair the final queued `squirrel_adult_male` runtime sprite row using the guarded C-PHASE 130 batch path.

## Scope

- Target row: `squirrel_adult_male`
- Runtime mutation scope: `sprites_runtime/squirrel/adult/male`
- Issues completed: 12
- Artifact root: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male`
- Queue state after completion: `done=60`, `queued=0`

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male/summary.json`
- Sprite contract proof: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male/sprite_contract.json`
- Runtime canvas proof: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/060-squirrel-adult-male/contact-sheets`

## Safety Boundaries

- No hosted AI was used.
- No local model was used.
- No network access was used.
- No asset prep was run.
- Runtime mutation was limited to the intended squirrel adult male row.

## Validation

- `dotnet run --project .\vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj -- --sprite-repair-batch --repo-root . --queue .\vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json --row-id squirrel_adult_male --out .\vnext\artifacts\c-phase-130-batches\060-squirrel-adult-male` passed.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\060-squirrel-adult-male\sprite_contract.json` passed with `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\060-squirrel-adult-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\060-squirrel-adult-male\runtime_canvas.md --fail-on-mismatch` passed with `mismatch_count=0`, `missing_count=0`, and `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\060-squirrel-adult-male\cockpit-sweep -Species squirrel -Age adult -Gender male` passed and wrote 6 matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\060-squirrel-adult-male\contact-sheets` passed and wrote all 10 species sheets.

## Queue Drain

C-PHASE 130 queue is fully drained: `done=60`, `queued=0`.
