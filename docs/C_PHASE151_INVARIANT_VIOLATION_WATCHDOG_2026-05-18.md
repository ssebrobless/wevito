# C-PHASE 151 - Invariant-Violation Watchdog

## Goal

Add a default-off, read-only invariant watchdog for the supervised self-improvement audit ledger. The watchdog detects impossible packet combinations and writes only `self_improvement_maturity_clock_reset` packets when a reset reason is not already present.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantCheck.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantCheckResult.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs`
- `vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogTests.cs`
- `docs/C_PHASE151_INVARIANT_VIOLATION_WATCHDOG_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `docs/codex-phase-history.jsonl` after PR creation.

Files not touched:

- No sprite assets.
- No content manifests.
- No model adapters.
- No network surfaces.
- No held-out eval store or held-out eval paths.
- No apply runner.

## Implemented

- Added `InvariantCheck` and `InvariantCheckResult` records for deterministic rule results.
- Added `InvariantViolationWatchdog.EnabledSetting = invariant_violation_watchdog_enabled`.
- The new flag defaults to `false` in shell settings hydration.
- Added `InvariantViolationWatchdog.Scan(DateTimeOffset nowUtc)` with six checks:
  - `rule_silent_mutation_in_review_only_scope` -> `SilentMutationDetected`.
  - `rule_silent_network` -> `SilentNetworkDetected`.
  - `rule_silent_hosted_ai` -> `SilentHostedAiDetected`.
  - `rule_apply_completed_without_awaiting_approval` -> `InvariantViolation`.
  - `rule_awaiting_approval_without_preceding_chain` -> `InvariantViolation`.
  - `rule_duplicate_awaiting_approval` -> `InvariantViolation`.
- The watchdog excludes existing `self_improvement_maturity_clock_reset` rows from detection inputs so it cannot trigger itself.
- Reset packet writes use `AuditLedgerService.Record` only, with all evidence flags set false and `Status = Completed`.
- Duplicate reset suppression is per `MaturityClockResetReason`; repeated scans do not add another reset for the same reason already present.
- Shell composition now constructs the watchdog from the existing audit database path and shared `KillSwitchService`.
- The shell autonomous tick calls the watchdog only when `invariant_violation_watchdog_enabled=true`; it logs triggered counts and does not surface a popup.
- Tests cover every rule, duplicate suppression, KillSwitch blocking, flag-off/absent blocking, no self-trigger loop, read-only SQL source assertions, and write-path packet-kind restrictions.

## Safety Boundaries

- The watchdog opens SQLite with `Mode = ReadOnly` and uses only a SELECT query for ledger inspection.
- The watchdog source does not contain SQL `INSERT`, `UPDATE`, or `DELETE`.
- The only packet kind written by the watchdog is `SelfImprovementPacketKinds.MaturityClockReset`.
- The watchdog writes no artifact files and does not touch sprite/content/model/network surfaces.
- The watchdog does not reference `IHeldOutEvalStore` or held-out eval paths.
- `KillSwitchService.IsActive()` returns an empty result and writes nothing.
- If `invariant_violation_watchdog_enabled` is absent or not true, the watchdog returns an empty result and writes nothing.

## Validation

Pre-flight on synced `17161b6c607b8f1095b5d15023d0f457e0534442`:

- `git status`: clean after stashing the three untracked Claude planning docs.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1125/1125.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

Final validation:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Invariant|MaturityScoreboard|PlainLanguage|SelfImprovement"`: passed, 260/260.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1138/1138.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with normal CRLF working-copy warnings only.

## Stop-Gate Checklist

- Watchdog writes any packet kind other than `self_improvement_maturity_clock_reset`: FALSE.
- Watchdog SQL contains `INSERT`, `UPDATE`, or `DELETE`: FALSE.
- Watchdog reads `IHeldOutEvalStore` or any held-out path: FALSE.
- Watchdog touches a file outside `vnext/artifacts/`: FALSE.
- `invariant_violation_watchdog_enabled` defaults to anything other than `false` or absent: FALSE.
- Pre-flight commands fail: FALSE.

## Next Phase

C-PHASE 152 - Scope-hash binding. Auto-continue is No; do not start until the user provides the C-PHASE 152 prompt.
