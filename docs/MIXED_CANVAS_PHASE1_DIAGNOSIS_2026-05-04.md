# Mixed Canvas Phase 1 Diagnosis - 2026-05-04

Scope: code-side Phase 1 only. This pass diagnosed the vNext `SpriteRuntimeCoverageTests` mixed-canvas failure without editing, regenerating, normalizing, or rewriting any sprite PNGs.

## Result Shape

```text
SpriteRuntimeCoverageTests failure
  |
  +-- required base frames exist: 10,800 checked
  +-- invalid PNG frames: 0
  +-- missing/count mismatches: 0
  `-- mixed-canvas sequences: 456
        |
        `-- all are width delta 0, height delta 1
```

## Artifacts

| Artifact | Purpose |
|---|---|
| `vnext/artifacts/runtime-canvas-contract-20260504-code-side.json` | Existing non-mutating canvas report used as the source. |
| `vnext/artifacts/runtime-canvas-contract-20260504-code-side.md` | Human-readable mixed-canvas summary. |
| `vnext/artifacts/mixed-canvas-diagnosis-20260504-phase1.json` | Detailed Phase 1 diagnosis with per-frame canvas, alpha bounds, margins, mtime, and risk class. |

## Summary

| Check | Result |
|---|---:|
| Mixed sequences | 456 |
| Missing sequences | 0 |
| Invalid PNG frames | 0 |
| Required base frames checked | 10,800 |
| Delta bin | `width +0`, `height +1` for all 456 |
| Risk class | `dimension_only_padding_candidate` for all 456 |
| Current `generation-summary.json` | Missing from `sprites_runtime` |
| Affected frame mtime range | `2026-05-04T15:51:34` to `2026-05-04T16:05:25` |

## Distribution

| Species | Mixed sequences |
|---|---:|
| `snake` | 144 |
| `fox` | 108 |
| `rat` | 108 |
| `crow` | 72 |
| `deer` | 24 |

| Animation | Mixed sequences |
|---|---:|
| `eat` | 108 |
| `sad` | 84 |
| `sick` | 84 |
| `walk` | 72 |
| `idle` | 36 |
| `happy` | 36 |
| `bathe` | 36 |

| Species / Animation | Count |
|---|---:|
| `crow / eat` | 36 |
| `crow / walk` | 36 |
| `deer / sad` | 12 |
| `deer / sick` | 12 |
| `fox / eat` | 36 |
| `fox / sad` | 36 |
| `fox / sick` | 36 |
| `rat / eat` | 36 |
| `rat / sad` | 36 |
| `rat / sick` | 36 |
| `snake / bathe` | 36 |
| `snake / happy` | 36 |
| `snake / idle` | 36 |
| `snake / walk` | 36 |

All six colors are affected evenly: `76` mixed sequences each for `blue`, `indigo`, `orange`, `red`, `violet`, and `yellow`.

## Diagnosis

The most likely source is `tools/generate_runtime_pose_sprites.py`, specifically the runtime generation path:

```text
export_animation_set
  -> apply_recipe
  -> fit_to_canvas
       |
       +-- starts with species/profile canvas intent
       +-- expands natural motion/canvas when needed
       `-- can add per-frame overflow padding
            without a sequence-level normalization pass
```

Relevant code areas:

| File | Relevant system |
|---|---|
| `tools/generate_runtime_pose_sprites.py` | `fit_to_canvas`, `expand_motion_frame_layout`, `export_animation_set` |
| `tools/report_runtime_canvas_mismatches.py` | Non-mutating detector for mixed sequence canvases |
| `vnext/tests/Wevito.VNext.Tests/SpriteRuntimeCoverageTests.cs` | Current failing test requiring each animation sequence to share one canvas |

Why this is probably not a visual/import corruption issue:

- The mismatch report covers required base animation families, not optional families like `hold_ball` or `pickup_ball`.
- Every mismatch is exactly one pixel of height only.
- Every color is affected evenly, which suggests the same base row/canvas pattern was propagated across color variants.
- No required frames are missing and no PNG headers are invalid.

## Safety Assessment

```text
safe to plan later:
  transparent sequence padding
  max natural canvas per sequence
  no scaling, no crop, no recolor

not safe:
  forcing all rows back into 72x64
  shrinking animal poses
  cropping extended motion
  per-frame independent resizing
```

All 456 sequences are Phase 2/3 candidates for a sequence-stable transparent-padding policy. The padding side/alignment must not be guessed in Phase 1; bobbing, walk, eat, sick, and sad frames can legitimately move inside the canvas. Phase 3 should compute a dry-run alignment recommendation per sequence and flag anything visually risky before mutation is allowed.

## Phase 1 Audit

Phase 1 passed its own boundary:

- No sprite PNGs were edited.
- No source boards were edited.
- No visual generation or import was started.
- The failure is now narrowed from a vague `80 != 81` test failure to a concrete list of `456` exact sequence targets.
- The likely code cause is identified as missing sequence-level canvas normalization after natural per-frame canvas expansion.

## Inputs For Phase 2

Phase 2 should define the policy in writing:

```text
one sequence
  -> choose one natural target canvas
  -> pad transparent pixels only
  -> preserve body position intentionally
  -> preserve motion extent
  -> never force old fixed 72x64 constraints
```

Recommended first policy decision: a sequence's target canvas should be the max width and max height already present in that sequence unless Phase 3 detects a reason to request manual visual review.

