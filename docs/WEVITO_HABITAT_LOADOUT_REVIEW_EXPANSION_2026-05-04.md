# Wevito Habitat Loadout Review Expansion

Updated: 2026-05-04

This is Phase 5 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It expands the habitat loadout plan into visual readiness, placeability, and
future zone/depth guidance.

It does not authorize runtime code changes, content changes, sprite edits,
scene placement, object scaling, new generation, import, or runtime PNG
mutation.

## Boundary

```text
Phase 5
  |
  +-- score first species spread
  +-- create second species spread
  +-- classify placeable props vs broad habitat boards
  +-- define future object-zone needs
  +-- define future depth/occlusion needs
  |
  +-- no scene placement in runtime
  +-- no object-zone implementation
  +-- no habitat asset edits
  +-- no new habitat generation
```

## Review Artifacts

```text
vnext/artifacts/visual-review/20260504-habitat-loadout-expansion/
  +-- habitat-loadout-second-spread-review-sheet.png
  +-- habitat-object-placeability-review-sheet.png
  +-- habitat-zone-depth-cue-review-sheet.png
  +-- habitat-loadout-expansion-summary.md
  +-- manifest.json
```

These are non-mutating review artifacts. The sheets use scaled/cropped review
thumbnails only. Source/runtime assets are unchanged.

## Species Loadout Readiness

| Species | Environment | Status | Notes |
| --- | --- | --- | --- |
| `goose` | Pond Bank | `accept_with_zone_work` | Good pond/nest/pebble direction; water-edge contact zones needed. |
| `rat` | Scrap Nest | `warning` | Strong clutter identity; crate/tunnel/storage assets are broad boards and need occlusion rules. |
| `crow` | Perch Roost | `accept_with_zone_work` | Perch/seed/mirror/bell direction works; perch anchors are required. |
| `snake` | Warm Stone | `accept_with_zone_work` | Basking/warm-ground direction works; long-body anchor and low occlusion rules are required. |
| `frog` | Reed Puddle | `accept_with_zone_work` | Wet low-ground direction works; waterline and moss contact zones are required. |
| `deer` | Meadow Edge | `accept_with_zone_work` | Hay/leaf/water/meadow direction works; larger body clearance must be preserved. |
| `pigeon` | Rooftop Ledge | `accept_with_zone_work` | Nest/stump/seed/mirror/bell direction works; ledge/perch anchors are required. |
| `raccoon` | Crate Hideout | `warning` | Loadout identity is good, but `blanket_mat` is empty and crate/storage assets need hide occlusion. |
| `squirrel` | Branch Litter | `accept_with_zone_work` | Nest/stump/nut/rope/chew direction works; mix of perch and ground zones needed. |
| `fox` | Den Brush | `warning` | Den/play direction is good, but `blanket_mat` is empty and fox plate/log shelter need scale review. |

## Placeability Summary

The main habitat gap is not the number of assets. The main gap is knowing which
assets are clean props and which are broad scene boards.

| Class | Count | Assets |
| --- | ---: | --- |
| `clean_prop` | 2 | `rock_basking_spot`, `ball` |
| `placeable_prop` | 9 | `hanging_feeder`, `pond_dish`, `shallow_water_dish`, `treat_cup`, `water_bowl`, `bell_toy`, `rope_toy`, `nut_pile`, `flower_petals` |
| `large_placeable` | 17 | `hay_bed`, `moss_bed`, `nest_bed`, `log_shelter`, `branch_perch`, `stump_perch`, `seed_tray`, `digging_tray`, `leaf_pile`, `mirror_trinket`, `food_crate`, `moss_patch`, `pebble_cluster`, `wooden_sign`, `bird_bowl`, `clover_bunch`, `shiny_reward` |
| `collage_or_board` | 16 | `cloth_mat`, `crate_hideout`, `tunnel_hide`, `dish_set`, `feeding_plate`, `storage_jar`, `chew_toy`, `lantern`, `seed_sack`, `stick_bundle`, `storage_basket`, `snack_bowl`, `fox_plate`, `reptile_tray`, `herbivore_bowl`, `bug_treat` |
| `invalid_empty` | 1 | `blanket_mat` |

## Important Finding: Blanket Mat

`blanket_mat.png` appears to have no visible alpha in the inspected runtime
asset.

```text
asset: sprites_shared_runtime/items/toys_b/blanket_mat.png
dimensions: 2x2
visible alpha: none
affected loadouts: raccoon, fox
status: invalid_empty
```

Do not rely on `blanket_mat` as a real rest object until it is replaced,
recovered, or intentionally removed from the loadouts.

Possible no-generation fallback choices:

| Species | Current | Temporary visual fallback |
| --- | --- | --- |
| `raccoon` | `blanket_mat` | `cloth_mat` if treated as a floor board, or `nest_bed` if a smaller bed is preferred. |
| `fox` | `blanket_mat` | `hay_bed` for warm ground rest, or `moss_bed` if the den should feel softer/natural. |

## First Future Zone Pilots

Start future object-zone work with simple props before broad collage boards.

```text
best first zone pilots
  |
  +-- ball
  |     +-- clean play prop
  |     +-- already used by goose/raccoon/fox
  |
  +-- water_bowl
  |     +-- medium drink prop
  |     +-- used by rat/deer/raccoon/squirrel/fox
  |
  +-- pond_dish
  |     +-- water-edge drink prop
  |     +-- useful for goose/frog
  |
  +-- rock_basking_spot
        +-- clean rest/bask prop
        +-- useful for snake
```

Avoid starting with `crate_hideout`, `tunnel_hide`, `snack_bowl`,
`fox_plate`, `reptile_tray`, or `herbivore_bowl` because those assets behave
more like wide boards/collages than simple single props.

## Object-Zone Requirements

Future code-side/content work should represent object zones explicitly.

| Field | Visual requirement |
| --- | --- |
| `asset_id` | Exact shared asset id, not just a display label. |
| `role` | `rest`, `hide`, `perch`, `bask`, `eat`, `drink`, `play`, `decor`, or `memorial`. |
| `species_ids` | Which species can use the object naturally. |
| `environment_ids` | Which room/environment can host it. |
| `screen_position` | Default placement in habitat coordinates. |
| `depth_band` | Whether it sits behind, under, beside, or in front of the pet. |
| `interaction_zone` | A rectangle/shape where the pet contacts or uses it. |
| `pet_anchor_hint` | Suggested pet foot/body/contact anchor. |
| `occlusion_mode` | Whether pet can go behind/inside/front-only. |
| `contact_shadow` | Whether object/pet needs a shadow blend. |

## Depth And Occlusion Guidance

| Zone type | Depth band | Examples | Notes |
| --- | --- | --- | --- |
| `floor_rest` | `ground_contact` | `hay_bed`, `moss_bed`, `nest_bed`, `rock_basking_spot` | Pet should sit/sleep on or just above the object. |
| `drink_low` | `ground_contact` | `water_bowl`, `pond_dish`, `shallow_water_dish` | Pet mouth/body anchor must line up with rim/waterline. |
| `eat_low` | `pet_interactive` | `seed_tray`, `bird_bowl`, `nut_pile` | Food should not obscure face unless intentionally near-mouth. |
| `perch` | `pet_interactive` | `branch_perch`, `stump_perch` | Needs perch anchor, especially for birds/squirrel. |
| `hide` | `near_occluder` | `crate_hideout`, `log_shelter`, `tunnel_hide` | Requires front/back occlusion split or explicit front-only use. |
| `large_decor` | `far_prop` | `seed_sack`, `stick_bundle`, `storage_basket`, `lantern` | Should usually sit behind or outside pet travel lane. |

## Generation Decision

Do not generate new habitat assets yet.

Exception to discuss later:

```text
blanket_mat
  -> likely missing/empty
  -> affects raccoon and fox rest loadouts
  -> can be solved by fallback remapping first
  -> generation only if no existing mat/bed fallback is accepted
```

## Phase 5 Status

```text
Phase 5: complete
species loadouts: directionally ready for future zone planning
main blocker: placeability and depth/occlusion, not missing art
confirmed asset issue: blanket_mat is invalid/empty
new generation needed now: no
asset mutation approved: no
runtime/content changes approved: no
next visual phase: Phase 6 goose prop-contact decision packet
```
