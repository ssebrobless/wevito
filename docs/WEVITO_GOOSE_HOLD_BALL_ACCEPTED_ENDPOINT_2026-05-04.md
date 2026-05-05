# Wevito Goose Hold Ball Accepted Endpoint

Updated: 2026-05-04

This note records the visual-side decision after code-side applied and proofed
the first one-row optional animation pilot.

## Decision Shape

```text
goose hold_ball pilot
  |
  +-- target
  |     +-- species: goose
  |     +-- age: baby
  |     +-- gender: female
  |     +-- color: blue
  |     +-- family: hold_ball
  |
  +-- changed runtime frames
  |     +-- hold_ball_00.png
  |     +-- hold_ball_01.png
  |     +-- hold_ball_02.png
  |     +-- hold_ball_03.png
  |
  +-- proof surface
  |     +-- Godot packaged proof
  |
  +-- visual decision
        +-- accept_applied_endpoint
```

## Changed Files

Only these four runtime frames changed:

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
```

No pickup, drop, carry, source-board, or all-color propagation work was
performed.

## Proof Artifacts

```text
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/
  +-- godot-packaged-proof-20260504-220556/
        +-- packaged-runtime-proof-contact-sheet.png
        +-- proof-summary.md
        +-- automation_report.json
```

## Runtime Overlay Confirmation

Code-side proof reported:

```text
kind: ball
metadata_family: hold_ball
metadata_key: goose/baby/female/blue
metadata_used: true
visible: true
z_index: 12
```

Visual interpretation:

```text
accepted
  -> ball reads attached to beak/front body
  -> ball remains runtime overlay
  -> body pose no longer reads as idle clone
  -> rollback not recommended
```

## Cleanup Plan Integration

This accepted row is now a protected endpoint reference.

```text
sprite cleanup work
  |
  +-- must not overwrite or "clean" this row casually
  |
  +-- may use this row as endpoint reference later
  |     +-- pickup_ball: low/ground ball moves toward hold endpoint
  |     +-- drop_ball: hold endpoint releases back to low/ground target
  |     +-- carry_ball_walk/run: contact should preserve this endpoint read
  |
  +-- must stay separate from shared asset cleanup
        +-- medicine/care
        +-- habitat objects
        +-- status/icons
```

## Current Limits

Still not approved:

- pickup/drop/carry generation
- all-color propagation
- broad optional-family batching
- automated cleanup of the accepted hold-ball row
- source-board edits

## Next Visual Work

Continue the sprite cleanup course of action:

```text
next visual task
  -> no-edit shared care/habitat classification packet
  -> protect accepted goose hold_ball row as reference state
```
