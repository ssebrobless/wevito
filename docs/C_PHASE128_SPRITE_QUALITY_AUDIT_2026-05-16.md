# C-PHASE 128: Sprite Quality Audit

Date: 2026-05-16
Branch: `claude-implementation/c-phase-128-sprite-quality-audit`

## Goal

Turn the C-PHASE 127 visual QA cockpit manifest into a deterministic, report-only sprite repair queue that can drive later guarded repair phases without mutating sprite assets during this phase.

## Scope

Implemented:

- Added `tools/build_sprite_repair_queue.py`.
- Added `SpriteRepairQueueReader` for runtime/code-side reading and validation of the generated queue.
- Added focused tests for queue loading, ordering, senior-stage rejection, missing-tool rejection, kill-switch behavior, and plain-language packet coverage.
- Added generated evidence under `vnext/artifacts/c-phase-128-sprite-repair-queue/`.
- Added `sprite_repair_queue_built` to `PlainLanguageExplainer.KnownPacketKinds`.

Not implemented:

- No sprite PNG mutation.
- No source board mutation.
- No visual generation/import.
- No model call, hosted AI call, training, or network access.
- No repair execution.

## Inputs

- `vnext/artifacts/c-phase-127-matrix-sweep/visual_qa_manifest.json`
- `tools/audit_sprite_contract.py`
- Runtime and authored sprite folders, read-only.

## Generated Queue Summary

- Rows queued: `60`
- Expected family rows: `60`
- Priority counts: `P0=0`, `P1=60`, `P2=0`, `P3=0`
- Tag counts: `missing_frames=720`, `crop_detected=1152`, `box_background=6`
- Senior rows: `0`
- Referenced repair tools: `tools/generate_runtime_pose_sprites.py`, `tools/repair_runtime_sprite_canvas_padding.py`

The queue currently treats each species/age/gender row as P1 because direct visible action families, especially `drink` and `groom`, are missing direct authored/runtime frames across the matrix. Crop and box-background evidence is retained on each affected row so the next phases can combine pose generation with canvas cleanup when needed.

## Artifacts

- `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.json`
- `vnext/artifacts/c-phase-128-sprite-repair-queue/repair_queue.md`
- `vnext/artifacts/c-phase-128-sprite-repair-queue/sprite_contract.json`

## Safety Boundaries

- The queue builder is read-only except for writing its timestamped artifact outputs.
- The C# reader validates that no row references the unsupported `senior` age stage.
- The C# reader rejects queues that reference missing repair tools.
- The C# reader honors `KillSwitchService` before stateful validation and returns an empty queue when the kill switch is active.
- No runtime PNG, source PNG, prop anchor, or content manifest was mutated.

## Stop-Gate Checklist

- [x] Queue rows do not include non-existent `senior` cells.
- [x] All referenced repair tools exist.
- [x] P0 count is not above 30. Actual: `0`.
- [x] No hosted AI call observed or required.
- [x] No network access required.
- [x] No sprite mutation occurred.

## Validation

- `python tools\build_sprite_repair_queue.py --manifest vnext\artifacts\c-phase-127-matrix-sweep\visual_qa_manifest.json --out vnext\artifacts\c-phase-128-sprite-repair-queue\repair_queue.json` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRepairQueue"` passed: `7/7`.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `951/951`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Next Phase

C-PHASE 129 should consume this repair queue and begin the first guarded repair pass only after user approval. The next phase should preserve the same mutation policy: exact scope, backup, sha256, dry-run, apply, post-proof, and rollback.
