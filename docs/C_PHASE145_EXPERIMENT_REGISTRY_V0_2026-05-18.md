# C-PHASE 145 - Experiment Registry v0

Date: 2026-05-18
Branch: `claude-implementation/c-phase-145-experiment-registry-v0`
Base: `296256629fc08803f72c98e95fee48a1ff8dbe15`

## Goal

Add the empty self-improvement experiment registry layer and wire it into the constitutional decision gate so no supervised self-improvement proposal can pass until a later reviewed phase registers an explicit experiment kind.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentKind.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentDescriptor.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentRegistry.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalReviewedEmitter.cs`
- `vnext/tests/Wevito.VNext.Tests/ExperimentRegistryTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ConstitutionalReviewedEmitterTests.cs`
- `docs/C_PHASE145_EXPERIMENT_REGISTRY_V0_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionService.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/tests/Wevito.VNext.Tests/ConstitutionalDecisionServiceTests.cs`

Not touched:

- No proposal producer.
- No autonomous loop registry tick.
- No capability flag.
- No model adapter.
- No hosted AI, network, sprite, content, or filesystem mutation surface.

## Implemented

- Added `ExperimentKind` and `ExperimentDescriptor` as the typed contract for future experiment kinds.
- Added `ExperimentRegistry` with an empty default registry and explicit construction paths for tests and the composition root.
- Kept registry mutation private so runtime code cannot silently register experiment kinds after construction.
- Wired `ConstitutionalDecisionService` to consult the registry and force `Blocked("no_experiment_kind_registered")` when the registry is empty.
- Extended `ShellCompositionRoot` with `CreateExperimentRegistry()`, returning an empty registry for this phase.
- Added `ConstitutionalReviewedEmitter`, which writes exactly one `self_improvement_constitutional_reviewed` audit packet per decision.
- Added tests covering empty-registry blocking, duplicate kind rejection, test-only registration, composition-root default behavior, and honest emitter flags.

## Safety Boundaries

- Default registry ships empty.
- No experiment kind is pre-seeded by `ShellCompositionRoot`.
- No decision path can return `Allowed` while the registry is empty.
- The emitter does not apply proposals, mutate files, call tools, call network, or call any model.
- The emitted audit packet flags are fixed honestly for this phase: `did_use_network=false`, `did_use_hosted_ai=false`, `did_use_local_model=false`, and `did_mutate=false`.
- The registry is immutable after construction outside the explicit factory paths.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ExperimentRegistry|ConstitutionalDecision|ConstitutionalReviewedEmitter|PlainLanguage|SelfImprovement"`: 253/253 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1085/1085 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No default experiment kind is pre-seeded.
- [x] No code path registers an experiment kind outside tests or composition-root construction.
- [x] `ConstitutionalDecisionService` returns blocked while the registry is empty.
- [x] `ConstitutionalReviewedEmitter` writes honest evidence flags.
- [x] No producer logic was added.
- [x] No capability flag was added.
- [x] No model, network, tool, sprite, or content mutation path was added.
- [x] Validation commands passed.

## Next Phase

C-PHASE 147 is `Eval gate manifest + held-out eval storage contract` according to the current 143-plus plan ordering. Auto-continue is No, so the next phase should not start until the user explicitly approves and provides the C-PHASE 147 prompt.
