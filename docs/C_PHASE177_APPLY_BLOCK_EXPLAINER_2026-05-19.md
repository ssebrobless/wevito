# C-PHASE 177 - Apply-Block Explainer Panel

## Goal

Add a read-only explanation surface for why supervised self-improvement apply remains blocked, using only existing `self_improvement_apply_prerequisite_check` audit packets and optional prerequisite-check artifacts.

Base commit: `9d77334887038a73c062029fa02c7c71730e08a0`

## Scope

Files added:
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyPrerequisiteExplanation.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyPrerequisiteExplainerService.cs`
- `vnext/tests/Wevito.VNext.Tests/ApplyPrerequisiteExplainerServiceTests.cs`

Files extended:
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreVisibilityTests.cs`

Not touched:
- No model adapters.
- No eval stores.
- No apply runner.
- No capability flags.
- No audit packet kinds.
- No sprite or runtime content assets.

## Implemented

- Added `ApplyPrerequisiteExplainerService`, a read-only SQLite reader for the latest `self_improvement_apply_prerequisite_check` packet matching an operation id.
- Added optional artifact enrichment from `prerequisite-check.json` under the configured artifact root.
- Added exact plain-language mappings for the ten known prerequisite rows.
- Added safe fallback reasons:
  - `kill_switch=true`
  - `no prerequisite check packet found`
  - `per-entry details unavailable; run the prerequisite check service to regenerate`
- Added a read-only Evidence tab expander titled `Why is apply blocked? (read-only)`.
- Wired the expander through `ShellCompositionRoot`, `ShellCoordinator`, and `ToolPopupWindow`.
- Extended held-out and in-distribution visibility tests so the new explainer types cannot depend on eval stores.

## Safety Boundaries

- The explainer never calls `ApplyRunnerPrerequisiteCheckService.Check(...)`.
- The explainer never writes to `AuditLedgerService`.
- The UI expander contains no `Button`, `ToggleSwitch`, `CheckBox`, or click handler.
- The service has no `Check`, `Run`, `Apply`, `Execute`, or `Emit` method.
- The service uses SQLite read-only mode.
- No network, model, training, mutation, or apply behavior was added.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyPrerequisiteExplainer|ApplyRunnerPrerequisite|PlainLanguage|SelfImprovement"`: passed, 298/298.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1360/1360.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No apply-check rerun from the explainer.
- [x] No audit writes from the explainer or UI expander.
- [x] No model call.
- [x] No network call.
- [x] No mutation or apply behavior.
- [x] No held-out or in-distribution eval store dependency.
- [x] No new capability flag.
- [x] No new audit packet kind.
- [x] All validation commands passed.

## Next Phase

C-PHASE 178 - Snapshot verifier wiring and artifact affordance - Auto-continue=No.
