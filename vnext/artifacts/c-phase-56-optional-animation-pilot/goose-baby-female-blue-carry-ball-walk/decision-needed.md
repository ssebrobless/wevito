# Decision Needed

Choose one label before any future apply work:

```text
accept_candidate_for_apply_plan
revise_candidate_before_apply
hold_optional_animation_pilot
```

Recommended current decision: `hold_optional_animation_pilot` until visual-side reviews the contact sheets and confirms whether the existing `carry_ball_walk` body-pose candidate is acceptable despite the prior manifest source-path mismatch.

If `accept_candidate_for_apply_plan` is chosen later, the next branch must run backup-before-apply, dry-run replacement scope, hash verification, apply, post-proof, rollback drill, and re-apply. Only `sprites_runtime/goose/baby/female/blue/carry_ball_walk_00.png..05.png` may be in mutation scope.

---

## Decision Recorded — 2026-06-12 (C-PHASE 198)

Chosen label:

```text
accept_candidate_for_apply_plan
```

Recorded by C-PHASE 198 (pet-sim completion track, owner decision D3 at track
go-ahead). All six candidate sha256 values re-verified against this packet's
target-manifest.json (6/6 OK). The candidates were normalized to a uniform 80x73
bottom-centered row canvas and applied to
`sprites_runtime/goose/baby/female/blue/carry_ball_walk_00..05.png`;
`carry_ball_run_00..05.png` was synthesized from the same normalized poses with a
deterministic run-bounce lift (0,1,2,1,0,2 px). Evidence + rollback:
`vnext/artifacts/c-phase-198-ball-pilot/`.
