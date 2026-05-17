# C-PHASE 130.012 - Deer Adult Male Runtime Sprite Repair

## Goal

Repair the next C-PHASE 128 queued runtime row: `deer_adult_male`.

## Scope

- Row: `deer / adult / male`
- Colors: `blue`, `indigo`, `orange`, `red`, `violet`, `yellow`
- Queue issues processed: `30`
- Runtime frame mutation only; no hosted AI, no asset prep, no source-board edits, no prop-anchor edits, and no content manifest edits.

## Implemented

- Ran the guarded sprite repair batch runner for `deer_adult_male`.
- Added missing direct runtime frames for `drink` and `groom` across all six colors.
- Processed queued crop/padding repairs for `eat`, `sad`, and `sleep` through the existing candidate, dry-run, backup, apply, and post-proof path.
- Removed transient authored candidate and proof-manifest residue after the batch.
- Marked `deer_adult_male` complete in `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.json`.

## Evidence

- Batch summary: `vnext/artifacts/c-phase-130-batches/012-deer-adult-male/sprite_repair_batch_summary.json`
- Sprite contract: `vnext/artifacts/c-phase-130-batches/012-deer-adult-male/sprite_contract.json`
- Runtime canvas report: `vnext/artifacts/c-phase-130-batches/012-deer-adult-male/runtime_canvas.json`
- Runtime canvas markdown: `vnext/artifacts/c-phase-130-batches/012-deer-adult-male/runtime_canvas.md`
- Matrix sweep: `vnext/artifacts/c-phase-130-batches/012-deer-adult-male/cockpit-sweep/visual_qa_manifest.json`
- Contact sheets: `vnext/artifacts/c-phase-130-batches/012-deer-adult-male/contact-sheets/`

## Validation

- `dotnet run --project .\vnext\src\Wevito.VNext.AutomationRunner\Wevito.VNext.AutomationRunner.csproj -- --sprite-repair-batch --repo-root . --queue .\vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json --row-id deer_adult_male --out .\vnext\artifacts\c-phase-130-batches\012-deer-adult-male` -> completed 30 repair issues.
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-130-batches\012-deer-adult-male\sprite_contract.json` -> `error_count=0`.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-130-batches\012-deer-adult-male\runtime_canvas.json --markdown .\vnext\artifacts\c-phase-130-batches\012-deer-adult-male\runtime_canvas.md --fail-on-mismatch` -> `mismatch_count=0`, `missing_count=0`, `invalid_count=0`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-matrix-sweep.ps1 -ArtifactRoot .\vnext\artifacts\c-phase-130-batches\012-deer-adult-male\cockpit-sweep -Species deer -Age adult -Gender male` -> 6 matrix rows.

## Queue State

- Before this batch: `49` queued, `11` done.
- After this batch: `48` queued, `12` done.
- Next queued row: `fox_baby_female`.

