# Wevito Sprite Noise Cleanup Course Of Action

Updated: 2026-05-04

This plan turns
`docs/WEVITO_GAME_FACING_SPRITE_NOISE_CHECKLIST_2026-05-04.md` into the next
visual-side cleanup path.

It originally began as a classification-first plan. The user later explicitly
approved visual-side cleanup mutation for confirmed dirty shared assets, so the
current course is now: small reversible visual batches, backups, proof sheets,
targeted audit, then continue.

## Accepted Endpoint Boundary

Code-side has applied and proofed the first one-row Godot pilot:

```text
accepted endpoint
  |
  +-- goose / baby / female / blue / hold_ball
  |
  +-- changed runtime frames
  |     +-- sprites_runtime/goose/baby/female/blue/hold_ball_00.png
  |     +-- sprites_runtime/goose/baby/female/blue/hold_ball_01.png
  |     +-- sprites_runtime/goose/baby/female/blue/hold_ball_02.png
  |     +-- sprites_runtime/goose/baby/female/blue/hold_ball_03.png
  |
  +-- visual decision
        +-- accept_applied_endpoint
```

Reference doc:

```text
docs/WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md
```

Cleanup implication:

```text
protect this row
  -> do not include it in cleanup mutation candidates
  -> do not use broad scrub scripts on it
  -> use it later as endpoint reference for pickup/drop/carry planning
```

## Current Course Shape

```text
sprite cleanup course
  |
  +-- Phase A: classify flagged assets
  |
  +-- Phase B: shared runtime asset cleanup plan
  |
  +-- Phase C: pet runtime source-aware cleanup plan
  |
  +-- Phase D: one no-edit proof packet per cleanup type
  |
  +-- Phase E: hand off deterministic cleanup candidates to code-side
  |
  +-- Phase F: after cleanup classification, resume optional-family planning
  |
  +-- stop before mutation unless user explicitly approves
```

## Phase A - Classify Before Repair

Goal: separate real visible defects from intentional art structure.

Inputs:

```text
vnext/artifacts/visual-review/20260504-game-facing-sprite-noise-audit/
  +-- game-facing-sprite-noise-checklist.csv
  +-- priority-flagged-game-assets.png
  +-- shared-runtime-flagged-assets.png
  +-- runtime-priority-flagged-assets.png
  +-- pale-boundary-residue-review-sheet.png
  +-- detached-components-review-sheet.png
  +-- edge-crop-risk-review-sheet.png
```

Classification labels:

| Label | Meaning | Next step |
| --- | --- | --- |
| `true_noise` | Detached pixels, haze, or matte residue visibly hurts runtime read. | Candidate cleanup proof. |
| `intentional_multipart` | Asset is a multi-object prop, food cluster, status symbol, or environment board. | Do not remove components automatically. |
| `intentional_highlight` | Pale pixels are feather/fur/shine/water detail. | Clear from repair queue. |
| `dirty_crop` | Visible art is clipped or too tight against the canvas edge. | Padding/crop policy proof. |
| `missing_asset` | Empty or unusable referenced art. | Replace/remap plan. |

First classification target:

```text
shared-runtime P0/P1 set
  -> blanket_mat
  -> medicine icon
  -> syringe
  -> thermometer
  -> water_bowl / pond_dish
  -> nest_bed / moss_bed / hay_bed
  -> environment goose/squirrel/crow/night_props
```

Why: shared runtime assets affect habitat, care, medicine, and UI read across
the whole game, and several were already suspicious in earlier visual planning.

Phase A first packet status:

```text
completed
  -> docs/WEVITO_SHARED_CARE_HABITAT_CLASSIFICATION_2026-05-04.md
  -> vnext/artifacts/visual-review/20260504-shared-care-habitat-classification/
```

Result:

| Asset | Classification |
| --- | --- |
| `medicine.png` | `true_ui_noise_candidate` |
| `syringe.png` | `minor_true_noise_candidate` |
| `thermometer.png` | `intentional_highlight_or_minor_residue_review` |
| `blanket_mat.png` | `missing_asset` |
| `water_bowl.png` | `intentional_multipart_plus_residue_review` |

Applied cleanup progress is tracked in:

```text
docs/WEVITO_SPRITE_CLEANUP_PROGRESS_2026-05-04.md
```

Current fixed/restored assets:

```text
sprites_shared_runtime/icons/medicine.png
sprites_shared_runtime/items/care/syringe.png
sprites_shared_runtime/items/toys_b/blanket_mat.png
sprites_shared_runtime/items/containers/water_bowl.png
sprites_shared_runtime/items/toys_b/hay_bed.png
sprites_shared_runtime/items/toys_b/log_shelter.png
sprites_shared_runtime/items/toys_b/nest_bed.png
sprites_shared_runtime/items/toys_b/moss_bed.png
sprites_shared_runtime/items/toys_b/rock_basking_spot.png
sprites_shared_runtime/items/containers/pond_dish.png
sprites_shared_runtime/icons/*.png
sprites_shared_runtime/status/*.png
```

## Phase B - Shared Runtime Asset Plan

Shared assets need classification before cleanup because many are intentionally
multi-part.

```text
shared asset decision tree
  |
  +-- is it empty?
  |     +-- yes -> remap or replace before use
  |
  +-- is it a collage/board?
  |     +-- yes -> mark not placeable; prefer clean drawn/vector stage prop
  |
  +-- is residue visible at UI/runtime scale?
  |     +-- yes -> create cleanup proof
  |
  +-- is it only edge-tight but readable?
        +-- yes -> clear or defer
```

P0 shared action:

| Asset | Decision |
| --- | --- |
| `sprites_shared_runtime/items/toys_b/blanket_mat.png` | Confirmed missing/empty; do not use in habitat loadouts until fixed or remapped. |

P1 shared cleanup packets:

| Packet | Assets |
| --- | --- |
| Medicine/care | `medicine.png`, `syringe.png`, `thermometer.png` |
| Habitat rest objects | `nest_bed.png`, `moss_bed.png`, `hay_bed.png`, `blanket_mat.png`, `log_shelter.png`, `rock_basking_spot.png` |
| Water/food containers | `water_bowl.png`, `pond_dish.png`, `dish_set.png`, `feeding_plate.png` |
| UI/status | `sprites_shared_runtime/icons/*.png`, `sprites_shared_runtime/status/*.png` |
| Environment boards | `goose.png`, `squirrel.png`, `crow.png`, `night_props.png` |

Completed shared cleanup packets:

```text
vnext/artifacts/visual-review/20260504-shared-icon-cleanup-phase7/
vnext/artifacts/visual-review/20260504-status-icon-cleanup-phase8/
```

Output per packet:

- contact sheet at actual size and 2x/4x
- classify each flag as real noise or intentional structure
- mark whether asset is placeable prop, icon, background board, or unsafe
- recommend clear/remap/repair/replace

## Phase C - Pet Runtime Source-Aware Plan

Pet runtime flags should not be fixed by raw deletion. A detached component can
be a tail tip, foot, beak, wing gap, water target, or prop overlay marker.

First pet cleanup targets:

```text
pet runtime candidates
  |
  +-- rat / baby / female / blue / drink_01
  +-- rat / baby / male / blue / drink_01
  |
  +-- pigeon / adult / male / blue / idle_02
  +-- pigeon / adult / male / blue / hold_ball_02
  +-- pigeon / adult / male / blue / bathe_02
  +-- pigeon / adult / male / blue / walk_01
  +-- pigeon / adult / male / blue / happy_01
  +-- pigeon / adult / male / blue / sick_02
  |
  +-- pigeon / adult / female / blue / sad_00
  +-- pigeon / adult / female / blue / sleep_00
  +-- pigeon / adult / female / blue / sleep_01
```

Protected pet runtime row:

```text
do not target for cleanup mutation
  -> goose / baby / female / blue / hold_ball_00..03

reason
  -> newly accepted endpoint from Godot packaged proof
  -> future pickup/drop/carry rows should reference it
```

Source-aware review requirements:

| Requirement | Reason |
| --- | --- |
| Compare against adjacent frames in same animation row. | Avoid removing intentional motion extremities. |
| Compare blue against propagated colors. | Find whether residue is inherited from a base source. |
| Show alpha components overlay. | Make detached pixels visible before cleanup. |
| Preserve every legitimate body/wing/tail/foot pixel. | Prevent identity or motion damage. |
| Do not apply to all colors until one blue source case is accepted. | Avoid multiplying a bad cleanup. |

## Phase D - No-Edit Proof Packets

Before any mutation, create proof packets only.

Proof packet shape:

```text
vnext/artifacts/visual-review/YYYYMMDD-sprite-cleanup-proof-<target>/
  +-- before-contact-sheet.png
  +-- flagged-pixels-overlay.png
  +-- proposed-after-preview.png
  +-- before-after-preview.gif
  +-- cleanup-manifest.json
  +-- cleanup-summary.md
```

Allowed cleanup operations after user approval:

- isolate likely detached junk in a proposed copy
- show transparent padding proposals
- show matte-residue mask overlays
- compare before/after without touching source/runtime files
- mutate only confirmed dirty shared runtime assets in small reversible batches
- write backups before mutation
- preserve known UI canvas/dimension contracts
- run a targeted audit and keep a proof sheet per batch

Still not allowed in this cleanup phase:

- overwrite `sprites_runtime`
- generate replacement art
- import source boards
- propagate across colors
- apply broad cleanup scripts across pet frames or source boards

## Phase E - Code-Side Handoff

Only after proof packets are reviewed, hand off deterministic candidates to
code-side for apply/workflow planning.

Code-side should receive:

- exact target paths
- before hashes
- proposed output paths
- proof contact sheets
- per-pixel change report
- rollback plan
- whether the target is pet runtime, shared prop, icon, status, or environment
- whether Godot, vNext, or both need proof

## Phase F - Resume Optional-Family Planning

After the first cleanup classification packet is complete, return to the
accepted goose endpoint and plan dependent rows only as no-edit visual packets:

```text
accepted hold endpoint
  |
  +-- pickup_ball plan
  |     +-- low/ground ball moves into accepted hold endpoint
  |
  +-- drop_ball plan
  |     +-- accepted hold endpoint releases to low/ground target
  |
  +-- carry_ball_walk/run plan
        +-- preserve accepted contact read during motion
```

This phase is planning-only unless the user explicitly approves a new
generation/import/apply loop.

## Recommended Next Visual Task

Continue with the next shared item cleanup packet:

```text
target packet
  |
  +-- highest-confidence food/toy/container assets
  +-- obvious generated residue or broken crop jobs only
  +-- backup/proof/audit before moving on
```

Why this packet next:

- habitat props, shared icons, and status icons are now cleaned
- the remaining shared-runtime debt is mostly food/toy/container generated art
- pet runtime cleanup remains source-aware and should not be broad-mutated

## Stop Rules

Stop and coordinate if:

- cleanup would alter a pet silhouette rather than remove obvious junk
- a shared asset is a collage but runtime wants a placeable prop
- a proposed cleanup depends on runtime placement changes
- a cleanup would need source-board regeneration
- the user asks to apply changes before a proof packet exists
- code-side is about to mutate the same asset row for the goose hold-ball proof
- a cleanup candidate touches the accepted goose hold-ball endpoint

## Current Decision

```text
visual side may continue
  -> shared-runtime visual cleanup batches
  -> proof sheets
  -> cleanup planning docs
  -> protected-reference planning for pickup/drop/carry after classification

visual side must not continue
  -> sprite generation
  -> sprite import
  -> pet runtime/source PNG mutation
  -> pickup/drop/carry expansion
  -> all-color propagation
```
