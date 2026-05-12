# C-PHASE 63 Always-On Runtime Supervisor

Date: 2026-05-12
Branch: `claude-implementation/c-phase-63-always-on-runtime-supervisor`

## Goal

Make Wevito safer to leave running while the user plays games, browses, codes, or uses other apps.

```text
always-on control
|
+-- user can keep pets visible
+-- helper work stays paused unless allowed
+-- quiet mode blocks helper work
+-- pet-only mode keeps Wevito as a pet overlay
+-- fullscreen apps trigger quiet behavior
+-- settings expose "what is Wevito doing?"
`-- no model calls, no sprite mutation, no asset prep
```

## Implemented

- Added `RuntimeSupervisorService` in vNext Core.
- Added safe default settings:
  - `runtime_quiet_mode=false`
  - `runtime_pet_only_mode=false`
  - `runtime_background_work_allowed=false`
  - `runtime_no_focus_steal=true`
  - `runtime_auto_quiet_fullscreen=true`
  - `runtime_max_background_tasks_per_hour=4`
  - `runtime_cpu_budget_percent=20`
  - `runtime_memory_budget_mb=512`
- Added Settings UI controls for quiet mode, pet-only mode, background helper previews, no-focus-steal, and fullscreen auto-quiet.
- Added a runtime status line in Settings so the user can see what Wevito is doing.
- Integrated the supervisor into Shell:
  - pet-only mode hides helper/tool surfaces but keeps Settings reachable so the user can turn it off
  - quiet/fullscreen auto-quiet blocks helper preview/execution work
  - default mode still allows user-initiated PET TASKS previews
- Added unit tests for supervisor modes, fullscreen auto-quiet, user-initiated work, and default settings.

## Safety Boundaries

This phase did not:

- call any model provider
- enable hosted AI
- generate or mutate sprites
- run asset prep
- add screen recording
- add autonomous scheduling
- execute background tasks automatically

## Validation

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "RuntimeSupervisor|PetTask|Settings"
```

Result: PASS, 37 / 37.

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Results:

- `dotnet build`: PASS, 0 warnings/errors.
- `dotnet test --no-build`: PASS, 307 / 307.
- `build-vnext -SkipAssetPrep -SkipTests`: PASS.

## Next Phase

C-PHASE 64 should build the local-first app-owned AI/research strategy. It should not start from hosted model calls. The correct shape is:

```text
local-first AI loop
|
+-- app-owned research planner
+-- local/offline provider interface
+-- source/evidence packets
+-- synthesis reports
+-- hosted providers remain explicit opt-in
`-- no GPT/Claude/Gemini/Codex runtime dependency
```
