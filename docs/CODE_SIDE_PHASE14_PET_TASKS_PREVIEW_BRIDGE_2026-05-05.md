# Code-Side Phase 14: PET TASKS Preview Bridge

Date: 2026-05-05

## Scope

Add a Shell-visible dry-run preview bridge for PET TASKS without enabling execution.

```text
PET TASKS card
   |
   +-- Draft or Approved only
   |
   v
PREVIEW button
   |
   v
PetTaskAdapterPreviewDispatcher
   |
   +-- localDocs report preview
   +-- spriteAudit report preview
   +-- unsupported family -> blocked card
```

This phase does not run Godot, does not run build/proof commands, does not import sprites, does not generate art, and does not mutate runtime/source PNGs.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskCardQueueService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskCardQueueServiceTests.cs`

## Behavior

- Adds a `PREVIEW` button to the `PET TASKS` queue controls.
- Enables preview only for `Draft` and `Approved` task cards.
- Keeps `WaitingForApproval` cards blocked until the user approves or cancels.
- Builds a dry-run `TaskAdapterRequest` from the selected card and policy snapshot.
- Injects safe approved roots at preview time:
  - `spriteAudit` -> resolved `sprites_runtime`
  - `localDocs` -> `docs` when present, otherwise repo root
- Writes adapter artifacts only under `vnext\artifacts\pet-tasks\<timestamp-tool-kind>\`.
- Applies preview results back to the selected card:
  - `PreviewReady` -> `Reviewing`
  - `Blocked` -> `Blocked`
  - `Failed` -> `Failed`
  - completed fallback -> `Done`
- Stores `ResultSummary`, `AuditLogPath`, and timeline entries for review.

## Tests Added

- `TryApplyAdapterResult_MovesDraftPreviewToReviewingWithAuditPath`
- `TryApplyAdapterResult_BlocksPreviewFromWaitingForApproval`

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `73 / 73`.

## Audit Notes

- No `Execute` run mode is reachable from the Shell.
- No build/proof command is reachable from `PET TASKS`.
- No visual-side candidate/proof folders are written by this bridge.
- The goose `drop_ball` apply/proof pilot remains a manual code-side workflow outside PET TASKS.
- Current spriteAudit targeting is still coarse because the parser does not yet extract frame/folder targets from free text.

## Next Safe Step

Add target extraction for local paths and obvious sprite row phrases so preview reports are useful without broad scans. Keep it parser-only and dry-run-only.
