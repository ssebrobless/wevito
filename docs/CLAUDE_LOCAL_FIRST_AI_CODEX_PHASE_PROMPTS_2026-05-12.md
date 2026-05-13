# Claude Local-First AI — Codex Phase Prompts (Medium Effort)

Date: 2026-05-12

These prompts are Codex-ready. Each is self-contained, references the source
of truth (`docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md`),
and is sized for one PR at medium reasoning effort. Run them in order. Do not
skip phases or bundle them.

Shared preamble (mentally include with each prompt):

```text
Repo: C:\Users\fishe\Documents\projects\wevito (or current worktree)
Read first:
  docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md
  docs/POST_CPHASE62_INDEPENDENT_AI_ROADMAP_2026-05-12.md
  docs/C_PHASE64_LOCAL_FIRST_AI_RESEARCH_STRATEGY_2026-05-12.md
  docs/C_PHASE63_ALWAYS_ON_RUNTIME_SUPERVISOR_2026-05-12.md

Invariants (apply to every phase):
- no hosted-AI dependency for normal runtime
- no hidden web access
- no hidden training
- no hidden local file access
- no hidden tool execution
- no hidden code/asset mutation
- pause/quiet/pet-only is always honored
- pets remain regular pet sprites (no special task animation)
- every mutation: exact scope + dry-run + backup hash + rollback + post-proof
- every learning step: reviewed data + eval gate + rollback
- every web fetch: citation + provenance + privacy filter
- every local model use degrades safely when runtime/model absent
- after C-PHASE 68, every adapter writes a row to the audit ledger
- after C-PHASE 69, every adapter honors KillSwitch
- after C-PHASE 71, every file read goes through UnifiedPolicyService

Validation baseline (always run at end of phase before opening PR):
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

PR rules:
- one phase per PR
- branch: claude-implementation/c-phase-<N>-<short-slug>
- commit messages reference C-PHASE <N>
- include phase report under docs/
- do NOT merge if any test, build, or smoke fails
```

## C-PHASE 65 — Autonomous Task Scheduler Preview

```text
Implement C-PHASE 65 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-65-autonomous-scheduler-preview

Add (new files):
- vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs
- vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs
- vnext/src/Wevito.VNext.Core/SchedulerEvidencePacket.cs
- vnext/tests/Wevito.VNext.Tests/AutonomousTaskSchedulerTests.cs
- vnext/tests/Wevito.VNext.Tests/RuntimeBudgetMeterTests.cs
- docs/C_PHASE65_AUTONOMOUS_TASK_SCHEDULER_PREVIEW_2026-05-12.md

Modify:
- vnext/src/Wevito.VNext.Core/PetTaskCardQueueService.cs (accept scheduler-origin drafts)
- vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs (expose budget snapshot)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs (wire scheduler poll into tick)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs) (one new Settings row)
- vnext/content/tool_definitions.json (add scheduler entry)

Triggers the scheduler may emit on (no others):
- stale_dashboard_report
- pending_reviewed_bundle
- failed_prior_proof_packet
- recurring_user_request

Hard rules:
- scheduler only emits cards with Status=Draft, ToolFamily set, AssignedPetId optional
- scheduler is dormant when supervisor != Active or BackgroundWorkAllowed=false
- scheduler never calls PetTaskAdapterPreviewDispatcher itself
- new setting `scheduler_enabled` default false
- RuntimeBudgetMeter persists state under %LOCALAPPDATA%/Wevito/audit/budget-meter.json
- scheduler writes evidence packet to vnext/artifacts/pet-tasks/<ts>-scheduler-proposal/

Tests required:
- dormant in Quiet/PetOnly/Fullscreen-quiet/BackgroundWorkAllowed=false
- respects MaxBackgroundTasksPerHour
- refuses to set status beyond Draft
- refuses to call any execution adapter
- budget meter rolls over hourly and survives restart

Validation: run the baseline.

Phase report: write docs/C_PHASE65_AUTONOMOUS_TASK_SCHEDULER_PREVIEW_2026-05-12.md
with the implemented/safety/validation sections in the style of the C-PHASE 63
and 64 reports.

PR title: "C-PHASE 65: Autonomous task scheduler preview (proposal-only)"

Auto-continue to C-PHASE 66 only if all of: build, tests, smoke, and the dormancy
checks above pass.
```

## C-PHASE 66 — Reviewed Learning Promotion Gate

```text
Implement C-PHASE 66 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-66-reviewed-learning-promotion-gate

Add:
- vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs
- vnext/src/Wevito.VNext.Core/LearningDatasetManifest.cs
- vnext/tests/Wevito.VNext.Tests/LearningLabPromotionServiceTests.cs
- docs/C_PHASE66_REVIEWED_LEARNING_PROMOTION_GATE_2026-05-12.md

Modify:
- vnext/src/Wevito.VNext.Core/PetMemoryStore.cs (accept dataset-version provenance)
- vnext/src/Wevito.VNext.Core/PetMemoryWriteGate.cs (require task_card_id for promoted rows)
- vnext/src/Wevito.VNext.Core/LearningLabBundleService.cs (no behavior change; re-export helpers if needed)
- vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml.cs (add "Propose promotion" button)
- vnext/content/tool_definitions.json (add learningPromotion tool family)

Promotion rules:
- only label `accept` is eligible
- explicit user approval required (TaskCard.Status=Approved, ToolFamily=learningPromotion)
- write jsonl + manifest.json to vnext/artifacts/learning-datasets/<vNNNN-yyyyMMdd-HHmmss>/
- manifest.sha256 must equal sha256 of examples.jsonl
- never copy binary assets — record source paths only
- AutomaticTrainingEnabled and AutomaticMemoryPromotionEnabled remain false
  (add tests that assert these constants)

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue to C-PHASE 67 only if all tests + smoke pass.
```

## C-PHASE 67 — Local Embeddings + Eval Loop

```text
Implement C-PHASE 67 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-67-local-embeddings-and-eval-loop

Add:
- vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs
- vnext/src/Wevito.VNext.Core/LearningEvalService.cs
- vnext/src/Wevito.VNext.Core/LearningEvalRecord.cs
- vnext/content/local-models/embeddings/bge-micro-v2/README.md
- vnext/content/local-models/embeddings/bge-micro-v2/.gitkeep
- vnext/tests/Wevito.VNext.Tests/OnnxTextEmbeddingServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/LearningEvalServiceTests.cs
- docs/C_PHASE67_LOCAL_EMBEDDINGS_AND_EVAL_LOOP_2026-05-12.md
- tools/install-local-embedder.ps1 (asks the user to confirm, downloads
  bge-micro-v2 ONNX into the content folder, writes sha256 manifest)

Modify:
- vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
  - EmbeddingDimensions becomes configurable (default 384 when ONNX is active,
    16 when hashing fallback)
  - on dimension mismatch with existing DB: rename to .legacy-<ts>.db and
    rebuild (mirror current corruption-rebuild pattern)
- vnext/src/Wevito.VNext.Core/Wevito.VNext.Core.csproj
  - add `Microsoft.ML.OnnxRuntime.DirectML` package
- vnext/content/tool_definitions.json
  - update pet_memory entry to reflect new dimensions / fallback

Key rules:
- OnnxTextEmbeddingService falls back to HashingTextEmbeddingService if model
  weights are missing or load fails; emit one audit line on degrade
- the .onnx model file is NOT committed; install-local-embedder.ps1 fetches it
- no network call in any test (use the hashing fallback or a stubbed loader)
- LearningEvalService computes recall@1, recall@3, mrr, latency p50/p95
- regression sentinel: if recall@1 or mrr drops > 2% vs prior baseline,
  set regression=true and refuse to mark the new dataset version as baseline
- everything offline; eval set is built from promoted dataset

Validation: baseline + run a tiny eval against a synthetic dataset.

Phase report and PR title per the plan.

Auto-continue: NO. Stop for user approval.
```

## C-PHASE 68 — Audit Ledger + Activity Reports

```text
Implement C-PHASE 68 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-68-activity-reports-and-audit-ledger

Add:
- vnext/src/Wevito.VNext.Core/AuditLedgerService.cs
- vnext/src/Wevito.VNext.Core/EvidencePacket.cs   (generalize ResearchEvidencePacket)
- vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs
- vnext/tests/Wevito.VNext.Tests/AuditLedgerServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/ActivitySummaryServiceTests.cs
- docs/C_PHASE68_ACTIVITY_REPORTS_AND_AUDIT_LEDGER_2026-05-12.md

Modify (route packet emission into the ledger):
- vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
- vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
- vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs
- vnext/src/Wevito.VNext.Core/LearningEvalService.cs
- vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)  (Activity panel)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs        (initialize ledger)

Ledger rules:
- file: %LOCALAPPDATA%/Wevito/audit/ledger.sqlite
- append-only (updates rejected)
- one row per evidence packet
- columns: id, packet_id, packet_kind, task_card_id, created_at_utc,
  did_use_network, did_use_hosted_ai, did_use_local_model, did_mutate,
  artifact_path, summary, status, error
- all `did_use_*` flags non-null

ActivitySummary rules:
- daily + weekly tabs
- group by packet_kind
- flag rows with did_use_network/did_use_hosted_ai/did_mutate prominently
- text-only; never embed sprite thumbnails

Tests required:
- ledger append-only invariant
- every existing preview adapter writes exactly one row per preview
- summary aggregates correctly with mocked clock
- did_use_* flags cannot be null

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue to C-PHASE 69 if tests + smoke pass.
```

## C-PHASE 69 — Independent Assistant Beta Gate + Kill Switch

```text
Implement C-PHASE 69 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-69-independent-assistant-beta-gate

Add:
- vnext/src/Wevito.VNext.Core/IndependentAssistantBetaGateService.cs
- vnext/src/Wevito.VNext.Core/BetaGateDecision.cs
- vnext/src/Wevito.VNext.Core/KillSwitchService.cs
- vnext/tests/Wevito.VNext.Tests/IndependentAssistantBetaGateServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/KillSwitchServiceTests.cs
- docs/C_PHASE69_INDEPENDENT_ASSISTANT_BETA_GATE_2026-05-12.md

Modify (every adapter/service that can act must consult KillSwitchService):
- vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
- vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs
- vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs
- vnext/src/Wevito.VNext.Core/LearningEvalService.cs
- vnext/src/Wevito.VNext.Core/LocalModelAdapter.cs
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs (initialize KillSwitch)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs) (big red "Stop everything" button)

Beta-gate inputs (read from ledger):
- long-session run (>= 4h uptime in Active mode)
- quiet-mode honored under fullscreen + user keystrokes
- RuntimeBudgetMeter not exceeded
- preview/execution history present
- learning promotion proof (>=1 accepted dataset + eval improved or unchanged)
- offline proof (LocalOnly session with no network rows)

Decision labels:
- enable_limited_autonomy
- keep_preview_only
- pause_for_safety_work

KillSwitch rules:
- setting `runtime_kill_switch` default false
- when true, every adapter/service/loop refuses and emits "kill_switch=true"
  audit row
- state persists across restart

Tests required:
- decision = enable_limited_autonomy only when every check passes
- decision = keep_preview_only when non-safety check fails
- decision = pause_for_safety_work when any safety check fails
- KillSwitch blocks: preview dispatcher, scheduler, promotion, eval, model adapter
- KillSwitch state persisted

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue: NO. User decision.
```

## C-PHASE 70 — Approved Web Research Connector

```text
Implement C-PHASE 70 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-70-approved-web-research-connector

Add:
- vnext/src/Wevito.VNext.Core/WebResearchConnector.cs
- vnext/src/Wevito.VNext.Core/IWebSearchBackend.cs
- vnext/src/Wevito.VNext.Core/OfflineWebSearchBackend.cs
- vnext/src/Wevito.VNext.Core/BraveSearchBackend.cs
- vnext/src/Wevito.VNext.Core/TavilySearchBackend.cs
- vnext/src/Wevito.VNext.Core/FirecrawlSearchBackend.cs
- vnext/src/Wevito.VNext.Core/WebFetchRecord.cs
- vnext/src/Wevito.VNext.Core/WebQueryPrivacyFilter.cs
- vnext/src/Wevito.VNext.Core/WindowsCredentialStore.cs
- vnext/tests/Wevito.VNext.Tests/WebResearchConnectorTests.cs
- vnext/tests/Wevito.VNext.Tests/WebQueryPrivacyFilterTests.cs
- docs/C_PHASE70_APPROVED_WEB_RESEARCH_CONNECTOR_2026-05-12.md

Modify:
- vnext/src/Wevito.VNext.Core/ResearchPlannerService.cs
  (accept WebFetchRecord list as another local-evidence source kind;
   keep DidUseNetwork honest)
- vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
  (when web_search_enabled and approved card, route fetch results into the
   evidence packet as Sources, not as model input)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
  (Web research panel: backend dropdown, store-key buttons, test connection,
   purge cache)
- vnext/content/tool_definitions.json

Connector rules:
- default backend: offline
- default settings: web_search_enabled=false, web_max_fetches_per_hour=30,
  web_max_fetches_per_task=5
- API keys live in Windows Credential Manager under Wevito/web-search/<backend>
- KillSwitch and supervisor mode block fetches
- approved TaskCard required per fetch
- every fetch produces WebFetchRecord and writes to:
    %LOCALAPPDATA%/Wevito/web-cache/<yyyymmdd>/<sha256>.json
- every fetch produces a single web_fetch evidence packet under
    vnext/artifacts/pet-tasks/<ts>-web-research/
- privacy filter strips Windows paths, env-vars, credential-shaped strings,
  email addresses; refuses near-empty queries
- a real network call must NEVER happen in tests (use injectable HttpClient
  that throws on actual send)
- hosted-AI is not enabled in this phase; do not route fetched content into a
  hosted-AI provider

Tests required:
- default backend is offline
- fetch without approved card is Blocked
- fetch under Quiet/PetOnly/Fullscreen-quiet is Blocked
- privacy filter strips Windows paths and credentials
- rate limits enforced per hour and per task
- cache hit avoids re-fetch unless force=true
- KillSwitch blocks
- ledger row per fetch

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue: NO. First network surface.
```

## C-PHASE 71 — Approved Local File/Tool Access + Unified Policy Engine

```text
Implement C-PHASE 71 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-71-approved-local-file-tool-access

Add:
- vnext/src/Wevito.VNext.Core/LocalToolAccessPolicy.cs
- vnext/src/Wevito.VNext.Core/UnifiedPolicyService.cs
- vnext/src/Wevito.VNext.Core/PolicyDecision.cs
- vnext/src/Wevito.VNext.Core/LocalToolExecutionPreviewAdapter.cs
- vnext/tests/Wevito.VNext.Tests/LocalToolAccessPolicyTests.cs
- vnext/tests/Wevito.VNext.Tests/UnifiedPolicyServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/LocalToolExecutionPreviewAdapterTests.cs
- docs/C_PHASE71_APPROVED_LOCAL_FILE_TOOL_ACCESS_2026-05-12.md

Modify:
- the four legacy evaluators delegate to UnifiedPolicyService:
  HelperAllowlistEvaluator, ToolPolicyEvaluator, CapturePolicyEvaluator,
  ProofExecutionAllowlistEvaluator
- file-reading adapters call UnifiedPolicyService.EvaluateRead(path):
  LocalDocsPreviewAdapter, AssetInventoryPreviewAdapter,
  CodeReviewPreviewAdapter, LocalResearchPreviewAdapter
- PetTaskAdapterPreviewDispatcher routes localToolExec to the new adapter

Allowlist defaults:
- roots: vnext/, docs/, tools/, vnext/artifacts/
- denylist (always): .git, **/secrets, **/.env*, **/credentials,
  %USERPROFILE%/.ssh, %LOCALAPPDATA%/Microsoft/, %APPDATA%/Microsoft/

localToolExec rules:
- default blocked
- when enabled, only whitelisted scripts under tools/ run, by sha256
- only dry-run mode in this phase; no real execution

Tests required:
- denylist beats allowlist
- read outside allowlist roots blocked
- symlink / `..` / case-folded traversal blocked
- legacy evaluators still green (regression)
- localToolExec default blocked
- ledger row per evaluation
- KillSwitch blocks

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue: NO.
```

## C-PHASE 72 — Local Model Runtime Integration (Ollama-First)

```text
Implement C-PHASE 72 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-72-local-model-runtime-integration

Add:
- vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs
- vnext/src/Wevito.VNext.Core/LocalRuntimeProbeService.cs
- vnext/src/Wevito.VNext.Core/ModelInferenceEvidencePacket.cs
- vnext/tests/Wevito.VNext.Tests/OllamaLocalModelAdapterTests.cs
- vnext/tests/Wevito.VNext.Tests/LocalRuntimeProbeServiceTests.cs
- docs/C_PHASE72_LOCAL_MODEL_RUNTIME_INTEGRATION_2026-05-12.md

Modify:
- vnext/src/Wevito.VNext.Core/ModelProviderMode.cs (track LocalProviderAvailable from probe)
- vnext/src/Wevito.VNext.Core/LocalModelAdapter.cs (keep as deterministic fallback)
- vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
  (route research/codeReview previews through the active local adapter;
   continue to write deterministic Synthesis when adapter degrades)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs) (Local AI panel)
- vnext/content/tool_definitions.json

Adapter rules:
- endpoint: http://127.0.0.1:11434 (configurable via local_runtime_ollama_endpoint)
- model: llama3.2:3b default
- probes GET /api/tags before any use; on failure -> LocalProviderAvailable=false
  and deterministic fallback returns
- never reads API keys
- only localhost endpoints are reachable (assert in tests with a fake
  HttpMessageHandler that fails non-127.0.0.1 connections)
- inference call: POST /v1/chat/completions (OpenAI-compatible)
- records: model id, runtime id, prompt sha256, response sha256, latency,
  did_use_local_model=true, did_use_network=false (localhost is not "the web")
- probe is dormant in Quiet/PetOnly
- KillSwitch blocks adapter and probe

ONNX/LLamaSharp adapters: stub interfaces and feature flags only in this phase
(no real wiring). Document them in the phase report as opt-in follow-ups.

Tests required:
- adapter falls back to deterministic when endpoint unreachable
- adapter records all required fields
- probe dormant in Quiet/PetOnly
- LocalOnly never accidentally calls a hosted endpoint
- KillSwitch blocks

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue: NO.
```

## C-PHASE 73 — Local Training / Tuning Pipeline

```text
Implement C-PHASE 73 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-73-local-training-tuning-pipeline

Add:
- vnext/src/Wevito.VNext.Core/LocalTrainingPlanService.cs
- vnext/src/Wevito.VNext.Core/LocalTuningRunner.cs
- vnext/src/Wevito.VNext.Core/RerankHead.cs
- vnext/src/Wevito.VNext.Core/RouterConfig.cs
- vnext/src/Wevito.VNext.Core/PromptConfigStore.cs
- tools/run-unsloth-lora.ps1
- tools/run-unsloth-lora.bat
- vnext/tests/Wevito.VNext.Tests/LocalTrainingPlanServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/LocalTuningRunnerTests.cs
- vnext/tests/Wevito.VNext.Tests/RerankHeadTests.cs
- docs/C_PHASE73_LOCAL_TRAINING_TUNING_PIPELINE_2026-05-12.md

Modify:
- vnext/src/Wevito.VNext.Core/LearningEvalService.cs (EvaluateAgainst(baseline))
- vnext/src/Wevito.VNext.Core/PetMemoryStore.cs (allow rerank-head application)
- vnext/src/Wevito.VNext.Core/AuditLedgerService.cs (new packet kinds:
  train_plan, tuning_apply, tuning_rollback)

Stages:
- 73A retrieval tuning (rerank head)
- 73B routing tuning (router config)
- 73C prompt config tuning (templates / top-k / thresholds)
- 73D LoRA pilot — Codex generates plan + ps1/bat scripts only; never runs Python

Tuning rules:
- writes only under vnext/content/ and vnext/artifacts/
- eval-after auto-rollback if metric drops > tolerance
- rollback restores byte-exact (sha256 match)
- LoRA stage gated by tuning_lora_enabled=false default
- KillSwitch blocks

Tests required:
- plan refuses LoRA when flag false
- dry-run never writes outside allowlist
- eval-after regression triggers rollback
- rollback byte-exact
- LoRA stage never runs Python directly

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue: NO.
```

## C-PHASE 74 — Guarded Code & Asset Mutation Execution

```text
Implement C-PHASE 74 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-74-guarded-code-asset-mutation

Add:
- vnext/src/Wevito.VNext.Core/GuardedMutationPlan.cs
- vnext/src/Wevito.VNext.Core/GuardedMutationService.cs
- vnext/src/Wevito.VNext.Core/MutationVerifier.cs
- vnext/tests/Wevito.VNext.Tests/GuardedMutationServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/MutationVerifierTests.cs
- docs/C_PHASE74_GUARDED_CODE_ASSET_MUTATION_2026-05-12.md

Refactor (preserve public API; delegate to GuardedMutationService):
- vnext/src/Wevito.VNext.Core/SpriteWorkflowApplyService.cs
- vnext/src/Wevito.VNext.Core/SpriteWorkflowDryRunApplyService.cs
- vnext/src/Wevito.VNext.Core/SpriteWorkflowRollbackService.cs
- vnext/src/Wevito.VNext.Core/SpriteWorkflowPostApplyProof.cs

Wire:
- PetTaskAdapterPreviewDispatcher route mutation tool family to a new
  preview adapter that produces dry-run packets only

Service rules:
- four steps: dry-run -> backup -> apply -> post-proof, with rollback on
  any failure
- target paths must be within UnifiedPolicyService allowlist
- backups under vnext/artifacts/mutation-backups/<ts>-<scope>/
- post-proof commands by file type:
  - .cs/.csproj/.sln -> dotnet build (+ relevant filter)
  - test files -> dotnet test
  - sprite paths -> tools/audit_sprite_contract.py
  - runtime PNG -> tools/report_runtime_canvas_mismatches.py
- refuse if RuntimeSupervisor.Mode != Active, KillSwitch=true, or card not Approved

Tests required:
- dry-run never writes outside backup folder
- scope-expand attempt is refused
- rollback byte-exact
- post-proof matches file type
- legacy sprite tests still green
- KillSwitch blocks

Validation: baseline + a small synthetic mutation test (e.g., a temp text file).

Phase report and PR title per the plan.

Auto-continue: NO.
```

## C-PHASE 75 — Autonomous Operations Beta

```text
Implement C-PHASE 75 per docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md §2.

Branch: claude-implementation/c-phase-75-autonomous-operations-beta

Add:
- vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs
- vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
- vnext/src/Wevito.VNext.Core/AutonomousOperationsConfig.cs
- vnext/tests/Wevito.VNext.Tests/AutonomousOperationsLoopTests.cs
- vnext/tests/Wevito.VNext.Tests/AutonomousBetaDecisionServiceTests.cs
- docs/C_PHASE75_AUTONOMOUS_OPERATIONS_BETA_2026-05-12.md

Modify:
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs (run loop on tick when enabled)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs) (autonomous status panel)

Loop rules:
- runs only if all of: beta gate decision = enable_limited_autonomy,
  runtime_autonomous_beta_enabled=true, supervisor.Mode=Active, KillSwitch=false,
  daily-cap not exceeded
- one iteration per tick (default 10 min interval)
- iteration steps: propose -> local research -> approved web fetch (if enabled)
  -> approved file read -> local model reasoning -> mutation proposal (preview)
- NEVER applies mutations
- emits one activity_summary packet per iteration

Beta decision checks (read from ledger over the last 7 days):
- Active uptime presence
- zero policy violations
- 100% mutations have proof packets
- zero hosted-AI calls in LocalOnly
- zero focus-steal events
- resource budget within +/- 10%
- decision label: enable_autonomous_beta | keep_supervised_preview | pause_for_reliability_work

Tests required:
- loop dormant when any gate fails
- loop never executes a mutation directly
- daily-cap enforced
- KillSwitch halts mid-iteration
- decision refuses to compute without ledger rows

Validation: baseline.

Phase report and PR title per the plan.

Auto-continue: NO. User decision.
```

## Phase Sequencing Checklist

```text
phase    auto-continue?    notes
65       yes               dormant by default, scheduler is proposal-only
66       yes               reviewed-data-only, no training
67       no                introduces ONNX dependency + dimension change
68       yes               ledger is observability; safe by construction
69       no                user decides whether to proceed toward autonomy
70       no                first network surface
71       no                broader file access
72       no                first real model adapter
73       no                first learning that changes config
74       no                first code/asset mutation surface beyond sprites
75       no                full autonomous loop
```
