# C-PHASE 109 - Tool Registry First-Class

Date: 2026-05-15
Branch: claude-implementation/c-phase-109-tool-registry-first-class
Decision IDs: Q15 tool-use mid-chat, Q21-U-2, Q22-R-4, Q17-Option-C, Q22-P-3, I-Reframe

## Goal

Expose existing report-first PET TASKS adapters as AI-callable local tools with structured schemas, policy gating, kill-switch protection, and Ollama-compatible tool descriptors.

## Implemented

- Added `ToolRegistry` and core `ToolDescriptor` records for AI-callable tool definitions.
- Registered existing preview/report adapters: `localDocs`, `localResearch`, `spriteAudit`, `petState`, `assetInventory`, `codeReview`, `codePatchPlan`, `buildProof`, `translateText`, `audioAssist`, `screenCapture`, and `petMemory`.
- Registered built-in placeholder surfaces for `bookmark_for_benchmark` and `pin_message` without enabling hidden mutation.
- Added `ToolInvocationService` so every invocation evaluates `UnifiedPolicyService` before adapter dispatch and records start/completion evidence.
- Added `OllamaToolFormatAdapter` for OpenAI-compatible function schema payloads and tool-call parsing.
- Extended `OllamaLocalModelAdapter` so local Ollama chat payloads can advertise the visible tool registry.
- Added `ToolInvocationStreamingBridge` so stream consumers can pause on tool-call events, invoke safely, and resume with a result event.
- Added settings-panel tool registry checkboxes. Disabled tools are hidden from the LLM-visible tool list.
- Declared registry tools in `vnext/content/tool_definitions.json`.
- Added plain-language explanations for `tool_invocation_started`, `tool_invocation_completed`, `tool_registry_loaded`, and `tool_registry_setting`.

## Safety Boundaries

- No hosted AI calls were made.
- No model weights were downloaded.
- No sprite/runtime/source PNGs were touched.
- No tool execution mode was broadened; existing adapters remain report-first or approval-gated.
- `buildProof` and `screenCapture` are advertised as approval-required and are blocked by policy until approved.
- Disabled tools are removed from the LLM-visible descriptor list.

## Stop Gates

- Tool invocation bypasses `UnifiedPolicyService`: false, covered by `ToolInvocationServiceTests.ConsultsUnifiedPolicyServiceFirst`.
- Registry exposes a tool not declared in `tool_definitions.json`: false, covered by `ToolRegistryTests.RegistryRefusesUndeclaredContentTools`.
- Disabled tools still appear in the LLM's tool list: false, covered by `ToolRegistryTests.UserDisabledToolsHiddenFromLlm`.
- Tool failure crashes the streaming bridge instead of returning error: false, covered by `ToolInvocationStreamingBridgeTests.HandlesToolFailure`.
- Kill switch does not halt tool invocations: false, covered by `ToolInvocationServiceTests.RefusesWhenKillSwitchActive`.
- New packet kinds missing from `PlainLanguageExplainer`: false, covered by `PlainLanguageExplainerTests.CoversToolRegistryKinds`.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ToolRegistry|ToolInvocation|OllamaToolFormat|PlainLanguage"`: passed, 83/83.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 672/672.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 110 should implement the pet/AI isolation contract on top of this registry. This phase is Auto-continue=No and stops after PR creation for review.
