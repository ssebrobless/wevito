# Wevito Care, Medicine, Object Review Packet

Date: 2026-05-05

Purpose: review the current shared item/object art pool for care, medicine, food, water, toys, habitat objects, and utility props, then define what remains visually/UI-wise.

This is a visual-side review packet. It does not authorize code edits, content edits, sprite edits, runtime/source PNG mutation, new generation, import, or asset-prep builds.

## Review Shape

```text
shared item art
  |
  +-- care / medicine
  +-- containers
  +-- food groups
  +-- toys and enrichment
  +-- habitat/rest objects
  +-- utility dressing
  |
  v
review result
  |
  +-- art mostly exists and is clean
  +-- many objects are not first-class gameplay/content records yet
  +-- next work is mapping and UI use, not broad repainting
```

## Review Artifacts

Generated packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-care-medicine-object-review\
  +-- item-asset-inventory.json
  +-- run-summary.md
  +-- qa\
        +-- care-medicine-assets.png
        +-- all-shared-item-assets.png
        +-- items-care.png
        +-- items-containers.png
        +-- items-food_birds.png
        +-- items-food_herbivore.png
        +-- items-food_omnivore.png
        +-- items-food_predator.png
        +-- items-toys_a.png
        +-- items-toys_b.png
        +-- items-utility.png
```

No source/runtime PNGs were modified by this review packet.

## Current Art Inventory

`sprites_shared_runtime/items` currently contains:

```text
9 care assets
9 container assets
9 bird food assets
9 herbivore food assets
9 omnivore food assets
9 predator/reptile food assets
9 toys_a assets
9 toys_b assets
9 utility assets
---
81 item/object PNGs total
```

## Current Content Inventory

`vnext/content/items.json` currently exposes seven umbrella content records:

| Content id | Display name | Category | Notes |
| --- | --- | --- | --- |
| `food-omnivore-scavenger` | Omnivore Food | food | broad diet item |
| `food-herbivore-grazer` | Grazer Food | food | broad diet item |
| `food-birds-seed` | Seed Mix | food | broad diet item |
| `food-predator-reptile` | Protein Tray | food | broad diet item |
| `water-bowl` | Water Bowl | water | one water item |
| `care-medicine` | Medicine Kit | care | broad medicine item |
| `care-doctor` | Doctor Call | care | service/action item |

Interpretation:

```text
art pool is broad
  |
  +-- 81 existing item/object visuals
  |
  v
content model is narrow
  |
  +-- 7 exposed content records
```

This is not an art failure. It is a future content/UI mapping task.

## Care And Medicine Findings

Care sheet:

```text
vnext\artifacts\visual-review\20260505-care-medicine-object-review\qa\care-medicine-assets.png
```

| Asset | Visual status | Recommended role |
| --- | --- | --- |
| `bandage_roll` | clean, readable | injury care / first aid |
| `first_aid_kit` | clean, strong red medical read | medicine kit / doctor aid |
| `grooming_brush` | clean, readable | grooming |
| `medicine_dropper` | clean, thin but distinct | liquid medicine |
| `pill_bottle` | clean, readable | pill medicine |
| `soap_bottle` | clean, readable | bath/cleanliness |
| `syringe` | clean, readable | vaccine/injection/doctor |
| `thermometer` | clean, thin but distinct | diagnosis/sick check |
| `towel` | clean, readable | bath/recovery comfort |

Care/medicine conclusion:

- No broad repaint is needed.
- The icons are clean enough for current UI use.
- `medicine_dropper` and `thermometer` should be checked in tiny UI sizes because they are narrow.
- `first_aid_kit`, `pill_bottle`, `soap_bottle`, and `towel` are especially readable.
- The next gap is mapping each care item to gameplay/status use.

## Food And Container Findings

The food/container pool is visually broad enough for species-specific feeding:

```text
containers
  +-- dish_set
  +-- feeding_plate
  +-- hanging_feeder
  +-- pond_dish
  +-- seed_tray
  +-- shallow_water_dish
  +-- storage_jar
  +-- treat_cup
  +-- water_bowl

diet art
  +-- birds
  +-- herbivore
  +-- omnivore
  +-- predator/reptile
```

Current content records use broad diet labels, not individual item art.

Recommended next mapping:

| Species group | Primary food visuals | Water/container visuals |
| --- | --- | --- |
| birds | `seed_pile`, `mixed_grain`, `bird_bowl`, `hanging_feeder_mix` | `seed_tray`, `hanging_feeder`, `shallow_water_dish` |
| herbivores | `leafy_greens`, `hay_bundle`, `clover_bunch`, `root_slice` | `water_bowl`, `pond_dish` when appropriate |
| omnivores | `bread_crumbs`, `snack_bowl`, `nut_pile`, `sliced_apple` | `water_bowl`, `dish_set` |
| predators/reptiles | `protein_bowl`, `reptile_tray`, `fish`, `bug_cup` | `shallow_water_dish`, `pond_dish` |

Potential visual caution:

- some food art is intentionally more detailed and larger than the small care icons
- these should be used as habitat/stage objects or item cards, not tiny toolbar icons

## Habitat And Utility Findings

The habitat object pool is strong enough to support species loadouts:

```text
rest / bed
  +-- blanket_mat
  +-- hay_bed
  +-- moss_bed
  +-- nest_bed
  +-- cloth_mat

hide / shelter
  +-- crate_hideout
  +-- log_shelter
  +-- tunnel_hide

perch / bask
  +-- branch_perch
  +-- stump_perch
  +-- rock_basking_spot

decor / utility
  +-- lantern
  +-- moss_patch
  +-- pebble_cluster
  +-- seed_sack
  +-- stick_bundle
  +-- storage_basket
  +-- wooden_sign
```

Most of these are not yet first-class gameplay objects. They should be mapped before any renderer/object-zone work.

Recommended first species loadout review remains:

```text
1. goose / Pond Bank
2. rat / Scrap Nest
3. crow / Perch Roost
4. snake / Warm Stone
5. frog / Reed Puddle
```

## Remaining Work

```text
next object/care phase
  |
  +-- visual-side
  |     +-- review category sheets
  |     +-- classify tiny-size readability
  |     +-- create first species loadout mockups
  |     +-- identify content mapping gaps
  |
  +-- code-side later
        +-- decide first-class content ids
        +-- implement object zones
        +-- implement depth/occlusion
        +-- implement item selection UI
```

## Recommended Content Mapping Work

Create a future `item_visual_mapping` or equivalent content layer that can describe:

```text
item_visual_mapping
  |
  +-- content_item_id
  +-- visual_asset_id
  +-- species_ids
  +-- category
  +-- role
  +-- default_surface
  +-- small_icon_safe
  +-- habitat_object_safe
```

This would let broad content records like `food-herbivore-grazer` choose a specific visual such as `hay_bundle`, `leafy_greens`, or `herbivore_bowl` depending on species/environment.

## Acceptance Criteria For This Lane

- Care/medicine art remains clean and readable.
- Every care asset has an intended gameplay/status role.
- First-class content mappings are explicit.
- Habitat loadouts use existing assets before requesting new generation.
- Small UI icons and large habitat objects are not forced into the same role.
- Object zones/depth/occlusion are planned before renderer work.

