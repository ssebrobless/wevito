# Wevito Canvas Normalization Policy Packet

Updated: 2026-05-04

This is Phase 3 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It turned the historical code-side mixed-canvas report and visual review sheets
into a concrete policy for future deterministic canvas normalization.

It does not authorize runtime PNG edits, source-board edits, generation, import,
or runtime code changes. It is a visual policy packet for later coordination.

## Boundary

```text
allowed now
  -> choose a first future pilot
  -> define alignment and proof rules
  -> define rejection conditions
  -> give code-side an implementation-ready visual contract

not allowed now
  -> rewrite sprites_runtime
  -> normalize PNGs in place
  -> crop, scale, recolor, or redraw frames
  -> change runtime tests or validators
  -> start broad asset production
```

## Evidence

Primary inputs:

```text
docs/WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md
vnext/artifacts/runtime-canvas-contract-20260504-code-side.md
vnext/artifacts/visual-review/20260504-canvas-normalization-review/
```

Historical code-side report:

| Metric | Count |
| --- | ---: |
| Checked sequences | 2880 |
| Checked frames | 10800 |
| Mixed-canvas sequences | 456 |
| Missing/count-mismatch sequences | 0 |
| Invalid PNG frames | 0 |
| Fixed-reference canvas check | disabled |

Current code-side result from
`C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md`:

| Metric | Count |
| --- | ---: |
| Checked sequences | 2880 |
| Checked frames | 10800 |
| Mixed-canvas sequences | 0 |
| Missing/count-mismatch sequences | 0 |
| Invalid/non-alpha PNG frames | 0 |

Status:

```text
old mixed-canvas gate
  -> closed in code-side worktree

visual policy value
  -> still active for future imports/generation
  -> do not force old 72x64 boxes
  -> preserve natural per-sequence canvases
```

Mixed-canvas species priority:

| Rank | Species | Mixed sequences | Visual risk |
| ---: | --- | ---: | --- |
| 1 | `snake` | 144 | Long slither body can be damaged by shrink/crop repairs. |
| 2 | `rat` | 108 | Small tail/feet/nose details can be lost. |
| 3 | `fox` | 108 | Legs and tail need stable room. |
| 4 | `crow` | 72 | Wing/body height and beak silhouette need preservation. |
| 5 | `deer` | 24 | Legs/head/antlers need clearance. |

## Decision Shape

```text
historical runtime test failure
  |
  +-- cause: 456 internally mixed-canvas base animation sequences
      |
      +-- wrong response
      |     -> force every sprite into an old global box
      |     -> shrink/crop natural animal motion
      |
      +-- approved response
            -> normalize per sequence
            -> transparent padding only
            -> preserve every visible pixel, silhouette, color, and frame count
            -> prove with before/after sheets, preview, dimensions, and hashes
```

The target is sequence-stable canvases, not global same-size canvases.

This packet is now a policy reference rather than an active blocker list. The
next blocker before visual mutation is the production-safe manifest/provenance
and apply workflow.

## Approved First Future Pilot

```text
snake / baby / female / blue / walk
```

Why this sequence:

| Reason | Detail |
| --- | --- |
| Highest-risk species family | `snake` has the highest mixed-canvas count. |
| Tests the most important visual rule | The long body must stay long; no mammal-like box collapse. |
| Existing review artifact exists | `snake-baby-female-blue-walk-canvas-review.png`. |
| Mismatch is bounded | Native frames are `132x64` and `132x65`; proposed stable canvas is `132x65`. |
| Good pilot size | Small enough to prove the deterministic workflow before broad sweeps. |

Pilot source frames:

```text
sprites_runtime/snake/baby/female/blue/walk_00.png
sprites_runtime/snake/baby/female/blue/walk_01.png
sprites_runtime/snake/baby/female/blue/walk_02.png
sprites_runtime/snake/baby/female/blue/walk_03.png
sprites_runtime/snake/baby/female/blue/walk_04.png
sprites_runtime/snake/baby/female/blue/walk_05.png
```

Expected stable canvas:

```text
132x65
```

This expected canvas comes from the maximum existing width and height inside
the current sequence. It is not a new art-size target.

## Alignment Policy

Default policy:

```text
1. preserve bottom/contact baseline
2. preserve horizontal body center
3. use top-left only if visual proof shows it is already stable
```

For the first pilot, use bottom-center placement as the initial deterministic
alignment. That matches the review sheet preview and should preserve the snake's
ground read while adding transparent padding to shorter frames.

If future visual proof shows bottom-center introduces worse jitter for a
specific sequence, the normalizer should stop and produce a review packet rather
than silently choosing a different anchor.

## Pixel Preservation Contract

Any future normalization implementation must be able to prove:

| Requirement | Meaning |
| --- | --- |
| `frame_count_preserved` | Same frame names and count before and after. |
| `visible_pixels_preserved` | Every non-transparent source pixel appears unchanged in the output. |
| `alpha_preserved` | No new opaque/semi-opaque art pixels are created. |
| `palette_preserved` | Existing visible colors are unchanged. |
| `silhouette_preserved` | No crop, scale, redraw, blur, or outline edits. |
| `padding_only` | The only new pixels are transparent canvas area. |
| `sequence_canvas_stable` | All frames in the sequence share width/height after normalization. |

This is not an art cleanup pass. It is transparent padding and alignment only.

## Proof Packet Required

For the first future pilot, code-side should produce a non-mutating proof packet
first, then only mutate assets after explicit approval.

Required review outputs:

```text
before contact sheet
after/stable-canvas contact sheet
before animated preview
after animated preview
dimension table
hash manifest
pixel-preservation report
rollback path
```

Recommended artifact folder for a non-mutating pilot proof:

```text
vnext/artifacts/visual-review/YYYYMMDD-canvas-normalization-pilot-snake-walk/
```

Required manifest fields:

| Field | Purpose |
| --- | --- |
| `target` | `snake/baby/female/blue/walk`. |
| `source_root` | Runtime sprite root used for proof. |
| `operation` | `transparent_padding_only`. |
| `alignment` | `bottom-center`, unless stopped for review. |
| `input_dimensions` | Per-frame original canvas sizes. |
| `output_dimensions` | Per-frame proposed stable canvas sizes. |
| `input_hashes` | Per-frame source hashes. |
| `output_hashes` | Per-frame proposed output hashes. |
| `pixel_preservation` | Machine-readable pass/fail details. |
| `review_artifacts` | Contact sheets/previews generated. |
| `rollback` | Exact backup or restore path if mutation is later approved. |

## Rejection Conditions

Reject the pilot if any of these occur:

| Condition | Why it blocks |
| --- | --- |
| Any visible source pixel changes color or alpha. | This would become art mutation, not normalization. |
| Any frame is cropped. | Natural motion was not preserved. |
| Any frame is scaled. | Test satisfaction would come at the cost of identity/readability. |
| The snake body becomes shorter or less expressive. | Violates the core visual rule. |
| The loop jitters more after alignment. | Stable canvas alone is not a visual pass. |
| Output dimensions vary inside the sequence. | Fails the purpose of normalization. |
| No before/after proof is produced. | Cannot safely approve mutation. |
| Rollback path is unclear. | Mutation must remain reversible. |

## Broader Rollout Order

If canvas normalization ever reopens, do not normalize a broad set in one first
pass. Prove the workflow in this order:

```text
pilot 1
  -> snake / baby / female / blue / walk
  -> proves long-body preservation

pilot 2
  -> crow / baby / female / blue / walk
  -> proves vertical one-pixel padding on compact bird body

pilot 3
  -> rat / adult / female / blue / eat
  -> proves small-tail/nose preservation

pilot 4
  -> fox / adult / female / blue / eat
  -> proves legs/tail preservation

pilot 5
  -> deer / baby / female / blue / sad
  -> proves leg/head clearance
```

Only after representative pilots pass should broad deterministic normalization
be considered. As of the latest code-side worktree, the current runtime payload
already passes the active sequence-stable canvas contract.

## Code-Side Implementation Prompt Draft

Use this only after visual/code-side coordination decides to attempt the first
non-mutating normalization proof.

```text
You are working in the Wevito repo:
C:\Users\fishe\Documents\projects\wevito

Please implement a non-mutating proof packet for the first canvas normalization
pilot only:

target:
snake / baby / female / blue / walk

Read first:
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\runtime-canvas-contract-20260504-code-side.md
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260504-canvas-normalization-review\canvas-normalization-review-summary.md
- C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\20260504-canvas-normalization-review\snake-baby-female-blue-walk-canvas-review.png

Hard rules:
- Do not modify sprites_runtime or source sprite assets.
- Do not generate or import new art.
- Do not crop, scale, recolor, redraw, blur, or change visible pixels.
- Produce review artifacts only, preferably under:
  C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\visual-review\YYYYMMDD-canvas-normalization-pilot-snake-walk\

The proposed stable canvas for this pilot is 132x65, using transparent padding
only and bottom-center placement unless your proof detects worse jitter.

Required outputs:
- before contact sheet
- after/stable-canvas preview contact sheet
- before animated preview
- after animated preview
- dimension table
- hash manifest
- pixel-preservation report proving visible pixels are unchanged
- rollback/mutation plan for later approval, but no mutation in this task

Stop and report instead of continuing if:
- any visible pixel would change
- any frame would crop or scale
- output would jitter worse than input
- rollback/provenance is unclear
```

## Phase 3 Decision

```text
Phase 3 status: complete
approved first future pilot: snake / baby / female / blue / walk
alignment default: bottom/contact baseline, using bottom-center for pilot proof
operation allowed: transparent padding only
mutation status: not approved
current canvas gate: green in code-side worktree
next visual phase: Phase 4 medicine/care visual review expansion
```
