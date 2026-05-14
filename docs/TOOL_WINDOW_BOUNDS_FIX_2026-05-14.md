# Tool Window Bounds Fix

## Goal

Keep PET TASKS, Creative Learning Lab, and Sprite Workflow V2 controls reachable on the current desktop instead of letting lower controls disappear below the taskbar or screen edge.

## Changes

- Increased the regular tool popup target height from `420` to `720`.
- Clamped tool popup height to the active work area.
- Added a shared `WindowPlacementHelper` that clamps dense standalone tool windows to `SystemParameters.WorkArea`.
- Applied that clamp to Creative Learning Lab and Sprite Workflow V2 on load.
- Added tests for the tool window height resolver.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ShellPresentation|ToolPopup|LearningLab|SpriteWorkflow" --no-build`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

Results:

- Focused tests: `56 / 56`
- Full tests: `593 / 593`
- Debug publish with `-SkipAssetPrep`: passed
