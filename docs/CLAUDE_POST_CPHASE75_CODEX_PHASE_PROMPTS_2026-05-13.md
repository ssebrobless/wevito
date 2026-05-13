# Wevito Post-C-PHASE-75 — Codex Phase Prompts (Medium Effort)

Date: 2026-05-13
Author: Claude (Opus 4.7)
Audience: Codex at medium reasoning effort
Source of truth: `docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md`

These prompts are Codex-ready. Each is self-contained, sized for one PR at medium
reasoning effort, and reuses the conventions established in C-PHASE 65–75.
Run them in order. Do not skip phases or bundle them.

## Shared Preamble (mentally include with each prompt)

```text
Repo: C:\Users\fishe\Documents\projects\wevito (or current worktree)

Read first:
  docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md
  docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md
  docs/C_PHASE75_AUTONOMOUS_OPERATIONS_BETA_2026-05-12.md
  docs/C_PHASE74_GUARDED_CODE_ASSET_MUTATION_2026-05-12.md
  docs/C_PHASE72_LOCAL_MODEL_RUNTIME_INTEGRATION_2026-05-12.md
  docs/C_PHASE68_ACTIVITY_REPORTS_AND_AUDIT_LEDGER_2026-05-12.md  (if needed)

Hard invariants (apply to every phase):
- no hosted-AI dependency for normal runtime
- no hidden web access, no hidden training, no hidden local file access
- no hidden tool execution, no hidden code/asset mutation
- pause/quiet/pet-only/fullscreen-quiet is always honored
- pets remain regular pet sprites (no task animation)
- every mutation: exact scope + dry-run + backup hash + rollback + post-proof
- every learning step: reviewed data + eval gate + rollback
- every web fetch: citation + provenance + privacy filter
- every local model use degrades safely when runtime/model absent
- every adapter writes an EvidencePacket row to AuditLedgerService
- every adapter honors KillSwitchService
- every file read goes through UnifiedPolicyService

Validation baseline (always run at the end of phase before opening PR):
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

PR rules:
- one phase per PR
- branch: claude-implementation/c-phase-<N>-<short-slug>
- commit messages reference C-PHASE <N>
- include phase report under docs/
- do NOT merge if any test, build, or smoke fails
- do NOT flip any default-off setting in this phase block
```

## C-PHASE 76 — Functional Local ONNX Embeddings

```text
Implement C-PHASE 76 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-76-functional-local-onnx-embeddings

Goal: replace the placeholder ONNX embedding path with real bge-micro-v2
inference, while keeping the deterministic hashing fallback intact for
test and degrade paths.

Add (new files):
- vnext/src/Wevito.VNext.Core/OnnxEmbeddingBackend.cs
- vnext/tests/Wevito.VNext.Tests/OnnxEmbeddingBackendTests.cs
- vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/README.md
- vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/model.onnx     (≤ 100 KB)
- vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/tokenizer.json
- docs/C_PHASE76_FUNCTIONAL_LOCAL_ONNX_EMBEDDINGS_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs
  (replace EmbedWithOnnxFallback with a real implementation using
   the new backend; keep the safe-degrade path; keep cache; keep audit line;
   never throw escapes)
- vnext/src/Wevito.VNext.Core/Wevito.VNext.Core.csproj
  (add the Microsoft.SemanticKernel.Connectors.Onnx package OR add a
   tokenizer dependency such as FastBertTokenizer; pick one and document)
- vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
  (verify the legacy DB rename + rebuild path still works; no behavior change
   required beyond a regression test if the existing path is fine)
- tools/install-local-embedder.ps1
  (also fetch tokenizer.json + vocab.txt; produce sha256 manifest)
- vnext/content/local-models/embeddings/bge-micro-v2/README.md
  (document model + tokenizer install steps and sha256s)
- vnext/content/tool_definitions.json
  (pet_memory entry: embeddingProviderWhenOnnxActive=bge-micro-v2)

Implementation guidance:
- Prefer Microsoft.SemanticKernel.Connectors.Onnx.BertOnnxTextEmbeddingGenerationService
  as the engine: SmartComponents.LocalEmbeddings is now a wrapper around it.
- The output must be L2-normalized 384-dim float[].
- Two identical inputs must return identical vectors (cache).
- A missing model or tokenizer must cause exactly one audit line:
  "embedding-degrade | <reason>" and silent fallback to HashingTextEmbeddingService.
- Tests must NOT download or load the real bge-micro-v2; they use the tiny
  fixture under vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/ which:
    - encodes a deterministic toy bert with 8-dim hidden state OR
    - causes the loader to fail predictably so the hashing fallback is verified

Hard rules:
- no network call in tests (assert via fake HttpClient that throws on send)
- ONNX inference only runs when IsOnnxReady == true (model + tokenizer present)
- audit line emitted exactly once on degrade (not per Embed call)
- L2-normalize before returning
- never throw out of Embed

Tests required:
- two identical inputs → identical vectors
- two different inputs → cosine != 1.0
- missing tokenizer → single audit line + hashing fallback
- missing model → single audit line + hashing fallback
- corrupt session via injected factory → fallback
- Dimensions = 384 when ONNX ready, 16 when degraded
- PetMemoryStore legacy DB rename + fresh DB rebuild when dimension changes

Validation: baseline plus
  dotnet test --filter "Embedding|PetMemory"

Phase report: write docs/C_PHASE76_FUNCTIONAL_LOCAL_ONNX_EMBEDDINGS_2026-05-13.md
in the style of the C-PHASE 72-75 reports (Goal, Scope, Safety Boundaries,
Validation, Next Phase).

PR title: "C-PHASE 76: Functional local ONNX embeddings (bge-micro-v2)"

Auto-continue: NO. First real ML inference path; stop for user review.
```

## C-PHASE 77 — Activity Dashboard & Live Status UX

```text
Implement C-PHASE 77 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-77-activity-dashboard-and-live-status-ux

Goal: give the user a one-glance "what is Wevito doing right now?" view
without opening Settings.

Add:
- vnext/src/Wevito.VNext.Core/LiveStatusFeed.cs
- vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
- vnext/src/Wevito.VNext.Core/OverlayStatusSnapshot.cs
- vnext/src/Wevito.VNext.Shell/OverlayStatusBannerView.xaml(.cs)
- vnext/tests/Wevito.VNext.Tests/LiveStatusFeedTests.cs
- vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs
- docs/C_PHASE77_ACTIVITY_DASHBOARD_AND_LIVE_STATUS_UX_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Shell/RoamBandWindow.xaml(.cs)  (host the banner)
- vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml(.cs) (Stop-everything corner toggle)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs) (Activity panel rework)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs       (wire feed)
- vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs  (use explainer)

Banner rules:
- non-intrusive small text strip, auto-fades after 60s idle
- hidden in Quiet, PetOnly, and during fullscreen-other (use existing
  RuntimeSupervisorStatus.IsFullscreenSession or detect from foreground window)
- never raises focus (WS_EX_NOACTIVATE; verify in smoke probe)
- click opens Settings → Activity panel
- text format: "Last action: <kind> at <hh:mm> · today: <N> previews, <M> approvals, 0 mutations"

LiveStatusFeed rules:
- reads ledger every 10s (configurable: live_status_poll_seconds, default 10, min 5)
- returns counts + last packet kind/time, never message bodies
- emits zero memory if ledger empty
- KillSwitch blocks polling

PlainLanguageExplainer rules:
- parametric tests over every known packet kind
- unknown kind returns "Unknown {kind}" + a WarnUnknownPacketKind event (so we
  can extend coverage later)
- never includes private text (only counts, timestamps, file paths to artifacts)

Stop-everything toggle rules:
- single click → KillSwitchService.SetActive(true) + audit row
- second click only revoked from inside the Settings panel after confirmation
- visible badge on overlay when kill switch is active

Tests required:
- feed returns empty snapshot when ledger empty
- feed groups across multi-day with mocked clock
- explainer covers every existing packet kind
- explainer returns "Unknown {kind}" for an unrecognized kind
- banner hides in Quiet/PetOnly (style binding to RuntimeSupervisorStatus.Mode)
- toggle persists kill switch, downstream adapter regression tests still pass

Validation: baseline.

Phase report: docs/C_PHASE77_ACTIVITY_DASHBOARD_AND_LIVE_STATUS_UX_2026-05-13.md

PR title: "C-PHASE 77: Activity dashboard + live status overlay"

Auto-continue: YES if all tests + smoke pass and no focus-steal regression.
```

## C-PHASE 78 — Quiet-Mode Hardening & Always-On Soak Validation

```text
Implement C-PHASE 78 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-78-quiet-hardening-and-soak-validation

Add:
- vnext/src/Wevito.VNext.Core/WindowsForegroundFullscreenMonitor.cs
- vnext/src/Wevito.VNext.Core/WindowsPowerHandler.cs
- vnext/src/Wevito.VNext.Core/FocusStealCounter.cs
- vnext/src/Wevito.VNext.Core/SoakRunnerService.cs
- vnext/tests/Wevito.VNext.Tests/WindowsForegroundFullscreenMonitorTests.cs
- vnext/tests/Wevito.VNext.Tests/WindowsPowerHandlerTests.cs
- vnext/tests/Wevito.VNext.Tests/FocusStealCounterTests.cs
- vnext/tests/Wevito.VNext.Tests/SoakRunnerServiceTests.cs
- tools/run-soak-validation.ps1
- docs/C_PHASE78_QUIET_HARDENING_AND_SOAK_VALIDATION_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/RuntimeSupervisorService.cs (consume monitor + handler)
- vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs       (5-min flush + startup replay)
- vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs        (start monitor + handler at boot)
- vnext/src/Wevito.VNext.Shell/RoamBandWindow.xaml.cs     (focus counter wiring)
- vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs    (focus counter wiring)

Fullscreen monitor rules:
- poll foreground window flags every 2s (DwmGetWindowAttribute + GetWindowLong exstyle)
- transition to fullscreen=true after 5s sustained; false after 15s sustained
- emit "fullscreen_state_change" audit row per transition

Power handler rules:
- subscribe to Microsoft.Win32.SystemEvents.PowerModeChanged
- subscribe to SessionSwitch (lock/unlock)
- on sleep or lock: force supervisor to Quiet; halt all background work
- on resume/unlock: audit "power_resume"; do NOT auto-switch back to Active

FocusStealCounter rules:
- increments on WM_ACTIVATE while foreground is fullscreen-other
- value persists in %LOCALAPPDATA%/Wevito/audit/focus-steal.json
- read-only outside the counter; never reset by Wevito itself

SoakRunnerService rules:
- runs only when caller explicitly opts in (no settings flag flip)
- only schedules thin local_research previews
- emits soak_session_start and soak_session_end packets
- summary includes ledger row totals, budget meter snapshot, focus-steal count

run-soak-validation.ps1:
- argument: -Hours <int> (1-24), -ArtifactRoot <path>
- runs Wevito for the given hours, captures the ledger snapshot to artifact root
- script DOES NOT modify any settings; it merely keeps the process running and
  runs `dotnet run` or attaches to the launched broker

Tests required:
- monitor thresholds with mocked clock
- power sleep forces Quiet
- power resume does NOT auto-switch to Active
- budget meter survives a kill -9 simulation (file present)
- focus-steal counter increments only under the stated condition
- soak runner refuses to enable any default-off capability
- KillSwitch blocks the soak runner

Validation: baseline plus
  dotnet test --filter "Fullscreen|Power|FocusSteal|Soak"

Manual soak (later, user-run):
  powershell -File .\tools\run-soak-validation.ps1 -Hours 4 -ArtifactRoot .\vnext\artifacts\soak\

Phase report: docs/C_PHASE78_QUIET_HARDENING_AND_SOAK_VALIDATION_2026-05-13.md

PR title: "C-PHASE 78: Quiet-mode hardening + soak validation harness"

Auto-continue: NO. Requires the user to run the soak.
```

## C-PHASE 79 — Local Runtime Onboarding

```text
Implement C-PHASE 79 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-79-local-runtime-onboarding

Add:
- vnext/src/Wevito.VNext.Core/LocalRuntimeOnboardingService.cs
- vnext/src/Wevito.VNext.Core/OnnxPhiLocalModelAdapter.cs
- vnext/tests/Wevito.VNext.Tests/LocalRuntimeOnboardingServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/OnnxPhiLocalModelAdapterTests.cs
- tools/install-local-runtime.ps1
- docs/C_PHASE79_LOCAL_RUNTIME_ONBOARDING_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/ModelProviderMode.cs           (auto-route LocalOnly→Ollama→deterministic)
- vnext/src/Wevito.VNext.Core/OllamaLocalModelAdapter.cs     (expose readable status)
- vnext/src/Wevito.VNext.Core/LocalRuntimeProbeService.cs    (expose recent probe history)
- vnext/src/Wevito.VNext.Core/PetTaskAdapterPreviewDispatcher.cs (wire active adapter)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)     (Local AI panel)
- vnext/content/tool_definitions.json                        (local_ai_research entry)

LocalRuntimeOnboardingService rules:
- Status() returns Installed, ModelPresent, EndpointReachable, LastProbeAtUtc
- InstallPlan() returns the ordered safe steps the install script will take
- never runs external commands itself; only proposes them
- emits runtime_onboarding evidence packet per status check

install-local-runtime.ps1 rules:
- detect Ollama via:
  - winget probe: `winget list Ollama.Ollama --exact`
  - PATH probe: `Get-Command ollama -ErrorAction SilentlyContinue`
- if missing, print and require an interactive prompt before running
  `winget install Ollama.Ollama` (the prompt is built into winget's UX)
- after install, run `ollama pull llama3.2:3b` (or the model id from settings)
- never writes outside `tools/` and `%LOCALAPPDATA%/Wevito/runtime-install/`
- writes install-log-<ts>.txt with timestamps + commands + exit codes

OnnxPhiLocalModelAdapter rules (in-process fallback, feature-flagged):
- default-off: local_runtime_inproc_enabled=false
- when on AND Ollama unavailable AND ONNX Phi-3 weights present: used as fallback
- never selected by default
- degrades to deterministic LocalModelAdapter if weights missing
- localhost-only (no network); package: Microsoft.ML.OnnxRuntime.GenAI

ModelProviderMode auto-route:
- Disabled → no adapter
- LocalOnly with probe.IsAvailable → OllamaLocalModelAdapter
- LocalOnly without Ollama + InprocEnabled + OnnxPhi weights → OnnxPhiLocalModelAdapter
- LocalOnly otherwise → LocalModelAdapter (deterministic)
- ApprovedCloud → never reached in this phase

Tests required:
- LocalRuntimeOnboardingService never runs commands
- ModelProviderMode never routes LocalOnly to a hosted adapter
- OnnxPhi adapter degrades when weights missing
- Feature flag default-off respected
- KillSwitch blocks all paths
- Probe is dormant in Quiet/PetOnly (regression of C-PHASE 72)

Validation: baseline plus
  dotnet test --filter "Onboarding|OnnxPhi|Ollama|LocalRuntime|ModelProvider"

Phase report: docs/C_PHASE79_LOCAL_RUNTIME_ONBOARDING_2026-05-13.md

PR title: "C-PHASE 79: Local runtime onboarding + in-process fallback"

Auto-continue: NO.
```

## C-PHASE 80 — Hybrid Retrieval + Rerank Pipeline

```text
Implement C-PHASE 80 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-80-hybrid-retrieval-and-rerank

Add:
- vnext/src/Wevito.VNext.Core/LocalRetrievalService.cs
- vnext/src/Wevito.VNext.Core/LocalDocumentIngestService.cs
- vnext/src/Wevito.VNext.Core/RetrievalChunk.cs
- vnext/src/Wevito.VNext.Core/RetrievalResult.cs
- vnext/src/Wevito.VNext.Core/ReciprocalRankFusion.cs
- vnext/tests/Wevito.VNext.Tests/LocalRetrievalServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/LocalDocumentIngestServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/ReciprocalRankFusionTests.cs
- docs/C_PHASE80_HYBRID_RETRIEVAL_AND_RERANK_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
  (add pet_doc_chunks table with columns: chunk_id, doc_id, path, sha256,
   text, chunked_at_utc, embedding (BLOB or via sqlite-vec virtual table)
   + an FTS5 mirror for the text column)
- vnext/src/Wevito.VNext.Core/ResearchPlannerService.cs
  (accept retrieved chunks as evidence sources; preserve template
   synthesis if no LocalReasoningService is provided)
- vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
- vnext/src/Wevito.VNext.Core/CodeReviewPreviewAdapter.cs
- vnext/src/Wevito.VNext.Core/AssetInventoryPreviewAdapter.cs
- vnext/src/Wevito.VNext.Core/RerankHead.cs                 (Apply(List<chunk>, query) → reordered)
- vnext/content/tool_definitions.json                      (retrieval entry)

LocalRetrievalService rules:
- dense top-K (default 20) via sqlite-vec
- BM25 top-K (default 20) via FTS5
- RRF fusion (k=60 default; expose constant)
- rerank top-N (default 8) via RerankHead
- returns RetrievalResult { Chunks, Scores, MethodTrace, RetrievedAtUtc }
- KillSwitch blocks; returns empty + audit row

LocalDocumentIngestService rules:
- inputs: list of paths to index
- each path validated by UnifiedPolicyService.EvaluateRead
- chunk size 512 tokens default, 64 overlap; settings-driven
- writes chunks to pet_doc_chunks table
- emits local_doc_ingest evidence packet
- never reads paths outside the approved allowlist

Chunker rules:
- text-only chunks (no binary files)
- chunks include source path, byte offsets, sha256 of source file at ingest time
- re-ingesting a path with a new sha256 produces new chunks; old chunks are
  archived (not deleted) with a tombstone row
- never throw on UnreadableFile; emit audit row and skip

Tests required:
- RRF deterministic for the same input lists
- Hybrid retrieval > dense-only on a tiny fixture corpus (assert recall@3 delta)
- Chunker honors UnifiedPolicyService
- Chunker handles UTF-8 files with mixed line endings
- Rerank head deterministic
- KillSwitch blocks
- No network call

Validation: baseline plus
  dotnet test --filter "Retrieval|Chunk|Rerank|Fusion|DocIngest"

Phase report: docs/C_PHASE80_HYBRID_RETRIEVAL_AND_RERANK_2026-05-13.md

PR title: "C-PHASE 80: Hybrid retrieval + local rerank pipeline"

Auto-continue: NO. First retrieval surface.
```

## C-PHASE 81 — Local Reasoning Pipeline

```text
Implement C-PHASE 81 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-81-local-reasoning-pipeline

Add:
- vnext/src/Wevito.VNext.Core/LocalReasoningService.cs
- vnext/src/Wevito.VNext.Core/CitationEnforcer.cs
- vnext/src/Wevito.VNext.Core/LocalReasoningEvidencePacket.cs
- vnext/tests/Wevito.VNext.Tests/LocalReasoningServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/CitationEnforcerTests.cs
- docs/C_PHASE81_LOCAL_REASONING_PIPELINE_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/ResearchPlannerService.cs
  (when a LocalReasoningService is supplied, produce real claims with
   chunk-id provenance; otherwise keep the template path)
- vnext/src/Wevito.VNext.Core/LocalResearchPreviewAdapter.cs
- vnext/src/Wevito.VNext.Core/CodeReviewPreviewAdapter.cs
- vnext/src/Wevito.VNext.Core/ModelProviderMode.cs           (enforce LocalOnly at the service)
- vnext/src/Wevito.VNext.Core/PromptConfigStore.cs           (expose role-keyed templates)
- vnext/content/local-ai/prompt-config.json                  (default templates)

LocalReasoningService rules:
- inputs: question, retrieved RetrievalResult, helper role, tool family,
  trusted/untrusted context, requested-at-utc
- system message keyed by helper role: Scout, Inspector, Builder
- user message embeds chunks as "[1] <text>\n[2] <text>\n..."
- calls active IModelAdapter (Ollama when available; otherwise deterministic)
- response goes through CitationEnforcer
- writes local_reasoning evidence packet with:
  prompt_sha256, response_sha256, model_id, retrieved_chunk_ids,
  citation_coverage_ratio, did_use_local_model, did_use_network=false,
  did_use_hosted_ai=false, did_mutate=false
- refuses to call a hosted adapter EVEN IF supplied
- KillSwitch blocks

CitationEnforcer rules:
- valid citations are bracket-numbered references [1], [2], ... that match
  chunk ids in the retrieval result
- sentences without any valid [N] are replaced with "(needs citation)"
- citation_coverage_ratio = #sentences-with-valid-citation / #total-sentences
- the enforcer never edits text inside citations
- never throws

Untrusted-context handling:
- WRAP untrusted context with the existing PetModelSummaryService.WrapUntrusted
- system message reminds the model that untrusted context is not authoritative

Tests required:
- service uses deterministic adapter when local model unavailable
- service refuses to call a hosted adapter even when one is injected
- enforcer strips sentences without [N]
- enforcer keeps sentences with valid [N] referencing real chunk ids
- coverage_ratio computed correctly
- empty retrieval → deterministic "no local evidence" synthesis, ratio=0
- KillSwitch blocks
- no network call (assert via fake HttpClient throwing on send)

Validation: baseline plus
  dotnet test --filter "LocalReasoning|Citation"

Phase report: docs/C_PHASE81_LOCAL_REASONING_PIPELINE_2026-05-13.md

PR title: "C-PHASE 81: Local reasoning pipeline (RAG + citation enforcement)"

Auto-continue: NO. First real-AI-on-real-content surface.
```

## C-PHASE 82 — Golden Eval Dataset + Hard Regression Gate

```text
Implement C-PHASE 82 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-82-golden-eval-and-regression-gate

Add:
- vnext/src/Wevito.VNext.Core/EvalRegressionGate.cs
- vnext/content/local-ai/eval-golden/questions.json             (50-100 rows)
- vnext/content/local-ai/eval-golden/documents/example-001.md   (and more, ~20 docs)
- vnext/content/local-ai/eval-golden/baseline.json
- tools/run-golden-eval.ps1
- .github/workflows/local-eval.yml
- vnext/tests/Wevito.VNext.Tests/EvalRegressionGateTests.cs
- vnext/tests/Wevito.VNext.Tests/GoldenEvalSmokeTests.cs
- docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/LearningEvalService.cs    (add RunGoldenEval method)
- vnext/src/Wevito.VNext.Core/LocalTuningRunner.cs      (gate apply with EvalRegressionGate)
- vnext/content/tool_definitions.json                  (eval entry)

Dataset rules:
- 100% synthetic, no external content
- 50-100 rows: question + expected source chunk ids + rubric note
- 15-25 documents under documents/ (markdown only)
- every question references real chunk ids from the corpus
- documents are written specifically for this eval; no licensing risk

EvalRegressionGate rules:
- baseline.json contains: recall@1, recall@3, mrr, citation_coverage_ratio,
  latency_p50, latency_p95, captured_at_utc, dataset_sha256
- gate computes current metrics, compares vs baseline
- regression triggers (any of):
  - recall@1 drop > 2%
  - mrr drop > 2%
  - citation_coverage_ratio drop > 5%
  - citation_coverage_ratio below absolute floor 0.6
- gate emits golden_eval evidence packet (PacketKind = "golden_eval")
- baseline can ONLY be overwritten with --accept-new-baseline flag

tools/run-golden-eval.ps1:
- argument: -ArtifactRoot <path>, optional -UpdateBaseline
- runs RunGoldenEval against the current code + dataset
- prints the comparison table + delta vs baseline
- writes the evidence packet
- exit code 1 on regression, 0 on pass (so CI can fail builds)

.github/workflows/local-eval.yml:
- triggers on PRs touching vnext/content/local-ai/ or vnext/src/Wevito.VNext.Core/*.cs
- runs: dotnet build + dotnet test + tools/run-golden-eval.ps1
- DOES NOT mark itself as a required check in this phase
  (leave required-check enablement to a separate manual step)

LocalTuningRunner integration:
- before any tuning apply, the gate is invoked with the candidate config
- if regression: auto-rollback (existing behavior) + audit row
- adds a new packet kind "golden_eval" to AuditLedgerService constants

Tests required:
- gate flags a regression with a worse retrieval head (use a deliberately
  bad rerank head fixture)
- gate passes at the baseline
- gate fails closed when dataset_sha256 doesn't match the committed dataset
- baseline overwrite refused without --accept-new-baseline
- tuning apply rolls back on regression
- KillSwitch blocks the gate

Validation: baseline plus
  dotnet test --filter "EvalRegression|GoldenEval|LearningEval|Tuning"
  powershell -File .\tools\run-golden-eval.ps1 -ArtifactRoot .\vnext\artifacts\eval-golden\

Phase report: docs/C_PHASE82_GOLDEN_EVAL_AND_REGRESSION_GATE_2026-05-13.md

PR title: "C-PHASE 82: Golden eval dataset + hard regression gate"

Auto-continue: NO. First PR-level gate.
```

## C-PHASE 83 — Self-Improvement Daily/Weekly Report Loop

```text
Implement C-PHASE 83 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-83-self-improvement-report-loop

Add:
- vnext/src/Wevito.VNext.Core/SelfImprovementReportService.cs
- vnext/src/Wevito.VNext.Core/RollbackProposalService.cs
- vnext/tests/Wevito.VNext.Tests/SelfImprovementReportServiceTests.cs
- vnext/tests/Wevito.VNext.Tests/RollbackProposalServiceTests.cs
- tools/run-self-improvement-report.ps1
- docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs   (optionally emit a report per iteration)
- vnext/src/Wevito.VNext.Core/ActivitySummaryService.cs     (expose Self-improvement tab data)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)    (new Self-improvement tab)
- vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml(.cs)    (badge if rollback proposal exists)

SelfImprovementReportService rules:
- inputs: AuditLedgerService snapshot range
- output: markdown report + JSON manifest under
  vnext/artifacts/pet-tasks/<ts>-self-improvement-report/
- the report groups by packet kind, flags `did_use_*` rows, links new dataset
  versions and tuning applies with their eval deltas
- text-only (no JSON dumps in the markdown body)
- KillSwitch blocks

RollbackProposalService rules:
- inputs: last tuning apply event + a subsequent eval result
- if eval regressed within 24h after a tuning apply: emit a `rollback_proposal`
  packet AND a Draft TaskCard with ToolFamily=guardedMutation pointing to the
  rollback step
- never auto-executes rollback (existing LocalTuningRunner rollback path is
  for the same-run case; this is for cross-run regressions)
- KillSwitch blocks

run-self-improvement-report.ps1:
- argument: -Window day|week, -ArtifactRoot <path>
- runs the report against the ledger and writes both report.md and report.json

Tests required:
- report contains zero flagged rows when offline mode is held for the window
- report aggregates kinds across multi-day correctly (mocked clock)
- rollback proposal only emitted on apply→regression within 24h
- rollback proposal never auto-executes
- text-only output (no JSON inside markdown)
- KillSwitch blocks both services

Validation: baseline plus
  dotnet test --filter "SelfImprovement|RollbackProposal"

Phase report: docs/C_PHASE83_SELF_IMPROVEMENT_REPORT_LOOP_2026-05-13.md

PR title: "C-PHASE 83: Self-improvement daily/weekly report + rollback proposals"

Auto-continue: YES.
```

## C-PHASE 84 — Guarded Code Mutation Pilot

```text
Implement C-PHASE 84 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-84-guarded-code-mutation-pilot

Add:
- vnext/tests/Wevito.VNext.Tests/GuardedMutationPilotTests.cs
- docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/GuardedMutationPreviewAdapter.cs
  (expose a Propose method that accepts a small known edit fixture and emits
   a dry-run packet)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
  (small "Propose mutation" mini-form for power users; default-disabled)
- vnext/content/tool_definitions.json
  (guardedMutation entry exposes a 'pilotEnabled' flag, default-off)

Pilot scenario:
- pick a small, non-load-bearing edit (Codex picks; examples below):
  - add a deprecation XML comment on a private helper that is not called
  - update a JSON comment-style metadata string in tool_definitions.json
  - bump a non-critical default value in an existing test fixture
- Codex must NOT pick anything in:
  - vnext/src/Wevito.VNext.Core/ runtime services
  - vnext/src/Wevito.VNext.Shell/ shell wiring
  - the policy / kill switch / audit ledger
- Codex must propose the change as a GuardedMutationPlan + Draft TaskCard
- User approves the card; then Codex (or operator) runs:
  - DryRun (writes the dry-run manifest)
  - Apply (writes backups + writes + post-proof)
  - if post-proof fails, automatic rollback (existing behavior)

Test additions:
- run an end-to-end synthetic mutation on a temp file fixture (not the real
  repo) verifying: dry-run produces correct diff, apply succeeds, post-proof
  passes, sha256 round-trips; then a deliberately failing post-proof case
  triggers byte-exact rollback
- regression: all 27 existing mutation tests + sprite tests stay green
- scope-expand attempt is refused
- KillSwitch blocks apply mid-run

Validation: baseline plus
  dotnet test --filter "GuardedMutation|MutationVerifier|SpriteWorkflow"
  powershell -File .\tools\build-vnext.ps1 -SkipAssetPrep -SkipTests

Phase report: docs/C_PHASE84_GUARDED_CODE_MUTATION_PILOT_2026-05-13.md
(include the chosen pilot scenario, the dry-run diff, the apply manifest,
and the rollback test result)

PR title: "C-PHASE 84: Guarded code mutation pilot (first reviewed apply)"

Auto-continue: NO. First reviewed code apply outside sprites.
```

## C-PHASE 85 — Autonomous Operations Promotion + 7-Day Sign-Off

```text
Implement C-PHASE 85 per docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md §3.

Branch: claude-implementation/c-phase-85-autonomous-operations-promotion

Pre-reqs (must be true before this phase opens):
- C-PHASE 76-84 merged
- A 7-day soak window has been run via tools/run-soak-validation.ps1
- AuditLedgerService contains >= 7 days of rows showing zero violations
- Golden eval passes at the latest baseline

Add:
- vnext/src/Wevito.VNext.Core/PromotionCriteriaSnapshot.cs
- vnext/tests/Wevito.VNext.Tests/PromotionCriteriaSnapshotTests.cs
- tools/run-promotion-eval.ps1
- docs/C_PHASE85_AUTONOMOUS_PROMOTION_CHECKLIST_2026-05-13.md
- docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md

Modify (minor):
- vnext/src/Wevito.VNext.Core/AutonomousBetaDecisionService.cs
  (consume PromotionCriteriaSnapshot to produce the final decision)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
  (Settings → Autonomous beta panel:
     show the snapshot table + a single "Try the autonomous beta" entry
     that requires confirmation before enable)

PromotionCriteriaSnapshot rules:
- inputs: ledger snapshot range (default 7 days), golden eval result,
  budget meter snapshot, focus-steal counter snapshot
- output: structured pass/fail table:
  - active_uptime_per_day >= 50% of waking hours
  - policy_violations == 0
  - mutations_with_proof_packets == 100%
  - hosted_ai_calls_in_local_only == 0
  - focus_steal_events == 0
  - resource_budget_within_pct == within +/- 10%
  - citation_coverage_ratio >= 0.6
- if all pass: decision = enable_autonomous_beta
- else: keep_supervised_preview or pause_for_reliability_work depending on
  which criterion failed (mirror C-PHASE 75 contract)

tools/run-promotion-eval.ps1:
- argument: -ArtifactRoot <path>
- runs the snapshot + golden eval + writes the final decision packet
- exit code 1 if decision != enable_autonomous_beta

Final flip:
- The PR DOES NOT enable runtime_autonomous_beta_enabled by default.
- It only updates the Settings UI to expose a "Try the autonomous beta"
  entry that the user can click. Click triggers a confirmation dialog
  and an audit row, then sets the setting.

Tests required:
- snapshot refuses to compute without 7+ days of ledger
- snapshot fails closed (returns keep_supervised_preview) on any criterion fail
- KillSwitch active during the window blocks enable_autonomous_beta
- Settings UI flip is confirmation-gated

Validation: baseline plus
  dotnet test --filter "PromotionCriteria|AutonomousBeta|AutonomousOperations"
  powershell -File .\tools\run-promotion-eval.ps1 -ArtifactRoot .\vnext\artifacts\promotion\

Phase report: docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md

PR title: "C-PHASE 85: Autonomous operations promotion + 7-day sign-off"

Auto-continue: NO. Final user decision.
```

## Phase Sequencing Checklist

```text
phase    auto-continue?    notes
76       no                first real ML inference path
77       yes               UI work, no new capability surfaces
78       no                requires user soak run
79       no                local runtime onboarding UX + in-process fallback (flag-gated)
80       no                first retrieval surface
81       no                first real-AI reasoning surface
82       no                first PR-level gate
83       yes               reporting, no new mutation surface
84       no                first reviewed code apply
85       no                final promotion decision
```

## Final Single Copy-Paste Prompt to Begin C-PHASE 76

Paste this into Codex (medium effort) to start the next phase. Everything
needed is in this prompt; the supporting docs are referenced. This will
produce one PR for C-PHASE 76 and stop for user review.

```text
You are Codex at medium reasoning effort, working in the Wevito repo at
C:\Users\fishe\Documents\projects\wevito (or the current worktree).

Read first:
  docs/CLAUDE_POST_CPHASE75_REVIEW_AND_ROADMAP_2026-05-13.md
  docs/CLAUDE_POST_CPHASE75_CODEX_PHASE_PROMPTS_2026-05-13.md
  docs/CLAUDE_LOCAL_FIRST_AI_FULL_IMPLEMENTATION_PLAN_2026-05-12.md
  docs/C_PHASE72_LOCAL_MODEL_RUNTIME_INTEGRATION_2026-05-12.md
  docs/C_PHASE75_AUTONOMOUS_OPERATIONS_BETA_2026-05-12.md
  vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs
  vnext/src/Wevito.VNext.Core/PetMemoryStore.cs

Implement C-PHASE 76: Functional Local ONNX Embeddings.

Branch: claude-implementation/c-phase-76-functional-local-onnx-embeddings

Goal:
Replace the placeholder ONNX embedding path in OnnxTextEmbeddingService
with real bge-micro-v2 inference. Two identical inputs must produce
identical L2-normalized 384-dim vectors. Two different inputs must produce
distinct vectors. A missing model or tokenizer must cause exactly one
"embedding-degrade | <reason>" audit line and fall back to
HashingTextEmbeddingService deterministically. No throw escapes Embed.

Add (new files):
- vnext/src/Wevito.VNext.Core/OnnxEmbeddingBackend.cs
- vnext/tests/Wevito.VNext.Tests/OnnxEmbeddingBackendTests.cs
- vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/README.md
- vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/model.onnx     (<=100KB tiny fixture)
- vnext/tests/Wevito.VNext.Tests/fakes/test-embedder/tokenizer.json
- docs/C_PHASE76_FUNCTIONAL_LOCAL_ONNX_EMBEDDINGS_2026-05-13.md

Modify:
- vnext/src/Wevito.VNext.Core/OnnxTextEmbeddingService.cs
  (real EmbedWithOnnxFallback; preserve cache + audit-once degrade + safe fallback)
- vnext/src/Wevito.VNext.Core/Wevito.VNext.Core.csproj
  (add Microsoft.SemanticKernel.Connectors.Onnx OR FastBertTokenizer; pick one
   and document the choice in the phase report)
- vnext/src/Wevito.VNext.Core/PetMemoryStore.cs
  (verify legacy DB rename + rebuild path still works on dimension change; only
   touch if needed; add a focused regression test)
- tools/install-local-embedder.ps1
  (also fetch tokenizer.json + vocab.txt; emit sha256 manifest)
- vnext/content/local-models/embeddings/bge-micro-v2/README.md
  (model + tokenizer install steps + sha256s)
- vnext/content/tool_definitions.json
  (pet_memory entry: embeddingProviderWhenOnnxActive=bge-micro-v2)

Hard rules:
- L2-normalize the returned vector
- ONNX inference only when IsOnnxReady (model file + tokenizer file present + load successful)
- exactly one "embedding-degrade | <reason>" audit line per service instance on degrade
- no network call in any test (assert via fake HttpClient that throws on send)
- never throw out of Embed(text)
- test fixtures use the tiny check-in under fakes/test-embedder/; never download real weights in tests
- the real bge-micro-v2 .onnx file is NOT committed in this phase (only the loader + tokenizer + README + installer)
- HashingTextEmbeddingService remains the deterministic fallback and tests of it must stay green

Tests required:
- two identical inputs produce identical vectors (cache + L2 deterministic)
- two different inputs produce cosine != 1.0
- missing tokenizer.json -> single audit line + hashing fallback
- missing model.onnx -> single audit line + hashing fallback
- corrupt session via injected _sessionFactory -> fallback
- Dimensions returns 384 when ONNX ready, 16 when degraded
- PetMemoryStore migrates <pet-id>.db -> <pet-id>.legacy-<ts>.db when dimension flips

Validation (run all):
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Embedding|PetMemory"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

Phase report:
Write docs/C_PHASE76_FUNCTIONAL_LOCAL_ONNX_EMBEDDINGS_2026-05-13.md in the
style of the C-PHASE 72-75 reports. Include:
- Goal
- Scope (what was changed)
- Safety Boundaries (degrade path + no-network + audit-once)
- Validation (test counts + commands run)
- Next Phase (C-PHASE 77; auto-continue is NO because this is the first real ML path)

Commit / PR:
- Branch: claude-implementation/c-phase-76-functional-local-onnx-embeddings
- Commit messages reference C-PHASE 76
- PR title: "C-PHASE 76: Functional local ONNX embeddings (bge-micro-v2)"
- Auto-continue: NO. Stop for user review after the PR is open and tests pass.

Stop gates (do not merge if any are violated):
- a test path opens a network connection
- a corrupt or missing tokenizer throws
- hashing fallback path stops working
- memory DB upgrade silently re-embeds old rows (it must rebuild fresh)
- embedding output is not L2-normalized
- audit line is emitted more than once per service instance during degrade
```
