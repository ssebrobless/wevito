# Claude Local-First AI Full Implementation Plan

Date: 2026-05-12
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort, executing phase-by-phase
Scope: post-`C-PHASE 64` path that turns Wevito into a self-sustaining local-first AI desktop pet/helper without a hosted-AI runtime brain

This document is the source-of-truth implementation plan. The companion file
`docs/CLAUDE_LOCAL_FIRST_AI_CODEX_PHASE_PROMPTS_2026-05-12.md` contains the
copy-paste prompts Codex should run, one per phase.

## 0. Executive Shape

```text
Wevito independent AI path (post C-PHASE 64)
|
+-- locked-in spine
|   +-- ModelProviderMode (Disabled, LocalOnly, ApprovedCloud) — present
|   +-- RuntimeSupervisorService (Active, Quiet, PetOnly + budgets) — present
|   +-- IModelAdapter + LocalModelAdapter (deterministic) — present
|   +-- ResearchPlannerService + ResearchEvidencePacket — present
|   +-- LocalResearchPreviewAdapter (report-only) — present
|   +-- PetTaskCardQueueService + PetTaskAdapterPreviewDispatcher — present
|   +-- PetMemoryStore (sqlite + sqlite-vec + FTS5) — present (hash-only embeddings)
|   +-- LearningLabBundleService (reviewed bundle export) — present
|   `-- SpriteWorkflow apply/dry-run/rollback/post-proof — present (sprite-only)
|
+-- next twelve phases (C-PHASE 65 through C-PHASE 75 + discovered gaps)
|   +-- 65  Autonomous Task Scheduler Preview
|   +-- 66  Reviewed Learning Promotion Gate
|   +-- 67  Local Embeddings + Learning/Eval Loop
|   +-- 68  Self-Improvement Activity Reports & Audit Ledger
|   +-- 69  Independent Assistant Beta Gate
|   +-- 70  Approved Web Research Connector
|   +-- 71  Approved Local File & Tool Access + Unified Policy Engine
|   +-- 72  Local Model Runtime Integration (Ollama-first, ONNX/LLamaSharp optional)
|   +-- 73  Local Training/Tuning Pipeline (retrieval → routing → LoRA)
|   +-- 74  Guarded Code & Asset Mutation Execution (generalized)
|   `-- 75  Autonomous Operations Beta
|
`-- hard invariants (never violated by any phase)
    +-- no hidden web access
    +-- no hidden training
    +-- no hidden local file access
    +-- no hidden tool execution
    +-- no hidden code/asset mutation
    +-- no hosted-AI brain for normal Wevito runtime
    +-- pause/quiet/pet-only always honored
    +-- pets remain regular pet sprites visually
    +-- every mutation: exact scope + dry-run + backup hash + rollback + post-proof
    +-- every learning step: reviewed data + eval gate + rollback
    +-- every web fetch: citation + provenance + privacy filter
    `-- every local model use degrades safely when runtime/model absent
```

## 1. Cross-Cutting Architecture (Codex must internalize before phase work)

These are spine concepts that recur across phases. Codex should not re-design
them per phase.

### 1.1 Capability flags (extend, don't duplicate)

All new capability enablement flows through settings keys read by services in
`Wevito.VNext.Core`. New flags follow the prefix convention:

```text
runtime_*       Runtime supervisor (already present)
pet_model_*     Model provider mode (already present)
scheduler_*     Autonomous scheduler
learning_*      Reviewed-bundle promotion + eval
audit_*         Activity/audit ledger
web_*           Web research connector
local_access_*  Local file/tool access
local_runtime_* Local model runtime (Ollama/ONNX/LLamaSharp)
tuning_*        Local training/tuning
mutation_*      Guarded code/asset mutation
beta_*          Beta gates
```

Defaults: everything disabled, every flag has an explicit setting key, every
"enabled" decision passes through a service method that returns `(bool, reason)`.

### 1.2 Evidence packet schema (one schema, many adapters)

Extend `ResearchEvidencePacket` into a generic `EvidencePacket` family that all
phases write:

```text
EvidencePacket
|
+-- packet_id (guid)
+-- packet_kind (research | scheduler_proposal | learning_promotion |
|                eval_run | web_fetch | local_access | model_inference |
|                tuning_run | mutation_proposal | mutation_apply |
|                mutation_rollback | activity_summary)
+-- task_card_id (guid?)
+-- created_at_utc
+-- inputs_hash (sha256 of normalized inputs)
+-- sources_inspected (path / uri / hash)
+-- claims_or_findings
+-- did_use_network (bool)
+-- did_use_hosted_ai (bool)
+-- did_use_local_model (bool)
+-- did_mutate (bool)
+-- write_paths
+-- backup_paths
+-- rollback_paths
+-- proof_paths
+-- synthesis / summary
+-- next_recommended_action
`-- audit_ledger_id (after C-PHASE 68)
```

This is the lingua franca every phase emits. The `EvidencePacket` is what the
activity-report loop, the audit ledger, and the user-facing "what is Wevito
doing?" surface read from.

### 1.3 Unified policy engine

`HelperAllowlistEvaluator`, `ToolPolicyEvaluator`, `CapturePolicyEvaluator`, and
`ProofExecutionAllowlistEvaluator` already exist. C-PHASE 71 consolidates them
into a `UnifiedPolicyService` that:

- evaluates one `PolicyRequest` (tool family, access mode, paths, network,
  approval state, runtime supervisor status, model provider mode)
- returns one `PolicyDecision` (Allowed | ApprovalRequired | Blocked + reason +
  effective allowlist)
- writes every decision to the audit ledger (after C-PHASE 68)
- default-deny: any decision the engine cannot resolve returns `Blocked`

### 1.4 Dry-run / apply / rollback / post-proof contract

`SpriteWorkflowDryRunApplyService` + `SpriteWorkflowApplyService` +
`SpriteWorkflowRollbackService` + `SpriteWorkflowPostApplyProof` are the
existing reference. C-PHASE 74 generalizes them into `GuardedMutationPlan` and
`GuardedMutationService` so the same gate works for:

- code patches
- asset packs
- learning datasets
- runtime config
- local memory rows
- model/adapter binaries

Required steps for any mutation:

```text
1. propose      (writes EvidencePacket.kind = mutation_proposal)
2. dry-run      (computes diff + backup plan, no writes, writes proof packet)
3. user approve (TaskCard transitions to Approved; supervisor must be Active)
4. apply        (writes EvidencePacket.kind = mutation_apply, with backup hashes)
5. post-proof   (sha256 of every written file + run validation commands)
6. rollback?    (exact reverse using backup hashes, writes mutation_rollback)
```

If any step fails or the user revokes approval, the chain stops and the
mutation is rolled back automatically.

### 1.5 Resource-budget runtime contract

`RuntimeSupervisorSettings.MaxBackgroundTasksPerHour`, `CpuBudgetPercent`, and
`MemoryBudgetMb` are already defined. Phases 65, 70, 72, 73, 74, 75 must
respect them. A new helper `RuntimeBudgetMeter` (added in C-PHASE 65) counts
work and refuses to start new background work when budgets are exhausted; it
logs throttling events to the audit ledger after C-PHASE 68.

### 1.6 Safe-degrade contract

Every external dependency (Ollama process, ONNX runtime, LLamaSharp native, web
connector, sqlite-vec extension, local model file, network) must have a
deterministic fallback that:

- never silently swaps to a hosted provider
- never blocks user-initiated work
- writes a single audit line explaining the degradation
- continues to serve the deterministic preview surface

The existing `LocalModelAdapter` and `HashingTextEmbeddingService` are the
templates: deterministic, no-network, no-credentials, no-throw.

## 2. Phase Catalog

Each phase below uses the same structure:

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
Auto-continue?  (yes = Codex may proceed to next phase after merge.
                no  = stop for user approval before continuing.)
```

### C-PHASE 65 — Autonomous Task Scheduler Preview

Goal: let Wevito propose useful background work as `Draft` task cards while
obeying quiet/pet-only mode and resource budgets. No execution.

Scope:

- Add `AutonomousTaskScheduler` in Core. It produces `TaskCard` drafts from
  whitelisted triggers only:
  - `stale_dashboard_report` (last run > N hours)
  - `pending_reviewed_bundle` (a Creative Lab bundle exists but never promoted)
  - `failed_prior_proof_packet` (post-apply proof failed; suggest a re-check)
  - `recurring_user_request` (user-pinned recurring task)
- Add `RuntimeBudgetMeter` (per-hour task counter + CPU/memory inspector) and
  wire it into the scheduler.
- Scheduler always writes a `scheduler_proposal` evidence packet to
  `vnext/artifacts/pet-tasks/<ts>-scheduler-proposal/`.
- Scheduler MUST NOT call `PetTaskAdapterPreviewDispatcher.BuildPreview`
  itself unless the user has separately approved background-helper-previews.
  In the default config it only emits `Draft` cards.
- Shell wiring: tick loop polls scheduler every 5 minutes when
  `RuntimeSupervisorStatus.Mode == Active` and `BackgroundWorkAllowed` is true;
  otherwise sleeps.
- Settings UI: a single new row "Allow Wevito to propose background tasks
  (preview only)". Default off.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/PetTaskCardQueueService.cs
vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

New files:

```text
vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs
vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs
vnext/src/Wevito.VNext.Core/SchedulerEvidencePacket.cs
vnext/tests/Wevito.VNext.Tests/AutonomousTaskSchedulerTests.cs
vnext/tests/Wevito.VNext.Tests/RuntimeBudgetMeterTests.cs
docs/C_PHASE65_AUTONOMOUS_TASK_SCHEDULER_PREVIEW_2026-05-12.md
```

Tests:

- Scheduler is dormant when `Quiet` or `PetOnly`.
- Scheduler is dormant when `BackgroundWorkAllowed=false`.
- Scheduler respects `MaxBackgroundTasksPerHour`.
- Scheduler refuses to emit `Approved` or higher status cards.
- Scheduler refuses to call execution adapters.
- Each proposal writes a `scheduler_proposal` packet with stable schema.
- Budget meter rolls over hourly and persists across process restarts to
  `%LOCALAPPDATA%/Wevito/audit/budget-meter.json`.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AutonomousTaskScheduler|RuntimeBudgetMeter|RuntimeSupervisor"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-scheduler-proposal/scheduler-proposal.json
vnext/artifacts/pet-tasks/<ts>-scheduler-proposal/run-summary.md
docs/C_PHASE65_AUTONOMOUS_TASK_SCHEDULER_PREVIEW_2026-05-12.md  (phase report)
```

Stop gates:

- Stop if scheduler can transition cards beyond `Draft`.
- Stop if scheduler can fire when `Quiet`/`PetOnly`/`Fullscreen-quiet`.
- Stop if scheduler ignores `BackgroundWorkAllowed=false`.
- Stop if proposals lack the standard evidence-packet fields.
- Stop if `RuntimeBudgetMeter` is bypassable.

Rollback:

- Single PR. Revert PR if smoke fails.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-65-autonomous-scheduler-preview`
- Commit messages reference `C-PHASE 65`.
- PR title: `C-PHASE 65: Autonomous task scheduler preview (proposal-only)`.

Auto-continue? Yes if all tests + smoke pass and the scheduler is provably
dormant by default; otherwise stop.

### C-PHASE 66 — Reviewed Learning Promotion Gate

Goal: let approved Creative Learning Lab bundles flow into local memory and
into a versioned learning-dataset directory. No training. No model call.

Scope:

- Add `LearningLabPromotionService` that:
  - reads `vnext/artifacts/creative-learning-lab/<ts>-reviewed-bundle/labels.json`
    + `sources.json` + `summary.md`
  - validates the bundle gate (already enforced at export, but re-validate)
  - filters labels: only rows with label `accept` are eligible; `revise`,
    `defer`, `reject`, `blocked`, and unlabeled rows are excluded
  - requires explicit user approval to promote (a `TaskCard` with
    `Status=Approved` and `ToolFamily=learningPromotion`)
- Promotion writes:
  - one row per accepted example into `PetMemoryStore.AddExample`
  - one immutable copy under
    `vnext/artifacts/learning-datasets/<dataset-version>/examples.jsonl`
  - a `learning_promotion` evidence packet under
    `vnext/artifacts/pet-tasks/<ts>-learning-promotion/`
- Add a constant `AutomaticTrainingEnabled = false` and a test that asserts
  it; mirror `LearningLabBundleService`'s pattern.
- Dataset versions are folders named `vNNNN-yyyyMMdd-HHmmss`. Each version
  carries a `manifest.json` (schema_version, examples_count, source bundle
  paths, sha256 of jsonl, reviewer field).

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
vnext/src/Wevito.VNext.Core/PetMemoryWriteGate.cs
vnext/src/Wevito.VNext.Core/LearningLabBundleService.cs
vnext/src/Wevito.VNext.Shell/CreativeLearningLabWindow.xaml.cs
vnext/content/tool_definitions.json
```

New files:

```text
vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs
vnext/src/Wevito.VNext.Core/LearningDatasetManifest.cs
vnext/tests/Wevito.VNext.Tests/LearningLabPromotionServiceTests.cs
docs/C_PHASE66_REVIEWED_LEARNING_PROMOTION_GATE_2026-05-12.md
```

Tests:

- Reject/blocked/defer/revise/unlabeled rows are filtered out.
- Promotion without an `Approved` task card is blocked.
- Manifest sha256 matches jsonl sha256.
- Binary assets are never copied (only source paths recorded).
- `AutomaticTrainingEnabled == false` and `AutomaticMemoryPromotionEnabled
  == false` invariants are preserved.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LearningLab|PetMemory|Promotion"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/learning-datasets/<vNNNN-ts>/manifest.json
vnext/artifacts/learning-datasets/<vNNNN-ts>/examples.jsonl
vnext/artifacts/pet-tasks/<ts>-learning-promotion/promotion-report.json
```

Stop gates:

- Stop if any rejected/blocked example becomes a dataset row.
- Stop if approval bypass is possible.
- Stop if dataset manifest is missing or its hash does not match.
- Stop if memory write occurs without an approved card id.

Rollback: revert PR + delete the offending dataset version folder.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-66-reviewed-learning-promotion-gate`
- PR title: `C-PHASE 66: Reviewed learning promotion gate (no training)`.

Auto-continue? Yes.

### C-PHASE 67 — Local Embeddings + Learning / Eval Loop

Goal: replace the 16-dim hashing embedding with a real ONNX local embedder,
build a deterministic eval harness, prove behavior improvement is measurable
and reversible, all offline.

Sub-goal (discovered gap): without real embeddings the eval loop is mostly
theatrical. This phase intentionally bundles the embeddings upgrade with the
eval loop so promotion in C-PHASE 66 has something measurable to improve.

Scope:

- Add an `OnnxTextEmbeddingService` implementing `ITextEmbeddingService`.
  - Bundled model: `bge-micro-v2` ONNX (~22 MiB, MIT-licensed, BERT-tokenized,
    384-dim).
  - Located at `vnext/content/local-models/embeddings/bge-micro-v2/`.
  - Loads on first call; falls back to `HashingTextEmbeddingService` if the
    model file is missing or load fails; emits one safe-degrade audit line.
  - Use `Microsoft.ML.OnnxRuntime` (DirectML execution provider) so the same
    binary runs CPU and GPU.
- Update `PetMemoryStore.EmbeddingDimensions` to 384 (config-derived).
  Versioned migration: when dimensions change, the existing DB is renamed
  `.legacy-<ts>.db` and a fresh DB is created (mirroring the existing
  corruption-rebuild pattern).
- Add `LearningEvalService` that:
  - reads the latest promoted dataset version
  - builds a deterministic eval set (questions, expected source ids)
  - computes deterministic retrieval metrics: recall@1, recall@3,
    mrr, latency_ms_p50, latency_ms_p95
  - writes an `eval_run` evidence packet under
    `vnext/artifacts/pet-tasks/<ts>-eval-run/`
  - supports before/after comparisons: every run records baseline dataset
    version, current dataset version, score delta
- Eval is local, no-network, deterministic (no LLM judge).
- Stop-on-regression: if `recall@1` or `mrr` drops by more than 2% versus the
  prior eval run on the same eval set, the run records `regression=true` and
  refuses to mark the dataset version as the new baseline.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
vnext/src/Wevito.VNext.Core/Wevito.VNext.Core.csproj
vnext/content/tool_definitions.json
```

New files:

```text
vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs
vnext/src/Wevito.VNext.Core/LearningEvalService.cs
vnext/src/Wevito.VNext.Core/LearningEvalRecord.cs
vnext/content/local-models/embeddings/bge-micro-v2/README.md
vnext/content/local-models/embeddings/bge-micro-v2/.gitkeep
vnext/tests/Wevito.VNext.Tests/OnnxTextEmbeddingServiceTests.cs
vnext/tests/Wevito.VNext.Tests/LearningEvalServiceTests.cs
docs/C_PHASE67_LOCAL_EMBEDDINGS_AND_EVAL_LOOP_2026-05-12.md
```

Notes on the bundled model:

- The `.onnx` file itself is NOT committed in this phase. The phase commits
  the loader, the metadata, the README documenting how the user installs the
  weights, and the no-network fallback. Codex must not silently fetch the
  weights; users opt in via a single CLI helper `tools/install-local-embedder.ps1`
  that downloads the model into the content folder and writes a sha256
  manifest, with explicit prompts.
- Test fixtures use a tiny test ONNX model checked in under
  `vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/` or use the hashing
  fallback to keep tests deterministic.

Tests:

- ONNX service uses cached embeddings (idempotent for same input).
- Missing ONNX file → falls back to hashing service + audit line emitted.
- Eval service computes deterministic metrics for a fixed dataset.
- Regression sentinel works (drop of 2%+ recall blocks new baseline).
- No network request is made during eval (assert via a fake `IHttpClient`
  that throws on call).

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Embedding|LearningEval|PetMemory"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-eval-run/eval-report.json
vnext/artifacts/pet-tasks/<ts>-eval-run/run-summary.md
vnext/artifacts/learning-datasets/<vNNNN-ts>/eval-baseline.json
```

Stop gates:

- Stop if removing or corrupting the ONNX file breaks Wevito instead of
  degrading.
- Stop if any eval path makes a network call.
- Stop if regression check is bypassable.
- Stop if changing embedding dimensions silently re-embeds or corrupts existing
  memory.

Rollback:

- Revert PR. Existing pet memory DBs that were migrated will be left as
  `.legacy-<ts>.db` files; document a manual restore step in the phase report.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-67-local-embeddings-and-eval-loop`
- PR title: `C-PHASE 67: Local ONNX embeddings + deterministic eval loop`.

Auto-continue? **No.** Stop for user approval — this phase introduces a real
ML dependency and changes the embedding dimension.

### C-PHASE 68 — Self-Improvement Activity Reports & Audit Ledger

Goal: a single auditable record of everything Wevito did, and a daily/weekly
activity report the user can read. Make the "what is Wevito doing?" question
answerable with one screen.

Sub-goal (discovered gap): C-PHASE 65–67 produce evidence packets, but they
are scattered under `vnext/artifacts/pet-tasks/`. A central ledger is needed
before C-PHASE 70–75 add more sensitive activity (web fetch, file read, model
inference, mutation).

Scope:

- Add `AuditLedgerService` that owns
  `%LOCALAPPDATA%/Wevito/audit/ledger.sqlite`.
  - schema: id, packet_id, packet_kind, task_card_id, created_at_utc,
    did_use_network, did_use_hosted_ai, did_use_local_model, did_mutate,
    artifact_path, summary, status, error
  - every adapter and service calls `AuditLedgerService.Record(packet)` when
    it produces an evidence packet
  - append-only; rows are immutable
  - has a `Snapshot(since, until, filters)` query method
- Add `ActivitySummaryService` that:
  - reads the ledger and produces daily/weekly summaries
  - groups by `packet_kind`
  - flags any `did_use_network=true`, `did_use_hosted_ai=true`,
    `did_mutate=true` rows for prominent display
- Settings UI gets a "Wevito activity" panel:
  - "Today" tab (default)
  - "This week" tab
  - rows are click-through to the artifact path
- The home/tool popup gets a one-line status: "Last action: <kind> at <hh:mm>
  · today: <N> previews, <M> approvals, 0 mutations".
- No screenshots or sprite preview thumbnails in the report — kept text-only
  to honor the "pets remain regular pet simulator characters" rule.

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/LearningLabPromotionService.cs
vnext/src/Wevito.VNext.Core/LearningEvalService.cs
vnext/src/Wevito.VNext.Core/AutonomousTaskScheduler.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
```

New files:

```text
vnext/src/Wevito.VNext.Core/AuditLedgerService.cs
vnext/src/Wevito.VNext.Core/EvidencePacket.cs
vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs
vnext/tests/Wevito.VNext.Tests/AuditLedgerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ActivitySummaryServiceTests.cs
docs/C_PHASE68_ACTIVITY_REPORTS_AND_AUDIT_LEDGER_2026-05-12.md
```

Tests:

- Ledger is append-only; updating a row is rejected.
- Activity summary correctly aggregates rows by kind, day, and flags.
- Ledger survives process restart (file-backed).
- All existing adapters emit at least one packet per preview run.
- `did_use_*` flags are required (non-null) for every row.

Validation commands:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AuditLedger|ActivitySummary|Adapter"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
%LOCALAPPDATA%/Wevito/audit/ledger.sqlite
vnext/artifacts/pet-tasks/<ts>-activity-summary/run-summary.md
```

Stop gates:

- Stop if any preview/execution path bypasses the ledger.
- Stop if any `did_use_*` flag can be left null/unspecified.
- Stop if the report shows mutation that has no proof packet attached.
- Stop if private text appears in summaries without the source label.

Rollback: revert PR. The `ledger.sqlite` file can be left in place; nothing
reads from it after revert.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-68-activity-reports-and-audit-ledger`
- PR title: `C-PHASE 68: Audit ledger and self-improvement activity reports`.

Auto-continue? Yes.

### C-PHASE 69 — Independent Assistant Beta Gate

Goal: decide whether Wevito is ready to begin limited autonomy in C-PHASE 75.
This is a decision phase, not a feature phase. The output is a packet and a
user-checked acceptance list.

Scope:

- Add `IndependentAssistantBetaGateService` that aggregates evidence from:
  - long-session run (4+ hours of uptime in `Active` mode)
  - quiet-mode honored under fullscreen and during user keystrokes
  - `RuntimeBudgetMeter` not exceeded in the run
  - tool preview/execution history present in ledger
  - learning promotion proof: at least one accepted dataset + eval improved
    or unchanged
  - offline proof: a full session run with `ModelProviderMode=LocalOnly` and
    no `did_use_network` row in the ledger
- Add a `BetaGateDecision` packet (`enable_limited_autonomy`,
  `keep_preview_only`, `pause_for_safety_work`).
- Add a single-button "Run beta-gate check" entry in Settings → Activity.
- Add a hard kill switch: a `KillSwitchService` that observes a single
  setting `runtime_kill_switch` (default false). When true, every adapter,
  scheduler, model adapter, web connector, file access service, and mutation
  service refuses to run.
- The decision packet records exactly which checks passed/failed and links to
  the supporting ledger ids.

New files:

```text
vnext/src/Wevito.VNext.Core/IndependentAssistantBetaGateService.cs
vnext/src/Wevito.VNext.Core/BetaGateDecision.cs
vnext/src/Wevito.VNext.Core/KillSwitchService.cs
vnext/tests/Wevito.VNext.Tests/IndependentAssistantBetaGateServiceTests.cs
vnext/tests/Wevito.VNext.Tests/KillSwitchServiceTests.cs
docs/C_PHASE69_INDEPENDENT_ASSISTANT_BETA_GATE_2026-05-12.md
```

Tests:

- Decision = `enable_limited_autonomy` only when every check passes.
- Decision = `keep_preview_only` if one or more checks fail but no safety
  issue is present.
- Decision = `pause_for_safety_work` if any safety check fails.
- Kill switch blocks every adapter/service in the chain.
- Kill switch state is persisted and survives restart.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "BetaGate|KillSwitch"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-beta-gate/beta-gate-decision.json
vnext/artifacts/pet-tasks/<ts>-beta-gate/run-summary.md
```

Stop gates:

- Stop if any check is bypassable.
- Stop if kill switch is not honored by any single adapter.
- Stop if "enable" decision is possible without `LocalOnly` proof.

Rollback: revert PR; kill switch is a single setting that defaults off.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-69-independent-assistant-beta-gate`
- PR title: `C-PHASE 69: Independent-assistant beta gate + kill switch`.

Auto-continue? **No.** This is a user-decision gate.

### C-PHASE 70 — Approved Web Research Connector

Goal: let Wevito fetch web sources through its own connector (not a browser
handoff), record citations and provenance, never enter hosted-AI reasoning,
honor privacy filters.

Scope:

- Add `WebResearchConnector` in Core with a pluggable `IWebSearchBackend`:
  - default backend: `OfflineWebSearchBackend` (returns empty + audit line)
  - real backends are loaded by user-configured backend id:
    - `brave-search` (REST, requires user-provided API key in Windows
      Credential Manager under `Wevito/web-search/brave`)
    - `tavily` (REST, key under `Wevito/web-search/tavily`)
    - `firecrawl` (REST, key under `Wevito/web-search/firecrawl`)
  - the connector itself never logs in or auto-discovers keys; it only reads
    keys when `web_search_enabled=true` AND a backend id is selected AND a
    `TaskCard.Status=Approved` is presented.
- Every fetch produces a `WebFetchRecord`:
  - url (original)
  - canonical_url (after redirect)
  - http_status
  - fetched_at_utc
  - title
  - excerpt (text only, first 1KB)
  - content_sha256 (of full body text)
  - response_headers (subset: content-type, last-modified, etag)
  - cache_path under `%LOCALAPPDATA%/Wevito/web-cache/<yyyymmdd>/<sha256>.json`
- Each task-card-scoped fetch run writes a `web_fetch` evidence packet under
  `vnext/artifacts/pet-tasks/<ts>-web-research/` with all `WebFetchRecord`s
  and a manifest with totals.
- Privacy filter: before any query is sent, run it through
  `WebQueryPrivacyFilter` that:
  - strips paths matching `[A-Za-z]:\\` (Windows local paths)
  - strips `%LOCALAPPDATA%`, `%USERPROFILE%`, `%APPDATA%`
  - strips strings that look like API keys, credentials, or email addresses
  - refuses queries shorter than 8 characters or that contain only stripped
    content (returns Blocked)
- Rate limit: respect `web_max_fetches_per_hour` (default 30) and
  `web_max_fetches_per_task` (default 5).
- `WebResearchConnector` does NOT send query text or page text to any hosted
  AI. The fetched pages become local sources for `ResearchPlannerService`.
- Settings UI: Wevito → Settings → Web research panel:
  - dropdown of backend ids (default `offline`)
  - "store key in Credential Manager" button per backend
  - "test connection" button (sends a known-safe query like "wikipedia")
  - "purge cache" button

New files:

```text
vnext/src/Wevito.VNext.Core/WebResearchConnector.cs
vnext/src/Wevito.VNext.Core/IWebSearchBackend.cs
vnext/src/Wevito.VNext.Core/OfflineWebSearchBackend.cs
vnext/src/Wevito.VNext.Core/BraveSearchBackend.cs
vnext/src/Wevito.VNext.Core/TavilySearchBackend.cs
vnext/src/Wevito.VNext.Core/FirecrawlSearchBackend.cs
vnext/src/Wevito.VNext.Core/WebFetchRecord.cs
vnext/src/Wevito.VNext.Core/WebQueryPrivacyFilter.cs
vnext/src/Wevito.VNext.Core/WindowsCredentialStore.cs (new credential store wrapper)
vnext/tests/Wevito.VNext.Tests/WebResearchConnectorTests.cs
vnext/tests/Wevito.VNext.Tests/WebQueryPrivacyFilterTests.cs
docs/C_PHASE70_APPROVED_WEB_RESEARCH_CONNECTOR_2026-05-12.md
```

Tests:

- Default backend = `offline`. Default `web_search_enabled=false`.
- A fetch without an approved card returns `Blocked`.
- A fetch with `KillSwitch=true` returns `Blocked`.
- A fetch under `Quiet`/`PetOnly`/`Fullscreen-quiet` is blocked or deferred.
- Privacy filter strips Windows paths and env-var-like names.
- Rate limits are enforced per hour and per task.
- Fake backend can be injected; all real backends use the same backend
  interface so tests do not hit the network.
- Cache lookup short-circuits a re-fetch within TTL; re-fetch is allowed
  with an explicit `force=true` flag from the user.
- All fetches write to the audit ledger.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Web|Privacy"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
%LOCALAPPDATA%/Wevito/web-cache/<yyyymmdd>/<sha256>.json
vnext/artifacts/pet-tasks/<ts>-web-research/web-fetch-manifest.json
vnext/artifacts/pet-tasks/<ts>-web-research/run-summary.md
```

Stop gates:

- Stop if any network request happens before approval.
- Stop if any query bypasses the privacy filter.
- Stop if fetched content reaches a hosted AI in this phase (hosted AI is
  not enabled in C-PHASE 70).
- Stop if cache entries omit content_sha256 or fetched_at_utc.
- Stop if API keys are read silently or stored anywhere except Credential
  Manager.

Rollback: revert PR; purge cache folder if needed.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-70-approved-web-research-connector`
- PR title: `C-PHASE 70: Approved web research connector with citations`.

Auto-continue? **No.** First phase that enables real network activity. Stop
for user approval.

### C-PHASE 71 — Approved Local File & Tool Access + Unified Policy Engine

Goal: let Wevito read approved local folders and run approved non-destructive
local tools, through a single policy engine that consolidates the existing
allowlist evaluators.

Scope:

- Add `LocalToolAccessPolicy` with:
  - allowlist roots (configurable, default: `vnext/`, `docs/`, `tools/`,
    `vnext/artifacts/`)
  - denylist roots (always denied, even if inside allowlist):
    `.git`, `**/secrets`, `**/.env*`, `**/credentials`, `%USERPROFILE%/.ssh`,
    `%LOCALAPPDATA%/Microsoft/`, `%APPDATA%/Microsoft/`
  - allow/deny by tool family:
    - `localDocs`, `localResearch`, `petState`, `assetInventory`,
      `codeReview`, `buildProof` → read-only
    - `screenCapture`, `audioAssist`, `translateText` → existing rules
    - `localToolExec` (new): a *narrowly scoped* tool family that runs
      whitelisted PowerShell scripts under `tools/` only, in dry-run mode by
      default
- Consolidate into `UnifiedPolicyService`:
  - subsumes `HelperAllowlistEvaluator`, `ToolPolicyEvaluator`,
    `CapturePolicyEvaluator`, `ProofExecutionAllowlistEvaluator`, plus the
    new `LocalToolAccessPolicy`
  - the existing evaluators remain as adapters delegating to the unified
    service so tests in those classes still pass
  - returns `PolicyDecision { Status, Reason, EffectiveRoots,
    EffectiveDenylist, ApprovalRequirement }`
  - records every decision to the audit ledger
- Each adapter that reads files calls `UnifiedPolicyService.EvaluateRead(path)`
  before reading. If `Blocked`, return `TaskAdapterResult.Blocked` with the
  policy reason.
- Provide a single Settings panel "Local access" showing:
  - the active allowlist roots
  - the active denylist
  - per-tool-family allow/deny toggle (most default to read-only allow,
    `localToolExec` defaults to blocked)

New files:

```text
vnext/src/Wevito.VNext.Core/LocalToolAccessPolicy.cs
vnext/src/Wevito.VNext.Core/UnifiedPolicyService.cs
vnext/src/Wevito.VNext.Core/PolicyDecision.cs
vnext/src/Wevito.VNext.Core/LocalToolExecutionPreviewAdapter.cs
vnext/tests/Wevito.VNext.Tests/LocalToolAccessPolicyTests.cs
vnext/tests/Wevito.VNext.Tests/UnifiedPolicyServiceTests.cs
vnext/tests/Wevito.VNext.Tests/LocalToolExecutionPreviewAdapterTests.cs
docs/C_PHASE71_APPROVED_LOCAL_FILE_TOOL_ACCESS_2026-05-12.md
```

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/HelperAllowlistEvaluator.cs
vnext/src/Wevito.VNext.Core/ToolPolicyEvaluator.cs
vnext/src/Wevito.VNext.Core/CapturePolicyEvaluator.cs
vnext/src/Wevito.VNext.Core/ProofExecutionAllowlistEvaluator.cs
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
vnext/src/Wevito.VNext.Core/LocalDocsPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/AssetInventoryPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/CodeReviewPreviewAdapter.cs
vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
```

Tests:

- Read of a denylisted path (.git, .env*, .ssh) is always blocked.
- Read outside allowlist roots is blocked.
- `localToolExec` default = blocked. Even after enable, requires per-script
  allowlist by sha256.
- Unified service emits exactly one ledger row per evaluation.
- The four legacy evaluators still produce the same decisions for previously
  green tests (regression).
- Symlink traversal, `..` traversal, and case-folded variants are blocked.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Policy|Allow|Access|LocalTool"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-local-access/policy-decision-log.json
vnext/artifacts/pet-tasks/<ts>-local-tool-exec/dry-run-plan.json
```

Stop gates:

- Stop if any adapter reads a path without consulting `UnifiedPolicyService`.
- Stop if denylist can be overridden by user allowlist.
- Stop if symlink or `..` traversal escapes a root.
- Stop if `localToolExec` ever runs in non-dry-run by default.

Rollback: revert PR. Legacy evaluator code paths remain unaffected because
adapters still expose them; only the consolidation revert is needed.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-71-approved-local-file-tool-access`
- PR title: `C-PHASE 71: Approved local file & tool access + unified policy`.

Auto-continue? **No.** Adds new access surface. Stop for user approval.

### C-PHASE 72 — Local Model Runtime Integration

Goal: connect a real local inference runtime so Wevito can reason without a
hosted AI. Default integration is Ollama (OpenAI-compatible REST on
`http://127.0.0.1:11434`); ONNX Runtime GenAI and LLamaSharp are optional
in-process backends added in follow-up patches but not required.

Scope:

- Add `OllamaLocalModelAdapter` implementing `IModelAdapter`:
  - reads endpoint from `local_runtime_ollama_endpoint` (default
    `http://127.0.0.1:11434`)
  - reads model id from `local_runtime_ollama_model` (default `llama3.2:3b`)
  - on first call: probes `GET /api/tags`; if the runtime is not running or
    the model is missing, the adapter records `LocalProviderAvailable=false`
    in `ModelProviderModeService` and returns a deterministic
    `ModelResponse(DidCallProvider=false, BlockReason=...)`
  - when available, calls `POST /v1/chat/completions` (OpenAI-compatible)
  - measures latency, records prompt hash, response hash, model id, runtime
    id, and `EvidencePacket.kind=model_inference`
- Add `LocalRuntimeProbeService`:
  - periodically (every 5 min in `Active`, never in `Quiet`/`PetOnly`)
    re-probes `/api/tags`
  - updates `LocalProviderAvailable`
  - the probe never auto-pulls a model
- Selection logic in `ModelProviderModeService`:
  - `Disabled` → no adapter is called
  - `LocalOnly` → `OllamaLocalModelAdapter` if available, else
    `LocalModelAdapter` (deterministic fallback)
  - `ApprovedCloud` → only if user explicitly approved both the mode and the
    provider id; otherwise treat as `Disabled` for this call
- Settings UI: "Local AI" panel:
  - Mode (Disabled / Local only / Approved cloud)
  - Local runtime (Ollama HTTP / ONNX in-process / LLamaSharp in-process)
  - Endpoint, model id, "probe now" button, last probe result
  - "What model is Wevito using right now?" status line
- Document optional backends:
  - `OnnxLocalModelAdapter` (Phi-3-mini-4k INT4 via ONNX Runtime GenAI +
    DirectML) — sketched but kept behind a feature flag
  - `LLamaSharpLocalModelAdapter` (in-process GGUF via LLamaSharp NuGet) —
    sketched but kept behind a feature flag
- Hard rule: no model adapter is allowed to read API keys for hosted
  providers. The only credential a local adapter reads is the local endpoint
  string. ApprovedCloud routes go through a future `HostedModelAdapter` that
  is *not* added in this phase.

New files:

```text
vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs
vnext/src/Wevito.VNext.Core/LocalRuntimeProbeService.cs
vnext/src/Wevito.VNext.Core/ModelInferenceEvidencePacket.cs
vnext/tests/Wevito.VNext.Tests/OllamaLocalModelAdapterTests.cs
vnext/tests/Wevito.VNext.Tests/LocalRuntimeProbeServiceTests.cs
docs/C_PHASE72_LOCAL_MODEL_RUNTIME_INTEGRATION_2026-05-12.md
```

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/ModelProviderMode.cs
vnext/src/Wevito.VNext.Core/LocalModelAdapter.cs
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

Tests:

- Adapter returns deterministic fallback when endpoint is unreachable.
- Adapter records prompt hash + response hash in the packet.
- Probe is dormant in `Quiet`/`PetOnly`.
- `LocalOnly` never accidentally calls a hosted endpoint (use a fake HTTP
  client that fails any non-localhost connection in tests).
- Latency is recorded and bounded by a configurable timeout (default 30s).
- KillSwitch blocks the adapter.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Ollama|LocalRuntime|ModelProvider"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-model-inference/inference-record.json
vnext/artifacts/pet-tasks/<ts>-model-inference/run-summary.md
```

Stop gates:

- Stop if a hosted endpoint can be reached in `LocalOnly`.
- Stop if model id, runtime id, or prompt/response hash is missing from
  packets.
- Stop if probe runs in `Quiet`/`PetOnly`.
- Stop if API keys are read by any local adapter.
- Stop if fallback to deterministic adapter is not exercised by tests.

Rollback: revert PR; deterministic adapter resumes.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-72-local-model-runtime-integration`
- PR title: `C-PHASE 72: Local model runtime (Ollama-first, safe degrade)`.

Auto-continue? **No.** Real model. Stop for user approval.

### C-PHASE 73 — Local Training / Tuning Pipeline

Goal: improve local behavior from reviewed data without uncontrolled training.
Start with retrieval and routing tuning; LoRA fine-tuning is gated as the
last step inside this phase.

Sub-phases (internal, all in one PR):

- 73A retrieval tuning (rerank weights, BM25/embedding fusion)
- 73B routing tuning (which helper handles which intent)
- 73C config tuning (prompt templates, system messages, top-k, threshold)
- 73D LoRA pilot (optional, gated by user)

Scope:

- Add `LocalTrainingPlanService` that:
  - reads the latest promoted dataset version
  - emits a `train_plan` manifest:
    `{ dataset_version, baseline_eval_id, target_metric, allowed_changes,
       budget_minutes, sha256s, train_id }`
  - target metrics use `LearningEvalService` numbers from C-PHASE 67
  - `allowed_changes` is an explicit list: e.g. `["rerank_weights",
    "router_config"]`; LoRA appears only when `tuning_lora_enabled=true`
- Add `LocalTuningRunner`:
  - executes plan steps in dry-run by default
  - 73A: tunes a small linear rerank head on top of embeddings
  - 73B: writes a routing table `vnext/content/router-config-<ts>.json`
  - 73C: writes a prompt-template config `vnext/content/prompt-config-<ts>.json`
  - 73D (LoRA): writes a Python plan file `tools/run-unsloth-lora.ps1` plus a
    `.bat` script; Codex must NOT auto-run it. The script wraps Unsloth/QLoRA;
    expected environment is documented in the phase report. The adapter
    treats the LoRA stage as out-of-process, evidence-only.
- Eval-after step uses `LearningEvalService.EvaluateAgainst(baseline)`:
  - if the new metric is worse than the baseline by more than a configurable
    tolerance, the change is auto-rolled back (file deletion + audit row)
  - if better, the new config is promoted by writing a `tuning_apply`
    evidence packet and registering the new config file in the active config
    pointer
- Rollback to a prior baseline is one click: pick a baseline id from the
  ledger; the service restores the prior config file.

New files:

```text
vnext/src/Wevito.VNext.Core/LocalTrainingPlanService.cs
vnext/src/Wevito.VNext.Core/LocalTuningRunner.cs
vnext/src/Wevito.VNext.Core/RerankHead.cs
vnext/src/Wevito.VNext.Core/RouterConfig.cs
vnext/src/Wevito.VNext.Core/PromptConfigStore.cs
tools/run-unsloth-lora.ps1
tools/run-unsloth-lora.bat
vnext/tests/Wevito.VNext.Tests/LocalTrainingPlanServiceTests.cs
vnext/tests/Wevito.VNext.Tests/LocalTuningRunnerTests.cs
vnext/tests/Wevito.VNext.Tests/RerankHeadTests.cs
docs/C_PHASE73_LOCAL_TRAINING_TUNING_PIPELINE_2026-05-12.md
```

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/LearningEvalService.cs
vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
vnext/src/Wevito.VNext.Core/AuditLedgerService.cs
```

Tests:

- Plan refuses to include `lora_train` when `tuning_lora_enabled=false`.
- Dry-run does not write to non-allowlisted paths.
- Eval-after regression triggers automatic rollback.
- Rollback restores the prior config byte-for-byte (sha256 match).
- LoRA stage is gated and only writes a plan file + script — never executes
  Python directly from .NET.
- KillSwitch blocks the runner.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Training|Tuning|RerankHead|Router"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-train-plan/train-plan.json
vnext/artifacts/pet-tasks/<ts>-tuning-run/tuning-result.json
vnext/content/router-config-<ts>.json
vnext/content/prompt-config-<ts>.json
vnext/content/active-config.json   (pointer to the current versions)
```

Stop gates:

- Stop if any tuning step writes outside `vnext/content/` or
  `vnext/artifacts/`.
- Stop if eval-after is bypassable.
- Stop if rollback does not match by sha256.
- Stop if LoRA stage runs without explicit user start.

Rollback: revert PR; delete the latest router/prompt config files; restore
prior `active-config.json` from the audit ledger.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-73-local-training-tuning-pipeline`
- PR title: `C-PHASE 73: Local training/tuning pipeline (retrieval → router → prompt → LoRA)`.

Auto-continue? **No.** Real learning surface. Stop for user approval.

### C-PHASE 74 — Guarded Code & Asset Mutation Execution (Generalized)

Goal: let Wevito make reversible code or asset changes only through
exact-scope proof packets, generalizing the sprite-only mutation gate.

Scope:

- Add `GuardedMutationPlan` (record) and `GuardedMutationService`:
  - inputs: `target_paths`, `proposed_changes` (list of `Edit` records), 
    `reason`, `task_card_id`, `source_evidence_packets`
  - dry-run produces a unified diff + computes sha256 of every target before
    and after the proposed change
  - backup step: for each target, write
    `vnext/artifacts/mutation-backups/<ts>-<scope>/<relpath>.backup`
    + a manifest with original sha256s
  - apply step writes new content + records new sha256s in the manifest
  - post-proof step runs an exact-scope verification:
    - `dotnet build` if any `.cs`/`.csproj`/`.sln` was touched
    - `dotnet test` if any test file was touched
    - `audit_sprite_contract.py` if any sprite path was touched
    - `report_runtime_canvas_mismatches.py` if any runtime PNG was touched
  - rollback step: restore each target from backup, verify sha256 match,
    delete the new files
- All four steps emit evidence packets and ledger rows.
- The service refuses to apply if:
  - any target falls outside the unified policy allowlist
  - any target is in the denylist
  - `KillSwitch=true`
  - `RuntimeSupervisorStatus.Mode != Active`
  - the corresponding `TaskCard.Status != Approved`
- Existing `SpriteWorkflowApplyService`, `SpriteWorkflowDryRunApplyService`,
  `SpriteWorkflowRollbackService`, and `SpriteWorkflowPostApplyProof` are
  refactored to delegate to `GuardedMutationService` while keeping their
  public APIs.

New files:

```text
vnext/src/Wevito.VNext.Core/GuardedMutationPlan.cs
vnext/src/Wevito.VNext.Core/GuardedMutationService.cs
vnext/src/Wevito.VNext.Core/MutationVerifier.cs
vnext/tests/Wevito.VNext.Tests/GuardedMutationServiceTests.cs
vnext/tests/Wevito.VNext.Tests/MutationVerifierTests.cs
docs/C_PHASE74_GUARDED_CODE_ASSET_MUTATION_2026-05-12.md
```

Files likely touched:

```text
vnext/src/Wevito.VNext.Core/SpriteWorkflowApplyService.cs
vnext/src/Wevito.VNext.Core/SpriteWorkflowDryRunApplyService.cs
vnext/src/Wevito.VNext.Core/SpriteWorkflowRollbackService.cs
vnext/src/Wevito.VNext.Core/SpriteWorkflowPostApplyProof.cs
vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs
```

Tests:

- Dry-run never writes outside the backup folder.
- Apply refuses if any target is outside policy allowlist.
- Apply refuses if `RuntimeSupervisorStatus.Mode != Active`.
- Apply refuses if task card is not Approved.
- Rollback restores byte-for-byte (sha256 match).
- Post-proof runs the matching command for the touched file type.
- Existing sprite tests still pass (regression).
- KillSwitch blocks the service.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Mutation|SpriteWorkflow"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-mutation-proposal/mutation-plan.json
vnext/artifacts/pet-tasks/<ts>-mutation-apply/mutation-apply-manifest.json
vnext/artifacts/pet-tasks/<ts>-mutation-rollback/rollback-manifest.json
vnext/artifacts/mutation-backups/<ts>-<scope>/...
```

Stop gates:

- Stop if scope expands after approval.
- Stop if rollback is not byte-exact.
- Stop if post-proof command does not match the touched file type.
- Stop if any sprite test regression appears.

Rollback: revert PR. Backups already produced remain usable.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-74-guarded-code-asset-mutation`
- PR title: `C-PHASE 74: Guarded code & asset mutation (generalized)`.

Auto-continue? **No.** First phase that mutates beyond sprites. Stop for user
approval.

### C-PHASE 75 — Autonomous Operations Beta

Goal: enable a narrow, fully gated autonomous loop end-to-end. The user can
leave Wevito running and it will:

- propose work (C-PHASE 65)
- research locally (C-PHASE 64) + optionally web (C-PHASE 70)
- read approved files (C-PHASE 71)
- reason with the local model (C-PHASE 72)
- propose mutations (C-PHASE 74)
- never apply mutations without user approval
- maintain audit ledger + activity reports (C-PHASE 68)

Scope:

- Add `AutonomousOperationsLoop` in Core:
  - orchestrates the chain above on a single timer tick
  - per tick: ask supervisor → if `Active` and budget allows → run one
    scheduled proposal → produce one preview → stop
  - emits one `activity_summary` packet per loop iteration
- Add `AutonomousBetaDecisionService`:
  - reads the beta gate decision (C-PHASE 69)
  - reads `runtime_autonomous_beta_enabled` (default false)
  - returns whether the loop may run
- Add a hard daily-cap: max N autonomous iterations per day (default 20),
  configurable.
- Add a "what is Wevito doing?" live status that updates per tick.
- No new mutation surface is added — only orchestration of existing ones.
- The final decision packet (`autonomous_operations_beta_decision`) is the
  required output of this phase, with checks:
  - 7 days of `Active` uptime with zero policy violations
  - 100% of mutations have proof packets
  - zero hosted-AI calls in `LocalOnly` mode
  - zero focus-steal events recorded
  - resource budget held within +/- 10% of configured limits
  - decision label ∈ `{ enable_autonomous_beta, keep_supervised_preview,
    pause_for_reliability_work }`

New files:

```text
vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs
vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
vnext/src/Wevito.VNext.Core/AutonomousOperationsConfig.cs
vnext/tests/Wevito.VNext.Tests/AutonomousOperationsLoopTests.cs
vnext/tests/Wevito.VNext.Tests/AutonomousBetaDecisionServiceTests.cs
docs/C_PHASE75_AUTONOMOUS_OPERATIONS_BETA_2026-05-12.md
```

Tests:

- Loop is dormant if `runtime_autonomous_beta_enabled=false`.
- Loop is dormant if `KillSwitch=true`.
- Loop is dormant in `Quiet`/`PetOnly`/`Fullscreen-quiet`.
- Loop never executes a mutation; only proposes.
- Daily cap is enforced.
- Beta decision is fully evidence-driven (refuses to compute without ledger
  rows).
- KillSwitch immediately stops the loop mid-iteration.

Validation:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Autonomous|BetaDecision"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Artifacts:

```text
vnext/artifacts/pet-tasks/<ts>-autonomous-iteration/iteration-summary.json
vnext/artifacts/pet-tasks/<ts>-autonomous-beta-decision/decision.json
```

Stop gates:

- Stop if the loop can execute a mutation directly.
- Stop if the loop ignores supervisor mode or kill switch.
- Stop if any iteration lacks an artifact trail.
- Stop if a hosted-AI call appears in `LocalOnly` mode.
- Stop if resource budgets are exceeded.

Rollback: revert PR; the rest of the system continues to function in
preview-first mode.

Commit / PR instructions:

- Branch: `claude-implementation/c-phase-75-autonomous-operations-beta`
- PR title: `C-PHASE 75: Autonomous operations beta (orchestration only)`.

Auto-continue? **No.** Final user-decision gate.

## 3. Phase Sequencing Rules

- **Always green main**: every phase merges only if `dotnet build`,
  `dotnet test`, and `tools\build-vnext.ps1 -SkipAssetPrep -SkipTests`
  all pass.
- **One PR per phase**: keep PRs reviewable.
- **Phase report doc**: every phase commits a phase report under `docs/`.
- **Branch naming**: `claude-implementation/c-phase-<N>-<short-slug>`.
- **Auto-continue rule**: green-light phases (65, 66, 68) can chain. All
  other phases require user approval before the next phase begins.
- **No skipping**: even if a phase looks "small", do not bundle it with
  another phase. The PR boundary is part of the safety system.
- **No retroactive flag flips**: once a setting is default-off, it stays
  default-off across all subsequent phases.

## 4. Discovered Gaps & How They Were Folded In

| Gap | Folded into |
| --- | --- |
| Real embeddings before eval loop | C-PHASE 67 |
| Audit ledger before web/file/model phases | C-PHASE 68 |
| Hard kill switch before any autonomy | C-PHASE 69 |
| Unified policy engine before broad file access | C-PHASE 71 |
| Local credential store wrapper | C-PHASE 70 (`WindowsCredentialStore`) |
| Generalized mutation gate before code mutation | C-PHASE 74 |
| Daily cap + per-iteration activity packet | C-PHASE 75 |

## 5. References Used

- `docs/POST_CPHASE62_INDEPENDENT_AI_ROADMAP_2026-05-12.md`
- `docs/C_PHASE64_LOCAL_FIRST_AI_RESEARCH_STRATEGY_2026-05-12.md`
- `docs/C_PHASE63_ALWAYS_ON_RUNTIME_SUPERVISOR_2026-05-12.md`
- `docs/C_PHASE61_CREATIVE_LEARNING_LAB_SEED_2026-05-12.md`
- `docs/C_PHASE53_PROJECT_PROGRESS_REPORT_2026-05-12.md`
- `docs/WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md`
- Source survey of `vnext/src/Wevito.VNext.Core` and
  `vnext/src/Wevito.VNext.Shell`.
- Research notes (May 2026):
  - LLamaSharp 0.27 / llama.cpp on Windows
  - Ollama 0.x OpenAI-compatible REST on `:11434`
  - ONNX Runtime GenAI + DirectML (Phi-3-mini-4k INT4)
  - `sqlite-vec` 0.1.7 (already in repo) vs DuckDB VSS / LanceDB
  - `SmartComponents.LocalEmbeddings` ONNX bge-micro-v2 (~22 MiB)
  - Unsloth / QLoRA Windows guidance
  - Microsoft Agent Governance Toolkit (April 2026) — default-deny policy
    engine, SLSA provenance, eval gates
  - Brave Search API / Tavily / Firecrawl — privacy + citation comparison
  - RAGAS / TruLens / DeepEval — deterministic golden datasets, regression as
    release gate
  - Self-improving agent verifiability constraint and rollback patterns
