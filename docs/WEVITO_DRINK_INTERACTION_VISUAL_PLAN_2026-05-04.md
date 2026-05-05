# Wevito Drink Interaction Visual Plan

Updated: 2026-05-04

This is a visual-side planning document for future `drink` improvements. It
does not request generation, import, sprite edits, runtime code changes, or
build/test runs.

Use this after the ball-contact pilot work is understood, or when a separate
drink-focused visual pass becomes necessary.

Use this alongside:

- `docs/WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md`
- `docs/WEVITO_ANIMATION_QA_RUBRIC.md`
- `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md`
- `docs/WEVITO_WORK_COMPANION_STATES.md`

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

Drink work should wait unless there is a clear visual reason to prioritize it.

## Why Drink Is Different

Ball families are prop-contact rows. Drink is an environmental-contact row.

```text
ball families

  pet body ──▶ carried / held / picked-up prop

drink

  pet body ──▶ lowered face / mouth / beak ──▶ low water-or-bowl target
```

The visual question is not whether the pet is holding something. The question is
whether the face/head/body orientation makes sense against a low drink target
without changing identity or introducing a large distracting object.

## Current Target

Initial drink review target:

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Source color | `blue` |
| Family | `drink` |
| Frame count | 4 |
| Runtime folder | `sprites_runtime/goose/baby/female/blue` |

Why this target:

- It keeps the visual review aligned with the current goose pilot.
- A beak-based drink motion has a clear low-contact read.
- The row is only four frames, so contact and identity problems are easy to
  inspect.

## Current Evidence

Target inspected:

```text
sprites_runtime/goose/baby/female/blue
```

File-size snapshot:

| Family | Frame | Bytes |
| --- | --- | ---: |
| `drink` | `00` | 9940 |
| `drink` | `01` | 9773 |
| `drink` | `02` | 9614 |
| `drink` | `03` | 10101 |

Hash snapshot:

| File | SHA-256 prefix |
| --- | --- |
| `idle_00.png` | `57AC4BBCD5B4` |
| `idle_01.png` | `4FB5C57BCE7A` |
| `idle_02.png` | `F988F4C46E45` |
| `idle_03.png` | `37AE4B62181E` |
| `drink_00.png` | `A7F5AF05FB25` |
| `drink_01.png` | `7A23F95D2C56` |
| `drink_02.png` | `FFBFB346D89E` |
| `drink_03.png` | `AAC21E6DB54D` |
| `happy_00.png` | `FA159AEEB49D` |
| `happy_01.png` | `0439B3608F06` |
| `happy_02.png` | `F45CFDD1BF9B` |
| `happy_03.png` | `06340482184F` |

Finding:

```text
drink_00..03 are not byte-identical to idle_00..03 or happy_00..03
drink file sizes are much larger than the nearby idle/happy examples
```

Interpretation: the current goose drink row appears to contain materially
different image content. It should not be treated as an obvious clone problem.
The first review should focus on whether the larger content is useful drink
readability or a symptom of an oversized target, background artifact, crop, or
unwanted visual noise.

## Anchor Pattern

Current blue anchor excerpt from `sprites_runtime/_metadata/prop_anchors.json`:

```text
goose / baby / female / blue

  drink
    anchor_norm: x=0.585 y=0.840
    target: water_or_bowl
    offset_px: x=0 y=-10
    scale: 0.234
    z_index: 9
```

Baby/female/blue drink anchors by species:

| Species | Anchor x | Anchor y | Offset | Scale | Target |
| --- | ---: | ---: | --- | ---: | --- |
| `rat` | 0.605 | 0.820 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `crow` | 0.585 | 0.800 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `fox` | 0.605 | 0.820 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `snake` | 0.605 | 0.860 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `deer` | 0.605 | 0.820 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `frog` | 0.545 | 0.820 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `pigeon` | 0.565 | 0.800 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `raccoon` | 0.585 | 0.820 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `squirrel` | 0.565 | 0.780 | `(0,-10)` | 0.234 | `water_or_bowl` |
| `goose` | 0.585 | 0.840 | `(0,-10)` | 0.234 | `water_or_bowl` |

Visual meaning:

```text
       normal pet body
             │
             ▼
   lowered head / beak / mouth
             │
             ▼
      low water-or-bowl target
```

The anchor pattern is intentionally low and fairly consistent. Species variation
mostly changes the horizontal contact point and how far down the head/mouth/beak
should reach.

## Drink Visual Target

The improved `drink` row should read as a small pet leaning down to drink from a
low target while preserving source identity.

Frame intent:

| Frame | Visual job |
| --- | --- |
| `drink_00` | Pet begins from a readable neutral or slight lowered pose. |
| `drink_01` | Head/beak/mouth moves toward low target. |
| `drink_02` | Clear drink contact or sip frame; target remains subtle. |
| `drink_03` | Recovery or loop-friendly drink variation. |

Acceptance:

- pet identity, age, gender, and color are preserved
- head/beak/mouth aligns with low drink target
- water/bowl target is visible enough to explain the action
- water/bowl target does not dominate the sprite
- motion reads as drinking, not eating a ball or picking something up
- four-frame row loops or exits cleanly
- no background, floor patch, shadow block, or stray object appears
- no crop, rescale, outline damage, or palette drift

Reject:

- row reads as idle with an unexplained object
- row reads as pickup/drop rather than drink
- water/bowl object is larger than the pet's head unless intentionally stylized
- target floats away from the mouth/beak contact
- pet bends into an impossible or species-breaking pose
- one frame has a much different canvas used-rect feel
- frame contains background pixels or a base/platform
- drink target occludes the face enough to damage identity

## Species-Specific Drink Reads

Use anatomy-specific verbs when writing future prompts.

| Species group | Drink read | Prompt guidance |
| --- | --- | --- |
| Beak drinkers | beak dips toward low water | Keep beak identity; avoid turning drink into bite/hold. |
| Muzzle drinkers | mouth lowers toward bowl | Keep head shape; avoid stretched neck or human posture. |
| Low-body drinkers | body crouches slightly | Keep silhouette readable; do not squash body too far. |
| Long-body drinkers | head/front curves downward | Preserve body curve; avoid coiling around the target unless intended. |

For the goose pilot, the desired read is:

```text
small goose
  -> beak/front head lowers toward subtle water-or-bowl target
  -> sip/contact frame
  -> recovery or loop frame
```

## Relationship To Work Companion States

Drink can eventually support care and companion moments, but it should stay
visually small.

Possible runtime meaning later:

| Runtime moment | Drink visual role |
| --- | --- |
| low energy recovery | short self-care sip |
| idle variety | quiet environment interaction |
| waiting state | calm low-motion loop |
| failed/retry recovery | gentle reset after a stressful action |

Phase boundary:

```text
visual planning now
  └── define drink readability and QA criteria

runtime behavior later
  └── decide when the pet chooses drink as a companion state
```

Do not add runtime state or behavior in this visual thread.

## Future Prompt Draft

This is a future prompt fragment only. Do not run it until code-side reliability
gates are clear and a drink pass is explicitly chosen.

```text
Create a 4-frame drink animation row for Wevito.

Subject: baby female goose, blue variant, matching the provided source exactly.
Canvas: transparent per-frame PNGs, 28x24 final runtime target, no background.
Action: the goose lowers its beak/head toward a small subtle water-or-bowl target
and takes a sip. The drink target should sit low and close to the beak contact
point. It should explain the action without dominating the sprite.

Keep identity, proportions, outline, palette, and pixel-art style. No crop,
no rescale, no floor/background patch, no oversized bowl, no ball-holding read.
```

## Review Checklist

Use this after future generation or manual art, before import:

| Check | Pass/Fail | Notes |
| --- | --- | --- |
| Source identity preserved |  |  |
| Species/age/gender/color preserved |  |  |
| Canvas transparent and correctly framed |  |  |
| Head/beak/mouth reaches low target |  |  |
| Drink target is subtle and readable |  |  |
| No background or platform pixels |  |  |
| No crop or scale drift |  |  |
| Row reads as drink, not pickup/drop/play |  |  |
| Four-frame loop or exit is clean |  |  |
| Contact sheet reviewed |  |  |
| Preview video reviewed |  |  |
| Packaged runtime proof reviewed |  |  |

Decision labels:

| Label | Meaning |
| --- | --- |
| `accept` | Row is ready for import once code-side gates allow it. |
| `revise_prompt` | Visual intent is right, but prompt needs clearer constraints. |
| `revise_art` | Prompt is fine, but frames need manual or generated fixes. |
| `reject` | Row violates identity, canvas, contact, target, or continuity rules. |

## Stop Rules

Stop drink work and return to review if any of these happen:

- generated drink art includes a background, floor, or platform
- target object dominates the pet
- pet identity drifts while lowering the head
- row needs runtime code changes to explain the action
- drink review starts competing with the higher-priority ball-contact pilot
- code-side reliability work says generation/import should wait

## Current Recommendation

Wait.

The goose drink row is not an obvious clone and should not outrank the ball
contact pilot. The safer visual order remains:

```text
1. accept goose hold_ball endpoint
2. solve pickup/drop around that endpoint
3. review carry continuity
4. inspect drink visually for oversized target or background artifacts
5. regenerate drink only if review finds a clear readability problem
```

Drink may become valuable for companion-state flavor later, but the immediate
visual gain is probably smaller than fixing prop-contact continuity first.
