# Code-Side Phase 5 Pet Command UI Progress - 2026-05-05

## Scope

Added the first visible Shell integration for the pet-helper command bar. This is a draft-only UI surface: users can type a task, route it to one of up to three active pet helpers, and see the parsed task-card/policy outcome. No tools execute from this surface yet.

## Shape

```text
HUD TOOLS strip
┌──────────┬───────────┬────────┬────────┬────────┐
│ LINK BIN │ PET TASKS │ EMPTY  │ EMPTY  │ EMPTY  │
└────┬─────┴─────┬─────┴────────┴────────┴────────┘
     │           │
     │           ▼
     │     ToolPopup: helpers
     │     ┌────────────────────────────────────────┐
     │     │ helper roster: 3 active pets max        │
     │     │ task text box + PREPARE                 │
     │     │ draft card + dry-run policy readout     │
     │     └────────────────────────────────────────┘
     ▼
ToolPopup: basket
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`

## Implemented

- Added a visible `PET TASKS` tab in the existing HUD `TOOLS` strip.
- Added a compact `helpers` popup panel with:
  - three helper slots derived from active pets,
  - a task input box,
  - a `PREPARE` button,
  - routed task-card status,
  - task kind/tool-family readout,
  - dry-run policy result,
  - latest timeline note.
- Wired the popup to `PetCommandBarService.SubmitDraft`.
- Kept the route intentionally non-mutating:
  - no Tool Broker command is sent,
  - no browser automation runs,
  - no file or sprite mutation occurs,
  - build/proof requests stop at `WaitingForApproval`,
  - risky requests stop at `Blocked`.
- Added `open-helpers` hotkey/tool routing support for future automation probes.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `51 / 51`.
- PowerShell source search confirmed no remaining source references to the old `WebToolSlot2Button`; generated `bin/obj` files were excluded from the source check.
- `rg` was unavailable in this shell because Windows returned `Access is denied`, so PowerShell `Select-String` was used instead.

## Boundaries

- This does not make pets autonomous yet.
- This does not run OpenAI/Gemini/Claude/browser/local tools.
- This does not write generated art, runtime sprites, source boards, or manifests.
- This does not persist task cards yet; it is Shell memory only.

## Next Safe Code-Side Step

Add a deterministic, non-executing task-card log/store and approval queue so the UI can retain draft tasks across renders/restarts before any actual tool execution bridge is designed.
