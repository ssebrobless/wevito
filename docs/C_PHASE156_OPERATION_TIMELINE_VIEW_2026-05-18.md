# C-PHASE 156: Operation Timeline View

## Goal

Add a read-only Evidence-tab operation timeline that lets the user enter an `operation_id` and inspect the supervised self-improvement packet chain in plain language.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/OperationTimelineRow.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/OperationTimelineService.cs`
- `vnext/tests/Wevito.VNext.Tests/OperationTimelineServiceTests.cs`
- `docs/C_PHASE156_OPERATION_TIMELINE_VIEW_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Not touched:

- No sprite assets.
- No content manifests.
- No model adapters.
- No network surfaces.
- No held-out eval stores or paths.
- No packet producers.
- No apply runner or mutation pathway.

## Implemented

- Added `OperationTimelineService`, a read-only SQLite-backed self-improvement timeline renderer.
- Filtered timeline rows to `packet_kind LIKE 'self_improvement_%'` only.
- Linked rows by operation text in `summary` and by matching `task_card_id` from those operation-bearing rows.
- Rendered rows chronologically with packet kind, status badge, mutation flag, local-model flag, and a plain-language sentence.
- Used `PlainLanguageExplainer` for known packet kinds and `"(unrecognized packet kind)"` for unknown self-improvement packet kinds.
- Added KillSwitch behavior: active KillSwitch returns a single blocked sentinel row and reads no ledger data.
- Added an Evidence-tab `Operation timeline` panel with an operation-id input and read-only grid.
- Stored the panel's operation id in `operation_timeline_operation_id` so the view can refresh during normal shell renders.
- Added tests for chronological chain rendering, unknown packet bucket text, KillSwitch sentinel, held-out/raw text hiding, read-only SQL/no write path, and UI bindings avoiding raw ledger fields.

## Safety Boundaries

- The service does not write audit packets.
- The service has no `AuditLedgerService.Record` path.
- The service opens SQLite in read-only mode.
- The service does not reference `IHeldOutEvalStore` or held-out paths.
- Raw `Summary` and `Error` ledger fields are never surfaced in `PlainLanguage`.
- Unknown self-improvement packet kinds are shown only as `"(unrecognized packet kind)"`.
- The UI panel has no apply, approve, mutate, or capability-toggle button.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "OperationTimeline|PlainLanguage|SelfImprovement"`: passed, 247/247.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1167/1167.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with line-ending warnings only.

## Stop-Gate Checklist

- [x] Service writes no packet.
- [x] Service displays no raw `Summary` or `Error` text.
- [x] Service queries only `self_improvement_%` packet kinds.
- [x] Unknown self-improvement packet kinds render as `"(unrecognized packet kind)"`.
- [x] No model call, network call, held-out eval read, or mutation path was added.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 157: Refused-approval reasons panel. Auto-continue remains No.
