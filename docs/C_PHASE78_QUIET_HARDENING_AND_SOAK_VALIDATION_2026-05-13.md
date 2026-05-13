# C-PHASE 78: Quiet Hardening And Soak Validation

## Goal

Prepare Wevito for trustworthy always-on operation by hardening quiet/fullscreen behavior and adding a manual soak validation harness.

## Scope

- Added sustained fullscreen-other monitoring with 5-second enter and 15-second exit thresholds.
- Added Windows power/session handling for sleep, resume, lock, and unlock events.
- Added a persistent focus-steal counter under `%LOCALAPPDATA%/Wevito/audit/focus-steal.json`.
- Added budget-meter startup replay / flush support so budget state exists even after process interruption.
- Added a report-only soak runner service and manual `tools/run-soak-validation.ps1` helper.

## Implemented

- `WindowsForegroundFullscreenMonitor` observes desktop context and emits `fullscreen_state_change` audit rows on sustained transitions.
- `WindowsPowerHandler` subscribes to `SystemEvents.PowerModeChanged` and `SystemEvents.SessionSwitch`; sleep/lock force Quiet and disable background helper work, while resume/unlock are audited without returning Wevito to Active.
- `FocusStealCounter` increments only when a Wevito window receives activation while a fullscreen-other foreground is active.
- `SoakRunnerService` starts only by explicit caller opt-in, refuses KillSwitch/default-off capability violations, writes `soak_session_start`, and can record `soak_session_end`.
- `run-soak-validation.ps1` creates a manual soak artifact folder and records paths/status without flipping settings or launching hidden work.

## Safety Boundaries

- No hosted AI calls.
- No network fetches.
- No sprite/runtime asset mutation.
- No automatic capability enablement.
- No hidden training or model use.
- No automatic return from Quiet to Active after resume/unlock.
- The soak script is manual and non-mutating; a real 4-hour soak remains user-run.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Fullscreen|Power|FocusSteal|Soak"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-soak-validation.ps1 -Hours 1 -ArtifactRoot .\vnext\artifacts\c-phase-78-soak-script-smoke`

## Manual Soak Gate

This phase must stop before promotion. The user should run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-soak-validation.ps1 -Hours 4 -ArtifactRoot .\vnext\artifacts\soak\
```

The manual soak should be reviewed for:

- zero focus-steal increments during fullscreen/other-app use,
- no hidden capability flips,
- no network/model activity unless explicitly approved in a later phase,
- stable budget-meter and audit-ledger files,
- no user-experience interference while normal PC work continues.

## Next Phase

C-PHASE 79 is blocked until the manual soak gate is reviewed, because it introduces local runtime onboarding surfaces.
