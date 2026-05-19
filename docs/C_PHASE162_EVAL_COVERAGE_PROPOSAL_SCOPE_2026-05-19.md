# C-PHASE 162 - Eval Coverage Proposal Scope

## Goal

Add a second review-only self-improvement autonomous scope, `eval-coverage-proposal`, so the supervised pipeline is not shaped around a single sprite-repair producer.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/EvalCoverageProposalDescriptor.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/EvalCoverageProposalScope.cs`
- `vnext/tests/Wevito.VNext.Tests/EvalCoverageProposalScopeTests.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/AutonomousScopeService.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`

Not touched:

- `SpriteRepairBatchProposalScope.cs`
- `SupervisedImprovementLoop.cs`
- held-out eval store
- in-distribution eval store
- packet taxonomy
- model adapters

## Implemented

- Added `EvalCoverageProposalDescriptor` with kind `eval-coverage-proposal`, mutation posture `ReviewOnly`, and default-off descriptor metadata.
- Added `EvalCoverageProposalScope`, an `IAutonomousScope` implementation.
- The scope blocks with no writes when the kill switch is active.
- The scope blocks with no writes when its autonomous scope flag is false or absent.
- The scope blocks with no writes when the audit ledger database is missing.
- When enabled, it opens the audit ledger with SQLite read-only mode and reads `self_improvement_eval_completed` rows with `Status=Passed` from the last 30 days.
- It parses each eval artifact's `results` object and unions gates with `status=Passed`.
- It computes coverage gaps from `EvalGateManifest.Default().Gates`.
- If no gaps remain, it returns successfully with summary `no eval coverage gaps` and writes nothing.
- If gaps exist, it writes `proposal.json`, `dry-run.json`, and `eval.json` under a new timestamped request folder in the provided artifact root.
- It emits the review-only packet sequence:
  `self_improvement_proposal_drafted`,
  `self_improvement_constitutional_reviewed`,
  `self_improvement_dry_run_completed`,
  `self_improvement_eval_completed`.
- Every emitted packet sets `DidUseNetwork=false`, `DidUseHostedAi=false`, `DidUseLocalModel=false`, and `DidMutate=false`.
- Added `EvalCoverageProposalScopeId` to `AutonomousScopeService.KnownScopes`.
- Added the default-off capability flag inventory row:
  `autonomous_scope_eval-coverage-proposal_enabled=false`.
- Added a composition-root factory and shell registration so the live autonomous scope registry can see the new scope.

Implementation note:

- `ShellCoordinator.cs` was extended with the one-line registration required to satisfy the phase's "register with the autonomous operations loop" requirement. The original allowed-file list omitted this file, but without this line the scope would be known in metadata yet unavailable to the live scope registry.

## Safety Boundaries

- No apply packet is written.
- No new packet kind is introduced.
- No model call is added.
- No network surface is added.
- No held-out eval store is referenced.
- No in-distribution eval store is referenced.
- No sprite or source asset mutation is possible from this scope.
- The scope source contains no `INSERT`, `UPDATE`, or `DELETE`; all audit ledger reads use read-only SQLite connections.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalCoverageProposal|EvalGate|PlainLanguage|SelfImprovement"`: passed, 266/266.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1220/1220.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Scope does not write `self_improvement_apply_completed`.
- [x] Scope does not write `self_improvement_apply_awaiting_approval`.
- [x] Scope does not write a new packet kind.
- [x] Scope does not reference `IHeldOutEvalStore`.
- [x] Scope does not reference `IInDistributionEvalStore`.
- [x] Scope flag defaults to `false`.
- [x] Scope source contains no `INSERT`, `UPDATE`, or `DELETE`.

## Next Phase

C-PHASE 163 - experiment manifest hash - Auto-continue=No.
