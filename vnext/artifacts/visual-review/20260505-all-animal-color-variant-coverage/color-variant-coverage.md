# All-Animal Color Variant Coverage

Status: non-mutating coverage audit and QA atlas.

```text
color variants
  |
  +-- species: 10
  +-- ages: 3
  +-- genders: 2
  +-- colors: 6
  |
  +-- expected color folders: 360
  +-- actual color folders: 360
  +-- missing color folders: 0
  +-- frame count errors: 0
  +-- mixed-canvas warnings: 2610
```

## Color Set

```text
red -> orange -> yellow -> blue -> indigo -> violet
```

## QA Atlas

```text
vnext/artifacts/visual-review/20260505-all-animal-color-variant-coverage/qa/
  +-- 60 six-color identity sheets
  +-- 60 six-color walk-motion sheets
  +-- 10 species idle index sheets
```

## Interpretation

If `missing color folders` and `frame count errors` are zero, the runtime color variants already exist for every animal, age, and gender. The next job is visual quality review, not recreating folders.

Mixed-canvas warnings are planning context only and should not be treated as an instruction to crop or shrink animals.
