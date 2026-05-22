# C-PHASE 193: Watchdog Observer in CapabilitiesAndGatesService

## Goal

C-PHASE 193 wires `InvariantViolationWatchdog.ScanAndEmit` into `CapabilitiesAndGatesService.Snapshot` behind a new default-false capability flag. This creates a second authorized read-only observer surface while preserving the C-191 facade pattern: hosts call `ScanAndEmit` only, never `Scan` or `EmitInvariantCheckFailedPackets` directly.

## Scope

Files modified:

| Path | Change |
| --- | --- |
| `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs` | Adds one default-false flag for the capabilities-and-gates observer. |
| `vnext/src/Wevito.VNext.Core/SelfImprovement/CapabilitiesAndGatesService.cs` | Adds optional watchdog dependency and guarded `ScanAndEmit` invocation in `Snapshot`. |
| `vnext/tests/Wevito.VNext.Tests/PostC189WatchdogProductTruthTests.cs` | Renames/extends the explicit external-facade caller allowlist pin from one to two external callers. |
| `vnext/tests/Wevito.VNext.Tests/PostC191WatchdogObserverProductTruthTests.cs` | Renames/extends the source/tool `ScanAndEmit` caller allowlist pin from two to three total callers. |

Files added:

| Path | Purpose |
| --- | --- |
| `vnext/tests/Wevito.VNext.Tests/CapabilitiesAndGatesServiceObserverWiringTests.cs` | Adds 8 product-truth facts for the second observer wiring point. |
| `docs/C_PHASE193_WATCHDOG_OBSERVER_IN_CAPABILITIES_AND_GATES_2026-05-22.md` | Phase report. |

Files not touched: no apply runner source, rollback runner source, supervised loop, scheduler, judge, replay, snapshot store, eval, scoring, UI, pet, sprite, asset, tool, or solution file changes.

## Cross-Cutting Caller-Allowlist Extension

The C-189 and C-191 product-truth pins are explicit-enumeration allowlists of the authorized external callers of `InvariantViolationWatchdog.ScanAndEmit`. They are intentionally strict so surprise callers fail loudly.

This phase extends the allowlist from 2 to 3 total source/tool callers by adding `CapabilitiesAndGatesService` alongside the watchdog definition and `ApplyRunnerActivityService`. This is the standard, expected pattern when a new authorized wiring lands: extend the enumeration by name and rename the fact so it remains truthful. It is not a relaxation, wildcard, or pattern-based bypass.

| File | Old fact | New fact |
| --- | --- | --- |
| `PostC189WatchdogProductTruthTests.cs` | `ProductTruth_watchdog_only_allowed_external_facade_producer_is_activity_service_under_flag` | `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_and_capabilities_and_gates_under_flag` |
| `PostC191WatchdogObserverProductTruthTests.cs` | `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_and_activity_service` | `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_and_capabilities_and_gates` |

Future observer-wiring phases must repeat this pattern: add the new authorized caller explicitly and rename the affected caller-allowlist facts to state the new authorized set.

## Implemented

| Fact or change | Summary |
| --- | --- |
| `ProductTruth_capabilities_and_gates_observer_flag_registered_default_false` | Confirms the new C-193 flag is registered with `bool.FalseString`. |
| `ProductTruth_capabilities_and_gates_service_ctor_backward_compatible` | Confirms 1-arg and 2-arg constructors still work and the watchdog parameter remains optional. |
| `ProductTruth_capabilities_and_gates_snapshot_flag_off_does_not_invoke_watchdog` | Confirms flag-off snapshots do not emit invariant packets. |
| `ProductTruth_capabilities_and_gates_snapshot_flag_on_invokes_watchdog_exactly_once` | Confirms one flag-on snapshot emits exactly one expected watchdog packet. |
| `ProductTruth_capabilities_and_gates_snapshot_flag_on_null_watchdog_does_not_throw` | Confirms flag-on with null watchdog preserves normal snapshot behavior. |
| `ProductTruth_capabilities_and_gates_service_source_does_not_call_scan_or_emit_directly` | Confirms the service calls only `ScanAndEmit`, not direct `Scan` or `EmitInvariantCheckFailedPackets`. |
| `ProductTruth_capabilities_and_gates_no_new_audit_packet_kind` | Confirms `KnownPacketKinds` remains at 160. |
| `ProductTruth_capabilities_and_gates_service_source_references_killswitch` | Confirms the host remains KillSwitch-participating. |
| `ProductTruth_watchdog_allowed_external_facade_producers_are_activity_service_and_capabilities_and_gates_under_flag` | Renamed/extended, not new: C-189 external facade producer allowlist now names ActivityService and CapabilitiesAndGatesService. |
| `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_activity_service_and_capabilities_and_gates` | Renamed/extended, not new: C-191 source/tool caller allowlist now names watchdog, ActivityService, and CapabilitiesAndGatesService. |

## Safety Boundaries

- New flag defaults false: `snapshot_v0_invariant_observer_in_capabilities_and_gates_enabled`.
- No new packet kind.
- No `PlainLanguageExplainer.KnownPacketKinds` change.
- No audit-ledger schema change.
- No UI change.
- No apply runner source change.
- No rollback runner source change.
- No scheduler, judge, replay, eval, scoring, pet, sprite, asset, tool, or solution change.
- `CapabilitiesAndGatesService` never calls `Scan` or `EmitInvariantCheckFailedPackets` directly; only `ScanAndEmit`.
- `ScanAndEmit` remains the facade for host wiring.
- Auto-continue: No.

## Validation

| Command | Result |
| --- | --- |
| `dotnet build .\vnext\Wevito.VNext.sln` | PASS: 0 warnings, 0 errors. |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | PASS: 1698/1698. |
| `--filter "CapabilitiesAndGatesServiceObserverWiring"` | PASS: 8/8. |
| `--filter "PostC191WatchdogObserverProductTruth"` | PASS: 16/16. |
| `--filter "PostC189WatchdogProductTruth"` | PASS: 15/15. |
| `--filter "ApplyRunnerActivityServiceObserverWiring"` | PASS: 8/8. |
| `--filter "InvariantViolationWatchdogScanAndEmitV0"` | PASS: 3/3. |
| `--filter "InvariantViolationWatchdogV0"` | PASS: 17/17. |
| `--filter "CapabilityFlagInventory"` | PASS: 4/4. |
| `--filter "PlainLanguage"` | PASS: 272/272. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | PASS. |
| `git diff --check` | PASS. |

## Stop-Gate Checklist

- [x] Branch created from `gate/post-c-phase-192`, not local `main`.
- [x] No `git checkout main`.
- [x] No `git pull` on main.
- [x] No file under `.codex\worktrees\` touched.
- [x] No IDE or generic `dotnet` process killed.
- [x] New flag defaults false.
- [x] No new packet kind.
- [x] No existing flag flipped.
- [x] C-189/C-191 caller allowlists extended explicitly by name.
- [x] All targeted regression filters pass.
- [x] Full suite passes at 1698/1698.
- [x] Auto-continue: No.

## Next Phase

To be decided by user; Auto-continue: No.
