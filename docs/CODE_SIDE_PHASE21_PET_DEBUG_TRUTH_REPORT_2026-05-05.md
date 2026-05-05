# Code-Side Phase 21: Pet Debug Truth Report

Date: 2026-05-05

## Scope

Add a read-only report builder that compares durable pet truth, derived wellbeing context, expected animation hints, and care-action readiness.

```text
GameContent + PetActor[]
   |
   +-- PetSimulationEngine.IsActionEnabled
   +-- PetWellbeingInterpreter.BuildSnapshots
   |
   v
PetDebugTruthReportBuilder
   |
   +-- structured PetDebugTruthReport
   +-- markdown summary
```

This phase does not add UI controls, execute adapters, mutate pets, mutate assets, or run visual proofing.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetDebugTruthReportBuilder.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetDebugTruthReportBuilderTests.cs`

## Report Contents

- generated timestamp
- companion mode
- per-pet identity, behavior state, visible animation, expected animation hint
- wellbeing urgency, drive, emotion, and summary
- status, condition, and personality context
- primary action readiness with reasons
- findings for care urgency and high-confidence animation/debug-truth mismatch

## Ownership Boundary

```text
PetSimulationEngine
   owns gameplay mutation and action truth

PetWellbeingInterpreter
   owns derived pet-state interpretation

PetDebugTruthReportBuilder
   owns read-only comparison/reporting only
```

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `85 / 85`.

## Audit Result

Pass. This is a safe code-side seam for future helper-agent context and debugging.

## Next Safe Step

Expose the report through a no-mutation PET TASKS adapter family such as `petState`, writing only timestamped markdown/JSON artifacts under `vnext\artifacts\pet-tasks`.
