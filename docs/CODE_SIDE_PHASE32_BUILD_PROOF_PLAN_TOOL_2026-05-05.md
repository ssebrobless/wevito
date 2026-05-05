# Code-Side Phase 32 Build Proof Plan Tool - 2026-05-05

## Goal

Add an approval-gated PET TASKS build/test proof planner.

The `buildProof` adapter writes the exact command sequence, expected artifacts, safety gates, and stop conditions. It does not run build commands.

```text
PET TASKS: "run a build proof"
   |
   v
WaitingForApproval task card
   |
   v
user approves
   |
   v
buildProof plan adapter
   |
   v
build-proof-plan-report.json + run-summary.md
```

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\BuildProofPreviewAdapter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-pet-tasks.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\BuildProofPreviewAdapterTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetAgentContractTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetTaskAdapterPreviewDispatcherTests.cs`

## What Changed

- Added build proof plan contracts:
  - `BuildProofCommandPlan`
  - `BuildProofPlanReport`
- Added `BuildProofPreviewAdapter`.
- Routed `buildProof` through the PET TASKS dispatcher.
- Kept `buildProof` as approval-required at task-card level.
- Extended the live PET TASKS probe with `-ApproveBeforePreview` for approval-gated task cards.
- Added `buildProof plans` to the PET TASKS capability line while keeping `build/proof execution` locked.

## Command Plan Produced

- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild`

## Safety Behavior

- Adapter run mode is dry-run preview only.
- Adapter does not execute commands.
- Adapter does not edit files.
- Adapter does not mutate assets.
- Build/proof command execution remains locked and requires a separate explicit approval/execution path.
- `-SkipAssetPrep` is mandatory for vNext publish while visual cleanup is active.

## Validation

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~BuildProofPreviewAdapterTests|FullyQualifiedName~PetTaskAdapterPreviewDispatcherTests|FullyQualifiedName~PetAgentContractTests|FullyQualifiedName~PetCommandParserTests"
```

Result: passed `33 / 33`.

```text
dotnet build .\vnext\Wevito.VNext.sln
```

Result: passed, `0` warnings, `0` errors.

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Result: passed `120 / 120`.

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Result: passed.

Live approval-aware PET TASKS probe:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "run a build proof" -ExpectedToolFamily buildProof -ApproveBeforePreview -SkipBuild
```

Result: passed.

Latest probe summary:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-134547-188-21e32969\summary.json
```

Latest build proof plan report:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-174601-buildproof-buildproof\run-summary.md
```

Latest live report summary:

- Commands planned: `4`
- Commands run: `false`
- Did mutate files: `false`
- Target sprite row hashes unchanged: `true`

## Audit

Pass.

- Correctness: build proof tasks now create a concrete command plan after approval.
- Safety: the adapter cannot run builds and keeps execution locked.
- Coordination: safe publish plan preserves `-SkipAssetPrep`.
- UX: user can see buildProof as a planning capability without confusing it with unlocked execution.

## Next Phase Recommendation

Proceed to Phase 33: proof execution design only.

Do not implement command execution yet unless explicitly approved. The next design should specify how execution approval, command allowlists, logging, cancellation, and rollback will work.
