# C-PHASE 184 - Apply-runner review UI (read-only)

## Goal

Add a passive Tool Popup evidence surface for the C-PHASE 183 apply-v0 packet sequence. The panel helps a reviewer see what the narrow artifact rename runner recorded without adding any way to approve, apply, rerun, mutate, or flip capability flags.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityEntry.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityService.cs`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityHeldOutAccessTests.cs`

Extended:

- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/Support/KillSwitchCoverageAllowList.cs`

Not touched:

- `ArtifactRenameApplyRunner`
- capability flag inventory
- packet kind inventory
- model/network/training surfaces
- sprite/source/content mutation paths

`KillSwitchCoverageAllowList.cs` was extended only for the new immutable DTO/enum types introduced by this phase. The runtime service itself depends on `KillSwitchService`.

## Implemented

```text
Audit ledger
    │
    ▼
ApplyRunnerActivityService
    │  read-only SQLite, packet_kind LIKE self_improvement_apply_v0_%
    │  groups by operation_id
    │  orders packet timeline ascending inside each operation
    │
    ▼
Tool Popup > Evidence
    │
    └── Apply-runner activity (read-only)
            ├── operation / scope / disposition
            ├── scope hash
            ├── source and destination paths when present
            ├── pre/post sha256 when present
            └── packet timeline with did_mutate markers
```

- `ApplyRunnerActivityService.ReadRecent(maxEntries)` returns recent apply-v0 operation groups or an empty list when the KillSwitch is active.
- Disposition is derived from the last packet in a group: `ApplyV0Completed` means `Succeeded`, `ApplyV0RolledBack` means `RolledBack`, otherwise `InProgress`.
- The new Tool Popup expander is text/list-only and contains no button, toggle, checkbox, menu item, or mutating event handler.
- Shell wiring reads the latest 20 activity groups on render and passes them into the existing Evidence tab view model path.

## Safety Boundaries

- No new capability flag.
- No new audit packet kind.
- No call to `ArtifactRenameApplyRunner.Apply`.
- No `EmitReport` call.
- No model call, hosted AI call, network call, training, or asset/source mutation.
- The service opens SQLite with `Mode=ReadOnly` and contains no write SQL.
- The activity service and DTOs reference no held-out or in-distribution eval store types.

## Validation

- Pre-flight `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- Pre-flight `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1442/1442.
- Pre-flight `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- Pre-flight `git diff --check`: passed.
- Focused C-PHASE 184 filter `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerActivity|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"`: passed, 313/313.

- Final `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings/errors.
- Final full test suite `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1454/1454.
- Final `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- Final `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Expander contains no Button / ToggleSwitch / CheckBox / MenuItem.
- [x] Expander does not call a mutating handler.
- [x] Activity service writes no packets.
- [x] Activity service opens SQLite read-only.
- [x] Activity service contains no INSERT / UPDATE / DELETE SQL.
- [x] Activity service references no held-out or in-distribution eval types.
- [x] No new capability flag added.
- [x] No new packet kind added.
- [x] Expander does not invoke the rename runner.
- [x] Pre-flight gate passed.

## Next Phase

C-PHASE 185 - Rollback evidence viewer + symmetric narrow rollback runner. Auto-continue remains No; do not start it until the user explicitly approves that phase.
