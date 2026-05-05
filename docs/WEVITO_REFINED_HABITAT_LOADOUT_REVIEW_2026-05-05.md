# Wevito Refined Habitat Loadout Review

Date: 2026-05-05

Purpose: review the second-pass habitat mockups using the tiered rule: one primary anchor, one interaction object, and one optional decor object.

This is a visual-side review. It does not authorize code edits, content edits, renderer changes, object-zone implementation, prop-anchor edits, sprite mutation, generation, or import.

## Review Artifacts

Generated packet:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260505-refined-habitat-loadout-mockups\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- refined-habitat-loadout-contact-sheet.png
        +-- goose-refined-three-object-loadout.png
        +-- rat-refined-three-object-loadout.png
        +-- crow-refined-three-object-loadout.png
        +-- snake-refined-three-object-loadout.png
        +-- frog-refined-three-object-loadout.png
```

No source/runtime PNGs, content files, prop anchors, or code were modified.

## Main Finding

The tiered loadout model is the correct direction.

```text
first mockup
  |
  +-- many useful objects
  +-- too crowded
  +-- pet often lost among props
  |
  v
refined mockup
  |
  +-- one primary anchor
  +-- one interaction object
  +-- one decor object
  +-- pet remains readable
```

## Species Review

| Species | Refined loadout | Decision | Notes |
| --- | --- | --- | --- |
| `goose` | `pond_dish` + `ball` + `pebble_cluster` | accept concept | Pet remains visible. Pond + ball fits current optional-animation work. |
| `rat` | `crate_hideout` + `snack_bowl` + `storage_basket` | accept with scale caution | Strong species fit. Crate is large but works as a primary shelter if placed behind/aside. |
| `crow` | `branch_perch` + `seed_tray` + `shiny_reward` | accept with perch-depth requirement | Needs explicit perch/contact rules before runtime placement. |
| `snake` | `rock_basking_spot` + `shallow_water_dish` + `moss_patch` | accept concept | Best when basking rock is the main rest anchor and tunnel clutter is omitted. |
| `frog` | `pond_dish` + `bug_treat` + `moss_patch` | accept concept | Strong species fit. Pond/moss/food reads clearly with fewer props. |

## Implementation Guidance

Future habitat/content work should represent object loadouts as tiers:

```text
habitat_loadout
  |
  +-- primary_anchor
  |     +-- bed / shelter / perch / pond / basking rock
  |
  +-- active_interaction
  |     +-- food / water / toy / care object
  |
  +-- optional_decor
        +-- one or two dressing props
```

Suggested rule:

```text
show
  |
  +-- one primary anchor always
  +-- one active interaction object based on current need/action
  +-- one optional decor object

do not show
  |
  +-- entire species object inventory at once
```

## Remaining Visual Work

Before code-side implements zones/depth:

1. Create simple placement/anchor rectangles for the five refined species.
2. Decide which objects are far props vs ground contact vs near occluders.
3. Test baby/teen/adult scale against each primary anchor.
4. Decide which object labels/content ids are first-class.

## Acceptance Criteria

- Pet stays focal.
- Habitat has clear species identity.
- Props have obvious roles.
- Scene does not look like an inventory spread.
- Objects are tiered before renderer work.

