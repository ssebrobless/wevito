# C-PHASE 112 — Benchmark Suite v1

## Goal

Implement the first local benchmark surface for Wevito's reframe as a local AI assistant. This phase covers Q17 v1 axes: chat correctness, tool-use correctness, retrieval accuracy, safety regression, and performance regression.

## Scope

- Added immutable v1 benchmark folders under `vnext/content/benchmarks/v1/`.
- Added `BenchmarkSuiteService`, `IBenchmarkKind`, v1 benchmark result/request records, `GraderTriad`, and `BenchmarkRegressionGate`.
- Added five deterministic benchmark kinds: chat, tool-use, retrieval, safety, and perf.
- Added `BenchmarkDailyChore` as the daily capability benchmark seam for chat/tool-use/retrieval.
- Added `benchmark_run`, `benchmark_regression_detected`, and `benchmark_baseline_updated` packet kinds to `PlainLanguageExplainer`.
- Added a Benchmarks tool surface and home-panel ambient benchmark badge.
- Added the missing 2026-05-15 Claude/decision docs to this branch so the phase contract is available from the repo.

## Safety Boundaries

- No hosted AI calls.
- No model downloads.
- No runtime sprite, source-board, or asset mutation.
- Deterministic grading is the metric of record.
- Production LLM grading remains advisory-only metadata in the design and is not authoritative in this implementation.
- Empty `approved/` benchmark cases return `NoBaseline`; the suite does not claim passing until reviewed cases exist.

## Implemented Behavior

- `BenchmarkSuiteService.Run(...)` loads cases from `vnext/content/benchmarks/v1/approved/*.json`, runs registered axes, writes `result.json` and `summary.md`, and records a `benchmark_run` evidence packet when a ledger is provided.
- `BenchmarkRegressionGate` implements FR4 tiering:
  - safety failure -> `HaltStopCard`
  - perf p95 regression above 20% -> `Halt`
  - capability regression above 15% -> `Halt`
  - capability regression 5-15% -> `TaskCard`
  - below 5% -> `Log`
- Tool popup now has a Benchmarks panel describing axes, cadence, and no-baseline status.
- Home panel now exposes a `BENCH --` ambient badge that opens the Benchmarks panel.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Benchmark|PlainLanguage"` passed: 109/109.
- `dotnet build .\vnext\Wevito.VNext.sln` passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 739/739.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Stop Gate Review

- Production LLM grading authoritative: false.
- Benchmark cases mutable after commit by this service: false.
- Safety axis regression fails to halt: false.
- Empty approved cases claim passing: false.
- New packet kind missing from `PlainLanguageExplainer`: false.

## Next Phase

C-PHASE 112 is Auto-continue = No. Stop for user review of benchmark grading semantics before moving to C-PHASE 113.
