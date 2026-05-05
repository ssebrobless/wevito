# Code-Side Cleanup Validation - 2026-05-05

## Scope

Validated the current code-side worktree after PET TASKS phases and visual-side sprite cleanup activity.
No runtime/source PNGs were reverted, deleted, normalized, regenerated, or imported during this pass.

## Worktree Cleanup

- Added top-level `artifacts/` to `.gitignore` so local recovery/proof scratch output no longer floods `git status`.
- Left `vnext/artifacts/` ignored as an artifact output area.
- Preserved existing visual-side sprite edits in `sprites_runtime/`.
- Preserved existing Godot/vNext source edits already present in the worktree.

The worktree is not expected to be clean yet. It still contains code-side phase files plus visual-side sprite/runtime changes that should be reviewed/staged intentionally, not discarded.

## Asset Validation

### Sprite Contract

Command:

```powershell
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\cleanup-validation\sprite-contract-post-probe-20260505.json
```

Result:

- Source boards: `30 / 30`
- Supporting inputs: `17 / 17`
- Runtime variant dirs: `360 / 360`
- Runtime frames: `10800 / 10800`
- Errors: `0`

### Runtime Canvas Reporter

Command:

```powershell
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\cleanup-validation\runtime-canvas-mismatches-post-probe-20260505.json
```

Result:

- Checked sequences: `2880`
- Checked frames: `10800`
- Mixed-canvas row mismatches: `0`
- Missing rows/files: `0`
- Invalid/non-alpha PNGs: `0`
- Canonical-size mismatches: `3852`

Canonical-size mismatches are informational under the current visual direction: sprites are no longer forced into a fixed authored box when natural motion needs wider/taller runtime frames.

## Build And Test Validation

Commands:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 120
```

Results:

- vNext solution build passed with `0` warnings and `0` errors.
- vNext tests passed `89 / 89`.
- Debug publish passed with `-SkipAssetPrep -SkipTests`.

## Runtime Probe Validation

### Action And Tool Probe

Command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -SkipBuild
```

Result:

- Summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260505-123214\summary.json`
- Doctor, medicine, bath, groom, feed, water, play, rest, and home action flows traced successfully.
- Settings compact/show names/status, basket paste/open/delete/open URL, and save traced successfully.

### PET TASKS Probe Harness Fix

`tools\probe-vnext-pet-tasks.ps1` now launches the Shell with an isolated audit scenario:

- `mode = Pinned`
- `webtools_visible = False`
- isolated data root
- isolated trace root
- unique timestamp plus GUID output folder

This avoids false failures when another app, such as Chrome, has focus and Wevito correctly enters passive/collapsed HUD mode.

### PET TASKS Runtime Probes

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -SkipBuild -StartupDelayMs 3500
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -SkipBuild -TaskText "review pet state and wellbeing" -ExpectedToolFamily petState -SkipSpriteHashCheck -StartupDelayMs 3500
```

Results:

- `spriteAudit` live UI probe passed.
- Summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-123138-223-450ea95a\summary.json`
- `petState` live UI probe passed.
- Summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-123138-240-0556575e\summary.json`
- PET TASKS wellbeing text was populated in the popup.
- `spriteAudit` target rows remained hash-stable.

## Current Readiness

The code-side runtime is ready for continued non-mutating PET TASKS adapter work and safe UI/control validation.

Visual-side sprite cleanup is structurally valid from the code perspective:

- Expected sprite folders exist.
- Expected frame count exists.
- No mixed-canvas runtime rows remain.
- No missing or invalid runtime PNGs were reported.

## Boundaries Still Active

- Keep using `-SkipAssetPrep` while visual cleanup is active.
- Do not regenerate, normalize, or import sprite runtime PNGs from code-side cleanup.
- Do not use PET TASKS for mutation, sprite import, generation, prop-anchor edits, candidate apply, all-color propagation, or packaged proof execution unless separately approved.
- Treat top-level `artifacts/` and `vnext/artifacts/` as local/generated proof output.

