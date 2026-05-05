# Wevito Color Palette Quality Review Pass 1

Date: 2026-05-05

Purpose: begin visual-quality review of the six egg-selected animal color variants now that folder coverage is complete.

This is a non-mutating visual review. It does not generate, recolor, import, normalize, or edit sprite PNGs.

## Scope

Reviewed first-priority species index sheets:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\goose-all-age-gender-color-index.png
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\pigeon-all-age-gender-color-index.png
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\frog-all-age-gender-color-index.png
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\raccoon-all-age-gender-color-index.png
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\squirrel-all-age-gender-color-index.png
```

Species reviewed:

```text
1. goose
2. pigeon
3. frog
4. raccoon
5. squirrel
```

## Review Criteria

```text
palette quality
  |
  +-- readable species identity
  +-- readable age/gender silhouette
  +-- six colors visibly distinct enough
  +-- no obvious palette corruption
  +-- no obvious missing/clipped sprite in actual runtime files
  +-- no broad recolor request unless a specific defect is visible
```

## Summary

Pass 1 found no blocker requiring broad recolor or regeneration.

```text
reviewed species
  |
  +-- goose: accept
  +-- pigeon: accept
  +-- frog: accept with note
  +-- raccoon: accept
  +-- squirrel: accept with atlas-display note
```

## Species Findings

| Species | Decision | Notes |
| --- | --- | --- |
| `goose` | accept | All six colors are readable. Goose identity and age/gender silhouette are preserved. Adult blue/indigo/violet variants remain distinct enough. |
| `pigeon` | accept | Strong bird identity across all colors. Blue/indigo/violet are darker but still distinct enough. No visible palette corruption. |
| `frog` | accept with note | Frog identity is strong. Some blue variants read green/teal rather than pure blue, but this works visually for frog skin and is not a blocker. |
| `raccoon` | accept | Mask and body identity survive all colors. Blue/indigo/violet are distinguishable. No immediate palette repair needed. |
| `squirrel` | accept with atlas-display note | Color variants are readable. The adult male row appears clipped in the index sheet because those actual frames are much taller than the sheet cell; direct runtime inspection of `sprites_runtime\squirrel\adult\male\red\idle_00.png` showed the actual PNG is not clipped. |

## Atlas Display Note

The current species index sheet format can visually clip or hide very tall frames.

Observed example:

```text
squirrel / adult / male / idle
```

Actual runtime PNG inspected:

```text
C:\Users\fishe\Documents\projects\wevito\sprites_runtime\squirrel\adult\male\red\idle_00.png
```

Result:

```text
actual sprite is not clipped
  |
  +-- index sheet cell is too short for this row
  +-- do not treat this as a sprite repair request
  +-- future atlas generator should use taller dynamic rows
```

## Repair Queue From Pass 1

```text
none
```

## Warnings / Follow-Up

| Item | Severity | Action |
| --- | --- | --- |
| Frog blue variants lean green/teal | low | Accept unless user wants stricter literal color identity. |
| Squirrel adult male index display clips in atlas | low | Fix atlas sheet layout later; do not edit sprites. |

## Next Review Pass

Continue with:

```text
6. crow
7. fox
8. deer
9. snake
10. rat
```

Also review walk-motion sheets after all species index sheets have been checked, because motion sheets can reveal palette readability issues that idle index sheets miss.

