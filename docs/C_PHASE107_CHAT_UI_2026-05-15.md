# C-PHASE 107 — Chat UI

## Goal

Replace the PET TASKS-first helper surface with a local-first multi-turn chat entry point while keeping the existing report-only task-card surface available behind a compact expander.

## Scope

- Added append-only SQLite chat history at `%LOCALAPPDATA%/Wevito/chat-history.sqlite`.
- Added FTS5 chat search and soft-delete filtering in search queries.
- Added session, streaming, title, and view-model services for local chat.
- Added an Ollama-compatible SSE streaming path with deterministic fallback when Ollama is unavailable.
- Added `ChatPanel` into `ToolPopupWindow` and moved the existing task-card controls behind `Task cards and report-only adapters`.
- Registered local chat in `vnext/content/tool_definitions.json`.
- Added plain-language packet coverage for:
  - `chat_session_started`
  - `chat_turn_completed`
  - `chat_turn_cancelled`
  - `chat_session_title_set`
  - `chat_search_performed`

## Safety Boundaries

- No hosted AI calls were added.
- Ollama streaming is localhost-only and falls back safely.
- Mid-stream tool tokens are detected, but no tool executes in this phase. Tool execution remains deferred to C-PHASE 109, so no `UnifiedPolicyService` bypass is introduced.
- Chat turn rows are append-only via SQLite triggers; direct UPDATE and DELETE attempts fail.
- Cancellation does not commit an assistant row or tool row after cancellation.
- Chat UI streaming is async and does not block the WPF UI thread.
- Search filters out soft-deleted sessions.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Chat|PlainLanguage"` passed: 74/74.
- `dotnet build .\vnext\Wevito.VNext.sln` passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 645/645.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Rollback

Revert the C-PHASE 107 PR. The chat panel, chat services, packet kinds, and tool definition disappear; PET TASKS task cards remain available through the prior surface.

## Next Phase

C-PHASE 108 should redesign helper-pet routing into explicit agent slots. Do not start it from this branch because C-PHASE 107 is `Auto-continue=No`.
