# Wevito Visual Handoff

Updated: 2026-05-04

This is the visual-side index and handoff for the current Wevito optional
animation improvement plan. It summarizes what this thread learned, where the
supporting docs live, and what order future visual work should follow.

It does not request generation, import, sprite edits, runtime code changes, or
build/test runs.

## Thread Boundary

This handoff is for visual planning only.

Do not use it as permission to:

- generate new sprite frames
- overwrite `sprites_runtime`
- edit `incoming_sprites`
- modify Godot scripts
- modify vNext source
- run long audits, packaged sweeps, or screenshot harnesses
- touch the Sprite Workflow App being built by the other thread

Code-side reliability gates are now green in the separate code-side worktree,
but visual generation/import still waits for explicit approval plus a
production-safe manifest/provenance/apply workflow.

Latest code-side handoff read:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md
```

Current coordination shape:

```text
visual side now
  |
  +-- allowed
  |     +-- no-edit contact sheets
  |     +-- no-edit color QA
  |     +-- optional animation review
  |     +-- visual rubric/gate doc updates
  |
  +-- still paused
        +-- generation
        +-- import
        +-- runtime/source PNG mutation
        +-- broad asset rewrites
        +-- one-row production pilot unless explicitly approved
```

Latest applied endpoint:

```text
goose / baby / female / blue / hold_ball
  |
  +-- Godot packaged proof: pass
  +-- visual decision: accept_applied_endpoint
  +-- ball remains runtime overlay
  +-- changed runtime files: hold_ball_00..03 only
  +-- protected from cleanup mutation
```

Decision doc:

```text
docs/WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md
```

Latest non-mutating review artifacts:

```text
vnext/artifacts/visual-review/20260504-visual-thread-review/
  +-- goose-baby-female-six-color-identity-sheet.png
  +-- goose-baby-female-six-color-walk-sheet.png
  +-- goose-baby-female-six-color-optional-prop-sheet.png
  +-- medicine-care-existing-assets-review-sheet.png
  +-- habitat-loadout-first-spread-review-sheet.png
  +-- visual-review-summary.md
  +-- manifest.json
```

These are review outputs only. They do not modify source/runtime PNGs.

Latest canvas normalization review artifacts:

```text
vnext/artifacts/visual-review/20260504-canvas-normalization-review/
  +-- canvas-normalization-overview.png
  +-- snake-baby-female-blue-walk-canvas-review.png
  +-- snake-baby-female-blue-idle-canvas-review.png
  +-- crow-baby-female-blue-walk-canvas-review.png
  +-- deer-baby-female-blue-sad-canvas-review.png
  +-- rat-adult-female-blue-eat-canvas-review.png
  +-- fox-adult-female-blue-eat-canvas-review.png
  +-- canvas-normalization-review-summary.md
  +-- manifest.json
```

These are also review outputs only. They preview stable per-sequence canvases
without changing runtime/source PNG dimensions.

Canvas normalization policy decision:

```text
historical first normalization pilot
  -> snake / baby / female / blue / walk
  -> proposed stable sequence canvas: 132x65
  -> operation: transparent padding only
  -> status: policy reference only

current code-side canvas contract
  -> green: 2880 sequences, 10800 frames
  -> 0 mixed-canvas rows
  -> 0 missing/count rows
  -> 0 invalid/non-alpha PNG rows
```

The policy packet is documented in
`docs/WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md`.

Latest goose optional-family review artifacts:

```text
vnext/artifacts/visual-review/20260504-goose-optional-family-review/
  +-- goose-baby-female-blue-optional-family-contact-sheet.png
  +-- goose-baby-female-blue-play_ball-vs-pickup_ball-check.png
  +-- goose-baby-female-blue-idle-vs-hold_ball-check.png
  +-- goose-baby-female-blue-drink-preview.gif
  +-- goose-baby-female-blue-play_ball-preview.gif
  +-- goose-baby-female-blue-hold_ball-preview.gif
  +-- goose-baby-female-blue-pickup_ball-preview.gif
  +-- goose-baby-female-blue-drop_ball-preview.gif
  +-- goose-baby-female-blue-carry_ball_walk-preview.gif
  +-- goose-baby-female-blue-carry_ball_run-preview.gif
  +-- goose-optional-family-review-summary.md
  +-- manifest.json
```

These confirm the current `goose / baby / female / blue` pilot state: `hold_ball`
matches `idle`, `pickup_ball` matches early `play_ball`, and `drop_ball` is
distinct but needs focused review for frame-size/contact readability.

Latest goose drop focused review artifacts:

```text
vnext/artifacts/visual-review/20260504-goose-drop-focus-review/
  +-- goose-drop-transition-context-sheet.png
  +-- goose-drop-alpha-bounds-and-stable-preview.png
  +-- goose-drop-transition-context-preview.gif
  +-- goose-drop-focus-review-summary.md
  +-- manifest.json
```

Focused finding: `drop_ball_02` is the clear outlier. It is much narrower than
the surrounding drop frames and reads as a partial body slice in context. Canvas
padding can reduce size wobble, but it will not make this frame a clear release
pose by itself.

Goose prop-contact decision:

```text
first repair target
  +-- goose / baby / female / blue / hold_ball

blocked until accepted hold endpoint
  +-- pickup_ball
  +-- drop_ball

defer until endpoint and canvas policy
  +-- carry_ball_walk
  +-- carry_ball_run

temporary accept / defer review
  +-- play_ball
  +-- drink
```

The decision packet is documented in
`docs/WEVITO_GOOSE_PROP_CONTACT_DECISION_PACKET_2026-05-04.md`.

Goose hold-ball prompt packet:

```text
target
  +-- goose / baby / female / blue / hold_ball

prepared
  +-- final provider prompt
  +-- exact reference attachment list
  +-- draft manifest values
  +-- required QA outputs
  +-- rollback/apply requirements

status
  +-- generation not approved
  +-- asset mutation not approved
```

The prompt packet is documented in
`docs/WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md`.

Production gate decision:

```text
Phase 8 gate
  +-- code-side canvas/test gate: green in code-side worktree
  +-- generation approved: no
  +-- import approved: no
  +-- runtime/source sprite mutation approved: no
  +-- next allowed work: no-edit QA and manifest/apply coordination
```

The gate packet is documented in
`docs/WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md`.

Phase 9 candidate proof:

```text
target
  +-- goose / baby / female / blue / hold_ball

final result
  +-- one-row Godot apply/proof completed
  +-- runtime hold_ball_00..03 changed
  +-- source boards not changed
  +-- ball is not baked into candidate PNGs
  +-- Godot runtime overlay confirmed
  +-- status: accepted endpoint
```

The candidate packet is documented in
`docs/WEVITO_GOOSE_HOLD_BALL_PHASE9_CANDIDATE_2026-05-04.md`.

Candidate artifacts:

```text
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/
  +-- candidate/hold_ball_00.png .. hold_ball_03.png
  +-- qa/contact-sheet.png
  +-- qa/preview.gif
  +-- qa/body-pose-preview.gif
  +-- validation.json
  +-- manifest.json
  +-- run-summary.md
```

Post-pilot expansion decision:

```text
Phase 10
  +-- expansion approved now: no
  +-- accepted endpoint: goose / baby / female / blue / hold_ball
  +-- next visual work: sprite cleanup classification packet
  +-- pickup/drop/carry expansion blocked until next explicit approval
```

The expansion plan is documented in
`docs/WEVITO_POST_PILOT_EXPANSION_PLAN_2026-05-04.md`.

The code-side copy-paste handoff prompt is documented in
`docs/WEVITO_CODE_SIDE_VISUAL_PHASES_HANDOFF_PROMPT_2026-05-04.md`.

Latest game-facing sprite cleanup docs:

```text
docs/WEVITO_GAME_FACING_SPRITE_NOISE_CHECKLIST_2026-05-04.md
docs/WEVITO_SPRITE_NOISE_CLEANUP_COURSE_OF_ACTION_2026-05-04.md
```

Cleanup direction:

```text
continue now
  -> no-edit shared care/habitat classification packet

protect
  -> accepted goose hold_ball row
```

Latest color variant expansion artifacts:

```text
vnext/artifacts/visual-review/20260504-color-variant-expansion/
  +-- color-variant-expansion-overview.png
  +-- goose-baby-male-six-color-identity-sheet.png
  +-- goose-baby-male-six-color-walk-sheet.png
  +-- goose-teen-female-six-color-identity-sheet.png
  +-- goose-teen-female-six-color-walk-sheet.png
  +-- goose-adult-female-six-color-identity-sheet.png
  +-- goose-adult-female-six-color-walk-sheet.png
  +-- color-variant-expansion-summary.md
  +-- manifest.json
```

Focused finding: `goose / baby / male` preserves the six-color vocabulary well,
but `goose / teen / female / blue` and `goose / adult / female / blue` read
gray/tan more strongly than blue. Treat this as a palette QA warning, not a
missing-file or generation issue.

Latest medicine/care expansion artifacts:

```text
vnext/artifacts/visual-review/20260504-medicine-care-expansion/
  +-- medicine-care-scale-readiness-sheet.png
  +-- medicine-care-condition-map-sheet.png
  +-- medicine-care-pet-context-sheet.png
  +-- medicine-care-expansion-summary.md
  +-- manifest.json
```

Focused finding: the medicine/care set is not missing; it is under-mapped.
`first_aid_kit`, `medicine_dropper`, and `pill_bottle` are ready as the core
medicine trio. `syringe` should remain doctor-only/high-severity.

Latest habitat loadout expansion artifacts:

```text
vnext/artifacts/visual-review/20260504-habitat-loadout-expansion/
  +-- habitat-loadout-second-spread-review-sheet.png
  +-- habitat-object-placeability-review-sheet.png
  +-- habitat-zone-depth-cue-review-sheet.png
  +-- habitat-loadout-expansion-summary.md
  +-- manifest.json
```

Focused finding: species habitat loadouts are directionally useful, but many
assets are broad boards/collages rather than simple placeable props.
`blanket_mat` appears empty/invalid and should not be relied on for raccoon or
fox rest loadouts until remapped or repaired.

## Map

```text
Wevito visual plan
  |
  +-- full asset planning
  |     +-- WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md
  |     +-- WEVITO_VISUAL_ASSET_MASTER_PLAN_2026-05-04.md
  |     +-- WEVITO_VISUAL_NEXT_EXECUTION_PLAN_2026-05-04.md
  |     +-- WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md
  |     +-- WEVITO_COLOR_VARIANT_QA_EXPANSION_2026-05-04.md
  |     +-- WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md
  |     +-- WEVITO_MEDICINE_CARE_REVIEW_EXPANSION_2026-05-04.md
  |     +-- WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md
  |     +-- WEVITO_HABITAT_LOADOUT_REVIEW_EXPANSION_2026-05-04.md
  |     +-- WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md
  |     +-- WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md
  |     +-- WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md
  |     +-- WEVITO_VISUAL_REVIEW_TRIAGE_2026-05-04.md
  |     +-- WEVITO_GOOSE_PROP_CONTACT_DECISION_PACKET_2026-05-04.md
  |     +-- WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md
  |     +-- WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md
  |     +-- WEVITO_GOOSE_HOLD_BALL_PHASE9_CANDIDATE_2026-05-04.md
  |     +-- WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md
  |     +-- WEVITO_POST_PILOT_EXPANSION_PLAN_2026-05-04.md
  |     +-- WEVITO_CODE_SIDE_VISUAL_PHASES_HANDOFF_PROMPT_2026-05-04.md
  |     +-- WEVITO_GAME_FACING_SPRITE_NOISE_CHECKLIST_2026-05-04.md
  |     +-- WEVITO_SPRITE_NOISE_CLEANUP_COURSE_OF_ACTION_2026-05-04.md
  |
  │
  ├── global direction
  │     └── WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md
  │
  ├── first pilot
  │     └── WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md
  │
  ├── batch expansion
  │     └── WEVITO_GRIP_POSE_BATCH_PLAN_2026-05-04.md
  │
  ├── transition families
  │     └── WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md
  │
  ├── movement continuity
  │     └── WEVITO_CARRY_BALL_CONTINUITY_VISUAL_PLAN_2026-05-04.md
  │
  ├── environmental interaction
  │     └── WEVITO_DRINK_INTERACTION_VISUAL_PLAN_2026-05-04.md
  │
  └── priority evidence
        └── WEVITO_OPTIONAL_ANIMATION_VISUAL_AUDIT_2026-05-04.md
```

## Created Visual Docs

| Doc | Purpose |
| --- | --- |
| `docs/WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md` | Current asset inventory and checklist for pets, variants, items, medicine/care, food, water, toys, habitat objects, environments, status, UI, and egg lifecycle. |
| `docs/WEVITO_VISUAL_ASSET_MASTER_PLAN_2026-05-04.md` | Expanded visual plan covering color QA, non-generation cleanup, medicine/care mapping, habitat staging, and asset workflow order. |
| `docs/WEVITO_VISUAL_NEXT_EXECUTION_PLAN_2026-05-04.md` | Concrete next sequence for visual-side work. |
| `docs/WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md` | QA plan for proving six egg-selected pet colors before recolor work. |
| `docs/WEVITO_COLOR_VARIANT_QA_EXPANSION_2026-05-04.md` | Phase 2 no-edit goose color expansion findings, including the teen/adult female blue palette warning. |
| `docs/WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md` | Mapping from conditions/treatment meanings to existing medicine and care art. |
| `docs/WEVITO_MEDICINE_CARE_REVIEW_EXPANSION_2026-05-04.md` | Phase 4 asset readiness, condition mapping, and no-generation recommendation for medicine/care visuals. |
| `docs/WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md` | Species habitat loadout plan for beds, shelters, perches, food/water, toys, and decorative props. |
| `docs/WEVITO_HABITAT_LOADOUT_REVIEW_EXPANSION_2026-05-04.md` | Phase 5 loadout readiness, placeability classification, zone/depth guidance, and `blanket_mat` issue note. |
| `docs/WEVITO_CANVAS_NORMALIZATION_VISUAL_GUIDE_2026-05-04.md` | Visual rules for resolving mixed-canvas sequences by stable per-sequence padding/alignment without shrinking natural motion. |
| `docs/WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md` | Phase 3 decision packet approving `snake / baby / female / blue / walk` as the first future non-mutating normalization proof target. |
| `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md` | Concrete remaining visual-side phase plan to follow when the user says `proceed`. |
| `docs/WEVITO_VISUAL_REVIEW_TRIAGE_2026-05-04.md` | Phase 1 triage of existing review artifacts into accept/warning/revise/blocked decisions. |
| `docs/WEVITO_GOOSE_PROP_CONTACT_DECISION_PACKET_2026-05-04.md` | Phase 6 decisions for goose optional families: hold first, pickup/drop blocked until endpoint, carry deferred, drink/play temporary. |
| `docs/WEVITO_GOOSE_HOLD_BALL_PROMPT_PACKET_2026-05-04.md` | Phase 7 prompt packet for the first future `goose / baby / female / blue / hold_ball` production pilot. |
| `docs/WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md` | Phase 8 gate decision: production remains closed for generation/import/mutation until explicit approval and proof readiness. |
| `docs/WEVITO_GOOSE_HOLD_BALL_PHASE9_CANDIDATE_2026-05-04.md` | Phase 9 non-applied manual candidate proof for the goose hold-ball endpoint. |
| `docs/WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md` | Applied Godot proof decision accepting `goose / baby / female / blue / hold_ball` as the protected endpoint. |
| `docs/WEVITO_POST_PILOT_EXPANSION_PLAN_2026-05-04.md` | Phase 10 expansion tree and hard stop rules after the first pilot candidate. |
| `docs/WEVITO_CODE_SIDE_VISUAL_PHASES_HANDOFF_PROMPT_2026-05-04.md` | Single copy-paste prompt for the code-side thread to reconcile all visual phases. |
| `docs/WEVITO_GAME_FACING_SPRITE_NOISE_CHECKLIST_2026-05-04.md` | No-edit game-facing sprite noise checklist for `sprites_runtime` and `sprites_shared_runtime`. |
| `docs/WEVITO_SPRITE_NOISE_CLEANUP_COURSE_OF_ACTION_2026-05-04.md` | Prioritized visual cleanup plan; protects the accepted goose hold-ball row. |
| `docs/WEVITO_VISUAL_IMPROVEMENT_PLAN_2026-05-04.md` | Overall visual strategy, boundaries, and pilot direction. |
| `docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md` | First no-generation pilot packet for `goose / baby / female / blue / hold_ball`. |
| `docs/WEVITO_GRIP_POSE_BATCH_PLAN_2026-05-04.md` | Grip taxonomy and expansion order by anatomy class. |
| `docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md` | Pickup/drop transition rules and prompt fragments. |
| `docs/WEVITO_CARRY_BALL_CONTINUITY_VISUAL_PLAN_2026-05-04.md` | Carry walk/run contact stability rules. |
| `docs/WEVITO_DRINK_INTERACTION_VISUAL_PLAN_2026-05-04.md` | Drink interaction readability and environmental target rules. |
| `docs/WEVITO_OPTIONAL_ANIMATION_VISUAL_AUDIT_2026-05-04.md` | Hash-based triage matrix for optional-family visual priorities. |

## Main Findings

```text
highest-confidence visual reuse
  │
  ├── pickup_ball clones play_ball first four frames
  │     └── all 10 inspected species in baby/female/blue
  │
  ├── drop_ball clones pickup_ball
  │     └── 9 of 10 inspected species in baby/female/blue
  │
  └── hold_ball clones idle
        └── frog, pigeon, raccoon, squirrel, goose
```

Lower immediate reuse concern:

```text
drink
  └── distinct from idle in all inspected species

carry_ball_walk
  └── distinct from walk in all inspected species

carry_ball_run
  └── present as a six-frame row, but no plain run row exists in inspected
      target folders for direct hash comparison
```

Important nuance:

```text
pickup/drop are probably the broadest visual problem
hold_ball is still the best first pilot
```

Reason: pickup/drop need an accepted contact endpoint before their motion can be
judged fairly. The first endpoint should be `goose / baby / female / blue /
hold_ball`.

## Recommended Visual Order

```text
do not generate yet
  │
  ▼
code-side reliability gates are green in code-side worktree
  │
  ▼
coordinate manifest/provenance/apply workflow
  │
  ▼
pilot one complete prop-contact set
  │
  ├── 1. goose hold_ball
  │       establish accepted beak/front-body endpoint
  │
  ├── 2. goose pickup_ball
  │       replace play clone with ground-to-contact transition
  │
  ├── 3. goose drop_ball
  │       release from endpoint back to low/ground target
  │
  ├── 4. goose carry_ball_walk/run
  │       review contact stability during movement
  │
  └── 5. goose drink
          inspect only if target/background/readability issues are visible
```

After the goose set:

```text
expand by failure type, not by full grid
  │
  ├── hold endpoint repair
  │     └── pigeon, frog, raccoon, squirrel
  │
  ├── pickup transition repair
  │     └── all species by grip type
  │
  ├── drop transition repair
  │     └── species where drop == pickup
  │
  ├── carry review
  │     └── only rows with visible contact drift or loop pop
  │
  └── drink review
        └── only rows with oversized targets, background artifacts, or poor
            mouth/beak alignment
```

## First Pilot Target

| Field | Value |
| --- | --- |
| Species | `goose` |
| Age | `baby` |
| Gender | `female` |
| Source color | `blue` |
| First family | `hold_ball` |
| Next families | `pickup_ball`, `drop_ball`, `carry_ball_walk`, `carry_ball_run`, then maybe `drink` |
| Runtime path | `sprites_runtime/goose/baby/female/blue` |
| Source board | `incoming_sprites/goose-baby.png` |
| Anchor metadata | `sprites_runtime/_metadata/prop_anchors.json` |

Current anchor summary:

```text
goose / baby / female / blue

  hold_ball
    anchor_norm: x=0.865 y=0.400
    scale: 0.426
    z_index: 12
    visual read: high/front beak or front-body contact

  pickup_ball / drop_ball
    anchor_norm: x=0.585 y=0.800
    scale: 0.392
    z_index: 11
    visual read: low/ground ball position

  carry_ball_walk
    same as hold_ball plus offset x=0 y=2

  carry_ball_run
    same as hold_ball plus offset x=2 y=1

  drink
    anchor_norm: x=0.585 y=0.840
    target: water_or_bowl
    offset x=0 y=-10
```

## Decision Tree

```text
Are code-side gates clear?
  │
  ├── no
  │     └── do not generate; keep planning or review docs only
  │
  └── yes
        │
        ▼
Is manifest/provenance/apply workflow ready?
  │
  ├── no
  │     └── no sprite mutation; continue no-edit QA and coordinate workflow
  │
  └── yes
        │
        ▼
Is goose hold_ball endpoint accepted?
  │
  ├── no
  │     └── generate or hand-author only this row, then review
  │
  └── yes
        │
        ▼
Does pickup show ground-to-contact motion?
  │
  ├── no
  │     └── repair pickup before broad batching
  │
  └── yes
        │
        ▼
Does drop show contact-to-ground release?
  │
  ├── no
  │     └── repair drop before broad batching
  │
  └── yes
        │
        ▼
Review carry and drink only if visible issues remain
```

## Accept / Reject Model

Accept future generated or hand-authored art only when it satisfies:

- source identity preserved
- species, age, gender, and color preserved
- transparent canvas
- expected frame count
- no background or floor artifacts
- no crop, scale, or used-rect damage
- contact point matches the family purpose
- row purpose is visually distinct from any cloned source
- contact sheet reviewed
- preview video reviewed
- packaged runtime proof reviewed when code-side gates allow it

Reject future art if:

- it fixes cloning but changes identity
- the prop floats, teleports, vanishes, or detaches
- the pet becomes a different species/age/body type
- the row needs runtime code changes to explain the action
- it introduces background pixels or object clutter
- it breaks the loop while improving a still frame

## Prompting Rule

Do not ask a generator to "make all optional animations better."

Use one small row target at a time:

```text
good
  goose / baby / female / blue / hold_ball

bad
  all species / all colors / all optional families
```

For the first actual generation request, use the prompt fragment in:

```text
docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md
```

For later rows, use the family-specific prompt fragments in:

```text
docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md
docs/WEVITO_CARRY_BALL_CONTINUITY_VISUAL_PLAN_2026-05-04.md
docs/WEVITO_DRINK_INTERACTION_VISUAL_PLAN_2026-05-04.md
```

## Handoff Note For The Code-Side Thread

The visual side is intentionally waiting on workflow, not on the old canvas
failure. The next production visual work should not begin until
manifest/provenance/apply, optional animation addressing, proof surface, and
rollback are coordinated or explicitly accepted for a narrow pilot.

When that happens, the code-side thread should not need to plan the art
direction from scratch. It should treat this handoff as the visual queue:

```text
1. support a single-row goose hold_ball pilot
2. preserve manifest/provenance/QA outputs
3. support contact-sheet and preview-video review
4. avoid broad batch generation until the pilot set is accepted
```

## Current State

Ready:

- visual priority map
- first pilot target
- grip taxonomy
- pickup/drop criteria
- carry criteria
- drink criteria
- hash-based optional-family audit
- code-side runtime canvas/test evidence, green in the code-side worktree

Not ready:

- visual generation
- runtime import
- broad species batching
- production-safe apply/provenance workflow
- vNext optional-animation addressing
- checklist edits
- code changes from this thread

Current recommendation:

```text
continue no-edit visual QA
continue sprite cleanup classification
protect accepted goose hold_ball endpoint
plan pickup/drop/carry only after cleanup packet and explicit approval
```
