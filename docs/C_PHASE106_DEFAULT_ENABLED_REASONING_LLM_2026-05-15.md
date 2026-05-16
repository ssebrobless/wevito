# C-PHASE 106 — Default-Enabled Reasoning LLM

Date: 2026-05-15

Decision IDs: `Q15-L2`, `I-Reframe`

## Goal

Make Wevito's normal model-provider posture local-first by default: `LocalOnly` mode, localhost Ollama endpoint, and `qwen2.5:7b-instruct-q4_K_M` as the default reasoning model. Hosted AI remains disabled unless a later phase explicitly changes that contract.

## Scope

Implemented:

- Changed fresh-state and hydrated-settings defaults from disabled/deterministic to `LocalOnly` + `ollama`.
- Changed the default Ollama model to `qwen2.5:7b-instruct-q4_K_M`.
- Added `OllamaModelBootstrapService`, mirroring the existing local-runtime onboarding shape:
  - one startup probe only,
  - no model download,
  - no hosted provider call,
  - evidence packet on missing runtime or missing model,
  - kill-switch skip before any packet write.
- Added evidence packet types:
  - `model_bootstrap_required`
  - `model_bootstrap_runtime_absent`
- Added both packet kinds to `PlainLanguageExplainer.KnownPacketKinds`.
- Added Settings UI status text for the reasoning model and a user-triggered `Pull default model` button.
- Added setup helpers:
  - `tools/install-ollama.ps1`
  - `tools/pull-default-model.ps1`
- Added documentation for the default local model under `vnext/content/local-models/llm/qwen2.5-7b/README.md`.

## Safety Boundaries

- No hosted-AI provider calls were added.
- No model weights were downloaded by tests or by Codex.
- The pull script is user-triggered only and calls `ollama pull qwen2.5:7b-instruct-q4_K_M`.
- Bootstrap evidence flags are honest:
  - `did_use_network=false`
  - `did_use_hosted_ai=false`
  - `did_use_local_model=false`
  - `did_mutate=false`
- If Ollama is absent or the model is missing, Wevito records the condition and continues with deterministic local fallback behavior.

## Stop-Gate Review

- `ModelProviderMode` did not flip to hosted or any hosted-capable default: false.
- No code path auto-downloads model weights: false.
- Startup bootstrap probe does not loop more than once per service instance: false.
- `pull-default-model.ps1` does not perform hidden file operations; it only invokes user-approved `ollama pull`: false.
- New packet kinds are present in `PlainLanguageExplainer.KnownPacketKinds`: false.

No stop gate is true.

## Validation

Passed:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "OllamaModelBootstrap|DefaultStateFactoryReasoningModel|PlainLanguage"
```

Result: 58 / 58 passed.

```powershell
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed with 0 warnings and 0 errors.

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: 621 / 621 passed.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed; shell, broker, dev controller, and soak driver published.

## Next Phase

Stop here for user review. C-PHASE 106 has `Auto-continue=No`; do not begin C-PHASE 107 until this PR is reviewed.
