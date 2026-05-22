# C-PHASE 192: Post-C-191 Watchdog Observer Product-Truth Retest

## Goal

C-PHASE 192 adds a test-only product-truth retest for the C-PHASE 191 watchdog observer wiring. It pins the `ScanAndEmit` facade, the default-false observer flag, the allowed ActivityService facade caller, the existing C-189/C-191 regression test shapes, and the no-new-packet/no-new-runtime behavior boundaries.

KillSwitch in this codebase is a centralized service that consumers query, not a registry that enumerates consumers. Fact #16 therefore pins the inverse direction: that the watchdog source and the activity service source each reference `KillSwitch`. The pre-existing behavioral tests (`InvariantViolationWatchdogV0` and `ApplyRunnerActivityServiceObserverWiring`) cover the runtime-obedience side.

## Scope

Files added:

| Path | Purpose |
| --- | --- |
| `vnext/tests/Wevito.VNext.Tests/PostC191WatchdogObserverProductTruthTests.cs` | Adds 16 product-truth facts for the post-C-191 watchdog observer surface. |
| `docs/C_PHASE192_POST_C191_WATCHDOG_OBSERVER_PRODUCT_TRUTH_2026-05-21.md` | Phase report. |

Files not touched:

| Area | Status |
| --- | --- |
| `vnext/src/` | No production source change. |
| `vnext/tools/` | No tool change. |
| Existing tests | No existing test file modified. |
| `vnext/Wevito.VNext.sln` | Unchanged. |
| Capability flags | No new flag; no flag flipped. |
| Packet kinds | No new packet kind. |
| Runtime behavior | No new runtime behavior. |

## Implemented

| Fact | Assertion summary |
| --- | --- |
| `ProductTruth_watchdog_scan_and_emit_method_exists_and_is_public` | Confirms `InvariantViolationWatchdog.ScanAndEmit(DateTimeOffset)` is public, instance, and returns `IReadOnlyList<InvariantCheckResult>`. |
| `ProductTruth_watchdog_scan_and_emit_body_references_scan_and_emit_in_source` | Confirms the facade body calls existing `Scan` and `EmitInvariantCheckFailedPackets`. |
| `ProductTruth_apply_v0_invariant_observer_flag_exists_default_false` | Confirms `apply_v0_invariant_observer_in_activity_service_enabled` exists and defaults false. |
| `ProductTruth_apply_runner_activity_service_ctor_accepts_optional_watchdog` | Confirms ActivityService still accepts an optional null-default watchdog parameter. |
| `ProductTruth_apply_runner_activity_service_does_not_call_scan_or_emit_directly_in_source` | Confirms ActivityService calls only `ScanAndEmit`, not `Scan` or `EmitInvariantCheckFailedPackets` directly. |
| `ProductTruth_only_emit_invariant_check_failed_packets_caller_in_src_and_tools_is_watchdog_itself` | Confirms no source/tool caller of `EmitInvariantCheckFailedPackets(` outside the watchdog source. |
| `ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_and_activity_service` | Confirms `ScanAndEmit(` appears only in the watchdog and ActivityService under source/tool scans. |
| `ProductTruth_post_c189_watchdog_product_truth_file_still_has_fifteen_facts` | Confirms the C-190 product-truth file still has 15 facts. |
| `ProductTruth_post_c189_refined_fact_name_still_present` | Confirms the refined C-190 allowed-facade fact still exists. |
| `ProductTruth_legacy_apply_runner_not_implemented_reason_unchanged` | Confirms `apply_runner_not_implemented_in_v0` remains present. |
| `ProductTruth_legacy_apply_completed_packet_kind_constant_unchanged` | Confirms `self_improvement_apply_completed` remains present. |
| `ProductTruth_known_packet_kinds_count_unchanged_by_c191` | Confirms `PlainLanguageExplainer.KnownPacketKinds.Count` remains 160. |
| `ProductTruth_audit_ledger_service_remains_append_only_no_update_delete_drop_sql` | Confirms no direct `UPDATE`, `DELETE FROM`, or `DROP TABLE` SQL statements are present in `AuditLedgerService`. |
| `ProductTruth_apply_runner_activity_service_observer_wiring_tests_file_present_with_eight_facts` | Confirms the C-191 observer wiring test file exists with 8 facts. |
| `ProductTruth_invariant_violation_watchdog_scan_and_emit_v0_tests_file_present_with_three_facts` | Confirms the C-191 ScanAndEmit v0 test file exists with 3 facts. |
| `ProductTruth_watchdog_and_activity_service_both_reference_killswitch_in_source` | Confirms the watchdog and the activity service each reference `KillSwitch` in source (structural participation pin; behavioral obedience is covered by existing C-189/C-191 tests). |

## Safety Boundaries

- TEST-ONLY.
- No production source change.
- No new capability flag.
- No new packet kind.
- No new runtime behavior.
- No model call.
- No network call.
- No held-out or in-distribution eval store/case read.
- No `System.Net.Http` or `System.Net.Sockets` import in the new test class.
- No file under `.codex\worktrees\` touched.
- Auto-continue: No.

## Validation

| Command | Result |
| --- | --- |
| `dotnet build .\vnext\Wevito.VNext.sln` | PASS. |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | PASS: 1690/1690. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | PASS. |
| `git diff --check` | PASS. |
| `--filter "PostC191WatchdogObserverProductTruth"` | PASS: 16/16. |
| `--filter "PostC189WatchdogProductTruth"` | PASS: 15/15. |
| `--filter "InvariantViolationWatchdogScanAndEmitV0"` | PASS: 3/3. |
| `--filter "ApplyRunnerActivityServiceObserverWiring"` | PASS: 8/8. |
| `--filter "InvariantViolationWatchdogV0"` | PASS: 17/17. |

## Stop-Gate Checklist

- [x] Operating in `C:\Users\fishe\Documents\projects\wevito`.
- [x] Never `git checkout main`, never `git pull` on main.
- [x] No file under `.codex\worktrees\` touched.
- [x] No file under `vnext/src/` modified.
- [x] No file under `vnext/tools/` modified.
- [x] No file in `vnext/tests/` modified other than the one new file added.
- [x] No new capability flag, no new packet kind, no new runtime behavior.
- [x] Test class does not import `System.Net.Http` or `System.Net.Sockets`.
- [x] Test class does not reference held-out or in-distribution eval stores/cases.
- [x] Full test count equals baseline test count plus 16.
- [x] All five targeted regression filters pass.
- [x] PR opened with Auto-continue: No.
- [x] Working tree clean.

## Next Phase

C-PHASE 193 candidate: second observer wiring point in another HOST surface. Auto-continue: No.
