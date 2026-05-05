# Wevito Visual Completion Tracker

Date: 2026-05-05

Purpose: one concrete list of where the visual side stands and what remains before the visual/UI lane can be considered fully complete.

This tracker is planning and coordination only. It does not authorize code edits, runtime/source PNG mutation, sprite generation/import, prop-anchor edits, PET TASKS execution, or asset-prep builds.

## Completion Shape

```text
visual completion
  |
  +-- DONE
  |     +-- visual source-of-truth docs
  |     +-- overlay-first design direction
  |     +-- Claude Design screenshot/reference capture
  |     +-- broad sprite/shared asset cleanup
  |     +-- all color folders and color QA
  |     +-- care/medicine/object art review
  |     +-- habitat tier/loadout/anchor planning
  |
  +-- REMAINING
  |     +-- code-side UI implementation and visual review
  |     +-- PET TASKS visual polish
  |     +-- Sprite Workflow V2 workbench acceptance
  |     +-- care/item content mapping
  |     +-- habitat object runtime proof
  |     +-- optional animation proof/expansion
  |     +-- helper function UI surfaces
  |     +-- final in-game visual QA
  |
  +-- GATED
        +-- sprite mutation
        +-- generation/import
        +-- all-color propagation
        +-- prop-anchor edits
        +-- PET TASKS execution
```

## Current State

| Area | Current State | Visual-Side Decision |
| --- | --- | --- |
| Source-of-truth docs | Green | Prefer the current dashboard, final sweep, and this tracker over older historical docs when statuses differ. |
| Overlay product direction | Green planning / code pending | Keep the roaming desktop-pet overlay as Wevito's home surface. Summon tools as popups/workbenches. |
| Claude Design references | Green | Four rendered examples captured and indexed. Use them as visual references only, not implementation truth. |
| PET TASKS | Yellow / code pending | Keep report-only. Improve labels, hierarchy, artifact actions, helper cards. No execution yet. |
| Sprite Workflow V2 | Yellow / other-thread pending | Keep as separate workbench/app. Use one-screen workflow reference. Mutation requires proof/rollback gates. |
| Color variant coverage | Green | All required six-color folders exist across the known sprite roots. |
| Color variant quality | Green | Index review, walk-motion review, dark-background check, and tall-row atlas follow-up are complete. No repair queue. |
| Shared asset cleanup | Green | Broad cleanup is complete. Future work should be targeted only. |
| Care/medicine/object art | Green/yellow | Art pool is broad and mostly clean. Remaining gap is content/UI mapping and runtime use. |
| Habitat objects | Green/yellow | Tiered loadout model and five-species placement/anchor contract exist. Runtime proof remains. |
| Optional animations | Gated | Code-side/manual apply/proof owns runtime mutation. Visual-side reviews proof outputs and plans next rows. |
| Clipboard Shelf | Yellow / code pending | Manual-save-only spec exists. Needs code-side storage/privacy implementation and visual proof. |
| Creative Learning Lab | Yellow / future | Read-only/review-first dashboard plan exists. No training/export/promotion flow yet. |

## Done List

```text
completed visual-side foundation
  |
  +-- visual status dashboard
  +-- visual final doc sweep
  +-- overlay-first integration plan
  +-- Claude Design visual translation
  +-- Claude screenshot index and approval map
  +-- color coverage audit
  +-- color quality reviews
  +-- tall-row atlas follow-up
  +-- care/medicine/object review packet
  +-- habitat loadout mockups
  +-- refined habitat three-object model
  +-- habitat placement/anchor contract
  +-- PET TASKS expectations/spec
  +-- Clipboard Shelf visual spec
  +-- Creative Learning Lab plan
```

Key evidence docs:

```text
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_STATUS_DASHBOARD_2026-05-05.md
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_FINAL_DOC_SWEEP_2026-05-05.md
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CLAUDE_DESIGN_SCREENSHOT_INDEX_2026-05-05.md
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_ATLAS_LAYOUT_FOLLOWUP_2026-05-05.md
C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md
```

## Remaining Work By Phase

### Phase 1 - Code-Side Reconciliation

Goal: make sure code-side accepts the current visual source-of-truth before implementation.

Remaining:

- code-side reads the current visual handoff prompt
- code-side identifies conflicts with its latest work
- code-side returns a single prompt back to visual-side
- visual-side updates this tracker if code-side changes the plan

Done when:

- both threads agree that overlay-first, report-only PET TASKS, and no visual mutation are still the current boundaries

### Phase 2 - Overlay UI Implementation Review

Goal: get the overlay-first UI plan implemented safely and visually reviewed.

Remaining:

- code-side validates overlay layering in vNext
- code-side keeps pass-through/unfocused/pinned behavior intact
- visual-side reviews screenshots or live UI
- visual-side verifies the pet remains the product center

Done when:

- current roaming overlay still feels alive and primary
- tool surfaces open as summoned UI, not a replacement dashboard
- before/after screenshots are captured

### Phase 3 - PET TASKS Visual Polish

Goal: make PET TASKS clear, useful, and safe while it remains report-only.

Remaining:

- add or review `REPORT ONLY` and `NO EXECUTION` labels
- refine helper cards or helper strip
- improve prepared task-card hierarchy
- add safe Open Report / Copy Path / Open Folder affordances
- visually distinguish PREPARE from PREVIEW
- optionally add contact-sheet output to spriteAudit reports

Blocked:

- execution adapter UI
- proof launch
- sprite generation/import/apply
- prop-anchor edits

Done when:

- a user can tell what PET TASKS can and cannot do at a glance
- report artifacts are easier to find/open
- no one mistakes the feature for an automation executor

### Phase 4 - Sprite Workflow V2 Workbench

Goal: align the separate Sprite Workflow App/workbench with the Claude Design one-screen reference.

Remaining:

- other thread/code-side compares current workbench against the visual spec
- queue, selected row, source/runtime/candidate/proof strips are visible without decision-critical scrolling
- provenance/hash, validator findings, dry-run/apply/rollback states are clear
- visual-side reviews workbench screenshots

Done when:

- one selected sprite row can be reviewed end-to-end in one surface
- mutation-capable controls are visibly gated by proof, hashes, approval, and rollback

### Phase 5 - Care, Medicine, And Item Content Mapping

Goal: connect the clean object art pool to real gameplay/UI item records.

Remaining:

- decide which of the 81 shared item/object PNGs become first-class content
- map medicine/care items to actions and UI labels
- confirm small-size readability inside actual UI
- decide inventory grouping: care, medicine, food, container, toy, utility, habitat
- visual-side reviews code-side/content implementation screenshots

Done when:

- core care/medicine objects are visible and understandable in UI
- doctor/medicine/bath/groom/feed/play/rest surfaces use the intended art
- missing mappings are documented instead of invisible

### Phase 6 - Habitat Runtime Placement

Goal: move habitat planning from mockups into a runtime-safe visual implementation.

Remaining:

- implement static primary anchors first
- implement active interaction object second
- implement depth/occlusion/contact-shadow rules third
- test baby/teen/adult scale against anchors
- expand beyond the five-species pilot only after first proof

First proof species:

```text
goose
rat
crow
snake
frog
```

Done when:

- each pilot species has a clean primary/interaction/decor composition
- pets do not visually collide with props
- age scale remains readable
- depth and contact shadows support the overlay rather than cluttering it

### Phase 7 - Optional Animation Expansion

Goal: expand optional animation quality without losing proof/rollback discipline.

Current known state:

- `goose / baby / female / blue / hold_ball` applied and proofed by code-side
- `goose / baby / female / blue / drop_ball` is code-side/manual proof scope
- ball remains runtime overlay only

Remaining:

- wait for code-side proof output for `drop_ball`
- visual-side reviews proof sheet and returns accept/revise/reject
- plan `pickup_ball` after drop endpoint is accepted
- plan `carry_ball_walk` and `carry_ball_run` contact stability
- only then plan all-color propagation
- only then consider next species/age/gender

Done when:

- one row has proven hold/drop/pickup/carry continuity
- overlay prop contact is stable
- no prop is baked into pet PNGs
- all-color propagation has its own proof packet

### Phase 8 - Helper Functions And Clipboard Shelf

Goal: make the pet useful as a helper without turning it into unsafe automation.

Remaining:

- code-side implements or plans Clipboard Shelf storage/privacy
- keep Clipboard Shelf manual-save-only
- design pet expression/state for saved snippets
- keep localDocs, spriteAudit, proof summaries, and visual QA queue summaries as safe first helpers
- visual-side reviews UI and artifact paths

Done when:

- helper surfaces are useful without being invasive
- clipboard history is not passively scraped
- user intent is explicit before saving or acting

### Phase 9 - Creative Learning Lab

Goal: preserve the useful reviewed-data dashboard idea without introducing risky training/import flows.

Remaining:

- decide if this belongs inside vNext or external tooling
- define read-only artifact index if code-side wants it
- keep accept/reject/revise review semantics
- defer training/export/data-promotion features

Done when:

- the lab is either explicitly deferred or implemented as read-only review
- no unreviewed examples can become production data automatically

### Phase 10 - Final In-Game Visual QA

Goal: verify the actual player-facing visual experience after implementation work lands.

Remaining:

- run safe build/probe only with current code-side-approved policy
- capture overlay screenshots
- capture PET TASKS screenshots
- capture care/medicine/item UI screenshots
- capture habitat runtime screenshots
- review optional animation proof outputs
- verify docs and implementation agree

Done when:

- screenshots prove the visual direction exists in the app
- no stale visual blockers remain
- all remaining issues are either fixed or explicitly deferred

### Phase 11 - Final Cross-Thread Handoff

Goal: end with both visual-side and code-side using the same current plan.

Remaining:

- update the code-side copy-paste prompt
- update the Sprite Workflow App prompt if needed
- point to this tracker and final evidence docs
- require code-side to return its own single copy-paste prompt after review

Done when:

- code-side, visual-side, and workflow-app work all point at the same current source-of-truth

## Visual Work That Is Currently Not Needed

```text
not needed right now
  |
  +-- broad sprite cleanup
  +-- broad recolor
  +-- recreating color folders
  +-- repainting care/medicine assets
  +-- more habitat art generation
  +-- new optional animation generation
  +-- all-color optional propagation
```

These can reopen only if a real proof or user review finds a visible defect.

## Current Blockers / Gates

| Gate | Owner | Why It Matters |
| --- | --- | --- |
| Runtime/source PNG mutation | Code-side/manual approval | Prevents unreviewed sprite changes and preserves rollback discipline. |
| Optional animation apply/proof | Code-side/manual approval | Needs hash/backup/rollback and Godot proof. |
| PET TASKS execution | Code-side future design | Current PET TASKS is intentionally report-only. |
| Prop-anchor edits | Code-side/manual approval | Affects runtime overlays and proof semantics. |
| Asset-prep build | Code-side/manual approval | Can regenerate runtime asset folders. |
| New generation/import | User + code/visual coordination | Must have manifest/provenance/proof/rollback first. |

## Final Definition Of Done

The visual side is fully complete when:

```text
done
  |
  +-- overlay-first UI is implemented and screenshot-reviewed
  +-- PET TASKS visual refinement is implemented and remains report-only
  +-- Sprite Workflow V2 workbench has accepted visual layout
  +-- care/medicine/item content mapping is visible in UI
  +-- habitat placement/depth/anchors are runtime-proofed
  +-- optional animation pilot path is accepted and expansion plan is proven
  +-- helper functions fit the pet overlay and privacy rules
  +-- Creative Learning Lab is either safely deferred or read-only
  +-- final visual QA screenshots exist
  +-- code-side and visual-side handoffs agree
```

