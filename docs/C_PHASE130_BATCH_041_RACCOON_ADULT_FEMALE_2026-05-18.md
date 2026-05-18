# C-PHASE 130.041 - Raccoon Adult Female Runtime Repair

## Goal
Repair the queued `raccoon_adult_female` runtime row through the guarded C-PHASE 130 sprite repair batch path.

## Scope
- Row: `raccoon_adult_female`
- Mutation scope: `sprites_runtime/raccoon/adult/female`
- Batch artifact root: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female`
- Queue state after completion: 41 done / 19 queued

## Safety
- Hosted AI used: no
- Network used: no
- Local model used: no
- Runtime PNG mutation: yes, limited to the row scope above
- Asset prep: not run

## Artifacts
- Batch summary: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/041-raccoon-adult-female/contact-sheets/`

## Validation
- Batch runner: passed, 24 repair issues completed
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
Continue with the next queued row: `raccoon_adult_male`.
