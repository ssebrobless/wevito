# Code Side Audit High-Reasoning Handoff - 2026-05-04

This document condenses the code/runtime audit into a planning handoff. It is intended to be pasted into a high-reasoning Codex session so the next pass can review the evidence and produce a concrete next-step plan.

## Primary Audit Output

Read this first:

`C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_RUNTIME_AUDIT_2026-05-04.md`

That report contains:

- Commands/tests run and pass/fail status.
- Code systems reviewed.
- Game controls reviewed.
- Visual/runtime behavior reviewed.
- Issues found, ordered by severity.
- Readiness gaps for upcoming visual/animation work.
- Safe next code-side tasks.
- Work that should wait for Claude's Hatch Pet plan.
- Cross-project findings.
- Papilionem availability result.
- Shadow/depth/grounding ideas for Wevito.

## Supporting Documents Reviewed

| Path | Why it mattered |
|---|---|
| `C:\Users\fishe\Documents\projects\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md` | Current coordination state, optional animation status, recent hold/pickup ball repairs, known gates. |
| `C:\Users\fishe\Documents\projects\wevito\docs\HATCH_PET_WEVITO_HANDOFF_2026-05-04.md` | Hatch Pet-inspired workflow guidance: manifests, provenance, canonical references, contact sheets, targeted repair queues, strict artifact rules. |
| `C:\Users\fishe\Documents\projects\wevito\docs\CLAUDE_HATCH_PET_WEVITO_PLAN_2026-05-04.md` | Expected but not present during audit. Broad pipeline/schema decisions should wait for this if it appears. |
| `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\workflow-runs\20260504-optional-family-visual-review-summary.md` | Confirmed optional families are complete and recently validated, with bespoke grip/pickup pose quality still deferred. |

## Important Code Areas Inspected

| Area | Relevant files |
|---|---|
| Godot runtime | `C:\Users\fishe\Documents\projects\wevito\scripts\pet.gd`, `C:\Users\fishe\Documents\projects\wevito\scripts\main_scene.gd` |
| vNext contracts | `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs` |
| vNext simulation | `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` |
| vNext shell rendering | `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs`, `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs` |
| vNext popup/dev tools | `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`, `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs` |
| vNext action config | `C:\Users\fishe\Documents\projects\wevito\vnext\content\actions.json` |
| Optional family contract | `C:\Users\fishe\Documents\projects\wevito\vnext\content\optional_animation_families.json` |
| Runtime sprite metadata | `C:\Users\fishe\Documents\projects\wevito\sprites_runtime\_metadata\prop_anchors.json` |
| Tests | `C:\Users\fishe\Documents\projects\wevito\vnext\tests\Wevito.VNext.Tests` |
| Build/release tooling | `C:\Users\fishe\Documents\projects\wevito\tools\build-vnext.ps1`, audit scripts under `tools` |

## Test And Validation Summary

```text
╔══════════════════════╦════════════════════════════════════════════╦════════╗
║ Lane                 ║ Result                                     ║ State  ║
╠══════════════════════╬════════════════════════════════════════════╬════════╣
║ Godot packaged build ║ Smoke, render effects, prop anchors pass   ║ Good   ║
║ Optional art audits  ║ 2520/2520 complete, 0 fallback-only        ║ Good   ║
║ Structural audit     ║ 60 rows, 0 active findings                 ║ Good   ║
║ vNext build script   ║ Timed out and left orphan Python process   ║ Block  ║
║ vNext unit tests     ║ 47 passed, 1 failed canvas consistency     ║ Block  ║
║ vNext UI probe       ║ Failed at Settings Save invocation         ║ Block  ║
╚══════════════════════╩════════════════════════════════════════════╩════════╝
```

## Core Findings

1. Godot/package validation is currently the strongest proof surface.

The packaged build launched and passed smoke tests, render effects, ghost/body-condition checks, and prop-anchor contract checks. This means the Godot lane is currently usable for deterministic proof of optional art behavior.

2. vNext is not fully ready for the new visual/animation workload.

The vNext model only supports base animation enum states:

`Idle`, `Walk`, `Eat`, `Happy`, `Sad`, `Sleep`, `Sick`, `Bathe`

It does not cleanly expose optional families such as:

`drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, `carry_ball_run`

3. The vNext action semantics are stale.

`actions.json` and `PetSimulationEngine` still map water to `Eat` and play to `Happy`. That means the UI can imply richer behavior while the runtime still displays old base animation families.

4. The asset pipeline has a real deterministic gate failure.

`dotnet test` fails because runtime sprite sequences have mixed canvas sizes. A scan found 456 required base-animation sequences with mixed frame canvases, especially around snake rows. This can cause visible jitter/cropping and should be fixed before broad new asset import.

5. The build script needs reliability work.

`tools\build-vnext.ps1 -Configuration Debug` timed out after roughly 15 minutes and left a Python child process running. The script needs logging, step boundaries, timeout protection, and probably a way to skip expensive deterministic sprite regeneration when not needed.

6. vNext UI automation is currently not a clean validation gate.

`tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild` launched and exercised actions, but failed at Settings Save invocation. This may be a UI automation contract mismatch rather than a manual UI failure, but it still prevents reliable control validation.

7. Habitat depth, occlusion, and pet grounding need a planned design.

Current code has stage props and prop shadows, but not a clear data-driven habitat interaction zone system, object occlusion/depth bands, or per-pet contact shadow contract. These will matter once polished sprites enter the game.

8. Papilionem could not be inspected.

`C:\Users\fishe\Documents\projects\papilionem` did not exist locally. Substitute cross-project review found reusable ideas in `sprite-animation-workflow`, `sprite-workflow-app`, and `mentees-of-the-mystical-godot`.

## Proposed Plans

### Plan A - Deterministic Code/Test Gate Cleanup

Goal: Make the codebase trustworthy before new visual import resumes.

Recommended tasks:

1. Fix or isolate the `tools\build-vnext.ps1` hang.
2. Add progress logging and step timeouts around sprite generation and publish stages.
3. Add a no-regenerate or use-existing-runtime-assets build mode.
4. Fix the failing canvas consistency test by normalizing sequence canvases or deliberately updating the contract.
5. Add a focused canvas mismatch report listing exact sequence directories and sizes.
6. Fix `tools\probe-vnext-actions.ps1` or the Settings Save automation contract.
7. Update audit wording where `runtime_frames_found 23040 / expected 10800` currently passes but reads confusingly.

Risk: Low to medium.

Do now: yes.

### Plan B - vNext Optional Animation Runtime Support

Goal: Let vNext actually use the optional animation families already validated in Godot/package audits.

Recommended tasks:

1. Add a runtime animation-family concept separate from the base `PetAnimationState` enum.
2. Update action definitions so actions can request optional families, prop overlays, fallback families, and timing.
3. Update `SpriteAssetService` so it can resolve optional family folders directly.
4. Update dev tools so optional families can be forced for visual QA.
5. Add vNext forced scenarios for `drink`, `play_ball`, `hold_ball`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, and `carry_ball_run`.
6. Keep fallbacks explicit and visible in logs/reports so missing optional art does not silently look "fine."

Risk: Medium.

Do now: plan only unless Claude's Hatch Pet plan has already landed.

### Plan C - Habitat Interaction, Depth, And Grounding

Goal: Prepare the game world to make new sprites look grounded instead of pasted onto the scene.

Recommended tasks:

1. Define habitat object zones for enter/perch/drink/eat/sleep/hide/play.
2. Add per-object anchors and occlusion/depth bands.
3. Add per-pet contact shadows based on species/age/current frame.
4. Add carried/held prop overlay ordering.
5. Add data-driven habitat contracts so art can ship with placement metadata.
6. Add validation scenarios that force pets through object interactions.

Risk: Medium to high.

Do now: plan only until Claude's Hatch Pet plan is reviewed.

### Plan D - Hatch Pet-Inspired Asset Workflow Alignment

Goal: Avoid repeating the previous blurry/broken/stale asset workflow problems.

Recommended tasks:

1. Create a manifest-first asset contract for generated/imported frames.
2. Track canonical source image, provenance, hashes, frame layout, target runtime path, and review status.
3. Require contact sheets and preview videos for every imported animation family.
4. Keep generated originals separate from verified runtime imports.
5. Record repair queues at frame scope: species, age, gender, color, family, frame_id.
6. Require strict checks for fake PNG backgrounds, holes, detached pixels, silhouette drift, frame-size jitter, and identity drift.
7. Do not let broad visual import proceed without the manifest/review trail.

Risk: Medium.

Do now: wait for Claude's Hatch Pet plan, then merge this with that plan.

## Cross-Project Planning Inputs

| Project | Path | Useful idea |
|---|---|---|
| Sprite animation workflow | `C:\Users\fishe\Documents\projects\sprite-animation-workflow` | Manifest discipline, source-of-truth rules, focused authoring packs, contact-sheet QA, family-scoped imports. |
| Sprite workflow app | `C:\Users\fishe\Documents\projects\sprite-workflow-app` | UI/automation redesign ideas, visible painting/editor concepts, compact workspace, canvas/preview layout. |
| Mentees of the Mystical Godot | `C:\Users\fishe\Documents\projects\mentees-of-the-mystical-godot` | Layered sprite composition, animation sheet metadata, isometric camera/depth concepts, platform/object placement ideas. |
| Papilionem | `C:\Users\fishe\Documents\projects\papilionem` | Requested priority source, but path was missing during audit. |

## Copy-Paste Prompt For High-Reasoning Codex

```text
You are reviewing Wevito with very high reasoning. Please review the code/runtime audit and produce a concrete next-step implementation plan.

Project root:
C:\Users\fishe\Documents\projects\wevito

Primary audit report:
C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_RUNTIME_AUDIT_2026-05-04.md

Handoff summary:
C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_AUDIT_HIGH_REASONING_HANDOFF_2026-05-04.md

Important coordination docs:
C:\Users\fishe\Documents\projects\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md
C:\Users\fishe\Documents\projects\wevito\docs\HATCH_PET_WEVITO_HANDOFF_2026-05-04.md
C:\Users\fishe\Documents\projects\wevito\docs\CLAUDE_HATCH_PET_WEVITO_PLAN_2026-05-04.md
C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\workflow-runs\20260504-optional-family-visual-review-summary.md

Note: The Claude Hatch Pet plan doc may or may not exist. If it exists, read it before proposing architecture or asset-pipeline changes. Do not rewrite or delete the Hatch Pet handoff doc.

Current audit findings to review:
1. Godot/package validation is mostly healthy. Packaged smoke, render effects, ghost/body-condition checks, optional animation readiness, structural sprite audit, and prop-anchor contract passed.
2. vNext is not fully ready for the upcoming visual/animation workload. It only exposes base animation enum states and cannot cleanly drive optional families such as drink, play_ball, hold_ball, pickup_ball, drop_ball, carry_ball_walk, and carry_ball_run.
3. vNext action semantics are stale. Water maps to Eat, play maps to Happy, and universal ball/fetch behavior is not represented cleanly.
4. tools\build-vnext.ps1 timed out after about 15 minutes and left an orphan Python process. Build/release gating needs reliability work.
5. dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore failed: 47 passed, 1 failed. The failing test was SpriteRuntimeCoverageTests.RuntimeSpriteFrames_AreCanonicalTransparentPngs due to mixed runtime sprite canvas sizes. A scan found 456 required base-animation sequences with mixed canvases, especially snake rows.
6. tools\probe-vnext-actions.ps1 -Configuration Debug -SkipBuild failed at Settings Save invocation, so vNext UI automation is not currently a reliable validation gate.
7. Habitat object zones, occlusion/depth bands, per-pet contact shadows, and shared prop/interaction contracts need a plan before final visual implementation.
8. Papilionem was not available at C:\Users\fishe\Documents\projects\papilionem. Substitute reusable ideas were found in sprite-animation-workflow, sprite-workflow-app, and mentees-of-the-mystical-godot.

Please produce a concrete next-step plan with:
- Recommended sequencing.
- Which work can be implemented immediately.
- Which work should wait for Claude's Hatch Pet plan.
- Affected files/systems.
- Risk level for each phase.
- Validation plan for each phase.
- Rollback plan.
- Any missing information you need before implementation.

Important constraints:
- Do not implement broad changes yet.
- Do not start risky Gemini/OpenAI visual generation or broad asset import.
- Do not create new visual generation/import conventions until the Hatch Pet-inspired plan is reconciled.
- Low-risk deterministic code/test/tooling fixes may be proposed as first implementation candidates.
- Preserve current Wevito style, sprite paths, and runtime asset contracts unless the plan explicitly explains a safer replacement.
```
