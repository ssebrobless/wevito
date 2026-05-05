# Goose Baby Female Ball Overlay Preflight

Updated: 2026-05-05

This is a non-mutating visual-side QA packet for `goose / baby / female` ball
states across all six egg colors.

The packet composites the shared ball asset into review sheets using current
prop-anchor metadata. It does not edit sprite PNGs.

## Review Shape

```text
goose / baby / female
  |
  +-- prop metadata
  |     +-- sprites_runtime\_metadata\prop_anchors.json
  |
  +-- prop asset
  |     +-- sprites_shared_runtime\items\toys_a\ball.png
  |
  +-- optional families
        +-- pickup_ball
        +-- drop_ball
        +-- hold_ball
        +-- carry_ball_walk
        +-- carry_ball_run
```

## Artifact Packet

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-goose-baby-female-ball-overlay-preflight\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\goose-baby-female-six-color-pickup_ball-offline-ball-overlay.png
  +-- qa\goose-baby-female-six-color-drop_ball-offline-ball-overlay.png
  +-- qa\goose-baby-female-six-color-hold_ball-offline-ball-overlay.png
  +-- qa\goose-baby-female-six-color-carry_ball_walk-offline-ball-overlay.png
  +-- qa\goose-baby-female-six-color-carry_ball_run-offline-ball-overlay.png
  +-- qa\blue-drop-ball-current-vs-pending-candidate-offline-ball-overlay.png
```

## What This Proves

- The six-color review can use one shared goose baby female anchor pattern.
- Current prop metadata is present for every reviewed color and family.
- The ball can be reviewed as an overlay without baking it into sprite frames.
- The pending blue `drop_ball` candidate can be compared to current blue
  runtime with the same offline overlay math.

## What This Does Not Prove

- It is not a packaged Godot proof.
- It does not confirm exact runtime z-order in the real scene.
- It does not approve any runtime/source PNG mutation.
- It does not approve all-color propagation.

## Visual Interpretation

```text
offline overlay sheet
  |
  +-- useful for visual planning
  +-- useful for spotting obvious contact/scale problems
  +-- useful for comparing current runtime vs pending candidate
  |
  +-- not a replacement for code-side Godot proof
```

If the future packaged Godot proof disagrees with this packet, trust the
packaged proof and update the visual plan from that evidence.

## Current Decision Context

Code-side still owns the one-row apply/proof/rollback step for:

```text
goose / baby / female / blue / drop_ball
```

The only eventual mutation scope remains:

```text
sprites_runtime\goose\baby\female\blue\drop_ball_00.png
sprites_runtime\goose\baby\female\blue\drop_ball_01.png
sprites_runtime\goose\baby\female\blue\drop_ball_02.png
sprites_runtime\goose\baby\female\blue\drop_ball_03.png
```

Blue `hold_ball` remains the accepted protected endpoint. The ball remains a
runtime overlay only.
