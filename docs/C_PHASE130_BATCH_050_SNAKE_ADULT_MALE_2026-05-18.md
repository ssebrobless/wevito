# C-PHASE 130.050 - Snake Adult Male Runtime Sprite Repair

## Goal

Repair the queued `snake_adult_male` runtime sprite row from the C-PHASE 128 sprite repair queue without using network, hosted AI, local models, asset prep, or broad sprite mutation.

## Scope

- Row: `snake_adult_male`
- Species: `snake`
- Age: `adult`
- Gender: `male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Families touched by the batch runner: `bathe`, `drink`, `eat`, `groom`, `happy`, `idle`, `play`, `sad`, `sleep`, `walk`
- Runtime mutation scope: `sprites_runtime/snake/adult/male/**`

## Evidence

- Batch artifact root: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male`
- Batch summary: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male/sprite_repair_batch_summary.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male/sprite_contract.json`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/050-snake-adult-male/contact-sheets`

## Safety Boundaries

- `didUseNetwork=false`
- `didUseHostedAi=false`
- `didUseLocalModel=false`
- `didMutate=true`
- Mutation stayed in the intended `snake/adult/male` runtime row.
- The three preserved untracked Claude plan docs were not staged or edited.

## Validation

- `dotnet run --project .\vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj -- --sprite-repair-batch --repo-root . --queue .\vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json --row-id snake_adult_male --out .\vnext\artifacts\c-phase-130-batches\050-snake-adult-male` - passed, 60 repair issues completed.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\050-snake-adult-male\sprite_contract.json` - passed, `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\050-snake-adult-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\050-snake-adult-male\runtime_canvas.md --fail-on-mismatch` - passed, `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\050-snake-adult-male\cockpit-sweep -Species snake -Age adult -Gender male` - passed, wrote 6 matrix rows.
- `python .\tools\export_pet_runtime_contact_sheets.py --runtime-root .\sprites_runtime --output-root .\vnext\artifacts\c-phase-130-batches\050-snake-adult-male\contact-sheets` - passed, wrote 10 species sheets.

## Queue State

- Done: 50
- Queued: 10
- Completed row: `snake_adult_male`
- Next queued row: `snake_baby_female`

## Next Phase

Continue C-PHASE 130 with `snake_baby_female` on a fresh branch from current `origin/main`.
