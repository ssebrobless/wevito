# C-PHASE 142 - Supervised Self-Improvement Loop Design

Date: 2026-05-18
Branch: `claude-implementation/c-phase-142-self-improvement-design`

## Goal

Define the next safe shape for Wevito's supervised self-improvement loop without adding code, flags, tests, model calls, or mutation pathways in this phase.

This document is the blueprint for a future implementation batch. It assumes Wevito remains a local-first AI assistant whose pet simulator is the cosmetic surface, not the runtime brain. It also assumes every risky capability keeps the existing guardrail posture: default-deny policy, KillSwitch, append-only audit ledger, user-visible evidence, and explicit approval before mutation.

## Current Building Blocks

```text
╔════════════════════════════════════════════════════════════════════╗
║                  Supervised Self-Improvement v0                  ║
╠════════════════════════════════════════════════════════════════════╣
║ Inputs already present                                            ║
║ ├─ AutonomousScopeService: registered scopes, default off          ║
║ ├─ sprite-repair-triage: review-only repair card proposals         ║
║ ├─ audit-ledger-cleanup: dry-run/move-only cleanup proof           ║
║ ├─ LocalDocumentRetrievalService: approved local corpus retrieval  ║
║ ├─ EvidenceSummaryService: did / did not / why rollups             ║
║ ├─ KillSwitchService: global halt surface                          ║
║ └─ AuditLedgerService: append-only proof trail                     ║
╠════════════════════════════════════════════════════════════════════╣
║ v0 design constraint                                              ║
║ └─ Wevito may propose and critique. A human remains the judge.     ║
╚════════════════════════════════════════════════════════════════════╝
```

Relevant decision IDs:

- `I-Reframe`: Wevito is a local AI assistant; pet simulator is the cosmetic wrapper.
- `S-B5`: Procedural cleanup plus Wevito-mediated local AI, with dry-run, backup, rollback, eval gate, and KillSwitch.
- `H4+`: Bounded autonomy with sandbox layers; not unbounded judgment.
- `R3`: Approved-allowlist web research starts empty and user-approved.
- `M5`: Maturity promotion depends on multi-axis evidence and resets on invariant violation.
- `W-C3`: Capabilities can be code-active, but input-controlled by empty initial state.
- `Q15-L2`: Qwen 2.5 7B Q4_K_M is the default local reasoning model through Ollama.
- `Q17-V1-Suite` and `Q17-Grader-Triad`: deterministic eval is authoritative; model self-grade is diagnostic only.

## Loop Shape

```text
┌──────────────┐
│ Trigger Gate │
└──────┬───────┘
       ▼
┌──────────────────────┐
│ Propose Experiment   │  local-only, approved scope, evidence-backed
└──────┬───────────────┘
       ▼
┌──────────────────────┐
│ Critique / Revise    │  constitutional checklist + local evidence
└──────┬───────────────┘
       ▼
┌──────────────────────┐
│ Dry-Run Proof Packet │  exact scope, no hidden mutation
└──────┬───────────────┘
       ▼
┌──────────────────────┐
│ Eval Gates           │  existing eval + held-out eval + performance
└──────┬───────────────┘
       ▼
┌──────────────────────┐
│ Human Review         │  explicit typed approval required
└──────┬───────────────┘
       ▼
┌──────────────────────┐
│ Apply If Approved    │  backup + sha256 + post-proof + rollback
└──────┬───────────────┘
       ▼
┌──────────────────────┐
│ Evidence Dashboard   │  did / did not / why + maturity clock updates
└──────────────────────┘
```

The v0 loop is not autonomous self-modification. It is supervised self-revision:

1. Propose a bounded change.
2. Critique it against Wevito's constitution and evidence.
3. Revise the proposal until it is safe enough for review.
4. Produce a dry-run proof packet.
5. Wait for explicit human approval before any apply path.

This uses the supervised self-revision half of Constitutional AI. It does not use RL from self-feedback, policy training, reward-model training, or automated self-approval.

## Trigger Conditions

Every supervised self-improvement attempt must pass all trigger gates:

| Gate | Required State |
|---|---|
| User-approved scope | Scope is registered and explicitly enabled by the user. |
| Time window | Current time is inside the scope's allowed schedule. |
| KillSwitch | `KillSwitchService.IsActive()` is false before every step. |
| Pause / quiet / pet-only mode | No proposal or runner may interrupt these modes. |
| User activity | No fullscreen game or protected foreground application is active. |
| CPU pressure | Non-Wevito CPU pressure is below the configured threshold. |
| RAM pressure | Pet game reserved floor remains protected; AI work cannot borrow below it. |
| Network policy | Network is disabled unless the exact scope has explicit allowlist entries. |
| Local model availability | If local model absent, degrade to deterministic proposal scaffolding only. |
| Evidence ledger | Audit ledger is writable before the attempt begins. |

If any trigger gate fails, Wevito writes a refusal/blocked evidence packet and does nothing else.

## Allowed Initial Experiment Kinds

v0 starts with a tiny, enumerated experiment registry. Empty registry means no experiments run.

| Experiment Kind | v0 Status | Mutation? | Purpose |
|---|---:|---:|---|
| `sprite-repair-batch-proposal` | Seed | No | Draft a review-only batch from existing sprite triage findings. |
| `audit-ledger-cleanup-proposal` | Seed | No | Propose cleanup candidates, preserving dry-run and rollback rules. |
| `local-doc-summary-improvement` | Seed | No | Improve local-doc answer packets using retrieved local evidence only. |
| `tool-help-copy-proposal` | Optional | No | Suggest clearer UI/help copy based on evidence dashboard confusion. |
| `benchmark-case-draft` | Optional | No | Draft benchmark cases for user review, never self-promote them. |

Explicitly disallowed in v0:

- Code mutation.
- Runtime/source PNG mutation.
- Capability flag changes.
- Web research expansion.
- Model weight updates.
- Training data promotion without review.
- Auto-merge.
- Automated judge.

## Constitutional Decision Service Shape

The future implementation should introduce a constitutional decision layer that every self-improvement proposal must pass before it can become a review packet.

```text
╔════════════════════════════════════╗
║ ConstitutionalDecisionService      ║
╠════════════════════════════════════╣
║ Inputs                             ║
║ ├─ requested experiment kind       ║
║ ├─ scope id + enabled state        ║
║ ├─ user confirmation state         ║
║ ├─ resource budget snapshot        ║
║ ├─ audit/evidence summary          ║
║ ├─ proposed mutation scope         ║
║ └─ current KillSwitch state        ║
╠════════════════════════════════════╣
║ Rules                              ║
║ ├─ default deny                    ║
║ ├─ denylist beats allowlist        ║
║ ├─ no implicit approval            ║
║ ├─ no hidden network/model/mutate  ║
║ └─ no state write while killed     ║
╠════════════════════════════════════╣
║ Output                             ║
║ ├─ Allowed                         ║
║ ├─ Blocked(reason)                 ║
║ └─ NeedsHumanApproval(reason)      ║
╚════════════════════════════════════╝
```

The service must be default-deny. It should make disallowed actions impossible by type where feasible, and refused by policy everywhere else.

## Explicit Approval Contract

Every apply path must require a typed or equivalent explicit approval argument:

```csharp
public sealed record UserApplyApproval(
    bool UserConfirmedInThisMessage,
    string ConfirmationText,
    DateTimeOffset ConfirmedAtUtc,
    string ApprovedScopeId,
    string ApprovedOperationId);
```

Rules:

- `UserConfirmedInThisMessage=false` always refuses.
- Empty or stale confirmation always refuses.
- Approval must name the exact operation or scope.
- Approval cannot be inferred from prior conversation, button hover, task-card existence, or successful dry-run.
- Approval cannot be supplied by Wevito itself, a local model, an autonomous scope, or a scheduler.
- If feasible, mutation APIs should not expose overloads that omit the approval type.

This follows the explicit-confirmation pattern seen in the BerriAI `self-improving-agent` repository, but Wevito applies it to local-first desktop mutation gates instead of hosted-agent workflows.

## Eval And Regression Gates

No proposal may reach apply-review without an evidence packet containing all relevant gates:

| Gate | Required Evidence |
|---|---|
| Build | Current relevant build command passes or is marked not applicable. |
| Unit tests | Relevant unit tests pass. |
| Benchmark suite | Deterministic benchmark subset passes. |
| In-distribution eval | Existing eval set has no regression. |
| Held-out eval | Sealed eval set has no regression and remains unseen during iteration. |
| Performance | Pet FPS/user-experience budget remains protected. |
| Scope hash | Target files/records have pre-change sha256 or equivalent identity proof. |
| Dry-run | Exact planned mutation set is listed before apply. |
| Backup | Backup path and hashes are recorded before apply. |
| Post-proof | Post-apply validation passes. |
| Rollback | Rollback path is verified and documented. |

In-distribution eval and held-out eval are separate gates. In-distribution eval checks continuity on known cases. Held-out eval catches silent overfitting or correctness loss that a proposal may learn to pass on the visible set.

The held-out eval set must not be shown to the proposing loop, local model prompt, or iterative repair process. Only the gate runner sees it at the end.

## Promotion Criteria And Maturity Clocks

Each capability has its own maturity clock. A clock measures clean evidence over time, not optimism.

```text
Clock starts at 0
   │
   ├─ clean dry-run evidence              ──▶ +progress
   ├─ user approval + successful apply    ──▶ +progress
   ├─ rollback drill succeeds             ──▶ +progress
   ├─ deterministic eval passes           ──▶ +progress
   ├─ held-out eval passes                ──▶ +progress
   │
   ├─ invariant violation                 ──▶ reset to 0
   ├─ silent mutation/network/model use   ──▶ reset to 0 + block
   ├─ failed rollback                     ──▶ reset to 0 + block
   └─ user rejects proposal as unsafe      ──▶ reset or pause by scope
```

Promotion to higher autonomy requires:

- Zero invariant violations in the current maturity window.
- Evidence dashboard shows no unexplained mutation/network/hosted-AI rows.
- Held-out eval stays green.
- User-visible reports remain understandable.
- User explicitly approves the promotion.

Any invariant violation resets the relevant capability clock to zero. Severe violations also pause the scope and require a user review before the scope can be enabled again.

## User-Visible Reporting Surface

C-PHASE 141's Evidence dashboard is the front door for self-improvement transparency.

Future self-improvement evidence packets should be rollup-friendly:

- `self_improvement_proposal_drafted`
- `self_improvement_constitutional_reviewed`
- `self_improvement_dry_run_completed`
- `self_improvement_eval_completed`
- `self_improvement_apply_awaiting_approval`
- `self_improvement_apply_refused`
- `self_improvement_apply_completed`
- `self_improvement_rollback_verified`
- `self_improvement_maturity_clock_reset`

Every packet must set:

- `did_use_network`
- `did_use_hosted_ai`
- `did_use_local_model`
- `did_mutate`

The dashboard should explain unknown packet kinds as "not claimed yet", never as successful work.

## v1+ Triad Shape

The Multi-Agent Evolve Proposer / Solver / Judge triad is useful later, but not in v0.

```text
v0:
┌──────────┐     ┌──────────┐     ┌─────────────┐
│ Proposer │ ──▶ │ Critique │ ──▶ │ Human Judge │
└──────────┘     └──────────┘     └─────────────┘

v1+ candidate:
┌──────────┐     ┌────────┐     ┌───────────────┐     ┌─────────────┐
│ Proposer │ ──▶ │ Solver │ ──▶ │ Local Judge   │ ──▶ │ Human Judge │
└──────────┘     └────────┘     └───────────────┘     └─────────────┘
```

In v1+, one local LLM could play Proposer, Solver, and Judge sequentially with different prompts and evidence packets. Even then, the local Judge is advisory. The deterministic eval gate and human approval remain authoritative.

## Non-Goals For v0

- No hosted AI consultation.
- No automatic merge.
- No capability flag self-flip.
- No model weight updates.
- No LoRA or fine-tuning.
- No automated Judge.
- No implicit approval shortcuts.
- No broad web browsing.
- No hidden local file access.
- No asset mutation from a proposal-only scope.
- No pet overlay that makes pets appear to "do AI work" visually.

## Follow-Up Implementation Batch

Recommended next phases after this design:

| Phase | Purpose | Mutation Posture |
|---|---|---|
| C-PHASE 143 | Self-improvement packet taxonomy and plain-language explainer rows | Code only; no mutation runner |
| C-PHASE 144 | ConstitutionalDecisionService v0 | Default-deny policy only |
| C-PHASE 145 | Experiment registry v0 with empty initial registry | No experiments enabled |
| C-PHASE 146 | Sprite-repair-batch-proposal dry-run scope | Review-only |
| C-PHASE 147 | Eval gate manifest and held-out eval storage contract | No hidden eval exposure |
| C-PHASE 148 | Explicit approval type for apply paths | Compile-time approval requirement |
| C-PHASE 149 | Maturity clock scoreboard | Evidence-derived only |
| C-PHASE 150 | Supervised self-improvement pilot | Proposal-only unless explicitly approved |

Each phase should be one PR, no auto-merge by Codex, with validation and a phase report.

## References

- Bai et al., "Constitutional AI: Harmlessness from AI Feedback" — https://arxiv.org/abs/2212.08073
- NVIDIA NeMo Framework, "Constitutional AI" overview — https://docs.nvidia.com/nemo-framework/user-guide/24.09/modelalignment/cai.html
- BerriAI, `self-improving-agent` repository — https://github.com/BerriAI/self-improving-agent
- Multi-Agent Evolve, "Multi-Agent Evolve: A Proposer-Solver-Judge Framework for Self-Improving Agents" — https://arxiv.org/pdf/2510.23595
- LangChain, "Agent Improvement Loop Starts with a Trace" — https://www.langchain.com/blog/traces-start-agent-improvement-loop

## Stop-Gate Confirmation

- [x] No code changed.
- [x] No tests changed or added.
- [x] No runtime/source sprites changed.
- [x] No content JSON changed.
- [x] No capability flags changed.
- [x] No model calls made.
- [x] No network calls made by Wevito runtime.
- [x] No mutation pathway added.

## Next Step

Use this document to ask Claude for a concrete C-PHASE 143+ implementation plan. The plan should preserve the v0 boundary: supervised proposals first, human judge always, deterministic and held-out eval gates before any apply path, and no hosted AI runtime brain.
