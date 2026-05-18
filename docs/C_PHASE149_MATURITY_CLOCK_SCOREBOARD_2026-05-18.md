# C-PHASE 149 - Maturity Clock Scoreboard

## Goal

Add a maturity clock and read-only Evidence-tab scoreboard derived from the append-only audit ledger. The clock is informational only; it does not enable, promote, approve, apply, or flip capability flags.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityClock.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityScoreboardService.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityClockResetReason.cs`
- `vnext/tests/Wevito.VNext.Tests/MaturityScoreboardServiceTests.cs`

Extended:

- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`

Not touched:

- Capability flag defaults.
- Model adapters.
- Apply runners.
- Held-out eval content.
- Sprite/runtime assets.

## Implemented

- Added `MaturityScoreboardService`, a ledger-derived scoreboard that counts progress increments from:
  - `self_improvement_dry_run_completed`
  - `self_improvement_apply_completed`
  - `self_improvement_rollback_verified`
  - `self_improvement_eval_completed` only when applicable gates are reported as passed, not `NotApplicable`.
- Added reset-reason detection for:
  - `InvariantViolation`
  - `SilentMutationDetected`
  - `SilentNetworkDetected`
  - `SilentHostedAiDetected`
  - `FailedRollback`
  - `UserRejectedProposal`
- The service writes only `self_improvement_maturity_clock_reset` packets, and only one packet per detected reason already absent from the ledger.
- Added an Evidence-tab "Maturity" panel showing total increments, component counts, and reset reasons.
- Added tests for progress counting, `NotApplicable` eval exclusion, reset packet writing, duplicate reset suppression, kill-switch blocking, read-only SQL behavior, and reset-reason detection.

## Safety Boundaries

- No capability flag changes based on maturity score.
- No model adapter call.
- No held-out eval data is exposed; only aggregate pass/not-applicable derived counts are used.
- No unknown packet kind is counted as progress.
- Reset packet writes set `DidUseNetwork=false`, `DidUseHostedAi=false`, `DidUseLocalModel=false`, and `DidMutate=false`.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "MaturityScoreboard|MaturityClock|PlainLanguage|SelfImprovement"`: passed, 246/246.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1114/1114.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No capability flag flips based on a clock value.
- [x] Scoreboard does not expose held-out eval content.
- [x] Service writes no packet kind other than `self_improvement_maturity_clock_reset`.
- [x] Unknown packet kinds are not counted as progress.

## Next Phase

C-PHASE 150 - Supervised self-improvement pilot. Auto-continue is No; wait for user approval before starting.
