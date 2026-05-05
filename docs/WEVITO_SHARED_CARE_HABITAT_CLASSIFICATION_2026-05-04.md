# Wevito Shared Care/Habitat Classification

Updated: 2026-05-04

This is the first no-edit classification packet from
`docs/WEVITO_SPRITE_NOISE_CLEANUP_COURSE_OF_ACTION_2026-05-04.md`.

It classifies six shared runtime assets that were flagged by the game-facing
sprite noise checklist. No sprite PNGs were modified.

## Review Shape

```text
shared care/habitat packet
  |
  +-- medicine/care
  |     +-- medicine icon
  |     +-- syringe
  |     +-- thermometer
  |
  +-- habitat/rest/container
        +-- blanket mat
        +-- water bowl
        +-- pond dish
```

Review artifacts:

```text
vnext/artifacts/visual-review/20260504-shared-care-habitat-classification/
  +-- shared-care-habitat-classification-sheet.png
  +-- classification-summary.md
  +-- classification.json
  +-- medicine-icon-classification-preview.png
  +-- syringe-classification-preview.png
  +-- thermometer-classification-preview.png
  +-- blanket-mat-classification-preview.png
  +-- water-bowl-classification-preview.png
  +-- pond-dish-classification-preview.png
```

## Decisions

| Asset | Classification | Decision |
| --- | --- | --- |
| `sprites_shared_runtime/icons/medicine.png` | `true_ui_noise_candidate` | Create a no-edit cleanup proof later; edge touch, low-alpha haze, detached pixels, and pale boundary residue are visible at icon scale. |
| `sprites_shared_runtime/items/care/syringe.png` | `minor_true_noise_candidate` | Candidate for tiny detached-pixel cleanup proof; keep doctor/high-severity use. |
| `sprites_shared_runtime/items/care/thermometer.png` | `intentional_highlight_or_minor_residue_review` | Do not auto-clean yet; pale line may be glass/highlight detail. Review at UI scale before any mask. |
| `sprites_shared_runtime/items/toys_b/blanket_mat.png` | `missing_asset` | Treat as unusable/empty; do not rely on it in habitat loadouts. Remap or replace before use. |
| `sprites_shared_runtime/items/containers/water_bowl.png` | `intentional_multipart_plus_residue_review` | Do not auto-delete detached components; many flags come from interior water/bowl structure. Needs placeability/readability review, not blind cleanup. |
| `sprites_shared_runtime/items/containers/pond_dish.png` | `intentional_multipart_plus_residue_review` | Do not auto-delete detached components; review as a habitat container candidate. Some edge/tile-like fragments may need manual cleanup proof later. |

## Visual Interpretation

```text
safe to classify now
  |
  +-- blanket_mat
  |     +-- genuinely missing/empty
  |     +-- remove from active visual recommendations until replaced/remapped
  |
  +-- medicine icon
  |     +-- strongest small cleanup candidate
  |     +-- likely benefits from clean icon proof
  |
  +-- syringe
  |     +-- tiny detached residue only
  |     +-- good deterministic cleanup proof candidate
  |
  +-- thermometer
  |     +-- probably highlight/detail, not urgent
  |
  +-- water_bowl / pond_dish
        +-- multi-part structures
        +-- need placeable-prop classification before cleanup
```

## Cleanup Queue Impact

Next cleanup proof candidates:

1. `sprites_shared_runtime/icons/medicine.png`
2. `sprites_shared_runtime/items/care/syringe.png`

Next classification candidates:

1. rest/habitat objects: `nest_bed`, `moss_bed`, `hay_bed`, `log_shelter`
2. container objects: `water_bowl`, `pond_dish`, `dish_set`, `feeding_plate`
3. environment boards: `goose`, `squirrel`, `crow`, `night_props`

## Stop Rules

Do not mutate these assets from this classification packet.

```text
allowed next
  -> no-edit cleanup proof for medicine icon
  -> no-edit cleanup proof for syringe
  -> no-edit habitat object classification packet

not allowed
  -> overwrite shared runtime PNGs
  -> auto-delete detached components from bowls
  -> use blanket_mat as an active habitat rest object
  -> generate replacement art without explicit approval
```
