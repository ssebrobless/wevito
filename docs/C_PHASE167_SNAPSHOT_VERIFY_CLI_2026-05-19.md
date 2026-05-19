# C-PHASE 167 - Snapshot Verify CLI

## Goal

Add a standalone, read-only verifier for C-PHASE 165 audit-ledger snapshot exports. The verifier accepts a snapshot JSON file, reconstructs the unsigned payload, recomputes the SHA-256 signature, checks row-count consistency, and asserts that every exported row preserves the required honesty/redaction invariants.

## Scope

Files added:

- `vnext/tools/Wevito.Tools.SnapshotVerify/Wevito.Tools.SnapshotVerify.csproj`
- `vnext/tools/Wevito.Tools.SnapshotVerify/Program.cs`
- `vnext/tests/Wevito.VNext.Tests/SnapshotVerifyCliTests.cs`
- `docs/C_PHASE167_SNAPSHOT_VERIFY_CLI_2026-05-19.md`

Files extended:

- `vnext/Wevito.VNext.sln`
- `docs/codex-phase-history.jsonl`

Not touched:

- `vnext/tools/Wevito.Tools.SnapshotExport/Program.cs`
- Production runtime projects and source files.
- Audit DB writer/reader services.
- Model, network, training, sprite, and apply surfaces.

## Implemented

- Added `Wevito.Tools.SnapshotVerify`, a standalone `net8.0` console project with no project references.
- Implemented `verify --snapshot <path>` with deterministic exit codes:
  - `0` pass.
  - `2` snapshot file missing.
  - `3` schema version mismatch.
  - `4` signature mismatch.
  - `5` row count mismatch.
  - `6` invariant violation.
- Reconstructs the unsigned snapshot payload with `WriteIndented=true`, sets `snapshot_sha256` to an empty string, computes lowercase SHA-256 over UTF-8 bytes, and compares that hash to the embedded `snapshot_sha256`.
- Validates each row:
  - `packet_kind` starts with `self_improvement_`.
  - `did_use_hosted_ai`, `did_use_network`, and `did_mutate` are all `false`.
  - `packet_id`, `task_card_id`, `summary`, and `error` are all `redacted`.
  - `created_at_utc` matches `row-<index>`.
- Emits one pass line per row plus a final `verify ok rows=<count> sha256=<hash>` line.
- Added CLI tests for pass, missing file, schema mismatch, signature mismatch, row count mismatch, invariant violation, and source-level guard checks against SQLite/network imports.

The phase prompt asked for valid fixtures generated through the snapshot exporter, but also set a stricter stop gate that verifier tests must not open SQLite. The implementation keeps the stricter safety boundary: verifier tests generate exporter-compatible signed JSON fixtures directly and never open SQLite.

## Safety Boundaries

- The verifier is a standalone tool project and is not referenced by Shell, Core, or Tests.
- The verifier does not import or reference `Microsoft.Data.Sqlite`.
- The verifier does not import `System.Net.Http` or `System.Net.Sockets`.
- The verifier does not open sockets.
- The verifier does not open SQLite.
- The verifier does not write audit rows or mutate any product data.
- Tests do not open sockets and do not require SQLite for verifier fixtures.
- No production runtime type imports `Wevito.Tools.SnapshotVerify`.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SnapshotVerify|SnapshotExport|PlainLanguage|SelfImprovement"` - passed, 259/259.
- `dotnet build .\vnext\Wevito.VNext.sln` - passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` - passed, 1255/1255.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` - passed.
- `git diff --check` - passed.

## Stop-Gate Checklist

- [x] Verifier does not open a network socket.
- [x] Verifier does not open SQLite.
- [x] Verifier does not write to any audit DB.
- [x] Solution has no ProjectReference from Shell/Core/Tests to `Wevito.Tools.SnapshotVerify`.
- [x] No production runtime type imports `Wevito.Tools.SnapshotVerify`.
- [x] Pre-flight commands passed before implementation.

## Next Phase

C-PHASE 168 - Replay runner CLI + read-only replay log viewer - Auto-continue=No.
