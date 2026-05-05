# Code-Side Phase 7 Task Queue Visibility Progress - 2026-05-05

## Scope

Made the saved helper-pet task queue visible in the `PET TASKS` popup. This phase is still display-only and does not add approval or execution buttons.

## Shape

```text
CompanionState.TaskCards
        │
        ▼
PetCommandBarState.QueuedTaskCards
        │
        ▼
ToolPopupWindow / PET TASKS
┌──────────────────────────────────────┐
│ latest card detail                    │
│ queue counts: draft / approval / block│
│ latest 3 queued cards                 │
└──────────────────────────────────────┘
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`

## Implemented

- Added `PetCommandBarState.QueuedTaskCards`.
- Passed saved task cards from `CompanionState.TaskCards` into the command-bar UI state.
- Updated `PET TASKS` popup to show:
  - total saved task-card count,
  - draft count,
  - waiting-for-approval count,
  - blocked count,
  - latest three queued cards.
- Added contract serialization coverage for queued command-bar state.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `55 / 55`.

## Boundaries

- No approval button was added.
- No task execution bridge was added.
- No Tool Broker command was added.
- No browser, filesystem, sprite, or network automation was added.

## Audit Result

Pass. The helper task queue is now visible enough to support a future explicit approval design without implying that tasks already run.

## Next Safe Code-Side Step

Design, but do not yet implement, the approval/execution bridge: exact allowed statuses, approval transitions, tool-family adapters, audit logs, and rollback requirements.
