# C-PHASE 130.047 - Rat Adult Female Runtime Repair

## Goal

Repair the queued `rat_adult_female` runtime sprite row without generation, hosted AI, local model use, or broad asset mutation.

## Scope

- Queue row: `rat_adult_female`
- Runtime scope: `sprites_runtime/rat/adult/female`
- Families repaired: queued adult female rat runtime families
- Colors repaired: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Repair issues completed: `60`

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/047-rat-adult-female/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/047-rat-adult-female/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/047-rat-adult-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/047-rat-adult-female/runtime_canvas.md`
- Cockpit sweep: `vnext/artifacts/c-phase-130-batches/047-rat-adult-female/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/047-rat-adult-female/contact-sheets/`

## Safety

- `didUseNetwork=false`
- `didUseHostedAi=false`
- `didUseLocalModel=false`
- `didMutate=true`
- Mutation was limited to the target runtime row.
- No source boards, prop anchors, generated assets, or content manifests were edited.

## Validation

- Sprite contract: `error_count=0`
- Runtime canvas: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- Cockpit sweep: `6` matrix rows written
- Runtime contact sheets: `10` species sheets exported
- Focused tests: `18/18` passed
- `git diff --check`: passed
- `dotnet build .\vnext\Wevito.VNext.sln`: passed
- Full tests: `966/966` passed
- Safe publish with `-SkipAssetPrep -SkipTests`: passed

## Queue State

- Done: `47`
- Queued: `13`
- Next queued row: `rat_adult_male`
