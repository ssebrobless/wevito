# Wevito Claude Design Overlay Integration Plan

Date: 2026-05-05

Purpose: convert the Claude Design Phase 1B concepts into a concrete implementation plan that preserves Wevito's current desktop-pet overlay as the core experience.

This is a visual-side planning document. It does not authorize runtime PNG mutation, sprite import, visual generation, prop-anchor edits, PET TASKS execution adapters, or broad vNext implementation.

## Core Decision

Do not replace the current Wevito overlay with Claude's dashboard.

The always-on transparent desktop pet overlay is the game. Claude's designs should become summoned tool surfaces, drawers, workbenches, and review dashboards that attach to that living overlay.

```text
current Wevito value
  |
  +-- pets roam, idle, sleep, react, and live on the desktop
  +-- transparent/pass-through overlay remains available
  +-- pinned HUD stays optional
  +-- tool popups appear only when summoned
  |
  v
Claude Design value
  |
  +-- stronger organization
  +-- clearer task states
  +-- proof-before-mutation workflow
  +-- helper-pet task cards
  +-- review dashboards
  |
  v
target product shape
  |
  +-- living overlay first
  +-- compact HUD second
  +-- expandable workbenches third
```

## Observed Claude Design Artifacts

Claude Design is now rendering the Phase 1B prototype. Raw observed screenshots are preserved here:

```text
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\
  +-- 01-sprite-workflow-v2-console-visible.png
  +-- 02-pet-command-bar-and-learning-lab-start.png
  +-- 03-creative-learning-lab-and-handoff-start.png
  +-- 04-implementation-handoff-notes-visible.png
```

The four Claude examples visible in the canvas are:

| Claude example | Keep | Reinterpret for current Wevito |
| --- | --- | --- |
| Sprite Workflow V2 / One-Screen Console | Yes | Separate summoned workbench, not the normal pet overlay. |
| Pet Command Bar + Three Helpers | Yes | Compact PET TASKS popup/drawer that can be opened from the existing overlay. |
| Creative Learning Lab | Later | Separate review dashboard for examples, datasets, labels, and evals. |
| Implementation Handoff Notes | Yes | Design-to-code contract and ownership map. |

## Product Shape

```text
desktop
  |
  +-- layer 0: living pet overlay
  |     +-- transparent
  |     +-- always-on-top
  |     +-- pass-through when unfocused
  |     +-- roaming pets remain visible
  |
  +-- layer 1: compact HUD
  |     +-- status
  |     +-- actions
  |     +-- tools
  |     +-- settings
  |
  +-- layer 2: summoned drawers/popups
  |     +-- PET TASKS
  |     +-- link bin / future clipboard shelf
  |     +-- care/tool detail panels
  |
  +-- layer 3: full workbenches
  |     +-- Sprite Workflow V2
  |     +-- Creative Learning Lab
  |     +-- visual QA/contact-sheet review
  |
  +-- layer 4: external proof/art tools
        +-- Godot packaged proof
        +-- source/candidate folders
        +-- Claude Design/Figma-style prototypes
```

Important behavior: opening a large workbench should not dismiss or replace the pet world by default. It can dim, dock beside, or temporarily pin the overlay, but the pets should still feel like they are present and alive.

## Overlay Preservation Rules

1. The pet overlay remains the default first screen.
2. Desktop roaming, idle/walk/sleep/action animations remain visible whenever practical.
3. Tool surfaces are summoned from the overlay, not used as a new landing page.
4. PET TASKS remains a report-only helper surface until code-side explicitly expands it.
5. Workbenches should be separate windows or large panels, not stuffed into the tiny pet HUD.
6. Mutating visual workflows must keep the proof/apply/rollback gate from the Claude Sprite Workflow design.
7. The user should always be able to get back to the quiet pet home state quickly.

## Claude Design Concept Adoption

### 1. Sprite Workflow V2

Adopt as a large, no-scroll workbench for sprite proof/review/apply flows.

Keep:

- queue column
- current target row
- source/runtime/candidate/proof strips
- validator findings
- provenance/hash/geometry panel
- approval gate
- dry-run/apply/rollback/export controls
- visible audit trail

Do not put this entire surface inside the current overlay HUD. It is too dense for the living-pet surface. It should open from a tool button, PET TASKS report, or future Sprite Workflow App entry point.

### 2. Pet Command Bar + Three Helpers

Adopt as the design direction for PET TASKS, but keep the current report-only boundary.

Keep:

- helper-pet slots
- helper role labels
- task drafting state
- command input
- generated task card
- approval/review/block states
- audit log
- Tool Broker ownership

Reinterpret:

- helper pets are not replacement pets
- helper cards should reference existing pets when possible
- small mood/status changes can appear in the overlay while the detailed task card lives in the popup
- execution remains unavailable until code-side explicitly implements safe adapters

### 3. Creative Learning Lab

Adopt as a later review dashboard, not near-term runtime UI.

Use for:

- raw vs cleaned example review
- accepted/rejected labels
- preference comparisons
- dataset bundle status
- eval benchmark status
- release readiness

Near-term visual-side use is planning and contact-sheet review. Do not connect it to training, data export, or automatic imports yet.

### 4. Implementation Handoff Notes

Adopt immediately as a coordination model.

Use it to keep every UI concept mapped to:

- owner
- source data
- allowed side effects
- proof artifacts
- mutation gates
- rollback expectations

## Concrete Implementation Phases

### Phase 1 - Overlay Integration Contract

Owner: visual-side planning, then code-side review.

Deliverables:

- this plan
- a compact overlay layer contract
- screenshots of the four Claude examples
- a code-side prompt asking them to validate the proposed UI layering

Definition of done:

- everyone agrees that the current pet overlay remains the primary product surface
- large Claude dashboards are classified as summoned workbenches, not replacements

### Phase 2 - Visual UI Translation Spec

Owner: visual side.

Create a doc that translates Claude's visual language into Wevito-specific components:

- compact HUD buttons
- pet helper cards
- task cards
- approval gates
- proof strips
- status chips
- audit log rows
- warning/error visual states
- no-scroll workbench rules

This should include what to copy, what to simplify, and what to ignore from Claude Design.

### Phase 3 - PET TASKS Visual Refinement Plan

Owner: visual side for specs, code-side for implementation.

Use Claude's Pet Command Bar design to improve current PET TASKS without changing its safety model:

- clearer helper slots
- visible `PREPARE` vs `PREVIEW` distinction
- report-only badge
- disabled execution affordance
- task-card timeline
- compact output path display
- audit trail grouped by pet/helper

Blocked in this phase:

- execution adapters
- sprite mutation
- Godot proof launch
- candidate apply

### Phase 4 - Sprite Workflow V2 Workbench Plan

Owner: visual side and the other Sprite Workflow App thread.

Use Claude's first artboard as the organizing reference for the separate Sprite Workflow App:

- queue on left
- selected row in center
- source/runtime/candidate/proof strips
- findings/provenance/action gate on right
- no mandatory vertical scrolling

This phase should coordinate with the other thread before implementation because that thread owns the Avalonia/.NET Sprite Workflow App work.

### Phase 5 - Visual QA And Cleanup Continuation

Owner: visual side.

Continue current asset work using the existing cleanup artifacts:

- all-animal color variant coverage is complete across runtime/authored/verified/legacy roots
- shared runtime/source cleanup is mostly complete
- remaining dirty visual work should be targeted only when a concrete visual problem is identified
- optional animation expansion remains coordinated with code-side apply/proof gates

Do not run broad sprite rewrites just because a dashboard exists.

### Phase 6 - Functional Pet Helper Concepts

Owner: visual side planning, code-side later.

Plan useful pet functions that fit the overlay fantasy:

| Function | Overlay expression | Tool reality | First safe mode |
| --- | --- | --- | --- |
| Sprite audit | pet inspects a contact sheet | PET TASKS `spriteAudit` | report-only |
| Local docs lookup | pet fetches a note | PET TASKS `localDocs` | report-only |
| Link bin | pet keeps a small basket | existing webtools/link bin | existing safe tool |
| Clipboard shelf | pet holds copied snippets | future webtool slot | manual/review-only |
| Proof summary | pet reviews a proof packet | reads existing artifacts | report-only |
| Care reminder | pet reacts to needs | simulation/action state | existing action UI |
| Visual QA queue | pet waits by a review pile | Sprite Workflow V2 queue | planning only |

The pets should feel like they are helping because they live in the overlay, not because the app becomes an enterprise dashboard.

### Phase 7 - Creative Learning Lab Planning

Owner: visual side.

Turn Claude's Creative Learning Lab artboard into a reviewed-data plan:

- what counts as a raw example
- what counts as cleaned
- how accept/reject/revise labels work
- what belongs in preference comparisons
- what evidence is needed before a bundle is trusted

This stays separate from runtime sprite mutation and training until a later explicit approval.

### Phase 8 - Code-Side Handoff

Owner: visual side prepares, code-side reviews.

Prepare one copy-paste prompt for code-side containing:

- this document path
- Claude screenshot artifact path
- PET TASKS visual refinement expectations
- Sprite Workflow V2 workbench interpretation
- current no-mutation boundaries
- next proposed implementation slices

Code-side should then decide which UI slice is safest to implement after its current work.

## Immediate Next Course Of Action

```text
now
  |
  +-- finish this integration plan
  |
  +-- create visual UI translation spec
  |
  +-- continue asset/visual cleanup only through targeted audits
  |
  +-- coordinate Sprite Workflow V2 with the other thread
  |
  +-- wait for explicit approval before any new visual generation/import
```

## Non-Negotiables

- Do not abandon or hide the free-roaming pet overlay.
- Do not turn Wevito into a static dashboard-first app.
- Do not use Claude Design as production code truth.
- Do not let PET TASKS mutate assets until code-side explicitly implements and validates an execution adapter.
- Do not bake runtime overlay props into pet frames.
- Do not run asset-prep builds while visual cleanup is active unless explicitly approved.

