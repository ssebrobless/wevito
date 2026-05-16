# Claude Request - Restore/Author Post-C-PHASE 116 Implementation Plans

Repo: `C:\Users\fishe\Documents\projects\wevito`

Current confirmed state:

- `main` includes C-PHASE 106 through C-PHASE 116.
- Latest merged commits:
  - C-PHASE 114: Codex loop reliability hardening.
  - C-PHASE 115: identity rename and UX language pass.
  - C-PHASE 116: chat-context-window management.
- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` references additional plan documents that are not present in this checkout:
  - `docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md`
  - `docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md`
  - `docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md`
  - `docs/CLAUDE_PHASE_PROMPTS_2026-05-15.md`
- `docs/codex-phase-queue.json` is currently empty.
- `docs/codex-phase-history.jsonl` is empty in this checkout.

Why this matters:

The next master-plan item after the merged reframe phases is C-PHASE 99, followed by C-PHASE 100, C-PHASE 98, and sprite rungs C-PHASE 117-120. The master plan only provides high-level summaries for those phases. Codex should not invent detailed implementation scopes for image runtime, palette grammar, fallback grammar, local generation, autonomy foundation, or sprite-rung work without the missing phase contracts.

## Copy-Paste Prompt For Claude

```text
You are reviewing the Wevito repo:

C:\Users\fishe\Documents\projects\wevito

Goal:
Restore or author the missing detailed implementation plans needed after C-PHASE 116 so Codex can continue at medium effort without improvising risky architecture or sprite-generation work.

Read first:

1. docs/DECISION_LEDGER_2026-05-15.md
2. docs/CLAUDE_MASTER_PLAN_2026-05-15.md
3. docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md
4. docs/C_PHASE106_DEFAULT_ENABLED_REASONING_LLM_2026-05-15.md
5. docs/C_PHASE107_CHAT_UI_2026-05-15.md
6. docs/C_PHASE108_AGENT_SLOT_REDESIGN_2026-05-15.md
7. docs/C_PHASE109_TOOL_REGISTRY_FIRST_CLASS_2026-05-15.md
8. docs/C_PHASE110_PET_AI_ISOLATION_CONTRACT_2026-05-15.md
9. docs/C_PHASE111_USER_PC_COEXISTENCE_POLICY_2026-05-15.md
10. docs/C_PHASE112_BENCHMARK_SUITE_V1_2026-05-15.md
11. docs/C_PHASE113_BENCHMARK_CASE_CURATION_UI_2026-05-15.md
12. docs/C_PHASE114_CODEX_LOOP_RELIABILITY_HARDENING_2026-05-15.md
13. docs/C_PHASE115_IDENTITY_RENAME_UX_LANGUAGE_PASS_2026-05-15.md
14. docs/C_PHASE116_CHAT_CONTEXT_WINDOW_MANAGEMENT_2026-05-15.md
15. docs/CLAUDE_POST_116_MISSING_PHASE_PLAN_REQUEST_2026-05-16.md

Current confirmed state:

- C-PHASE 106 through C-PHASE 116 are merged into main.
- The detailed post-116 docs referenced by docs/CLAUDE_MASTER_PLAN_2026-05-15.md are missing from this checkout:
  - docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md
  - docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md
  - docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md
  - docs/CLAUDE_PHASE_PROMPTS_2026-05-15.md
- docs/codex-phase-queue.json is empty.
- docs/codex-phase-history.jsonl is empty.

Required output:

Create or restore the missing docs with enough detail for medium-effort Codex implementation:

1. docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-16.md
2. docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-16.md
3. docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-16.md
4. docs/CLAUDE_POST_116_PHASE_PROMPTS_2026-05-16.md
5. Optional: update docs/codex-phase-queue.json with the next approved implementation phase if appropriate.

The docs must preserve all existing Wevito invariants:

- Local-first AI assistant; no hosted AI runtime brain.
- No hidden network access, hidden local file access, hidden training, hidden model downloads, or hidden mutation.
- Pet visuals remain the cosmetic/game surface; task completion stays under the hood.
- Any mutation requires exact scope, dry-run, backup/hash/rollback, and post-proof.
- Sprite/image generation and asset mutation must remain gated and provenance-backed.
- Capability flags default off unless a prior merged phase explicitly made the capability default-on.
- KillSwitch, quiet mode, user-PC coexistence, audit ledger rows, and plain-language packet coverage must remain binding.
- One phase per PR.

For each phase, include:

- Phase number, title, goal, decision IDs implemented.
- Branch name.
- Files likely touched.
- New files.
- Exact tasks.
- Stop gates.
- Validation commands.
- Required artifacts.
- Rollback plan.
- PR title.
- Auto-continue yes/no.
- A copy-paste Codex prompt for that phase.

Coverage required:

- C-PHASE 99: multi-domain palette grammar registry.
- C-PHASE 100: general fallback grammar.
- C-PHASE 98: local image generation runtime.
- C-PHASE 117: sprite rung 1, golden-seed UI and heuristic prefilter.
- C-PHASE 118: sprite rung 2, image embedding scoring.
- C-PHASE 119: sprite rung 3, palette conformer and variant propagation.
- C-PHASE 120: sprite rung 4, remaining animation expansion.
- Foundation/autonomy phases C-PHASE 86-96 if they are still needed after the current merged state.
- Gap phases C-PHASE 97-105 if they are still needed after the current merged state.

Important:

- Do not tell Codex to start broad sprite generation/import without strong gates.
- Do not let Codex invent image-generation runtime dependencies without explicit install/download approval.
- If a phase requires a new dependency, model download, network access, training, or asset mutation, mark Auto-continue=No.
- If a phase is now obsolete because later merged work already covers it, say so explicitly and provide a replacement phase or skip rationale.

When done, return a single copy-paste prompt for Codex that points to the new docs and tells Codex exactly which phase to start next.
```
