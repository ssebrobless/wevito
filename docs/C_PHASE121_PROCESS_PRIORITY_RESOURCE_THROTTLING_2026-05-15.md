# C-PHASE 121 - Process Priority And Resource Throttling

Date: 2026-05-15
Branch: `claude-implementation/c-phase-121-process-priority-resource-throttling`
Decision context: non-interruption and user-PC coexistence hardening after C-PHASE 116.

## Goal

Protect the user's foreground PC experience while Wevito runs background helper work.

## Scope

- Added background-friendly process priority policy.
- Added active-user disk I/O throttling policy.
- Added Codex compile/test processor cap policy.
- Added Windows Game Mode coexistence signal.
- Added user-protection floors to `RuntimeBudgetMeter`.
- Added Settings UI controls for resource throttling.

## Implemented

- `ProcessPriorityManagerService`
  - Sets Wevito process targets to `BelowNormal` when enabled.
  - Allows short foreground boosts to `Normal`.
  - Refuses `AboveNormal`, `High`, and `RealTime`.
  - Honors `KillSwitchService`.
- `DiskIoBudgetService`
  - Caps background writes at 20 MB/s while the user was active in the last 30 seconds.
  - Suggests cooperative one-second yields.
  - Emits `disk_io_budget_throttled`.
- `CodexCompileThrottleService`
  - Uses `MSBUILDPROCESSORCOUNT=2` while the user is active.
  - Allows up to 6 when the user is idle for 5 minutes.
  - `tools/run-codex-loop.ps1` now sets the environment cap when a phase starts.
- `GameModeDetectorService`
  - Reads Windows Game Bar/Game Mode registry signal.
  - Feeds `game_mode` into `CoexistenceTriggerService`.
  - Emits `game_mode_detected` and `game_mode_cleared`.
- `RuntimeBudgetMeter`
  - Blocks non-foreground background reservations when Wevito CPU, GPU utilization, or available RAM violate user-protection floors.
  - Foreground user work can still proceed even below the floor.
- Settings UI
  - Added toggles for BelowNormal priority, disk I/O budget, and Game Mode honor.
  - Added Codex active compile cap selector.

## Safety Boundaries

- No hosted AI calls.
- No network calls.
- No model downloads.
- No sprite/runtime asset mutation.
- No process is set above `Normal`.
- Foreground user-initiated work is not blocked by the new protection floors.
- Registry access safely degrades when unavailable.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ProcessPriority|DiskIoBudget|CodexCompileThrottle|GameModeDetector|UserProtectionFloor|PlainLanguage"` passed: 166/166.
- `dotnet build .\vnext\Wevito.VNext.sln` passed: 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 866/866.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Stop Gate Review

- AboveNormal or higher priority: false. Service refuses those classes.
- Disk I/O budget exceeded by > 5 MB/s for > 30s sustained: false in policy tests; live long-duration disk stress was not run.
- Codex compile uses all 8 cores while user active: false. Active cap is 2 by default.
- Game Mode detector misses known game executable: false by injected Game Mode signal test; no live known-game executable was launched during validation.
- User-protection floor blocks foreground task: false. Foreground bypass test passed.
- Missing plain-language packet kind: false. All new resource-throttling packet kinds are covered.

## Next Phase

Auto-continue is `No` for this phase because it touches OS-level priority/resource behavior. Review PR before proceeding.
