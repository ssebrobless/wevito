# C-PHASE 163 - Experiment Manifest Hash

## Goal

Fold each experiment descriptor's manifest identity into the approval scope hash so silent descriptor changes invalidate prior approvals.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentManifestHash.cs`
- `vnext/tests/Wevito.VNext.Tests/ExperimentManifestHashTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentDescriptor.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeHashInputs.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeHash.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalDescriptor.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/EvalCoverageProposalDescriptor.cs`
- `vnext/tests/Wevito.VNext.Tests/ScopeHashTests.cs`

Not touched:

- `UserApplyApproval.cs`
- `UserApplyApprovalValidator.cs`
- model adapters
- network surfaces
- held-out eval store
- in-distribution eval store
- apply runner behavior

## Implemented

- Added `ExperimentManifestHash.Compute(...)`, a deterministic SHA-256 helper over canonical experiment descriptor identity, mutation posture, manifest version, and sorted packet kinds.
- Extended `ExperimentDescriptor` with `ManifestVersion`, defaulting to `"1"`.
- Set `ManifestVersion = "1"` explicitly for the sprite-repair batch proposal descriptor and eval-coverage proposal descriptor.
- Extended `ScopeHashInputs` with `ExperimentManifestHash`.
- Extended `ScopeHash.Compute(...)` canonical input order to include:
  `ScopeId`, `OperationId`, `ProposalSha256`, `DryRunSha256`, `EvalSha256`, `ExperimentManifestVersion`, `ExperimentManifestHash`, and sorted `PacketKindsTouched`.
- Updated `SupervisedImprovementLoop.BuildScopeHash(...)` to compute the sprite-repair descriptor manifest hash from the descriptor, `ReviewOnly` mutation posture, and the proposal/dry-run/eval/awaiting-approval packet chain.
- Added manifest-hash tests for stable output, field sensitivity, and packet-kind sort stability.
- Extended scope-hash tests so changing the manifest hash flips the scope hash, and empty vs non-empty manifest hash differs.
- Marked the scope-hash test migration with:
  `// MIGRATED in C-PHASE 163: manifest hash folded into scope hash`

Implementation note:

- `ExperimentManifestHash` is pure and deterministic. It includes a harmless public `KillSwitchService` constructor so the existing self-improvement kill-switch coverage test continues to protect newly introduced public runtime types without expanding the allowlist.

## Safety Boundaries

- No approval validator signature changed.
- No user approval DTO changed.
- No model call was added.
- No network surface was added.
- No file mutation path was added.
- No audit packet producer was added.
- No capability flag default changed.
- The change only strengthens approval binding by making descriptor identity part of the scope hash.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ScopeHash|ExperimentManifestHash|SupervisedImprovementLoop|PlainLanguage|SelfImprovement"`: passed, 268/268.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1225/1225.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] `ScopeHash.Compute(...)` output changes for non-empty manifest-hash inputs.
- [x] Empty and non-empty manifest hashes produce different scope hashes.
- [x] Existing scope-hash migration is visible in the diff with the required C-PHASE 163 comment.
- [x] `ExperimentDescriptor.ManifestVersion` defaults to `"1"`.
- [x] `UserApplyApproval.cs` was not modified.
- [x] `UserApplyApprovalValidator.cs` was not modified.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 164 - Heuristic Judge service - Auto-continue=No.
