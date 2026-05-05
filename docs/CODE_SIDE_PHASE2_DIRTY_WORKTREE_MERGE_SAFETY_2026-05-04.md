# Code-Side Phase 2 Dirty Worktree Merge Safety

Date: 2026-05-04

Worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

Purpose:

Classify the current dirty worktree before any staging, commit, merge, or cross-thread reconciliation. No files were staged, reverted, deleted, or moved during this phase.

## Current Shape

```text
Dirty worktree
|
+-- A. Current canvas/runtime contract closure
|   +-- 1704 tracked sprites_runtime PNG changes
|   +-- canvas reporter/tooling changes
|   +-- SpriteRuntimeCoverageTests contract update
|   +-- Phase 4A/4BC docs and checklist notes
|
+-- B. Code-side runtime bridge/reliability work
|   +-- vNext work-companion animation states
|   +-- Shell pulses/fallbacks/cache invalidation
|   +-- popup-aware action probe improvements
|   +-- build-vnext skip/timeout controls
|
+-- C. Local recovery artifacts
|   +-- 1704 untracked backup PNGs under artifacts/recovery
|   +-- keep local by default; do not commit unless explicitly wanted
|
+-- D. Earlier Godot/gameplay/visual-runtime edits
|   +-- scripts/game_manager.gd
|   +-- scripts/main_scene.gd
|   +-- scripts/pet.gd
|   +-- tools/audit_sprite_contract.py
|   +-- useful but not part of this Phase 2 canvas/reliability closure
|
+-- E. Gemini/browser automation helpers
    +-- untracked helper scripts under tools/
    +-- keep out of code-side runtime commits unless separately reviewed
```

## Inventory Counts

- Tracked modified files: `1718`.
- Tracked modified runtime PNGs: `1704`.
- Tracked modified docs: `1`.
- Tracked modified scripts: `3`.
- Tracked modified tools: `3`.
- Tracked modified vNext files: `7`.
- Untracked files: `1713`.
- Untracked recovery PNGs: `1704`.
- Untracked docs: `3`.
- Untracked tools: `6`.

## Recommended Commit/Stage Buckets

### Bucket 1: vNext Runtime Reliability And Work-Companion Bridge

Recommended as its own code commit, after Phase 3 validation:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\build-vnext.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-actions.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetSimulationEngineTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE1_RUNTIME_GATE_PROGRESS_2026-05-04.md`

Why separate:

This bucket changes runtime behavior and validation harnesses but does not require the 1704 PNG payload. It is easier to review, bisect, and roll back by itself.

### Bucket 2: Sequence-Stable Runtime Canvas Contract Tooling

Recommended as its own contract/tooling commit:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\report_runtime_canvas_mismatches.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\review_runtime_canvas_alignment.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\SpriteRuntimeCoverageTests.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\RUNTIME_CANVAS_NORMALIZATION_PHASE4A_SAFE_APPLY_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\RUNTIME_CANVAS_NORMALIZATION_PHASE4BC_CLOSURE_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md`

Why separate:

This defines the current approved runtime contract: natural per-sequence canvases, stable frame sizes inside each exact animation row, alpha-capable PNGs, no missing required rows, and legacy fixed-canvas checks as diagnostics only.

### Bucket 3: Runtime PNG Canvas Payload

Recommended as a separate asset commit:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime\**\*.png`

Scope:

`1704` tracked PNGs changed through transparent padding only across Phase 4A, Phase 4B, and Phase 4C.

Why separate:

The asset payload is large and binary-heavy. Keeping it isolated makes review and rollback much safer.

### Bucket 4: Local-Only Recovery Backups

Do not stage by default:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-normalization-20260504-phase4a-safe-padding`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-alpha-bottom-20260504-phase4b-alpha-bottom`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-preserve-original-y-20260504-phase4c-preserve-original-y`

Why local-only:

Git already contains the pre-change PNG versions. These folders are useful as local recovery anchors, but committing them would duplicate large binary data.

### Bucket 5: Hold For Separate Review

Do not include in the above code-side runtime/canvas commits without a deliberate review:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\scripts\game_manager.gd`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\scripts\main_scene.gd`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\scripts\pet.gd`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\audit_sprite_contract.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\build_sprite_source_manifest.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\gemini_cdp_helper.mjs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\gemini_existing_window_driver.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\launch_gemini_automation_chrome.ps1`

Observed themes:

- `scripts/game_manager.gd`, `scripts/main_scene.gd`, and `scripts/pet.gd` contain meaningful Godot gameplay/interaction/runtime path changes, including habitat anchors, runtime/shared sprite preference, drink/fetch routing, thrown ball flow, optional animation loading, and movement-to-anchor behavior.
- `tools\audit_sprite_contract.py` now accepts natural non-empty alpha-capable runtime PNG dimensions and includes optional expanded animation rows.
- The Gemini/browser helper tools look related to visual-generation automation rather than deterministic code-side runtime reliability.

Recommendation:

Preserve these files exactly as they are for now, but do not stage them with the vNext reliability or canvas normalization commits. They should get a separate review once the visual/code thread boundary is clear.

## Validation Evidence Already Available

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-final-sequence-stable.md`
  - `2880` sequences checked.
  - `10800` frames checked.
  - `0` mixed-canvas sequences.
  - `0` missing/count mismatches.
  - `0` invalid PNGs.
- `SpriteRuntimeCoverageTests` passed `2 / 2` after changing the test to the approved sequence-stable natural-canvas contract.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180` passed vNext tests `26 / 26` and published broker/shell artifacts before this Phase 2 docs-only audit.
- `git diff --check` passed during the prior docs reconciliation phase.

## Phase 2 Risk Notes

- The worktree is intentionally very dirty; a broad `git add .` would be unsafe.
- The 1704 PNG runtime payload should be reviewed/committed independently from code.
- The recovery backup folders are useful locally, but should not be committed unless the team explicitly wants duplicated binary rollback artifacts.
- Some older docs contain mojibake quote artifacts from prior notes. They are not part of this phase and should be cleaned in a small docs-only pass if desired.
- The Godot script changes may be valuable, but they affect gameplay/visual runtime behavior and should not be silently bundled into the current vNext reliability/canvas closure.

## Recommended Next Phase

Proceed to Phase 3: Runtime/Build Validation Sweep.

Suggested Phase 3 checks:

1. Run the sequence-stable runtime canvas reporter in fail-on-mismatch mode.
2. Run focused vNext tests for runtime coverage, simulation bridge behavior, and repair/planner contracts if present.
3. Run `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180`.
4. Run the popup-aware action/tool probe if the build completes.
5. Record validation results before any staging decision.
