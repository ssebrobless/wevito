# C-PHASE 171: Ollama Loopback Scoring Provider

## Goal

Add a default-off, unwired local scoring provider type that can score via an Ollama loopback endpoint in a later approved phase, without enabling model calls, network activity, mutation, training, or apply behavior today.

Base commit recorded for this phase: `071ffac8ff1af4cba5024abf7294d03e874ef631`.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/IScoringHttpClient.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/DefaultScoringHttpClient.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/OllamaLoopbackScoringProvider.cs`
- `vnext/tests/Wevito.VNext.Tests/FakeScoringHttpClient.cs`
- `vnext/tests/Wevito.VNext.Tests/OllamaLoopbackScoringProviderTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/tests/Wevito.VNext.Tests/LocalScoringProviderContractTests.cs`

Not touched:

- No shell producer wiring.
- No audit packet kind.
- No snapshot, replay, eval, or apply runner surface.
- No sprite/runtime asset.
- No capability flag default was flipped on.

## Implemented

- Added `OllamaLoopbackScoringProvider`, gated by both `local_scoring_provider_enabled` and `local_scoring_provider_ollama_enabled`.
- Added loopback-only endpoint enforcement: non-`127.0.0.1:<port>` endpoints are refused before any HTTP client call.
- Added deterministic default fallback values inside the provider only:
  - endpoint fallback: `127.0.0.1:11434`
  - model fallback: `qwen2.5:7b-instruct-q4_k_m`
- Kept capability inventory defaults safe:
  - `local_scoring_provider_ollama_enabled=False`
  - endpoint and model settings are empty by default, with fallback values described in plain language.
- Added `DefaultScoringHttpClient` as the eventual loopback transport wrapper.
- Added `FakeScoringHttpClient` and tests so this phase never constructs the real HTTP wrapper and never opens a socket.
- Updated the prior local-scoring default-deny contract test so it continues to verify the default-deny contract files while allowing the new explicitly planned HTTP wrapper file.

## Safety Boundaries

- No producer constructs or wires `OllamaLoopbackScoringProvider`.
- Tests assert no production producer references the provider outside the capability inventory.
- Tests assert the real `DefaultScoringHttpClient` is not constructed by test code.
- Non-loopback endpoints are refused before any fake HTTP call is made.
- The provider honors `KillSwitchService`.
- The provider refuses unless both capability flags are explicitly true.
- No audit ledger writes are introduced.
- No hosted AI path is introduced.
- No model call is made by validation.
- No training, tool execution, code mutation, asset mutation, or apply behavior is introduced.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "OllamaLoopback|LocalScoring|PlainLanguage|SelfImprovement" --no-restore`
  - Passed: `265/265`
- `dotnet build .\vnext\Wevito.VNext.sln`
  - Passed with `0` warnings and `0` errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - Passed: `1294/1294`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
  - Passed. Broker, shell, dev-controller, and soak-driver artifacts published.
- `git diff --check`
  - Passed. Only line-ending warnings were reported for touched files.

## Stop-Gate Checklist

- [x] Provider remains unwired from production producer paths.
- [x] Provider refuses unless capability flags are explicitly enabled.
- [x] Capability flags default off or empty.
- [x] Non-loopback endpoint is refused.
- [x] Tests do not construct the real HTTP wrapper.
- [x] No new audit packet kind was added.
- [x] No hosted AI call was added.
- [x] No model call was made.
- [x] No network socket was opened by tests.
- [x] No mutation, training, or apply behavior was added.
- [x] Validation passed.

All stop gates are false.

## Next Phase

C-PHASE 172 — Capabilities & gates dashboard panel — Auto-continue=No.
