# C-PHASE 64 Local-First AI And Research Strategy

Date: 2026-05-12
Branch: `claude-implementation/c-phase-64-local-first-ai-research`

## Goal

Move Wevito toward becoming its own app-owned AI/research runtime without making GPT, Claude, Gemini, Codex, or another hosted service the backend brain.

```text
C-PHASE 64 shape
|
+-- local/offline provider mode contract
+-- deterministic local model adapter
+-- local-first research planner
+-- evidence packet schema
+-- PET TASKS localResearch preview adapter
+-- no hosted model call
+-- no network fetch
`-- no autonomous execution
```

## Implemented

- Added `ModelProviderModeService` with explicit modes:
  - `disabled`
  - `local_only`
  - `approved_cloud`
- Added safe provider defaults:
  - provider mode: `disabled`
  - local provider: `deterministic-local`
  - local provider available: `false`
  - hosted provider: `none`
  - hosted provider approved: `false`
- Added `LocalModelAdapter`, a deterministic no-provider adapter that produces local suggestions without network calls.
- Added `ResearchEvidencePacket` records:
  - source records
  - claim records
  - synthesis
  - next recommended action
  - hosted-AI/network usage flags
- Added `ResearchPlannerService`, which creates local-first evidence packets without fetching the web or calling hosted AI.
- Added a report-only `localResearch` PET TASKS preview adapter that writes:
  - `research-evidence-packet.json`
  - `run-summary.md`
- Added parser/routing support so research requests route to the ResearchHelper.
- Updated tool definitions and PET TASKS copy to include `localResearch`.

## Safety Boundaries

This phase did not:

- call a hosted model
- read API keys
- fetch the web
- enable browser automation
- mutate sprites/assets/source boards
- run asset prep
- add autonomous scheduling
- train or fine-tune a model

Hosted providers remain a later explicit opt-in path only. Normal operation should continue moving toward `local_only`.

## Validation

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Model|Provider|Research|PetCommand"
```

Result: PASS, 62 / 62.

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Results:

- `dotnet build`: PASS, 0 warnings/errors.
- `dotnet test --no-build`: PASS, 316 / 316.
- `build-vnext -SkipAssetPrep -SkipTests`: PASS.

## Next Phase

C-PHASE 65 should add an autonomous scheduler preview layer. It should use the C-PHASE 63 supervisor and C-PHASE 64 localResearch adapter to propose report-only work, not execute hidden background tasks.

```text
scheduler rule
|
+-- propose only
+-- obey quiet/pet-only mode
+-- write evidence artifacts
+-- never mutate without approval
`-- summarize visible activity
```
