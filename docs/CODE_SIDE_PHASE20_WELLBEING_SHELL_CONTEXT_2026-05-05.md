# Code-Side Phase 20: Wellbeing Shell Context

Date: 2026-05-05

## Scope

Expose Phase 19 `PetWellbeingSnapshot` data in the Shell as read-only helper-pet context.

```text
PetActor durable truth
   |
   v
PetWellbeingInterpreter
   |
   v
PetCommandBarState.WellbeingSnapshots
   |
   v
PET TASKS popup readout
```

This phase does not change pet behavior, persistence, sprite assets, adapters, generation, imports, Godot proofing, or runtime PNGs.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`

## Behavior Added

- `PetCommandBarState` now carries optional `WellbeingSnapshots`.
- `ShellCoordinator` refreshes snapshots when rendering helper state, submitting draft cards, changing task status, and running dry-run previews.
- PET TASKS popup now shows a compact `Wellbeing:` block for up to three active pets.
- The live PET TASKS probe now asserts that the wellbeing text exists and records it in `summary.json`.

## Current UI Example

```text
Wellbeing:
Rat 1: Watch / SelfMaintenance / Relief | bright
Crow 2: Watch / SelfMaintenance / Relief | bright
Fox 3: Watch / SelfMaintenance / Relief | bright
```

## Ownership Boundary

```text
PetSimulationEngine
   owns pet truth and mutations

PetWellbeingInterpreter
   owns derived interpretation

Shell / PET TASKS
   displays derived context only
   does not write pet truth from snapshots
```

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `83 / 83`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 120` passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -SkipBuild` passed.
- Latest probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-021334\summary.json`.

## Audit Result

Pass. The phase is additive, report/display-only, and keeps the PET TASKS adapter lane non-mutating.

## Next Safe Step

Plan and implement a read-only pet-state/debug-truth report surface that compares visible UI/action readiness against derived wellbeing context, without changing gameplay behavior or assets.
