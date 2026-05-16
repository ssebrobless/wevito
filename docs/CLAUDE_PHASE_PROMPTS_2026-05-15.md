# Wevito Codex Phase Prompts — Copy-Paste Ready

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (consumed one prompt per phase by
`tools/run-codex-loop.ps1`)

## 0. How to Use This File

Each phase below is a copy-paste-ready Codex CLI prompt. The loop runner
extracts the right section by phase ID and passes it to Codex. Each prompt
references:

- `DECISION_LEDGER_2026-05-15.md` for decision IDs (`P5`, `FR4`, etc.)
- The phase plan doc (`CLAUDE_<scope>_PHASES_<range>_PLAN_2026-05-15.md`)
  for the full scope/files/tests/validation/stop-gates spec
- The master plan (`CLAUDE_MASTER_PLAN_2026-05-15.md`) for execution
  context

Prompts are intentionally compact — the full spec lives in the plan
docs; this file is the *invocation* surface.

## 0.1 Universal preamble (every phase begins with this)

```text
You are Codex CLI at medium reasoning effort, working on the wevito
project at C:\Users\fishe\Documents\projects\wevito\.

Before writing any code, read these three files:
1. docs/DECISION_LEDGER_2026-05-15.md — every decision ID referenced below
2. docs/CLAUDE_MASTER_PLAN_2026-05-15.md — execution context + invariants
3. The phase plan doc named in the specific prompt below

Follow these rules for every phase:
- Run all named validation commands before opening the PR
- Verify every named stop gate is FALSE before opening the PR
- If any stop gate is TRUE: open the PR in Draft, write a phase_blocked audit
  row, and halt
- Reference the decision IDs in your commit message
- Add new packet kinds to PlainLanguageExplainer.KnownPacketKinds
- Set evidence-packet did_use_* flags honestly
- Consult KillSwitchService at top of every public method that writes state
- Mirror the named Pattern service's DI shape + test names

When done: open PR with the title from the phase plan, write the phase
report doc, and let the loop runner decide whether to auto-merge based
on Auto-continue setting + test results + stop gate status.
```

---

## C-PHASE 85d — Autonomous Operations Promotion (Residual)

```text
[Phase 85d]

Read docs/CLAUDE_POST_CPHASE85_BLOCKER_PLAN_2026-05-15.md section 4.4
(Final Promotion PR with real evidence).

Preconditions:
- A 7-day soak window must have completed via tools/run-soak-driver.ps1
- tools/run-promotion-eval.ps1 must return decision=enable_autonomous_beta
  with all criteria PASS

If preconditions aren't met: HALT and write phase_blocked row "soak_or_eval_incomplete".

If preconditions met:
- Snapshot vnext/artifacts/promotion/<latest>/snapshot.json
  + decision.json + run-summary.md
- Snapshot vnext/artifacts/soak/<latest>/manifest.json
- Update docs/C_PHASE85_AUTONOMOUS_OPERATIONS_PROMOTION_2026-05-13.md
  with the decision packet contents + criteria table
- DO NOT change runtime_autonomous_beta_enabled setting (user clicks the
  Settings entry shipped in C-PHASE 85c to flip it)

PR title: "C-PHASE 85d: Autonomous operations promotion (with real evidence)"
Auto-continue? No
```

---

## C-PHASE 106 — Default-Enabled Reasoning LLM

```text
[Phase 106]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 106 — Default-Enabled Reasoning LLM".

Implements decision Q15-L2 (Qwen 2.5 7B Q4_K_M as default).

Scope summary:
- Flip ModelProviderMode default to LocalOnly in DefaultStateFactory.cs
- Add OllamaModelBootstrapService (probe + Stop card if model missing)
- Add tools/install-ollama.ps1 + tools/pull-default-model.ps1
- Add Settings UI status row in ToolPopupWindow

Pattern: LocalRuntimeOnboardingService.cs

Run validation:
  dotnet test --filter "OllamaModelBootstrap|DefaultStateFactoryReasoningModel|PlainLanguage"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test --no-build
  .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests

Verify all stop gates from the plan doc are FALSE.

Branch: claude-implementation/c-phase-106-default-enabled-reasoning-llm
Auto-continue? No
```

---

## C-PHASE 107 — Chat UI

```text
[Phase 107]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 107 — Chat UI".

Implements decisions Q15-Layout-2 + Q15-Streaming + Q15-Tool-Use +
Q15-History + Q15-Interrupt + Q15-Search + Q15-Title.

Scope summary:
- Add ChatHistoryStore (sqlite + FTS5 + append-only triggers)
- Add ChatSessionService + ChatStreamingService (SSE) + ChatTitleService
- Add ChatViewModel + ChatPanel.xaml replacing pet-task report area
- Tool-use mid-chat via streaming bridge (defers actual tool registry
  to C-PHASE 109; for now mock tool calls with placeholder dispatcher)
- Esc + Stop button cancel
- FTS5 search input
- Auto-title via short Ollama completion

Pattern: CreativeLearningLabWindow + OllamaLocalModelAdapter (extend
streaming).

Validation: standard + manual smoke (open wevito, type a message, see
streamed response).

Branch: claude-implementation/c-phase-107-chat-ui
Auto-continue? No
```

---

## C-PHASE 108 — Agent-Slot Redesign

```text
[Phase 108]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 108 — Agent-Slot Redesign".

Implements Q16-A4 + CM3 + NM3 + RS3 + DA1+DA3 + VS3.

Scope summary:
- DELETE PetHelperRole enum
- DELETE Scout/Inspector/Builder hardcoded helper GUIDs in PetCommandBarService.cs
- Add AgentSlot, AgentSlotService, AgentSlotDispatcher, AgentToolConcurrencyCoordinator
- Refactor PetTaskAdapterPreviewDispatcher to consume AgentSlot
- VS3 UI: new Agents tab in ToolPopupWindow + 3 icons in HomePanelWindow

Pattern: RuntimeSupervisorService.cs + PetTaskCardQueueService.cs

Validation: standard. Verify zero references to PetHelperRole/HelperPet
remain. Verify pet name renames propagate to agent names live.

Branch: claude-implementation/c-phase-108-agent-slot-redesign
Auto-continue? No
```

---

## C-PHASE 109 — Tool Registry First-Class

```text
[Phase 109]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 109 — Tool Registry First-Class".

Scope summary:
- Add ToolRegistry + ToolDescriptor (descriptor per existing preview adapter)
- Add ToolInvocationService (consults UnifiedPolicyService first; never bypasses kill switch)
- Add OllamaToolFormatAdapter (translates registry → OpenAI functions format)
- Add ToolInvocationStreamingBridge (pauses streaming on tool_call_start)
- Add Tools tab in ToolPopupWindow with per-tool enable/disable toggles

Pattern: PetTaskAdapterPreviewDispatcher.cs + OllamaLocalModelAdapter.BuildPayload

Validation: standard + manual smoke (ask wevito "what's in my docs folder"
in chat; verify localDocs tool fires).

Branch: claude-implementation/c-phase-109-tool-registry-first-class
Auto-continue? No
```

---

## C-PHASE 110 — Pet/AI Isolation Contract

```text
[Phase 110]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 110 — Pet/AI Isolation Contract".

Implements Q18 (F3 + P4 + R4 + G3+G4 + C4 + I3).

Scope summary:
- Add PetFpsMonitorService (reads Godot fps via IPC, hourly snapshot, violation packet on < 30 fps)
- Add RamPressureCascadeService (tiered: 3 GB → suspend experiments, 2 GB → unload image-gen, 1.5 GB → unload LLM, 1 GB → emergency stop)
- Add WevitoProcessWatchdogService in Broker (restart crashed children, max 3 in 5 min, then Stop card)
- Add ImageGenIdleGuardService (block background image-gen when pet not idle)
- Add UserInteractingWithPet transient state to RuntimeSupervisorService (5s after pet input)
- Godot IPC: pet.gd reports fps via PetFpsSample; main_scene.gd reports input events

Pattern: WindowsForegroundFullscreenMonitor + RuntimeBudgetMeter.

Validation: standard + PetFpsSoakTest (10-min sustained AI load; pet fps stays >= 30).

Branch: claude-implementation/c-phase-110-pet-ai-isolation-contract
Auto-continue? No
```

---

## C-PHASE 111 — User-PC Coexistence Policy

```text
[Phase 111]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 111 — User-PC Coexistence Policy".

Implements Q19 (T5 + D5 + L3 + B4 + R4 + V3).

Pre-seed app list with: LeagueClient.exe, LeagueofLegends.exe,
RiotClientServices.exe, zoom.exe, Teams.exe, obs64.exe, obs32.exe,
Discord.exe.

Scope summary:
- Add CoexistenceTriggerService (fullscreen + app list + CPU + network)
- Add DoNotDisturbScheduleService (configurable schedule + quick-toggle)
- Add WorkloadTierService (Foreground / Maintenance / Experimentation; tier-aware reservation)
- Extend RuntimeBudgetMeter for tier reservations
- V3: send IPC to Godot to play `sleep` animation when wevito is yielding

Pattern: WindowsForegroundFullscreenMonitor + WindowsPowerHandler.

Validation: standard + manual smoke (open zoom.exe → verify within 30s
wevito enters Quiet + pet plays sleep).

Branch: claude-implementation/c-phase-111-user-pc-coexistence-policy
Auto-continue? Yes
```

---

## C-PHASE 101 — Always-On Household Maintenance

```text
[Phase 101]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 101 — Always-On Household Maintenance".

Scope summary:
- Add HouseholdMaintenanceService + IHouseholdChore interface
- Register 5 day-1 chores: DailySelfImprovementReportChore,
  LedgerIntegrityCheckChore, EmbeddingRefreshChore, GoldenEvalRefreshChore,
  EvidenceSummaryChore
- Wire into ShellCoordinator.Tick (max 1 chore per tick to avoid burst)

Pattern: DailyEvidenceSnapshotService.cs

Validation: standard.

Branch: claude-implementation/c-phase-101-household-maintenance
Auto-continue? Yes
```

---

## C-PHASE 102 — User Feedback Ingestion

```text
[Phase 102]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 102 — User Feedback Ingestion".

Scope summary:
- Add UserFeedbackStore (sqlite append-only, mirrors AuditLedgerService triggers)
- Add IUserFeedbackSurface for UI consumers
- Add UserFeedbackSummaryReader for 7-day aggregates
- Wire 👍/👎 buttons into ToolPopupWindow task cards + CreativeLearningLabWindow review rows + HomePanelWindow toasts

Pattern: AuditLedgerService + LearningLabLabelStore.

Validation: standard.

Branch: claude-implementation/c-phase-102-user-feedback-ingestion
Auto-continue? Yes
```

---

## C-PHASE 112 — Benchmark Suite v1

```text
[Phase 112]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 112 — Benchmark Suite v1".

Implements Q17-V1-Suite + CA6 + GI1+GI4-reinterpreted + FR4 + UI5 + V1-Immutable.

Scope summary:
- Add BenchmarkSuiteService + IBenchmarkKind + 5 kinds (Chat / ToolUse /
  Retrieval / Safety / Perf)
- Add GraderTriad (Deterministic primary; LLM advisory; HeldOut placeholder)
- Add BenchmarkRegressionGate (FR4 tiered)
- Wire CA6 cadence (per-phase safety+perf via Codex loop; daily capability
  via household chore; on-demand button)
- Add Benchmarks tab + ambient HomePanel badge

Pattern: LearningEvalService.cs + SelfImprovementReportService.cs

Validation: standard.

Branch: claude-implementation/c-phase-112-benchmark-suite-v1
Auto-continue? No
```

---

## C-PHASE 113 — Benchmark Curation UI

```text
[Phase 113]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 113 — Benchmark Case Curation UI".

Implements Q17-Option-C (Codex drafts + user approves + bookmark-from-chat).

Scope summary:
- Add BenchmarkCaseDraftService (generates drafts from chat history)
- Add BenchmarkCaseCurationStore (append-only review state)
- Add BenchmarkCurationViewModel + UI sub-tab in Benchmarks tab
- Add 🔖 button on assistant messages in ChatPanel (per C-PHASE 107)
- Add adversarial-case form (skips draft queue; adversarial cases must come from human hostile thinking)

Pattern: LearningLabLabelStore + CreativeLearningLabWindow.

Validation: standard + manual smoke (bookmark a chat response → draft case appears).

Branch: claude-implementation/c-phase-113-benchmark-case-curation-ui
Auto-continue? Yes
```

---

## C-PHASE 116 — Chat-Context-Window Management

```text
[Phase 116]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 116 — Chat-Context-Window Management".

Implements Q22 (C-5 + A-1 + S-3 + R-4 + P-3 + T-4 + F-4).

Scope summary:
- Add ChatContextBudgetService (static 4K+70K+30K+16K+8K = 128K for Qwen 2.5 7B)
- Add RollingSummarizerService (compress oldest 20K → 3K summary at 80% pressure)
- Add RetrievalAutomaticInjector (top-3 per turn) + retrieve_from_memory tool
- Add PinnedContextStore (4K budget, FIFO eviction)
- Add ToolResultBudgetService (4K cap + truncation marker + full result on disk)
- Add ChatColdStorageService (6-month archival to cold sqlite)

Pattern: SelfImprovementReportService + LocalRetrievalService + UserFeedbackStore.

Validation: standard.

Branch: claude-implementation/c-phase-116-chat-context-window-management
Auto-continue? Yes
```

---

## C-PHASE 115 — Identity Rename + UX Language Pass

```text
[Phase 115]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 115 — Identity Rename + UX Language Pass".

Implements Q21 (N4 + N-3 + U-2 + D-4 + O-2).

Scope summary:
- Mechanical rename: PetCommandBarService → ChatInputBarService, PetTaskCard → AgentTaskCard, etc.
- KEEP AS-IS: PetActor, PetMemoryStore, PetSimulationEngine, PetWellbeingInterpreter, PetStatePreviewAdapter, PetModelSummaryService, PetDebugTruthReportBuilder
- Contracts namespace (Wevito.VNext.Contracts.PetAgentContracts) unchanged
- Add AiIdentityService (settings-backed name, default "Wevito")
- Add FirstLaunchWizardWindow (4 steps: name AI + name slots + initial scope + first greeting)
- ToolPopupWindow top tab strip per U-2: Chat | Activity | Agents | Tools | Benchmarks | Creative Lab | Settings
- Rewrite README.md + new WHAT_IS_WEVITO.md + new docs/INDEX.md
- DO NOT modify historical C_PHASE0..C_PHASE85c docs

Validation: standard + sweep test (zero references to renamed types).

Branch: claude-implementation/c-phase-115-identity-rename-ux-language-pass
Auto-continue? Yes
```

---

## C-PHASE 121 — Process Priority & Resource Throttling

```text
[Phase 121]

Read docs/CLAUDE_NON_INTERRUPTION_AND_PET_SIM_PHASES_121_THROUGH_124_2026-05-15.md
section "C-PHASE 121 — Process Priority & Resource Throttling".

Scope summary:
- Add ProcessPriorityManagerService (BelowNormal default; boost to Normal on foreground 5s decay)
- Add DiskIoBudgetService (20 MB/s cap when user active)
- Add CodexCompileThrottleService (MSBUILDPROCESSORCOUNT=2 when user active, 6 when idle)
- Add GameModeDetectorService (Windows Game Mode → Quiet)
- Extend RuntimeBudgetMeter with user-protection floors

Pattern: WindowsPowerHandler + RuntimeBudgetMeter.

Validation: standard.

Branch: claude-implementation/c-phase-121-process-priority-resource-throttling
Auto-continue? No
```

---

## C-PHASE 122 — Notification, Focus, Audio & Workspace Discipline

```text
[Phase 122]

Read docs/CLAUDE_NON_INTERRUPTION_AND_PET_SIM_PHASES_121_THROUGH_124_2026-05-15.md
section "C-PHASE 122 — Notification, Focus, Audio & Workspace Discipline".

Scope summary:
- Add NotificationPolicyService (defer during user activity)
- Add FocusDisciplineService (ShowActivated=false default; no focus theft)
- Add AudioOutputPolicyService (silent by default; pet sounds opt-in; TTS banned in v1)
- Add MultiMonitorService (pet stays on preferred monitor)
- Add CursorReactivityService (pet head-turn within 200px proximity; 1/10s rate limit)
- Add TrayIconDisciplineService (no flash/badge unless user-explicit)

Pattern: FocusStealCounter + LiveStatusFeed.

Validation: standard.

Branch: claude-implementation/c-phase-122-notification-focus-audio-workspace-discipline
Auto-continue? Yes
```

---

## C-PHASE 99 — Multi-Domain Palette Grammar Registry

```text
[Phase 99]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 99 — Multi-Domain Palette Grammar Registry".

Implements decision P5 + S-11b.

Scope summary:
- Add PaletteGrammar, PaletteSlotSemantic, PaletteVariant, PaletteConformanceRules records
- Add PaletteGrammarRegistry (load all from vnext/content/palette-grammars/)
- Add PaletteConformer (nearest-neighbor remap + alpha threshold)
- Seed 37 grammars: 30 sprite (10 species × 3 ages with natural canonical), 1 portrait, 4 environment, 1 item, 1 icon

Pattern: LearningLabBundleService + LocalDocumentIngestService.

Validation: standard + verify all 37 seed grammars parse.

Branch: claude-implementation/c-phase-99-palette-grammar-registry
Auto-continue? No
```

---

## C-PHASE 100 — General Fallback Grammar

```text
[Phase 100]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 100 — General Fallback Grammar".

Scope summary:
- Add vnext/content/palette-grammars/general-pixel-art.json with 8-slot generic palette
- Add GrammarRouterService (hint > keyword > fallback)
- Add GeneralPixelArtPolicyService (denylist for prohibited subjects)
- Extend PaletteConformer to handle general grammar constraints (256x256 max, anti-alias disallowed)

Pattern: WebQueryPrivacyFilter.

Validation: standard.

Branch: claude-implementation/c-phase-100-general-fallback-grammar
Auto-continue? Yes
```

---

## C-PHASE 98 — Local Image Generation Runtime

```text
[Phase 98]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 98 — Local Image Generation Runtime".

Scope summary:
- Add LocalImageRuntimeProbeService (mirrors LocalRuntimeProbeService)
- Add IImageModelAdapter + ImageModelRequest + ImageModelResponse
- Add Automatic1111ImageAdapter + ComfyUIImageAdapter + DeterministicImageFallbackAdapter
- Add ImageInferenceEvidencePacket
- Settings UI: image runtime status row

Pattern: Mirror OllamaLocalModelAdapter closely.

Validation: standard. No model weights downloaded by tests.

Branch: claude-implementation/c-phase-98-local-image-generation-runtime
Auto-continue? No
```

---

## C-PHASE 117 — Sprite Rung 1: Golden-Seed UI

```text
[Phase 117]

Read docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md
section "C-PHASE 117 — Sprite Rung 1: Golden-Seed UI + Heuristic Prefilter".

Implements S-S5 + S-G6.

Scope summary:
- Add SpriteTemplateCandidatePoolService (walks 4 source trees, heuristic prefilter)
- Add GoldenSeedingService (sidecar JSON storage, append-only)
- Add GoldenSeedingPanel in CreativeLearningLabWindow (keyboard-first review grid)
- Add tools/build_sprite_candidate_pool.py

Pattern: LearningLabBundleService + CreativeLearningLabWindow.

Validation: standard + python tools/build_sprite_candidate_pool.py --dry-run.

Branch: claude-implementation/c-phase-117-sprite-rung-1
Auto-continue? No
```

---

## C-PHASE 118 — Sprite Rung 2: Image Embedding Scoring

```text
[Phase 118]

Read docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md
section "C-PHASE 118 — Sprite Rung 2: Image Embedding Scoring".

Scope summary:
- Add OnnxImageEmbeddingBackend (mirrors OnnxEmbeddingBackend; CLIP-image)
- Add OnnxImageEmbeddingService (lazy load, hashing fallback)
- Add SpriteSimilarityService (score candidate vs cell's goldens via cosine)
- Add tools/install-image-embedder.ps1 (SHA-verified)
- BookmarkFromChatTool wired to scorer

Pattern: Mirror OnnxTextEmbeddingService + HashingTextEmbeddingService exactly.

Validation: standard + verify hashing fallback works when model missing.

Branch: claude-implementation/c-phase-118-sprite-rung-2
Auto-continue? No
```

---

## C-PHASE 119 — Sprite Rung 3: Palette Conformer + Color Propagation

```text
[Phase 119]

Read docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md
section "C-PHASE 119 — Sprite Rung 3: Palette Conformer + Color Propagation".

Implements P5 + S-11a/b.

Scope summary:
- Add ColorVariantPropagationService (reads canonical golden, propagates to 6 variants via grammar)
- Add tools/propagate-canonical-to-variants.ps1 (runs through GuardedMutationService)
- Sprite Workflow V2 UI: "Propagate Goldens" button per cell

Pattern: propagate_authored_colors.py + SpriteWorkflowApplyService.

Validation: standard + verify outline/eye unchanged across all variants.

Branch: claude-implementation/c-phase-119-sprite-rung-3
Auto-continue? Yes
```

---

## (Soak ends, 85d lands above)

---

## C-PHASE 95 — Build Hot-Swap Wrapper

```text
[Phase 95]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 95 — Build Hot-Swap Wrapper".

Implements W-B3 + W-14a-ii + W-14b-ii + W-14c-iii + W-14d-iii.

Scope summary:
- Add tools/run-build-and-hotswap.ps1 (smoke test pre-swap, abort on fail)
- Add WevitoHotSwapCoordinator (wait for natural pause, persist state, exit)
- Full state survives across swap (audit ledger + memory + chat history + scopes + soak heartbeat)

Pattern: build-release.ps1 + run-latest.bat.

Validation: standard + manual smoke (trigger swap, verify state intact).

Branch: claude-implementation/c-phase-95-build-hot-swap-wrapper
Auto-continue? No
```

---

## C-PHASE 96 — Codex Loop Runner (Initial)

```text
[Phase 96]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 96 — Codex Loop Runner (Initial)".

Scope summary:
- Add tools/run-codex-loop.ps1 (basic queue → invoke Codex → validate → merge or halt)
- Add tools/codex-phase-queue-init.ps1 (seeds queue from master plan)
- Initial docs/codex-phase-queue.json shipped empty
- FR4 retry logic deferred to C-PHASE 114

Pattern: PowerShell + gh CLI.

Validation: standard + manual smoke (run with one test phase, verify behavior).

Branch: claude-implementation/c-phase-96-codex-loop-runner-initial
Auto-continue? No
```

---

## C-PHASE 114 — Codex Loop Reliability Hardening

```text
[Phase 114]

Read docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
section "C-PHASE 114 — Codex Loop Reliability Hardening".

Implements Q20 (T4-modified + S2 + F4 + M4 + L3 + I4 + H4) +
the cross-cutting requirement that the loop also respects coexistence
triggers (Q19 — pause during gaming sessions).

Scope summary:
- Extend tools/run-codex-loop.ps1 with: timeout (2h baseline, idle-extend
  indefinitely via GetLastInputInfo > 10 min), stuck detection (30 min no
  commit halt), F4 retry-once with failure context then remediation card,
  M4 auto-rebase or halt
- Add CodexPhaseQueueService + 3 state files (queue / status / history.jsonl)
- Add CodexLoopWatchdogService in Broker
- Add tools/codex-inject.ps1, codex-loop-pause.ps1, codex-loop-resume.ps1
- Add tools/idle-detector/ small dotnet console for GetLastInputInfo
- Add ambient HomePanel badge "Codex loop: phase X, Y min in"
- Consult CoexistenceTriggerService: pause loop during gaming/video calls

Pattern: SoakDriverCommandService + WevitoProcessWatchdogService.

Validation: standard.

Branch: claude-implementation/c-phase-114-codex-loop-reliability-hardening
Auto-continue? No
```

---

## C-PHASE 86 — Pre-Approved Scope Service (H3)

```text
[Phase 86]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 86".

Scope summary:
- Add PetTaskAutonomousScopeService (declare/revoke/get-active scopes)
- Add AutonomousExecutionLoop (executes within scope via GuardedMutationService)
- Scope card UI in Settings tab

Pattern: AutonomousOperationsLoop + LearningLabPromotionService.

Validation: standard.

Branch: claude-implementation/c-phase-86-pre-approved-scope-service
Auto-continue? No
```

---

## C-PHASE 87 — Sandboxed Experiment Runner

```text
[Phase 87]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 87".

Scope summary:
- Add IExperimentKind + ExperimentRegistry (empty by default)
- Add ExperimentRunnerService (timeout per kind, budget reserved via RuntimeBudgetMeter)
- Add ExperimentResultStore (append-only sqlite)

Pattern: AutonomousOperationsLoop + LearningEvalService.

Validation: standard.

Branch: claude-implementation/c-phase-87-sandboxed-experiment-runner
Auto-continue? No
```

---

## C-PHASE 88 — Experiment Kind: sprite-template-candidate-generation

```text
[Phase 88]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 88".

Scope summary:
- Add SpriteTemplateCandidateGenerationKind : IExperimentKind
- Picks weakest cell by user-approval rate → generates via IImageModelAdapter
  with palette grammar → conforms → scores via SpriteSimilarityService →
  proposes Draft card if score > threshold
- IsSafeForAutoExecution = false (always proposes; user approves manually)

Pattern: SpriteWorkflowCandidateImporter.

Validation: standard.

Branch: claude-implementation/c-phase-88-experiment-kind-sprite-template
Auto-continue? No
```

---

## C-PHASE 93 — Composite Fitness Scoreboard

```text
[Phase 93]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 93".

Implements A-M5 multi-axis scoreboard.

Scope summary:
- Add CompositeFitnessScoreboardService (per-axis scores, no single-number collapse)
- Read from UserFeedbackStore, LearningEvalService, audit ledger, RuntimeBudgetMeter, ExperimentResultStore
- 1-hour cadence; persist history
- Extends Benchmarks tab UI

Pattern: SelfImprovementReportService.

Validation: standard.

Branch: claude-implementation/c-phase-93-composite-fitness-scoreboard
Auto-continue? No
```

---

## C-PHASE 94 — Maturity Promotion Service

```text
[Phase 94]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 94".

Implements A-M5 per-capability criteria.

Scope summary:
- Add MaturityPromotionService (reads scoreboard + per-capability criterion JSON)
- Seed criteria files: r3-to-r4.json, experiment-kind-auto-runnable.json, lora-self-promote.json
- Criteria themselves mutation-gated (loosening requires guarded mutation)
- Settings → Autonomous Operations panel shows status

Pattern: PromotionCriteriaSnapshot.

Validation: standard.

Branch: claude-implementation/c-phase-94-maturity-promotion-service
Auto-continue? No
```

---

## C-PHASE 89 — Experiment Kind: LoRA Hyperparameter Search

```text
[Phase 89]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 89".

Scope summary:
- Add LoraHyperparameterSearchKind : IExperimentKind
- Tries combos from registered grid (rank, alpha, lr, epochs)
- Invokes ImageLoRATrainingRunner (C-PHASE 97)
- Auto-rollback on > 2% regression
- IsSafeForAutoExecution = false (expensive + mutating)

Pattern: LocalTuningRunner.

Validation: standard.

Branch: claude-implementation/c-phase-89-experiment-kind-lora-hyperparam
Auto-continue? No
```

---

## C-PHASE 97 — Image LoRA Training Pipeline

```text
[Phase 97]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 97 — Image LoRA Training Pipeline".

Scope summary:
- Add ImageLoRATrainingPlanService (plan-only manifest)
- Add ImageLoRATrainingRunner (invokes Python sidecar via ProcessCommandRunner; 30-min timeout)
- Add ImageLoRAEvalService (CLIP-image score vs golden set)
- Add tools/image-lora-training/ (train.py + requirements.txt + verify-deps.ps1)
- Settings: tuning_image_lora_enabled default false

Pattern: LocalTrainingPlanService + LocalTuningRunner.

Validation: standard + tools/image-lora-training/verify-deps.ps1 (no auto-download).

Branch: claude-implementation/c-phase-97-image-lora-training-pipeline
Auto-continue? No
```

---

## C-PHASE 90 — Constitutional Decision Service

```text
[Phase 90]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 90".

Scope summary:
- Add ConstitutionalDecisionService + IConstitutionalRule
- Initial rules: BudgetExceeded, UnregisteredKind, InvariantViolation, SettingsFlip, HostedAiCall, WebOutsideAllowlist
- Versioned + mutation-gated

Pattern: UnifiedPolicyService.

Validation: standard.

Branch: claude-implementation/c-phase-90-constitutional-decision-service
Auto-continue? No
```

---

## C-PHASE 91 — Approved Research Connector (R3)

```text
[Phase 91]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 91".

Implements A-R3-now (allowlist starts empty).

Scope summary:
- Extend WebResearchConnector with allowlist mode + time-boxed sessions
- Fetched content enters "research notes" channel (never directly as model instructions)
- Per-fetch evidence packet with URL + sha256 + session id

Pattern: WebResearchConnector (extend).

Validation: standard + verify empty allowlist on day 1.

Branch: claude-implementation/c-phase-91-approved-research-connector
Auto-continue? No
```

---

## C-PHASE 92 — Self-Improvement Promotion Gate

```text
[Phase 92]

Read docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
section "C-PHASE 92".

Scope summary:
- Add SelfImprovementPromotionService (proposes promotions on measurable improvement)
- 30-day mandatory user approval; afterward gated by C-PHASE 94 maturity criteria

Pattern: LearningLabPromotionService.

Validation: standard.

Branch: claude-implementation/c-phase-92-self-improvement-promotion-gate
Auto-continue? No
```

---

## C-PHASE 103 — Memory Consolidation

```text
[Phase 103]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 103".

Scope summary:
- Add MemoryConsolidationService (7-day cadence chore; archive rows older than 30 days)
- Add LearnedPatternStore + PatternDiscoveryService (confidence > 0.7 only)

Pattern: SelfImprovementReportService + AuditLedgerService.

Validation: standard.

Branch: claude-implementation/c-phase-103-memory-consolidation
Auto-continue? Yes
```

---

## C-PHASE 104 — Strategic Planner

```text
[Phase 104]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 104".

Scope summary:
- Add StrategicPlannerService (reads scoreboard, identifies weakest axis, picks kind with highest expected value)
- Add ExperimentKindHistoryStore (per-kind axis history)
- Modify ExperimentRunnerService to consult planner first, fall back to weighted-random

Pattern: AutonomousBetaDecisionService + EvalRegressionGate.

Validation: standard.

Branch: claude-implementation/c-phase-104-strategic-planner
Auto-continue? No
```

---

## C-PHASE 105 — Activity Digest UI

```text
[Phase 105]

Read docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md section
"C-PHASE 105".

Scope summary:
- Add ActivityDigestService (reads reports + summaries + patterns + cards + experiments)
- Add ActivityDigestPanel in ToolPopupWindow (Today / Week / Month modes)
- Read-only; uses PlainLanguageExplainer summaries (never raw private text)

Pattern: ActivitySummaryService.

Validation: standard.

Branch: claude-implementation/c-phase-105-activity-digest-ui
Auto-continue? Yes
```

---

## C-PHASE 120 — Sprite Rung 4: Remaining Animations

```text
[Phase 120]

Read docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md
section "C-PHASE 120".

Scope summary:
- Process phase: repeat rungs 1-3 for the 7 non-idle animations
- Add SpriteTemplateCompletionReportService (aggregate progress)
- Archive incoming_sprites/ to sprites_archive/<ts>/

Pattern: Reuses everything from C-PHASE 117-119.

Validation: standard.

Branch: claude-implementation/c-phase-120-sprite-rung-4
Auto-continue? Yes
```

---

## C-PHASE 123 — Pet Visual Polish

```text
[Phase 123]

Read docs/CLAUDE_NON_INTERRUPTION_AND_PET_SIM_PHASES_121_THROUGH_124_2026-05-15.md
section "C-PHASE 123 — Pet Visual Polish".

Depends on C-PHASE 120 (clean sprites must be in place).

Scope summary:
- Godot pet.gd: animation cross-fade, position interpolation, idle micro-behaviors (blink/look/twitch/wag), particle effects (z/heart/crumb/bubble/tear/dust), window shake reaction
- Add ParticleEffectsLayer Node2D
- Add PetVisualPolishLogger (rate-limited audit)

Pattern: Standard Godot patterns + existing pet.gd state machine.

Validation: standard + soak test (60fps maintained with all effects).

Branch: claude-implementation/c-phase-123-pet-visual-polish
Auto-continue? Yes
```

---

## C-PHASE 124 — Pet Behavior + Pet ↔ AI Awareness

```text
[Phase 124]

Read docs/CLAUDE_NON_INTERRUPTION_AND_PET_SIM_PHASES_121_THROUGH_124_2026-05-15.md
section "C-PHASE 124".

Depends on C-PHASE 109 (tool registry) + C-PHASE 107 (chat UI).

Scope summary:
- Goal-based pet wandering (seek_food_zone, etc.)
- Cursor curiosity (head turn within 80px proximity)
- Add PetStateTool to ToolRegistry (AI can query pet state)
- Add PetInteractionLogger
- Add PetStateContextInjector (AI gets fyi prompt when pet stat critical)

Pattern: Existing pet.gd + ToolRegistry + RetrievalAutomaticInjector.

Validation: standard.

Branch: claude-implementation/c-phase-124-pet-behavior-pet-ai-awareness
Auto-continue? Yes
```

---

## Universal closing instructions (applies to every phase)

```text
Before opening PR for any phase:
1. All named tests pass
2. dotnet build = 0 errors, 0 warnings
3. tools/build-vnext.ps1 succeeds
4. Every stop gate listed in the phase plan is FALSE
5. PlainLanguageExplainer.KnownPacketKinds includes every new packet kind
6. Phase report doc written under docs/C_PHASE<NNN>_*.md
7. Commit message references the decision IDs implemented

When the loop runner detects a green PR with Auto-continue=Yes, it
auto-merges and proceeds. When Auto-continue=No, it halts for explicit
user review.

If anything is ambiguous: do NOT improvise. Halt the phase, write a
phase_blocked audit row with the ambiguity description, and let the
user clarify via tools/codex-inject.ps1.
```
