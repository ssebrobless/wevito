# C-PHASE 160 - In-Distribution Eval Store

## Goal

Add an empty, local-only in-distribution eval case store that proposal loops may use later, while keeping it strictly type-separated from held-out eval data.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IInDistributionEvalStore.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/InDistributionEvalCase.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/InDistributionEvalStore.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionVersusHeldOutTypeSeparationTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`

Not touched:

- `HeldOutEvalStore.cs`
- `IHeldOutEvalStore.cs`
- model adapters
- capability flags
- sprite assets
- held-out eval case content

## Implemented

- Added an `InDistributionEvalCase` schema with the required `case_kind` discriminator value `in_distribution`.
- Added an `IInDistributionEvalStore` contract type and an `InDistributionEvalStore` implementation.
- The store lists only top-level `*.json` files under its configured root.
- The store skips malformed JSON, non-JSON files, and JSON files missing `case_kind: in_distribution`.
- The store refuses path traversal reads.
- The store returns empty/null while `KillSwitchService` is active.
- `ShellCompositionRoot` now exposes a factory for the default local in-distribution eval root:
  `%LOCALAPPDATA%\WevitoVNext\eval\in-distribution`.
- Added tests proving the in-distribution store does not read held-out-format files.
- Added type-separation tests proving the in-distribution and held-out contracts/stores remain distinct and no self-improvement constructor mixes both store types.

Implementation note:

- `IInDistributionEvalStore` is implemented as a tiny abstract contract type rather than a C# interface so the existing kill-switch coverage guard can verify a public `KillSwitchService` constructor without changing the test allow-list in this phase.

## Safety Boundaries

- No producer consumes this store yet.
- No held-out eval store API changed.
- No held-out eval case JSON was added.
- No capability flag was introduced or changed.
- No model, network, training, mutation, or audit-writing path was added.
- Held-out-format JSON files remain invisible to the in-distribution store unless they explicitly carry the in-distribution discriminator.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InDistributionEval|HeldOutEval|PlainLanguage|SelfImprovement"`: passed, 269/269.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1202/1202.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No class in `Wevito.VNext.Core.SelfImprovement` takes both held-out and in-distribution eval stores.
- [x] The in-distribution store refuses path traversal outside the configured root.
- [x] `InDistributionEvalCase` includes the `case_kind` discriminator.
- [x] Held-out-format files are not read by the in-distribution store.
- [x] No held-out case JSON was committed.
- [x] No capability flag default changed.

## Next Phase

C-PHASE 161 - EvalGateRunner v1 - Auto-continue=No.
