# Code-Side Phase 17: PET TASKS Live UI Probe

Date: 2026-05-05

## Scope

Verify the PET TASKS `PREVIEW` bridge through the running vNext Shell UI without enabling execution or mutating visual assets.

```text
probe-vnext-pet-tasks.ps1
   |
   v
Wevito Home Panel -> TOOLS -> PET TASKS
   |
   v
type command -> PREPARE -> PREVIEW
   |
   v
spriteAudit report artifact
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`

## Bug Found And Fixed

The first live probe attempt exposed a real PET TASKS UI issue:

- The render loop could overwrite unsaved command text with an empty saved `PetCommandBarState.InputText`.
- In automated and no-activate/focus-sensitive paths, this meant clicking `PREPARE` submitted an empty task.
- The app then produced `Blocked: Please enter a task for one of your helper pets.`
- The `PREVIEW` button stayed disabled because the queued card was blocked.

Fix:

- `RenderPetCommandPanel` now only syncs saved input text into `PetCommandTextBox` when the saved input is non-empty.
- This prevents passive renders from erasing newly typed user text.

## Probe Behavior

The new probe:

- launches the Debug vNext Shell with isolated data and trace folders
- opens `TOOLS`
- opens `PET TASKS`
- enters `review goose baby female blue sprites`
- clicks `PREPARE`
- clicks `PREVIEW`
- waits for a `spriteAudit` `PreviewReady` trace
- confirms the audit report path exists
- confirms target sprite hashes did not change

Rows hash-checked:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime\goose\baby\female\blue`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\bin\Debug\net8.0-windows\sprites_runtime\goose\baby\female\blue`

## Latest Probe Artifact

- Summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-014813\summary.json`
- Trace: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-014813\trace\Wevito.VNext.Shell.trace.log`
- PET TASKS report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-054827-spriteaudit-reviewsprites\run-summary.md`

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -SkipBuild` passed.
- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `78 / 78`.

## Audit Notes

- No `Execute` run mode was added.
- No Godot/package proof command was run by PET TASKS.
- No sprite import or visual generation was performed.
- No runtime/source PNGs were mutated.
- The probe writes only timestamped artifacts under `vnext\artifacts\pet-task-probes` and `vnext\artifacts\pet-tasks`.

## Next Safe Step

Pause PET TASKS expansion unless the visual-side cleanup process needs more report detail. The next code-side work should probably shift back to broader game readiness or a manually approved apply/proof task, rather than adding agent execution.
