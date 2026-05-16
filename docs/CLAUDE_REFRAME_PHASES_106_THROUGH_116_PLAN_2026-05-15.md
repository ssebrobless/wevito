# Wevito Reframe-Phase Plan: C-PHASE 106 through 116

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort
Companion files:
- `docs/DECISION_LEDGER_2026-05-15.md` (authoritative source for every decision ID referenced below)
- `docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md` (gap phases, separately authored)
- `docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_CODEX_PHASE_PROMPTS_2026-05-15.md` (to be authored)

## 0. Purpose

These eleven phases implement the **identity reframe**: wevito IS a local AI
assistant, the pet simulator is the cosmetic visual wrapper, the chat is the
primary user interface, and the three "pets" are agent slots the AI dispatches
work to. See [DECISION_LEDGER_2026-05-15.md §7](DECISION_LEDGER_2026-05-15.md)
for the full reframe rationale.

The reframe phases land on top of the existing `C-PHASE 85d` residual + the
gap phases (97-105). When all of 106-116 are merged, wevito is functionally
a Claude/GPT-equivalent local assistant with built-in tools, running an
always-on reasoning model, with the pet visuals as a never-affected ambient
surface.

## 0.1 Hard Invariants (carried forward unchanged)

```text
- Wevito remains its own local AI; no hosted GPT/Claude/Codex/Gemini at runtime.
- No hidden web access, no hidden training, no hidden file access.
- Every risky capability stays default-off OR ships with empty initial state.
- Pets remain visually regular pet-sim characters.
- Always-on behavior must not interfere with the user's PC.
- KillSwitch / audit ledger / evidence packets / rollback / supervisor modes
  remain load-bearing across every phase below.
- Every new packet kind MUST appear in PlainLanguageExplainer.KnownPacketKinds.
- Every new local-model adapter MUST safely degrade when runtime is absent.
- Every new mutation pathway MUST go through GuardedMutationService.
- Pet sim experience (visuals + gameplay) MUST NOT be affected by AI workload.
```

## 0.2 Phase template (same as gap-phase plan)

```text
Goal             (1-2 sentences)
Scope            (bulleted, concrete; named services/methods)
Pattern          (existing service Codex must imitate)
Files likely    (exact paths to edit)
  touched
New files       (exact paths to create)
Tests           (named test methods + assertion shape)
Validation       (literal copy-pasteable PowerShell)
  commands
Artifacts        (exact paths produced)
Stop gates       (boolean conditions; Codex halts on any true)
Rollback         (single-action revert)
Commit/PR        (branch, commit-prefix, PR title)
Auto-continue?   (Yes/No with one-line reason)
```

---

## C-PHASE 106 — Default-Enabled Reasoning LLM

**Goal:** Flip `ModelProviderMode` from `Disabled` default to `LocalOnly`
default with Qwen 2.5 7B Q4_K_M as the bundled-by-default local model.
Wevito always has a reasoning LLM available the moment the user opens it,
per Q15-L2.

**Scope:**

- Update `ModelProviderMode` default in `DefaultStateFactory.cs` from
  `Disabled` to `LocalOnly`.
- Update default Ollama endpoint setting `local_runtime_ollama_endpoint`
  to `http://127.0.0.1:11434` and model setting `local_runtime_ollama_model`
  to `qwen2.5:7b-instruct-q4_K_M`.
- Add `OllamaModelBootstrapService` in `Wevito.VNext.Core`:
  - Probes Ollama on startup (one shot, via `LocalRuntimeProbeService`).
  - If Ollama is reachable BUT the configured model is not pulled, emits
    a `model_bootstrap_required` audit packet with a plain-language
    instruction ("Run `ollama pull qwen2.5:7b-instruct-q4_K_M` to enable
    chat") and surfaces it as a Stop card.
  - If Ollama is unreachable, emits a `model_bootstrap_runtime_absent`
    packet linking to install instructions; chat falls back to
    `LocalModelAdapter` deterministic mode (no real reasoning, but
    wevito stays alive).
  - Service runs once per startup, never spams.
- Add `tools/install-ollama.ps1` helper (no auto-download; instructions
  + clear next-step text, mirroring `tools/install-local-embedder.ps1`).
- Add `tools/pull-default-model.ps1` that invokes `ollama pull` with the
  configured default model name.
- Document the model choice + minimum specs in a new
  `vnext/content/local-models/llm/qwen2.5-7b/README.md`.
- Settings UI in `ToolPopupWindow` adds a "Reasoning model status" row:
  shows current model, runtime status (`Available`/`Unavailable`/`Pulling`/
  `Bootstrap required`), latency-on-last-call, and a "Pull default model"
  button that runs `pull-default-model.ps1`.

**Pattern:** Follow `LocalRuntimeOnboardingService` at
`vnext/src/Wevito.VNext.Core/LocalRuntimeOnboardingService.cs` for the
"probe → instruct → degrade" pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/DefaultStateFactory.cs
vnext/src/Wevito.VNext.Core/ModelProviderMode.cs                  (default change)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/OllamaModelBootstrapService.cs
vnext/src/Wevito.VNext.Core/OllamaModelBootstrapEvidencePacket.cs
vnext/content/local-models/llm/qwen2.5-7b/README.md
tools/install-ollama.ps1
tools/pull-default-model.ps1
vnext/tests/Wevito.VNext.Tests/OllamaModelBootstrapServiceTests.cs
vnext/tests/Wevito.VNext.Tests/DefaultStateFactoryReasoningModelTests.cs
docs/C_PHASE106_DEFAULT_ENABLED_REASONING_LLM_2026-05-15.md
```

**Tests:**

- `DefaultStateFactoryReasoningModelTests.ModelProviderModeDefaultsToLocalOnly`
- `DefaultStateFactoryReasoningModelTests.DefaultEndpointIsLoopback`
- `DefaultStateFactoryReasoningModelTests.DefaultModelIsQwen25_7b`
- `OllamaModelBootstrapServiceTests.ProbeOnceOnStartupOnly` — service does not re-probe within a single session.
- `OllamaModelBootstrapServiceTests.EmitsBootstrapRequiredWhenModelMissing`
- `OllamaModelBootstrapServiceTests.EmitsRuntimeAbsentWhenOllamaDown`
- `OllamaModelBootstrapServiceTests.RespectsKillSwitch`
- `OllamaModelBootstrapServiceTests.SuccessPathEmitsNoBootstrapPacket`
- `PlainLanguageExplainerTests.CoversModelBootstrapRequiredKind`
- `PlainLanguageExplainerTests.CoversModelBootstrapRuntimeAbsentKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "OllamaModelBootstrap|DefaultStateFactoryReasoningModel|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/artifacts/model-bootstrap/<ts>/packet.json
docs/C_PHASE106_DEFAULT_ENABLED_REASONING_LLM_2026-05-15.md
```

**Stop gates:**

- Stop if `ModelProviderMode` default flips to anything that allows hosted-AI.
- Stop if any path auto-downloads model weights.
- Stop if probe loops more than once per session.
- Stop if `pull-default-model.ps1` reads or writes outside the model folder.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; `ModelProviderMode` returns to `Disabled` default;
chat uses deterministic `LocalModelAdapter` fallback (no reasoning).

**Commit/PR:**

- Branch: `claude-implementation/c-phase-106-default-enabled-reasoning-llm`
- PR title: `C-PHASE 106: Default-enabled reasoning LLM (Qwen 2.5 7B, LocalOnly)`.
- Phase report: `docs/C_PHASE106_DEFAULT_ENABLED_REASONING_LLM_2026-05-15.md`.

**Auto-continue?** **No.** This flips a load-bearing default that affects
every subsequent phase; stop for user review.

---

## C-PHASE 107 — Chat UI

**Goal:** Replace the pet-task report area in `ToolPopupWindow` with a
multi-turn chat surface. The chat owns the popup body; existing tabs move
to a top tab strip. Implements all of Q15: streaming, mid-chat tool-use,
searchable history, interrupt, auto-title.

**Scope:**

- Add `ChatHistoryStore` in `Wevito.VNext.Core`:
  - sqlite under `%LOCALAPPDATA%/Wevito/chat-history.sqlite`
  - Schema: `session_id`, `turn_id`, `role` (user/assistant/tool/system),
    `content`, `tool_call_json` (nullable), `tool_result_id` (nullable),
    `created_at_utc`, `model_id`, `tokens_used`.
  - Append-only via sqlite triggers (mirror `AuditLedgerService` pattern).
  - FTS5 virtual table over `content` for search.
- Add `ChatSessionService`:
  - `Guid StartNewSession(string? title = null)`
  - `Guid GetCurrentSessionId()`
  - `IReadOnlyList<ChatTurn> GetTurns(Guid sessionId, int limit = 50)`
  - `IReadOnlyList<ChatSessionSummary> ListSessions(int limit = 20)`
- Add `ChatStreamingService`:
  - Orchestrates the streaming response from `IModelAdapter` (Ollama SSE).
  - Detects tool-call tokens in stream; pauses generation, invokes tool
    via `AgentToolDispatcher` (post-C-PHASE 109), resumes with result in
    context.
  - Emits `ChatStreamEvent` (token, tool_call_start, tool_call_result,
    tool_call_end, complete, cancelled).
- Add `ChatTitleService`:
  - On first turn complete in a new session, issues a short (~50 token)
    completion request to summarize the conversation into a 5-7 word title.
  - Writes title back to `ChatHistoryStore`.
- Add `ChatViewModel` and `ChatPanelXaml` in `Wevito.VNext.Shell`:
  - Replaces the existing pet-task report area in `ToolPopupWindow.xaml`.
  - Bound to `ChatStreamingService` for token-by-token rendering.
  - Tool-call inline expanders rendered as "🔧 using `<tool>`..." with
    expandable arguments + result.
  - Esc key + Stop button cancel mid-stream (cancellation token).
  - Search input above message list; uses FTS5 to filter history.
  - "New chat" button at top (per Q22-C-5).
  - "Bookmark for benchmark" button on each assistant message (per
    Q17-Option-C, integrated with C-PHASE 113).
- New packet kinds: `chat_session_started`, `chat_turn_completed`,
  `chat_turn_cancelled`, `chat_session_title_set`, `chat_search_performed`.

**Pattern:** Follow `CreativeLearningLabWindow.xaml.cs` for the
sqlite-backed view-model + xaml binding pattern. Follow
`OllamaLocalModelAdapter.SuggestAsync` for the HTTP streaming pattern;
extend it to SSE chunking by reading `Content.ReadAsStreamAsync` and
parsing `data: {...}` lines.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs       (streaming overload)
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/ChatHistoryStore.cs
vnext/src/Wevito.VNext.Core/ChatSessionService.cs
vnext/src/Wevito.VNext.Core/ChatSessionSummary.cs
vnext/src/Wevito.VNext.Core/ChatTurn.cs
vnext/src/Wevito.VNext.Core/ChatStreamEvent.cs
vnext/src/Wevito.VNext.Core/ChatStreamingService.cs
vnext/src/Wevito.VNext.Core/ChatTitleService.cs
vnext/src/Wevito.VNext.Shell/ChatViewModel.cs
vnext/src/Wevito.VNext.Shell/ChatPanel.xaml
vnext/src/Wevito.VNext.Shell/ChatPanel.xaml.cs
vnext/tests/Wevito.VNext.Tests/ChatHistoryStoreTests.cs
vnext/tests/Wevito.VNext.Tests/ChatSessionServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ChatStreamingServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ChatTitleServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ChatViewModelTests.cs
docs/C_PHASE107_CHAT_UI_2026-05-15.md
```

**Tests:**

- `ChatHistoryStoreTests.AppendIsAppendOnly`
- `ChatHistoryStoreTests.FtsFindsTurnsByContent`
- `ChatHistoryStoreTests.RespectsKillSwitch`
- `ChatSessionServiceTests.NewSessionGetsFreshId`
- `ChatSessionServiceTests.ListSessionsOrderedByMostRecent`
- `ChatStreamingServiceTests.StreamsTokensInOrder`
- `ChatStreamingServiceTests.DetectsToolCallMidStream`
- `ChatStreamingServiceTests.InjectsToolResultBackIntoContext`
- `ChatStreamingServiceTests.CancellationStopsStreamCleanly`
- `ChatStreamingServiceTests.FallsBackOnAdapterError`
- `ChatTitleServiceTests.GeneratesTitleFromFirstTurn`
- `ChatTitleServiceTests.DoesNotTitleOnSubsequentTurns`
- `ChatViewModelTests.RendersToolExpanderForToolCallTurn`
- `ChatViewModelTests.SearchFiltersByFtsHit`
- `PlainLanguageExplainerTests.CoversAllNewChatKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Chat|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/chat-history.sqlite
vnext/artifacts/chat-sessions/<ts>-session-summary/summary.json
docs/C_PHASE107_CHAT_UI_2026-05-15.md
```

**Stop gates:**

- Stop if chat rows can be UPDATEd or DELETEd via any code path.
- Stop if a mid-stream cancellation leaves zombie rows or orphan tool calls.
- Stop if the chat UI blocks the UI thread (must be fully async).
- Stop if any tool call bypasses `UnifiedPolicyService` consultation.
- Stop if search exposes content from sessions marked as soft-deleted.

**Rollback:** revert PR; chat UI vanishes; users return to using the old
pet-task surface for AI interaction.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-107-chat-ui`
- PR title: `C-PHASE 107: Chat UI (multi-turn, streaming, tool-use, history)`.
- Phase report: `docs/C_PHASE107_CHAT_UI_2026-05-15.md`.

**Auto-continue?** **No.** First primary UI surface for the AI; stop for
user review before agent-slot redesign (C-PHASE 108) restructures the
helper-pets data model below the chat.

---

## C-PHASE 108 — Agent-Slot Redesign

**Goal:** Delete the hardcoded Scout/Inspector/Builder helper pets with
role enums; replace with three agent slots (per Q16-A4 + NM3 + RS3 +
DA1+DA3 + VS3) whose names mirror active pet slot names with "Agent N"
fallback. Concurrent tool execution per CM3 (parallel tools, serial LLM
generation).

**Scope:**

- Delete `PetHelperRole` enum from `PetAgentContracts.cs`.
- Delete `PetCommandBarService.ScoutHelperId / InspectorHelperId /
  BuilderHelperId` static GUID constants.
- Delete `BuildDefaultRoster()` method's hardcoded helper construction.
- Add `AgentSlot` record replacing `HelperPet`:
  - `SlotIndex` (0, 1, 2)
  - `Name` (computed from pet name or "Agent N")
  - `Status` (`Idle`, `RunningTool`, `Generating`, `Suspended`)
  - `CurrentTaskCardId` (nullable Guid)
  - `LastUsedAtUtc`
- Add `AgentSlotService`:
  - Reads active pet roster from `PetSimulationEngine` IPC; maps slot 0/1/2
    to pet 0/1/2 names; fallback to "Agent N".
  - Provides `IReadOnlyList<AgentSlot> GetSlots()`.
  - On pet rename → fires `agent_slot_renamed` packet + invalidates name cache.
- Add `AgentSlotDispatcher`:
  - One method `AgentSlot? PickAgentForTask(TaskKind kind, TaskOrigin origin)`.
  - For `TaskOrigin.UserVisible` → consults `IModelAdapter` for a
    routing suggestion (per DA1).
  - For `TaskOrigin.Background` → first-idle slot (per DA3).
  - Per RS3: foreground tasks (user-visible) get priority; background
    tasks yield.
- Add tool-call concurrency manager `AgentToolConcurrencyCoordinator`:
  - Tool calls from any agent can run in parallel (per CM3).
  - LLM generation from any agent serializes via single Ollama context
    (per CM3 constraint).
  - Tracks in-flight tool calls per agent; provides
    `IReadOnlyList<InFlightToolCall> GetInFlight()`.
- Refactor `PetTaskAdapterPreviewDispatcher` to consume `AgentSlot` instead
  of `HelperPet`. (This is the rename in C-PHASE 115, but the structural
  change happens here.)
- New packet kinds: `agent_slot_assigned`, `agent_slot_renamed`,
  `agent_slot_status_changed`.
- VS3 UI surfaces in `ToolPopupWindow`:
  - New "Agents" tab (per Q21-U-2 tab strip).
  - Three rows showing each slot's name + current status + current tool
    icon + elapsed time.
- VS3 UI surfaces in `HomePanelWindow`:
  - Three small icons near the existing helper-pet rendering area.
  - Each icon: pet sprite (or generic agent silhouette) + busy/idle dot
    + tool icon when running a tool.

**Pattern:** Follow `RuntimeSupervisorService` at
`vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs` for the
status-tracking + UI-notification pattern. Follow `PetTaskCardQueueService`
for the slot-collection management pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Contracts/PetAgentContracts.cs              (DELETE PetHelperRole)
vnext/src/Wevito.VNext.Core/PetCommandBarService.cs                (DELETE hardcoded GUIDs)
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs      (consume AgentSlot)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/AgentSlot.cs
vnext/src/Wevito.VNext.Core/AgentSlotStatus.cs
vnext/src/Wevito.VNext.Core/AgentSlotService.cs
vnext/src/Wevito.VNext.Core/AgentSlotDispatcher.cs
vnext/src/Wevito.VNext.Core/AgentToolConcurrencyCoordinator.cs
vnext/src/Wevito.VNext.Core/InFlightToolCall.cs
vnext/src/Wevito.VNext.Core/TaskOrigin.cs
vnext/tests/Wevito.VNext.Tests/AgentSlotServiceTests.cs
vnext/tests/Wevito.VNext.Tests/AgentSlotDispatcherTests.cs
vnext/tests/Wevito.VNext.Tests/AgentToolConcurrencyCoordinatorTests.cs
docs/C_PHASE108_AGENT_SLOT_REDESIGN_2026-05-15.md
```

**Tests:**

- `AgentSlotServiceTests.SlotNamesMirrorPetNames`
- `AgentSlotServiceTests.FallsBackToAgentNWithFewerThanThreePets`
- `AgentSlotServiceTests.PetRenamePropagatesToAgentName`
- `AgentSlotServiceTests.PetRemovalRevertsToAgentN`
- `AgentSlotDispatcherTests.UserVisibleTaskConsultsAdapter`
- `AgentSlotDispatcherTests.BackgroundTaskPicksFirstIdle`
- `AgentSlotDispatcherTests.RespectsRuntimeSupervisorMode`
- `AgentSlotDispatcherTests.FallsBackWhenAdapterReturnsNull`
- `AgentToolConcurrencyCoordinatorTests.ParallelToolCallsAcrossAgents`
- `AgentToolConcurrencyCoordinatorTests.SerializesLlmGenerationCalls`
- `AgentToolConcurrencyCoordinatorTests.TracksInFlightAccurately`
- `PlainLanguageExplainerTests.CoversAgentSlotAssignedKind`
- `PlainLanguageExplainerTests.CoversAgentSlotRenamedKind`
- `PlainLanguageExplainerTests.CoversAgentSlotStatusChangedKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AgentSlot|AgentTool|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/artifacts/agent-slots/<ts>-status-snapshot/snapshot.json
docs/C_PHASE108_AGENT_SLOT_REDESIGN_2026-05-15.md
```

**Stop gates:**

- Stop if `PetHelperRole` enum still exists anywhere in source.
- Stop if any hardcoded helper-pet GUID still exists.
- Stop if LLM generation can run concurrently for two agents (must serialize).
- Stop if agent names don't update live on pet rename.
- Stop if dispatcher hands user-visible tasks to non-AI logic.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; hardcoded helper pets return; chat continues to
work (C-PHASE 107 doesn't depend on this restructuring).

**Commit/PR:**

- Branch: `claude-implementation/c-phase-108-agent-slot-redesign`
- PR title: `C-PHASE 108: Agent-slot redesign (delete roles + add concurrency)`.
- Phase report: `docs/C_PHASE108_AGENT_SLOT_REDESIGN_2026-05-15.md`.

**Auto-continue?** **No.** Restructures the agent data model; stop for
user review before tool registry (C-PHASE 109) builds on this.

---

## C-PHASE 109 — Tool Registry First-Class (AI-Callable)

**Goal:** Expose every existing preview-adapter tool as an
AI-callable tool with a structured schema, so the LLM can invoke them
mid-chat. Implements the Q15 "tool-use mid-chat" requirement.

**Scope:**

- Add `ToolRegistry` in `Wevito.VNext.Core`:
  - Singleton-style service that holds `IReadOnlyList<ToolDescriptor>`.
  - `ToolDescriptor` record: `ToolFamily` (string), `Name` (string),
    `Description` (string), `ArgumentSchema` (JSON Schema string),
    `ReturnSchema` (JSON Schema string), `RiskLevel` (existing
    `ToolRiskLevel` enum), `RequiresApproval` (bool), `Adapter`
    (existing preview adapter delegate).
  - Initialized at startup; reads tool surfaces from existing adapters
    (`LocalDocsPreviewAdapter`, `LocalResearchPreviewAdapter`,
    `SpriteAuditPreviewAdapter`, etc.) plus new built-in tools:
    `retrieve_from_memory` (Q22-R-4), `bookmark_for_benchmark`
    (Q17-Option-C), `pin_message` (Q22-P-3).
- Add `ToolInvocationService`:
  - `Task<ToolInvocationResult> InvokeAsync(string toolFamily, JsonElement args, CancellationToken ct)`
  - Routes to the registered adapter; runs through `UnifiedPolicyService`
    gate FIRST; never bypasses kill switch.
  - Writes `tool_invocation_started` / `tool_invocation_completed`
    evidence packets.
- Add `OllamaToolFormatAdapter`:
  - Translates `ToolRegistry` descriptors into Ollama's OpenAI-compatible
    tool format (functions list with JSON Schema).
  - Translates Ollama's `tool_call` responses back into
    `ToolInvocationService.InvokeAsync` calls.
- Extend `OllamaLocalModelAdapter.SuggestAsync` to include the tools
  array in the request payload (when streaming via C-PHASE 107).
- Add `ToolInvocationStreamingBridge`:
  - Pauses streaming on tool_call_start.
  - Invokes via `ToolInvocationService`.
  - Returns result as tool message into next streaming request.
  - Resumes streaming.
- Settings UI in `ToolPopupWindow` adds a "Tools" tab (per Q21-U-2):
  - Lists every registered tool with its description, risk level,
    approval requirement.
  - User can disable individual tools (writes `tool_registry_setting`
    packet); disabled tools are not advertised to the LLM.
- New packet kinds: `tool_invocation_started`, `tool_invocation_completed`,
  `tool_registry_loaded`, `tool_registry_setting`.

**Pattern:** Follow `PetTaskAdapterPreviewDispatcher.BuildPreview` for
the adapter-routing pattern. Follow `OllamaLocalModelAdapter.BuildPayload`
for the request-payload construction.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs            (extend payload with tools)
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs    (expose adapters to registry)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/ToolRegistry.cs
vnext/src/Wevito.VNext.Core/ToolDescriptor.cs
vnext/src/Wevito.VNext.Core/ToolInvocationService.cs
vnext/src/Wevito.VNext.Core/ToolInvocationResult.cs
vnext/src/Wevito.VNext.Core/OllamaToolFormatAdapter.cs
vnext/src/Wevito.VNext.Core/ToolInvocationStreamingBridge.cs
vnext/tests/Wevito.VNext.Tests/ToolRegistryTests.cs
vnext/tests/Wevito.VNext.Tests/ToolInvocationServiceTests.cs
vnext/tests/Wevito.VNext.Tests/OllamaToolFormatAdapterTests.cs
vnext/tests/Wevito.VNext.Tests/ToolInvocationStreamingBridgeTests.cs
docs/C_PHASE109_TOOL_REGISTRY_FIRST_CLASS_2026-05-15.md
```

**Tests:**

- `ToolRegistryTests.LoadsAllExistingPreviewAdapters`
- `ToolRegistryTests.UserDisabledToolsHiddenFromLlm`
- `ToolInvocationServiceTests.RoutesToCorrectAdapter`
- `ToolInvocationServiceTests.ConsultsUnifiedPolicyServiceFirst`
- `ToolInvocationServiceTests.RefusesWhenKillSwitchActive`
- `ToolInvocationServiceTests.WritesEvidencePacket`
- `OllamaToolFormatAdapterTests.GeneratesValidOpenAiFunctionsSchema`
- `OllamaToolFormatAdapterTests.ParsesToolCallResponse`
- `ToolInvocationStreamingBridgeTests.PausesAndResumesStream`
- `ToolInvocationStreamingBridgeTests.HandlesToolFailure`
- `ToolInvocationStreamingBridgeTests.RespectsCancellation`
- `PlainLanguageExplainerTests.CoversToolInvocationStartedKind`
- `PlainLanguageExplainerTests.CoversToolInvocationCompletedKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ToolRegistry|ToolInvocation|OllamaToolFormat|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/artifacts/tool-invocations/<ts>-<tool-family>/packet.json
docs/C_PHASE109_TOOL_REGISTRY_FIRST_CLASS_2026-05-15.md
```

**Stop gates:**

- Stop if any tool invocation bypasses `UnifiedPolicyService`.
- Stop if the registry exposes a tool not declared in `tool_definitions.json`.
- Stop if disabled tools still appear in the LLM's tool list.
- Stop if a tool failure crashes the streaming bridge instead of returning error.
- Stop if the kill switch doesn't halt in-flight tool invocations.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; AI returns to chat-only mode without tool use;
preview adapters still function via the manual TaskCard surface.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-109-tool-registry-first-class`
- PR title: `C-PHASE 109: Tool registry first-class (AI-callable tools)`.
- Phase report: `docs/C_PHASE109_TOOL_REGISTRY_FIRST_CLASS_2026-05-15.md`.

**Auto-continue?** **No.** AI gains the ability to invoke tools
autonomously mid-chat; stop for user review of the safety gate behavior.

---

## C-PHASE 110 — Pet/AI Isolation Contract

**Goal:** Implement Q18 isolation contract: Godot pet game maintains 60fps
target with 30fps floor regardless of AI workload (F3), AI priority is
tiered (P4), RAM pressure cascade (R4), image-gen idle-only or explicit (G3+G4),
crash isolation with watchdog (C4), pet input fully preempts AI (I3).

**Scope:**

- Add `PetFpsMonitorService` in `Wevito.VNext.Core`:
  - Reads Godot's `Engine.get_frames_per_second()` via existing IPC contract
    (extend `Wevito.VNext.Contracts.PetAgentContracts` with a
    `PetFpsSample` record).
  - Samples every 250ms; emits hourly `pet_fps_snapshot` packet with
    p50/p95/min.
  - On below-30 detection, emits immediate `pet_fps_violation` packet +
    auto-throttles `AutonomousOperationsLoop` to 50% budget for 5 min
    via `RuntimeBudgetMeter`.
- Add `RamPressureCascadeService`:
  - Polls `GC.GetTotalMemory(false)` + `Process.GetCurrentProcess().WorkingSet64`
    + cross-process estimates every 5s.
  - Tier thresholds (free RAM):
    - `< 3 GB` → suspend background experiments via supervisor signal.
    - `< 2 GB` → invoke Ollama API `/api/generate { keep_alive: 0 }` to
      unload image-gen sidecar if present (graceful).
    - `< 1.5 GB` → invoke same on LLM model (chat goes offline; pet keeps running).
    - `< 1 GB` → emit `ram_pressure_emergency` packet + open Stop card.
  - Pet game reserved minimum 1 GB enforced separately (this service
    never touches Godot RAM).
- Add `WevitoProcessWatchdogService` (in Broker, not Core):
  - Monitors PIDs of: WPF shell, Ollama (if running), Python image-gen
    sidecar (if running), Godot pet game.
  - On unexpected exit, restarts with backoff (1s, 5s, 30s); max 3
    restarts in 5 min; on cap, emits `process_crash_recovery` packet
    with `cap_reached=true` + opens Stop card.
- Add `UserInteractingWithPetState` transient mode to
  `RuntimeSupervisorService`:
  - Triggered by pet input events from Godot IPC.
  - Lasts 5s after last input.
  - During this state, `AutonomousOperationsLoop.TryRunIteration` returns
    block reason "user_interacting_with_pet=true".
- Add `ImageGenIdleGuardService`:
  - Reads pet-game idle state via IPC (extend
    `Wevito.VNext.Contracts.PetAgentContracts.PetActor` with
    `IdleSince` field).
  - Refuses background image-gen invocations when pet idle < 10s.
  - User-triggered invocations bypass this check (G4 path).
- New packet kinds: `pet_fps_snapshot`, `pet_fps_violation`,
  `ram_pressure_event`, `ram_pressure_emergency`, `process_crash_recovery`,
  `image_gen_idle_guard_blocked`, `user_interacting_state_entered`,
  `user_interacting_state_cleared`.

**Pattern:** Follow `WindowsForegroundFullscreenMonitor.cs` and
`WindowsPowerHandler.cs` for the state-monitoring + packet-emitting
pattern. Follow `RuntimeBudgetMeter.cs` for the reservation /
state-persistence pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Contracts/PetAgentContracts.cs              (add PetFpsSample + IdleSince)
vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs            (add UserInteractingWithPet state)
vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs            (consult new states)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Broker/Program.cs                            (wire watchdog)
vnext/content/tool_definitions.json
scripts/pet.gd                                                       (Godot: send fps IPC)
scripts/main_scene.gd                                                (Godot: send input events IPC)
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/PetFpsMonitorService.cs
vnext/src/Wevito.VNext.Core/PetFpsSample.cs
vnext/src/Wevito.VNext.Core/RamPressureCascadeService.cs
vnext/src/Wevito.VNext.Core/RamPressureTier.cs
vnext/src/Wevito.VNext.Broker/WevitoProcessWatchdogService.cs
vnext/src/Wevito.VNext.Core/ImageGenIdleGuardService.cs
vnext/tests/Wevito.VNext.Tests/PetFpsMonitorServiceTests.cs
vnext/tests/Wevito.VNext.Tests/RamPressureCascadeServiceTests.cs
vnext/tests/Wevito.VNext.Tests/WevitoProcessWatchdogServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ImageGenIdleGuardServiceTests.cs
vnext/tests/Wevito.VNext.Tests/PetFpsSoakTest.cs                   (10-min soak)
docs/C_PHASE110_PET_AI_ISOLATION_CONTRACT_2026-05-15.md
```

**Tests:**

- `PetFpsMonitorServiceTests.SnapshotEmittedHourly`
- `PetFpsMonitorServiceTests.ViolationEmittedOnBelow30`
- `PetFpsMonitorServiceTests.ThrottlesAutonomousLoopOnViolation`
- `RamPressureCascadeServiceTests.SuspendsExperimentsAtTier1`
- `RamPressureCascadeServiceTests.UnloadsImageGenAtTier2`
- `RamPressureCascadeServiceTests.UnloadsLlmAtTier3`
- `RamPressureCascadeServiceTests.EmergencyAtTier4`
- `RamPressureCascadeServiceTests.PetGameReservedMinNotTouched`
- `WevitoProcessWatchdogServiceTests.RestartsCrashedChildWithBackoff`
- `WevitoProcessWatchdogServiceTests.RespectsMaxRestartsCap`
- `WevitoProcessWatchdogServiceTests.EmitsPacketOnRecovery`
- `ImageGenIdleGuardServiceTests.BlocksBackgroundWhenPetActive`
- `ImageGenIdleGuardServiceTests.AllowsUserTriggeredImmediate`
- `PetFpsSoakTest.LoopAt100PercentMaintains30FpsFloor` — runs simulated AI load at 100% for 10 min; asserts pet fps stays >= 30.

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetFps|RamPressure|WevitoProcessWatchdog|ImageGenIdleGuard|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/pet-fps-history.jsonl
%LOCALAPPDATA%/Wevito/audit/ram-pressure-history.jsonl
%LOCALAPPDATA%/Wevito/audit/process-watchdog.json
docs/C_PHASE110_PET_AI_ISOLATION_CONTRACT_2026-05-15.md
```

**Stop gates:**

- Stop if the pet game can be RAM-cascaded (its reserved minimum is sacred).
- Stop if a watchdog restart bypasses the kill switch.
- Stop if the soak test fails (pet fps drops below 30 under load).
- Stop if `UserInteractingWithPet` state can be set by anything other than
  actual Godot input events.
- Stop if image-gen idle guard can be bypassed for background invocations.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; isolation guarantees revert to "best effort";
soak test infrastructure remains for future use.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-110-pet-ai-isolation-contract`
- PR title: `C-PHASE 110: Pet/AI isolation contract (F3+P4+R4+G3+G4+C4+I3)`.
- Phase report: `docs/C_PHASE110_PET_AI_ISOLATION_CONTRACT_2026-05-15.md`.

**Auto-continue?** **No.** First contract that mediates between AI workload
and pet experience; stop for user review of soak test results.

---

## C-PHASE 111 — User-PC Coexistence Policy

**Goal:** Implement Q19 coexistence policy: auto-pause triggers (T5),
configurable + quick-toggle DnD (D5), three workload tiers (L3), reserved
budget per tier with adaptive borrowing (B4), tiered resume after triggers
clear (R4), pet plays `sleep` animation when wevito yields (V3).

**Scope:**

- Add `CoexistenceTriggerService`:
  - Polls every 10s for: foreground app name (extend
    `WindowsForegroundFullscreenMonitor`), CPU% from non-wevito processes
    (via `PerformanceCounter`), network bandwidth (via WMI counters).
  - User-configurable trigger list under setting `coexistence_app_list`
    (JSON array, default seeded with `["zoom.exe", "teams.exe", "obs64.exe",
    "discord.exe"]`).
  - On any trigger fires, emits `coexistence_trigger_fired` packet +
    signals `RuntimeSupervisorService` to enter `Quiet` mode.
  - On all triggers clear, starts resume timer (R4 tiered).
- Add `DoNotDisturbScheduleService`:
  - Reads user-configured schedule from setting `dnd_schedule_json`
    (default empty).
  - Reads quick-toggle state from setting `dnd_quick_toggle_until_utc`.
  - Exposes `bool IsInDoNotDisturb(DateTimeOffset now)`.
  - Quick-toggle is set via UI ("DnD now / 1h / until tomorrow").
- Add `WorkloadTierService` (replaces `RuntimeSupervisorMode` direct use
  in some call sites):
  - Three tiers: `UserForeground`, `Maintenance`, `Experimentation`.
  - Per-tier budget: foreground gets reserved minimum 60% + can borrow
    above; maintenance reserved 30%; experimentation reserved 10%; all
    can borrow from each other's idle slack (B4).
  - `bool TryReserve(WorkloadTier tier, double estimatedCpuPercent, int estimatedMemoryMb)`.
- Extend `RuntimeBudgetMeter` to consult `WorkloadTierService` for
  tier-aware reservations.
- Settings UI in `ToolPopupWindow`:
  - "Settings" tab grows a "Coexistence" section with trigger toggles +
    app list editor.
  - "Settings" tab grows a "Do Not Disturb" section with schedule editor
    + quick-toggle preview.
- Add quick-toggle on `HomePanelWindow`:
  - Right-click pet → menu includes "Do Not Disturb" submenu with options
    ("DnD now", "1h", "until tomorrow", "off").
- Pet animation switch (V3):
  - When `RuntimeSupervisorMode == Quiet` or `WorkloadTierService` is
    blocking foreground due to coexistence trigger, send IPC to Godot to
    set `force_animation=sleep`.
  - On resume, clear `force_animation`; pet returns to normal behavior.
- New packet kinds: `coexistence_trigger_fired`, `coexistence_trigger_cleared`,
  `dnd_state_changed`, `tier_priority_adjusted`,
  `pet_animation_forced` (already exists; reuse).

**Pattern:** Follow `WindowsForegroundFullscreenMonitor.cs` for the
trigger-detection pattern. Follow `WindowsPowerHandler.cs` for the state
change + supervisor signal pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs            (consume WorkloadTierService)
vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs                  (tier-aware reservations)
vnext/src/Wevito.VNext.Core/WindowsForegroundFullscreenMonitor.cs   (extend with app-list match)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/CoexistenceTriggerService.cs
vnext/src/Wevito.VNext.Core/DoNotDisturbScheduleService.cs
vnext/src/Wevito.VNext.Core/WorkloadTier.cs
vnext/src/Wevito.VNext.Core/WorkloadTierService.cs
vnext/tests/Wevito.VNext.Tests/CoexistenceTriggerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/DoNotDisturbScheduleServiceTests.cs
vnext/tests/Wevito.VNext.Tests/WorkloadTierServiceTests.cs
docs/C_PHASE111_USER_PC_COEXISTENCE_POLICY_2026-05-15.md
```

**Tests:**

- `CoexistenceTriggerServiceTests.AppListMatchTriggersQuietMode`
- `CoexistenceTriggerServiceTests.FullscreenAppTriggers`
- `CoexistenceTriggerServiceTests.CpuPressureTriggers`
- `CoexistenceTriggerServiceTests.TieredResumeOnClear`
- `DoNotDisturbScheduleServiceTests.ScheduleWindowHonored`
- `DoNotDisturbScheduleServiceTests.QuickToggleOverrides`
- `DoNotDisturbScheduleServiceTests.UntilTomorrowClearsAtMidnight`
- `WorkloadTierServiceTests.ForegroundReservesMinimum60Pct`
- `WorkloadTierServiceTests.AdaptiveBorrowingAboveFloor`
- `WorkloadTierServiceTests.ExperimentationYieldsToMaintenance`
- `WorkloadTierServiceTests.MaintenanceYieldsToForeground`
- `PlainLanguageExplainerTests.CoversAllCoexistenceKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Coexistence|DoNotDisturb|WorkloadTier|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/audit/coexistence-events.jsonl
%LOCALAPPDATA%/Wevito/settings/coexistence-app-list.json
%LOCALAPPDATA%/Wevito/settings/dnd-schedule.json
docs/C_PHASE111_USER_PC_COEXISTENCE_POLICY_2026-05-15.md
```

**Stop gates:**

- Stop if any trigger can bypass the kill switch.
- Stop if pet animation force-state leaks beyond DnD/quiet (must clear on resume).
- Stop if app list defaults include hosted-AI clients (must be local-tool list only).
- Stop if foreground tier can be starved by maintenance/experimentation.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; coexistence behavior reverts to "always run";
existing quiet/petonly modes still work.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-111-user-pc-coexistence-policy`
- PR title: `C-PHASE 111: User-PC coexistence policy (T5+D5+L3+B4+R4+V3)`.
- Phase report: `docs/C_PHASE111_USER_PC_COEXISTENCE_POLICY_2026-05-15.md`.

**Auto-continue?** **Yes.** Behavior-only changes on top of existing
supervisor + budget meter; no new mutation surface; no new model surface.

---

## C-PHASE 112 — Benchmark Suite v1

**Goal:** Implement Q17 benchmark v1 (B1 chat + B2 tool-use + B4 retrieval
+ B7 safety + B8 perf), with CA6 cadence, GI1+GI4 reinterpreted grading
triad, FR4 tiered failure response, UI5 detail tab + ambient badge, V1
immutable versioning.

**Scope:**

- Add `BenchmarkSuiteService` in `Wevito.VNext.Core`:
  - `BenchmarkRunResult Run(BenchmarkSuiteVersion version, BenchmarkRunRequest request)`.
  - Iterates over registered `IBenchmarkKind` implementations.
  - Each kind reads cases from `vnext/content/benchmarks/v1/approved/*.json`
    (per V1 immutable versioning).
  - Writes one `benchmark_run` evidence packet per run with per-axis
    scores + the model version + the benchmark version.
- Add `IBenchmarkKind` interface:
  - `string AxisName { get; }`
  - `BenchmarkKindResult RunCases(IReadOnlyList<BenchmarkCase> cases, BenchmarkContext context)`
- Add five v1 kinds:
  - `ChatCorrectnessBenchmarkKind` — B1
  - `ToolUseCorrectnessBenchmarkKind` — B2
  - `RetrievalAccuracyBenchmarkKind` — B4 (extends existing `LearningEvalService`)
  - `SafetyRegressionBenchmarkKind` — B7
  - `PerfRegressionBenchmarkKind` — B8 (measures p50/p95 latency, RAM peak, VRAM peak)
- Add `GraderTriad`:
  - `Deterministic` (always mandatory; exact-match / substring / embedding-cosine ≥ threshold / JSON-shape match)
  - `ProductionLLM` (advisory only; metadata in packet; never authoritative)
  - `HeldOut` (deferred to v2; placeholder interface).
- Add `BenchmarkRegressionGate`:
  - Compares latest run to baseline.
  - FR4 tiers:
    - Safety axis any failure → return `Halt + StopCard`.
    - Perf axis regression > 20% → return `Halt`.
    - Capability axis regression > 15% → return `Halt`.
    - Capability axis regression 5-15% → return `TaskCard`.
    - Capability axis regression < 5% → return `Log`.
- Add CA6 cadence wiring:
  - Per-phase (run via Codex loop hooks; safety + perf only).
  - Daily (registered as `BenchmarkDailyChore` in C-PHASE 101 registry; B1+B2+B4).
  - On-demand from UI button.
- Settings UI in `ToolPopupWindow`:
  - New "Benchmarks" tab (per Q21-U-2).
  - Shows per-axis line charts (last 30 days).
  - "Run benchmarks now" button + per-kind toggles.
- HomePanelWindow ambient badge: small "📊 87%" widget showing latest
  composite score; clicks open Benchmarks tab.
- New packet kinds: `benchmark_run`, `benchmark_regression_detected`,
  `benchmark_baseline_updated`.

**Pattern:** Follow `LearningEvalService.cs` for the eval-run +
regression-gate pattern. Follow `SelfImprovementReportService.cs` for
the windowed-report-emitting pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/HouseholdMaintenanceService.cs         (register BenchmarkDailyChore)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/BenchmarkSuiteService.cs
vnext/src/Wevito.VNext.Core/IBenchmarkKind.cs
vnext/src/Wevito.VNext.Core/BenchmarkCase.cs
vnext/src/Wevito.VNext.Core/BenchmarkKindResult.cs
vnext/src/Wevito.VNext.Core/BenchmarkRunResult.cs
vnext/src/Wevito.VNext.Core/GraderTriad.cs
vnext/src/Wevito.VNext.Core/BenchmarkRegressionGate.cs
vnext/src/Wevito.VNext.Core/Benchmarks/ChatCorrectnessBenchmarkKind.cs
vnext/src/Wevito.VNext.Core/Benchmarks/ToolUseCorrectnessBenchmarkKind.cs
vnext/src/Wevito.VNext.Core/Benchmarks/RetrievalAccuracyBenchmarkKind.cs
vnext/src/Wevito.VNext.Core/Benchmarks/SafetyRegressionBenchmarkKind.cs
vnext/src/Wevito.VNext.Core/Benchmarks/PerfRegressionBenchmarkKind.cs
vnext/src/Wevito.VNext.Core/Chores/BenchmarkDailyChore.cs
vnext/content/benchmarks/v1/approved/.gitkeep
vnext/content/benchmarks/v1/draft/.gitkeep
vnext/content/benchmarks/v1/README.md
vnext/tests/Wevito.VNext.Tests/BenchmarkSuiteServiceTests.cs
vnext/tests/Wevito.VNext.Tests/BenchmarkRegressionGateTests.cs
vnext/tests/Wevito.VNext.Tests/Benchmarks/<one test file per kind>.cs
docs/C_PHASE112_BENCHMARK_SUITE_V1_2026-05-15.md
```

**Tests:**

- `BenchmarkSuiteServiceTests.RunsAllRegisteredKinds`
- `BenchmarkSuiteServiceTests.SkipsKindsWithEmptyApprovedCases`
- `BenchmarkSuiteServiceTests.WritesPerRunEvidencePacket`
- `BenchmarkSuiteServiceTests.IncludesBenchmarkVersionInPacket`
- `BenchmarkRegressionGateTests.SafetyFailureReturnsHaltStopCard`
- `BenchmarkRegressionGateTests.PerfRegressionAbove20PctReturnsHalt`
- `BenchmarkRegressionGateTests.CapabilityRegressionAbove15PctReturnsHalt`
- `BenchmarkRegressionGateTests.CapabilityRegression5To15PctReturnsTaskCard`
- `BenchmarkRegressionGateTests.CapabilityRegressionBelow5PctReturnsLog`
- `ChatCorrectnessBenchmarkKindTests.DeterministicGradingAgainstExpected`
- `ToolUseCorrectnessBenchmarkKindTests.StructuralJsonCheck`
- `RetrievalAccuracyBenchmarkKindTests.ComputesRecallAt1And3AndMrr`
- `SafetyRegressionBenchmarkKindTests.AllAdversarialPromptsMustFailToTriggerAction`
- `PerfRegressionBenchmarkKindTests.MeasuresLatencyAndPeakMemory`
- `PlainLanguageExplainerTests.CoversAllBenchmarkKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Benchmark|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/artifacts/benchmark-runs/<ts>-<axis>/result.json
vnext/content/benchmarks/v1/approved/*.json       (user-curated, post-C-PHASE 113)
docs/C_PHASE112_BENCHMARK_SUITE_V1_2026-05-15.md
```

**Stop gates:**

- Stop if production LLM grading becomes authoritative (deterministic must
  always be the metric of record).
- Stop if a benchmark case can be modified after commit (V1 immutability).
- Stop if safety axis regression doesn't halt the loop.
- Stop if `approved/` is empty AND benchmark suite claims "passing"
  (must report `no_baseline` honestly).
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; existing golden-eval (C-PHASE 82) continues to
serve as the only regression gate; benchmark trend UI vanishes.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-112-benchmark-suite-v1`
- PR title: `C-PHASE 112: Benchmark suite v1 (B1+B2+B4+B7+B8 + GraderTriad + FR4)`.
- Phase report: `docs/C_PHASE112_BENCHMARK_SUITE_V1_2026-05-15.md`.

**Auto-continue?** **No.** First measurement surface for "is wevito
improving"; stop for user review of the grading semantics.

---

## C-PHASE 113 — Benchmark Case Curation UI

**Goal:** Implement Q17-Option-C curation flow: Codex generates draft
cases, user approves via UI (bookmark-from-chat for capability cases,
explicit hostile-brainstorm for adversarial cases).

**Scope:**

- Add `BenchmarkCaseDraftService`:
  - On phase merge, Codex (via prompt) generates 5-10 draft cases per
    capability kind based on observed chat history + tool usage patterns.
  - Drafts are written to `vnext/content/benchmarks/v1/draft/<kind>/*.json`.
- Add `BenchmarkCaseCurationStore`:
  - Tracks per-case review state: `pending_review`, `approved`,
    `rejected`, `revised`.
  - Mirrors `LearningLabLabelStore` pattern.
  - Append-only sqlite triggers.
- Add `BenchmarkCurationViewModel` + UI in ToolPopupWindow's "Benchmarks"
  tab:
  - "Curation" sub-tab.
  - Shows draft cases pending review with inline editor.
  - "Approve" moves case from `draft/` to `approved/`.
  - "Reject" deletes draft.
  - "Revise" allows editing then approving.
- Bookmark-from-chat integration (per Q17-Option-C):
  - Each assistant message in ChatPanel has a 🔖 button.
  - Click opens inline editor: "Expected answer: ..." (pre-filled with
    actual response).
  - User edits + saves → new draft case appears in curation queue.
- Adversarial brainstorm helper:
  - New "Add adversarial case" button on Benchmarks > Curation tab.
  - Opens a form: "Hostile prompt: ___" + "Expected wevito behavior:
    refuse / block / sanitize".
  - User types; case appears as approved (adversarial cases bypass draft
    queue since they must come from human hostile thinking).
- New packet kinds: `benchmark_case_drafted`, `benchmark_case_approved`,
  `benchmark_case_rejected`, `benchmark_case_revised`,
  `benchmark_case_bookmarked_from_chat`.

**Pattern:** Follow `LearningLabLabelStore.cs` and
`CreativeLearningLabWindow.xaml.cs` for the draft-review-approve UI
pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Shell/ChatPanel.xaml             (add bookmark button)
vnext/src/Wevito.VNext.Shell/ChatPanel.xaml.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml       (add Curation sub-tab)
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/BenchmarkCaseDraftService.cs
vnext/src/Wevito.VNext.Core/BenchmarkCaseCurationStore.cs
vnext/src/Wevito.VNext.Shell/BenchmarkCurationViewModel.cs
vnext/src/Wevito.VNext.Shell/BenchmarkCurationPanel.xaml
vnext/src/Wevito.VNext.Shell/BenchmarkCurationPanel.xaml.cs
vnext/tests/Wevito.VNext.Tests/BenchmarkCaseDraftServiceTests.cs
vnext/tests/Wevito.VNext.Tests/BenchmarkCaseCurationStoreTests.cs
vnext/tests/Wevito.VNext.Tests/BenchmarkCurationViewModelTests.cs
docs/C_PHASE113_BENCHMARK_CASE_CURATION_UI_2026-05-15.md
```

**Tests:**

- `BenchmarkCaseDraftServiceTests.DraftsHonorKindAxis`
- `BenchmarkCaseDraftServiceTests.NeverOverwritesApprovedCases`
- `BenchmarkCaseCurationStoreTests.AppendIsAppendOnly`
- `BenchmarkCaseCurationStoreTests.ApproveMovesCaseFromDraftToApproved`
- `BenchmarkCaseCurationStoreTests.RejectDeletesDraftWithoutRow`
- `BenchmarkCurationViewModelTests.ShowsPendingCasesOnly`
- `BenchmarkCurationViewModelTests.BookmarkFromChatCreatesDraftRow`
- `BenchmarkCurationViewModelTests.AdversarialCaseSkipsDraftQueue`
- `ChatPanelBookmarkButtonTests.RendersOnAssistantMessages`
- `ChatPanelBookmarkButtonTests.OpensInlineEditorOnClick`
- `PlainLanguageExplainerTests.CoversAllCurationKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "BenchmarkCase|BenchmarkCuration|ChatPanelBookmark|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
vnext/content/benchmarks/v1/draft/*.json
vnext/content/benchmarks/v1/approved/*.json
%LOCALAPPDATA%/Wevito/audit/benchmark-curation.sqlite
docs/C_PHASE113_BENCHMARK_CASE_CURATION_UI_2026-05-15.md
```

**Stop gates:**

- Stop if approved cases can be modified or deleted (V1 immutability).
- Stop if drafts overwrite approved.
- Stop if bookmark-from-chat creates approved cases directly (must go through draft).
- Stop if adversarial case form doesn't write user-provided expected behavior.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; benchmark cases stay at whatever was previously
seeded; bookmark button vanishes.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-113-benchmark-case-curation-ui`
- PR title: `C-PHASE 113: Benchmark case curation UI (bookmark-from-chat + draft approval)`.
- Phase report: `docs/C_PHASE113_BENCHMARK_CASE_CURATION_UI_2026-05-15.md`.

**Auto-continue?** **Yes.** Read-write own-data + UI surface; no new
model surface; no new mutation pathway.

---

## C-PHASE 114 — Codex Loop Reliability Hardening

**Goal:** Implement Q20 loop reliability: per-phase timeout with idle-extend
(T4-modified), no-commit-stuck detection (S2), retry-once-then-remediation-card
(F4), auto-rebase-or-halt (M4), three-file state (L3), human-inject points
(I4), heartbeat + watchdog + ambient UI (H4).

**Scope:**

- Extend `tools/run-codex-loop.ps1` from C-PHASE 96 (build hot-swap wrapper
  + loop runner, written separately) to include:
  - Phase timeout: 2h baseline. Reads `GetLastInputInfo` via P/Invoke
    in a small helper exe (`tools/idle-detector.exe`); when idle > 10 min,
    timeout extends indefinitely. Snaps back on user input.
  - Stuck detection: monitors git log of Codex's branch; if no new commit
    in 30 min, halts with reason "stuck_no_commit_30min".
  - Test/build failure retry: F4. On first failure, re-invokes Codex with
    failure context attached. On second failure, generates a remediation
    task card describing the failure.
  - Merge conflict: M4. Attempts `git rebase` automatically; on non-trivial
    conflict, halts.
- Add `CodexPhaseQueueService` in `Wevito.VNext.Core`:
  - Reads/writes `docs/codex-phase-queue.json` (pending queue),
    `docs/codex-loop-status.json` (in-flight), `docs/codex-phase-history.jsonl`
    (append-only audit).
  - JSON schemas documented in companion file.
- Add `CodexLoopRunnerService` (calls into PowerShell wrapper or runs
  loop logic in-process):
  - `Task<CodexLoopRunResult> RunLoopAsync(CancellationToken ct)`
  - Reads queue → invokes Codex CLI → runs validation → auto-merges or
    halts → advances.
- Add `CodexLoopWatchdogService` (in Broker, mirroring
  `WevitoProcessWatchdogService` from C-PHASE 110):
  - Reads `codex_loop_heartbeat` ledger rows.
  - If no heartbeat for > 10 min while loop is in "running" state, kills
    the loop runner and respawns.
- Add `tools/codex-inject.ps1`:
  - `-PhaseId X` parameter; inserts the phase at the front of the queue.
- Add `tools/codex-loop-pause.ps1` and `tools/codex-loop-resume.ps1`:
  - Pause writes a `loop_paused` row + sentinel file.
  - Resume reads sentinel + clears it; loop runner consults sentinel each
    iteration.
- Ambient UI badge in `HomePanelWindow`:
  - "Codex loop: phase 110, 23 min in" with progress dot.
  - Click opens `ToolPopupWindow` → Activity tab → Loop section.
- New packet kinds: `codex_loop_heartbeat`, `codex_phase_started`,
  `codex_phase_retried`, `codex_phase_blocked`, `codex_phase_completed`,
  `codex_loop_paused`, `codex_loop_resumed`.

**Pattern:** Follow `SoakDriverCommandService.cs` for the PowerShell-wrapper
+ dotnet-helper pattern. Follow `WevitoProcessWatchdogService.cs` (from
C-PHASE 110) for the watchdog pattern.

**Files likely touched:**

```text
tools/run-codex-loop.ps1                                            (extend from C-PHASE 96)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
vnext/src/Wevito.VNext.Broker/Program.cs                             (wire watchdog)
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/CodexPhaseQueueService.cs
vnext/src/Wevito.VNext.Core/CodexLoopRunnerService.cs
vnext/src/Wevito.VNext.Core/CodexPhaseQueueEntry.cs
vnext/src/Wevito.VNext.Core/CodexLoopStatus.cs
vnext/src/Wevito.VNext.Broker/CodexLoopWatchdogService.cs
tools/codex-inject.ps1
tools/codex-loop-pause.ps1
tools/codex-loop-resume.ps1
tools/idle-detector/idle-detector.csproj                             (small dotnet console)
tools/idle-detector/Program.cs
docs/codex-phase-queue.json                                          (initial empty queue)
docs/codex-loop-status.json                                          (initial empty status)
docs/codex-phase-history.jsonl                                       (initial empty file)
vnext/tests/Wevito.VNext.Tests/CodexPhaseQueueServiceTests.cs
vnext/tests/Wevito.VNext.Tests/CodexLoopRunnerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/CodexLoopWatchdogServiceTests.cs
docs/C_PHASE114_CODEX_LOOP_RELIABILITY_HARDENING_2026-05-15.md
```

**Tests:**

- `CodexPhaseQueueServiceTests.QueuePersistsAcrossRestart`
- `CodexPhaseQueueServiceTests.HistoryIsAppendOnly`
- `CodexPhaseQueueServiceTests.InjectMovesPhaseToFront`
- `CodexLoopRunnerServiceTests.TimeoutAfterTwoHoursWhenUserPresent`
- `CodexLoopRunnerServiceTests.ExtendedTimeoutWhenUserIdle`
- `CodexLoopRunnerServiceTests.StuckDetectionHaltsOn30MinNoCommit`
- `CodexLoopRunnerServiceTests.RetryOnceWithFailureContext`
- `CodexLoopRunnerServiceTests.OpensRemediationCardAfterSecondFailure`
- `CodexLoopRunnerServiceTests.AutoRebaseOnFastForwardConflict`
- `CodexLoopRunnerServiceTests.HaltsOnSemanticConflict`
- `CodexLoopWatchdogServiceTests.RestartsLoopOnMissedHeartbeat`
- `CodexLoopWatchdogServiceTests.RespectsKillSwitch`
- `PlainLanguageExplainerTests.CoversAllCodexLoopKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "CodexPhaseQueue|CodexLoopRunner|CodexLoopWatchdog|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet build .\tools\idle-detector\idle-detector.csproj
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
docs/codex-phase-queue.json
docs/codex-loop-status.json
docs/codex-phase-history.jsonl
docs/C_PHASE114_CODEX_LOOP_RELIABILITY_HARDENING_2026-05-15.md
```

**Stop gates:**

- Stop if the loop can advance past a phase whose tests didn't pass.
- Stop if the loop can flip `Auto-continue=No` phases without user nod.
- Stop if KillSwitch can be bypassed by the watchdog.
- Stop if the idle detector reads anything other than `GetLastInputInfo`.
- Stop if the queue file allows mutation of completed phases.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; loop runner reverts to C-PHASE 96 baseline (no
timeout extension, no stuck detection, halts on first failure).

**Commit/PR:**

- Branch: `claude-implementation/c-phase-114-codex-loop-reliability-hardening`
- PR title: `C-PHASE 114: Codex loop reliability hardening (T4mod+S2+F4+M4+L3+I4+H4)`.
- Phase report: `docs/C_PHASE114_CODEX_LOOP_RELIABILITY_HARDENING_2026-05-15.md`.

**Auto-continue?** **No.** This phase IS the loop's behavior; merging it
is a no-op on Codex's behavior until the loop is restarted, so stopping
gives user a chance to review the new loop config.

---

## C-PHASE 115 — Identity Rename + UX Language Pass

**Goal:** Implement Q21: rename misleading internals (N4), AI identity
service with first-run wizard (N-3), settings UI restructure with top tab
strip (U-2), README rewrite + new docs (D-4), 4-step onboarding wizard (O-2).

**Scope:**

- Mechanical renames (N4):
  - `PetCommandBarService` → `ChatInputBarService` (just rename; method
    contents unchanged)
  - `PetCommandParser` → `ChatPromptParser`
  - `PetCommandBarState` → `ChatInputBarState`
  - `PetTaskCard` (class) → `AgentTaskCard`
  - `PetTaskCardQueueService` → `AgentTaskCardQueueService`
  - `PetTaskArtifactPathResolution` → `AgentTaskArtifactPathResolution`
  - `PetTaskAdapterPreviewDispatcher` → `AgentToolDispatcher`
  - `HelperPet` → `AgentSlot` (already renamed in C-PHASE 108; this phase
    just sweeps remaining references)
  - `PetHelperRole` → DELETED (already in C-PHASE 108)
  - `PetHelperAvailability` → `AgentSlotAvailability`
  - `PetHelperProfile` → `AgentSlotProfile`
- Keep as-is: `PetActor`, `PetMemoryStore`, `PetSimulationEngine`,
  `PetWellbeingInterpreter`, `PetStatePreviewAdapter`, `PetModelSummaryService`,
  `PetDebugTruthReportBuilder` (these refer to the actual pet game).
- Contracts namespace unchanged (`PetAgentContracts.cs` keeps its name to
  preserve API stability; internal references updated).
- Add `AiIdentityService` in `Wevito.VNext.Core`:
  - Reads/writes setting `ai_identity_name` (default "Wevito").
  - Exposes `string GetAiName()`.
  - On change, emits `ai_identity_set` packet.
- Settings UI restructure (U-2):
  - `ToolPopupWindow` body becomes the chat panel (already done in C-PHASE 107).
  - Top tab strip: `Chat (default) | Activity | Agents | Tools | Benchmarks | Creative Lab | Settings`.
  - Existing tab content rewired to new locations.
- Add `FirstLaunchWizardWindow` (O-2):
  - 4 steps:
    1. "What should I call you?" → writes `ai_identity_name`.
    2. "What should I call your agents?" → writes per-slot name overrides
       (if user has pets, defaults to their names; else "Agent 1/2/3").
    3. "Want me to do anything in the background right now?" → options:
       "Just chat" (empty registry), "Help with sprite cleanup" (seeds
       `sprite-template-candidate-generation` in registry), "Add later".
    4. "First chat" → wevito sends a greeting using configured AI name.
  - Wizard runs once on first launch; writes `first_launch_completed`
    packet. Re-runnable via Settings → "Run first-launch wizard again".
- Doc rewrite (D-4):
  - Rewrite `README.md` leading with "Wevito is a local AI assistant.
    The desktop pet is its visual surface."
  - New `WHAT_IS_WEVITO.md` explaining architecture.
  - New `docs/INDEX.md` orienting docs folder (groups by era + topic).
  - `SPRITE_PIPELINE_KIT/README.md` updated with AI-app pivot context.
  - Historical phase docs (C_PHASE0 through C_PHASE85c) untouched
    (frozen audit trail).
- New packet kinds: `first_launch_step_completed`, `ai_identity_set`,
  `agent_slot_renamed` (already in C-PHASE 108; reused here).

**Pattern:** Mechanical renames follow standard refactor pattern; no novel
architecture. `FirstLaunchWizardWindow` follows `CreativeLearningLabWindow.xaml.cs`
shape for sqlite-backed step state. `AiIdentityService` follows
`KillSwitchService.cs` for settings-backed singleton pattern.

**Files likely touched (count: ~50, mostly mechanical):**

```text
Every file referencing the renamed types (vnext/src/**, vnext/tests/**)
README.md
docs/INDEX.md                                                        (new)
SPRITE_PIPELINE_KIT/README.md
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml                    (tab strip)
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/AiIdentityService.cs
vnext/src/Wevito.VNext.Shell/FirstLaunchWizardWindow.xaml
vnext/src/Wevito.VNext.Shell/FirstLaunchWizardWindow.xaml.cs
vnext/tests/Wevito.VNext.Tests/AiIdentityServiceTests.cs
vnext/tests/Wevito.VNext.Tests/FirstLaunchWizardTests.cs
vnext/tests/Wevito.VNext.Tests/IdentityRenameSweepTests.cs
WHAT_IS_WEVITO.md                                                    (new top-level)
docs/INDEX.md                                                        (new)
docs/C_PHASE115_IDENTITY_RENAME_UX_LANGUAGE_PASS_2026-05-15.md
```

**Tests:**

- `IdentityRenameSweepTests.NoLegacyPetTaskCardReferences`
- `IdentityRenameSweepTests.NoLegacyHelperPetReferences`
- `IdentityRenameSweepTests.NoLegacyPetCommandBarReferences`
- `IdentityRenameSweepTests.PetActorAndPetMemoryStoreKeptAsIs`
- `AiIdentityServiceTests.DefaultsToWevito`
- `AiIdentityServiceTests.PersistsCustomNameAcrossRestart`
- `AiIdentityServiceTests.EmitsPacketOnNameChange`
- `FirstLaunchWizardTests.RunsOnceOnly`
- `FirstLaunchWizardTests.EachStepCompletesIndependently`
- `FirstLaunchWizardTests.SeedDefaultRegistryOption2`
- `FirstLaunchWizardTests.WritesFirstLaunchCompletedPacket`
- `PlainLanguageExplainerTests.CoversAiIdentitySetKind`
- `PlainLanguageExplainerTests.CoversFirstLaunchStepCompletedKind`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "IdentityRename|AiIdentity|FirstLaunch|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
README.md                                                            (rewritten)
WHAT_IS_WEVITO.md                                                    (new)
docs/INDEX.md                                                        (new)
docs/C_PHASE115_IDENTITY_RENAME_UX_LANGUAGE_PASS_2026-05-15.md
```

**Stop gates:**

- Stop if any C-PHASE-historical doc (C_PHASE0 through C_PHASE85c) is modified.
- Stop if `PetActor`, `PetMemoryStore`, `PetSimulationEngine` are renamed.
- Stop if `Wevito.VNext.Contracts.PetAgentContracts` namespace changes
  (API stability).
- Stop if the first-launch wizard runs more than once unless explicitly re-triggered.
- Stop if any rename leaves a dangling reference (compile must be 0 errors,
  0 warnings).
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; mechanical renames revert; wizard window disappears;
README reverts.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-115-identity-rename-ux-language-pass`
- PR title: `C-PHASE 115: Identity rename + UX language pass`.
- Phase report: `docs/C_PHASE115_IDENTITY_RENAME_UX_LANGUAGE_PASS_2026-05-15.md`.

**Auto-continue?** **Yes.** Mostly mechanical renames + UI restructure;
no new model surface; no new autonomy expansion.

---

## C-PHASE 116 — Chat-Context-Window Management at Year-Scale

**Goal:** Implement Q22 context management: explicit sessions (C-5), static
128K allocation (A-1), rolling summarization at 80% (S-3), hybrid mid-chat
retrieval (R-4), pinned context budget (P-3), tool result budget with
truncation marker (T-4), 6-month archival to cold storage (F-4).

**Scope:**

- Add `ChatContextBudgetService`:
  - Tracks token counts in the live context per session.
  - Static allocation per A-1: 4K system + 70K turns + 30K retrieval +
    16K tool buffer + 8K reply.
  - Provides `int RemainingTurnsBudget(Guid sessionId)`.
  - Emits `context_budget_pressure` packet at 80%.
- Add `RollingSummarizerService`:
  - Triggered by `ChatContextBudgetService` at 80% turns budget.
  - Reads oldest ~20K of turns from `ChatHistoryStore`.
  - Invokes Ollama with a short context (~25K) to compress to ~3K summary.
  - Writes summary to `PetMemoryStore` as a memory row with `kind=session_summary`.
  - Originals stay in `ChatHistoryStore` (FTS searchable).
  - Emits `summarization_run` packet.
- Add `RetrievalAutomaticInjector`:
  - Per user turn, embeds the user message via existing
    `OnnxTextEmbeddingService`.
  - Queries `PetMemoryStore` for top-3 nearest summaries / pinned items.
  - Injects results into next LLM call's context.
  - Emits `retrieval_triggered` packet with cosine scores.
- Add `retrieve_from_memory` tool definition (registered in C-PHASE 109's
  ToolRegistry):
  - Args: `query`, `top_k` (default 5), `kind_filter` (optional).
  - Returns top-K memory rows matching the query.
  - Used for explicit deep-dive retrieval beyond the automatic top-3.
- Add `PinnedContextStore`:
  - sqlite under `%LOCALAPPDATA%/Wevito/pinned-context.sqlite`.
  - Schema: `pin_id`, `content`, `pinned_at_utc`, `unpinned_at_utc`
    (nullable; pins are soft-unpinned not deleted).
  - 4K total token budget (P-3); FIFO-evict oldest if over budget.
  - UI surface: "📌 pin" button on each chat message (alongside 🔖
    benchmark button); long-press → "unpin".
- Add `ToolResultBudgetService`:
  - `(string truncated, string fullPath, int totalTokens, int truncatedTokens) FormatToolResult(string toolFamily, string rawResult)`.
  - Truncates at 4K tokens.
  - Stores full result to `vnext/artifacts/tool-results/<ts>-<family>/result.json`.
  - Injects "[truncated; full result at <relative-path>]" marker per T-4.
  - Emits `tool_result_truncated` packet when truncation occurs.
- Add `ChatColdStorageService`:
  - Runs on monthly cadence (registered as chore in C-PHASE 101's
    HouseholdMaintenanceService).
  - Identifies chat sessions inactive > 6 months.
  - Moves rows to `%LOCALAPPDATA%/Wevito/chat-history-cold.sqlite`.
  - Migrates associated memory summaries + pins.
  - Active store stays lean. Cold store has same schema + FTS5.
  - Lazy read: cold store consulted only when retrieval search yields no
    hits in active store.
  - Emits `chat_session_archived` packet per archival.
- New packet kinds: `context_budget_pressure`, `summarization_run`,
  `retrieval_triggered`, `tool_result_truncated`, `chat_session_archived`,
  `pinned_message_added`, `pinned_message_removed`.

**Pattern:** `RollingSummarizerService` follows `SelfImprovementReportService.cs`
for the windowed-summarize pattern. `RetrievalAutomaticInjector` follows
`LocalRetrievalService.cs` for the embed-and-query pattern.
`ChatColdStorageService` follows `LearningLabPromotionService.cs` for the
move-with-manifest pattern. `PinnedContextStore` follows
`UserFeedbackStore.cs` (from C-PHASE 102) for the append-only pattern with
soft-delete.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/ChatHistoryStore.cs                    (read-only consumers added)
vnext/src/Wevito.VNext.Core/ChatStreamingService.cs                (consume context budget)
vnext/src/Wevito.VNext.Core/HouseholdMaintenanceService.cs         (register chat-cold chore)
vnext/src/Wevito.VNext.Core/ToolInvocationService.cs                (consume tool result budget)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ChatPanel.xaml                         (add pin button)
vnext/src/Wevito.VNext.Shell/ChatPanel.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/ChatContextBudgetService.cs
vnext/src/Wevito.VNext.Core/RollingSummarizerService.cs
vnext/src/Wevito.VNext.Core/RetrievalAutomaticInjector.cs
vnext/src/Wevito.VNext.Core/PinnedContextStore.cs
vnext/src/Wevito.VNext.Core/ToolResultBudgetService.cs
vnext/src/Wevito.VNext.Core/ChatColdStorageService.cs
vnext/src/Wevito.VNext.Core/Tools/RetrieveFromMemoryTool.cs
vnext/src/Wevito.VNext.Core/Chores/ChatColdStorageChore.cs
vnext/tests/Wevito.VNext.Tests/ChatContextBudgetServiceTests.cs
vnext/tests/Wevito.VNext.Tests/RollingSummarizerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/RetrievalAutomaticInjectorTests.cs
vnext/tests/Wevito.VNext.Tests/PinnedContextStoreTests.cs
vnext/tests/Wevito.VNext.Tests/ToolResultBudgetServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ChatColdStorageServiceTests.cs
vnext/tests/Wevito.VNext.Tests/RetrieveFromMemoryToolTests.cs
docs/C_PHASE116_CHAT_CONTEXT_WINDOW_MANAGEMENT_2026-05-15.md
```

**Tests:**

- `ChatContextBudgetServiceTests.TracksTokensAccurately`
- `ChatContextBudgetServiceTests.EmitsPressurePacketAt80Percent`
- `RollingSummarizerServiceTests.TriggeredAt80Percent`
- `RollingSummarizerServiceTests.PreservesOriginalsInChatHistory`
- `RollingSummarizerServiceTests.SummaryStoredInPetMemoryStore`
- `RollingSummarizerServiceTests.RespectsKillSwitch`
- `RetrievalAutomaticInjectorTests.AutoInjectsTop3PerUserTurn`
- `RetrievalAutomaticInjectorTests.SkipsWhenNoRelevantMemory`
- `PinnedContextStoreTests.RespectsTokenBudget`
- `PinnedContextStoreTests.FifoEvictionOnBudgetExceeded`
- `PinnedContextStoreTests.SoftUnpinPreservesRow`
- `ToolResultBudgetServiceTests.TruncatesAt4kTokens`
- `ToolResultBudgetServiceTests.WritesFullResultToDisk`
- `ToolResultBudgetServiceTests.IncludesTruncationMarker`
- `ChatColdStorageServiceTests.MovesSessionsInactive6Months`
- `ChatColdStorageServiceTests.PreservesAppendOnlyInvariantInColdStore`
- `ChatColdStorageServiceTests.LazyReadOnRetrievalMiss`
- `RetrieveFromMemoryToolTests.ReturnsTopKMatchingQuery`
- `PlainLanguageExplainerTests.CoversAllContextManagementKinds`

**Validation commands:**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ChatContext|RollingSummarizer|RetrievalAutomatic|PinnedContext|ToolResultBudget|ChatColdStorage|RetrieveFromMemory|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Artifacts:**

```text
%LOCALAPPDATA%/Wevito/chat-history-cold.sqlite                       (after first archival)
%LOCALAPPDATA%/Wevito/pinned-context.sqlite
vnext/artifacts/tool-results/<ts>-<family>/result.json
vnext/artifacts/summarization-runs/<ts>-session/summary.json
docs/C_PHASE116_CHAT_CONTEXT_WINDOW_MANAGEMENT_2026-05-15.md
```

**Stop gates:**

- Stop if context budget can overflow (must short-circuit before token limit).
- Stop if summarizer modifies originals (must be read-only on `ChatHistoryStore`).
- Stop if cold storage loses rows (must be byte-for-byte copy then delete).
- Stop if cold store violates append-only invariant.
- Stop if pinned context can grow past its budget.
- Stop if tool result truncation loses the marker injection.
- Stop if any new packet kind is missing from `PlainLanguageExplainer`.

**Rollback:** revert PR; context grows unbounded toward Qwen's 128K limit;
tool results inject full bytes; chat history grows without archival.

**Commit/PR:**

- Branch: `claude-implementation/c-phase-116-chat-context-window-management`
- PR title: `C-PHASE 116: Chat-context-window management at year-scale`.
- Phase report: `docs/C_PHASE116_CHAT_CONTEXT_WINDOW_MANAGEMENT_2026-05-15.md`.

**Auto-continue?** **Yes.** Read-summarize-archive pattern + storage
management; no new model surface; no new mutation pathway.

---

## Sequencing Recommendation (Final Order in Master Plan)

Per `DECISION_LEDGER_2026-05-15.md §11`, the reframe phases interleave
with the gap phases and sprite rungs. Recommended landing order:

```text
0.   USER SOAK START                  (front-loaded per S-11d; parallel)
1.   C-PHASE 106                      Default-enabled reasoning LLM
2.   C-PHASE 107                      Chat UI
3.   C-PHASE 108                      Agent-slot redesign
4.   C-PHASE 109                      Tool registry first-class
5.   C-PHASE 110                      Pet/AI isolation contract
6.   C-PHASE 111                      User-PC coexistence policy
7.   C-PHASE 101                      Household maintenance     (now wevito has day-1 work)
8.   C-PHASE 102                      User feedback ingestion
9.   C-PHASE 112                      Benchmark suite v1
10.  C-PHASE 113                      Benchmark curation UI
11.  C-PHASE 116                      Chat context window management
12.  C-PHASE 115                      Identity rename + UX language pass
13.  C-PHASE 99                       Palette grammar registry
14.  C-PHASE 100                      General fallback grammar
15.  C-PHASE 98                       Image generation runtime
16.  C-PHASE 117 (Sprite Rung 1)      Golden-seed UI + heuristic prefilter
17.  C-PHASE 118 (Sprite Rung 2)      Image embedding scoring
18.  C-PHASE 119 (Sprite Rung 3)      Palette conformer + propagation
19.  SOAK ENDS (~day 7)
20.  C-PHASE 85d                      Autonomous operations promotion
21.  C-PHASE 95                       Build hot-swap wrapper
22.  C-PHASE 96                       Codex loop runner (initial)
23.  C-PHASE 114                      Codex loop reliability hardening
24.  C-PHASE 86                       Pre-approved scope service
25.  C-PHASE 87                       Sandboxed experiment runner
26.  C-PHASE 88                       Experiment kind: sprite-template
27.  C-PHASE 93                       Composite fitness scoreboard
28.  C-PHASE 94                       Maturity promotion service
29.  C-PHASE 89                       Experiment kind: LoRA hyperparam search
30.  C-PHASE 97                       Image LoRA training pipeline
31.  C-PHASE 90                       Constitutional decision service
32.  C-PHASE 91                       Approved research connector (R3)
33.  C-PHASE 92                       Self-improvement promotion gate
34.  C-PHASE 103                      Memory consolidation
35.  C-PHASE 104                      Strategic planner
36.  C-PHASE 105                      Activity digest UI
37.  C-PHASE 120 (Sprite Rung 4)      Remaining animations
```

Phases 21-23 (build pipeline + loop reliability) cluster together as the
"loop hardens itself" group. After phase 23 lands, every subsequent phase
runs through the fully-hardened loop with no human intervention required
except at `Auto-continue=No` gates.

## Closing Notes for Codex

Each phase above follows the standard template; medium reasoning effort is
the target. When picking up a phase:

- Read `DECISION_LEDGER_2026-05-15.md` to resolve any decision ID
  referenced in the scope section.
- Read the named **Pattern** file before writing the new file.
- Mirror the pattern's test names + dependency-injection shape.
- Set evidence-packet `did_use_*` flags honestly.
- Consult KillSwitch at the top of any public method that writes state.
- Halt at the first stop gate that becomes true; never auto-merge a phase
  whose stop gates fired.

If validation fails or stop gates trigger, halt the loop; do not amend
the merge. The loop runner from C-PHASE 114 will write a remediation card
or open a Stop card per the failure-response policy (Q20-F4).
