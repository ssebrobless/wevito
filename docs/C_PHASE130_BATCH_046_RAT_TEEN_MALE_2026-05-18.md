# C-PHASE 130.046 - Rat Teen Male Runtime Repair

## Goal

Repair the queued `rat_teen_male` runtime sprite row without generation, hosted AI, local model use, or broad asset mutation.

## Scope

- Queue row: `rat_teen_male`
- Runtime scope: `sprites_runtime/rat/teen/male`
- Families repaired: `drink`, `groom`
- Colors repaired: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Repair issues completed: `12`

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/046-rat-teen-male/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/046-rat-teen-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/046-rat-teen-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/046-rat-teen-male/runtime_canvas.md`
- Cockpit sweep: `vnext/artifacts/c-phase-130-batches/046-rat-teen-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/046-rat-teen-male/contact-sheets/`

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

- Done: `46`
- Queued: `14`
- Next queued row: `rat_adult_female`
