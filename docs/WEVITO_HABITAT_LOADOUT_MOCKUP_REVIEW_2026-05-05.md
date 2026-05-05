# Wevito Habitat Loadout Mockup Review

Date: 2026-05-05

Purpose: review first habitat loadout mockups for five priority species using existing environment, item, and runtime pet assets.

This is a visual-side review. It does not authorize code edits, content edits, renderer changes, object-zone implementation, prop-anchor edits, sprite mutation, generation, or import.

## Review Artifacts

Generated packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-habitat-loadout-mockups\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- habitat-loadout-mockup-contact-sheet.png
        +-- goose-habitat-loadout-mockup.png
        +-- rat-habitat-loadout-mockup.png
        +-- crow-habitat-loadout-mockup.png
        +-- snake-habitat-loadout-mockup.png
        +-- frog-habitat-loadout-mockup.png
```

The mockups are intentionally review-only compositions. They use current assets but do not represent runtime placement, depth, occlusion, or final scale.

## Main Finding

The existing object pool is strong, but the first loadouts are too crowded when every recommended object is shown at once.

```text
object pool
  |
  +-- enough useful assets exist
  |
  v
layout problem
  |
  +-- too many objects per habitat at once
  +-- several objects compete for the same ground band
  +-- labels and pets get visually crowded
  |
  v
recommended solution
  |
  +-- define primary / secondary / decor tiers
  +-- show fewer active objects at once
  +-- rotate optional props by state, need, or season
```

## Species Findings

| Species | Decision | Notes |
| --- | --- | --- |
| `goose` | promising, needs simpler staging | Pond/nest/water/leaf pieces fit the species. The pond dish and existing environment water overlap visually; use one primary water focal point plus small decor. |
| `rat` | too crowded as full loadout | Scrap/crate/tunnel language fits, but too many large box objects hide the pet. Needs a smaller primary shelter plus one food/water point. |
| `crow` | promising, needs vertical/perch rules | Perches and shiny items fit. Current flat mockup makes branch/stick objects compete with floor objects. Needs explicit perch depth/anchor rules. |
| `snake` | promising, needs basking hierarchy | Rock, moss, shallow water, and tunnel all fit, but should not all be active foreground objects. Basking rock should be the main anchor. |
| `frog` | promising, needs water/moss simplification | Pond/moss/bug/leaf language works. Should prefer pond + one moss patch + one food/enrichment item at a time. |

## Recommended Loadout Tiers

Use tiers instead of placing every object at once.

```text
species habitat loadout
  |
  +-- primary anchor
  |     +-- one bed/shelter/perch/water feature
  |
  +-- interaction object
  |     +-- food, water, toy, care target
  |
  +-- optional decor
        +-- one or two non-interactive dressing props
```

## First Refined Loadout Proposal

| Species | Primary anchor | Interaction object | Optional decor |
| --- | --- | --- | --- |
| `goose` | `pond_dish` or environment pond, not both as focal | `ball` or `shallow_water_dish` | `leaf_pile`, `pebble_cluster` |
| `rat` | `crate_hideout` or `tunnel_hide` | `snack_bowl`, `water_bowl` | `storage_basket` |
| `crow` | `branch_perch` | `seed_tray` or `hanging_feeder` | `mirror_trinket`, `shiny_reward` |
| `snake` | `rock_basking_spot` | `shallow_water_dish` or `reptile_tray` | `moss_patch`, `pebble_cluster` |
| `frog` | `pond_dish` | `bug_treat` or `shallow_water_dish` | `moss_patch`, `leaf_pile` |

## Runtime/Renderer Implications

Future implementation should not simply draw every asset in a species list.

Needed contract:

```text
habitat object
  |
  +-- role
  +-- priority tier
  +-- state trigger
  +-- anchor zone
  +-- depth band
  +-- occlusion mode
  +-- contact shadow
```

Recommended first renderer/content slice, when code-side is ready:

1. one primary anchor object per species
2. one interaction object visible at a time
3. one optional decor object
4. explicit depth and pet-contact rules

## Visual Acceptance Criteria

- Pet remains the focal point.
- Objects support species identity without hiding the pet.
- Each visible object has a clear role.
- The habitat does not look like an inventory dump.
- Object scale feels plausible beside baby, teen, and adult forms.
- Water/food/toy/rest anchors are visually distinct.

## Next Visual Work

Create refined three-object mockups for the same five species:

```text
primary anchor + one interaction object + one optional decor object
```

This should happen before asking code-side to implement object zones or content mappings.

