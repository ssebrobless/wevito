# C-PHASE 72 Local Model Runtime Integration

Generated: 2026-05-12

## Goal

Add an Ollama-first local model runtime seam so Wevito can call a local model when explicitly configured, while safely degrading to the deterministic local scaffold when Ollama is unavailable. This phase keeps the runtime local-first and does not enable hosted AI.

## Scope

Implemented:

- `LocalRuntimeProbeService` for localhost-only Ollama probing via `GET /api/tags`.
- `OllamaLocalModelAdapter` for OpenAI-compatible local chat calls via `POST /v1/chat/completions`.
- `ModelInferenceEvidencePacket` for prompt hash, response hash, runtime id, model id, latency, fallback status, and local/hosted/network flags.
- `ModelProviderModeService` endpoint/model settings for:
  - `local_runtime_ollama_endpoint`
  - `local_runtime_ollama_model`
- Local AI settings status text in `ToolPopupWindow`.
- `tool_definitions.json` metadata describing the local Ollama provider.
- Optional local-model synthesis routing for `localResearch` and `codeReview` previews when a local adapter is explicitly injected.

## Safety Boundaries

- The default runtime remains disabled/deterministic unless a local adapter is explicitly supplied and local mode is configured later.
- No hosted provider is called.
- No API keys or Windows Credential Manager entries are read.
- Only localhost, `127.0.0.1`, or `::1` endpoints are permitted.
- Non-local endpoints are rejected before HTTP is attempted.
- `Quiet` and `PetOnly` supervisor modes make the runtime probe dormant.
- `KillSwitchService` blocks the probe and adapter.
- If Ollama is unavailable, the adapter writes evidence and falls back to `LocalModelAdapter`.
- Localhost model calls are recorded as `didUseLocalModel=true`, `didUseNetwork=false`, and `didUseHostedAi=false`.
- ONNX/LLamaSharp are not wired in this phase; they remain opt-in follow-ups.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Ollama|LocalRuntime|Model"`  
  Result: 27/27 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`  
  Result: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`  
  Result: 375/375 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`  
  Result: passed. Broker and shell published under `vnext/artifacts/`.

## Next Phase

C-PHASE 73 is a stop-and-ask phase for local training/tuning. It should not begin until this local runtime seam is reviewed. The next phase must keep tuning opt-in, rollbackable, and eval-gated, and it must not run LoRA/Python training scripts automatically.
