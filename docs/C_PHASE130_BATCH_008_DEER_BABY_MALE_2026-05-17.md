# C-PHASE 130.008 - Deer Baby Male Runtime Repair

## Goal

Repair the next queued C-PHASE 128 sprite row: `deer_baby_male`.

## Scope

- Target row: `deer / baby / male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Queue movement: `53 queued / 7 done` to `52 queued / 8 done`
- Mutated runtime scope: `sprites_runtime/deer/baby/male/**`
- Supporting code scope: padding repair candidate mode now accepts a queue `sourcePath` for fallback-backed target families.

## Implemented

- Completed 60 repair issue(s) for `deer_baby_male`.
- Added direct `drink_00..03` and `groom_00..03` frames for all six colors.
- Added direct `play_00..03` frames for all six colors from the queue-provided `happy_00` fallback source.
- Processed existing crop-detected families through guarded candidate generation, dry-run apply, backup, apply, and post-proof.
- Fixed the C-PHASE 130 repair runner/tool seam so `sourcePath` is forwarded to the padding repair tool and fallback-backed rows can become real direct target-family frames.

## Evidence

- Batch root: `vnext/artifacts/c-phase-130-batches/008-deer-baby-male`
- Batch summary: `vnext/artifacts/c-phase-130-batches/008-deer-baby-male/sprite_repair_batch_summary.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/008-deer-baby-male/runtime_canvas.md`
- Sprite contract report: `vnext/artifacts/c-phase-130-batches/008-deer-baby-male/sprite_contract.json`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/008-deer-baby-male/cockpit-sweep/visual_qa_manifest.md`
- Focused contact sheet: `vnext/artifacts/c-phase-130-batches/008-deer-baby-male/contact-sheets/deer-baby-male-focused.png`

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\008-deer-baby-male\sprite_contract.json`
  - Result: `error_count=0`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\008-deer-baby-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\008-deer-baby-male\runtime_canvas.md --fail-on-mismatch`
  - Result: `mismatch_count=0`, `missing_count=0`, `invalid_count=0`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\008-deer-baby-male\cockpit-sweep -Species deer -Age baby -Gender male`
  - Result: 6 matrix rows
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRepairBatchRunner"`
  - Result: 13/13 passed
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRuntimeCoverage|SpriteAssetServiceFrameSelection|SpriteRepairBatchRunner" --no-build`
  - Result: 18/18 passed
- `dotnet build .\vnext\Wevito.VNext.sln`
  - Result: passed
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - Result: 966/966 passed
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
  - Result: passed

## Safety Boundaries

- No hosted AI calls.
- No asset prep.
- No source board edits.
- No prop anchor edits.
- No content manifest edits.
- No unrelated species/age/gender rows edited.

## Next Row

Next queued row after this batch is `deer_teen_female`.
