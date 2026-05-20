# C-PHASE 178 - Eval Coverage Health Service

## Goal

Add a read-only eval coverage health surface that reports whether Wevito has enough held-out and in-distribution eval cases before any future apply runner could be considered.

Base commit: `ae714d5e6833bb4aaa5f3345040d7cc4f2026c78`

## Scope

Files added:
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalCoverageHealthSnapshot.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalCoverageHealthService.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalCoverageHealthServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalCoverageHealthHeldOutAccessTests.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalCoverageHealthUIBindingTests.cs`

Files extended:
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionVersusHeldOutTypeSeparationTests.cs`

The separation test update was required because it already had an explicit C-PHASE 174 exception for a counts-only service receiving both eval stores. C-PHASE 178 adds the same kind of documented exception for `EvalCoverageHealthService`, with access-recorder tests proving it uses only `ListCaseIds()`.

## Implemented

- Added `EvalCoverageHealthSnapshot`, carrying only counts, minimums, pass/fail booleans, timestamp, and reason.
- Added `EvalCoverageHealthService`, which calls only:
  - `IHeldOutEvalStore.ListCaseIds()`
  - `IInDistributionEvalStore.ListCaseIds()`
- Added minimums:
  - held-out: `5`
  - in-distribution: `10`
- Added KillSwitch behavior: active KillSwitch returns zero counts with reason `kill_switch=true`.
- Added `ShellCompositionRoot.CreateEvalCoverageHealthService(...)`.
- Wired the service into `ShellCoordinator`.
- Added an Evidence tab expander titled `Eval coverage (read-only)` beneath `Why is apply blocked? (read-only)`.
- Added tests for thresholds, KillSwitch behavior, no `ReadCase()` access, no case-ID UI binding, and visibility lockdown.

## Safety Boundaries

- No case IDs are carried in the snapshot.
- No case contents are carried in the snapshot.
- The UI displays only counts, minimums, pass/fail text, timestamp, and reason.
- The service does not call `ReadCase()` on either store.
- No audit ledger writes.
- No model calls.
- No network calls.
- No mutation or apply behavior.
- No new capability flag.
- No new audit packet kind.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalCoverageHealth|HeldOutEvalStoreVisibility|InDistributionEvalStoreVisibility|PlainLanguage|SelfImprovement"`: passed, 292/292.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1368/1368.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Service does not call `ReadCase()` on either store.
- [x] Snapshot does not surface case IDs.
- [x] UI does not surface case IDs.
- [x] Tests do not require or display case content.
- [x] Held-out and in-distribution lockdown tests still cover non-allowlisted surfaces.
- [x] No packet kind added.
- [x] No capability flag added.
- [x] No audit ledger writes.
- [x] All validation commands passed.

## Next Phase

C-PHASE 179 - Supervised scoring dry-run (default off) - Auto-continue=No.
