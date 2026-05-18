# Codex Starter Prompt - C-PHASE 143

Paste the fenced block below verbatim into Codex CLI at medium reasoning
effort.

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo:
  C:\Users\fishe\Documents\projects\wevito

Latest merged commit on origin/main at planning time:
  80f52f59b9648f61fd99db2b01eccc4db6fbf83e
Last implementation commit referenced by the planning request:
  2e08b0ef9 — C-PHASE 142: supervised self-improvement loop design (no code)

Your next phase is C-PHASE 143 (Self-improvement packet taxonomy +
plain-language explainer rows). It adds NINE new packet kinds to
PlainLanguageExplainer.KnownPacketKinds and a constants class
SelfImprovementPacketKinds. No producer logic. No mutation runner. No
model call. No capability flag.

Authoritative plan + per-phase prompt set you must follow:
  docs/CLAUDE_C_PHASE143_PLUS_PLAN_2026-05-18.md                   (master plan; §4.1 = this phase; §1 hard invariants; §7 reading order)
  docs/CLAUDE_C_PHASE143_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-18.md   (per-phase prompts; §0 Common Header; §1 = this phase)
  docs/C_PHASE142_SUPERVISED_SELF_IMPROVEMENT_LOOP_DESIGN_2026-05-18.md  (design blueprint)
  docs/CLAUDE_MASTER_PLAN_2026-05-15.md  §4                        (hard invariants)

Hard invariants you must preserve at all times:
  - No hosted AI as Wevito's runtime brain.
  - No silent network access. Only loopback endpoints for local model calls.
  - No silent training. No model weight updates.
  - No silent file, tool, code, or asset mutation.
  - Every mutation pathway requires exact scope + dry run + backup + sha256 evidence + apply + post-proof + rollback + user-visible report.
  - KillSwitchService halts every adapter, scheduler, autonomous loop, runner, scope, experiment, eval runner, and approval handler.
  - AuditLedgerService remains append-only.
  - Every new audit packet kind appears in PlainLanguageExplainer.KnownPacketKinds with a plain-language sentence.
  - Every evidence packet sets did_use_network / did_use_hosted_ai / did_use_local_model / did_mutate honestly.
  - Capability flags introduced by this batch default OFF.
  - Held-out eval data must not be visible to any proposal loop, model prompt, or iterative repair process.
  - Pets remain visually normal pet-sim characters; no AI-task animation overlay.
  - Pet game FPS / user PC experience must stay protected from AI workload.
  - The human is the judge in v0. Codex never auto-merges Auto-continue=No phases.

Read FIRST (do not skip):
  1. docs/CLAUDE_C_PHASE143_PLUS_PLAN_2026-05-18.md
  2. docs/CLAUDE_C_PHASE143_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-18.md  §0 + §1
  3. docs/C_PHASE142_SUPERVISED_SELF_IMPROVEMENT_LOOP_DESIGN_2026-05-18.md
  4. docs/CLAUDE_MASTER_PLAN_2026-05-15.md §4
  5. docs/C_PHASE141_EVIDENCE_DASHBOARD_V1_2026-05-18.md
  6. docs/C_PHASE133_AUTONOMOUS_LOOP_SCOPE_GATE_2026-05-18.md
  7. docs/BUG_BOARD.md

Step 0 — Sync working tree to origin/main (REQUIRED first action):
  Set-Location 'C:\Users\fishe\Documents\projects\wevito'
  git status                  # confirm working tree is clean
  git checkout main
  git pull origin main
  git rev-parse HEAD          # must descend from 2e08b0ef9; record into the phase report
  git checkout -b claude-implementation/c-phase-143-self-improvement-packet-taxonomy

Pre-flight gate (after sync, before any code change):
  - git status                 # working tree clean
  - dotnet build .\vnext\Wevito.VNext.sln
  - dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  - git diff --check
  If any fail, HALT and open a Stop card describing the failure. Do not proceed.

C-PHASE 143 work:

  1. Add a new file
     vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs
     containing public const string fields for these nine names (spelled
     exactly):
       self_improvement_proposal_drafted
       self_improvement_constitutional_reviewed
       self_improvement_dry_run_completed
       self_improvement_eval_completed
       self_improvement_apply_awaiting_approval
       self_improvement_apply_refused
       self_improvement_apply_completed
       self_improvement_rollback_verified
       self_improvement_maturity_clock_reset

  2. Extend vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs:
       - Add each of the nine kinds to KnownPacketKinds (using the
         constants from SelfImprovementPacketKinds, not string literals
         inside the explainer).
       - Add a plain-language sentence per kind to the existing
         explanation table. Sentences must describe what the packet
         means in user-readable terms (e.g. "Wevito drafted a supervised
         self-improvement proposal for review.").

  3. Extend vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs:
       - For each of the nine kinds: assert KnownPacketKinds contains it.
       - For each kind: assert the explainer returns a non-empty
         plain-language sentence.
       - Add a test that uses reflection over PlainLanguageExplainer.cs
         (or a Roslyn helper, or a regex over the file text) to assert
         the explainer does NOT reference any of the nine kinds via raw
         string literals — only via SelfImprovementPacketKinds.

  4. Do NOT add any service that writes any of these packets.
  5. Do NOT add any capability flag.
  6. Do NOT touch any model adapter, network surface, or filesystem
     surface outside the files listed above.

Allowed file changes for this phase:
  - vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs (new)
  - vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs           (extend)
  - vnext/tests/Wevito.VNext.Tests/PlainLanguageExplainerTests.cs   (extend)
  - docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md  (new)
  - docs/codex-phase-history.jsonl                                  (one append)

NO other files may change.

Stop gates (HALT and open the PR as Draft if any is TRUE):
  - Any of the nine kinds missing from KnownPacketKinds.
  - Any of the nine kinds missing a plain-language sentence.
  - Any test asserting actual production of a self-improvement packet.
  - Any code references a packet kind by string literal instead of the
    new SelfImprovementPacketKinds constants.
  - The five pre-flight commands fail.

Validation (run before opening PR):
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Write the phase report at:
  docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md
With sections:
  - Goal
  - Scope (files added / extended / not touched)
  - Implemented (new constants, new explainer rows, new tests)
  - Safety Boundaries (no producer, no flag, no model call, no mutation)
  - Validation (the five commands and their outcomes)
  - Stop-Gate Checklist (all FALSE)
  - Next Phase (C-PHASE 144 — ConstitutionalDecisionService v0 — Auto-continue=No)

PR and commit:
  - Branch: claude-implementation/c-phase-143-self-improvement-packet-taxonomy
  - Commit: "C-PHASE 143: self-improvement packet taxonomy + plain-language rows"
  - PR title: "C-PHASE 143: Self-improvement packet taxonomy (taxonomy + explainer rows; no producer)"
  - Body: link to docs/C_PHASE143_SELF_IMPROVEMENT_PACKET_TAXONOMY_2026-05-18.md.
  - Auto-continue: NO. Halt for user review after the PR opens.

When you finish C-PHASE 143, append one row to docs/codex-phase-history.jsonl
with phase_id, UTC timestamp, branch, and PR URL.

If you complete C-PHASE 143 and the user explicitly approves continuing,
the next phase is C-PHASE 144 (ConstitutionalDecisionService v0). Do NOT
start 144 until the user pastes its prompt from
docs/CLAUDE_C_PHASE143_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-18.md §2.

Begin.
```
