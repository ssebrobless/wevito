# C-PHASE 84: Guarded Code Mutation Pilot

## Goal
Prove the first reviewed guarded-code-mutation loop without giving Wevito broad code-writing power.

## Chosen Pilot Scenario
The pilot uses a synthetic temp fixture only:

- Target pattern: `vnext/content/guarded-mutation-pilot/example.txt`
- Initial content: `before`
- Proposed content: `after`
- Scope: one text file under the pilot-only approved root
- Surface: Draft `guardedMutation` task card and dry-run packet

No real repo source file is mutated by the pilot tests.

## Implemented
- `GuardedMutationPreviewAdapter.Propose(...)` creates a default-off pilot proposal with:
  - `GuardedMutationPlan`
  - Draft `TaskCard`
  - preview artifact packet
  - strict target root: `vnext/content/guarded-mutation-pilot/`
- `GuardedMutationService.DryRun(...)` now writes `mutation-diff.md` beside `mutation-dry-run.json`.
- `tool_definitions.json` exposes `guardedMutation.pilotEnabled=false`.
- PET TASKS shows a disabled power-user mutation pilot mini-form.
- Added `GuardedMutationPilotTests` for dry-run, apply, rollback, scope refusal, and KillSwitch blocking.

## Safety Boundaries
- Pilot UI is disabled by default.
- The proposal path is root-limited.
- Apply still requires an Approved task card and Active runtime supervisor.
- KillSwitch blocks apply.
- Failed post-proof rolls back byte-exactly.
- No hosted AI, no network, no model calls, no sprite mutation, and no broad code generation.

## Validation
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "GuardedMutation|MutationVerifier|SpriteWorkflow"` passed: 31/31.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 505/505.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Stop Gate
Auto-continue is **NO**. This is the first reviewed code-apply surface outside sprites, so the next phase should wait for review.
