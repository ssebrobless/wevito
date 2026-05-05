# Runtime Canvas Normalization Phase 4B/4C Closure - 2026-05-04

## Shape

```text
Phase 4A safe pad
  546 mixed rows -> 222 mixed rows
  972 PNGs changed
        |
        v
Phase 4B grouped review
  222 color rows -> 37 source groups
  32 groups safe by stable alpha-bottom
  600 PNGs changed
        |
        v
Phase 4C final visual-policy apply
  30 color rows -> 5 source groups
  preserve original y, pad below/right only
  132 PNGs changed
        |
        v
Final sequence-stable runtime
  2880 sequences checked
  10800 frames checked
  0 mixed-canvas rows
  0 missing/count rows
  0 invalid PNGs
```

## Scope

This closure finished the deterministic runtime-canvas lane in the active Codex worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

No Gemini/OpenAI generation, sprite repainting, crop salvage, silhouette cleanup, recoloring, alpha cleanup, scaling, or visual import happened in this pass. The only runtime PNG mutation was transparent canvas expansion around existing pixels.

## What Changed

- Added `tools/review_runtime_canvas_alignment.py`.
- Extended `tools/report_runtime_canvas_mismatches.py` so `--fail-on-mismatch` now fails on sequence instability, missing/count mismatches, or invalid PNGs, while legacy fixed-canvas mismatches stay diagnostic unless `--fail-on-canonical-mismatch` is requested.
- Updated `vnext/tests/Wevito.VNext.Tests/SpriteRuntimeCoverageTests.cs` from the old fixed `72x64` canvas contract to the approved sequence-stable natural-canvas contract.
- Updated `tools/build-vnext.ps1` warning text so skipped asset prep no longer implies tests require `generation-summary.json`.
- Mutated `1704` runtime PNGs under `sprites_runtime` using transparent padding only:
  - Phase 4A: `972`
  - Phase 4B: `600`
  - Phase 4C: `132`

## Backups

- Phase 4A backups: `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-normalization-20260504-phase4a-safe-padding`
- Phase 4B backups: `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-alpha-bottom-20260504-phase4b-alpha-bottom`
- Phase 4C backups: `C:\Users\fishe\.codex\worktrees\36d6\wevito\artifacts\recovery\runtime-canvas-preserve-original-y-20260504-phase4c-preserve-original-y`

Backup counts:

- Phase 4A: `972` PNGs
- Phase 4B: `600` PNGs
- Phase 4C: `132` PNGs

## Review Artifacts

- Phase 4B grouped review JSON: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-review-20260504.json`
- Phase 4B grouped review markdown: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-review-20260504.md`
- Phase 4B contact sheets: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-review-20260504-sheets`
- Post-Phase 4B remaining review JSON: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-review-20260504-after-phase4b.json`
- Post-Phase 4B remaining review markdown: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-review-20260504-after-phase4b.md`
- Post-Phase 4B remaining sheets: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-review-20260504-after-phase4b-sheets`
- Phase 4B apply report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-phase4b-apply-report.json`
- Phase 4C apply report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-alignment-phase4c-apply-report.json`

## Final Validation

- `python C:\Users\fishe\Documents\projects\wevito\tools\plan_runtime_canvas_normalization.py --runtime-root C:\Users\fishe\.codex\worktrees\36d6\wevito\sprites_runtime --output C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-post-phase4c.json --markdown C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-normalization-post-phase4c.md`
  - Passed.
  - `planned_sequences=0`
  - `planned_changed_frames=0`
  - `risk_counts={}`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\runtime-canvas-contract-20260504-final-sequence-stable.json --markdown .\vnext\artifacts\runtime-canvas-contract-20260504-final-sequence-stable.md --fail-on-mismatch`
  - Passed.
  - `mismatch_count=0`
  - `missing_count=0`
  - `invalid_count=0`
  - `canonical_mismatch_count=3852` remains as legacy fixed-canvas diagnostic data only.
- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --filter "FullyQualifiedName~SpriteRuntimeCoverageTests"`
  - Passed `2 / 2`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180`
  - Passed.
  - vNext tests passed `26 / 26`.
  - Broker publish passed.
  - Shell publish passed.

## Remaining Meaning Of Canonical Mismatches

The reporter still records `3852` legacy fixed-canvas diagnostic mismatches. These are expected after approving natural per-sequence canvases, because many animals and motion families now legitimately exceed the old `72x64` frame box. They should not be treated as failures unless we intentionally run with `--fail-on-canonical-mismatch` for historical comparison.

The active runtime gate is now:

```text
each exact species / age / gender / color / animation row
  -> expected frame count exists
  -> every frame is a transparent PNG
  -> every frame in that exact row uses one shared natural canvas
```

## Visual Thread Coordination

The visual thread can treat the runtime sequence-canvas blocker as closed in this worktree. Future visual QA/contact sheets should no longer be distracted by per-frame canvas size jitter inside an animation row.

Important caution: this pass did not improve silhouettes, identity drift, holes, style consistency, crop quality, color quality, or animation quality. It only removed runtime canvas instability while preserving existing pixels. Visual QA still owns those art-quality judgments.
