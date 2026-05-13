# C-PHASE 79: Local Runtime Onboarding

## Goal

Make Wevito able to explain and prepare its local model runtime path without depending on hosted AI, hidden installs, or automatic model downloads.

## Scope

- Added a report-only local runtime onboarding service.
- Added a safe local runtime install helper script with timestamped logs.
- Added local-only provider routing for Disabled, Ollama, ONNX Phi fallback, and deterministic fallback.
- Added an ONNX Phi adapter seam that is default-off and degrades to deterministic output instead of loading weights or running inference.
- Added runtime probe history and readable Ollama status strings.
- Updated the Local AI Research tool definition to document the disabled in-process fallback.

## Implemented

- `LocalRuntimeOnboardingService` reports installed/runtime/model status, returns ordered install steps, and records `runtime_onboarding` audit rows.
- `install-local-runtime.ps1` supports `-PlanOnly`, logs under `%LOCALAPPDATA%/Wevito/runtime-install/`, probes `winget` and `Get-Command`, and only runs install/pull commands when the user explicitly launches the script without plan-only mode.
- `ModelProviderModeService.DecideRoute` keeps hosted providers blocked in this phase and routes `LocalOnly` to Ollama, ONNX Phi, or deterministic fallback.
- `LocalModelProviderRouterAdapter` wires the active adapter path into PET TASKS model preview dispatch without enabling hosted calls.
- `OnnxPhiLocalModelAdapter` preserves the future in-process seam but does not add model weights, download dependencies, or perform real inference in this phase.
- `ToolPopupWindow` now surfaces the in-process fallback setting in the Local AI Runtime panel.

## Safety Boundaries

- No hosted AI calls.
- No model downloads during validation.
- No ONNX Runtime GenAI package or downloaded weights were added.
- No runtime/source PNG mutation.
- No hidden web access, hidden local file access, hidden training, or hidden tool execution.
- `local_runtime_inproc_enabled` defaults to `false`.
- The ONNX Phi path remains deterministic until a later explicit dependency/weights approval phase.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Onboarding|OnnxPhi|Ollama|LocalRuntime|ModelProvider"`: passed, 24/24.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\install-local-runtime.ps1 -PlanOnly`: passed; wrote a plan-only log without executing install/pull commands.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 474/474.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 80 should not start automatically. It is the first hybrid retrieval/rerank surface and should begin only after this PR is reviewed and merged.
