# Code-Side To Visual Handoff - 2026-05-04

This handoff summarizes the code-side runtime, validation, asset-contract, and packaging-readiness work completed in the Codex worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

The visual-side thread should use this as coordination input before starting any sprite generation, sprite import, runtime PNG mutation, or broad visual implementation work.

## Current Coordination Shape

```text
CODE-SIDE STATUS
|
+-- Runtime canvas contract
|   +-- Green: 2880 sequences, 10800 frames
|   +-- Green: 0 mixed-canvas rows
|   +-- Green: 0 missing/count rows
|   +-- Green: 0 invalid/non-alpha PNG rows
|
+-- vNext validation
|   +-- Green: focused sprite/simulation tests
|   +-- Green: full vNext tests, 26 / 26
|   +-- Green: build-vnext Debug -SkipAssetPrep
|   +-- Green: popup-aware action/tool probe
|
+-- Visual work permissions
|   +-- Allowed now: no-edit QA, contact sheets, planning
|   +-- Paused now: generation, import, runtime/source PNG mutation
|   +-- Coordinate first: production pilot, manifest/apply workflow
|
+-- Commit/package readiness
    +-- vNext debug shell publish ready
    +-- Godot release packaging not ready from this worktree yet
    +-- No files staged, committed, pushed, or release-packaged
```

## What Changed On The Code Side

The code-side work closed the old sprite runtime canvas gate and converted it into an explicit natural per-sequence frame-size contract. The active contract is now:

- Frames inside the same animation row must have a stable canvas size.
- Different animation rows may use different natural canvas sizes.
- PNG frames must preserve alpha.
- Missing frames, frame-count mismatches, mixed-canvas rows, and non-alpha PNG outputs are active failures.
- Legacy fixed `72x64` mismatches are diagnostic only unless explicitly requested.

The code-side work also validated the vNext runtime shell, action controls, settings/tool popup behavior, basket/link flow, and current sprite loading path.

## Code-Side Evidence Docs

- Phase 2 merge-safety report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE2_DIRTY_WORKTREE_MERGE_SAFETY_2026-05-04.md`
- Phase 3 validation sweep: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md`
- Phase 4 runtime hardening: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md`
- Phase 5 visual readiness check: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE5_VISUAL_READINESS_CHECK_2026-05-04.md`
- Phase 6 commit/packaging readiness: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE6_COMMIT_PACKAGING_READINESS_2026-05-04.md`
- Main checklist: `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md`

## Validation Artifacts

- Final sequence-stable canvas report: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-final-sequence-stable.md`
- Phase 3 runtime canvas validation: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase3-validation.md`
- Phase 4 hardened runtime canvas validation: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.md`
- Passing action/tool probe summary: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`
- Debug shell publish entry point: `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\shell\Wevito.VNext.Shell.exe`

## Important Update For Visual Gate Docs

The main-project visual docs were read from:

`C:\Users\fishe\Documents\projects\wevito\docs`

Those docs may still say the runtime production gate is blocked by `456` mixed-canvas failures or by a full vNext test failure. That status is stale relative to this code-side worktree.

Current code-side result:

- Runtime canvas validation: green.
- Full vNext test project: green, `26 / 26`.
- `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180`: green.
- Popup-aware action/tool probe: green.

The visual thread should update its gate docs before using those old failures as blockers.

## What The Visual Thread Can Safely Do Now

```text
SAFE VISUAL WORK NOW
|
+-- No-edit contact sheets
+-- No-edit color variant QA
+-- No-edit optional animation review
+-- Visual rubric updates
+-- Production-gate doc updates using current code-side evidence
+-- Planning for manifest/provenance/apply workflow
```

These are safe because they do not mutate runtime/source sprite PNGs and do not lock in new generation/import conventions before the Hatch Pet-inspired workflow is finalized.

## What Remains Paused

```text
PAUSED UNTIL COORDINATED
|
+-- New Gemini/OpenAI sprite generation
+-- Sprite import into runtime/source trees
+-- Runtime PNG mutation
+-- Source PNG mutation
+-- Broad asset rewrites
+-- Broad runtime/content implementation
+-- One-row production pilot unless explicitly approved
```

The code-side work made the runtime contract testable, but it did not create the final production-safe import/apply workflow. Visual mutation should wait until that workflow is designed or explicitly approved for a narrow pilot.

## Readiness Gaps To Coordinate

### Manifest / Provenance / Apply Workflow

The repo still needs a production-safe asset workflow that records source references, generated candidates, hashes, review status, accepted outputs, and rollback paths. This should be aligned with the Hatch Pet-inspired plan before visual production work resumes.

Suggested direction:

- Add manifest metadata before importing new visuals.
- Keep candidate assets separate from accepted runtime assets.
- Require contact-sheet review and automated validation before applying.
- Keep an explicit rollback target for every applied batch.

Risk: high if skipped, because new visual work could become hard to audit or revert.

### Optional Animation Addressing

The vNext runtime currently supports core states plus work-companion states, but not first-class optional animation families such as:

- `hold_ball`
- `pickup_ball`
- `drop_ball`
- `carry_ball_walk`
- `carry_ball_run`

The visual docs can review these families, but code-side routing still needs a cleaner addressing model before those become reliable gameplay-facing animations.

Risk: medium/high, because visual assets could be created but remain unreachable or inconsistently mapped at runtime.

### Body-Condition Renderer

The runtime currently resolves pet sprites by species, age, gender, color, and animation. It does not yet support fat/skinny condition variants as a first-class rendering dimension.

Risk: medium, because body-condition art can be planned now but should not be imported as if the renderer already supports it.

### Ghost Renderer

The runtime does not yet have a complete ghost-form rendering path or death-to-ghost lifecycle renderer. Ghost visuals should remain in design/planning until the code path is specified.

Risk: medium, because ghost sprites could otherwise become disconnected concept art.

### Habitat Zones / Depth / Occlusion / Contact Shadows

Habitat presentation exists, and the shell has hardcoded presentation helpers, but there is not yet a full content-driven contract for:

- object zones
- perch/enter/overlap anchors
- depth bands
- occlusion rules
- contact shadows
- pet/object interaction placement

Risk: medium/high, because polished habitat assets need runtime placement and depth rules to avoid looking pasted onto the scene.

### Care Item Content Mapping

Existing care/medicine art appears under-mapped rather than absent. The runtime has broad action/category concepts, but individual care assets are not yet fully first-class treatment/content mappings.

Risk: medium, because new medicine art should not be generated before the existing art and intended care meanings are mapped.

### Asset Prep Policy

`tools\build-vnext.ps1` can regenerate runtime assets unless `-SkipAssetPrep` is used. Code-side validation used `-SkipAssetPrep` to preserve the current tested runtime PNG payload.

Risk: medium, because an accidental asset-prep run can overwrite or conflict with the current runtime PNG state.

## Dirty Worktree And Merge Notes

No files were staged, committed, pushed, or packaged during this phase set.

The worktree still has a large dirty state, including:

- `1704` tracked runtime PNG changes under `sprites_runtime`.
- `1704` untracked recovery PNG backups under `artifacts\recovery`.
- vNext runtime bridge/reliability code changes.
- sequence-stable canvas tooling/test/docs changes.
- older Godot script changes and Gemini/browser helper tools that should remain separate-review.

The recommended staging buckets are documented in:

`C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE6_COMMIT_PACKAGING_READINESS_2026-05-04.md`

Do not use a broad `git add .` from this worktree.

## Recommended Next Coordination Sequence

```text
NEXT SEQUENCE
|
+-- 1. Visual thread updates stale production-gate docs
|
+-- 2. Visual thread continues no-edit QA/contact sheets
|
+-- 3. Code side designs manifest/provenance/apply workflow
|
+-- 4. Code side designs optional animation addressing
|
+-- 5. Both sides approve one narrow visual production pilot
|
+-- 6. Pilot applies through manifest workflow
|
+-- 7. Validate runtime tests + contact sheets + shell probe
```

This keeps the visual thread productive without letting new sprite mutations outrun the code-side safety rails.

## Single Biggest Remaining Blocker

The biggest blocker is not the canvas contract anymore. It is the missing production-safe asset apply workflow: manifest, provenance, review status, accepted-output hashes, validation proof, and rollback. Until that exists, new generation/import work remains risky even though the current runtime contract is green.

