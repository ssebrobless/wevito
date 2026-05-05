# Wevito Pet Runtime Substantial Flags Resolved

Updated: 2026-05-05

This note resolves the remaining substantial-component flags after the
source-aware pet runtime cleanup. These flags were not automatically scrubbed
because they represent larger disconnected visual structures where deletion can
damage animal identity or animation.

## Resolution Shape

```text
remaining substantial flags
  |
  +-- reviewed visually
  |     +-- 642 frames
  |
  +-- safe fixes found
  |     +-- none beyond the already-applied tiny-speck pass
  |     +-- none beyond the already-applied raccoon drink bar pass
  |
  +-- resolved as intentional exceptions
        +-- feet / wing / beak detail
        +-- tails and pose separation
        +-- antlers and legs
        +-- frog leg / jump poses
        +-- prop or action silhouettes
```

## Classification

Classification artifacts:

```text
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/
  +-- pet-runtime-substantial-flag-classification.md
  +-- pet-runtime-substantial-flag-classification.json
  +-- qa/
```

Counts:

| Classification | Frames | Resolution |
| --- | ---: | --- |
| `intentional_feet_wing_or_beak_detail` | 474 | Reviewed exception. |
| `intentional_prop_or_action_silhouette` | 48 | Reviewed exception. |
| `intentional_tail_or_pose_separation` | 36 | Reviewed exception. |
| `intentional_antlers_or_legs` | 12 | Reviewed exception. |
| `intentional_leg_or_jump_pose` | 12 | Reviewed exception. |
| `needs_visual_review` | 60 | Reviewed manually; reclassified as intentional body/tail/motion separation. |

The `needs_visual_review` proof sheet was opened and reviewed. It showed
fox/rat/snake/squirrel body and tail separations, not loose residue.

## Proof Sheets

```text
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/qa/intentional_feet_wing_or_beak_detail.png
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/qa/intentional_prop_or_action_silhouette.png
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/qa/intentional_tail_or_pose_separation.png
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/qa/intentional_antlers_or_legs.png
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/qa/intentional_leg_or_jump_pose.png
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/substantial-flag-classification/qa/needs_visual_review.png
```

## Final Decision

```text
substantial component flags
  |
  +-- considered handled
  |     +-- reviewed and classified
  |     +-- no further automatic cleanup recommended
  |
  +-- future edits only by targeted animation/family review
        +-- compare adjacent frames
        +-- preserve body identity
        +-- preserve prop/action silhouettes
```

Do not broad-delete these components. The component metric is useful for
finding suspicious frames, but it is not a final error condition for pet runtime
art because many valid poses naturally have disconnected legs, feet, tails,
antlers, wings, or action silhouettes.

## Relationship To Applied Cleanup

Already applied:

```text
tiny detached specks
  -> 258 frames
  -> 762 pixels removed

raccoon drink top bars
  -> 48 frames
  -> 4,380 pixels removed
```

Protected and still untouched:

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
```
