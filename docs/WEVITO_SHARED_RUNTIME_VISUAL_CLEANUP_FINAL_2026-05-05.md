# Wevito Shared Runtime Visual Cleanup Final

Updated: 2026-05-05

This document summarizes the visual-side cleanup pass over
`sprites_shared_runtime`. It records what changed, where the backups/proofs are,
and what the final audit still reports after human review.

## Cleanup Shape

```text
shared runtime cleanup
  |
  +-- cleaned / restored
  |     +-- care and medicine assets
  |     +-- habitat props
  |     +-- UI icons
  |     +-- status icons
  |     +-- food / toy / container / utility items
  |     +-- portraits
  |     +-- environment boards
  |     +-- tight-cropped moon sprites
  |
  +-- protected
  |     +-- sprites_shared_runtime/items/toys_a/ball.png
  |     +-- accepted goose hold_ball runtime endpoint
  |
  +-- reviewed exceptions
        +-- portrait silhouette / highlight component flags
```

## Scope

Changed shared-runtime visual assets only:

```text
sprites_shared_runtime/icons/
sprites_shared_runtime/status/
sprites_shared_runtime/items/
sprites_shared_runtime/portraits/
sprites_shared_runtime/environment/
sprites_shared_runtime/celestial/moon_01.png
sprites_shared_runtime/celestial/moon_02.png
sprites_shared_runtime/celestial/moon_03.png
sprites_shared_runtime/celestial/moon_06.png
sprites_shared_runtime/celestial/moon_07.png
```

Not touched:

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
sprites_shared_runtime/items/toys_a/ball.png
```

## Cleanup Batches

| Phase | Target | Count | Proof / manifest |
| --- | ---: | ---: | --- |
| 1 | care/icon packet | 2 | `vnext/artifacts/visual-review/20260504-shared-care-icon-cleanup-phase1/` |
| 2 | blanket restore | 1 | `vnext/artifacts/visual-review/20260504-blanket-mat-restore-phase2/` |
| 3 | water bowl | 1 | `vnext/artifacts/visual-review/20260504-habitat-rest-container-cleanup-phase3/` |
| 6 | habitat prop replacements | 6 | `vnext/artifacts/visual-review/20260504-habitat-prop-manual-cleanup-phase6/` |
| 7 | shared UI icons | 21 | `vnext/artifacts/visual-review/20260504-shared-icon-cleanup-phase7/` |
| 8 | status icons | 8 | `vnext/artifacts/visual-review/20260504-status-icon-cleanup-phase8/` |
| 9 | shared item assets | 61 | `vnext/artifacts/visual-review/20260504-shared-item-cleanup-phase9/` |
| 10 | 48x48 portraits | 420 | `vnext/artifacts/visual-review/20260505-portrait-cleanup-phase10/` |
| 11 | environment boards | 12 | `vnext/artifacts/visual-review/20260505-environment-board-cleanup-phase11/` |
| 12 | tight-cropped moon sprites | 5 | `vnext/artifacts/visual-review/20260505-celestial-crop-cleanup-phase12/` |

Every batch has a backup folder, cleaned copies, and proof sheets.

## Final Audit

Final audit:

```text
vnext/artifacts/visual-review/20260505-shared-runtime-final-post-phase12-audit/
  +-- shared-runtime-final-post-phase12-audit.md
  +-- shared-runtime-final-post-phase12-audit.json
  +-- remaining-portrait-flags-review-sheet.png
```

Result:

```text
checked: 554 shared runtime PNGs
remaining metric flags: 32
remaining confirmed visible noise/crop errors: 0
```

The 32 remaining metric flags are portrait component/highlight flags. Visual
review shows they correspond to silhouette pieces or tiny highlights, not loose
residue:

```text
remaining flags
  |
  +-- detached silhouette parts
  |     +-- tails
  |     +-- feet
  |     +-- antlers
  |
  +-- tiny highlight pixels
        +-- color/detail highlights inside clean portrait crops
```

Review sheet:

```text
vnext/artifacts/visual-review/20260505-shared-runtime-final-post-phase12-audit/remaining-portrait-flags-review-sheet.png
```

## Notes For Code Side

This was visual-side asset work, not runtime implementation. The pass did not
run vNext builds, Godot proofs, or sprite runtime regeneration.

Important coordination notes:

```text
do not overwrite
  -> accepted goose hold_ball runtime endpoint
  -> ball overlay asset

do verify later
  -> whether environment boards are used directly or only as grouped references
  -> whether changed shared item natural dimensions need any runtime placement tuning
  -> whether portrait cleanup should be propagated into any source-of-truth portrait workflow
```

The source-aware pet runtime cleanup was completed after this shared-runtime
pass. See:

```text
docs/WEVITO_PET_RUNTIME_VISUAL_CLEANUP_FINAL_2026-05-05.md
```

Next safest work is optional-animation planning from the accepted goose
hold-ball endpoint, or adjacent-frame review of specific pet runtime
family/action targets. Do not broad-scrub pet runtime frames.
