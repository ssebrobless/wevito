# C-PHASE 135 - Autonomous Scope UX And Safety Preview

## Goal

Add a preview-only surface for autonomous scopes so Wevito can show what each background scope would do before any autonomous tick is allowed to run.

## Scope

- Added `AutonomousScopePreview`, preview items, and preview evidence flags.
- Added `IAutonomousScope.DescribePlannedActionsAsync(...)`.
- Added `AutonomousScopeService.PreviewAsync(...)` with KillSwitch-first blocking.
- Added an `autonomous_scope_preview` audit packet kind.
- Added a Tool Popup `Autonomy` tab with scope toggles, recent tick/result text, and `Run Once Now (Preview)` buttons.
- Added tests for non-mutating previews, KillSwitch blocking, persisted flag safety, and plain-language coverage.

## Implemented Behavior

- `sprite-repair-triage` preview lists P0/P1 queue rows that would draft review-only task cards on the next eligible tick.
- `audit-ledger-cleanup` preview lists old archived JSONL files that would move, including source path, destination path, sha256, and approximate age in days.
- Preview does not draft task cards, move files, change scope flags, alter settings, call models, call the network, or mutate assets.
- Preview records `autonomous_scope_preview` only when KillSwitch is not active.
- If KillSwitch is active, preview returns `kill_switch=true` without writing an audit packet.

## Safety Boundaries

- No apply path was added.
- No sprite PNGs, content JSON, runtime assets, or audit JSONL files are mutated by preview.
- No hosted AI, local model, or network calls are made by preview.
- Existing autonomous scope toggles still control real ticking; preview does not bypass the autonomous beta gate for execution.
- `AuditLedgerService` remains append-only.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AutonomousScope|PlainLanguage"`: passed, 205/205.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 999/999.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Preview does not mutate files, settings, or task cards.
- [x] Preview does not call hosted AI or any non-loopback endpoint.
- [x] KillSwitch blocks preview generation before scope inspection.
- [x] No new public preview writer bypasses KillSwitch.
- [x] Evidence flags for preview are false for network, hosted AI, local model, and mutation.

## Next Phase

C-PHASE 136 can build on this preview surface if the next plan wants richer autonomous-scope history, typed confirmation for future apply paths, or live UX proof automation.
