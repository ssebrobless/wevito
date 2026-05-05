# Wevito Color Variant QA Plan

Updated: 2026-05-04

This plan defines how to verify Wevito's six egg-selected color variants before
any palette cleanup, recolor, or new visual generation begins.

It is a docs-only QA plan. It does not request sprite edits, generated art,
runtime code changes, or build/test runs.

## Goal

```text
egg color choice
  -> red / orange / yellow / blue / indigo / violet
  -> pet runtime folder
  -> consistent readable palette across all frames
  -> same species / age / gender identity
```

The runtime folder structure exists. The remaining question is whether the
variants look intentional, consistent, and readable.

## Canonical Color Set

Color ids:

```text
red
orange
yellow
blue
indigo
violet
```

These ids appear in every species record in `vnext/content/species.json` and in
the runtime folder structure:

```text
sprites_runtime/<species>/<age>/<gender>/<color>/
```

## Current Structural Evidence

Full runtime variant shape checked during the visual planning pass:

| Check | Result |
| --- | ---: |
| Expected species/age/gender/color folders | 360 |
| Missing folders | 0 |
| Colors per species | 36 folders |

First QA target checked:

| Target | PNG count |
| --- | ---: |
| `goose/baby/female/red` | 64 |
| `goose/baby/female/orange` | 64 |
| `goose/baby/female/yellow` | 64 |
| `goose/baby/female/blue` | 64 |
| `goose/baby/female/indigo` | 64 |
| `goose/baby/female/violet` | 64 |

Interpretation:

```text
the color folders and frame volume exist
  -> do not recreate folders
  -> do not assume missing variants
  -> verify palette quality and consistency instead
```

## First QA Target

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Colors | red, orange, yellow, blue, indigo, violet |
| Reason | Same target family as the current visual pilot; beak/body contrast makes palette problems obvious. |

Why not start with all species:

```text
one target
  -> define scoring language
  -> find actual defect types
  -> decide whether tools/reports are enough
  -> then expand

all targets at once
  -> too many screenshots
  -> weak review
  -> easy to miss subtle identity drift
```

## Required Contact Sheets

No asset edits are needed for this plan. The desired QA output is a set of
contact sheets once code/tool ownership is clear.

### Sheet 1 - Identity Row

Purpose: compare the six colors on the most identity-stable frames.

Recommended frames:

```text
idle_00
happy_00
sad_00
sleep_00
sick_00
bathe_00
```

Layout:

```text
columns: red | orange | yellow | blue | indigo | violet
rows:    idle | happy | sad | sleep | sick | bathe
```

Review focus:

- species identity
- age read
- gender/source side preservation
- body/beak/marking contrast
- outline readability
- status expression readability

### Sheet 2 - Motion Consistency Row

Purpose: compare a short animation family across all colors.

Recommended frames:

```text
walk_00..05
```

Layout:

```text
one row per color
six frames per row
```

Review focus:

- no hue flicker across frames
- no frame with different saturation/brightness
- no outline or marking loss during motion
- no canvas/crop anomaly obvious in a color variant

### Sheet 3 - Optional Family Spot Check

Purpose: verify palette behavior in the first optional family target.

Recommended frames:

```text
hold_ball_00..03
pickup_ball_00..03
drop_ball_00..03
```

Layout:

```text
section: hold_ball
  columns: red | orange | yellow | blue | indigo | violet
  frames:  00..03

section: pickup_ball
  columns: red | orange | yellow | blue | indigo | violet
  frames:  00..03

section: drop_ball
  columns: red | orange | yellow | blue | indigo | violet
  frames:  00..03
```

Review focus:

- prop remains readable against each body color
- ball contact is not hidden by dark or saturated palettes
- pickup/drop reuse problems remain visible for review
- no color-specific background artifact appears

## Pass / Warning / Fail

Use three levels so the review does not overreact to tiny differences.

| Level | Meaning | Action |
| --- | --- | --- |
| `pass` | Color reads clearly and preserves identity. | Keep. |
| `warning` | Minor palette issue but pet remains readable. | Track for cleanup queue. |
| `fail` | Palette damages identity, readability, or frame consistency. | Block broad expansion and plan repair. |

## Defect Categories

| Defect | Level | Description |
| --- | --- | --- |
| `hue_wrong` | warning/fail | Color does not read as the named egg color. |
| `too_dark` | warning/fail | Body or markings collapse into outline/shadow. |
| `too_bright` | warning/fail | Body overpowers face/markings or looks neon. |
| `low_contrast` | warning/fail | Important anatomy becomes hard to distinguish. |
| `marking_loss` | fail | Species-specific marks disappear. |
| `outline_loss` | fail | Pet boundary becomes unclear against background/UI. |
| `frame_flicker` | fail | Palette changes across animation frames. |
| `status_unreadable` | warning/fail | Sad/sick/sleep expressions become hard to read. |
| `prop_blend` | warning/fail | Ball, water, food, or care prop blends into body color. |
| `identity_drift` | fail | Recolor makes the pet read like a different species/age/gender. |

## Scoring Form

Use one row per reviewed target.

| Target | Color | Result | Defects | Notes |
| --- | --- | --- | --- | --- |
| `goose/baby/female` | red |  |  |  |
| `goose/baby/female` | orange |  |  |  |
| `goose/baby/female` | yellow |  |  |  |
| `goose/baby/female` | blue |  |  |  |
| `goose/baby/female` | indigo |  |  |  |
| `goose/baby/female` | violet |  |  |  |

## Expansion Order

After the first target has a scoring language:

```text
1. goose / baby / female
2. goose / baby / male
3. goose / teen / female
4. goose / adult / female
5. pigeon / baby / female
6. frog / baby / female
7. raccoon / baby / female
8. squirrel / baby / female
9. remaining species by grip/shape group
```

Reason:

- goose aligns with the current optional-animation pilot
- pigeon checks a similar beak profile
- frog checks low/central body color behavior
- raccoon/squirrel check marking-heavy small mammal palettes
- broad expansion waits until the scoring rubric is stable

## Palette Repair Rules

Do not repair color variants yet. When repair becomes allowed, follow these
rules:

```text
repair only the palette
  -> do not change pose
  -> do not change canvas
  -> do not change alpha
  -> do not change frame count
  -> do not change species markings unless the source palette is wrong
```

Preferred repair approach:

| Step | Requirement |
| --- | --- |
| 1 | Record source paths and hashes. |
| 2 | Create before/after contact sheet. |
| 3 | Apply palette adjustment to one family first. |
| 4 | Review against all six colors. |
| 5 | Expand only if no identity/canvas/crop issues appear. |

## Stop Rules

Stop palette work if:

- any repair changes canvas size
- any repair changes alpha silhouette
- any repair makes age/gender/species less clear
- a color-specific frame flicker appears
- review cannot distinguish intended color from background/environment
- code-side reliability gates say visual production should wait

## Current Recommendation

Proceed with no-edit QA only:

```text
next action
  -> create or request contact sheets for goose/baby/female all six colors

not yet
  -> no broad recolor
  -> no source-board repaint
  -> no generated replacements
```

The structural color variant work is on track. The next job is proving that the
variants look good enough to carry the egg-selection identity of the game.
