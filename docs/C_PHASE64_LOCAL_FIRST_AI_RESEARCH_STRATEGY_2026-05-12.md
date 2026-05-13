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

These boundaries describe the C-PHASE 64 safety posture, not the final product ceiling. The app should eventually earn the right to use web research, approved local files, local tool execution, local learning, local training/tuning, and guarded code/asset mutation. C-PHASE 64 keeps those disabled because the trust rails must exist before the autonomy is turned on.

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

## Current vs Eventual Capability Ladder

```text
Wevito autonomy ladder
|
+-- C-PHASE 64 now
|   +-- local provider mode contract exists
|   +-- deterministic local adapter can preview
|   +-- localResearch writes evidence packets
|   +-- no network, no training, no mutation
|   `-- hosted AI stays disabled
|
+-- near term
|   +-- schedule report-only work
|   +-- promote reviewed examples into local memory/evals
|   +-- run local evals that prove behavior improved
|   +-- summarize what happened while always-on
|   `-- remain preview-first
|
+-- earned autonomy
|   +-- fetch web sources through approved connectors
|   +-- read approved local files and app state
|   +-- run approved non-destructive tools
|   +-- learn from reviewed outcomes
|   +-- train/tune local models or routing policies
|   `-- propose code/assets changes with backup/hash/rollback/proof
|
`-- target state
    +-- Wevito is the AI runtime
    +-- local models and local memory are the normal brain
    +-- hosted AI is not required
    +-- user can leave it running without losing control of the PC
    `-- high-risk changes remain gated and reversible
```

## Capability Policy

| Capability | C-PHASE 64 status | Eventual target | Required gate before enable |
| --- | --- | --- | --- |
| Web research | Disabled | App-owned research connector with citations/cache | User approval, source log, rate limits, privacy filter |
| API keys | Not read | Allowed for non-AI services or optional cloud bootstrap only | Explicit provider config, visible purpose, no silent reads |
| Hosted AI | Disabled | Optional accelerator, not runtime brain | Separate approval, consent notice, capability flag |
| Local model use | Stubbed by deterministic adapter | Local inference runtime for normal helper tasks | Install check, offline proof, fallback behavior |
| Local learning | Not training | Reviewed examples -> memory/evals -> local improvement | Human-reviewed labels, promotion report, rollback |
| Local training/tuning | Disabled | Local-only tuning when useful and measurable | Dataset manifest, eval improvement, resource controls |
| Asset/code mutation | Disabled | Guarded apply with proof | Dry run, exact scope, backup hashes, rollback drill, post-proof |
| Background autonomy | Disabled | Quiet always-on preview/execution ladder | Supervisor policy, resource budget, visible activity history |

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

## Reorganized Next Phases

```text
next path after C-PHASE 64
|
+-- C-PHASE 65: scheduler preview
|   `-- Wevito proposes useful work while obeying quiet/pet-only modes
|
+-- C-PHASE 66: reviewed learning promotion
|   `-- accepted Creative Learning Lab examples become local memory/eval data
|
+-- C-PHASE 67: local learning/eval loop
|   `-- prove behavior changes are measurable, local, reversible, and explainable
|
+-- C-PHASE 68: self-improvement activity reports
|   `-- always-on work becomes visible without pet task-completion animations
|
+-- C-PHASE 69: independent assistant beta gate
|   `-- decide whether limited autonomy is ready
|
+-- C-PHASE 70: approved web research connector
|   `-- enable web fetching only with citations, cache, privacy rules, and user control
|
+-- C-PHASE 71: approved local file/tool access expansion
|   `-- let pets inspect approved folders/tools without broad PC control
|
+-- C-PHASE 72: local model runtime integration
|   `-- connect a real local inference runtime while keeping hosted AI optional/off
|
+-- C-PHASE 73: local training/tuning pipeline
|   `-- train or tune local behavior only from reviewed datasets with eval gates
|
+-- C-PHASE 74: guarded code/asset mutation execution
|   `-- allow reversible changes through exact-scope proof packets
|
`-- C-PHASE 75: autonomous operations beta
    `-- limited self-directed work with pause, budget, rollback, and daily report controls
```
