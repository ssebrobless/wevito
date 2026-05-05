# Wevito Game-Facing Sprite Noise Checklist

Updated: 2026-05-04

This checklist records a no-edit visual audit of the game-facing sprite payload:

```text
game-facing visual payload
  |
  +-- sprites_runtime
  |     +-- pet animation frames currently shown by the runtime
  |
  +-- sprites_shared_runtime
        +-- items, care art, status icons, portraits, environments, UI icons
```

No sprite PNGs were modified. This audit produces review artifacts only.

## Scope

| Root | Assets checked | Assets flagged |
| --- | ---: | ---: |
| `sprites_runtime` | 23040 | 11375 |
| `sprites_shared_runtime` | 555 | 538 |
| **Total** | **23595** | **11913** |

Important scope note:

```text
included here
  -> current game-facing runtime assets

not deep-classified here
  -> incoming_sprites
  -> sprites
  -> sprites_authored
  -> sprites_authored_verified

reason
  -> those are source, raw fallback, or staging lanes
  -> the first completed checklist should cover what the game can display now
```

The broader source/staging tree should get a separate source-board audit after
code-side confirms which raw roots are still active fallback surfaces.

## Artifact Index

Full checklist and review sheets:

```text
vnext/artifacts/visual-review/20260504-game-facing-sprite-noise-audit/
  +-- game-facing-sprite-noise-checklist.csv
  +-- game-facing-sprite-noise-checklist.json
  +-- game-facing-sprite-noise-summary.md
  +-- manifest.json
  +-- priority-flagged-game-assets.png
  +-- runtime-priority-flagged-assets.png
  +-- shared-runtime-flagged-assets.png
  +-- edge-crop-risk-review-sheet.png
  +-- pale-boundary-residue-review-sheet.png
  +-- detached-components-review-sheet.png
```

Use the CSV as the complete checklist. The markdown below is the human triage
view.

## Flag Meanings

| Flag | Meaning | Visual interpretation |
| --- | --- | --- |
| `empty_or_fully_transparent` | No visible alpha pixels. | Usually a blocker if the asset is referenced. |
| `detached_components` | Visible pixels outside the largest alpha component. | Can be real noise, or a multi-object prop/board that needs manual classification. |
| `pale_boundary_residue` | Pale low-saturation pixels on alpha boundary. | Possible matte/checker/crop residue, but can also be feather/fur highlight. |
| `low_alpha_haze` | Many very-low-alpha pixels. | Possible fringe/haze at runtime scale. |
| `edge_pixel_touch` | Visible pixels touch canvas edge. | Possible dirty crop, or normal tight runtime framing. |
| `tight_crop_margin` | Only 1px transparent margin. | Review before editing; not always wrong. |

## Checklist Summary

Severity counts:

| Severity | Count |
| --- | ---: |
| `critical` | 2460 |
| `high` | 840 |
| `medium` | 7948 |
| `low` | 665 |

Flag counts:

| Flag | Count |
| --- | ---: |
| `edge_pixel_touch` | 8997 |
| `detached_components` | 3875 |
| `low_alpha_haze` | 1050 |
| `tight_crop_margin` | 933 |
| `pale_boundary_residue` | 802 |
| `empty_or_fully_transparent` | 1 |

## Priority Buckets

```text
priority queue
  |
  +-- P0: missing/empty referenced art
  |
  +-- P1: shared runtime assets with obvious collage/matte residue
  |
  +-- P1: pet runtime frames with repeated detached/haze artifacts
  |
  +-- P2: crop-risk rows where edge touch may be acceptable but needs review
  |
  +-- P3: source/staging roots not included in this completed pass
```

## P0 - Empty Asset

- [ ] `sprites_shared_runtime/items/toys_b/blanket_mat.png`
  - Flags: `empty_or_fully_transparent`
  - Why it matters: previous habitat planning already identified
    `blanket_mat` as unsafe for raccoon/fox rest loadouts.
  - Action: do not use this asset in habitat loadouts until remapped or
    replaced.

## P1 - Shared Runtime Assets

These assets dominate the top of the checklist. Many are broad item boards,
environment collages, or multi-object icons, so detached components are not
automatically removable. They still deserve first manual review because they are
large, visible, and often include pale boundary residue.

- [ ] `sprites_shared_runtime/environment/goose.png`
- [ ] `sprites_shared_runtime/environment/squirrel.png`
- [ ] `sprites_shared_runtime/environment/night_props.png`
- [ ] `sprites_shared_runtime/environment/crow.png`
- [ ] `sprites_shared_runtime/items/containers/dish_set.png`
- [ ] `sprites_shared_runtime/items/containers/storage_jar.png`
- [ ] `sprites_shared_runtime/items/containers/water_bowl.png`
- [ ] `sprites_shared_runtime/items/containers/pond_dish.png`
- [ ] `sprites_shared_runtime/items/toys_b/nest_bed.png`
- [ ] `sprites_shared_runtime/items/toys_a/mirror_trinket.png`
- [ ] `sprites_shared_runtime/items/toys_a/rope_toy.png`
- [ ] `sprites_shared_runtime/items/toys_a/digging_tray.png`
- [ ] `sprites_shared_runtime/items/food_herbivore/herbivore_bowl.png`
- [ ] `sprites_shared_runtime/items/food_herbivore/root_slice.png`
- [ ] `sprites_shared_runtime/items/food_birds/berry_cluster.png`
- [ ] `sprites_shared_runtime/items/food_birds/sunflower_seeds.png`
- [ ] `sprites_shared_runtime/items/food_birds/shiny_reward.png`
- [ ] `sprites_shared_runtime/items/food_predator/bug_cup.png`
- [ ] `sprites_shared_runtime/items/food_predator/fish.png`
- [ ] `sprites_shared_runtime/items/food_omnivore/snack_bowl.png`
- [ ] `sprites_shared_runtime/status/thirsty.png`
- [ ] `sprites_shared_runtime/status/sick.png`
- [ ] `sprites_shared_runtime/status/hungry.png`
- [ ] `sprites_shared_runtime/status/dirty.png`
- [ ] `sprites_shared_runtime/status/happy.png`

Medicine/care-specific flags from the same pass:

- [ ] `sprites_shared_runtime/icons/medicine.png`
  - Flags: `edge_pixel_touch`, `detached_components`,
    `pale_boundary_residue`, `low_alpha_haze`
- [ ] `sprites_shared_runtime/items/care/syringe.png`
  - Flags: `detached_components`
- [ ] `sprites_shared_runtime/items/care/thermometer.png`
  - Flags: `pale_boundary_residue`

## P1 - Pet Runtime Frames

The highest-signal pet runtime findings are not distributed evenly. The repeated
pattern is:

```text
runtime pet noise
  |
  +-- rat
  |     +-- baby blue drink_01 has very large detached component count
  |
  +-- pigeon
        +-- adult rows repeat detached / low-alpha haze across colors
        +-- many affected rows are copied across base and optional families
```

First concrete runtime frame checks:

- [ ] `sprites_runtime/rat/baby/female/blue/drink_01.png`
- [ ] `sprites_runtime/rat/baby/male/blue/drink_01.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/idle_02.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/hold_ball_02.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/bathe_02.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/walk_01.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/happy_01.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/sick_02.png`
- [ ] `sprites_runtime/pigeon/adult/female/blue/sad_00.png`
- [ ] `sprites_runtime/pigeon/adult/female/blue/sleep_00.png`
- [ ] `sprites_runtime/pigeon/adult/female/blue/sleep_01.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/carry_ball_walk_01.png`
- [ ] `sprites_runtime/pigeon/adult/male/blue/carry_ball_run_01.png`

Color propagation warning:

```text
same pigeon residue appears across indigo/orange/red/violet/yellow rows
  -> likely propagated from a shared source frame
  -> fix source or canonical color base first
  -> then propagate only after review
```

## P2 - Crop-Risk Review

Many runtime rows touch the edge of their per-sequence canvas. This is not
automatically a defect because the current canvas contract allows natural
per-sequence sizes. Treat `edge_pixel_touch` as a review marker, not as a command
to pad every sprite.

First crop-risk questions:

- [ ] Does the contact point or body edge visibly clip at runtime scale?
- [ ] Does padding improve readability without changing pose art?
- [ ] Does the same row already pass code-side sequence-stable canvas rules?
- [ ] Would padding create a proof burden for Godot or vNext placement?

## Species Pressure Map

Flagged pet runtime rows by species:

| Species | Flagged rows |
| --- | ---: |
| `rat` | 1506 |
| `snake` | 1404 |
| `crow` | 1398 |
| `pigeon` | 1308 |
| `fox` | 1276 |
| `frog` | 1256 |
| `deer` | 888 |
| `raccoon` | 855 |
| `squirrel` | 846 |
| `goose` | 638 |

This map should guide review order, but not automatic repair order. Rat/snake
and crow have high counts partly because natural long/wing/body shapes touch
their frames. Pigeon has a clearer repeated haze/detached-component pattern.

## Animation Pressure Map

Flagged pet runtime rows by animation family:

| Animation | Flagged rows |
| --- | ---: |
| `walk` | 1752 |
| `bathe` | 1176 |
| `idle` | 1176 |
| `sick` | 1176 |
| `eat` | 1176 |
| `hold_ball` | 1059 |
| `happy` | 781 |
| `sleep` | 678 |
| `drink` | 653 |
| `sad` | 588 |
| `carry_ball_run` | 303 |
| `play_ball` | 265 |
| `drop_ball` | 205 |
| `pickup_ball` | 205 |
| `carry_ball_walk` | 182 |

## Working Rule

Do not clean by deleting every detached component automatically.

```text
safe review order
  |
  +-- classify
  |     +-- real noise
  |     +-- intentional multi-part object
  |     +-- intentional highlight/fringe
  |
  +-- prove one representative row or asset
  |
  +-- only then consider deterministic cleanup
```

This checklist is an inspection queue, not an asset mutation instruction.
