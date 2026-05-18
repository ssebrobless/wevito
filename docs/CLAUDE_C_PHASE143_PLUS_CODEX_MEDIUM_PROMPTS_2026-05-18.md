# Codex Medium-Effort Prompts - C-PHASE 143+

Date: 2026-05-18
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (medium reasoning effort) + project owner (review)
Paired plan: `docs/CLAUDE_C_PHASE143_PLUS_PLAN_2026-05-18.md`

Each prompt below is a copy-paste ready medium-effort Codex prompt for
one phase in the C-PHASE 143+ batch. The Common Header in §0 applies to
every prompt; do not repeat it inline. Paste the Common Header above the
phase-specific prompt the first time, then paste only the phase-specific
section for each subsequent phase.

## 0. Common Header

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo:
  C:\Users\fishe\Documents\projects\wevito

Pre-flight (run first; halt on any failure):
  1. git status                       # working tree clean
  2. git checkout main; git pull origin main
  3. git rev-parse HEAD               # record for the phase report
  4. dotnet build .\vnext\Wevito.VNext.sln
  5. dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  6. powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  7. git diff --check

Required reading before any code change:
  - docs/C_PHASE142_SUPERVISED_SELF_IMPROVEMENT_LOOP_DESIGN_2026-05-18.md
  - docs/CLAUDE_C_PHASE143_PLUS_PLAN_2026-05-18.md
  - docs/CLAUDE_MASTER_PLAN_2026-05-15.md  §4 (hard invariants)
  - docs/C_PHASE141_EVIDENCE_DASHBOARD_V1_2026-05-18.md
  - docs/C_PHASE133_AUTONOMOUS_LOOP_SCOPE_GATE_2026-05-18.md
  - The previous phase report in this batch, if any.

Hard invariants that apply to every phase in this batch:
  - No hosted AI as Wevito's runtime brain.
  - No silent network access. Only loopback endpoints for local model calls.
  - No silent training. No model weight updates.
  - No silent file, tool, code, or asset mutation.
  - Every mutation pathway requires exact scope + dry run + backup + sha256 evidence + apply + post-proof + rollback + user-visible report.
  - KillSwitchService halts every adapter, scheduler, autonomous loop, runner, scope, experiment, eval runner, and approval handler.
  - AuditLedgerService remains append-only (sqlite UPDATE/DELETE triggers).
  - Every new audit packet kind appears in PlainLanguageExplainer.KnownPacketKinds with a plain-language sentence.
  - Every evidence packet sets did_use_network / did_use_hosted_ai / did_use_local_model / did_mutate honestly.
  - Capability flags introduced by this batch default OFF.
  - Held-out eval data must not be visible to any proposal loop, model prompt, or iterative repair process.
  - Pets remain visually normal pet-sim characters; no AI-task animation overlay.
  - Pet game FPS / user PC experience must stay protected from AI workload.
  - Pet game has 1 GB reserved RAM minimum.
  - The human is the judge in v0. Codex never auto-merges Auto-continue=No phases.

Validation discipline:
  - Run the validation command set named in the per-phase prompt before opening the PR.
  - Open the PR in Draft and write a `codex_phase_blocked` audit row if ANY stop gate listed in the per-phase prompt is TRUE.
  - Write the phase report doc at the path named in the per-phase prompt.
  - Append one row to docs/codex-phase-history.jsonl with phase_id, timestamp_utc, branch, pr_url.

Auto-continue is NO for every phase in this batch. Halt for user review after opening each PR.
```

## 1. C-PHASE 143 - Self-improvement packet taxonomy

```text
[C-PHASE 143]

Goal: Add the nine self-improvement packet kinds enumerated in C-PHASE 142
to PlainLanguageExplainer.KnownPacketKinds plus a plain-language sentence
for each. No producer logic, no model call, no mutation runner, no
capability flag in this phase.

Branch:
  claude-implementation/c-phase-143-self-improvement-packet-taxonomy

Files in scope:
  vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs           (extend)
  vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs (new)
  vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs   (extend)
  docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md  (new)

Packet kinds to register (all nine; spelled exactly):
  self_improvement_proposal_drafted
  self_improvement_constitutional_reviewed
  self_improvement_dry_run_completed
  self_improvement_eval_completed
  self_improvement_apply_awaiting_approval
  self_improvement_apply_refused
  self_improvement_apply_completed
  self_improvement_rollback_verified
  self_improvement_maturity_clock_reset

Place constants in SelfImprovementPacketKinds. Any future phase that
references one of these kinds must use the constant, never a string
literal.

Hard constraints:
  - No service that writes any of these packets is added in this phase.
  - No capability flag added or flipped.
  - No model adapter touched.
  - No new network or filesystem surface.

Stop gates (HALT and open PR as Draft if any is TRUE):
  - Any of the nine kinds missing from KnownPacketKinds.
  - Any of the nine kinds missing a plain-language sentence.
  - Any test asserting actual production of a self-improvement packet.
  - Any phase code references a packet kind by string literal instead of
    the new SelfImprovementPacketKinds constants.

Validation (from repo root):
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md
Commit: "C-PHASE 143: self-improvement packet taxonomy + plain-language rows"
PR title: "C-PHASE 143: Self-improvement packet taxonomy (taxonomy + explainer rows; no producer)"
Auto-continue: No

When done, halt for user review.
```

## 2. C-PHASE 144 - ConstitutionalDecisionService v0

```text
[C-PHASE 144]

Goal: Add the default-deny ConstitutionalDecisionService that every
future self-improvement proposal must pass before becoming a review
packet. No proposal producers, no experiment runners, no apply paths in
this phase. The service must be safe to call from C-PHASE 145+ as a pure
gate.

Branch:
  claude-implementation/c-phase-144-constitutional-decision-service-v0

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionService.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionInput.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalDecisionOutcome.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalRule.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/DefaultDenyRule.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/KillSwitchActiveRule.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ScopeMustBeEnabledRule.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/NoNetworkInScopeRule.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/NoHostedAiRule.cs
  vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs               (extend; register service)
  vnext/tests/Wevito.VNext.Tests/ConstitutionalDecisionServiceTests.cs

ConstitutionalDecisionOutcome shape:
  abstract record ConstitutionalDecisionOutcome
  {
    sealed record Allowed() : ConstitutionalDecisionOutcome;
    sealed record Blocked(string Reason) : ConstitutionalDecisionOutcome;
    sealed record NeedsHumanApproval(string Reason) : ConstitutionalDecisionOutcome;
  }

Rule order (first match wins):
  KillSwitchActiveRule  -> Blocked("kill_switch_active")
  ScopeMustBeEnabledRule -> Blocked("scope_not_enabled")
  NoHostedAiRule         -> Blocked("hosted_ai_forbidden")
  NoNetworkInScopeRule   -> Blocked("network_not_allowed_for_scope")
  DefaultDenyRule        -> Blocked("default_deny_no_explicit_allow")

Behavior:
  - Pure function over inputs; no I/O, no model call, no network, no file write.
  - Consults KillSwitchService.IsActive() only.
  - Service does NOT write audit packets in this phase. The first
    self_improvement_constitutional_reviewed packet writer lands in
    C-PHASE 145.
  - With an empty experiment registry (the v0 baseline), the service must
    map "registry is empty" to Blocked("no_experiment_kind_registered").
    Since registry plumbing lands in C-PHASE 145, in this phase model the
    empty-registry case as an explicit Input flag the test exercises.
  - Default outcome MUST be Blocked.

Hard constraints:
  - No rule has an allowlist entry by default.
  - No rule writes state.
  - No rule calls a local-model or hosted-AI adapter.
  - The service exposes no overload that allows callers to skip rules.

Stop gates:
  - Any rule has an allowlist entry.
  - Any rule writes state.
  - Any rule reaches a model adapter.
  - Default outcome is anything other than Blocked.

Validation (from repo root):
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ConstitutionalDecision|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE144_CONSTITUTIONAL_DECISION_SERVICE_V0_2026-05-18.md
Commit: "C-PHASE 144: ConstitutionalDecisionService v0 (default-deny)"
PR title: "C-PHASE 144: ConstitutionalDecisionService v0 (default-deny rules, no producer)"
Auto-continue: No

When done, halt for user review.
```

## 3. C-PHASE 145 - Experiment registry v0 (empty)

```text
[C-PHASE 145]

Goal: Add ExperimentRegistry, ExperimentDescriptor, and ExperimentKind
types. The registry ships empty. Hook the registry into
ConstitutionalDecisionService so it returns
Blocked("no_experiment_kind_registered") when no kind is registered, and
emit the first self_improvement_constitutional_reviewed audit packet
from a small `ConstitutionalReviewedEmitter` helper.

Branch:
  claude-implementation/c-phase-145-experiment-registry-v0

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentKind.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentDescriptor.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ExperimentRegistry.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ConstitutionalReviewedEmitter.cs
  vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs              (extend; register empty registry)
  vnext/tests/Wevito.VNext.Tests/ExperimentRegistryTests.cs
  vnext/tests/Wevito.VNext.Tests/ConstitutionalReviewedEmitterTests.cs

Behavior:
  - ExperimentRegistry.RegisteredKinds returns IReadOnlyList<ExperimentDescriptor>.
  - In this phase, RegisteredKinds is empty.
  - Register method only callable from tests + composition root.
  - ConstitutionalDecisionService consults registry: if empty,
    Blocked("no_experiment_kind_registered").
  - ConstitutionalReviewedEmitter writes one
    self_improvement_constitutional_reviewed audit packet per
    decision, using the constant from SelfImprovementPacketKinds, with
    flags did_mutate=false, did_use_network=false,
    did_use_hosted_ai=false, did_use_local_model=false.

Hard constraints:
  - No experiment kind is pre-seeded in the default registry.
  - No proposal producer exists yet.
  - No autonomous loop ticks into the registry.
  - Registry is immutable after composition-root build except via tests.

Stop gates:
  - Any pre-seeded kind in the default registry.
  - Any code path registers a kind outside tests + composition root.
  - ConstitutionalDecisionService returns Allowed while registry is empty.
  - Emitter writes any flag dishonestly.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ExperimentRegistry|ConstitutionalDecision|ConstitutionalReviewedEmitter|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE145_EXPERIMENT_REGISTRY_V0_2026-05-18.md
Commit: "C-PHASE 145: experiment registry v0 (empty initial registry)"
PR title: "C-PHASE 145: Experiment registry v0 (empty initial registry; constitutional gate wired)"
Auto-continue: No

When done, halt for user review.
```

## 4. C-PHASE 147 - Eval gate manifest + held-out eval storage contract

```text
[C-PHASE 147]

Goal: Define the eleven-gate eval manifest from C-PHASE 142 and the
held-out eval storage contract. No actual eval runs in this phase. No
held-out content is seeded.

Branch:
  claude-implementation/c-phase-147-eval-gate-manifest

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateManifest.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateResult.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/EvalGateRunner.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/IHeldOutEvalStore.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Eval/HeldOutEvalStore.cs
  vnext/tests/Wevito.VNext.Tests/EvalGateManifestTests.cs
  vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs
  docs/SELF_IMPROVEMENT_EVAL_GATE_MANIFEST_2026-05-18.md            (manifest description)

Gates listed in the manifest (all eleven, in this order):
  Build
  UnitTests
  BenchmarkSuite
  InDistributionEval
  HeldOutEval
  Performance
  ScopeHash
  DryRun
  Backup
  PostProof
  Rollback

EvalGateResult shape:
  abstract record EvalGateResult
  {
    sealed record Passed() : EvalGateResult;
    sealed record Failed(string Reason) : EvalGateResult;
    sealed record NotApplicable(string Reason) : EvalGateResult;
  }

Held-out store contract:
  - IHeldOutEvalStore is internal to the Eval namespace.
  - HeldOutEvalStore is registered only as IHeldOutEvalStore.
  - HeldOutEvalStoreVisibilityTests must prove via reflection that no
    class outside the Eval namespace can resolve IHeldOutEvalStore from
    the configured ServiceProvider, AND that no other public class
    declares a constructor parameter typed IHeldOutEvalStore.
  - If KillSwitchService.IsActive() is true, EvalGateRunner returns
    NotApplicable("kill_switch_active") for the HeldOutEval gate without
    touching the store.

Hard constraints:
  - No held-out content is created or seeded in this phase.
  - No held-out content is logged, summarized, or exposed outside Eval.
  - No proposal consumes the manifest in this phase.
  - Manifest enumerates the eleven gates exactly (no more, no fewer).

Stop gates:
  - IHeldOutEvalStore reachable outside Eval namespace.
  - Manifest missing any gate.
  - EvalGateResult permits a value other than Passed | Failed | NotApplicable.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "EvalGate|HeldOutEval|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE147_EVAL_GATE_MANIFEST_2026-05-18.md
Commit: "C-PHASE 147: eval gate manifest + held-out eval storage contract"
PR title: "C-PHASE 147: Eval gate manifest + held-out eval storage contract (no runs)"
Auto-continue: No

When done, halt for user review.
```

## 5. C-PHASE 146 - Sprite-repair-batch-proposal dry-run scope

```text
[C-PHASE 146]

Goal: Register the first experiment kind, sprite-repair-batch-proposal,
in the registry. The scope is review-only. A tick reads the existing
sprite-repair triage queue, drafts a batch proposal, runs applicable
eval gates from the C-PHASE 147 manifest in dry-run mode, and writes the
four expected evidence packets. Sprite art is never mutated.

Branch:
  claude-implementation/c-phase-146-sprite-repair-batch-proposal-dry-run

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalDescriptor.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalScope.cs
  vnext/src/Wevito.VNext.Core/AutonomousScopeService.cs             (extend; register new scope, default OFF)
  vnext/src/Wevito.VNext.Core/AutonomousScopeRegistry.cs            (extend)
  vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml                 (add Autonomy → "Sprite-repair batch proposal (dry-run)" toggle; default OFF; description must explicitly say "Review only. No sprite mutation. No apply.")
  vnext/tests/Wevito.VNext.Tests/SpriteRepairBatchProposalScopeTests.cs

Behavior:
  - Default OFF.
  - Trigger gates from C-PHASE 142 §"Trigger Conditions" all enforced.
  - One tick produces, in order, exactly these four packets:
      self_improvement_proposal_drafted
      self_improvement_constitutional_reviewed
      self_improvement_dry_run_completed
      self_improvement_eval_completed
  - Each packet sets did_mutate=false, did_use_network=false,
    did_use_hosted_ai=false, did_use_local_model=false.
  - Eval gates that require apply state (Backup, PostProof, Rollback)
    return NotApplicable("review_only_v0").
  - Test asserts sha256 of every source PNG referenced by the drafted
    proposal is identical before and after a tick (no mutation).

Hard constraints:
  - No PNG mutation.
  - No sprite manifest mutation.
  - No batch approval. No self_improvement_apply_* packet written by
    this scope in v0.
  - No local model call from this scope by default.
  - All packet kinds used must be the constants from
    SelfImprovementPacketKinds; no string literals.

Stop gates:
  - Tick writes any self_improvement_apply_* packet.
  - Sha256 of any sprite source file changes during a tick.
  - Scope toggle defaults to ON.
  - Scope writes a packet kind not registered in C-PHASE 143.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SpriteRepairBatchProposal|ExperimentRegistry|ConstitutionalDecision|EvalGate|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE146_SPRITE_REPAIR_BATCH_PROPOSAL_DRY_RUN_2026-05-18.md
Commit: "C-PHASE 146: sprite-repair-batch-proposal dry-run scope (review-only)"
PR title: "C-PHASE 146: Sprite-repair-batch-proposal dry-run scope (review-only; default off)"
Auto-continue: No

When done, halt for user review.
```

## 6. C-PHASE 148 - Explicit approval type for apply paths

```text
[C-PHASE 148]

Goal: Introduce the typed UserApplyApproval contract from C-PHASE 142,
add the validator, and forbid construction from any non-user-driven code
path. No apply path is moved to require the type in this phase. Wiring
happens in C-PHASE 150.

Branch:
  claude-implementation/c-phase-148-user-apply-approval-type

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApproval.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/IRequiresUserApplyApproval.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/UserApplyApprovalValidator.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/ApprovalResult.cs
  vnext/tests/Wevito.VNext.Tests/UserApplyApprovalValidatorTests.cs

UserApplyApproval shape:
  public sealed record UserApplyApproval(
      bool UserConfirmedInThisMessage,
      string ConfirmationText,
      DateTimeOffset ConfirmedAtUtc,
      string ApprovedScopeId,
      string ApprovedOperationId);

ApprovalResult shape:
  abstract record ApprovalResult
  {
    sealed record Accepted() : ApprovalResult;
    sealed record Refused(string Reason) : ApprovalResult;
  }

Validator rules:
  - UserConfirmedInThisMessage=false  -> Refused("not_confirmed_in_this_message")
  - Whitespace ConfirmationText        -> Refused("empty_confirmation_text")
  - ConfirmedAtUtc older than 60s      -> Refused("stale_confirmation")
  - ApprovedScopeId mismatch           -> Refused("scope_id_mismatch")
  - ApprovedOperationId mismatch       -> Refused("operation_id_mismatch")
  - Otherwise                          -> Accepted

Hard constraints:
  - No overload bypasses the validator.
  - No autonomous loop / scheduler / scope service / model adapter
    namespace may construct UserApplyApproval. Add a test that uses
    reflection to assert that no public constructor parameter or method
    signature in those namespaces returns or constructs UserApplyApproval.
  - Refusal pathway returns ApprovalResult; never throws.

Stop gates:
  - Validator exposes a bypass overload.
  - Any non-user-driven namespace constructs UserApplyApproval.
  - Refusal pathway throws.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "UserApplyApproval|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE148_USER_APPLY_APPROVAL_2026-05-18.md
Commit: "C-PHASE 148: explicit user-apply-approval type for apply paths"
PR title: "C-PHASE 148: Explicit user-apply-approval type (no apply paths enabled yet)"
Auto-continue: No

When done, halt for user review.
```

## 7. C-PHASE 149 - Maturity clock scoreboard

```text
[C-PHASE 149]

Goal: Add the maturity clock and a read-only Evidence-tab scoreboard
derived from the audit ledger. No capability flag flips automatically
from any clock value.

Branch:
  claude-implementation/c-phase-149-maturity-clock-scoreboard

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityClock.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityScoreboardService.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/Maturity/MaturityClockResetReason.cs
  vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml                 (extend Evidence tab with a "Maturity" row group; read-only)
  vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs              (wire data binding)
  vnext/tests/Wevito.VNext.Tests/MaturityScoreboardServiceTests.cs

MaturityClock progress sources (each contributes +1 increment per packet):
  self_improvement_dry_run_completed
  self_improvement_apply_completed
  self_improvement_rollback_verified
  self_improvement_eval_completed (only when EvalGateRunner reports all
    applicable gates Passed; not NotApplicable)

MaturityClock reset reasons:
  InvariantViolation
  SilentMutationDetected
  SilentNetworkDetected
  SilentHostedAiDetected
  FailedRollback
  UserRejectedProposal     (presence of self_improvement_apply_refused
                            with reason=user_rejection)

Behavior:
  - Pure read over the audit ledger; uses the same read-only access
    pattern as EvidenceSummaryService.
  - Scoreboard surface is read-only; no button on it flips a capability
    flag.
  - On detecting a reset condition, the service writes ONE
    self_improvement_maturity_clock_reset packet per detected reset
    (constant from SelfImprovementPacketKinds). No other packet kind
    written by this service.

Hard constraints:
  - No capability flag is changed by this service.
  - Scoreboard does NOT include held-out eval content.
  - No model adapter call.
  - Scoreboard never claims an unknown packet kind as progress; unknown
    kinds remain not-claimed.

Stop gates:
  - Any capability flag flips based on a clock value.
  - Scoreboard exposes held-out eval content.
  - Service writes a packet kind other than self_improvement_maturity_clock_reset.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "MaturityScoreboard|MaturityClock|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE149_MATURITY_CLOCK_SCOREBOARD_2026-05-18.md
Commit: "C-PHASE 149: maturity clock scoreboard (read-only over audit ledger)"
PR title: "C-PHASE 149: Maturity clock scoreboard (evidence-derived; no promotion automation)"
Auto-continue: No

When done, halt for user review.
```

## 8. C-PHASE 150 - Supervised self-improvement pilot

```text
[C-PHASE 150]

Goal: Wire the supervised loop end-to-end as a proposal-only pilot.
Default OFF. Apply paths require user-supplied UserApplyApproval.
Nothing in the system applies, approves, merges, or promotes without
the user.

Branch:
  claude-implementation/c-phase-150-supervised-self-improvement-pilot

Files in scope (new unless marked):
  vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs
  vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoopSettings.cs
  vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs               (extend; register loop)
  vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml                 (extend Autonomy tab: "Supervised improvement (pilot)" toggle; default OFF; description must say "Proposal-only. Apply still requires explicit approval.")
  vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs              (wire pilot toggle + the approval-acceptance surface)
  vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopTests.cs
  vnext/tests/Wevito.VNext.Tests/SupervisedImprovementLoopSafetyTests.cs

Loop behavior:
  - Default OFF.
  - Settings key: supervised_improvement_loop_enabled=false.
  - Runs on the existing autonomous tick cadence.
  - Each tick at most produces one proposal sequence ending at
    self_improvement_apply_awaiting_approval.
  - Refusals write self_improvement_apply_refused with a reason.
  - Loop NEVER constructs UserApplyApproval. The Tool Popup approval
    surface is the only constructor caller. Wevito itself, including the
    loop, scope, or any service, MUST NOT.

Approval surface (Tool Popup):
  - Render any open self_improvement_apply_awaiting_approval proposal as
    a card.
  - Card has an explicit "Approve apply" button that:
      - Requires the user to type the operation id verbatim into a confirm
        field.
      - Constructs UserApplyApproval with UserConfirmedInThisMessage=true.
      - Passes it to UserApplyApprovalValidator.
      - On Accepted, hands off to a future apply runner (not implemented
        in this phase; route to a NotImplementedException-equivalent stub
        that writes self_improvement_apply_refused with reason
        "apply_runner_not_implemented_in_v0").
      - On Refused, writes self_improvement_apply_refused.
  - No "auto-approve", "bulk approve", or "trust this scope" button.

Hard constraints:
  - Default OFF.
  - No apply path runs without user-supplied UserApplyApproval.
  - No hosted-AI call. No network call. No silent local model call.
  - The pilot never auto-approves, auto-merges, or auto-promotes.

Stop gates:
  - Loop applies without UserApplyApproval.
  - Loop ticks while KillSwitch is active.
  - Loop ticks while scope OFF.
  - Loop ticks while pilot toggle OFF.
  - Loop changes any capability flag default.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "SupervisedImprovementLoop|UserApplyApproval|ExperimentRegistry|ConstitutionalDecision|EvalGate|SpriteRepairBatchProposal|MaturityScoreboard|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Phase report: docs/C_PHASE150_SUPERVISED_SELF_IMPROVEMENT_PILOT_2026-05-18.md
Commit: "C-PHASE 150: supervised self-improvement pilot (proposal-only; default off)"
PR title: "C-PHASE 150: Supervised self-improvement pilot (proposal-only; default off)"
Auto-continue: No

When done, halt for user review.
```

## Appendix A - Per-Phase Reading List

| Phase | Required reading (in addition to Common Header set) |
|---|---|
| 143 | EvidenceDashboard report C_PHASE141 |
| 144 | C-PHASE 143 phase report once written |
| 145 | C-PHASE 143/144 phase reports |
| 147 | C-PHASE 142 §"Eval And Regression Gates" (re-read) |
| 146 | C-PHASE 143/144/145/147 phase reports + C-PHASE 128 sprite-repair queue report |
| 148 | C-PHASE 142 §"Explicit Approval Contract" (re-read) |
| 149 | C-PHASE 141 evidence-tab report (re-read) |
| 150 | All prior C-PHASE 143+ phase reports |
