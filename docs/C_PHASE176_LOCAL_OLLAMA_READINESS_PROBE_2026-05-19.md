# C-PHASE 176: Local Ollama Readiness Probe

## Goal

Add a default-off, loopback-only readiness probe for the local Ollama runtime. The probe checks only `GET /api/tags` on `127.0.0.1:<port>`, records whether the configured model appears in the returned model list, and never sends a prompt, asks for generation, scores anything, trains anything, or mutates files.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Readiness/LocalOllamaReadinessSnapshot.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Readiness/LocalOllamaReadinessProbeService.cs`
- `vnext/tests/Wevito.VNext.Tests/LocalOllamaReadinessProbeServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/LocalOllamaReadinessProbeServiceTextScanTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/IScoringHttpClient.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Scoring/DefaultScoringHttpClient.cs`
- `vnext/tests/Wevito.VNext.Tests/FakeScoringHttpClient.cs`
- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionEvalStoreVisibilityTests.cs`

Not touched:

- No model adapter wiring was changed.
- No autonomous loop, scheduler, first-launch handler, or background tick invokes the probe.
- No held-out or in-distribution eval store access was added.
- No apply runner, training, prompt generation, or mutation pathway was added.

## Implemented

- Added `self_improvement_local_runtime_probe` to `SelfImprovementPacketKinds`.
- Added a plain-language explanation for the new packet kind.
- Added `local_ollama_readiness_probe_enabled=false` and `local_ollama_readiness_probe_endpoint=""` to `CapabilityFlagInventory`.
- Added `IScoringHttpClient.GetAsync` and a `DefaultScoringHttpClient.GetAsync` implementation that repeats the existing `127.0.0.1` loopback enforcement.
- Added `LocalOllamaReadinessProbeService`, which:
  - writes no packet when KillSwitch is active;
  - writes no packet when the capability flag is absent/off;
  - refuses non-`127.0.0.1` endpoints without writing a packet;
  - calls only `GET http://127.0.0.1:<port>/api/tags` when enabled;
  - parses only model names from the returned `models[]` list;
  - writes one honest audit packet on attempted probe with `DidUseNetwork=true`, `DidUseHostedAi=false`, `DidUseLocalModel=false`, `DidMutate=false`;
  - never stores the raw response body in the packet summary.
- Added a read-only `Local runtime readiness (read-only)` Evidence expander that displays the latest already-written probe packet. The expander has no button, toggle, checkbox, click handler, or probe invocation.
- Extended visibility tests so the readiness probe service cannot gain held-out or in-distribution eval-store dependencies.

## Safety Boundaries

- Default state is off: `local_ollama_readiness_probe_enabled=false`.
- KillSwitch active means no packet and no HTTP call.
- Non-loopback endpoint means no packet and no HTTP call.
- The service depends on `IScoringHttpClient`, not `DefaultScoringHttpClient`; production composition constructs the default client only inside `ShellCompositionRoot`.
- Test coverage uses `FakeScoringHttpClient`; tests do not construct the real HTTP client or open sockets.
- The Tool Popup expander is read-only and cannot run the probe.
- The probe sends no prompts and contains no `/api/generate`, `/api/chat`, or `/api/embeddings` endpoint.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalOllamaReadinessProbe|OllamaLoopback|LocalScoring|PlainLanguage|SelfImprovement"`: passed, 306/306.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1355/1355.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Probe sends no prompt and uses only `GET /api/tags`.
- [x] Probe is not invoked from a background loop, scheduler, or first-launch handler.
- [x] Probe writes no packet when flag is off.
- [x] Probe writes no packet when KillSwitch is active.
- [x] Tests do not construct `DefaultScoringHttpClient`.
- [x] Probe refuses hosts other than `127.0.0.1`.
- [x] Tool Popup expander contains no Button, ToggleSwitch, CheckBox, or click handler.
- [x] Validation commands pass.

## Next Phase

C-PHASE 177: Apply-block explainer panel (read-only). Auto-continue is No.
