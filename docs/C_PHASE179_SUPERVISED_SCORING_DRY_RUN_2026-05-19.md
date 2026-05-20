# C-PHASE 179 - Supervised Scoring Dry-Run

## Goal

Add a default-off supervised self-improvement scoring dry-run that can exercise the configured `ILocalScoringProvider` without exposing raw prompt text, reading eval stores, mutating files, or enabling apply behavior.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/SupervisedScoringDryRunService.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/SupervisedScoringDryRunResult.cs`
- `vnext/tests/Wevito.VNext.Tests/SupervisedScoringDryRunServiceTests.cs`

Extended:

- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/LocalScoringProviderContractTests.cs`
- `vnext/tests/Wevito.VNext.Tests/OllamaLoopbackScoringProviderTests.cs`

Not touched:

- No shell UI was changed.
- No apply runner was added.
- No model adapter default was changed.
- No held-out or in-distribution eval store API was changed.
- No network path was added outside the existing injectable `ILocalScoringProvider` contract.

## Implemented

- Added `SupervisedScoringDryRunService.EnabledSetting = supervised_scoring_dry_run_enabled`, defaulted off in `CapabilityFlagInventory`.
- Added `SelfImprovementPacketKinds.ScoringDryRun = self_improvement_scoring_dry_run` and registered it with `PlainLanguageExplainer`.
- Added a dry-run service that:
  - refuses without writing a packet when Stop Everything is active;
  - refuses without writing a packet when the dry-run flag is off;
  - reads the latest awaiting-approval audit row via read-only SQLite;
  - extracts only `scopeHash` / `scope_hash` from the awaiting-approval artifact;
  - builds `LocalScoringRequest` with only `PromptSha256 = sha256(operationId + "|" + scopeHash)` and `Rubric = supervised_self_improvement_dry_run_v1`;
  - writes exactly one `self_improvement_scoring_dry_run` packet only after a provider is invoked.
- Added `ShellCompositionRoot.CreateSupervisedScoringDryRunService(...)` as an explicit factory that requires a caller-provided `ILocalScoringProvider`, so production defaults remain `NotConfiguredScoringProvider`.
- Added tests for KillSwitch, default-off refusal, missing awaiting-approval refusal, default provider refusal, Ollama flag refusal, fake loopback scoring success, request hashing, packet summary safety, explainer coverage, capability defaults, and eval-store visibility.
- Updated two older guard tests to recognize the intentional C-PHASE 179 `ScoringDryRun` exception while still blocking Ollama-specific packet kinds and accidental broader scoring-provider wiring.

## Safety Boundaries

- No raw prompt text is stored in `LocalScoringRequest`, packet summary, artifacts, or test fixtures.
- Packet summaries do not include score numbers, raw rubric bodies, raw prompts, or raw scope hashes.
- `DidUseHostedAi` is always false.
- `DidMutate` is always false.
- `DidUseNetwork` and `DidUseLocalModel` are true only when a provider returns a scored result with a non-empty model identity.
- The service does not reference `IHeldOutEvalStore`, `HeldOutEvalStore`, `IInDistributionEvalStore`, or `InDistributionEvalStore`.
- The default production scoring provider remains `NotConfiguredScoringProvider`.
- No apply behavior was added or enabled.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SupervisedScoringDryRun|OllamaLoopback|LocalScoring|PlainLanguage|SelfImprovement"`: passed, 310/310.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1381/1381.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Packet is not written when the flag is off or KillSwitch is active.
- [x] `LocalScoringRequest` carries no raw prompt text.
- [x] Packet summary contains no score number, raw rubric body, raw scope hash, or raw prompt text.
- [x] Tests use fake scoring HTTP only; no real socket is opened.
- [x] Default production composition does not wire `OllamaLoopbackScoringProvider` as the active provider.
- [x] Validation commands passed.

## Next Phase

C-PHASE 180 - Apply-runner design freeze document - Auto-continue=No.
