# Wevito Visual Next Execution Plan

Updated: 2026-05-04

This is the concrete next plan for the visual-side thread. It turns the broader
asset inventory and master plan into a sequence of small, reviewable tasks.

It is docs/reporting-first. It does not request sprite generation, sprite edits,
runtime code changes, or build/test runs.

## Current Boundary

```text
visual thread
  -> asset planning
  -> visual QA criteria
  -> mapping tables
  -> non-mutating audits
  -> handoff prompts/plans

code-side thread
  -> build/test reliability
  -> runtime display plumbing
  -> probes
  -> import/app/runtime code

latest code-side update
  -> runtime canvas contract green in code-side worktree
  -> full vNext tests green: 26 / 26
  -> debug publish green with -SkipAssetPrep
  -> production mutation still paused pending manifest/apply workflow

do not cross
  -> no PNG rewrites
  -> no source-board edits
  -> no runtime code edits
  -> no broad generation
```

## Concrete Order

```text
Phase 1: Color Variant QA Plan
  purpose: prove the six egg colors before palette edits
  output: WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md

Phase 2: Medicine/Care Visual Mapping
  purpose: map existing care art to conditions and treatment meanings
  output: WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md

Phase 3: Habitat Loadout Mapping
  purpose: assign beds, shelters, perches, water, food, and props by species
  output: WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md

Phase 4: Non-Generation Cleanup Queue
  purpose: define cleanup categories and safe review order
  output: WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md,
          then WEVITO_EXISTING_ASSET_CLEANUP_QUEUE_2026-05-04.md

Phase 5: Visual Production Gate
  purpose: decide when visual generation or PNG edits can safely begin
  output: updated handoff / prompt for the code-side thread
```

## Why This Order

```text
egg colors
  -> identity foundation

medicine/care
  -> important gameplay surface with existing art already available

habitat objects
  -> gives pets a believable home and object context

cleanup queue
  -> refines what exists before generating replacements

optional animation production
  -> waits for code-side reliability gates
```

## Phase 1 - Color Variant QA Plan

First target:

| Target | Reason |
| --- | --- |
| `goose / baby / female` across all six colors | Already used by the optional-animation pilot, and beak/body contrast makes palette issues easy to see. |

Current evidence:

```text
goose/baby/female/red:    64 PNGs
goose/baby/female/orange: 64 PNGs
goose/baby/female/yellow: 64 PNGs
goose/baby/female/blue:   64 PNGs
goose/baby/female/indigo: 64 PNGs
goose/baby/female/violet: 64 PNGs
```

Deliverable:

- define contact-sheet layout
- define pass/fail criteria
- define palette defect categories
- define expansion order after the first target
- define stop rules before any recolor

## Phase 2 - Medicine/Care Mapping

Existing art candidates:

```text
bandage_roll
first_aid_kit
grooming_brush
medicine_dropper
pill_bottle
soap_bottle
syringe
thermometer
towel
```

Deliverable:

- map condition groups to existing care art
- distinguish generic medicine from specific treatment visuals
- identify which assets should become first-class gameplay content later
- define visual review criteria at UI scale

## Phase 3 - Habitat Loadout Mapping

Existing object candidates:

```text
beds/mats: blanket_mat, hay_bed, moss_bed, nest_bed, cloth_mat
shelters: crate_hideout, log_shelter, tunnel_hide
perches/basking: branch_perch, stump_perch, rock_basking_spot
containers: pond_dish, seed_tray, shallow_water_dish, water_bowl
utility: moss_patch, pebble_cluster, stick_bundle, lantern, wooden_sign
```

Deliverable:

- assign default object sets by species/environment
- classify object scale and interaction role
- identify sleep/hide/eat/drink/play/perch zones for future code work
- define visual review order

## Phase 4 - Existing Asset Cleanup Queue

Cleanup categories:

| Category | Mutation now? |
| --- | --- |
| Color contact-sheet review | no |
| Alpha/background residue review | no |
| Canvas/crop consistency review | no; use per-sequence stable canvas rules from `WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md` |
| Medicine/care icon readability review | no |
| Habitat object scale review | no |
| Optional animation contact review | no |

Deliverable:

- ordered cleanup queue
- entry criteria for a cleanup task
- exit criteria for accepting a cleanup task
- rules for when generation is allowed

Historical code-side canvas report:

```text
vnext/artifacts/runtime-canvas-contract-20260504-code-side.md
  checked sequences: 2880
  mixed-canvas sequences: 456
  affected species: crow, deer, fox, rat, snake
```

Current code-side canvas result:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md
  checked sequences: 2880
  checked frames: 10800
  mixed-canvas sequences: 0
  missing/count rows: 0
  invalid/non-alpha PNG rows: 0
```

Visual rule:

```text
normalize each affected animation sequence to one stable canvas
preserve larger natural motion
do not shrink/crop animals into an old fixed box
```

## Production Gate

Visual production may begin only when:

- code-side reliability gates remain clear in the active target branch/worktree
- manifest/provenance workflow is available
- contact sheet is mandatory
- preview video is mandatory for animation
- rollback path is known
- batch scope is one target family/set, not the whole grid

First production candidate after gates:

```text
goose / baby / female / blue / hold_ball
```

First non-production QA candidate now:

```text
goose / baby / female / all six colors
```

## Immediate Action

Proceed with Phase 1 now:

```text
create WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md
```

Then continue with Phase 2 and Phase 3 mapping docs if no contradictions appear.
