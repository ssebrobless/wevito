# Wevito Visual Review Triage

Updated: 2026-05-04

This is Phase 1 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It triages the non-mutating review artifacts already created by the visual-side
thread.

It does not authorize generation, import, source-board edits, runtime PNG edits,
or runtime code changes.

## Inputs

```text
vnext/artifacts/visual-review/20260504-visual-thread-review/
vnext/artifacts/visual-review/20260504-canvas-normalization-review/
vnext/artifacts/visual-review/20260504-goose-optional-family-review/
vnext/artifacts/visual-review/20260504-goose-drop-focus-review/
```

Related planning docs:

- `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`
- `docs/WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md`
- `docs/WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md`
- `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md`
- `docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md`
- `docs/WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md`
- `docs/WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md`

## Status Labels

| Label | Meaning |
| --- | --- |
| `accept_review` | Review artifact is useful and supports the current plan. |
| `warning` | Usable, but contains a concern that should shape later work. |
| `revise_later` | Needs future art/cleanup, but not a blocker for current review expansion. |
| `blocked` | Do not proceed to production from this state. |

## Triage Map

```text
review artifacts
  |
  +-- color QA
  |     +-- accept_review
  |     +-- warning: indigo/violet close in value
  |
  +-- medicine/care
  |     +-- accept_review
  |     +-- warning: syringe should stay doctor/high-severity
  |
  +-- habitat loadouts
  |     +-- warning
  |     +-- collage-like assets must not be treated as clean placeable props yet
  |
  +-- canvas normalization
  |     +-- accept_review
  |     +-- blocked for production until policy/pilot is agreed
  |
  +-- goose optional families
        +-- hold: blocked for production quality, usable as evidence
        +-- pickup: blocked for production quality
        +-- drop: blocked until frame 02 decision
        +-- carry: revise_later after canvas normalization
        +-- drink: accept_review / defer
```

## Color QA Triage

Artifact folder:

```text
vnext/artifacts/visual-review/20260504-visual-thread-review/
```

Relevant files:

- `goose-baby-female-six-color-identity-sheet.png`
- `goose-baby-female-six-color-walk-sheet.png`
- `goose-baby-female-six-color-optional-prop-sheet.png`

Decision table:

| Area | Status | Notes |
| --- | --- | --- |
| Goose baby female six-color identity | `accept_review` | All six colors read as distinct egg outcomes in the first sheet. |
| Goose baby female walk color consistency | `accept_review` | Good enough to expand no-edit review to more goose age/gender targets. |
| Goose optional prop colors | `accept_review` | Useful review sheet; also reinforces optional-family quality issues. |
| Indigo/violet separation | `warning` | Still distinguishable, but close in value. Track as future palette concern. |

Phase result:

```text
Color QA can expand in Phase 2.
Do not recolor yet.
```

Recommended Phase 2 first targets:

```text
goose / baby / male / all six colors
goose / teen / female / all six colors
goose / adult / female / all six colors
```

## Medicine/Care Triage

Artifact folder:

```text
vnext/artifacts/visual-review/20260504-visual-thread-review/
```

Relevant file:

- `medicine-care-existing-assets-review-sheet.png`

Decision table:

| Asset group | Status | Notes |
| --- | --- | --- |
| Basic care set | `accept_review` | The nine existing assets are readable as a set. |
| `first_aid_kit`, `medicine_dropper`, `pill_bottle`, `thermometer`, `bandage_roll` | `accept_review` | Strong first candidates for condition/treatment mapping. |
| `grooming_brush`, `soap_bottle`, `towel` | `accept_review` | Best treated as hygiene/comfort care rather than generic medicine. |
| `syringe` | `warning` | Clear asset, but should remain doctor/high-severity. Avoid casual care use. |

Phase result:

```text
Medicine/care should use existing assets first.
No new medicine generation is justified yet.
```

Recommended Phase 4 work:

- score each asset at toolbar scale
- score each asset as scene object
- confirm condition-to-care mapping
- decide first-class content candidates later

## Habitat Loadout Triage

Artifact folder:

```text
vnext/artifacts/visual-review/20260504-visual-thread-review/
```

Relevant file:

- `habitat-loadout-first-spread-review-sheet.png`

Decision table:

| Area | Status | Notes |
| --- | --- | --- |
| First species spread direction | `accept_review` | Goose, rat, crow, snake, and frog loadouts are directionally useful. |
| Species appropriateness | `accept_review` | The chosen assets broadly match each species environment. |
| Placeability of shared assets | `warning` | Several assets are collage-like or very large and should not be treated as clean single props yet. |
| Runtime object-zone readiness | `revise_later` | Needs code-side zone/depth planning later. |

Phase result:

```text
Habitat planning should continue, but with a placeability filter.
Do not assume every thumbnail is a clean scene-placeable object.
```

Recommended Phase 5 work:

- create a second-spread review for deer, pigeon, raccoon, squirrel, fox
- mark each object as `placeable`, `collage/source`, `needs_cleanup`, or `decor_only`

## Canvas Normalization Triage

Artifact folder:

```text
vnext/artifacts/visual-review/20260504-canvas-normalization-review/
```

Relevant files:

- `canvas-normalization-overview.png`
- `snake-baby-female-blue-walk-canvas-review.png`
- `snake-baby-female-blue-idle-canvas-review.png`
- `crow-baby-female-blue-walk-canvas-review.png`
- `deer-baby-female-blue-sad-canvas-review.png`
- `rat-adult-female-blue-eat-canvas-review.png`
- `fox-adult-female-blue-eat-canvas-review.png`
- `canvas-normalization-review-summary.md`

Decision table:

| Area | Status | Notes |
| --- | --- | --- |
| Per-sequence max canvas preview | `accept_review` | The sheets correctly show stable review canvases without changing PNGs. |
| Preserve natural motion rule | `accept_review` | Snake examples prove long horizontal canvases must be preserved. |
| Bottom-center alignment as default | `warning` | Looks plausible, but should be verified with actual loop preview before implementation. |
| Production normalization | `blocked` | Do not normalize PNGs until pilot sequence, alignment, rollback, and proof path are agreed. |

Phase result:

```text
Canvas normalization needs a visual policy packet before code-side implementation.
Best first pilot remains snake / baby / female / blue / walk.
```

Recommended Phase 3 first output:

- choose `snake/baby/female/blue/walk` as normalization pilot candidate
- define proof requirements
- write code-side handoff prompt for deterministic implementation later

## Goose Optional-Family Triage

Artifact folder:

```text
vnext/artifacts/visual-review/20260504-goose-optional-family-review/
```

Relevant files:

- `goose-baby-female-blue-optional-family-contact-sheet.png`
- `goose-baby-female-blue-play_ball-vs-pickup_ball-check.png`
- `goose-baby-female-blue-idle-vs-hold_ball-check.png`
- preview GIFs for all optional families
- `goose-optional-family-review-summary.md`

Decision table:

| Family | Status | Notes |
| --- | --- | --- |
| `drink` | `accept_review` | Distinct and materially expressive. Defer unless target/background readability fails later. |
| `play_ball` | `accept_review` | Useful baseline, not the immediate repair target. |
| `hold_ball` | `blocked` for production quality | Exact idle clone. Still the correct first endpoint production candidate after gates. |
| `pickup_ball` | `blocked` for production quality | Exact clone of early `play_ball`; needs future transition repair. |
| `drop_ball` | `blocked` pending focused decision | Distinct, but frame-size/readability concerns confirmed. |
| `carry_ball_walk` | `revise_later` | Distinct, but one-pixel canvas wobble and endpoint dependency remain. |
| `carry_ball_run` | `revise_later` | Same as carry walk. |

Phase result:

```text
Goose optional-family production should not start yet.
The first production candidate remains hold_ball, but only after gates.
```

## Goose Drop Focus Triage

Artifact folder:

```text
vnext/artifacts/visual-review/20260504-goose-drop-focus-review/
```

Relevant files:

- `goose-drop-transition-context-sheet.png`
- `goose-drop-alpha-bounds-and-stable-preview.png`
- `goose-drop-transition-context-preview.gif`
- `goose-drop-focus-review-summary.md`

Decision table:

| Area | Status | Notes |
| --- | --- | --- |
| `drop_ball_00` | `accept_review` | Reads as goose body, large frame. |
| `drop_ball_01` | `warning` | Partial body/crop-like composition begins. |
| `drop_ball_02` | `blocked` | Narrow partial slice; does not clearly read as release pose on its own. |
| `drop_ball_03` | `warning` | More readable than frame 02 but still not a clean endpoint. |
| Stable canvas preview | `accept_review` as evidence | Padding can reduce wobble, but cannot fix unclear pose content. |

Phase result:

```text
Do not treat goose drop_ball as production-acceptable.
Revise drop after the hold endpoint is accepted.
```

## Overall Decisions

| Area | Decision |
| --- | --- |
| Color QA | Expand no-edit review. |
| Medicine/care | Use existing assets first; no generation yet. |
| Habitat | Continue mapping, but classify placeable vs collage/source assets. |
| Canvas normalization | Prepare policy/pilot packet; no PNG normalization yet. |
| Goose hold_ball | First future production candidate after gates. |
| Goose pickup_ball | Future repair after hold endpoint. |
| Goose drop_ball | Future repair; current frame 02 is blocked. |
| Goose carry rows | Defer until endpoint and canvas policy are settled. |
| Visual generation/import | Still paused. |

## Next Phase Recommendation

Proceed to:

```text
Phase 2 - Color Variant QA Expansion
```

Why:

- it is non-mutating
- code-side already approved no-edit color QA/contact sheets
- it increases confidence in the egg-selected palette system
- it does not depend on canvas normalization or generation

Concrete next output:

```text
vnext/artifacts/visual-review/<new color expansion folder>/
  +-- goose-baby-male-six-color-identity-sheet.png
  +-- goose-teen-female-six-color-identity-sheet.png
  +-- goose-adult-female-six-color-identity-sheet.png
  +-- color-variant-expansion-summary.md
  +-- manifest.json
```

## Stop Conditions

Do not proceed to production if:

- user asks only to `proceed` without saying production is approved
- code-side canvas policy is not agreed
- manifest/provenance workflow is not agreed
- rollback path is not known
- contact sheet and preview proof are not required

If the user says `proceed`, do Phase 2 next.
