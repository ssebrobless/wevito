# C-PHASE 133 - Autonomous Loop Scope Gate

## Goal

Add the first named autonomous-loop scopes while keeping them default-off, review-first, and safe:

- `sprite-repair-triage`
- `audit-ledger-cleanup`

## Scope

Implemented C-PHASE 133 as a narrow code-side gate. This phase does not mutate sprite art, does not approve or execute task cards, does not call hosted AI, and does not add any network surface.

## Implemented

- Added `AutonomousScopeService`, `AutonomousScopeRegistry`, and scope descriptors with per-scope setting keys:
  - `autonomous_scope_sprite-repair-triage_enabled`
  - `autonomous_scope_audit-ledger-cleanup_enabled`
- Registered the two scopes with `AutonomousOperationsLoop`.
- Kept both scopes disabled by default.
- Added `sprite-repair-triage` scope:
  - Reads the C-PHASE 128 repair queue.
  - Drafts review-only task cards for P0/P1 rows.
  - Uses `ToolFamily=sprite-repair-batch-proposal`.
  - Keeps policy disabled and approval-required.
  - Does not preview, execute, approve, or mutate sprites.
- Added `audit-ledger-cleanup` scope:
  - Moves only old already-archived `*.jsonl` audit files into an `archive` folder.
  - Does not delete files.
  - Does not edit file contents.
  - Records sha256 evidence in the cleanup summary artifact.
- Added Tool Popup settings toggles and status text for the two scopes.
- Added audit packets and plain-language explanations:
  - `autonomous_scope_enabled_changed`
  - `autonomous_scope_tick`
  - `sprite_repair_triage_card_drafted`
  - `audit_ledger_cleanup_summary`

## Safety Boundaries

- Both scopes require the global autonomous beta setting and their individual scope setting.
- KillSwitch blocks scope execution through `AutonomousScopeService`.
- Sprite repair triage drafts cards only; no sprite runtime/source PNG mutation is possible through this scope.
- Audit cleanup is limited to archived JSONL files older than 30 days under the audit root and uses `File.Move`, never delete.
- Evidence packet flags are honest:
  - sprite triage card drafts: no network, no hosted AI, no local model, no mutation.
  - audit cleanup summary: mutation flag is true only when at least one archived file is moved.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AutonomousScope|SpriteRepairTriage|AuditLedgerCleanup|PlainLanguage"` passed: 199/199.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 993/993.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
- `git diff --check` passed with only normal CRLF warnings.

## Stop Gates

- No hosted AI call added or observed.
- No silent network access added.
- No sprite mutation path added.
- No task-card approval or execution added.
- No capability flag defaults were flipped on.
- KillSwitch remains consulted before scope execution.
- AuditLedger remains append-only; cleanup operates on archived JSONL files beside the ledger, not the SQLite audit ledger rows.

## Next Phase

Auto-continue is disabled for C-PHASE 133. The next step is user review of the PR before any new autonomous-loop expansion.
