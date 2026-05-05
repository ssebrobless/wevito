# Wevito Optional Animation Visual Audit

Updated: 2026-05-04

This is a visual-side audit of optional animation reuse patterns in the current
runtime sprites. It uses read-only hash comparison to find rows that are likely
visually duplicated or derived from another row.

It does not request generation, import, sprite edits, runtime code changes, or
build/test runs.

Scope inspected:

```text
sprites_runtime/{species}/baby/female/blue
```

Species inspected:

```text
rat, crow, fox, snake, deer, frog, pigeon, raccoon, squirrel, goose
```

Use this alongside:

- `docs/WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md`
- `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md`
- `docs/WEVITO_GRIP_POSE_BATCH_PLAN_2026-05-04.md`
- `docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md`
- `docs/WEVITO_CARRY_BALL_CONTINUITY_VISUAL_PLAN_2026-05-04.md`
- `docs/WEVITO_DRINK_INTERACTION_VISUAL_PLAN_2026-05-04.md`

## Boundary

This thread owns visual planning only.

Do not use this document as permission to:

- generate new sprite frames
- overwrite `sprites_runtime`
- edit `incoming_sprites`
- modify Godot scripts
- modify vNext source
- run packaged audits or screenshot harnesses
- touch the Sprite Workflow App being built by the other thread

This audit is a prioritization map, not an implementation request.

## Audit Shape

The goal is to separate high-value visual work from rows that may already be
distinct enough to leave alone for now.

```text
optional families
  │
  ├── obvious reuse by hash
  │     └── high-priority visual improvement candidates
  │
  ├── distinct by hash but still contact-sensitive
  │     └── review before regenerating
  │
  └── distinct by hash and lower immediate risk
        └── defer unless visual review finds a concrete problem
```

## Hash Checks Used

These checks are intentionally simple. They do not prove visual quality; they
only identify exact file reuse.

| Check | Meaning |
| --- | --- |
| `hold_ball_00..03 == idle_00..03` | Hold row is likely an idle clone. |
| `pickup_ball_00..03 == play_ball_00..03` | Pickup row likely reuses early play frames. |
| `drop_ball_00..03 == pickup_ball_00..03` | Drop row likely reuses pickup frames. |
| `carry_ball_walk_00..05 == walk_00..05` | Carry-walk likely reuses plain walk. |
| `drink_00..03 == idle_00..03` | Drink row likely reuses idle. |
| `run_00..05 exists` | Whether carry-run has a direct plain-run comparison row. |

## Audit Matrix

Legend:

| Mark | Meaning |
| --- | --- |
| `CLONE` | Exact hash match across the compared rows. |
| `distinct` | Not an exact hash match. |
| `missing base` | Direct comparison row was not present. |

| Species | Hold vs idle | Pickup vs play first 4 | Drop vs pickup | Carry-walk vs walk | Drink vs idle | Plain run row |
| --- | --- | --- | --- | --- | --- | --- |
| `rat` | distinct | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `crow` | distinct | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `fox` | distinct | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `snake` | distinct | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `deer` | distinct | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `frog` | `CLONE` | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `pigeon` | `CLONE` | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `raccoon` | `CLONE` | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `squirrel` | `CLONE` | `CLONE` | `CLONE` | distinct | distinct | missing base |
| `goose` | `CLONE` | `CLONE` | distinct | distinct | distinct | missing base |

## Findings

Highest-confidence reuse:

```text
pickup_ball is reused from play_ball first four frames
  └── all 10 inspected species

drop_ball is reused from pickup_ball
  └── 9 of 10 inspected species

hold_ball is reused from idle
  └── frog, pigeon, raccoon, squirrel, goose
```

Lower immediate reuse concern:

```text
drink is distinct from idle
  └── all 10 inspected species

carry_ball_walk is distinct from walk
  └── all 10 inspected species

carry_ball_run exists, but no plain run row exists in this inspected slice
  └── judge by visual readability, not direct hash comparison
```

## Visual Priority

The audit suggests this priority shape:

```text
highest value
  │
  ├── pickup_ball
  │     └── cloned from play for every inspected species
  │
  ├── drop_ball
  │     └── cloned from pickup for nearly every inspected species
  │
  ├── hold_ball for idle-clone species
  │     └── frog, pigeon, raccoon, squirrel, goose
  │
  ├── carry_ball_walk/run
  │     └── review contact and loop continuity before regenerating
  │
  └── drink
        └── defer unless visual review finds target/background issues
```

This slightly adjusts the earlier goose-first plan:

```text
old visual assumption
  hold_ball may be the main visual gap

new audit-informed view
  pickup/drop reuse is the broadest visual gap
  hold_ball is still the best endpoint pilot
```

The two ideas can coexist. `hold_ball` is still the safest first pilot because
pickup/drop need a grip endpoint. But once that endpoint is accepted, pickup/drop
should probably receive visual attention before carry or drink.

## Recommended Pilot Path

Start with one complete prop-contact set, not broad generation.

```text
goose / baby / female / blue

  1. hold_ball
       purpose: establish accepted beak/front-body contact

  2. pickup_ball
       purpose: replace play clone with ground-to-contact transition

  3. drop_ball
       purpose: confirm release from contact back to low/ground target

  4. carry_ball_walk/run
       purpose: verify contact stability during movement

  5. drink
       purpose: inspect only after higher-priority ball-contact work
```

Why goose still works:

- It is one of the `hold_ball == idle` species.
- It has a clear high/front contact point.
- It is the one inspected species where `drop_ball != pickup_ball`, giving us a
  useful comparison between cloned and distinct transition behavior.
- The previous pilot docs already define its source paths, anchors, and visual
  target.

## Batch Path After Goose

Do not expand to every color, age, and gender first. Expand by visual failure
type.

```text
batch 1: hold endpoint repair
  goose
  frog
  pigeon
  raccoon
  squirrel

batch 2: pickup transition repair
  all species, one controlled demographic/color first

batch 3: drop transition repair
  all species where drop == pickup

batch 4: carry continuity review
  only regenerate rows with visible contact drift or loop pop

batch 5: drink review
  only regenerate rows with oversized targets, background artifacts, or poor
  mouth/beak alignment
```

## Species Notes

Hold endpoint candidates:

| Species | Why it matters |
| --- | --- |
| `goose` | Clear beak/front-body contact; current pilot target. |
| `pigeon` | Beak contact similar to goose; useful second beak test. |
| `frog` | Central/low body contact; tests non-beak anatomy. |
| `raccoon` | Paw/mouth ambiguity; tests small forelimb support. |
| `squirrel` | Paw/front-body hold; tests tucked close prop read. |

Pickup/drop candidates:

| Species group | Why it matters |
| --- | --- |
| Beak/muzzle species | Need visible movement from low target to face contact. |
| Paw/body-support species | Need support motion without human-hand poses. |
| Long/low-body species | Need transition without collapsing silhouette. |

## Review Rules

Hash reuse is a triage signal, not a verdict.

Before replacing any row:

- inspect contact sheets
- inspect preview videos
- compare against source identity
- compare against accepted endpoint contact
- confirm row purpose is visually different from the cloned source
- avoid regenerating rows that are distinct and already readable

Reject any future visual output that:

- fixes cloning but introduces identity drift
- improves one frame while breaking the loop
- changes color or age read
- creates background or floor artifacts
- relies on runtime code changes to explain the action
- cannot pass the QA rubric in `docs/WEVITO_ANIMATION_QA_RUBRIC.md`

## Current Recommendation

Wait on generation, but use this audit to choose the first visual target once the
code-side reliability gates are clear.

The strongest next visual production sequence is:

```text
1. goose hold_ball endpoint
2. goose pickup_ball transition
3. goose drop_ball transition
4. review goose carry_ball_walk/run against the endpoint
5. expand endpoint repair to pigeon/frog/raccoon/squirrel
6. expand pickup/drop repair by grip type
```

This keeps the next work small while still addressing the broadest visual reuse
patterns found in the current runtime sprites.
