# Code-Side Phase 31 Code Patch Plan Tool - 2026-05-05

## Goal

Add a read-only planning layer before PET TASKS is allowed to perform code edits.

The `codePatchPlan` tool writes a proposed scope, file candidates, implementation steps, validation plan, rollback plan, and safety gates. It does not edit files or run commands.

```text
PET TASKS: "plan a code fix"
   |
   v
codePatchPlan preview adapter
   |
   v
code-patch-plan-report.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\CodePatchPlanPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\CodePatchPlanPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## What Changed

- Added `TaskKind.PlanCodePatch`.
- Added patch-plan report contracts:
  - `CodePatchPlanFileCandidate`
  - `CodePatchPlanStep`
  - `CodePatchPlanReport`
- Added `CodePatchPlanPreviewAdapter`.
- Added `codePatchPlan` routing to the PET TASKS dispatcher.
- Added parser routing for commands such as `plan a code fix`, `code patch plan`, and `patch plan`.
- Added shell policy and default approved roots shared with `codeReview`:
  - `vnext\src`
  - `vnext\tests`
  - `tools`
  - `scripts`
- Added `codePatchPlan` to the PET TASKS capability line and live probe assertion.

## Safety Behavior

- Run mode: dry-run preview only.
- Access mode: read-only only.
- Writes only new markdown/JSON artifacts under `vnext\artifacts\pet-tasks`.
- Blocks execute mode.
- Blocks targets outside approved roots.
- Skips visual/runtime asset folders, build folders, cache folders, and artifact folders.
- Includes safety gates that explicitly block runtime/source PNG mutation, visual generation/import, prop-anchor edits, asset prep, destructive commands, and implementation without approval.

## Validation

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~CodePatchPlanPreviewAdapterTests|FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~PetTaskAdapterPreviewDispatcherTests|FullyQualifiedName~PetAgentContractTests"
```

Result: passed `32 / 32`.

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `116 / 116`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live PET TASKS probe:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "plan a code fix for the shell popup" -ExpectedToolFamily codePatchPlan -SkipBuild
```

Result: passed.

Latest probe summary:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-134019-028-554bdc30\summary.json
```

Latest code patch plan report:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-174032-codepatchplan-plancodepatch\run-summary.md
```

Latest live report summary:

- Candidate files: `40`
- Did mutate code: `false`
- Target sprite row hashes unchanged: `true`

## Audit

Pass.

- Correctness: PET TASKS can now produce a code patch plan report from the same helper command bar.
- Safety: no code edits, no command execution, no asset mutation.
- UX: the capability line exposes `codePatchPlan` beside `codeReview`.
- Coordination: visual-side cleanup remains protected by the no-PNG-mutation and `-SkipAssetPrep` gates.

## Next Phase Recommendation

Proceed to Phase 32: build/test proof task planning.

Keep Phase 32 approval-gated:

- prepare a build/test command plan,
- show exact commands and risks,
- do not execute build/proof commands from PET TASKS without approval,
- keep `-SkipAssetPrep` mandatory while visual cleanup is active.
