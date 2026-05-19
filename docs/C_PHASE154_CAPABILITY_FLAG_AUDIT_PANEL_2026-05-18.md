# C-PHASE 154: Capability-Flag Audit Panel

## Goal

Add a read-only Evidence-tab panel that shows Wevito's capability flags, their safe defaults, their current values, and a plain-language explanation of what each flag allows.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagAuditService.cs`
- `vnext/tests/Wevito.VNext.Tests/CapabilityFlagInventoryTests.cs`
- `vnext/tests/Wevito.VNext.Tests/CapabilityFlagAuditServiceTests.cs`
- `docs/C_PHASE154_CAPABILITY_FLAG_AUDIT_PANEL_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Not touched:

- No sprite assets.
- No content manifests.
- No model adapters.
- No network surfaces.
- No autonomous scope behavior.
- No packet producers.

## Implemented

- Added `CapabilityFlagInventory`, a canonical read-only list of safety-relevant capability gates.
- Added `CapabilityFlagAuditService`, which renders each flag as name, default, current value, default/overridden state, and plain-language meaning.
- Added KillSwitch masking: when `runtime_kill_switch=true`, current values display as `(masked: kill_switch=true)` and no row is reported as default.
- Wired the Shell coordinator to build the read-only audit rows during render.
- Added an Evidence-tab `Capability flags` panel with a read-only grid and no buttons or toggles.
- Added tests proving defaults are only `False` or empty, names are unique, required autonomy/model/local-access gates are inventoried, KillSwitch masking works, overridden values render, and the panel section is read-only.

## Safety Boundaries

- No capability flag defaults were changed.
- No UI element was added that can flip a flag.
- No audit packet kind was added.
- No audit ledger writes were added.
- No model, hosted AI, network, training, file mutation, code mutation, or sprite mutation path was touched.
- The service has no `AuditLedgerService` dependency and no `Record` call path.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "CapabilityFlag|PlainLanguage|SelfImprovement"`: passed, 250/250.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1155/1155.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with line-ending warnings only.

## Stop-Gate Checklist

- [x] Service writes no flag.
- [x] Inventory includes the known autonomy, scheduler, self-improvement, model, web, file, tool, local-docs, and kill-switch gates from predecessor code.
- [x] Panel renders no write button.
- [x] Every `DefaultValue` is exactly `False` or empty.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 155: Approval-card detail view. Auto-continue remains No.
