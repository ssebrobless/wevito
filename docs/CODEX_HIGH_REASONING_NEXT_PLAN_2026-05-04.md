# Codex High-Reasoning Next Plan - 2026-05-04

This plan reconciles the Codex code-side audit, the Codex high-reasoning handoff, and Claude's Hatch Pet adoption plan. It is a concrete implementation sequence for what to do next without accidentally duplicating work that already exists or colliding with the active sprite/visual lane.

No broad code or asset changes were made while writing this plan.

## Inputs Reviewed

| Path | Role |
| --- | --- |
| `C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_RUNTIME_AUDIT_2026-05-04.md` | Codex audit of runtime/code readiness, tests, controls, and gaps. |
| `C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_AUDIT_HIGH_REASONING_HANDOFF_2026-05-04.md` | Handoff summary and original high-reasoning prompt. |
| `C:\Users\fishe\Documents\projects\wevito\docs\CLAUDE_HATCH_PET_WEVITO_PLAN_2026-05-04.md` | Claude's Hatch Pet adoption plan. This now exists and should be considered authoritative for generation-contract direction. |
| `C:\Users\fishe\Documents\projects\wevito\docs\HATCH_PET_WEVITO_HANDOFF_2026-05-04.md` | Codex's original Hatch Pet handoff. Do not delete or rewrite. |
| `C:\Users\fishe\Documents\projects\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md` | Current coordination state. Dirty and active; do not edit casually. |
| `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\workflow-runs\20260504-optional-family-visual-review-summary.md` | Optional family proof summary. |
| `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_ANIMATION_GENERATION_CONTRACT.md` | Phase 1 contract doc, already present. |
| `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_ANIMATION_QA_RUBRIC.md` | Phase 1 QA rubric, already present. |
| `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_WORK_COMPANION_STATES.md` | Phase 1 work-companion state vocabulary, already present. |
| `C:\Users\fishe\Documents\projects\wevito\docs\wevito-animation-run.schema.json` | Phase 1 manifest schema, already present. |

## Current Shape

```text
WEVITO NEXT WORK
|
+-- A. Contract/workflow lane
|   +-- Phase 1 docs exist
|   +-- Phase 2 helper tools exist syntactically
|   +-- Needs behavior validation and ownership/commit decision
|
+-- B. Code/test reliability lane
|   +-- build-vnext.ps1 hung
|   +-- dotnet test fails on sprite canvas mismatch
|   +-- vNext UI probe fails at Settings Save
|
+-- C. Runtime feature lane
|   +-- Godot can drive optional families
|   +-- vNext cannot drive optional families first-class yet
|   +-- Habitat depth/occlusion/contact shadows need design
|
+-- D. Visual generation lane
    +-- Optional families are complete and packaged-validated
    +-- Bespoke grip/pickup poses remain the honest visual ceiling
    +-- Broad generation/import should wait until A and B are stable
```

## Reconciled Judgment

Claude's recommendation to land Phase 1 first is correct, but the Phase 1 files already exist in the working tree. The next useful move is not to recreate them. The next useful move is to validate and stabilize what is already present, then fix deterministic code/test gates before doing any broad visual generation or asset import.

The order should be:

1. Confirm Phase 1 docs/schema and Phase 2 helper tools are internally consistent.
2. Recover deterministic build/test confidence.
3. Make vNext able to prove optional animation families without changing art.
4. Add habitat grounding/depth contracts.
5. Only then run bespoke visual authoring batches.

## Phase 0 - Coordination And Existing Artifact Verification

Goal: Turn the current mixed planning state into a stable checkpoint.

Current evidence:

| Artifact | State |
| --- | --- |
| `WEVITO_ANIMATION_GENERATION_CONTRACT.md` | Present. |
| `WEVITO_ANIMATION_QA_RUBRIC.md` | Present. |
| `WEVITO_WORK_COMPANION_STATES.md` | Present. |
| `wevito-animation-run.schema.json` | Present and JSON-parses. |
| `tools/generate_layout_guide.py` | Present and syntax-compiles. |
| `tools/prepare_wevito_animation_run.py` | Present and syntax-compiles. |
| `tools/finalize_wevito_animation_run.py` | Present and syntax-compiles. |
| `tools/record_wevito_animation_result.py` | Present and syntax-compiles. |

Commands already run during this planning pass:

```powershell
python - <<inline compile/json parse check>>
```

Result:

```text
compile-ok tools\generate_layout_guide.py
compile-ok tools\prepare_wevito_animation_run.py
compile-ok tools\finalize_wevito_animation_run.py
compile-ok tools\record_wevito_animation_result.py
json-ok docs/wevito-animation-run.schema.json
```

Affected files:

| Action | Files |
| --- | --- |
| Review only | Phase 1 docs and Phase 2 helper tools listed above. |
| Do not edit unless needed | `VNEXT_IMPLEMENTATION_CHECKLIST.md`, `.sprite-workflow/**`, dirty sprite repair/import tools. |

Validation plan:

| Check | Command/approach |
| --- | --- |
| Schema is valid JSON | `python -c "import json; json.load(open('docs/wevito-animation-run.schema.json'))"` |
| Phase 2 scripts parse | Compile source with Python `compile()` without writing `__pycache__`. |
| Manifest example conforms | Build a small sample manifest and run the helper's schema smoke validator. |
| No broad side effects | `git status --short` before/after. |

Rollback plan:

| If something is wrong | Rollback |
| --- | --- |
| A doc has a bad contract assumption | Patch only the new Phase 1 doc, not old coordination docs. |
| A helper tool is broken | Patch only the new helper tool or mark it experimental in this plan. |
| Artifact ownership is unclear | Pause before committing and ask which active lane owns the untracked files. |

Risk: Low.

Recommended status: Do immediately before code changes.

## Phase 1 - Deterministic Build/Test Gate Recovery

Goal: Restore trust in local code gates before visual work resumes.

This phase should happen before optional-family runtime expansion or new art import because otherwise every later validation result is suspect.

Tasks:

| Task | Affected files/systems | Risk | Notes |
| --- | --- | ---: | --- |
| Add step logging and bounded child-process handling to vNext build script. | `tools/build-vnext.ps1` | Medium | This file was not on Claude's strict dirty-tools hands-off list, but inspect current status first. |
| Add a skip/regenerate switch for expensive runtime sprite generation. | `tools/build-vnext.ps1`, possibly generator invocation only | Medium | Do not change sprite generation logic in this phase. |
| Add a non-mutating canvas mismatch report. | New tool preferred, for example `tools/report_runtime_canvas_mismatches.py` | Low | Avoid editing dirty `SpriteRuntimeCoverageTests.cs` while another lane may own it. |
| Diagnose the 456 mixed-canvas required sequences. | Report only at first | Low | Do not rewrite sprites until the report explains causes. |
| Fix vNext Settings Save automation/probe mismatch. | `tools/probe-vnext-actions.ps1`, `ToolPopupWindow.xaml`, `ToolPopupWindow.xaml.cs` | Low-medium | Prefer fixing automation IDs or probe invocation, not UI behavior, unless UI is genuinely broken. |
| Re-run direct vNext tests. | `vnext/Wevito.VNext.sln` | Low | Current known result is 47 passed, 1 failed. |

Validation plan:

| Check | Expected |
| --- | --- |
| Build script dry path | Logs each step and cannot leave orphan Python/dotnet child processes on timeout. |
| Direct tests | `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore` passes or fails only on explicitly deferred sprite asset mismatch. |
| UI probe | `tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500` reaches summary and reports pass/fail clearly. |
| Canvas report | Exact affected sequence directories and frame sizes are listed in a JSON/markdown artifact. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| Build script logging/switches | Revert only `tools/build-vnext.ps1`. |
| New canvas report tool | Delete the new tool and any generated report artifacts. |
| Probe/UI automation fix | Revert only touched probe/XAML files. |

Risk boundary:

Do not normalize or rewrite runtime sprite PNGs in this phase. First produce a report and decide whether the mismatch is a generator bug, stale asset residue, or an intentional non-28x24 policy conflict.

Recommended status: Implement next, after Phase 0 validation.

## Phase 2 - Runtime Optional Animation Bridge For vNext

Goal: Let vNext display optional animation families that already exist and validate in Godot, without requiring new art.

Problem:

`PetAnimationState` is currently a base enum. `SpriteAssetService` derives folder names from that enum. vNext actions map `water -> Eat` and `play -> Happy`, so optional families on disk are not first-class runtime states.

Design direction:

```text
Action request
  -> semantic action id
  -> preferred animation family id
  -> fallback base PetAnimationState
  -> SpriteAssetService resolves frames
  -> visible fallback is logged if optional frames are missing
```

Concrete tasks:

| Task | Affected files/systems | Risk | Notes |
| --- | --- | ---: | --- |
| Add an animation-family concept separate from `PetAnimationState`. | `vnext/src/Wevito.VNext.Contracts/Models.cs` | Medium | Prefer string/id or value object over cramming all optional families into the enum. |
| Extend action config with preferred optional family and fallback. | `vnext/content/actions.json` or a new companion config | Medium | Avoid breaking existing save/action semantics. |
| Teach `SpriteAssetService` to resolve optional family ids. | `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs` | Medium | Fallback to current enum behavior if unavailable. |
| Add dev-tool forcing for optional families. | `DevToolModels.cs`, `ToolPopupWindow.xaml.cs`, `ShellCoordinator.cs` | Medium | This is needed for visual QA. |
| Add tests for resolution/fallback. | `vnext/tests/Wevito.VNext.Tests` | Medium | Include missing-family fallback tests. |

Validation plan:

| Check | Expected |
| --- | --- |
| Unit tests | Optional family exists -> frame path resolves to that family. Missing optional family -> deterministic fallback. |
| Dev tool | Can force `drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, `carry_ball_run`. |
| Action mapping | Water can show `drink`; ball/play actions can show relevant ball families. |
| Existing behavior | Existing base action tests still pass. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| Contract/model additions | Revert `Models.cs` and corresponding serializer tests. |
| Action config additions | Revert config or keep fields ignored by old code. |
| SpriteAssetService changes | Revert resolver branch; old base enum path remains intact. |

Risk boundary:

Do not author new optional art here. This is a runtime bridge over existing files only.

Recommended status: Plan now, implement after Phase 1 gates are healthy.

## Phase 3 - Sprite Repair And QA Vocabulary Alignment

Goal: Align Wevito's repair queue vocabulary with Hatch Pet QA terms without rewriting the repair architecture.

Claude's plan is right that the architecture already exists. The issue is vocabulary and visibility.

Concrete tasks:

| Task | Affected files/systems | Risk | Notes |
| --- | --- | ---: | --- |
| Add proposed issue kinds after coordination. | `SpriteRepairContracts.cs` | Medium | Candidates: `ChromaAdjacentResidue`, `SparseFrame`, `SizeOutlier`, `EdgeCropRisk`, `IdentityDrift`. |
| Add detectors where deterministic. | `SpriteValidator.cs` | Medium | `IdentityDrift` should probably remain manual/review-only, not an automated detector. |
| Update planner priorities. | `RepairPlanner.cs`, `RepairQueueDiscoveryService.cs` | Medium | Preserve smallest-failing-scope-first behavior. |
| Add queue status/report vocabulary. | Contracts and state store/report path | Medium | Add `awaiting_review` and `paused` concepts carefully if UI consumes statuses. |
| Add tests. | `SpriteValidatorTests.cs`, `RepairPlannerTests.cs` | Medium | Do not edit dirty `SpriteRuntimeCoverageTests.cs` unless ownership is clear. |

Validation plan:

| Check | Expected |
| --- | --- |
| Unit tests | New issue kinds are detected/planned as expected. |
| Repair queue JSON | Still readable by app/runtime consumers. |
| Manual-review guard | `IdentityDrift` never auto-applies. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| New enum members | Revert contracts and dependent tests if no persisted queue relies on them. |
| Status changes | Keep compatibility reader for old statuses or revert status model before merge. |

Risk boundary:

Do not run broad autonomous repair or Gemini generation during this phase. This is vocabulary and deterministic detection only.

Recommended status: Implement after Phase 1. Can be parallel-planned with Phase 2, but code changes overlap vNext contracts.

## Phase 4 - Habitat Grounding, Depth, And Prop Interaction Contracts

Goal: Make new polished sprites look grounded in the actual game environment.

Problem:

The audit found prop shadows and hardcoded anchors, but no clear cross-runtime system for per-pet contact shadows, habitat object zones, occlusion bands, or enter/perch/play interaction metadata.

Proposed shape:

```text
habitat visual contract
|
+-- object id
+-- screen position / depth band
+-- interaction zones
|   +-- eat
|   +-- drink
|   +-- perch
|   +-- sleep
|   +-- hide
|   +-- play
+-- occlusion layer
+-- contact shadow policy
+-- prop anchor references
```

Concrete tasks:

| Task | Affected files/systems | Risk | Notes |
| --- | --- | ---: | --- |
| Write habitat visual contract doc first. | New doc under `docs/` | Low | Use current Godot/vNext behavior as baseline. |
| Add or extend habitat config schema. | `vnext/content`, Godot config if shared | Medium-high | Plan before code. |
| Add per-pet contact shadow rendering. | vNext shell renderer, Godot only if needed | Medium | Should be deterministic and not asset-heavy. |
| Add object depth/occlusion bands. | Renderer plus config | Medium-high | Needs screenshots to prove. |
| Add forced scenarios. | Godot package scenarios and vNext probes | Medium | Validate enter/perch/drink/play placements. |

Validation plan:

| Check | Expected |
| --- | --- |
| Screenshot proof | Pets appear grounded across species/age sizes. |
| Interaction proof | Pets can approach/use habitat objects without hiding behind wrong layer. |
| Regression proof | Current prop-anchor contract still passes. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| Config additions | Keep ignored by old renderer or revert new config. |
| Renderer changes | Feature flag contact shadows/depth bands before full default-on. |

Risk boundary:

Do not combine this with visual sprite generation. The renderer should improve existing sprites first.

Recommended status: Plan after Phase 2 bridge; implement before final visual sweep.

## Phase 5 - Work-Companion Runtime States

Goal: Make Wevito react to PC/workflow events as a useful desktop companion.

Claude's state vocabulary is sound, but it should not be implemented until basic vNext gates pass and the optional-family bridge is decided.

Concrete tasks:

| Task | Affected files/systems | Risk | Notes |
| --- | --- | ---: | --- |
| Add additive semantic states. | `Models.cs` or separate state model | Medium | Consider separate semantic state rather than expanding `PetAnimationState` too far. |
| Add fallback mapping. | `SpriteAssetService.cs` | Medium | `waving/jumping -> happy`, `failed -> sad`, `waiting/review -> idle`, `running -> walk`. |
| Wire low-risk events. | `ShellCoordinator.cs`, basket/link bin/tool popup paths | Medium | Start with capture success/failure and popup waiting. |
| Add dev forcing. | Dev tools | Low-medium | Important for testing without new art. |

Validation plan:

| Check | Expected |
| --- | --- |
| Serialization | Old saves still load. |
| Fallback | New states show existing frames if no authored family exists. |
| Event probe | Link capture and invalid payload visibly choose correct semantic state. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| New state model | Revert additive fields/enum and migration code. |
| Event wiring | Remove event-to-state transitions, base pet behavior remains. |

Risk boundary:

No new state sprites in this phase. Use fallbacks only.

Recommended status: After Phase 2 or folded into it if the animation-family model naturally supports semantic states.

## Phase 6 - Webtools Slot 2 Prototype

Goal: Add a user-facing runtime tool that proves Wevito is more than a sprite viewer.

Recommendation:

Clipboard Shelf is still the best first slot because it builds on existing Link Bin and broker concepts.

Concrete tasks:

| Task | Affected files/systems | Risk | Notes |
| --- | --- | ---: | --- |
| Define clipboard shelf model. | Contracts/Core | Medium | Capacity-bounded, persisted, typed rows. |
| Add shelf service. | `vnext/src/Wevito.VNext.Core` | Medium | Mirror basket service style. |
| Add UI panel. | `ToolPopupWindow.xaml`, `.xaml.cs`, Home panel slot wiring | Medium | Do after Settings Save probe is healthy. |
| Add pet reactions. | Work-companion state hooks | Medium | Uses Phase 5 fallback states. |
| Add tests. | vNext tests | Medium | Include persistence and duplicate/invalid cases. |

Validation plan:

| Check | Expected |
| --- | --- |
| Service tests | Add, duplicate, remove, persist, restore all work. |
| UI probe | Slot 2 opens shelf and common actions invoke. |
| Broker safety | Clipboard capture does not break Link Bin. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| New shelf service/UI | Revert slot 2 wiring and service files. |
| Model additions | Add compatibility migration or revert before save schema ships. |

Recommended status: Defer until Phase 1 and Phase 2 are stable.

## Phase 7 - Bespoke Grip/Pickup Visual Authoring

Goal: Replace stable-but-generic overlay poses with truly expressive per-species ball grip/pickup/hold poses.

This is the first phase that should touch risky generation/import.

Prerequisites:

| Prerequisite | Why |
| --- | --- |
| Phase 1 contracts accepted | Prevents untracked, provenance-free generation. |
| Phase 2 helper tools behavior-validated | Ensures layout guide, manifest, record, contact sheet, and preview outputs exist. |
| Phase 1 build/test gates healthy | Prevents false visual QA. |
| Optional-family bridge planned or implemented | Ensures vNext can actually display the families being improved. |
| Contact-sheet and preview review mandatory | Prevents alpha-pass/visual-fail rows. |

Pilot:

| Pilot | Reason |
| --- | --- |
| `goose / baby / female / blue / hold_ball` | The user repeatedly saw goose issues, and goose has been most iterated. |

Validation plan:

| Check | Expected |
| --- | --- |
| Manifest | Complete and schema-valid. |
| Provenance | Source path and SHA-256 recorded. |
| Contact sheet | Visually reviewed before apply. |
| Preview | Motion reads as grip/hold/pickup, not static overlay. |
| Runtime proof | Packaged or dev runtime shows the intended result. |
| Repair fallback | Reject or repair if identity drift, box matte, holes, or slot bleed appear. |

Rollback plan:

| Change | Rollback |
| --- | --- |
| Applied runtime frames | Restore from manifest `apply_backup_dir`. |
| Accepted source | Mark job `rejected` or supersede with a new job; do not overwrite original source. |

Recommended status: Wait.

## Immediate Recommended Work Order

```text
1. Phase 0
   Verify and reconcile existing Phase 1 docs and Phase 2 helper tools.

2. Phase 1
   Fix deterministic code/test gates:
   build-vnext reliability, canvas mismatch reporting, vNext UI probe.

3. Phase 2
   Add vNext optional animation-family bridge with explicit fallbacks.

4. Phase 3
   Align repair taxonomy/status vocabulary with Hatch Pet QA terms.

5. Phase 4
   Plan and implement habitat grounding/depth contracts.

6. Phase 5-6
   Add work-companion states and Clipboard Shelf once runtime state plumbing is stable.

7. Phase 7
   Resume visual generation with a goose grip-pose pilot only after tooling proves contact sheet, preview, provenance, and rollback.
```

## Work To Avoid For Now

| Avoid | Why |
| --- | --- |
| Broad Gemini/OpenAI sprite generation | The generation manifest and proof workflow needs a stable checkpoint first. |
| Runtime PNG mass normalization | Canvas mismatch must be diagnosed before asset rewrite. |
| Editing dirty Lane A files casually | `.sprite-workflow/**`, sprite app docs, many tools, and `VNEXT_IMPLEMENTATION_CHECKLIST.md` are active coordination surfaces. |
| Changing `SpriteRuntimeCoverageTests.cs` without ownership check | Claude flagged it as dirty/active. |
| Implementing Clipboard Shelf before fixing UI probe | The probe already fails at Settings Save, so new UI would be hard to validate. |

## Concrete First Implementation Slice

If the next instruction is "start implementing," the safest first slice is:

| Step | Output |
| --- | --- |
| Inspect `tools/build-vnext.ps1` and current process-spawn behavior. | Small plan for logging/timeouts/skip-regenerate switch. |
| Add a new non-mutating canvas mismatch reporter. | JSON/markdown report listing exact mixed-canvas sequences. |
| Diagnose vNext Settings Save probe failure. | Either an automation-id/probe fix or a precise bug report. |
| Run direct tests and the probe again. | Updated pass/fail evidence. |

This slice intentionally avoids visual generation and avoids modifying the known dirty sprite-import/generation tools.

## Missing Information Before Broad Implementation

| Question | Why it matters |
| --- | --- |
| Who owns the already-present Phase 1 docs and Phase 2 helper tools? | They are untracked. They may have been created by another lane and should be committed/reviewed as a coherent group. |
| Should vNext remain the primary runtime or should Godot remain the primary proof surface for visual features? | Claude says vNext is center of gravity, but Codex audit found Godot currently has stronger optional-family support. |
| Are non-28x24 runtime frames intentional anywhere after the user's "do not box-fit" feedback? | The current QA docs say 28x24 exactly, but the user explicitly warned against cramped box-fitting. This needs a final policy distinction between source-generation freedom and runtime-frame contract. |
| Should optional-family states be a new string family id or enum values? | A string/id model is more flexible, but an enum is simpler and safer for current tests. |

## Final Recommendation

Proceed with deterministic reliability work first, not new visuals. The project is closest to being unblocked if we make the build/test/probe gates honest, then give vNext a clean optional-animation bridge. Visual generation should resume only after the manifest, contact sheet, preview, and rollback tools are behavior-validated.

