# Runtime Canvas Normalization Phase 4A Safe Apply - 2026-05-04

## Scope

```text
Phase 4A
+-- pre-apply dry run
|   +-- active worktree sprites_runtime
|   +-- no visual generation/import
+-- safe apply
|   +-- safe_transparent_pad only
|   +-- transparent canvas padding only
|   +-- no scaling, cropping, repainting, recoloring, or alpha cleanup
+-- backups
|   +-- artifacts/recovery/runtime-canvas-normalization-20260504-phase4a-safe-padding
+-- post-apply audit
    +-- review_alignment rows remain untouched
```

This phase performed the first mutating runtime PNG step in the active Codex worktree at `C:\Users\fishe\.codex\worktrees\36d6\wevito`. It used the existing normalization planner from `C:\Users\fishe\Documents\projects\wevito\tools\plan_runtime_canvas_normalization.py`, but all runtime roots, outputs, backups, and mutations were pointed at this worktree.

The apply was intentionally narrow. It only padded frames that the dry-run classified as `safe_transparent_pad`. Rows classified as `review_alignment` were skipped because they need either a stronger deterministic bottom/top anchor rule or visual review before padding.

## Commands Run

- `python -m py_compile C:\Users\fishe\Documents\projects\wevito\tools\plan_runtime_canvas_normalization.py`
  - Passed.
- `python C:\Users\fishe\Documents\projects\wevito\tools\plan_runtime_canvas_normalization.py --runtime-root C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime --output C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-plan-20260504-preapply.json --markdown C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-plan-20260504-preapply.md`
  - Passed as a dry run.
- `python C:\Users\fishe\Documents\projects\wevito\tools\plan_runtime_canvas_normalization.py --runtime-root C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime --apply-safe --stamp 20260504-phase4a-safe-padding --recovery-root C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery --output C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-post-phase4a.json --markdown C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-post-phase4a.md --apply-report C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-phase4a-apply-report.json`
  - Passed and applied safe padding only.
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\runtime-canvas-contract-20260504-after-phase4a.json --markdown .\vnext\artifacts\runtime-canvas-contract-20260504-after-phase4a.md`
  - Passed as a reporter.
- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --filter "FullyQualifiedName~SpriteRuntimeCoverageTests"`
  - Failed on known runtime-contract gates described below.

## Results

```text
pre-apply mixed rows:      546
safe rows applied:         324
review rows skipped:       222
PNG files padded:          972
post-apply mixed rows:     222
missing frame rows:        0
invalid PNG frames:        0
backup PNG files:          972
```

The active worktree differed from the earlier main-checkout snapshot. The main-checkout plan had `456` mixed rows, but this worktree had `546` mixed rows before Phase 4A. The worktree result is therefore documented separately and should not be merged blindly with the main-checkout artifact counts.

## Files And Artifacts

- Apply report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-phase4a-apply-report.json`
- Post-apply JSON: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-post-phase4a.json`
- Post-apply markdown: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-post-phase4a.md`
- Canvas contract JSON: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-after-phase4a.json`
- Canvas contract markdown: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-after-phase4a.md`
- Backups: `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-normalization-20260504-phase4a-safe-padding`

## Applied Padding Breakdown

By species:

- `crow`: 12 PNG files padded
- `deer`: 114 PNG files padded
- `frog`: 264 PNG files padded
- `goose`: 18 PNG files padded
- `pigeon`: 564 PNG files padded

By animation:

- `bathe`: 162 PNG files padded
- `eat`: 114 PNG files padded
- `happy`: 126 PNG files padded
- `idle`: 108 PNG files padded
- `sad`: 54 PNG files padded
- `sick`: 192 PNG files padded
- `sleep`: 60 PNG files padded
- `walk`: 156 PNG files padded

## Remaining Review Rows

The remaining `222` mixed-canvas rows are all `review_alignment`, not missing or invalid PNGs.

By species:

- `crow`: 180 rows
- `goose`: 36 rows
- `deer`: 6 rows

By animation:

- `eat`: 36 rows
- `bathe`: 30 rows
- `happy`: 30 rows
- `sad`: 30 rows
- `walk`: 30 rows
- `idle`: 24 rows
- `sick`: 24 rows
- `sleep`: 18 rows

These rows have ambiguous top/bottom alpha margins, so automatic padding could visually shift the subject in a way that changes apparent ground contact or motion. They should not be auto-applied until Phase 4B decides a stronger anchor rule or uses a visual contact-sheet review.

## Test Status

`SpriteRuntimeCoverageTests` still fails in this worktree for two known reasons:

- `RuntimeSpriteTree_HasExpectedShape` expects a runtime generation summary file, but this worktree does not currently have the expected `generation-summary.json` from asset prep.
- `RuntimeSpriteFrames_AreCanonicalTransparentPngs` still asserts the older fixed canvas contract, for example `expected 72`, `actual 89`, while this phase is preserving natural wider canvases and only enforcing sequence-stable canvases.

This failure is not a regression from Phase 4A. It confirms that the runtime test contract and the newer natural-canvas visual policy still need reconciliation before full green coverage is possible.

## Recommendation

Phase 4B should not mass-apply the remaining rows yet. The safest next step is:

```text
Phase 4B
+-- build review contact sheets for the 222 review_alignment rows
+-- group duplicate color variants by source row
+-- decide deterministic anchor rules per species/animation
+-- apply only rows where visual/contact anchor is clear
+-- update SpriteRuntimeCoverageTests to sequence-stable natural canvas policy only after runtime policy is approved
```

The visual thread can safely begin no-edit contact-sheet review for these remaining rows because Phase 4A did not alter silhouettes, colors, scale, or pose geometry. It only expanded transparent canvases around already-existing pixels.
