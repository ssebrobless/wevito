# Wevito PET TASKS Visual Refinement Spec

Date: 2026-05-05

Purpose: translate the rendered Claude Design "Pet Command Bar + Three Helpers" artboard into a safe, overlay-compatible refinement plan for Wevito's existing PET TASKS popup.

This spec is visual-side guidance. PET TASKS remains report-only until code-side explicitly implements, validates, and coordinates any execution adapter.

## Current Boundary

```text
PET TASKS today
  |
  +-- PREPARE
  |     +-- parses command
  |     +-- creates task card
  |     +-- evaluates policy
  |
  +-- PREVIEW
        +-- localDocs
        +-- spriteAudit
        +-- report-only artifacts
        +-- no mutation
```

Do not add:

- sprite generation
- sprite import
- runtime/source PNG mutation
- prop-anchor edits
- Godot proof launch
- apply/proof/rollback controls
- `EXECUTE` mode

## Target Shape

```text
PET TASKS popup
  |
  +-- Header
  |     +-- PET TASKS
  |     +-- REPORT ONLY badge
  |     +-- helper count / queue count
  |
  +-- Helper Strip
  |     +-- Helper 1
  |     +-- Helper 2
  |     +-- Helper 3
  |
  +-- Command Row
  |     +-- input
  |     +-- PREPARE
  |     +-- PREVIEW
  |
  +-- Task Card
  |     +-- parsed target
  |     +-- adapter
  |     +-- policy
  |     +-- output artifact
  |
  +-- Timeline
  |     +-- drafted
  |     +-- policy checked
  |     +-- preview ready
  |     +-- blocked / cancelled
  |
  +-- Safety Footer
        +-- "Reports only. No tools run from here yet."
```

## Helper Strip

Use three helper slots because Claude Design's three-helper model maps cleanly onto the current concept without overwhelming the overlay.

Recommended display:

| Field | Example | Notes |
| --- | --- | --- |
| Name | Bean | User-facing helper name. |
| Species | frog | Can later map to an existing pet. |
| Role | sprite QA | Short and functional. |
| State | available / drafting / reviewing / blocked | Do not imply execution. |
| Current task | `review goose baby female blue sprites` | Single-line truncation is fine. |

Visual tone:

- small pixel avatar
- compact role chip
- readable status chip
- no giant profile cards
- no human assistant framing

## Command Row

Recommended wording:

| Control | Label | Meaning |
| --- | --- | --- |
| Text input | `Ask a helper to prepare a report...` | Avoid promising execution. |
| Button | `PREPARE` | Parse and create a task card. |
| Button | `PREVIEW` | Produce or read a report-only artifact. |

Useful examples:

```text
review goose baby female blue sprites
Bean, summarize latest sprite audit
Pip, find docs about color variants
Nix, review the last failed proof packet
```

Blocked examples until later:

```text
apply goose drop_ball
generate new fox sprites
import this candidate
run Godot proof
```

These should produce a clear blocked/policy message, not a silent failure.

## Task Card

Each prepared task should show:

- helper
- parsed intent
- parsed target
- adapter
- policy result
- risk level
- output location
- current status
- next allowed action

Suggested state labels:

```text
drafted
policy-checked
preview-ready
blocked
cancelled
done
failed
```

Use warnings plainly:

```text
Blocked: sprite mutation is not available from PET TASKS.
Blocked: Godot proof launch is not enabled here yet.
Blocked: visual generation requires explicit manual approval.
```

## Timeline / Audit

The timeline should be short enough to fit in the popup:

```text
14:21 Bean drafted spriteAudit
14:21 policy allowed report-only preview
14:22 preview wrote run-summary.md
14:22 hashes unchanged
```

The most important audit truth for visual-side:

- did it write artifacts?
- where?
- did sprite hashes change?
- was anything blocked?

## Artifact Controls

Future UI refinement should prioritize:

- `Open Report`
- `Copy Path`
- `Open Folder`

Do not add:

- `Apply`
- `Import`
- `Generate`
- `Run Proof`

until code-side explicitly creates those workflows.

## Contact-Sheet Recommendation

For future `spriteAudit` improvements, visual-side wants contact sheets when the report has visual findings.

Target output:

```text
vnext\artifacts\pet-tasks\<timestamp-task>\
  +-- task-card.json
  +-- policy-preview.json
  +-- manifest.json
  +-- run-summary.md
  +-- report.json
  +-- qa\
        +-- contact-sheet.png
```

Contact sheets should be required for:

- noise / detached pixels
- crop/tight-margin concerns
- palette drift
- source vs runtime mismatch
- optional animation visual review
- overlay metadata review

## Overlay Compatibility

PET TASKS should feel like a popup the pets brought forward, not a replacement for the desktop pet home.

Rules:

- preserve pass-through/unfocused overlay behavior
- keep popup closeable
- keep the pet world visible behind or beside the popup when practical
- avoid full-screen takeover for report-only tasks
- do not hide the main pet action buttons permanently

## Implementation Notes For Code-Side

Likely implementation surface:

- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`

Code-side should verify exact ownership before editing. This spec does not require a new architecture.

Recommended safest slices:

1. Add clearer `REPORT ONLY` and `NO EXECUTION` labels.
2. Improve prepared task-card visual hierarchy.
3. Add artifact path open/copy controls.
4. Add helper strip with three compact helper slots.
5. Add timeline grouping.
6. Only later add contact-sheet support to the `spriteAudit` adapter.

## Acceptance Checklist

- PET TASKS still runs report-only.
- No new mutation affordance appears.
- `PREPARE` and `PREVIEW` are visually distinct.
- Helper cards are compact and do not cover the pet overlay unnecessarily.
- Blocked tasks explain why they are blocked.
- Artifact paths are easy to open/copy.
- Existing localDocs and spriteAudit workflows remain intact.

