# Wevito Post-Pilot Expansion Plan

Updated: 2026-05-04

This is Phase 10 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It defines how Wevito should expand visual production after the first hold-ball
pilot.

It does not authorize generation, import, runtime PNG edits, source-board edits,
runtime code changes, or broad batch production.

## Current Gate

```text
Phase 9 pilot
  |
  +-- target: goose / baby / female / blue / hold_ball
  +-- result: applied one-row Godot proof
  +-- visual status: accept_applied_endpoint
  +-- runtime mutation: yes, exactly four hold_ball frames
  +-- source mutation: no
  +-- generation used: no
  +-- pickup/drop/carry work: no
  +-- all-color propagation: no
  |
  +-- expansion status: first review-only expansion packet prepared
```

Accepted endpoint record:

```text
docs/WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md
```

Proof first:

```text
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/godot-packaged-proof-20260504-220556/packaged-runtime-proof-contact-sheet.png
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/godot-packaged-proof-20260504-220556/proof-summary.md
```

## Decision Tree

```text
human review of Phase 9 candidate
  |
  +-- accept_for_apply_probe
  |     |
  |     +-- coordinate with code-side before apply
  |     +-- apply only this one hold_ball row
  |     +-- produce packaged/runtime proof immediately
  |     +-- decide accept/revise/reject from real runtime proof
  |
  +-- revise_candidate
  |     |
  |     +-- keep same target
  |     +-- create a second non-applied candidate
  |     +-- compare candidate A vs candidate B
  |     +-- do not move to pickup/drop yet
  |
  +-- reject_manual_candidate
        |
        +-- use Phase 7 provider prompt packet
        +-- produce a generated or hand-authored candidate
        +-- require manifest, contact sheet, preview, and proof
        +-- do not expand until a hold endpoint is accepted

current state after proof
  |
  +-- accept_applied_endpoint
        |
        +-- use hold_ball as protected endpoint reference
        +-- sprite cleanup classification completed
        +-- first pickup/drop/carry review packet prepared
```

## Expansion Rule

Do not expand by grid.

```text
wrong expansion
  -> all species
  -> all colors
  -> all optional families
  -> before one endpoint is accepted

right expansion
  -> one accepted endpoint
  -> one dependent transition
  -> one proof surface
  -> one review decision
```

## Expansion Order After Accepted Hold Endpoint

`goose / baby / female / blue / hold_ball` is now accepted in real Godot
runtime proof. Cleanup classification is complete, and the first review-only
expansion packet has been prepared. Apply/proof still requires code-side
coordination before mutating runtime PNGs.

| Order | Target | Why |
| ---: | --- | --- |
| 1 | `goose / baby / female / blue / pickup_ball` | Must travel from low/ground ball to the accepted hold endpoint. |
| 2 | `goose / baby / female / blue / drop_ball` | Must release from accepted endpoint and replace the partial-slice problem in `drop_ball_02`. |
| 3 | `goose / baby / female / blue / carry_ball_walk` | Tests whether the accepted endpoint holds while walking. |
| 4 | `goose / baby / female / blue / carry_ball_run` | Tests faster motion and prop-contact stability. |
| 5 | `goose / baby / male / blue / hold_ball` | Same species/age, opposite gender; checks gender consistency. |
| 6 | `goose / teen / female / blue / hold_ball` | Same species/gender, older body proportions. |
| 7 | `goose / adult / female / blue / hold_ball` | Adult proportions and the known blue palette warning need review. |
| 8 | `pigeon / baby / female / blue / hold_ball` | Similar beak-grip family. |
| 9 | `frog / baby / female / blue / hold_ball` | Harder non-beak body/contact read. |
| 10 | `raccoon` or `squirrel / baby / female / blue / hold_ball` | Paw/mouth ambiguity and marking-heavy identity. |

## Color Expansion Rule

Do not propagate a new pose across all six colors until the blue source variant
is accepted.

When color propagation becomes appropriate:

```text
blue accepted endpoint
  |
  +-- propagate to red/orange/yellow/indigo/violet
  +-- create six-color contact sheet
  +-- check palette identity and prop contrast
  +-- watch known goose teen/adult female blue issue separately
```

The known color warning remains:

```text
goose / teen / female / blue
goose / adult / female / blue
  -> blue reads gray/tan in Phase 2 review
  -> do not hide this under pose expansion
```

## Production Batch Limits

| Stage | Maximum batch |
| --- | --- |
| First accepted endpoint | 1 row, 1 variant |
| First dependent transition | 1 row, same variant |
| First color propagation | 1 family, 1 species/age/gender, 6 colors |
| First species expansion | 1 family, 1 new species/age/gender/color |
| First small batch | 3 to 5 rows only after two clean accepted pilots |

## Proof Requirements For Every Expansion Step

Each future step must include:

- manifest matching `docs/wevito-animation-run.schema.json`
- source hash and provenance note
- backup path before apply
- contact sheet
- preview GIF/video
- validation report
- packaged/runtime proof after apply
- markdown run summary with accept/revise/reject decision

No row should be considered accepted from file presence alone.

## First Expansion Review Packet

Review-only packet:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-optional-expansion-review/
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames/
  +-- qa/optional-expansion-candidate-review-sheet.png
  +-- qa/pickup_ball-candidate-preview.gif
  +-- qa/drop_ball-candidate-preview.gif
  +-- qa/carry_ball_walk-candidate-preview.gif
  +-- qa/carry_ball_run-candidate-preview.gif
```

Supporting drop-only candidate packet:

```text
vnext/artifacts/animation-runs/20260505-goose-baby-female-blue-drop-ball-candidate/
  +-- manifest.json
  +-- run-summary.md
  +-- candidate-frames/drop_ball_00.png
  +-- candidate-frames/drop_ball_01.png
  +-- candidate-frames/drop_ball_02.png
  +-- candidate-frames/drop_ball_03.png
  +-- qa/drop-ball-current-vs-candidate-contact-sheet.png
  +-- qa/drop-ball-candidate-preview.gif
```

Visual decision:

```text
pickup_ball
  -> current runtime row looks proof-ready

drop_ball
  -> current runtime row has partial sliced frames
  -> candidate replaces it with full-body existing goose frames
  -> candidate is review-only and not applied

carry_ball_walk
  -> current runtime row looks proof-ready

carry_ball_run
  -> current runtime row looks proof-ready
```

Candidate decision labels:

```text
accept_optional_expansion_review_for_apply_plan
revise_drop_candidate
hold_optional_expansion
```

User decision recorded on 2026-05-05:

```text
accept_optional_expansion_review_for_apply_plan
```

Code-side apply/proof handoff:

```text
docs/WEVITO_OPTIONAL_EXPANSION_APPLY_PROOF_HANDOFF_2026-05-05.md
```

Important scope note: this decision only moves the `drop_ball_00..03` candidate
to code-side apply/proof planning. `pickup_ball`, `carry_ball_walk`, and
`carry_ball_run` remain proof-only current runtime rows for this step.

## Code-Side Coordination Needs

Before any apply/runtime proof:

| Need | Why |
| --- | --- |
| Confirm ball remains runtime overlay | Prevent double-ball baked-frame mistakes. |
| Confirm proof surface | Decide whether Godot package, vNext, or both prove the candidate. |
| Confirm rollback procedure | Runtime frames must restore exactly if rejected. |
| Confirm sequence-stable canvas policy impact | Do not confuse optional-row proof with the historical mixed-canvas blocker; code-side now reports the active canvas contract green in its worktree. |
| Confirm manifest/provenance path | Avoid ad hoc asset replacement. |

## Stop Rules

Stop expansion if:

- the hold endpoint is not accepted first
- the candidate creates a double-ball or missing-ball render
- pickup/drop are attempted before hold endpoint acceptance
- a generated candidate causes identity drift
- a color propagation step weakens egg-color identity
- packaged/runtime proof is missing
- rollback path is unclear
- the user has not approved the next mutation/import step

## Phase 10 Status

```text
Phase 10: complete
accepted endpoint: goose / baby / female / blue / hold_ball
expansion review packet: prepared
runtime mutation from expansion packet: no
first future optional targets: pickup_ball, drop_ball, carry_ball_walk, carry_ball_run
post-pilot expansion: accepted for code-side one-row drop_ball apply/proof planning
```
