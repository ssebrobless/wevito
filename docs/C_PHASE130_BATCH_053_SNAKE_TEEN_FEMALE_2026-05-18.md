# C-PHASE 130.053 - Snake Teen Female Runtime Sprite Repair

## Goal

Repair the queued `snake_teen_female` runtime sprite row from the C-PHASE 128 sprite repair queue without using network, hosted AI, local models, asset prep, or broad sprite mutation.

## Scope

- Row: `snake_teen_female`
- Species: `snake`
- Age: `teen`
- Gender: `female`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Families touched by the batch runner: `bathe`, `drink`, `eat`, `groom`, `happy`, `idle`, `play`, `sad`, `sleep`, `walk`
- Runtime mutation scope: `sprites_runtime/snake/teen/female/**`

## Evidence

- Batch artifact root: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female`
- Batch summary: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female/sprite_repair_batch_summary.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female/sprite_contract.json`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/053-snake-teen-female/contact-sheets`

## Safety Boundaries

- Batch runner completed without network, hosted AI, or local model use.
- Runtime mutation stayed in the intended `snake/teen/female` row.
- The three preserved untracked Claude plan docs were not staged or edited.

## Validation

- `dotnet run --project .\vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj -- --sprite-repair-batch --repo-root . --queue .\vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json --row-id snake_teen_female --out .\vnext\artifacts\c-phase-130-batches\053-snake-teen-female` - passed, 60 repair issues completed.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\053-snake-teen-female\sprite_contract.json` - passed, `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\053-snake-teen-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\053-snake-teen-female\runtime_canvas.md --fail-on-mismatch` - passed, `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\053-snake-teen-female\cockpit-sweep -Species snake -Age teen -Gender female` - passed, wrote 6 matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\053-snake-teen-female\contact-sheets` - passed, wrote 10 species sheets.

## Queue State

- Done: 53
- Queued: 7
- Completed row: `snake_teen_female`
- Next queued row: `snake_teen_male`

## Next Phase

Continue C-PHASE 130 with `snake_teen_male` on a fresh branch from current `origin/main`.
