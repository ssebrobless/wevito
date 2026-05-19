# C-PHASE 165: Audit Ledger Snapshot Export CLI

## Goal

Create a standalone, read-only audit ledger snapshot export tool for self-improvement operation chains. The export is deterministic, redacts sensitive runtime identifiers and free-text ledger content, and includes a SHA-256 signature over the unsigned JSON payload.

Base commit verified before implementation:

- `1475bdc7da54422a8b239b1f2c1a7dc1a0ac62de`

## Scope

Files added:

- `vnext/tools/Wevito.Tools.SnapshotExport/Wevito.Tools.SnapshotExport.csproj`
- `vnext/tools/Wevito.Tools.SnapshotExport/Program.cs`
- `vnext/tests/Wevito.VNext.Tests/SnapshotExportCliTests.cs`

Files extended:

- `vnext/Wevito.VNext.sln`

Files intentionally not touched:

- Shell runtime code
- Core runtime self-improvement services
- Audit ledger schema
- Held-out eval store
- Capability flags
- Sprite/runtime assets
- Model adapters

## Implemented

- Added `Wevito.Tools.SnapshotExport`, a standalone `net8.0` console tool.
- Supported command:
  - `export --db <path> --operation-id <id> --output <file> [--force]`
- Implemented required exit codes:
  - `2` when the database file is missing.
  - `3` when the operation id is empty.
  - `4` when the output file exists and `--force` is not supplied.
- Opened SQLite with `Mode=ReadOnly`.
- Selected only `self_improvement_%` rows that match the operation id directly in `summary` or through a matching `task_card_id` chain.
- Ordered rows by `created_at_utc ASC, id ASC`.
- Redacted exported row fields:
  - `packet_id`
  - `task_card_id`
  - `created_at_utc`
  - `summary`
  - `error`
- Wrote JSON with:
  - `schemaVersion`
  - `scope`
  - `operation_id`
  - `row_count`
  - `rows`
  - `snapshot_sha256`
- Computed `snapshot_sha256` over the UTF-8 JSON bytes with the signature field empty, then inserted the final hash.
- Added solution membership for the tool project without referencing it from Shell or Tests.
- Added tests covering successful deterministic export, exit codes, redaction, signature verification, read-only SQL guardrails, and forbidden held-out/network surfaces.

## Safety Boundaries

- No hosted AI calls.
- No model calls.
- No network calls.
- No audit database writes.
- No mutation runner.
- No capability flag changes.
- No held-out eval store access.
- No Shell/Core runtime dependency on the export CLI.
- Raw `summary` and `error` ledger text is not surfaced in the exported snapshot.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SnapshotExport|OperationTimeline|PlainLanguage|SelfImprovement"`: passed, `256/256`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, `1241/1241`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] CLI is not referenced from Shell/Core runtime.
- [x] CLI does not write to the audit database.
- [x] CLI does not surface raw `summary` or `error` text.
- [x] CLI computes the signature with the signature field empty.
- [x] Pre-flight and final validation commands pass.

## Next Phase

C-PHASE 166 — Snapshot import verifier — Auto-continue=No.
