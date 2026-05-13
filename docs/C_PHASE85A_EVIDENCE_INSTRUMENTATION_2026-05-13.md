# C-PHASE 85a: Evidence-Collection Instrumentation

## Goal
Make Wevito emit honest audit-ledger evidence during normal runtime use so a later soak window can evaluate autonomous-beta readiness from real signals instead of synthetic promotion rows.

## Scope
- Added `RuntimeSessionTracker` for runtime session start, hourly heartbeat, paused, and end packets.
- Added `DailyEvidenceSnapshotService` for once-per-day resource-budget and focus-steal snapshots.
- Wired both services into `ShellCoordinator` without enabling any new capability.
- Tightened `RuntimeBudgetMeter.ReadCurrent(...)` so read-only budget snapshots use the same budget-limit reasoning as reservations without consuming a slot.
- Updated `WindowsPowerHandler` evidence rows so power/session packets are `Completed` rows and do not look like policy violations.
- Updated `AutonomousBetaDecisionService` to require precise uptime, budget, and focus snapshot evidence, while limiting policy violations to explicit policy blocks or errored policy rows.
- Added plain-language explanations for all new packet kinds.
- Added focused tests for runtime session tracking, daily snapshots, beta-decision precision, and power evidence status.

## Safety Boundaries
- No default-off capability was enabled.
- No model call, hosted AI call, network access, training, local file exploration, or mutation surface was added.
- KillSwitch suppresses daily snapshots and emits runtime-session paused evidence instead of continuing normal session proof.
- Runtime heartbeats are rate-limited to one per configured interval, defaulting to 60 minutes and clamped to a minimum of 15 minutes.
- Power/session handling still forces Quiet mode on sleep/lock and does not auto-return Wevito to Active on resume/unlock.

## Validation
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "RuntimeSession|DailyEvidenceSnapshot|AutonomousBeta|PlainLanguage|WindowsPower"` passed: 61/61.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 530/530.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Next Phase
C-PHASE 85b is the next planned phase, but auto-continue is **NO**. This PR should stop for review before starting the soak driver and evidence-collection status UX.
