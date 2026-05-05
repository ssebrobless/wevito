# Goose Baby Female Ball Overlay Preflight

Status: non-mutating visual QA packet.

This packet renders offline contact sheets for `goose / baby / female` optional ball families across all six colors using the current prop-anchor metadata and shared ball asset.

It does not apply, overwrite, import, normalize, or mutate runtime/source PNGs. The ball is composited into review sheets only.

## Review Shape

```text
goose / baby / female
  |
  +-- six colors
  |     +-- red / orange / yellow / blue / indigo / violet
  |
  +-- ball families
        +-- pickup_ball
        +-- drop_ball
        +-- hold_ball
        +-- carry_ball_walk
        +-- carry_ball_run
```

## QA Artifacts

```text
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/qa/goose-baby-female-six-color-pickup_ball-offline-ball-overlay.png
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/qa/goose-baby-female-six-color-drop_ball-offline-ball-overlay.png
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/qa/goose-baby-female-six-color-hold_ball-offline-ball-overlay.png
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/qa/goose-baby-female-six-color-carry_ball_walk-offline-ball-overlay.png
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/qa/goose-baby-female-six-color-carry_ball_run-offline-ball-overlay.png
vnext/artifacts/visual-review/20260505-goose-baby-female-ball-overlay-preflight/qa/blue-drop-ball-current-vs-pending-candidate-offline-ball-overlay.png
```

## Interpretation

These sheets mirror the alpha used-rect anchor math in `scripts/pet.gd` closely enough for visual preflight, but they are not a packaged Godot proof. Code-side still owns the actual one-row `drop_ball` apply/proof/rollback.

## Visual Notes

- `hold_ball` blue remains the accepted protected endpoint.
- `drop_ball` blue current runtime is still unchanged; the pending candidate is shown separately with the same overlay math.
- All six colors share the same goose baby female ball anchor values, so color propagation risk is mostly pose/palette consistency rather than per-color anchor drift.
- The ball remains runtime overlay only. Do not bake the ball into sprite PNGs.

## Next Use

After code-side completes the blue `drop_ball` proof, compare the packaged proof to these offline sheets. If the Godot proof diverges significantly, trust the packaged proof and update the visual plan from that evidence.
