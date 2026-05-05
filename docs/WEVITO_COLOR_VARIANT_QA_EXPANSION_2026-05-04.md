# Wevito Color Variant QA Expansion

Updated: 2026-05-04

This is Phase 2 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It records the expanded no-edit color QA pass for goose variants.

It does not authorize recolor, sprite edits, source-board edits, generation, or
runtime code changes.

## Artifact Folder

```text
vnext/artifacts/visual-review/20260504-color-variant-expansion/
```

Artifacts:

```text
color-variant-expansion-overview.png
goose-baby-male-six-color-identity-sheet.png
goose-baby-male-six-color-walk-sheet.png
goose-teen-female-six-color-identity-sheet.png
goose-teen-female-six-color-walk-sheet.png
goose-adult-female-six-color-identity-sheet.png
goose-adult-female-six-color-walk-sheet.png
color-variant-expansion-summary.md
manifest.json
```

## Scope

Targets reviewed:

| Target | Colors | PNG count per color |
| --- | --- | ---: |
| `goose / baby / male` | red, orange, yellow, blue, indigo, violet | 64 |
| `goose / teen / female` | red, orange, yellow, blue, indigo, violet | 64 |
| `goose / adult / female` | red, orange, yellow, blue, indigo, violet | 64 |

Sheets created:

```text
identity sheet
  -> idle_00
  -> happy_00
  -> sad_00
  -> sleep_00
  -> sick_00
  -> bathe_00

walk sheet
  -> walk_00..05
```

All sheets place existing runtime PNGs without resizing. Source/runtime
dimensions are unchanged.

## Triage

| Target | Status | Notes |
| --- | --- | --- |
| `goose / baby / male` | `accept_review` | Six colors read clearly, including a blue `blue` variant. |
| `goose / teen / female` | `warning` | Most colors read distinctly, but `blue` reads gray/tan rather than blue. |
| `goose / adult / female` | `warning` | Same concern as teen female: `blue` reads gray/tan rather than blue. |
| Indigo/violet separation | `accept_review` with watch | Better separation on teen/adult sheets than the first baby female sheet, but both remain dark. |
| Yellow/olive read | `warning` | Yellow leans olive/green; may be acceptable for goose, but should be reviewed against egg-color expectations. |

## Main Finding

```text
goose baby color variants
  -> look structurally and visually aligned with egg color vocabulary

goose teen/adult female blue
  -> does not strongly read as blue
  -> likely needs palette review before any broad color cleanup or final egg-color approval
```

This is a palette QA concern, not a missing-file concern.

## Decision

Phase 2 can continue later, but the first expansion pass found enough to create
a concrete palette concern:

```text
palette warning
  target: goose / teen female / blue
  target: goose / adult female / blue
  issue: hue_wrong / low_blue_identity
  action: track for future palette cleanup, no recolor yet
```

Do not generate or recolor yet. The next useful review action is to determine
whether this is goose-specific, age-stage-specific, or a broader problem.

## Recommended Next Color QA Targets

Continue no-edit review with:

```text
pigeon / baby / female / all six colors
frog / baby / female / all six colors
raccoon / baby / female / all six colors
squirrel / baby / female / all six colors
```

Reason:

- pigeon checks another beak/bird profile
- frog checks a low/central body profile
- raccoon and squirrel check marking-heavy mammal profiles
- this helps decide whether the color issue is goose-specific

## Stop Rules

Do not proceed to palette production if:

- `blue` fails to read as blue in multiple species/age stages
- recolor workflow would alter canvas, alpha, or silhouette
- source identity becomes weaker after palette adjustment
- code-side gates for asset proof are not ready

## Phase 2 Status

```text
Phase 2 initial goose expansion: complete
Phase 2 broader species expansion: pending
Next recommended phase per current plan: Phase 3 canvas policy packet
Alternative if user wants more color QA first: continue Phase 2 with pigeon/frog/raccoon/squirrel
```
