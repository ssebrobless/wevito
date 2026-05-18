# C-PHASE 136 - Sprite-Repair Triage Enrichment

## Goal

Make `sprite-repair-triage` draft cards useful for human review without crossing into sprite mutation or candidate generation.

## Scope

- Added `TaskCard.ReviewPayload` as an optional string dictionary so review-only cards can carry structured details without breaking existing readers.
- Added `SpriteRepairBatchPlan` and `SpriteRepairBatchRunner.BuildPlanForReview(...)`.
- Enriched sprite triage cards with species, age, gender, flagged frame paths, repair command lines, candidate output directory strings, runtime target directory strings, would-write paths, and plan summaries.
- Added `sprite_repair_triage_card_enriched` evidence packet coverage.
- Added tests for non-mutating plan extraction and enriched triage cards.

## Implemented

- `BuildPlanForReview(...)` reuses the same target, candidate folder, repair command, and runtime path calculations as the guarded batch runner.
- The review plan does not call the command runner, does not create the candidate directory, and does not write runtime sprites.
- Triage cards remain `Draft`, `NeedsApproval=true`, and `ToolFamily=sprite-repair-batch-proposal`.
- The card payload stores paths and command strings only. It does not embed image bytes or base64 data.
- The existing batch apply path is unchanged.

## Safety Boundaries

- No sprite PNGs were mutated.
- No `sprites_authored/.candidates/<batch-id>/` directory is created during triage.
- No repair tool is executed by triage enrichment.
- Evidence flags for enriched triage packets remain network=false, hosted_ai=false, local_model=false, mutate=false.
- Existing review/approval gates remain intact.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRepairTriage|SpriteRepairBatchRunner|TaskCard|PlainLanguage"`: passed, 237/237.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1006/1006.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Triage did not write under `sprites_authored/.candidates/`.
- [x] Triage did not write under `sprites_runtime/`.
- [x] Triage payload contains paths and command strings only, not image bytes.
- [x] Triage cards stay draft/review-only and require approval.
- [x] Existing guarded apply behavior remains covered by tests.

## Next Phase

C-PHASE 137 is the next planned phase: audit-ledger cleanup dry-run and rollback posture.
