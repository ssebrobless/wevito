# C-PHASE 194: Watchdog Observer in ProposalQualityMetricsService

## Goal

C-PHASE 194 wires `InvariantViolationWatchdog.ScanAndEmit` into `ProposalQualityMetricsService.Snapshot` behind a new default-false capability flag. This creates the third authorized read-only observer surface after `ApplyRunnerActivityService` and `CapabilitiesAndGatesService`, while preserving the facade rule: hosts call `ScanAndEmit` only, never `Scan` or `EmitInvariantCheckFailedPackets` directly.

## Scope

Files modified:

| Path | Change |
| --- | --- |
| `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs` | Adds one default-false flag for the proposal-quality-metrics observer. |
| `vnext/src/Wevito.VNext.Core/SelfImprovement/ProposalQualityMetricsService.cs` | Adds optional watchdog/settings dependencies and guarded `ScanAndEmit` invocation in `Snapshot`. |
| `vnext/tests/Wevito.VNext.Tests/PostC189WatchdogProductTruthTests.cs` | Renames/extends the explicit external-facade caller allowlist pin from two to three external callers. |
| `vnext/tests/Wevito.VNext.Tests/PostC191WatchdogObserverProductTruthTests.cs` | Renames/extends the source/tool `ScanAndEmit` caller allowlist pin from three to four total callers. |

Files added:

| Path | Purpose |
| --- | --- |
| `vnext/tests/Wevito.VNext.Tests/ProposalQualityMetricsServiceObserverWiringTests.cs` | Adds 8 product-truth facts for the third observer wiring point. |
| `docs/C_PHASE194_WATCHDOG_OBSERVER_IN_PROPOSAL_QUALITY_METRICS_2026-05-22.md` | Phase report. |

Files not touched: no apply runner source, rollback runner source, supervised loop, scheduler, judge, replay, snapshot store, eval, scoring, UI, pet, sprite, asset, tool, or solution file changes.

## Cross-Cutting Caller-Allowlist Extension

The C-189 and C-191 product-truth pins are explicit-enumeration allowlists of the authorized external callers of `InvariantViolationWatchdog.ScanAndEmit`. They are intentionally strict so surprise callers fail loudly.

This phase extends the allowlist from 3 to 4 total source/tool callers by adding `ProposalQualityMetricsService` alongside the watchdog definition, `ApplyRunnerActivityService`, and `CapabilitiesAndGatesService`. This is the standard, expected pattern when a new authorized wiring lands: extend the enumeration by name and rename the fact so it remains truthful. It is not a relaxation, wildcard, or pattern-based bypass.

| File | Old fact | New fact |
| --- | --- | --- |
| `PostC189WatchdogProductTruthTests.cs` | `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_and_capabilities_and_gates_under_flag` | `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_capabilities_and_gates_and_proposal_quality_metrics_under_flag` |
| `PostC191WatchdogObserverProductTruthTests.cs` | `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_and_capabilities_and_gates` | `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_capabilities_and_gates_and_proposal_quality_metrics` |

Future observer-wiring phases must repeat this pattern: add the new authorized caller explicitly and rename the affected caller-allowlist facts to state the new authorized set.

## Implemented

| Fact or change | Summary |
| --- | --- |
| `ProductTruth_proposal_quality_metrics_observer_flag_registered_default_false` | Confirms the new C-194 flag is registered with `bool.FalseString`. |
| `ProductTruth_proposal_quality_metrics_service_ctor_backward_compatible` | Confirms pre-existing constructor call shapes still work and the watchdog/settings parameters remain optional. |
| `ProductTruth_proposal_quality_metrics_snapshot_flag_off_does_not_invoke_watchdog` | Confirms flag-off snapshots do not emit invariant packets. |
| `ProductTruth_proposal_quality_metrics_snapshot_flag_on_records_exactly_one_invariant_row` | Confirms one flag-on snapshot emits exactly one expected watchdog packet. |
| `ProductTruth_proposal_quality_metrics_snapshot_flag_on_null_watchdog_does_not_throw` | Confirms flag-on with null watchdog preserves normal snapshot behavior. |
| `ProductTruth_proposal_quality_metrics_service_source_does_not_call_scan_or_emit_directly` | Confirms the service calls only `ScanAndEmit`, not direct `Scan` or `EmitInvariantCheckFailedPackets`. |
| `ProductTruth_proposal_quality_metrics_no_new_audit_packet_kind` | Confirms `KnownPacketKinds` remains at 160. |
| `ProductTruth_proposal_quality_metrics_service_source_references_killswitch` | Confirms the host remains KillSwitch-participating. |
| `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_capabilities_and_gates_and_proposal_quality_metrics_under_flag` | Renamed/extended, not new: C-189 external facade producer allowlist now names ActivityService, CapabilitiesAndGatesService, and ProposalQualityMetricsService. |
| `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_capabilities_and_gates_and_proposal_quality_metrics` | Renamed/extended, not new: C-191 source/tool caller allowlist now names watchdog, ActivityService, CapabilitiesAndGatesService, and ProposalQualityMetricsService. |

## Safety Boundaries

- New flag defaults false: `snapshot_v0_invariant_observer_in_proposal_quality_metrics_enabled`.
- No new packet kind.
- `PlainLanguageExplainer.KnownPacketKinds` remains 160.
- No audit-ledger schema change.
- `AuditLedgerService` remains append-only.
- No UI change.
- No pet, sprite, asset, or FPS-sensitive surface change.
- No apply runner source change.
- No rollback runner source change.
- No supervised loop change.
- No scheduler, judge, replay, eval, scoring, tool, or solution change.
- Held-out and in-distribution eval data remain invisible to all surfaces.
- `ProposalQualityMetricsService` never calls `Scan` or `EmitInvariantCheckFailedPackets` directly; only `ScanAndEmit`.
- `ScanAndEmit` remains the facade for host wiring.
- Auto-continue: No.

## Validation

| Command | Result |
| --- | --- |
| `dotnet build .\vnext\Wevito.VNext.sln` | PASS: 0 warnings, 0 errors. |
| `--filter "ProposalQualityMetricsServiceObserverWiring"` | PASS: 8/8. |
| `--filter "CapabilitiesAndGatesServiceObserverWiring"` | PASS: 8/8. |
| `--filter "ApplyRunnerActivityServiceObserverWiring"` | PASS: 8/8. |
| `--filter "PostC189WatchdogProductTruth"` | PASS: 15/15. |
| `--filter "PostC191WatchdogObserverProductTruth"` | PASS: 16/16. |
| `--filter "InvariantViolationWatchdogScanAndEmitV0"` | PASS: 3/3. |
| `--filter "InvariantViolationWatchdogV0"` | PASS: 17/17. |
| `--filter "ProposalQualityMetrics"` | PASS: 15/15. |
| `--filter "CapabilityFlagInventory"` | PASS: 4/4. |
| `--filter "PlainLanguage"` | PASS: 272/272. |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | PASS: 1706/1706. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | PASS. |
| `git diff --check` | PASS. |

## Stop-Gate Checklist

- [x] Branch created from `gate/post-c-phase-193`, not local `main`.
- [x] No `git checkout main`.
- [x] No `git pull` on main.
- [x] No file under `.codex\worktrees\` touched.
- [x] No IDE or generic `dotnet` process killed.
- [x] New flag defaults false.
- [x] No new packet kind.
- [x] No existing flag flipped.
- [x] `KnownPacketKinds` remains 160.
- [x] C-189/C-191 caller allowlists extended explicitly by name.
- [x] All targeted regression filters pass.
- [x] Full suite passes at 1706/1706.
- [x] Auto-continue: No.

## Next Phase

Deferred to user; Auto-continue: No.
