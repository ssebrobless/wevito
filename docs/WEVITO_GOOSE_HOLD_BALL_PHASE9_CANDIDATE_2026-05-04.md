# Wevito Goose Hold-Ball Phase 9 Candidate

Updated: 2026-05-04

This is Phase 9 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It creates a first non-applied candidate proof for:

```text
goose / baby / female / blue / hold_ball
```

No `sprites_runtime` files were overwritten. No source boards were edited. No
code files were changed.

## Runtime Contract Correction

The existing optional-family PNGs do not bake the ball into the sprite frame.
Godot renders the ball as a separate carried-item sprite using:

```text
sprites_runtime/_metadata/prop_anchors.json
sprites_shared_runtime/items/toys_a/ball.png
```

Therefore the Phase 9 candidate body-pose PNGs do not include a baked ball.
The QA contact sheet and preview include a runtime-style ball overlay only for
review.

```text
candidate PNG
  -> goose body pose only

QA composite preview
  -> goose body pose
  -> plus runtime ball overlay at hold_ball anchor
```

This avoids a future double-ball bug if the candidate is ever applied.

## Artifact Folder

```text
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/
```

Key outputs:

| Output | Path |
| --- | --- |
| Candidate frames | `candidate/hold_ball_00.png` through `candidate/hold_ball_03.png` |
| Contact sheet | `qa/contact-sheet.png` |
| Composite preview | `qa/preview.gif` |
| Body-only preview | `qa/body-pose-preview.gif` |
| Composite stills | `qa/hold_ball_00_composite-preview.png` through `hold_ball_03_composite-preview.png` |
| Validation report | `validation.json` |
| Manifest | `manifest.json` |
| Run summary | `run-summary.md` |
| Current-frame backup copy | `backup-before-apply/hold_ball_00.png` through `hold_ball_03.png` |
| Layout guide | `layout-guide-hold-ball-4.png` |
| Packaged proof placeholder | `qa/packaged-runtime-proof-PENDING-NOT-APPLIED.png` |

## Candidate Method

The candidate is a conservative manual proof:

```text
source basis
  -> current goose / baby / female / blue / idle frames

manual adjustment
  -> tiny head/front-body pose changes
  -> no species redesign
  -> no baked prop
  -> no runtime file overwrite

review composite
  -> ball overlay placed from hold_ball anchor
  -> anchor_norm x=0.865 y=0.400
  -> scale=0.426
  -> z_index=12
```

The candidate is intentionally small. Its job is to prove whether a subtle
body-pose endpoint can improve the held-ball read before any provider generation
or broader art pass.

## Review Decision

```text
status: needs_human_review
applied: no
candidate kind: manual body-pose proof
ball baked into PNGs: no
runtime composite proof: pending because not applied
```

Suggested review labels:

| Label | Meaning |
| --- | --- |
| `accept_for_apply_probe` | Candidate reads better than idle clone and can be applied to a temporary/proof branch after code-side coordination. |
| `revise_candidate` | Direction is right, but pose/contact needs stronger adjustment before apply. |
| `reject_manual_candidate` | Manual micro-pose is not enough; use provider/manual redraw workflow from Phase 7 instead. |

## Acceptance Checklist

Before any future apply:

- candidate still reads as `goose / baby / female / blue`
- composite preview reads as held ball, not floating overlay
- body-only frames do not damage beak, face, feet, or outline
- frame-to-frame motion is subtle and loop-safe
- backup path is confirmed
- manifest source fields are acceptable for a manual run
- packaged/runtime proof will be produced immediately after apply
- rollback can restore the previous `hold_ball_00..03` files

## Current Recommendation

Do not expand beyond this candidate. Do not generate pickup/drop/carry work yet.

Next best step is human review of:

```text
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/qa/contact-sheet.png
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/qa/preview.gif
```

If accepted, coordinate with code-side before any apply/runtime proof step.

## Phase 9 Status

```text
Phase 9: candidate proof complete
runtime/source mutation: no
generation used: no
import/apply used: no
decision needed: accept_for_apply_probe / revise_candidate / reject_manual_candidate
post-pilot expansion: blocked until this one candidate is reviewed
```
