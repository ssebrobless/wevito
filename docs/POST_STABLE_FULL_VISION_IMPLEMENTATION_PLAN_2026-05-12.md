# Post-Stable Full Vision Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Protect the shipped Wevito v0.1.0 desktop baseline, then complete the remaining expanded Wevito vision through safe visual, tool, helper-agent, Sprite Workflow V2, and Creative Learning Lab phases.

**Architecture:** Work from the stable release outward. Every phase starts from fresh `origin/main`, changes one capability lane, produces a report under `docs/`, validates with deterministic commands, opens a PR, and merges only after the phase is green. Risky systems stay preview/report-only until the phase explicitly adds a reviewed approval gate.

**Tech Stack:** Godot 4 desktop pet package, PowerShell build/release scripts, Python sprite/audit tools, .NET 8 WPF vNext Shell/Broker/Core, xUnit tests, GitHub releases/PRs, local JSON/SQLite artifacts.

---

## Current Baseline

```text
stable baseline
|
+-- release tag: v0.1.0-desktop
+-- stable zip: WevitoDesktopPet-v0.1.0-desktop-win64.zip
+-- sha256: c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc
+-- target commit: 97285ad9887423fc42cc3b562087180ff0d8f90e
+-- source candidate: v0.1.0-desktop-rc4
+-- vNext tests: 280/280 passing
+-- runtime sprite contract: 0 mixed, 0 missing, 0 invalid
`-- full expanded vision estimate: about 84%
```

Read these first before any implementation phase:

- `docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`
- `docs/C_PHASE52_STABLE_RELEASE_PROMOTION_2026-05-12.md`
- `docs/C_PHASE50_RC4_MANUAL_PLAYER_QA_2026-05-12.md`
- `docs/C_PHASE49_RC4_PACKAGE_AND_CLEAN_VALIDATION_2026-05-12.md`
- `docs/C_PHASE46_AI_HELPER_GATE_REVIEW_2026-05-12.md`
- `docs/C_PHASE42_PET_TASKS_UX_CLARITY_2026-05-12.md`
- `SPRITE_PIPELINE_KIT/README.md`

## Global Rules

```text
global execution rules
|
+-- always branch from fresh origin/main
+-- one C-PHASE per branch and PR
+-- never work directly on main
+-- no broad sprite mutation unless the phase explicitly allows one scoped target
+-- no live model calls unless a later user approval explicitly opens the gate
+-- no screen recording or external audio booster control by default
+-- no asset-prep regeneration unless C-PHASE 54 guard allows it intentionally
+-- every mutation-capable phase needs backup/hash/rollback/proof
`-- every phase updates docs and validates before PR
```

Branch naming:

```text
claude-implementation/c-phase-54-post-stable-baseline-lock
claude-implementation/c-phase-55-visual-source-of-truth
claude-implementation/c-phase-56-optional-animation-pilot
claude-implementation/c-phase-57-tool-hub-simplification
claude-implementation/c-phase-58-adapter-proof-packets
claude-implementation/c-phase-59-model-consent-flag
claude-implementation/c-phase-60-sprite-workflow-v2-compact
claude-implementation/c-phase-61-creative-learning-lab-seed
```

Default validation after code changes:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Default release/sprite safety checks when a phase touches assets, sprite tooling, or build scripts:

```powershell
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\<phase>\runtime-canvas.json --markdown .\vnext\artifacts\<phase>\runtime-canvas.md --fail-on-mismatch
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\<phase>\sprite-contract.json
python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\<phase>\optional-readiness.json --markdown .\vnext\artifacts\<phase>\optional-readiness.md
```

## Phase Map

```text
post-stable execution
|
+-- C-PHASE 54: lock stable baseline and guard asset prep
|
+-- C-PHASE 55: reconcile visual source-of-truth vs runtime
|
+-- C-PHASE 56: optional animation pilot expansion
|
+-- C-PHASE 57: simplify PET TASKS and tool hub UX
|
+-- C-PHASE 58: prove each tool adapter from stable baseline
|
+-- C-PHASE 59: model capability flag and consent UI
|
+-- C-PHASE 60: compact Sprite Workflow V2 workbench
|
`-- C-PHASE 61: Creative Learning Lab reviewed-data seed
```

## C-PHASE 54 - Post-Stable Baseline Lock And Asset-Prep Safety

**Goal:** Make it difficult to accidentally disturb the stable v0.1.0 visual/runtime baseline.

**Files:**

- Create: `vnext/content/stable_release_lock.json`
- Create: `tools/assert-stable-baseline.ps1`
- Modify: `tools/build-vnext.ps1`
- Create: `docs/C_PHASE54_POST_STABLE_BASELINE_LOCK_2026-05-12.md`

**Exact implementation:**

- [ ] Add `vnext/content/stable_release_lock.json` with:

```json
{
  "schemaVersion": 1,
  "stableTag": "v0.1.0-desktop",
  "stableAsset": "WevitoDesktopPet-v0.1.0-desktop-win64.zip",
  "stableSha256": "c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc",
  "sourceRcTag": "v0.1.0-desktop-rc4",
  "targetCommitish": "97285ad9887423fc42cc3b562087180ff0d8f90e",
  "lockedRuntimeRoots": [
    "sprites_runtime",
    "sprites_shared_runtime"
  ],
  "closedGates": [
    "live_model_calls",
    "broad_sprite_mutation",
    "visual_generation_import",
    "asset_prep_regeneration",
    "prop_anchor_edits",
    "all_color_propagation",
    "screen_recording",
    "external_audio_booster_control",
    "automatic_training_data_promotion"
  ]
}
```

- [ ] Modify `tools/build-vnext.ps1` by adding a new switch parameter:

```powershell
[switch]$AllowAssetPrepAfterStable
```

- [ ] In `tools/build-vnext.ps1`, define:

```powershell
$StableReleaseLock = Join-Path $ProjectRoot "vnext\content\stable_release_lock.json"
```

- [ ] Before the existing non-`$SkipAssetPrep` asset-prep block, add this guard:

```powershell
if (-not $SkipAssetPrep -and (Test-Path $StableReleaseLock) -and -not $AllowAssetPrepAfterStable) {
    throw "Stable release lock is present. Re-run with -SkipAssetPrep, or pass -AllowAssetPrepAfterStable only in an approved asset-prep phase."
}
```

- [ ] Create `tools/assert-stable-baseline.ps1` that:

```powershell
param(
    [string]$ArtifactRoot = ".\vnext\artifacts\c-phase-54-post-stable-baseline-lock"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LockPath = Join-Path $ProjectRoot "vnext\content\stable_release_lock.json"
if (-not (Test-Path $LockPath)) {
    throw "Missing stable release lock: $LockPath"
}

$lock = Get-Content -Raw $LockPath | ConvertFrom-Json
if ($lock.stableTag -ne "v0.1.0-desktop") {
    throw "Unexpected stableTag: $($lock.stableTag)"
}
if ($lock.stableSha256 -ne "c12a61d0ddd55bfa611bc14480dd298584e2e9d3849ea8e01064beb71c4efacc") {
    throw "Unexpected stableSha256: $($lock.stableSha256)"
}

New-Item -ItemType Directory -Force -Path $ArtifactRoot | Out-Null
python (Join-Path $ProjectRoot "tools\report_runtime_canvas_mismatches.py") `
    --output (Join-Path $ArtifactRoot "runtime-canvas.json") `
    --markdown (Join-Path $ArtifactRoot "runtime-canvas.md") `
    --fail-on-mismatch
python (Join-Path $ProjectRoot "tools\audit_sprite_contract.py") `
    --output (Join-Path $ArtifactRoot "sprite-contract.json")
python (Join-Path $ProjectRoot "tools\audit_optional_animation_readiness.py") `
    --output (Join-Path $ArtifactRoot "optional-readiness.json") `
    --markdown (Join-Path $ArtifactRoot "optional-readiness.md")

$summary = [ordered]@{
    stableTag = $lock.stableTag
    stableSha256 = $lock.stableSha256
    runtimeCanvas = "runtime-canvas.json"
    spriteContract = "sprite-contract.json"
    optionalReadiness = "optional-readiness.json"
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 -Path (Join-Path $ArtifactRoot "stable-baseline-summary.json")
Write-Host "Stable baseline assertion passed: $ArtifactRoot"
```

- [ ] Validate that guarded asset-prep fails:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipTests
```

Expected: non-zero exit with `Stable release lock is present`.

- [ ] Validate normal safe build still works:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Expected: pass.

- [ ] Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\assert-stable-baseline.ps1
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

- [ ] Create `docs/C_PHASE54_POST_STABLE_BASELINE_LOCK_2026-05-12.md` with the lock contents, commands run, pass/fail table, and the exact guarded failure text.

**Stop gates:**

- Stop if the safe `-SkipAssetPrep` build fails.
- Stop if `assert-stable-baseline.ps1` reports mixed canvas, missing frames, invalid PNGs, or sprite contract errors.
- Stop if the guard would block `build-release.ps1`; it must only protect asset-prep regeneration.

## C-PHASE 55 - Visual Source-Of-Truth Recovery Plan

**Goal:** Produce a deterministic map of runtime assets, authored verified assets, source boards, optional fallbacks, and missing authored-source coverage.

**Files:**

- Create: `tools/report_visual_source_of_truth.py`
- Create: `docs/C_PHASE55_VISUAL_SOURCE_OF_TRUTH_RECOVERY_2026-05-12.md`
- Output artifacts: `vnext/artifacts/c-phase-55-visual-source-of-truth/`

**Exact implementation:**

- [ ] Create `tools/report_visual_source_of_truth.py` that reads:
  - `sprites_runtime/`
  - `sprites_authored_verified/`
  - `incoming_sprites/`
  - `vnext/content/optional_animation_families.json`
  - `sprites_runtime/_metadata/prop_anchors.json` if present
- [ ] The script must write:
  - `visual-source-of-truth.json`
  - `visual-source-of-truth.md`
- [ ] The JSON must include these top-level keys:

```json
{
  "runtimeRequired": {},
  "authoredVerified": {},
  "sourceBoards": {},
  "optionalFamilies": {},
  "priorityQueues": {}
}
```

- [ ] `priorityQueues` must include:
  - `missing_authored_care`
  - `missing_authored_expression`
  - `optional_fallback_only`
  - `runtime_only_optional`
  - `needs_visual_review`
- [ ] Use existing audit tools as source evidence rather than reimplementing every image check:
  - `audit_sprite_contract.py`
  - `report_authored_sprite_coverage.py`
  - `audit_optional_animation_readiness.py`
  - `report_runtime_canvas_mismatches.py`
- [ ] Do not mutate any PNG, JSON content manifest, source board, or runtime metadata.
- [ ] Run:

```powershell
python .\tools\report_visual_source_of_truth.py --output-root .\vnext\artifacts\c-phase-55-visual-source-of-truth
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-55-visual-source-of-truth\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-55-visual-source-of-truth\runtime-canvas.md --fail-on-mismatch
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-55-visual-source-of-truth\sprite-contract.json
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

- [ ] Create `docs/C_PHASE55_VISUAL_SOURCE_OF_TRUTH_RECOVERY_2026-05-12.md` with:
  - top missing authored-source areas,
  - top optional fallback-only families,
  - recommended first visual review queue,
  - explicit statement that no asset mutation occurred.

**Stop gates:**

- Stop if any runtime audit fails.
- Stop if the script needs to infer missing source by modifying assets.
- Stop if visual generation/import seems necessary; this phase is report-only.

## C-PHASE 56 - Optional Animation Pilot Expansion

**Goal:** Prove one optional animation expansion lane without scaling broadly.

**Default target:** `goose / baby / female / blue / carry_ball_walk`

Reason: the goose baby female blue lane already has prior optional-art history and ball overlay proof context, while `carry_ball_walk` is still fallback-only.

**Files:**

- Use existing services:
  - `vnext/src/Wevito.VNext.Core/SpriteWorkflowCandidateImporter.cs`
  - `vnext/src/Wevito.VNext.Core/SpriteWorkflowDryRunApplyService.cs`
  - `vnext/src/Wevito.VNext.Core/SpriteWorkflowApplyService.cs`
  - `vnext/src/Wevito.VNext.Core/SpriteWorkflowRollbackService.cs`
  - `vnext/src/Wevito.VNext.Core/SpriteWorkflowPostApplyProof.cs`
  - `vnext/src/Wevito.VNext.Core/SpriteWorkflowContactSheetGenerator.cs`
- Add tests only if a service behavior is missing:
  - `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowApplyRollbackTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowContactSheetGeneratorTests.cs`
- Create: `docs/C_PHASE56_OPTIONAL_ANIMATION_PILOT_2026-05-12.md`
- Output artifacts: `vnext/artifacts/c-phase-56-optional-animation-pilot/`

**Exact implementation:**

- [ ] Run C-PHASE 55 first and use its `optional_fallback_only` queue.
- [ ] Build a pilot packet for the target:

```text
vnext/artifacts/c-phase-56-optional-animation-pilot/goose-baby-female-blue-carry-ball-walk/
|-- source-runtime-contact-sheet.png
|-- fallback-reference-contact-sheet.png
|-- target-manifest.json
|-- README.md
`-- decision-needed.md
```

- [ ] If no candidate frames already exist for this exact target, stop after the packet and ask for visual-side candidate creation. Do not fabricate candidate art.
- [ ] If candidate frames exist, verify:
  - frame count matches expected family count,
  - PNGs are RGBA,
  - no ball is baked if the runtime overlay should render the ball,
  - runtime target path is exactly `sprites_runtime/goose/baby/female/blue/carry_ball_walk_*.png`.
- [ ] Dry-run apply and require:
  - exact replacement list,
  - backup-before-apply path,
  - pre/post hashes,
  - no file outside the four or six target frames.
- [ ] Apply only if the user or visual-side explicitly accepts the candidate packet.
- [ ] Always run rollback drill after apply, verify baseline hashes, then re-apply so the branch ends in post-apply state.
- [ ] Run:

```powershell
python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\c-phase-56-optional-animation-pilot\optional-readiness.json --markdown .\vnext\artifacts\c-phase-56-optional-animation-pilot\optional-readiness.md
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-56-optional-animation-pilot\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-56-optional-animation-pilot\runtime-canvas.md --fail-on-mismatch
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

**Stop gates:**

- Stop if candidate hashes differ from manifest.
- Stop if candidate frames include a baked ball when runtime overlay is expected.
- Stop if any mutation scope exceeds the selected optional family.
- Stop if rollback cannot restore exact bytes.

## C-PHASE 57 - Tool Hub Simplification Pass

**Goal:** Make PET TASKS feel like one simple entry point, not a developer dashboard.

**Files:**

- Modify: `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- Modify: `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- Modify: `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- Modify tests:
  - `vnext/tests/Wevito.VNext.Tests/PetCommandParserTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/PetTaskCardQueueServiceTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/PetTasksPreviewSmokeTests.cs`
- Create: `docs/C_PHASE57_TOOL_HUB_SIMPLIFICATION_2026-05-12.md`

**Exact implementation:**

- [ ] Add a single visible command bar near the tool/task section with placeholder:

```text
Ask Scout, Inspector, or Builder to prepare a report...
```

- [ ] The command bar must submit to the existing `PetCommandSubmitted` path.
- [ ] Preserve the three helper-pet limit and existing helper names/roles:
  - Scout: research/docs
  - Inspector: sprite/pet-state review
  - Builder: build/code/checklist planning
- [ ] Do not add special pet animations for task completion. Pets remain normal pets visually.
- [ ] Add or preserve clear status labels:
  - `PREPARE`
  - `PREVIEW`
  - `RUN APPROVED`
  - `REPORT ONLY`
  - `DISABLED`
- [ ] Result cards should show:
  - tool family,
  - helper name,
  - whether execution is disabled, preview-only, or approval-gated,
  - artifact folder path if present.
- [ ] Run tool probes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "summarize the local docs" -ExpectedToolFamily localDocs -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review goose baby female blue sprites" -ExpectedToolFamily spriteAudit -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "inventory assets in sprites_runtime" -ExpectedToolFamily assetInventory -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review pet state" -ExpectedToolFamily petState -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "review the code in Wevito.VNext.Core" -ExpectedToolFamily codeReview -SkipBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\probe-vnext-pet-tasks.ps1 -TaskText "plan a code fix in vnext" -ExpectedToolFamily codePatchPlan -SkipBuild
```

- [ ] Run:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

**Stop gates:**

- Stop if the UI change makes any previously probe-green family route incorrectly.
- Stop if command routing depends on test-only pet names like Bean or Scout-only fixture data.
- Stop if the UI implies live AI/model calls are active.

## C-PHASE 58 - Adapter Proof Packets

**Goal:** Produce repeatable proof packets for all current PET TASKS adapter families.

**Files:**

- Create: `tools/run-pet-task-adapter-proof-pack.ps1`
- Create: `docs/C_PHASE58_ADAPTER_PROOF_PACKETS_2026-05-12.md`
- Output artifacts: `vnext/artifacts/c-phase-58-adapter-proof-packets/`

**Exact implementation:**

- [ ] Create a PowerShell wrapper that runs known-good probe text for:
  - localDocs
  - spriteAudit
  - assetInventory
  - petState
  - codeReview
  - codePatchPlan
  - buildProof
  - translateText
  - audioAssist
  - screenCapture
- [ ] Use corrected no-prefix task text:

```text
localDocs: summarize the local docs
spriteAudit: review goose baby female blue sprites
assetInventory: inventory assets in sprites_runtime
petState: review pet state
codeReview: review the code in Wevito.VNext.Core
codePatchPlan: plan a code fix in vnext
buildProof: run a build proof
translateText: translate Hello goose to Spanish
audioAssist: boost my PC volume
screenCapture: screenshot the Wevito window
```

- [ ] Use flags:
  - buildProof: `-ApproveBeforePreview`
  - translateText: `-ExpectExecuteEnabledAfterPreview`
  - all others: `-SkipBuild`
- [ ] The wrapper must write:
  - `adapter-proof-summary.json`
  - `adapter-proof-summary.md`
  - one trace copy per adapter if available.
- [ ] It must fail non-zero if any expected family differs from actual family.
- [ ] Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-pet-task-adapter-proof-pack.ps1
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

**Stop gates:**

- Stop if screenCapture or audioAssist executes a privacy/system action rather than producing the expected safe preview/proof.
- Stop if any model call is attempted.
- Stop if proofs write into old visual-side artifact folders instead of a new C-PHASE 58 folder.

## C-PHASE 59 - Model Capability Flag And Consent UI

**Goal:** Add a disabled-by-default model capability flag and user-facing consent UI without making a live model call.

**Files:**

- Modify: `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- Modify: `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml`
- Modify: `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs`
- Reuse: `vnext/src/Wevito.VNext.Core/ModelConsentNoticeBuilder.cs`
- Reuse: `vnext/src/Wevito.VNext.Core/PetModelSummaryService.cs`
- Add tests:
  - `vnext/tests/Wevito.VNext.Tests/ModelConsentNoticeBuilderTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/PetTaskAdapterPreviewDispatcherTests.cs`
  - new `vnext/tests/Wevito.VNext.Tests/ModelCapabilitySettingsTests.cs` if needed
- Create: `docs/C_PHASE59_MODEL_CAPABILITY_FLAG_AND_CONSENT_UI_2026-05-12.md`

**Exact implementation:**

- [ ] Add default setting in `ShellCoordinator.ApplyDefaultSettings`:

```csharp
hydrated.TryAdd("pet_model_adapter_enabled", bool.FalseString);
hydrated.TryAdd("pet_model_first_call_approved", bool.FalseString);
```

- [ ] Add a settings row in the Tool Popup settings surface:

```text
AI helper summaries: Disabled
```

- [ ] The UI must show:
  - provider: Anthropic,
  - credential target: `Wevito/anthropic/api-key`,
  - data sent summary,
  - local audit path description,
  - allowlist summary,
  - statement that enabling does not execute tools.
- [ ] Wire only a fake/inert adapter in tests. Do not instantiate `AnthropicModelAdapter` in Shell in this phase unless the code path is unreachable without both settings and a fake test mode.
- [ ] The first-call button must not call a provider. It only persists `pet_model_first_call_approved=true` after the consent copy is shown.
- [ ] Add tests proving:
  - defaults are disabled,
  - preview results do not include model summaries while disabled,
  - consent text mentions provider, credential target, data sent, audit path, and no tool execution,
  - enabling setting with fake adapter can append a fake summary,
  - no live adapter is called in tests.
- [ ] Run:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Model|PetTask|Consent|Capability" 
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

**Stop gates:**

- Stop if the implementation reads Windows Credential Manager.
- Stop if any network/provider call is attempted.
- Stop if personality/helper name can bypass the tool/model allowlist.

## C-PHASE 60 - Sprite Workflow V2 Compact Workbench

**Goal:** Make Sprite Workflow V2 understandable as one selected row with source/runtime/candidate/proof lanes, not a scrolling maze.

**Files:**

- Modify: `vnext/src/Wevito.VNext.Shell/SpriteWorkflowV2Window.xaml`
- Modify: `vnext/src/Wevito.VNext.Shell/SpriteWorkflowV2Window.xaml.cs`
- Reuse tests:
  - `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowContractTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowContactSheetGeneratorTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowDryRunApplyServiceTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/SpriteWorkflowApplyRollbackTests.cs`
- Create: `docs/C_PHASE60_SPRITE_WORKFLOW_V2_COMPACT_WORKBENCH_2026-05-12.md`

**Exact implementation:**

- [ ] Keep the left queue list compact and capped.
- [ ] Show one selected target at a time.
- [ ] The selected target panel must have four lanes:
  - Source authored/verified
  - Runtime current
  - Candidate
  - Proof/rollback
- [ ] Add a target summary line:

```text
species | age | gender | color | family | expected frames | status
```

- [ ] Keep apply disabled until:
  - candidate import succeeded,
  - dry-run succeeded,
  - user typed the exact row id,
  - mutation scope is exact.
- [ ] Add an obvious "Report only / no mutation yet" state before import.
- [ ] Do not add generation, browser, or Gemini controls to this phase.
- [ ] Run:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteWorkflow"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

**Stop gates:**

- Stop if the UI can apply without dry-run.
- Stop if the UI suggests generation/import is automatic.
- Stop if any runtime PNG changes during normal window load.

## C-PHASE 61 - Creative Learning Lab Reviewed-Data Seed

**Goal:** Turn the Creative Learning Lab into a clear reviewed-example index and export surface without uncontrolled training.

**Files:**

- Modify: `vnext/src/Wevito.VNext.Core/LearningLabLabelStore.cs`
- Modify: `vnext/src/Wevito.VNext.Core/LearningLabBundleService.cs`
- Modify: `vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml`
- Modify: `vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml.cs`
- Tests:
  - `vnext/tests/Wevito.VNext.Tests/LearningLabLabelBundleTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/PetMemoryRouterTests.cs`
  - `vnext/tests/Wevito.VNext.Tests/PetMemoryWriteGateTests.cs`
- Create: `docs/C_PHASE61_CREATIVE_LEARNING_LAB_SEED_2026-05-12.md`

**Exact implementation:**

- [ ] Keep label vocabulary:

```text
accept
reject
revise
defer
blocked
```

- [ ] Add a visible statement in the window:

```text
Reviewed examples are saved locally. Nothing trains automatically.
```

- [ ] Add an export action that writes a reviewed bundle under:

```text
vnext/artifacts/creative-learning-lab/<yyyyMMdd-HHmmss>-reviewed-bundle/
```

- [ ] Bundle must contain:
  - `labels.json`
  - `summary.md`
  - `sources.json`
  - no copied private binary assets unless explicitly selected later.
- [ ] Bundle readiness rules:
  - at least one `accept`,
  - zero `blocked`,
  - all exported rows include source path, label, reviewer, notes, created time, schema version.
- [ ] Add tests proving:
  - blocked labels prevent export-ready result,
  - accepted labels are included,
  - no uncontrolled training flag or provider call exists,
  - export path stays under `vnext/artifacts/creative-learning-lab`.
- [ ] Run:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LearningLab|PetMemory"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

**Stop gates:**

- Stop if any provider/model call is introduced.
- Stop if export copies uncontrolled binary assets.
- Stop if labels can be silently promoted into training data.

## Final Integration Phase

After C-PHASE 61, run a short rollup:

```text
C-PHASE 62 - Full Expanded Vision Reassessment
|
+-- update WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md
+-- regenerate dashboard PDFs
+-- run build/test/audits
+-- summarize remaining percent gaps
`-- decide next feature lane
```

Validation:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
python .\tools\report_runtime_canvas_mismatches.py --output .\vnext\artifacts\c-phase-62-rollup\runtime-canvas.json --markdown .\vnext\artifacts\c-phase-62-rollup\runtime-canvas.md --fail-on-mismatch
python .\tools\audit_sprite_contract.py --output .\vnext\artifacts\c-phase-62-rollup\sprite-contract.json
python .\tools\audit_optional_animation_readiness.py --output .\vnext\artifacts\c-phase-62-rollup\optional-readiness.json --markdown .\vnext\artifacts\c-phase-62-rollup\optional-readiness.md
```

## Self-Review

Spec coverage:

- Stable baseline protection is covered by C-PHASE 54.
- Visual/source recovery is covered by C-PHASE 55.
- Optional animations are covered by C-PHASE 56.
- Tool Hub simplification is covered by C-PHASE 57.
- Adapter proofs are covered by C-PHASE 58.
- Model consent and flag are covered by C-PHASE 59.
- Sprite Workflow V2 is covered by C-PHASE 60.
- Creative Learning Lab is covered by C-PHASE 61.

Placeholder scan:

- No phase uses TBD or unspecified mutation scope.
- Every phase has concrete files, commands, stop gates, and report paths.

Risk review:

- C-PHASE 56 is the first phase that may mutate runtime sprites. It must stop for candidate review before apply.
- C-PHASE 59 intentionally does not make a live model call.
- C-PHASE 61 intentionally does not train or promote data automatically.
