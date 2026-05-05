# Code-Side Phase 6 Task Draft Queue Progress - 2026-05-05

## Scope

Made `PET TASKS` draft cards durable without enabling tool execution. The goal was to stop helper-pet tasks from being purely transient Shell memory while preserving the safety boundary that pet helpers can only prepare draft cards.

## Shape

```text
PET TASKS submit
      │
      ▼
PetCommandBarService.SubmitDraft
      │ parses + policy checks only
      ▼
TaskCard
      │
      ▼
PetTaskCardQueueService
      │ caps local queue at 25 cards
      ▼
CompanionState.TaskCards
      │
      ▼
AppRepository JSON app_state
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskCardQueueService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\DefaultStateFactory.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskCardQueueServiceTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\AppRepositoryTests.cs`

## Implemented

- Added nullable `CompanionState.TaskCards` for backward-compatible state deserialization.
- Hydrated missing task-card lists to `[]` when Shell loads older state.
- Added `PetTaskCardQueueService` with:
  - duplicate replacement by card id,
  - newest-first ordering,
  - default cap of `25` task cards.
- Updated `PET TASKS` submit handling so a prepared card is saved into `CompanionState.TaskCards`.
- Updated initial `PET TASKS` render to show the latest locally saved draft card when available.
- Persisted draft cards through the existing app-state JSON path, avoiding a database migration in this phase.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `54 / 54`.
- Added coverage:
  - queue newest-first/cap behavior,
  - queue duplicate replacement,
  - repository save/load round-trip for task cards and basket items together.

## Boundaries

- No Tool Broker execution was added.
- No browser automation was added.
- No filesystem task execution was added.
- No sprite generation/import/apply path was added.
- No database schema migration was added; task cards are stored in app-state JSON for now.

## Audit Result

Pass. The phase preserves the draft-only safety boundary, gives the UI durable state, and keeps the next execution/approval bridge reversible.

## Next Safe Code-Side Step

Add a small approval-state seam for queued task cards: users should be able to see `Draft`, `WaitingForApproval`, and `Blocked` cards clearly before any future execution bridge is designed.
