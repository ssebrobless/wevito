# Wevito Visual/UI Remaining Implementation Plan

Date: 2026-05-05

Purpose: provide one concrete phase-by-phase plan for all remaining visual-side, UI-side, asset-review, and pet-helper design work.

This plan consolidates:

- Claude Design Phase 1B output
- overlay-first product direction
- sprite cleanup progress
- all-animal color variant coverage
- PET TASKS report-only boundary
- Sprite Workflow V2 workbench coordination
- care/medicine/habitat/object visual inventory
- optional animation apply/proof boundaries

This is a planning document. It does not authorize code edits, runtime/source PNG mutation, sprite generation/import, PET TASKS execution adapters, prop-anchor edits, or asset-prep builds.

## Current Project Shape

```text
visual/UI work
  |
  +-- mostly complete
  |     +-- shared asset cleanup
  |     +-- source shared sync
  |     +-- runtime tiny-noise cleanup
  |     +-- care/medicine core icon cleanup
  |     +-- habitat prop cleanup
  |     +-- all-animal color folder coverage
  |     +-- Claude Design Phase 1B review
  |
  +-- still active / incomplete
  |     +-- overlay-first UI implementation planning
  |     +-- PET TASKS visual refinement
  |     +-- Sprite Workflow V2 design handoff
  |     +-- color palette quality review
  |     +-- optional animation expansion coordination
  |     +-- Clipboard Shelf and useful pet-helper functions
  |     +-- Creative Learning Lab planning
  |     +-- final visual QA and cross-thread handoff
  |
  +-- code-side/manual gates
        +-- runtime PNG apply/proof
        +-- Godot packaged proof
        +-- vNext UI implementation
        +-- PET TASKS adapter implementation
        +-- Sprite Workflow App implementation
```

## Completed Baseline

| Area | Status | Evidence |
| --- | --- | --- |
| Overlay-first design decision | Complete | `docs\WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md` |
| Claude Design visual translation | Complete | `docs\WEVITO_CLAUDE_DESIGN_VISUAL_TRANSLATION_SPEC_2026-05-05.md` |
| Claude screenshot preservation | Complete | `vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\` |
| Claude screenshot index | Complete | `docs\WEVITO_CLAUDE_DESIGN_SCREENSHOT_INDEX_2026-05-05.md` |
| Shared runtime/source cleanup | Mostly complete | `docs\WEVITO_SPRITE_CLEANUP_PROGRESS_2026-05-04.md` |
| Color variant folder coverage | Complete | `docs\WEVITO_ALL_ANIMAL_COLOR_VARIANT_COVERAGE_2026-05-05.md` |
| Asset inventory checklist | Established | `docs\WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md` |
| PET TASKS expectation boundary | Established | `docs\VISUAL_SIDE_PET_TASKS_ADAPTER_EXPECTATIONS_2026-05-05.md` |
| Pet helper function roadmap | Established | `docs\WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md` |
| Clipboard Shelf visual spec | Complete | `docs\WEVITO_CLIPBOARD_SHELF_VISUAL_SPEC_2026-05-05.md` |
| Care/medicine/object review packet | Complete | `docs\WEVITO_CARE_MEDICINE_OBJECT_REVIEW_PACKET_2026-05-05.md` |
| Color index quality review | Complete for all species index sheets | `docs\WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS1_2026-05-05.md`, `docs\WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS2_2026-05-05.md` |
| Creative Learning Lab plan | Complete | `docs\WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md` |
| Visual status dashboard | Complete | `docs\WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md` |
| Sprite Workflow App handoff prompt | Complete | `docs\WEVITO_SPRITE_WORKFLOW_APP_THREAD_HANDOFF_PROMPT_2026-05-05.md` |
| Walk-motion color review | Complete for risk targets | `docs\WEVITO_COLOR_PALETTE_WALK_MOTION_REVIEW_PASS3_2026-05-05.md` |
| First habitat loadout mockups | Complete | `docs\WEVITO_HABITAT_LOADOUT_MOCKUP_REVIEW_2026-05-05.md` |
| Crow dark-background contrast review | Complete | `docs\WEVITO_CROW_DARK_BACKGROUND_CONTRAST_REVIEW_2026-05-05.md` |
| Tall-row color atlas follow-up | Complete | `docs\WEVITO_COLOR_ATLAS_LAYOUT_FOLLOWUP_2026-05-05.md` |
| Refined habitat loadout mockups | Complete | `docs\WEVITO_REFINED_HABITAT_LOADOUT_REVIEW_2026-05-05.md` |
| Habitat placement anchor contract | Complete for five-species pilot | `docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md` |
| Final visual doc sweep | Complete | `docs\WEVITO_VISUAL_FINAL_DOC_SWEEP_2026-05-05.md` |

## Master Phase Map

```text
Phase 0  Baseline lock
Phase 1  Overlay-first UI contract
Phase 2  Claude Design extraction and handoff
Phase 3  PET TASKS visual refinement
Phase 4  Sprite Workflow V2 workbench coordination
Phase 5  Color palette quality review
Phase 6  Asset/care/medicine/habitat review
Phase 7  Optional animation visual expansion
Phase 8  Pet helper functions and Clipboard Shelf
Phase 9  Creative Learning Lab
Phase 10 Final visual QA sweep
Phase 11 Cross-thread implementation handoff
```

## Phase 0 - Baseline Lock

Goal: freeze what is already known so future work does not reopen solved problems.

Status: mostly complete.

Tasks:

- record that all six color folders exist for all ten species, three ages, two genders
- record that shared runtime/source cleanup is complete except classified exceptions
- record that the Claude Design examples rendered successfully
- record that PET TASKS is report-only
- preserve current no-mutation boundaries

Done when:

- this plan points to the existing evidence docs
- future phases know which work is quality review, not coverage creation

Do not do:

- broad recolor
- broad cleanup rerun
- asset-prep build
- runtime PNG mutation

## Phase 1 - Overlay-First UI Contract

Goal: ensure all UI work preserves the free-roaming desktop-pet overlay.

Status: complete as planning, implementation pending code-side.

Core rule:

```text
overlay remains home
  |
  +-- PET TASKS is a popup/drawer
  +-- Sprite Workflow V2 is a summoned workbench
  +-- Creative Learning Lab is a later dashboard
  +-- tool surfaces never become the default home screen
```

Remaining tasks:

- ask code-side to validate the layering contract against current vNext UI architecture
- confirm popup/workbench behavior does not break pass-through/unfocused/pinned overlay behavior
- eventually capture before/after screenshots when code-side implements UI changes

Relevant docs:

- `docs\WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md`
- `docs\WEVITO_CLAUDE_DESIGN_VISUAL_TRANSLATION_SPEC_2026-05-05.md`
- `docs\WEVITO_OVERLAY_FIRST_IMPLEMENTATION_ROADMAP_2026-05-05.md`

## Phase 2 - Claude Design Extraction And Handoff

Goal: convert Claude Design from a prototype into implementation reference material.

Status: complete for current visual-side needs.

Completed:

- observed four rendered Claude examples
- preserved screenshots in design artifacts
- updated Phase 1B run doc
- created compact screenshot index and approval map

Remaining tasks:

- export or capture cleaner full-artboard images if Claude Design allows export
- hand off to code-side and Sprite Workflow App thread when needed

Artifacts:

```text
vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\
vnext\artifacts\design\phase1b-claude-design\screenshot-index-20260505\
```

Acceptance criteria:

- every Claude example has a saved visual reference
- every borrowed idea is mapped to a Wevito surface
- no one treats Claude's prototype as production code truth

## Phase 3 - PET TASKS Visual Refinement

Goal: make PET TASKS visually clearer and more pet-like while keeping it report-only.

Status: spec complete, implementation pending code-side.

Visual target:

```text
PET TASKS popup
  |
  +-- REPORT ONLY header
  +-- three helper slots
  +-- command input
  +-- PREPARE / PREVIEW distinction
  +-- task card
  +-- policy result
  +-- output artifact path
  +-- timeline/audit
  +-- safety footer
```

Remaining tasks:

- code-side reviews visual spec
- decide whether helper cards fit inside current popup or need a refined layout
- implement report-only visual labels
- add Open Report / Copy Path / Open Folder affordances if safe
- later add contact-sheet output to spriteAudit reports

Relevant doc:

- `docs\WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md`

Blocked:

- execution mode
- Godot proof launch
- sprite apply/import/generation
- runtime/source PNG mutation

## Phase 4 - Sprite Workflow V2 Workbench Coordination

Goal: use Claude's Sprite Workflow V2 artboard to guide the separate Sprite Workflow App/workbench.

Status: spec complete, implementation owned by other thread/code-side.

Target shape:

```text
Sprite Workflow V2
  |
  +-- Queue
  +-- Current Row
  +-- Source strip
  +-- Runtime strip
  +-- Candidate strip
  +-- Proof preview
  +-- Validator findings
  +-- Provenance/hash panel
  +-- Dry-run / Apply / Rollback / Export
```

Remaining tasks:

- send spec to Sprite Workflow App thread
- compare current Avalonia/.NET app against no-scroll Claude layout
- identify which layout changes are safe
- keep Sprite Workflow V2 separate from the desktop-pet overlay
- require proof/apply/rollback gates before any mutation-capable work

Relevant doc:

- `docs\WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md`

## Phase 5 - Color Palette Quality Review

Goal: move beyond "folders exist" and verify the six egg colors are actually readable, attractive, and identity-safe.

Status: coverage complete; index-sheet review complete; risk-focused walk-motion review complete; crow dark-background review complete; tall-row atlas follow-up complete.

Completed:

```text
sprites_runtime:            360 / 360 color folders
sprites_authored:           360 / 360 color folders
sprites_authored_verified:  360 / 360 color folders
sprites:                    360 / 360 color folders
portraits:                  complete
```

Remaining tasks:

- flag palette defects only when visible
- classify future issues as `repair`, `accept`, or `defer`
- create targeted prompt/repair specs only for confirmed bad color variants

Review order:

```text
1. goose
2. pigeon
3. frog
4. raccoon
5. squirrel
6. crow
7. fox
8. deer
9. snake
10. rat
```

Artifacts:

```text
vnext\artifacts\visual-review\20260505-all-animal-color-variant-coverage\qa\
```

Do not do:

- broad recolor all animals
- regenerate color folders
- treat mixed-canvas warnings as crop instructions

Completed review docs:

```text
docs\WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS1_2026-05-05.md
docs\WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS2_2026-05-05.md
docs\WEVITO_COLOR_PALETTE_WALK_MOTION_REVIEW_PASS3_2026-05-05.md
docs\WEVITO_CROW_DARK_BACKGROUND_CONTRAST_REVIEW_2026-05-05.md
docs\WEVITO_COLOR_ATLAS_LAYOUT_FOLLOWUP_2026-05-05.md
```

Tall-row artifact note:

```text
squirrel / adult / male:   142x139 native idle frame
deer / adult / female:     135x114 native idle frame
decision:                  use dynamic-row review sheets; no sprite repair
artifact packet:            vnext\artifacts\visual-review\20260505-color-atlas-layout-followup\
```

Current color repair queue:

```text
none
```

## Phase 6 - Asset, Care, Medicine, Habitat, And Object Review

Goal: verify the supporting object ecosystem feels complete and readable.

Status: large cleanup complete; review packet complete; final gameplay/content mapping still pending.

Already cleaned:

- medicine icon
- syringe
- care items
- habitat props
- food/water/container assets
- shared icons
- status icons
- portraits
- environment boards
- celestial art

Remaining tasks:

- verify care/medicine assets are visually distinct in small UI contexts
- map each care/medicine asset to intended gameplay use
- verify food/water/container assets are visually distinguishable
- review habitat object scale consistency
- identify any missing first-class content mappings
- decide which objects belong in gameplay now vs later
- define habitat placement/anchor rectangles after refined loadout review
- test baby/teen/adult scale against each primary anchor

Priority care/medicine checklist:

| Asset | Review question |
| --- | --- |
| `bandage_roll` | Is injury/first-aid meaning clear? |
| `first_aid_kit` | Is doctor/medicine meaning clear? |
| `grooming_brush` | Is grooming meaning clear at small size? |
| `medicine_dropper` | Is liquid medicine distinct from syringe? |
| `pill_bottle` | Is pill medicine readable? |
| `soap_bottle` | Is bath/cleanliness readable? |
| `syringe` | Is it clean and not scary/noisy? |
| `thermometer` | Is sick/diagnosis readable? |
| `towel` | Is bath/recovery meaning clear? |

Relevant docs:

- `docs\WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md`
- `docs\WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md`
- `docs\WEVITO_SOURCE_SHARED_ASSET_CLEANUP_2026-05-05.md`
- `docs\WEVITO_CARE_MEDICINE_OBJECT_REVIEW_PACKET_2026-05-05.md`
- `docs\WEVITO_HABITAT_LOADOUT_MOCKUP_REVIEW_2026-05-05.md`
- `docs\WEVITO_REFINED_HABITAT_LOADOUT_REVIEW_2026-05-05.md`
- `docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md`

## Phase 7 - Optional Animation Visual Expansion

Goal: continue optional animation quality work safely, one target row/family at a time.

Status: active but gated by code-side apply/proof ownership.

Known state:

- `goose / baby / female / blue / hold_ball` was applied/proofed by code-side and passed
- `goose / baby / female / blue / drop_ball` remains code-side/manual apply/proof scope
- ball remains runtime overlay only
- no pickup/drop/carry expansion should happen until the one-row proof path is accepted

Remaining tasks:

- wait for code-side/manual proof outcome for `drop_ball`
- review proof sheet when available
- decide accept/revise/reject for that endpoint
- only then plan pickup/carry expansion
- only after a proven row, plan all-color propagation
- only after all-color propagation is validated, consider next species/age/gender

Optional family priority:

```text
1. hold_ball endpoint protection
2. drop_ball one-row proof
3. pickup_ball one-row review
4. carry_ball_walk contact stability
5. carry_ball_run contact stability
6. all-color propagation
7. next species row
```

Do not do:

- bake ball into pet PNGs
- mutate runtime PNGs from visual-side
- start pickup/drop/carry generation before proof agreement
- expand to all colors without row proof

## Phase 8 - Pet Helper Functions And Clipboard Shelf

Goal: plan and then coordinate useful pet functions that fit the overlay.

Status: roadmap complete; Clipboard Shelf visual spec complete; implementation pending.

Safe functions first:

- localDocs summary
- spriteAudit report-only preview
- proof summary from existing artifacts
- visual QA queue summaries
- link bin improvements
- Clipboard Shelf planning
- care/action suggestions

Clipboard Shelf target:

```text
Clipboard Shelf
  |
  +-- explicitly saved snippets only
  +-- title
  +-- text preview
  +-- timestamp
  +-- copy
  +-- delete
  +-- clear
```

Remaining tasks:

- decide exact UI placement in TOOLS
- coordinate code-side storage/privacy model
- keep it manual-save only
- design pet expression for saved snippets

Relevant doc:

- `docs\WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md`
- `docs\WEVITO_CLIPBOARD_SHELF_VISUAL_SPEC_2026-05-05.md`

## Phase 9 - Creative Learning Lab

Goal: turn Claude's Creative Learning Lab artboard into a future reviewed-data/dashboard plan.

Status: concept observed; detailed read-only/review-first plan complete.

Remaining tasks:

- decide what belongs in vNext vs external tooling
- code-side later decides whether a read-only artifact index/dashboard is worth implementing

Completed plan:

```text
docs\WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md
```

Blocked:

- training export
- automatic import
- unreviewed data promotion
- source/runtime mutation

## Phase 10 - Final Visual QA Sweep

Goal: make sure all visual/UI lanes agree and no stale blocker remains.

Status: complete for current docs; historical docs may still preserve earlier blockers as dated context.

Remaining tasks:

- re-open latest code-side handoff when code-side returns new work
- keep dashboard and handoff prompt current after each code-side reconciliation

Suggested final dashboard:

```text
visual status
  |
  +-- overlay/UI
  +-- PET TASKS
  +-- Sprite Workflow V2
  +-- color variants
  +-- shared assets
  +-- care/medicine
  +-- habitat/objects
  +-- optional animations
  +-- helper functions
  +-- learning lab
```

Current dashboard:

```text
docs\WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md
```

Final sweep:

```text
docs\WEVITO_VISUAL_FINAL_DOC_SWEEP_2026-05-05.md
```

## Phase 11 - Cross-Thread Implementation Handoff

Goal: give code-side and the Sprite Workflow App thread exact next actions.

Remaining tasks:

- update the existing code-side prompt with this master plan
- create a separate prompt for the Sprite Workflow App thread if needed
- include all relevant docs and artifact paths
- include current no-mutation boundaries
- ask each thread to return a single prompt back to visual-side after review

Existing handoff prompt:

- `docs\WEVITO_OVERLAY_DESIGN_CODE_SIDE_HANDOFF_PROMPT_2026-05-05.md`

Needs update:

- add `docs\WEVITO_VISUAL_UI_REMAINING_IMPLEMENTATION_PLAN_2026-05-05.md`
- add phase priorities
- add current remaining work list

## Execution Order Recommendation

```text
next visual-side sequence
  |
  +-- 1. finish this master plan
  |
  +-- 2. update code-side handoff prompt
  |
  +-- 3. create Clipboard Shelf visual spec
  |
  +-- 4. create care/medicine/object review packet
  |
  +-- 5. wait for code-side review of habitat placement contract
  |
  +-- 6. wait for/coordinate optional animation proof labels
  |
  +-- 7. create Creative Learning Lab detailed plan
  |
  +-- 8. final visual QA dashboard
```

## Global Stop Conditions

Pause and coordinate before:

- changing runtime/source PNGs
- running asset-prep builds
- generating new sprites
- importing candidate frames
- applying optional animation rows
- changing prop anchors
- adding PET TASKS execution
- broad-recoloring animals
- broad-refactoring the overlay UI

## Definition Of Done For The Visual/UI Lane

The visual/UI lane is ready for a broader implementation push when:

- overlay-first UI contract is accepted by code-side
- PET TASKS visual refinement has a safe implementation plan
- Sprite Workflow V2 has a no-scroll workbench plan accepted by the other thread
- color variants have been reviewed for quality, not just coverage
- care/medicine/habitat/object assets have a reviewed content mapping
- optional animation expansion has a proven one-row path
- Clipboard Shelf has a privacy-safe visual/product spec
- Creative Learning Lab has a clear reviewed-data plan
- final handoff prompts point to one consistent source of truth
