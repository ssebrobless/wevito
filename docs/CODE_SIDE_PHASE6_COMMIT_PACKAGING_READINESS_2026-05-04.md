# Code-Side Phase 6 Commit And Packaging Readiness

Date: 2026-05-04

Worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

Purpose:

Refresh commit/package readiness after Phases 3-5. This phase did not stage, commit, push, package a Godot release, generate art, or mutate runtime PNGs.

## Current Worktree Shape

```text
current dirty state
|
+-- tracked modified files: 1718
|   +-- runtime PNG payload: 1704
|   +-- docs: 1
|   +-- tools: 3
|   +-- vNext: 7
|   +-- Godot scripts: 3
|
+-- untracked files: 1717
    +-- recovery PNG backups: 1704
    +-- docs: 7
    +-- tools: 6
```

Tracked non-PNG modified files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\scripts\game_manager.gd`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\scripts\main_scene.gd`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\scripts\pet.gd`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\audit_sprite_contract.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\build-vnext.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-actions.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetSimulationEngineTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\SpriteRuntimeCoverageTests.cs`

Untracked non-recovery files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE1_RUNTIME_GATE_PROGRESS_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE2_DIRTY_WORKTREE_MERGE_SAFETY_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE5_VISUAL_READINESS_CHECK_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\RUNTIME_CANVAS_NORMALIZATION_PHASE4A_SAFE_APPLY_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\RUNTIME_CANVAS_NORMALIZATION_PHASE4BC_CLOSURE_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\build_sprite_source_manifest.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\gemini_cdp_helper.mjs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\gemini_existing_window_driver.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\launch_gemini_automation_chrome.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\report_runtime_canvas_mismatches.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\review_runtime_canvas_alignment.py`

## Recommended Commit Stack

Do not use `git add .`.

```text
commit stack
|
+-- Commit 1: vNext runtime bridge and validation harness
|   +-- build-vnext.ps1
|   +-- probe-vnext-actions.ps1
|   +-- vNext Shell/Contracts tests touched by runtime bridge
|   +-- CODE_SIDE_PHASE1_RUNTIME_GATE_PROGRESS doc
|
+-- Commit 2: runtime canvas contract tooling/tests/docs
|   +-- report_runtime_canvas_mismatches.py
|   +-- review_runtime_canvas_alignment.py
|   +-- SpriteRuntimeCoverageTests
|   +-- Phase 4A / 4BC / Phase 2-6 docs
|   +-- VNEXT_IMPLEMENTATION_CHECKLIST.md
|
+-- Commit 3: runtime PNG canvas payload
|   +-- sprites_runtime/**/*.png
|
+-- Hold: Godot/gameplay and optional-runtime script changes
|   +-- scripts/*.gd
|   +-- tools/audit_sprite_contract.py
|
+-- Hold/local-only: recovery backups and Gemini automation helpers
    +-- artifacts/recovery/**
    +-- tools/gemini_*
    +-- tools/launch_gemini_automation_chrome.ps1
    +-- tools/build_sprite_source_manifest.py unless accepted into manifest workflow
```

## Packaging Readiness

### vNext Shell Package

Ready as a debug publish artifact from Phase 3.

Published shell entry point:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\shell\Wevito.VNext.Shell.exe`

Validation behind it:

- `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180` passed.
- Full vNext tests passed `26 / 26`.
- Action/tool probe passed with summary:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`

### Godot Release Package

Not ready to package from this worktree yet.

Reason:

`tools\build-release.ps1` would package the current Godot project state, including separate-review Godot script changes and the large dirty runtime asset payload. That would blur the safe commit boundary and could ship changes that have not been isolated or validated as a Godot gameplay slice.

Recommended gate before Godot release packaging:

1. Decide whether to include or hold `scripts\game_manager.gd`, `scripts\main_scene.gd`, and `scripts\pet.gd`.
2. Decide whether `tools\audit_sprite_contract.py` belongs with Godot/runtime visual readiness.
3. Commit or otherwise freeze the runtime PNG payload.
4. Run a Godot-specific launch/control/visual proof pass.
5. Only then run `tools\build-release.ps1`.

## Staging Risk

Highest-risk staging mistakes:

- Accidentally staging `artifacts\recovery\**`, which duplicates `1704` backup PNGs.
- Accidentally bundling Godot gameplay changes into vNext reliability commits.
- Accidentally bundling Gemini/browser automation helper scripts into deterministic code-side commits.
- Accidentally combining the `1704` binary PNG payload with small C# and tooling changes.

## Validation Status To Cite

- Phase 3 report:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md`

- Phase 4 report:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md`

- Latest runtime canvas contract:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.md`

- Latest action/tool probe:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`

## Phase 6 Decision

Ready for a careful, explicit staging pass later.

Not ready for broad staging, Godot release packaging, or visual production mutation.
