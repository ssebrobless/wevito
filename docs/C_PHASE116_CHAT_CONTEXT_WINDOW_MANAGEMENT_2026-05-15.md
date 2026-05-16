# C-PHASE 116 - Chat-Context-Window Management

Branch: `claude-implementation/c-phase-116-chat-context-window-management`

## Goal

Implement the Q22 context-management layer so Wevito can keep long-running local chats useful without overflowing the local model window.

## Scope

- Added static context-budget tracking for the 128K Qwen allocation.
- Added rolling summarization triggered at 80% of the current-turns budget.
- Added automatic local-memory retrieval injection for user turns.
- Added pinned context storage with a dedicated FIFO-enforced token budget.
- Added tool-result truncation with full-result artifacts and inline retrieval markers.
- Added cold chat storage for inactive sessions using copy-to-cold plus active soft-delete so append-only chat turns remain protected.
- Registered `retrieve_from_memory` as an explicit local tool surface.

## Implemented

- `ChatContextBudgetService` tracks turn-token usage, remaining budget, and emits `context_budget_pressure`.
- `RollingSummarizerService` summarizes the oldest bounded turn window, stores `kind=session_summary` in `PetMemoryStore`, preserves originals in `ChatHistoryStore`, and emits `summarization_run`.
- `RetrievalAutomaticInjector` retrieves top local memory rows per user turn and emits `retrieval_triggered`.
- `PinnedContextStore` writes `%LOCALAPPDATA%/Wevito/pinned-context.sqlite`, soft-unpins rows, and FIFO-evicts active pins over budget.
- `ToolResultBudgetService` truncates large results, writes full JSON artifacts under `vnext/artifacts/tool-results/`, and emits `tool_result_truncated`.
- `ChatColdStorageService` copies inactive sessions to `chat-history-cold.sqlite`, soft-deletes the active session row, preserves append-only turn triggers, and emits `chat_session_archived`.
- `ChatPanel` now exposes a Pin button beside benchmark bookmarking.
- `ToolInvocationService` now routes adapter output through the tool-result budget.

## Safety Boundaries

- No hosted AI calls were added.
- No sprite or runtime asset mutation.
- No model weight downloads.
- Chat turns remain append-only; cold storage does not drop triggers or physically delete turn rows.
- KillSwitch is checked before all new state-writing services mutate settings, sqlite rows, artifacts, or memory.
- Tool result truncation always includes a marker pointing to the full stored artifact.

## Validation

- Focused context-management tests passed: `154 / 154`.
- `dotnet build .\vnext\Wevito.VNext.sln` passed with `0` warnings and `0` errors.
- Full vNext tests passed: `836 / 836`.
- Safe publish passed with `-SkipAssetPrep -SkipTests`.
- `vnext/content/tool_definitions.json` parsed as valid JSON.

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ChatContext|RollingSummarizer|RetrievalAutomatic|PinnedContext|ToolResultBudget|ChatColdStorage|RetrieveFromMemory|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

## Notes

- C-PHASE 116 requested "move rows" to cold storage. The existing active chat store enforces append-only turn rows with no-delete triggers, so this implementation copies inactive sessions to cold storage and soft-deletes active sessions instead of physically deleting turns. This keeps the user-facing active session list lean while preserving the append-only invariant.
- `retrieve_from_memory` is registered as an explicit local tool definition. The concrete `RetrieveFromMemoryTool` is available for explicit in-process retrieval and can be wired into richer tool invocation flows later without changing the storage contract.
