# C-PHASE 69: Independent Assistant Beta Gate And Kill Switch

## Goal

Add the review gate that decides whether Wevito can move beyond preview-only behavior, plus a persistent kill switch that blocks helper work immediately.

## Scope

- Added `KillSwitchService` with default-off setting `runtime_kill_switch`.
- Wired the kill switch into preview dispatch, scheduler proposals, learning promotion, learning eval, and local model suggestions.
- Added `IndependentAssistantBetaGateService` to read the audit ledger and produce one of:
  - `enable_limited_autonomy`
  - `keep_preview_only`
  - `pause_for_safety_work`
- Added a text proof packet under `vnext/artifacts/pet-tasks/<timestamp>-beta-gate/`.
- Added a Settings checkbox labeled `STOP all helper work immediately`.

## Implemented

- `vnext/src/Wevito.VNext.Core/KillSwitchService.cs`
- `vnext/src/Wevito.VNext.Core/BetaGateDecision.cs`
- `vnext/src/Wevito.VNext.Core/IndependentAssistantBetaGateService.cs`
- `vnext/tests/Wevito.VNext.Tests/KillSwitchServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/IndependentAssistantBetaGateServiceTests.cs`
- `vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs`
- `vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs`
- `vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs`
- `vnext/src/Wevito.VNext.Core/LearningEvalService.cs`
- `vnext/src/Wevito.VNext.Core/LocalModelAdapter.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

## Safety Boundaries

- Kill switch defaults to `false`.
- When active, blocked services return `kill_switch=true` and emit a local ledger row when a ledger is available.
- The beta gate cannot enable limited autonomy unless every check passes.
- Any safety failure, including network or hosted-AI ledger rows, returns `pause_for_safety_work`.
- No hosted AI calls, web calls, training, sprite mutation, or autonomous execution were added.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "BetaGate|KillSwitch"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

## Next Phase

C-PHASE 70 is the first network-capable surface: approved web research. It must remain stopped until the user explicitly approves opening that surface.
