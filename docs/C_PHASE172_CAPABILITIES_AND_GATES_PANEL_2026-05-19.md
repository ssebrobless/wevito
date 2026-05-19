# C-PHASE 172: Capabilities And Gates Panel

## Goal

Add a read-only Evidence-tab panel that summarizes every capability gate from `CapabilityFlagInventory.Entries`, showing each flag's default value, current value, plain-language meaning, state, and KillSwitch status without adding any toggle, setting writer, audit packet, model call, network call, or mutation path.

Base commit recorded for this phase: `784a218d989aeaa44398c96c1a3e41a7e5511b82`.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/CapabilitiesAndGatesSnapshot.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/CapabilitiesAndGatesService.cs`
- `vnext/tests/Wevito.VNext.Tests/CapabilitiesAndGatesServiceTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Not touched:

- No capability flag inventory entries were added.
- No audit packet kind was added.
- No settings writer was added.
- No model adapter, local scoring provider, eval store, apply runner, or mutation surface was modified.

## Implemented

- Added `CapabilitiesAndGatesEntry` and `CapabilitiesAndGatesSnapshot` DTOs.
- Added `CapabilitiesAndGatesService`, which reads current settings and `CapabilityFlagInventory.Entries` in declared order.
- Snapshot state rules:
  - `on` when current value is `true`.
  - `off` when current value is `false`.
  - `unset` otherwise.
  - When KillSwitch is active, every state appends `; kill_switch=true`.
- Added a composition-root factory and ShellCoordinator wiring so the Tool Popup receives a fresh snapshot during render.
- Added `Capabilities and gates (read-only)` at the top of the Evidence tab.
- The new expander displays text-only rows and has no buttons, toggles, checkboxes, click handlers, or setting writers.

## Safety Boundaries

- The panel is read-only.
- The service does not write settings.
- The service does not write audit rows.
- The service does not reference `IHeldOutEvalStore`, `IInDistributionEvalStore`, `IModelAdapter`, or `ILocalScoringProvider`.
- The UI expander contains no interactive control for flipping capabilities.
- No capability flag default changed.
- No model call, network call, training, apply behavior, or mutation behavior was introduced.
- Public self-improvement DTOs include a KillSwitch constructor overload only to satisfy the repo-wide KillSwitch coverage invariant; they remain pure data records.

## Validation

- Pre-flight `dotnet build .\vnext\Wevito.VNext.sln`
  - Passed with `0` warnings and `0` errors.
- Pre-flight `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - Passed: `1294/1294`.
- Pre-flight `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
  - Passed.
- Pre-flight `git diff --check`
  - Passed.
- Phase-specific `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "CapabilitiesAndGates|PlainLanguage|SelfImprovement"`
  - Passed: `254/254`.
- Final `dotnet build .\vnext\Wevito.VNext.sln`
  - Passed with `0` warnings and `0` errors.
- Final `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - Passed: `1300/1300`.
- Final `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
  - Passed.
- Final `git diff --check`
  - Passed. Only line-ending warnings were reported for touched files.

## Stop-Gate Checklist

- [x] New expander contains no `Button`.
- [x] New expander contains no `ToggleSwitch`.
- [x] New expander contains no `CheckBox`.
- [x] New expander contains no click handler.
- [x] Service writes no settings.
- [x] Service writes no audit rows.
- [x] No new capability flag was added.
- [x] No new audit packet kind was added.
- [x] No held-out or in-distribution eval store reference was added.
- [x] No model adapter or local scoring provider reference was added.
- [x] Validation passed.

All stop gates are false.

## Next Phase

C-PHASE 173 — Self-improvement product-truth retest — Auto-continue=No.
