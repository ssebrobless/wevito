# C-PHASE 152 Scope-Hash Binding

## Goal

Bind every supervised self-improvement apply-approval card to the exact proposal, dry-run, eval, manifest version, operation id, scope id, and packet-kind set that produced it.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeHash.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeHashInputs.cs`
- `vnext/tests/Wevito.VNext.Tests/ScopeHashTests.cs`

Extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApproval.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApprovalValidator.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/IRequiresUserApplyApproval.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/UserApplyApprovalValidatorTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopTests.cs`

Not touched:

- No sprite assets.
- No model adapters.
- No network surfaces.
- No mutation runner.
- No capability flag defaults.

`IRequiresUserApplyApproval.cs` was updated as a necessary companion signature edit because `UserApplyApprovalValidator` implements that interface and C-PHASE 152 requires the validator method to accept `expectedScopeHash`.

## Implemented

- Added canonical `ScopeHash.Compute(...)`.
- Canonical input uses SHA-256 over UTF-8 JSON with `WriteIndented=false`, no naming policy, and ordinal-sorted `PacketKindsTouched`.
- Added `ApprovedScopeHash` to `UserApplyApproval`.
- Validator refuses missing or mismatched hash with `scope_hash_mismatch`.
- Supervised approval cards now include `scope_hash` in the review payload, timeline, and `apply-awaiting-approval.json` artifact.
- Tool Popup now displays the bound scope hash prominently on the approval card.
- ShellCoordinator passes the expected hash from the selected card payload into the approval handler.
- Tests cover stable hash output, field sensitivity, packet-kind sort stability, hash mismatch refusal, and approval artifact/payload hash persistence.

## Safety Boundaries

- No producer writes any new packet kind.
- No apply runner was added.
- No sprite, code, or asset mutation pathway was enabled.
- No model call, hosted-AI call, or network path was added.
- The v0 apply approval path still refuses with `apply_runner_not_implemented_in_v0` after validation succeeds.
- `UserApplyApproval` construction remains limited to the Tool Popup production surface.

## Validation

- `git status` before implementation: clean on `claude-implementation/c-phase-152-scope-hash-binding`.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ScopeHash|UserApplyApproval|SupervisedImprovementLoop|PlainLanguage|SelfImprovement"`: passed, 266/266.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed, 0 warnings, 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1144/1144.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No hash function other than SHA-256 was introduced for scope binding.
- [x] Production `UserApplyApproval` construction includes `ApprovedScopeHash`.
- [x] Validator refuses empty or mismatched scope hash.
- [x] Non-Tool-Popup production namespaces do not construct `UserApplyApproval`.
- [x] Pre-flight and final validation commands passed.

## Next Phase

C-PHASE 153: KillSwitch coverage assertion. Auto-continue is No.
