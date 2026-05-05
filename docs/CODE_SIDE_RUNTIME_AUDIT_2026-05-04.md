# Code Side Runtime Audit - 2026-05-04

Scope: Wevito code/runtime readiness for upcoming visual, animation, and Hatch Pet-inspired workflow work.

No broad implementation changes were made. This audit ran deterministic checks and produced report/artifact files only. The Claude Hatch Pet plan doc was not present at the time of review.

## Runtime Shape

```text
╔══════════════════════════════════════════════════════════════════════════════╗
║ Wevito readiness for visual/animation work                                  ║
╠══════════════════════╦══════════════════════════════════════╦═══════════════╣
║ Godot/package lane   ║ Optional art, prop anchors, ghost and ║ Mostly ready  ║
║                      ║ body-condition scenarios validate     ║               ║
╠══════════════════════╬══════════════════════════════════════╬═══════════════╣
║ vNext shell lane     ║ Base care controls exist, but optional║ Partial       ║
║                      ║ animation families are not first-class║               ║
╠══════════════════════╬══════════════════════════════════════╬═══════════════╣
║ Asset pipeline lane  ║ Audits pass, but build script hangs   ║ Blocked gate  ║
║                      ║ and tests find canvas jitter          ║               ║
╠══════════════════════╬══════════════════════════════════════╬═══════════════╣
║ Workflow plan lane   ║ Hatch Pet handoff exists; Claude plan ║ Waiting       ║
║                      ║ not yet present                       ║               ║
╚══════════════════════╩══════════════════════════════════════╩═══════════════╝
```

## Documents Reviewed

| Document | Status | Notes |
|---|---:|---|
| `docs/VNEXT_IMPLEMENTATION_CHECKLIST.md` | Read | Latest coordination says `hold_ball` and `pickup_ball` were reseeded and package sweeps passed. Optional lane is no longer blocked, but bespoke grip/pickup poses remain a future quality gate. |
| `docs/HATCH_PET_WEVITO_HANDOFF_2026-05-04.md` | Read | Hatch Pet is useful as asset-discipline inspiration, not as a runtime virtual-pet implementation. Main value: manifests, provenance, canonical references, contact sheets, preview videos, targeted repair queues, and strict artifact rules. |
| `docs/CLAUDE_HATCH_PET_WEVITO_PLAN_2026-05-04.md` | Missing | Architectural/pipeline conventions should wait for this plan before broad implementation. |
| `vnext/artifacts/workflow-runs/20260504-optional-family-visual-review-summary.md` | Read | Optional families are complete and validated, but `hold_ball` and `pickup_ball` currently use safe reseeded poses rather than bespoke species grip/pickup poses. |

## Commands And Results

| Command | Result | Key output |
|---|---:|---|
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug` | Failed by timeout | Timed out after about 15 minutes. A child `python.exe` was still running from the timed-out build and was stopped. This likely occurred during deterministic runtime sprite generation before normal .NET test/publish completion. |
| `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore` | Failed | 47 passed, 1 failed. Failure: `SpriteRuntimeCoverageTests.RuntimeSpriteFrames_AreCanonicalTransparentPngs`, with mixed frame heights in runtime animation sequences. |
| `python tools\audit_optional_animation_readiness.py --output ... --markdown ...` | Passed | `2520 / 2520` optional targets complete, `0` fallback-only, `0` invalid optional art, `0` errors. |
| `python tools\audit_sprite_contract.py --output ...` | Passed | 30 source boards, 17 supporting inputs, 360 runtime variant dirs, 23,040 runtime frames, `0` errors. Note: the report still labels 10,800 as expected base frames, so wording is stale or ambiguous now that optional frames exist. |
| `python tools\audit_structural_sprite_quality.py` | Passed | 60 rows scanned, 0 active findings, 0 age-size findings. Artifact: `vnext/artifacts/structural-sprite-audit-20260504-160745.md`. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\smoke-test.ps1 -ExePath .\builds\release\WevitoDesktopPet-v20260504-pickup-ball-reseed-win64.exe -RunSeconds 30` | Passed | Packaged Godot smoke passed for fresh and legacy scenarios. |
| Packaged scenario `WEVITO_AUTOMATION_SCENARIO=render_effects` | Passed | 8 checks passed: healthy, obesity, malnutrition, ghost tint, ghost props, death conversion, and new living slot behavior. |
| Packaged scenario `WEVITO_AUTOMATION_SCENARIO=prop_anchor_contract` | Passed | 360 rows, 2160 ball placements, 360 drink anchor checks, 360 drink prop checks, 0 failures. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500` | Failed | The vNext UI probe ran action sessions, then failed at Settings Save: `Failed to invoke save button.` No summary artifact was produced. |

## Systems Reviewed

| Area | Files/systems inspected | Readiness |
|---|---|---|
| Godot runtime | `scripts/pet.gd`, `scripts/main_scene.gd`, package automation scenarios | Stronger support for optional families, ball overlays, water props, body condition, ghost tint, and automation scenarios. |
| vNext contracts/core | `vnext/src/Wevito.VNext.Contracts/Models.cs`, `vnext/src/Wevito.VNext.Core/PetSimulationEngine.cs`, `vnext/content/actions.json` | Base actions only. Optional animation families are not represented as first-class runtime states. |
| vNext shell/rendering | `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs`, `HomePanelWindow.xaml.cs`, `ToolPopupWindow.xaml.cs`, `ShellCoordinator.cs` | Basic shell is functional, but optional animation forcing, carried prop rendering, and dev scenarios are incomplete. |
| Asset loading | `sprites_runtime`, `sprites_authored_verified`, `sprites_shared_runtime`, `vnext/content/optional_animation_families.json`, `sprites_runtime/_metadata/prop_anchors.json` | Naming/path contracts are clear, but canvas consistency currently fails unit tests. |
| Repair/workflow code | `SpriteRepairContracts.cs`, scanner/repair classes, automation runner | Local deterministic repair exists. Queue status model does not match the desired `awaiting_review` / `paused` workflow vocabulary. |
| Build/release tooling | `tools/build-vnext.ps1`, audit scripts, smoke scripts, package scenarios | Godot package validation works. vNext build script is not reliable in the current state. |

## Game Controls Reviewed

| Surface | What works | Concern |
|---|---|---|
| Packaged Godot desktop pet | Launches, smoke-tests fresh and migrated saves, validates render effects, validates prop anchors. | This lane is the best current proof surface for optional animation work. |
| Godot action flow | `pet.gd` and `main_scene.gd` support optional families such as `drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, and `carry_ball_run`. | Bespoke grip/pickup pose quality is intentionally not solved yet. |
| vNext care buttons | Feed, water, rest, play, groom, bath, medicine, doctor, and home are wired through `actions.json` and `PetSimulationEngine`. | Water maps to `Eat`; play maps to `Happy`; no `drink` or ball-family state is triggered. |
| vNext action options | Habitat loadout resolver can recommend context items and group options. | Universal ball/fetch flow is not represented the way Godot represents it. |
| vNext basket/popup/settings/dev tools | Popup windows and dev commands exist. | UI probe failed invoking Settings Save. Dev animation picker is enum-based and cannot force optional animation families. |
| Debug/dev scenarios | Godot has deterministic automation scenarios. | vNext lacks equivalent forced optional-family scenarios. |

## Issues Found By Severity

### P0 - Current Build/Test Gates

| Issue | Evidence | Impact | Recommendation |
|---|---|---|---|
| vNext build script can hang | `tools/build-vnext.ps1 -Configuration Debug` timed out after about 15 minutes and left a Python child process. | vNext cannot be treated as a reliable release/build gate until this is bounded and observable. | Add progress logging, step timeouts, cache/rebuild switches, and a narrow deterministic sprite-generation test before invoking full publish. |
| vNext unit tests fail on runtime canvas mismatch | `SpriteRuntimeCoverageTests.RuntimeSpriteFrames_AreCanonicalTransparentPngs` failed. A scan found 456 required base-animation sequences with mixed frame canvases, heavily affecting snake rows. | Mixed frame canvas sizes can cause visible jitter, cropped previews, and false confidence in visual QA. | Normalize per-sequence canvases or change the generation/import contract deliberately. Do this before broad new visual imports. |

### P1 - Runtime Feature Readiness

| Issue | Evidence | Impact | Recommendation |
|---|---|---|---|
| vNext does not expose optional animation families | `PetAnimationState` only includes `Idle`, `Walk`, `Eat`, `Happy`, `Sad`, `Sleep`, `Sick`, `Bathe`. `SpriteAssetService` derives animation folder from that enum. | `drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, and carry animations can exist on disk but cannot be naturally driven by vNext controls. | After Claude's plan, add an optional-family runtime layer rather than overloading the base enum. |
| vNext action semantics are stale for current visual goals | `actions.json` maps water to `eat` and play to `happy`. | UI suggests care/play behaviors, but the sprite family shown will not match the richer asset set. | Introduce action-to-animation-family contracts that can select base, optional, prop, and fallback families. |
| vNext UI probe fails at settings save | `probe-vnext-actions.ps1` failed: `Failed to invoke save button.` | Automated control validation cannot prove settings/popup behavior end-to-end. | Inspect automation IDs/control invocation path and update either the UI contract or the probe. |
| Repair queue status vocabulary is too coarse | Repair statuses are `Pending`, `Running`, `Escalated`. | Does not cleanly support user-facing states like `awaiting_review` and `paused with reason`. | Plan status model update with migration/report compatibility. |

### P2 - Visual Integration Gaps

| Issue | Evidence | Impact | Recommendation |
|---|---|---|---|
| Universal ball/fetch is split between lanes | Godot supports ball hold/throw concepts. vNext has generic toy items and no ball action family. | Future art may validate in Godot but remain invisible or confusing in vNext. | Choose one cross-runtime ball action contract. |
| Habitat object zones and occlusion are partial | vNext props use hardcoded/default anchors and dynamic stage props. No manifest-level enter/perch/occlusion zones were found. | New habitat art can look flat or fail to ground pets visually. | Add data-driven object zones, depth bands, per-object occlusion rules, and pet shadow/contact points. |
| Per-pet visual grounding is limited | Stage props have shadow/backdrop support, but no clear per-pet ground/contact shadow system was found in vNext. | Pets can appear pasted onto the scene, especially as sizes vary by age/species. | Add per-pet contact shadows and depth sorting before final visual polish. |
| Optional contract script wording is stale | Sprite contract reports `runtime_frames_found 23040 / expected 10800` but still `error_count 0`. | Audits are correct enough to pass but confusing for coordination. | Update report labels to distinguish required base frames from optional frames. |
| Trace noise is high in vNext probe | Repeated `stage-props` trace lines dominate probe output. | Makes real UI/control failures harder to spot. | Gate repeated trace spam behind verbose mode or summarize per session. |

## Visual And Animation Readiness

| Requirement | Current state | Ready? |
|---|---|---:|
| Apply new runtime sprites | Use `sprites_runtime/<species>/<age>/<gender>/<color>/<family>_%02d.png`. | Yes, with canvas mismatch caveat. |
| Apply optional families | Godot: yes. vNext: no first-class runtime state. | Partial |
| Prop anchors | `sprites_runtime/_metadata/prop_anchors.json` validates in packaged Godot. | Yes for Godot |
| Body condition rendering | Godot scenario validates obesity, malnutrition, healthy scale/alpha. | Yes for Godot |
| Ghost rendering | Godot scenario validates ghost tint, prop hiding, death conversion, replacement slot. | Yes for Godot |
| Forced dev-build scenarios | Godot has strong scenarios. vNext has dev tools but no optional-family forced scenario. | Partial |
| Habitat object zones/occlusion | Hardcoded and partial. | Not ready |
| Hatch Pet-inspired manifests/provenance | Handoff doc exists. Concrete schema/implementation should wait for Claude. | Waiting |

## Where New Visuals Should Land

```text
sprites_runtime/
├─ <species>/<age>/<gender>/<color>/
│  ├─ idle_00.png
│  ├─ walk_00.png
│  ├─ eat_00.png
│  ├─ drink_00.png
│  ├─ play_ball_00.png
│  ├─ hold_ball_00.png
│  ├─ pickup_ball_00.png
│  ├─ drop_ball_00.png
│  ├─ carry_ball_walk_00.png
│  └─ carry_ball_run_00.png
├─ _metadata/
│  └─ prop_anchors.json
└─ shared/supporting items live under sprites_shared_runtime/
```

The Godot runtime already knows this shape. The vNext shell can load the same directory shape for base enum animations, but it currently lacks an animation-family field that would allow optional families to be selected cleanly.

## Safe Next Code-Side Tasks

| Task | Risk | Suggested timing |
|---|---:|---|
| Fix or isolate the `build-vnext.ps1` hang with logging, step timeouts, and a no-regenerate mode. | Medium | Now, deterministic and test-gate focused. |
| Add a targeted canvas consistency report that lists exact sequence directories and sizes before tests fail. | Low | Now. |
| Fix the vNext Settings Save automation/probe mismatch. | Low | Now. |
| Update sprite contract report wording to separate base expected frames from optional frames. | Low | Now. |
| Add a plan for vNext optional animation family support. | Medium | Wait for Claude unless only planning. |
| Add data-driven habitat zones, occlusion bands, contact shadows, and prop interaction anchors. | Medium-high | Plan now, implement after Claude alignment. |
| Expand repair queue statuses to include review/paused with reason. | Medium | Plan now, implement after workflow plan. |

## Work That Should Wait For Claude's Hatch Pet Plan

| Area | Reason to wait |
|---|---|
| New visual generation/import conventions | The Hatch-inspired manifest/provenance plan may change source-of-truth rules. |
| Crop/geometry salvage and broad sprite rewrites | Current canvas mismatch and box-boundary concerns need one coherent policy. |
| Bespoke grip, pickup, carry, and prop pose authoring | These are visual-generation decisions, not just code decisions. |
| Body segmentation, fat/skinny variants, randomized traits, and ghosts | These affect asset schema, runtime layering, and generation prompts. |
| Broad Sprite Workflow App integration | User intentionally paused risky app changes until planning is clearer. |

## Cross-Project Findings

Papilionem was not available at `C:\Users\fishe\Documents\projects\papilionem`, so no Papilionem current-code or history review could be completed. I inspected nearby local projects non-destructively for reusable ideas.

| Candidate | Source path | Commit/branch/tag | Relevance | Adaptation | Risk | Timing |
|---|---|---|---|---|---:|---|
| Manifest-first sprite pipeline | `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\PROCESS_PLAYBOOK.md` | `main`, `e2f9543` | Strong match for Hatch Pet-style source-of-truth, variant axes, frame contracts, and import discipline. | Convert ideas into Wevito-specific manifests after Claude defines schema. | Low | Planned |
| Reusable sprite playbook | `C:\Users\fishe\Documents\projects\sprite-animation-workflow\REUSABLE_SPRITE_PIPELINE_PLAYBOOK.md` | `main`, `e2f9543` | Emphasizes canonical references, focused authoring packs, direct image downloads, family-scoped imports, and immediate contact-sheet QA. | Use as checklist language, not direct code. | Low | Planned |
| Sprite Workflow App UI/data split plan | `C:\Users\fishe\Documents\projects\sprite-workflow-app\CODEX_PLAN.md` | Current workspace | Useful for future app cleanup: separate VM, data layer, automation service, compact tabs. | Defer actual app work; preserve as input to Hatch plan. | Medium | Deferred |
| Paint/animation editor design notes | `C:\Users\fishe\Documents\projects\sprite-workflow-app\CODEX_DRAWING_PLAN.md` | Current workspace | Relevant to user's desire to see real painting, large canvas, onion-skin, preview playback, brush/eraser/fill/dropper. | Use to design visible repair UI later. | Medium | Deferred |
| Layered sprite composition | `C:\Users\fishe\Documents\projects\mentees-of-the-mystical-godot\scripts\autoload\sprite_manager.gd` | `main`, `d515d61`; also initial `dedd5d6` | Shows layer ordering/cache approach for body, hair, equipment, weapon overlays. | Adapt concept for Wevito traits, fat/skinny, ghost, accessories, antlers. | Medium | Planned after Claude |
| Animation sheet metadata generation | `C:\Users\fishe\Documents\projects\mentees-of-the-mystical-godot\generate_sprite_sheets.py` | `main`, `d515d61`; also initial `dedd5d6` | Generates animation sheet layout and timing metadata. | Use idea for Wevito family manifests/contact sheets, not direct art code. | Low-medium | Planned |
| Isometric camera and grounding concepts | `C:\Users\fishe\Documents\projects\mentees-of-the-mystical-godot\scripts\autoload\camera_manager.gd` | `main`, `d515d61`; also initial `dedd5d6` | Orthographic/isometric concepts can inspire depth bands, camera framing, stage composition. | Wevito desktop pet does not need direct 3D camera copy. Use principles only. | Medium | Deferred |
| Data-like platform placement | `C:\Users\fishe\Documents\projects\mentees-of-the-mystical-godot\scenes\combat\combat_arena.gd` | `main`, `d515d61`; also initial `dedd5d6` | Platform definitions hint at data-driven object placement and depth. | Adapt as habitat zones with `x/y/width/depth/height/occlusion`. | Medium | Planned |

## Papilionem Historical 2D/Isometric Findings

| Requested source | Result |
|---|---|
| `C:\Users\fishe\Documents\projects\papilionem` | Missing. The directory does not exist locally at the requested path. |
| Papilionem branches/tags/history | Not available because the project path is missing. |
| Earlier 2D/isometric Papilionem systems | Not available for inspection. |

Substitute historical review was performed on `mentees-of-the-mystical-godot` because it is a local Godot project with sprite, layering, and isometric presentation ideas. It has no older branch/tag beyond `main`, and the initial commit already contained the relevant `sprite_manager.gd`, `camera_manager.gd`, `world_3d.gd`, and `combat_arena.gd` systems.

## Shadow, Depth, And Grounding Ideas For Wevito

```text
Habitat stage
├─ Backdrop layer
├─ Far prop layer
├─ Pet depth bands
│  ├─ pet contact shadow
│  ├─ pet sprite
│  └─ carried/held prop overlay
├─ Near prop/occluder layer
└─ UI/cue overlay
```

Recommended future direction:

| Idea | Why it helps | Risk | Timing |
|---|---|---:|---|
| Per-pet contact shadow based on species/age scale | Makes pets feel grounded and reduces pasted-on look. | Low-medium | Plan now, implement after renderer alignment. |
| Habitat object depth bands | Allows pets to walk behind/in front of habitat objects. | Medium | Plan after Claude. |
| Object interaction zones | Lets animals enter, perch, drink, eat, sleep, hide, or play at specific anchors. | Medium | Plan after Claude. |
| Prop anchor contract shared by Godot and vNext | Keeps ball, water, food, and carried props consistent across runtimes. | Medium | Planned |
| Animation family timing manifest | Prevents stiff or mismatched optional animations. | Low-medium | Planned |

## Final Judgment

Godot/package validation is in a usable state for proving visual behavior, and the latest packaged build passes the most important deterministic optional-art checks. vNext is not yet ready to receive the full upcoming optional visual/animation workload without code-side work: it cannot drive optional animation families cleanly, its UI probe currently fails, and its test suite is failing because runtime animation sequences have mixed frame canvas sizes.

The safest next move is to fix deterministic code/test gates first, then fold Claude's Hatch Pet plan into the asset manifest and optional-family runtime design before any broad visual generation/import work resumes.

## Code-Side Stabilization Addendum - 2026-05-04

Scope: deterministic code-side follow-up after the visual thread accidentally touched Phase 3/4 runtime work. No visual generation, import, crop salvage, or broad art rewrite was started.

```text
incoming accidental code
|-- Phase 3 repair taxonomy  --> keep, code-side-owned
|-- Phase 4 companion states --> keep, but harden runtime behavior
`-- broad workflow/visual UI   --> defer to visual/Claude plan
```

### Keep / Adapt / Defer Decision

| Area | Decision | Notes |
|---|---|---|
| Hatch Pet-aligned `SpriteIssueKind` taxonomy and planner tests | Keep | Conservative detector/planner vocabulary is useful code-side contract work. |
| Work-companion animation states `Waving`, `Jumping`, `Failed`, `Waiting`, `Review` | Adapt and keep | Added one-time fallback/missing sprite traces and prevented ambient waiting/review pulses from overriding active explicit animation overrides. |
| Repair automation runtime surface | Defer expansion | Existing code was not removed, but new broad automation/workflow behavior should not be added here until Claude's plan is reconciled. |
| Visual generation/import conventions | Defer | Stays owned by visual planning and future agreed manifest/provenance workflow. |

### Deterministic Fixes Applied

| File | Change |
|---|---|
| `tools/build-vnext.ps1` | Added a warning when `-SkipAssetPrep` is used without `-SkipTests`, because sprite runtime coverage expects a freshly prepared asset tree. |
| `tools/report_runtime_canvas_mismatches.py` | Added a non-mutating mixed-canvas reporter. It does not force the old fixed 72x64 box; fixed-reference checks are optional only. |
| `tools/probe-vnext-actions.ps1` | Added pinned probe scenarios, action-option invocation, robust Settings Save probing, link-bin readiness handling, and selected-row basket delete validation. |
| `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs` | Added trace-once logging for sprite fallback/missing signatures. |
| `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs` | Preserved explicit animation overrides when ambient work-companion waiting/review states are active. Removed a build warning from an async lambda with no await. |
| `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml` and `.xaml.cs` | Basket delete now enables for selected rows as well as marked rows. |

### Validation Results

| Command | Result | Evidence |
|---|---:|---|
| `python .\tools\report_runtime_canvas_mismatches.py --output ... --markdown ...` | Completed | `vnext/artifacts/runtime-canvas-contract-20260504-code-side.json`; `456` mixed-canvas sequences, `0` missing, `0` invalid. |
| `dotnet test .\vnext\Wevito.VNext.sln -c Debug --filter "FullyQualifiedName~SpriteValidatorTests|FullyQualifiedName~RepairPlannerTests|FullyQualifiedName~PetSimulationEngineTests"` | Passed | `34 / 34` focused tests passed. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 180` | Passed | Published shell at `vnext/artifacts/shell/Wevito.VNext.Shell.exe` without regenerating assets. |
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild -StartupDelayMs 2500` | Passed | `vnext/artifacts/action-probes/20260504-183950/summary.json`; all action traces, settings traces, basket traces, save trace, broker open URL, clipboard restore, and shell liveness passed. |
| `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore` | Failed, known asset gate | `SpriteRuntimeCoverageTests.RuntimeSpriteFrames_AreCanonicalTransparentPngs`: mixed sequence canvas, expected `80`, actual `81`. This is now diagnosed by the non-mutating canvas report and was not repaired in this code-side pass. |

### Current Biggest Remaining Code-Side Blocker

The remaining failing gate is asset-tree consistency, not C# logic: `sprites_runtime` has `456` base-animation sequences whose frames are not internally canvas-stable. Because sprites are allowed to exceed the old fixed box, the next repair should normalize each animation sequence to one stable canvas without shrinking/cropping the animal into an artificial 72x64 constraint.
