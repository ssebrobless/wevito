# Wevito Overlay-First Implementation Roadmap

Date: 2026-05-05

Purpose: organize the next visual/product implementation phases after reviewing the rendered Claude Design Phase 1B examples.

This roadmap keeps Wevito's living desktop-pet overlay as the primary experience. It treats larger Claude Design panels as summoned tools, not replacements for the overlay.

## North Star

```text
Wevito
  |
  +-- living desktop pets
  |     +-- always-on-top transparent overlay
  |     +-- pass-through when unfocused
  |     +-- pinned HUD when needed
  |     +-- pets roam, idle, sleep, react, and use habitat
  |
  +-- compact player HUD
  |     +-- needs
  |     +-- care actions
  |     +-- tools
  |     +-- settings
  |
  +-- summoned work surfaces
        +-- PET TASKS popup
        +-- link bin / clipboard shelf
        +-- Sprite Workflow V2 workbench
        +-- Creative Learning Lab
        +-- visual QA/contact-sheet review
```

The overlay should feel like the home. The work surfaces should feel like rooms the pets can bring you to.

## Current Evidence

| Area | Current status | Evidence |
| --- | --- | --- |
| Claude Design Phase 1B | Rendered successfully after usage reset. | `vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\` |
| Color variant coverage | Complete across runtime, authored, authored verified, and legacy sprite roots. | `docs\WEVITO_ALL_ANIMAL_COLOR_VARIANT_COVERAGE_2026-05-05.md` |
| Shared/source cleanup | Runtime/shared/source cleanup is largely complete; remaining flags are classified tight-but-clean or intentional. | `docs\WEVITO_SPRITE_CLEANUP_PROGRESS_2026-05-04.md` |
| PET TASKS | Live as report-only helper surface. | `docs\VISUAL_SIDE_PET_TASKS_ADAPTER_EXPECTATIONS_2026-05-05.md` and code-side handoff |
| vNext game action readiness | Code-side reports runtime action probe and full tests green in its worktree. | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-05.md` |

## Phase 1 - Overlay Preservation Contract

Status: complete.

Output:

- `docs\WEVITO_CLAUDE_DESIGN_OVERLAY_INTEGRATION_PLAN_2026-05-05.md`
- `docs\WEVITO_CLAUDE_DESIGN_VISUAL_TRANSLATION_SPEC_2026-05-05.md`
- `vnext\artifacts\design\phase1b-claude-design\observed-render-20260505\`

Gate:

- The free-roaming desktop-pet overlay remains the default product surface.
- Claude's large panels become summoned tools/workbenches.
- No code/runtime/sprite mutation is implied by the design docs.

## Phase 2 - PET TASKS Visual Refinement

Owner: visual-side spec, code-side implementation.

Goal: make PET TASKS feel like a pet-helper surface while keeping it report-only.

```text
PET TASKS vNext
  |
  +-- helper strip
  |     +-- 3 helpers
  |     +-- name / species / role / availability
  |
  +-- command input
  |     +-- PREPARE: draft a task card
  |     +-- PREVIEW: produce/read a report-only artifact
  |
  +-- task card
  |     +-- target
  |     +-- adapter
  |     +-- risk
  |     +-- policy result
  |     +-- artifact path
  |
  +-- timeline / audit
  |
  +-- safety footer
        +-- REPORT ONLY
        +-- no execution
        +-- no sprite mutation
```

Deliverable:

- `docs\WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md`

Allowed:

- UI planning
- clearer labels
- artifact open/copy affordance recommendations
- contact-sheet recommendation for future `spriteAudit`

Blocked:

- `EXECUTE`
- generation/import/apply
- Godot proof launch
- runtime/source PNG mutation

## Phase 3 - Sprite Workflow V2 Workbench Integration

Owner: visual-side spec, Sprite Workflow App thread/code-side implementation.

Goal: use Claude's first artboard as the organization model for a separate no-scroll workbench.

```text
Sprite Workflow V2
  |
  +-- Queue
  |
  +-- Current Row
  |
  +-- Visual Evidence
  |     +-- source
  |     +-- runtime
  |     +-- candidate
  |     +-- proof
  |
  +-- Findings
  |
  +-- Provenance
  |
  +-- Actions
        +-- dry-run
        +-- apply
        +-- rollback
        +-- export
```

Deliverable:

- `docs\WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md`

Key rule:

- This is a workbench opened from the overlay/tool lane, not the overlay itself.

## Phase 4 - Useful Pet Helper Function Plan

Owner: visual-side planning, code-side later.

Goal: define practical functions pets can perform without turning them into generic dashboards.

Deliverable:

- `docs\WEVITO_PET_HELPER_FUNCTIONS_ROADMAP_2026-05-05.md`

Initial safe functions:

- localDocs summary
- spriteAudit report-only preview
- link bin / URL basket
- Clipboard Shelf planning
- proof summary from existing artifacts
- care reminders and action suggestions
- visual QA queue summaries

## Phase 5 - Visual Asset Continuation

Owner: visual side.

Goal: continue visual work without broad risky asset rewrites.

```text
asset lane
  |
  +-- already complete
  |     +-- shared asset cleanup
  |     +-- source shared sync
  |     +-- color variant coverage
  |
  +-- continue only when targeted
  |     +-- contact-sheet review
  |     +-- optional animation row QA
  |     +-- visual artifact cleanup if concrete flags appear
  |
  +-- code-side owned
        +-- apply/proof pilots
        +-- runtime PNG mutation
        +-- source-board import
```

Next safe visual work:

- review generated contact sheets
- classify concrete visual errors
- prepare candidate prompts or cleanup specs
- keep `goose / baby / female / blue / drop_ball` outside PET TASKS until code-side completes/coordinates apply/proof

## Phase 6 - Clipboard Shelf And Webtools Expansion

Owner: visual-side planning, code-side later.

Goal: adapt the Hatch Pet-inspired "webtools slot 2" idea into Wevito's overlay.

Design target:

```text
TOOLS
  |
  +-- Link Bin
  |
  +-- Clipboard Shelf
  |     +-- saved snippets
  |     +-- source app/title when available
  |     +-- timestamp
  |     +-- quick copy
  |     +-- clear/delete
  |
  +-- PET TASKS
  |
  +-- future proof/review tools
```

Rule:

- Clipboard Shelf should be a small helper shelf, not a surveillance/logging surface. It should only save what the user explicitly asks it to keep.

## Phase 7 - Creative Learning Lab Plan

Owner: visual-side planning.

Goal: turn Claude's third artboard into a later reviewed-data surface.

```text
Creative Learning Lab
  |
  +-- raw examples
  +-- cleaned examples
  +-- labels
  +-- preference comparisons
  +-- dataset bundles
  +-- eval benchmarks
  +-- release readiness
```

Blocked until explicit future approval:

- training data export
- automatic imports
- broad source/runtime mutation
- unreviewed example promotion

## Phase 8 - Cross-Thread Handoff

Owner: visual side.

Goal: provide code-side and the Sprite Workflow App thread a single prompt with:

- docs created in this phase
- Claude screenshot artifact paths
- overlay preservation rules
- PET TASKS refinement request
- Sprite Workflow V2 workbench request
- current no-mutation boundaries

Deliverable:

- `docs\WEVITO_OVERLAY_DESIGN_CODE_SIDE_HANDOFF_PROMPT_2026-05-05.md`

## Audit Rules

Before any implementation request leaves visual-side:

- confirm the overlay remains primary
- confirm PET TASKS remains report-only unless code-side explicitly changes that
- confirm no sprite mutation is requested accidentally
- confirm Sprite Workflow V2 is a separate workbench
- confirm generated/imported assets are not requested unless the user explicitly approves that lane

