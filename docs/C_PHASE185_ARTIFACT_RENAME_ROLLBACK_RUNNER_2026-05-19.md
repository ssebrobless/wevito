# C-PHASE 185 - Artifact Rename Rollback Runner

Date: 2026-05-20
Branch: `claude-implementation/c-phase-185-artifact-rename-rollback-runner`
Base: `9f87d9fa34bbbc2b3b4dfcdb093390428047a0f7`

## Goal

Add the symmetric, narrow rollback path for the v0 artifact rename apply runner:

- `ArtifactRenameRollbackRunner` renames exactly one artifact from `*.approved.json` back to `*.draft.json`.
- The operation is confined to the configured artifact root under `<operation-id>/<scope-id>/`.
- The operation remains default-off and requires explicit rollback flags plus the existing apply-runner design/implementation approvals.
- The Tool Popup gains a read-only "Rollback evidence" expander showing rollback-related apply-v0 activity without any mutating control.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/RollbackRequest.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/RollbackResult.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerHeldOutAccessTests.cs`

Extended:

- `SelfImprovementPacketKinds` with the three explicit rollback packet kinds.
- `PlainLanguageExplainer` with known-kind rows and user-readable explanations.
- `CapabilityFlagInventory` with two default-false rollback flags.
- `ShellCompositionRoot` with a rollback runner factory.
- `ApplyRunnerActivityService` so explicit rollback completion is displayed as rolled back.
- `ToolPopupWindow.xaml(.cs)` with a read-only rollback evidence expander.
- Held-out visibility and KillSwitch pure-data allow-list tests for the new DTO/result types.

Not touched:

- No source-code, sprite, runtime asset, content manifest, model, network, or training surface was expanded.
- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` remains unchanged.
- No new UI control can invoke explicit rollback.

## Implemented

- `RollbackRequest` mirrors the apply request while requiring `ApprovedRelativePath`.
- `RollbackResult` is a closed result union with `Refused` and `Succeeded`.
- `ArtifactRenameRollbackRunner.ExplicitRollback(...)` performs the narrow inverse rename after:
  - KillSwitch check.
  - `apply_runner_v0_explicit_rollback_enabled=true`.
  - `apply_runner_v0_explicit_rollback_design_approved=true`.
  - `apply_runner_design_approved=true`.
  - `apply_runner_implementation_phase_approved=true`.
  - Apply-runner prerequisite check passes.
  - Relative path matches `operation/scope/name.approved.json`.
  - Scope and operation path segments match the request.
  - Source exists, is not a reparse point, and destination does not already exist.
  - Latest awaiting-approval token and scope hash match.
- Successful rollback writes:
  - `self_improvement_apply_v0_explicit_rollback_started`
  - `self_improvement_apply_v0_explicit_rollback_completed`
- Refused rollback writes:
  - `self_improvement_apply_v0_explicit_rollback_refused`
- The rollback evidence expander is read-only and filters the same read-only activity service to rolled-back or explicit-rollback packet sequences.

## Safety Boundaries

- Rollback runner uses only artifact-root-confined paths.
- Source must end with `.approved.json`.
- Destination must end with `.draft.json`.
- Destination must not already exist.
- New capability flags default to `False`.
- New packet kinds are registered in `PlainLanguageExplainer.KnownPacketKinds`.
- Runner does not call `ArtifactRenameApplyRunner.Apply`.
- Runner does not reference held-out or in-distribution eval stores/cases.
- UI expander contains no Button, ToggleSwitch, CheckBox, or MenuItem and has no click path to mutation.
- No network, hosted AI, local model, training, source-code mutation, sprite mutation, or content mutation was added.

## Validation

Pre-flight on the base tip:

- `dotnet build .\vnext\Wevito.VNext.sln` - passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` - passed, 1454/1454.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` - passed.
- `git diff --check` - passed.

Implementation validation:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameRollback|ArtifactRenameApply|ApplyRunnerActivity|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"` - passed, 370/370.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameRollback|ArtifactRenameApply|ApplyRunnerActivity|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement|ApplyRunnerPrerequisite"` - passed, 384/384.
- `dotnet build .\vnext\Wevito.VNext.sln` - passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` - passed, 1484/1484.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` - passed.
- `git diff --check` - passed.

## Stop-Gate Checklist

- [x] Rollback runner refuses non-`.approved.json` source extension.
- [x] Rollback runner produces only `.draft.json` destination extension.
- [x] Rollback runner writes only under the artifact root.
- [x] Rollback runner does not call `ArtifactRenameApplyRunner.Apply`.
- [x] New rollback capability flags default `False`.
- [x] New rollback packet kinds are present in `KnownPacketKinds`.
- [x] Rollback evidence expander has no mutating controls.
- [x] Pre-flight gate passed.

## Next Phase

C-PHASE 186 - Mutation sandbox hardening. Auto-continue: No.
