# C-PHASE 190 - Watchdog Product-Truth Pin

## Goal

Pin the post-C-PHASE 189 `InvariantViolationWatchdog` surface with deterministic product-truth tests, without changing production source, adding runtime behavior, enabling flags, or introducing any new packet kind.

## Scope

Files added:

- `vnext/tests/Wevito.VNext.Tests/PostC189WatchdogProductTruthTests.cs`
- `docs/C_PHASE190_WATCHDOG_PRODUCT_TRUTH_2026-05-21.md`

Files NOT touched:

- No files under `vnext/src/`.
- No files under `vnext/tools/`.
- No repo-root `tools/` files.
- No `vnext/Wevito.VNext.sln`.
- No UI, pet, sprite, asset, held-out eval, in-distribution eval, runner, scheduler, loop, judge, replay, snapshot, scoring, or eval runtime surface.

## Implemented

| Fact name | Assertion summary |
| --- | --- |
| `ProductTruth_invariant_watchdog_type_exists_in_invariants_namespace` | Confirms `InvariantViolationWatchdog` is loadable at `Wevito.VNext.Core.SelfImprovement.Invariants.InvariantViolationWatchdog`. |
| `ProductTruth_new_c189_packet_kind_string_value_unchanged` | Pins `SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed` to `self_improvement_apply_v0_invariant_check_failed`. |
| `ProductTruth_new_c189_packet_kind_registered_in_plain_language_explainer` | Confirms the C-189 packet kind remains in `PlainLanguageExplainer.KnownPacketKinds`. |
| `ProductTruth_new_c189_packet_kind_has_nonempty_plain_language_sentence` | Pins the explainer sentence and verifies terminal punctuation. |
| `ProductTruth_new_c189_capability_flag_default_false` | Confirms `apply_v0_invariant_check_emit_enabled` defaults `bool.FalseString`. |
| `ProductTruth_new_c189_capability_flag_not_flipped_by_any_producer` | Scans source files outside the inventory/watchdog and rejects suspicious true/enable patterns around the C-189 flag. |
| `ProductTruth_supervised_loop_apply_runner_not_implemented_reason_unchanged` | Confirms `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` remains `apply_runner_not_implemented_in_v0`. |
| `ProductTruth_legacy_apply_completed_constant_unchanged` | Confirms legacy `SelfImprovementPacketKinds.ApplyCompleted` remains `self_improvement_apply_completed`. |
| `ProductTruth_watchdog_source_has_no_mutating_sql` | Confirms watchdog source contains no `INSERT INTO`, `UPDATE`, `DELETE FROM`, `DROP TABLE`, `ALTER TABLE`, or `TRUNCATE`. |
| `ProductTruth_watchdog_source_has_no_network_imports` | Confirms watchdog source does not import `System.Net.Http` or `System.Net.Sockets`. |
| `ProductTruth_watchdog_source_has_no_held_out_or_in_distribution_eval_references` | Confirms watchdog source does not reference held-out or in-distribution eval store terms. |
| `ProductTruth_watchdog_source_has_no_ui_pet_or_sprite_references` | Confirms watchdog source does not reference UI, pet, or sprite namespace roots. |
| `ProductTruth_watchdog_has_no_producer_in_src_tree` | Confirms no source file outside the watchdog invokes `EmitInvariantCheckFailedPackets`. |
| `ProductTruth_watchdog_audit_ledger_usage_pinned_to_append_only_or_absent` | Pins the actual C-189 mode: append-only `AuditLedgerService.Record` writes plus read-only SQLite scan mode. |
| `ProductTruth_watchdog_kill_switch_behavior_pinned` | Pins the actual C-189 mode: watchdog accepts `KillSwitchService` and checks `IsActive()`. |

## Safety Boundaries

- No production source changes.
- No model call.
- No network call.
- No held-out or in-distribution eval content read.
- No new capability flag.
- No new audit packet kind.
- No new apply behavior.
- No UI change.
- No mutation beyond reading source files from the test process.

## Validation

| Command | Result |
| --- | --- |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PostC189WatchdogProductTruth"` | Passed: 15/15 |
| `dotnet build .\vnext\Wevito.VNext.sln` | Passed: 0 warnings, 0 errors |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1663/1663 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed |
| `git diff --check` | Passed |
| Focused re-run `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "PostC189WatchdogProductTruth"` | Passed: 15/15 |

## Stop-Gate Checklist

- [x] Zero files under `vnext/src/` changed.
- [x] Zero files under `vnext/tools/` or repo-root `tools/` changed.
- [x] `vnext/Wevito.VNext.sln` unchanged.
- [x] No new capability flag added; no existing flag flipped.
- [x] No new packet kind added.
- [x] No runner, scheduler, loop, judge, replay, snapshot, scoring, or eval surface invoked from the new test class.
- [x] Test class does not import `System.Net.Http` or `System.Net.Sockets`.
- [x] Test class does not reference held-out or in-distribution eval stores/cases.
- [x] Phase report doc present.
- [x] Auto-continue: No.

## Next Phase

C-PHASE 191 candidate: Option B from the C-190 plan (read-only observer wiring of InvariantViolationWatchdog behind a NEW default-false flag). Auto-continue: No.
