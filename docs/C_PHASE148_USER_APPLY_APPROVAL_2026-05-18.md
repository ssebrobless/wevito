# C-PHASE 148 - User Apply Approval

## Goal

Introduce the typed `UserApplyApproval` contract for future supervised apply paths without enabling any apply path in this phase.

## Scope

Added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApproval.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/IRequiresUserApplyApproval.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApprovalValidator.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApprovalResult.cs`
- `vnext/tests/Wevito.VNext.Tests/UserApplyApprovalValidatorTests.cs`

Not touched:

- Autonomous loop execution wiring.
- Scheduler behavior.
- Model adapters.
- Sprite/runtime assets.
- Capability flags.
- Apply runners.

## Implemented

- Added `UserApplyApproval`, a sealed record carrying the user-confirmed-in-this-message bit, confirmation text, UTC confirmation time, approved scope id, and approved operation id.
- Added `ApprovalResult.Accepted` and `ApprovalResult.Refused(reason)` so refusals are normal control flow and do not throw.
- Added `IRequiresUserApplyApproval` as the explicit marker/contract for future apply paths.
- Added `UserApplyApprovalValidator` with the required refusal reasons:
  - `not_confirmed_in_this_message`
  - `empty_confirmation_text`
  - `stale_confirmation`
  - `scope_id_mismatch`
  - `operation_id_mismatch`
- Added tests for all accept/refuse cases, no bypass overloads, no throwing refusal path, no production construction outside the approval contract files, and no public approval signatures in autonomous/scheduler/scope/model-adapter-style namespaces.

## Safety Boundaries

- No producer emits self-improvement apply packets in this phase.
- No apply path was moved to require or accept this type yet; C-PHASE 150 owns wiring.
- No model call, hosted AI call, network call, training, sprite mutation, or file mutation pathway was added.
- The validator has one public method and returns `ApprovalResult` for refusal cases.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "UserApplyApproval|PlainLanguage|SelfImprovement"`: passed, 249/249.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1107/1107.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] Validator exposes no bypass overload.
- [x] No non-user-driven namespace constructs `UserApplyApproval`.
- [x] Refusal pathway returns `ApprovalResult`; it does not throw.
- [x] No apply path enabled.
- [x] No capability flag changed.

## Next Phase

C-PHASE 149 - Maturity clock scoreboard. Auto-continue is No; wait for user approval before starting.
