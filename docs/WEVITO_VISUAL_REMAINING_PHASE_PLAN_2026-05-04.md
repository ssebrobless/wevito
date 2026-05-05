# Wevito Visual Remaining Phase Plan

Updated: 2026-05-04

This is the organized remaining visual-side plan. It is meant to be advanced
one phase at a time when the user says `proceed`.

It does not authorize generation, import, runtime PNG edits, source-board edits,
or runtime code changes. Visual generation/import remains paused until the
sequence-stable canvas policy, manifest/provenance workflow, rollback path, and
QA gates are agreed.

## Current State

```text
visual planning complete enough to proceed in review mode
  |
  +-- asset inventory exists
  +-- color variant QA plan exists
  +-- medicine/care mapping exists
  +-- medicine/care review expansion exists
  +-- habitat loadout mapping exists
  +-- habitat loadout review expansion exists
  +-- canvas normalization guide exists
  +-- canvas normalization policy packet exists
  +-- goose optional-family review exists
  +-- goose drop focused review exists
  +-- goose prop-contact decision packet exists
  +-- goose hold-ball prompt packet exists
  +-- production gate check exists
  +-- goose hold-ball Phase 9 candidate proof exists
  +-- post-pilot expansion plan exists
  +-- code-side visual phases handoff prompt exists
  |
  +-- no visual generation/import yet
  +-- no runtime/source PNG edits yet
```

Current no-edit review artifacts:

```text
vnext/artifacts/visual-review/20260504-visual-thread-review/
vnext/artifacts/visual-review/20260504-canvas-normalization-review/
vnext/artifacts/visual-review/20260504-goose-optional-family-review/
vnext/artifacts/visual-review/20260504-goose-drop-focus-review/
vnext/artifacts/visual-review/20260504-color-variant-expansion/
vnext/artifacts/visual-review/20260504-medicine-care-expansion/
vnext/artifacts/visual-review/20260504-habitat-loadout-expansion/
```

## Phase Map

```text
Phase 1  Review artifact triage
         status: complete in WEVITO_VISUAL_REVIEW_TRIAGE_2026-05-04.md
Phase 2  Color variant QA expansion
         status: initial goose expansion complete in
                 WEVITO_COLOR_VARIANT_QA_EXPANSION_2026-05-04.md;
                 broader species expansion pending
Phase 3  Canvas normalization visual policy packet
         status: complete in
                 WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md
Phase 4  Medicine/care visual review expansion
         status: complete in
                 WEVITO_MEDICINE_CARE_REVIEW_EXPANSION_2026-05-04.md
Phase 5  Habitat loadout visual review expansion
         status: complete in
                 WEVITO_HABITAT_LOADOUT_REVIEW_EXPANSION_2026-05-04.md
Phase 6  Goose prop-contact decision packet
         status: complete in
                 WEVITO_GOOSE_PROP_CONTACT_DECISION_PACKET_2026-05-04.md
Phase 7  Prompt and generation packet preparation
         status: complete in
                 WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md
Phase 8  Production gate check
         status: complete in
                 WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md;
                 gate closed for generation/import/mutation
Phase 9  First visual production pilot
         status: non-applied candidate proof complete in
                 WEVITO_GOOSE_HOLD_BALL_PHASE9_CANDIDATE_2026-05-04.md;
                 needs human review before apply
Phase 10 Post-pilot expansion plan
         status: complete in
                 WEVITO_POST_PILOT_EXPANSION_PLAN_2026-05-04.md
```

Only Phases 1-7 are visual planning/review work. Phases 8-10 require explicit
coordination before any asset mutation.

## Phase 1 - Review Artifact Triage

Goal: turn the first review sheets into decisions.

Inputs:

- `vnext/artifacts/visual-review/20260504-visual-thread-review/visual-review-summary.md`
- `vnext/artifacts/visual-review/20260504-canvas-normalization-review/canvas-normalization-review-summary.md`
- `vnext/artifacts/visual-review/20260504-goose-optional-family-review/goose-optional-family-review-summary.md`
- `vnext/artifacts/visual-review/20260504-goose-drop-focus-review/goose-drop-focus-review-summary.md`

Tasks:

| Task | Output |
| --- | --- |
| Score goose six-color review sheets. | Color QA result table. |
| Score medicine/care review sheet. | Care asset readiness table. |
| Score habitat first-spread sheet. | Habitat asset/object notes. |
| Score canvas normalization examples. | Alignment recommendation table. |
| Score goose optional-family packet. | Pilot readiness decision table. |

Exit criteria:

- each artifact has `accept`, `warning`, `revise_later`, or `blocked` status
- no asset edits are requested
- next phase priorities are clear

## Phase 2 - Color Variant QA Expansion

Goal: expand no-edit color QA beyond the first goose target.

Inputs:

- `docs/WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md`
- `docs/WEVITO_COLOR_VARIANT_QA_EXPANSION_2026-05-04.md`
- first color sheets in `20260504-visual-thread-review`
- first expansion sheets in `20260504-color-variant-expansion`

Recommended targets:

```text
1. goose / baby / male / all six colors
2. goose / teen / female / all six colors
3. goose / adult / female / all six colors
4. pigeon / baby / female / all six colors
5. frog / baby / female / all six colors
6. raccoon / baby / female / all six colors
7. squirrel / baby / female / all six colors
```

Tasks:

| Task | Output |
| --- | --- |
| Generate contact sheets only. | New visual-review artifact folder. |
| Compare egg color identity. | Pass/warning/fail per color. |
| Flag close-value pairs. | Palette concern list. |
| Flag prop/color blending. | Optional-family color concerns. |

Exit criteria:

- enough evidence to decide whether palette cleanup is needed
- no source/runtime images modified

Current status:

```text
initial goose expansion: complete
broader species expansion: pending
key warning: goose teen/adult female blue reads gray/tan rather than blue
next decision: continue broader color QA, or proceed to Phase 3 canvas policy
```

## Phase 3 - Canvas Normalization Visual Policy Packet

Goal: turn the code-side mixed-canvas report into a concrete visual policy for
future deterministic normalization.

Inputs:

- `docs/WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md`
- `docs/WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md`
- `vnext/artifacts/runtime-canvas-contract-20260504-code-side.md`
- `vnext/artifacts/visual-review/20260504-canvas-normalization-review/`

Tasks:

| Task | Output |
| --- | --- |
| Pick first normalization pilot sequence. | Recommended pilot, likely `snake/baby/female/blue/walk`. |
| Decide alignment default. | Bottom-center vs baseline/contact anchor. |
| Define before/after proof requirements. | Contact sheet + GIF/video + hashes. |
| Define rejection conditions. | No shrink, no crop, no worse jitter. |
| Write code-side handoff prompt. | Copy-paste prompt when ready. |

Exit criteria:

- one sequence is approved as the first future normalization pilot
- visual expectations are explicit enough for code-side implementation later

Current status:

```text
complete
approved first future pilot: snake / baby / female / blue / walk
expected stable sequence canvas: 132x65
alignment default: bottom/contact baseline, bottom-center for the pilot proof
allowed operation: transparent padding only
mutation status: not approved
```

## Phase 4 - Medicine/Care Visual Review Expansion

Goal: decide which existing care assets are ready, which need cleanup, and which
could become first-class content later.

Inputs:

- `docs/WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md`
- `docs/WEVITO_MEDICINE_CARE_REVIEW_EXPANSION_2026-05-04.md`
- `vnext/artifacts/visual-review/20260504-visual-thread-review/medicine-care-existing-assets-review-sheet.png`
- `vnext/artifacts/visual-review/20260504-medicine-care-expansion/`

Tasks:

| Task | Output |
| --- | --- |
| Score each care asset at icon scale. | Asset readiness table. |
| Score each care asset as scene object. | Scene-readiness table. |
| Map care assets to conditions. | Confirmed condition visual map. |
| Identify first-class candidates. | Future content proposal. |
| Identify cleanup needs. | No-generation cleanup list. |

Exit criteria:

- no new medicine generation unless a real gap remains
- `syringe` handling remains high-severity/doctor-only unless user decides otherwise

Current status:

```text
complete
new generation needed: no
core medicine trio: first_aid_kit, medicine_dropper, pill_bottle
diagnosis/recovery/hygiene support: thermometer, bandage_roll, grooming_brush, soap_bottle, towel
syringe policy: doctor-only / high-severity
mutation status: not approved
```

## Phase 5 - Habitat Loadout Visual Review Expansion

Goal: move from loadout table to visual confidence.

Inputs:

- `docs/WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md`
- `docs/WEVITO_HABITAT_LOADOUT_REVIEW_EXPANSION_2026-05-04.md`
- `vnext/artifacts/visual-review/20260504-visual-thread-review/habitat-loadout-first-spread-review-sheet.png`
- `vnext/artifacts/visual-review/20260504-habitat-loadout-expansion/`

Tasks:

| Task | Output |
| --- | --- |
| Score first spread: goose, rat, crow, snake, frog. | Species loadout notes. |
| Identify collage-like assets vs placeable single props. | Placeability table. |
| Create second-spread review sheet. | deer, pigeon, raccoon, squirrel, fox. |
| Define object-zone needs. | Future code-side zone requirements. |
| Define depth/occlusion needs. | Future renderer handoff notes. |

Exit criteria:

- species habitat defaults are ready for future code-side zone planning
- rough/collage assets are not mistaken for clean placeable props

Current status:

```text
complete
species loadouts: directionally ready for future object-zone planning
main warning: many habitat assets are broad boards/collages, not simple props
confirmed asset issue: blanket_mat is invalid/empty
first future zone pilots: ball, water_bowl, pond_dish, rock_basking_spot
mutation status: not approved
```

## Phase 6 - Goose Prop-Contact Decision Packet

Goal: decide the precise status of the first optional animation pilot set.

Inputs:

- `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md`
- `docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md`
- `docs/WEVITO_GOOSE_PROP_CONTACT_DECISION_PACKET_2026-05-04.md`
- `vnext/artifacts/visual-review/20260504-goose-optional-family-review/`
- `vnext/artifacts/visual-review/20260504-goose-drop-focus-review/`

Tasks:

| Family | Current evidence | Likely decision |
| --- | --- | --- |
| `hold_ball` | exact idle clone | first endpoint production candidate |
| `pickup_ball` | exact play clone | needs future transition repair |
| `drop_ball` | distinct but `drop_ball_02` is partial-slice outlier | needs revision after hold endpoint |
| `carry_ball_walk` | distinct, one-pixel canvas wobble | review after endpoint/normalization |
| `carry_ball_run` | distinct, one-pixel canvas wobble | review after endpoint/normalization |
| `drink` | distinct and large | defer unless readability issue is confirmed |

Deliverable:

- one concise decision doc or update with `accept temporary`, `repair first`,
  `defer`, or `blocked` labels for each goose optional family.

Exit criteria:

- first production target is still confirmed or adjusted
- prompt prep can begin without ambiguity

Current status:

```text
complete
first repair target: goose / baby / female / blue / hold_ball
blocked until hold endpoint: pickup_ball, drop_ball
defer until endpoint/canvas policy: carry_ball_walk, carry_ball_run
accept temporary / defer review: play_ball, drink
mutation status: not approved
```

## Phase 7 - Prompt And Generation Packet Preparation

Goal: prepare generation/authoring packets without running generation.

Inputs:

- `docs/WEVITO_ANIMATION_GENERATION_CONTRACT.md`
- `docs/WEVITO_ANIMATION_QA_RUBRIC.md`
- `docs/wevito-animation-run.schema.json`
- `docs/WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md`
- goose pilot and prop-contact docs
- visual review artifacts

Tasks:

| Task | Output |
| --- | --- |
| Write final hold-ball generation prompt. | Prompt packet. |
| Define exact references to attach. | Source/reference list with paths. |
| Define manifest fields for the run. | Draft manifest values. |
| Define review artifacts required. | Contact sheet, GIF/video, before/after. |
| Define rollback path. | Backup/apply requirements. |

Exit criteria:

- user can hand the prompt packet to Claude or another generation thread
- still no generation/import has happened in this thread

Current status:

```text
complete
prompt packet target: goose / baby / female / blue / hold_ball
provider prompt prepared: yes
reference attachment list prepared: yes
draft manifest values prepared: yes
QA outputs required: contact sheet, preview, validation, packaged proof, run summary
mutation status: not approved
```

## Phase 8 - Production Gate Check

Goal: decide if asset mutation is allowed.

Required checks:

| Gate | Required status |
| --- | --- |
| code-side build/probe | stable enough for proof |
| mixed-canvas policy | agreed |
| manifest/provenance workflow | agreed |
| rollback path | known |
| contact sheet review | mandatory |
| preview video/GIF | mandatory for animation |
| batch scope | one row/family target only |

Exit criteria:

- explicit user approval to generate/import or explicit decision to keep waiting

Current status:

```text
complete
production gate: closed
generation approved: no
import approved: no
runtime/source sprite mutation approved: no
next allowed work: non-mutating coordination/proof planning only
```

## Phase 9 - First Visual Production Pilot

Goal: produce or hand-author the first real visual replacement only after gates.

Status:

```text
non-applied candidate proof complete
runtime/source sprites not overwritten
generation used: no
import/apply used: no
decision needed: accept_for_apply_probe / revise_candidate / reject_manual_candidate
```

Default target:

```text
goose / baby / female / blue / hold_ball
```

Rules:

- no broad batch
- no all-species generation
- no all-color generation first
- preserve source identity
- record provenance
- require contact sheet and preview
- rollback must be possible

Exit criteria:

- accept/revise/reject decision for the one row
- no expansion until review is clean

Current status:

```text
candidate proof complete
artifact folder: vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/
review first: qa/contact-sheet.png and qa/preview.gif
apply status: not applied
post-pilot expansion: blocked until human review
```

## Phase 10 - Post-Pilot Expansion Plan

Goal: expand only after the first pilot proves the workflow.

Status:

```text
complete
expansion approved now: no
next required decision: review Phase 9 candidate
first possible apply target: goose / baby / female / blue / hold_ball
code-side handoff prompt: WEVITO_CODE_SIDE_VISUAL_PHASES_HANDOFF_PROMPT_2026-05-04.md
```

Likely order:

```text
1. same goose target pickup_ball
2. same goose target drop_ball
3. same goose target carry review/repair
4. same species opposite gender
5. same species older ages
6. similar grip species: pigeon
7. harder grip species: frog/raccoon/squirrel
```

Exit criteria:

- production workflow is stable enough for small batches
- quality does not regress as scope expands

Current status:

```text
post-pilot expansion planning complete
do not expand until Phase 9 candidate has accept/revise/reject decision
do not apply candidate until code-side/user coordination is explicit
```

## Command Vocabulary

When the user says `proceed`, use this order:

```text
next uncompleted phase
  -> do only that phase
  -> keep artifacts non-mutating unless phase explicitly says production gate passed
  -> update handoff if new review artifacts are created
  -> stop with summary and next recommended phase
```

If the user says `proceed with production`, still pause and verify Phase 8 gates
unless the user explicitly waives them.

## Immediate Next Phase

The next action should be:

```text
human review of Phase 9 candidate
```

Review first:

```text
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/qa/contact-sheet.png
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/qa/preview.gif
```

Then send the code-side handoff prompt if code-side coordination is desired.
