# C-PHASE 130.032 - Pigeon Baby Male Runtime Repair

## Goal
Repair the queued `pigeon_baby_male` runtime sprite row through the guarded C-PHASE 130 sprite repair batch process.

## Scope
- Row: `pigeon_baby_male`
- Runtime scope: `sprites_runtime/pigeon/baby/male`
- Batch artifact root: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male`
- Queue source: `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.json`

## Repair Result
- Batch runner completed 24 repair issue(s).
- `succeeded=true`
- `didUseNetwork=false`
- `didUseHostedAi=false`
- `didUseLocalModel=false`
- `didMutate=true`

## Evidence
- Batch summary: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/032-pigeon-baby-male/contact-sheets/`

## Proof Results
- Sprite contract: `error_count=0`
- Runtime canvas: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- Matrix sweep: 6 matrix rows written
- Contact sheets: exported for all 10 species

## Queue Update
- `pigeon_baby_male` marked `done`
- Completed by: `C-PHASE 130.032`
- Queue state after update: 32 done / 28 queued
- Next queued row: `pigeon_teen_female`

## Safety Boundaries
- No hosted AI calls were made.
- No network use was reported by the batch runner.
- No local model use was reported by the batch runner.
- Runtime mutation stayed scoped to `sprites_runtime/pigeon/baby/male`.
- Transient candidate/proof execution residue was cleaned before final validation.

## Validation
- Focused sprite/runtime tests: passed, 18/18
- `git diff --check`: passed
- `dotnet build .\vnext\Wevito.VNext.sln`: passed
- Full tests: passed, 966/966
- `tools/build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed
