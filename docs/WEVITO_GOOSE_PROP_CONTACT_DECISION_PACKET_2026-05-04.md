# Wevito Goose Prop-Contact Decision Packet

Updated: 2026-05-04

This is Phase 6 of `docs/WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`.
It consolidates the existing `goose / baby / female / blue` optional-family
review into a concrete decision packet.

It does not authorize generation, import, runtime PNG edits, source-board edits,
runtime code changes, packaged sweeps, or Sprite Workflow App changes.

## Boundary

```text
Phase 6
  |
  +-- decide current goose optional-family status
  +-- rank repair order
  +-- define dependencies between hold/pickup/drop/carry/drink
  +-- identify which current rows are temporary-acceptable
  |
  +-- no generation
  +-- no import
  +-- no sprite edits
  +-- no runtime code changes
```

## Evidence

Primary review artifacts:

```text
vnext/artifacts/visual-review/20260504-goose-optional-family-review/
  +-- goose-baby-female-blue-optional-family-contact-sheet.png
  +-- goose-baby-female-blue-play_ball-vs-pickup_ball-check.png
  +-- goose-baby-female-blue-idle-vs-hold_ball-check.png
  +-- goose-optional-family-review-summary.md

vnext/artifacts/visual-review/20260504-goose-drop-focus-review/
  +-- goose-drop-transition-context-sheet.png
  +-- goose-drop-alpha-bounds-and-stable-preview.png
  +-- goose-drop-transition-context-preview.gif
  +-- goose-drop-focus-review-summary.md
```

Supporting plans:

```text
docs/WEVITO_GOOSE_HOLD_BALL_VISUAL_PILOT_2026-05-04.md
docs/WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md
docs/WEVITO_CARRY_BALL_CONTINUITY_VISUAL_PLAN_2026-05-04.md
docs/WEVITO_DRINK_INTERACTION_VISUAL_PLAN_2026-05-04.md
docs/WEVITO_CANVAS_NORMALIZATION_POLICY_PACKET_2026-05-04.md
```

## Decision Shape

```text
goose prop-contact work
  |
  +-- 1. hold_ball endpoint
  |     +-- must be accepted before pickup/drop/carry production
  |
  +-- 2. pickup_ball into endpoint
  |     +-- current row is play_ball clone
  |
  +-- 3. drop_ball out of endpoint
  |     +-- current row has frame-02 partial-slice blocker
  |
  +-- 4. carry_ball_walk / carry_ball_run
  |     +-- distinct rows, but endpoint/contact and canvas policy come first
  |
  +-- 5. drink
        +-- distinct row, not part of ball-contact repair chain
```

## Family Decisions

| Family | Current evidence | Decision | Why |
| --- | --- | --- | --- |
| `hold_ball` | Exact idle clone; 4 frames; `60x59`; same hashes as `idle_00..03`. | `repair_first` | It is the required endpoint for every ball-contact row. Current row is acceptable only as a temporary placeholder. |
| `pickup_ball` | Exact hash match to `play_ball_00..03`; sizes `100x120` through `102x125`. | `blocked_until_hold_endpoint` | It cannot be judged or authored correctly until the accepted hold contact exists. Current row is not a real pickup. |
| `drop_ball` | Distinct row, but `drop_ball_02` is `45x87` with a narrow `37x79` alpha bbox and reads as a partial body slice. | `blocked_until_hold_endpoint_then_repair` | Stable padding can reduce canvas wobble, but it cannot make the partial-slice frame read as release. |
| `carry_ball_walk` | Distinct from plain walk by hash; 6 frames; `80x72`/`80x73` one-pixel canvas wobble. | `defer` | Needs accepted hold contact and canvas-normalization policy before final contact-stability judgment. |
| `carry_ball_run` | Distinct 6-frame row; `80x72`/`80x73` one-pixel canvas wobble; no local plain `run` row for direct comparison. | `defer` | Same dependency as carry walk, plus run-read needs separate motion proof. |
| `play_ball` | Distinct 6-frame row; currently reused by pickup first four frames. | `accept_temporary` | Not the first repair target; useful as context/reference but should not stand in for pickup. |
| `drink` | Distinct 4-frame row; larger canvases `85x118` through `102x80`; not an idle/happy clone. | `accept_temporary_defer_review` | Drink is an environmental-contact row, not part of the ball endpoint chain. Review later for target/background readability. |

## Immediate Production Order Later

Do not start production yet. When gates allow, use this order:

```text
1. goose / baby / female / blue / hold_ball
   -> create accepted beak/front-body endpoint

2. goose / baby / female / blue / pickup_ball
   -> ground/low ball travels into accepted endpoint

3. goose / baby / female / blue / drop_ball
   -> accepted endpoint releases to ground/low ball
   -> replace or repair partial-slice frame problem

4. goose / baby / female / blue / carry_ball_walk
   -> prove ball stays attached while walking

5. goose / baby / female / blue / carry_ball_run
   -> prove faster carry read without detachment

6. goose / baby / female / blue / drink
   -> review only if environmental-contact readability becomes a priority
```

## Current Placeholder Policy

The current runtime rows can remain as placeholders while visual production is
paused.

| Family | Placeholder allowed? | Note |
| --- | --- | --- |
| `hold_ball` | yes | Placeholder only; not accepted as final bespoke grip. |
| `pickup_ball` | yes | Placeholder only; do not treat as proof that pickup is visually done. |
| `drop_ball` | warning | Existing row is distinct but visually suspect; do not expand from it. |
| `carry_ball_walk` | yes | Distinct enough to keep while endpoint work waits. |
| `carry_ball_run` | yes | Distinct enough to keep while endpoint work waits. |
| `play_ball` | yes | Not currently the repair target. |
| `drink` | yes | Defer unless review finds target/background problems. |

## Repair Acceptance Gates

Any future goose prop-contact candidate needs:

| Gate | Requirement |
| --- | --- |
| Identity | Still reads as `goose / baby / female / blue`; no age/species/body drift. |
| Endpoint | `hold_ball` establishes believable beak/front-body contact. |
| Prop contact | Ball is attached or intentionally traveling; no floating overlay read. |
| Motion | Pickup/drop/carry frames have readable sequence logic. |
| Canvas | Sequence-stable canvas or approved transparent-padding proof. |
| Pixel cleanliness | No matte, guide marks, detached specks, edge crops, or background patches. |
| Proof | Contact sheet, preview, manifest, hashes, and rollback/provenance notes. |

## Stop Rules

Stop the future production pilot if:

- `hold_ball` cannot establish a clear endpoint without identity drift
- pickup/drop are generated before the accepted endpoint exists
- `drop_ball_02` is preserved as a partial-slice release frame
- carry rows are judged only by the presence of a ball, not attachment stability
- canvas fixes crop, scale, or shrink natural motion
- any candidate needs heavy manual cleanup before it can be reviewed fairly

## Phase 6 Status

```text
Phase 6: complete
first repair target: goose / baby / female / blue / hold_ball
blocked until hold endpoint: pickup_ball, drop_ball
defer until endpoint/canvas policy: carry_ball_walk, carry_ball_run
accept temporary / defer review: play_ball, drink
asset mutation approved: no
generation approved: no
next visual phase: Phase 7 prompt and generation packet preparation
```
