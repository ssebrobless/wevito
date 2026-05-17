# C-PHASE 130.001 - Crow Baby Female Sprite Repair Batch

## Goal

Run the first C-PHASE 128 repair queue row through the C-PHASE 129 guarded repair pipeline.

## Scope

- Queue row: `crow_baby_female`
- Species / age / gender: `crow / baby / female`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Animation families repaired: `drink`, `groom`
- Runtime PNGs added: 48 direct frames
- Runtime PNGs replaced: 0

## Implemented

- Added single-issue candidate mode to `tools/generate_runtime_pose_sprites.py`.
- Added `--sprite-repair-batch` mode to `Wevito.VNext.AutomationRunner`.
- Added filtered matrix sweep support to `tools/run-matrix-sweep.ps1`.
- Added a regression test proving sprite workflow backup/staging folders stay outside `sprites_runtime`.
- Ran the real `crow_baby_female` queue row through `SpriteRepairBatchRunner`.
- Updated `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.json` so `crow_baby_female` is `done`.

## Repair Strategy

This batch resolves missing direct `drink` and `groom` runtime frames without inventing new art:

- `drink_00..03` candidates were seeded from the same row/color's existing `eat_00..03` frames.
- `groom_00..03` candidates were seeded from the same row/color's existing `bathe_00..03` frames.
- The candidate generator wrote no runtime files directly; all runtime mutation went through dry-run, apply, post-proof, and evidence-packet paths.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/001-crow-baby-female/sprite_repair_batch_summary.json`
- Before contact sheet: `vnext/artifacts/c-phase-130-batches/001-crow-baby-female/before-contact-sheets/crow.png`
- After contact sheet: `vnext/artifacts/c-phase-130-batches/001-crow-baby-female/after-contact-sheets/crow.png`
- Family cockpit sweep: `vnext/artifacts/c-phase-130-batches/001-crow-baby-female/cockpit-sweep/visual_qa_manifest.json`
- Structural audit: `vnext/artifacts/c-phase-130-batches/001-crow-baby-female/sprite_contract.json`
- Canvas audit: `vnext/artifacts/c-phase-130-batches/001-crow-baby-female/runtime_canvas.json`

## Safety Boundaries

- No hosted AI calls.
- No network calls.
- No local model calls.
- No source board edits.
- No cross-species edits.
- No edits outside `crow / baby / female` runtime row.
- Backups and staging were moved out of `sprites_runtime` after the first validation caught that generated workflow folders polluted runtime coverage tests.

## Validation

- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\001-crow-baby-female\sprite_contract.json` - passed, `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\001-crow-baby-female\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\001-crow-baby-female\runtime_canvas.md --fail-on-mismatch` - passed, `mismatch_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\001-crow-baby-female\cockpit-sweep -Species crow -Age baby -Gender female` - passed, 6 matrix rows, 0 heuristic tags.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRuntimeCoverage|SpriteAssetServiceFrameSelection|SpriteRepairBatchRunner" --no-build` - passed, 16/16 after backup/staging path correction.
- `dotnet build .\vnext\Wevito.VNext.sln` - passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` - passed, 965/965.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` - passed.

## Stop Gates

- More than one species touched: false.
- More than one age stage touched: false.
- Runtime coverage failed after final correction: false.
- SpriteAssetService frame selection failed: false.
- Post-proof rolled back: false.
- No-op apply: false; all 48 touched files were new direct runtime frames.

## Residual Notes

This is a deterministic direct-frame repair, not a bespoke animation redraw. It removes fallback-only `drink`/`groom` behavior for the first queue row and proves the guarded batch pipeline. Later C-PHASE 130 batches can use the same path, but rows involving crop/background/body-quality defects may need stronger source-specific repair tools before they are safe to apply.
