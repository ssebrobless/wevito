# Wevito Animation QA Rubric

Updated: 2026-05-04

This rubric adapts Hatch Pet's QA discipline to Wevito's per-frame runtime
pipeline. It is intentionally stricter than geometry-only validation because a
sprite can be the right size and still be the wrong animal, the wrong expression,
or a visually broken animation.

Related documents:

- [WEVITO_ANIMATION_GENERATION_CONTRACT.md](WEVITO_ANIMATION_GENERATION_CONTRACT.md)
- [wevito-animation-run.schema.json](wevito-animation-run.schema.json)
- [SPRITE_SOURCE_OF_TRUTH.md](SPRITE_SOURCE_OF_TRUTH.md)
- [AUTHORED_ANIMATION_WORKFLOW.md](AUTHORED_ANIMATION_WORKFLOW.md)

## Required Surface

Every accepted Wevito runtime frame must satisfy the sprite source of truth:

| Requirement | Gate |
| --- | --- |
| Format | Transparent PNG per frame. |
| Cell size | Exactly `28x24`. |
| Runtime tree | `sprites_runtime/<species>/<age>/<gender>/<color>/<family>_<frame>.png`. |
| Variant surface | 10 species, 3 ages, 2 genders, 6 colors. |
| Base families | `idle`, `walk`, `eat`, `happy`, `sad`, `sleep`, `sick`, `bathe`. |
| Optional families | `drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, `carry_ball_run`. |
| QA outputs | Contact sheet, preview video/GIF, validation report, packaged runtime proof. |

The six runtime colors are `blue`, `red`, `orange`, `yellow`, `indigo`, and
`violet`.

## Family Frame Counts

Frame counts must match the family definition currently used by the pipeline.
Optional families are:

| Family | Frames | Notes |
| --- | ---: | --- |
| `drink` | 4 | Water or bowl contact must stay attached to the pet action. |
| `play_ball` | 6 | Ball must remain readable and species-appropriate. |
| `hold_ball` | 4 | Ball must stay anchored to the hold pose. |
| `pickup_ball` | 4 | Motion should read as pickup, not teleport. |
| `drop_ball` | 4 | Motion should read as release/drop, not teleport. |
| `carry_ball_walk` | 6 | Walk gait and ball anchor must both remain stable. |
| `carry_ball_run` | 6 | Run gait and ball anchor must both remain stable. |

Base-family frame counts should follow the current source-of-truth family
definitions. If a base-family count conflicts with this document, the source of
truth wins and the manifest must record the exact expected `frame_count`.

## Visual Identity

Identity drift is a blocker.

The sprite must preserve:

- species and body plan
- age silhouette and proportions
- gender variant details where present
- face design and expression language
- outline weight and pixel-adjacent style
- palette family and color propagation
- prop shape, prop side, and prop contact point

A row that looks like a related but different pet fails even if it passes size,
transparency, and frame-count checks. This is the biggest lesson to carry over
from Hatch Pet: deterministic validation is necessary, but visual acceptance is a
human-facing gate.

## Animation Completeness

An accepted animation must:

- include exactly the expected number of frames
- show visible pose progression, not repeated copies of one frame
- loop without a severe first-to-last pop
- keep the pet inside the `28x24` cell without clipping important features
- keep state-specific props attached and readable
- avoid slot bleed from neighboring frames
- avoid visible labels, guide marks, grids, backgrounds, or UI artifacts

## Contact Sheet And Preview Gate

Every run must produce a contact sheet and a preview video or GIF.

The contact sheet must show every candidate frame at inspectable scale with clear
frame order. The preview must show timing and loop quality. These are mandatory
QA artifacts because they expose:

- identity drift
- prop teleporting
- repeated static frames
- neighbor-frame slivers
- matte residue
- readability loss at game scale
- motion that reads differently than the family name

Do not accept a run from file presence alone.

## Packaged Runtime Proof

Every accepted run must include packaged/runtime proof. The proof must show that
the animation visible to the player is the same set of frames that passed contact
sheet and preview review.

The proof is required because generation and import artifacts are not enough.
Wevito accepts the runtime asset, not the provider output.

## Error List

Errors block apply. If already applied, they require rollback or immediate repair.
Names below are keyed to the current `SpriteIssueKind` enum unless marked as a
Phase 3 proposed name.

| Issue key | Source | Error condition |
| --- | --- | --- |
| `DimensionMismatch` | Existing | Any frame is not exactly `28x24`. |
| `FullyTransparent` | Existing | Any required frame is empty or effectively missing. |
| `WhiteBoxMatte` | Existing | A white or opaque rectangular background remains. |
| `DetachedBorderJunk` | Existing | Detached border shards, guide fragments, or neighboring-slot slivers are visible. |
| `BodyHoles` | Existing | Cleanup removes body/prop pixels or leaves holes in the sprite. |
| `SequenceInconsistency` | Existing | Frame order, loop, or pose progression breaks the intended family. |
| `SilhouetteDrift` | Existing | Silhouette changes enough that identity or age/body plan is no longer trustworthy. |
| `PaletteMismatch` | Existing | Runtime color or source palette shifts into an incorrect variant. |
| `GenericArtifact` | Existing | Any visible artifact not covered by a narrower key. |
| `ChromaAdjacentResidue` | Phase 3 proposed | Chroma-key fringe or related residue remains around the pet or prop. |
| `SparseFrame` | Phase 3 proposed | A required frame is too small, too empty, or clearly collapsed. |
| `IdentityDrift` | Phase 3 proposed | Species, face, proportions, markings, or prop identity changes into a different pet. |
| Missing required proof | Process | Contact sheet, preview, validation report, or packaged runtime proof is absent. |
| Missing runtime frame | Process | `runtime_paths` does not contain exactly the expected ordered frame count. |

## Warning List

Warnings require human review and an explicit acceptance note in the run summary.
They do not automatically block apply unless visible in the contact sheet,
preview, or packaged runtime proof.

| Issue key | Source | Warning condition |
| --- | --- | --- |
| `PaleEdgePixels` | Existing | Light edge pixels appear but do not visibly damage the sprite at runtime scale. |
| `SequenceInconsistency` | Existing | Minor timing or loop roughness is visible but acceptable for the current family. |
| `SilhouetteDrift` | Existing | Small pose-shape variation needs review but preserves identity. |
| `PaletteMismatch` | Existing | Minor color variance is visible but remains inside the intended color identity. |
| `GenericArtifact` | Existing | Low-risk artifact requires visual sign-off. |
| `EdgePixelTouch` | Phase 3 proposed | Opaque pixels touch the frame edge and may indicate clipping risk. |
| `SizeOutlier` | Phase 3 proposed | Frame size differs from siblings but may be valid pose exaggeration. |
| Slot extraction fallback | Process | Import accepted a fallback extraction path after review. |
| Manual/source caveat | Process | A provider or manual edit was accepted with a documented exception. |

## Repair Policy

Repair the smallest failing scope first:

1. Single frame repair.
2. One family row for one species/age/gender/color.
3. Source-family regeneration.
4. Broader workflow repair only when identity, layout, or propagation is broken
   across many frames.

Manual edits must preserve the same run record and explain the change in
`provenance_note` or the markdown summary. New generated visuals require a new
job entry and a new source hash.

## Acceptance Checklist

Before accepting a run, confirm:

- manifest validates against `wevito-animation-run.schema.json`
- all required target paths are ordered and accounted for
- each PNG is transparent and exactly `28x24`
- contact sheet exists and was visually inspected
- preview video/GIF exists and was visually inspected
- packaged runtime proof exists and matches the accepted frame set
- validation report has no errors
- warnings have explicit human review notes
- identity drift is absent
- prop anchor behavior is correct for optional families

If any item fails, the run is repair/reject, not accepted.
