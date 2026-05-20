# C-PHASE 180 - Proposal Quality Metrics Service

## Goal

Add a read-only proposal quality metrics surface for supervised self-improvement operations. The service aggregates existing audit-ledger rows and artifact files so the user can inspect proposal health without running checks, writing packets, mutating files, or exposing eval case content.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ProposalQualityMetricsSnapshot.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ProposalQualityMetricsService.cs`
- `vnext/tests/Wevito.VNext.Tests/ProposalQualityMetricsServiceTests.cs`

Extended:

- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreVisibilityTests.cs`

Not touched:

- No audit packet kind was added.
- No capability flag was added.
- No apply runner or mutation behavior was added.
- No held-out or in-distribution eval store API was touched.

## Implemented

- Added `ProposalQualityMetricsSnapshot` carrying packet counts, scope-hash format status, judge pass counts, snapshot age, replay result kind, eval gate coverage, awaiting-approval status, apply-refused count, timestamp, and reason.
- Added `ProposalQualityMetricsService` with constructor `(string databasePath, string artifactRoot, KillSwitchService? killSwitch)`.
- The service opens SQLite with `Mode=ReadOnly`, has no `AuditLedgerService` field, and never writes to the audit ledger.
- Added artifact-root reads for `snapshot*.json`, `replay-result.json`, awaiting-approval artifacts, and eval artifacts.
- Added `ShellCompositionRoot.CreateProposalQualityMetricsService(...)` using the standard `%LOCALAPPDATA%\WevitoVNext\artifacts` root.
- Added ShellCoordinator read-only registration and snapshot passing for the current awaiting-approval operation.
- Added Tool Popup Evidence expander: `Proposal quality metrics (read-only)`, with text-only rows and no Button / ToggleSwitch / CheckBox.
- Added tests for KillSwitch, empty ledger, judge ratio extraction, apply-refused count, partial eval gate coverage, scope-hash/status/snapshot/replay metrics, and the no-`AuditLedgerService` field invariant.
- Extended held-out and in-distribution visibility tests to include the metrics service and snapshot.

## Safety Boundaries

- The service does not take `AuditLedgerService`; it receives only a database path and reads through `SqliteOpenMode.ReadOnly`.
- No audit ledger writes are performed.
- No held-out or in-distribution eval store references are present.
- No model call, network call, training, mutation, or apply behavior was added.
- The Tool Popup expander is display-only and contains no interactive controls.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ProposalQuality|EvalCoverage|PlainLanguage|SelfImprovement"`: passed, 306/306.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1388/1388.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Service reads audit DB and artifact root read-only.
- [x] Service does not take or store `AuditLedgerService`.
- [x] No held-out / in-distribution store reference.
- [x] Tool Popup expander has no interactive control.
- [x] No audit packet kind added.
- [x] No capability flag added.
- [x] No apply behavior added.
- [x] Validation commands passed.

## Next Phase

C-PHASE 181 - Apply-runner still-not-implemented status report - Auto-continue=No.
