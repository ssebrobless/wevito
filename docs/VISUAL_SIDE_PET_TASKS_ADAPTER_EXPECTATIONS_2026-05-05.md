# Visual-Side PET TASKS Adapter Expectations

Updated: 2026-05-05

This document answers the code-side Phase 10 coordination questions for the
PET TASKS / pet-agent helper adapter system.

It does not authorize visual generation, sprite import, runtime PNG mutation,
source-board mutation, all-color propagation, or broad optional expansion.

## Current Shape

```text
PET TASKS adapter rollout
  |
  +-- first safe lane
  |     +-- localDocs
  |     +-- spriteAudit
  |     +-- report-only proof summaries
  |
  +-- later lane
  |     +-- proofCapture contact sheets
  |     +-- Godot packaged proof commands
  |     +-- vNext proof commands
  |
  +-- blocked lane
        +-- sprite apply/import
        +-- runtime/source PNG mutation
        +-- visual generation
        +-- all-color propagation
```

## Answers To Code-Side Questions

### 1. First PET TASKS Proof Surface

Use `report-only proof` first.

Recommended order:

```text
PET TASKS proof adapters
  |
  +-- Phase A: report-only proof
  |     +-- no builds
  |     +-- no Godot launch
  |     +-- no vNext launch
  |     +-- read existing files and write markdown/json/contact sheets only
  |
  +-- Phase B: Godot packaged proof
  |     +-- only after report-only adapter is boring and reliable
  |     +-- best proof surface for optional animation names and runtime overlays
  |
  +-- Phase C: vNext proof
        +-- after optional animation addressing/proof capture is mature there
```

Rationale: current visual-side needs deterministic non-mutating review packets
more than UI-triggered packaged proof. Godot remains the first real packaged
proof surface for optional ball animations because it currently supports
optional animation names and carried-item overlays.

### 2. Existing Scripts And Artifact Conventions

Code-side should prefer existing scripts where they already match the task:

```text
tools\audit_optional_animation_readiness.py
tools\audit_optional_handoff_packs.py
tools\report_runtime_canvas_mismatches.py
tools\prepare_wevito_animation_run.py
```

Use `prepare_wevito_animation_run.py` cautiously. It belongs in explicit
animation-run workflows, not generic spriteAudit, and should not be used by
PET TASKS to create candidate/apply packets without separate approval.

Artifact convention for visual/report adapters:

```text
vnext\artifacts\visual-review\<yyyymmdd-slug>\
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
        +-- *.png
        +-- *.gif only when useful
```

Artifact convention for animation candidate/proof workflows:

```text
vnext\artifacts\animation-runs\<yyyymmdd-target-slug>\
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames\      only for explicit candidate workflows
  +-- backup-before-*       only for explicit apply workflows
  +-- qa\
  +-- proof-summary.md      for packaged proof
```

PET TASKS should not invent a parallel artifact layout.

### 3. spriteAudit Output Format

For `spriteAudit`, output all three when possible:

```text
spriteAudit output
  |
  +-- markdown report
  |     +-- human-readable decision notes
  |
  +-- JSON report
  |     +-- machine-readable paths, hashes, counts, issue classes
  |
  +-- contact sheet
        +-- only if the audit has visual findings or sampled examples
```

Minimum acceptable output is markdown plus JSON. Contact sheets should be
required for visual flags such as noise, crop, detached pixels, palette drift,
overlay contact, or candidate-vs-current comparisons.

### 4. proofCapture Overlay Metadata

Yes. `proofCapture` contact sheets for ball or drink families should include
runtime overlay metadata by default when metadata exists.

Required rule:

```text
proofCapture
  |
  +-- raw sprite/contact view
  |     +-- shows the actual pet PNG frames
  |
  +-- overlay view
        +-- uses prop_anchors.json when available
        +-- labels metadata key and family
        +-- labels offline approximation vs packaged proof
```

For `goose / baby / female / blue / drop_ball`, the ball must remain runtime
overlay only. Do not bake the ball into PNGs.

### 5. Visual-Side Artifact Folders Code-Side Must Not Write Into

PET TASKS adapters should write only new timestamped artifact folders.

Do not write into existing visual-side or hand-authored packets unless the task
explicitly names that exact folder and the user approves it:

```text
vnext\artifacts\animation-runs\20260504-goose-baby-female-blue-hold-ball-pilot\
vnext\artifacts\animation-runs\20260505-goose-baby-female-blue-drop-ball-candidate\
vnext\artifacts\animation-runs\20260505-goose-baby-female-blue-optional-expansion-review\
vnext\artifacts\visual-review\20260505-goose-baby-female-six-color-preflight\
vnext\artifacts\visual-review\20260505-goose-baby-female-ball-overlay-preflight\
```

Also avoid writing into:

```text
candidate-frames\
backup-before-*\
godot-packaged-proof-*\
```

unless the adapter is in an explicitly approved apply/proof workflow.

Preferred PET TASKS write target:

```text
vnext\artifacts\pet-tasks\<yyyymmdd-hhmmss-task-slug>\
  +-- task-card.json
  +-- policy-preview.json
  +-- manifest.json
  +-- run-summary.md
  +-- qa\
```

If code-side would rather keep all visual artifacts under
`vnext\artifacts\visual-review`, use a `pet-task-` prefix in a new folder name:

```text
vnext\artifacts\visual-review\<yyyymmdd-pet-task-sprite-audit-slug>\
```

### 6. Goose drop_ball Apply/Proof Ownership

Yes. Keep the goose `drop_ball` one-row apply/proof outside PET TASKS for now.

Reason:

```text
goose / baby / female / blue / drop_ball
  |
  +-- mutates runtime PNGs
  +-- requires exact hash checks
  +-- requires backup and rollback
  +-- depends on accepted hold_ball endpoint protection
  +-- should remain hand-controlled
```

Code-side owns that manual one-row Godot apply/proof/rollback path. PET TASKS
should not run it, wrap it, or approximate it yet.

## First Adapter Recommendation

Implement either `localDocs` or `spriteAudit` first.

Preferred first `spriteAudit` behavior:

```text
input
  |
  +-- target path or target row
  +-- dry-run preview
  +-- policy check
  |
  +-- approved run
        |
        +-- read PNGs/docs/metadata
        +-- write markdown report
        +-- write JSON report
        +-- write contact sheet if visual flags exist
        +-- no runtime/source mutation
```

Allowed side effects:

- new markdown/JSON/contact-sheet artifacts under a new task folder
- task-card status/log updates

Forbidden side effects:

- PNG writes under `sprites_runtime`
- PNG writes under `sprites_shared_runtime`
- source-board writes
- visual generation
- imports or normalization
- prop-anchor metadata edits
- applying candidate frames
- build or proof commands unless that adapter phase is separately approved

## Summary Decision

```text
visual-side answer
  |
  +-- first proof surface: report-only
  +-- first adapter: localDocs or spriteAudit
  +-- spriteAudit output: markdown + JSON + contact sheet when useful
  +-- proofCapture overlays: yes by default when metadata exists
  +-- write only new timestamped artifact folders
  +-- goose drop_ball apply/proof stays manual/code-side, outside PET TASKS
```

## Phase 11/12 Visual Review

Follow-up review:

```text
docs\VISUAL_SIDE_PET_TASKS_PHASE11_12_REVIEW_2026-05-05.md
```

Result:

```text
localDocs preview adapter: visually safe
spriteAudit preview adapter: visually safe
next dry-run dispatcher: visually safe if it remains preview/report-only
```
