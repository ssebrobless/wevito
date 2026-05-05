# Wevito Creative Learning Lab Plan

Date: 2026-05-05

Purpose: turn Claude Design's "Creative Learning Lab" artboard into a concrete future plan for reviewed examples, labels, preference comparisons, dataset bundles, and visual evaluation.

This is a visual/product planning document. It does not authorize training, data export, unreviewed example promotion, automatic imports, sprite generation, source/runtime PNG mutation, or model fine-tuning.

## Product Role

Creative Learning Lab is a future review dashboard, not a gameplay overlay and not an automatic training machine.

```text
visual work artifacts
  |
  +-- raw examples
  +-- cleaned examples
  +-- accepted/rejected labels
  +-- preference comparisons
  +-- eval benchmarks
  |
  v
Creative Learning Lab
  |
  +-- organizes evidence
  +-- makes review state visible
  +-- gates bundle readiness
  +-- never promotes unreviewed data
```

## What It Should Help With

Wevito has a growing amount of visual evidence:

- sprite cleanup before/after sheets
- color variant atlases
- optional animation candidate sheets
- packaged proof screenshots
- artifact manifests
- issue classifications
- accepted/rejected visual decisions

Creative Learning Lab should make that review history useful and searchable.

## Claude Artboard Elements To Keep

| Claude element | Keep? | Wevito use |
| --- | --- | --- |
| Raw example count | Yes | All incoming/proposed examples not yet reviewed. |
| Cleaned example count | Yes | Examples with background/noise/crop cleanup complete. |
| Labeled example count | Yes | Examples with accept/reject/revise decisions. |
| Bundle count | Yes | Human-approved sets for future eval/training. |
| Eval benchmark bars | Yes | Quality gates before trusting a workflow. |
| Preference comparison cards | Yes | A/B review of candidates. |
| Reviewer feedback log | Yes | Keeps human reasons attached to decisions. |
| Training/export buttons | Defer | Only after explicit future approval. |

## Data States

```text
example lifecycle
  |
  +-- raw
  |     +-- captured or generated but not reviewed
  |
  +-- cleaned
  |     +-- obvious technical junk removed
  |     +-- source/provenance preserved
  |
  +-- labeled
  |     +-- accept
  |     +-- reject
  |     +-- revise
  |     +-- defer
  |
  +-- bundled
  |     +-- reviewed group
  |     +-- manifest
  |     +-- intended use
  |
  +-- evaluated
        +-- benchmark result
        +-- release decision
```

## Label Vocabulary

| Label | Meaning | Can enter bundle? |
| --- | --- | --- |
| `accept` | Good enough for intended reference/eval use. | yes |
| `reject` | Should not be used. | no |
| `revise` | Potentially useful after correction. | no, not yet |
| `defer` | Not enough context or waiting on proof. | no |
| `blocked` | Policy/ownership/technical gate prevents use. | no |

## Example Types

| Type | Source | Review need |
| --- | --- | --- |
| `sprite-cleanup-before-after` | visual-review artifacts | Verify fix was real and not destructive. |
| `color-variant-atlas` | color coverage artifacts | Verify palette quality and identity. |
| `optional-animation-candidate` | animation-run artifacts | Verify pose, overlay policy, manifest, proof. |
| `packaged-proof` | Godot/vNext proof artifacts | Verify runtime behavior matches artifact claim. |
| `ui-prototype` | Claude Design / screenshots | Extract design ideas, not code truth. |
| `habitat-loadout` | object review sheets/mockups | Verify object scale/readability. |

## Bundle Readiness Gates

Before an example bundle is trusted:

```text
bundle gate
  |
  +-- source paths recorded
  +-- provenance recorded
  +-- reviewer labels present
  +-- rejected examples excluded
  +-- no secrets/personal data
  +-- no unapproved sprite mutation
  +-- intended use stated
  +-- rollback/deprecation path known
```

## Eval Benchmarks

Potential Wevito visual evals:

| Eval | Measures | Minimum useful signal |
| --- | --- | --- |
| `sprite-validator-vs-human` | Validator agreement with human labels. | Finds missed noise/crop defects. |
| `palette-identity` | Color variant readability. | Species identity survives recolor. |
| `optional-overlay-policy` | Prop overlay correctness. | Ball/drink props are not baked when they should be runtime overlays. |
| `contact-sheet-clarity` | QA sheet usefulness. | Reviewer can make decision from sheet. |
| `habitat-scale` | Object/pet scale fit. | Objects do not overpower or hide pets. |
| `ui-safety-language` | Tool UI clarity. | User can tell preview vs apply vs blocked. |

## UI Shape

```text
Creative Learning Lab
  |
  +-- top metrics
  |     +-- raw
  |     +-- cleaned
  |     +-- labeled
  |     +-- bundled
  |     +-- eval
  |
  +-- review queue
  |     +-- examples needing human decision
  |
  +-- comparison panel
  |     +-- A vs B candidates
  |
  +-- bundle panel
  |     +-- ready / waiting / blocked
  |
  +-- eval panel
  |     +-- benchmark bars
  |
  +-- feedback log
        +-- reviewer notes
```

## Relationship To Overlay

Creative Learning Lab is not part of the always-on overlay.

```text
overlay
  |
  +-- pet can say "review queue has 3 items"
  |
  v
Creative Learning Lab
  |
  +-- opens as larger workbench/dashboard
```

The overlay may surface small notifications or PET TASKS summaries, but the lab itself should be a larger summoned surface.

## Relationship To Sprite Workflow V2

Sprite Workflow V2 handles active production/proof/apply workflows.

Creative Learning Lab handles historical reviewed examples and eval/bundle readiness.

```text
Sprite Workflow V2
  |
  +-- current row
  +-- candidate
  +-- proof
  +-- apply/rollback
  |
  v
accepted/rejected evidence
  |
  v
Creative Learning Lab
```

## First Safe Slice

The first implementation should be read-only:

1. Scan existing `vnext/artifacts/visual-review` and `vnext/artifacts/animation-runs`.
2. Index markdown/JSON manifests.
3. Show a local dashboard of review packets and labels.
4. Do not import/export/training-write anything.
5. Add manual labels only in a separate, review-owned metadata file.

## Blocked Until Explicit Approval

- model training
- dataset export
- automatic example promotion
- unreviewed bundle creation
- mutation of source/runtime PNGs
- provider generation
- uploading local project data to external services

## Visual Acceptance Criteria

- The user can see what is raw, cleaned, labeled, bundled, and evaluated.
- No rejected/deferred example appears ready for training/export.
- Every bundle has source/provenance.
- Review labels are human-readable.
- Eval bars do not imply perfection; they show gate status.
- The lab feels like a review room, not the main pet home.

