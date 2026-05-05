# Wevito Asset Inventory Checklist

Updated: 2026-05-04

This checklist records the current visual asset inventory and the remaining
asset-establishment work for pets, color variants, items, medicine/care assets,
food, water, toys, habitat objects, environments, status visuals, UI icons, and
egg lifecycle art.

It is a docs-only planning artifact. It does not request generation, import,
runtime code changes, sprite edits, or build/test runs.

## Source Shape

```text
Wevito visual assets
  |
  +-- pet sprites
  |   +-- incoming_sprites species/age source boards
  |   +-- sprites_runtime species/age/gender/color animation frames
  |
  +-- shared runtime assets
  |   +-- environment backdrops
  |   +-- portraits
  |   +-- item/object icons
  |   +-- care/medicine assets
  |   +-- food/water/container assets
  |   +-- toys/enrichment/habitat objects
  |   +-- celestial/status/UI assets
  |
  +-- content records
      +-- vnext/content/species.json
      +-- vnext/content/items.json
      +-- vnext/content/environments.json
      +-- vnext/content/conditions.json
```

Important distinction:

```text
asset exists on disk != asset is first-class in gameplay/content
```

The current art inventory is broader than the current item/content inventory.

## Pet Coverage

Canonical species from `vnext/content/species.json` and `sprites_runtime`:

| Species | Source/runtime present | Ages | Genders | Colors |
| --- | --- | --- | --- | --- |
| `rat` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `crow` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `fox` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `snake` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `deer` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `frog` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `pigeon` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `raccoon` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `squirrel` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |
| `goose` | yes | baby, teen, adult | female, male | red, orange, yellow, blue, indigo, violet |

Structural check performed during this planning pass:

```text
expected runtime variant folders: 360
missing runtime variant folders: 0
color folders per species: 36
```

Interpretation:

- The runtime directory shape for color variants is present.
- The color-variant model is still the intended egg-driven model:
  `red`, `orange`, `yellow`, `blue`, `indigo`, `violet`.
- The next visual work is palette-quality verification and regeneration path
  planning, not proving the folders exist.

Color-variant checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Six color folders for every species/age/gender | Present structurally | Verify palette quality visually and by deterministic palette reports. |
| Colors match egg result vocabulary | Present in `species.json` | Confirm egg UI/content uses the same six ids. |
| Variant palettes preserve species identity | Unknown visually | Build contact sheets comparing all six colors per species/age/gender. |
| Variant palettes preserve gender/age silhouette | Unknown visually | Review side-by-side boards before any broad recolor. |
| Palette tool path is documented | Partial | Define safe recolor workflow in the master plan. |

## Pet Animation Coverage

Base runtime contract from `docs/SPRITE_SOURCE_OF_TRUTH.md`:

| Base family | Expected frames |
| --- | ---: |
| `idle` | 4 |
| `walk` | 6 |
| `eat` | 4 |
| `happy` | 4 |
| `sad` | 2 |
| `sleep` | 2 |
| `sick` | 4 |
| `bathe` | 4 |

Optional families from the current optional-animation work:

| Optional family | Expected frames | Visual priority |
| --- | ---: | --- |
| `drink` | 4 | Review later; distinct from idle in inspected sample. |
| `play_ball` | 6 | Baseline for pickup reuse detection. |
| `hold_ball` | 4 | First endpoint pilot. |
| `pickup_ball` | 4 | High priority; cloned from play in inspected sample. |
| `drop_ball` | 4 | High priority; cloned from pickup for most inspected species. |
| `carry_ball_walk` | 6 | Review contact stability before regenerating. |
| `carry_ball_run` | 6 | Review contact stability before regenerating. |

Animation checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Base frames exist in runtime tree | Previously audited as present | Do not mass-edit; diagnose mixed-canvas report first. |
| Optional frames exist in runtime tree | Previously audited as complete | Use visual audit docs for priority. |
| Optional families visually expressive | Partial | Start with goose hold/pickup/drop once code-side gates clear. |
| Contact sheets exist for new runs | Required by new contract | Enforce before importing new art. |
| Preview videos exist for new runs | Required by new contract | Enforce before importing new art. |
| Manifest/provenance exists for new runs | Required by new contract | Enforce before importing new art. |

## Incoming Source Boards

Current supporting source boards found in `incoming_sprites`:

| Source board | Visual category |
| --- | --- |
| `egg-hatch-lifecycle.png` | Egg and hatch lifecycle |
| `environments-A.png` | Environment source art |
| `environments-B.png` | Environment source art |
| `food-birds-seed.png` | Bird food set |
| `food-herbivore-grazer.png` | Herbivore food set |
| `food-omnivore-scavenger.png` | Omnivore food set |
| `food-predator-reptile.png` | Predator/reptile food set |
| `large_action_icons.png` | Large UI/action icons |
| `medicine-care.png` | Medicine and care source art |
| `small_ui_icons.png` | Small UI icons |
| `status-effects.png` | Status effect visuals |
| `sun.png` | Celestial/day visual |
| `moon.png` | Celestial/night visual |
| `toys-enrichment-A.png` | Toys/enrichment source art |
| `toys-enrichment-B.png` | Toys/habitat/shelter source art |
| `utility-shelter-props.png` | Utility and shelter props |
| `water-feeding-containers.png` | Water and feeding containers |

Checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Source boards are preserved | Present | Treat as canonical inputs, not throwaways. |
| Board-to-runtime extraction summary exists | Present in shared runtime summaries | Review summaries before new extraction. |
| Board layout is documented | Partial | Add per-board cell maps before hand-editing. |
| Hatch-style provenance is recorded | Planned | Use generation contract for future derived assets. |

## Shared Runtime Asset Counts

Current `sprites_shared_runtime` counts:

| Category | PNG count | Notes |
| --- | ---: | --- |
| `celestial` | 12 | Sun/moon frames. |
| `environment` | 12 | Ten species environments plus indoor/night variants. |
| `icons` | 21 | UI/action/status icons. |
| `items` | 81 | Food, containers, care, toys, utility/habitat objects. |
| `portraits` | 420 | Pet portraits or portrait variants. |
| `status` | 8 | Status-effect visuals. |

Item subcategory counts:

| Item group | PNG count |
| --- | ---: |
| `care` | 9 |
| `containers` | 9 |
| `food_birds` | 9 |
| `food_herbivore` | 9 |
| `food_omnivore` | 9 |
| `food_predator` | 9 |
| `toys_a` | 9 |
| `toys_b` | 9 |
| `utility` | 9 |

## Care And Medicine Assets

Current care art in `sprites_shared_runtime/items/care`:

| Asset | Likely use |
| --- | --- |
| `bandage_roll` | Injury care, doctor follow-up, first aid |
| `first_aid_kit` | General medicine kit / care action |
| `grooming_brush` | Grooming care |
| `medicine_dropper` | Liquid medicine |
| `pill_bottle` | Pill/tablet medicine |
| `soap_bottle` | Bath/cleanliness |
| `syringe` | Doctor/vaccine/injection care |
| `thermometer` | Sick/diagnosis |
| `towel` | Bath/recovery comfort |

Current content records in `vnext/content/items.json` expose:

| Content item | Icon id | Notes |
| --- | --- | --- |
| `care-medicine` | `medicine` | Generic medicine kit. |
| `care-doctor` | `doctor` | Doctor call action. |

Medicine/care checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Generic medicine icon | Present | Keep as top-level action icon. |
| Generic doctor icon | Present | Keep as top-level action icon. |
| Multiple care/medicine object sprites | Present as art | Decide which become first-class gameplay items. |
| Condition-specific medicine visuals | Not first-class yet | Map condition groups to existing care assets before generating anything. |
| Medicine visual variants are documented | Partial | Add treatment-to-asset mapping plan. |
| Medicine assets pass cleanup/readability review | Unknown | Review icons at actual UI scale and runtime scale. |

Recommended medicine mapping:

| Condition group | Existing visual candidates |
| --- | --- |
| injury / foot problems / joint pain | `bandage_roll`, `first_aid_kit` |
| viral / respiratory / parasites / infection | `medicine_dropper`, `pill_bottle`, `thermometer` |
| doctor-only intervention | `syringe`, `first_aid_kit` |
| hygiene-linked conditions | `soap_bottle`, `towel`, `grooming_brush` |

## Food, Water, And Containers

Content items currently expose:

| Content item | Icon id | Species group |
| --- | --- | --- |
| `food-omnivore-scavenger` | `food_meat` | rat, crow, fox, raccoon |
| `food-herbivore-grazer` | `food_plant` | deer, squirrel, goose |
| `food-birds-seed` | `food_sweet` | crow, pigeon, goose |
| `food-predator-reptile` | `food_meat` | snake, frog, fox |
| `water-bowl` | `water_bowl` | rat, fox, deer, squirrel, raccoon |

Runtime art groups:

| Group | Assets |
| --- | --- |
| `containers` | `dish_set`, `feeding_plate`, `hanging_feeder`, `pond_dish`, `seed_tray`, `shallow_water_dish`, `storage_jar`, `treat_cup`, `water_bowl` |
| `food_birds` | `berry_cluster`, `bird_bowl`, `bug_treat`, `fish_morsel`, `hanging_feeder_mix`, `mixed_grain`, `seed_pile`, `shiny_reward`, `sunflower_seeds` |
| `food_herbivore` | `berry_cluster`, `clover_bunch`, `flower_petals`, `grain_bundle`, `hay_bundle`, `herbivore_bowl`, `leafy_greens`, `pond_greens`, `root_slice` |
| `food_omnivore` | `berry_cluster`, `bread_crumbs`, `egg`, `fish_scrap`, `grain_mix`, `meat_chunk`, `nut_pile`, `sliced_apple`, `snack_bowl` |
| `food_predator` | `bug_cup`, `egg`, `fish`, `fox_plate`, `meat_chunk`, `mouse_prey`, `protein_bowl`, `reptile_tray`, `worm_pile` |

Checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Food groups have source art | Present | Review readability at icon and scene scale. |
| Runtime food item sprites exist | Present | Decide which are gameplay items vs decorative variants. |
| Water containers exist | Present | Map `drink` animation to water/bowl anchors. |
| Species food mapping exists | Present in content | Verify icons match the food group visually. |
| Container object zones exist | Not first-class | Add to habitat/object plan before runtime work. |

## Toys, Enrichment, And Habitat Objects

Current toy and habitat-adjacent assets:

| Group | Assets |
| --- | --- |
| `toys_a` | `ball`, `bell_toy`, `branch_perch`, `chew_toy`, `digging_tray`, `leaf_pile`, `mirror_trinket`, `rope_toy`, `tunnel_hide` |
| `toys_b` | `blanket_mat`, `crate_hideout`, `hay_bed`, `log_shelter`, `memorial_object`, `moss_bed`, `nest_bed`, `rock_basking_spot`, `stump_perch` |
| `utility` | `cloth_mat`, `food_crate`, `lantern`, `moss_patch`, `pebble_cluster`, `seed_sack`, `stick_bundle`, `storage_basket`, `wooden_sign` |

Habitat checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Bed-like assets | Present: `blanket_mat`, `hay_bed`, `moss_bed`, `nest_bed` | Choose species/default mappings. |
| Shelter/hide assets | Present: `crate_hideout`, `log_shelter`, `tunnel_hide` | Define hide/sleep object zones. |
| Perch/basking assets | Present: `branch_perch`, `stump_perch`, `rock_basking_spot` | Define species-specific use rules. |
| Play assets | Present: `ball`, `bell_toy`, `chew_toy`, `rope_toy`, `mirror_trinket` | Map play actions to object categories. |
| Memorial asset | Present | Keep separate from normal habitat props. |
| Habitat depth/occlusion zones | Not first-class yet | Plan before runtime implementation. |
| Pet contact shadows | Partial/renderer-specific | Plan before final polish. |

Recommended species/default habitat mapping:

| Species | Environment | Bed/shelter candidate | Extra object candidate |
| --- | --- | --- | --- |
| `rat` | Scrap Nest | `crate_hideout`, `cloth_mat` | `food_crate`, `storage_basket` |
| `crow` | Perch Roost | `branch_perch`, `stump_perch` | `shiny_reward`, `mirror_trinket` |
| `fox` | Den Brush | `log_shelter`, `blanket_mat` | `digging_tray` |
| `snake` | Warm Stone | `rock_basking_spot`, `moss_bed` | `pebble_cluster` |
| `deer` | Meadow Edge | `hay_bed`, `leaf_pile` | `clover_bunch` |
| `frog` | Reed Puddle | `moss_bed`, `pond_dish` | `moss_patch` |
| `pigeon` | Rooftop Ledge | `nest_bed`, `stump_perch` | `seed_tray` |
| `raccoon` | Crate Hideout | `crate_hideout`, `blanket_mat` | `storage_basket` |
| `squirrel` | Branch Litter | `nest_bed`, `stump_perch` | `nut_pile` |
| `goose` | Pond Bank | `nest_bed`, `pond_dish` | `shallow_water_dish` |

## Environments And Celestial Assets

Current environments from `vnext/content/environments.json`:

| Environment id | Display name | Asset id |
| --- | --- | --- |
| `rat` | Scrap Nest | `rat` |
| `crow` | Perch Roost | `crow` |
| `fox` | Den Brush | `fox` |
| `snake` | Warm Stone | `snake` |
| `deer` | Meadow Edge | `deer` |
| `frog` | Reed Puddle | `frog` |
| `pigeon` | Rooftop Ledge | `pigeon` |
| `raccoon` | Crate Hideout | `raccoon` |
| `squirrel` | Branch Litter | `squirrel` |
| `goose` | Pond Bank | `goose` |
| `indoor-room` | Indoor Room | `indoor_room` |
| `night-props` | Calm Night | `night_props` |

Checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Species default environments exist | Present | Review grounding and contrast with pet sprites. |
| Indoor room exists | Present | Decide role: fallback/home/default room. |
| Night prop variant exists | Present | Confirm day/night transition visuals. |
| Sun/moon assets exist | Present | Review against shell scale and background contrast. |
| Object depth bands exist | Not first-class | Add to habitat visual contract later. |

## Status, Conditions, And UI

Current condition records in `vnext/content/conditions.json`:

| Category | Conditions |
| --- | --- |
| Innate | respiratoryProblems, parasites, dentalProblems, sheddingIssues, jointStiffness, skinInfections, viralSusceptibility, dentalOvergrowth, footProblems |
| Acquired | obesity, malnutrition, depression, anxiety, jointPain, exhaustion, injury |

Visual support:

| Asset source | Current role |
| --- | --- |
| `incoming_sprites/status-effects.png` | Source status effects board. |
| `sprites_shared_runtime/status` | 8 runtime status PNGs. |
| `sprites_shared_runtime/icons` | 21 UI/action icons, including medicine and doctor. |

Checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Status effect source board exists | Present | Map each condition to status icon/effect. |
| Runtime status icons exist | Present | Verify naming and content mapping. |
| Medicine/doctor icons exist | Present | Review at toolbar scale. |
| Condition-specific care visuals exist | Partial | Map to care assets before generating new art. |
| Status overlays avoid identity damage | Unknown | Review overlays against pet colors and ghost/death states. |

## Egg And Hatch Lifecycle

Checklist:

| Requirement | Current status | Next visual action |
| --- | --- | --- |
| Egg hatch source board exists | Present: `incoming_sprites/egg-hatch-lifecycle.png` | Review stages and color egg logic. |
| Six color outcomes are canonical | Present in species content | Confirm egg color visuals match red/orange/yellow/blue/indigo/violet. |
| Runtime hatch assets are first-class | Unknown | Inventory runtime usage before generating new hatch art. |
| Egg-to-palette mapping is documented | Partial | Add visual mapping after code-side confirms hatch flow. |

## Current Gaps

The project is on the right track, but the visual asset establishment work has
three different kinds of gaps:

```text
1. asset art exists but is not first-class content
   examples: individual medicine/care tools, habitat beds, enrichment objects

2. asset folders exist but visual quality is not proven
   examples: six pet color variants, optional ball-contact families

3. content exists but richer visual mapping is incomplete
   examples: conditions -> medicine visuals, species -> habitat object defaults
```

Priority gaps:

| Gap | Why it matters | First safe action |
| --- | --- | --- |
| Color variant quality not reviewed | Egg-selected palette is core identity. | Generate contact sheets, no asset edits yet. |
| Medicine/care assets not fully mapped | User expects multiple medicine visuals. | Create treatment-to-asset mapping before content/code changes. |
| Habitat beds/shelters not mapped | Pets need believable homes. | Choose default object set per species/environment. |
| Optional pickup/drop are cloned/reused | Visible action quality ceiling. | Pilot goose hold/pickup/drop after gates clear. |
| Cleanup path not prioritized | Assets are improving but need safe refinement. | Use reports, contact sheets, preview video, and deterministic cleanup first. |

## Next Checklist Pass

Recommended next docs-only pass:

```text
1. Create palette QA sheets plan for all species.
2. Create medicine/care visual mapping table.
3. Create habitat object default mapping table.
4. Create non-generation cleanup queue:
   - canvas/crop
   - alpha/background residue
   - palette consistency
   - icon readability
   - object grounding
5. Only then begin asset edits or generation.
```

Do not start broad image generation until the code-side reliability gates and
manifest/provenance workflow are ready.
