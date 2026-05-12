# C-PHASE 30 Release Blocker

Date: 2026-05-12
Branch: `claude-implementation/c-phase-30-release`

## Summary

C-PHASE 30 reached a release-packaging blocker before the final tag step. Release build, Release tests, and the safe vNext Release publish passed, but the Godot desktop export path still does not complete in a release-safe window.

## What Passed

| Check | Result |
| --- | --- |
| `dotnet build .\vnext\Wevito.VNext.sln -c Release` | Pass |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj -c Release --no-build` | Pass, 278/278 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Release -SkipAssetPrep` | Pass, includes Release tests and publish |

## Blocker

The C-PHASE 30 prompt requires:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Release
```

That path runs asset preparation without `-SkipAssetPrep`. During the run:

1. Shared sprite cleanup completed.
2. Stage prop extraction completed.
3. Runtime pet sprite generation started through `tools/generate_runtime_pose_sprites.py`.
4. The generator continued running for more than ten minutes of CPU time and had to be stopped.
5. Before being stopped, it rewrote a large number of `sprites_runtime/**` PNGs and modified generated shared-runtime summary files.

Those generated changes were rolled back because committing a partial broad runtime PNG rewrite would risk overwriting the visual cleanup lane.

After the user approved the safe path, the vNext Release publish was rerun with `-SkipAssetPrep` and passed.

The next packaging issue was `tools\build-release.ps1`, which drives a Godot Windows export. Fixes/hardening attempted in this branch:

1. Capture Godot stderr/stdout without treating warning text as an immediate PowerShell `NativeCommandError`.
2. Add an explicit `-ExportTimeoutSeconds` guard so Godot export timeouts kill the process tree cleanly.
3. Narrow the Godot export preset from broad resource export to the main scene plus dynamic runtime asset includes.
4. Add `.gdignore` markers to hide non-game and legacy source trees from Godot's project scanner while keeping `vnext/content` visible.

The scanner improved from roughly 122,046 scan steps to roughly 11,379 scan steps, and duplicate UID warnings disappeared after hiding duplicate legacy sprite folders. However, export still timed out during Godot reimport/export work.

## Why This Is A Stop Condition

The no-`SkipAssetPrep` release path is not currently safe as a release gate because it can regenerate tracked runtime art from `incoming_sprites`, creating broad sprite mutations instead of validating the existing cleaned runtime tree.

This conflicts with the project rule that visual/runtime asset mutation should be provenance-backed, reviewable, and intentionally approved.

The Godot desktop export path is also not currently reliable as a C-PHASE 30 release gate because the project still asks Godot to import/export thousands of individual runtime PNG resources. Even after reducing the scan set, Godot timed out during export and did not produce `WevitoDesktopPet-vcphase30-rc6-win64.exe`.

## Recommended Next Step

Before tagging a release, choose one of these paths:

1. Tag/package the vNext desktop helper release from the already validated runtime assets using `build-vnext.ps1 -Configuration Release -SkipAssetPrep`, and defer Godot desktop export.
2. Refactor asset prep into a validation-first mode that generates into a temporary staging directory, reports diffs, and only applies with explicit approval.
3. Refactor the Godot release package to reduce individual PNG import pressure, likely by packaging runtime sprites outside Godot's import pipeline or by moving to atlas/pack files.
4. Re-run no-`SkipAssetPrep` or long Godot export only if the user explicitly accepts the runtime and packaging risk.

Decision taken: option 1 for the first release candidate. Tag `v0.1.0-vnext-rc1` as a vNext-only release candidate, then handle options 2 and 3 as follow-up hardening phases.

## Not Done Yet

- `tools\build-release.ps1` was run after hardening, but Godot export timed out.
- `docs\WEVITO_USER_HELP_GUIDE_2026-05-05.md` was not created yet.
- Release tag decision recorded in `docs/C_PHASE30_VNEXT_RC_RELEASE_DECISION_2026-05-12.md`; tag creation happens after this branch merges to `main`.
- No live model call was made.
