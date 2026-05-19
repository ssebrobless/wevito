# C-PHASE 174 - Apply-runner prerequisite checklist service

## Goal

Add a default-off, read-only prerequisite checklist service for any future apply runner. This phase does not add an apply runner and does not make any model, network, training, or mutation path available.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerPrerequisiteCheckResult.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerPrerequisiteCheckService.cs`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerPrerequisiteCheckServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerPrerequisiteHeldOutAccessTests.cs`

Extended:

- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
- `vnext/tests/Wevito.VNext.Tests/InDistributionVersusHeldOutTypeSeparationTests.cs`
- `vnext/tests/Wevito.VNext.Tests/Support/KillSwitchCoverageAllowList.cs`

Not touched:

- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`
- apply runner implementation paths
- model, network, training, and mutation surfaces

## Implemented

- Added `self_improvement_apply_prerequisite_check`.
- Added plain-language explainer support for the new packet kind.
- Added `apply_runner_prerequisite_check_enabled=false` by default.
- Added service-level snapshot and replay recency-window settings, both defaulting to `7`.
- Added `ApplyRunnerPrerequisiteCheckService.Check(...)`.
- Disabled flag returns one synthetic failed entry and writes no packet.
- KillSwitch returns failed entries and writes no packet.
- Enabled path evaluates ten prerequisites and writes one audit packet only.
- Packet evidence flags are always `DidUseNetwork=false`, `DidUseHostedAi=false`, `DidUseLocalModel=false`, `DidMutate=false`.

## Ten Prerequisites

1. KillSwitch is callable and not active.
2. `eval_gate_runner_v1_enabled=true`.
3. Latest heuristic judge packet for the operation has `rules_passed == rules_evaluated`.
4. Recent signed snapshot verifies.
5. Held-out store has at least one case id.
6. In-distribution store has at least one case id.
7. Latest awaiting-approval artifact scope hash recomputes correctly.
8. Recent replay result is `Identical`.
9. Runtime capability settings have no unexpected enabled default-off flags.
10. Apply runner remains declared not implemented in v0.

## Safety Boundaries

- The service has no `Apply`, `Execute`, or `Run` method.
- The service does not call any apply runner.
- The service reads only `IHeldOutEvalStore.ListCaseIds()` and never reads held-out case contents.
- The service writes only `self_improvement_apply_prerequisite_check` packets.
- The service writes no packet when disabled or when KillSwitch is active.
- No capability flag defaults true.
- No hosted AI, network, local model, training, or mutation behavior was added.

## Validation

| Command | Result |
| --- | --- |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerPrerequisite|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"` | Passed: 282/282 |
| `dotnet build .\vnext\Wevito.VNext.sln` | Passed |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` | Passed: 1331/1331 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` | Passed |
| `git diff --check` | Passed |

## Stop-Gate Checklist

- [x] Service reads no held-out content beyond `ListCaseIds()`.
- [x] Service and new types do not implement or call an apply runner.
- [x] `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` remains `apply_runner_not_implemented_in_v0`.
- [x] `apply_runner_prerequisite_check_enabled` defaults `False`.
- [x] New packet kind is in `PlainLanguageExplainer.KnownPacketKinds`.
- [x] No hosted AI, network, local model, training, mutation, or apply behavior added.

## Next Phase

This phase completes the C-PHASE 167-174 batch. After this PR is merged, request the next plan before drafting further phases.
