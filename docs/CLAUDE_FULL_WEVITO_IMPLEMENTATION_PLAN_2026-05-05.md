# Claude Full Wevito Implementation Plan

Date: 2026-05-05
Author: Claude master review
Baseline commit: `c1d0a5cc9 Add polished project dashboard packet` on `main`

## Purpose

This document is the master implementation roadmap for finishing Wevito.

It is written so a medium-intelligence Codex model can pick up any phase, read only the docs and files referenced in that phase, and execute it without re-reading the entire conversation.

It is the source-of-truth phase ordering. The companion `CLAUDE_CODEX_MEDIUM_PHASE_PROMPTS_2026-05-05.md` contains copy-paste prompts for each phase. The companion `CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md` defines exactly when to commit and push.

## Reading Order For Codex

When Codex starts any phase it should read, in order:

1. This file (the relevant phase section).
2. `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` (current high-level status).
3. The phase-specific docs referenced in that phase.
4. The phase-specific source files referenced in that phase.

Codex should not read every doc in `docs\` before starting. Use only what the phase lists.

## Architectural Rules That Apply To Every Phase

These rules override anything ambiguous in older phase docs.

```text
1. Game runtime, visual asset mutation, tool execution, and AI/ML learning
   are four separate concerns. Do not entangle them.
2. PET TASKS work happens under the hood. Pets stay as normal pet-sim
   overlay animals visually. No special task-completion / review / failure
   pet animations.
3. Three pet AI helpers max. Their animal personalities stay simple and
   game-like. Their tool capability under the hood can be powerful, but
   personality never grants permission.
4. Sprite Workflow V2 is simple, compact, one-result oriented. No giant
   multi-pane scroll workbench.
5. Creative Learning Lab starts as reviewed examples + preference memory,
   not uncontrolled training.
6. Screenshot/screen recording is privacy-safe and approval-gated. Default
   capture indicator stays on. Wevito's own sensitive surfaces use
   SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE).
7. Audio booster functionality is safe. Normal Windows volume is fine.
   Driver/APO/external booster setup is guidance or explicit handoff
   unless a safe path is proven.
8. Translation clearly documents what powers it and whether text leaves
   the machine. Default provider = none until user picks.
9. Coding tools start with codeReview and codePatchPlan before approved
   patch execution. buildProof execution stays behind a fake-runner
   stub phase before real commands.
10. PET TASKS execution adapters are report-only first, approval-gated
    second, executing third. Each step is its own phase with its own
    commit checkpoint.
11. Every mutation-capable phase has: backup-before-apply, hash
    verification, exact rollback, post-apply proof, audit log entry.
12. No phase ships without: build green, full tests green, safe Debug
    publish green, and (where applicable) a live PET TASKS probe.
```

## Phase Numbering Convention

Phases here are labelled `C-PHASE N`. The C- prefix distinguishes them from the historical `CODE_SIDE_PHASE_N` docs that describe already-completed work. Historical phase docs remain valid context but should not be re-executed.

Phase numbering starts at 0 (preflight) and ends at 28 (release). Phases are kept small on purpose. A phase that touches more than three projects in `vnext/src` or more than one runtime asset folder must be split.

## Branch Strategy Summary

(See `CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md` for full detail.)

- Default branch: `claude-implementation/<phase-id>-<slug>` per phase.
- Each phase commits at logical checkpoints, then pushes the branch and opens a PR into `main`.
- `main` is fast-forwarded only after build/tests/probe pass and a human approves.
- A phase that reaches a stop-and-ask condition pushes its current branch and stops without merging.

## Validation Commands Cheatsheet

These commands recur in many phases. Run from repo root unless noted.

```text
# Build
dotnet build .\vnext\Wevito.VNext.sln

# Full tests
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build

# Focused tests (replace filter)
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~<TestClassName>"

# Safe Debug publish (must keep -SkipAssetPrep while visual cleanup is active)
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

# PET TASKS live probe
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "<command>" -ExpectedToolFamily <family> -SkipBuild

# Sprite contract / canvas / readiness
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\<phase>-sprite-contract.json
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\<phase>-canvas.json --markdown .\vnext\artifacts\<phase>-canvas.md
python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\<phase>-optional.json --markdown .\vnext\artifacts\<phase>-optional.md
```

If any of these change interfaces, the phase that changes them must update this section.

## Master Phase Map

```text
M0  Foundation:        C-PHASE 0   Preflight baseline check
M0  Foundation:        C-PHASE 1   Action / Animation / Prop unified contract
M0  Foundation:        C-PHASE 2   Auto-care visible + drink animation routing
M0  Foundation:        C-PHASE 3   Staged fetch sequence controller
M0  Foundation:        C-PHASE 4   Care / item content mapping
M1  Habitat & World:   C-PHASE 5   Habitat object manifest schema
M1  Habitat & World:   C-PHASE 6   Habitat 5-species pilot runtime placement
M1  Habitat & World:   C-PHASE 7   Habitat expansion to remaining 5 species
M2  Tool Hub UX:       C-PHASE 8   Tool Hub Phase-26 IA refactor
M2  Tool Hub UX:       C-PHASE 9   Three-helper roster + pet command bar @addressing
M2  Tool Hub UX:       C-PHASE 10  Artifact controls (Open Report / Copy Path / Open Folder)
M3  Tool Surfaces:     C-PHASE 11  Wevito-window screenshot execution
M3  Tool Surfaces:     C-PHASE 12  Region + last-region screenshot
M3  Tool Surfaces:     C-PHASE 13  Short proof clips (5-10s, WGC + MF SinkWriter)
M3  Tool Surfaces:     C-PHASE 14  Translation glossary + QA review
M3  Tool Surfaces:     C-PHASE 15  Translation second provider (LibreTranslate local)
M3  Tool Surfaces:     C-PHASE 16  Audio external boost handoff (Equalizer APO / FxSound)
M3  Tool Surfaces:     C-PHASE 17  Audio boost preset planning (text plan only)
M3  Tool Surfaces:     C-PHASE 18  buildProof execution allowlist + fake runner
M3  Tool Surfaces:     C-PHASE 19  buildProof execution real runner (gated)
M4  Sprite Workflow:   C-PHASE 20  Sprite Workflow V2 read-only auditor
M4  Sprite Workflow:   C-PHASE 21  Sprite Workflow V2 candidate import + dry-run apply
M4  Sprite Workflow:   C-PHASE 22  Sprite Workflow V2 backup-apply + rollback + post-proof
M4  Sprite Workflow:   C-PHASE 23  Optional animation drop_ball one-row pilot through V2
M5  Learning + Agent:  C-PHASE 24  Creative Learning Lab read-only artifact index
M5  Learning + Agent:  C-PHASE 25  Creative Learning Lab labels + bundles + eval gates
M5  Learning + Agent:  C-PHASE 26  Pet AI agent pilot (single helper, read-only)
M5  Learning + Agent:  C-PHASE 27  Pet AI agent three-helper routing + sqlite-vec memory
M6  Release:           C-PHASE 28  Aging / death / ghost lifecycle visible flow
M6  Release:           C-PHASE 29  Final QA sweep (canvas reconciliation + scenario sweeps)
M6  Release:           C-PHASE 30  Packaging + final user help guide + release tag
```

This is the canonical order. Some phases can be parallelized (e.g. C-PHASE 5 and C-PHASE 8 do not overlap), but Codex should default to sequential execution unless the user explicitly asks for parallel work.

---

# Phase Definitions

## C-PHASE 0 — Preflight Baseline Check

Goal: prove the working tree is exactly the expected baseline and capture a baseline snapshot before any new work.

Scope: read-only.

Tasks:

1. Confirm `git status` is clean.
2. Confirm `git rev-parse HEAD` returns `c1d0a5cc9` (or the current `origin/main` if it has advanced).
3. Run all validation commands from the cheatsheet and capture output to `vnext\artifacts\baseline-2026-05-05\`.
4. Run `python .\tools\audit_sprite_contract.py`, `report_runtime_canvas_mismatches.py`, `audit_optional_animation_readiness.py`. Save outputs to that folder.
5. Confirm vNext build is `0` warnings / `0` errors.
6. Confirm full test count = `144 / 144`.
7. Run a probe per existing tool family: `localDocs`, `spriteAudit`, `assetInventory`, `petState`, `codeReview`, `codePatchPlan`, `buildProof`, `translateText`, `audioAssist`, `screenCapture`. Each probe should produce a `summary.json`.
8. Write `docs\C_PHASE0_BASELINE_2026-05-05.md` summarizing pass/fail per check.

Affected files: none mutated. Only artifacts under `vnext\artifacts\baseline-2026-05-05\` and one new doc.

Risks: low. If any check fails, stop and ask before continuing — see "Stop and Ask" conditions.

Validation: every check must pass. If any test or probe fails, do not proceed to C-PHASE 1.

Rollback: delete the artifact folder and the new doc.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-0-baseline`.
- Commit: `chore(phase0): capture baseline validation snapshot`.
- Push, open PR, merge to `main` only after a human reviews.

---

## C-PHASE 1 — Action / Animation / Prop Unified Contract

Goal: separate simulation `Action` from visible `AnimationFamily` and from `PropOverlay`. Today `actions.json` overloads the base `animationState` (e.g. `water` → `eat`) and there is no first-class optional family selector in vNext. This phase fixes that without changing any sprite art.

Scope:
- `vnext\src\Wevito.VNext.Contracts\Models.cs` — add `AnimationFamily` (base + optional), `PropOverlayKind`, `ActionVisualIntent`.
- `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs` — extend action records with `optionalAnimationFamily`, `propOverlay`.
- `vnext\content\actions.json` — add explicit optional-family hints for water (`drink`), play (`play_ball`).
- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — emit `ActionVisualIntent` on action.
- `vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs` — load optional family folder when intent specifies one; fall back to base.
- New tests in `vnext\tests\Wevito.VNext.Tests\` for action → intent → family resolution.

Tasks:

1. Add the new types to Contracts. Keep them backwards compatible — `ActionVisualIntent` defaults to base `animationState` only.
2. Extend the action JSON shape with optional fields. Don't break existing consumers.
3. Update `actions.json`: `water` gets `"optionalAnimationFamily": "drink"`, `play` gets `"optionalAnimationFamily": "play_ball"`. All other fields unchanged.
4. Extend `PetSimulationEngine` to emit visual intent on action. Add a unit test class `ActionVisualIntentTests`.
5. Extend `SpriteAssetService` to attempt optional-family folder load and fall back. Add `SpriteAssetServiceOptionalFamilyTests`.
6. Do not yet wire `PropOverlayKind` to a renderer — define it but leave overlay rendering to C-PHASE 3.
7. Run focused tests, then full tests, then safe Debug publish.

Affected files: 5 in `vnext/src`, 1 in `vnext/content`, 2 new test files. Do not touch sprites_*. Do not touch `prop_anchors.json`.

Risks:
- Breaking existing action-driven tests if defaults change. Mitigation: keep defaults equal to current behavior.
- Godot/vNext divergence: Godot already has optional families. This phase only adds the contract on the vNext side.

Validation:
```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "FullyQualifiedName~ActionVisualIntentTests|FullyQualifiedName~SpriteAssetServiceOptionalFamilyTests"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild
```

Rollback: `git revert <merge commit>` on `main`. Sprites untouched, so rollback is purely code.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-1-action-animation-prop-contract`.
- Commits:
  - `feat(contracts): add AnimationFamily, PropOverlayKind, ActionVisualIntent`
  - `feat(content): hint optional families for water and play`
  - `feat(core): emit ActionVisualIntent from simulation`
  - `feat(shell): SpriteAssetService loads optional family folders with fallback`
  - `test: cover action/intent/family resolution`
- Open PR, merge after green.

---

## C-PHASE 2 — Auto-Care Visible + Drink Animation Routing

Goal: when auto-drink raises hydration, the pet should visibly play `drink` (where art exists). Same pattern for any other auto-care action introduced later. Today auto-care updates stats silently.

Scope:
- `scripts\game_manager.gd` (Godot) — route auto-drink through the same animation path as manual drink.
- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — when auto-care fires, emit the same `ActionVisualIntent` that a manual action would.
- `vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs` — already covered by C-PHASE 1; verify behavior under auto-care.
- Tests in `vnext\tests\Wevito.VNext.Tests\` for auto-care intent emission.

Tasks:

1. Audit `game_manager.gd` for every auto-care path (`auto_drink`, any auto-feed, auto-rest). Confirm none change stats silently without an animation request.
2. Make sure auto-drink prefers the optional `drink` family when available, otherwise falls back to `eat`.
3. Add or extend the dev forced scenario in Godot to assert that low hydration triggers a visible drink animation.
4. Mirror the change in vNext: `PetSimulationEngine` exposes auto-care decisions via the same intent contract.
5. New unit tests for auto-care intent.

Affected files: 1 .gd, 1 .cs, optionally 1 test file.

Risks:
- Godot side may have an existing auto-drink path that bypasses signal routing. Confirm before editing.
- A pet that drinks while moving could double-trigger animations. Add a one-shot guard.

Validation: focused tests, full tests, safe publish, manual Godot scenario screenshot if Godot is available.

Rollback: revert the .gd and .cs changes. No assets touched.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-2-auto-care-visible`.
- Commit: `feat(sim): auto-care emits visible animation intent (drink)`.
- Open PR, merge after green.

---

## C-PHASE 3 — Staged Fetch Sequence Controller

Goal: replace the current single-shot fetch with `pickup → hold → carry_walk/run → drop → return-to-idle`. Ball stays a runtime overlay (do not bake into pet PNGs).

Scope:
- `scripts\pet.gd` — add a sequence controller with named stages.
- `scripts\game_manager.gd` — call the sequence on fetch action.
- `vnext\src\Wevito.VNext.Core\PetSimulationEngine.cs` — model fetch as a multi-step action.
- `vnext\src\Wevito.VNext.Shell\SpriteAssetService.cs` — verify each optional family loads cleanly.
- New tests for sequence transitions and timeout fallbacks.

Tasks:

1. Define stage enum: `MoveToBall`, `Pickup`, `Hold`, `CarryWalk`, `CarryRun`, `Drop`, `ReturnIdle`. Each stage has a max duration and a fallback animation if the optional family is missing for the current species/age/gender/color.
2. Implement on Godot side first (it has the strongest existing prop overlay support).
3. Mirror in vNext for parity in proof scenarios.
4. Add a dev forced scenario `force_fetch_sequence` for goose/baby/female/blue (the accepted hold_ball pilot row).
5. Verify ball stays a runtime overlay — no PNG mutation. Verify `prop_anchors.json` is read but not written.
6. Add tests for: stage advance, timeout fallback, optional-family-missing fallback.

Affected files: 2 .gd, 2 .cs, 1+ test files. No sprites, no `prop_anchors.json`.

Risks:
- Long fetch sequences can deadlock if a stage timeout fires while another animation is queued. Mitigate with a hard fallback to `idle`.
- A pet that completes pickup but lacks `carry_ball_walk` for its row should gracefully hold and walk instead of stalling.

Validation: focused tests, full tests, safe publish, Godot dev scenario screenshot if available, PET TASKS petState probe.

Rollback: revert the four code files. Optional families still exist in art, no harm.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-3-staged-fetch`.
- Commits:
  - `feat(sim): staged fetch action contract`
  - `feat(godot): pet.gd implements staged fetch sequence`
  - `feat(godot): game_manager.gd calls staged fetch`
  - `feat(vnext): mirror staged fetch in PetSimulationEngine`
  - `test: cover stage transitions and fallbacks`

---

## C-PHASE 4 — Care / Item Content Mapping

Goal: connect the broad shared art pool (81 item PNGs) to concrete content records. Today `vnext\content\items.json` exposes only 7 records. After this phase the content layer should describe specific care/medicine/food/water/toy items and their visual asset ids.

Scope:
- `vnext\content\items.json` — add specific item records.
- New `vnext\content\item_visual_mapping.json` (or extension to items.json) — describe `content_item_id → visual_asset_id`, species applicability, small-icon vs habitat-object safety.
- `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs` — model the mapping.
- `vnext\src\Wevito.VNext.Core\ContentRepository.cs` — load and expose mapping.
- New tests for mapping coverage.

Tasks:

1. Use `docs\WEVITO_CARE_MEDICINE_OBJECT_REVIEW_PACKET_2026-05-05.md` as the source of truth for which assets become first-class content. Map each:
   - care: `bandage_roll`, `first_aid_kit`, `grooming_brush`, `medicine_dropper`, `pill_bottle`, `soap_bottle`, `syringe`, `thermometer`, `towel`.
   - food (per group): see review packet's species-group table.
   - water/container: `water_bowl`, `pond_dish`, `shallow_water_dish`, `seed_tray`, `dish_set`, `feeding_plate`, `hanging_feeder`.
   - toys: `ball`, `bell_toy`, `chew_toy`, `rope_toy`, `mirror_trinket`, etc.
2. Each record gets: `id`, `displayName`, `category`, `visualAssetId`, `speciesIds[]`, `smallIconSafe`, `habitatObjectSafe`, `notes`.
3. Add a `ContentCoverageTests` extension that validates every visual asset id resolves to a real PNG path under `sprites_shared_runtime/items/<group>/`.
4. Do not generate or rename any PNGs. Mapping only.
5. Mark `medicine_dropper` and `thermometer` as `smallIconSafe = false` per review packet's small-size readability concern, and surface that hint in tests.

Affected files: 1 JSON updated, 1-2 new JSON if separated, 2 .cs, 1 test.

Risks:
- Test brittleness if asset folder layout changes. Use canonical relative paths.
- `tool_definitions.json` only has `basket` and `settings` — don't conflate items and tools.

Validation: focused mapping tests, full tests, safe publish, build green.

Rollback: revert the new JSON content and contract changes.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-4-care-item-content-mapping`.
- Commit: `feat(content): map shared item art to first-class content records`.

---

## C-PHASE 5 — Habitat Object Manifest Schema

Goal: replace hardcoded prop layout in `HabitatLoadoutResolver.cs` with a declarative manifest. Schema only this phase — runtime placement comes in C-PHASE 6.

Scope:
- New `vnext\content\habitat_loadouts.json` — per-species/environment slots: primary anchor, interaction, decor.
- `vnext\src\Wevito.VNext.Contracts\ContentContracts.cs` — `HabitatObjectSlot` record per the placement anchor contract doc.
- `vnext\src\Wevito.VNext.Shell\HabitatLoadoutResolver.cs` — read manifest with hardcoded fallback as a safety net (so this phase doesn't break the runtime).
- `vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs` — extend with depth/occlusion/contact_shadow fields.
- New tests for manifest schema coverage.

Tasks:

1. Translate `docs\WEVITO_HABITAT_PLACEMENT_ANCHOR_CONTRACT_2026-05-05.md` directly into `habitat_loadouts.json` for the five pilot species (goose, rat, crow, snake, frog). Other five species can have a placeholder `{primary, interaction, decor}` set to existing defaults.
2. Define `DepthBand` enum: `Backdrop`, `FarProp`, `GroundContact`, `PetShadow`, `PetBody`, `HeldOrCarriedProp`, `NearOccluder`, `UiOverlay`. Match the contract doc.
3. Add `OcclusionMode` and `ContactShadowMode` enums.
4. Resolver loads manifest; if file missing or species missing, fall back to existing hardcoded behavior. Log a warning trace via `TraceLog`.
5. New tests: `HabitatLoadoutManifestTests`, `HabitatLoadoutResolverFallbackTests`.

Affected files: 1 new JSON, 2 .cs updated, 1 .cs extended, 2 test files.

Risks: schema bikeshed. Pin the schema to the contract doc and don't expand fields beyond what's there.

Validation: focused tests, full tests, safe publish.

Rollback: revert resolver to hardcoded path; remove manifest JSON.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-5-habitat-manifest`.
- Commits:
  - `feat(content): habitat loadouts manifest schema`
  - `feat(content): pilot-species habitat loadouts data`
  - `feat(shell): HabitatLoadoutResolver reads manifest with fallback`

---

## C-PHASE 6 — Habitat 5-Species Pilot Runtime Placement

Goal: actually apply the manifest at runtime for goose / rat / crow / snake / frog. Implement primary anchor + interaction object + depth band rendering. No expansion to other species yet.

Scope:
- `vnext\src\Wevito.VNext.Shell\HabitatPresentationModels.cs` — add z-order computation from depth band.
- `vnext\src\Wevito.VNext.Shell\HomePanelWindow.xaml.cs` (or whichever surface renders habitat) — consume new presentation models.
- `scripts\main_scene.gd` and `scripts\pet.gd` (Godot) — same depth band concept.
- New tests.

Tasks:

1. Implement depth band → z-order in vNext renderer. Order should match the manifest contract.
2. Add a contact shadow under each pilot species' primary anchor and under the pet body. Use a simple soft ellipse PNG for now (one shared shadow asset, not per species).
3. Implement primary anchor + interaction object slot per species. The interaction object visibility is driven by current need/action where applicable (so a thirsty goose shows a `pond_dish`).
4. Run a Godot dev forced scenario for each pilot species and capture screenshots into `vnext\artifacts\c-phase-6-habitat-pilot\`.
5. Visual review checkpoint: pause and ask before expansion to remaining species.

Affected files: 2-4 .cs, 2 .gd, possibly one new shared PNG (`sprites_shared_runtime/items/utility/contact_shadow.png` if not already present — confirm first).

Risks:
- Adding a new shared PNG counts as visual mutation. If shadow PNG must be added, treat that as gated by visual-side review and consider deferring shadow rendering to a no-PNG approach (vector ellipse drawn in WPF).
- Depth band ordering can fight with WPF Canvas Z-index; verify the renderer exposes the right hook.

Validation: focused tests, full tests, safe publish, screenshot proof artifacts present.

Rollback: revert renderer changes; manifest still loads but is ignored.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-6-habitat-pilot`.
- This is a STOP-AND-ASK checkpoint after screenshots are captured. Do not auto-merge to `main`. Wait for human review of the screenshots.

---

## C-PHASE 7 — Habitat Expansion To Remaining 5 Species

Goal: extend habitat manifest + runtime placement to fox, deer, pigeon, raccoon, squirrel.

Scope: add manifest entries; verify renderer handles them; capture screenshots.

Tasks:

1. Use `docs\WEVITO_ASSET_INVENTORY_CHECKLIST_2026-05-04.md` "Recommended species/default habitat mapping" table.
2. Add manifest entries for the five non-pilot species.
3. Run dev scenarios; capture screenshots.
4. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` "Habitat/environment" row to ~70%.

Affected files: 1 JSON, 1 doc.

Risks: none new.

Validation: focused tests, full tests, safe publish, screenshots present.

Rollback: revert manifest entries.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-7-habitat-expansion`.
- Commit: `feat(content): habitat loadouts for all 10 species`.

---

## C-PHASE 8 — Tool Hub Phase-26 IA Refactor

Goal: implement the Tool Hub Information Architecture spec end to end.

Scope:
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml`
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs`
- `tools\probe-vnext-pet-tasks.ps1` (extend assertions, do not break old ones)
- New visible XAML elements per `docs\WEVITO_TOOL_HUB_INFORMATION_ARCHITECTURE_2026-05-05.md` and `docs\WEVITO_PET_TASKS_VISUAL_REFINEMENT_SPEC_2026-05-05.md`.

Tasks:

1. Rename popup title `Pet Helpers` → `PET TASKS`.
2. Move command textbox to top.
3. Replace long intro copy with `Ask your pets ...` placeholder + concise capability/locked lines.
4. Restructure into sections per IA doc: header, helper strip, command row, task card, result, timeline, safety footer.
5. Add `REPORT ONLY` badge to header (from refinement spec).
6. Preserve all existing automation IDs: `PetCommandTextBox`, `PetCommandSubmitButton`, `PetTaskCapabilityText`, `PetWellbeingSnapshotText`, `PetTaskQueueComboBox`, `PetTaskApproveButton`, `PetTaskPreviewButton`, `PetTaskCancelButton`, `PetTaskExecuteButton`.
7. Add `PetTaskResultPathText` and `PetTaskNextActionText` if not already present.
8. Add `Open Report` / `Copy Path` / `Open Folder` buttons (deferred wiring — they go live in C-PHASE 10).
9. Update probe to assert new structure without removing old assertions.

Affected files: 2 in Shell, 1 probe script. No assets, no content files.

Risks:
- Probe regressions. Mitigation: run every existing probe before merging.
- Layout breaks at small popup sizes. Mitigation: test at 720x540 and 1080x720.

Validation: focused tests, full tests, safe publish, every existing probe passes, screenshot of new layout.

Rollback: revert XAML + xaml.cs + probe.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-8-tool-hub-refactor`.
- Commit: `feat(shell): Tool Hub IA refactor (PET TASKS layout)`.

---

## C-PHASE 9 — Three-Helper Roster + Pet Command Bar @Addressing

Goal: implement the user's requested named-pet command bar. Three helper pets, role chips, `@Name` addressing, name-prefix addressing (`Bean,` or `Pip,`).

Scope:
- `vnext\src\Wevito.VNext.Contracts\PetAgentContracts.cs` — `HelperPet` record (name, species, role, state, currentTaskId).
- `vnext\src\Wevito.VNext.Core\PetCommandParser.cs` — parse `@Name`, `Name,` prefixes, route to addressed helper.
- `vnext\src\Wevito.VNext.Core\PetCommandBarService.cs` — manage active roster (max 3), assign default names if user hasn't picked.
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml` — three helper cards.
- New tests.

Tasks:

1. Default names: `Scout`, `Inspector`, `Builder`. User can rename in settings later (out of scope for this phase).
2. Default roles: docs/research, sprite QA, code/proof.
3. Parser: `@Scout review goose ...` routes to Scout. Same for `Scout, ...`. Unaddressed routes to current selected helper or first available.
4. Helper state enum: `Available`, `Drafting`, `Reviewing`, `Blocked`.
5. Animal personality: Scout = frog, Inspector = pigeon, Builder = rat (defaults; user can rebind). Personality affects visual avatar/state animation only — never permission. (This rule is in `WEVITO_PET_AGENT_TEXT_BAR_PLAN_2026-05-05.md`.)
6. Tests: parser routing, roster cap, default names, addressing fallback.

Affected files: 4 .cs, 1 XAML, 2 test files.

Risks:
- Conflict with existing `PetCommandParser` heuristics. Run all existing parser tests.
- Confusing UX if a helper is `Blocked` and user addresses it anyway. Surface a polite blocked message.

Validation: focused tests, full tests, safe publish, probe with `-TaskText "Scout, summarize sprite docs"`.

Rollback: revert parser/service/contract changes.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-9-three-helper-roster`.
- Commits:
  - `feat(contracts): HelperPet roster model`
  - `feat(parser): @Name and Name, addressing`
  - `feat(shell): three-helper card strip`

---

## C-PHASE 10 — Artifact Controls (Open Report / Copy Path / Open Folder)

Goal: wire the buttons stubbed in C-PHASE 8.

Scope: `ToolPopupWindow.xaml.cs` only.

Tasks:

1. `Open Report` opens the most-recent `run-summary.md` in the OS default markdown viewer or text editor.
2. `Copy Path` copies the artifact path to clipboard.
3. `Open Folder` opens the artifact folder in Explorer.
4. All three buttons require a current task card with a result path. Disable otherwise.
5. Tests for the path resolution logic (in `PetTaskCardQueueServiceTests`).

Affected files: 1 .cs, 1 test file.

Risks:
- Path injection if the artifact path is somehow user-controlled. It is not — it comes from adapters. Still, validate the path is under `vnext\artifacts\pet-tasks\` before opening.

Validation: focused tests, full tests, safe publish, manual UI test (probe with `-OpenReportAfter` flag added to probe).

Rollback: revert one .cs.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-10-artifact-controls`.
- Commit: `feat(shell): wire Open Report / Copy Path / Open Folder`.

---

## C-PHASE 11 — Wevito-Window Screenshot Execution

Goal: turn the existing `screenCapture` preview adapter into an approval-gated execution adapter for Wevito-window-only capture.

Scope:
- New `vnext\src\Wevito.VNext.Core\ScreenCaptureExecutionAdapter.cs`.
- `vnext\src\Wevito.VNext.Core\PetTaskAdapterPreviewDispatcher.cs` — route execution.
- `vnext\src\Wevito.VNext.Shell\ShellCoordinator.cs` — execute on RUN.
- `vnext\src\Wevito.VNext.Shell\ToolPopupWindow.xaml.cs` — enable RUN for reviewed `screenCapture` cards.
- New shell-side capture backend: `vnext\src\Wevito.VNext.Shell\ScreenCaptureBackend.cs` using Windows.Graphics.Capture (per research notes).
- Tests including a fake backend.

Tasks:

1. TFM: bump `Wevito.VNext.Shell` to `net8.0-windows10.0.19041.0` if not already, so WinRT projections work without an extra package.
2. Implement `IScreenCaptureBackend` with two implementations: `WindowsGraphicsCaptureBackend` (real) and `FakeScreenCaptureBackend` (test).
3. Capture flow:
   - Find Wevito's main HWND.
   - Use `IGraphicsCaptureItemInterop.CreateForWindow`.
   - Capture one frame, save to `vnext\artifacts\pet-tasks\<ts>-screencapture-execute\screenshot.png`.
   - Write `manifest.json` with HWND title, target window rect, capture timestamp, capture indicator state, redaction state.
   - Write `run-summary.md`.
4. Mark Wevito's settings/sensitive surfaces with `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` so a captured Wevito window cannot recursively capture its own credential/billing surfaces.
5. Approval gate: `RUN` only enabled for reviewed `screenCapture` cards with target `wevito_window`. Other targets (foreground / desktop / region / recording) remain blocked in this phase.
6. Tests:
   - `ScreenCaptureExecutionAdapterTests` (uses fake backend; verifies artifact files exist, manifest fields correct).
   - `ToolPolicyEvaluatorTests` updates to assert that only `wevito_window` is approvable in this phase.
7. Live probe: `probe-vnext-pet-tasks.ps1 -TaskText "screenshot the Wevito window" -ExpectedToolFamily screenCapture -ApproveBeforePreview -ExpectExecuteEnabledAfterPreview -SkipBuild` then click RUN to confirm.

Affected files: 4 new .cs, 4 updated .cs, 2 test files, possibly csproj TFM bump.

Risks:
- TFM bump cascades to Broker / AutomationRunner. Confirm only Shell needs WinRT.
- Capture indicator (yellow border) appears on Win11; that's by design. Do not suppress.
- Some CI/headless environments cannot capture. Detect and surface "capture unavailable" as a blocked message.

Validation: focused tests, full tests, safe publish, live probe with real RUN.

Rollback: revert .cs adds and edits. TFM bump may need to stay if other phases depend on it; document if so.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-11-wevito-window-capture`.
- Commits:
  - `feat(shell): WindowsGraphicsCaptureBackend`
  - `feat(core): ScreenCaptureExecutionAdapter`
  - `feat(shell): RUN gate for wevito_window capture`
  - `test: cover capture execution with fake backend`

---

## C-PHASE 12 — Region + Last-Region Screenshot

Goal: extend capture to user-selected region, with `last region` recall.

Scope: new `RegionPickerWindow` in Shell, extends `ScreenCaptureExecutionAdapter`, extends `CapturePolicyEvaluator`.

Tasks:

1. Region picker: a transparent always-on-top WPF window with a drag-to-select rectangle. ESC cancels.
2. Persist last region in `%LOCALAPPDATA%\Wevito\last_region.json` (geometry only, no metadata).
3. Approval gate: region capture is approval-required (medium risk).
4. Last region is recallable via `screenshot last region`.
5. Tests: parser command coverage, policy gate, region geometry round-trip.

Affected files: 1 new XAML window, 1 new .cs adapter extension, 1 .cs policy update, 2 test files.

Risks: per-monitor DPI scaling on multi-monitor setups. Use `PerMonitorV2` DPI awareness already standard in WPF .NET 8.

Validation: focused tests, full tests, safe publish, live probe with `-TaskText "screenshot a region"`.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-12-region-capture`.

---

## C-PHASE 13 — Short Proof Clips

Goal: add 5-10 second MP4 proof clips for sprite/proof review. Use Windows Graphics Capture frame pool → Media Foundation H.264 sink writer per research notes.

Scope: new `videoCapture` task family (or extend `screenCapture`); new `MediaFoundationVideoEncoder.cs`; UI controls for start/stop with hard duration cap.

Tasks:

1. Hard cap: 10 seconds. UI shows countdown.
2. Default scene = Wevito window only. Region/foreground/desktop blocked.
3. Default audio = none.
4. Visible recording indicator in Wevito's own UI (not relying on system yellow border).
5. Save to `vnext\artifacts\pet-tasks\<ts>-screencapture-clip\clip.mp4` + manifest + summary.
6. Tests with a fake encoder.

Affected files: 1 new .cs encoder, 1 .cs adapter extension, 1 XAML overlay, 2 test files.

Risks:
- Media Foundation sink writer is error-prone. Wrap in `try/finally` with proper COM release.
- Disk I/O on slow drives can drop frames. Buffer to memory and flush at end.

Validation: focused tests, full tests, safe publish, live probe + manual review of MP4.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-13-proof-clips`.
- This is a STOP-AND-ASK checkpoint before merge. Recording is the highest-privacy capture. Confirm with user before merging.

---

## C-PHASE 14 — Translation Glossary + QA Review

Goal: add glossary and post-translation QA per phase 41 spec in `docs\WEVITO_TOOL_AGENT_UI_MASTER_PLAN_2026-05-05.md`.

Scope:
- `vnext\content\translation_glossaries.json` — small starter glossaries by language.
- `vnext\src\Wevito.VNext.Core\TranslationPreviewAdapter.cs` — surface glossary preview.
- `vnext\src\Wevito.VNext.Core\TranslationExecutionAdapter.cs` — apply glossary, emit QA warnings (placeholder drift, code-block changes, markdown drift, target empty, fallback used).
- New tests.

Tasks:

1. Glossary entries: `{source: "...", target: "...", caseSensitive, notes}` per language pair.
2. DeepL native glossary path (Pro tier). Document that on Free tier glossary is restricted; if Free, use a pre/post substitution shim.
3. QA warnings list in the result artifact, not blocking.
4. Tests cover glossary substitution, placeholder preservation, markdown preservation, code-block preservation.

Affected files: 1 new JSON, 2 .cs, 1-2 test files.

Risks: DeepL Free vs Pro divergence on glossary; document clearly.

Validation: focused tests, full tests, safe publish, probe with translation request that contains code blocks and placeholders.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-14-translation-glossary`.

---

## C-PHASE 15 — Translation Second Provider (LibreTranslate Local)

Goal: add a second provider so users can choose between cloud (DeepL) and local (LibreTranslate sidecar). Default still = none.

Scope:
- `vnext\src\Wevito.VNext.Core\LibreTranslateClient.cs` — talks to a localhost LibreTranslate sidecar.
- `vnext\src\Wevito.VNext.Core\TranslationProviderRouter.cs` — multi-provider routing.
- Provider selection persisted in user settings.
- Tests with a fake LibreTranslate server.

Tasks:

1. Sidecar discovery: probe `http://localhost:5000/translate`. If present, mark `libretranslate` as `Available`.
2. Sidecar setup is documented but not auto-installed by Wevito. Provide a setup guide artifact.
3. Provider router exposes `Available`, `Configured`, `Default`, `User-selected` states.
4. First-translation consent dialog explains: provider, where text goes, retention. (Per research.)
5. Tests cover provider availability, fallback when one provider fails.

Affected files: 1 new .cs, 1-2 .cs updated, 1-2 test files.

Risks: localhost assumption; handle non-default ports.

Validation: focused tests, full tests, safe publish, probe.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-15-libretranslate-provider`.

---

## C-PHASE 16 — Audio External Boost Handoff

Goal: turn the planned phase-45 (External Boost Handoff) into a real adapter. Detect FxSound / Equalizer APO; generate a setup guide; never auto-install.

Scope:
- New `vnext\src\Wevito.VNext.Core\AudioBoostHandoffAdapter.cs`.
- Extend audio task kinds.
- New tests.

Tasks:

1. Detection:
   - FxSound: check `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` and standard install paths, plus running process `fxsound.exe`.
   - Equalizer APO: check install path `C:\Program Files\EqualizerAPO\` and `config\config.txt` existence.
2. Setup guide: write a markdown summary that lists status, official download URL (DeepL-style provider router pattern, no automatic browser open), reboot warning per research notes, and config-file path.
3. Never edit `config.txt`. Never trigger an install. Never elevate privileges.
4. Tests with mocked filesystem detection.

Affected files: 1 new .cs, 1-2 .cs updated, 1-2 test files.

Risks:
- Detection false positives if user uninstalled but registry left over. Note in manifest.
- Reboot requirement is real and outside Wevito's control.

Validation: focused tests, full tests, safe publish, probe.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-16-audio-boost-handoff`.

---

## C-PHASE 17 — Audio Boost Preset Planning

Goal: emit conservative gain/EQ preset *plans* (text only). User applies externally.

Scope: `AudioBoostHandoffAdapter` extension.

Tasks:

1. Generate 2-3 sample Equalizer APO `config.txt` blocks (one for "warmth", one for "speech clarity", one for "modest +3 dB safe boost").
2. Each plan includes:
   - explicit limiter / -1 dBTP true-peak ceiling note (per research, EBU R 128 s2).
   - WHO 80 dB(A)/40h/week safety note.
   - text disclaimer: "user applies manually; Wevito does not edit your config".
3. Plan saved as `boost-plan.txt` next to `run-summary.md` in artifact folder.
4. Tests: plan content includes safety strings; no plan exceeds +6 dB.

Affected files: 1 .cs, 1 test file.

Risks: legal/health framing. Use the precise WHO/EBU citations from research notes; do not invent dB numbers.

Validation: focused tests, full tests, safe publish, probe.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-17-audio-boost-preset-plans`.

---

## C-PHASE 18 — buildProof Execution Allowlist + Fake Runner

Goal: implement the design from `docs\CODE_SIDE_PHASE33_PROOF_EXECUTION_DESIGN_2026-05-05.md` up to the fake-runner checkpoint. No real commands run yet.

Scope:
- New `vnext\src\Wevito.VNext.Contracts\ProofExecution*` records (Command, Request, Manifest, Result).
- New `vnext\src\Wevito.VNext.Core\ProofExecutionAllowlistEvaluator.cs`.
- New `vnext\src\Wevito.VNext.Core\ICommandRunner.cs` interface.
- New `vnext\src\Wevito.VNext.Core\FakeCommandRunner.cs`.
- New tests: allowlist matching, blocked composition (no `Remove-Item`, `git reset`, redirection, asset-prep without `-SkipAssetPrep`), `-SkipAssetPrep` enforcement, manifest round-trip, fake-runner integration.

Tasks:

1. Allowlist matches by exact-equality of executable + ordered argument list. No regex shell composition.
2. Hard-blocks: `Remove-Item`, `git reset`, `git checkout`, package install, browser automation, asset-prep, anything elevating privileges.
3. `build-vnext.ps1` only allowed with `-SkipAssetPrep` while visual cleanup is active. Treat that as an environment flag in C-PHASE 30.
4. Fake runner records the command and returns a synthetic result.
5. PET TASKS UI: under-the-hood plumbing only this phase. Real RUN button stays disabled for `buildProof` even after approval — explicitly. The next phase enables it.

Affected files: 4-5 new .cs, 1 .cs updated, 2-3 test files.

Risks: spec drift from design doc. Keep field names identical to design doc.

Validation: focused tests, full tests, safe publish, probe (preview only).

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-18-buildproof-execution-stub`.

---

## C-PHASE 19 — buildProof Execution Real Runner

Goal: enable real command execution for the four allowlisted commands.

Scope:
- New `vnext\src\Wevito.VNext.Core\ProcessCommandRunner.cs`.
- `ToolPopupWindow.xaml.cs` — enable RUN for reviewed `buildProof` cards.
- New tests.

Tasks:

1. `ProcessCommandRunner` uses `System.Diagnostics.Process` with explicit working directory, stdout/stderr redirection, timeout, cancellation.
2. Pre-run: snapshot protected sprite hashes (re-use the existing approach from current probes).
3. Post-run: re-hash and assert no protected sprite changed. If changed, mark result as `MutationDetected` and stop.
4. Tests: cancellation, timeout, mutation detection.
5. Live probe with a harmless command (e.g. `dotnet --version`) added to allowlist for tests only — production allowlist stays as in the design doc.

Affected files: 1-2 new .cs, 1 .cs updated, 2-3 test files.

Risks: process lifetime / orphaning. Use `Process.Kill(entireProcessTree: true)`.

Validation: focused tests, full tests, safe publish, live probe with RUN clicked.

Rollback: revert. Real runner can be turned off by reverting one feature flag.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-19-buildproof-execution-real`.
- This is a STOP-AND-ASK checkpoint before merge.

---

## C-PHASE 20 — Sprite Workflow V2 Read-Only Auditor

Goal: build the read-only first slice of Sprite Workflow V2 inside Wevito (not a separate app). Use the one-screen layout from `docs\WEVITO_SPRITE_WORKFLOW_V2_OVERLAY_COMPATIBILITY_SPEC_2026-05-05.md`.

Scope:
- New WPF window `SpriteWorkflowV2Window.xaml` summoned from Tool Hub.
- New service `SpriteWorkflowManifestReader.cs` that reads existing runtime/authored/verified roots and emits a queue.
- New `SpriteWorkflowContactSheetGenerator.cs` (Python script wrapper or pure C# Pillow-equivalent via `System.Drawing`/`SkiaSharp`).
- Reuse `SpriteWorkflowContracts.cs` if present; extend if needed.
- Tests.

Tasks:

1. Window layout: queue (left), current row (center: source/runtime/candidate/proof strips), validator findings + provenance (bottom).
2. Read-only: no apply, no rollback, no candidate import.
3. Selecting a queue row populates the strips.
4. BLAKE3 hashes per file shown in provenance panel (per research notes — BLAKE3 is faster than SHA-256 for big asset trees).
5. Tests cover manifest read, queue building, contact-sheet generation with fake source.

Affected files: 1 XAML, 2-3 new .cs, optional 1 Python script under `tools\` if contact sheet generation lives there.

Risks: SkiaSharp adds a NuGet dependency. Confirm cross-platform restrictions don't break Broker/AutomationRunner builds.

Validation: focused tests, full tests, safe publish, manual screenshot of V2 window.

Rollback: revert; window file deletion.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-20-sprite-workflow-v2-readonly`.

---

## C-PHASE 21 — Sprite Workflow V2 Candidate Import + Dry-Run Apply

Goal: add candidate import lane and dry-run apply. Mutation still blocked.

Scope: `SpriteWorkflowV2Window` extension; new `SpriteWorkflowCandidateImporter.cs`; new `SpriteWorkflowDryRunApplyService.cs`.

Tasks:

1. Candidate import: copy candidate PNGs from a user-picked folder into `sprites_authored\<species>\<row>\.candidates\<ts>\`.
2. Never overwrite source/runtime/verified.
3. Dry-run apply: produce a planned-changes manifest (would-overwrite-this, would-back-up-that, would-update-these-hashes) but does not actually move/copy.
4. Tests cover import safety (no overwrite of runtime/verified) and dry-run plan correctness.

Affected files: 2 new .cs, 1 XAML extension, 2 test files.

Risks: file path mistakes can land candidates in `sprites_runtime`. Tests must assert candidate path prefix.

Validation: focused tests, full tests, safe publish.

Rollback: revert; no runtime files touched.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-21-sprite-workflow-v2-import-dryrun`.

---

## C-PHASE 22 — Sprite Workflow V2 Backup-Apply + Rollback + Post-Proof

Goal: enable real one-row apply with backup + rollback + post-apply proof.

Scope: new `SpriteWorkflowApplyService.cs`, `SpriteWorkflowRollbackService.cs`, `SpriteWorkflowPostApplyProof.cs`.

Tasks:

1. Apply flow (per research atomic-write pattern):
   - Hash existing files (BLAKE3).
   - Copy existing → `sprites_runtime\.backup\<row-id>-<ts>\`.
   - Copy candidate → `sprites_runtime\.staging\<row-id>-<ts>\`.
   - `os.replace`-equivalent on each PNG (use `File.Move(overwrite:true)` on same volume).
   - Write `apply.json` log with old/new hashes.
2. Rollback: `swap-back-from-backup` by hash.
3. Post-apply proof: re-run `audit_sprite_contract.py` and `report_runtime_canvas_mismatches.py`; if any new error, auto-rollback.
4. Limit to one row per apply call.
5. Tests: apply happy path, rollback, post-proof failure triggers auto-rollback.

Affected files: 3-4 new .cs, 1 XAML extension, 3+ test files.

Risks: filesystem atomicity on network drives (per research). Reject apply if not on same drive.

Validation: focused tests, full tests, safe publish, manual one-row apply on a test sprite.

Rollback: built-in; also `git checkout -- sprites_runtime/<species>/<row>` works as last resort.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-22-sprite-workflow-v2-apply-rollback`.
- STOP-AND-ASK before merge. This is the first PNG-mutating phase.

---

## C-PHASE 23 — Optional Animation drop_ball One-Row Pilot Through V2

Goal: prove V2 by pushing the planned `goose / baby / female / blue / drop_ball` one-row pilot through it.

Scope: V2 only; no new code.

Tasks:

1. Acquire candidate frames (from existing Gemini handoff packet or a manual paint).
2. Run V2: import → dry-run → review → apply → post-proof.
3. Confirm rollback works by manually triggering rollback after apply, then re-applying.
4. Capture screenshots of every step into `vnext\artifacts\c-phase-23-drop-ball-pilot\`.
5. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` "Sprites/visuals" row to ~85%.

Affected files: PNGs in one row's runtime folder, dashboard doc.

Risks:
- The candidate quality may be unacceptable. If so, rollback and stop.
- Visual-side review must approve the candidate before this phase merges.

Validation: V2 post-proof = green; dashboard updated.

Rollback: V2 rollback button.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-23-drop-ball-pilot`.
- STOP-AND-ASK before merge. Visual-side must sign off.

---

## C-PHASE 24 — Creative Learning Lab Read-Only Artifact Index

Goal: implement the read-only first slice of `docs\WEVITO_CREATIVE_LEARNING_LAB_PLAN_2026-05-05.md`.

Scope: new `CreativeLearningLabWindow.xaml`; new `LearningLabArtifactIndexer.cs`.

Tasks:

1. Scan `vnext\artifacts\visual-review\` and `vnext\artifacts\animation-runs\`.
2. Index markdown/JSON manifests.
3. Show the lab dashboard (top metrics: raw / cleaned / labeled / bundled / eval; review queue; bundle panel; eval panel).
4. Read-only. No labels written yet.
5. Tests cover indexer.

Affected files: 1 XAML, 2 new .cs, 1 test file.

Risks: artifact folder layout drift. Tolerant indexer.

Validation: focused tests, full tests, safe publish, manual screenshot.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-24-learning-lab-readonly`.

---

## C-PHASE 25 — Creative Learning Lab Labels + Bundles + Eval Gates

Goal: add manual labeling and bundle readiness gates. Still no training/export.

Scope: extend Lab; new `LearningLabLabelStore.cs` (sqlite) and `LearningLabBundleService.cs`.

Tasks:

1. SQLite at `%LOCALAPPDATA%\Wevito\learning_lab.db`.
2. Labels: `accept`, `reject`, `revise`, `defer`, `blocked`.
3. Bundle gate: source paths recorded, provenance recorded, reviewer labels present, rejected examples excluded, intended use stated, rollback path known. (Per learning-lab plan doc.)
4. Eval bench placeholders: `sprite-validator-vs-human`, `palette-identity`, `optional-overlay-policy`, etc. — surface a list, no actual benchmarks run yet.
5. Tests cover label CRUD, bundle gating.

Affected files: 2 new .cs, 1 XAML extension, 2 test files.

Risks: SQLite migration on schema change. Use a single forward-compat schema with version column.

Validation: focused tests, full tests, safe publish.

Rollback: revert; delete `learning_lab.db` if needed.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-25-learning-lab-labels-bundles`.

---

## C-PHASE 26 — Pet AI Agent Pilot (Single Helper, Read-Only)

Goal: connect one helper pet to a real model adapter so it can produce richer suggestions on top of existing tool outputs. Read-only tasks only — no execution beyond the existing tool surfaces.

Scope:
- New `vnext\src\Wevito.VNext.Core\IModelAdapter.cs`.
- `vnext\src\Wevito.VNext.Core\AnthropicModelAdapter.cs` (or equivalent provider, with an `IModelAdapter` shape so it's swappable).
- Extension to the `localDocs`/`spriteAudit` adapters: when a helper has a model adapter configured, append a model-generated summary section to the report.
- Per-helper allowlist (per research, mirror Claude Agent SDK's allow/deny model with deny-wins).
- Tests with a fake model adapter.

Tasks:

1. Default model adapter = none (no network calls until user configures).
2. Per-helper config: `Scout` can call model on `localDocs`/`codeReview`; `Inspector` on `spriteAudit`/`petState`; `Builder` on `codePatchPlan`. Capability tokens are short-lived and scoped per call (research recommendation).
3. Approval gate before any first model call.
4. Audit log: every model call writes to `vnext\artifacts\pet-tasks\<ts>-<helper>-<tool>\model-call.json` with provider, args hash, decision, latency.
5. Untrusted-content tagging: any web/file content fetched as part of the tool is tagged `<untrusted>` in the prompt (per research, dual-LLM/spotlighting pattern).
6. Tests cover allowlist resolution, per-helper scope, audit log shape.

Affected files: 3-4 new .cs, 1-2 .cs updated, 2-3 test files. New entry in `tool_definitions.json` for model-adapter capability per helper.

Risks:
- Model API key handling. Store in Windows Credential Manager (DPAPI), never plaintext config.
- Indirect prompt injection through document content. Mitigate with the tag wrapping above and never let model output drive subsequent tool calls without human review.
- Lethal-trifecta separation: ensure no single helper can simultaneously read untrusted external content, hold private user data, and communicate outbound. Scout reads untrusted; Builder doesn't read untrusted; Inspector reads sprite-only data.

Validation: focused tests, full tests, safe publish, live probe with fake adapter.

Rollback: revert; remove key from credential manager if user wants.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-26-pet-agent-pilot`.
- STOP-AND-ASK before merge. First model-call phase.

---

## C-PHASE 27 — Pet AI Agent Three-Helper Routing + sqlite-vec Memory

Goal: full three-helper routing with per-pet preference memory. Use sqlite-vec (per research, the 2026 stable embedded vector store).

Scope:
- New `vnext\src\Wevito.VNext.Core\PetMemoryStore.cs` (sqlite-vec + FTS5).
- Routing: `PetCommandBarService` consults memory for "preferred helper for this kind of task".
- Per-pet allowlist as in C-PHASE 26.
- Tests.

Tasks:

1. Memory store schema: `examples (id, pet_id, kind, content, label, embedding, created_at)`. One DB file per pet (research recommendation).
2. Embeddings: use the model adapter; cache them.
3. Memory writes are themselves a gated tool (validating gate per research).
4. Routing: when a command comes in unaddressed, the router asks each pet's memory for top-K relevant examples and picks the helper with the strongest match. Fallback to round-robin.
5. Tests cover memory CRUD, routing, gating.

Affected files: 2 new .cs, 1-2 .cs updated, 2-3 test files. NuGet `sqlite-vec` added.

Risks: vector DB corruption on crash. WAL mode + small writes; transaction-wrap.

Validation: focused tests, full tests, safe publish.

Rollback: revert; delete memory DB files.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-27-three-helper-memory`.

---

## C-PHASE 28 — Aging / Death / Ghost Lifecycle Visible Flow

Goal: complete the simulation lifecycle. Today aging, death, and ghost states exist but are visually weak.

Scope: `PetSimulationEngine.cs` lifecycle path; ghost variant rendering; death scene; memorial overlay (uses existing `memorial_object` art).

Tasks:

1. Aging stages: baby → teen → adult → senior. Senior is new; uses adult sprites with optional desaturation overlay.
2. Death: clear visual flow with `sad` animation, dim, transition to ghost variant if available, otherwise fade out.
3. Ghost variant: only rendered if art exists for the row (gated). Otherwise fall back to a generic ghost overlay.
4. Memorial spawn: drop a `memorial_object` at the death location. Persist for one in-game day.
5. Save/load migration: ensure old saves still load. Add migration test.
6. Tests cover aging tick, death trigger, ghost fallback, memorial spawn, save/load round-trip.

Affected files: 2-3 .cs, 1-2 test files.

Risks: save/load schema. Bump version, write a migration.

Validation: focused tests, full tests, safe publish, Godot manual scenario for death.

Rollback: revert.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-28-lifecycle`.

---

## C-PHASE 29 — Final QA Sweep

Goal: run the full release-readiness sweep. No new feature work.

Scope: tests, scenarios, screenshot proof packets, dashboard updates.

Tasks:

1. Run every probe (`localDocs`, `spriteAudit`, `assetInventory`, `petState`, `codeReview`, `codePatchPlan`, `buildProof`, `translateText`, `audioAssist`, `screenCapture`).
2. Run Godot in-game forced scenarios for every species/age/gender × {feed, water, drink, play (fetch), groom, bath, medicine, doctor, rest}.
3. Capture screenshots of every habitat, every pilot animation, every UI surface.
4. Reconcile main repo runtime canvas: re-run `report_runtime_canvas_mismatches.py`. Target = 0 mismatches. If non-zero, follow the deterministic normalization path from `docs\RUNTIME_CANVAS_NORMALIZATION_PHASE3_DRY_RUN_2026-05-04.md`. STOP-AND-ASK before applying broad normalization.
5. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` to reflect actual current state.
6. Re-export PDF dashboard packet.

Affected files: dashboard doc, PDF, screenshots.

Risks: canvas normalization is medium risk. Confirm with visual-side first.

Validation: every probe green, every scenario captured, dashboard at ~95%+ in all rows except optional-animation expansion.

Rollback: revert dashboard updates; canvas normalization is rollback-able via V2 if used.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-29-final-qa`.

---

## C-PHASE 30 — Packaging + Final User Help Guide + Release Tag

Goal: package Wevito for shipping and tag a release.

Scope:
- `tools\build-release.ps1` — verify it works end to end. Update if broken.
- New `docs\WEVITO_USER_HELP_GUIDE_2026-05-05.md` — user-facing controls/help.
- Release tag.

Tasks:

1. Verify `build-release.ps1` produces a Release build.
2. Run full test suite under Release.
3. Run safe publish without `-SkipAssetPrep` (only if all visual cleanup is complete and main repo canvas is green from C-PHASE 29).
4. Write user help guide covering: install, controls, PET TASKS basics, helper pets, settings, troubleshooting, privacy disclosures (capture, translation, audio, model use).
5. Tag `v0.1.0-claude-rollup` (or whatever version the user picks).
6. Update dashboard to "Release Candidate".

Affected files: tools script (maybe), new doc, git tag.

Risks: Release build differs from Debug. Run full QA again under Release.

Validation: clean Release build, all tests pass, manual install on a fresh machine if possible.

Rollback: delete tag (`git tag -d`), revert doc.

Commit/push checkpoint:
- Branch: `claude-implementation/c-phase-30-release`.
- STOP-AND-ASK before tag and merge.

---

# Stop-and-Ask Conditions

Codex must stop and ask the user before continuing if any of these occur:

1. C-PHASE 0 baseline check fails any item.
2. Any test count regresses (existing tests start failing).
3. A protected sprite hash changes outside an approved Sprite Workflow V2 apply.
4. A capture, translation, audio, or model-call adapter is about to make its first real network/system call in this checkout.
5. C-PHASE 6 (habitat pilot), C-PHASE 13 (proof clips), C-PHASE 19 (real build runner), C-PHASE 22 (V2 apply), C-PHASE 23 (drop_ball pilot), C-PHASE 26 (first model call), C-PHASE 30 (release tag) — these are explicit stop-and-ask checkpoints.
6. Any phase requires a TFM bump that cascades to multiple projects.
7. Any phase needs a NuGet package added.
8. Any phase touches `prop_anchors.json`, `sprites_runtime/_metadata/`, or any `sprites_authored_verified/` content.
9. Any phase needs `build-vnext.ps1` without `-SkipAssetPrep` — until C-PHASE 29 / 30.
10. The user's `git status` shows uncommitted work that wasn't there at C-PHASE 0.

When stopping, Codex should: push its current branch, write a short markdown note explaining what it would do next, and ask.

# Phase Independence Map

Phases that can run in parallel (different branches, different files):

```text
C-PHASE 1 ┄ C-PHASE 8     (contracts vs UI)
C-PHASE 4 ┄ C-PHASE 5     (content mapping vs habitat schema)
C-PHASE 14 ┄ C-PHASE 16  (translation vs audio)
C-PHASE 20 ┄ C-PHASE 24  (sprite workflow vs learning lab)
```

Default execution = sequential per the master phase map. Parallelism only if the user asks.

# Update Workflow

When a phase merges to `main`:

1. Update `docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md` percentages.
2. Re-export PDF dashboard:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\export-project-dashboard-pdf.ps1
python .\tools\export-project-dashboard-packet-pdf.py
```

3. Add a one-liner to `docs\EXECUTION_BOARD.md` (or create it) under "Claude Implementation Plan" with phase id + status.

# End

This is the master plan. For copy-paste prompts, see `CLAUDE_CODEX_MEDIUM_PHASE_PROMPTS_2026-05-05.md`. For commit/push rules, see `CLAUDE_PHASE_COMMIT_PUSH_STRATEGY_2026-05-05.md`. For risks, see `CLAUDE_IMPLEMENTATION_RISK_REGISTER_2026-05-05.md`. For research sources, see `CLAUDE_WEB_RESEARCH_NOTES_2026-05-05.md`.
