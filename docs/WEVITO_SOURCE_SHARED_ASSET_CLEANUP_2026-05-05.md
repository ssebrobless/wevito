# Wevito Source Shared Asset Cleanup

Updated: 2026-05-05

This pass continued visual cleanup by checking the source-side shared assets
under `sprites/`.

It does not touch pet runtime frames, optional-animation runtime rows, source
animal boards, prop anchors, or `sprites_shared_runtime`.

## Cleanup Shape

```text
runtime shared cleanup
  |
  +-- already polished assets in sprites_shared_runtime
  |
  v
source shared durability pass
  |
  +-- sprites/celestial
  +-- sprites/environment
  +-- sprites/icons
  +-- sprites/items
  +-- sprites/status
  |
  v
future asset prep is less likely to reintroduce old dirty boards
```

## Audit Packet

Initial non-mutating audit:

```text
vnext/artifacts/visual-review/20260505-source-shared-cleanup-audit/
  +-- source-shared-cleanup-audit.json
  +-- source-shared-cleanup-audit.md
  +-- qa/source-shared-all-flags.png
  +-- qa/source-shared-priority-flags.png
```

Result:

```text
source shared PNGs checked: 559
all audit flags: 516
priority non-portrait flags: 96
```

The priority sheet showed many source shared assets were still old generated
boards or dirty crops, even though the matching runtime shared copies had
already been cleaned.

## Applied Cleanup

Applied packet:

```text
vnext/artifacts/visual-review/20260505-source-shared-runtime-sync-cleanup/
  +-- manifest.json
  +-- run-summary.md
  +-- backup-before-sync/
  +-- runtime-source-copies/
  +-- qa/source-shared-sync-before-after.png
  +-- post-sync-audit.json
  +-- post-sync-audit.md
  +-- qa/source-shared-sync-post-remaining-flags.png
```

Operation:

```text
flagged source shared asset
  |
  +-- matching sprites_shared_runtime counterpart exists
  |
  v
backup original source PNG
  |
  v
copy cleaned runtime counterpart into sprites/
```

Changed:

```text
source shared files changed: 94
runtime files changed: 0
pet frame boards changed: 0
prop anchors changed: 0
optional animation rows changed: 0
```

## Post-Audit

Post-audit result for the changed/excluded source files:

```text
checked: 95
remaining flags: 4
```

Remaining flags:

| Asset | Reason left alone |
| --- | --- |
| `sprites/icons/exercise.png` | Tight 1px horizontal margin, but visually clean 16x16 icon. |
| `sprites/icons/memoriam.png` | Tight 1px top margin, but visually clean 16x16 icon. |
| `sprites/icons/water.png` | Tight 1px bottom margin, but visually clean 16x16 icon. |
| `sprites/egg/egg_04.png` | Tight 1px bottom margin; no matching runtime shared counterpart exists. |

These are not confirmed dirt/noise. Do not shrink or redraw them without a
specific UI problem.

## Protected Rows Verified

After cleanup, the protected goose optional rows still matched the pre-apply
hashes recorded in the apply/proof handoff:

```text
sprites_runtime/goose/baby/female/blue/drop_ball_00..03 unchanged
sprites_runtime/goose/baby/female/blue/hold_ball_00..03 unchanged
```

## Next Recommendation

Do not run broad automatic cleanup next. The source shared layer is now aligned
with the cleaned runtime shared layer for the flagged source assets that had a
matching counterpart.

Next visual work should stay targeted:

```text
next visual lane
  |
  +-- wait for manual code-side drop_ball proof
  +-- review PET TASKS spriteAudit reports if requested
  +-- only clean a specific source/runtime asset if a fresh visual error is found
```
