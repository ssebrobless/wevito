# Code Side Phase 1 Runtime Gate Progress - 2026-05-04

## Scope

```text
Phase 1 runtime gate
├── build-vnext reliability
│   └── skip/timeout knobs for deterministic code-only validation
├── runtime canvas diagnosis
│   └── non-mutating mismatch reporter
├── vNext UI probe
│   ├── current popup-based Actions path
│   ├── Settings popup save path
│   └── Link Bin paste/open/delete path
└── tiny shell UI fix
    └── Basket Delete can act on selected or single saved row
```

This pass stayed in the deterministic code/runtime lane. It did not start Gemini/OpenAI generation, asset import convention changes, sprite repair, or broad visual rewrites.

## Files Changed

- `tools/build-vnext.ps1`
- `tools/probe-vnext-actions.ps1`
- `tools/report_runtime_canvas_mismatches.py`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`

Unrelated dirty files already existed in this worktree and were not modified by this pass:

- `scripts/game_manager.gd`
- `scripts/main_scene.gd`
- `scripts/pet.gd`
- `tools/audit_sprite_contract.py`
- `tools/build_sprite_source_manifest.py`
- `tools/gemini_cdp_helper.mjs`
- `tools/gemini_existing_window_driver.py`
- `tools/launch_gemini_automation_chrome.ps1`

## What Changed

- `build-vnext.ps1` now supports `-SkipAssetPrep`, `-SkipTests`, and `-StepTimeoutSeconds`, with named step logging and timeout handling.
- `build-vnext.ps1` warns when `-SkipAssetPrep` is used without `-SkipTests`, because sprite runtime coverage expects freshly prepared assets plus `generation-summary.json`.
- Added `tools/report_runtime_canvas_mismatches.py`, a non-mutating PNG IHDR reporter for mixed-canvas runtime sequences.
- Updated `tools/probe-vnext-actions.ps1` to use the current popup UI:
  - opens `ActionsButton`, then clicks `ActionMenu*Button`
  - selects the first action option when an action opens an item-choice grid
  - opens `SettingsButton`, toggles settings, then clicks `SettingsSaveButton`
  - starts probe sessions from a pinned audit scenario so results do not depend on whatever app is foreground on the user's desktop
  - records `failure.json` on probe errors
  - marks Link Bin rows for deletion and validates paste/open/delete traces
  - accepts explicit "not needed right now" feedback as a successful control response for actions like `Home`
- Fixed Basket Delete enablement/behavior:
  - delete state updates when basket selection changes
  - Delete enables for marked rows, selected rows, or one saved row
  - Delete handler falls back to the only saved row when no mark/selection is preserved

## Validation Results

- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\runtime-canvas-mismatches-20260504-worktree.json --markdown .\vnext\artifacts\runtime-canvas-mismatches-20260504-worktree.md`
  - Passed as a reporter.
  - Checked `2880` sequences and `10800` frames.
  - Found `546` mixed-canvas sequences, `0` missing/count-mismatch sequences, and `0` invalid PNG frames.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 180`
  - Passed and republished `vnext\artifacts\shell\Wevito.VNext.Shell.exe`.
  - Re-run after the Phase 4 warning cleanup also passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500`
  - Passed.
  - Summary: `vnext\artifacts\action-probes\20260504-175949\summary.json`.
  - Covered Doctor, Medicine, Bath, Groom, Feed with option, Water, Play with option, Rest, Home not-needed feedback, Settings, Save, Link Bin paste/open/delete, broker OpenUrl, and clipboard restoration.
- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --filter "FullyQualifiedName~SpriteValidatorTests|FullyQualifiedName~RepairPlannerTests|FullyQualifiedName~PetSimulationEngineTests"`
  - Passed `12 / 12` in this worktree.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180`
  - Failed only in sprite runtime coverage gates.
  - `RuntimeSpriteTree_HasExpectedShape`: missing runtime generation summary because asset prep was intentionally skipped.
  - `RuntimeSpriteFrames_AreCanonicalTransparentPngs`: expected width `72`, actual `89` on current runtime assets.

## Remaining Blocker

The largest remaining Phase 1 blocker is still the runtime sprite asset contract, not the shell controls. In this worktree the reporter found `546` mixed-canvas runtime sequences across `crow`, `deer`, `frog`, `goose`, and `pigeon`. Do not mass-normalize or rewrite these assets until the visual lane confirms the source-generation freedom versus runtime-canvas contract policy.

## Safe Next Code-Side Tasks

- Decide whether `build-vnext.ps1 -SkipAssetPrep` should automatically imply `-SkipTests` or print a stronger warning, because full sprite coverage tests expect generation summary artifacts.
- Add a small test around `ToolPopupWindow` basket delete semantics if a UI-testable seam is available, or keep the passing probe as the runtime proof.
- Keep the pinned audit-scenario probe pattern for future desktop-control tests so browser/user foreground state cannot hide controls.
- Reconcile this worktree's Phase 1 results with the main project docs under `C:\Users\fishe\Documents\projects\wevito\docs` before commit/push.

## Work To Keep Waiting

- Broad sprite PNG normalization.
- New Gemini/OpenAI visual generation or import conventions.
- Clipboard Shelf / Webtools Slot 2 implementation.
- Bespoke grip-pose visual authoring.

## Phase 2 Runtime Work-Companion Bridge

```text
visual thread states
├── Waving  ──┐
├── Jumping ──┼── runtime enum + dev selector
├── Failed  ──┤
├── Waiting ──┤
└── Review  ──┘
        │
        ▼
SpriteAssetService fallback
├── Waving  -> happy
├── Jumping -> happy
├── Failed  -> sad
├── Waiting -> idle
└── Review  -> idle
        │
        ▼
current assets render safely until authored work-state rows exist
```

This phase folded in only the safe runtime seam from the visual thread's Phase 4 update. It did not copy the broad repair-automation UI/pipeline work and did not edit sprite PNGs.

### Files Changed In Phase 2

- `vnext/src/Wevito.VNext.Contracts/Models.cs`
- `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/tests/Wevito.VNext.Tests/PetSimulationEngineTests.cs`

### What Changed In Phase 2

- Added `Waving`, `Jumping`, `Failed`, `Waiting`, and `Review` to `PetAnimationState`.
- Added sprite fallback routing for unauthored work-companion states:
  - `Waving` and `Jumping` use existing `happy` rows.
  - `Failed` uses existing `sad` rows.
  - `Waiting` and `Review` use existing `idle` rows.
- Added visible shell pulses:
  - invalid broker action -> `Failed`
  - clipboard capture / open tool popup -> `Waiting`
  - valid Link Bin capture -> `Jumping`
  - invalid Link Bin capture -> `Failed`
  - open saved link -> short `Walk`
- Added ambient `Waiting` while a tool popup is open, preserving active explicit/pulse overrides.
- Added `SpriteAssetService.InvalidateCache` as a tiny forward-compatible seam for later deterministic asset apply flows.
- Added trace-once protection for sprite fallback/missing logs so fallback rendering stays diagnosable without flooding traces every frame.
- Added a simulation regression test proving active work-companion overrides survive normal `Tick` presentation refresh.
- Follow-up self-audit fix: ambient popup `Waiting` now preserves any already-active different animation override, so dev/manual animation previews are not hidden just because a popup is open.

### Phase 2 Validation

- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --filter "FullyQualifiedName~PetSimulationEngineTests"`
  - Passed `17 / 17`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 180`
  - Passed and republished `vnext\artifacts\shell\Wevito.VNext.Shell.exe`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500`
  - Passed after Phase 2 and the follow-up ambient-override audit fix.
  - Latest summary: `vnext\artifacts\action-probes\20260504-182214\summary.json`.
  - Covered all primary action buttons/options, Settings Save, Link Bin paste/open/delete, broker OpenUrl, and clipboard restoration.
  - Work-state fallback traces were present and no longer repeated every render frame.

### Phase 2 Coordination Note

The main project at `C:\Users\fishe\Documents\projects\wevito` already contains the broader visual-thread Phase 3/4 work, including repair taxonomy/planner/validator changes and repair automation review states. This worktree intentionally implemented only the small runtime work-companion bridge so it can merge cleanly with that lane later.

## Phase 3 Read-Only Runtime Asset Contract Diagnostics

```text
SpriteRuntimeCoverageTests
├── generation-summary.json must exist
├── every required row must have the expected frame count
├── every required frame must be transparent PNG color type 4 or 6
└── every required frame must match canonical canvas
    ├── default: 72x64
    └── snake walk: baby 104x64, teen 112x64, adult 120x64
```

The first version of the reporter only explained mixed-canvas rows. Phase 3 expanded it to mirror the canonical canvas check too, so the visual lane can see both "frames within a row disagree" and "frames agree with each other but disagree with the runtime contract."

### Files Changed In Phase 3

- `tools/report_runtime_canvas_mismatches.py`

### What Changed In Phase 3

- Added the canonical canvas contract from `SpriteRuntimeCoverageTests.cs` into the reporter.
- Added `generation-summary.json` presence reporting.
- Added frame-level canonical mismatch counts and markdown sections by animation/species.
- Extended `--fail-on-mismatch` so it also fails on canonical mismatches or missing generation summary.

### Phase 3 Validation

- `python -m py_compile .\tools\report_runtime_canvas_mismatches.py`
  - Passed.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\runtime-canvas-contract-20260504-worktree.json --markdown .\vnext\artifacts\runtime-canvas-contract-20260504-worktree.md`
  - Passed as a read-only reporter.
  - Checked `2880` sequences and `10800` frames.
  - Found `546` mixed-canvas sequences.
  - Found `3852` canonical canvas frame mismatches.
  - Found `0` missing/count-mismatch sequences and `0` invalid PNG frames.
  - Reported `generation-summary.json` as missing in this worktree's `sprites_runtime`.
  - Latest markdown: `vnext\artifacts\runtime-canvas-contract-20260504-worktree.md`.

### Phase 3 Coordination Note

This diagnostic makes the remaining red gate more precise without changing the gate. Do not edit `SpriteRuntimeCoverageTests.cs` or mass-resize runtime PNGs from this code-side lane; visual/source policy still needs to decide how generated free-form sprite canvases become runtime-compatible while preserving motion and scale.
