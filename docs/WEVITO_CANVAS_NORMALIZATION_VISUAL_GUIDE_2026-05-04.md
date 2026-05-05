# Wevito Canvas Normalization Visual Guide

Updated: 2026-05-04

This guide records the visual-side response to the historical code-side runtime
canvas report:

```text
vnext/artifacts/runtime-canvas-contract-20260504-code-side.md
```

It is docs-only. It does not request sprite rewrites, runtime code changes,
generation, import, or broad repair automation.

## Code-Side Gate Update

Code-side stabilization is now green in the separate code-side worktree:

| Area | Current state |
| --- | --- |
| Runtime canvas contract | Passed: 2880 sequences, 10800 frames, 0 mixed-canvas rows, 0 missing/count rows, 0 invalid/non-alpha PNG rows |
| Full vNext tests | Passed: 26 / 26 |
| Debug vNext publish | Passed with `-SkipAssetPrep` |
| Popup-aware action/settings/link-bin probe | Passed |

Evidence read:

```text
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_TO_VISUAL_HANDOFF_2026-05-04.md
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md
C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md
```

The old 456 mixed-canvas result below is historical context only. Do not use it
as current blocking evidence unless the code-side worktree changes are rejected
or not merged.

```text
old blocker -> closed in code-side worktree
new blocker -> manifest/provenance/apply workflow before visual mutation
```

## Important Visual Decision

Do not force all sprites back into an old fixed canvas.

```text
wrong repair
  -> shrink/crop every animal into the old tiny box
  -> lose wings, legs, slithering bodies, jumps, expressive motion

right repair
  -> normalize each animation sequence to one stable canvas
  -> preserve natural animal motion
  -> add transparent padding/alignment as needed
```

The goal is sequence stability, not global sameness.

## Report Summary

Historical result from `runtime-canvas-contract-20260504-code-side.md`:

| Metric | Count |
| --- | ---: |
| Checked sequences | 2880 |
| Checked frames | 10800 |
| Mixed-canvas sequences | 456 |
| Missing/count-mismatch sequences | 0 |
| Invalid PNG frames | 0 |
| Fixed-reference canvas check | disabled |

Current code-side result:

| Metric | Count |
| --- | ---: |
| Checked sequences | 2880 |
| Checked frames | 10800 |
| Mixed-canvas sequences | 0 |
| Missing/count-mismatch sequences | 0 |
| Invalid/non-alpha PNG frames | 0 |

Current interpretation:

```text
sequence-stable canvas contract
  -> green in code-side worktree
  -> keep visual policy for future assets
  -> do not run new normalization from visual side
```

Mixed canvas by animation:

| Animation | Mixed sequences |
| --- | ---: |
| `bathe` | 36 |
| `eat` | 108 |
| `happy` | 36 |
| `idle` | 36 |
| `sad` | 84 |
| `sick` | 84 |
| `walk` | 72 |

Mixed canvas by species:

| Species | Mixed sequences |
| --- | ---: |
| `crow` | 72 |
| `deer` | 24 |
| `fox` | 108 |
| `rat` | 108 |
| `snake` | 144 |

First examples in the report are mostly one-pixel height differences such as:

```text
crow/adult/female/blue walk: 89x80, 89x81
crow/baby/female/blue walk: 72x66, 72x67
```

That is exactly the kind of issue that should usually be solved by transparent
padding and stable alignment, not crop or scale changes.

## Visual Contract

For any one sequence:

```text
sprites_runtime/<species>/<age>/<gender>/<color>/<animation>_00.png
sprites_runtime/<species>/<age>/<gender>/<color>/<animation>_01.png
...
```

All frames in that sequence should share:

- same canvas width
- same canvas height
- stable baseline/ground read
- stable body anchor
- transparent padding where extra motion needs room

Frames do not need to share the same canvas size as other species, ages, colors,
or animations if larger natural motion needs more room.

## Visual QA Before Any Repair

Before any asset mutation:

```text
1. choose a small affected target
2. create contact sheet from current frames
3. mark natural motion extremes
4. decide sequence canvas from maximum needed bounds
5. decide alignment anchor
6. only then allow deterministic padding/normalization
```

First visual review candidates:

| Priority | Target | Why |
| ---: | --- | --- |
| 1 | `snake` base animations | Highest mixed-canvas count and natural long-body motion. |
| 2 | `rat` base animations | High mixed-canvas count; small body can be harmed by over-cropping. |
| 3 | `fox` base animations | High mixed-canvas count; legs/tail need preservation. |
| 4 | `crow` base animations | Wings/body height needs stable frame room. |
| 5 | `deer` base animations | Lower count but legs/antlers can be damaged by tight boxes. |

First concrete target should be one sequence, for example:

```text
snake / baby / female / blue / walk
```

Reason: it tests the rule that natural motion should be preserved rather than
collapsed into a fixed box.

## Normalization Rules

Acceptable:

- transparent padding
- stable per-sequence canvas dimensions
- baseline/contact alignment
- preserving natural frame extrema
- preserving color, alpha, silhouette, and frame count
- recording before/after dimensions and hashes
- contact sheet before and after

Not acceptable:

- scaling the animal down only to fit old dimensions
- cropping tails, wings, legs, antlers, jumps, or slither poses
- changing pose art
- changing color palette
- changing alpha silhouette
- aligning by top-left if it makes the pet jitter
- applying one global canvas size to all species without visual reason

## Alignment Policy

Preferred alignment:

```text
ground/contact baseline first
body center second
top-left last
```

Use top-left only if the sequence was authored that way and visual review shows
no jitter.

Species notes:

| Species | Alignment concern |
| --- | --- |
| `snake` | Preserve full body curve and slither length; do not compress to mammal-like box. |
| `crow` | Preserve wing/body height and beak silhouette. |
| `deer` | Preserve legs, head, and antler/head clearance. |
| `fox` | Preserve legs, tail, and forward motion. |
| `rat` | Preserve tiny feet, tail, and nose; avoid losing small silhouette features. |

## Acceptance Checklist

Use this after any future normalization attempt:

| Check | Pass/Fail | Notes |
| --- | --- | --- |
| All frames in sequence share canvas width/height |  |  |
| No frame is cropped |  |  |
| No frame is scaled down |  |  |
| Transparent padding only where needed |  |  |
| Baseline/contact point is stable |  |  |
| Loop has less jitter than before |  |  |
| Species identity preserved |  |  |
| Color variant preserved |  |  |
| Alpha/background remains clean |  |  |
| Before/after contact sheet reviewed |  |  |
| Runtime proof reviewed when code-side gate allows |  |  |

## Stop Rules

Stop normalization work if:

- a repair shrinks animal motion instead of padding around it
- a repair crops natural extrema
- a repair fixes tests but worsens visual motion
- alignment causes more jitter than the original sequence
- the operation cannot be described as deterministic and reversible
- provenance/backup paths are unclear

## Relationship To Existing Visual Plans

This guide updates the non-generation cleanup lane:

```text
old cleanup concern
  -> mixed canvas / crop / jitter

new concrete rule
  -> per-sequence stable canvas with natural motion preserved
```

It does not change the optional animation priority:

```text
after code-side gates
  -> goose hold_ball endpoint
  -> goose pickup/drop transitions
```

But it adds a prerequisite for broad visual production:

```text
base animation canvas stability should be resolved or explicitly exempted
before broad new generated art is imported
```

## Current Recommendation

Next visual-side work should be:

```text
1. treat the old mixed-canvas count as historical
2. preserve this guide as the future visual rule for natural per-sequence canvases
3. continue no-edit QA/contact sheets only
4. wait for manifest/provenance/apply coordination before any PNG mutation
```

The visual-side job is still to make sure any future repair preserves the
animals, not just the test. The current code-side job is now the production-safe
apply workflow, not fixing the old canvas gate.
