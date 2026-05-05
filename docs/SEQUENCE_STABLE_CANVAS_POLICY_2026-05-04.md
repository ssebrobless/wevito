# Sequence-Stable Canvas Policy - 2026-05-04

Scope: code-side Phase 2 policy for resolving Wevito's mixed-canvas sprite gate without reintroducing the old boxed-in sprite problem. This document defines the rule set for future dry-run tooling and any later approved transparent-padding repair.

No sprite PNGs were edited for this phase.

## Core Shape

```text
runtime sprite row
  |
  +-- species / age / gender / color / animation
        |
        +-- frame_00.png
        +-- frame_01.png
        +-- frame_02.png
        `-- ...

policy:
  one row may use a natural canvas size
  every frame inside that row must use the same canvas size
```

The runtime should support animals that genuinely need more room:

```text
allowed natural motion
  |
  +-- snake slither extends horizontally
  +-- frog jump extends legs/body
  +-- bird wings/legs change pose envelope
  +-- deer/fox/rat body actions need taller or wider room
  `-- optional prop actions may need prop contact space
```

The policy is not "everything must fit in 72x64." The policy is "each sequence must be internally stable."

## Definitions

| Term | Meaning |
|---|---|
| Sequence | One animation row for one exact runtime variant, for example `sprites_runtime/goose/baby/female/blue/walk_00..05.png`. |
| Canvas | PNG width and height, including transparent pixels. |
| Used alpha bounds | The visible non-transparent animal/prop pixels inside the canvas. |
| Natural canvas | The canvas size needed to preserve the largest legitimate pose in a sequence without scaling or cropping. |
| Sequence-stable canvas | One shared canvas width and height for every frame in the same sequence. |
| Transparent padding | Adding only transparent pixels around an existing PNG. No scaling, cropping, recoloring, repainting, or alpha cleanup. |

## Required Invariant

```text
for each sequence:
  max(frame.width)  == min(frame.width)
  max(frame.height) == min(frame.height)
```

This invariant applies within the exact sequence only. It does not require different species, ages, genders, colors, or animation families to share one universal canvas.

## Allowed

| Allowed operation | Reason |
|---|---|
| Larger canvases for naturally larger motion | Preserves real animation extent instead of cramping the pet. |
| Different canvas sizes for different animation families | `idle`, `walk`, `drink`, `hold_ball`, and `carry_ball_walk` may have different visual envelopes. |
| Different canvas sizes for different species/ages | A snake, frog, goose, and deer should not all be constrained to the same geometry. |
| Transparent padding to match a sequence's largest frame | Fixes runtime jitter/test gates without changing visible art. |
| Sequence-level target canvas chosen from current max dimensions | Minimizes mutation and avoids shrinking/cropping. |
| Manual review before ambiguous alignment choices | Some actions bob or lean intentionally, so padding side should not be guessed blindly. |

## Forbidden

| Forbidden operation | Why |
|---|---|
| Forcing every frame into `72x64` | Recreates the boxed-in motion problem and can damage snakes, frogs, birds, and extended poses. |
| Scaling frames independently | Causes visible size pulsing and identity drift. |
| Cropping frames to match smaller neighbors | Cuts off legitimate body parts or motion extension. |
| Repainting, recoloring, or alpha cleanup during canvas stabilization | Mixes visual repair with geometry stabilization and makes rollback/review harder. |
| Changing frame count or animation naming | Breaks runtime contracts and test assumptions. |
| Treating optional family clones as solved by canvas padding | Clone quality is a visual-production issue, not a canvas issue. |

## Target Canvas Rule

Default target for a mixed sequence:

```text
target_width  = max(existing frame widths)
target_height = max(existing frame heights)
```

This rule is intentionally conservative. It only expands smaller frames up to the largest canvas already present in that same row.

Example from Phase 1:

```text
crow/adult/female/blue/walk
  existing sizes: 89x80, 89x81
  target size:    89x81
  allowed fix:    pad 89x80 frames to 89x81 with transparent pixels
  forbidden fix:  shrink 89x81 to 89x80 or resize all frames to 72x64
```

## Alignment Policy

Phase 3 dry-run tooling must recommend alignment before any mutating repair exists.

Recommended decision order:

```text
1. If the smaller frame is clearly missing transparent top room:
     pad top

2. If the smaller frame is clearly missing transparent bottom room:
     pad bottom

3. If motion/action bobbing makes top/bottom ambiguous:
     preserve ground/contact intent when detectable

4. If contact intent cannot be inferred:
     flag for manual visual review
```

General alignment guidance:

| Animation / case | Preferred anchor |
|---|---|
| Grounded locomotion | Preserve feet/body ground contact where visible. |
| Snake slither | Preserve body centerline and full horizontal extent; do not crop curves. |
| Frog jump | Preserve extended leg/body envelope; do not squash into idle bounds. |
| Eat/drink/care | Preserve mouth/beak/contact target alignment. |
| Sad/sick/sleep | Preserve posture silhouette; minor vertical bob may be intentional. |
| Hold/carry/pickup/drop ball | Preserve prop contact point and prop readability. |

If a one-pixel canvas mismatch is caused by an action pose moving vertically, padding still may be safe, but the side of padding should be explicit in the dry-run report.

## Risk Classes For Phase 3

Phase 3 should classify every sequence before allowing Phase 4 mutation.

| Risk class | Meaning | Phase 4 eligibility |
|---|---|---|
| `safe_transparent_pad` | Same width or height delta only; target is current max canvas; no crop/scale needed; alignment is unambiguous. | Eligible. |
| `review_alignment` | Same dimensions can be stabilized by transparent padding, but top/bottom/center alignment is ambiguous. | Not eligible until reviewed or a better rule is added. |
| `manual_visual_review` | Used alpha bounds, edge contact, or motion semantics suggest padding may hide a deeper visual issue. | Not eligible. |
| `not_canvas_repair` | Missing frames, invalid PNGs, clone quality, bad silhouette, artifact, or identity drift. | Not eligible for canvas repair. |

Phase 1's 456 findings are all dimension-only padding candidates, but Phase 3 must still decide alignment and risk class per sequence.

## Dry-Run Requirements

Before any PNG is edited, the dry-run must output:

```text
for every target sequence:
  target canvas
  current frame sizes
  proposed padding per frame
  proposed alignment
  risk class
  reason
  exact files that would be changed
  exact files that would remain unchanged
```

Required artifacts:

| Artifact | Purpose |
|---|---|
| JSON report | Machine-readable exact target list and proposed operations. |
| Markdown summary | Human-readable grouping by species, animation, risk, and action. |
| Optional contact sheet list | Input list for visual thread review if alignment is ambiguous. |

The dry-run must be the default. Mutation must require an explicit flag in Phase 4.

## Mutation Requirements For Later Phase 4

Any future mutating repair must obey:

```text
only transparent pixels may be added
no scaling
no cropping
no recoloring
no alpha cleanup
no frame count changes
backup before write
write report after write
rerun SpriteRuntimeCoverageTests
rerun non-mutating canvas report
```

Suggested backup shape:

```text
artifacts/recovery/canvas-normalization-<stamp>/
  sprites_runtime/<species>/<age>/<gender>/<color>/<animation>_NN.png
```

## Relationship To Visual Work

Visual QA/contact sheets can continue as non-mutating work while this policy exists.

Visual production should still wait for:

```text
sequence-stable canvas dry-run
  -> reviewed risk classes
  -> explicit approval for mutation
  -> manifest/provenance/rollback path
```

The policy supports the user's direction:

```text
do not box sprites in
  -> allow natural pose envelopes
  -> preserve source identity and motion
  -> stabilize canvas around the motion
```

## Phase 2 Decision

Adopt this rule:

```text
A runtime animation sequence is valid when every frame in that exact sequence
shares one transparent canvas that is large enough for the largest legitimate
pose in that sequence, without scaling, cropping, repainting, or recoloring any
frame.
```

