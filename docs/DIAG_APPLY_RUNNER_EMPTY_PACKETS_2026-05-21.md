# DIAG: Apply/Rollback Runner Emits Empty Packet List on origin/main (2026-05-21)

## Background

Critical bug board PR #286 documented five apply/rollback audit-packet tests failing on `origin/main` after a cold-cache rebuild. The failing tests all assert that the narrow apply-v0 and explicit-rollback runners emit a specific audit packet sequence, but the observed packet list is empty.

This diagnostic is read-only. No source, test, solution, tool, or runtime file was changed. The goal is to identify the root cause and describe the smallest safe Phase F1 fix scope.

## Reproduction

Pre-flight confirmed PR #286 was merged before this diagnostic began:

- `gh pr view 286 --json state,mergedAt` returned `state=MERGED`, `mergedAt=2026-05-21T05:40:46Z`.
- `git log origin/main --oneline -5` showed `74ba89b9e Merge pull request #286 from ssebrobless/docs/bug-board-2026-05-21-apply-runner-regression`.
- `dotnet build .\vnext\Wevito.VNext.sln` passed before the targeted diagnostic test runs.

## Determinism Check (3 runs + isolated run)

The five-test filter was run three times with detailed console logging. The same five tests failed on all three runs, and each failure reported `Actual: []`.

```
╔══════════════╦══════════════════════════════════════════════════════════════════╗
║ Run          ║ Result                                                           ║
╠══════════════╬══════════════════════════════════════════════════════════════════╣
║ Run 1        ║ 5 total, 5 failed; all observed packet lists were empty          ║
║ Run 2        ║ 5 total, 5 failed; all observed packet lists were empty          ║
║ Run 3        ║ 5 total, 5 failed; all observed packet lists were empty          ║
║ Isolated run ║ Apply_happy_path_writes_six_packets_in_order failed alone       ║
╚══════════════╩══════════════════════════════════════════════════════════════════╝
```

The isolated run rules out cross-test pollution for the simplest failing test. The failure is deterministic in the current environment.

## Failing Tests (5)

- `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_happy_path_writes_six_packets_in_order`
- `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_backup_sha256_mismatch_rolls_back_emits_no_completed`
- `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_post_proof_mismatch_rolls_back_destination_back_to_source`
- `Wevito.VNext.Tests.ArtifactRenameApplyRunnerTests.Apply_kill_switch_activated_between_backup_and_post_proof_rolls_back`
- `Wevito.VNext.Tests.ArtifactRenameRollbackRunnerTests.ExplicitRollback_happy_path_writes_started_and_completed_packets`

## Code Under Test

### Apply Runner

`ArtifactRenameApplyRunner.Apply(...)` performs preflight validation before emitting any apply-v0 packets. The relevant guards include the kill switch, seven required capability flags, prerequisite check result, extension rules, path traversal checks, source/destination confinement, source existence, reparse point refusal, destination non-existence, approval-token match, and scope-hash match.

Relevant packet `AuditLedgerService.Record(...)` call sites in `ArtifactRenameApplyRunner.cs`:

- `ApplyV0DryRunStarted` at line 124.
- `ApplyV0DryRunCompleted` at line 138.
- `ApplyV0RolledBack` for backup mismatch at line 157.
- `ApplyV0BackupWritten` at line 161.
- `ApplyV0RolledBack` for rename exception at line 180.
- `ApplyV0Applied` at line 184.
- `ApplyV0RolledBack` for post-proof mismatch at line 197.
- `ApplyV0PostProofCompleted` at line 201.
- `ApplyV0Completed` at line 202.
- `ApplyV0RolledBack` from `RollBackAfterBackup(...)` at line 326.

The apply runner's private `Record(...)` method stamps every packet with `DateTimeOffset.UtcNow` at line 433. It does not use the injected `_clock` for audit packet timestamps.

### Rollback Runner

`ArtifactRenameRollbackRunner.ExplicitRollback(...)` performs similar guard checks before the explicit rollback path emits packets. The relevant guards include kill switch, rollback/apply design flags, prerequisite check result, extension rules, path traversal checks, source/destination confinement, source existence, reparse point refusal, destination non-existence, approval-token match, and scope-hash match.

Relevant packet `AuditLedgerService.Record(...)` call sites in `ArtifactRenameRollbackRunner.cs`:

- `ApplyV0ExplicitRollbackStarted` at lines 120-126.
- `ApplyV0ExplicitRollbackCompleted` at lines 163-169.
- `ApplyV0ExplicitRollbackRefused` at lines 278-289.

The rollback runner's private `Record(...)` method stamps every packet with `DateTimeOffset.UtcNow` at line 367. It does not use the injected `_clock` for audit packet timestamps.

### Audit Ledger Capture Path

The tests construct a real `AuditLedgerService` against a temporary SQLite database. `AuditLedgerService.Record(...)` inserts rows into `audit_ledger` with the packet's `CreatedAtUtc` value. `AuditLedgerService.Snapshot(sinceUtc, untilUtc)` reads rows where `created_at_utc >= $since AND created_at_utc <= $until`.

The test readback helpers use fixed date windows:

- `ArtifactRenameApplyRunnerTests.ApplyRows()` reads `Snapshot(2026-05-19T00:00:00Z, 2026-05-21T00:00:00Z)`.
- `ArtifactRenameRollbackRunnerTests.RollbackRows()` reads `Snapshot(2026-05-19T00:00:00Z, 2026-05-21T00:00:00Z)`.

Since this diagnostic ran on 2026-05-21 after midnight UTC, packets stamped with real `DateTimeOffset.UtcNow` can be inserted after the fixed `2026-05-21T00:00:00Z` upper bound. The runner can therefore emit packets that the test helpers immediately filter out.

## Per-Test Q1-Q9 Analysis

### 1. Apply_happy_path_writes_six_packets_in_order

- Q1. The test asserts this order: `ApplyV0DryRunStarted`, `ApplyV0DryRunCompleted`, `ApplyV0BackupWritten`, `ApplyV0Applied`, `ApplyV0PostProofCompleted`, `ApplyV0Completed`.
- Q2. These packets are emitted in `ArtifactRenameApplyRunner.cs` at lines 124, 138, 161, 184, 201, and 202.
- Q3. Guards before the emit calls include kill switch inactive, all seven apply-v0 flags true, prerequisite result met, JSON extension allowed, relative path/scope confinement, source exists, no reparse point, destination absent, approval token match, and scope hash match.
- Q4. The test fixture constructs `Ledger = new AuditLedgerService(Path.Combine(Root, "ledger.sqlite"))` in `ArtifactRenameApplyRunnerTests.cs` lines 384-387.
- Q5. The test reads via `ApplyRows()`, which calls `Ledger.Snapshot(...)` on the same `Ledger` instance and filters `self_improvement_apply_v0_` rows.
- Q6. The fixture uses a fixed clock of `2026-05-20T00:00:00Z` for runner construction, but the readback window ends at `2026-05-21T00:00:00Z`.
- Q7. The runner records packet timestamps with real `DateTimeOffset.UtcNow`, not the injected fixed clock.
- Q8. The test uses `AllFlagsTrue()`, which sets every required apply-v0 flag to `True`.
- Q9. The evidence does not point to a guard returning early: the failure is the readback packet list, and the readback helper's fixed date window can exclude successfully inserted rows after `2026-05-21T00:00:00Z`.

### 2. Apply_backup_sha256_mismatch_rolls_back_emits_no_completed

- Q1. The test asserts this order: `ApplyV0DryRunStarted`, `ApplyV0DryRunCompleted`, `ApplyV0RolledBack`.
- Q2. These packets are emitted in `ArtifactRenameApplyRunner.cs` at lines 124, 138, and 157 for the backup-sha256 mismatch path.
- Q3. Guards before the emit calls are the same apply preflight guards as the happy path. The mismatch is injected through `backupSha256Override`, causing the backup mismatch rollback path after dry-run packets are emitted.
- Q4. The test fixture constructs the `AuditLedgerService` in `ArtifactRenameApplyRunnerTests.cs` lines 384-387.
- Q5. The test reads via `ApplyRows()` from the same ledger instance, but inside the fixed `2026-05-19T00:00:00Z` to `2026-05-21T00:00:00Z` window.
- Q6. The fixture clock is fixed to `2026-05-20T00:00:00Z`, while the readback window upper bound is fixed at midnight on 2026-05-21.
- Q7. The runner's audit packet `CreatedAtUtc` uses real `DateTimeOffset.UtcNow`.
- Q8. The test uses `AllFlagsTrue()`, setting all required apply-v0 flags to `True`.
- Q9. No evidence shows a new default-off flag blocking execution; the empty list is explained by the same timestamp/readback-window mismatch.

### 3. Apply_post_proof_mismatch_rolls_back_destination_back_to_source

- Q1. The test asserts this order: `ApplyV0DryRunStarted`, `ApplyV0DryRunCompleted`, `ApplyV0BackupWritten`, `ApplyV0Applied`, `ApplyV0RolledBack`.
- Q2. These packets are emitted in `ArtifactRenameApplyRunner.cs` at lines 124, 138, 161, 184, and 197 for the post-proof mismatch rollback path.
- Q3. Guards before the emit calls are the apply preflight guards. The post-proof mismatch is caused by the test's `postProofSha256Override`.
- Q4. The same fixture-level `AuditLedgerService` is constructed in `ArtifactRenameApplyRunnerTests.cs` lines 384-387.
- Q5. The test reads via `ApplyRows()` from the same ledger instance and filters apply-v0 rows by packet kind.
- Q6. The fixture fixed clock is `2026-05-20T00:00:00Z`; the readback upper bound is `2026-05-21T00:00:00Z`.
- Q7. The runner records audit packet timestamps using real `DateTimeOffset.UtcNow`.
- Q8. The test uses `AllFlagsTrue()`.
- Q9. The result path reaches rollback behavior in the test assertion context; the packet list is empty because the readback helper can exclude packets emitted after the fixed upper-bound timestamp.

### 4. Apply_kill_switch_activated_between_backup_and_post_proof_rolls_back

- Q1. The test asserts this order: `ApplyV0DryRunStarted`, `ApplyV0DryRunCompleted`, `ApplyV0BackupWritten`, `ApplyV0RolledBack`.
- Q2. These packets are emitted in `ArtifactRenameApplyRunner.cs` at lines 124, 138, 161, and 326. The backup and rollback records use bypass behavior so they can be written even when the kill switch is activated mid-flight.
- Q3. The initial preflight requires kill switch inactive and all apply guards passing. The test then activates the kill switch during `onAfterBackup`. The later rollback record uses `bypassKillSwitch: true`.
- Q4. The same fixture-level `AuditLedgerService` is constructed in `ArtifactRenameApplyRunnerTests.cs` lines 384-387.
- Q5. The test reads via `ApplyRows()` from the same ledger instance.
- Q6. The fixture fixed clock is `2026-05-20T00:00:00Z`; the readback upper bound is fixed at `2026-05-21T00:00:00Z`.
- Q7. The runner records audit packet timestamps using real `DateTimeOffset.UtcNow`, including bypassed rollback packets.
- Q8. The test uses `AllFlagsTrue()` before invoking the runner.
- Q9. The kill-switch behavior is intentional mid-flight, not a preflight early return. The same fixed readback window explains why even bypassed records are not visible to the assertion.

### 5. ExplicitRollback_happy_path_writes_started_and_completed_packets

- Q1. The test asserts this order: `ApplyV0ExplicitRollbackStarted`, `ApplyV0ExplicitRollbackCompleted`.
- Q2. These packets are emitted in `ArtifactRenameRollbackRunner.cs` at lines 120-126 and 163-169.
- Q3. Guards before the emit calls include kill switch inactive, rollback/apply design flags true, prerequisite result met, JSON extension allowed, relative path/scope confinement, source exists, no reparse point, destination absent, approval token match, and scope hash match.
- Q4. The test fixture constructs `Ledger = new AuditLedgerService(Path.Combine(Root, "ledger.sqlite"))` in `ArtifactRenameRollbackRunnerTests.cs` lines 325-328.
- Q5. The test reads via `RollbackRows()`, which calls `Ledger.Snapshot(...)` on the same ledger instance and filters rows whose packet kind contains `explicit_rollback`.
- Q6. The fixture uses a fixed clock of `2026-05-20T00:00:00Z`, while `RollbackRows()` uses the same fixed window ending at `2026-05-21T00:00:00Z`.
- Q7. The rollback runner records packet timestamps using real `DateTimeOffset.UtcNow`.
- Q8. The test uses `AllFlagsTrue()`, setting rollback and prerequisite apply flags to `True`.
- Q9. The evidence points to readback-window exclusion rather than a guard returning early.

## Root Cause Hypothesis

- **Hypothesis:** The five tests fail because `ApplyRows()` and `RollbackRows()` read only through `2026-05-21T00:00:00Z`, while the apply and rollback runners stamp audit packets with real `DateTimeOffset.UtcNow`; on 2026-05-21 after midnight UTC, emitted rows fall outside the tests' fixed snapshot window and the observed packet lists become empty.
- **Evidence for:** The same five tests fail deterministically across three runs; the simplest apply happy-path test fails in isolation; every failure reports `Actual: []`; the tests use the same `AuditLedgerService` instance for runner writes and readback; `AuditLedgerService.Snapshot(...)` filters by `created_at_utc <= $until`; both readback helpers use `untilUtc = 2026-05-21T00:00:00Z`; both runners stamp emitted packets with `DateTimeOffset.UtcNow` rather than the injected fixed clock.
- **Evidence against / unknowns:** This read-only diagnostic did not modify code to dump the SQLite rows outside the fixed window. That would be a useful confirmation but would require an additional diagnostic action beyond the current no-scratch/no-source constraint.
- **Confidence:** High.

## Recommended Fix Scope (Phase F1, description only)

Phase F1 should be test-only unless reviewers deliberately want production audit timestamps to use the injected clock. The smallest safe fix is to adjust the readback helpers in:

- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs`

Recommended change: widen the `Snapshot(...)` upper bound used by `ApplyRows()` and `RollbackRows()` to include real-time audit rows, for example `DateTimeOffset.UtcNow.AddMinutes(1)` or another future-safe bound, while preserving packet-kind filtering and `OrderBy(row => row.Id)`.

Estimated size: 2-6 test lines. No production source change is required. No capability-flag default change is required. No audit packet kind change is required. No `PlainLanguageExplainer` update is required.

## Files Read

- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs`
- `vnext/src/Wevito.VNext.Core/AuditLedgerService.cs`
- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Sandbox/MutationScopeGuard.cs`
- `vnext/src/Wevito.VNext.Core/KillSwitchService.cs`

## Validation Performed

- Confirmed PR #286 was merged before Phase D1 began.
- Confirmed `dotnet build .\vnext\Wevito.VNext.sln` passed before the diagnostic test runs.
- Ran the five failing tests three times with detailed logging. All three runs failed the same five tests with `Actual: []`.
- Ran `Apply_happy_path_writes_six_packets_in_order` alone. It failed in isolation with `Actual: []`.
- Reviewed the scoped test, runner, ledger, packet-kind, capability-flag, mutation-scope, and kill-switch files listed above.

## Open Questions / Next Diagnostic Steps If Needed

If reviewers want one more proof point before Phase F1, a narrow follow-up diagnostic could inspect the temporary SQLite ledger after a failing run with a wider timestamp query and confirm that the expected rows exist outside the fixed test window. That would still be read-only, but it was not performed in this phase because the current prompt forbids scratch scripts and limits the PR to one diagnostic doc.
