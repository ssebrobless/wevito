# C-PHASE 108 - Agent-Slot Redesign

Date: 2026-05-15
Branch: claude-implementation/c-phase-108-agent-slot-redesign
Decision IDs: Q16-A4, Q16-CM3, Q16-NM3, Q16-RS3, Q16-DA1+DA3, Q16-VS3, I-Reframe

## Goal

Replace the old hardcoded Scout / Inspector / Builder helper-pet model with three visible agent slots. The slots mirror active pet names when pets exist and fall back to `Agent 1`, `Agent 2`, and `Agent 3` when fewer than three pets are active.

## Implemented

- Deleted the `PetHelperRole`, `HelperPet`, and `HelperPetState` contract surface.
- Removed the hardcoded Scout / Inspector / Builder helper GUIDs and default roster construction.
- Added `AgentSlot`, `AgentSlotStatus`, `TaskOrigin`, and `InFlightToolCall` contracts.
- Added `AgentSlotService` for pet-name mirroring, fallback slot naming, deterministic non-hardcoded slot IDs, per-slot tool-family priorities, and rename evidence packets.
- Added `AgentSlotDispatcher` so user-visible routing consults the model adapter while background work picks the first idle slot.
- Added `AgentToolConcurrencyCoordinator` so tool calls can run in parallel while local LLM generation is serialized through a single gate.
- Updated model request surfaces from helper role enums to string agent roles.
- Updated PET TASKS UI wording from helper-pet language toward agent slots.
- Added home-panel ambient agent badges showing each slot name and current/idle tool state.
- Added plain-language packet support for `agent_slot_assigned`, `agent_slot_renamed`, and `agent_slot_status_changed`.

## Safety Boundaries

- No hosted AI calls were made.
- No model weights were downloaded.
- No sprite/runtime/source PNGs were touched.
- No tool execution modes were broadened.
- Existing report-first PET TASKS behavior remains intact.
- Agent names are presentation/routing labels only; they do not grant permission.

## Stop Gates

- `PetHelperRole` enum still exists anywhere in source: false.
- Hardcoded helper-pet GUID still exists in source: false.
- LLM generation can run concurrently for two agents: false, covered by `AgentToolConcurrencyCoordinatorTests`.
- Agent names do not update live on pet rename: false, covered by `AgentSlotServiceTests` rename packet test.
- Dispatcher hands user-visible tasks to non-AI logic: false, covered by `AgentSlotServiceTests.Dispatcher_UserVisibleTaskConsultsModelAdapter`.
- New packet kinds missing from `PlainLanguageExplainer`: false, covered by focused PlainLanguage test filter.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AgentSlot|AgentTool|PlainLanguage"`: passed, 75/75.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 652/652.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- Stop-gate source scan for old helper-role symbols and hardcoded helper GUIDs: passed, 0 matches in `vnext/src`.

## Next Phase

C-PHASE 109 should build on this slot model when wiring chat/tool action dispatch. This phase stops after PR creation per the plan.
