# Claude Plan - C-PHASE 143+ Supervised Self-Improvement Buildout

Date: 2026-05-18
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (executor, medium reasoning effort) + project owner (review)
Source: `docs/C_PHASE142_SUPERVISED_SELF_IMPROVEMENT_LOOP_DESIGN_2026-05-18.md`
Current main tip at planning time: `80f52f59b9648f61fd99db2b01eccc4db6fbf83e`

## 0. Summary

C-PHASE 142 produced the design blueprint for a supervised self-improvement
loop. This plan turns that blueprint into a concrete batch of code phases
(C-PHASE 143 through C-PHASE 150) that build the loop in safe order:
packet taxonomy → constitutional decision service → empty experiment
registry → eval gate manifest → first review-only experiment scope →
explicit-approval type → maturity-clock scoreboard → supervised pilot.

Every phase in this batch is Auto-continue=No. Human review of each PR is
mandatory before the next phase begins. No phase in this batch flips a
capability default to on. No phase in this batch adds a hosted-AI runtime
brain, a network surface, a model-weight update path, or an auto-merge
shortcut. The v0 boundary from C-PHASE 142 is preserved: Wevito may
propose, critique, dry-run, report, and await approval; the human is the
judge.

## 1. Hard Invariants (apply to every phase in this batch)

Lifted from `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` §4 and the C-PHASE 142
design doc. These are the durable invariants that Codex must preserve in
every commit of this batch:

- No hosted AI as Wevito's runtime brain.
- No silent network access.
- No silent training. No model-weight updates from this batch.
- No silent file, tool, code, or asset mutation.
- Every mutation pathway requires exact scope + dry run + backup + sha256
  evidence + apply + post-proof + rollback + user-visible report.
- KillSwitchService halts every adapter, scheduler, autonomous loop,
  runner, scope, experiment, eval runner, and approval handler.
- AuditLedgerService remains append-only (sqlite UPDATE/DELETE triggers
  remain enforced).
- Every new audit packet kind appears in
  `PlainLanguageExplainer.KnownPacketKinds` with a plain-language sentence.
- Every evidence packet sets `did_use_network`, `did_use_hosted_ai`,
  `did_use_local_model`, and `did_mutate` honestly.
- Capability flags introduced by this batch default OFF unless explicitly
  approved by the user in writing.
- Held-out eval data must not be visible to any proposal loop, local model
  prompt, or iterative repair process.
- Pets remain visually regular pet-sim characters. No AI-task animation
  overlay. No pet-overlay that makes pets appear to "do AI work" visually.
- Pet game FPS / user PC experience must stay protected from AI workload.
- Pet game has 1 GB reserved RAM minimum.
- The human is the judge in v0. Wevito may not approve, merge, or apply
  on its own behalf.
- Auto-continue=No phases never auto-merge.

## 2. Phase Inventory

Recommended order, with one ordering refinement from the user's
suggested sequence (147 moved before 146 so the first review-only
experiment scope can emit eval-gate-shaped evidence from day one):

| # | Phase | Title | Auto-continue |
|---|---|---|---|
| 1 | C-PHASE 143 | Self-improvement packet taxonomy + plain-language explainer rows | No |
| 2 | C-PHASE 144 | ConstitutionalDecisionService v0 (default-deny policy only) | No |
| 3 | C-PHASE 145 | Experiment registry v0 (empty initial registry) | No |
| 4 | C-PHASE 147 | Eval gate manifest + held-out eval storage contract | No |
| 5 | C-PHASE 146 | Sprite-repair-batch-proposal dry-run scope (review-only) | No |
| 6 | C-PHASE 148 | Explicit approval type for apply paths (compile-time gate) | No |
| 7 | C-PHASE 149 | Maturity clock scoreboard (evidence-derived only) | No |
| 8 | C-PHASE 150 | Supervised self-improvement pilot (proposal-only by default) | No |

Reordering rationale: The original sequence put eval gate manifest (147)
after the first experiment scope (146). Building 147 first means C-PHASE
146 can write `self_improvement_eval_completed`-shaped packets in their
final shape, instead of needing a retrofit phase later. The order also
keeps the compile-time `UserApplyApproval` contract (148) earlier than
the pilot (150) so the pilot phase only has to wire up the existing type.

## 3. Dependency Graph

```text
143 packet taxonomy        ──> 144, 145, 146, 147, 148, 149, 150
144 constitutional v0      ──> 145, 146, 148, 150
145 experiment registry    ──> 146, 150
147 eval gate manifest     ──> 146, 149, 150
146 sprite-repair scope    ──> 149, 150
148 approval type          ──> 150
149 maturity scoreboard    ──> 150
150 supervised pilot       ──> (terminates this batch)
```

## 4. Per-Phase Spec

Each phase below names: goal, branch, files likely in scope, hard
constraints, stop gates, validation commands, expected report path,
commit message pattern, PR title, Auto-continue.

### 4.1 C-PHASE 143 - Self-improvement packet taxonomy + plain-language explainer rows

- Goal: Add the nine self-improvement packet kinds enumerated in C-PHASE
  142 §"User-Visible Reporting Surface" to
  `PlainLanguageExplainer.KnownPacketKinds`, plus a plain-language
  sentence for each. No producer logic yet. No mutation runner. No model
  call. Code-only registration.
- Branch: `claude-implementation/c-phase-143-self-improvement-packet-taxonomy`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs` (extend known
    kinds + add new explanation strings)
  - `vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs` (cover
    each new kind: known, has explanation, did_* flag interpretation)
  - `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
    (new — string constants only, no behavior)
  - `docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md`
- New packet kinds (must register all nine):
  - `self_improvement_proposal_drafted`
  - `self_improvement_constitutional_reviewed`
  - `self_improvement_dry_run_completed`
  - `self_improvement_eval_completed`
  - `self_improvement_apply_awaiting_approval`
  - `self_improvement_apply_refused`
  - `self_improvement_apply_completed`
  - `self_improvement_rollback_verified`
  - `self_improvement_maturity_clock_reset`
- Hard constraints:
  - No service that writes any of these packets is added in this phase.
  - No capability flag is added or flipped.
  - No model adapter is touched.
  - No new network or filesystem surface.
- Stop gates:
  - Any new packet kind missing from `KnownPacketKinds`.
  - Any new packet kind missing a plain-language sentence.
  - Any packet kind constant referenced by string literal in code rather
    than the new `SelfImprovementPacketKinds` constants.
  - Any test asserting actual production of a self-improvement packet
    (premature; producer phases do that).
- Validation:
  - `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PlainLanguage|SelfImprovement"`
  - `dotnet build .\vnext\Wevito.VNext.sln`
  - `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
  - `git diff --check`
- Expected report: `docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md`
- Commit pattern: `C-PHASE 143: self-improvement packet taxonomy + plain-language rows`
- PR title: `C-PHASE 143: Self-improvement packet taxonomy (taxonomy + explainer rows; no producer)`
- Auto-continue: No

### 4.2 C-PHASE 144 - ConstitutionalDecisionService v0

- Goal: Add a default-deny decision layer that every future
  self-improvement proposal must pass before becoming a review packet.
  No proposal producers, no experiment runners, and no apply paths in
  this phase. The service must be safe to call from C-PHASE 145+ as a
  pure gate.
- Branch: `claude-implementation/c-phase-144-constitutional-decision-service-v0`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionService.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionInput.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionOutcome.cs` (new — `Allowed | Blocked(reason) | NeedsHumanApproval(reason)`)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalRule.cs` (new — interface)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/DefaultDenyRule.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/KillSwitchActiveRule.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeMustBeEnabledRule.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/NoNetworkInScopeRule.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/NoHostedAiRule.cs` (new)
  - `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs` (register service)
  - `vnext/tests/Wevito.VNext.Tests/ConstitutionalDecisionServiceTests.cs` (new)
- Behavior (v0):
  - Default decision is `Blocked("default_deny_no_explicit_allow")`.
  - If KillSwitch active → `Blocked("kill_switch_active")`.
  - If requested scope is not enabled → `Blocked("scope_not_enabled")`.
  - If proposal requests network and the scope has no allowlist entry →
    `Blocked("network_not_allowed_for_scope")`.
  - If proposal requests hosted-AI → `Blocked("hosted_ai_forbidden")`.
  - No proposal kind is allowed yet in v0 (experiment registry is empty
    until C-PHASE 145 and remains empty there). The service must accept
    a `Blocked("no_experiment_kind_registered")` path when registry is
    empty.
  - The service does not write audit packets in this phase. C-PHASE 145
    (registry) is the earliest caller that writes
    `self_improvement_constitutional_reviewed`.
- Hard constraints:
  - Service is pure-function over inputs; no I/O, no model call, no
    network, no file write.
  - Service consults `KillSwitchService.IsActive()`; no other side effects.
  - Service is registered at the composition root with the same DI
    pattern as the existing `AutonomousScopeService`.
- Stop gates:
  - Any rule has an allowlist entry by default.
  - Any rule writes state.
  - Any rule calls a local model adapter or hosted-AI adapter.
  - The service exposes an overload that allows callers to skip rules.
  - Default outcome is anything other than `Blocked`.
- Validation: same five commands as C-PHASE 143, with the filter
  expanded to `"ConstitutionalDecision|PlainLanguage|SelfImprovement"`.
- Expected report: `docs/C_PHASE144_CONSTITUTIONAL_DECISION_SERVICE_V0_2026-05-18.md`
- Commit pattern: `C-PHASE 144: ConstitutionalDecisionService v0 (default-deny)`
- PR title: `C-PHASE 144: ConstitutionalDecisionService v0 (default-deny rules, no producer)`
- Auto-continue: No

### 4.3 C-PHASE 145 - Experiment registry v0 with empty initial registry

- Goal: Add `ExperimentRegistry` and `ExperimentDescriptor` types and the
  per-scope registry plumbing. The registry ships empty (zero registered
  experiment kinds). No proposal producer is started in this phase.
- Branch: `claude-implementation/c-phase-145-experiment-registry-v0`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentDescriptor.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentRegistry.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentKind.cs` (new — record for kind id + scope id + mutation posture)
  - `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs` (register empty registry)
  - `vnext/src/Wevito.VNext.Core/Audit/AuditPackets.cs` (or equivalent) — helper that emits `self_improvement_constitutional_reviewed` packets from rule outcomes
  - `vnext/tests/Wevito.VNext.Tests/ExperimentRegistryTests.cs` (new)
- Behavior (v0):
  - Registry exposes `IReadOnlyList<ExperimentDescriptor> RegisteredKinds`.
  - In this phase the list is empty.
  - Registry exposes `bool TryGetKind(string kindId, out ExperimentDescriptor)` and a `Register` method whose ONLY callers in this phase are test fixtures and the composition root.
  - Calling `ConstitutionalDecisionService` with any kind id while registry is empty must return `Blocked("no_experiment_kind_registered")`.
  - The first review packet written is `self_improvement_constitutional_reviewed` with `did_mutate=false`, `did_use_network=false`, `did_use_hosted_ai=false`, `did_use_local_model=false`.
- Hard constraints:
  - No experiment kind is registered by default.
  - No proposal producer is added.
  - No autonomous loop ticks into this registry.
  - Registry is immutable after composition root build except via tests.
- Stop gates:
  - Any pre-seeded experiment kind in the default registry.
  - Any code path that registers a kind outside of tests + composition root.
  - Any code path that lets `ConstitutionalDecisionService` succeed with an empty registry.
- Validation: same five commands; filter
  `"ExperimentRegistry|ConstitutionalDecision|PlainLanguage|SelfImprovement"`.
- Expected report: `docs/C_PHASE145_EXPERIMENT_REGISTRY_V0_2026-05-18.md`
- Commit pattern: `C-PHASE 145: experiment registry v0 (empty initial registry)`
- PR title: `C-PHASE 145: Experiment registry v0 (empty initial registry; constitutional gate wired)`
- Auto-continue: No

### 4.4 C-PHASE 147 - Eval gate manifest + held-out eval storage contract

(Order note: built before C-PHASE 146 so the first review-only experiment
scope can emit eval-gate-shaped evidence from day one.)

- Goal: Define the eval gate manifest and the held-out eval storage
  contract from C-PHASE 142 §"Eval And Regression Gates". No experiment
  scope consumes it yet — that comes in C-PHASE 146.
- Branch: `claude-implementation/c-phase-147-eval-gate-manifest`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateManifest.cs` (new — record listing the eleven gates)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateResult.cs` (new — `Passed | Failed(reason) | NotApplicable`)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateRunner.cs` (new — orchestrator stub; no actual runs in this phase)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IHeldOutEvalStore.cs` (new — read-only interface; no implementation that exposes content to proposers)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalStore.cs` (new — file-backed, encrypted-at-rest-or-isolated-path implementation; readable only by the gate runner; never returned through any other service)
  - `vnext/tests/Wevito.VNext.Tests/EvalGateManifestTests.cs` (new)
  - `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs` (new — proves no path other than `EvalGateRunner` can read held-out content)
  - `docs/SELF_IMPROVEMENT_EVAL_GATE_MANIFEST_2026-05-18.md` (manifest description, not a phase report)
- Gates listed in the manifest (C-PHASE 142 §"Eval And Regression Gates"):
  Build, Unit tests, Benchmark suite, In-distribution eval, Held-out
  eval, Performance, Scope hash, Dry-run, Backup, Post-proof, Rollback.
- Held-out store contract:
  - `IHeldOutEvalStore` is internal to the eval subsystem; never exposed
    via any tool, dev-control surface, or audit dashboard.
  - Test asserts the store is not returned through `ServiceProvider` to
    any class outside the eval namespace.
  - Held-out content path is gated by `KillSwitchService.IsActive()`; if
    the kill switch is active, the gate runner reports `NotApplicable`
    rather than attempting to read.
- Hard constraints:
  - No actual eval runs are wired yet.
  - No proposal consumes the manifest yet.
  - No held-out content is created or seeded in this phase; the contract
    is the deliverable.
  - No held-out content is logged, summarized, or otherwise exposed to
    any code path outside the gate runner.
- Stop gates:
  - `IHeldOutEvalStore` reachable from `ToolRegistry`, `EvidenceSummaryService`,
    `AutonomousScopeService`, or any shell view.
  - Manifest missing any of the eleven gates.
  - Gate result type allows a value other than `Passed | Failed | NotApplicable`.
- Validation: same five commands; filter expanded with `EvalGate|HeldOutEval`.
- Expected report: `docs/C_PHASE147_EVAL_GATE_MANIFEST_2026-05-18.md`
- Commit pattern: `C-PHASE 147: eval gate manifest + held-out eval storage contract`
- PR title: `C-PHASE 147: Eval gate manifest + held-out eval storage contract (no runs)`
- Auto-continue: No

### 4.5 C-PHASE 146 - Sprite-repair-batch-proposal dry-run scope

- Goal: Register the first experiment kind, `sprite-repair-batch-proposal`,
  in the registry from C-PHASE 145. The scope is review-only: it drafts a
  batch proposal from the existing sprite-repair triage queue, runs all
  applicable eval gates from the manifest in C-PHASE 147 in dry-run mode,
  and writes the three evidence packets:
  `self_improvement_proposal_drafted`,
  `self_improvement_constitutional_reviewed`,
  `self_improvement_dry_run_completed`,
  `self_improvement_eval_completed`. The scope NEVER applies a sprite
  batch; it produces a review packet only.
- Branch: `claude-implementation/c-phase-146-sprite-repair-batch-proposal-dry-run`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalScope.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalDescriptor.cs` (new — registers the kind with `MutationPosture=ReviewOnly`)
  - `vnext/src/Wevito.VNext.Core/AutonomousScopeService.cs` (extend to recognize the new scope; default OFF)
  - `vnext/src/Wevito.VNext.Core/AutonomousScopeRegistry.cs` (extend)
  - `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml` (add scope toggle UI under Autonomy tab; off by default)
  - `vnext/tests/Wevito.VNext.Tests/SpriteRepairBatchProposalScopeTests.cs` (new)
- Behavior (v0):
  - Default OFF.
  - Trigger gates from C-PHASE 142 §"Trigger Conditions" all enforced.
  - When tick fires: read the existing sprite-repair triage queue, draft
    a batch proposal listing target rows (read-only), run applicable
    eval gates in `NotApplicable` mode for any gate that requires apply
    state, and write the four evidence packets above with honest flags
    (`did_mutate=false`, `did_use_local_model=false` unless the user has
    explicitly opted local critique into this scope in a later phase —
    not in 146).
  - Sprite runtime/source PNG files are never written. Test asserts
    sha256 of selected source PNGs before and after a tick is identical.
- Hard constraints:
  - No PNG mutation.
  - No sprite manifest mutation.
  - No batch approval. No `self_improvement_apply_*` packet may be
    written by this scope in v0.
  - No local model call from this scope by default.
- Stop gates:
  - Tick produces a `self_improvement_apply_completed` packet.
  - Sha256 of any sprite source file changes during a tick.
  - Scope toggle defaults to ON.
  - Scope writes a packet kind not registered in C-PHASE 143.
- Validation: same five commands; filter expanded with
  `SpriteRepairBatchProposal|ExperimentRegistry|EvalGate`.
- Expected report: `docs/C_PHASE146_SPRITE_REPAIR_BATCH_PROPOSAL_DRY_RUN_2026-05-18.md`
- Commit pattern: `C-PHASE 146: sprite-repair-batch-proposal dry-run scope (review-only)`
- PR title: `C-PHASE 146: Sprite-repair-batch-proposal dry-run scope (review-only; default off)`
- Auto-continue: No

### 4.6 C-PHASE 148 - Explicit approval type for apply paths

- Goal: Introduce the typed `UserApplyApproval` contract from C-PHASE
  142 §"Explicit Approval Contract". Wire it as a compile-time required
  argument on any future apply API. No apply path is enabled in this
  phase; the type and policy plumbing land first.
- Branch: `claude-implementation/c-phase-148-user-apply-approval-type`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApproval.cs` (new sealed record)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/IRequiresUserApplyApproval.cs` (new marker interface for apply APIs)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApprovalValidator.cs` (new — pure-function refusal of `UserConfirmedInThisMessage=false`, empty `ConfirmationText`, stale `ConfirmedAtUtc`, mismatched `ApprovedScopeId`/`ApprovedOperationId`)
  - `vnext/tests/Wevito.VNext.Tests/UserApplyApprovalValidatorTests.cs` (new)
- Behavior:
  - `UserConfirmedInThisMessage=false` → refuse.
  - Empty or whitespace `ConfirmationText` → refuse.
  - `ConfirmedAtUtc` older than configurable staleness window (default 60s) → refuse.
  - `ApprovedScopeId` or `ApprovedOperationId` mismatch with caller → refuse.
  - Validator returns a typed `ApprovalResult` (`Accepted | Refused(reason)`); no exceptions on refusal.
  - There is no overload anywhere that lets a caller bypass the validator.
- Hard constraints:
  - No production API is moved to require `UserApplyApproval` in this
    phase yet (that happens in C-PHASE 150). Adding the type and validator
    is enough.
  - No autonomous loop, scheduler, scope service, or local model adapter
    may construct a `UserApplyApproval` instance. Test asserts the
    constructor is not reachable from those namespaces.
- Stop gates:
  - Validator has an overload that skips a rule.
  - Any non-user-driven code path constructs `UserApplyApproval`.
  - Refusal pathway throws instead of returning a typed result.
- Validation: same five commands; filter expanded with
  `UserApplyApproval`.
- Expected report: `docs/C_PHASE148_USER_APPLY_APPROVAL_2026-05-18.md`
- Commit pattern: `C-PHASE 148: explicit user-apply-approval type for apply paths`
- PR title: `C-PHASE 148: Explicit user-apply-approval type (no apply paths enabled yet)`
- Auto-continue: No

### 4.7 C-PHASE 149 - Maturity clock scoreboard

- Goal: Add the maturity clock from C-PHASE 142
  §"Promotion Criteria And Maturity Clocks". The scoreboard is derived
  from audit ledger evidence only; it does not flip any capability flag.
- Branch: `claude-implementation/c-phase-149-maturity-clock-scoreboard`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityClock.cs` (new)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityScoreboardService.cs` (new — pure read-only over audit ledger)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityClockResetReason.cs` (new enum)
  - `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml` (extend Evidence tab to render a maturity scoreboard row group)
  - `vnext/tests/Wevito.VNext.Tests/MaturityScoreboardServiceTests.cs` (new)
- Behavior:
  - Clock derives `progress` from clean dry-run evidence, approved apply
    events, rollback drill events, deterministic eval pass events, and
    held-out eval pass events. Each contributes a deterministic +progress
    increment.
  - Clock resets to zero on: invariant violation,
    silent-mutation/network/hosted-AI rows, failed rollback, user
    rejection (when a `self_improvement_apply_refused` packet is present).
  - Scoreboard surface in the Evidence tab is read-only; no button on it
    flips a capability flag.
  - No capability promotion happens automatically from any clock value.
- Hard constraints:
  - Scoreboard does not write to the audit ledger except for one
    `self_improvement_maturity_clock_reset` packet per detected reset.
    All other reads are pure summary.
  - Scoreboard does not call any model adapter.
  - Scoreboard does not include held-out eval content; it only references
    pass/fail counts from gate-result packets.
- Stop gates:
  - Any capability flag flips based on a clock value.
  - Scoreboard exposes held-out eval content.
  - Scoreboard writes packets other than `self_improvement_maturity_clock_reset`.
- Validation: same five commands; filter expanded with
  `MaturityScoreboard|MaturityClock`.
- Expected report: `docs/C_PHASE149_MATURITY_CLOCK_SCOREBOARD_2026-05-18.md`
- Commit pattern: `C-PHASE 149: maturity clock scoreboard (read-only over audit ledger)`
- PR title: `C-PHASE 149: Maturity clock scoreboard (evidence-derived; no promotion automation)`
- Auto-continue: No

### 4.8 C-PHASE 150 - Supervised self-improvement pilot

- Goal: Wire the supervised loop end-to-end as a proposal-only pilot.
  The pilot consumes the experiment registry (145), gates proposals
  through the constitutional service (144), runs the eval gates (147)
  in dry-run mode, and produces a complete `self_improvement_*` packet
  sequence for one experiment kind (`sprite-repair-batch-proposal` from
  146). Apply paths remain gated by `UserApplyApproval` (148) and
  default to refused in v0. The pilot is OFF by default; the user must
  enable it in writing.
- Branch: `claude-implementation/c-phase-150-supervised-self-improvement-pilot`
- Files in scope:
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs` (new — orchestrator over scope, constitutional service, eval gates, approval validator)
  - `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoopSettings.cs` (new — feature flag `supervised_improvement_loop_enabled=false`)
  - `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs` (register loop)
  - `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml` (add a pilot toggle UI under Autonomy → Supervised improvement; default OFF; description: "Proposal-only. Apply still requires explicit approval.")
  - `vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopTests.cs` (new)
  - `vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopSafetyTests.cs` (new — asserts the loop refuses to apply without `UserApplyApproval`, refuses while KillSwitch active, refuses while scope OFF, refuses when registry empty)
- Behavior:
  - Loop runs on the existing autonomous tick cadence, gated by trigger
    conditions, scope toggle, and KillSwitch.
  - Each tick at most produces one proposal evidence packet sequence
    ending at `self_improvement_apply_awaiting_approval`.
  - The loop never constructs a `UserApplyApproval`. Apply paths only
    succeed when the user supplies the typed approval through the
    Tool Popup approval surface (UI wiring lives in this phase; default
    behavior is the surface shows the proposal as "awaiting approval"
    and offers no auto-approve button).
  - Refusals write `self_improvement_apply_refused` with the reason.
- Hard constraints:
  - Default OFF.
  - No apply path runs without a user-supplied `UserApplyApproval`.
  - No hosted-AI call. No silent network call. No silent mutation. No
    silent local model call. (A future phase may opt local critique in
    behind explicit approval; not in v0.)
  - The pilot never auto-approves, auto-merges, or auto-promotes a
    capability.
- Stop gates:
  - Loop applies without `UserApplyApproval`.
  - Loop runs while KillSwitch active.
  - Loop ticks while scope OFF.
  - Loop ticks while pilot toggle OFF.
  - Loop changes any capability flag default.
- Validation: same five commands; filter expanded with
  `SupervisedImprovementLoop|UserApplyApproval|ExperimentRegistry|ConstitutionalDecision|EvalGate|SpriteRepairBatchProposal`.
- Expected report: `docs/C_PHASE150_SUPERVISED_SELF_IMPROVEMENT_PILOT_2026-05-18.md`
- Commit pattern: `C-PHASE 150: supervised self-improvement pilot (proposal-only; default off)`
- PR title: `C-PHASE 150: Supervised self-improvement pilot (proposal-only; default off)`
- Auto-continue: No

## 5. Risk Register

| Risk | Likelihood | Mitigation |
|---|---|---|
| New packet kind ships without explainer row | Low | C-PHASE 143 lands taxonomy first; later phases reuse `SelfImprovementPacketKinds` constants — string literals are a stop gate. |
| Constitutional service grows an allowlist before the user approves one | Medium | Default-deny is enforced by `DefaultDenyRule` and asserted by tests; allowlist entries land only by user-approved phase. |
| Held-out eval content leaks through a tool result | Medium | C-PHASE 147 adds a visibility test that asserts the store is not reachable outside the eval subsystem. |
| Sprite-repair dry-run scope mutates art accidentally | Low | C-PHASE 146 test asserts sha256 of source PNGs is identical before and after a tick. |
| `UserApplyApproval` gets bypassed by a future caller | Medium | C-PHASE 148 forbids constructor reach from autonomous-loop / scheduler / scope / model adapter namespaces, asserted in tests. |
| Maturity clock causes a silent capability flip | Low | C-PHASE 149 forbids capability-flag writes from the scoreboard; only one packet kind allowed (`self_improvement_maturity_clock_reset`). |
| Pilot auto-approves a proposal | Low | C-PHASE 150 forbids `UserApplyApproval` construction inside the loop; refusal pathway is the default. |
| New code adds a network surface | Low | Existing repo-wide tests (no-network policy) cover this; phase prompts also restate the constraint. |
| Pet game FPS degrades during a tick | Low | Trigger gates from C-PHASE 142 §"Trigger Conditions" require user-activity / CPU / RAM checks; C-PHASE 146 and C-PHASE 150 inherit. |
| Codex auto-merges an Auto-continue=No phase | Low | Auto-continue=No on every phase in this batch; loop runner respects that. |

## 6. Milestone Narrative

- After C-PHASE 143: the audit ledger and Evidence dashboard recognize
  every self-improvement packet kind by name, even though no producer
  writes them yet.
- After C-PHASE 144: any caller can ask the constitutional service
  whether a proposal would be allowed; the answer is always `Blocked`
  in v0.
- After C-PHASE 145: the experiment registry exists, but is empty.
  Constitutional decisions over it still always return `Blocked`.
- After C-PHASE 147: the eval gate manifest is canonical and the
  held-out eval store has a visibility contract that prevents leakage.
- After C-PHASE 146: the first review-only experiment kind is
  registered; the scope can be toggled on, runs a tick, and emits the
  four expected proposal packets. Sprite art is unchanged.
- After C-PHASE 148: every future apply API requires a
  `UserApplyApproval` argument. No code path can construct one outside
  user-driven flows.
- After C-PHASE 149: the user can see, on the Evidence tab, how many
  clean evidence events each scope has accrued and whether the clock has
  been reset.
- After C-PHASE 150: the supervised loop runs end-to-end in proposal
  mode. The user can approve an apply path by hand. Nothing in the
  system can approve, merge, or apply on the user's behalf.

## 7. Codex Reading Order

Codex should read these in order before starting any phase in this
batch:

1. `docs/C_PHASE142_SUPERVISED_SELF_IMPROVEMENT_LOOP_DESIGN_2026-05-18.md`
2. `docs/CLAUDE_C_PHASE143_PLUS_PLAN_2026-05-18.md` (this doc)
3. `docs/CLAUDE_C_PHASE143_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-18.md`
4. `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` §4 (hard invariants)
5. `docs/C_PHASE141_EVIDENCE_DASHBOARD_V1_2026-05-18.md`
6. `docs/C_PHASE133_AUTONOMOUS_LOOP_SCOPE_GATE_2026-05-18.md`
7. `docs/BUG_BOARD.md`

The single starter prompt for Codex to begin this batch is in
`docs/CODEX_PROMPT_C_PHASE_143_2026-05-18.md`.
