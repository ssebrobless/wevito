# C-PHASE 30 vNext Release Candidate Decision

Date: 2026-05-12
Tag: `v0.1.0-vnext-rc1`

## Decision

Proceed with a vNext-only release candidate tag from the validated C-PHASE 30 package path.

## Why

The vNext helper shell release path is validated:

- Release build passed.
- Release tests passed, 278/278.
- Release publish passed with `-SkipAssetPrep`.
- Runtime canvas contract remained green with 0 mixed rows.
- Godot script checks passed.

The Godot desktop export path remains blocked by asset import/export pressure from thousands of individual PNG resources. That is a packaging hardening problem, not a reason to hold the validated vNext helper shell release candidate.

## Scope Of This Release Candidate

Included:

- vNext Shell and Broker release artifacts.
- Current validated runtime assets as-is.
- PET TASKS/tool hub release-candidate surface.
- User help guide.

Excluded:

- Godot desktop executable release.
- Broad sprite/runtime asset regeneration.
- Live model calls.
- Any final production release claim.

## Follow-Up Phase

Start a focused Godot packaging hardening phase after this tag. The likely direction is to stop forcing Godot to import thousands of loose runtime PNGs for release packaging, either by using external runtime asset loading, a slim Godot runtime bundle, atlases/pack files, or another asset-package strategy.
