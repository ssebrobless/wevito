# Claude Planning Request - C-PHASE 134+

## Current Repo State

Repo:

`C:\Users\fishe\Documents\projects\wevito`

Latest merged main tip:

`9b478282c C-PHASE 133: Autonomous loop scope gate (sprite-repair-triage + audit-ledger-cleanup; default off) (#225)`

The planned C-PHASE 125 through C-PHASE 133 batch is complete and merged. Codex searched the repo after C-PHASE 133 and found no C-PHASE 134+ implementation plan yet.

## Recently Completed Phase Reports To Review

- `docs/C_PHASE125_PRODUCT_TRUTH_BASELINE_2026-05-16.md`
- `docs/C_PHASE126A_PET_GAME_UX_FIXED_SCOPE_2026-05-16.md`
- `docs/C_PHASE127_VISUAL_QA_MANIFEST_2026-05-16.md`
- `docs/C_PHASE128_SPRITE_REPAIR_QUEUE_2026-05-16.md`
- `docs/C_PHASE129_SPRITE_REPAIR_BATCH_POLICY_2026-05-17.md`
- `docs/C_PHASE131_GREEN_COLOR_DECISION_2026-05-18.md`
- `docs/C_PHASE132_LOCAL_BRAIN_DEFAULT_ON_2026-05-18.md`
- `docs/C_PHASE133_AUTONOMOUS_LOOP_SCOPE_GATE_2026-05-18.md`
- `docs/BUG_BOARD.md`
- `docs/codex-phase-history.jsonl`

## Durable Invariants

- No hosted AI as Wevito's runtime brain.
- No silent network access.
- No silent training.
- No silent file, tool, code, or asset mutation.
- Every mutation pathway requires exact scope, dry run, backup, sha256 evidence, apply, post-proof, rollback, and user-visible report.
- KillSwitch must halt adapters, scheduler, autonomous loops, runners, and scopes.
- AuditLedgerService remains append-only.
- New audit packet kinds must be added to `PlainLanguageExplainer.KnownPacketKinds`.
- Pets remain normal pet-sim characters visually.
- AI/tool task completion stays under the hood, not as special pet animations.
- Capability flags default off unless explicitly approved.
- Pet-game FPS and user PC experience must stay protected from AI/tool workload.

## Current Capability Baseline

Wevito now has:

- Local-first AI/product framing.
- Local brain heartbeat and default-on shell readiness path, with safe degraded behavior when the local runtime is absent.
- Hosted model adapter refusal at the shell composition root.
- Autonomous beta loop infrastructure.
- Default-off autonomous scope gates:
  - `sprite-repair-triage`
  - `audit-ledger-cleanup`
- Sprite repair queue and batch policy infrastructure.
- A large completed sprite repair batch sequence through C-PHASE 130.
- Disabled-green v1 color decision from C-PHASE 131.

## Planning Goal

Create the next concrete implementation plan for C-PHASE 134 onward.

The next plan should move Wevito from "safe local-first scaffold with guarded autonomous scope gates" toward an actually useful local-first AI assistant that can help with research, planning, sprite/game QA, evidence collection, and supervised self-improvement without depending on GPT, Claude, Codex, Gemini, or any hosted AI as the product runtime brain.

## Focus Areas For C-PHASE 134+

Please decide the best sequencing, but consider these areas:

1. Post-C-PHASE-133 verification baseline.
2. Autonomous scope UX and safety proof.
3. Sprite-repair triage usefulness without mutation.
4. Audit-ledger cleanup proof and rollback/reversibility posture.
5. Local brain runtime setup and user-visible status.
6. Local research / local document retrieval quality.
7. Supervised self-improvement loop design.
8. Pet game visual/UX bug follow-up from `BUG_BOARD.md`.
9. Tool hub simplification and reliability.
10. Evidence dashboards that explain what Wevito did, did not do, and why.

## Requested Claude Outputs

Please produce:

1. A master implementation plan doc under `docs/`.
2. A Codex-medium phase prompt doc under `docs/`.
3. Exact phase list starting at C-PHASE 134.
4. For each phase:
   - goal
   - branch name
   - files likely in scope
   - hard constraints
   - stop gates
   - validation commands
   - expected report path
   - commit message pattern
   - PR title
   - auto-continue yes/no
5. A final single copy-paste prompt for Codex to begin C-PHASE 134.

## Recommended First Prompt Back To Codex

After writing the plan docs, hand back a single prompt that tells Codex:

- the repo path
- which docs to read first
- the exact C-PHASE 134 scope
- validation commands
- stop gates
- branch name
- PR title
- whether to auto-continue
