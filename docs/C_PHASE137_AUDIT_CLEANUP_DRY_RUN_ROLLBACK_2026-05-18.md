# C-PHASE 137 - Audit-Ledger Cleanup Dry-Run + Rollback

## Goal

Bring the `audit-ledger-cleanup` autonomous scope into the same safety posture as the guarded sprite workflow: preview first, dry-run evidence, exact hash-backed apply, and a rollback tool that can reverse recorded archive moves without editing the SQLite audit ledger.

## Scope

- Added dry-run planning and summary writing to `AuditLedgerCleanupScope`.
- Added hash-rich cleanup summaries under `audit\cleanup-summaries\`.
- Added rollback script `tools/rollback-audit-ledger-cleanup.ps1`.
- Added Pester coverage for the rollback script.
- Added packet kinds to `PlainLanguageExplainer`.

No sprite PNGs, content manifests, runtime assets, local model settings, hosted AI settings, or product capability defaults were changed.

## Implemented

- `AuditLedgerCleanupPlan`, `AuditLedgerCleanupMovePlan`, and `AuditLedgerCleanupSummary` record the planned source path, destination path, pre-move SHA256, post-move SHA256, and age for each candidate archive move.
- `AuditLedgerCleanupScope.ApplyDryRun()` writes a `dry-run` summary artifact and records `audit_ledger_cleanup_dry_run` with `did_mutate=false`.
- `AuditLedgerCleanupScope.ApplyCleanup()` preserves the existing archive behavior while writing an `apply` summary artifact with pre/post hashes.
- `AuditLedgerCleanupScope.TryRun()` now consults the kill switch before applying cleanup.
- `tools/rollback-audit-ledger-cleanup.ps1` restores files from an apply summary with:
  - malformed summary refusal, exit `4`;
  - kill-switch sentinel refusal, exit `3`;
  - destination hash mismatch refusal, exit `2`;
  - missing destination refusal, exit `5`;
  - idempotent success, exit `0`;
  - rollback evidence under `cleanup-summaries\rollback-<summary-id>.json`.
- `tools/rollback-audit-ledger-cleanup.tests.ps1` covers happy path restore, SHA mismatch abort, missing summary abort, and KillSwitch refusal.

## Safety Boundaries

- Dry-run mode does not move or delete files.
- Apply mode still moves only old `*archived*.jsonl` files from the audit root top level into `archive\`.
- Rollback refuses moves whose source or destination is outside the recorded audit root.
- Rollback never deletes files and never edits SQLite audit rows.
- Rollback preflights all destination hashes before moving anything, preventing partial rollback on known-bad inputs.
- Packet flags remain honest: dry-run is non-mutating; apply mutates only when files move; rollback evidence is file-based.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AuditLedgerCleanup|PlainLanguage"`: passed, `209 / 209`.
- `Invoke-Pester .\tools\rollback-audit-ledger-cleanup.tests.ps1`: passed, `4 / 4`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, `1014 / 1014`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\rollback-audit-ledger-cleanup.ps1 -DryRun`: passed, no-op dry-run without summary path.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Dry-run path was not observed moving or deleting files.
- [x] Rollback script aborts on SHA256 mismatch.
- [x] Rollback script refuses paths outside the audit root.
- [x] Apply path retains honest mutation flags.
- [x] No hosted AI, network fetch, model call, sprite mutation, or capability default change was introduced.

## Next Phase

C-PHASE 138 is the local brain runtime status drilldown. It should remain text-guidance-only and must not execute Ollama, winget, curl, or any model setup command.
