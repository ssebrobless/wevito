# Wevito All-Animal Color Variant Coverage

Updated: 2026-05-05

This pass verifies the six egg-selected color variants for every animal in the
runtime tree.

It does not generate art, recolor frames, import sprites, normalize canvases, or
mutate runtime/source PNGs.

## Color Variant Shape

```text
animals
  |
  +-- 10 species
  |     +-- rat
  |     +-- crow
  |     +-- fox
  |     +-- snake
  |     +-- deer
  |     +-- frog
  |     +-- pigeon
  |     +-- raccoon
  |     +-- squirrel
  |     +-- goose
  |
  +-- 3 ages
  |     +-- baby
  |     +-- teen
  |     +-- adult
  |
  +-- 2 genders
  |     +-- female
  |     +-- male
  |
  +-- 6 colors
        +-- red
        +-- orange
        +-- yellow
        +-- blue
        +-- indigo
        +-- violet
```

## Runtime Coverage Result

```text
expected runtime color folders: 360
actual runtime color folders: 360
missing runtime color folders: 0
frame count errors: 0
```

Interpretation:

```text
the runtime variants already exist
  |
  +-- do not recreate color folders
  +-- do not broad-recolor without a defect
  +-- review palette quality with contact sheets
```

The runtime frame count is complete for every species, age, gender, and color
variant across the expected base and optional animation families.

## Authored And Legacy Coverage Result

After the follow-up pass, the animal color-variant folder coverage is complete
across the active and legacy animal roots:

```text
sprites_runtime:            360 / 360 age-gender-color folders present
sprites_authored:           360 / 360 age-gender-color folders present
sprites_authored_verified:  360 / 360 age-gender-color folders present
sprites:                    360 / 360 age-gender-color folders present
```

The only folder gap found was in the legacy `sprites/` tree:

```text
sprites/rat/baby/male/orange
sprites/rat/teen/male/orange
sprites/rat/adult/male/orange
```

These three folders were created from the matching complete authored rows:

```text
sprites_authored/rat/baby/male/orange
sprites_authored/rat/teen/male/orange
sprites_authored/rat/adult/male/orange
```

Artifact:

```text
vnext/artifacts/visual-review/20260505-legacy-sprites-rat-orange-male-fill/
  +-- manifest.json
  +-- run-summary.md
```

This follow-up did not modify `sprites_runtime`, `sprites_authored`,
`sprites_authored_verified`, prop anchors, optional animation rows, or shared
runtime assets.

## Portrait Coverage Result

Portrait variants are also complete:

```text
sprites_shared_runtime/portraits: 420 / 420 present
sprites/portraits:                420 / 420 present
missing portrait variants:        0
```

The `420` portrait files represent:

```text
10 species
  x 3 ages
  x 2 genders
  x 7 portrait forms
      |
      +-- base portrait
      +-- red
      +-- orange
      +-- yellow
      +-- blue
      +-- indigo
      +-- violet
```

## QA Atlas

Artifact packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\
  +-- color-variant-coverage.json
  +-- color-variant-coverage.md
  +-- qa\
```

QA sheets generated:

```text
60 six-color identity sheets
60 six-color walk-motion sheets
10 species idle index sheets
```

Sheet folder:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\
```

Example species index:

```text
vnext/artifacts/visual-review/20260505-all-animal-color-variant-coverage/qa/goose-all-age-gender-color-index.png
```

## Mixed-Canvas Warnings

The coverage audit recorded mixed-canvas warnings:

```text
mixed-canvas warnings: 2610
```

These are not missing color variants. They are existing sequence/frame geometry
differences already known from the canvas-normalization discussions.

Do not treat this number as a request to crop or shrink animals. Any future
normalization must preserve natural motion, extended poses, wings, legs, tails,
and species silhouettes.

## What Is Done

```text
color variant creation status
  |
  +-- runtime variants exist for all animals
  +-- authored variants exist for all animals
  +-- legacy sprites age/gender/color folders now exist for all animals
  +-- runtime frame counts are complete
  +-- source/shared portrait variants exist
  +-- QA atlas exists for visual review
```

## What Remains

The remaining work is quality review, not coverage creation:

```text
next color work
  |
  +-- review atlas sheets by species group
  +-- flag colors that harm identity/readability
  +-- repair only specific palette defects
  +-- do not broad-recolor all animals
```

Recommended first review order:

```text
1. goose
2. pigeon
3. frog
4. raccoon
5. squirrel
6. remaining species by visual risk
```

This keeps color review aligned with the current goose optional-animation pilot
without blocking the whole project on broad palette work.
