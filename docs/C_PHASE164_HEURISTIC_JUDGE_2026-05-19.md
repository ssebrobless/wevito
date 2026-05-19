# C-PHASE 164 - Heuristic Judge Service

## Goal

Add a deterministic, no-model critique service for open self-improvement approval cards. The service is default-off and writes only `self_improvement_judge_critique` packets.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Judge/HeuristicJudgeRule.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Judge/HeuristicJudgeFinding.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Judge/HeuristicJudgeService.cs`
- `vnext/tests/Wevito.VNext.Tests/HeuristicJudgeServiceTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`

Not touched:

- model adapters
- hosted AI surfaces
- held-out eval store
- in-distribution eval store
- apply runner behavior
- sprite/runtime assets

## Implemented

- Added `HeuristicJudgeRule` and `HeuristicJudgeFinding` result records.
- Added `HeuristicJudgeService.EnabledSetting = "heuristic_judge_enabled"`.
- Added read-only SQLite lookup for the latest `self_improvement_apply_awaiting_approval` row for an operation id.
- Added artifact-path canonicalization and refusal for awaiting-approval artifacts outside `vnext/artifacts`.
- Added six deterministic judge rules:
  - `rule_did_mutate_false`
  - `rule_apply_runner_not_implemented`
  - `rule_scope_hash_present_format`
  - `rule_artifact_paths_under_artifacts`
  - `rule_proposal_source_hashes_match_live`
  - `rule_eval_lists_every_manifest_gate`
- Added exactly one `self_improvement_judge_critique` packet per critique call when enabled and a valid awaiting-approval artifact exists.
- Added `JudgeCritique` to `SelfImprovementPacketKinds`.
- Added a plain-language explanation for the new packet kind.
- Added the default-off capability flag inventory row.
- Wired the service through `ShellCompositionRoot`.
- Wired `ShellCoordinator` to run the judge on tick only when `heuristic_judge_enabled=true` and an open awaiting-approval card has an `operation_id`.

## Safety Boundaries

- The service performs no model calls.
- The service performs no network calls.
- The service does not reference `IModelAdapter`.
- The service does not reference `IHeldOutEvalStore`.
- The service does not reference `IInDistributionEvalStore`.
- The service SQL contains no `INSERT`, `UPDATE`, or `DELETE`.
- The service writes only `SelfImprovementPacketKinds.JudgeCritique`.
- The new capability flag defaults to `false`.
- No UI panel was added in this phase.
- No apply behavior was added or enabled.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "HeuristicJudge|PlainLanguage|SelfImprovement"`: passed, 255/255.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1236/1236.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Service does not reference `IModelAdapter`.
- [x] Service writes no packet kind except `JudgeCritique`.
- [x] Service does not reference `IHeldOutEvalStore`.
- [x] Service does not reference `IInDistributionEvalStore`.
- [x] `heuristic_judge_enabled` defaults to `false`.
- [x] A single `Critique(...)` call emits at most one packet.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 165 - snapshot export CLI - Auto-continue=No.
