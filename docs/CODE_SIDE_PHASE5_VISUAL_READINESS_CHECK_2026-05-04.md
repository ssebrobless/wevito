# Code-Side Phase 5 Visual Readiness Check

Date: 2026-05-04

Worktree:

`C:\Users\fishe\.codex\worktrees\36d6\wevito`

Purpose:

Check whether the current code-side runtime is ready for the visual-side thread to continue QA, contact sheets, and eventually asset implementation. This phase did not generate images, import sprites, mutate PNGs, stage files, or implement broad runtime changes.

## Input Docs Reviewed

The visual-side docs are not present in this forked worktree yet. They were read from the main project path:

`C:\Users\fishe\Documents\projects\wevito\docs`

Key docs reviewed:

- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_HANDOFF_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_NEXT_EXECUTION_PLAN_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_COLOR_VARIANT_QA_PLAN_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_MEDICINE_CARE_VISUAL_MAPPING_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_HABITAT_OBJECT_LOADOUT_PLAN_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_REMAINING_PHASE_PLAN_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md`

Code-side reports reviewed:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE2_DIRTY_WORKTREE_MERGE_SAFETY_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md`

## Readiness Shape

```text
visual work type
|
+-- no-edit contact sheets / QA
|   +-- ready
|   +-- use current runtime assets
|   +-- cite code-side Phase 3/4 validation
|
+-- visual generation / import / PNG mutation
|   +-- still paused
|   +-- needs explicit user approval
|   +-- needs manifest/provenance/apply workflow
|   +-- needs updated visual gate docs
|
+-- runtime/content implementation
    +-- partial
    +-- enough for current shell proof/probes
    +-- not enough for ghost/body-condition/habitat-zone roadmap
```

## What Is Ready

### Runtime Sprite Contract

Ready for non-mutating review.

Evidence:

- Phase 3 canvas validation: `2880` sequences, `10800` frames, `0` mixed-canvas sequences, `0` missing/count mismatches, `0` invalid PNGs.
- Phase 4 hardening: reporter now also fails non-alpha PNG color types, matching the C# test contract.
- `SpriteRuntimeCoverageTests` passed after the current natural per-sequence canvas contract.

Relevant files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\report_runtime_canvas_mismatches.py`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\SpriteRuntimeCoverageTests.cs`

### Build/Test/Probe Reliability

Ready for code-side proof loops.

Evidence:

- Full vNext tests passed `26 / 26`.
- `build-vnext.ps1 -Configuration Debug -SkipAssetPrep -StepTimeoutSeconds 180` passed tests, broker publish, and shell publish.
- Popup-aware action/tool probe passed all action traces, settings traces, basket traces, save trace, and shell-alive checks.

Relevant files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\build-vnext.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\tools\probe-vnext-actions.ps1`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\action-probes\20260504-205959\summary.json`

### Color QA Contact Sheets

Ready as no-edit visual QA.

Reason:

The runtime folder structure is complete and the current code-side runtime contract is green. The visual side can safely continue contact sheets for color review as long as it does not overwrite source/runtime PNGs.

First safe target from visual docs:

`goose / baby / female / all six colors`

### Dev/Audit Scenario Seams

Partially ready.

Ready today:

- The shell supports `WEVITO_VNEXT_SCENARIO_PATH`.
- Scenarios can spawn pets by species, age, gender, color, animation state, facing direction, and environment.
- Dev tools can apply appearance, environment, vitals, animation overrides, presets, and conditions.
- The probe uses isolated scenarios to keep validation reliable.

Relevant files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`

## Stale Or Conflicting Visual-Gate Assumptions

The visual production gate in the main project docs is now stale relative to this code-side worktree.

Stale statement:

- `WEVITO_PRODUCTION_GATE_CHECK_2026-05-04.md` says full vNext tests still fail because `sprites_runtime` has `456` internally mixed-canvas base-animation sequences.

Current code-side evidence:

- Phase 3/4 reporter runs show `0` mixed-canvas sequences.
- Full vNext tests pass `26 / 26`.
- `build-vnext.ps1` passes in this worktree when run with `-SkipAssetPrep`.

Recommendation:

Before visual production opens, update the visual-side gate docs to cite:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE3_VALIDATION_SWEEP_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\docs\CODE_SIDE_PHASE4_RUNTIME_CONTRACT_HARDENING_2026-05-04.md`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\artifacts\runtime-canvas-contract-20260504-phase4-hardening.md`

## Readiness Gaps

### Manifest / Provenance / Apply Workflow

Status: not ready for mutation.

The visual docs describe manifest/provenance discipline, but this worktree does not yet have an approved end-to-end production runner that:

- hashes source references
- writes a manifest before and after generation/import
- backs up exact target frames
- applies a candidate
- produces contact sheet and preview proof
- runs runtime proof
- supports exact rollback

Recommendation:

Plan this as a code-side implementation phase before any generation/import pilot.

### Optional Animation Families

Status: partially ready, with a gap.

The runtime folders and some Godot-side script changes recognize optional families such as `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, and `carry_ball_run`. The vNext shell enum currently does not expose these optional animation states as first-class `PetAnimationState` values.

Current vNext states include base states and work-companion states:

`Idle`, `Walk`, `Eat`, `Happy`, `Sad`, `Sleep`, `Sick`, `Bathe`, `Waving`, `Jumping`, `Failed`, `Waiting`, `Review`.

Recommendation:

Before the goose hold-ball pilot is implemented in the vNext shell, decide whether optional visual families become new runtime states, action-scoped sprite aliases, or a separate prop-animation layer.

### Body Condition Rendering

Status: not ready.

The simulation tracks `obesity`, `malnutrition`, fitness, health, habits, and conditions, but the renderer only chooses sprites by:

`species / age / gender / color / animation`

There is no body-condition sprite dimension, overlay layer, or morph/scaling policy for fat/skinny variants.

Recommendation:

Plan body-condition rendering separately after base visual QA is stable. Avoid adding fat/skinny sprite assets until the runtime addressing scheme is agreed.

### Ghost Rendering

Status: not ready.

The current inspected vNext model has aging/conditions but no clear death/ghost lifecycle renderer, no ghost form asset routing, and no per-pet ghost material/overlay contract.

Recommendation:

Treat ghost form as a future runtime feature: likely an overlay/material or shader-like tint pipeline rather than full duplicate sprite grids for every random trait combination.

### Habitat Object Zones / Occlusion / Depth

Status: partial.

What exists:

- `HabitatLoadoutResolver` recommends items and builds a few dynamic stage props.
- `HomePanelWindow` renders backdrop, ground shade, props, pet sprites, and interaction cues.
- There are early hardcoded interaction placement rules for feed/water/bath/rest/home/play.

What is missing:

- data-driven habitat object zone records
- object role metadata in content
- per-object depth bands
- per-object occlusion behavior
- per-object pet anchor hints
- per-object contact shadow policy
- stage-safe validation for object assets like `blanket_mat`

Relevant files:

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\HabitatLoadoutResolver.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs`

Recommendation:

Do not add broad habitat content yet. First create a small content contract for object role, depth band, interaction zone, occlusion mode, and pet anchor hint.

### Medicine/Care Item Mapping

Status: partial.

What exists:

- Runtime art for care items exists in shared assets.
- `HabitatLoadoutResolver` has hardcoded care pools for grooming, bath, medicine, doctor, and emergency doctor.

What is missing:

- `items.json` only exposes broad `care-medicine` and `care-doctor`.
- First-class care item content records do not exist for `medicine_dropper`, `pill_bottle`, `bandage_roll`, `thermometer`, `grooming_brush`, `soap_bottle`, `towel`, or `syringe`.
- Condition-to-care visual mapping is in docs, not content.

Recommendation:

Once visual review confirms care item readiness, move the mapping into content instead of expanding the hardcoded shell pools.

### Build Asset Prep Risk

Status: needs a policy decision.

`build-vnext.ps1` can still run asset prep unless `-SkipAssetPrep` is used. That asset prep path may regenerate `sprites_runtime`, which is risky while the normalized runtime PNG payload is not yet staged/committed and while visual import policy is still being coordinated.

Recommendation:

Use `-SkipAssetPrep` for code-side validation until the runtime PNG payload and visual import workflow are reconciled. Treat full asset-prep builds as an explicit asset-pipeline phase.

## Safe Next Code-Side Tasks

```text
safe now
|
+-- reconcile visual docs with Phase 3/4 validation
+-- design manifest/provenance/apply runner
+-- design optional animation addressing in vNext
+-- design habitat object-zone content contract
+-- design body-condition and ghost rendering contracts
+-- add read-only reports/probes
```

## Still Paused

```text
paused
|
+-- visual generation
+-- sprite import
+-- runtime/source PNG mutation
+-- broad Godot script merge
+-- broad habitat/content implementation
+-- fat/skinny/ghost asset production
```

## Phase 5 Decision

No-edit visual QA and contact sheets can safely continue.

Visual generation/import/runtime PNG mutation should stay paused until:

1. The visual gate docs are updated with current code-side green validation.
2. The manifest/provenance/apply workflow is implemented or explicitly accepted as manual.
3. Optional animation addressing is decided for the vNext shell.
4. The user explicitly approves a one-row production pilot.
