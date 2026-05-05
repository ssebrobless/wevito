# Goose Hold Ball Code Apply/Proof Plan - 2026-05-04

Target:

```text
goose / baby / female / blue / hold_ball
```

Status: `accept_for_apply_probe`, but not applied. This document is a pre-apply plan only.

## Boundary

```text
allowed now
  -> clarify manifest/schema geometry
  -> verify rollback hashes
  -> define one-row Godot proof plan

not allowed yet
  -> overwrite sprites_runtime
  -> edit source boards
  -> generate new art
  -> import new frames beyond this candidate
  -> expand pickup/drop/carry
  -> propagate all colors
```

The candidate is body-pose only. The ball must remain a separate runtime overlay.

## Manifest Clarification

Updated files:

```text
docs/wevito-animation-run.schema.json
vnext/artifacts/animation-runs/20260504-goose-baby-female-blue-hold-ball-pilot/manifest.json
```

Clarification:

```text
jobs[0].expected_geometry
  -> role: source_layout_reference_not_runtime_png
  -> 28x24 describes legacy/source layout guidance only
  -> not the candidate runtime PNG size

import.candidate_frame_geometry
  -> width: 60
  -> height: 59
  -> mode: RGBA
  -> frame_count: 4
  -> ball_baked_into_candidate: false
  -> runtime_overlay_separate: true
```

The schema now allows `runtime_overlay_art` under `references` so a run can cite the ball art path without implying the prop is baked into the pet frames.

## Candidate Inputs

```text
candidate/hold_ball_00.png
candidate/hold_ball_01.png
candidate/hold_ball_02.png
candidate/hold_ball_03.png
```

Candidate frame hashes:

| Frame | Size | SHA-256 |
| --- | --- | --- |
| `hold_ball_00.png` | `60x59 RGBA` | `e2cac548eb4652ef77fe872af927e2e0e07d0cc42837bebaa5b595366ad1333a` |
| `hold_ball_01.png` | `60x59 RGBA` | `1e847d80d35fd0cf6e5bf7a0a1aa8218c8db223957af08856019d7418d2779d2` |
| `hold_ball_02.png` | `60x59 RGBA` | `8b6fb4322a6ee10a19e6e48e31a58a292d66f43e6bc7481ecc1cf4d89b4789dd` |
| `hold_ball_03.png` | `60x59 RGBA` | `859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a` |

## Rollback Contract

Current runtime hashes match the `backup-before-apply` hashes exactly:

| Frame | Current Runtime SHA-256 | Backup SHA-256 |
| --- | --- | --- |
| `hold_ball_00.png` | `57ac4bbcd5b4b531e3de2b1ed0e914ac47f6518900352d8fac9033e6c8e649b4` | `57ac4bbcd5b4b531e3de2b1ed0e914ac47f6518900352d8fac9033e6c8e649b4` |
| `hold_ball_01.png` | `4fb5c57bce7a600b2488bd3ddb617c59fa011e3f3d53df805aead6190874c575` | `4fb5c57bce7a600b2488bd3ddb617c59fa011e3f3d53df805aead6190874c575` |
| `hold_ball_02.png` | `f988f4c46e45f7089a4214ebfd1ea2908e389b09fcfa6ff474f41523c3fdabea` | `f988f4c46e45f7089a4214ebfd1ea2908e389b09fcfa6ff474f41523c3fdabea` |
| `hold_ball_03.png` | `37ae4b62181e2db843bb9934c696316fbd6a86bdaebbd49144369f62d455fb93` | `37ae4b62181e2db843bb9934c696316fbd6a86bdaebbd49144369f62d455fb93` |

Rollback rule:

```text
restore backup-before-apply/hold_ball_00..03
  -> back to sprites_runtime/goose/baby/female/blue/hold_ball_00..03
  -> verify all four hashes equal the backup hashes above
```

## Runtime Overlay Contract

Runtime overlay references:

```text
sprites_runtime/_metadata/prop_anchors.json
sprites_shared_runtime/items/toys_a/ball.png
```

Godot support points:

```text
scripts/pet.gd
  -> EXPANDED_OPTIONAL_ANIMATIONS includes hold_ball
  -> perform_action("hold_ball") sets carried item to "ball"
  -> _update_carried_item_sprite_from_metadata uses prop_anchors.json
  -> get_carried_item_snapshot reports metadata_key, metadata_family, position, scale, z_index
```

Acceptance rule:

```text
candidate PNGs show goose body only
Godot renders ball separately
proof fails if ball is missing or appears twice
```

## Why Godot First

```text
Godot
  -> supports optional animation family strings
  -> supports prop anchors and carried-item overlay
  -> correct first proof surface

vNext
  -> PetAnimationState currently has base/work-companion states only
  -> no first-class hold_ball/pickup/drop/carry addressing yet
  -> defer until optional animation addressing is designed
```

## Apply Procedure After Explicit Approval

Do not run this until the user explicitly approves the apply/proof step.

```text
1. Re-verify manifest JSON and schema smoke validation.
2. Re-verify current runtime hashes still match backup-before-apply.
3. Copy only these four candidate files:
   candidate/hold_ball_00.png -> sprites_runtime/goose/baby/female/blue/hold_ball_00.png
   candidate/hold_ball_01.png -> sprites_runtime/goose/baby/female/blue/hold_ball_01.png
   candidate/hold_ball_02.png -> sprites_runtime/goose/baby/female/blue/hold_ball_02.png
   candidate/hold_ball_03.png -> sprites_runtime/goose/baby/female/blue/hold_ball_03.png
4. Verify runtime hashes now equal candidate hashes.
5. Run Godot proof for goose / baby / female / blue / hold_ball.
6. Capture proof screenshot/GIF showing runtime ball overlay.
7. Record accept/revise/reject in run summary.
8. If rejected, restore backup-before-apply and verify original hashes.
```

Do not use `tools/apply_optional_animation_candidate.py` for this first proof because its default behavior propagates the candidate across all colors and writes authored plus runtime outputs. This pilot must touch one runtime row only.

## Proof Requirements

Required after apply:

```text
Godot visual proof
  -> target goose / baby / female / blue
  -> animation hold_ball
  -> carried item ball visible
  -> prop metadata family hold_ball
  -> no double ball
  -> no missing ball
  -> no pickup/drop/carry/color expansion

Hash proof
  -> pre-apply runtime hashes recorded
  -> applied runtime hashes equal candidate hashes
  -> rollback hashes remain available

Report proof
  -> update validation/report artifact with applied status
  -> update packaged/runtime proof path from pending to actual proof path
```

## Stop Rules

Stop and rollback or do not apply if:

- current runtime hashes no longer match `backup-before-apply`
- candidate dimensions are not `60x59 RGBA`
- ball appears baked into candidate frames
- prop anchor metadata or ball art is missing
- Godot cannot force/show `hold_ball` with carried ball
- proof shows double-ball or missing-ball behavior
- any step tries to touch pickup/drop/carry or non-blue colors
- any tool attempts broad propagation

