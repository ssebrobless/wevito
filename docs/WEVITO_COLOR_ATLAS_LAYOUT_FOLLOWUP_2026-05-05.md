# Wevito Color Atlas Layout Follow-Up

Date: 2026-05-05

Purpose: close the remaining color-review artifact issue where a few tall sprite rows looked awkward in fixed-height atlas sheets.

This is a non-mutating review pass. It did not edit sprite PNGs, source boards, runtime rows, prop anchors, content files, code, or build outputs.

## Shape

```text
fixed review atlas issue
  |
  +-- tall native frames
  |     +-- squirrel / adult / male / idle_00.png: 142x139
  |     +-- deer / adult / female / idle_00.png: 135x114
  |
  +-- artifact-only fix
        +-- dynamic row height
        +-- native dimensions printed per cell
        +-- no sprite scaling/cropping diagnosis
        +-- no repair queue
```

## Artifact Packet

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-color-atlas-layout-followup\
  |
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- squirrel-dynamic-row-color-index.png
        +-- deer-dynamic-row-color-index.png
```

## Scope

Reviewed `idle_00.png` for:

```text
species
  |
  +-- squirrel
  +-- deer

rows per species
  |
  +-- baby / female
  +-- baby / male
  +-- teen / female
  +-- teen / male
  +-- adult / female
  +-- adult / male

colors per row
  |
  +-- red
  +-- orange
  +-- yellow
  +-- blue
  +-- indigo
  +-- violet
```

Total sampled frames: 72.

## Findings

| Target | Finding | Decision |
| --- | --- | --- |
| `squirrel / adult / male` | Native frame is much taller and wider than most rows. Dynamic review sheet displays it cleanly when row height honors `142x139`. | Accept as sprite; use dynamic-row review artifact. |
| `squirrel / other rows` | Baby/teen rows stay `72x64`; adult female is `78x71`. All display cleanly. | Accept. |
| `deer / adult / female` | Native frame is wider/taller than standard rows. Dynamic review sheet displays it cleanly when row height honors `135x114`. | Accept as sprite; use dynamic-row review artifact. |
| `deer / other rows` | Baby rows are `72x64`, teen female is `63x71`, teen/adult male rows fit their native slots. | Accept. |

## Decision

No sprite repair is recommended.

```text
visual concern
  |
  +-- old fixed-height atlas displayed tall rows awkwardly
  |
  +-- direct dynamic-row review shows:
        +-- sprites are readable
        +-- color variants stay distinct enough
        +-- age/gender silhouette is preserved
        +-- no dirty crop or noise repair is visible
```

The right follow-up is to use dynamic-row layouts for future review sheets that mix old `72x64` rows with larger natural-motion rows.

## Guidance For Future Contact Sheets

Use review-sheet cells that:

- derive row height from the tallest native frame in the row
- center sprites on a neutral checkerboard background
- print native dimensions per cell
- avoid implying that larger natural motion is a defect
- keep runtime PNG dimensions untouched

Do not:

- shrink tall sprites to fit a legacy review slot
- crop review thumbnails in a way that looks like runtime data loss
- treat an atlas layout artifact as a sprite cleanup task
- normalize runtime/source PNGs from the visual-side thread

## Repair Queue

```text
none
```

