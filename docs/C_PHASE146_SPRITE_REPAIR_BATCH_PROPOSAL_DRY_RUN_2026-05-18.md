# C-PHASE 146 - Sprite-Repair Batch Proposal Dry-Run Scope

Date: 2026-05-18
Branch: `claude-implementation/c-phase-146-sprite-repair-batch-proposal-dry-run`
Base: `968d4b994c8384d33aeb3dfeb50b4d59c33fceb3`

## Goal

Register the first supervised self-improvement experiment kind and add a default-off, review-only autonomous scope that drafts sprite repair batch proposal evidence without mutating sprite art.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalDescriptor.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalScope.cs`
- `vnext/tests/Wevito.VNext.Tests/SpriteRepairBatchProposalScopeTests.cs`
- `docs/C_PHASE146_SPRITE_REPAIR_BATCH_PROPOSAL_DRY_RUN_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Core/AutonomousScopeService.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/ConstitutionalDecisionServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ExperimentRegistryTests.cs`

Not touched:

- No PNG mutation.
- No sprite manifest mutation.
- No candidate generation.
- No apply or rollback pathway.
- No local model call.
- No hosted AI or network surface.
- No capability flag default was flipped on.

## Implemented

- Added `sprite-repair-batch-proposal` as the first registered experiment kind through `ShellCompositionRoot.CreateExperimentRegistry()`.
- Added `SpriteRepairBatchProposalScope` as a separate autonomous scope from the older `sprite-repair-triage` scope.
- Added the new scope descriptor to `AutonomousScopeService.KnownScopes` with `CanMutate=false`.
- Added shell construction for the scope using the existing C-PHASE 128 repair queue path.
- Added Tool Popup autonomy-tab controls for enabling and previewing the new scope, with visible wording: "Review only. No sprite mutation. No apply."
- One eligible tick drafts a review-only TaskCard and writes exactly these four packets in order:
  1. `self_improvement_proposal_drafted`
  2. `self_improvement_constitutional_reviewed`
  3. `self_improvement_dry_run_completed`
  4. `self_improvement_eval_completed`
- All four packets set `did_mutate=false`, `did_use_network=false`, `did_use_hosted_ai=false`, and `did_use_local_model=false`.
- The eval artifact uses the C-PHASE 147 gate manifest and marks apply-only gates (`Backup`, `Post-proof`, `Rollback`) as `NotApplicable("review_only_v0")`.
- Tests assert source sprite sha256 stays identical before and after the tick.

## Safety Boundaries

- Scope defaults off and requires the existing autonomous beta + per-scope enabled gates.
- The scope creates proposal/dry-run/eval artifacts only.
- No `self_improvement_apply_*` packet is written.
- No sprite runtime/source PNG file is written.
- No local model is called by default.
- All packet kinds come from `SelfImprovementPacketKinds`.
- Constitutional review is recorded, but the default-deny constitutional layer still blocks apply-like trust escalation.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRepairBatchProposal|ExperimentRegistry|ConstitutionalDecision|EvalGate|PlainLanguage|SelfImprovement"`: 261/261 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1097/1097 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Tick does not write any `self_improvement_apply_*` packet.
- [x] Sha256 of the source sprite file tested remains unchanged after a tick.
- [x] Scope toggle defaults off.
- [x] Scope writes only packet kinds registered in C-PHASE 143.
- [x] No PNG mutation path was added.
- [x] No sprite manifest mutation path was added.
- [x] No local model call was added.
- [x] Validation commands passed.

## Next Phase

C-PHASE 148 is `Explicit user-apply-approval type for apply paths`. Auto-continue is No, so C-PHASE 148 should not start until the user explicitly approves and provides the C-PHASE 148 prompt.
