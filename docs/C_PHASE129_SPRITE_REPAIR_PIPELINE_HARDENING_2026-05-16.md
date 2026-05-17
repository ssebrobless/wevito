# C-PHASE 129: Sprite Repair Pipeline Hardening

Date: 2026-05-16
Branch: `claude-implementation/c-phase-129-sprite-repair-hardening`

## Goal

Add the guarded mutation harness that future sprite repair batches must use before any runtime PNG repair is allowed to land.

## Scope

Implemented:

- Added `SpriteRepairBatchRunner`.
- Added `SpriteRepairBatchRunnerTests`.
- Added plain-language coverage for:
  - `sprite_repair_batch`
  - `sprite_repair_batch_rolled_back`
- The runner orchestrates:
  - repair command invocation through `ICommandRunner`
  - candidate output constrained to `sprites_authored/.candidates/<batch-id>/`
  - dry-run planning through `SpriteWorkflowDryRunApplyService`
  - runtime apply through `SpriteWorkflowApplyService`
  - post-proof and rollback through `SpriteWorkflowPostApplyProof`
  - audit evidence packets with honest mutation/network/model flags

Not implemented:

- No production sprite repair batch was run.
- No runtime PNGs were changed in the repository.
- No source boards were changed.
- No model call, hosted AI call, training, or network access.

## Mutation Contract

The runner rejects unsafe paths before command execution:

- Candidate output must stay under `sprites_authored/.candidates/`.
- Runtime writes must resolve under `sprites_runtime/`.
- Unsupported `Senior` age stage rows are rejected by the target builder.
- Missing repair tools fail before mutation.
- The kill switch blocks the run before any command starts.

When a run proceeds:

1. The repair command writes candidate frames only.
2. Dry-run produces a manifest from candidate frames to runtime targets.
3. Apply creates runtime backups and swaps files.
4. Post-proof runs after apply.
5. Failed proof rolls back byte-for-byte through the existing rollback service.
6. The audit ledger records whether mutation actually happened.

## Evidence Flags

Successful or rolled-back apply paths write:

- `DidMutate=true`
- `DidUseNetwork=false`
- `DidUseHostedAi=false`
- `DidUseLocalModel=false`

Pre-apply failures remain honest and do not claim mutation.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRepairBatchRunner|PlainLanguage"` passed: `191/191`.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: `964/964`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
- `git diff --check` passed.

## Stop-Gate Checklist

- [x] Test scenario did not write outside `sprites_runtime/`.
- [x] Rollback test restored the byte-exact original.
- [x] New packet kinds are covered by `PlainLanguageExplainer`.
- [x] Real apply evidence paths use `DidMutate=true`.
- [x] No real runtime sprite mutation was performed in this phase.

## Next Phase

C-PHASE 130 can use this runner to apply one repair queue row per PR. Each batch must still be reviewed independently and must include backup, proof, rollback evidence, and updated queue status.
