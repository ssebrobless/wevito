# C-PHASE 30 Release Blocker

Date: 2026-05-12
Branch: `claude-implementation/c-phase-30-release`

## Summary

C-PHASE 30 reached a release-packaging blocker before the final tag step. Release build and Release tests passed, but the required no-`SkipAssetPrep` vNext publish did not complete safely.

## What Passed

| Check | Result |
| --- | --- |
| `dotnet build .\vnext\Wevito.VNext.sln -c Release` | Pass |
| `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj -c Release --no-build` | Pass, 278/278 |

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

## Why This Is A Stop Condition

The no-`SkipAssetPrep` release path is not currently safe as a release gate because it can regenerate tracked runtime art from `incoming_sprites`, creating broad sprite mutations instead of validating the existing cleaned runtime tree.

This conflicts with the project rule that visual/runtime asset mutation should be provenance-backed, reviewable, and intentionally approved.

## Recommended Next Step

Before tagging a release, choose one of these paths:

1. Release from the already validated runtime assets by making `build-vnext.ps1 -Configuration Release -SkipAssetPrep` the release packaging path.
2. Refactor asset prep into a validation-first mode that generates into a temporary staging directory, reports diffs, and only applies with explicit approval.
3. Re-run no-`SkipAssetPrep` with a longer timeout only if the user explicitly accepts that it may rewrite broad runtime sprite content.

Recommended default: option 1 for the first release candidate, then option 2 as a follow-up hardening phase.

## Not Done Yet

- `tools\build-release.ps1` was not run after this blocker.
- `docs\WEVITO_USER_HELP_GUIDE_2026-05-05.md` was not created yet.
- No release tag was created.
- No live model call was made.
