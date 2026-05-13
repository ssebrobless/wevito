# Wevito Post-C-PHASE-75 Review and Next-Roadmap Plan

Date: 2026-05-13
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort + project owner review
Companion file: `docs/CLAUDE_POST_CPHASE75_CODEX_PHASE_PROMPTS_2026-05-13.md`
Source plan reviewed: `docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md` and C-PHASE 65–75 phase reports

This document covers:
1. Current-state report (what is actually complete vs scaffolded)
2. Gap analysis (always-on, real local AI, autonomy, UX, safety)
3. Concrete phase roadmap C-PHASE 76 through C-PHASE 85
4. Per-phase goal/tasks/files/safety/validation/gates/PR instructions
5. Where Codex execution prompts live (see companion file)
6. A single copy-paste prompt to begin C-PHASE 76

## 0. Executive Picture

```text
post C-PHASE 75 reality
|
+-- scaffolded spine is broad and disciplined
|   +-- audit ledger (append-only, sqlite triggers)
|   +-- unified policy engine (default-deny, denylist > allowlist)
|   +-- kill switch (settings-backed, every adapter consults it)
|   +-- runtime supervisor + budget meter
|   +-- evidence-packet schema across all adapters
|   +-- guarded mutation gate (sprite + generic targets)
|   +-- approved web research connector (offline default)
|   +-- local model adapter seam (Ollama, deterministic fallback)
|   +-- local training/tuning pipeline (dry-run+eval+rollback)
|   `-- autonomous operations loop (proposal-only, daily-cap, decision-gated)
|
+-- not-yet-functional pieces
|   +-- ONNX embeddings inference (loader yes, tokenizer+forward pass NO)
|   +-- Ollama path requires manual user setup and explicit adapter injection
|   +-- ResearchPlannerService is template-driven, not actually reasoning
|   +-- LoRA stage is plan-only scripts; no real training pipeline
|   +-- Eval set is synthetic; no golden dataset
|   +-- 7-day ledger soak history required for autonomy sign-off does not exist
|   +-- Activity UI is text panels; no "what is Wevito doing right now" surface
|   +-- Real RAG (hybrid retrieval + rerank) is not wired to model adapter
|   `-- No first-class code mutation pilot beyond sprites
|
`-- next phase block (C-PHASE 76 through C-PHASE 85)
    +-- 76 Functional Local ONNX Embeddings
    +-- 77 Activity Dashboard & Live Status UX
    +-- 78 Quiet-Mode Hardening & Always-On Soak Validation
    +-- 79 Local Runtime Onboarding (Ollama bootstrap + ONNX fallback)
    +-- 80 Hybrid Retrieval + Rerank Pipeline
    +-- 81 Local Reasoning Pipeline (RAG-grounded local model)
    +-- 82 Golden Eval Dataset + Hard Regression Gate
    +-- 83 Self-Improvement Daily/Weekly Report Loop
    +-- 84 Guarded Code Mutation Pilot (first reviewed code apply)
    `-- 85 Autonomous Operations Promotion + 7-Day Sign-Off
```

Hard product constraints (unchanged from the C-PHASE 64 plan and reinforced here):

- Wevito must become its own local AI, not a wrapper around hosted GPT/Claude/Gemini
- No hidden web access, no hidden local file access, no hidden training
- No hidden tool execution, no hidden mutation
- Every risky capability remains default-off until reviewed
- Pets remain visually regular pet-sim characters
- Task completion happens under the hood
- Always-on behavior must not interfere with the user's PC experience
- Pause / Quiet / PetOnly / KillSwitch / proof packets / rollback / audit ledger / user-readable activity summaries are all load-bearing

## 1. Current-State Report

### 1.1 What is actually complete

Verified from source + phase reports + test counts.

```text
fully functional (code + tests + safety gates + green)
|
+-- C-PHASE 63 runtime supervisor + quiet/pet-only/fullscreen-quiet
+-- C-PHASE 64 local-first research scaffold + ResearchEvidencePacket
+-- C-PHASE 65 AutonomousTaskScheduler (proposal-only, default-off)
+-- C-PHASE 65 RuntimeBudgetMeter (hourly counter, persisted)
+-- C-PHASE 66 LearningLabPromotionService (accept-only, manifest sha256)
+-- C-PHASE 66 LearningDatasetManifest (versioned vNNNN-yyyyMMdd-HHmmss)
+-- C-PHASE 68 AuditLedgerService (append-only, sqlite UPDATE/DELETE triggers)
+-- C-PHASE 68 EvidencePacket family + ActivitySummaryService
+-- C-PHASE 69 KillSwitchService (settings-backed, every adapter consults it)
+-- C-PHASE 69 IndependentAssistantBetaGateService (decision = label + reasons)
+-- C-PHASE 70 WebResearchConnector (offline default, privacy filter, rate limits)
+-- C-PHASE 70 WindowsCredentialStore (no auto-discovery, target = Wevito/web-search/<id>)
+-- C-PHASE 71 UnifiedPolicyService (denylist > allowlist, traversal/symlink blocked)
+-- C-PHASE 71 LocalToolExecutionPreviewAdapter (dry-run only)
+-- C-PHASE 72 LocalRuntimeProbeService (loopback-only enforced, dormant in Quiet/PetOnly)
+-- C-PHASE 72 OllamaLocalModelAdapter (HTTP POST /v1/chat/completions, safe fallback)
+-- C-PHASE 73 LocalTrainingPlanService + LocalTuningRunner (dry-run+eval-gate+rollback)
+-- C-PHASE 73 RerankHead + RouterConfig + PromptConfigStore (under vnext/content/local-ai/)
+-- C-PHASE 74 GuardedMutationPlan + GuardedMutationService + MutationVerifier
+-- C-PHASE 74 Sprite workflow services delegated to MutationVerifier
+-- C-PHASE 75 AutonomousOperationsLoop (proposal-only, daily cap, ledger-decision gated)
+-- C-PHASE 75 AutonomousBetaDecisionService (refuses without ledger evidence)
`-- test suite: 405/405 green per C-PHASE 75 report
```

### 1.2 What is scaffolded but not truly functional

These pieces compile, pass tests, write evidence packets — but the runtime behavior degrades to a placeholder. They are "structurally correct, semantically pending."

```text
scaffolded, not yet functional
|
+-- OnnxTextEmbeddingService
|   +-- loads the InferenceSession when the .onnx file is present
|   +-- BUT EmbedWithOnnxFallback() always returns DegradeToFallback(...)
|   +-- tokenizer (BertTokenizer) wiring is missing; forward pass is missing
|   +-- result: real embeddings never run; hashing fallback is always used
|   +-- Dimensions property returns 384 only if IsOnnxReady is true; otherwise 16
|
+-- OllamaLocalModelAdapter
|   +-- code path works end-to-end against a real Ollama daemon
|   +-- BUT no install-helper, no model-pull helper, no "test connection" wizard
|   +-- the adapter is selected only when explicitly injected
|   +-- ModelProviderModeService does not auto-route LocalOnly to Ollama
|   +-- result: user installing Ollama themselves and editing settings is the entry path
|
+-- ResearchPlannerService
|   +-- emits structured ResearchEvidencePacket with sources/claims/synthesis
|   +-- BUT claims and synthesis are template strings keyed off counts
|   +-- it does not call OllamaLocalModelAdapter or do real reasoning
|   +-- result: research packets read like reports, but the "synthesis" is canned
|
+-- LocalResearchPreviewAdapter + ModelProviderModeService.RouteSynthesis path
|   +-- accepts an optional IModelAdapter for synthesis
|   +-- BUT no caller wires the OllamaLocalModelAdapter into this path by default
|   +-- result: the "local AI" still falls through to deterministic strings
|
+-- LoRA pilot (C-PHASE 73D)
|   +-- tools/run-unsloth-lora.{ps1,bat} exist as plan files
|   +-- LocalTuningRunner.Run throws for stage=LoraPilot
|   +-- result: real fine-tuning is not implemented; reviewers can see the contract
|
+-- LearningEvalService
|   +-- recall@1, recall@3, mrr, latency_p50/p95 computation exists
|   +-- BUT no checked-in golden dataset
|   +-- BUT no CI gate; eval is only run when a caller invokes the service
|   +-- result: eval can be invoked, but it never gates anything automatically
|
+-- AutonomousBetaDecisionService
|   +-- consumes 7-day ledger snapshot and produces decision label
|   +-- BUT no 7-day soak ledger has been collected yet
|   +-- result: enable_autonomous_beta is never reachable in practice today
|
`-- Activity surface (Settings → Activity panel from C-PHASE 68)
    +-- daily/weekly text tabs exist
    +-- BUT no "live ticker" status line; no per-pet "doing X now" indicator
    +-- result: user has to open Settings to know what Wevito is doing
```

### 1.3 What is default-disabled and why

```text
default-off flags (intentional; do not flip without phase work)
|
+-- pet_model_adapter (provider=none, requires first-call approval)
+-- scheduler_enabled (autonomous scheduler is proposal-only)
+-- web_search_enabled (web research backend = offline)
+-- local_tool_exec_enabled (only dry-run preview when on)
+-- tuning_lora_enabled (LoRA stage is plan-only)
+-- runtime_autonomous_beta_enabled (autonomous ops loop dormant)
+-- runtime_kill_switch (false means active; true blocks everything)
|
why default-off
+-- each flag gates a capability that meaningfully expands blast radius
+-- each flag has a documented enable path
+-- flipping requires a user-approved TaskCard or Settings UI confirmation
`-- the hard-invariant tests pin these defaults
```

### 1.4 Test surface

```text
test suite as of C-PHASE 75 report
|
+-- 405 / 405 green
+-- 13 / 13 autonomous-operations / beta-decision tests
+-- 27 / 27 guarded-mutation / sprite-workflow tests
+-- 27 / 27 ollama / runtime-probe / model-provider tests
+-- 68 / 68 policy / allowlist / access / local-tool tests
+-- 12 / 12 training / tuning / rerank tests
`-- baseline build + smoke (tools/build-vnext.ps1 -SkipAssetPrep -SkipTests) green
```

## 2. Gap Analysis

### 2.1 Always-on readiness gaps

```text
gap                                                                      severity
|
+-- ONNX embedding inference not wired (tokenizer + forward pass missing)   HIGH
+-- no 7-day soak ledger to feed beta gate decision                         HIGH
+-- no automatic re-enable of background work after long idle               MED
+-- no Windows power/sleep handling beyond budget meter rollover            MED
+-- no DPI/multimon smoke-test on Settings UI                               LOW
+-- no crash-recovery beyond persisted budget meter JSON                    MED
+-- no fullscreen-game detection telemetry recorded to ledger               MED
+-- no per-pet "now doing X" status line in the overlay                     HIGH
`-- no global "stop everything" UX surfaced at a corner of every window     MED
```

### 2.2 Real local AI gaps

```text
gap                                                                      severity
|
+-- ONNX embeddings always degrade to hashing                               HIGH
+-- no hybrid retrieval (BM25 + embedding + rerank)                         HIGH
+-- no RAG context construction for local model calls                       HIGH
+-- ResearchPlannerService synthesis is template-driven                     HIGH
+-- ModelProviderModeService LocalOnly does not auto-route to Ollama        MED
+-- no Ollama install / model-pull bootstrap UX                             HIGH
+-- no ONNX-Phi-3 or LLamaSharp in-process fallback adapter                 MED
+-- no citation-enforcement on model outputs (model may invent claims)      HIGH
+-- no prompt-config + system-message profile per helper role               MED
`-- no streaming output to UI                                               LOW
```

### 2.3 Self-improvement gaps

```text
gap                                                                      severity
|
+-- no checked-in golden eval dataset                                       HIGH
+-- no CI / pre-merge regression gate using LearningEvalService             HIGH
+-- no daily/weekly "what changed" report rolling up ledger rows            HIGH
+-- no automatic candidate generation from reviewed bundles into eval set   MED
+-- no A/B history viewer (compare two dataset versions visually)           MED
+-- no LoRA training pipeline (stays plan-only)                             MED
+-- no autonomous rollback when downstream metric drops post-promotion      MED
`-- no per-helper-role eval split (Scout, Inspector, Builder)               LOW
```

### 2.4 UX gaps

```text
gap                                                                      severity
|
+-- Settings (ToolPopupWindow) is growing into a dev-panel feel             HIGH
+-- no single command bar entry per the master plan                         HIGH
+-- no "What is Wevito doing right now?" overlay banner                     HIGH
+-- proof packets are JSON files; no human-readable result cards            MED
+-- no per-pet activity micro-timeline                                      MED
+-- approval flow forces the user to find a card in a list                  MED
+-- helper roles + tool families exposed even when irrelevant               MED
+-- no "explain in plain language what this evidence packet is"             MED
+-- no error / safe-degrade visible signal to the user (only audit lines)   MED
`-- no first-run tutorial / consent walkthrough                             LOW
```

### 2.5 Safety gates that must stay in place

These are the hard invariants that no phase below is allowed to weaken.

```text
hard invariants (locked across C-PHASE 76 through C-PHASE 85)
|
+-- runtime_kill_switch=true halts every adapter, scheduler, loop
+-- default ModelProviderMode = Disabled; LocalOnly is opt-in
+-- web_search_enabled=false default; OfflineWebSearchBackend default
+-- local_tool_exec_enabled=false default; only sha256-allowlisted scripts even when on
+-- tuning_lora_enabled=false default; LoraPilot stays plan-only
+-- runtime_autonomous_beta_enabled=false default
+-- AutonomousTaskScheduler never executes; only Drafts
+-- AutonomousOperationsLoop never mutates; only proposes
+-- GuardedMutationService never applies without an approved TaskCard
+-- UnifiedPolicyService denylist beats allowlist; traversal/symlink blocked
+-- AuditLedgerService is append-only (sqlite triggers); never UPDATE/DELETE
+-- every local model adapter must support deterministic fallback
+-- local model adapters never read hosted-provider API keys
+-- only loopback endpoints permitted for local runtime
+-- every evidence packet has did_use_network/hosted_ai/local_model/mutate set
+-- no helper / no adapter / no service may call a hosted AI in LocalOnly mode
`-- pets remain visually regular pet-sim characters; no task animation
```

### 2.6 Mapping gaps to phases

```text
gap                                              first addressed in
|
+-- ONNX embedding inference                     C-PHASE 76
+-- "What is Wevito doing right now?" UX         C-PHASE 77
+-- 7-day soak ledger collection                 C-PHASE 78
+-- Windows fullscreen / power handling          C-PHASE 78
+-- Ollama install / model-pull UX               C-PHASE 79
+-- ONNX-Phi-3 / LLamaSharp fallback             C-PHASE 79
+-- Hybrid retrieval BM25 + embedding + rerank   C-PHASE 80
+-- RAG context for local model calls            C-PHASE 81
+-- ResearchPlannerService real synthesis        C-PHASE 81
+-- Citation enforcement                         C-PHASE 81
+-- Golden eval dataset                          C-PHASE 82
+-- CI regression gate                           C-PHASE 82
+-- Daily/weekly "what changed" report           C-PHASE 83
+-- A/B history viewer                           C-PHASE 83
+-- First reviewed code mutation                 C-PHASE 84
+-- Autonomous operations sign-off + promotion   C-PHASE 85
`-- LoRA pilot real run                          deferred (post-85, optional)
```

## 3. Roadmap C-PHASE 76 → C-PHASE 85

Each phase follows the C-PHASE-65-to-75 shape:

```text
Goal
Scope (exact)
Files likely touched
New files
Tests
Validation commands
Artifacts / reports
Stop gates
Rollback plan
Commit / PR instructions
Auto-continue?
```

### C-PHASE 76 — Functional Local ONNX Embeddings

Goal: real semantic embeddings with `bge-micro-v2` ONNX + a checked-in BERT tokenizer wiring, so the eval loop, retrieval pipeline, and learning promotions stop being theatrical.

Scope:

- Replace `EmbedWithOnnxFallback` in `OnnxTextEmbeddingService` with a real implementation:
  - load tokenizer (vocab.txt + tokenizer.json or BertTokenizer's expected vocab format)
  - encode inputs → `input_ids`, `attention_mask`, `token_type_ids` (int64 tensors)
  - run the loaded `InferenceSession` with these inputs
  - mean-pool the `last_hidden_state` over `attention_mask` to produce a 384-dim vector
  - L2-normalize the vector
- Prefer `Microsoft.SemanticKernel.Connectors.Onnx.BertOnnxTextEmbeddingGenerationService` as the engine (this is what `SmartComponents.LocalEmbeddings` now wraps); wrap it behind the existing `ITextEmbeddingService` so call sites do not change.
- Update `tools/install-local-embedder.ps1` to also pull `tokenizer.json` and `vocab.txt`; write a sha256 manifest covering all three files.
- `OnnxTextEmbeddingService.IsOnnxReady` now requires model + tokenizer + warm load.
- Safe degrade: any tokenizer/inference failure emits one audit line `embedding-degrade | <reason>` and falls back to `HashingTextEmbeddingService` deterministically. No throw escapes.
- Versioned migration: when `PetMemoryStore.EmbeddingDimensions` changes between hashing (16) and ONNX (384), existing DB is renamed `<pet-id>.legacy-<ts>.db` and a fresh DB is created (already implemented; verify still works after this change).

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs
vnext/src/Wevito.VNext.Core/Wevito.VNext.Core.csproj   (add SemanticKernel.Connectors.Onnx if used)
vnext/src/Wevito.VNext.Core/PetMemoryStore.cs           (only if migration touch-up needed)
vnext/content/local-models/embeddings/bge-micro-v2/README.md
tools/install-local-embedder.ps1
vnext/content/tool_definitions.json                     (pet_memory entry: embeddingProvider when ONNX active)
```

New files:

```text
vnext/src/Wevito.VNext.Core/OnnxEmbeddingBackend.cs      (the actual SK-wrapped or hand-rolled inference impl)
vnext/tests/Wevito.VNext.Tests/OnnxEmbeddingBackendTests.cs
vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/README.md
vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/model.onnx     (tiny deterministic ONNX, ≤ 100 KB)
vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/tokenizer.json
docs/C_PHASE76_FUNCTIONAL_LOCAL_ONNX_EMBEDDINGS_2026-05-13.md
```

Tests:

- Two identical inputs produce identical 384-dim vectors (cache + L2 deterministic).
- Two different inputs produce different vectors with cosine ≠ 1.0.
- Missing `tokenizer.json` triggers a single audit line + hashing fallback.
- Missing `model.onnx` triggers a single audit line + hashing fallback.
- Corrupt session triggers fallback (use the existing `_sessionFactory` injection).
- Dimensions: 384 when ONNX ready, 16 when degraded.
- `PetMemoryStore` migrates `<pet-id>.db` → `<pet-id>.legacy-<ts>.db` when the dimension changes.
- No network call: assert via an `HttpClient` test double that throws on any send.
- Tests use a tiny fixture ONNX checked in under `fakes/test-embedder/` (no Hugging Face fetch).

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Embedding|PetMemory"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-embedding-warmup/warmup-report.json
vnext/artifacts/pet-tasks/<ts>-embedding-warmup/run-summary.md
docs/C_PHASE76_FUNCTIONAL_LOCAL_ONNX_EMBEDDINGS_2026-05-13.md
```

Stop gates:

- Stop if any test path opens a network connection.
- Stop if a corrupt or missing tokenizer throws (must degrade).
- Stop if hashing fallback path stops working after this change.
- Stop if memory DB upgrade silently re-embeds old rows (must rebuild fresh).
- Stop if embedding output is not L2-normalized.

Rollback: revert PR; degraded hashing path resumes.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-76-functional-local-onnx-embeddings`
- Commit messages reference `C-PHASE 76`
- PR title: `C-PHASE 76: Functional local ONNX embeddings (bge-micro-v2)`

Auto-continue? **No.** First real ML inference path. Stop for user review.

### C-PHASE 77 — Activity Dashboard & Live Status UX

Goal: give the user a one-glance answer to "what is Wevito doing right now?" without opening Settings or reading JSON.

Scope:

- Add a compact, non-intrusive `OverlayStatusBanner` to the existing overlay (small text strip; auto-fades when idle for > 60s).
  - shows: `Last action: <kind> at <hh:mm> · today: <N> previews, <M> approvals, 0 mutations`
  - clicking opens the existing Activity panel
  - hidden in `Quiet` and `PetOnly` modes
  - never steals focus (`WS_EX_NOACTIVATE` already used on overlay; reuse)
- Add `LiveStatusFeed` in Core:
  - reads the audit ledger every 10s (default; settings-driven)
  - emits an `OverlayStatusSnapshot { LastPacketKind, LastAtUtc, TodayCounts, FlaggedRows }`
  - never embeds private text — only counts + kinds + timestamps
- Add a `PlainLanguageExplainer` service:
  - takes an evidence packet kind and returns a one-line human-readable sentence
  - for example: `web_fetch` → "Fetched 3 web research records from the Brave backend."
  - used by both the overlay banner and the Activity panel
- Activity panel rework:
  - daily and weekly tabs stay
  - each row shows `[kind icon] [plain-language sentence] [open artifact] [open packet]`
  - rows with `did_use_network`, `did_use_hosted_ai`, or `did_mutate` get a small badge
- Add a single global "Stop everything" toggle to the corner of the overlay (calls `KillSwitchService.SetActive(true)`).

Files likely touched:

```text
vnext/src/Wevito.VNext.Shell/RoamBandWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs
```

New files:

```text
vnext/src/Wevito.VNext.Core/LiveStatusFeed.cs
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Core/OverlayStatusSnapshot.cs
vnext/src/Wevito.VNext.Shell/OverlayStatusBannerView.xaml(.cs)
vnext/tests/Wevito.VNext.Tests/LiveStatusFeedTests.cs
vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs
docs/C_PHASE77_ACTIVITY_DASHBOARD_AND_LIVE_STATUS_UX_2026-05-13.md
```

Tests:

- Feed returns empty snapshot when ledger is empty.
- Feed groups by day correctly (mocked clock, multiple rows on multiple days).
- Explainer covers every known packet kind (parametric test) — fail closed on unknown kind (return `Unknown {kind}` with a `WarnUnknownPacketKind` event for future coverage).
- Banner hides when `RuntimeSupervisorStatus.Mode != Active`.
- Banner never raises focus events (assert `WindowsGraphicsCapture` not triggered, `WS_EX_NOACTIVATE` set).
- Stop-everything toggle persists `runtime_kill_switch=true` and downstream tests that check kill-switch observance still pass.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LiveStatus|PlainLanguage|ActivitySummary"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-live-status-smoke/snapshot-sample.json
docs/C_PHASE77_ACTIVITY_DASHBOARD_AND_LIVE_STATUS_UX_2026-05-13.md
```

Stop gates:

- Stop if banner can ever steal focus (run smoke probe `tools/probe-pet-tasks-preview.ps1`).
- Stop if banner displays private text (only kinds, counts, plain-language sentences).
- Stop if explainer crashes on an unknown packet kind (must return a safe placeholder).
- Stop if "Stop everything" toggle is not honored by any adapter (regression test sweep).

Rollback: revert PR; existing Settings → Activity panel is unaffected.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-77-activity-dashboard-and-live-status-ux`
- PR title: `C-PHASE 77: Activity dashboard + live status overlay`

Auto-continue? **Yes** if all tests + smoke pass and no focus-steal events are recorded in the smoke run.

### C-PHASE 78 — Quiet-Mode Hardening & Always-On Soak Validation

Goal: prove the system can be left running for a multi-hour window without violating supervisor rules, without focus-steal, without budget overruns, and that the ledger collects enough rows for the autonomous-beta decision.

Scope:

- Add `WindowsForegroundFullscreenMonitor`:
  - polls the foreground window's exstyle / dwm flags every 2s
  - sets `RuntimeSupervisorStatus.IsFullscreenSession=true` when a true-fullscreen window is foreground for ≥ 5s
  - sets it back to false after ≥ 15s of non-fullscreen
  - writes a `fullscreen_state_change` audit row when the state toggles
- Add `WindowsPowerHandler` (listens to `Microsoft.Win32.SystemEvents.PowerModeChanged` and `SessionSwitch`):
  - sleep → supervisor mode forced to `Quiet`, kill background work
  - resume → record `power_resume` audit row, do not auto-switch back to Active (user does)
  - lock screen → equivalent to sleep for our purposes
- Add `BudgetMeterPersistencePolicy`:
  - flush every 5 min to `%LOCALAPPDATA%/Wevito/audit/budget-meter.json`
  - on startup, refuse to start background work for 60s while the previous budget snapshot is replayed
- Add `SoakRunnerService` (off by default, opt-in for a single session):
  - holds the runtime in `Active` for a configurable window
  - schedules a thin loop: every 10 min, generate one `local_research` preview using only local docs
  - never enables any not-default capability
  - emits a `soak_session` packet at start and at end summarizing budget usage, focus-steal count, and ledger row totals
- Add a focus-steal counter: if any Wevito window receives a `WM_ACTIVATE` while another fullscreen app holds foreground, increment a counter; the soak summary must end with 0.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs
vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/src/Wevito.VNext.Shell/RoamBandWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs
```

New files:

```text
vnext/src/Wevito.VNext.Core/WindowsForegroundFullscreenMonitor.cs
vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
vnext/src/Wevito.VNext.Core/FocusStealCounter.cs
vnext/src/Wevito.VNext.Core/SoakRunnerService.cs
vnext/tests/Wevito.VNext.Tests/WindowsForegroundFullscreenMonitorTests.cs
vnext/tests/Wevito.VNext.Tests/WindowsPowerHandlerTests.cs
vnext/tests/Wevito.VNext.Tests/FocusStealCounterTests.cs
vnext/tests/Wevito.VNext.Tests/SoakRunnerServiceTests.cs
tools/run-soak-validation.ps1
docs/C_PHASE78_QUIET_HARDENING_AND_SOAK_VALIDATION_2026-05-13.md
```

Tests:

- Foreground monitor flips state only after the 5s / 15s thresholds (mocked clock).
- Power handler sleep → supervisor forced to Quiet, all schedulers dormant.
- Power resume does not auto-activate background work.
- Budget meter survives a kill -9 simulation (file present after, counts honored).
- Focus-steal counter increments only when WM_ACTIVATE arrives while foreground is fullscreen-other.
- Soak runner refuses to enable web/file/tool/mutation surfaces by default.
- Soak runner emits start + end packets; ledger snapshot post-run shows zero policy violations.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Fullscreen|Power|FocusSteal|Soak"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Manual soak (must be done before C-PHASE 85):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-soak-validation.ps1 -Hours 4 -ArtifactRoot .\vnext\artifacts\soak\
```

Artifacts:

```text
vnext/artifacts/soak/<ts>-soak-session/soak-summary.json
vnext/artifacts/soak/<ts>-soak-session/run-summary.md
%LOCALAPPDATA%/Wevito/audit/ledger.sqlite (rows accumulate)
docs/C_PHASE78_QUIET_HARDENING_AND_SOAK_VALIDATION_2026-05-13.md
```

Stop gates:

- Stop if focus-steal counter is not zero in any 4-hour soak.
- Stop if budget meter exceeds configured thresholds by > 10%.
- Stop if supervisor fails to enter Quiet on system sleep.
- Stop if soak runner can enable any default-off capability.
- Stop if a fullscreen-state change does not emit a ledger row.

Rollback: revert PR; soak runner remains optional and off by default.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-78-quiet-hardening-and-soak-validation`
- PR title: `C-PHASE 78: Quiet-mode hardening + soak validation harness`

Auto-continue? **No.** Requires the user to run the soak.

### C-PHASE 79 — Local Runtime Onboarding

Goal: make the local model path reachable for a normal user (install Ollama, pull a default model, test the connection, switch to LocalOnly mode). Add a documented in-process fallback adapter (ONNX-Phi-3 or LLamaSharp) behind a feature flag so the local AI can still run when Ollama is unavailable.

Scope:

- Add `tools/install-local-runtime.ps1`:
  - checks if Ollama is installed (winget probe + path probe)
  - if not, prompts the user once and runs `winget install Ollama.Ollama` (user-approved)
  - runs `ollama pull llama3.2:3b` (or the model id in settings)
  - prints the resulting endpoint, model id, and version
  - never writes outside `tools/` and `%LOCALAPPDATA%/Wevito/runtime-install/`
- Add `LocalRuntimeOnboardingService` in Core:
  - exposes `Status()` → `Installed`, `ModelPresent`, `EndpointReachable`, `LastProbeAtUtc`
  - exposes `InstallPlan()` → a list of safe, user-approved steps the install script will take
  - emits a `runtime_onboarding` evidence packet per status check
- Wire `ModelProviderModeService` so that when `pet_model_mode=LocalOnly`:
  - it prefers `OllamaLocalModelAdapter` (existing) when probe says available
  - otherwise it routes to `LocalModelAdapter` (deterministic) with a visible degrade label
  - never silently switches to a hosted adapter
- Add an in-process fallback adapter behind a feature flag (`local_runtime_inproc_enabled=false` default):
  - `OnnxPhiLocalModelAdapter` using `Microsoft.ML.OnnxRuntime.GenAI` + DirectML (Phi-3-mini-4k INT4)
  - on missing weights: degrades deterministically, audit line, no throw
  - documented as opt-in; not wired into default path
- Settings UI: Local AI panel now shows:
  - "Status: Ollama installed / running / model present"
  - "Probe now" + "Install Ollama" + "Pull model" buttons (each requires confirmation)
  - "Local runtime: Ollama HTTP / ONNX in-process / Deterministic only" mode
  - "What model is Wevito using right now?" status line is mandatory and live

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/ModelProviderMode.cs
vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs
vnext/src/Wevito.VNext.Core/LocalRuntimeProbeService.cs
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
vnext/content/tool_definitions.json
```

New files:

```text
vnext/src/Wevito.VNext.Core/LocalRuntimeOnboardingService.cs
vnext/src/Wevito.VNext.Core/OnnxPhiLocalModelAdapter.cs
vnext/tests/Wevito.VNext.Tests/LocalRuntimeOnboardingServiceTests.cs
vnext/tests/Wevito.VNext.Tests/OnnxPhiLocalModelAdapterTests.cs
tools/install-local-runtime.ps1
docs/C_PHASE79_LOCAL_RUNTIME_ONBOARDING_2026-05-13.md
```

Tests:

- Onboarding service returns `Installed=false, ModelPresent=false, EndpointReachable=false` when both probes return failure (mocked).
- Onboarding refuses to run any external command on its own; it only proposes a plan.
- ModelProviderMode auto-routes LocalOnly to Ollama when available, otherwise to deterministic fallback (never to a hosted adapter).
- OnnxPhi adapter degrades to deterministic fallback when weights are missing.
- Feature flag default-off: in-process adapter is not selected unless `local_runtime_inproc_enabled=true`.
- Probe is dormant in Quiet/PetOnly (regression of C-PHASE 72).
- KillSwitch blocks every onboarding action.
- The Settings UI shows the live model id, runtime id, last probe time.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Onboarding|OnnxPhi|Ollama|LocalRuntime|ModelProvider"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-runtime-onboarding/onboarding-status.json
vnext/artifacts/pet-tasks/<ts>-runtime-onboarding/run-summary.md
%LOCALAPPDATA%/Wevito/runtime-install/install-log-<ts>.txt   (only when the install script runs)
docs/C_PHASE79_LOCAL_RUNTIME_ONBOARDING_2026-05-13.md
```

Stop gates:

- Stop if any code path silently downloads a model or installs Ollama without an approval gesture.
- Stop if LocalOnly is ever routed to a hosted adapter.
- Stop if the in-process adapter is selected by default.
- Stop if "What model is Wevito using right now?" can ever be blank when LocalOnly is on.

Rollback: revert PR; Ollama-only path resumes.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-79-local-runtime-onboarding`
- PR title: `C-PHASE 79: Local runtime onboarding + in-process fallback`

Auto-continue? **No.** Adds a real-runtime UX surface.

### C-PHASE 80 — Hybrid Retrieval + Rerank Pipeline

Goal: when answering a research/code-review question, Wevito retrieves the right local context — hybrid (BM25 + dense embedding) + a small local rerank head — and exposes the retrieved chunks as the model's context.

Scope:

- Add `LocalRetrievalService`:
  - dense retrieval from `PetMemoryStore` via `sqlite-vec`
  - keyword retrieval from `PetMemoryStore` via FTS5 (BM25)
  - reciprocal-rank fusion (RRF) combining both lists (k=60 default)
  - top-N candidates piped through `RerankHead` (from C-PHASE 73)
  - exposes `RetrievalResult { Chunks, Scores, MethodTrace }`
- Document store integration:
  - chunk size default 512 tokens with 64-token overlap
  - each chunk gets `chunk_id`, `doc_id`, `path`, `sha256`, `chunked_at_utc`, `embedding`
  - chunker writes into a new table `pet_doc_chunks` with FTS5 mirror
  - chunker honors `UnifiedPolicyService.EvaluateRead` per source path
- Add a small `LocalDocumentIngestService`:
  - takes a list of paths (e.g., the `docs/` folder)
  - calls the chunker
  - writes a `local_doc_ingest` evidence packet under `vnext/artifacts/pet-tasks/<ts>-doc-ingest/`
- Wire retrieval into:
  - `LocalResearchPreviewAdapter` (already takes a `ResearchPlannerService`)
  - `CodeReviewPreviewAdapter`
  - `AssetInventoryPreviewAdapter` (read-only doc lookup)

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/PetMemoryStore.cs              (add pet_doc_chunks table + FTS5)
vnext/src/Wevito.VNext.Core/ResearchPlannerService.cs      (accept retrieved chunks)
vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/CodeReviewPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/AssetInventoryPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/RerankHead.cs
vnext/content/tool_definitions.json
```

New files:

```text
vnext/src/Wevito.VNext.Core/LocalRetrievalService.cs
vnext/src/Wevito.VNext.Core/LocalDocumentIngestService.cs
vnext/src/Wevito.VNext.Core/RetrievalChunk.cs
vnext/src/Wevito.VNext.Core/RetrievalResult.cs
vnext/src/Wevito.VNext.Core/ReciprocalRankFusion.cs
vnext/tests/Wevito.VNext.Tests/LocalRetrievalServiceTests.cs
vnext/tests/Wevito.VNext.Tests/LocalDocumentIngestServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ReciprocalRankFusionTests.cs
docs/C_PHASE80_HYBRID_RETRIEVAL_AND_RERANK_2026-05-13.md
```

Tests:

- Dense + BM25 fusion improves recall on a fixed mini-dataset vs either alone.
- Chunker honors `UnifiedPolicyService` — denylisted paths are skipped with a `local_access_blocked` audit row.
- Chunker writes chunk sha256 and a `last_chunked_at_utc` timestamp.
- Rerank head reorders results deterministically with the head's stored weights.
- Retrieval respects KillSwitch (returns empty + audit line).
- No network call (assert via fake HTTP client).

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Retrieval|Chunk|Rerank|Fusion|DocIngest"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-doc-ingest/manifest.json
vnext/artifacts/pet-tasks/<ts>-retrieval/<query-hash>/retrieval-trace.json
docs/C_PHASE80_HYBRID_RETRIEVAL_AND_RERANK_2026-05-13.md
```

Stop gates:

- Stop if any chunk is ingested without a `UnifiedPolicyService` decision row.
- Stop if rerank head can re-write itself (rerank head config is read-only at runtime; tuning is C-PHASE 73's path).
- Stop if retrieval returns content from outside the approved roots.
- Stop if FTS5 + sqlite-vec migrations are not idempotent.

Rollback: revert PR; delete the new `pet_doc_chunks` table (or leave; nothing reads it).

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-80-hybrid-retrieval-and-rerank`
- PR title: `C-PHASE 80: Hybrid retrieval + local rerank pipeline`

Auto-continue? **No.** First real retrieval surface.

### C-PHASE 81 — Local Reasoning Pipeline

Goal: make local research and code-review previews actually use the local model (when available) with retrieved chunks as grounded context, enforce citation references, and degrade safely when no local model is available.

Scope:

- Add `LocalReasoningService`:
  - inputs: question, retrieved chunks (from C-PHASE 80), helper role, tool family, trusted/untrusted context
  - builds a prompt: system message per helper role + user message embedding `[1] chunk, [2] chunk, ...` blocks with citation markers
  - calls the active `IModelAdapter` (Ollama when available; otherwise deterministic)
  - enforces citations: every claim sentence in the output must reference at least one chunk id; sentences without a citation marker are stripped and replaced with a `(needs citation)` placeholder
  - records a `local_reasoning` evidence packet with: prompt sha256, response sha256, model id, retrieved chunk ids, citation_coverage_ratio
- Update `ResearchPlannerService.BuildClaims` / `BuildSynthesis` so that when a `LocalReasoningService` is provided, it produces real claims with chunk-id provenance instead of templates.
- Wire `LocalReasoningService` into:
  - `LocalResearchPreviewAdapter` (existing model-routing path)
  - `CodeReviewPreviewAdapter`
- Refuse to use hosted-AI in this phase — even if the user has approved a hosted adapter, `LocalOnly` mode is enforced at the service level for these previews.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/ResearchPlannerService.cs
vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/CodeReviewPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/ModelProviderMode.cs
vnext/src/Wevito.VNext.Core/PromptConfigStore.cs
vnext/content/local-ai/prompt-config.json   (default prompts/system messages)
```

New files:

```text
vnext/src/Wevito.VNext.Core/LocalReasoningService.cs
vnext/src/Wevito.VNext.Core/CitationEnforcer.cs
vnext/src/Wevito.VNext.Core/LocalReasoningEvidencePacket.cs
vnext/tests/Wevito.VNext.Tests/LocalReasoningServiceTests.cs
vnext/tests/Wevito.VNext.Tests/CitationEnforcerTests.cs
docs/C_PHASE81_LOCAL_REASONING_PIPELINE_2026-05-13.md
```

Tests:

- Reasoning service uses the deterministic adapter when no local model is available (regression of C-PHASE 72 safe degrade).
- Reasoning service refuses to call a hosted adapter even when one is supplied.
- Citation enforcer removes sentences without `[N]` references; keeps sentences with valid `[N]` ids that match retrieved chunks.
- Citation coverage ratio is recorded in the packet and surfaced in the activity panel.
- Empty retrieval (no chunks) → service returns a "no local evidence" deterministic synthesis with `citation_coverage_ratio=0`.
- KillSwitch blocks the service.
- No network call (assert via fake HTTP client throwing on send).

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalReasoning|Citation"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-local-reasoning/reasoning-packet.json
vnext/artifacts/pet-tasks/<ts>-local-reasoning/run-summary.md
docs/C_PHASE81_LOCAL_REASONING_PIPELINE_2026-05-13.md
```

Stop gates:

- Stop if reasoning ever calls a hosted adapter.
- Stop if a packet's `citation_coverage_ratio` field is missing.
- Stop if a sentence with `[N]` that references a chunk id not in the retrieval set is kept.
- Stop if KillSwitch is not honored.

Rollback: revert PR; ResearchPlannerService falls back to template synthesis.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-81-local-reasoning-pipeline`
- PR title: `C-PHASE 81: Local reasoning pipeline (RAG + citation enforcement)`

Auto-continue? **No.** First real-AI-on-real-content surface.

### C-PHASE 82 — Golden Eval Dataset + Hard Regression Gate

Goal: build a checked-in golden dataset, run it on every PR via the existing `LearningEvalService`, and refuse to merge configurations or datasets that regress retrieval recall or citation coverage.

Scope:

- Author a small (~50–100 row) golden dataset under `vnext/content/local-ai/eval-golden/`:
  - `questions.json` — questions + expected source chunk ids + a short rubric
  - `documents/` — the documents the questions reference (under `vnext/content/local-ai/eval-golden/documents/`)
  - all content is synthetic, local, license-free, and reviewed
- Extend `LearningEvalService` with `RunGoldenEval(dataset_version, retrieval_service)`:
  - returns metrics `recall@1`, `recall@3`, `mrr`, `citation_coverage_ratio`, latency_p50, latency_p95
- Add `EvalRegressionGate`:
  - baseline metrics stored in `vnext/content/local-ai/eval-golden/baseline.json` (committed)
  - on each invocation compares current vs baseline
  - returns `regression=true` if `recall@1` drops > 2%, `mrr` drops > 2%, or `citation_coverage_ratio` drops > 5%
  - emits a `golden_eval` evidence packet
- Add `tools/run-golden-eval.ps1` for the CI/local invocation.
- Wire the gate into the existing `LocalTuningRunner` so any tuning apply is also gated by `EvalRegressionGate` against the golden set.
- Document a CI workflow stub (`.github/workflows/local-eval.yml`) that runs the gate on PRs touching `vnext/content/local-ai/` or `vnext/src/Wevito.VNext.Core/*.cs`. Commit the workflow file but do not enable a required branch protection in this phase.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/LearningEvalService.cs
vnext/src/Wevito.VNext.Core/LocalTuningRunner.cs
vnext/content/tool_definitions.json
```

New files:

```text
vnext/src/Wevito.VNext.Core/EvalRegressionGate.cs
vnext/content/local-ai/eval-golden/questions.json
vnext/content/local-ai/eval-golden/documents/example-001.md
vnext/content/local-ai/eval-golden/documents/example-002.md
vnext/content/local-ai/eval-golden/baseline.json
tools/run-golden-eval.ps1
.github/workflows/local-eval.yml
vnext/tests/Wevito.VNext.Tests/EvalRegressionGateTests.cs
vnext/tests/Wevito.VNext.Tests/GoldenEvalSmokeTests.cs
docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md
```

Tests:

- Gate flags a regression when fed a deliberately worse retrieval head.
- Gate passes when fed the baseline.
- Tuning apply rolls back when the gate flags a regression (regression of C-PHASE 73 rollback path).
- No network call in eval.
- Golden dataset is parseable and complete (every question references real chunk ids).
- KillSwitch blocks gate execution.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalRegression|GoldenEval|LearningEval|Tuning"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-golden-eval.ps1 -ArtifactRoot .\vnext\artifacts\eval-golden\
```

Artifacts:

```text
vnext/artifacts/eval-golden/<ts>-golden-eval/eval-report.json
vnext/artifacts/eval-golden/<ts>-golden-eval/run-summary.md
vnext/content/local-ai/eval-golden/baseline.json   (updated only by an explicit reviewer action)
docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md
```

Stop gates:

- Stop if baseline can be overwritten without a reviewer action (must require explicit `--accept-new-baseline` flag).
- Stop if the gate can pass with `citation_coverage_ratio < 0.6` (lower bound enforced).
- Stop if the gate is bypassable when `tuning_lora_enabled=false`.
- Stop if the workflow file enables required-branch-protection automatically.

Rollback: revert PR; remove the workflow file if present.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-82-golden-eval-and-regression-gate`
- PR title: `C-PHASE 82: Golden eval dataset + hard regression gate`

Auto-continue? **No.** First gating mechanism on PR-level changes.

### C-PHASE 83 — Self-Improvement Daily/Weekly Report Loop

Goal: produce a human-readable rolling report of "what Wevito did, what it learned, what changed" with rollback hooks for any regression, surfaced in the existing Activity panel.

Scope:

- Add `SelfImprovementReportService`:
  - reads ledger rows for a window (default: last 24h, last 7d)
  - groups by packet kind
  - highlights:
    - new dataset versions promoted
    - tuning applies + their eval deltas
    - mutations applied (always linked to proof packets)
    - any flagged rows (`did_use_network=true`, `did_use_hosted_ai=true`, `did_mutate=true`)
  - writes a markdown report under `vnext/artifacts/pet-tasks/<ts>-self-improvement-report/`
- Add `RollbackProposalService`:
  - if a tuning apply is followed by an eval regression within 24h, the service emits a `rollback_proposal` packet
  - rollback never executes automatically; it produces a TaskCard in `Draft` status linking the apply packet to the rollback step
- Activity panel adds a "Self-improvement" tab showing the latest report.
- Add `tools/run-self-improvement-report.ps1` for manual invocation; the autonomous operations loop optionally generates one per iteration (still proposal-only).

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs
vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml(.cs)
```

New files:

```text
vnext/src/Wevito.VNext.Core/SelfImprovementReportService.cs
vnext/src/Wevito.VNext.Core/RollbackProposalService.cs
vnext/tests/Wevito.VNext.Tests/SelfImprovementReportServiceTests.cs
vnext/tests/Wevito.VNext.Tests/RollbackProposalServiceTests.cs
tools/run-self-improvement-report.ps1
docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md
```

Tests:

- Report contains zero flagged rows when offline mode held for the entire window.
- Report groups + counts packet kinds correctly across multi-day windows (mocked clock).
- Rollback proposal is emitted only when a regression follows an apply within 24h.
- Rollback proposal is never auto-executed; only proposed.
- KillSwitch blocks both services.
- All rendered text is plain English, no JSON dumps in the markdown.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SelfImprovement|RollbackProposal"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-self-improvement-report/report.md
vnext/artifacts/pet-tasks/<ts>-self-improvement-report/report.json
vnext/artifacts/pet-tasks/<ts>-rollback-proposal/proposal.json
docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md
```

Stop gates:

- Stop if any rollback runs automatically.
- Stop if a flagged row is not surfaced in the report.
- Stop if private text appears in a report (only kinds, counts, packet links, plain-language sentences).

Rollback: revert PR.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-83-self-improvement-report-loop`
- PR title: `C-PHASE 83: Self-improvement daily/weekly report + rollback proposals`

Auto-continue? **Yes** if all tests + smoke pass.

### C-PHASE 84 — Guarded Code Mutation Pilot

Goal: run the first reviewed code mutation through `GuardedMutationService` end-to-end (dry-run → backup → apply → post-proof → rollback-on-failure), on a small, low-risk patch.

Scope:

- Pick a real, small repo change that benefits the project but is non-load-bearing. Example candidates:
  - add a deprecation warning to a now-unused helper
  - update a comment block in `tool_definitions.json`
  - bump a constant default value in a non-critical setting
  - rename a private field in a single file
- Codex must propose the change as a `GuardedMutationPlan` (a `Draft` task card), not as a direct edit.
- The user approves the card. Codex (or the operator) then runs:
  - dry-run → produces backup plan + diff
  - apply → backups + writes + post-proof
  - if post-proof fails, automatic rollback (already implemented)
- Wire `PetTaskAdapterPreviewDispatcher.guardedMutation` to also be reachable from the Settings UI (a "Propose mutation" mini-form) for power users.
- Document the end-to-end flow with screenshots/text in the phase report.
- Do **not** generalize this to broad code generation — this is a single proof of the loop.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/GuardedMutationPreviewAdapter.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
vnext/content/tool_definitions.json
```

New files:

```text
vnext/tests/Wevito.VNext.Tests/GuardedMutationPilotTests.cs
docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md
```

Tests (mostly regression with one new pilot scenario):

- Pilot scenario: edit a known synthetic test fixture file, run dry-run, apply, then forcibly fail the post-proof command, verify exact rollback.
- Scope-expand attempt is refused.
- KillSwitch blocks apply mid-run.
- All four sprite tests + all 27 existing mutation tests remain green.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "GuardedMutation|MutationVerifier|SpriteWorkflow"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/mutations/<ts>-pilot-apply/mutation-apply.json
vnext/artifacts/mutation-backups/<ts>-pilot/...
docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md
```

Stop gates:

- Stop if any scope expansion occurs.
- Stop if rollback is not byte-exact.
- Stop if the pilot mutation has any cross-file ripple.
- Stop if the post-proof command for the touched file type is wrong.

Rollback: revert the pilot PR's code edit, keep the GuardedMutationService work.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-84-guarded-code-mutation-pilot`
- PR title: `C-PHASE 84: Guarded code mutation pilot (first reviewed apply)`

Auto-continue? **No.** First reviewed code apply outside sprites.

### C-PHASE 85 — Autonomous Operations Promotion + 7-Day Sign-Off

Goal: with all prior gates green, run a sustained autonomous-operations beta over 7 days and produce the promotion decision packet.

Scope:

- Run `AutonomousOperationsLoop` daily for 7 days in `LocalOnly` mode with the user's normal computer usage.
- Each day: `tools/run-self-improvement-report.ps1` produces the daily report.
- Beta-gate decision (existing `AutonomousBetaDecisionService`) must now produce:
  - `enable_autonomous_beta` only if every check passes across the 7-day window:
    - Active uptime > 50% per active day
    - zero policy violations
    - 100% of mutations have proof packets (zero unprotected mutations)
    - zero hosted-AI calls in LocalOnly
    - zero focus-steal events
    - resource budget held within +/- 10%
    - citation coverage ratio ≥ 0.6 on the golden eval gate
- Final promotion: a single PR flips `runtime_autonomous_beta_enabled` default to a documented Settings UI value of "Off (user opt-in)" with a "Try the autonomous beta" entry that requires explicit consent before enable.

Tasks:

- Author the promotion checklist as a Markdown file:
  - `docs/C_PHASE85_AUTONOMOUS_PROMOTION_CHECKLIST_2026-05-13.md`
- Implement a `tools/run-promotion-eval.ps1` script that walks the 7-day ledger and emits the final decision packet.
- Add a tiny `PromotionCriteriaSnapshot` Core service that returns the structured pass/fail table.
- The PR must include the actual evidence (snapshotted ledger excerpt + golden eval result).

New files:

```text
vnext/src/Wevito.VNext.Core/PromotionCriteriaSnapshot.cs
vnext/tests/Wevito.VNext.Tests/PromotionCriteriaSnapshotTests.cs
tools/run-promotion-eval.ps1
docs/C_PHASE85_AUTONOMOUS_PROMOTION_CHECKLIST_2026-05-13.md
docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md
```

Tests:

- Snapshot service refuses to compute without 7+ days of ledger.
- Snapshot service fails closed (returns `keep_supervised_preview`) when any criterion fails.
- KillSwitch state during the window blocks `enable_autonomous_beta`.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PromotionCriteria|AutonomousBeta|AutonomousOperations"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\
```

Artifacts:

```text
vnext/artifacts/promotion/<ts>-promotion/snapshot.json
vnext/artifacts/promotion/<ts>-promotion/decision.json
vnext/artifacts/promotion/<ts>-promotion/run-summary.md
docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md
```

Stop gates:

- Stop if any criterion is not satisfied.
- Stop if promotion enables anything else (mutation apply, hosted AI, etc.).
- Stop if the user-facing entry can be flipped without explicit consent.

Rollback: revert PR; runtime_autonomous_beta_enabled stays default-off.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-85-autonomous-operations-promotion`
- PR title: `C-PHASE 85: Autonomous operations promotion + 7-day sign-off`

Auto-continue? **No.** Final user-decision gate for this roadmap chunk.

## 4. Phase Sequencing Rules

```text
auto-continue table
|
phase    auto-continue?    why
|
76       no                first real ML inference path
77       yes               UI work, no new capability surfaces
78       no                requires the user to run a soak
79       no                adds runtime onboarding UX
80       no                first retrieval surface
81       no                first real-AI reasoning surface
82       no                first PR-level regression gate
83       yes               reporting, no new mutation surface
84       no                first reviewed code apply
85       no                final promotion decision
```

Branch naming: `claude-implementation/c-phase-<N>-<short-slug>`. One PR per phase. Phase report doc per phase. Validation baseline must pass before any PR opens.

## 5. Discovered Gaps and How They Are Folded In

| Gap                                                       | Folded into |
| --------------------------------------------------------- | ----------- |
| ONNX inference not wired                                  | C-PHASE 76  |
| Live "what is Wevito doing?" UX                            | C-PHASE 77  |
| 7-day soak ledger evidence absent                          | C-PHASE 78  |
| Windows power / fullscreen handling                       | C-PHASE 78  |
| Real local model entry path (install / pull / probe)      | C-PHASE 79  |
| In-process fallback (ONNX Phi-3 / LLamaSharp)             | C-PHASE 79  |
| Hybrid retrieval + rerank                                 | C-PHASE 80  |
| RAG context, citation enforcement, no hosted AI in LocalOnly | C-PHASE 81 |
| Golden eval + regression gate                             | C-PHASE 82  |
| Daily/weekly report + rollback proposals                  | C-PHASE 83  |
| First reviewed code apply                                 | C-PHASE 84  |
| Autonomous promotion sign-off                             | C-PHASE 85  |

## 6. References (May 2026)

- `docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md`
- `docs/CLAUDE_LOCAL_FIRST_AI_CODEX_PHASE_PROMPTS_2026-05-12.md`
- `docs/C_PHASE65_AUTONOMOUS_TASK_SCHEDULER_PREVIEW_2026-05-12.md` through `docs/C_PHASE75_AUTONOMOUS_OPERATIONS_BETA_2026-05-12.md`
- `docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`
- Source survey of `vnext/src/Wevito.VNext.Core/`, `vnext/src/Wevito.VNext.Shell/`, `vnext/tests/Wevito.VNext.Tests/`, `tools/`, `vnext/content/`
- Web research notes:
  - `SmartComponents.LocalEmbeddings` → wraps Semantic Kernel `BertOnnxTextEmbeddingGenerationService`; default model is `bge-micro-v2` (~22.9 MiB, INT8 quantized, MIT-licensed).
  - `LLamaSharp` 0.27 in-process llama.cpp bindings on Windows (CPU + CUDA 11/12 + Vulkan).
  - `Ollama` OpenAI-compatible REST on `127.0.0.1:11434`; Ollama 0.x; Windows-native installer via winget.
  - `Microsoft.ML.OnnxRuntime.GenAI` for Phi-3-mini-4k INT4 with DirectML execution provider.
  - Hybrid retrieval guidance: RRF fusion of BM25 (FTS5) + dense vectors (sqlite-vec); rerank head (small linear model on top of embeddings) improves recall by ~10-15% vs dense-only on text-heavy corpora.
  - Eval frameworks: RAGAS / TruLens / DeepEval; CI-style hard regression gates with 2% recall, 2% MRR, 5% citation coverage thresholds.
  - Windows 11 May 2026 update: native fullscreen-game deprioritization; combine with `WS_EX_NOACTIVATE` for non-focus-steal overlay; `Microsoft.Win32.SystemEvents.PowerModeChanged` and `SessionSwitch` for sleep/lock detection.
  - Microsoft governance toolkit guidance: kill-switch controllers, append-only audit ledgers, default-deny policy engines, evidence packets with `did_use_network/hosted_ai/local_model/mutate` flags, SLSA-style provenance.
  - Local-first RAG with SQLite + FTS5: native BM25 ranking + sqlite-vec extension for vector storage; deterministic and zero-network by default.

## 7. Where to Start Right Now

Begin C-PHASE 76. Use the copy-paste prompt in section 9 of `docs/CLAUDE_POST_CPHASE75_CODEX_PHASE_PROMPTS_2026-05-13.md`, or the kickoff prompt at the bottom of this document.
