# Code-Side To Visual Handoff

Date: 2026-05-05

## Current Code-Side Status

Code-side has completed the PET TASKS dry-run/report infrastructure and a vNext game interaction readiness pass.

```text
PET TASKS
   |
   +-- localDocs dry-run preview
   +-- spriteAudit dry-run report
   +-- live UI PREVIEW probe
   |
   v
report-only artifacts

Game readiness
   |
   +-- safe build with -SkipAssetPrep
   +-- action/tool runtime probe
   +-- 79/79 vNext tests
```

## Important Boundaries

- PET TASKS is still report-only.
- PET TASKS does not run `Execute` mode.
- PET TASKS does not run Godot/package proofing.
- PET TASKS does not generate art.
- PET TASKS does not import sprites.
- PET TASKS does not mutate runtime/source PNGs.
- PET TASKS does not edit prop anchors.
- The goose `drop_ball` one-row apply/proof pilot remains manual code-side work outside PET TASKS.

## Code-Side Work Completed

### PET TASKS Infrastructure

Relevant docs:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE13_ADAPTER_PREVIEW_DISPATCHER_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE14_PET_TASKS_PREVIEW_BRIDGE_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE15_PET_TASK_TARGET_EXTRACTION_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE16_PET_TASKS_PREVIEW_PROBE_2026-05-05.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE17_PET_TASKS_LIVE_UI_PROBE_2026-05-05.md`

Key files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\LocalDocsPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\SpriteAuditPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`

Probe scripts:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-pet-tasks-preview.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`

Latest PET TASKS live probe:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-014813\summary.json`

Latest PET TASKS generated report from live probe:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-054827-spriteaudit-reviewsprites\run-summary.md`

### Game Interaction Readiness

Relevant doc:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE18_GAME_INTERACTION_READINESS_2026-05-05.md`

Action/tool probe summary:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260505-015533\summary.json`

Important result:

- doctor, medicine, bath, groom, feed, water, play, rest, home action flows passed runtime trace validation
- settings/save passed runtime trace validation
- basket paste/open/delete passed runtime trace validation

### Tiny Fix

`PetSimulationEngine.DescribeAging` now uses ASCII ` - ` instead of a non-ASCII middle dot, because the shell output could render it as mojibake.

File:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`

Test:

- `DescribeAging_UsesAsciiSeparatorForShellText`

## Current Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -SkipBuild` passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 120` passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500` passed.
- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `79 / 79`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 120` passed and refreshed `vnext\artifacts\shell`.

## Asset Safety Policy For Visual Thread

Use `-SkipAssetPrep` for code-side build/publish validation while visual cleanup is active.

Reason:

- `build-vnext.ps1` without `-SkipAssetPrep` runs sprite cleaning/generation scripts.
- Those scripts can rewrite runtime asset folders.
- Visual-side cleanup should not be overwritten by an accidental asset-prep build.

## What Visual-Side Can Safely Assume

- vNext runtime action controls are currently operational.
- PET TASKS can produce non-mutating spriteAudit reports through the app UI.
- PET TASKS reports are not an approval to mutate assets.
- PET TASKS report output currently includes markdown and JSON only; contact sheets are still deferred.
- PET TASKS simple target phrase support works for rows like `goose baby female blue`.
- Generated PET TASKS artifacts should stay under timestamped `vnext\artifacts\pet-tasks\...` folders.

## Suggested Visual-Side Use

If visual-side wants a non-mutating row audit through PET TASKS, it can ask code-side to run a command like:

```text
review goose baby female blue sprites
```

That creates a focused `spriteAudit` report without changing PNGs.

## Do Not Use PET TASKS Yet For

- candidate apply
- sprite import
- visual generation
- all-color propagation
- prop-anchor edits
- packaged proof capture
- Godot apply/proof pilots
- broad cleanup automation

Those remain separate manual/coordinated workflows.
