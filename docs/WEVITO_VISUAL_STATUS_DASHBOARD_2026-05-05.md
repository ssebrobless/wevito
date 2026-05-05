# Wevito Visual Status Dashboard

Date: 2026-05-05

Purpose: provide a compact current status dashboard for Wevito visual/UI work.

This dashboard summarizes current state only. It does not authorize code edits, sprite mutation, generation, import, prop-anchor edits, PET TASKS execution, or asset-prep builds.

## Current Shape

```text
visual/UI lane
  |
  +-- green / mostly complete
  |     +-- overlay-first design direction
  |     +-- Claude Design review
  |     +-- shared asset cleanup
  |     +-- color folder coverage
  |     +-- care/object review packet
  |
  +-- yellow / planned, needs code-side or follow-up
  |     +-- PET TASKS visual refinement
  |     +-- Sprite Workflow V2 workbench implementation
  |     +-- Clipboard Shelf implementation
  |     +-- Creative Learning Lab read-only dashboard
  |     +-- walk-motion/background color QA
  |
  +-- gated / do not start without approval
        +-- optional animation apply/proof
        +-- sprite generation/import
        +-- runtime/source PNG mutation
        +-- PET TASKS execution adapters
```

## Status Table

| Area | Status | Current decision | Source docs |
| --- | --- | --- | --- |
| Overlay-first UI | Green planning / code pending | Keep living desktop-pet overlay as home. | `WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md` |
| Claude Design Phase 1B | Green | Use as visual reference, not code truth. Screenshot index complete. | `WEVITO_CLAUDE_DESIGN_PHASE1B_RUN_2026-05-05.md`, `WEVITO_CLAUDE_DESIGN_SCREENSHOT_INDEX_2026-05-05.md` |
| PET TASKS | Yellow | Refine visuals, keep report-only. | `WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md` |
| Sprite Workflow V2 | Yellow | Separate summoned workbench, not overlay replacement. | `WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md` |
| Color coverage | Green | All folders present across runtime/authored/verified/legacy roots. | `WEVITO_ALL_ANIMAL_COLOR_VARIANT_COVERAGE_2026-05-05.md` |
| Color quality | Green | Index, risk walk-motion, crow dark-background, and tall-row atlas follow-up checks complete. No repair queue. | `WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS1_2026-05-05.md`, `WEVITO_COLOR_PALETTE_QUALITY_REVIEW_PASS2_2026-05-05.md`, `WEVITO_COLOR_PALETTE_WALK_MOTION_REVIEW_PASS3_2026-05-05.md`, `WEVITO_CROW_DARK_BACKGROUND_CONTRAST_REVIEW_2026-05-05.md`, `WEVITO_COLOR_ATLAS_LAYOUT_FOLLOWUP_2026-05-05.md` |
| Shared asset cleanup | Green | Broad cleanup complete; only targeted future fixes. | `WEVITO_SPRITE_CLEANUP_PROGRESS_2026-05-04.md` |
| Care/medicine/object art | Green/yellow | Art pool clean/broad; content mapping remains. | `WEVITO_CARE_MEDICINE_OBJECT_REVIEW_PACKET_2026-05-05.md` |
| Habitat loadouts | Green/yellow | Five-species placement/anchor contract drafted; code-side implementation still pending. | `WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md`, `WEVITO_HABITAT_LOADOUT_MOCKUP_REVIEW_2026-05-05.md`, `WEVITO_REFINED_HABITAT_LOADOUT_REVIEW_2026-05-05.md`, `WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md` |
| Optional animations | Gated | Code-side/manual apply/proof owns mutation. | `WEVITO_VISUAL_UI_REMAINING_IMPLEMENTATION_PLAN_2026-05-05.md` |
| Clipboard Shelf | Yellow | Manual-save-only spec complete. | `WEVITO_CLIPBOARD_SHELF_VISUAL_SPEC_2026-05-05.md` |
| Creative Learning Lab | Yellow | Read-only reviewed-data dashboard plan complete. | `WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md` |
| Final doc sweep | Green | Current source-of-truth docs reconciled; historical docs may preserve old blockers as context. | `WEVITO_VISUAL_FINAL_DOC_SWEEP_2026-05-05.md` |

## Current Repair Queues

```text
color palette repair queue
  |
  +-- none

shared asset broad cleanup queue
  |
  +-- none

targeted visual follow-up queue
  |
  +-- tall-row atlas follow-up complete; use dynamic-row review sheets
  +-- wait for code-side review of habitat placement contract
  +-- wait for optional-animation apply/proof outcomes
```

## Current Hard Boundaries

```text
do not start from visual-side
  |
  +-- runtime PNG mutation
  +-- source-board mutation
  +-- new sprite generation
  +-- candidate import
  +-- all-color propagation
  +-- prop-anchor edits
  +-- PET TASKS execution
  +-- build-vnext without -SkipAssetPrep
```

## Next Best Visual-Side Work

Recommended order:

1. Send updated code-side handoff prompt.
2. Send Sprite Workflow App thread prompt if that thread needs it.
3. Wait for code-side optional-animation proof/apply outcomes before planning further mutation.
4. Prepare final handoff after code-side returns its next review.

## Current Code-Side Ask

Code-side should review:

```text
docs\WEVITO_OVERLAY_DESIGN_CODE_SIDE_HANDOFF_PROMPT_2026-05-05.md
```

Main ask:

- validate overlay-first UI contract
- plan safe PET TASKS visual refinement
- keep PET TASKS report-only
- treat Clipboard Shelf as manual-save only
- recognize color repair queue is currently empty
- recognize care/object art gap is content mapping, not repainting
