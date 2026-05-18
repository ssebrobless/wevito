# C-PHASE 141 - Evidence Dashboard v1

Date: 2026-05-18
Branch: `claude-implementation/c-phase-141-evidence-dashboard-v1`

## Goal

Add a read-only Evidence tab to the Tool Hub so the user can quickly answer:

- What did Wevito do?
- What did Wevito not do?
- Why was an action refused or blocked?
- Did anything use network, hosted AI, local model, or mutation paths?

## Scope

Implemented a local audit-ledger summary over the existing SQLite audit table. The dashboard groups recent audit packets by known packet kind and shows counts for mutations, network use, hosted-AI use, local-model use, and refusals.

The dashboard supports:

- Date range presets: last 24 hours, last 7 days, last 30 days, and all.
- Packet caps: 100, 250, 500, and 1000.
- JSON export to `vnext/artifacts/c-phase-141-evidence-dashboard/` only.
- Unknown packet-kind reporting without claiming those packets as known dashboard rows.

## Implemented

- Added `EvidenceSummaryService` as a read-only audit rollup service.
- Added `evidence_dashboard_summary_exported` to `PlainLanguageExplainer.KnownPacketKinds`.
- Added an `Evidence` Tool Hub tab and catalog entry.
- Wired the shell to render Evidence summaries using the current settings snapshot.
- Added export handling that writes only to the phase artifact folder and records an honest audit packet.
- Added tests for rollups, date ranges, packet caps, read-only SQL behavior, export boundaries, KillSwitch behavior, and plain-language packet registration.

## Safety Boundaries

- No hosted AI calls.
- No network access.
- No local model calls.
- No runtime sprite, source sprite, content manifest, or prop-anchor mutation.
- Audit-ledger summary uses SQLite read-only mode.
- New summary query code issues only `SELECT`.
- Export is user-triggered only and is restricted to `vnext/artifacts/c-phase-141-evidence-dashboard/`.
- KillSwitch blocks summary and export.
- Unknown packet kinds are hidden from claimed rollup rows and surfaced only as unknown counts/names.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvidenceSummary|PlainLanguage"`: 221/221 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1052/1052 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed, with only normal CRLF working-copy warnings.

## Stop-Gate Checklist

- [x] No hosted AI call added or observed in this phase.
- [x] No network access added or required.
- [x] No silent file/tool/code/asset mutation added.
- [x] No capability flag default changed.
- [x] AuditLedger remains append-only; summary code is read-only.
- [x] Export writes only to the phase artifact folder.
- [x] KillSwitch blocks summary/export.
- [x] Validation commands passed.

## Next Phase

Auto-continue is No. Stop after opening the C-PHASE 141 PR and wait for review before starting C-PHASE 142.
