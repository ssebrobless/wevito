# Wevito Habitat Placement Anchor Contract

Date: 2026-05-05

Purpose: convert the refined habitat loadout reviews into a concrete visual contract for placement anchors, depth tiers, occlusion, and age-scale behavior.

This is a visual-side planning document. It does not authorize code edits, content edits, renderer changes, object-zone implementation, prop-anchor edits, sprite mutation, generation, or import.

## Review Artifacts

Anchor/depth draft packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-habitat-anchor-depth-planning\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- habitat-anchor-depth-contact-sheet.png
        +-- goose-anchor-depth-draft.png
        +-- rat-anchor-depth-draft.png
        +-- crow-anchor-depth-draft.png
        +-- snake-anchor-depth-draft.png
        +-- frog-anchor-depth-draft.png
```

Age-scale packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-habitat-primary-anchor-age-scale\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- primary-anchor-age-scale-contact-sheet.png
        +-- goose-primary-anchor-age-scale.png
        +-- rat-primary-anchor-age-scale.png
        +-- crow-primary-anchor-age-scale.png
        +-- snake-primary-anchor-age-scale.png
        +-- frog-primary-anchor-age-scale.png
```

No runtime/source PNGs, content files, prop anchors, or code were modified.

## Contract Shape

```text
habitat placement
  |
  +-- primary anchor
  |     +-- one always-visible species identity object
  |
  +-- active interaction zone
  |     +-- current food / water / toy / care object
  |
  +-- optional decor zone
  |     +-- one low-priority dressing prop
  |
  +-- pet roam band
  |     +-- where normal idle/walk/rest poses can sit
  |
  +-- near occluder
        +-- foreground edge that can partially overlap pet/object
```

## Proposed Data Fields

Future code/content work should represent:

```text
habitat_object_slot
  |
  +-- species_id
  +-- environment_id
  +-- slot_id
  +-- role
  +-- asset_id
  +-- priority_tier
  +-- default_rect
  +-- age_scale_policy
  +-- depth_band
  +-- occlusion_mode
  +-- contact_shadow_mode
  +-- interaction_zone
  +-- notes
```

## Depth Bands

```text
backdrop
  |
  +-- far_prop
  |
  +-- ground_contact
  |
  +-- pet_shadow
  |
  +-- pet_body
  |
  +-- held_or_carried_prop
  |
  +-- near_occluder
  |
  +-- ui_overlay
```

Recommended role mapping:

| Depth band | Use | Examples |
| --- | --- | --- |
| `far_prop` | dressing behind pets | `storage_basket`, `moss_patch`, `pebble_cluster`, `shiny_reward` |
| `ground_contact` | objects pets stand/rest beside | `pond_dish`, `rock_basking_spot`, `snack_bowl`, `seed_tray` |
| `pet_interactive` | action target or held prop | `ball`, `bug_treat`, `shallow_water_dish` |
| `near_occluder` | foreground overlap edge | pond rim, crate edge, perch branch, shelter mouth |
| `ui_overlay` | labels/status only | not habitat art |

## Species Contract Draft

| Species | Primary anchor | Interaction | Decor | Contract decision |
| --- | --- | --- | --- | --- |
| `goose` | `pond_dish` | `ball` | `pebble_cluster` | Accept. Pond dish works across baby/teen/adult. Keep pond/rim as possible near occluder. |
| `rat` | `crate_hideout` | `snack_bowl` | `storage_basket` | Accept with posture/occlusion rule. Crate is large; pet should sit in front/near opening, not centered over it. |
| `crow` | `branch_perch` | `seed_tray` | `shiny_reward` | Accept with explicit perch anchor. Crow should perch on/near branch, not float in front of it. |
| `snake` | `rock_basking_spot` | `shallow_water_dish` | `moss_patch` | Accept with adult scale caution. Rock works but is small for adult coil; allow larger basking rect or age-specific anchor scale. |
| `frog` | `pond_dish` | `bug_treat` | `moss_patch` | Accept. Pond dish works across ages; frog needs front-edge contact/shadow so it does not look pasted on. |

## Age-Scale Findings

| Species | Baby | Teen | Adult | Action |
| --- | --- | --- | --- | --- |
| `goose` | good | good | good | Use same primary anchor across ages. |
| `rat` | good but crate dominates | acceptable | acceptable but occlusion-sensitive | Place pet beside/front of crate; crate should be anchor, not seat. |
| `crow` | good as concept | acceptable | acceptable | Needs perch contact point; age scaling alone is not enough. |
| `snake` | good | good | anchor small for adult | Consider adult-specific larger rock zone or allow coil to extend beyond rock. |
| `frog` | good | good | good | Use pond edge/contact shadow. |

## Placement Rules

1. Do not center every pet on the primary object.
2. Primary anchor should establish place, not swallow the pet.
3. Interaction object should appear only when relevant to need/action when possible.
4. Decor should be optional and low-priority.
5. Roam band should avoid covering the most detailed prop silhouettes.
6. Near occluders should overlap only a small lower-body area unless a species-specific hide/rest pose supports more.
7. Baby/teen/adult should share the same visual language but may need different anchor offsets.

## Renderer/Content Implementation Recommendation

When code-side is ready, the smallest useful implementation is:

```text
Phase A
  |
  +-- define static visual slots for 5 species
  +-- one primary anchor per species
  +-- no interaction switching yet
  +-- no occlusion beyond existing foreground rules

Phase B
  |
  +-- add active interaction object slot
  +-- show food/water/toy based on current action/need

Phase C
  |
  +-- add near-occluder masks/depth bands
  +-- add age-specific offsets where needed
```

## Not Yet

- do not create all-species object zones in one broad pass
- do not place all loadout objects at once
- do not repaint anchors before testing current assets
- do not make the habitat system depend on generated replacement art
- do not use PET TASKS to apply habitat placement

