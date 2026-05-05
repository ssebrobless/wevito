# Code-Side Phase 22: Pet State Preview Adapter

Date: 2026-05-05

## Scope

Expose the Phase 21 debug-truth report through PET TASKS as a preview-only `petState` adapter.

```text
PET TASKS command
   |
   v
PetCommandParser
   |
   +-- TaskKind.ReviewPetState
   +-- tool family: petState
   |
   v
PetTaskAdapterPreviewDispatcher
   |
   v
PetStatePreviewAdapter
   |
   +-- run-summary.md
   +-- pet-state-report.json
```

This phase writes only new timestamped artifacts under `vnext\artifacts\pet-tasks`. It does not mutate pets, sprites, source boards, prop anchors, shared assets, generated art, or candidate/proof folders.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetStatePreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetCommandParserTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetStatePreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`

## Commands Supported

Examples that now route to `petState`:

- `review pet state and wellbeing`
- `summarize pet status`
- `show pet needs`
- `review personality`
- `debug truth for pets`

## Safety Rules Preserved

- Adapter only supports `TaskAdapterRunMode.DryRunPreview`.
- Adapter requires a read-only `petState` policy.
- Adapter blocks artifact roots outside a `pet-tasks` folder.
- Adapter writes markdown/JSON reports only.
- Live probe can test `petState` while skipping sprite hash checks, and can still test `spriteAudit` with hash checks enabled.

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `89 / 89`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 120` passed.
- Live `petState` UI probe passed:
  - `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-022254\summary.json`
  - report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-062308-petstate-reviewpetstate\run-summary.md`
- Regression `spriteAudit` UI probe passed:
  - `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-022329\summary.json`

## Audit Result

Pass. PET TASKS now has two verified no-mutation preview surfaces: `spriteAudit` for sprite row reports and `petState` for pet wellbeing/debug-truth reports.

## Next Safe Step

Create a concise cross-thread update doc so visual-side knows code-side added `petState` report-only support and did not loosen the no-mutation visual boundaries.
