# Runtime Canvas Normalization Phase 3 Dry-Run - 2026-05-04

Scope: code-side Phase 3. This phase added and ran a dry-run reporter that plans sequence-stable transparent padding without editing any PNGs.

## Shape

```text
Phase 1
  -> identify 456 mixed-canvas sequences

Phase 2
  -> define sequence-stable natural canvas policy

Phase 3
  -> dry-run exact transparent-padding operations
       |
       +-- JSON plan
       +-- Markdown summary
       `-- no mutation
```

## Tool Added

| File | Purpose |
|---|---|
| `tools/plan_runtime_canvas_normalization.py` | Scans required base runtime sprite sequences and plans transparent padding to make each sequence canvas-stable. Dry-run only. |

The tool reports:

- target canvas per sequence
- current canvas per frame
- proposed transparent padding per frame
- files that would change
- files that would remain unchanged
- risk class and reason

## Dry-Run Command

```powershell
python .\tools\plan_runtime_canvas_normalization.py `
  --output .\vnext\artifacts\runtime-canvas-normalization-plan-20260504.json `
  --markdown .\vnext\artifacts\runtime-canvas-normalization-plan-20260504.md
```

## Artifacts

| Artifact | Purpose |
|---|---|
| `vnext/artifacts/runtime-canvas-normalization-plan-20260504.json` | Full machine-readable dry-run plan. |
| `vnext/artifacts/runtime-canvas-normalization-plan-20260504.md` | Human-readable summary grouped by risk/species/animation. |

## Results

| Check | Result |
|---|---:|
| Checked sequences | 2,880 |
| Checked frames | 10,800 |
| Planned sequences | 456 |
| Planned changed frames | 900 |
| Missing sequences | 0 |
| Invalid frames | 0 |
| `safe_transparent_pad` | 396 |
| `review_alignment` | 60 |
| `manual_visual_review` | 0 |
| `not_canvas_repair` | 0 |

## Distribution

| Species | Planned sequences |
|---|---:|
| `snake` | 144 |
| `fox` | 108 |
| `rat` | 108 |
| `crow` | 72 |
| `deer` | 24 |

| Animation | Planned sequences |
|---|---:|
| `eat` | 108 |
| `sad` | 84 |
| `sick` | 84 |
| `walk` | 72 |
| `idle` | 36 |
| `happy` | 36 |
| `bathe` | 36 |

## Risk Breakdown

```text
456 planned sequences
  |
  +-- 396 safe_transparent_pad
  |     `-- eligible for a future guarded Phase 4 mutating repair
  |
  `-- 60 review_alignment
        `-- transparent padding can stabilize them, but top/bottom alignment
            should be reviewed or governed by a better rule first
```

`review_alignment` is concentrated in:

| Risk | Species | Count |
|---|---|---:|
| `review_alignment` | `crow` | 48 |
| `review_alignment` | `fox` | 12 |

| Risk | Animation | Count |
|---|---|---:|
| `review_alignment` | `walk` | 36 |
| `review_alignment` | `eat` | 18 |
| `review_alignment` | `sad` | 6 |

## Interpretation

The dry-run supports the Phase 1 diagnosis:

```text
problem type
  -> sequence canvas jitter
  -> one-pixel height mismatch
  -> transparent padding candidate

not problem type
  -> missing frame
  -> invalid PNG
  -> forced old 72x64 requirement
  -> visual clone quality
  -> artifact cleanup
```

The 396 safe rows can probably be padded automatically in a future Phase 4 if approved. The 60 review rows should either receive a better deterministic alignment rule or be handed to the visual thread as contact-sheet review targets before mutation.

## Phase 3 Audit

Phase 3 passed its boundary:

- No sprite PNGs were edited.
- No source boards were edited.
- No generation or import was started.
- The tool is dry-run by design and has no write/apply flag.
- Syntax check passed with `python -m py_compile`.
- The output plan lists exact operations and risk classes.

## Next Phase Inputs

Phase 4 should not blindly mutate all 456 sequences. Recommended next sequence:

```text
Phase 4A
  -> implement guarded apply path
  -> apply only safe_transparent_pad rows
  -> backup before write
  -> rerun canvas report + SpriteRuntimeCoverageTests

Phase 4B
  -> decide whether review_alignment rows need:
       a better deterministic alignment rule
       or visual contact-sheet review first
```

If the user wants maximum safety, run Phase 4A first on the 396 safe rows only and leave the 60 alignment-review rows untouched until visual-side review catches up.

