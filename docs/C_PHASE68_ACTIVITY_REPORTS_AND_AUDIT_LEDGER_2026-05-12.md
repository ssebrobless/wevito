# C-PHASE 68: Audit Ledger And Activity Reports

## Goal

Make Wevito's helper activity inspectable from one append-only local ledger and one compact activity summary.

## Scope

- Added an append-only SQLite audit ledger at `%LOCALAPPDATA%/Wevito/audit/ledger.sqlite`.
- Added a generalized `EvidencePacket` record for scheduler, adapter, learning promotion, and eval events.
- Added `ActivitySummaryService` for daily/weekly aggregation by packet kind.
- Routed PET TASKS preview results through the ledger from `PetTaskAdapterPreviewDispatcher`.
- Routed direct evidence producers into the ledger: scheduler proposals, learning promotion, and learning eval.
- Added a text-only Settings activity panel; it does not embed sprite thumbnails or special pet task animations.

## Implemented

- `vnext/src/Wevito.VNext.Core/EvidencePacket.cs`
- `vnext/src/Wevito.VNext.Core/AuditLedgerService.cs`
- `vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs`
- `vnext/tests/Wevito.VNext.Tests/AuditLedgerServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ActivitySummaryServiceTests.cs`
- `vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs`
- `vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs`
- `vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs`
- `vnext/src/Wevito.VNext.Core/LearningEvalService.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

## Safety Boundaries

- Ledger writes are local-only.
- Ledger rows require non-null `did_use_network`, `did_use_hosted_ai`, `did_use_local_model`, and `did_mutate` flags.
- SQLite triggers reject updates and deletes so the ledger is append-only.
- The activity panel is text-only and summarizes existing ledger rows.
- No hosted AI calls, network calls, asset mutation, model calls, training, or sprite changes were added.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AuditLedger|ActivitySummary|Adapter"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

## Next Phase

C-PHASE 69 should add the independent-assistant beta gate and hard kill switch. The kill switch should use the audit ledger as proof input and should block every adapter/service chain when active.
