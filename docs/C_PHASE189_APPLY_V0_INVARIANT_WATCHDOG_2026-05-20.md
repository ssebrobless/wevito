# C-PHASE 189 - Apply-v0 InvariantViolationWatchdog extension

## Goal

Extend `InvariantViolationWatchdog` with deterministic checks for impossible apply-v0 and explicit-rollback-v0 ledger sequences, while preserving the legacy supervised apply invariant rule and the existing maturity-clock reset path.

## Scope (Added / Extended / Not touched)

Added:

- `vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogV0Tests.cs`
- `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md`

Extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs`
- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`

Not touched:

- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`
- Legacy `SelfImprovementPacketKinds.ApplyCompleted`
- UI, pet-sim, sprite workflow, source-code mutation, model, network, held-out eval, in-distribution eval, and tool surfaces
- `Wevito.VNext.sln`

## Implemented (the three rules + the optional emit method, table format)

| Rule | Invariant id | Packet sequence checked | Failure condition |
| --- | --- | --- | --- |
| R1 | `v0_completed_without_applied` | `self_improvement_apply_v0_applied` -> `self_improvement_apply_v0_completed` | Any apply-v0 completed row has no matching apply-v0 applied row with the same `operation_id`. |
| R2 | `v0_rolled_back_followed_by_completed` | `self_improvement_apply_v0_rolled_back` then `self_improvement_apply_v0_completed` | Any apply-v0 completed row is later than a rolled-back row for the same `operation_id`; equal timestamps are tie-broken by ledger row id. |
| R3 | `explicit_rollback_completed_without_started` | `self_improvement_apply_v0_explicit_rollback_started` -> `self_improvement_apply_v0_explicit_rollback_completed` | Any explicit rollback completed row has no matching explicit rollback started row with the same `operation_id`; refused rows do not count as starts. |

| Optional emit surface | Behavior |
| --- | --- |
| Packet kind | `self_improvement_apply_v0_invariant_check_failed` |
| Capability flag | `apply_v0_invariant_check_emit_enabled`, default `bool.FalseString` |
| KillSwitch behavior | `KillSwitchService.IsActive()` makes the emit method return without writing. |
| Evidence honesty | Emitted packets set `DidUseNetwork=false`, `DidUseHostedAi=false`, `DidUseLocalModel=false`, and `DidMutate=false`. |
| Default behavior | No producer enables the new flag; default-off scans write no new invariant-check-failed packet. |

## Safety Boundaries

- No hosted AI runtime brain was added.
- No network or socket surface was added.
- No training, model-weight update, UI surface, pet-sim change, sprite workflow change, or source-code mutation pathway was added.
- `AuditLedgerService` remains append-only.
- The watchdog source still contains no `UPDATE`, `DELETE FROM`, `DROP TABLE`, or `INSERT INTO` SQL.
- The legacy maturity-clock reset writer remains unchanged in behavior.
- The new packet kind is registered in `PlainLanguageExplainer.KnownPacketKinds` and has a plain-language sentence.
- The new capability flag defaults `bool.FalseString` and no producer sets it to true.
- No held-out or in-distribution eval store type is referenced by the new code.
- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` remains `apply_runner_not_implemented_in_v0`.
- Legacy `SelfImprovementPacketKinds.ApplyCompleted` remains `self_improvement_apply_completed`.

## Validation

| Command | Outcome |
| --- | --- |
| Pre-flight `dotnet build .\vnext\Wevito.VNext.sln` | Passed: 0 warnings, 0 errors. |
| Pre-flight `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1629/1629. |
| Pre-flight `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed. |
| Pre-flight `git diff --check` | Passed. |
| Red test run `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InvariantViolationWatchdogV0"` before production changes | Failed as expected: new packet kind, emit flag, and emit method were not yet implemented. |
| Focused new tests `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InvariantViolationWatchdogV0"` | Passed: 17/17. |
| Required A `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InvariantViolationWatchdog\|InvariantViolationWatchdogV0\|PlainLanguage\|SelfImprovement"` | Passed: 338/338. |
| Required B `dotnet build .\vnext\Wevito.VNext.sln` | Passed: 0 warnings, 0 errors. |
| Required C `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1648/1648. |
| Required D `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed. |
| Required E `git diff --check` | Passed; Git reported line-ending normalization warnings only. |
| Stop-gate check `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "MutationScopeAudit"` | Passed: 6/6. |

## Stop-Gate Checklist

- [x] The legacy rule `BuildApplyCompletedWithoutAwaitingApproval` is unchanged.
- [x] `SelfImprovementPacketKinds.ApplyCompleted` constant is unchanged.
- [x] `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` is unchanged.
- [x] New packet kind `self_improvement_apply_v0_invariant_check_failed` is present in `PlainLanguageExplainer.KnownPacketKinds`.
- [x] New flag `apply_v0_invariant_check_emit_enabled` defaults `bool.FalseString` in `CapabilityFlagInventory`.
- [x] No new flag is set True by any producer in this PR.
- [x] No mocking framework is imported in the new test class.
- [x] No `UPDATE`, `DELETE FROM`, `DROP TABLE`, or new `INSERT` SQL appears in the watchdog source.
- [x] Watchdog still passes the C-PHASE 186 `MutationScopeAuditTests`.
- [x] No new `System.Net.Http` import and no new socket open.
- [x] No UI surface changed. No pet-sim or sprite-workflow file changed.
- [x] No held-out or in-distribution eval store type is referenced by the new code.
- [x] Phase report doc written at `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md`.

## Next Phase

C-PHASE 190 - Apply-runner status report additive narrow-runner fields is NOT YET APPROVED FOR IMPLEMENTATION. It requires explicit user signoff before Codex begins it.
