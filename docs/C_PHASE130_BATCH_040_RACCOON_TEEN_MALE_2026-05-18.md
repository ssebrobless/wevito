# C-PHASE 130.040 - Raccoon Teen Male Runtime Repair

## Goal
Repair the queued `raccoon_teen_male` runtime row through the guarded C-PHASE 130 sprite repair batch path.

## Scope
- Row: `raccoon_teen_male`
- Mutation scope: `sprites_runtime/raccoon/teen/male`
- Batch artifact root: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male`
- Queue state after completion: 40 done / 20 queued

## Safety
- Hosted AI used: no
- Network used: no
- Local model used: no
- Runtime PNG mutation: yes, limited to the row scope above
- Asset prep: not run

## Artifacts
- Batch summary: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/040-raccoon-teen-male/contact-sheets/`

## Validation
- Batch runner: passed, 12 repair issues completed
- Sprite contract: passed, `error_count=0`
- Runtime canvas: passed, `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- Matrix sweep: passed, 6 rows written
- Contact sheets: passed, 10 species sheets exported
- Focused tests: passed, 18/18
- `git diff --check`: passed
- `dotnet build .\vnext\Wevito.VNext.sln`: passed
- Full tests: passed, 966/966
- Safe publish with `-SkipAssetPrep -SkipTests`: passed

## Next
Continue with the next queued row: `raccoon_adult_female`.
