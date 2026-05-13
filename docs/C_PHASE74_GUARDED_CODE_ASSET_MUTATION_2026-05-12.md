# C-PHASE 74 Guarded Code & Asset Mutation

## Goal

Add a generalized mutation gate so Wevito can eventually make reversible code or
asset edits only through an explicit, auditable flow:

```text
dry-run -> backup -> apply -> post-proof -> rollback-on-failure
```

## Scope

- Added `GuardedMutationPlan` records and manifests.
- Added `GuardedMutationService` with dry-run/apply/rollback-on-proof-failure.
- Added `MutationVerifier` to choose proof commands by touched file type.
- Added `GuardedMutationPreviewAdapter` for PET TASKS preview-only packets.
- Registered `guardedMutation` in the preview dispatcher and tool definitions.
- Routed sprite post-apply proof command selection through `MutationVerifier`.

## Safety Boundaries

- Preview adapter does not mutate files.
- Apply requires an approved `TaskCard`.
- Runtime supervisor must be `Active`.
- KillSwitch blocks.
- Target paths must stay inside approved `UnifiedPolicyService` roots.
- Backups are written under `vnext/artifacts/mutation-backups/<timestamp-scope>/`.
- Post-proof failure rolls back and verifies byte-exact sha256 restoration.
- No hosted AI, web fetches, model calls, or asset generation were added.

## Proof Command Mapping

- `.cs`, `.csproj`, `.sln`, `.xaml` -> `dotnet build .\vnext\Wevito.VNext.sln`
- files under `vnext/tests` -> `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- sprite paths -> `python .\tools\audit_sprite_contract.py`
- runtime PNG paths -> `python .\tools\report_runtime_canvas_mismatches.py --fail-on-mismatch`

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "GuardedMutation|MutationVerifier|SpriteWorkflow"` passed: 27 / 27.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 392 / 392.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Residual Notes

The generic mutation service is ready for guarded text/binary replacement plans.
PET TASKS exposes only preview packets in this phase. Future C-PHASE 75 must
continue to avoid direct mutation apply from autonomous loops.

## Next Phase

C-PHASE 75 can build the autonomous operations beta on top of this, but must
never apply mutations directly. It should only propose and preview mutation work
until the user explicitly approves a guarded apply path.
