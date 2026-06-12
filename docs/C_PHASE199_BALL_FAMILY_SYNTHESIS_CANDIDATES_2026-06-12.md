# C-PHASE 199: Ball-Family Synthesis Tool + Full-Matrix Dry-Run Candidates

Date: 2026-06-12
Branch: `claude-implementation/c-phase-199-ball-family-synthesis-candidates`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §6.4
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §4

## Goal

The production synthesis tool plus reviewable candidates for all six ball families
across the matrix. ZERO writes to `sprites_runtime/` (verified: contract audit 0
errors after the run; runtime PNG census 14,214 = 14,202 baseline + the 12 C-198
pilot frames; no tracked modifications).

## Tool

`tools/generate_optional_ball_families.py`. Transform table (ball is overlay-only —
these are body-pose loops semantically matched to each family):

| Family | Frames | Source row | Transform |
| --- | --- | --- | --- |
| play_ball | 6 | happy | [h0,h1,h2,h3,h2,h1] + bounce lift (0,1,2,1,0,1) px |
| hold_ball | 4 | idle | [i0..i3] + settle lift (0,1,1,0) px |
| pickup_ball | 4 | eat | [e0..e3] (head lowers toward ground) |
| drop_ball | 4 | eat reversed | [e3..e0] (head returns up) |
| carry_ball_walk | 6 | walk | [w0..w5] unchanged |
| carry_ball_run | 6 | walk | [w0..w5] + run-bounce lift (0,1,2,1,0,2) px — the C-198 pilot transform |

Lift is clamped to each frame's transparent top margin, so edge-touching art
(C-197 flagged rows) is never cropped. No repaint, scale, mirror, or warp. Output
canvas equals the source row's canvas → per-row canvas consistency holds by
construction. Frame counts come from `OPTIONAL_EXPANDED_ANIMATIONS` (imported, not
redefined). The tool refuses any `--out-root` without an `artifacts` path segment,
skips families already in runtime, and supports `--hash-only` for determinism
checks.

## Dry-Run Results

- Frames generated: **10,770** (= 360 variants × 30 ball-family frames − the
  pilot's 30 existing). Families skipped as already-in-runtime: 6 (the C-198
  pilot's). Errors: 0.
- Determinism: full run vs `--hash-only` re-run — manifest sha256 sets identical.
  **PASS.**
- Manifests: `candidate-manifest.json` (+ `candidate-manifest-run2.json` proof copy)
  with per-frame sha256 — C-200 verifies against these before apply.

## Review Sheets

`vnext/artifacts/c-phase-199-ball-candidates/review-sheets/<species>.png` — one per
species; one row per variant (36 rows): play_ball ×6, carry_ball_run ×6, then
pickup/hold/drop/carry_walk first frames. The goose sheet's baby/female/blue row is
the applied C-198 pilot, labeled `[PILOT/runtime]`, as the visual reference.

## OWNER REVIEW RESOLVED 2026-06-12 — 0/10 APPROVED

Review each sheet; check a species to approve its matrix apply. C-200's scope is
exactly the checked species.

- [ ] rat — smaller issues
- [ ] crow — smaller issues (assigned by owner follow-up)
- [ ] fox — larger issues
- [ ] snake — larger issues
- [ ] deer — larger issues
- [ ] frog — larger issues
- [ ] pigeon — larger issues
- [ ] raccoon — smaller issues
- [ ] squirrel — smaller issues
- [ ] goose (35 remaining variants; pilot already applied) — smaller issues

**Verdict (owner, 2026-06-12): no species approved.** Larger issues (missing chunks
of sprites, very blurry, art inconsistencies): deer, fox, frog, pigeon, snake.
Smaller issues (small cutoffs, silhouette cleanup, sizing issues, cleaning between
the legs): crow, goose, raccoon, rat, squirrel. Because candidates are synthesized
verbatim from each variant's happy/idle/eat/walk source frames, these defects live
in the SOURCE required families — repair flows through the pulled-forward C-PHASE
203 (see plan Amendment R2), candidates regenerate deterministically afterward, and
a second review round gates C-200. Triage queue seeded at
`vnext/artifacts/c-phase-203-quality-triage/triage-queue.json`.

Note for review: pickup/hold/drop/carry_walk reuse already-shipped eat/idle/walk
poses verbatim (lowest risk). The judgment calls are play_ball (happy-bounce loop)
and carry_ball_run (walk-bounce loop). BUG-007 (color-tint inconsistency, e.g.
goose/blue and fox/blue required families) is visible in these sheets because the
candidates faithfully inherit their source rows' colors — approving a species here
approves the POSES; color repair stays in C-PHASE 203's queue.

## Stop-Gate Checklist

- [x] No write outside `vnext/artifacts/c-phase-199-ball-candidates/`.
- [x] All candidates RGBA with contract frame counts; canvas = source row canvas.
- [x] Determinism proof PASS.
- [x] No ball pixels baked (transforms are pure paste/offset of existing frames).

## Validation

- `dotnet build` PASS (0/0); full suite 1715/1715; sprite-filtered 77/77;
  `build-vnext -SkipAssetPrep -SkipTests` PASS; `git diff --check` PASS.
- Post-run contract audit: 0 errors; runtime census unchanged.
