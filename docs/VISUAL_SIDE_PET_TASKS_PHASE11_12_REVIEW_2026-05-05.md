# Visual-Side Review Of PET TASKS Phase 11/12

Updated: 2026-05-05

This reviews code-side PET TASKS Phase 11 and Phase 12 from the visual-side
lane.

Reviewed docs:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE11_LOCAL_DOCS_PREVIEW_ADAPTER_2026-05-05.md
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE12_SPRITE_AUDIT_PREVIEW_ADAPTER_2026-05-05.md
C:\Users\fishe\Documents\projects\wevito\docs\VISUAL_SIDE_PET_TASKS_ADAPTER_EXPECTATIONS_2026-05-05.md
```

## Review Shape

```text
code-side PET TASKS progress
  |
  +-- Phase 11: localDocs preview
  |     +-- dry-run only
  |     +-- read-only
  |     +-- writes no files
  |     +-- no Shell execution wiring
  |
  +-- Phase 12: spriteAudit preview
        +-- dry-run report mode
        +-- read-only target validation
        +-- markdown + JSON output
        +-- new pet-tasks artifact folder only
        +-- no contact sheet yet
```

## Visual-Side Result

```text
visual-side review result: no objection
```

The implemented direction matches the visual-side safe lane:

- no visual generation
- no sprite import
- no runtime/source PNG mutation
- no prop-anchor edits
- no candidate apply
- no all-color propagation
- no Godot/vNext packaged proof execution from PET TASKS
- no writes into existing visual-side candidate/proof folders

## Phase 11 LocalDocs Notes

`localDocs` is safe as implemented because it is preview-only and writes no
files.

Visual-side expectation:

```text
localDocs
  |
  +-- may enumerate approved docs
  +-- may summarize preview metadata
  +-- should not rewrite docs
  +-- should not create visual artifacts
```

## Phase 12 SpriteAudit Notes

`spriteAudit` is safe as implemented because it:

- requires dry-run/report mode,
- writes only markdown and JSON under a `pet-tasks` artifact root,
- validates approved roots,
- preserves PNG hashes,
- rejects unsafe artifact folders,
- does not create or modify image assets.

The lack of contact sheet output is acceptable for this phase because markdown
plus JSON was the minimum safe output. Contact sheets should be a later adapter
phase.

## Contact Sheet Follow-Up Requirement

When code-side later adds spriteAudit contact sheets, visual-side recommends:

```text
spriteAudit contact sheet phase
  |
  +-- only generate sheets for sampled findings or explicit targets
  +-- keep original frame dimensions visible in labels
  +-- preserve native frame pixels
  +-- write only under new pet-tasks artifact folders
  +-- include hashes in JSON, not on crowded sheet labels
```

Contact sheets should be generated for:

- detached/noise findings,
- crop/margin findings,
- large/tiny frame findings,
- palette or color-variant comparisons,
- current-vs-candidate comparisons,
- overlay contact checks when proofCapture is later approved.

## Continued Boundary For Goose drop_ball

The manual optional-animation pilot remains outside PET TASKS:

```text
goose / baby / female / blue / drop_ball
  |
  +-- apply/proof owner: code-side manual workflow
  +-- PET TASKS involvement: none
  +-- visual-side involvement: review docs and non-mutating QA only
```

Reason:

- it mutates runtime PNGs,
- it requires exact hash checks,
- it requires backup/rollback,
- it depends on the accepted protected `hold_ball` endpoint,
- it should not become a generic helper-pet task yet.

## Next Safe Code-Side Step

Visual-side agrees the next safe code-side step is:

```text
dry-run adapter dispatcher
  |
  +-- selects localDocs or spriteAudit by tool family
  +-- remains preview/report-only
  +-- does not wire runtime execution to PET TASKS yet
  +-- does not run Godot/vNext proof commands
  +-- does not mutate image assets
```

## Summary

```text
Phase 11 localDocs preview: visually safe
Phase 12 spriteAudit preview: visually safe
dispatcher preview step: visually safe if still no-mutation
goose drop_ball apply/proof: keep manual and outside PET TASKS
```
