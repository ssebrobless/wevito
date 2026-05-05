# Code-Side Phase 16: PET TASKS Preview Probe

Date: 2026-05-05

## Scope

Add a small non-mutating probe path for the PET TASKS dry-run preview pipeline.

```text
probe-pet-tasks-preview.ps1
        |
        v
PetTasksPreviewSmokeTests
        |
        +-- PetCommandParser
        +-- PetTaskAdapterPreviewDispatcher
        +-- SpriteAuditPreviewAdapter
        |
        v
temp pet-tasks report artifacts
```

This probe does not use Godot, does not package the game, does not import sprites, does not generate art, and does not mutate runtime/source PNGs.

## Files Added

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-pet-tasks-preview.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTasksPreviewSmokeTests.cs`

## Behavior

- Runs the focused `PetTasksPreviewSmokeTests` test class.
- Exercises an end-to-end dry-run path:
  - command text: `Bean, review goose baby female blue sprites`
  - parser extracts `goose\baby\female\blue`
  - dispatcher routes to `spriteAudit`
  - adapter writes markdown and JSON into a temporary `pet-tasks` artifact folder
  - source PNG bytes are compared before/after to prove no mutation

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-pet-tasks-preview.ps1` passed `1 / 1`.
- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `78 / 78`.

## Audit Notes

- The probe writes only temporary test artifacts, not main repo visual folders.
- The PET TASKS Shell bridge still exposes dry-run preview only.
- `Execute` mode remains unreachable.
- Godot proofing and the goose `drop_ball` one-row apply/proof pilot remain outside PET TASKS.

## Next Safe Step

Run a manual app smoke later if desired:

1. open vNext Shell
2. open `PET TASKS`
3. submit `Bean, review goose baby female blue sprites`
4. click `PREVIEW`
5. verify a `pet-tasks` report path appears
6. confirm no sprite PNGs were modified
