# C-PHASE 191 - Watchdog Observer Wiring

## Goal

Wire `InvariantViolationWatchdog` into the read-only apply-runner activity surface through a new `ScanAndEmit` facade, behind a default-off capability flag, while preserving the C-PHASE 190 product-truth pin that forbids external direct calls to `EmitInvariantCheckFailedPackets`.

## Scope

Files added:

- `docs/CLAUDE_C_PHASE191_PLAN_2026-05-21.md`
- `docs/C_PHASE191_WATCHDOG_OBSERVER_WIRING_2026-05-21.md`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityServiceObserverWiringTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogScanAndEmitV0Tests.cs`

Files modified:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityService.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/tests/Wevito.VNext.Tests/PostC189WatchdogProductTruthTests.cs`

Surfaces NOT touched:

- No apply runner source.
- No rollback runner source.
- No supervised loop source.
- No scheduler, judge, replay, snapshot export, eval, scoring, UI, pet, sprite, or asset surface.
- No `vnext/Wevito.VNext.sln`.
- No `docs/codex-phase-history.jsonl`.

## Conflict & Resolution

C-PHASE 190 pinned that no source file outside `InvariantViolationWatchdog.cs` invokes `EmitInvariantCheckFailedPackets(`. A direct C-PHASE 191 observer call would have broken that pin.

The resolution is a watchdog-owned facade:

- `InvariantViolationWatchdog.ScanAndEmit(DateTimeOffset nowUtc)` calls existing `Scan(nowUtc)`.
- If results are non-empty, it calls existing `EmitInvariantCheckFailedPackets(results, nowUtc)`.
- `ApplyRunnerActivityService` may call only `ScanAndEmit`, never `Scan` or `EmitInvariantCheckFailedPackets` directly.
- The refined product-truth fact now allows exactly one external facade caller, in `ApplyRunnerActivityService.cs`, and requires the new observer flag guard near the call site.
- The original direct-emit pin still literally holds: no file outside the watchdog source calls `EmitInvariantCheckFailedPackets(`.

## Implemented

| Change | Location |
| --- | --- |
| Added `ScanAndEmit(DateTimeOffset nowUtc)` facade | `InvariantViolationWatchdog.cs` |
| Added default-false observer flag `apply_v0_invariant_observer_in_activity_service_enabled` | `CapabilityFlagInventory.cs` |
| Added optional watchdog and settings-provider constructor parameters | `ApplyRunnerActivityService.cs` |
| Added flag-guarded per-read `ScanAndEmit` invocation | `ApplyRunnerActivityService.cs` |
| Passed existing watchdog and live settings provider into activity service | `ShellCompositionRoot.cs`, `ShellCoordinator.cs` |
| Refined C-190 producer product-truth fact | `PostC189WatchdogProductTruthTests.cs` |
| Added observer wiring tests | `ApplyRunnerActivityServiceObserverWiringTests.cs` |
| Added facade composition tests | `InvariantViolationWatchdogScanAndEmitV0Tests.cs` |

## Safety Boundaries

- No hosted AI as runtime brain. No silent network. No silent mutation.
- Every new capability flag defaults False.
- Every new audit packet kind registered in `PlainLanguageExplainer.KnownPacketKinds` with a plain-language sentence.
- KillSwitch must halt every surface this batch adds.
- `AuditLedgerService` remains append-only (no UPDATE/DELETE/DROP TABLE SQL).
- Held-out eval data invisible to all surfaces.
- Human is v0 judge. `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason == "apply_runner_not_implemented_in_v0"` unchanged.
- `SelfImprovementPacketKinds.ApplyCompleted == "self_improvement_apply_completed"` unchanged.
- Auto-continue: No on every PR.
- Codex HALTs on any failure.
- `ApplyRunnerActivityService` never calls `Scan` or `EmitInvariantCheckFailedPackets` directly; only `ScanAndEmit`.

## Validation

| Command | Result |
| --- | --- |
| `dotnet build .\vnext\Wevito.VNext.sln` | Passed: 0 warnings, 0 errors |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1674/1674 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed |
| `git diff --check` | Passed; Git reported line-ending normalization warnings only |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "PostC189WatchdogProductTruth"` | Passed: 15/15 |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "InvariantViolationWatchdogV0"` | Passed: 17/17 |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "InvariantViolationWatchdogScanAndEmitV0"` | Passed: 3/3 |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "ApplyRunnerActivity"` | Passed: 20/20 |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "ApplyRunnerActivityServiceObserverWiring"` | Passed: 8/8 |

## Refined Facts

| Original name | Refined name | Original assertion | Refined assertion |
| --- | --- | --- | --- |
| `ProductTruth_watchdog_has_no_producer_in_src_tree` | `ProductTruth_watchdog_only_allowed_external_facade_producer_is_activity_service_under_flag` | No source file outside the watchdog invokes `EmitInvariantCheckFailedPackets(`. | Still forbids external direct `EmitInvariantCheckFailedPackets(` calls, allows exactly one external `ScanAndEmit(` caller in `ApplyRunnerActivityService.cs`, requires the observer flag guard near the call, and forbids unapproved external direct `.Scan(` calls. |

## Stop-Gate Checklist

- [x] Step 0 workspace gate passed (`C:\Users\fishe\Documents\projects\wevito`).
- [x] No file under `.codex\worktrees\` touched.
- [x] `ScanAndEmit` added on watchdog as one additive method.
- [x] HOST calls `ScanAndEmit` and neither `Scan` nor `EmitInvariantCheckFailedPackets` directly.
- [x] C-190 fact refinement tightens with allowed-caller, allowed-method, and flag-guard conditions.
- [x] Original direct emit pin literally still holds.
- [x] Zero new packet kind.
- [x] New flag defaults `bool.FalseString`.
- [x] No existing flag flipped.
- [x] Zero change to apply/rollback runners, supervised loop, scheduler, judge, replay, snapshot export, eval, scoring, UI, pet, sprite, or asset surfaces.
- [x] `vnext/Wevito.VNext.sln` unchanged.
- [x] All five targeted regression filters pass.
- [x] Auto-continue: No.

## Next Phase

C-PHASE 192 candidate: extend observer to capabilities-and-gates snapshot OR begin narrow scheduler tick. Auto-continue: No.
