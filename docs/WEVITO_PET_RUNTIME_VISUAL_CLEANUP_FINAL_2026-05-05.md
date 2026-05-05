# Wevito Pet Runtime Visual Cleanup Final

Updated: 2026-05-05

This document summarizes the source-aware visual cleanup pass over
`sprites_runtime`. Unlike shared props and icons, pet runtime frames were cleaned
conservatively: same canvas sizes, no generation, no import, no animation
expansion, and no broad body-shape scrubbing.

## Cleanup Shape

```text
pet runtime cleanup
  |
  +-- audited all runtime frames
  |     +-- 23,040 PNGs checked
  |
  +-- cleaned safe confirmed defects
  |     +-- tiny detached specks
  |     +-- thin detached top bars in raccoon male drink rows
  |
  +-- protected
  |     +-- accepted goose / baby / female / blue / hold_ball_00..03
  |
  +-- left alone after review
        +-- tails
        +-- feet
        +-- antlers
        +-- crouch/pose separation
        +-- prop/action silhouettes
```

## Scope

Changed:

```text
306 unique sprites_runtime PNGs
```

Not touched:

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
sprites_runtime/goose/baby/female/blue/hold_ball_01.png
sprites_runtime/goose/baby/female/blue/hold_ball_02.png
sprites_runtime/goose/baby/female/blue/hold_ball_03.png
```

Those four frames remain the accepted applied endpoint from the goose hold-ball
Godot proof.

## Batches

Artifacts:

```text
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/
  +-- pet-runtime-source-aware-audit.md
  +-- pet-runtime-source-aware-audit.json
  +-- backup-before-cleanup/
  +-- cleaned-copies/
  +-- pet-runtime-tiny-cleanup-manifest.json
  +-- pet-runtime-tiny-cleanup-summary.md
  +-- backup-before-bar-cleanup/
  +-- bar-cleaned-copies/
  +-- pet-runtime-bar-cleanup-manifest.json
  +-- pet-runtime-bar-cleanup-summary.md
  +-- pet-runtime-final-audit.md
  +-- pet-runtime-final-audit.json
  +-- qa/
```

Cleanup results:

| Batch | Frames | Pixels removed | Rule |
| --- | ---: | ---: | --- |
| Tiny detached specks | 258 | 762 | Remove only components <= 6 px, <= 4x4, tiny relative to body. |
| Thin top bars | 48 | 4,380 | Remove only detached horizontal bars near the top of raccoon male drink frames. |

Total:

```text
unique runtime frames changed: 306
removed confirmed dirty pixels: 5,142
canvas dimensions changed: 0
```

## Final Audit

Final audit:

```text
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/pet-runtime-final-audit.md
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/pet-runtime-final-audit.json
```

Result:

```text
checked runtime frames: 23,040
safe tiny-noise candidates remaining: 0
thin top-bar candidates remaining: 0
substantial detached frames flagged for subsequent review: 642
```

The remaining 642 were subsequently reviewed and resolved as intentional
exceptions rather than mutation targets. See:

```text
docs/WEVITO_PET_RUNTIME_SUBSTANTIAL_FLAGS_RESOLVED_2026-05-05.md
```

The review sheet shows the highest-detached examples:

```text
vnext/artifacts/visual-review/20260505-pet-runtime-source-aware-cleanup/qa/pet-runtime-final-substantial-detached-review-sheet.png
```

## Protected Endpoint Hashes

```text
sprites_runtime/goose/baby/female/blue/hold_ball_00.png
  e2cac548eb4652ef77fe872af927e2e0e07d0cc42837bebaa5b595366ad1333a

sprites_runtime/goose/baby/female/blue/hold_ball_01.png
  1e847d80d35fd0cf6e5bf7a0a1aa8218c8db223957af08856019d7418d2779d2

sprites_runtime/goose/baby/female/blue/hold_ball_02.png
  8b6fb4322a6ee10a19e6e48e31a58a292d66f43e6bc7481ecc1cf4d89b4789dd

sprites_runtime/goose/baby/female/blue/hold_ball_03.png
  859ea85f5a36325f7cc0e15a1e945bc040feef8d1a7eb5178ee46ddf55b6760a
```

## Next Safe Work

Do not run broad pet-frame scrubbing. The next visual work should be one of:

```text
next visual work
  |
  +-- optional animation expansion from accepted goose hold_ball endpoint
  |
  +-- color-variant QA sheets after code-side confirms current asset gate
  |
  +-- targeted animation/family polish only when a specific visual problem is identified
```
