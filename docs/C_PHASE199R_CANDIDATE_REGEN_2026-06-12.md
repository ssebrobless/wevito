# C-PHASE 199R: Ball-Candidate Regeneration + Second Review Round

Date: 2026-06-12
Branch: `claude-implementation/c-phase-199r-candidate-regen`
Plan: Amendment R2 (§10.2) / Prompts §11
Predecessors: C-203A (PR #308), C-203B Stage 2 (PR #312) — all source repair merged

## What was done

1. Re-ran `tools/generate_optional_ball_families.py` over the full matrix against
   the repaired + regenerated runtime sources → 10,770 candidate frames at
   `vnext/artifacts/c-phase-199r-ball-candidates/` (local). Determinism verified:
   two `--hash-only` runs byte-identical. Errors: 0.
2. **Pilot variant regenerated too**: goose/baby/female/blue's 6 live ball
   families were synthesized from PRE-repair sources in C-198, so they were
   re-derived from the repaired sources via a pruned temp tree
   (`_pilot-refresh/`, 30 frames). C-200's goose batch will overwrite the
   pilot's runtime families with these (the only intentional overwrite —
   everything else is additive).
3. Rebuilt the 10 per-species review sheets (`review-sheets/<species>.png`),
   same layout as C-199: one row per variant — play_ball ×6, carry_ball_run ×6,
   then pickup/hold/drop/carry_walk first frames. The goose pilot row is
   labeled `[PILOT/refreshed]`.
4. The C-199 candidate set is superseded; its manifest remains at
   `vnext/artifacts/c-phase-199-ball-candidates/candidate-manifest.json` for
   provenance. **The C-199 approval checklist is void.**

## Stop-Gate Checklist

- [x] No write outside the c-phase-199r artifact root.
- [x] Hash-stable across two --hash-only runs.
- [x] Every source row consumed passes the contract audit (0 errors) and the
      BUG-007 color-conformance census (0/360 nonconforming) — verified in
      C-203B Stage 2 post-proof at the same tree state.

## AWAITING PER-SPECIES OWNER APPROVAL BEFORE C-PHASE 200

Review each sheet at `vnext/artifacts/c-phase-199r-ball-candidates/review-sheets/`;
check a species to approve its matrix apply. C-200's scope is exactly the
checked species.

- [ ] crow
- [ ] deer
- [ ] fox
- [ ] frog
- [ ] goose (incl. pilot-variant refresh)
- [ ] pigeon
- [ ] raccoon
- [ ] rat
- [ ] snake
- [ ] squirrel

Context for review: every candidate derives from art that passed this track's
repair gates (C-203A procedural cleanup; C-203B Gemini regeneration with the
≤4-frame/256px/sharpness-gate anti-blur policy; BUG-007 tint conformance
0/360). Known deferred row: snake/adult/male kept its old walk art (4 failed
Gemini rolls), so its carry_ball_walk/run candidates derive from the OLD walk
frames — flag that row if it bothers you and C-200 can exclude it.

## Next

C-200 guarded matrix apply (approved species only, ~1,080 PNGs per species,
per-species batches, prop_anchors.json + coverage-test pin) → C-204 → C-205.
