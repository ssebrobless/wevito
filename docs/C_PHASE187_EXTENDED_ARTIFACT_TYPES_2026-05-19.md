# C-PHASE 187: Extended Artifact-Only Rename Scope

## Goal

Extend the narrow v0 artifact rename apply/rollback runners beyond JSON while keeping the scope artifact-only, default-off, and bounded to safe document-style artifacts.

Allowed pairs:

- `*.draft.json` -> `*.approved.json`
- `*.draft.txt` -> `*.approved.txt`
- `*.draft.md` -> `*.approved.md`
- `*.draft.svg` -> `*.approved.svg`

## Scope

Files extended:

- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs`

Files not touched:

- No packet-kind file change.
- No UI surface change.
- No mutation allow-list change.
- No `MutationScopeGuard` change.
- No model, network, held-out eval, sprite, source-code, or asset mutation surface.

## Implemented

- Added `ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting` with name `apply_runner_v0_extended_artifact_types_enabled`.
- Registered the new flag in `CapabilityFlagInventory` with `DefaultValue=false`.
- Updated apply relative-path validation to allow `draft.(json|txt|md|svg)`.
- Updated rollback relative-path validation to allow `approved.(json|txt|md|svg)`.
- Preserved JSON as the base accepted artifact type without needing the new flag.
- Gated `.txt`, `.md`, and `.svg` behind `apply_runner_v0_extended_artifact_types_enabled=True`.
- Added unconditional forbidden-extension refusal before regex handling for:
  `.cs`, `.csproj`, `.xaml`, `.config`, `.exe`, `.dll`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.ico`, `.wav`, `.ogg`, `.ttf`, `.otf`, `.yaml`, `.yml`, `.toml`, `.ini`, `.bat`, `.ps1`, `.sh`, `.py`, `.js`, `.ts`, `.java`, `.kt`, `.swift`, `.rs`, `.go`.
- Added apply tests for non-JSON flag refusal, `.txt/.md/.svg` happy paths, forbidden extension refusal, unsupported extension refusal, and the default-false flag.
- Added rollback tests for the same extension/refusal matrix.

## Safety Boundaries

- No new audit packet kind was added.
- No new runner was added.
- No UI control was added.
- No MutationAllowList entry was added.
- The C-PHASE 186 `MutationScopeGuard` remains unchanged and still protects apply/rollback paths.
- The supervised improvement loop still refuses with `apply_runner_not_implemented_in_v0`.
- The new capability flag defaults `False`.
- The runner refuses every listed source-code, binary, media, script, config, and asset extension even when the new flag is `True`.

## Validation

- Pre-flight `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- Pre-flight `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1502/1502.
- Pre-flight `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- Pre-flight `git diff --check`: passed.
- Focused validation `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameApply|ArtifactRenameRollback|MutationScope|PlainLanguage|SelfImprovement"`: passed, 447/447.
- Post-change `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- Post-change `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1576/1576.
- Post-change `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Stop-Gate Checklist

- [x] Runner accepts only `json`, `txt`, `md`, and `svg`.
- [x] Runner refuses non-JSON extensions when `apply_runner_v0_extended_artifact_types_enabled` is absent or false.
- [x] New flag defaults `False`.
- [x] Forbidden extension list covers the required 30 extensions.
- [x] No MutationAllowList entry was added.
- [x] Pre-flight gate passed.

## Next Phase

C-PHASE 188 — Post-C-PHASE-183 apply-runner product-truth retest — Auto-continue=No.
