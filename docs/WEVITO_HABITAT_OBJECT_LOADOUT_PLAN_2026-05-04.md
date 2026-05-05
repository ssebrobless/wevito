# Wevito Habitat Object Loadout Plan

Updated: 2026-05-04

This document maps existing habitat, toy, container, and utility assets to
species environments. It is meant to guide future visual staging and later
runtime object-zone work.

It is docs-only. It does not request renderer changes, content changes, sprite
edits, or new generated assets.

## Goal

```text
species environment
  -> default bed / shelter / perch / container / toy / dressing
  -> believable pet home
  -> later interaction zones
  -> later depth and occlusion rules
```

The immediate task is mapping. Implementation belongs to a later code-side or
content-side slice.

## Current Environment Set

From `vnext/content/environments.json`:

| Species | Environment id | Display name |
| --- | --- | --- |
| `rat` | `rat` | Scrap Nest |
| `crow` | `crow` | Perch Roost |
| `fox` | `fox` | Den Brush |
| `snake` | `snake` | Warm Stone |
| `deer` | `deer` | Meadow Edge |
| `frog` | `frog` | Reed Puddle |
| `pigeon` | `pigeon` | Rooftop Ledge |
| `raccoon` | `raccoon` | Crate Hideout |
| `squirrel` | `squirrel` | Branch Litter |
| `goose` | `goose` | Pond Bank |

Shared environments:

| Environment id | Display name | Use |
| --- | --- | --- |
| `indoor-room` | Indoor Room | Generic fallback/home interior. |
| `night-props` | Calm Night | Night/calm overlay or variant. |

## Existing Object Pool

Habitat-relevant assets already exist in `sprites_shared_runtime/items`.

```text
beds and mats
  blanket_mat
  hay_bed
  moss_bed
  nest_bed
  cloth_mat

shelters and hides
  crate_hideout
  log_shelter
  tunnel_hide

perches and basking
  branch_perch
  stump_perch
  rock_basking_spot

containers
  dish_set
  feeding_plate
  hanging_feeder
  pond_dish
  seed_tray
  shallow_water_dish
  storage_jar
  treat_cup
  water_bowl

toys and enrichment
  ball
  bell_toy
  chew_toy
  digging_tray
  leaf_pile
  mirror_trinket
  rope_toy

utility dressing
  food_crate
  lantern
  moss_patch
  pebble_cluster
  seed_sack
  stick_bundle
  storage_basket
  wooden_sign
```

## Default Loadouts

These are visual recommendations, not content/runtime changes.

| Species | Bed/rest | Shelter/hide | Food/water | Enrichment | Dressing |
| --- | --- | --- | --- | --- | --- |
| `rat` | `cloth_mat` | `crate_hideout` | `snack_bowl`, `water_bowl` | `chew_toy`, `tunnel_hide` | `food_crate`, `storage_basket` |
| `crow` | `nest_bed` | `branch_perch` | `hanging_feeder`, `seed_tray` | `mirror_trinket`, `bell_toy` | `shiny_reward`, `stick_bundle` |
| `fox` | `blanket_mat` | `log_shelter` | `fox_plate`, `water_bowl` | `digging_tray`, `ball` | `leaf_pile`, `stick_bundle` |
| `snake` | `moss_bed` | `rock_basking_spot` | `reptile_tray`, `shallow_water_dish` | `tunnel_hide` | `pebble_cluster`, `moss_patch` |
| `deer` | `hay_bed` | `leaf_pile` | `herbivore_bowl`, `water_bowl` | `bell_toy` | `clover_bunch`, `flower_petals` |
| `frog` | `moss_bed` | `moss_patch` | `pond_dish`, `shallow_water_dish` | `bug_treat`, `leaf_pile` | `pebble_cluster` |
| `pigeon` | `nest_bed` | `stump_perch` | `bird_bowl`, `seed_tray` | `mirror_trinket`, `bell_toy` | `stick_bundle` |
| `raccoon` | `blanket_mat` | `crate_hideout` | `snack_bowl`, `water_bowl` | `ball`, `rope_toy` | `storage_basket`, `food_crate` |
| `squirrel` | `nest_bed` | `stump_perch` | `nut_pile`, `water_bowl` | `chew_toy`, `rope_toy` | `stick_bundle`, `seed_sack` |
| `goose` | `nest_bed` | `moss_patch` | `pond_dish`, `shallow_water_dish` | `ball`, `leaf_pile` | `pebble_cluster` |

Some listed food objects live in food subfolders, not the habitat/toy subfolders.
That is intentional: a believable habitat loadout may combine food, container,
toy, and utility art.

## Object Roles

| Role | Meaning | Examples |
| --- | --- | --- |
| `rest` | Pet can sleep/rest near or on it. | `blanket_mat`, `hay_bed`, `moss_bed`, `nest_bed`, `cloth_mat` |
| `hide` | Pet can tuck behind/inside it. | `crate_hideout`, `log_shelter`, `tunnel_hide` |
| `perch` | Bird/squirrel-like pet can sit on it. | `branch_perch`, `stump_perch` |
| `bask` | Reptile/amphibian can rest on it. | `rock_basking_spot` |
| `eat` | Food interaction point. | `feeding_plate`, `seed_tray`, `bird_bowl`, `snack_bowl` |
| `drink` | Water interaction point. | `water_bowl`, `pond_dish`, `shallow_water_dish` |
| `play` | Enrichment interaction point. | `ball`, `bell_toy`, `rope_toy`, `mirror_trinket` |
| `decor` | Visual dressing only. | `lantern`, `wooden_sign`, `pebble_cluster`, `stick_bundle` |
| `memorial` | Special state object. | `memorial_object` |

## Future Object-Zone Contract

Later runtime/content work should represent object placement and interaction
zones explicitly.

```text
habitat_object
  |
  +-- asset_id
  +-- role
  +-- environment_ids
  +-- species_ids
  +-- screen_position
  +-- depth_band
  +-- occlusion_mode
  +-- interaction_zone
  |     +-- x
  |     +-- y
  |     +-- width
  |     +-- height
  |
  +-- contact_shadow
  +-- pet_anchor_hint
```

The visual plan should define the desired behavior before code implements it.

## Depth Bands

Recommended visual layers:

```text
backdrop
  -> far props
  -> pet shadow
  -> pet body
  -> held/carried prop
  -> near occluders
  -> UI/status overlays
```

Object examples by depth:

| Depth band | Object examples |
| --- | --- |
| `far_prop` | `lantern`, `wooden_sign`, `stick_bundle`, `seed_sack` |
| `ground_contact` | `cloth_mat`, `hay_bed`, `moss_bed`, `pond_dish`, `water_bowl` |
| `pet_interactive` | `ball`, `rope_toy`, `chew_toy`, `feeding_plate` |
| `near_occluder` | `crate_hideout`, `log_shelter`, `tunnel_hide`, tall perch objects |
| `special_overlay` | `memorial_object`, status-specific props |

## First Review Targets

Start with a small species spread:

| Target | Why |
| --- | --- |
| `goose / Pond Bank` | Current visual pilot species; tests pond/water/nest objects. |
| `rat / Scrap Nest` | Tests crate, clutter, and small mammal scale. |
| `crow / Perch Roost` | Tests perch and hanging/seed objects. |
| `snake / Warm Stone` | Tests basking object and long-body grounding. |
| `frog / Reed Puddle` | Tests water and moss scale. |

## Visual QA Criteria

Pass criteria:

- object belongs naturally in the species environment
- object is readable at scene scale
- pet can plausibly stand, sit, sleep, drink, eat, hide, or play near it
- object does not overpower the pet
- object palette does not hide pet color variants
- object has no visible background or matte artifact

Warning criteria:

- object is useful but too large/small for one age stage
- object is visually readable but lacks an obvious interaction point
- object palette is fine for one species but clashes with another
- object might need a shadow/contact adjustment later

Fail criteria:

- object cannot be understood without a label
- object blocks or hides too much of the pet
- object makes the pet look pasted on or floating
- object appears to come from a different art style
- object introduces background/floor artifacts

## What Not To Do Yet

- do not place objects in runtime scenes from this visual thread
- do not edit environment files
- do not scale or repaint habitat objects yet
- do not generate replacement beds/shelters before reviewing current assets
- do not make object zones without code-side ownership

## Current Recommendation

Use existing habitat objects first.

```text
next visual step
  -> review loadout contact sheets / mockups for 5 species

not yet
  -> renderer changes
  -> object-zone implementation
  -> new habitat generation
```

Wevito already has many of the objects it needs. The gap is turning them into
intentional species loadouts and later giving those objects interaction zones.
