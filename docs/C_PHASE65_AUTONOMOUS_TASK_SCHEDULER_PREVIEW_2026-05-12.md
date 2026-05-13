# C-PHASE 65 Autonomous Task Scheduler Preview

Date: 2026-05-12
Branch: `claude-implementation/c-phase-65-autonomous-scheduler-preview`

## Goal

Let Wevito propose useful background work as draft task cards while preserving the local-first safety posture: no hidden preview dispatch, no execution, no network, no training, and no mutation.

```text
C-PHASE 65 shape
|
+-- scheduler setting defaults off
+-- runtime supervisor must allow background work
+-- runtime budget meter must reserve capacity
+-- scheduler trigger must be allowlisted
+-- draft task card is created
+-- scheduler evidence packet is written
`-- user must manually preview anything useful
```

## Implemented

- Added `AutonomousTaskScheduler`.
- Added allowlisted scheduler triggers:
  - `stale_dashboard_report`
  - `pending_reviewed_bundle`
  - `failed_prior_proof_packet`
  - `recurring_user_request`
- Added `SchedulerEvidencePacket` and `SchedulerProposalResult`.
- Added `RuntimeBudgetMeter` with hourly persisted budget state at `%LOCALAPPDATA%/Wevito/audit/budget-meter.json`.
- Added `RuntimeSupervisorService.ReadBudgetSnapshot`.
- Wired shell polling every five minutes, gated by:
  - `scheduler_enabled=true`
  - `RuntimeSupervisorStatus.BackgroundWorkAllowed=true`
  - runtime budget availability
- Added a settings row: "Allow Wevito to propose background tasks (preview only)".
- Added `autonomous_scheduler` metadata to `vnext/content/tool_definitions.json`.
- Added Claude local-first implementation plan docs to the repo so future phases have the same source-of-truth instructions.

## Safety Boundaries

This phase does not:

- call hosted AI
- fetch the web
- read credentials
- call `PetTaskAdapterPreviewDispatcher`
- run execution adapters
- train or tune anything
- mutate runtime/source assets
- create task cards beyond `Draft`

The scheduler is dormant by default because both `scheduler_enabled` and `runtime_background_work_allowed` default to `false`.

## Validation

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AutonomousTaskScheduler|RuntimeBudgetMeter|RuntimeSupervisor"
```

Result: PASS, 18 / 18.

Full phase validation:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Results:

- `dotnet build`: PASS, 0 warnings/errors.
- `dotnet test --no-build`: PASS, 327 / 327.
- `build-vnext -SkipAssetPrep -SkipTests`: PASS.

## Next Phase

C-PHASE 66 should add the reviewed learning promotion gate. It can use C-PHASE 65 scheduler proposals later, but promotion must still require explicit user approval and must not train a model.

```text
next safe step
|
+-- reviewed Creative Learning Lab bundle
+-- accept labels only
+-- approved task card required
+-- write dataset manifest + JSONL
`-- no training
```
