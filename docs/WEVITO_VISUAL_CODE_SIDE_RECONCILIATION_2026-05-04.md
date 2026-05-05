# Wevito Visual / Code-Side Reconciliation

Updated: 2026-05-04

This note records what the visual-side thread learned from the latest code-side
handoff in the separate worktree:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito
```

It is docs-only. It does not authorize generation, import, source/runtime PNG
mutation, broad asset rewrites, or runtime implementation.

## Current Shape

```text
code-side gate
  |
  +-- green now
  |     +-- sequence-stable runtime canvas contract
  |     +-- full vNext tests
  |     +-- Debug build/publish with -SkipAssetPrep
  |     +-- popup-aware action/tool probe
  |
  +-- still unresolved before visual mutation
        +-- manifest/provenance/apply workflow
        +-- optional animation addressing
        +-- proof surface for a one-row pilot
        +-- rollback execution path
        +-- asset-prep policy before builds that regenerate sprites_runtime
```

## Code-Side Evidence Read

| Evidence | Path |
| --- | --- |
| Handoff | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md` |
| Validation sweep | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md` |
| Runtime hardening | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md` |
| Visual readiness | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE5_VISUAL_READINESS_CHECK_2026-05-04.md` |
| Packaging readiness | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE6_COMMIT_PACKAGING_READINESS_2026-05-04.md` |
| Checklist | `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md` |

## Updated Gate Facts

| Topic | Previous visual assumption | Current code-side evidence |
| --- | --- | --- |
| Runtime canvas | 456 mixed-canvas rows were an active blocker. | 2880 sequences and 10800 frames pass with 0 mixed-canvas rows. |
| Full vNext tests | Full tests failed at sprite runtime coverage. | Full vNext tests pass 26 / 26. |
| Build/publish | Code-only publish had passed, broad readiness unclear. | Debug build/publish passes with `-SkipAssetPrep`. |
| Action/tool probe | Probe was green from an older pass. | Popup-aware probe is green at `20260504-205959`. |
| Visual mutation | Paused because runtime canvas/test gate was red. | Still paused, but now because production-safe apply/provenance workflow is missing. |

## Visual-Side Docs Updated

```text
docs/WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md
docs/WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md
docs/WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md
docs/WEVITO_VISUAL_HANDOFF_2026-05-04.md
docs/WEVITO_VISUAL_NEXT_EXECUTION_PLAN_2026-05-04.md
docs/WEVITO_POST_PILOT_EXPANSION_PLAN_2026-05-04.md
```

## What Visual Side Can Do Now

```text
allowed
  +-- no-edit contact sheets
  +-- no-edit color variant QA
  +-- no-edit optional animation review
  +-- visual rubric updates
  +-- gate/handoff doc updates citing code-side evidence
  +-- planning for manifest/provenance/apply workflow
```

## What Remains Paused

```text
paused
  +-- new visual generation
  +-- sprite import
  +-- runtime PNG mutation
  +-- source PNG mutation
  +-- broad asset rewrites
  +-- one-row production pilot unless explicitly approved
```

## Next Visual Recommendation

Keep reviewing and planning without mutation. The next useful visual-side work
is either:

1. continue no-edit contact-sheet QA, or
2. help define acceptance criteria for the manifest/provenance/apply workflow
   that code-side needs before the goose hold-ball candidate can be applied.

The old canvas failure should no longer be used as the reason to block visual
production. The current blocker is workflow safety.
