# Code-Side Phase 9 Task Approval State Progress - 2026-05-05

## Scope

Added local approval-state editing for saved `PET TASKS` task cards without enabling task execution. This is the last safety seam before any future adapter bridge.

## Shape

```text
queued TaskCard
      │
      ▼
PET TASKS selector
      │
      ├── APPROVE ──▶ WaitingForApproval -> Approved
      │                  │
      │                  └── no execution adapter runs
      │
      └── CANCEL ───▶ Draft/WaitingForApproval/Approved -> Cancelled
                         │
                         └── no execution was started
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskCardQueueService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskCardQueueServiceTests.cs`

## Implemented

- Added `PetTaskCardQueueService.TryTransitionStatus`.
- Allowed transitions:
  - `WaitingForApproval` -> `Approved`
  - `Draft` -> `Cancelled`
  - `WaitingForApproval` -> `Cancelled`
  - `Approved` -> `Cancelled`
- Blocked approval of plain `Draft` cards so safe/read-only drafts do not imply execution readiness.
- Added `PET TASKS` queue selector plus `APPROVE` and `CANCEL` buttons.
- Persisted approved/cancelled status changes into `CompanionState.TaskCards`.
- Added timeline/result summaries that explicitly state no execution adapter has run.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `58 / 58`.

## Boundaries

- No task execution adapter was added.
- No Tool Broker command was added.
- No browser, filesystem, build, sprite, network, Gemini, Claude, or visual mutation action was added.
- Approval currently means "approved locally for a future adapter," not "run now."

## Audit Result

Pass. The UI can now safely preserve user intent and approval/cancellation state before any future execution bridge is considered.

## Next Safe Step

Before implementing actual adapters, run a cross-thread coordination checkpoint with visual-side because the first real adapters likely touch proof capture, local docs, sprite audits, and build proofs.
