# Codex Medium Post-Stable Phase Prompts

Use these prompts one phase at a time after `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md` is merged. Each prompt assumes a medium-effort Codex model and repeats the critical guardrails so the worker does not need hidden context.

## C-PHASE 54 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch:

`claude-implementation/c-phase-54-post-stable-baseline-lock`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`
- `docs/C_PHASE52_STABLE_RELEASE_PROMOTION_2026-05-12.md`

Task:

Implement C-PHASE 54 exactly as specified in the plan.

Required outputs:

- `vnext/content/stable_release_lock.json`
- `tools/assert-stable-baseline.ps1`
- guarded `tools/build-vnext.ps1`
- `docs/C_PHASE54_POST_STABLE_BASELINE_LOCK_2026-05-12.md`

Important constraints:

- Do not mutate sprites.
- Do not run asset prep except for the expected failing guard test.
- The safe build path with `-SkipAssetPrep` must pass.
- No release publishing.
- No model calls.

Validation:

Run the expected failing guard command:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipTests`

Expected: fails with stable release lock message.

Then run:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\assert-stable-baseline.ps1`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Commit, push, open PR, and stop after merge or PR ready state depending on local workflow. Do not start C-PHASE 55 in the same branch.
=== PROMPT END ===

## C-PHASE 55 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-55-visual-source-of-truth`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE54_POST_STABLE_BASELINE_LOCK_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

Task:

Implement C-PHASE 55 as a report-only visual source-of-truth recovery phase.

Required outputs:

- `tools/report_visual_source_of_truth.py`
- `vnext/artifacts/c-phase-55-visual-source-of-truth/visual-source-of-truth.json`
- `vnext/artifacts/c-phase-55-visual-source-of-truth/visual-source-of-truth.md`
- `docs/C_PHASE55_VISUAL_SOURCE_OF_TRUTH_RECOVERY_2026-05-12.md`

Important constraints:

- Do not mutate PNGs, source boards, runtime folders, prop anchors, or content manifests.
- Do not generate or import art.
- Use existing audit tools as evidence.

Validation:

`python .\tools\report_visual_source_of_truth.py --output-root .\vnext\artifacts\c-phase-55-visual-source-of-truth`

`python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-55-visual-source-of-truth\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-55-visual-source-of-truth\runtime-canvas.md --fail-on-mismatch`

`python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-55-visual-source-of-truth\sprite-contract.json`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Commit, push, open PR, and stop. Do not start visual mutation.
=== PROMPT END ===

## C-PHASE 56 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-56-optional-animation-pilot`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE55_VISUAL_SOURCE_OF_TRUTH_RECOVERY_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

Task:

Prepare the optional animation pilot for:

`goose / baby / female / blue / carry_ball_walk`

Required outputs:

- `vnext/artifacts/c-phase-56-optional-animation-pilot/goose-baby-female-blue-carry-ball-walk/`
- `docs/C_PHASE56_OPTIONAL_ANIMATION_PILOT_2026-05-12.md`

Important constraints:

- Do not fabricate art.
- Do not apply candidate frames unless a valid candidate packet already exists and the user explicitly approves apply.
- If no exact candidate exists, stop after creating the source/runtime/fallback proof packet and ask for visual-side candidate creation.
- If apply is approved later, use backup/hash/rollback/post-proof exactly.
- Ball must remain runtime overlay unless the family explicitly does not use ball overlay.

Validation for report-only pilot packet:

`python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\c-phase-56-optional-animation-pilot\optional-readiness.json --markdown .\vnext\artifacts\c-phase-56-optional-animation-pilot\optional-readiness.md`

`python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-56-optional-animation-pilot\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-56-optional-animation-pilot\runtime-canvas.md --fail-on-mismatch`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Commit, push, open PR, and stop. Do not scale to more optional targets.
=== PROMPT END ===

## C-PHASE 57 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-57-tool-hub-simplification`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE42_PET_TASKS_UX_CLARITY_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

Task:

Simplify PET TASKS into one clear command entry and clearer task/result cards while preserving existing safe routing.

Likely files:

- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/tests/Wevito.VNext.Tests/PetCommandParserTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetTaskCardQueueServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetTasksPreviewSmokeTests.cs`

Important constraints:

- No live model calls.
- No new execution modes.
- No special task-completion pet animations.
- Pets must still act like regular pets.
- Do not use test-only names like Bean as live routing assumptions.

Required proof probes:

Run the six no-execution probes listed in C-PHASE 57 of the plan.

Validation:

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Create `docs/C_PHASE57_TOOL_HUB_SIMPLIFICATION_2026-05-12.md`, commit, push, open PR, and stop.
=== PROMPT END ===

## C-PHASE 58 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-58-adapter-proof-packets`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE57_TOOL_HUB_SIMPLIFICATION_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

Task:

Create a repeatable PET TASKS adapter proof runner.

Required outputs:

- `tools/run-pet-task-adapter-proof-pack.ps1`
- `vnext/artifacts/c-phase-58-adapter-proof-packets/adapter-proof-summary.json`
- `vnext/artifacts/c-phase-58-adapter-proof-packets/adapter-proof-summary.md`
- `docs/C_PHASE58_ADAPTER_PROOF_PACKETS_2026-05-12.md`

Important constraints:

- No model calls.
- No screen recording.
- No external audio booster control.
- Proofs must stay in the C-PHASE 58 artifact folder.

Validation:

`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-pet-task-adapter-proof-pack.ps1`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Commit, push, open PR, and stop.
=== PROMPT END ===

## C-PHASE 59 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-59-model-consent-flag`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE46_AI_HELPER_GATE_REVIEW_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

Task:

Add disabled-by-default model capability settings and first-call consent UI. Do not make a live provider call.

Likely files:

- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- `vnext/src/Wevito.VNext.Core/ModelConsentNoticeBuilder.cs`
- `vnext/tests/Wevito.VNext.Tests/ModelConsentNoticeBuilderTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetTaskAdapterPreviewDispatcherTests.cs`

Required defaults:

`pet_model_adapter_enabled=false`

`pet_model_first_call_approved=false`

Important constraints:

- Do not instantiate a live `AnthropicModelAdapter` in Shell.
- Do not read Windows Credential Manager.
- Do not call Anthropic/OpenAI/any provider.
- Use fake/inert adapter paths in tests only.

Validation:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Model|PetTask|Consent|Capability"`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Create `docs/C_PHASE59_MODEL_CAPABILITY_FLAG_AND_CONSENT_UI_2026-05-12.md`, commit, push, open PR, and stop. First live model call requires a later explicit user approval.
=== PROMPT END ===

## C-PHASE 60 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-60-sprite-workflow-v2-compact`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE55_VISUAL_SOURCE_OF_TRUTH_RECOVERY_2026-05-12.md`
- `SPRITE_PIPELINE_KIT/README.md`

Task:

Compact Sprite Workflow V2 so it shows one selected row with source/runtime/candidate/proof lanes.

Likely files:

- `vnext/src/Wevito.VNext.Shell/SpriteWorkflowV2Window.xaml`
- `vnext/src/Wevito.VNext.Shell/SpriteWorkflowV2Window.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowContractTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowContactSheetGeneratorTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowDryRunApplyServiceTests.cs`
- `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowApplyRollbackTests.cs`

Important constraints:

- No generation controls.
- No Gemini/browser controls.
- No automatic apply.
- Apply must require candidate import, dry-run, exact row-id confirmation, and exact mutation scope.
- Window load must not mutate runtime PNGs.

Validation:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteWorkflow"`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Create `docs/C_PHASE60_SPRITE_WORKFLOW_V2_COMPACT_WORKBENCH_2026-05-12.md`, commit, push, open PR, and stop.
=== PROMPT END ===

## C-PHASE 61 Prompt

=== PROMPT START ===
You are working in the Wevito repo:

`C:\Users\fishe\Documents\projects\wevito`

Create branch from fresh `origin/main`:

`claude-implementation/c-phase-61-creative-learning-lab-seed`

Read first:

- `docs/POST_STABLE_FULL_VISION_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`

Task:

Make Creative Learning Lab a reviewed-example index and export surface with no automatic training.

Likely files:

- `vnext/src/Wevito.VNext.Core/LearningLabLabelStore.cs`
- `vnext/src/Wevito.VNext.Core/LearningLabBundleService.cs`
- `vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml.cs`
- `vnext/tests/Wevito.VNext.Tests/LearningLabLabelBundleTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetMemoryRouterTests.cs`
- `vnext/tests/Wevito.VNext.Tests/PetMemoryWriteGateTests.cs`

Important constraints:

- No model/provider calls.
- No training.
- No automatic promotion of labels into model memory.
- Export reviewed bundles only under `vnext/artifacts/creative-learning-lab/`.
- Do not copy private binary assets into bundles unless a later phase explicitly approves it.

Validation:

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LearningLab|PetMemory"`

`dotnet build .\vnext\Wevito.VNext.sln`

`dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`

Create `docs/C_PHASE61_CREATIVE_LEARNING_LAB_SEED_2026-05-12.md`, commit, push, open PR, and stop.
=== PROMPT END ===
