# Visual-Side Code Readiness Reconciliation

Updated: 2026-05-05

This reconciles the latest code-side handoff with visual-side cleanup and QA
planning.

Reviewed code-side handoff:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-05.md
```

Supporting code-side docs:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE17_PET_TASKS_LIVE_UI_PROBE_2026-05-05.md
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE18_GAME_INTERACTION_READINESS_2026-05-05.md
```

Supporting code-side artifacts:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-task-probes\20260505-014813\summary.json
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260505-015533\summary.json
C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\pet-tasks\20260505-054827-spriteaudit-reviewsprites\run-summary.md
```

## Current Shape

```text
code-side readiness
  |
  +-- PET TASKS
  |     +-- live in vNext
  |     +-- localDocs dry-run preview
  |     +-- spriteAudit dry-run report
  |     +-- live UI probe passed
  |     +-- report-only, no Execute mode
  |
  +-- game interaction readiness
  |     +-- doctor / medicine / bath / groom passed
  |     +-- feed / water / play / rest passed
  |     +-- home / settings-save / basket tools passed
  |     +-- vNext tests pass 79 / 79
  |
  +-- visual asset safety
        +-- build-vnext must use -SkipAssetPrep
        +-- no asset regeneration unless explicitly approved
```

## Visual-Side Assumptions Updated

Visual-side can assume:

- PET TASKS can produce non-mutating report artifacts.
- PET TASKS report output currently includes markdown and JSON only.
- PET TASKS contact-sheet generation is still deferred.
- PET TASKS simple target phrases work for rows like
  `review goose baby female blue sprites`.
- PET TASKS output should stay under timestamped
  `vnext\artifacts\pet-tasks\...` folders.
- vNext action/tool controls are currently operational in code-side validation.
- Full vNext code tests pass in the code-side worktree.

Visual-side must not assume:

- PET TASKS can apply sprite candidates.
- PET TASKS can run Godot packaged proofs.
- PET TASKS can generate/import/normalize sprite assets.
- PET TASKS can edit prop anchors or shared prop assets.
- PET TASKS can safely drive all-color propagation.
- Code-side worktree docs/artifacts are already merged into main.

## Build Safety Rule

```text
safe while visual cleanup is active
  |
  +-- tools\build-vnext.ps1 -SkipAssetPrep
```

Avoid:

```text
tools\build-vnext.ps1
  |
  +-- without -SkipAssetPrep
        |
        +-- can run sprite cleaning/generation scripts
        +-- can rewrite runtime asset folders
        +-- can overwrite visual cleanup or candidate evidence
```

This applies to visual-side validation and code-side coordination while the
asset state is still being manually reviewed.

## PET TASKS Safe Use

Safe request shape:

```text
ask code-side or vNext PET TASKS:
  review <species> <age> <gender> <color> sprites
```

Example:

```text
review goose baby female blue sprites
```

Expected safe output:

```text
vnext\artifacts\pet-tasks\<timestamp>-spriteaudit-<slug>\
  +-- run-summary.md
  +-- sprite-audit-report.json
  +-- qa\       may be empty until contact sheets are implemented
```

The latest code-side sample report scanned 30 packaged goose baby female blue
frames, found 0 structural header findings, and confirmed no sprite mutation.

## Manual Workflow Still Required

The optional animation pilot remains outside PET TASKS:

```text
goose / baby / female / blue / drop_ball
  |
  +-- manual code-side apply/proof/rollback only
  +-- no PET TASKS apply
  +-- no PET TASKS Godot proof
  +-- no all-color propagation
```

Reason:

- it mutates runtime PNGs,
- it has strict hash and backup requirements,
- it depends on the accepted protected `hold_ball` endpoint,
- it needs hand-controlled rollback if proof fails.

## Next Visual-Side Work

Until the manual `drop_ball` proof returns, visual-side should stay in:

```text
non-mutating visual lane
  |
  +-- review packets
  +-- contact sheets
  +-- asset inventory notes
  +-- QA rubrics
  +-- cleanup guidance
  +-- code-side handoff prompts
```

Do not start:

- visual generation,
- sprite import,
- runtime/source PNG mutation,
- prop-anchor edits,
- candidate apply,
- all-color propagation,
- broad optional expansion.

## Summary Decision

```text
PET TASKS status: usable for report-only visual support
build policy: use -SkipAssetPrep
action readiness: green from code-side probe
manual drop_ball pilot: still outside PET TASKS
visual-side next lane: non-mutating QA/planning only
```
