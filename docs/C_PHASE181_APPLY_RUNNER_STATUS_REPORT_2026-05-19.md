# C-PHASE 181 - Apply-runner still-not-implemented status report

## Goal

Add a default-off, explicitly emitted status report that proves Wevito still has no supervised self-improvement apply runner in v0, plus read-only surfaces for reviewing the latest report.

## Scope

Added:
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReport.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReportService.cs`
- `vnext/tools/Wevito.Tools.ApplyRunnerStatusReport/`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerStatusReportServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerStatusReportCliTests.cs`

Extended:
- `SelfImprovementPacketKinds`, `PlainLanguageExplainer`, and `CapabilityFlagInventory`
- `ShellCompositionRoot`, `ShellCoordinator`, and `ToolPopupWindow`
- held-out and in-distribution eval visibility lockdown tests
- `vnext/Wevito.VNext.sln`

Not touched:
- no apply runner
- no model adapter
- no network surface
- no eval store read path
- no sprite or content asset

## Implemented

- Added packet kind `self_improvement_apply_runner_status_report`.
- Added capability flag `apply_runner_status_report_enabled`, default `False`.
- Added `ApplyRunnerStatusReportService.EmitReport`, which writes exactly one status packet only when the flag is enabled and KillSwitch is inactive.
- Added `ApplyRunnerStatusReportService.ReadLatest`, which reads the latest report from the audit ledger without writing.
- Added CLI `Wevito.Tools.ApplyRunnerStatusReport report [--emit]`.
- Added Evidence-tab expander `Apply runner status (read-only)`.
- Added tests proving flag-off and KillSwitch paths write no packet, flag-on emits one packet, read-latest works, CLI behavior matches exit codes, and the CLI does not reference SQLite directly.
- Extended visibility tests so status report types cannot depend on held-out or in-distribution eval stores.

## Safety Boundaries

- `apply_runner_implemented` is derived from `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`; it is not hardcoded true.
- The status packet records `DidUseNetwork=false`, `DidUseHostedAi=false`, `DidUseLocalModel=false`, and `DidMutate=false`.
- The Tool Popup expander is read-only and does not call `EmitReport`.
- The CLI does not open SQLite directly; it uses `AuditLedgerService` and `ApplyRunnerStatusReportService`.
- The feature does not approve, apply, mutate, run models, fetch network resources, or read held-out/in-distribution eval content.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerStatusReport|PlainLanguage|SelfImprovement"`: passed, 295/295.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1401/1401.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Report packet never contains `apply_runner_implemented=true`.
- [x] CLI does not open SQLite directly.
- [x] Tool Popup expander does not auto-invoke `EmitReport`.
- [x] No held-out or in-distribution eval store dependency was added to the status report surface.
- [x] No apply behavior was added.
- [x] No model call, network call, training, or mutation path was added.

## Next Phase

C-PHASE 182 - Apply-runner design draft (doc-only, no implementation, Auto-continue=No).
