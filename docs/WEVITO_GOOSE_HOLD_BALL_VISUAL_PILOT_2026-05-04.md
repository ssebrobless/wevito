# Wevito Visual Pilot: Goose Hold Ball

Updated: 2026-05-04

This is a no-generation pilot packet for the first future bespoke grip-pose
visual improvement. It prepares the visual target, prompt language, references,
and acceptance criteria. It does not ask anyone to generate, import, or edit
sprite frames yet.

Coordination rule: wait for the code-side reliability gates before running this
pilot.

## Target

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Source color | `blue` |
| Family | `hold_ball` |
| Frame count | 4 |
| Runtime target | `sprites_runtime/goose/baby/female/blue/hold_ball_00.png` through `hold_ball_03.png` |

Why this target:

- Goose has a clear beak/body grip read, so success or failure will be obvious.
- `hold_ball` is complete and stable, but currently lacks bespoke grip
  expressiveness.
- The current `hold_ball_00..03` runtime frames are byte-size identical to
  `idle_00..03`, confirming this row is effectively idle-derived.
- The pilot is only one row, which keeps review and rollback simple.

## Current Reference Paths

Use these as the first-pass local reference set:

| Purpose | Path |
| --- | --- |
| Canonical source board | `incoming_sprites/goose-baby.png` |
| Current hold row | `sprites_runtime/goose/baby/female/blue/hold_ball_00.png` through `hold_ball_03.png` |
| Identity baseline | `sprites_runtime/goose/baby/female/blue/idle_00.png` through `idle_03.png` |
| Positive expression baseline | `sprites_runtime/goose/baby/female/blue/happy_00.png` through `happy_03.png` |
| Prop anchors | `sprites_runtime/_metadata/prop_anchors.json` |
| Prior apply report | `vnext/artifacts/workflow-runs/20260504-apply-optional-goose-baby-female-hold_ball.json` |
| Prior cleanup report | `vnext/artifacts/workflow-runs/20260504-clean-optional-goose-baby-female-hold_ball.json` |
| Prior cleanup visual | `vnext/artifacts/workflow-runs/20260504-clean-optional-goose-baby-female-hold_ball-before-after.png` |

Current blue anchor excerpt:

```text
goose/baby/female/blue
  sample_frame: sprites_runtime/goose/baby/female/blue/idle_00.png
  sample_texture: 60 x 59
  sample_used_rect: x=8 y=4 w=43 h=51
  hold_ball anchor_norm: x=0.865 y=0.4
  hold_ball scale: 0.426
  hold_ball z_index: 12
```

Visual interpretation: the current anchor is high and forward, consistent with a
beak/front-body hold. The bespoke pose should make that contact feel physically
true rather than simply placing a ball there.

## Desired Visual Result

The improved row should read as:

```text
baby female goose
  |
  +-- same head / face / body proportions as canonical source
  +-- ball held at the beak/front-body contact point
  +-- subtle 4-frame hold motion, not an idle clone
  +-- no prop floating
  +-- no body shrink, crop, or box-fit
```

Frame intent:

| Frame | Visual intent |
| ---: | --- |
| `00` | Settled hold pose; ball visibly connected to beak/front body. |
| `01` | Tiny stabilizing lift or head/body micro-adjustment. |
| `02` | Return/settle variation; ball remains anchored. |
| `03` | Loop bridge back to frame 00 without a pop. |

This is not a dramatic action. It should be calm, readable, and physically
connected.

## Reject Conditions

Reject the candidate if any of these appear:

- the goose face/head changes species identity
- the baby proportions become adult-like or generic
- the ball floats away from the beak/front body
- the row is still just an idle clone with a pasted ball
- the ball teleports between frames
- the body is squeezed or resized to fit the cell
- wings, feet, beak, or ball are cropped
- any text, labels, UI, guide marks, shadows, dust, speed lines, glows, motion
  arcs, checkerboard, white/black background, or detached effects appear
- the candidate requires heavy cleanup to become valid

Identity drift is a blocker even if all four frames are valid PNGs.

## Prompt Draft

Use this prompt only after the code-side reliability gates are green and the
manifest/proof tools are behavior-validated.

```text
Create a Wevito hold_ball animation for goose / baby / female.

Use the attached canonical goose baby source and the current runtime references
as identity locks. Preserve the exact baby goose identity: head shape, beak,
face language, body proportions, outline weight, color family, and silhouette.

Return exactly 4 transparent sprite frames in 4 clear slots. Each frame will be
imported to a 28x24 runtime cell. Keep the goose centered with safe padding. Do
not crop, stretch, shrink, or box-fit the goose.

The ball must be physically held at the goose's beak/front-body grip point. It
must look clenched or supported by the goose, not pasted over the idle pose. The
ball must remain attached across all 4 frames.

Make the motion subtle: a small hold, stabilizing adjustment, and loop-safe
settle. Do not make a big jump, throw, pickup, or drop action.

No text, labels, UI, speech bubbles, speed lines, dust, shadows, glows, motion
arcs, checkerboard, white background, black background, guide marks, borders, or
detached effects.
```

## Provider Reference Pack

When generation opens, assemble the provider handoff with:

| Slot | Content |
| --- | --- |
| 1 | canonical source crop/board for `goose / baby / female` |
| 2 | current `hold_ball` row/contact sheet |
| 3 | current `idle` row for identity baseline |
| 4 | current `happy` row for positive expression baseline |
| 5 | 4-frame layout guide from the Phase 2 layout-guide helper |
| 6 | prop-anchor note showing `hold_ball anchor_norm x=0.865 y=0.4` |

Do not upload broad unrelated species sheets. The provider should see enough to
lock identity and motion, not enough to improvise a redesign.

## Review Checklist

```text
GOOSE HOLD_BALL PILOT REVIEW

Contract
  [ ] 4 frames only
  [ ] transparent output after import
  [ ] runtime cells are 28x24 after import
  [ ] no copied guide marks or backgrounds

Identity
  [ ] still goose
  [ ] still baby
  [ ] still female variant where visible
  [ ] head/beak/face match canonical source
  [ ] outline and color family remain Wevito-native

Grip / Prop
  [ ] ball attached to beak/front body
  [ ] no floating overlay read
  [ ] no ball teleport between frames
  [ ] ball scale remains consistent

Motion
  [ ] visible hold variation, not exact idle clone
  [ ] subtle loop
  [ ] no pickup/drop/play action bleed

Artifacts
  [ ] no matte or chroma fringe
  [ ] no detached specks
  [ ] no edge crop
  [ ] no body holes

Proof
  [ ] manifest complete
  [ ] source hash recorded
  [ ] contact sheet reviewed
  [ ] preview reviewed
  [ ] packaged/runtime proof reviewed
```

## Review Decision Form

| Field | Value |
| --- | --- |
| Run ID |  |
| Candidate source path |  |
| Source hash |  |
| Contact sheet path |  |
| Preview path |  |
| Runtime proof path |  |
| Identity | pass / warning / fail |
| Grip contact | pass / warning / fail |
| Motion readability | pass / warning / fail |
| Artifact cleanliness | pass / warning / fail |
| Decision | accept / repair / reject |
| Human note |  |

## Safe Expansion After Pilot

Only expand if the goose baby female pilot succeeds without identity drift and
without heavy cleanup.

Suggested next order:

1. `goose / baby / male / blue / hold_ball`
2. `goose / teen / female / blue / hold_ball`
3. `goose / adult / female / blue / hold_ball`
4. one mammal with paw/mouth ambiguity, likely `rat` or `fox`
5. one non-mammal shape challenge, likely `snake` or `frog`

Do not expand to all species until the pilot proves that the prompt and review
loop can reliably improve grip poses rather than create pretty but off-model
sprites.

## Handoff Note

This packet is ready for a future visual-generation pass, not for immediate
execution. The current project recommendation remains: wait for the code-side
reliability work before generation/import.
