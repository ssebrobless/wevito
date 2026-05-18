# C-PHASE 147 - Eval Gate Manifest

Date: 2026-05-18
Branch: `claude-implementation/c-phase-147-eval-gate-manifest`
Base: `1bea1573251ea1a0858d00ab317756da221ab69f`

## Goal

Define the supervised self-improvement eval gate manifest and held-out eval storage contract before the first review-only experiment scope consumes them.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateManifest.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateResult.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateRunner.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IHeldOutEvalStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalStore.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalGateManifestTests.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `docs/SELF_IMPROVEMENT_EVAL_GATE_MANIFEST_2026-05-18.md`
- `docs/C_PHASE147_EVAL_GATE_MANIFEST_2026-05-18.md`

Not touched:

- No experiment scope.
- No proposal producer.
- No held-out eval content.
- No eval execution.
- No model adapter.
- No capability flag.
- No sprite, content, filesystem mutation, or network surface.

## Implemented

- Added `EvalGateManifest.Default()` with all eleven required gates: Build, Unit tests, Benchmark suite, In-distribution eval, Held-out eval, Performance, Scope hash, Dry-run, Backup, Post-proof, and Rollback.
- Added `EvalGateResult` with only `Passed`, `Failed(reason)`, and `NotApplicable(reason)`.
- Added `EvalGateRunner.Preview()` as an orchestrator stub that returns `NotApplicable("no_eval_run_wired_v0")` for every gate.
- Added kill-switch handling in the runner so active kill switch returns `NotApplicable("kill_switch=true")`.
- Added `IHeldOutEvalStore` and `HeldOutEvalStore` as the isolated read contract for future held-out eval content.
- Added store guardrails so held-out list/read operations return no content while the kill switch is active.
- Added tests proving the manifest has all eleven gates, the result type has only the three allowed result shapes, runner preview does not read held-out content in this phase, and forbidden surfaces do not expose the held-out store types.

## Safety Boundaries

- No actual eval runs are wired.
- No proposal consumes the manifest.
- No held-out content is created or seeded.
- Held-out content is not logged, summarized, or exposed to PET TASKS, shell views, autonomous scopes, tool registry surfaces, or evidence summaries.
- The runner does not read held-out content in C-PHASE 147.
- The kill switch prevents held-out store reads and causes runner preview gates to report `NotApplicable`.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalGate|HeldOutEval"`: 7/7 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1092/1092 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] `IHeldOutEvalStore` is not exposed through `ToolRegistry`, `EvidenceSummaryService`, or `AutonomousScopeService`.
- [x] Manifest includes all eleven gates.
- [x] Gate result type only allows `Passed`, `Failed`, or `NotApplicable`.
- [x] No actual eval run was wired.
- [x] No held-out content was seeded.
- [x] No model, network, tool, sprite, or content mutation path was added.

## Next Phase

C-PHASE 146 is `Sprite-repair-batch-proposal dry-run scope (review-only)`. Auto-continue is No, so C-PHASE 146 should not start until the user explicitly approves and provides the C-PHASE 146 prompt.
