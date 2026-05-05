# Wevito Grip-Pose Batch Plan

Updated: 2026-05-04

This is the visual-side expansion plan for bespoke grip poses after the first
`goose / baby / female / blue / hold_ball` pilot. It is intentionally a planning
document only: no generation, no import, no sprite edits, and no runtime code.

Use this alongside:

- `docs/WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md`
- `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md`
- `docs/WEVITO_ANIMATION_QA_RUBRIC.md`
- `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md`

## Batch Principle

Do not expand by coverage grid. Expand by grip type.

```text
good expansion
  pilot row
    -> same species opposite gender
    -> same species older ages
    -> one similar grip type
    -> one harder grip type

bad expansion
  pilot row
    -> all 10 species
    -> all ages
    -> all ball families
    -> too many failures to explain cleanly
```

The goal is to learn what visual instructions work for each anatomy class before
committing to a broad generation batch.

## Species Grip Taxonomy

The current `sprites_runtime/_metadata/prop_anchors.json` shows clear grip
clusters for `baby / female / blue` hold-ball anchors:

| Species | Grip type | Hold anchor read | Visual instruction |
| --- | --- | --- | --- |
| `crow` | beak grip | high/front | Ball should be pinched by or tucked against the beak. |
| `pigeon` | beak grip | high/front | Ball should touch the beak without swallowing the face. |
| `goose` | beak/front-body grip | high/front | Ball should sit at beak/front-body contact, not float above the chest. |
| `fox` | mouth grip | far front | Ball should be held in mouth/jaw area with body still fox-like. |
| `rat` | mouth/paw support | front-mid | Ball may be near mouth with tiny forepaw/body support. |
| `raccoon` | paw/mouth grip | front-mid | Ball may be held by forepaws or mouth; avoid human hand pose. |
| `deer` | mouth/front-body hold | high/front | Ball should be gently held near muzzle, not between antlers or hooves. |
| `squirrel` | paw/front-body grip | upper-mid | Ball can be tucked close to paws/body; avoid oversized hands. |
| `frog` | mouth/body cup | central-low | Ball should be close to mouth/front body, not held by human-like hands. |
| `snake` | mouth/body curve | far front-mid | Ball should read as supported by mouth/body curve; avoid paw logic. |

Anchor snapshot for `baby / female / blue`:

| Species | Hold x | Hold y | Pickup/drop x | Pickup/drop y |
| --- | ---: | ---: | ---: | ---: |
| `rat` | 0.805 | 0.520 | 0.605 | 0.780 |
| `crow` | 0.865 | 0.340 | 0.585 | 0.760 |
| `fox` | 0.925 | 0.420 | 0.605 | 0.780 |
| `snake` | 0.935 | 0.520 | 0.605 | 0.820 |
| `deer` | 0.805 | 0.360 | 0.605 | 0.780 |
| `frog` | 0.665 | 0.560 | 0.545 | 0.780 |
| `pigeon` | 0.845 | 0.340 | 0.565 | 0.760 |
| `raccoon` | 0.805 | 0.500 | 0.585 | 0.780 |
| `squirrel` | 0.785 | 0.400 | 0.565 | 0.740 |
| `goose` | 0.865 | 0.400 | 0.585 | 0.800 |

These numbers are not prompt text by themselves. They are visual intent:
high/front anchors mean beak or muzzle contact, lower/central anchors mean
body/paw/mouth support.

## Expansion Sequence

Do not start this sequence until the goose pilot is accepted.

| Step | Target | Why |
| ---: | --- | --- |
| 1 | `goose / baby / female / blue / hold_ball` | First pilot. Clear beak/front-body read. |
| 2 | `goose / baby / male / blue / hold_ball` | Same species/age, checks gender variant preservation. |
| 3 | `goose / teen / female / blue / hold_ball` | Same grip type, larger body. |
| 4 | `goose / adult / female / blue / hold_ball` | Same grip type, highest adult proportion risk. |
| 5 | `crow / baby / female / blue / hold_ball` | Beak grip transfer to a smaller bird. |
| 6 | `fox / baby / female / blue / hold_ball` | Mouth grip; tests mammal contrast. |
| 7 | `raccoon / baby / female / blue / hold_ball` | Paw/mouth ambiguity; tests whether prompt avoids human hands. |
| 8 | `frog / baby / female / blue / hold_ball` | Central-low grip; tests non-mammal body cup. |
| 9 | `snake / baby / female / blue / hold_ball` | Hardest anatomy; mouth/body curve without paws. |

Stop after any step that produces identity drift or requires heavy cleanup.

## Family Expansion Order

Once hold-ball grip works for at least three grip types, expand family by family:

```text
1. hold_ball
   reason: static grip is easiest to judge

2. pickup_ball
   reason: uses same grip endpoint plus ground-to-hold transition

3. drop_ball
   reason: inverse of pickup, must avoid prop pop

4. carry_ball_walk
   reason: combines grip with gait

5. carry_ball_run
   reason: highest risk of shrink/smear/anchor slide
```

Do not use `play_ball` as the first bespoke grip target. It is more expressive
but less constrained, so it can hide prop-contact failures behind motion.

## Prompt Modifiers By Grip Type

Use the base prompt from the goose pilot, then add one grip-type modifier.

### Beak Grip

```text
The ball should be visibly pinched or supported at the beak/front-face contact
point. Keep the beak shape recognizable. Do not cover the face, turn the beak
into a hand, or place the ball floating in front of the head.
```

### Mouth Grip

```text
The ball should be held at the mouth/jaw area with a small contact change in the
head pose. Keep the animal's face and muzzle recognizable. Do not make the ball
look swallowed, glued to the cheek, or floating outside the mouth.
```

### Paw / Body Support

```text
The ball may be tucked close to the front paws or chest, but the pose must stay
animal-like. Avoid human hand gestures, oversized fingers, or a standing mascot
pose. Keep the ball supported by the pet's normal anatomy.
```

### Body Cup / Amphibian

```text
The ball should sit close to the mouth/front-body area with a compact body
support pose. Do not add human hands. Do not stretch the frog into a mammal-like
grip. Keep the silhouette low and species-specific.
```

### Body Curve / Snake

```text
The ball should read as supported by the mouth or body curve. Do not invent paws
or hands. Keep the snake silhouette continuous and readable, with no body knots,
cropped coils, or detached ball.
```

## Per-Step Review Gate

Every step must produce and review:

| Proof | Required question |
| --- | --- |
| Contact sheet | Does every frame preserve identity and prop contact? |
| Preview loop | Does the hold/pickup/drop/carry verb read at timing scale? |
| Current-row comparison | Is this meaningfully better than the idle/reseed baseline? |
| Runtime proof | Does it still read correctly in the actual display context? |
| Decision note | What concrete visual reason justifies accept/repair/reject? |

If the new row is not meaningfully better than the current stable row, reject it.
Do not accept new art only because it is new.

## Common Failure Patterns

| Failure | Why it matters | Decision |
| --- | --- | --- |
| Pretty redesign | Looks good but no longer Wevito's pet. | Reject |
| Idle clone plus ball | Does not solve the visual ceiling. | Reject |
| Floating prop | Breaks physical contact truth. | Reject |
| Prop teleport | Fails motion readability. | Repair or reject |
| Humanized grip | Makes pet look like a mascot/person. | Reject |
| Box-fit shrink | Damages identity and scale. | Reject |
| Heavy cleanup needed | Provider output is unstable. | Reject unless single-frame repair is tiny |
| Minor edge warning | May be acceptable after review. | Warning |
| Small pose stiffness | May be acceptable if contact improves. | Warning |

## Batch Stop Rules

Stop the batch immediately when:

- two consecutive candidates show identity drift
- a grip-type modifier causes humanized anatomy
- provider output repeatedly includes text/background/effects
- cleanup is doing more work than generation
- review cannot clearly explain why the new row is better
- code-side reliability gates are still unresolved

The correct response to a bad batch is not "generate more." It is to revise the
prompt/reference pack or narrow the target.

## Ready-To-Run Batch Prompt Skeleton

This is a skeleton, not a command to run now.

```text
Target: <species> / <age> / <gender> / blue / <family>
Grip type: <beak | mouth | paw-body | body-cup | body-curve>
Frame count: <4 or 6>

Use the canonical source, current runtime row, identity baseline row, prop anchor
note, and layout guide as references. Preserve exact Wevito identity and produce
only the requested family row.

Add grip modifier:
<paste one grip-type modifier here>

Reject constraints:
No text, UI, guide marks, background, checkerboard, shadows, glows, dust, speed
lines, motion arcs, detached effects, identity drift, prop floating, prop
teleport, box-fitting, or invented anatomy.
```

## Visual Thread Next Actions

While code-side reliability work continues, the visual thread can safely:

1. Prepare per-species prompt modifiers.
2. Prepare review packets for `pickup_ball` and `drop_ball`.
3. Review existing contact sheets if provided by the user or code-side thread.
4. Refine stop rules and accept/reject examples.

The visual thread should not generate or import frames until explicitly cleared.
