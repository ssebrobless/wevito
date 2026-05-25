# C-PHASE 195: Watchdog Observer in EvalCoverageHealthService

## Goal

Wire `InvariantViolationWatchdog.ScanAndEmit` into `EvalCoverageHealthService.Snapshot(...)` as the fourth authorized facade caller, behind a new default-false flag. Preserve held-out eval invisibility: the host continues to count case IDs only, and the watchdog hook receives only the snapshot timestamp.

## Anchor

- `gate/post-c-phase-194` at `ff3e9c3be99287b67a9ec10cc5b772824a1628a8`
- KnownPacketKinds baseline: 160
- Baseline full suite: 1706/1706

## Branch

`c-phase-195-watchdog-observer-in-eval-coverage-health`

## Implemented

| File path | Kind | Count delta | Summary |
| --- | --- | --- | --- |
| `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalCoverageHealthService.cs` | edited | 0 | Adds `WatchdogObserverEnabledSetting`, optional watchdog/settings dependencies, and one guarded `ScanAndEmit(now)` call before the snapshot return. |
| `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs` | edited | 0 | Registers `snapshot_v0_invariant_observer_in_eval_coverage_health_enabled` with `bool.FalseString`. |
| `vnext/tests/Wevito.VNext.Tests/EvalCoverageHealthServiceObserverWiringTests.cs` | new | +8 facts | Adds product-truth pins for flag default, ctor compatibility, flag-off/on/null behavior, facade-only source, no packet-kind change, and KillSwitch participation. |
| `vnext/tests/Wevito.VNext.Tests/PostC189WatchdogProductTruthTests.cs` | renamed/extended, not new | 0 | Extends the external facade producer allowlist from 3 to 4 and renames the fact in place. |
| `vnext/tests/Wevito.VNext.Tests/PostC191WatchdogObserverProductTruthTests.cs` | renamed/extended, not new | 0 | Extends the source/tool `ScanAndEmit` caller allowlist from 4 to 5 and renames the fact in place. |
| `vnext/tests/Wevito.VNext.Tests/PostC191WatchdogObserverProductTruthTests.cs` | new-fact | +1 fact | Adds the watchdog-held-out source-invisibility pin. |
| `vnext/docs/C_PHASE195_WATCHDOG_OBSERVER_IN_EVAL_COVERAGE_HEALTH_2026-05-24.md` | new | 0 | Phase report. |

## Cross-Cutting Caller-Allowlist Extension

The C-189 product-truth pin now extends from 3 to 4 authorized external facade producers by adding `EvalCoverageHealthService` alongside `ApplyRunnerActivityService`, `CapabilitiesAndGatesService`, and `ProposalQualityMetricsService`.

The C-191 product-truth pin now extends from 4 to 5 total source/tool callers by adding `EvalCoverageHealthService` alongside the watchdog definition and the three existing host surfaces.

Both allowlist changes are rename-in-place extensions. They remain explicit enumerations, not wildcard or pattern-based relaxations.

| File | Old fact | New fact |
| --- | --- | --- |
| `PostC189WatchdogProductTruthTests.cs` | `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_capabilities_and_gates_and_proposal_quality_metrics_under_flag` | `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_capabilities_and_gates_proposal_quality_metrics_and_eval_coverage_health_under_flag` |
| `PostC191WatchdogObserverProductTruthTests.cs` | `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_capabilities_and_gates_and_proposal_quality_metrics` | `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_capabilities_and_gates_proposal_quality_metrics_and_eval_coverage_health` |

## Held-Out Adjacency

`EvalCoverageHealthService` is held-out-adjacent by design: it receives `IHeldOutEvalStore` and `IInDistributionEvalStore` to call `ListCaseIds()` and produce count-only health snapshots. It does not call `ReadCase()`.

This phase keeps that boundary intact. The watchdog hook receives only `now`, and `InvariantViolationWatchdog` continues to read only the audit ledger by packet kind. No held-out content, case body, prompt, or store instance is passed to the watchdog or audit-emitted by this wiring.

`PostC191WatchdogObserverProductTruthTests` grows by one fact: `ProductTruth_invariant_violation_watchdog_source_does_not_reference_held_out_eval_store`, which asserts the watchdog source does not reference `IHeldOutEvalStore`, `HeldOutEvalStore`, `HeldOutCase`, or `_heldOut`.

## Build + Test Results

| Command | Result |
| --- | --- |
| `dotnet build vnext\Wevito.VNext.sln -c Release -nologo` | PASS: 0 warnings, 0 errors. |
| `--filter "FullyQualifiedName~EvalCoverageHealthServiceObserverWiringTests"` | PASS: 8/8. |
| `--filter "FullyQualifiedName~CapabilitiesAndGatesServiceObserverWiringTests"` | PASS: 8/8. |
| `--filter "FullyQualifiedName~ApplyRunnerActivityServiceObserverWiringTests"` | PASS: 8/8. |
| `--filter "FullyQualifiedName~ProposalQualityMetricsServiceObserverWiringTests"` | PASS: 8/8. |
| `--filter "FullyQualifiedName~PostC189WatchdogProductTruthTests"` | PASS: 15/15. |
| `--filter "FullyQualifiedName~PostC191WatchdogObserverProductTruthTests"` | PASS: 17/17. |
| `dotnet test vnext\Wevito.VNext.sln -c Release -nologo --no-build` | PASS: 1715/1715. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | PASS. |
| `git diff --check` | PASS. |

## Risks Considered

- Held-out eval exposure: mitigated by passing only `now` into `ScanAndEmit` and adding a watchdog source pin against held-out store references.
- Surprise observer callers: mitigated by explicit C-189/C-191 caller allowlist extensions.
- Silent runtime expansion: mitigated by default-false flag registration.
- Packet-kind drift: mitigated by existing and new `KnownPacketKinds.Count == 160` pins.
- Apply/rollback behavior drift: no apply runner, rollback runner, scheduler, replay, judge, scoring, UI, pet, sprite, asset, or tool behavior is changed.

## Stop-Gate Checklist

- [x] Branch anchored on `gate/post-c-phase-194`.
- [x] No `git checkout main`.
- [x] No `git pull` on main.
- [x] No file under `.codex\worktrees\` touched.
- [x] No IDE or generic `dotnet` process killed.
- [x] New flag defaults `bool.FalseString`.
- [x] No new packet kind.
- [x] `KnownPacketKinds.Count` remains 160.
- [x] Watchdog hook receives only `now`.
- [x] Watchdog source remains free of held-out eval store references.
- [x] C-189/C-191 caller allowlists extended explicitly by name.
- [x] Auto-continue: No.
