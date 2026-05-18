# C-PHASE 150: Supervised Self-Improvement Pilot

## Goal

Wire the supervised self-improvement loop end-to-end as a proposal-only pilot. The loop is default-off, runs only on the existing autonomous cadence, and cannot apply, merge, promote, or mutate without an explicit user approval object.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoopSettings.cs`
- `vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopSafetyTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/UserApplyApprovalValidatorTests.cs`

## Implemented

- Added `supervised_improvement_loop_enabled`, defaulted to `false` in shell settings hydration.
- Added `SupervisedImprovementLoop` as a proposal-only layer after autonomous scopes run.
- The loop requires all of these to be true before ticking:
  - KillSwitch off.
  - autonomous beta enabled.
  - `sprite-repair-batch-proposal` scope enabled.
  - supervised pilot enabled.
  - runtime supervisor active and background work allowed.
- Each eligible tick can add at most one `self_improvement_apply_awaiting_approval` card.
- The approval card records scope id, operation id, source proposal card id, and an evidence artifact path.
- The Tool Popup Autonomy tab now exposes `Supervised improvement (pilot)` with the exact safety copy: `Proposal-only. Apply still requires explicit approval.`
- The Tool Popup renders any open awaiting-approval card with a typed operation-id confirmation field and `Approve apply` button.
- The Tool Popup is the only production caller that constructs `UserApplyApproval`.
- Accepted approval validates through `UserApplyApprovalValidator`, then intentionally writes `self_improvement_apply_refused` with `apply_runner_not_implemented_in_v0`.
- Refused approval writes `self_improvement_apply_refused` with the validator reason.

## Safety Boundaries

- No hosted AI call.
- No network call.
- No local model call.
- No sprite mutation.
- No apply runner implementation.
- No auto-approve, bulk approve, or trust-this-scope button.
- No capability flag defaults changed except adding the new supervised pilot flag as default-off.
- The loop never constructs `UserApplyApproval`.
- Apply remains blocked even after a valid typed approval because v0 has no apply runner.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SupervisedImprovementLoop|UserApplyApproval|ExperimentRegistry|ConstitutionalDecision|EvalGate|SpriteRepairBatchProposal|MaturityScoreboard|PlainLanguage|SelfImprovement"`: passed, 289/289.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1125/1125.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- Loop applies without `UserApplyApproval`: false.
- Loop ticks while KillSwitch active: false.
- Loop ticks while scope OFF: false.
- Loop ticks while pilot toggle OFF: false.
- Loop changes any capability flag default: false.

## Next Phase

C-PHASE 150 is Auto-continue=No. Halt for user review after opening the PR. The next work should be planned only after this review gate is cleared.
