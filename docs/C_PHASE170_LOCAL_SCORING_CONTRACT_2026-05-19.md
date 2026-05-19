# C-PHASE 170 — Local Scoring Provider Contract

## Goal

Add a default-deny local scoring provider contract that can later support supervised self-improvement scoring without exposing raw prompt text, opening network sockets, calling Ollama, reading eval stores, or wiring any producer.

Base commit recorded at phase start:

`0f1896bbbe741e110d9dc6a158c8a374427b737b`

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/LocalScoringRequest.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/LocalScoringResult.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/ILocalScoringProvider.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/NotConfiguredScoringProvider.cs`
- `vnext/tests/Wevito.VNext.Tests/LocalScoringProviderContractTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`

Files intentionally not touched:

- Any proposal loop producer
- Any eval store
- Any model adapter
- Any HTTP, socket, Ollama, or loopback implementation
- Any audit packet kind registry
- Any mutation or apply runner

## Implemented

- Added `LocalScoringRequest` with only `PromptSha256` and `Rubric`.
- Added `LocalScoringResult` with `Refused` and `Scored` result shapes.
- Added `ILocalScoringProvider` as an abstract record contract.
- Added `NotConfiguredScoringProvider`, which always refuses by default.
- Added `local_scoring_provider_enabled` to `CapabilityFlagInventory` with default `False`.
- Added `ShellCompositionRoot.CreateLocalScoringProvider`, returning the default-deny provider only.
- Added tests for default refusal, KillSwitch refusal, composition root shape, capability flag default, request shape, no network/Ollama source tokens, no eval store references, and no producer consumption.

## Safety Boundaries

- No producer consumes `ILocalScoringProvider`.
- No scoring source imports or references `System.Net.Http`, `System.Net.Sockets`, `127.0.0.1`, `localhost`, or `Ollama`.
- `LocalScoringRequest` carries no raw prompt text.
- `local_scoring_provider_enabled` defaults `False`.
- KillSwitch active returns `Refused("kill_switch=true")`.
- Default provider returns `Refused("local_scoring_provider_not_configured")`.
- No held-out or in-distribution eval store reference was added.
- No model call, network call, training path, audit write, or mutation path was added.

## Validation

Focused validation:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalScoring|PlainLanguage|SelfImprovement" --no-restore`

Result: passed, `255/255`.

Build:

`dotnet build .\vnext\Wevito.VNext.sln`

Result: passed, `0` warnings, `0` errors.

Full tests:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Result: passed, `1284/1284`.

Publish:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

Result: passed.

Whitespace:

`git diff --check`

Result: passed.

## Stop-Gate Checklist

- [x] Scoring source files do not import `System.Net.Http`.
- [x] Scoring source files do not import `System.Net.Sockets`.
- [x] Scoring source files do not reference `Ollama`.
- [x] Scoring source files do not reference `127.0.0.1`.
- [x] Scoring source files do not reference `localhost`.
- [x] No producer wires `ILocalScoringProvider`.
- [x] `LocalScoringRequest` has no `Prompt` property.
- [x] `local_scoring_provider_enabled` defaults `False`.
- [x] Validation commands pass.

All stop gates are false.

## Next Phase

C-PHASE 171 — Loopback Ollama scoring provider type.

Auto-continue: No.
