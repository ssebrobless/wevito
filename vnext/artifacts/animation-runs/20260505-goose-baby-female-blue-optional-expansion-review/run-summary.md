# Goose Baby Female Blue Optional Expansion Review

Status: review-only, not applied.

This packet prepares the first post-hold endpoint optional expansion set for `goose / baby / female / blue`.

```text
hold_ball accepted endpoint
  |
  +-- pickup_ball      -> current row looks proof-ready
  +-- drop_ball        -> new candidate fixes broken partial-slice row
  +-- carry_ball_walk  -> current row looks proof-ready
  +-- carry_ball_run   -> current row looks proof-ready
```

Proofs:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/qa/optional-expansion-candidate-review-sheet.png
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/qa/pickup_ball-candidate-preview.gif
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/qa/drop_ball-candidate-preview.gif
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/qa/carry_ball_walk-candidate-preview.gif
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/qa/carry_ball_run-candidate-preview.gif
```

Next decision label for user/code-side:

```text
accept_optional_expansion_review_for_apply_plan
revise_drop_candidate
hold_optional_expansion
```
