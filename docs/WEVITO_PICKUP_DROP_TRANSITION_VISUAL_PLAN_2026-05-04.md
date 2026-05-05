# Wevito Pickup/Drop Transition Visual Plan

Updated: 2026-05-04

This is a visual-side planning document for future `pickup_ball` and
`drop_ball` improvements. It does not request generation, import, sprite edits,
or runtime code changes.

Use this after the first `goose / baby / female / blue / hold_ball` pilot has
been accepted. The hold pose defines the contact endpoint; pickup and drop are
the short transition rows that make the ball travel to and from that endpoint.

Use this alongside:

- `docs/WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md`
- `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md`
- `docs/WEVITO_GRIP_POSE_BATCH_PLAN_2026-05-04.md`
- `docs/WEVITO_ANIMATION_QA_RUBRIC.md`
- `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md`

## Boundary

This thread owns visual planning only.

Do not use this document as permission to:

- generate new sprite frames
- overwrite files in `sprites_runtime`
- edit `incoming_sprites`
- change Godot scripts
- change vNext source code
- run packaged audits or long screenshot harnesses
- touch the Sprite Workflow App being built by the other thread

The next visual action is review and planning, not production.

## Why This Exists

The current first visual target is a better `hold_ball` row for:

```text
goose / baby / female / blue / hold_ball
```

That row is the endpoint. Once it is accepted, `pickup_ball` and `drop_ball`
need to make the ball arrive at and leave that same endpoint without a visual
pop.

```text
pickup_ball

  ground ball        reaching / lowering        grip endpoint
┌──────────────┐     ┌──────────────────┐     ┌───────────────┐
│ ball is low  │ ──▶ │ body leans or     │ ──▶ │ ball reaches   │
│ and readable │     │ head lowers       │     │ hold contact   │
└──────────────┘     └──────────────────┘     └───────────────┘

drop_ball

  grip endpoint      release motion             ground ball
┌───────────────┐    ┌──────────────────┐     ┌──────────────┐
│ ball starts   │ ─▶ │ ball separates    │ ─▶ │ ball settles  │
│ at contact    │    │ from pet          │     │ low/readable  │
└───────────────┘    └──────────────────┘     └──────────────┘
```

The failure mode is different from `hold_ball`: hold fails when the ball floats
or the grip anatomy is wrong; pickup/drop fail when the prop teleports, vanishes,
or appears to belong to a different animation family.

## Current Evidence

Target inspected:

```text
sprites_runtime/goose/baby/female/blue
```

File-size snapshot:

| Family | Frame | Bytes |
| --- | --- | ---: |
| `pickup_ball` | `00` | 4306 |
| `pickup_ball` | `01` | 4132 |
| `pickup_ball` | `02` | 4197 |
| `pickup_ball` | `03` | 4281 |
| `play_ball` | `00` | 4306 |
| `play_ball` | `01` | 4132 |
| `play_ball` | `02` | 4197 |
| `play_ball` | `03` | 4281 |
| `play_ball` | `04` | 4309 |
| `play_ball` | `05` | 3758 |
| `drop_ball` | `00` | 4410 |
| `drop_ball` | `01` | 2953 |
| `drop_ball` | `02` | 1596 |
| `drop_ball` | `03` | 3376 |

Hash snapshot:

| File | SHA-256 prefix |
| --- | --- |
| `pickup_ball_00.png` | `BFFF0487CB5A` |
| `pickup_ball_01.png` | `717396E4AFEF` |
| `pickup_ball_02.png` | `3876B02396CB` |
| `pickup_ball_03.png` | `B5E59670BB53` |
| `play_ball_00.png` | `BFFF0487CB5A` |
| `play_ball_01.png` | `717396E4AFEF` |
| `play_ball_02.png` | `3876B02396CB` |
| `play_ball_03.png` | `B5E59670BB53` |
| `play_ball_04.png` | `212BC4C9CD3D` |
| `play_ball_05.png` | `3A2DEA77B222` |
| `drop_ball_00.png` | `1141DA61A8B9` |
| `drop_ball_01.png` | `20E9044D7A38` |
| `drop_ball_02.png` | `DE50C4AA2571` |
| `drop_ball_03.png` | `509CD3B9A9D1` |
| `hold_ball_00.png` | `57AC4BBCD5B4` |
| `hold_ball_01.png` | `4FB5C57BCE7A` |
| `hold_ball_02.png` | `F988F4C46E45` |
| `hold_ball_03.png` | `37AE4B62181E` |

Finding:

```text
pickup_ball_00..03 == play_ball_00..03 by hash
```

Interpretation: the current goose pickup row appears to reuse the first four
frames of `play_ball`. That may be structurally valid, but it is not a true
ground-to-hold transition. A future bespoke pickup row should be judged on
whether it creates readable travel from the ground position to the accepted hold
contact.

`drop_ball` is not byte-identical to pickup/play in this sample, and its middle
frames are much smaller. That does not prove it is wrong; it means the row needs
specific visual review for disappearing ball, over-cropping, or an unclear
release arc.

## Anchor Relationship

Current blue anchor excerpt from `sprites_runtime/_metadata/prop_anchors.json`:

```text
goose / baby / female / blue

  hold_ball
    anchor_norm: x=0.865 y=0.400
    visual read: high/front beak or front-body contact

  pickup_ball and drop_ball
    anchor_norm: x=0.585 y=0.800
    visual read: low/ground ball position
```

The transition problem is the distance between those two reads:

```text
       high/front hold contact
              ▲
              │
              │   visible transition needed
              │
              ▼
       low/ground ball position
```

If the generated art only redraws the pet with a ball at one anchor, it will not
solve pickup/drop. The motion needs a short visual path between anchors.

## Production Order

Do not generate pickup/drop before the hold endpoint has passed review.

```text
┌────────────────────────────┐
│ 1. Accept hold_ball pilot   │
└─────────────┬──────────────┘
              │
              ▼
┌────────────────────────────┐
│ 2. Plan pickup from ground  │
│    to accepted hold point   │
└─────────────┬──────────────┘
              │
              ▼
┌────────────────────────────┐
│ 3. Plan drop from accepted  │
│    hold point to ground     │
└─────────────┬──────────────┘
              │
              ▼
┌────────────────────────────┐
│ 4. Only then consider carry │
│    walk/run continuity      │
└────────────────────────────┘
```

Reason: if the hold contact changes later, pickup/drop have to be re-authored or
re-reviewed against the new endpoint.

## Pickup Visual Target

The improved `pickup_ball` row should read as:

```text
goose notices low ball
  └── lowers/reaches toward it
        └── ball begins to attach to beak/front-body contact
              └── final frame can lead into accepted hold pose
```

Frame intent:

| Frame | Visual job |
| --- | --- |
| `pickup_ball_00` | Ball is low and visible; pet still mostly neutral. |
| `pickup_ball_01` | Head/body begins moving toward ball; no identity drift. |
| `pickup_ball_02` | Contact begins; ball is not yet fully in hold position. |
| `pickup_ball_03` | Ball reaches the accepted hold contact or a clear lead-in pose. |

Acceptance:

- ball travels from low/ground read toward hold contact
- no instant jump from ground to beak
- goose remains baby/female/blue with same silhouette family
- four-frame row loops or exits cleanly without a harsh pop
- row is visually distinct from `play_ball` while still compatible with it
- no new crop, shadow, outline, palette, or alpha artifact

Reject:

- first four frames are just `play_ball` again
- ball appears already held in every frame
- ball appears on ground in every frame
- body becomes a different species, age, or scale
- ball disappears for a frame without a readable motion reason
- final pickup frame cannot plausibly lead into `hold_ball`

## Drop Visual Target

The improved `drop_ball` row should read as:

```text
goose starts with accepted hold contact
  └── releases the ball
        └── ball descends or separates visibly
              └── final frame leaves ball low/ground-readable
```

Frame intent:

| Frame | Visual job |
| --- | --- |
| `drop_ball_00` | Ball begins at or very near accepted hold contact. |
| `drop_ball_01` | Beak/body releases; ball separates but remains readable. |
| `drop_ball_02` | Ball travels downward or outward without vanishing. |
| `drop_ball_03` | Ball is low/ground-readable and compatible with idle/play follow-up. |

Acceptance:

- ball starts from accepted hold contact
- release is visible, not a one-frame disappearance
- final frame reads as ground/low ball
- pet pose remains anatomically plausible
- row can precede idle, play, or pickup without an obvious pop
- no accidental shrink caused by over-tight content boxing

Reject:

- ball is missing in a middle frame without visual explanation
- row reads like the pet is eating or swallowing the ball
- final frame still looks held
- release frame damages face/beak identity
- the pet body collapses into a smaller used rect
- drop row cannot connect back to the low pickup/play anchor

## Future Prompt Drafts

These are future prompt fragments only. Do not run them until the code-side
reliability gates are clear and the hold pilot has been accepted.

Pickup prompt fragment:

```text
Create a 4-frame pickup_ball animation row for Wevito.

Subject: baby female goose, blue variant, matching the provided source exactly.
Canvas: transparent per-frame PNGs, 28x24 final runtime target, no background.
Action: the goose picks up a small ball from a low/ground position and brings it
to the established beak/front-body hold contact. The ball should move visibly
from low/front to the hold point across the 4 frames. Do not reuse play_ball
poses as-is; make this read as a pickup transition.

Keep the goose identity, proportions, outline, palette, and pixel-art style.
No floating prop, no teleporting ball, no missing ball, no crop, no rescale.
```

Drop prompt fragment:

```text
Create a 4-frame drop_ball animation row for Wevito.

Subject: baby female goose, blue variant, matching the provided source exactly.
Canvas: transparent per-frame PNGs, 28x24 final runtime target, no background.
Action: the goose releases a small ball from the established beak/front-body
hold contact and lets it move down toward the low/ground position. The ball
should visibly separate from the beak/body and settle low by the final frame.

Keep the goose identity, proportions, outline, palette, and pixel-art style.
No disappearing prop, no teleporting ball, no swallowed-ball read, no crop,
no rescale.
```

## Review Checklist

Use this after future generation or manual art, before import:

| Check | Pass/Fail | Notes |
| --- | --- | --- |
| Source identity preserved |  |  |
| Species/age/gender/color preserved |  |  |
| Canvas transparent and correctly framed |  |  |
| `pickup_ball` starts low/ground-readable |  |  |
| `pickup_ball` ends near accepted hold contact |  |  |
| `drop_ball` starts near accepted hold contact |  |  |
| `drop_ball` ends low/ground-readable |  |  |
| Ball does not teleport |  |  |
| Ball does not vanish unintentionally |  |  |
| Contact matches accepted `hold_ball` endpoint |  |  |
| Row does not duplicate `play_ball` unless intentional |  |  |
| Contact sheet reviewed |  |  |
| Preview video reviewed |  |  |
| Packaged runtime proof reviewed |  |  |

Decision labels:

| Label | Meaning |
| --- | --- |
| `accept` | Row is ready for import once code-side gates allow it. |
| `revise_prompt` | Visual intent is right, but prompt needs clearer constraints. |
| `revise_art` | Prompt is fine, but the produced frames need manual or generated fixes. |
| `reject` | Row violates identity, canvas, contact, or continuity requirements. |

## Stop Rules

Stop pickup/drop work and return to review if any of these happen:

- the accepted `hold_ball` endpoint changes
- generated pickup/drop art introduces identity drift
- the ball location cannot be explained frame-to-frame
- the row depends on runtime code changes to look correct
- code-side reliability work reports that visual generation/import should wait

## Current Recommendation

Wait.

The visual side has enough information to plan pickup/drop, but not enough
reason to generate them yet. The clean next art sequence remains:

```text
1. accept goose/baby/female/blue hold_ball endpoint
2. generate or hand-author goose pickup_ball against that endpoint
3. generate or hand-author goose drop_ball against that endpoint
4. review all three rows together as one prop-contact set
5. then decide whether carry_ball_walk/run should inherit the same contact
```

This keeps the work small, reviewable, and easy for the code-side thread to
support later.
