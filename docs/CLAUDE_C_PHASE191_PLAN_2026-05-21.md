# C-PHASE 191 Plan - ScanAndEmit Facade + ActivityService Observer Wiring

## Goal

Wire `InvariantViolationWatchdog` as a passive observer into `ApplyRunnerActivityService` through a new additive `ScanAndEmit` facade, behind a new default-false capability flag. Path A preserves the C-PHASE 190 pin's intent: no source file outside the watchdog may call `EmitInvariantCheckFailedPackets` directly, while a single read-only host may call the facade under an explicit flag gate.

## Conflict resolved

C-PHASE 190 pinned that no source file outside `InvariantViolationWatchdog.cs` calls `EmitInvariantCheckFailedPackets(`. A direct observer call from `ApplyRunnerActivityService` would violate that pin. Path A resolves the conflict by adding `ScanAndEmit(DateTimeOffset nowUtc)` on the watchdog and refining the product-truth fact to allow exactly one external facade caller, in `ApplyRunnerActivityService.cs`, only via `ScanAndEmit(`, and only when guarded by `apply_v0_invariant_observer_in_activity_service_enabled`.

## Constraints carried forward

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

## Options considered

### Option A - ScanAndEmit facade + strict pin refinement

Add one watchdog facade method, one default-false observer flag, guarded `ApplyRunnerActivityService` wiring, a strict product-truth refinement, and focused facade/observer tests. This keeps the low-level emit method watchdog-owned and gives future reviewers deterministic checks for allowed caller, allowed method, and required flag guard.

Blast radius: M.

Single biggest risk: a read observer can become too eager if the flag guard drifts.

### Option B - Loosen the pin directly

Allow the activity host to call `EmitInvariantCheckFailedPackets(` directly. This is rejected because it weakens the product-truth claim and makes future unguarded producers harder to distinguish from the intended observer.

Blast radius: M.

Single biggest risk: direct low-level emit calls outside the watchdog become normalized.

### Option C - Sequenced C-190.1 then C-191

First land a test-only pin refinement, then land observer wiring later. This is rejected because Path A can preserve the same safety boundary in one reviewable PR without deleting or weakening the C-190 fact.

Blast radius: S then M.

Single biggest risk: extra phase overhead without a stronger safety result.

## Implementation shape

- NEW METHOD on watchdog: `ScanAndEmit(DateTimeOffset nowUtc)`. Visibility: public because the host and tests already consume the existing public watchdog surface.
- Body: call `Scan(nowUtc)`, if results are non-empty call `EmitInvariantCheckFailedPackets(results, nowUtc)`, return the results.
- KillSwitch: inherited from `Scan` and `EmitInvariantCheckFailedPackets`, which both check `KillSwitchService.IsActive()` at entry.
- NEW FLAG: `apply_v0_invariant_observer_in_activity_service_enabled` (DefaultValue=false).
- HOST constructor gets optional `InvariantViolationWatchdog? watchdog = null`, stored readonly.
- HOST follows the existing sibling-service settings-provider convention with an optional `Func<IReadOnlyDictionary<string, string>>? settingsProvider = null` defaulting to an empty dictionary.
- Per existing HOST read operation, append: when `watchdog is not null` and `IsTrue(_settingsProvider(), ObserverEnabledSetting)`, call `watchdog.ScanAndEmit(DateTimeOffset.UtcNow)`.
- HOST never calls `Scan` or `EmitInvariantCheckFailedPackets` directly.
- DIPOINT passes the existing watchdog and live settings provider into HOST, relying on the per-call guard. It does not add composition-time construction of a second watchdog.

## Pin refinements

- `ProductTruth_watchdog_has_no_producer_in_src_tree` becomes `ProductTruth_watchdog_only_allowed_external_facade_producer_is_activity_service_under_flag`.
- The refined fact still forbids `EmitInvariantCheckFailedPackets(` calls from any file outside the watchdog source.
- The refined fact allows exactly one external `ScanAndEmit(` call site, in `ApplyRunnerActivityService.cs`.
- The refined fact requires that call site to be guarded by `IsTrue(_settingsProvider(), ObserverEnabledSetting)`.
- The refined fact forbids external direct `.Scan(` calls outside the watchdog source and the existing shell watchdog scan path.

## What this PR does NOT do

- Does not add any new packet kind.
- Does not change `Scan` behavior.
- Does not change `EmitInvariantCheckFailedPackets` behavior.
- Does not flip any existing flag.
- Does not change apply or rollback runners.
- Does not introduce schedulers.
- Does not change UI, sprite, pet, or asset surfaces.
- Does not touch held-out eval data.
- Does not invoke judge, replay, snapshot export, scoring, or eval surfaces.

## Acceptance criteria

- Exactly four new files: this plan doc, phase report, observer wiring tests, and `ScanAndEmit` v0 tests.
- Production changes are limited to the watchdog facade method, one default-false inventory flag, `ApplyRunnerActivityService`, and the host construction point.
- `PostC189WatchdogProductTruthTests.cs` keeps 15 facts and refines the producer pin without deleting it.
- No new packet kind.
- New flag defaults `bool.FalseString`.
- Full test count after = 1674/1674.
- Focused filters pass: `PostC189WatchdogProductTruth`, `InvariantViolationWatchdogV0`, `InvariantViolationWatchdogScanAndEmitV0`, `ApplyRunnerActivity`, and `ApplyRunnerActivityServiceObserverWiring`.
- Preflight gate (build, full test, build-vnext.ps1, `git diff --check`) passes.
- Auto-continue: No.

## References

- `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md`
- `docs/C_PHASE190_WATCHDOG_PRODUCT_TRUTH_2026-05-21.md`
