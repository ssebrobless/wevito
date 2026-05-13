# C-PHASE 82: Golden Eval Dataset And Regression Gate

## Goal

Add a checked-in synthetic golden eval that can catch local retrieval/reasoning regressions before future learning, tuning, or autonomous changes are promoted.

## Scope

- Added a synthetic 50-question / 15-document golden eval dataset under `vnext/content/local-ai/eval-golden/`.
- Added `EvalRegressionGate` with dataset sha256 validation, recall/MRR/citation thresholds, report output, and `golden_eval` audit rows.
- Added `LearningEvalService.RunGoldenEval`.
- Added `tools/run-golden-eval.ps1` for local/CI execution.
- Added a non-required GitHub Actions workflow for local eval checks.
- Added regression, dataset hash, baseline overwrite, KillSwitch, and checked-in dataset smoke tests.

## Implemented

- Baseline metrics are locked to the deterministic current fixture behavior: recall@1 `0.98`, recall@3 `1.0`, MRR `0.99`, citation coverage `1.0`.
- Dataset hash excludes `baseline.json`; baseline updates require the explicit `-UpdateBaseline` script flag or `updateBaseline: true` in code.
- Regression triggers match the plan: recall@1 drop over `0.02`, MRR drop over `0.02`, citation coverage drop over `0.05`, or citation coverage below `0.60`.
- `LocalTuningRunner` can optionally run the golden gate and rolls back if the gate fails.

## Safety Boundaries

- Dataset is synthetic and checked in.
- No hosted AI calls.
- No web/network calls.
- No model downloads.
- No runtime/source PNG mutation.
- Baseline rewrite is explicit only.
- GitHub workflow is added but not made a required branch protection check in this phase.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalRegression|GoldenEval|LearningEval|Tuning"`: passed, 13/13.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-golden-eval.ps1 -ArtifactRoot .\vnext\artifacts\eval-golden\`: passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 493/493.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 83 may start after this PR is reviewed and merged. It adds self-improvement reporting and rollback proposals on top of the audit ledger and golden eval evidence.
