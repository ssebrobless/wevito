# C-PHASE 186: Mutation Sandbox Hardening

## Goal

Add two narrow defenses around the first self-improvement apply/rollback prototype:

- A runtime path guard that throws before artifact rename operations can escape the configured artifact root.
- A static test surface that pins the C-PHASE 186 mutation allow-list and verifies safety-critical mutators are discovered from IL rather than hidden in prose.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Sandbox/MutationScopeGuard.cs`
- `vnext/tests/Wevito.VNext.Tests/MutationScopeGuardTests.cs`
- `vnext/tests/Wevito.VNext.Tests/MutationScopeAuditTests.cs`
- `vnext/tests/Wevito.VNext.Tests/Support/MutationAllowList.cs`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs`
- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`

Not touched:

- No model adapter.
- No network surface.
- No held-out eval store.
- No supervised loop apply wiring.
- No sprite, source-code, model-weight, or asset mutation behavior.

## Implemented

- Added `MutationScopeGuard.ThrowIfOutsideScope(string intendedPath, string scopeRoot, string scopeName)`.
- Guard resolves both the intended path and scope root with `Path.GetFullPath`.
- Guard allows paths under the configured root using Windows-safe case-insensitive comparison.
- Guard throws synchronously with the scope name and intended path when a path escapes the declared scope.
- `ArtifactRenameApplyRunner` now guards dry-run, backup, source, destination, restore, and backup-copy paths before file writes/copies/moves.
- `ArtifactRenameRollbackRunner` now guards dry-run, source, and destination paths before file writes/moves.
- Added `SelfImprovementPacketKinds.MutationScopeAuditEvent`.
- Registered the new packet kind in `PlainLanguageExplainer.KnownPacketKinds` with a plain-language sentence.
- Added `mutation_scope_audit_emit_enabled` to `CapabilityFlagInventory` with `DefaultValue=false`.
- Added `MutationScopeGuard` to held-out visibility tests so it cannot grow held-out eval dependencies.
- Added static tests for the exact eight-name mutation allow-list, guard no-swallow behavior, known mutator IL discovery, packet/default-off registration, and guarded runner source calls.

## Safety Boundaries

- The guard does not write audit packets by default.
- The new `mutation_scope_audit_emit_enabled` flag defaults `False`.
- No new producer emits `self_improvement_mutation_scope_audit_event`.
- No new capability flag is enabled.
- No hosted AI, model call, network call, training, or apply-loop integration was added.
- No SQL `UPDATE` or `DELETE` path was added.
- The supervised improvement loop still refuses with `apply_runner_not_implemented_in_v0`.

## Validation

- Pre-flight `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- Pre-flight `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1484/1484.
- Pre-flight `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- Pre-flight `git diff --check`: passed.
- Focused validation `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "MutationScope|ArtifactRename|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"`: passed, 376/376.
- Post-change `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- Post-change `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1502/1502.
- Post-change `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Stop-Gate Checklist

- [x] MutationAllowList contains only the listed eight names.
- [x] Runtime guard has no try/catch that hides scope violations.
- [x] Static mutation tests derive known mutating callers from IL.
- [x] New packet kind is not emitted by default.
- [x] New capability flag defaults `False`.
- [x] Known mutators including `AuditLedgerService` are discovered by the IL scan.
- [x] Pre-flight commands passed.

## Next Phase

C-PHASE 187 — Expanded but artifact-only rename scope — Auto-continue=No.
