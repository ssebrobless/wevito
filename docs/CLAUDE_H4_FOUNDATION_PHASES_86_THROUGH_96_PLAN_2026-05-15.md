# Wevito H4+ Foundation Phase Plan: C-PHASE 86 through 96

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex at medium reasoning effort
Companion files:
- `docs/DECISION_LEDGER_2026-05-15.md`
- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md`

## 0. Purpose

These eleven phases land the autonomy infrastructure that lets wevito
run experiments, mutate its own state under guard, score its own
improvement, and promote new capabilities once maturity criteria are
met. They sit above `C-PHASE 85d` (autonomous beta promotion) and below
the reframe/gap/sprite phases.

```text
Sequence within the master plan:
... reframe + gap + sprite phases land first ...
SOAK ENDS
C-PHASE 85d   Autonomous Operations Promotion
C-PHASE 95    Build Hot-Swap Wrapper
C-PHASE 96    Codex Loop Runner (initial)
C-PHASE 114   Codex Loop Reliability Hardening (already documented in reframe doc)
C-PHASE 86    Pre-Approved Scope Service (H3)         <-- this doc
C-PHASE 87    Sandboxed Experiment Runner + Registry   <-- this doc
C-PHASE 88    Experiment Kind: sprite-template-gen     <-- this doc
C-PHASE 93    Composite Fitness Scoreboard             <-- this doc
C-PHASE 94    Maturity Promotion Service               <-- this doc
C-PHASE 89    Experiment Kind: LoRA hyperparam search  <-- this doc
C-PHASE 97    Image LoRA Training (gap doc)
C-PHASE 90    Constitutional Decision Service          <-- this doc
C-PHASE 91    Approved Research Connector Expansion    <-- this doc
C-PHASE 92    Self-Improvement Promotion Gate          <-- this doc
... rest of autonomy + sprite rung 4 ...
```

## 0.1 Hard Invariants (carried forward)

```text
- LocalOnly model provider mode; no hosted AI.
- Every mutation: dry-run + backup + apply + post-proof + rollback?
- Every learning step: reviewed data + eval gate + rollback.
- KillSwitch halts every adapter, scheduler, loop, runner.
- Every new packet kind in PlainLanguageExplainer.KnownPacketKinds.
- Default state: capabilities code-active, inputs empty.
- Promotion criteria versioned + mutation-gated.
- Reset on violation.
```

---

## C-PHASE 86 — Pre-Approved Scope Service (H3)

**Goal:** User declares scopes within which wevito is allowed to execute
work autonomously (not just propose). Each scope is itself a TaskCard
the user approves once; it persists until revoked.

**Scope:**

- Add `PetTaskAutonomousScopeService` in `Wevito.VNext.Core`:
  - `IReadOnlyList<AutonomousScope> GetActiveScopes()`.
  - `Guid DeclareScope(AutonomousScopeRequest request, TaskCard approvalCard)`.
  - `void RevokeScope(Guid scopeId)`.
  - Scope schema: `tool_family`, `target_pattern` (e.g., `species=rat,fox`),
    `max_per_day`, `requires_dry_run` (always true), `expires_at_utc`
    (default 30d), `approval_card_id`.
- Add `AutonomousExecutionLoop`:
  - Mirrors `AutonomousOperationsLoop` (proposal-only) but with execution
    authority within declared scopes.
  - Per iteration: read declared scopes, find matching candidates,
    execute via `GuardedMutationService` (dry-run + apply + post-proof).
  - Refuses to execute outside any scope.
  - Halts on KillSwitch or supervisor mode != Active.
- Settings UI: "Settings" → "Autonomous Scopes" tab grows a scope-declaration
  form + active-scopes list with revoke button.
- New packet kinds: `autonomous_scope_declared`, `autonomous_scope_revoked`,
  `autonomous_execution_within_scope`.

**Pattern:** Follow `AutonomousOperationsLoop.cs` for the loop shape;
extend with execution authority within declared scope. Follow
`LearningLabPromotionService.cs` for the approval-card-gated mutation.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/AutonomousOperationsLoop.cs                (informational reference)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml
vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/AutonomousScope.cs
vnext/src/Wevito.VNext.Core/AutonomousScopeRequest.cs
vnext/src/Wevito.VNext.Core/PetTaskAutonomousScopeService.cs
vnext/src/Wevito.VNext.Core/AutonomousExecutionLoop.cs
vnext/tests/Wevito.VNext.Tests/PetTaskAutonomousScopeServiceTests.cs
vnext/tests/Wevito.VNext.Tests/AutonomousExecutionLoopTests.cs
docs/C_PHASE86_PRE_APPROVED_SCOPE_SERVICE_2026-05-15.md
```

**Tests:**

- `PetTaskAutonomousScopeServiceTests.DeclareRequiresApprovedTaskCard`
- `PetTaskAutonomousScopeServiceTests.RevokeRemovesFromActive`
- `PetTaskAutonomousScopeServiceTests.ExpiredScopesAutoRevoked`
- `AutonomousExecutionLoopTests.RefusesOutOfScopeExecution`
- `AutonomousExecutionLoopTests.RespectsKillSwitch`
- `AutonomousExecutionLoopTests.RespectsMaxPerDay`
- `AutonomousExecutionLoopTests.AlwaysRunsThroughGuardedMutation`
- `AutonomousExecutionLoopTests.RollsBackOnPostProofFail`
- `PlainLanguageExplainerTests.CoversAutonomousScopeKinds`

**Validation:** standard build/test/build-vnext.

**Stop gates:**
- Stop if scope can be declared without approved task card.
- Stop if execution can bypass `GuardedMutationService`.
- Stop if execution exceeds `max_per_day`.
- Stop if expired scopes still execute.

**Rollback:** revert PR; autonomy returns to proposal-only mode.

**Commit/PR:** branch `claude-implementation/c-phase-86-pre-approved-scope-service`,
title `C-PHASE 86: Pre-approved scope service (H3 foundation)`.

**Auto-continue?** **No.** First execution authority granted to wevito.

---

## C-PHASE 87 — Sandboxed Experiment Runner + Kind Registry

**Goal:** Add the H4+ experiment infrastructure: a runner that picks
from a registered list of experiment kinds, runs them in resource/time/
filesystem-bounded contexts, scores results, keeps what works.

**Scope:**

- Add `IExperimentKind` interface:
  - `string Id { get; }`
  - `string AxisTargeted { get; }`
  - `Task<ExperimentResult> RunAsync(ExperimentContext context, CancellationToken ct)`
  - `bool IsSafeForAutoExecution()` (default false until kind earns M5 criterion)
- Add `ExperimentRegistry`:
  - Holds `IReadOnlyList<IExperimentKind>` registered at startup.
  - Default registry is EMPTY (per W-C3 first-boot decision).
  - `Register(IExperimentKind kind)` is a guarded mutation (no free-form add).
- Add `ExperimentRunnerService`:
  - Per tick: consults `StrategicPlannerService` (post-C-PHASE 104) for
    next kind to run; if planner returns null, falls back to weighted-random
    based on past success rate.
  - Reserves budget via `RuntimeBudgetMeter`.
  - Runs the kind in a `Task` with `CancellationTokenSource` bound to a
    per-kind timeout (default 30 min, kind-overridable).
  - Captures result; updates kind history store (per C-PHASE 104).
  - Writes `experiment_run_started` / `experiment_run_completed` /
    `experiment_run_cancelled` packets.
- Add `ExperimentResultStore`:
  - sqlite append-only; one row per run.
  - Schema: `run_id`, `kind_id`, `axis`, `outcome` (improved/regressed/neutral),
    `magnitude`, `started_at_utc`, `ended_at_utc`, `evidence_packet_path`.
- New packet kinds: `experiment_run_started`, `experiment_run_completed`,
  `experiment_run_cancelled`, `experiment_kind_registered`.

**Pattern:** Follow `AutonomousOperationsLoop.cs` for the iteration shape;
follow `LearningEvalService.cs` for the result-stored-with-eval-packet pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/RuntimeBudgetMeter.cs                      (reservation hook)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/IExperimentKind.cs
vnext/src/Wevito.VNext.Core/ExperimentContext.cs
vnext/src/Wevito.VNext.Core/ExperimentResult.cs
vnext/src/Wevito.VNext.Core/ExperimentRegistry.cs
vnext/src/Wevito.VNext.Core/ExperimentRunnerService.cs
vnext/src/Wevito.VNext.Core/ExperimentResultStore.cs
vnext/tests/Wevito.VNext.Tests/ExperimentRegistryTests.cs
vnext/tests/Wevito.VNext.Tests/ExperimentRunnerServiceTests.cs
vnext/tests/Wevito.VNext.Tests/ExperimentResultStoreTests.cs
docs/C_PHASE87_SANDBOXED_EXPERIMENT_RUNNER_2026-05-15.md
```

**Tests:**

- `ExperimentRegistryTests.DefaultRegistryIsEmpty`
- `ExperimentRegistryTests.RegisterRequiresGuardedMutation`
- `ExperimentRunnerServiceTests.RefusesWhenBudgetExhausted`
- `ExperimentRunnerServiceTests.RespectsKillSwitch`
- `ExperimentRunnerServiceTests.CancelsAfterTimeout`
- `ExperimentRunnerServiceTests.WritesPacketPerStateTransition`
- `ExperimentResultStoreTests.AppendIsAppendOnly`
- `ExperimentResultStoreTests.QueryByKindAndOutcome`
- `PlainLanguageExplainerTests.CoversAllExperimentKinds`

**Validation:** standard.

**Stop gates:**
- Stop if registry can be modified outside guarded mutation.
- Stop if runner can launch when budget exhausted.
- Stop if kill switch bypassed.
- Stop if a kind runs longer than its declared timeout.

**Rollback:** revert PR; no autonomous experimentation; wevito stays
proposal-only post-85d.

**Commit/PR:** branch `claude-implementation/c-phase-87-sandboxed-experiment-runner`,
title `C-PHASE 87: Sandboxed experiment runner + kind registry`.

**Auto-continue?** **No.** First execution surface beyond mutations;
stop for explicit review.

---

## C-PHASE 88 — Experiment Kind: sprite-template-candidate-generation

**Goal:** Register the first experiment kind: wevito proposes new sprite
template candidates via the image generation runtime (C-PHASE 98) +
image LoRA (C-PHASE 97). Outputs are Draft cards; never auto-applies in
this phase.

**Scope:**

- Add `SpriteTemplateCandidateGenerationKind : IExperimentKind`:
  - `Id = "sprite-template-candidate-generation"`
  - `AxisTargeted = "sprite_template_approval_rate"`
  - `IsSafeForAutoExecution = false` (always proposes Draft cards; user
    approves manually)
  - Per run:
    1. Pick a target (species, age, gender, animation) cell that is
       weakest by user-approval rate.
    2. Call `IImageModelAdapter.GenerateAsync` (C-PHASE 98) with the
       cell's palette grammar (C-PHASE 99) as `palette_grammar_id`.
    3. Run output through `PaletteConformer.Conform` (C-PHASE 99).
    4. Score via `CLIP-image` embedding cosine vs the cell's goldens
       (C-PHASE 99 / sprite rung 2).
    5. If score > threshold, propose as Draft TaskCard with the
       generated frame + score + prompt; user reviews via existing
       Sprite Workflow V2 surface.
- Settings: `sprite_candidate_gen_min_score` (default 0.65),
  `sprite_candidate_gen_max_per_session` (default 3).
- New packet kind: `sprite_candidate_proposed`.

**Pattern:** Follow `SpriteWorkflowCandidateImporter.cs` for the
proposal-as-task-card pattern.

**Files likely touched:**

```text
vnext/src/Wevito.VNext.Core/ExperimentRegistry.cs                       (register the kind)
vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
vnext/content/tool_definitions.json
```

**New files:**

```text
vnext/src/Wevito.VNext.Core/Experiments/SpriteTemplateCandidateGenerationKind.cs
vnext/tests/Wevito.VNext.Tests/Experiments/SpriteTemplateCandidateGenerationKindTests.cs
docs/C_PHASE88_EXPERIMENT_KIND_SPRITE_TEMPLATE_2026-05-15.md
```

**Tests:**

- `SpriteTemplateCandidateGenerationKindTests.PicksWeakestCellByApprovalRate`
- `SpriteTemplateCandidateGenerationKindTests.ProposesAsDraftCardOnly`
- `SpriteTemplateCandidateGenerationKindTests.RefusesBelowScoreThreshold`
- `SpriteTemplateCandidateGenerationKindTests.RespectsMaxPerSession`
- `SpriteTemplateCandidateGenerationKindTests.CallsPaletteConformer`
- `PlainLanguageExplainerTests.CoversSpriteCandidateProposedKind`

**Validation:** standard.

**Stop gates:**
- Stop if kind auto-applies instead of proposing.
- Stop if generated frame skips palette conformance.
- Stop if cell selection bias is wrong (must target weakest).

**Rollback:** revert PR; experiment registry has no kind to run; wevito
sits at "registered runner with empty kind list."

**Commit/PR:** branch `claude-implementation/c-phase-88-experiment-kind-sprite-template`,
title `C-PHASE 88: Experiment kind: sprite-template-candidate-generation`.

**Auto-continue?** **No.** First registered kind; stop for review.

---

## C-PHASE 89 — Experiment Kind: lora-hyperparameter-search

**Goal:** Wevito tries different LoRA training hyperparameters (rank,
alpha, learning rate, epochs) against the existing approved sprite
corpus, scores via golden eval, promotes winner with rollback if regression.

**Depends on:** C-PHASE 97 (image LoRA training pipeline).

**Scope:**

- Add `LoraHyperparameterSearchKind : IExperimentKind`:
  - `Id = "lora-hyperparameter-search"`
  - `AxisTargeted = "sprite_template_approval_rate"`
  - `IsSafeForAutoExecution = false` initially (LoRA training is
    expensive + mutates content; user reviews each).
  - Per run:
    1. Pick hyperparameter combo from a registered grid
       (e.g., `rank ∈ {8, 16, 32}`, `alpha ∈ {16, 32, 64}`,
       `lr ∈ {1e-4, 5e-4, 1e-3}`, `epochs ∈ {5, 10}`).
    2. Invoke `ImageLoRATrainingRunner` (C-PHASE 97) with the combo.
    3. Run `ImageLoRAEvalService` against existing baseline.
    4. If new LoRA beats prior by ≥ X%, propose promotion as Draft TaskCard.
    5. If regression > 2%, auto-rollback per existing infrastructure.
- New packet kind: `lora_hyperparam_combo_tried`.

**Pattern:** Follow `LocalTuningRunner.cs` for the train/eval/rollback
pattern; this kind orchestrates that for hyperparameter search.

**Files likely touched / new files:** similar shape to C-PHASE 88, scoped
to the LoRA kind.

**Tests:**

- `LoraHyperparameterSearchKindTests.PicksFromRegisteredGrid`
- `LoraHyperparameterSearchKindTests.RefusesWhenC97NotLanded`
- `LoraHyperparameterSearchKindTests.AutoRollbackOnRegression`
- `LoraHyperparameterSearchKindTests.ProposesPromotionWhenBeatsBaseline`

**Validation:** standard; gated on C-PHASE 97 + 87 + 88 having merged.

**Stop gates:**
- Stop if kind runs without C-PHASE 97 infrastructure present.
- Stop if regression rollback is bypassable.

**Auto-continue?** **No.** First training-mutation kind.

---

## C-PHASE 90 — Constitutional Decision Service

**Goal:** Hard rules wevito applies when picking what to do autonomously.
Prevents wevito from going off-rails even when strategic planner suggests
something that would violate invariants.

**Scope:**

- Add `ConstitutionalDecisionService`:
  - Holds `IReadOnlyList<IConstitutionalRule>`.
  - On every experiment selection / mutation proposal, runs all rules.
  - Any rule returning `Veto` blocks the action.
- Initial rule set:
  - `BudgetExceededRule` — must respect `RuntimeBudgetMeter`.
  - `UnregisteredKindRule` — must come from registered registry.
  - `InvariantViolationRule` — must not target a path that would violate
    any hard invariant.
  - `SettingsFlipRule` — must never propose flipping a default-off setting
    automatically.
  - `HostedAiCallRule` — must never call hosted AI.
  - `WebOutsideAllowlistRule` — must never fetch URL outside R3 allowlist.
- Rules are versioned + mutation-gated (per A-M5).
- New packet kind: `constitutional_decision_made`.

**Pattern:** Follow `UnifiedPolicyService.cs` for the rule-evaluation +
veto pattern.

**Files / tests:** standard.

**Auto-continue?** **No.** Hard-rule layer; user reviews.

---

## C-PHASE 91 — Approved Research Connector Expansion (R3)

**Goal:** Extend C-PHASE 70's offline-default `WebResearchConnector` to
allow approved URL allowlist with time-boxed sessions. Per A-R3-now.

**Scope:**

- Extend `WebResearchConnector` with allowlist mode:
  - Setting `web_research_allowlist_json` (default `[]`).
  - Setting `web_research_session_max_minutes` (default 30).
  - Setting `web_research_session_max_urls` (default 50).
  - User declares a "research session" via TaskCard; allowlist URLs can
    be fetched within session window.
- Fetched content is *never* injected directly into model prompts; it
  enters a separate "research notes" channel that the model can
  *summarize* via tool call, not execute.
- Every fetch writes `web_fetched_during_session` packet with URL,
  content sha256, session id, purpose tag.
- New packet kinds: `research_session_started`, `research_session_ended`,
  `web_fetched_during_session`, `web_research_allowlist_changed`.

**Pattern:** Follow existing `WebResearchConnector.cs`.

**Files / tests:** standard.

**Auto-continue?** **No.** First real web access; user reviews allowlist
contents + session shape.

---

## C-PHASE 92 — Self-Improvement Promotion Gate

**Goal:** When wevito's experiments produce measurable improvement, gate
promotion behind user approval for the first 30 days, then track-record-based.

**Scope:**

- Add `SelfImprovementPromotionService`:
  - Reads experiment results from `ExperimentResultStore`.
  - When a result shows improvement above threshold (e.g., LoRA hyperparam
    combo beat baseline), emits a `self_improvement_promotion_proposal`
    Draft card.
  - User approves → promotion applies via `GuardedMutationService`.
  - First 30 days: every promotion requires user approval.
  - After 30 days of clean track record: subsequent promotions of the
    same kind auto-apply *if* the kind's M5 criterion is satisfied
    (per C-PHASE 94).
- New packet kind: `self_improvement_promotion_proposed`,
  `self_improvement_promotion_auto_applied`.

**Pattern:** Follow `LearningLabPromotionService.cs`.

**Files / tests:** standard.

**Auto-continue?** **No.** First self-modifying promotion path.

---

## C-PHASE 93 — Composite Fitness Scoreboard

**Goal:** Implement the M5 multi-axis scoreboard. Reads from user feedback
(C-PHASE 102), benchmarks (C-PHASE 112), invariant violation count, mutation
proof coverage, budget conformance, experiment success rate. Surfaces both
detail tab (UI5) and ambient badge.

**Scope:**

- Add `CompositeFitnessScoreboardService`:
  - Computes per-axis scores from sources:
    - `template_approval_rate_7d` from `UserFeedbackStore`
    - `golden_eval_recall_at_1` from latest `LearningEvalService` run
    - `mutation_proof_coverage_pct` from audit ledger
    - `invariant_violation_count_30d` from audit ledger
    - `budget_conformance_pct` from `RuntimeBudgetMeter`
    - `experiment_success_rate_30d` from `ExperimentResultStore`
    - `user_feedback_signal_30d` from `UserFeedbackStore`
  - Computed every 1 hour; persisted to
    `%LOCALAPPDATA%/Wevito/composite-fitness-history.jsonl`.
  - Multi-axis snapshot is NOT collapsed into a single number; each axis
    has its own threshold for the maturity gate (per A-M5).
- UI: extends Benchmarks tab (C-PHASE 112) with the per-axis scoreboard.
- New packet kind: `composite_fitness_snapshot`.

**Pattern:** Follow `SelfImprovementReportService.cs`.

**Files / tests:** standard.

**Auto-continue?** **No.** Foundation for maturity gating; user reviews axis weights.

---

## C-PHASE 94 — Maturity Promotion Service

**Goal:** Read composite fitness + per-capability track record; decide
whether to auto-unlock the next capability tier (R3→R4, experiment
auto-execution, etc.) per the M5 model.

**Scope:**

- Add `MaturityPromotionService`:
  - `MaturityPromotionDecision Evaluate(string capabilityId, DateTimeOffset now)`.
  - Reads capability-specific criterion from
    `vnext/content/maturity-criteria/<capability-id>.json`.
  - Reads composite fitness + track-record sources.
  - Returns `Promote` | `KeepSupervised` | `BlockedByCriterion(criterion)`.
- Initial criteria files committed:
  - `r3-to-r4.json` — 100 approved sessions, 90 days, 0 prompt-injection
    incidents, composite > threshold.
  - `experiment-kind-auto-runnable.json` — 50 successful runs, 90% success
    rate, 0 invariant violations.
  - `lora-self-promote.json` — beats prior baseline by X%, 5 prior
    promotions retained.
- Criteria themselves are mutation-gated (loosening requires guarded
  mutation; tightening is free).
- UI: Settings → Autonomous Operations → Maturity panel shows each
  capability's current criterion status.
- New packet kinds: `maturity_promotion_evaluated`, `maturity_promotion_applied`.

**Pattern:** Follow `PromotionCriteriaSnapshot.cs` from C-PHASE 85c.

**Files / tests:** standard.

**Auto-continue?** **No.** Foundation for autonomous promotion; user reviews.

---

## C-PHASE 95 — Build Hot-Swap Wrapper

**Goal:** Implement W-B3 + W-14a-ii + W-14b-ii + W-14c-iii + W-14d-iii:
auto-install + smoke-test + hot-swap at natural pause + full state survival.

**Scope:**

- Add `tools/run-build-and-hotswap.ps1`:
  - Triggered after a phase merge by `run-codex-loop.ps1` (C-PHASE 96).
  - Runs `build-vnext.ps1` to produce new build artifact.
  - Runs `<new-build> --smoke-test --no-shell` (~30-60s) — validates
    audit/settings/memory/ONNX load.
  - On smoke fail: writes `auto_update_aborted` audit row, opens Stop
    card, does NOT swap.
  - On smoke pass: signals current wevito process to enter `Quiet` mode,
    persist in-flight state to `%LOCALAPPDATA%/Wevito/`, exit gracefully.
  - Spawns new build; new build reads back state, writes
    `runtime_session_start` with `prior_session_id`.
- Add `WevitoHotSwapCoordinator` in shell:
  - Detects swap signal from wrapper.
  - Waits for "natural pause" (no clicks 5s, no mouseover, no animation).
  - Persists state; exits.
- New packet kinds: `auto_update_proposed`, `auto_update_smoke_pass`,
  `auto_update_aborted`, `hot_swap_initiated`, `hot_swap_completed`.

**Pattern:** Follow existing `build-release.ps1` + `run-latest.bat`.

**Files / tests:** standard.

**Auto-continue?** **No.** First self-update path; user reviews.

---

## C-PHASE 96 — Codex Loop Runner (Initial)

**Goal:** Implement the basic loop runner per Q18-runner. Reads phase
queue, invokes Codex, runs validation, auto-merges or halts, advances.
**Hardening** (T4-modified timeout, S2 stuck-detect, F4 retry, M4
rebase, L3 three-file state, I4 inject, H4 heartbeat-watchdog) ships
separately in C-PHASE 114.

**Scope:**

- Add `tools/run-codex-loop.ps1`:
  - Reads `docs/codex-phase-queue.json` (initial empty).
  - Picks next pending phase entry.
  - Invokes Codex CLI with the prompt file location + phase ID.
  - On Codex completion, runs `dotnet build` + `dotnet test`.
  - On green + `Auto-continue=Yes`: auto-merges PR via `gh pr merge`.
  - On green + `Auto-continue=No`: writes `phase_awaiting_review` audit row,
    halts.
  - On red: halts (FR4 retry logic deferred to C-PHASE 114).
  - Triggers `run-build-and-hotswap.ps1` on successful merge.
- Add `tools/codex-phase-queue-init.ps1`:
  - Initial seed of the queue file from the master plan's sequence.
- Initial queue file `docs/codex-phase-queue.json` shipped empty
  (intentionally; user runs `codex-phase-queue-init.ps1` to populate).
- New packet kinds: `codex_phase_started`, `codex_phase_completed`,
  `codex_phase_awaiting_review`, `codex_loop_idle`.

**Pattern:** PowerShell + `gh` CLI.

**Files / tests:** standard.

**Auto-continue?** **No.** First unattended-loop scaffold; user reviews.

---

## Closing Notes

These 11 phases are the autonomy backbone. Once they merge along with
C-PHASE 114 (loop reliability hardening from the reframe doc), wevito
can run end-to-end Codex loops on subsequent phases — including the
sprite-cleanup work, the image LoRA training, the strategic planner,
and the maturity-driven capability unlocks.

Each phase here is sized for medium-Codex effort (3-7 new files, 5-10 tests,
single architectural concern). The `Auto-continue=No` count is high (10
of 11) because every phase in this group lands a new autonomy primitive
that the project owner should consciously review before it goes live.
