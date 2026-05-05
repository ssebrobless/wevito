# Wevito Carry Ball Continuity Visual Plan

Updated: 2026-05-04

This is a visual-side planning document for future `carry_ball_walk` and
`carry_ball_run` improvements. It does not request generation, import, sprite
edits, runtime code changes, or build/test runs.

Use this only after the first prop-contact set is understood:

```text
hold_ball endpoint
  -> pickup_ball transition into endpoint
  -> drop_ball transition out of endpoint
  -> carry_ball_walk/run continuity around endpoint
```

Use this alongside:

- `docs/WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md`
- `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md`
- `docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md`
- `docs/WEVITO_GRIP_POSE_BATCH_PLAN_2026-05-04.md`
- `docs/WEVITO_ANIMATION_QA_RUBRIC.md`
- `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md`

## Boundary

This thread owns visual planning only.

Do not use this document as permission to:

- generate new sprite frames
- overwrite `sprites_runtime`
- edit `incoming_sprites`
- modify Godot scripts
- modify vNext source
- run sprite audits, packaged sweeps, or screenshot harnesses
- touch the Sprite Workflow App being built by the other thread

Carry work should wait until the code-side reliability gates are clear and the
hold endpoint has passed visual review.

## Current Target

Initial continuity target:

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Source color | `blue` |
| Families | `carry_ball_walk`, `carry_ball_run` |
| Frame count | 6 each |
| Runtime folder | `sprites_runtime/goose/baby/female/blue` |

Why this target:

- It builds directly on the current goose hold-ball pilot.
- The high/front hold anchor makes contact drift easy to spot.
- Six-frame locomotion rows expose jitter and attachment problems more clearly
  than four-frame static rows.

## Visual Problem

Carry rows are not just locomotion plus a ball. They need to keep the prop
visually attached to the same grip point while the body moves.

```text
wrong read

  body motion:  frame 00   frame 01   frame 02   frame 03
                 │          │          │          │
  ball motion:   ●       ●        ●       ●
                 └──── ball drifts independently from contact

right read

  body motion:  frame 00   frame 01   frame 02   frame 03
                 │          │          │          │
  ball motion:   ●          ●          ●          ●
                 └──── ball stays locked to beak/front-body contact
```

The review question is not "is there a ball?" It is "does the ball remain
attached to the accepted hold contact while the pet walks or runs?"

## Current Evidence

Target inspected:

```text
sprites_runtime/goose/baby/female/blue
```

File-size snapshot:

| Family | Frame | Bytes |
| --- | --- | ---: |
| `walk` | `00` | 3303 |
| `walk` | `01` | 3213 |
| `walk` | `02` | 3163 |
| `walk` | `03` | 3271 |
| `walk` | `04` | 3112 |
| `walk` | `05` | 3157 |
| `carry_ball_walk` | `00` | 3362 |
| `carry_ball_walk` | `01` | 3274 |
| `carry_ball_walk` | `02` | 3281 |
| `carry_ball_walk` | `03` | 3332 |
| `carry_ball_walk` | `04` | 3171 |
| `carry_ball_walk` | `05` | 3220 |
| `carry_ball_run` | `00` | 3395 |
| `carry_ball_run` | `01` | 3309 |
| `carry_ball_run` | `02` | 3343 |
| `carry_ball_run` | `03` | 3340 |
| `carry_ball_run` | `04` | 3185 |
| `carry_ball_run` | `05` | 3268 |

Hash snapshot:

| File | SHA-256 prefix |
| --- | --- |
| `walk_00.png` | `FA159AEEB49D` |
| `walk_01.png` | `9C55542A9CCC` |
| `walk_02.png` | `9A2EB84FFA8B` |
| `walk_03.png` | `46ABCD96C693` |
| `walk_04.png` | `C2AF4CE415E7` |
| `walk_05.png` | `345C00EB991D` |
| `carry_ball_walk_00.png` | `4B6C113C55D1` |
| `carry_ball_walk_01.png` | `0866FD671462` |
| `carry_ball_walk_02.png` | `0CE8D962A97D` |
| `carry_ball_walk_03.png` | `DB239085270B` |
| `carry_ball_walk_04.png` | `BF6B9DB7E473` |
| `carry_ball_walk_05.png` | `878208E8E2F7` |
| `carry_ball_run_00.png` | `2683A8DBBBF3` |
| `carry_ball_run_01.png` | `2DB44C1FDE69` |
| `carry_ball_run_02.png` | `74D71AAABB1B` |
| `carry_ball_run_03.png` | `8E308C230C2C` |
| `carry_ball_run_04.png` | `1086C1BBAC9F` |
| `carry_ball_run_05.png` | `5A0C4AF9A23E` |

Finding:

```text
carry_ball_walk != walk by hash
carry_ball_run exists as a six-frame row
no plain run_00..05 row was present in the inspected target folder
```

Interpretation: the current carry rows are not byte-identical to the plain walk
row, which is encouraging. The future visual review should focus less on
duplicate detection and more on contact stability, motion readability, and
whether carry-run reads as intentional faster locomotion despite lacking a local
plain-run row for direct comparison.

## Anchor Relationship

Current blue anchor excerpt from `sprites_runtime/_metadata/prop_anchors.json`:

```text
goose / baby / female / blue

  hold_ball
    anchor_norm: x=0.865 y=0.400
    scale: 0.426
    z_index: 12

  carry_ball_walk
    anchor_norm: x=0.865 y=0.400
    offset_px: x=0 y=2
    scale: 0.426
    z_index: 12

  carry_ball_run
    anchor_norm: x=0.865 y=0.400
    offset_px: x=2 y=1
    scale: 0.426
    z_index: 12
```

Visual meaning:

```text
hold_ball contact
     │
     ├── carry_ball_walk: same contact, slight low offset
     │
     └── carry_ball_run: same contact, slight forward/low offset
```

The carry families should inherit the accepted hold contact. Offsets can imply
motion and weight, but they should not make the ball look detached.

## Carry Walk Target

The improved `carry_ball_walk` row should read as a calm walk while holding the
ball at the accepted beak/front-body contact.

Frame intent:

| Frame | Visual job |
| --- | --- |
| `carry_ball_walk_00` | Neutral carry pose; ball visibly attached. |
| `carry_ball_walk_01` | Gentle body step; ball follows contact point. |
| `carry_ball_walk_02` | Step reaches high/low variation; no prop lag. |
| `carry_ball_walk_03` | Opposite step or return; contact remains true. |
| `carry_ball_walk_04` | Secondary step variation; no silhouette damage. |
| `carry_ball_walk_05` | Clean loop back to frame `00`. |

Acceptance:

- ball remains attached to the same contact point across all frames
- gait reads slower and calmer than carry-run
- body keeps goose baby proportions and blue variant identity
- no frame feels like a static hold row with only the prop moving
- no frame feels like plain walk with a floating prop pasted on top
- loop from frame `05` to `00` is visually comfortable

Reject:

- ball jitters independently from the beak/front body
- ball floats higher/lower than the accepted contact without motion reason
- walk body loses the accepted hold pose entirely
- one frame has a visibly different scale or used-rect feel
- row becomes too expressive and reads as play rather than carry
- final frame pops when looping back to frame `00`

## Carry Run Target

The improved `carry_ball_run` row should read as faster movement while the ball
is still held.

Frame intent:

| Frame | Visual job |
| --- | --- |
| `carry_ball_run_00` | Forward-leaning carry pose; contact established. |
| `carry_ball_run_01` | Faster stride begins; ball remains locked to contact. |
| `carry_ball_run_02` | Peak stride or bounce; ball may trail slightly but not detach. |
| `carry_ball_run_03` | Opposite stride; contact remains readable. |
| `carry_ball_run_04` | Recovery frame; no prop disappearance. |
| `carry_ball_run_05` | Clean loop or lead-in to frame `00`. |

Acceptance:

- run reads faster than walk through pose, lean, or stride spacing
- ball follows the accepted contact with only minor motion offset
- no ball teleport between adjacent frames
- no face/beak distortion caused by trying to show speed
- six-frame loop feels continuous
- row can transition from `hold_ball` without changing the grip logic

Reject:

- ball appears to be thrown rather than carried
- ball trails far enough to look detached
- run looks identical to walk except for filenames
- pet identity shifts to a different body shape
- speed lines, blur, or effects break the pixel-art style
- one frame hides the ball behind the body without clear reason

## Continuity Grid

Use this grid to review the whole prop-contact set together.

| From | To | Expected visual relationship |
| --- | --- | --- |
| `pickup_ball_03` | `hold_ball_00` | Pickup lands at the accepted hold contact. |
| `hold_ball_00` | `carry_ball_walk_00` | Same grip; walk begins without prop pop. |
| `hold_ball_00` | `carry_ball_run_00` | Same grip; run begins with only motion offset. |
| `carry_ball_walk_05` | `carry_ball_walk_00` | Walk loops cleanly. |
| `carry_ball_run_05` | `carry_ball_run_00` | Run loops cleanly. |
| `carry_ball_walk` | `carry_ball_run` | Same hold logic, different movement energy. |
| `carry_ball_walk/run` | `drop_ball_00` | Drop starts from the same hold contact. |

Review shape:

```text
              ┌─────────────┐
              │ hold_ball   │
              └──────┬──────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌─────────────────┐       ┌─────────────────┐
│ carry_ball_walk │       │ carry_ball_run  │
└────────┬────────┘       └────────┬────────┘
         │                         │
         └────────────┬────────────┘
                      ▼
              ┌─────────────┐
              │ drop_ball   │
              └─────────────┘
```

## Future Prompt Drafts

These are future prompt fragments only. Do not run them until code-side
reliability gates are clear and the hold/pickup/drop visual targets are settled.

Carry-walk prompt fragment:

```text
Create a 6-frame carry_ball_walk animation row for Wevito.

Subject: baby female goose, blue variant, matching the provided source exactly.
Canvas: transparent per-frame PNGs, 28x24 final runtime target, no background.
Action: the goose walks calmly while carrying a small ball at the established
beak/front-body hold contact. The ball should stay attached to that contact as
the body steps through the walk cycle. Add only subtle motion offset; do not let
the ball float or jitter independently.

Keep identity, proportions, outline, palette, and pixel-art style. No crop,
no rescale, no detached prop, no play-ball posing.
```

Carry-run prompt fragment:

```text
Create a 6-frame carry_ball_run animation row for Wevito.

Subject: baby female goose, blue variant, matching the provided source exactly.
Canvas: transparent per-frame PNGs, 28x24 final runtime target, no background.
Action: the goose runs while carrying a small ball at the established
beak/front-body hold contact. The run should read faster than carry_ball_walk
through body lean and stride energy, while the ball remains visibly attached
with only a slight forward or downward motion offset.

Keep identity, proportions, outline, palette, and pixel-art style. No speed
effects, no crop, no rescale, no detached prop, no thrown-ball read.
```

## Review Checklist

Use this after future generation or manual art, before import:

| Check | Pass/Fail | Notes |
| --- | --- | --- |
| Source identity preserved |  |  |
| Species/age/gender/color preserved |  |  |
| Canvas transparent and correctly framed |  |  |
| Carry contact matches accepted `hold_ball` |  |  |
| Ball remains attached across walk frames |  |  |
| Ball remains attached across run frames |  |  |
| Walk reads slower/calmer than run |  |  |
| Run reads faster without breaking style |  |  |
| Walk loop frame `05 -> 00` is clean |  |  |
| Run loop frame `05 -> 00` is clean |  |  |
| No prop jitter or teleporting |  |  |
| No frame-level identity drift |  |  |
| Contact sheet reviewed |  |  |
| Preview video reviewed |  |  |
| Packaged runtime proof reviewed |  |  |

Decision labels:

| Label | Meaning |
| --- | --- |
| `accept` | Row is ready for import once code-side gates allow it. |
| `revise_prompt` | Visual intent is right, but prompt needs clearer constraints. |
| `revise_art` | Prompt is fine, but frames need manual or generated fixes. |
| `reject` | Row violates identity, canvas, contact, loop, or continuity rules. |

## Stop Rules

Stop carry work and return to review if any of these happen:

- the accepted hold endpoint changes
- pickup/drop reveal a different contact path than expected
- generated carry art only moves the ball and not the body
- generated carry art only moves the body and leaves a pasted-on prop
- carry-run requires a runtime movement change to read correctly
- code-side reliability work says generation/import should wait

## Current Recommendation

Wait.

The current carry rows are structurally present and not byte-identical to plain
walk. That means this is probably not the first visual repair to spend generation
budget on. The safer order is:

```text
1. accept goose hold_ball contact endpoint
2. solve pickup/drop transitions against that endpoint
3. review existing carry walk/run against the accepted endpoint
4. only regenerate carry if contact drift or loop pop is visible
```

Carry is important, but it should inherit decisions made by the smaller
hold/pickup/drop set instead of forcing those decisions early.
