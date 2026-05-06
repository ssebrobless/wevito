# Claude Codex Medium Phase Prompts

Date: 2026-05-05
Author: Claude master review
Audience: Codex medium-reasoning thread starting fresh on a phase
Companion to: `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`

## How To Use This Doc

Each section below is a self-contained prompt for one phase.

To run a phase:

1. Find the section for the phase you want to run.
2. Copy the entire section (from `=== PROMPT START` to `=== PROMPT END`) into your Codex thread.
3. Codex executes the phase, commits, pushes, and (for non-stop-and-ask phases) opens a PR.

Each prompt assumes Codex starts cold. It includes:

- exact docs to read
- repo state assertions
- exact commands to run
- exact files likely to edit
- exact validation steps
- exact commit and push instructions

Codex should not skip the "read first" step. The point of this format is that Codex doesn't need any prior context — but it does need the docs cited per phase.

## Shared Preamble (Repeat Every Phase)

Every phase prompt starts with a shared preamble. It is included verbatim in every prompt below so each is fully copy-paste-able.

The preamble:

- pins repo path and OS conventions
- pins the master rules
- pins the validation cheatsheet
- pins the commit/push strategy

## C-PHASE 0 — Preflight Baseline Check

=== PROMPT START

You are running C-PHASE 0 of the Wevito Claude implementation plan.

Repo: `C:\Users\fishe\Documents\projects\wevito`
OS: Windows. Default shell: PowerShell. Bash also available. Use forward slashes in paths inside docs but `\` in commands when invoking PowerShell.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — section "C-PHASE 0".
2. `docs\CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md` — section "Sample Sequence — C-PHASE 1" for the cadence pattern.
3. `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` — current state.

**Master rules (apply every phase):**

- Game runtime, visual asset mutation, tool execution, AI/ML learning are four separate concerns.
- PET TASKS work happens under the hood. No special task-completion / review / failure pet animations.
- Three pet AI helpers max. Personality affects presentation, never permission.
- Sprite Workflow V2 is simple, compact, one-result oriented.
- Creative Learning Lab starts as reviewed examples, not training.
- Capture is privacy-safe and approval-gated. Default capture indicator stays on.
- Audio defaults: normal Windows volume only. APO/external is guidance/handoff.
- Translation default = none until user picks; document where text goes.
- Coding tools: codeReview and codePatchPlan before approved patch execution.
- Mutation-capable phases need backup-before-apply, hash verification, exact rollback, post-apply proof.
- Every phase: build green, full tests green, safe Debug publish green, relevant PET TASKS probe green.

**Goal of this phase:** Prove the working tree is the expected baseline and capture a snapshot before any new work.

**Branch:** `claude-implementation/c-phase-0-baseline`

**Tasks:**

1. Confirm `git status` is clean. Confirm `git rev-parse HEAD` is reachable from `origin/main` and the latest commit is `c1d0a5cc9` (or the current `origin/main` if it has advanced — verify with the user before proceeding if it has).
2. Create branch `claude-implementation/c-phase-0-baseline` off `origin/main`.
3. Create folder `vnext\artifacts\baseline-2026-05-05\`.
4. Run and capture output to `vnext\artifacts\baseline-2026-05-05\`:
   - `dotnet build .\vnext\Wevito.VNext.sln` → `build.txt`
   - `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` → `tests.txt`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` → `safe-publish.txt`
   - `python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\baseline-2026-05-05\sprite-contract.json`
   - `python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\baseline-2026-05-05\canvas.json --markdown .\vnext\artifacts\baseline-2026-05-05\canvas.md`
   - `python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\baseline-2026-05-05\optional-readiness.json --markdown .\vnext\artifacts\baseline-2026-05-05\optional-readiness.md`
5. Confirm: build = `0` warnings / `0` errors. Tests = `144 / 144` (or current count). Note actual numbers.
6. Run a probe per existing tool family (these create new artifact folders — do NOT capture output here, just run and confirm exit 0):
   - `localDocs`, `spriteAudit`, `assetInventory`, `petState`, `codeReview`, `codePatchPlan`, `buildProof`, `translateText`, `audioAssist`, `screenCapture`.
   - For each: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "<command>" -ExpectedToolFamily <family> -SkipBuild`. For `buildProof` add `-ApproveBeforePreview`. For `translateText` add `-ExpectExecuteEnabledAfterPreview`.
7. Write `docs\C_PHASE0_BASELINE_2026-05-05.md` summarizing every check with pass/fail and any observed deviation from expected.

**Files to edit:**

- New: `docs\C_PHASE0_BASELINE_2026-05-05.md`
- New folder: `vnext\artifacts\baseline-2026-05-05\` (text + JSON outputs)

**Do not edit:** any code, any sprites, any other docs.

**Validation:** every check above passes. If any test or probe fails, STOP and message the user with the exact failure.

**Commit:** single commit `chore(phase0): capture baseline validation snapshot  [Phase: C-PHASE 0]`.

**Push and PR:** push the branch; open PR titled `C-PHASE 0: Preflight baseline check`. Body lists results from step 7. Not a stop-and-ask phase; user can fast-merge.

=== PROMPT END

## C-PHASE 1 — Action / Animation / Prop Unified Contract

=== PROMPT START

You are running C-PHASE 1 of the Wevito Claude implementation plan.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — section "C-PHASE 1".
2. `docs\CODE_SIDE_DEEP_REVIEW_AND_AGENT_TOOLS_PLAN_2026-05-05.md` — sections "P1: vNext cannot yet express the optional visual contract" and "Phase 2: Unified Action/Animation/Prop Contract".
3. `vnext\src\Wevito.VNext.Contracts\Models.cs` — current types.
4. `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs` — current action shape.
5. `vnext\content\actions.json` — current action data.
6. `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — current emission of action effects.
7. `vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs` — current animation folder loader.
8. `vnext\content\optional_animation_families.json` — current optional families.

**Master rules:** see C-PHASE 0 prompt above.

**Goal:** Decouple simulation `Action` from visible `AnimationFamily` and from `PropOverlayKind`. Today `actions.json` overloads base `animationState` (e.g. `water` → `eat`) and there is no first-class optional family selector in vNext. Fix without changing any sprite art.

**Branch:** `claude-implementation/c-phase-1-action-animation-prop-contract`

**Files to edit:**

- `vnext\src\Wevito.VNext.Contracts\Models.cs` — add `AnimationFamily` (enum: `Idle, Walk, Eat, Happy, Sad, Sleep, Sick, Bathe, Drink, PlayBall, HoldBall, PickupBall, DropBall, CarryBallWalk, CarryBallRun`), `PropOverlayKind` (enum: `None, Ball, WaterBowl, FoodDish, MedicineItem, GroomingItem, BathTowel`), `ActionVisualIntent` (record: `AnimationFamily Family`, `PropOverlayKind Overlay`, `bool LoopUntilStopped`).
- `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs` — extend the action record with `string? OptionalAnimationFamily`, `string? PropOverlay`. Default both to null.
- `vnext\content\actions.json` — add `"optionalAnimationFamily": "drink"` to `water`. Add `"optionalAnimationFamily": "play_ball"` to `play`. Don't change other fields.
- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — when applying an action, emit `ActionVisualIntent` derived from the action record. Default falls back to base `animationState` when optional family is null.
- `vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs` — extend the load path so optional family folders (e.g. `sprites_runtime/<species>/<age>/<gender>/<color>/drink/`) are tried before falling back to the base family folder. Do NOT walk to a missing folder; exists check first.

**Tests to add (in `vnext\tests\Wevito.VNext.Tests\`):**

- `ActionVisualIntentTests.cs` — assert: water with `optionalAnimationFamily: drink` produces intent with `AnimationFamily.Drink`. Water without optional family falls back to `AnimationFamily.Eat`.
- `SpriteAssetServiceOptionalFamilyTests.cs` — assert: when optional family folder exists, it is selected. When missing, base family folder is selected.

Do NOT touch:
- `vnext\content\optional_animation_families.json` (used elsewhere)
- `sprites_runtime/_metadata/prop_anchors.json`
- any PNG
- any Godot script

**Tasks:**

1. Add types to `Models.cs`. Keep them backwards compatible.
2. Extend action record in `ContentContracts.cs`. Defaults preserve current behavior.
3. Update `actions.json` for water and play only.
4. Extend `PetSimulationEngine` to emit `ActionVisualIntent`.
5. Extend `SpriteAssetService` for optional family fallback.
6. Add focused tests.
7. Run validation.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~ActionVisualIntentTests|FullyQualifiedName~SpriteAssetServiceOptionalFamilyTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild
```

Pass: focused tests green, build = `0/0`, full tests `144/144` (or +new tests), safe publish OK, probe OK.

**Commits (in order):**

1. `feat(contracts): add AnimationFamily, PropOverlayKind, ActionVisualIntent  [Phase: C-PHASE 1]`
2. `feat(content): hint optional families for water and play  [Phase: C-PHASE 1]`
3. `feat(core): emit ActionVisualIntent from simulation  [Phase: C-PHASE 1]`
4. `feat(shell): SpriteAssetService loads optional family folders with fallback  [Phase: C-PHASE 1]`
5. `test: cover action/intent/family resolution  [Phase: C-PHASE 1]`

**Push:** after each commit. Final push: confirm branch is clean.

**PR:** `C-PHASE 1: Action / Animation / Prop unified contract`. Not stop-and-ask. User merges.

=== PROMPT END

## C-PHASE 2 — Auto-Care Visible + Drink Animation Routing

=== PROMPT START

You are running C-PHASE 2.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 2".
2. `docs\CODE_SIDE_DEEP_REVIEW_AND_AGENT_TOOLS_PLAN_2026-05-05.md` — "P1: Auto-care can be statistically correct but visually invisible".
3. `scripts\game_manager.gd` — current auto-drink path.
4. `scripts\pet.gd` — current animation control.
5. `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — auto-care path (after C-PHASE 1).

**Master rules:** see C-PHASE 0.

**Goal:** When auto-drink raises hydration, the pet visibly plays `drink` (where art exists). Auto-care must use the same animation path as manual action.

**Branch:** `claude-implementation/c-phase-2-auto-care-visible`

**Tasks:**

1. Audit `game_manager.gd` for every auto-care path. Find every place that updates a stat without playing an animation. Document each in a comment in the commit body.
2. For auto-drink specifically: route through the same `pet.gd` animation request that manual drink uses. Prefer optional `drink` family; fall back to `eat` if unavailable.
3. Add a one-shot guard so a pet that drinks while moving doesn't double-trigger.
4. Mirror in vNext: `PetSimulationEngine` exposes auto-care decisions via the same `ActionVisualIntent` from C-PHASE 1.
5. Add or extend dev forced scenario for low-hydration → visible drink in Godot.

**Files likely to edit:**

- `scripts\game_manager.gd` (Godot)
- `scripts\pet.gd` (Godot, possibly)
- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`
- New test: `vnext\tests\Wevito.VNext.Tests\AutoCareIntentTests.cs`

**Do not touch:** sprites, content JSON beyond what C-PHASE 1 changed, prop anchors.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~AutoCareIntentTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Manual: if Godot is available, run a dev forced scenario for low-hydration on goose/baby/female/blue and verify drink animation plays. Capture a screenshot to `vnext\artifacts\c-phase-2-auto-care\`.

**Commits:**

1. `feat(godot): auto-drink routes through visible animation request  [Phase: C-PHASE 2]`
2. `feat(vnext): mirror auto-care visual intent in simulation  [Phase: C-PHASE 2]`
3. `test: cover auto-care visual intent  [Phase: C-PHASE 2]`

**PR:** `C-PHASE 2: Auto-care visible + drink animation routing`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 3 — Staged Fetch Sequence Controller

=== PROMPT START

You are running C-PHASE 3.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 3".
2. `docs\CODE_SIDE_DEEP_REVIEW_AND_AGENT_TOOLS_PLAN_2026-05-05.md` — "P1: Fetch/carry/hold is not a true sequence yet".
3. `docs\WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md` — accepted hold-ball pilot.
4. `docs\WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md` — visual plan.
5. `scripts\pet.gd` — existing animation/prop overlay code.
6. `scripts\game_manager.gd` — existing fetch path.
7. `sprites_runtime/_metadata/prop_anchors.json` — READ ONLY. Do not edit.

**Master rules:** see C-PHASE 0.

**Goal:** Replace single-shot fetch with `MoveToBall → Pickup → Hold → CarryWalk/Run → Drop → ReturnIdle`. Ball stays a runtime overlay.

**Branch:** `claude-implementation/c-phase-3-staged-fetch`

**Tasks:**

1. Define `FetchStage` enum in `pet.gd` and a stage controller with: current stage, max-duration-per-stage, fallback-on-missing-family.
2. Implement Godot side first.
3. Mirror in `PetSimulationEngine.cs` for proof scenario parity.
4. Verify ball is overlay only — never bake into pet PNG.
5. Add dev forced scenario `force_fetch_sequence` for goose/baby/female/blue.
6. Tests for stage advance, timeout fallback, optional-family-missing fallback.

**Files likely to edit:**

- `scripts\pet.gd` (Godot)
- `scripts\game_manager.gd` (Godot)
- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`
- `vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs` — verify each optional family loads cleanly.
- New tests: `vnext\tests\Wevito.VNext.Tests\StagedFetchSequenceTests.cs`.

**Do not touch:**

- `prop_anchors.json` (READ ONLY)
- any PNG
- `sprites_authored*`

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~StagedFetchSequenceTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild
```

Manual (if Godot available): run forced fetch scenario; capture screenshot per stage; save to `vnext\artifacts\c-phase-3-staged-fetch\`.

**Commits:**

1. `feat(sim): staged fetch action contract  [Phase: C-PHASE 3]`
2. `feat(godot): pet.gd implements staged fetch sequence  [Phase: C-PHASE 3]`
3. `feat(godot): game_manager.gd calls staged fetch  [Phase: C-PHASE 3]`
4. `feat(vnext): mirror staged fetch in PetSimulationEngine  [Phase: C-PHASE 3]`
5. `test: cover stage transitions and fallbacks  [Phase: C-PHASE 3]`

**PR:** `C-PHASE 3: Staged fetch sequence controller`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 4 — Care / Item Content Mapping

=== PROMPT START

You are running C-PHASE 4.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 4".
2. `docs\WEVITO_CARE_MEDICINE_OBJECT_REVIEW_PACKET_2026-05-05.md` — source of truth for content mapping.
3. `docs\WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md` — asset paths.
4. `vnext\content\items.json` — current 7-record state.
5. `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs` — extend.
6. `vnext\src\Wevito.VNext.Core\ContentRepository.cs` — extend loader.

**Master rules:** see C-PHASE 0.

**Goal:** Connect the 81 shared item PNGs to first-class content records.

**Branch:** `claude-implementation/c-phase-4-care-item-content-mapping`

**Tasks:**

1. Create new `vnext\content\item_visual_mapping.json` with one record per first-class item. Each record:
   ```json
   {
     "id": "care-bandage-roll",
     "displayName": "Bandage Roll",
     "category": "care",
     "visualAssetId": "items/care/bandage_roll",
     "speciesIds": [],
     "smallIconSafe": true,
     "habitatObjectSafe": false,
     "notes": "Injury care, doctor follow-up"
   }
   ```
2. Cover: 9 care, 9 containers (including water-bowl variants), 9 toys_a, 9 toys_b, 9 utility (use review packet's habitat list). Food groups: cover the species-group mappings from the review packet.
3. Per packet: mark `medicine_dropper` and `thermometer` as `smallIconSafe: false`.
4. Extend `ContentContracts.cs` with `ItemVisualMapping` record.
5. Extend `ContentRepository.cs` to load `item_visual_mapping.json`.
6. Add `vnext\tests\Wevito.VNext.Tests\ItemVisualMappingTests.cs` that asserts every `visualAssetId` resolves to a real PNG path under `sprites_shared_runtime\items\<group>\<asset>.png`.

**Files likely to edit:**

- New: `vnext\content\item_visual_mapping.json`
- `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs`
- `vnext\src\Wevito.VNext.Core\ContentRepository.cs`
- New test: `vnext\tests\Wevito.VNext.Tests\ItemVisualMappingTests.cs`
- (Optional) `vnext\tests\Wevito.VNext.Tests\ContentCoverageTests.cs` extension to include new mappings.

**Do not touch:** PNGs, other content files, `tool_definitions.json`.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~ItemVisualMappingTests|FullyQualifiedName~ContentCoverageTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Commits:**

1. `feat(content): item visual mapping schema  [Phase: C-PHASE 4]`
2. `feat(content): map shared item art to first-class records  [Phase: C-PHASE 4]`
3. `feat(core): ContentRepository loads item visual mappings  [Phase: C-PHASE 4]`
4. `test: cover item visual mapping coverage  [Phase: C-PHASE 4]`

**PR:** `C-PHASE 4: Care / item content mapping`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 5 — Habitat Object Manifest Schema

=== PROMPT START

You are running C-PHASE 5.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 5".
2. `docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md` — source of truth for schema.
3. `docs\WEVITO_REFINED_HABITAT_LOADOUT_REVIEW_2026-05-05.md` — pilot species data.
4. `vnext\src\Wevito.VNext.Shell\HabitatLoadoutResolver.cs` — current hardcoded path.
5. `vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs` — current presentation.

**Master rules:** see C-PHASE 0.

**Goal:** Replace hardcoded prop layout with a declarative manifest. Schema only — runtime placement comes in C-PHASE 6.

**Branch:** `claude-implementation/c-phase-5-habitat-manifest`

**Tasks:**

1. New `vnext\content\habitat_loadouts.json`. Per species/environment, three slots: `primary`, `interaction`, `decor`. Each slot: `assetId`, `defaultRect`, `depthBand`, `occlusionMode`, `contactShadowMode`, `ageScalePolicy`, `notes`.
2. Pilot species (per anchor contract doc): goose, rat, crow, snake, frog. Other 5: provide placeholder entries that mirror current hardcoded behavior (so renderer fallback is identical).
3. New enum types in `ContentContracts.cs`: `DepthBand` (Backdrop, FarProp, GroundContact, PetShadow, PetBody, HeldOrCarriedProp, NearOccluder, UiOverlay), `OcclusionMode` (None, BodyOnly, FullPet), `ContactShadowMode` (None, Soft, Hard), `AgeScalePolicy` (Constant, PerStage).
4. `HabitatLoadoutResolver` reads manifest. If file missing or species missing, log a `TraceLog` warning and fall back to hardcoded behavior.
5. Extend `HabitatPresentationModels.cs` with `DepthBand`, `OcclusionMode`, `ContactShadowMode` fields. Renderer doesn't use them yet.

**Files likely to edit:**

- New: `vnext\content\habitat_loadouts.json`
- `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs`
- `vnext\src\Wevito.VNext.Shell\HabitatLoadoutResolver.cs`
- `vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs`
- New tests: `vnext\tests\Wevito.VNext.Tests\HabitatLoadoutManifestTests.cs`, `HabitatLoadoutResolverFallbackTests.cs`.

**Do not touch:** sprites, prop_anchors.json, renderer XAML.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~HabitatLoadoutManifestTests|FullyQualifiedName~HabitatLoadoutResolverFallbackTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Commits:**

1. `feat(content): habitat loadouts manifest schema  [Phase: C-PHASE 5]`
2. `feat(content): pilot-species + fallback habitat loadouts  [Phase: C-PHASE 5]`
3. `feat(shell): HabitatLoadoutResolver reads manifest with fallback  [Phase: C-PHASE 5]`
4. `test: cover habitat loadout manifest + resolver fallback  [Phase: C-PHASE 5]`

**PR:** `C-PHASE 5: Habitat object manifest schema`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 6 — Habitat 5-Species Pilot Runtime Placement

=== PROMPT START

You are running C-PHASE 6. **This is a STOP-AND-ASK phase.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 6".
2. `docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md`.
3. `vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs` (or whichever surface renders habitat — confirm by searching for `HabitatLoadoutResolver` consumers).
4. `vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs` (after C-PHASE 5).
5. `scripts\main_scene.gd`, `scripts\pet.gd`.

**Master rules:** see C-PHASE 0.

**Goal:** Apply manifest at runtime for goose / rat / crow / snake / frog. Implement primary anchor + interaction object + depth band.

**Branch:** `claude-implementation/c-phase-6-habitat-pilot`

**Tasks:**

1. Implement depth band → z-order in vNext renderer. Order matches manifest contract.
2. Add a contact shadow under each pilot species' primary anchor and pet body. Prefer drawing as a soft ellipse in WPF (vector, no PNG). If a PNG is unavoidable, STOP AND ASK first.
3. Implement primary anchor + interaction object slot per species. Interaction object visibility driven by need/action (thirsty goose → `pond_dish`).
4. Run Godot dev forced scenarios for each pilot species. Capture screenshots to `vnext\artifacts\c-phase-6-habitat-pilot\<species>.png`.
5. **STOP AND ASK** after screenshots are captured. Do NOT auto-merge.

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs` (z-order computation)
- `vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs` (consume models)
- `vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml` (drawing surface, possibly)
- `scripts\main_scene.gd`, `scripts\pet.gd` (Godot mirror, optional this phase)
- New tests for depth-band ordering.

**Do not touch:** PNGs unless explicitly approved, prop_anchors.json, sprites_authored*.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~HabitatDepthBandTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Manual: screenshots present for all five species.

**Commits:**

1. `feat(shell): depth-band z-order computation  [Phase: C-PHASE 6]`
2. `feat(shell): contact shadow rendering under pets and primary anchors  [Phase: C-PHASE 6]`
3. `feat(shell): consume habitat manifest for pilot species  [Phase: C-PHASE 6]`
4. `test: cover depth-band ordering  [Phase: C-PHASE 6]`
5. `chore(phase6): habitat pilot screenshots  [Phase: C-PHASE 6]`

**Push and PR:** push branch, open PR as **DRAFT** titled `C-PHASE 6 [DRAFT]: Habitat 5-species pilot runtime placement`. Then send the user a stop-and-ask message per `CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md` "Sample Sequence — C-PHASE 22".

=== PROMPT END

## C-PHASE 7 — Habitat Expansion To Remaining 5 Species

=== PROMPT START

You are running C-PHASE 7.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 7".
2. `docs\WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md` — "Recommended species/default habitat mapping".
3. `vnext\content\habitat_loadouts.json` (after C-PHASE 5/6).

**Master rules:** see C-PHASE 0. Pre-condition: C-PHASE 6 merged with screenshots approved.

**Goal:** Extend manifest + renderer to fox, deer, pigeon, raccoon, squirrel.

**Branch:** `claude-implementation/c-phase-7-habitat-expansion`

**Tasks:**

1. Add manifest entries for the five non-pilot species using inventory checklist's mapping table.
2. Run dev scenarios; capture screenshots to `vnext\artifacts\c-phase-7-habitat-expansion\<species>.png`.
3. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` "Habitat/environment" row to ~70%.
4. Re-export PDF dashboard:
   ```text
   powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\export-project-dashboard-pdf.ps1
   python .\tools\export-project-dashboard-packet-pdf.py
   ```

**Files likely to edit:**

- `vnext\content\habitat_loadouts.json`
- `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.pdf` (regenerated)
- `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_PACKET_2026-05-05.pdf` (regenerated)

**Do not touch:** PNGs, prop_anchors.json.

**Validation:** full tests, safe publish, screenshots present, PDFs regenerated.

**Commits:**

1. `feat(content): habitat loadouts for all 10 species  [Phase: C-PHASE 7]`
2. `chore(phase7): habitat expansion screenshots  [Phase: C-PHASE 7]`
3. `docs: bump habitat completion in dashboard  [Phase: C-PHASE 7]`
4. `docs: regenerate dashboard PDFs  [Phase: C-PHASE 7]`

**PR:** `C-PHASE 7: Habitat expansion to remaining 5 species`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 8 — Tool Hub Phase-26 IA Refactor

=== PROMPT START

You are running C-PHASE 8.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 8".
2. `docs\WEVITO_TOOL_HUB_INFORMATION_ARCHITECTURE_2026-05-05.md` — IA spec.
3. `docs\WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md` — refinement spec.
4. `docs\CODE_SIDE_PHASE26_TOOL_HUB_SHELL_REFACTOR_2026-05-05.md` (if it exists, for prior context).
5. `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
6. `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
7. `tools\probe-vnext-pet-tasks.ps1`

**Master rules:** see C-PHASE 0.

**Goal:** Implement Tool Hub IA end to end.

**Branch:** `claude-implementation/c-phase-8-tool-hub-refactor`

**Tasks:**

1. Rename popup title `Pet Helpers` → `PET TASKS`.
2. Move command textbox to top.
3. Replace long intro with `Ask your pets ...` placeholder.
4. Restructure into IA sections: header, helper strip, command row, task card, result, timeline, safety footer.
5. Add `REPORT ONLY` badge to header.
6. **Preserve every existing automation ID** (verify by grep before and after):
   - `PetCommandTextBox`, `PetCommandSubmitButton`, `PetTaskCapabilityText`, `PetWellbeingSnapshotText`, `PetTaskQueueComboBox`, `PetTaskApproveButton`, `PetTaskPreviewButton`, `PetTaskCancelButton`, `PetTaskExecuteButton`.
7. Add `PetTaskResultPathText` and `PetTaskNextActionText` if not present.
8. Add `Open Report`, `Copy Path`, `Open Folder` buttons but leave wiring stubbed (real handlers in C-PHASE 10).
9. Update probe to assert new structure without removing old assertions.

**Files to edit:**

- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `tools\probe-vnext-pet-tasks.ps1`

**Do not touch:** any adapter, content, sprite, or other XAML.

**Validation:**

```text
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
# Run every existing probe
foreach ($t in @("localDocs","spriteAudit","assetInventory","petState","codeReview","codePatchPlan","translateText","audioAssist","screenCapture")) {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskKind $t -SkipBuild
}
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "run a build proof" -ExpectedToolFamily buildProof -ApproveBeforePreview -SkipBuild
```

Manual: capture screenshot of new layout to `vnext\artifacts\c-phase-8-tool-hub\layout.png`.

**Commits:**

1. `feat(shell): Tool Hub IA refactor (PET TASKS layout)  [Phase: C-PHASE 8]`
2. `feat(shell): add REPORT ONLY badge and result/next-action labels  [Phase: C-PHASE 8]`
3. `chore(tools): probe asserts new Tool Hub structure  [Phase: C-PHASE 8]`

**PR:** `C-PHASE 8: Tool Hub IA refactor`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 9 — Three-Helper Roster + Pet Command Bar @Addressing

=== PROMPT START

You are running C-PHASE 9.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 9".
2. `docs\WEVITO_PET_AGENT_TEXT_BAR_PLAN_2026-05-05.md` — three-helper plan.
3. `docs\WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md` — pet roles.
4. `vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
5. `vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
6. `vnext\src\Wevito.VNext.Core\PetCommandBarService.cs`
7. `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml` (after C-PHASE 8)

**Master rules:** see C-PHASE 0. Especially: personality affects presentation, never permission.

**Goal:** Three named helper pets with `@Name` and `Name,` addressing. Cap = 3.

**Branch:** `claude-implementation/c-phase-9-three-helper-roster`

**Tasks:**

1. Add `HelperPet` record to `PetAgentContracts.cs`: `Id`, `Name`, `Species`, `Role`, `State` (Available, Drafting, Reviewing, Blocked), `CurrentTaskId`.
2. Default roster: Scout (frog, docs/research), Inspector (pigeon, sprite QA), Builder (rat, code/proofs).
3. `PetCommandParser`: parse `@Name`, `Name,` (case-insensitive on names, comma optional). Routes to addressed helper. Unaddressed → current selected helper or first available.
4. `PetCommandBarService`: manage roster, enforce cap = 3. `AddHelper()` rejects if active count == 3.
5. `ToolPopupWindow.xaml`: render three helper cards (compact). Each card shows name, species, role, state, current task id (truncated).
6. Tests: parser routing, roster cap, default names, addressing fallback, cap exceeded rejection.
7. Add a test asserting that policy decisions are identical regardless of helper name/role (loop over (Scout, Inspector, Builder) and confirm same `ToolPolicyEvaluator` outcome for the same task).

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs`
- `vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- `vnext\src\Wevito.VNext.Core\PetCommandBarService.cs`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~PetCommandParserTests|FullyQualifiedName~PetCommandBarServiceTests|FullyQualifiedName~ToolPolicyEvaluatorTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "Scout, summarize the sprite docs" -ExpectedToolFamily localDocs -SkipBuild
```

**Commits:**

1. `feat(contracts): HelperPet roster model  [Phase: C-PHASE 9]`
2. `feat(parser): @Name and Name, addressing  [Phase: C-PHASE 9]`
3. `feat(core): PetCommandBarService roster cap  [Phase: C-PHASE 9]`
4. `feat(shell): three-helper card strip  [Phase: C-PHASE 9]`
5. `test: cover parser routing, cap, policy invariance  [Phase: C-PHASE 9]`

**PR:** `C-PHASE 9: Three-helper roster + @addressing`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 10 — Artifact Controls

=== PROMPT START

You are running C-PHASE 10.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 10".
2. `docs\WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md` — "Artifact Controls".
3. `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
4. `vnext\src\Wevito.VNext.Core\PetTaskCardQueueService.cs`

**Master rules:** see C-PHASE 0.

**Goal:** Wire the `Open Report` / `Copy Path` / `Open Folder` buttons stubbed in C-PHASE 8.

**Branch:** `claude-implementation/c-phase-10-artifact-controls`

**Tasks:**

1. `Open Report`: open the most-recent `run-summary.md` in the OS default app. Use `Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true })`.
2. `Copy Path`: `Clipboard.SetText(path)`.
3. `Open Folder`: `Process.Start("explorer.exe", $"/select,\"{path}\"")`.
4. Disable all three buttons unless the current task card has a result path.
5. **Validate** the path is under `vnext\artifacts\pet-tasks\` (use `Path.GetFullPath` and prefix-check the canonical path) before opening. If not, show a "blocked: path outside artifact root" message.
6. Tests in `PetTaskCardQueueServiceTests` for the path resolution helper.

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `vnext\src\Wevito.VNext.Core\PetTaskCardQueueService.cs` (path helper, if not already there)
- `vnext\tests\Wevito.VNext.Tests\PetTaskCardQueueServiceTests.cs`

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~PetTaskCardQueueServiceTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild
```

Manual: in the running app, run a localDocs preview, then click Open Report, Copy Path, Open Folder. Verify each works.

**Commits:**

1. `feat(shell): wire Open Report / Copy Path / Open Folder  [Phase: C-PHASE 10]`
2. `test: cover artifact path validation  [Phase: C-PHASE 10]`

**PR:** `C-PHASE 10: Artifact controls`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 11 — Wevito-Window Screenshot Execution

=== PROMPT START

You are running C-PHASE 11.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 11".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 1 (Windows screen capture).
3. `docs\SCREEN_CAPTURE_TOOLING_RESEARCH_PLAN_2026-05-05.md`.
4. `docs\CODE_SIDE_PHASE40_SCREEN_CAPTURE_PREVIEW_2026-05-05.md` — preview is already implemented.
5. `vnext\src\Wevito.VNext.Core\ScreenCapturePreviewAdapter.cs`
6. `vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
7. `vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
8. `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
9. `vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`

**Master rules:** see C-PHASE 0. Capture is privacy-safe and approval-gated.

**Goal:** Approval-gated execution adapter for Wevito-window-only capture. Use Windows.Graphics.Capture.

**Branch:** `claude-implementation/c-phase-11-wevito-window-capture`

**Tasks:**

1. Audit project references first: confirm only `Wevito.VNext.Shell` will need WinRT. If TFM bump cascades to `Broker` or `AutomationRunner`, STOP AND ASK.
2. Bump `Wevito.VNext.Shell` TFM to `net8.0-windows10.0.19041.0` (or higher) if not already.
3. New `vnext\src\Wevito.VNext.Shell\IScreenCaptureBackend.cs` interface.
4. New `vnext\src\Wevito.VNext.Shell\WindowsGraphicsCaptureBackend.cs`:
   - Use `IGraphicsCaptureItemInterop.CreateForWindow`.
   - Capture one frame.
   - Save PNG via `Direct3D11CaptureFramePool` → CPU readback → encode.
   - Long-lived `DispatcherQueueController` (don't let it GC).
5. New `vnext\tests\Wevito.VNext.Tests\fakes\FakeScreenCaptureBackend.cs` — generates a deterministic 1×1 PNG for tests.
6. New `vnext\src\Wevito.VNext.Core\ScreenCaptureExecutionAdapter.cs`:
   - Saves to `vnext\artifacts\pet-tasks\<ts>-screencapture-execute\`:
     - `screenshot.png`
     - `manifest.json` (HWND title, target window rect, capture timestamp, indicator state, redaction state)
     - `run-summary.md`.
7. Set `WDA_EXCLUDEFROMCAPTURE` on Wevito's settings/credential surfaces. If those don't exist as separate windows yet, document what to do when they're added (TODO comment in `App.xaml.cs`).
8. Approval gate: `RUN` only enabled for reviewed `screenCapture` cards with target = `wevito_window`. Other targets remain blocked.
9. Update `CapturePolicyEvaluator` to expose the per-target allow/deny matrix.
10. Update probe: `-TaskText "screenshot the Wevito window" -ExpectedToolFamily screenCapture -ApproveBeforePreview -ExpectExecuteEnabledAfterPreview`. Add `-ExpectArtifactFileCreated screenshot.png` if probe supports it (extend probe if not).
11. Tests:
    - `ScreenCaptureExecutionAdapterTests` — uses fake backend; asserts manifest fields, artifact files exist.
    - `CapturePolicyEvaluatorTests` updates — only `wevito_window` is approvable in this phase.

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj` (TFM)
- New: `vnext\src\Wevito.VNext.Shell\IScreenCaptureBackend.cs`
- New: `vnext\src\Wevito.VNext.Shell\WindowsGraphicsCaptureBackend.cs`
- New: `vnext\src\Wevito.VNext.Core\ScreenCaptureExecutionAdapter.cs`
- `vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`
- `tools\probe-vnext-pet-tasks.ps1`
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~ScreenCaptureExecutionAdapterTests|FullyQualifiedName~CapturePolicyEvaluatorTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "screenshot the Wevito window" -ExpectedToolFamily screenCapture -ApproveBeforePreview -ExpectExecuteEnabledAfterPreview -SkipBuild
```

Manual: in running app, run a screenshot task end-to-end with RUN clicked. Confirm screenshot.png is created. Confirm OS yellow border appeared during capture.

**Commits:**

1. `chore(shell): bump TFM to net8.0-windows10.0.19041.0 for WinRT  [Phase: C-PHASE 11]`
2. `feat(shell): IScreenCaptureBackend + WindowsGraphicsCaptureBackend  [Phase: C-PHASE 11]`
3. `feat(core): ScreenCaptureExecutionAdapter + manifest  [Phase: C-PHASE 11]`
4. `feat(shell): RUN gate for wevito_window capture  [Phase: C-PHASE 11]`
5. `feat(shell): WDA_EXCLUDEFROMCAPTURE on sensitive surfaces  [Phase: C-PHASE 11]`
6. `chore(tools): probe extends with artifact-file assertion  [Phase: C-PHASE 11]`
7. `test: cover capture execution with fake backend  [Phase: C-PHASE 11]`

**PR:** `C-PHASE 11: Wevito-window screenshot execution`. Not stop-and-ask itself, but if TFM cascades or if first run captures unexpected content, escalate.

=== PROMPT END

## C-PHASE 12 — Region + Last-Region Screenshot

=== PROMPT START

You are running C-PHASE 12.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 12".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 1.
3. `vnext\src\Wevito.VNext.Core\ScreenCaptureExecutionAdapter.cs` (after C-PHASE 11).
4. `vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`.

**Master rules:** see C-PHASE 0.

**Goal:** User-selected region capture, with last-region recall.

**Branch:** `claude-implementation/c-phase-12-region-capture`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Shell\RegionPickerWindow.xaml`: transparent always-on-top WPF window with drag-to-select rectangle. ESC cancels. Returns `Rect` or null.
2. Persist last region: `%LOCALAPPDATA%\Wevito\last_region.json`. Geometry only.
3. Approval gate: region capture is approval-required (medium risk).
4. Last region recallable via parser: `screenshot last region`.
5. Use `PerMonitorV2` DPI awareness (already standard for WPF .NET 8 — confirm).
6. Tests: parser command coverage, policy gate, region geometry round-trip.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Shell\RegionPickerWindow.xaml` + `.xaml.cs`
- `vnext\src\Wevito.VNext.Core\ScreenCaptureExecutionAdapter.cs` (region branch)
- `vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`
- `vnext\src\Wevito.VNext.Core\PetCommandParser.cs`
- New tests.

**Validation:** focused tests, full tests, safe publish, probe with `-TaskText "screenshot a region"` (region picker UI is interactive; the probe just checks routing).

**Commits:**

1. `feat(shell): RegionPickerWindow drag-to-select  [Phase: C-PHASE 12]`
2. `feat(core): region capture with last-region recall  [Phase: C-PHASE 12]`
3. `feat(parser): screenshot last region  [Phase: C-PHASE 12]`
4. `test: cover region capture routing  [Phase: C-PHASE 12]`

**PR:** `C-PHASE 12: Region + last-region screenshot`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 13 — Short Proof Clips

=== PROMPT START

You are running C-PHASE 13. **This is a STOP-AND-ASK phase.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 13".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 1.
3. `docs\CLAUDE_IMPLEMENTATION_RISK_REGISTER_2026-05-05.md` — R04, R14.
4. `vnext\src\Wevito.VNext.Shell\WindowsGraphicsCaptureBackend.cs` (after C-PHASE 11).

**Master rules:** see C-PHASE 0. Recording is the highest-privacy capture; default off and visibly indicated.

**Goal:** 5-10 second MP4 proof clips, Wevito window only. WGC frame pool → Media Foundation H.264 sink writer.

**Branch:** `claude-implementation/c-phase-13-proof-clips`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Shell\MediaFoundationVideoEncoder.cs`. Wraps `MFCreateSinkWriterFromURL`, `IMFTransform` for NV12 conversion. Wrap COM in try/finally.
2. Extend backend with `CaptureClipAsync(IntPtr hwnd, TimeSpan duration, string outputPath)`.
3. Hard cap: 10 seconds. UI shows countdown.
4. Default scene = Wevito window only. Region/foreground/desktop blocked in this phase.
5. Default audio = none.
6. Visible Wevito-drawn recording indicator (separate from OS yellow border). Drawn on a small always-on-top overlay.
7. Save to `vnext\artifacts\pet-tasks\<ts>-screencapture-clip\clip.mp4` + manifest + summary.
8. Tests with a `FakeMediaFoundationVideoEncoder` returning a deterministic 1-frame stub.
9. Update `CapturePolicyEvaluator` to expose recording approval state.
10. **STOP AND ASK** before merging. Recording is privacy-sensitive.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Shell\MediaFoundationVideoEncoder.cs`
- New: `vnext\src\Wevito.VNext.Shell\RecordingIndicatorWindow.xaml`
- `vnext\src\Wevito.VNext.Shell\WindowsGraphicsCaptureBackend.cs` (extension)
- `vnext\src\Wevito.VNext.Core\ScreenCaptureExecutionAdapter.cs` (clip branch)
- `vnext\src\Wevito.VNext.Core\CapturePolicyEvaluator.cs`
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~MediaFoundationVideoEncoderTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Manual: record a 5-second clip of Wevito; confirm clip.mp4 plays; confirm Wevito recording indicator was visible.

**Commits:**

1. `feat(shell): MediaFoundationVideoEncoder  [Phase: C-PHASE 13]`
2. `feat(shell): RecordingIndicatorWindow visible Wevito-drawn indicator  [Phase: C-PHASE 13]`
3. `feat(shell): WGC clip capture into MP4  [Phase: C-PHASE 13]`
4. `feat(core): screenCapture clip artifact  [Phase: C-PHASE 13]`
5. `test: cover clip encoding with fake encoder  [Phase: C-PHASE 13]`

**Push and PR:** open PR as **DRAFT** titled `C-PHASE 13 [DRAFT]: Short proof clips`. Send stop-and-ask message.

=== PROMPT END

## C-PHASE 14 — Translation Glossary + QA Review

=== PROMPT START

You are running C-PHASE 14.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 14".
2. `docs\TRANSLATION_AND_AUDIO_TOOL_RESEARCH_PLAN_2026-05-05.md`.
3. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 3.
4. `docs\CODE_SIDE_PHASE35_TRANSLATION_DEEPL_EXECUTION_ADAPTER_2026-05-05.md`.
5. `vnext\src\Wevito.VNext.Core\TranslationPreviewAdapter.cs`
6. `vnext\src\Wevito.VNext.Core\TranslationExecutionAdapter.cs`
7. `vnext\src\Wevito.VNext.Core\TranslationProviderRouter.cs`
8. `vnext\src\Wevito.VNext.Core\DeepLTranslationClient.cs`

**Master rules:** see C-PHASE 0. Translation default = none; document what powers it.

**Goal:** Glossary support and post-translation QA warnings.

**Branch:** `claude-implementation/c-phase-14-translation-glossary`

**Tasks:**

1. New `vnext\content\translation_glossaries.json`. Per language pair: `[{source, target, caseSensitive, notes}, ...]`.
2. Provide a starter glossary for English ↔ Spanish covering Wevito-specific terms (pet names, "PET TASKS", species names in their canonical English).
3. Extend `TranslationPreviewAdapter` to surface glossary entries that would apply.
4. Extend `TranslationExecutionAdapter`:
   - For DeepL Pro: use the native glossary API (create glossary on first use, cache id).
   - For DeepL Free or no glossary support: pre/post substitution shim.
   - Skip glossary substitution inside fenced code blocks (` ``` `) and inline code (`` ` ``).
5. Emit QA warnings (non-blocking) in result artifact:
   - placeholder drift (e.g. `{{name}}` rewritten)
   - markdown drift (`#`, `**`, etc. changed)
   - target empty
   - fallback used
   - glossary term not preserved
6. Tests: glossary substitution, code-block preservation, placeholder preservation, markdown preservation, QA warning emission.

**Files likely to edit:**

- New: `vnext\content\translation_glossaries.json`
- `vnext\src\Wevito.VNext.Core\TranslationPreviewAdapter.cs`
- `vnext\src\Wevito.VNext.Core\TranslationExecutionAdapter.cs`
- `vnext\src\Wevito.VNext.Core\DeepLTranslationClient.cs` (glossary API)
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~TranslationPreviewAdapterTests|FullyQualifiedName~TranslationExecutionAdapterTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "translate Hello goose to Spanish" -ExpectedToolFamily translateText -ExpectExecuteEnabledAfterPreview -SkipBuild
```

**Commits:**

1. `feat(content): translation glossaries starter data  [Phase: C-PHASE 14]`
2. `feat(core): glossary substitution in translation pipeline  [Phase: C-PHASE 14]`
3. `feat(core): post-translation QA warnings  [Phase: C-PHASE 14]`
4. `test: cover glossary + QA  [Phase: C-PHASE 14]`

**PR:** `C-PHASE 14: Translation glossary + QA review`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 15 — Translation Second Provider (LibreTranslate Local)

=== PROMPT START

You are running C-PHASE 15.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 15".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 3.
3. `vnext\src\Wevito.VNext.Core\TranslationProviderRouter.cs`
4. `vnext\src\Wevito.VNext.Core\DeepLTranslationClient.cs`

**Master rules:** see C-PHASE 0. Default provider = none.

**Goal:** Second translation provider — LibreTranslate localhost sidecar. User choice; nothing auto-installed.

**Branch:** `claude-implementation/c-phase-15-libretranslate-provider`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\LibreTranslateClient.cs` with `HttpClient` against `http://localhost:5000/translate` (configurable port).
2. Provider readiness probe: `GET /languages`. If success, mark `Available`.
3. Extend `TranslationProviderRouter` to expose multi-provider state: `Available`, `Configured`, `Default`, `UserSelected`.
4. First-use consent dialog: provider name, "text stays on your machine" disclosure, retention note.
5. Setup guide artifact: `vnext\artifacts\pet-tasks\<ts>-translatetext-libretranslate-setup\setup-guide.md` with install instructions linking https://github.com/LibreTranslate/LibreTranslate.
6. Tests with a fake LibreTranslate server (in-process `HttpListener`).

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\LibreTranslateClient.cs`
- `vnext\src\Wevito.VNext.Core\TranslationProviderRouter.cs`
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~LibreTranslateClientTests|FullyQualifiedName~TranslationProviderRouterTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

**Commits:**

1. `feat(core): LibreTranslateClient localhost sidecar  [Phase: C-PHASE 15]`
2. `feat(core): TranslationProviderRouter multi-provider state  [Phase: C-PHASE 15]`
3. `feat(core): first-use consent + setup guide  [Phase: C-PHASE 15]`
4. `test: cover LibreTranslate client + router  [Phase: C-PHASE 15]`

**PR:** `C-PHASE 15: Translation second provider (LibreTranslate)`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 16 — Audio External Boost Handoff

=== PROMPT START

You are running C-PHASE 16.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 16".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 2.
3. `docs\TRANSLATION_AND_AUDIO_TOOL_RESEARCH_PLAN_2026-05-05.md`.
4. `vnext\src\Wevito.VNext.Core\AudioAssistPreviewAdapter.cs`
5. `vnext\src\Wevito.VNext.Core\AudioAssistExecutionAdapter.cs`
6. `vnext\src\Wevito.VNext.Core\WindowsAudioEndpointStatusReader.cs`

**Master rules:** see C-PHASE 0. Driver/APO/external = guidance/handoff only.

**Goal:** Detect FxSound / Equalizer APO; generate setup guide; never install or edit configs.

**Branch:** `claude-implementation/c-phase-16-audio-boost-handoff`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\AudioBoostHandoffAdapter.cs`.
2. Extend audio task kinds to include `boostHandoff`.
3. Detection:
   - FxSound: check `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*` for FxSound DisplayName, plus `Get-Process fxsound` running, plus `C:\Program Files\FxSound\` exists.
   - Equalizer APO: check `C:\Program Files\EqualizerAPO\config\config.txt` exists.
4. Status: `Installed`, `Partial` (registry but no config), `NotInstalled`.
5. Setup guide: write `setup-guide.md` with status, official URL, reboot warning per research, config path. Include the WHO 80 dB(A) note + EBU R 128 -1 dBTP true-peak ceiling.
6. **Never** edit `config.txt`. **Never** trigger an install. **Never** elevate.
7. Tests with mocked filesystem detection.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\AudioBoostHandoffAdapter.cs`
- `vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs`
- `vnext\src\Wevito.VNext.Core\PetCommandParser.cs` (route "boost" / "equalizer" commands)
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~AudioBoostHandoffAdapterTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "help me boost my PC volume" -ExpectedToolFamily audioAssist -SkipBuild
```

**Commits:**

1. `feat(core): AudioBoostHandoffAdapter with FxSound/Equalizer APO detection  [Phase: C-PHASE 16]`
2. `feat(core): boost handoff task kind + setup guide artifact  [Phase: C-PHASE 16]`
3. `test: cover boost handoff detection states  [Phase: C-PHASE 16]`

**PR:** `C-PHASE 16: Audio external boost handoff`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 17 — Audio Boost Preset Planning

=== PROMPT START

You are running C-PHASE 17.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 17".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 2.
3. `vnext\src\Wevito.VNext.Core\AudioBoostHandoffAdapter.cs` (after C-PHASE 16).

**Master rules:** see C-PHASE 0. Plans only — user applies externally.

**Goal:** Generate conservative gain/EQ preset plans (text only).

**Branch:** `claude-implementation/c-phase-17-audio-boost-preset-plans`

**Tasks:**

1. Extend `AudioBoostHandoffAdapter` to emit `boost-plan.txt` next to `setup-guide.md`.
2. Three presets:
   - "Warmth": `Filter: ON LSC Fc 200 Hz Gain +1.5 dB Q 0.7` style.
   - "Speech clarity": small +2 dB at 2-4 kHz, gentle.
   - "Modest +3 dB safe boost": preamp +3 dB with explicit limiter note.
3. Each plan includes:
   - **WHO 80 dB(A) / 40 h/week safety note** verbatim from research (cite https://www.who.int/news-room/questions-and-answers/item/deafness-and-hearing-loss-safe-listening).
   - **EBU R 128 -1 dBTP true-peak ceiling** note (cite https://tech.ebu.ch/publications/r128).
   - "User applies manually; Wevito does not edit your config" disclaimer.
4. Cap any preset at +6 dB. Tests assert no plan exceeds.
5. Tests assert each plan contains the safety strings.

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Core\AudioBoostHandoffAdapter.cs`
- New tests.

**Validation:** focused tests, full tests, safe publish, probe.

**Commits:**

1. `feat(core): conservative boost preset plans + safety notes  [Phase: C-PHASE 17]`
2. `test: cap and safety-note assertions  [Phase: C-PHASE 17]`

**PR:** `C-PHASE 17: Audio boost preset planning`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 18 — buildProof Execution Allowlist + Fake Runner

=== PROMPT START

You are running C-PHASE 18.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 18".
2. `docs\CODE_SIDE_PHASE33_PROOF_EXECUTION_DESIGN_2026-05-05.md` — design source of truth.
3. `vnext\src\Wevito.VNext.Core\BuildProofPreviewAdapter.cs`.

**Master rules:** see C-PHASE 0. No real runner this phase.

**Goal:** Allowlist evaluator + fake runner. PET TASKS UI plumbing only — RUN stays disabled this phase.

**Branch:** `claude-implementation/c-phase-18-buildproof-execution-stub`

**Tasks:**

1. New contracts (in `Contracts\`):
   - `ProofExecutionCommand`
   - `ProofExecutionRequest`
   - `ProofExecutionManifest`
   - `ProofExecutionResult`
2. New `vnext\src\Wevito.VNext.Core\ProofExecutionAllowlistEvaluator.cs`.
3. New `vnext\src\Wevito.VNext.Core\ICommandRunner.cs`.
4. New `vnext\src\Wevito.VNext.Core\FakeCommandRunner.cs`.
5. Allowlist matches by exact-equality of executable + ordered argument list.
6. Hard-block list runs first: `Remove-Item`, `git reset`, `git checkout`, package install, browser automation, asset prep without `-SkipAssetPrep`.
7. Tests:
   - exact-match allowlist
   - blocked composition (semicolons, redirection)
   - blocked destructive
   - blocked asset-prep
   - `-SkipAssetPrep` enforcement
   - manifest round-trip
   - fake-runner integration

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs` (extend) or new file `ProofExecutionContracts.cs`
- New: `vnext\src\Wevito.VNext.Core\ProofExecutionAllowlistEvaluator.cs`
- New: `vnext\src\Wevito.VNext.Core\ICommandRunner.cs`
- New: `vnext\src\Wevito.VNext.Core\FakeCommandRunner.cs`
- New tests.

**Do not** enable RUN in `ToolPopupWindow.xaml.cs` for `buildProof` this phase.

**Validation:** focused tests, full tests, safe publish, probe (preview only).

**Commits:**

1. `feat(contracts): proof execution contracts  [Phase: C-PHASE 18]`
2. `feat(core): ProofExecutionAllowlistEvaluator + hard-block list  [Phase: C-PHASE 18]`
3. `feat(core): ICommandRunner + FakeCommandRunner  [Phase: C-PHASE 18]`
4. `test: cover allowlist matching, blocks, manifest, fake runner  [Phase: C-PHASE 18]`

**PR:** `C-PHASE 18: buildProof execution allowlist + fake runner`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 19 — buildProof Execution Real Runner

=== PROMPT START

You are running C-PHASE 19. **This is a STOP-AND-ASK phase.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 19".
2. `docs\CLAUDE_IMPLEMENTATION_RISK_REGISTER_2026-05-05.md` — R03, R13.
3. `vnext\src\Wevito.VNext.Core\ProofExecutionAllowlistEvaluator.cs` (after C-PHASE 18).
4. `vnext\src\Wevito.VNext.Core\ICommandRunner.cs`.
5. `vnext\src\Wevito.VNext.Core\FakeCommandRunner.cs`.

**Master rules:** see C-PHASE 0.

**Goal:** Real command execution for the four allowlisted commands. `Process.Kill(entireProcessTree: true)` on cancel.

**Branch:** `claude-implementation/c-phase-19-buildproof-execution-real`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\ProcessCommandRunner.cs`. `System.Diagnostics.Process` with explicit `WorkingDirectory`, `RedirectStandardOutput/Error`, timeout, cancellation token.
2. Pre-run: snapshot BLAKE3 (or SHA256 if BLAKE3 NuGet unavailable) hash of every file under `vnext\src`, `vnext\tests`, `tools`, `scripts`, `sprites_*`, `vnext\content`. Store snapshot in manifest.
3. Post-run: re-hash. If any tracked file changed, mark `MutationDetected = true` and STOP — do not advance to RUN.
4. Probe commands write only to `vnext\artifacts\` — assert this in post-run check.
5. `ToolPopupWindow.xaml.cs` — enable RUN for reviewed `buildProof` cards.
6. Tests:
   - cancellation
   - timeout
   - `MutationDetected` triggers stop
7. Live probe with a harmless command (add `dotnet --version` to test allowlist only — production allowlist stays as in design doc).

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\ProcessCommandRunner.cs`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs` (RUN handler for buildProof)
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~ProcessCommandRunnerTests|FullyQualifiedName~BuildProofExecutionTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "run a build proof" -ExpectedToolFamily buildProof -ApproveBeforePreview -ExpectExecuteEnabledAfterPreview -SkipBuild
```

Manual: in app, click RUN on a reviewed buildProof card. Confirm command executes, log file is written, no mutation.

**Commits:**

1. `feat(core): ProcessCommandRunner with hash-snapshot mutation detection  [Phase: C-PHASE 19]`
2. `feat(shell): RUN gate for reviewed buildProof  [Phase: C-PHASE 19]`
3. `test: cancellation, timeout, mutation detection  [Phase: C-PHASE 19]`

**Push and PR:** open as **DRAFT** `C-PHASE 19 [DRAFT]: buildProof execution real runner`. Send stop-and-ask message.

=== PROMPT END

## C-PHASE 20 — Sprite Workflow V2 Read-Only Auditor

=== PROMPT START

You are running C-PHASE 20.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 20".
2. `docs\WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md`.
3. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 5.
4. `vnext\src\Wevito.VNext.Contracts\SpriteWorkflowContracts.cs`.

**Master rules:** see C-PHASE 0. Read-only this phase.

**Goal:** First slice of Sprite Workflow V2 inside Wevito (not a separate app). One-screen layout. Read-only.

**Branch:** `claude-implementation/c-phase-20-sprite-workflow-v2-readonly`

**Tasks:**

1. New WPF window `vnext\src\Wevito.VNext.Shell\SpriteWorkflowV2Window.xaml`. Summoned from Tool Hub via a button or command.
2. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowManifestReader.cs`. Reads `sprites_runtime`, `sprites_authored`, `sprites_authored_verified` and emits a queue of rows (one per species/age/gender/color).
3. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowContactSheetGenerator.cs`. Use SkiaSharp (NuGet `SkiaSharp` 2.88+) — confirm cross-platform won't break Broker/AutomationRunner (likely fine; SkiaSharp ships in Shell only).
4. Window layout:
   - Left: queue (filterable).
   - Center: source / runtime / candidate / proof strips for selected row.
   - Right: validator findings + provenance (BLAKE3 hashes, manifest entries).
5. **Read-only**: no apply, no rollback, no candidate import.
6. Selecting a queue row populates strips from disk.
7. Tests cover manifest read, queue building, contact-sheet generation with fake source.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Shell\SpriteWorkflowV2Window.xaml` + `.xaml.cs`
- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowManifestReader.cs`
- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowContactSheetGenerator.cs`
- `vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs` (open V2 window)
- `vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj` (add SkiaSharp NuGet — STOP AND ASK if cascade)
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~SpriteWorkflowManifestReaderTests|FullyQualifiedName~SpriteWorkflowContactSheetGeneratorTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Manual: open Sprite Workflow V2 window; pick a queue row (e.g. goose/baby/female/blue/idle); verify strips render; verify provenance shows BLAKE3 hashes.

**Commits:**

1. `feat(core): SpriteWorkflowManifestReader  [Phase: C-PHASE 20]`
2. `feat(core): SpriteWorkflowContactSheetGenerator (SkiaSharp)  [Phase: C-PHASE 20]`
3. `feat(shell): SpriteWorkflowV2Window read-only one-screen layout  [Phase: C-PHASE 20]`
4. `test: cover manifest reader + contact sheet generation  [Phase: C-PHASE 20]`

**PR:** `C-PHASE 20: Sprite Workflow V2 read-only auditor`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 21 — Sprite Workflow V2 Candidate Import + Dry-Run Apply

=== PROMPT START

You are running C-PHASE 21.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 21".
2. `vnext\src\Wevito.VNext.Shell\SpriteWorkflowV2Window.xaml(.cs)` (after C-PHASE 20).
3. `vnext\src\Wevito.VNext.Core\SpriteWorkflowManifestReader.cs`.

**Master rules:** see C-PHASE 0. No mutation this phase.

**Goal:** Candidate import + dry-run apply. Mutation still blocked.

**Branch:** `claude-implementation/c-phase-21-sprite-workflow-v2-import-dryrun`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowCandidateImporter.cs`. Copies candidate PNGs from a user-picked folder into `sprites_authored\<species>\<row>\.candidates\<ts>\`.
2. **Never** overwrite source/runtime/verified — assert in tests.
3. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowDryRunApplyService.cs`. Produces a planned-changes manifest:
   - what files would be overwritten
   - what files would be backed up
   - what hashes would change
4. Dry-run does **not** actually move/copy.
5. UI: candidate import button + dry-run button + dry-run preview panel.
6. Tests: import safety (no overwrite of runtime/verified), dry-run plan correctness, candidate path prefix assertion.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowCandidateImporter.cs`
- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowDryRunApplyService.cs`
- `vnext\src\Wevito.VNext.Shell\SpriteWorkflowV2Window.xaml` + `.xaml.cs` (extension)
- New tests.

**Validation:** focused tests, full tests, safe publish.

**Commits:**

1. `feat(core): SpriteWorkflowCandidateImporter (writes only to .candidates)  [Phase: C-PHASE 21]`
2. `feat(core): SpriteWorkflowDryRunApplyService planned-changes manifest  [Phase: C-PHASE 21]`
3. `feat(shell): import + dry-run UI in V2  [Phase: C-PHASE 21]`
4. `test: candidate import safety + dry-run correctness  [Phase: C-PHASE 21]`

**PR:** `C-PHASE 21: V2 candidate import + dry-run apply`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 22 — Sprite Workflow V2 Backup-Apply + Rollback + Post-Proof

=== PROMPT START

You are running C-PHASE 22. **This is a STOP-AND-ASK phase. First PNG mutation.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 22".
2. `docs\CLAUDE_IMPLEMENTATION_RISK_REGISTER_2026-05-05.md` — R01, R21, R28, R30.
3. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 5.
4. `vnext\src\Wevito.VNext.Core\SpriteWorkflowDryRunApplyService.cs` (after C-PHASE 21).

**Master rules:** see C-PHASE 0. Mutation is gated; backup + rollback + post-proof mandatory.

**Goal:** Real one-row apply with backup + rollback + post-apply proof.

**Branch:** `claude-implementation/c-phase-22-sprite-workflow-v2-apply-rollback`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowApplyService.cs`. Apply flow:
   - Hash existing files (BLAKE3).
   - Reject apply if `sprites_runtime` and `.staging` are on different volumes (call `Path.GetPathRoot`; if different, error).
   - Copy existing → `sprites_runtime\.backup\<row-id>-<ts>\`.
   - Copy candidate → `sprites_runtime\.staging\<row-id>-<ts>\`.
   - `File.Move(staging, runtime, overwrite: true)` per PNG.
   - Write `apply.json` log: `{rowId, timestamp, oldHashes, newHashes, backupPath}`.
2. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowRollbackService.cs`. Restore from `.backup\<row-id>-<ts>\` by hash.
3. New `vnext\src\Wevito.VNext.Core\SpriteWorkflowPostApplyProof.cs`. After apply: re-run `audit_sprite_contract.py` and `report_runtime_canvas_mismatches.py`. If new errors, auto-rollback.
4. UI: apply button (confirm typed-in row id before enabling), rollback button.
5. Limit: one row per apply call.
6. Backup retention: keep last 50.
7. Tests:
   - apply happy path (with fake row)
   - rollback
   - post-proof failure → auto-rollback
   - same-volume check
   - typed-confirmation gate

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowApplyService.cs`
- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowRollbackService.cs`
- New: `vnext\src\Wevito.VNext.Core\SpriteWorkflowPostApplyProof.cs`
- `vnext\src\Wevito.VNext.Shell\SpriteWorkflowV2Window.xaml(.cs)` (apply/rollback UI)
- New tests (extensive).

**Do not run a real apply on a real row in this phase.** That's C-PHASE 23.

**Validation:** focused tests, full tests, safe publish. Run apply/rollback only on a test fixture row.

**Commits:**

1. `feat(core): SpriteWorkflowApplyService backup-then-replace + same-volume guard  [Phase: C-PHASE 22]`
2. `feat(core): SpriteWorkflowRollbackService restore by hash  [Phase: C-PHASE 22]`
3. `feat(core): SpriteWorkflowPostApplyProof + auto-rollback  [Phase: C-PHASE 22]`
4. `feat(shell): apply/rollback UI with typed confirmation  [Phase: C-PHASE 22]`
5. `test: apply, rollback, post-proof, same-volume, retention  [Phase: C-PHASE 22]`

**Push and PR:** open as **DRAFT** `C-PHASE 22 [DRAFT]: V2 backup-apply + rollback + post-proof`. Send stop-and-ask. This phase enables PNG mutation — no real row applied yet.

=== PROMPT END

## C-PHASE 23 — Optional Animation drop_ball One-Row Pilot Through V2

=== PROMPT START

You are running C-PHASE 23. **This is a STOP-AND-ASK phase. First real PNG mutation.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 23".
2. `docs\WEVITO_GOOSE_HOLD_BALL_ACCEPTED_ENDPOINT_2026-05-04.md`.
3. `docs\WEVITO_PICKUP_DROP_TRANSITION_VISUAL_PLAN_2026-05-04.md`.
4. `docs\WEVITO_OPTIONAL_EXPANSION_APPLY_PROOF_HANDOFF_2026-05-05.md`.

**Master rules:** see C-PHASE 0. Visual-side must approve before merge.

**Goal:** Push the planned `goose / baby / female / blue / drop_ball` one-row pilot through V2.

**Branch:** `claude-implementation/c-phase-23-drop-ball-pilot`

**Tasks:**

1. Confirm visual-side has provided candidate frames (check `vnext\artifacts\visual-review\` and `incoming_sprites\` for handoff packets). If absent, STOP AND ASK.
2. Run V2: import → dry-run → review → apply → post-proof.
3. **Manually trigger rollback** after apply, then re-apply, to confirm rollback path works on a real row.
4. Capture screenshots of every step into `vnext\artifacts\c-phase-23-drop-ball-pilot\<step>.png`.
5. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` "Sprites/visuals" row to ~85%.
6. Re-export PDF dashboard.

**Files affected:**

- PNGs in `sprites_runtime\goose\baby\female\blue\drop_ball\` (via V2 only).
- `sprites_runtime\.backup\<row-id>-<ts>\` (V2-managed).
- `sprites_runtime\.staging\<row-id>-<ts>\` (cleaned at end).
- `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`.
- Dashboard PDFs.

**Validation:** V2 post-proof = green. `audit_optional_animation_readiness.py` shows `drop_ball` for that row as ready.

**Commits:**

1. `chore(visual): drop_ball candidate import to .candidates  [Phase: C-PHASE 23]`
2. `feat(visual): drop_ball pilot apply via V2  [Phase: C-PHASE 23]`
3. `chore(visual): drop_ball pilot screenshots  [Phase: C-PHASE 23]`
4. `docs: bump sprite/visual completion in dashboard  [Phase: C-PHASE 23]`
5. `docs: regenerate dashboard PDFs  [Phase: C-PHASE 23]`

**Push and PR:** open as **DRAFT** `C-PHASE 23 [DRAFT]: drop_ball one-row pilot through V2`. Stop-and-ask: visual-side must sign off before merge.

=== PROMPT END

## C-PHASE 24 — Creative Learning Lab Read-Only Artifact Index

=== PROMPT START

You are running C-PHASE 24.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 24".
2. `docs\WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md`.

**Master rules:** see C-PHASE 0. Read-only.

**Goal:** Read-only first slice of the Lab.

**Branch:** `claude-implementation/c-phase-24-learning-lab-readonly`

**Tasks:**

1. New WPF window `vnext\src\Wevito.VNext.Shell\CreativeLearningLabWindow.xaml`. Summoned from Tool Hub.
2. New `vnext\src\Wevito.VNext.Core\LearningLabArtifactIndexer.cs`. Scans `vnext\artifacts\visual-review\` and `vnext\artifacts\animation-runs\`. Indexes markdown/JSON manifests.
3. UI:
   - Top metrics: raw / cleaned / labeled / bundled / eval (all 0 initially except indexed counts).
   - Review queue panel.
   - Bundle panel.
   - Eval panel (placeholders).
4. Read-only. No labels written yet.
5. Tests cover indexer + tolerant manifest read.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Shell\CreativeLearningLabWindow.xaml` + `.xaml.cs`
- New: `vnext\src\Wevito.VNext.Core\LearningLabArtifactIndexer.cs`
- `vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs` (open Lab window)
- New tests.

**Validation:** focused tests, full tests, safe publish, screenshot of Lab window.

**Commits:**

1. `feat(core): LearningLabArtifactIndexer  [Phase: C-PHASE 24]`
2. `feat(shell): CreativeLearningLabWindow read-only dashboard  [Phase: C-PHASE 24]`
3. `test: cover indexer tolerance  [Phase: C-PHASE 24]`

**PR:** `C-PHASE 24: Creative Learning Lab read-only artifact index`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 25 — Creative Learning Lab Labels + Bundles + Eval Gates

=== PROMPT START

You are running C-PHASE 25.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 25".
2. `docs\WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md` — bundle gates and labels.
3. `vnext\src\Wevito.VNext.Shell\CreativeLearningLabWindow.xaml(.cs)` (after C-PHASE 24).

**Master rules:** see C-PHASE 0. Still no training/export.

**Goal:** Manual labeling + bundle readiness gates.

**Branch:** `claude-implementation/c-phase-25-learning-lab-labels-bundles`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\LearningLabLabelStore.cs`. SQLite at `%LOCALAPPDATA%\Wevito\learning_lab.db`. Schema: `examples (id INTEGER PRIMARY KEY, source_path TEXT, label TEXT, reviewer TEXT, notes TEXT, created_at TEXT, schema_version INTEGER)`.
2. Labels: `accept`, `reject`, `revise`, `defer`, `blocked`.
3. New `vnext\src\Wevito.VNext.Core\LearningLabBundleService.cs`. Bundle gate per learning-lab plan doc.
4. UI: label dropdown per queue row, bundle preview panel.
5. Eval bench placeholders surface a list, no benchmarks run.
6. Tests cover label CRUD, bundle gate, schema migration (`schema_version` round-trip).

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\LearningLabLabelStore.cs`
- New: `vnext\src\Wevito.VNext.Core\LearningLabBundleService.cs`
- `vnext\src\Wevito.VNext.Shell\CreativeLearningLabWindow.xaml` + `.xaml.cs` (extension)
- New tests.

**Validation:** focused tests, full tests, safe publish.

**Commits:**

1. `feat(core): LearningLabLabelStore (sqlite)  [Phase: C-PHASE 25]`
2. `feat(core): LearningLabBundleService gate logic  [Phase: C-PHASE 25]`
3. `feat(shell): label + bundle UI in Lab  [Phase: C-PHASE 25]`
4. `test: label CRUD + bundle gate + schema  [Phase: C-PHASE 25]`

**PR:** `C-PHASE 25: Lab labels + bundles + eval gates`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 26 — Pet AI Agent Pilot (Single Helper, Read-Only)

=== PROMPT START

You are running C-PHASE 26. **This is a STOP-AND-ASK phase. First model call.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 26".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 4.
3. `docs\CLAUDE_IMPLEMENTATION_RISK_REGISTER_2026-05-05.md` — R02, R16, R17.
4. `docs\WEVITO_PET_AGENT_TEXT_BAR_PLAN_2026-05-05.md`.

**Master rules:** see C-PHASE 0. Personality affects presentation, never permission. Default model adapter = none.

**Goal:** Connect one helper pet to a real model adapter for richer suggestions on top of existing tool outputs. Read-only tasks only.

**Branch:** `claude-implementation/c-phase-26-pet-agent-pilot`

**Tasks:**

1. New `vnext\src\Wevito.VNext.Core\IModelAdapter.cs`. Method: `Task<ModelResponse> SuggestAsync(ModelRequest, CancellationToken)`.
2. New `vnext\src\Wevito.VNext.Core\AnthropicModelAdapter.cs` (default), with `IModelAdapter` shape so OpenAI/local can be swapped later.
3. Default = none. No provider configured by default. First-call consent dialog (provider, what is sent, retention, training, link to provider's privacy doc).
4. API key in Windows Credential Manager (DPAPI). Never plaintext config. Use `Microsoft.AspNetCore.DataProtection` or direct `CredWriteW` P/Invoke.
5. Per-helper allowlist (research-driven, deny-wins):
   - `Scout`: localDocs, codeReview (read untrusted external content allowed; outbound network = model only)
   - `Inspector`: spriteAudit, petState (sprite-only data; no untrusted external)
   - `Builder`: codePatchPlan, codeReview (no untrusted external)
6. Approval gate before any first model call.
7. Audit log per call: `vnext\artifacts\pet-tasks\<ts>-<helper>-<tool>\model-call.json` with provider, args hash, decision, latency.
8. Untrusted-content tagging: any web/file content fetched by the tool is wrapped in `<untrusted>` tags before reaching the model.
9. Lethal-trifecta separation enforced: tests assert no single helper has all three of read-untrusted-external, read-private, network-send.
10. Tests with a fake model adapter.

**Files likely to edit:**

- New: `vnext\src\Wevito.VNext.Core\IModelAdapter.cs`
- New: `vnext\src\Wevito.VNext.Core\AnthropicModelAdapter.cs`
- New: `vnext\src\Wevito.VNext.Core\HelperAllowlistEvaluator.cs`
- `vnext\src\Wevito.VNext.Core\LocalDocsPreviewAdapter.cs` (append model summary if helper has adapter)
- `vnext\src\Wevito.VNext.Core\SpriteAuditPreviewAdapter.cs` (same)
- `vnext\src\Wevito.VNext.Core\CodeReviewPreviewAdapter.cs` (same)
- `vnext\src\Wevito.VNext.Core\CodePatchPlanPreviewAdapter.cs` (same)
- `vnext\content\tool_definitions.json` (add per-helper model-adapter capability)
- New tests.

**Validation:**

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~AnthropicModelAdapterTests|FullyQualifiedName~HelperAllowlistEvaluatorTests|FullyQualifiedName~LethalTrifectaTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Live: do not make a real model call this phase unless user explicitly approves on the stop-and-ask. Tests use fake adapter.

**Commits:**

1. `feat(core): IModelAdapter + AnthropicModelAdapter  [Phase: C-PHASE 26]`
2. `feat(core): HelperAllowlistEvaluator (deny-wins, per-pet)  [Phase: C-PHASE 26]`
3. `feat(core): per-tool model summary append for allowed helpers  [Phase: C-PHASE 26]`
4. `feat(content): per-helper model-adapter capability declaration  [Phase: C-PHASE 26]`
5. `test: lethal-trifecta separation, allowlist, fake adapter  [Phase: C-PHASE 26]`

**Push and PR:** open as **DRAFT** `C-PHASE 26 [DRAFT]: Pet AI agent pilot (single helper, read-only)`. Send stop-and-ask. First real model call must be user-approved.

=== PROMPT END

## C-PHASE 27 — Pet AI Agent Three-Helper Routing + sqlite-vec Memory

=== PROMPT START

You are running C-PHASE 27.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 27".
2. `docs\CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md` — section 4.
3. `vnext\src\Wevito.VNext.Core\IModelAdapter.cs` (after C-PHASE 26).
4. `vnext\src\Wevito.VNext.Core\HelperAllowlistEvaluator.cs`.

**Master rules:** see C-PHASE 0.

**Goal:** Three-helper routing + per-pet preference memory using sqlite-vec.

**Branch:** `claude-implementation/c-phase-27-three-helper-memory`

**Tasks:**

1. Add NuGet `sqlite-vec` (or pinned wrapper). STOP AND ASK if it cascades.
2. New `vnext\src\Wevito.VNext.Core\PetMemoryStore.cs`. One DB file per pet at `%LOCALAPPDATA%\Wevito\memory\<pet-id>.db`. Schema: `examples (id INTEGER PK, kind TEXT, content TEXT, label TEXT, embedding BLOB, created_at TEXT)`. WAL mode. Vector column via sqlite-vec.
3. Embeddings via the model adapter. Cache.
4. Memory writes are themselves a gated tool (validating gate).
5. Routing: when a command comes in unaddressed, the router asks each pet's memory for top-K relevant examples (vector similarity); pick helper with strongest match; fallback to round-robin.
6. Self-healing on startup: integrity check; if corrupt, rename file and start fresh; surface "memory rebuilt" notification.
7. Tests: memory CRUD, routing, gating, integrity-check rebuild.

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj` (NuGet)
- New: `vnext\src\Wevito.VNext.Core\PetMemoryStore.cs`
- `vnext\src\Wevito.VNext.Core\PetCommandBarService.cs` (memory-driven routing)
- `vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs` (memory write gating)
- New tests.

**Validation:** focused tests, full tests, safe publish.

**Commits:**

1. `chore(deps): add sqlite-vec NuGet  [Phase: C-PHASE 27]`
2. `feat(core): PetMemoryStore with sqlite-vec + integrity self-heal  [Phase: C-PHASE 27]`
3. `feat(core): memory-driven helper routing  [Phase: C-PHASE 27]`
4. `feat(core): memory write as gated tool  [Phase: C-PHASE 27]`
5. `test: memory CRUD, routing, integrity rebuild  [Phase: C-PHASE 27]`

**PR:** `C-PHASE 27: Three-helper routing + sqlite-vec memory`. Not stop-and-ask, but verify NuGet doesn't cascade.

=== PROMPT END

## C-PHASE 28 — Aging / Death / Ghost Lifecycle Visible Flow

=== PROMPT START

You are running C-PHASE 28.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 28".
2. `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — current lifecycle.
3. `scripts\pet_data.gd` — Godot lifecycle.
4. `scripts\game_manager.gd`.

**Master rules:** see C-PHASE 0.

**Goal:** Complete visible lifecycle: aging, death, ghost variant fallback, memorial spawn.

**Branch:** `claude-implementation/c-phase-28-lifecycle`

**Tasks:**

1. Aging stages: baby → teen → adult → senior (new). Senior uses adult sprites with optional desaturation overlay.
2. Death: clear visual flow — `sad` animation → dim → transition to ghost variant if available, else fade out.
3. Ghost variant: only render if art exists for the row; else generic ghost overlay.
4. Memorial spawn: drop a `memorial_object` at death location. Persists for one in-game day.
5. Save/load migration: schema `version` field bumped. Forward-only migration. Fall back to defaults for missing fields.
6. Tests: aging tick, death trigger, ghost fallback, memorial spawn, save/load round-trip with v1 → v2.

**Files likely to edit:**

- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs`
- `vnext\src\Wevito.VNext.Core\AppRepository.cs` (save schema)
- `vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs` (death flow rendering)
- `scripts\pet_data.gd`, `scripts\pet.gd`, `scripts\game_manager.gd`
- New tests.

**Validation:** focused tests, full tests, safe publish, Godot manual scenario for death.

**Commits:**

1. `feat(sim): senior age stage + aging tick  [Phase: C-PHASE 28]`
2. `feat(sim): death visual flow with ghost fallback  [Phase: C-PHASE 28]`
3. `feat(sim): memorial spawn  [Phase: C-PHASE 28]`
4. `feat(repo): save schema v2 with forward migration  [Phase: C-PHASE 28]`
5. `feat(godot): mirror lifecycle changes  [Phase: C-PHASE 28]`
6. `test: aging, death, ghost, memorial, save migration  [Phase: C-PHASE 28]`

**PR:** `C-PHASE 28: Aging / death / ghost lifecycle`. Not stop-and-ask.

=== PROMPT END

## C-PHASE 29 — Final QA Sweep

=== PROMPT START

You are running C-PHASE 29.

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 29".
2. `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` — current state.
3. `docs\RUNTIME_CANVAS_NORMALIZATION_PHASE3_DRY_RUN_2026-05-04.md` — for canvas reconciliation, if needed.

**Master rules:** see C-PHASE 0. No new feature work.

**Goal:** Full release-readiness sweep.

**Branch:** `claude-implementation/c-phase-29-final-qa`

**Tasks:**

1. Run every probe (fresh artifact folders). Save summaries.
2. Run Godot in-game forced scenarios (if Godot is available) for every species/age/gender × {feed, water, drink, play (fetch), groom, bath, medicine, doctor, rest}.
3. Capture screenshots: every habitat, every pilot animation, every UI surface, every helper-pet card state.
4. Re-run `report_runtime_canvas_mismatches.py`. Target = 0 mismatches.
5. If non-zero: STOP AND ASK before applying broad normalization. Reference the dry-run doc.
6. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` to reflect current state. Most rows should be 90%+.
7. Re-export PDF dashboard.

**Files likely to edit:**

- `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- Dashboard PDFs (regenerated)
- Maybe canvas-normalized runtime PNGs (only with stop-and-ask approval)
- New artifacts under `vnext\artifacts\c-phase-29-final-qa\`

**Validation:** every probe green, every scenario captured, dashboard updated.

**Commits:**

1. `chore(qa): final probe sweep artifacts  [Phase: C-PHASE 29]`
2. `chore(qa): final scenario screenshots  [Phase: C-PHASE 29]`
3. `docs: bump dashboard to release-candidate state  [Phase: C-PHASE 29]`
4. `docs: regenerate dashboard PDFs  [Phase: C-PHASE 29]`

**PR:** `C-PHASE 29: Final QA sweep`. Not stop-and-ask unless canvas reconciliation needed.

=== PROMPT END

## C-PHASE 30 — Packaging + Final User Help Guide + Release Tag

=== PROMPT START

You are running C-PHASE 30. **This is a STOP-AND-ASK phase. Release tag.**

Repo: `C:\Users\fishe\Documents\projects\wevito`. OS: Windows.

**Read these first:**

1. `docs\CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md` — "C-PHASE 30".
2. `docs\CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md` — "Tagging".
3. `tools\build-release.ps1`.
4. `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` (after C-PHASE 29).

**Master rules:** see C-PHASE 0.

**Goal:** Package Wevito for shipping. Tag a release.

**Branch:** `claude-implementation/c-phase-30-release`

**Tasks:**

1. Verify `tools\build-release.ps1` runs end-to-end. If broken, fix.
2. Run full test suite under `Release`. STOP AND ASK if any test fails in Release that passes in Debug.
3. Run safe publish without `-SkipAssetPrep` (only if all visual cleanup is complete and main repo canvas is green from C-PHASE 29). If canvas not green, STOP AND ASK.
4. Write `docs\WEVITO_USER_HELP_GUIDE_2026-05-05.md`:
   - Install
   - Controls (keyboard / mouse)
   - PET TASKS basics
   - Helper pets (Scout / Inspector / Builder, addressing)
   - Settings (translation provider, audio, capture, model)
   - Privacy disclosures (capture + Recall, translation + provider, audio + APO/FxSound, model + provider)
   - Troubleshooting (probe failures, canvas, sprite workflow rollback, memory rebuild)
5. Tag `v0.1.0-claude-rollup` (or whatever version the user picks; ask first).
6. Update dashboard to "Release Candidate".

**Files likely to edit:**

- `tools\build-release.ps1` (if needed)
- New: `docs\WEVITO_USER_HELP_GUIDE_2026-05-05.md`
- `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- Dashboard PDFs

**Validation:**

```text
dotnet build .\vnext\Wevito.VNext.sln -c Release
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1
```

Manual: install on a fresh machine if possible.

**Commits:**

1. `chore(tools): verify build-release.ps1  [Phase: C-PHASE 30]`
2. `docs: WEVITO_USER_HELP_GUIDE_2026-05-05.md  [Phase: C-PHASE 30]`
3. `docs: bump dashboard to RC  [Phase: C-PHASE 30]`
4. `docs: regenerate dashboard PDFs  [Phase: C-PHASE 30]`

**Push and PR:** open as **DRAFT** `C-PHASE 30 [DRAFT]: Release packaging + user help guide`. Stop-and-ask before tag.

After user approves and PR merges, tag from `main`:

```bash
git checkout main
git pull --ff-only
git tag -a v0.1.0-claude-rollup -m "Wevito Claude rollup release"
git push origin v0.1.0-claude-rollup
```

=== PROMPT END

# End

For the master plan, see `CLAUDE_FULL_WEVITO_IMPLEMENTATION_PLAN_2026-05-05.md`. For commit/push rules, see `CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md`. For risks, see `CLAUDE_IMPLEMENTATION_RISK_REGISTER_2026-05-05.md`. For research sources, see `CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md`.
