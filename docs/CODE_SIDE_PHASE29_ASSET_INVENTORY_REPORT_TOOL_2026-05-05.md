# Code-Side Phase 29 Asset Inventory Report Tool - 2026-05-05

## Goal

Add a read-only PET TASKS asset inventory tool so code-side and visual-side can inspect asset structure without opening many folders or mutating files.

```text
PET TASKS: "inventory assets"
   |
   v
assetInventory preview adapter
   |
   v
asset-inventory-report.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\AssetInventoryPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\AssetInventoryPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## What Changed

- Added `TaskKind.InventoryAssets`.
- Added asset inventory report contracts:
  - `AssetInventoryRootSummary`
  - `AssetInventoryFinding`
  - `AssetInventoryReport`
- Added `AssetInventoryPreviewAdapter`.
- Added `assetInventory` routing to the PET TASKS dispatcher.
- Added parser routing for commands such as `inventory assets`, `asset inventory`, and `count assets`.
- Added shell policy and approved roots for:
  - `sprites_runtime`
  - `sprites_shared_runtime`
- Added `assetInventory` to the PET TASKS capability line.
- Extended live PET TASKS probe validation to require `assetInventory` in the capability text.

## Current Report Capabilities

- Counts root directories.
- Counts all files.
- Counts PNGs.
- Counts `.png.import` sidecars.
- Counts JSON files.
- Counts runtime variant folders at `species / age / gender / color` depth.
- Flags orphan `.png.import` sidecars.
- Flags empty directories.
- Writes JSON and markdown only.

## Validation

First parallel focused test attempt collided with a simultaneous build writing the same contracts DLL. The build passed, and the focused tests passed when rerun alone.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build --filter "FullyQualifiedName~AssetInventoryPreviewAdapterTests|FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~PetTaskAdapterPreviewDispatcherTests"
```

Result: passed `20 / 20`.

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `104 / 104`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live PET TASKS probe:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "inventory assets" -ExpectedToolFamily assetInventory -SkipBuild
```

Result: passed.

Latest probe summary:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-132232-002-14ed7aee\summary.json
```

Latest inventory report:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-172245-assetinventory-inventoryassets\run-summary.md
```

Latest live report summary:

- Runtime variant folders: `360`
- Runtime PNGs: `10800`
- Shared PNGs: `554`
- Findings: `0`
- Did mutate assets: `false`

## Audit

Pass.

- Correctness: adapter reports current runtime/shared asset structure.
- Safety: no mutation/import/generation/candidate apply/contact-sheet generation.
- Maintainability: follows existing PET TASKS adapter artifact pattern.
- UX: user can ask `inventory assets` from the same PET TASKS bar.
- Coordination: useful to visual-side cleanup without touching visual-side files.

## Next Phase Recommendation

Proceed to Phase 30: code review preview tool.

Keep Phase 30 read-only:

- approved-root code scan,
- language/file type detection,
- structure summary,
- likely issue report,
- suggested tests,
- no edits,
- no command execution.
