# Goose Baby Female Six-Color Preflight

Updated: 2026-05-05

This is a non-mutating visual-side review for `goose / baby / female` across
all six egg colors.

It does not apply, overwrite, import, normalize, or mutate runtime/source PNGs.

## Review Shape

```text
goose / baby / female
  |
  +-- colors
  |     +-- red
  |     +-- orange
  |     +-- yellow
  |     +-- blue
  |     +-- indigo
  |     +-- violet
  |
  +-- core rows
  |     +-- idle
  |     +-- happy
  |     +-- sad
  |     +-- walk
  |
  +-- optional rows
        +-- pickup_ball
        +-- drop_ball
        +-- hold_ball
        +-- carry_ball_walk
        +-- carry_ball_run
```

## Artifact Packet

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-goose-baby-female-six-color-preflight\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\goose-baby-female-six-color-core-runtime-sheet.png
  +-- qa\goose-baby-female-six-color-optional-runtime-sheet.png
  +-- qa\blue-drop-ball-current-vs-pending-candidate.png
```

## Main Findings

- All six color folders are present.
- All reviewed core and optional runtime rows are present.
- Blue `hold_ball` is the accepted applied endpoint and should remain
  protected.
- Blue `drop_ball` remains unchanged in runtime; the pending candidate is shown
  separately for code-side apply/proof planning.
- `pickup_ball`, `carry_ball_walk`, and `carry_ball_run` are current runtime
  proof-only rows for the current code-side step.
- The ball remains runtime overlay only. This packet does not bake or prove ball
  overlays.

## Mixed-Canvas Warnings

The packet recorded 24 mixed-canvas warnings. These are planning context, not a
request to crop or shrink the animal.

```text
all six colors
  |
  +-- pickup_ball
  |     +-- 101x125, 100x120, 101x123, 102x125
  |
  +-- drop_ball
  |     +-- 119x112, 83x113, 45x87, 78x114
  |
  +-- carry_ball_walk
  |     +-- mostly 80x72 with frame 03 at 80x73
  |
  +-- carry_ball_run
        +-- mostly 80x72 with frame 03 at 80x73
```

Interpretation:

- `drop_ball` is the only row in the current code-side apply/proof scope.
- The blue `drop_ball` candidate replaces the broken partial-slice runtime row
  with full-body existing goose frames.
- The `pickup_ball` mixed sizes appear consistent across all six colors and are
  part of existing current runtime rows.
- The `carry_ball_walk` and `carry_ball_run` one-pixel frame-height difference
  is consistent across all six colors and should not block visual review by
  itself.

## Next Use

After code-side finishes the one-row blue `drop_ball` apply/proof:

```text
blue optional endpoint decision
  |
  +-- accept_applied_optional_expansion
  |     +-- compare against six-color preflight
  |     +-- plan color propagation deliberately
  |
  +-- revise_or_rollback_drop
  |     +-- use blue current-vs-candidate sheet
  |     +-- keep other colors untouched
  |
  +-- hold_before_more_expansion
        +-- keep this packet as baseline evidence
```

Do not propagate the blue endpoint to other colors until the applied blue proof
is accepted and a separate color-propagation plan is approved.
