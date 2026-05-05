# Wevito Production Gate Check

Updated: 2026-05-04

This is Phase 8 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It decides whether visual production may move from planning into generation,
import, or runtime asset mutation.

Decision:

```text
production gate
  -> closed for generation
  -> closed for import
  -> closed for runtime/source sprite mutation
  -> open only for non-mutating proof/planning coordination
```

This document does not authorize any additional production mutation. A later
non-applied Phase 9 candidate exists, but it remains review-only until the user
explicitly approves an apply/proof step.

## Code-Side Reconciliation Update

Code-side handoff read:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md
```

Current code-side evidence from that worktree:

| Area | Current evidence |
| --- | --- |
| Runtime canvas contract | Green: 2880 sequences, 10800 frames, 0 mixed-canvas rows, 0 missing/count rows, 0 invalid/non-alpha PNG rows. |
| Full vNext tests | Green: 26 / 26. |
| Debug publish | Green with `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180`. |
| Popup-aware action/tool probe | Green at `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`. |

This supersedes the older visual-side assumption that full vNext tests were
blocked by 456 mixed-canvas rows. The active remaining blocker for visual
mutation is now the missing production-safe manifest/provenance/apply workflow,
plus optional-animation addressing and proof-surface coordination.

## Gate Shape

```text
Phase 8 gate
  |
  +-- code-side build/probe stability
  +-- sequence-stable canvas contract
  +-- manifest/provenance workflow
  +-- rollback path
  +-- QA proof requirements
  +-- one-row batch scope
  +-- explicit user approval
  |
  +-- result: wait, do not mutate assets yet
```

## Gate Matrix

| Gate | Current evidence | Status | Decision |
| --- | --- | --- | --- |
| Code-side build/probe | Code-side Phase 3/4/7 reports show full vNext tests passed 26 / 26, debug publish passed with `-SkipAssetPrep`, and popup-aware action/tool probe passed. | `pass_in_code_worktree` | Strong enough for no-edit visual QA and future proof planning. |
| Runtime canvas contract | Code-side final sequence-stable validation shows 2880 sequences, 10800 frames, 0 mixed-canvas rows, 0 missing/count rows, and 0 invalid/non-alpha PNG rows. | `pass_in_code_worktree` | The old mixed-canvas blocker is closed in the code-side worktree; do not treat 456 rows as current blocking evidence. |
| Canvas policy | `WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md` defines the correct visual rule: per-sequence natural canvases, transparent padding only, no forced 72x64 shrink/crop. | `policy_ready` | Keep as the rule for any future normalization or import work. |
| Manifest/provenance workflow | `WEVITO_ANIMATION_GENERATION_CONTRACT.md`, `WEVITO_ANIMATION_QA_RUBRIC.md`, schema, and goose prompt packet exist. | `planning_ready` | Ready as a contract for a future run, not proof that tooling has executed it. |
| Rollback path | Goose prompt packet defines backup path and apply rules. | `planning_ready` | Known in docs; must be executed by the production/import worker before apply. |
| Contact sheet review | Required by contract and prompt packet. | `required_not_run` | Future candidate must produce this before acceptance. |
| Preview GIF/video | Required by contract and prompt packet. | `required_not_run` | Future candidate must produce this before acceptance. |
| Packaged/runtime proof | Required by contract and prompt packet. | `required_not_run` | Future candidate must prove runtime view after any apply. |
| Batch scope | Phase 6/7 lock first target to one row: `goose / baby / female / blue / hold_ball`. | `pass` | Scope is correct. |
| Explicit user approval for mutation | User has asked to proceed through planning phases, but has not explicitly approved generation/import/runtime PNG mutation. | `blocked` | Do not proceed to Phase 9 until explicit approval is given. |

## Production Decision

```text
generate new visual candidate now?
  -> no

import candidate frames now?
  -> no

overwrite runtime/source sprites now?
  -> no

prepare a non-mutating proof/run folder later?
  -> yes, after coordination
```

The first production target remains:

```text
goose / baby / female / blue / hold_ball
```

But it is not approved for execution yet.

## Why The Gate Stays Closed

The project now has enough planning and code-side runtime confidence for
no-edit QA and a future disciplined pilot, but not enough workflow support or
permission to mutate assets.

Blocking factors:

| Blocker | Why it matters |
| --- | --- |
| No explicit mutation approval | Planning-phase `proceed` commands do not equal permission to generate/import/overwrite assets. |
| Manifest/apply workflow is documented, not production-executed | The first real apply still needs source hash capture, manifest write, validation, contact sheet, preview, packaged proof, and exact rollback. |
| Optional animation addressing is not first-class in vNext | `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, and `carry_ball_run` still need a coordinated runtime addressing decision before broad production. |
| Proof surface still needs coordination | Code-side and visual-side need to decide whether Godot, vNext, or both are required for the first applied proof. |

## What Is Allowed Next

Allowed without opening Phase 9:

```text
allowed
  +-- cite the code-side green runtime evidence
  +-- continue no-edit contact sheets and color QA
  +-- ask code-side to design/review manifest/apply feasibility
  +-- prepare non-mutating folder scaffolding only if explicitly requested
  +-- continue planning docs
  +-- produce copy-paste coordination prompts

not allowed
  +-- provider image generation
  +-- importing candidate frames
  +-- normalizing runtime PNGs
  +-- overwriting sprites_runtime or source boards
  +-- broad batch work
```

## Conditions To Open Phase 9

Phase 9 may begin only after all of these are true:

| Condition | Required state |
| --- | --- |
| User approval | Explicit instruction to generate/import/mutate the one-row pilot. |
| Scope | Still exactly one row: `goose / baby / female / blue / hold_ball`. |
| Reference packet | `WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md` is accepted as the packet to use. |
| Manifest/provenance | The run worker can create a real manifest with real source hash/timestamp, not placeholder values. |
| Backup path | Existing `hold_ball_00..03` frames are backed up before apply. |
| QA artifacts | Contact sheet and preview are produced before acceptance. |
| Runtime proof | Packaged/runtime screenshot proof is produced after any apply. |
| Rollback | Previous frames can be restored exactly. |
| Optional animation routing | Code-side confirms how this row will be addressed in the chosen proof runtime. |

## Phase 8 Status

```text
Phase 8: complete
production gate: closed
mutation approved: no
generation approved: no
import approved: no
current code-side canvas/test gate: green in code-side worktree
next action: no-edit QA or manifest/apply coordination
default next visual phase: do not apply or generate without explicit approval
```
