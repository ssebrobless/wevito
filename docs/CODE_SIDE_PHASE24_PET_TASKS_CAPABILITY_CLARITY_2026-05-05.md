# Code-Side Phase 24: PET TASKS Capability Clarity

Date: 2026-05-05

## Scope

Make the PET TASKS popup clearer about what helper pets can safely do right now.

```text
PET TASKS popup
   |
   +-- preview-ready tools
   |     +-- spriteAudit
   |     +-- petState
   |     +-- localDocs
   |
   +-- locked actions
         +-- sprite import
         +-- generation
         +-- PNG mutation
         +-- prop anchors
         +-- build/proof execution
         +-- browser automation
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`

## Implemented

- Added a compact capability line to the PET TASKS popup.
- Added UIAutomation coverage for the capability line.
- Extended live PET TASKS probe summaries with `capability_text`.
- Kept this phase display-only; no new execution powers were added.

## Validation

- `dotnet build .\vnext\Wevito.VNext.sln` passed with `0` warnings.
- Full vNext tests passed `90 / 90`.
- Safe Debug publish passed with `-SkipAssetPrep -SkipTests`.
- Live PET TASKS `spriteAudit` probe passed:
  - `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-124027-472-a16363a4\summary.json`
- Live PET TASKS `localDocs` probe passed:
  - `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-124027-499-4042d40e\summary.json`
- Live PET TASKS `petState` probe passed:
  - `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-124027-472-7d4a79dd\summary.json`

## Audit Result

Pass. The user-facing PET TASKS surface now states the active report-only tools and the locked unsafe actions. This should reduce confusion while keeping the pet-agent tool layer safely non-mutating.

## Next Safe Step

Create a concise code-side handoff update so the visual thread knows the current PET TASKS/report/validation state before it continues broader sprite cleanup coordination.
