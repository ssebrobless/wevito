# Wevito Code-Side Deep Review And Agent Tools Plan

Date: 2026-05-05

Scope: non-mutating code/runtime review, pet functionality review, validation pass, OpenClaw-inspired helper/tool research, The-Workinator local project inspection, and AI-agent planning. No sprite generation, sprite import, runtime PNG rewrite, or broad implementation was performed.

## Executive Shape

```text
╔══════════════════════════════════════════════════════════════════════════════╗
║ Current Wevito Code-Side Reality                                           ║
╠════════════════╦════════════════════════════════════╦═══════════════════════╣
║ Godot runtime  ║ best current pet/visual proof app  ║ usable, needs polish  ║
║ vNext core     ║ strongest personality simulation   ║ good foundation       ║
║ vNext shell    ║ desktop helper + pet surface       ║ missing optional art   ║
║ Asset tree     ║ optional families structurally ok  ║ main repo base gate no ║
║ Helper tools   ║ basket/dev/settings/tool surface   ║ ready for expansion    ║
║ Agent concept  ║ promising if permissioned/visible  ║ design before build    ║
╚════════════════╩════════════════════════════════════╩═══════════════════════╝
```

Bottom line: Wevito can keep moving, but the next code-side work should not be visual generation. The safer sequence is to first reconcile the main-repo asset validation gate, then unify action/animation/prop contracts across Godot and vNext, then build a small permissioned helper-tool foundation that pets can visibly "carry" as task cards.

## Files And Docs Reviewed

Primary Wevito docs:

- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_VISUAL_HANDOFF_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_SPRITE_CLEANUP_PROGRESS_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_SPRITE_NOISE_CLEANUP_COURSE_OF_ACTION_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\CODE_SIDE_RUNTIME_AUDIT_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\CLAUDE_HATCH_PET_WEVITO_PLAN_2026-05-04.md`
- `C:\Users\fishe\Documents\projects\wevito\docs\HATCH_PET_WEVITO_HANDOFF_2026-05-04.md`

Godot/runtime code:

- `C:\Users\fishe\Documents\projects\wevito\scripts\pet_data.gd`
- `C:\Users\fishe\Documents\projects\wevito\scripts\pet.gd`
- `C:\Users\fishe\Documents\projects\wevito\scripts\game_manager.gd`
- `C:\Users\fishe\Documents\projects\wevito\scripts\main_scene.gd`
- `C:\Users\fishe\Documents\projects\wevito\sprites_runtime\_metadata\prop_anchors.json`
- `C:\Users\fishe\Documents\projects\wevito\sprites_shared_runtime\items\toys_a\ball.png`

vNext code/content:

- `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs`
- `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`
- `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs`
- `C:\Users\fishe\Documents\projects\wevito\vnext\src\Wevito.VNext.Shell\HabitatLoadoutResolver.cs`
- `C:\Users\fishe\Documents\projects\wevito\vnext\content\actions.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\content\optional_animation_families.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\content\species.json`

Local cross-project sources:

- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\README.md`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\docs\concrete-plan.md`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\docs\current-audit-and-roadmap.md`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\server.py`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\app.js`

External OpenClaw references:

- `https://github.com/openclaw/openclaw`
- `https://docs.openclaw.ai/tools`
- `https://docs.openclaw.ai/tools/skills`
- `https://docs.openclaw.ai/security`
- `https://docs.openclaw.ai/architecture`

Blocked source:

- `C:\Users\fishe\Documents\projects\papilionem` was not present on disk during this pass, so Papilionem ML reuse could not be reviewed yet.

## Validation Results

```text
╔══════════════════════════════════════════════╦══════════╦════════════════════╗
║ Check                                        ║ Result   ║ Notes              ║
╠══════════════════════════════════════════════╬══════════╬════════════════════╣
║ vNext focused tests                          ║ PASS     ║ 41 / 41            ║
║ vNext build script, SkipAssetPrep/SkipTests  ║ PASS     ║ Debug publish ok   ║
║ Optional animation readiness                 ║ PASS     ║ 2520 / 2520        ║
║ Sprite contract audit                        ║ PASS     ║ no contract errors ║
║ Full vNext tests in main repo                ║ FAIL     ║ 53 pass, 1 fail    ║
║ Runtime canvas reporter in main repo         ║ WARN     ║ 456 mixed rows     ║
╚══════════════════════════════════════════════╩══════════╩════════════════════╝
```

Commands run:

- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests -StepTimeoutSeconds 180`
- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore --filter "FullyQualifiedName~PetSimulationEngineTests|FullyQualifiedName~ContentCoverageTests|FullyQualifiedName~BasketServiceTests|FullyQualifiedName~ModeReducerTests|FullyQualifiedName~RepairPlannerTests|FullyQualifiedName~SpriteValidatorTests"`
- `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\code-side-deep-review-runtime-canvas-20260505.json --markdown .\vnext\artifacts\code-side-deep-review-runtime-canvas-20260505.md`
- `python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\code-side-deep-review-optional-readiness-20260505.json --markdown .\vnext\artifacts\code-side-deep-review-optional-readiness-20260505.md`
- `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\code-side-deep-review-sprite-contract-20260505.json`

Generated/read validation artifacts:

- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\code-side-deep-review-runtime-canvas-20260505.md`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\code-side-deep-review-runtime-canvas-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\code-side-deep-review-optional-readiness-20260505.md`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\code-side-deep-review-optional-readiness-20260505.json`
- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\code-side-deep-review-sprite-contract-20260505.json`

Important caveat: previous coordination docs said the separate code-side worktree had a green 0-mismatch canvas state. The main repo path reviewed here currently reports 456 mixed-canvas base rows. That does not contradict the earlier worktree result; it means the green worktree state and the main repo state must be reconciled before declaring the main asset gate green.

## Current Pet Functionality

```text
╔══════════════════════════════════════════════════════════════════╗
║ Pet Functionality Shape                                        ║
╠════════════════════╦════════════════════════╦═══════════════════╣
║ Need/stat decay    ║ Godot + vNext          ║ working           ║
║ Personality seeds  ║ Godot + vNext          ║ working           ║
║ Personality drift  ║ strongest in vNext     ║ promising         ║
║ Interactions       ║ Godot game flow        ║ broad but uneven  ║
║ Optional visuals   ║ strongest in Godot     ║ usable            ║
║ Helper UI/tools    ║ strongest in vNext     ║ promising         ║
╚════════════════════╩════════════════════════╩═══════════════════╝
```

Godot findings:

- `pet_data.gd` already has a meaningful pet model: hunger, hydration, happiness, energy, health, cleanliness, affection, grooming, fitness, age, lifecycle, conditions, personality traits, movement state, water bowl state, and animation state.
- `game_manager.gd` supports a broad interaction set: feeding, hydration, petting, cuddling, talking, play, fetch, workout, rest, grooming, bathing, medicine, doctor-like care, condition checks, aging, sickness, death, and habitat interactions.
- `pet.gd` supports optional animation families and separate carried prop overlays. This is currently the strongest runtime contract for `hold_ball`, `play_ball`, `drink`, `pickup_ball`, `drop_ball`, `carry_ball_walk`, and `carry_ball_run`.
- Horizontal movement facing is implemented in `pet.gd`, so pets should face the direction they move in normal motion.
- Ball and water bowl props are overlays, not baked into pet PNGs. This matches the accepted goose hold-ball pilot contract.
- `STAGE_SCALE_MULTIPLIERS` are all `1.0`, so age-size differences currently rely almost entirely on sprite art rather than runtime scale policy.
- Auto-drink in `game_manager.gd` updates hydration and bowl water when hydration is low, but it does not obviously force a visible drink animation. That can make a working stat interaction feel invisible.
- Fetch in Godot works as a single action flow with a thrown marker and best-available animation, but it is not yet a proper staged pipeline of pickup -> hold -> carry -> drop.

vNext findings:

- `PetSimulationEngine.cs` is the strongest personality/habit/condition system currently in the project. It seeds traits deterministically, updates habits, drifts personality, applies aging, conditions, lifecycle outcomes, movement, and derived presentation.
- vNext action tests are healthy in focused coverage, and the simulation is a good place to formalize long-term pet identity and behavior.
- vNext currently maps actions through base animation states. `actions.json` maps water to `eat` and play to `happy`, while optional animation families are listed separately in `optional_animation_families.json`.
- `PetAnimationState` does not include optional families. This is intentional enough for base behavior, but it prevents vNext from cleanly forcing or previewing `drink`, `hold_ball`, `pickup_ball`, `drop_ball`, or carry animations.
- `SpriteAssetService.cs` can load animation folders by state name and has fallback behavior for work-companion states, but not a first-class optional family selector.
- vNext shell does not yet have a carried prop overlay equivalent to Godot's ball/water-bowl overlay.
- `HabitatLoadoutResolver.cs` hardcodes a lot of dynamic stage prop positions. It is workable for presentation, but not yet the manifest-driven habitat-zone/depth/occlusion system we want.

## Issues Ordered By Severity

### P0: Main repo runtime canvas gate is not green

The main repo still has 456 mixed-canvas base animation rows. The full vNext test suite fails in `SpriteRuntimeCoverageTests.RuntimeSpriteFrames_AreCanonicalTransparentPngs` with expected 80 / actual 81. This is the top code-side reliability blocker because it means validation status differs between the separate code-side worktree and the shared main repo.

Affected artifact:

- `C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\code-side-deep-review-runtime-canvas-20260505.md`

Recommended treatment: reconcile the green worktree canvas normalization with the main repo or repeat the approved deterministic normalization path in main after protecting visual-side cleanup changes.

### P0: Dirty workspace is too broad for casual commit/apply

The main repo has a very large dirty/staged/untracked state, including `.codex-cache`, `.vs`, `.sprite-workflow`, many recovery artifacts, runtime sprites, docs, and generated outputs. This is not inherently bad, but it makes broad commits risky.

Recommended treatment: before major code implementation, create a narrow change plan and stage only reviewed files. Do not clean/revert unrelated work.

### P1: vNext cannot yet express the optional visual contract

Godot can drive optional animation names and prop overlays. vNext cannot yet cleanly select optional animation families or render carried props. That means vNext is not ready to be the first proof surface for future optional art unless we add a separate action/animation-family contract.

Recommended treatment: add an explicit `AnimationFamily` or `ActionVisualIntent` layer rather than overloading `PetAnimationState`.

### P1: Fetch/carry/hold is not a true sequence yet

Wevito has art and runtime support for pickup/hold/carry/drop families, but the behavior is still closer to "play an action animation" than "perform a staged physical interaction."

Recommended treatment: implement an action sequence controller:

```text
throw ball
  -> pet notices target
  -> move toward ball
  -> pickup_ball
  -> hold_ball idle beat
  -> carry_ball_walk/run toward user/home
  -> drop_ball
  -> happy/reward beat
```

### P1: Auto-care can be statistically correct but visually invisible

Auto-drink currently appears to happen in stats without necessarily showing the drink animation. Similar hidden state changes will hurt the "I can see my pet living" feel.

Recommended treatment: route automatic care actions through the same visible action-family system as manual actions whenever possible.

### P1: Habitat zones/depth/occlusion are not centralized

Godot has habitat interaction constants and vNext has hardcoded stage props. The project needs one manifest-style contract for object zones, anchors, depth bands, occlusion, contact shadows, and per-species usable interactions.

Recommended treatment: create a habitat interaction manifest and consume it from both proof/runtime surfaces.

### P2: Sprite contract audit output is confusing

`audit_sprite_contract.py` reports `runtime_frames_found 23040` while `runtime_frames_expected 10800` and still has `error_count 0`. This is probably because optional frames are included in the found count while expected covers base frames. The behavior may be correct, but the wording is stale.

Recommended treatment: split base-frame and optional-frame counts in the report.

### P2: Runtime size policy is not explicit enough

The user repeatedly noticed animals shrinking/growing or being visually boxed. Code currently relies heavily on authored art geometry. vNext has species-level pet scale; Godot stage multipliers are neutral.

Recommended treatment: define a size policy:

- sprite art may exceed the original generation box if motion needs it
- runtime sequence canvases must be internally consistent
- animal scale should be species/stage-driven, not frame-by-frame crop-driven
- age size differences should be visual-policy driven and testable

## Readiness For Upcoming Visual Work

```text
╔═════════════════════════════╦══════════════╦════════════════════════════════╗
║ Area                        ║ Status       ║ Next Needed                    ║
╠═════════════════════════════╬══════════════╬════════════════════════════════╣
║ Goose hold-ball pilot       ║ accepted     ║ protect from cleanup           ║
║ Optional family coverage    ║ structurally ║ visual QA, then apply policy   ║
║ Main base canvas validation ║ not green    ║ reconcile/fix 456 rows         ║
║ Godot proof surface         ║ ready-ish    ║ staged action proof            ║
║ vNext proof surface         ║ incomplete   ║ optional/prop renderer bridge  ║
║ Habitat object system       ║ partial      ║ manifest zones/depth/shadows   ║
║ Pet agent helper concept    ║ not started  ║ tool registry + task cards     ║
╚═════════════════════════════╩══════════════╩════════════════════════════════╝
```

Visual-side cleanup can continue if it remains careful and avoids broad automatic mutations, but code-side should not claim the full project is clean until the main repo canvas mismatch is resolved.

## OpenClaw-Inspired Ideas For Wevito

OpenClaw is valuable here less because Wevito should become OpenClaw, and more because it shows a pattern for making local AI actions structured, visible, permissioned, and auditable.

Reusable concepts:

- Gateway/control-plane shape: one local broker owns tasks, tools, events, logs, and status.
- Tool registry: tools are typed functions with names, inputs, outputs, permissions, and logs.
- Skills/workflows: markdown or JSON task packs teach the assistant when and how to use tools.
- Per-agent/tool allowlists: different pet personalities could have different allowed helper capabilities.
- Session/task history: every helper action should produce a visible record.
- Security posture: local-first does not mean safe by default; tools need allow/deny lists, approvals, and a small access surface.

Wevito adaptation:

```text
╔════════════════════════════════════════════════════════════════╗
║ Wevito Helper/Agent Stack                                    ║
╠════════════════════╦═══════════════════════════════════════════╣
║ Pet personality    ║ chooses vibe, pace, reactions, prompts   ║
║ Task card          ║ visible user-approved work item          ║
║ Tool broker        ║ deterministic local actions only         ║
║ Tool registry      ║ screenshot, clipboard, notes, links      ║
║ Audit log          ║ what happened, when, why, result         ║
║ Permission layer   ║ ask before risky actions                 ║
╚════════════════════╩═══════════════════════════════════════════╝
```

Do not start with autonomous web/email/filesystem agents. Start with low-risk local helper actions that are useful and visible:

- save a copied link into the basket
- turn clipboard text into a task note
- capture a screenshot with a pet/task label
- remind the user to review an item
- open a local doc or checklist
- summarize the current Wevito task state
- prepare a visual QA packet from existing files

Only after that works should we consider higher-agency flows.

## The-Workinator Reuse Findings

The-Workinator has several concrete ideas that fit Wevito's helper-tool direction.

Source paths:

- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\README.md`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\docs\concrete-plan.md`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\docs\current-audit-and-roadmap.md`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\server.py`
- `C:\Users\fishe\Documents\projects\work-folder\The-Workinator\app.js`

Reusable systems:

- Task intake: paste/open a task, classify it, extract requirements, produce a checklist.
- Task folders: create structured local task folders with metadata and artifacts.
- Capture tools: screenshot capture, recording support, crop/trim workflows.
- Knowledge base: save reusable workflow packets and references locally.
- QA review: final readiness screen that checks missing artifacts, missing steps, contradiction risk, and submission readiness.
- Workflow library: saved packets with tags, favorites, archive state, and search.
- Manual-control boundary: supports the user without secretly submitting/acting on third-party platforms.

Best Wevito adaptation:

```text
The-Workinator idea       -> Wevito version
────────────────────────────────────────────────────────────────
Task intake               -> pet task card / helper request
Capture studio            -> pet-assisted screenshot + proof capture
Workflow packet           -> reusable pet skill/workflow
Knowledge base            -> local helper memory + visual QA history
QA review                 -> pet brings "review needed" state
Manual-control boundary   -> pet asks before risky/external actions
```

Implementation risk: low to medium if we port concepts, high if we directly merge/copy the app. Recommendation is to adapt the product shape, not copy code.

## Sprite Animation Workflow Review

Newly reviewed source:

- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\README.md`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\REUSABLE_SPRITE_PIPELINE_PLAYBOOK.md`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\README.md`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\PROCESS_PLAYBOOK.md`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\PRESET_GUIDE.md`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\FUTURE_SPRITE_ANIMATION_GUIDELINE.md`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\tools\bootstrap_sprite_pipeline.py`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\templates\incoming_sprite_manifest.template.json`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\templates\motion_families.template.json`
- `C:\Users\fishe\Documents\projects\sprite-animation-workflow\SPRITE_PIPELINE_KIT\templates\SPRITE_PIPELINE_CHECKLIST.template.md`

Current shape:

```text
╔══════════════════════════════════════════════════════════════════╗
║ sprite-animation-workflow                                       ║
╠════════════════════╦═══════════════════════════╦═════════════════╣
║ Docs/playbooks     ║ strong workflow wisdom    ║ reusable        ║
║ Templates          ║ source/manifests/families ║ useful starter  ║
║ Bootstrap script   ║ scaffolds a new project   ║ simple/solid    ║
║ Live operation UI  ║ not present               ║ needs V2        ║
║ Apply/rollback     ║ not implemented           ║ needs V2        ║
║ Contact/proof loop ║ documented, not toolized  ║ needs V2        ║
╚════════════════════╩═══════════════════════════╩═════════════════╝
```

Useful lessons already captured there:

- lock source-of-truth art before generating anything
- define runtime roots, authored roots, variant axes, and motion families
- use small motion-family generation jobs instead of giant mixed boards
- keep Gemini/direct-generation results in a verified authored lane first
- never treat a candidate as complete until contact sheets, previews, and runtime proof pass
- propagate palette/color variants only after base motion is approved
- keep a written resume queue for interrupted work

Limitations of the old kit:

- It is mostly a documentation/scaffold bundle, not an active tool.
- The manifest template is intentionally minimal and does not model Wevito's real target identity: species, age, gender, color, family, frame id, source hash, candidate hash, geometry, prop-overlay contract, proof status, and rollback path.
- It does not provide a live queue, preview gallery, apply/rollback buttons, or proof runner.
- It does not prevent the exact mistakes Wevito hit later: stale orders, unclear source-of-truth lanes, invisible apply state, visual work hidden below scroll, and accidental broad mutation.
- It depends on human discipline rather than tool-enforced gates.

Recommended V2 direction:

```text
╔════════════════════════════════════════════════════════════════════╗
║ Hyper-Simplified Sprite Workflow V2                              ║
╠════════════════════════════════════════════════════════════════════╣
║ 1. Project manifest                                               ║
║    species/age/gender/color/family/frame -> source/runtime paths  ║
║                                                                    ║
║ 2. Queue board                                                     ║
║    one row per concrete repair or generation target               ║
║                                                                    ║
║ 3. Candidate folder                                                ║
║    generated/imported frames never overwrite runtime first         ║
║                                                                    ║
║ 4. Review strip                                                    ║
║    source vs current runtime vs candidate vs animated preview      ║
║                                                                    ║
║ 5. Apply probe                                                     ║
║    backs up exact current files, applies one row, runs proof       ║
║                                                                    ║
║ 6. Rollback                                                        ║
║    restores hashes exactly if proof or human review fails          ║
║                                                                    ║
║ 7. Evidence log                                                    ║
║    hashes, screenshots, contact sheets, status, decision labels    ║
╚════════════════════════════════════════════════════════════════════╝
```

This should be a tool, but not another giant "automation surface." The first version should fit on one screen and should answer only four questions:

- What exact sprite row are we working on?
- What files are source, runtime, candidate, backup, and proof?
- What changed visually?
- Can we apply or rollback safely?

Minimum V2 features:

- Import/read Wevito's current runtime tree and optional family specs.
- Create a concrete frame-scoped queue from validation findings and visual review notes.
- Generate contact sheets and GIF previews from existing source/runtime/candidate frames.
- Store candidates outside `sprites_runtime`.
- Apply only one selected row at a time.
- Make a backup-before-apply folder with hashes.
- Run proof scripts after apply.
- Roll back exact files by hash.
- Emit a markdown run summary for visual/code-side coordination.

Features to explicitly avoid in V2:

- no hidden autonomous long-running loops
- no app window that requires deep scrolling to see current work
- no broad "fix all sprites" mutation button
- no Gemini browser automation as a required first feature
- no direct runtime overwrite without candidate review
- no baked-in Wevito-only assumptions in the core schema

Best integration point with the existing plan:

- V2 belongs after the main repo asset gate is reconciled and before broad new visual generation.
- It can reuse concepts from `sprite-animation-workflow`, Hatch Pet, The-Workinator, and OpenClaw.
- It should share the same permission/task-card philosophy as the future Wevito helper tools: visible state, small operations, explicit approval, audit log, rollback.

Suggested V2 implementation risk: medium. The tool itself can be low risk if it starts read-only, but apply/rollback becomes medium risk because it touches runtime PNGs. It should start as a read-only auditor/previewer, then add one-row apply probes.

## Creative-AI / Dibu Review

Newly reviewed source:

- `C:\Users\fishe\Documents\projects\creative-ai\README.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\DIBU_BUILD_PLAN.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\DIBU_MODEL_INDEPENDENCE_ROADMAP.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\DIBU_MODEL_INDEPENDENCE_IMPLEMENTATION_PLAN_BY_CLAUDE.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\DIBU_INDEPENDENCE_IMPLEMENTATION_EVALUATION_BY_CLAUDE.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\COLLABORATIVE_TRAINING_WORKFLOW.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\MAINTAINER_RELEASE_WORKFLOW.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\DIBU_PERSONALITY.md`
- `C:\Users\fishe\Documents\projects\creative-ai\docs\architecture.md`
- `C:\Users\fishe\Documents\projects\creative-ai\services\dibu-orchestrator\app\main.py`
- `C:\Users\fishe\Documents\projects\creative-ai\services\dibu-orchestrator\app\adapters\base.py`
- `C:\Users\fishe\Documents\projects\creative-ai\services\dibu-orchestrator\app\services\asset_store.py`
- `C:\Users\fishe\Documents\projects\creative-ai\services\dibu-orchestrator\app\services\data_collector.py`
- `C:\Users\fishe\Documents\projects\creative-ai\services\dibu-orchestrator\app\services\training_controller.py`
- `C:\Users\fishe\Documents\projects\creative-ai\services\dibu-orchestrator\app\db\schema.sql`
- `C:\Users\fishe\Documents\projects\creative-ai\scripts\curate_dataset.py`
- `C:\Users\fishe\Documents\projects\creative-ai\scripts\export_contribution_bundle.py`
- `C:\Users\fishe\Documents\projects\creative-ai\packages\shared-types\src\asset.ts`
- `C:\Users\fishe\Documents\projects\creative-ai\packages\shared-types\src\session.ts`
- `C:\Users\fishe\Documents\projects\creative-ai\packages\shared-types\src\export.ts`

Current shape:

```text
╔════════════════════════════════════════════════════════════════════╗
║ Dibu / creative-ai                                                ║
╠══════════════════════╦═══════════════════════════╦═════════════════╣
║ Desktop app          ║ Tauri + React canvas      ║ useful pattern  ║
║ Orchestrator         ║ FastAPI backend           ║ reusable shape  ║
║ Adapters             ║ LLM/image/segmentation    ║ strong pattern  ║
║ Asset lanes          ║ raw/cleaned/thumb/export  ║ directly useful ║
║ Data engine          ║ logs/datasets/versions    ║ important       ║
║ Training lab         ║ visible jobs + bundles    ║ very relevant   ║
║ Personality          ║ curious creative host     ║ useful tone     ║
╚══════════════════════╩═══════════════════════════╩═════════════════╝
```

Most important lesson:

```text
bad learning loop
  pet/tool changes itself live because it "learned"
       ↓
  unstable behavior, no benchmark, no rollback

good learning loop
  reviewed examples
       ↓
  curated datasets
       ↓
  evaluation first
       ↓
  offline training/fine-tuning
       ↓
  deliberate model or rules release
```

This maps extremely well to Wevito. Pets can act as creative companions that collect feedback, explain choices, and prepare training examples, but they should not silently mutate the production art model, sprite assets, or runtime behavior.

Reusable Dibu systems:

- Adapter boundaries: `LLMAdapter`, `ImageGenAdapter`, and `SegmentationAdapter` are clean model/tool seams.
- Asset lanes: raw generated output, cleaned transparent sprites, thumbnails, and exports are separated.
- SQLite metadata: sessions, assets, conversation turns, interaction logs, dataset examples, and dataset versions are tracked explicitly.
- Data collection: prompt, interpreted intent, selected asset, output paths, latency, and user action become future training examples.
- Curation scripts: logs become dataset examples only through a review/approval process.
- Contribution bundles: reviewed examples can be exported/imported without sharing raw local databases.
- Training controller: training is visible, stateful, pausable/stoppable, and logs progress.
- Release workflow: dataset/model promotion is deliberate and benchmark-aware.
- Personality rule: Dibu is a host/guide, not a boastful fake genius.

Wevito adaptation:

```text
Dibu idea                    -> Wevito version
────────────────────────────────────────────────────────────────────
Creative sandbox             -> sprite candidate/review sandbox
Raw/cleaned/thumb/export     -> source/candidate/preview/runtime/proof lanes
Interaction logs             -> visual decision + pet-helper action logs
Dataset examples             -> reviewed sprite/edit/preference examples
Training Lab                 -> Creative Learning Lab panel
Contribution bundle          -> sprite QA/training/example bundle
Adapter architecture         -> swappable generator/evaluator/segmenter tools
Dibu host personality        -> pet creative guide personality
```

Potential "pet learns art" design:

```text
╔════════════════════════════════════════════════════════════════════╗
║ Pet Creative Learning Loop                                       ║
╠════════════════════════════════════════════════════════════════════╣
║ 1. Pet watches a review session                                  ║
║    "You accepted this goose frame; you rejected this blurry one." ║
║                                                                    ║
║ 2. Tool records structured labels                                 ║
║    subject, motion, issue kind, decision, notes, accepted path     ║
║                                                                    ║
║ 3. Pet summarizes preference                                      ║
║    "You prefer clear silhouettes and real leg extension."          ║
║                                                                    ║
║ 4. Human approves dataset example                                 ║
║    no automatic training from raw reactions                        ║
║                                                                    ║
║ 5. Offline eval/training improves narrow tools                    ║
║    command parser, prompt builder, crop checker, quality scorer    ║
║                                                                    ║
║ 6. New tool version is promoted deliberately                      ║
║    release notes, metrics, rollback path                          ║
╚════════════════════════════════════════════════════════════════════╝
```

What Wevito should not do:

- Do not train a broad foundation art model as a near-term goal.
- Do not let a pet agent silently approve or overwrite sprites.
- Do not treat user frustration or casual comments as training labels without explicit review.
- Do not make the visual thread depend on local-model training before the basic sprite workflow is stable.
- Do not copy Dibu wholesale into Wevito; reuse the architecture and data patterns.

Best first Wevito use:

- Add Dibu-style data lanes to Sprite Workflow V2: `source`, `candidate`, `cleaned`, `thumbnail`, `proof`, `export`.
- Add Dibu-style interaction logs for visual QA decisions.
- Add a Creative Learning Lab document/tool panel later that can export reviewed examples.
- Start with narrow learnable tasks:
  - command parsing for sprite targets
  - prompt packet generation
  - issue classification
  - preference summaries
  - simple quality/evaluation scoring
- Defer image-model fine-tuning until we have enough reviewed data and a real benchmark.

Implementation risk: medium-high if treated as model training; low-medium if treated first as data/metadata infrastructure. The correct first implementation is data capture and review bundles, not training.

## Papilionem ML Reuse Status

`C:\Users\fishe\Documents\projects\papilionem` was not present during this pass. Because of that, no real Papilionem ML capabilities were reviewed.

Planned follow-up if path is provided:

- inspect model/inference/training modules
- inspect creature/agent behavior systems
- look for state embedding, memory, behavior selection, or ML policy tools
- identify whether anything can safely inform Wevito pet-agent behavior

Current recommendation: do not block the helper-tool plan on Papilionem. Start with deterministic task cards and tool registry, then revisit ML once we can inspect it.

## Proposed Code-Side Phases

### Phase 0: Reconcile Repo State And Protect Visual Work

Goal: make sure code-side and visual-side are not stepping on each other.

Tasks:

- Compare main repo and code-side worktree asset validation state.
- Identify exactly which code-side changes produced 0 mixed-canvas rows in the separate worktree.
- Protect accepted goose hold-ball pilot and visual-side shared asset fixes.
- Create a narrow staging policy for future code changes.

Risk: low if read-only/diff-first.

Validation:

- no file mutation unless approved
- produce a short reconciliation note

### Phase 1: Fix Main Runtime Canvas/Test Gate

Goal: make the main repo pass the full vNext test suite without broad visual rewrites.

Tasks:

- Re-run current mismatch reporter.
- Apply only approved deterministic canvas normalization or import the already-reviewed green worktree result.
- Preserve sprite content positions and visual intent.
- Update canvas report artifacts.
- Re-run full vNext tests.

Risk: medium because runtime PNGs may change; needs explicit approval and backup.

Validation:

- `dotnet test .\vnext\Wevito.VNext.sln -c Debug --no-restore`
- `report_runtime_canvas_mismatches.py` returns 0 mixed rows
- visual side spot-checks any touched species before broad continuation

### Phase 2: Unified Action/Animation/Prop Contract

Goal: separate simulation state, visual animation family, and prop overlays.

Tasks:

- Define an `ActionIntent` or `ActionFamily` model.
- Define an `AnimationFamily` model that includes optional families.
- Define carried/nearby prop overlay metadata for ball, bowl, food, medicine, grooming, and habitat props.
- Update vNext action mapping so `water` can request `drink`, and fetch can request pickup/hold/carry/drop.
- Keep `PetAnimationState` for emotional/base states; do not overload it with every optional family.

Risk: medium.

Validation:

- new unit tests for action -> animation-family -> prop requirements
- no required sprite file changes

### Phase 3: Pet Personality And Interaction Parity

Goal: make personality development and interactions feel consistent across Godot and vNext.

Tasks:

- Decide whether vNext is the source-of-truth simulation and Godot consumes/export mirrors, or whether Godot remains standalone with parity rules.
- Add tests/specs for affection, stubbornness, playfulness, social need, activity, cleanliness preference, refusal chance, habits, sickness, age, and death.
- Ensure interactions visibly map to action families, not just stat changes.
- Add debug/dev forced scenarios for specific personality and condition states.

Risk: medium-high if it changes runtime behavior.

Validation:

- deterministic simulation tests
- Godot scenario proof
- manual pet behavior checklist

### Phase 4: Staged Fetch/Carry/Drink Proof

Goal: prove one complete physical interaction loop before expanding all animals.

Tasks:

- Implement action sequence controller for fetch.
- Use existing optional families in order: pickup -> hold -> carry -> drop.
- Keep ball runtime overlay only.
- Make auto-drink optionally visible with drink animation and water-bowl overlay.
- Add a dev scenario to force the sequence for one selected pet.

Risk: medium.

Validation:

- Godot dev proof for goose/baby/female/blue
- no double-ball rendering
- movement faces direction
- action completes cleanly and returns to idle/home

### Phase 5: Habitat Manifest, Depth, Occlusion, Contact Shadows

Goal: make habitats feel grounded and interactive instead of decorative.

Tasks:

- Create a habitat object manifest with object type, species compatibility, zone, anchor, depth band, occlusion mode, and contact shadow.
- Replace hardcoded/hard-to-find prop layout assumptions over time.
- Add simple ground contact shadows under pets and key props.
- Add perch/enter/rest/drink/eat/use zones for habitat objects.

Risk: medium.

Validation:

- manifest schema tests
- one species habitat proof
- visual review contact sheet/screenshots

### Phase 6: Wevito Helper Tool Foundation

Goal: create the safe foundation for pets helping with PC/web tasks.

Tasks:

- Add a local tool registry with typed tool definitions.
- Add a task-card model with states: waiting, working, review, failed, completed.
- Add a visible audit log for helper actions.
- Add approval rules for anything that opens external windows, writes files, sends messages, or runs commands.
- Start with safe tools: save link, summarize clipboard, capture screenshot, open local doc, create note, export QA packet.

Risk: medium if bounded; high if external automation is added too early.

Validation:

- unit tests for tool permissions and task state transitions
- UI proof showing pet task card state and result log

### Phase 6A: Hyper-Simplified Sprite Workflow V2

Goal: replace the confusing heavy Sprite Workflow App direction with a compact one-screen workflow console for sprite candidates, review, apply, rollback, and proof.

Tasks:

- Use `sprite-animation-workflow` as the workflow vocabulary source, not as a direct app implementation.
- Define a project-agnostic manifest schema with Wevito adapters.
- Build read-only queue/contact-sheet/proof views first.
- Add candidate import into an isolated lane.
- Add one-row apply with backup-before-apply hashes.
- Add exact rollback.
- Add markdown run summaries for visual/code coordination.

Risk: medium. Start read-only to keep risk low; treat apply/rollback as a separate approval-gated step.

Validation:

- read-only import validates current Wevito tree without mutation
- generated contact sheet matches source/runtime/candidate paths
- one-row dry-run apply reports exact files and hashes
- one-row real apply requires explicit approval and proves rollback

### Phase 6B: Creative Learning Lab

Goal: use Dibu's strongest ideas to let Wevito learn from reviewed art decisions without unstable live self-modification.

Tasks:

- Add a reviewed-example schema for sprite/edit/preference decisions.
- Capture visual review decisions from Sprite Workflow V2: accepted, revised, rejected, issue labels, notes, source/candidate/proof paths, hashes, and reviewer.
- Store raw/candidate/cleaned/thumbnail/proof lanes explicitly.
- Add exportable contribution/training bundles for reviewed examples.
- Add a small dashboard showing available reviewed examples by species, family, issue kind, and decision.
- Keep training offline and separate from the running game.
- Define evaluation benchmarks before any model fine-tuning.

Risk: low-medium for data capture; high if training or generated-art mutation is added too soon.

Validation:

- reviewed example bundles validate without runtime PNG mutation
- no unreviewed log becomes training data automatically
- evaluation benchmark exists before any model promotion
- every promoted rule/model/tool version has notes and rollback path

### Phase 7: Pet Agent Pilot

Goal: let one pet act as a visible helper persona without giving it unsafe autonomy.

Tasks:

- Assign one pet one simple task card.
- Pet personality affects presentation, not permission boundaries.
- The helper can ask for review, fail visibly, wait, wave, or celebrate.
- The user approves all risky actions.
- Keep task execution deterministic and logged.

Risk: medium.

Validation:

- one local-only task end-to-end
- no silent file/network mutations
- clear user-visible result

### Phase 8: ML / Higher-Agency Exploration

Goal: decide whether ML adds value after deterministic behavior is reliable.

Tasks:

- Locate and inspect Papilionem.
- Identify whether ML can support behavior selection, memory clustering, preference modeling, or local assistant routing.
- Compare Papilionem findings with Dibu's staged independence model before choosing any training approach.
- Keep ML advisory, not authoritative, until we have testable guardrails.

Risk: high if started too early.

Validation:

- design review before implementation
- no always-on autonomous tool use

## Recommended Immediate Next Step

The best next code-side move is Phase 0 followed by Phase 1: reconcile the separate green code-side worktree with the main repo and fix the main runtime canvas/test gate. That gives visual-side a safer floor while it continues manual cleanup.

After that, the next best design step is Phase 2: the unified action/animation/prop contract. That is the hinge that makes personality, fetch, drink, hold-ball, habitat interactions, and vNext/Godot parity stop fighting each other.

The new sprite-animation-workflow review adds one important planning update: before broad new visual mutation, we should build or specify the Hyper-Simplified Sprite Workflow V2 as a small read-only auditor/previewer first, then add one-row apply/rollback. That would give visual cleanup the tool it needed originally without reviving the confusing oversized app.

The creative-ai review adds a second planning update: if we want pets to "learn art," the first implementation should be a Creative Learning Lab that collects reviewed examples and preference labels. Training, fine-tuning, or model replacement should come only after evaluation benchmarks exist.

## Open Questions For Discussion

- Should vNext become the source-of-truth pet simulation, with Godot treated as a visual/runtime proof client, or should Godot remain the main game runtime with vNext as the helper/simulation lab?
- Should the first helper-tool pilot live in vNext shell only, or should it also surface inside the Godot pet runtime?
- Is the OpenClaw repo intended here definitely `https://github.com/openclaw/openclaw`, or did you mean a different OpenClaw repository?
- Where is Papilionem currently located if not `C:\Users\fishe\Documents\projects\papilionem`?
- Should the visual thread pause any runtime PNG cleanup touching crow/deer/fox/rat/snake until the code-side canvas gate is fixed, or should code-side adapt after visual cleanup lands?
- Should Sprite Workflow V2 be a small standalone local app, a Wevito `tools/` console/web page, or a Sprite Workflow App replacement inside `C:\Users\fishe\Documents\projects\sprite-workflow-app`?
- Should Dibu/creative-ai remain a separate source project, with Wevito only borrowing architecture, or should we eventually build a shared local creative orchestrator used by both Dibu and Wevito?
