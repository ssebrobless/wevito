# Goose Baby Female Six-Color Visual Preflight

Status: non-mutating review packet.

This packet reviews current runtime PNG rows for `goose / baby / female` across the six egg colors:

```text
red -> orange -> yellow -> blue -> indigo -> violet
```

It does not apply, overwrite, import, normalize, or mutate runtime/source PNGs.

## Review Shape

```text
goose / baby / female
  |
  +-- core state check
  |     +-- idle
  |     +-- happy
  |     +-- sad
  |     +-- walk
  |
  +-- optional ball-state check
        +-- pickup_ball
        +-- drop_ball
        +-- hold_ball
        +-- carry_ball_walk
        +-- carry_ball_run
```

## QA Artifacts

```text
vnext/artifacts/visual-review/20260505-goose-baby-female-six-color-preflight/qa/goose-baby-female-six-color-core-runtime-sheet.png
vnext/artifacts/visual-review/20260505-goose-baby-female-six-color-preflight/qa/goose-baby-female-six-color-optional-runtime-sheet.png
vnext/artifacts/visual-review/20260505-goose-baby-female-six-color-preflight/qa/blue-drop-ball-current-vs-pending-candidate.png
```

## Findings

- All six expected color folders are present: red, orange, yellow, blue, indigo, violet.
- All reviewed core and optional families have runtime PNG rows present.
- Blue `hold_ball` is now the accepted applied endpoint and should be protected.
- Blue `drop_ball` remains current runtime in-place; the pending candidate is shown separately for code-side proof planning.
- `pickup_ball`, `carry_ball_walk`, and `carry_ball_run` are review/proof-only current runtime rows for the current code-side step.
- Ball overlay remains runtime-only; these sheets do not bake or prove the ball overlay.

## Warnings To Carry Forward

Mixed frame canvases appear in some existing rows. This is review context, not an instruction to crop or shrink animals. If code-side normalizes later, it should preserve natural goose motion and sequence silhouette.

Warning count: 24

## Next Visual Use

After code-side finishes the one-row blue `drop_ball` apply/proof, use this packet to decide whether the next visual step should be:

```text
accept applied blue optional endpoint
  |
  +-- compare six-color optional consistency
  +-- plan color propagation only after blue endpoint is accepted
  +-- avoid automatic propagation if palette identity or prop contact degrades
```
