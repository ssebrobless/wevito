# Wevito Habitat Prop Cleanup Classification

Updated: 2026-05-04

This note records Phase 5/6 of the sprite cleanup campaign: habitat prop
cleanup classification after the first automated proposal pass, followed by the
manual replacement batch that cleaned the confirmed contaminated props.

## Classification Shape

```text
habitat prop cleanup
  |
  +-- fixed now
  |     +-- blanket_mat restored
  |     +-- water_bowl cleaned
  |     +-- hay_bed manually replaced
  |     +-- log_shelter manually replaced
  |     +-- nest_bed manually replaced
  |     +-- moss_bed manually replaced
  |     +-- rock_basking_spot manually replaced
  |     +-- pond_dish manually replaced
  |
  +-- do not auto-clean
  |     +-- dish_set
  |     +-- feeding_plate
  |
  +-- classify before mutation
        +-- remaining large shared item/icon/status flags
```

## Applied Fixes

| Asset | Action |
| --- | --- |
| `sprites_shared_runtime/items/toys_b/blanket_mat.png` | Restored from existing clean source asset. |
| `sprites_shared_runtime/items/containers/water_bowl.png` | Applied safe cleaned source proposal. |
| `sprites_shared_runtime/items/toys_b/hay_bed.png` | Applied manual clean pixel-style replacement; revised to one component / zero detached pixels. |
| `sprites_shared_runtime/items/toys_b/log_shelter.png` | Applied manual clean pixel-style replacement. |
| `sprites_shared_runtime/items/toys_b/nest_bed.png` | Applied manual clean pixel-style replacement. |
| `sprites_shared_runtime/items/toys_b/moss_bed.png` | Applied manual clean pixel-style replacement. |
| `sprites_shared_runtime/items/toys_b/rock_basking_spot.png` | Applied manual clean pixel-style replacement. |
| `sprites_shared_runtime/items/containers/pond_dish.png` | Applied manual clean pixel-style replacement. |

## Rejected Automated Proposals

| Asset | Why rejected |
| --- | --- |
| `hay_bed.png` | Cleaner reduced the prop to sparse outline fragments. |
| `log_shelter.png` | Cleaner introduced/retained checkerboard contamination and lost the prop read. |
| `pond_dish.png` | Cleaner produced an effectively empty/destructive result. |

## Runtime Context

The vNext shell contains clean drawn/hardcoded stage visuals for several habitat
objects, including:

```text
hay_bed
moss_bed
nest_bed
log_shelter
rock_basking_spot
pond_dish
```

Observed references:

```text
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/HabitatLoadoutResolver.cs
vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs
```

Visual implication:

```text
dirty shared PNG
  |
  +-- may still matter for Godot/shared item surfaces
  |
  +-- may not be the primary vNext stage visual
        +-- do not spend broad cleanup effort until usage is confirmed
```

## Current Decision

```text
fixed
  -> blanket_mat
  -> water_bowl
  -> hay_bed
  -> log_shelter
  -> nest_bed
  -> moss_bed
  -> rock_basking_spot
  -> pond_dish

deferred/manual
  -> dish_set
  -> feeding_plate
```

## Next Safe Work

The next safe visual work is not broad automatic PNG cleanup. It is one of:

1. code-side usage confirmation for which shared habitat PNGs are actually
   shown in Godot and vNext surfaces,
2. manual classification of `dish_set` and `feeding_plate`, or
3. shared icon/status cleanup for obvious contaminated generated assets, or
4. returning to optional-animation planning using the accepted goose
   `hold_ball` endpoint.

Phase 6 proof artifacts:

```text
vnext/artifacts/visual-review/20260504-habitat-prop-manual-cleanup-phase6/
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- qa/habitat-prop-manual-cleanup-before-after.png
  +-- manifest.json
  +-- phase6-summary.md
  +-- hay-bed-revision.json
```
