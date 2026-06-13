# C-PHASE 203B Stage 2 (+ C-202): Round-trip complete, guarded apply, BUG-007 closed

Date: 2026-06-12
Branch: `claude-implementation/c-phase-203b-stage2-apply`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §10.2 (Amendment R2/R2b)
Prior stages: prep PR #309, driver PR #310, results checkpoint PR #311

## Round-trip final tally (204 boards)

- 204/204 boards returned through the owner's logged-in Gemini window
  (`batch-drive-motion-gemini.ps1`; quota pause + window restart mid-session,
  resumed via skip-existing).
- Sharpness gate (0.55× variance-of-Laplacian vs current runtime): 187 clean
  passes; 16 near-miss rejections (ratios 0.42–0.53) ACCEPTED after visual
  review — deer/pigeon/fox/frog references are the noisy originals being
  replaced, which inflates the bar (review sheets archived in
  `vnext/artifacts/c-phase-203b-imports/gate-review*-sheet.png`); 13 first-roll
  genuine-blur boards re-rolled successfully.
- True persistent failure: `snake-adult-male-locomotion_walk_a` — 4 rolls all
  produced cropped/zoomed body-segment frames (incl. a strengthened-prompt
  roll). **Deferred**: snake/adult/male keeps its current runtime walk row;
  queued for C-203 follow-up. Every other row regenerated.

## Guarded apply (`tools/apply_authored_family_imports.py`, new)

- Assembled 48 complete rows (8 species × 6 rows) from staged gate-verified
  frames; walk requires both _a/_b halves (snake/adult/male walk skipped as
  deferred).
- Per frame: fringe+speck cleanup (C-203A contract), then `colorize()` into ALL
  six color variants — **no passthrough color** — written to `sprites_runtime`
  AND `sprites_authored_verified` (goose/squirrel authored entries are new).
- 8,568 files written (714 frames × 6 colors × 2 trees); every overwritten file
  backed up with sha256 (`vnext/artifacts/c-phase-203b-apply/backup/`).

## BUG-007 CLOSED

The post-apply full-matrix census exposed the bug's true extent: deer/fox/frog
blue variants (18 rows) still carried natural-colored frames in their
non-regenerated families (sad/sleep/sick/bathe/groom/drink) — the same
propagate-passthrough trap as goose. All 360 such frames colorized
(backup + sha256 in `blue-oldfamily-tintfix.json`). Final census:
**0/360 variants nonconforming** (`tint-census-final.json`). The new apply
lane tints all six colors unconditionally, so the bug class cannot re-enter.

## Post-proof

- Sprite contract audit: error_count=0 (strict stray-family pin active).
- Runtime canvas: mismatch=0 missing=0 invalid=0.
- Tint census (all 10 species, 360 variants): 0 nonconforming.
- Full suite: 1717/1717. build-vnext -SkipAssetPrep: PASS. git diff --check: PASS.
- Visual checks archived: fox-blue-check.png (mixed old/new families coherent),
  spot-check.png (frog hop cycle, fox, snake, deer, pigeon).

## Stop-Gate Checklist

- [x] Mutation scope: sprites_runtime + sprites_authored_verified for the 8
      lane species' regenerated families + deer/fox/frog blue old-family tint
      fix only; backups for every overwrite.
- [x] No board imported below the sharpness bar without an archived visual
      review record.
- [x] Anti-blur policy enforced end-to-end (≤4-frame boards, 256px cells,
      gate at import).
- [x] Post-proof green across contract/canvas/tint/suite/build.
- [x] Deferred item recorded (snake/adult/male walk) — no silent gap.

## Artifacts (local `vnext/artifacts/c-phase-203b-apply/` + `c-phase-203b-imports/`)

apply.json (manifest), backup/ (sha256-recorded), blue-oldfamily-tintfix.json,
tint-census*.json, sprite_contract.json, runtime_canvas.json/.md, rollback.ps1,
stage2-status.json (full import/override/deferral ledger), gate-review sheets.

## Next

C-199R: regenerate all ball-family candidates from the repaired+regenerated
sources (deterministic), rebuild the 10 per-species review sheets, second owner
approval round → C-200 matrix apply → C-204 → C-205.
