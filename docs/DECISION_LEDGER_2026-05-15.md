# Wevito Decision Ledger

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Source: full design conversation, 2026-05-14 → 2026-05-15
Purpose: single audit surface for every architectural decision made during the
sprite-template + AI-app reframe design session. Every locked decision is
listed here with its rationale, alternatives considered, and the question it
came from. Use this doc to verify nothing was missed or buried; push back on
anything that doesn't feel right.
Audience: project owner (review/audit) + Codex (consume as authoritative
source for every phase prompt below).

## 0. How to read this doc

Each decision has:

- **ID** — short tag we use in plan docs (`A3`, `P5`, `FR4`, etc.)
- **Decision** — the locked choice
- **Rationale** — why this was the call
- **Alternatives** — what we considered
- **Source** — which question in the design conversation
- **Phase impact** — which C-PHASE(s) implement this

Decisions are grouped by topic, not chronology. Where two decisions interact
(e.g., G6 + S5 + P5), the rationale calls out the dependency.

---

## 1. Hardware Reality (constraint that shapes everything)

```text
Machine                   Constraint
DESKTOP-F3KB5SP           Windows 10 Home, 64-bit
i7-9700K @ 3.6 GHz        8 cores / 8 threads, mid-tier for ML
16 GB RAM                 binding constraint (LLM + image-gen can't both be hot)
RTX 2070 SUPER 8 GB       enough for 7B LLM Q4 OR SD 1.5, not both simultaneously
954 GB SSD                plenty
NVIDIA CUDA-capable       SD 1.5 + LoRA feasible, SDXL marginal/off-table
```

**Implications carried into every other decision:**

- LLM warm by default; image-gen cold-by-default with 5-min idle unload.
- Background work pauses when free RAM drops below 3 GB.
- SDXL is off the table; SD 1.5 + pixel-art LoRA is the image target.
- 16 GB RAM means simultaneous LLM + image-gen + Python sidecar +
  user-foreground apps cannot all run hot. Aggressive lazy-load / unload.
- Max 1 active LLM generation stream at a time (Ollama single-context).

---

## 2. Sprite Track Decisions

### S-A — Canonical base sprite per (species, age, gender) per 1 color, propagate
- **Source:** Q1
- **Alternatives:** B turnaround reference sheets, C parametric/compositional templates, D doc/schema templates only
- **Rationale:** Codebase already has `propagate_authored_colors.py` and `sprites_authored/` override layer; A collapses the 3,360-frame surface to ~60 canonical cells; matches existing pipeline.
- **Phase impact:** sprite rungs 1-4

### S-A3 — 2 frames per animation, walk keeps 4 frames
- **Source:** Q2 + Q11a
- **Alternatives:** A1 full 30-frame set, A2 1-frame key-poses, A4 idle-only-first
- **Rationale:** Tamagotchi-class 2-frame animations work fine; walk is the most visible cycle so it earns more frames. ~1,320 frames at canonical color instead of ~1,800.
- **Phase impact:** sprite rungs 1-4

### S-B5 — Procedural cleanup + wevito-mediated local AI (no cloud, no raw SD in inner loop)
- **Source:** Q3
- **Alternatives:** B1 pure curation, B2 procedural only, B3 raw local AI refine, B4 procedural + curation only
- **Rationale:** AI in the inner loop *with wevito's invariants* (dry-run / backup / rollback / eval-gate / KillSwitch) is bounded; circular cleanup was caused by cloud AI without those guardrails. Same gates as existing C-PHASE 73/74 architecture.
- **Phase impact:** sprite rungs 1-4, C-PHASE 97-98

### S-F3 — Co-equal sprite + capability project, ladder-staged
- **Source:** Q4
- **Alternatives:** F1 fix-sprites only, F2 capability-only
- **Rationale:** F1 wastes the wevito architecture; F2 risks stalling on capabilities; F3 each new capability has to unblock the next batch of templates.
- **Phase impact:** the whole roadmap shape

### S-G6 — Golden-set similarity scoring (seeded by user)
- **Source:** Q5
- **Alternatives:** G1 human-only approval, G2 heuristic-only, G3 embedding-rank without human, G4 heuristic+human, G5 three-gate
- **Rationale:** G6 has the highest payoff for F3 because it forces wevito to ship a real image-similarity capability that maps cleanly onto C-PHASE 67/76 ONNX-embedder shape. Golden set grows organically once seeded.
- **Phase impact:** sprite rung 2, C-PHASE 99

### S-S5 — Heuristic-prefiltered best-of-N picker, per (species, age, gender, animation) cell
- **Source:** Q6
- **Alternatives:** S1 browse-and-mark, S2 contact sheet, S3 pairwise, S4 unfiltered best-of-N
- **Rationale:** Existing `report_sprite_visual_quality.py` and `audit_source_to_runtime_quality.py` filter the candidate pool down; user picks from a smaller survivor set. Per-animation seeding gives the eval gate per-animation granularity.
- **Phase impact:** sprite rung 1

### S-P5 — Universal 6-slot palette + per-species accent semantics
- **Source:** Q7
- **Alternatives:** P1 4-slot minimal, P2 standard 6-slot, P3 8-slot rich, P4 per-species palette structure
- **Rationale:** Mechanical uniformity (one propagation engine) + species identity preserved through accent slot's per-species meaning (raccoon mask, frog spots, snake stripes).
- **Sub-decisions:**
  - **P5a** Outline species-fixed, variant-invariant. One dark per species, never changes with variant. Anchors silhouette across rainbow.
  - **P5b** Eye species-fixed, variant-invariant. Eyes are species identity, not color identity.
  - **Palette-conformer mandatory.** Goldens are *cleaned* frames (nearest-neighbor remap to 6 declared colors + alpha threshold), not raw existing frames.
- **Phase impact:** sprite rung 3, C-PHASE 99

### S-11a — Walk = 4 frames, other animations = 2 frames
- **Source:** Q11a
- **Rationale:** Walk is the most visible animation; stuttery walks read as broken. Other animations tolerate 2-frame alternation fine.

### S-11b — Natural canonical palette per species, not rainbow
- **Source:** Q11b
- **Alternatives:** Pick one of red/orange/yellow/blue/indigo/violet as canonical
- **Rationale:** "Red rat" is arbitrary; natural rat-colored canonical has semantic meaning; LoRA training on canonical color gets a much stronger learning signal. The 6 rainbow variants become explicit fantasy/playful overlays on a natural base.
- **Phase impact:** C-PHASE 99 grammar JSON files

### S-11c — Rat as canary species for rung 1
- **Source:** Q11c
- **Rationale:** Most complete existing data across all four source trees; simplest visual complexity; accent slot semantics are flexible.

### S-11d — Front-load the 7-day soak window
- **Source:** Q11d
- **Rationale:** 7 wall-clock days is dead time we can't compress; running in parallel with sprite rungs 1-3 (which are H1-mode and don't need autonomy) means 85d is ready right when rung 4 needs it.

---

## 3. Autonomy / AI Architecture Decisions

### A-IG5 — Aesthetic-constrained general image generation
- **Source:** Q8 (image-gen scope)
- **Alternatives:** IG1 sprite-only, IG2 pet-asset-domain, IG3 general-purpose constrained, IG4 full unconstrained
- **Rationale:** Wevito can generate any subject as long as the aesthetic is "simpler pixel art" conformant. Registry of palette grammars per domain + general fallback grammar. Subject freedom + auto-checkable aesthetic conformance.
- **Phase impact:** C-PHASE 97-100

### A-H4+ — Sandboxed autonomous experimentation (six-layer safety net)
- **Source:** Q9 (autonomy level)
- **Alternatives:** H1 proposal-only, H2 per-task approval, H3 pre-approved scope, H4 unbounded
- **Rationale:** Bounded autonomy with multi-layered sandbox. Filesystem / process / network / resource / mutation / decision sandboxes. Each layer catches a different failure mode. Wevito picks from registered experiment kinds, scores results, keeps what works. Bounded RL-style learning, not AGI judgment.
- **Honest constraint:** Local AI today can't do open-ended judgment. H4+ is bounded experimentation, not "wevito decides anything." If you want open-ended judgment, that requires hosted AI, which is banned. Bounded local autonomy with good safety is the realistic ceiling.
- **Phase impact:** C-PHASE 86, 87, 88, 89, 90, 104

### A-R3-now, R4-future-M5-gated
- **Source:** Q10 (research surface)
- **Alternatives:** R1 filesystem-only, R2 local-corpus only, R4 broad web with denylist, R5 no research at all
- **Rationale:** R3 (approved-allowlist web research) starts with empty allowlist, user explicitly adds URLs. Prompt-injection countermeasures: fetched content enters "research notes" channel, never directly as instructions. R4 unlocks only after R3 earns its M5 maturity criterion (90 days zero injection incidents + 100 approved sessions + composite fitness threshold).
- **Phase impact:** C-PHASE 91

### A-M5 — Composite fitness scoreboard + per-capability maturity criteria
- **Source:** Q12 (maturity)
- **Alternatives:** M1 time-only, M2 activity-volume, M3 composite-only, M4 domain-specific only
- **Rationale:** Multi-axis scoreboard (template approval rate, golden eval, mutation proof coverage, invariant violations = 0, budget conformance, experiment success rate, user feedback) + each capability gate declares its own track-record criterion. Composite fitness for general maturity, domain-specific for capability unlocks. Reset on violation.
- **Promotion criteria are versioned and themselves mutation-gated** — tightening allowed without approval, loosening requires guarded mutation.
- **Phase impact:** C-PHASE 93, 94

---

## 4. Workflow Autonomy Gates (Codex Loop Behavior)

### W-A2 — Codex auto-merges if tests pass AND Auto-continue=Yes
- **Source:** Q13a
- **Alternatives:** A1 manual-merge only, A3 auto-merge everything, A4 commit-direct-to-main
- **Rationale:** Matches existing `Auto-continue?` discipline. User only intervenes at architecturally significant gates.

### W-B3 — Build auto-installs + hot-swap with state preservation
- **Source:** Q13b
- **Alternatives:** B1 manual install, B2 auto-install no hot-swap, B4 kill-and-respawn no graceful exit
- **Rationale:** Hot-swap at "natural pause" with state preservation lets wevito update seamlessly without user effort. State lives in `%LOCALAPPDATA%/Wevito/` and survives the swap.

### W-C3 — Capabilities code-active by default, input-controlled by empty initial state
- **Source:** Q13c, refined in Q14
- **Alternatives:** C1 default-off + consent per capability, C2 default-off + M5 auto-unlock
- **Rationale:** Reconciles "all capabilities up and running" with the safety invariant. Capability is code-active day 1 but its input data (scope, registry, allowlist, corpus) starts empty. User populates inputs; wevito does nothing harmful until inputs are populated. KillSwitch + audit + rollback remain.
- **Day-1 capability matrix:** H3 scope registry empty, H4+ experiment registry empty, R3 allowlist empty, LoRA dataset empty, image LoRA not installed, autonomous-ops proposal-only.

### W-D2 — Codex auto-advances on Auto-continue=Yes phases; stops at No
- **Source:** Q13d
- **Rationale:** Same Auto-continue discipline as merge gate.

### W-M3 — Hybrid execution model with per-phase Auto-continue field
- **Source:** Q11e
- **Rationale:** Default to Yes for small low-risk phases (tests-only, scaffold-only, doc-only). Default to No for any phase touching autonomy, training, or new capabilities.

---

## 5. Build Hot-Swap Mechanics (Q14)

### W-14a-ii — Smoke = audit/settings/memory/ONNX load (~30-60s) pre-swap
- **Source:** Q14a
- **Alternatives:** 14a-i build-success-only, 14a-iii adds 30s headless tick, 14a-iv re-runs full test suite locally
- **Rationale:** Catches runtime startup failures without re-running CI work.

### W-14b-ii — Abort + audit row + Stop card on smoke failure (never auto-revert main)
- **Source:** Q14b
- **Alternatives:** 14b-i silent abort, 14b-iii auto-revert merged commit
- **Rationale:** Surface failure loudly; let user decide whether to revert.

### W-14c-iii — Hot-swap at "natural pause": no clicks 5s, no mouseover, no animation playing
- **Source:** Q14c
- **Alternatives:** 14c-i immediate, 14c-ii on next idle tick, 14c-iv user-click-or-5min-defer
- **Rationale:** Respects user attention without requiring explicit click.

### W-14d-iii — Full state survives swap, including in-flight task cards + soak heartbeat
- **Source:** Q14d
- **Rationale:** "Seamless" requires wevito wakes exactly where it left off. Synchronous disk persistence at exit; new build reads back on startup.

---

## 6. First-Boot Behavior

### W-Q15-registry — H4+ experiment registry seeded with `sprite-template-candidate-generation` only, proposal-only mode
- **Source:** Q15 (first-boot)
- **Rationale:** Smallest viable kind; immediately useful for sprite work. Additional kinds gate behind their own phases + M5 criteria.

### W-Q16-allowlist — R3 web research allowlist empty on day 1
- **Source:** Q16 (R3 seed)
- **Rationale:** Zero risk day 1; user adds URLs explicitly via Settings.

### W-Q17-synthesis — Two-file synthesis pattern: master plan + Codex phase prompts
- **Source:** Q17 (synthesis format)
- **Rationale:** Matches existing post-75/post-85 plan convention. Codex consumes phase-prompt file one prompt at a time.

### W-Q18-runner — `tools/run-codex-loop.ps1` wrapper (new C-PHASE 96)
- **Source:** Q18 (loop runner identity)
- **Rationale:** Reads phase manifest, invokes Codex CLI, runs validation, auto-merges if green + Auto-continue=Yes, advances queue. KillSwitch halts loop. Single user invocation runs end-to-end or to next gate.

---

## 7. Identity Reframe

### I-Reframe — Wevito is a local AI assistant; pet simulator is the cosmetic wrapper
- **Source:** late-conversation reframe
- **Implications:**
  - Wevito's product identity is the AI, not the pet sim.
  - Pet visuals + gameplay run unaffected by AI workload.
  - Three "pets" are agent slots, not role-assigned helpers.
  - Chat is the primary interaction mode.
  - Local reasoning LLM is always-on default-enabled (was previously default-disabled).
  - Settings UI, README, packaging messaging all reframe accordingly.
- **Phase impact:** C-PHASE 53, 54, 55, 56, 62

### I-Single-Effort — One thinking effort tier
- **Source:** late-conversation reframe
- **Rationale:** Don't worry about multiple reasoning depths; pick one model + one config.

### I-Analogy — Wevito-app = Codex-app, internal AI = GPT-model
- **Source:** late-conversation reframe
- **Implication:** Wevito provides the home/tool-framework; the internal AI is the cognition.

---

## 8. Chat + Reasoning LLM (Q15 of reframe)

### Q15-L2 — Qwen 2.5 7B Q4_K_M as default reasoning LLM
- **Source:** Q15a
- **Alternatives:** L1 Llama 3.1 8B, L3 Phi-4 14B, L4 Mistral/Mixtral, L5 multi-model, L6 abstract-and-defer
- **Rationale:** Best-in-class tool-calling for 7B tier; fits 8 GB VRAM (~4.5 GB used, ~3 GB free); compatible with existing OllamaLocalModelAdapter; "single effort" maps cleanly to one Ollama model.
- **Hardware confirmation:** Fits your machine with headroom.

### Q15-Layout-2 — Chat owns ToolPopupWindow body
- **Source:** Q15b
- **Alternatives:** Layout-1 sidebar within popup, Layout-3 separate ChatWindow, Layout-4 persistent overlay chat
- **Rationale:** Chat IS the product; it owns the popup body. Existing tabs (Settings, Activity, Evidence Collection, Creative Lab, Benchmarks) move to a top tab row. Overlay stays pet-focused to preserve invariant I-Reframe.

### Q15-Streaming — Token-streaming via Ollama SSE
- **Source:** Q15b sub-decision
- **Rationale:** Standard chat UX; Ollama supports it natively.

### Q15-Tool-Use — AI calls tools mid-message
- **Source:** Q15b sub-decision
- **Rationale:** The killer feature. Tool call → UnifiedPolicyService gate → tool runs → result back into context → model continues. Visible in chat as "🔧 using tool X" expander.

### Q15-History — Multi-turn ChatHistoryStore (sqlite + FTS5, append-only)
- **Source:** Q15b sub-decision
- **Rationale:** Mirrors AuditLedgerService trigger pattern + UserFeedbackStore (C-PHASE 102) shape.

### Q15-Interrupt — Esc key + Stop button cancel mid-stream
- **Source:** Q15b sub-decision

### Q15-Search — FTS5 over chat history
- **Source:** Q15b sub-decision

### Q15-Title — Model auto-titles conversations from first turn; user-editable
- **Source:** Q15b sub-decision

---

## 9. Three-Agent Reframe (Q16 of reframe)

### Q16-A4 — Hybrid worker pool: internal concurrency + visible naming
- **Source:** Q16
- **Alternatives:** A1 drop entirely, A2 internal only invisible, A3 visible but role-less
- **Rationale:** Concurrency is real (8 cores, 8 GB VRAM allow parallel tool calls); visible naming preserves the "fun" pet-named workers.

### Q16-CM3 — Tool calls parallel, LLM generation serial
- **Source:** Q16a
- **Alternatives:** CM1 three parallel Ollama sessions, CM2 fully serial under the hood
- **Rationale:** Tools (filesystem, embedder, web fetch, sprite ops) can run truly parallel without VRAM contention. LLM generation serializes via Ollama single-context limit. Realistically: 1 active generation + N tool-results = full parallelism without VRAM thrashing.

### Q16-NM3 — Agent names = pet slot names; "Agent N" fallback when fewer than 3 pets
- **Source:** Q16b
- **Alternatives:** NM1 verbatim live mirror, NM2 persistent agent name overriding pet renames
- **Rationale:** Closest to your stated vision. Pet renames propagate live; if pet is removed, agent name reverts to "Agent N" cleanly.

### Q16-RS3 — Task-kind-based priority (foreground task gets 60% budget, background shares 40%)
- **Source:** Q16c
- **Alternatives:** RS1 per-agent quota, RS2 single shared FIFO
- **Rationale:** Priority moves with task assignment, not slot identity. Whichever agent is handling chat gets foreground priority; whichever is doing background work gets leftover.

### Q16-DA1+DA3 — AI dispatches for user-visible work; first-idle for background
- **Source:** Q16d
- **Alternatives:** DA1 only (always AI decides), DA2 separate dispatcher service, DA3 only (always first-idle)
- **Rationale:** Hybrid. User-visible work benefits from AI's contextual choice (and supports the "fun" naming). Background work has no judgment requirement; first-idle is simpler.

### Q16-VS3 — Both detail tab + ambient overlay icons
- **Source:** Q16e
- **Alternatives:** VS1 detail only, VS2 ambient only
- **Rationale:** VS2 gives ambient "is anything happening" glance; VS1 gives "what exactly are they doing" detail.

---

## 10. Benchmark Suite (Q17 of reframe)

### Q17-V1-Suite — v1 ships B1 chat-correctness + B2 tool-use + B4 retrieval + B7 safety + B8 perf
- **Source:** Q17a
- **Deferred to v2:** B3 sprite-quality (after C-PHASE 99/97), B5 multi-step (after C-PHASE 55), B6 user-feedback regression (after C-PHASE 102 collects baseline)
- **Rationale:** v1 measures core competency + non-negotiable regressions (safety, perf). Sprite/multi-step/feedback need prerequisite phases.

### Q17-CA6 — Hybrid cadence: per-phase for safety+perf; daily for capability; on-demand always
- **Source:** Q17b
- **Alternatives:** CA1 per-commit, CA2 per-phase only, CA3 daily only, CA4 weekly only, CA5 on-demand only
- **Rationale:** Safety+perf must regression-gate the phase loop. Capability benchmarks are expensive (~50 model calls) and don't need per-phase frequency.

### Q17-GI1+GI4-reinterpreted — Deterministic grading is primary truth; production LLM grading is metadata; held-out grader for v2 prose
- **Source:** Q17c (user picked GI1+GI4 explicitly)
- **Honest reframe:** GI1 alone is circular; GI1 *as supplementary signal alongside* deterministic primary is useful (measures LLM self-awareness). Wevito's score on the dashboard is deterministic-only; LLM self-grade is logged for diagnosis. Held-out grader (separate Ollama model) lights up for v2 prose-heavy benchmarks.
- **Implementation:** `GraderTriad { Deterministic, ProductionLLM, HeldOut? }` — deterministic is mandatory and authoritative; others are advisory.

### Q17-FR4 — Tiered failure response
- **Source:** Q17d
- **Alternatives:** FR1 log only, FR2 always-halt, FR3 auto-rollback
- **Rationale:**
  - Safety regression (B7) → auto-halt + Stop card. No exceptions.
  - Perf regression > 20% (B8) → auto-halt.
  - Capability regression > 15% → halt.
  - Capability regression 5-15% → open task card, continue loop.
  - Capability regression < 5% → log only.

### Q17-UI5 — Detailed Benchmarks tab + ambient HomePanelWindow badge
- **Source:** Q17e
- **Rationale:** Detail when you want it; ambient when you don't.

### Q17-V1-Immutable — Benchmark cases are immutable once committed; new cases additive only
- **Source:** Q17f
- **Rationale:** Comparability over time requires immutability. Mutation breaks the metric.

### Q17-Option-C — Codex drafts cases; user approves with bookmark-from-chat growing the corpus organically
- **Source:** Q17g
- **Approach refinement:** v1 starter = 25 adversarial (B7) + 30 bootstrap (B1+B2+B4 drafts you approve) = **55 cases on day 1**, growing organically toward 175+ via "🔖 benchmark this" button in chat.
- **One-time user cost:** ~90 min day 1 (60 min adversarial brainstorm + 30 min bootstrap approval).
- **Ongoing user cost:** ~1 min per chat session if you bookmark a notable exchange.

---

## 11. Phase Inventory

### Already shipped (referenced for context, no work)
- Pre-65: pet behavior, audit ledger, policy, kill switch, supervisor, evidence packets, memory store, learning lab, sprite workflow, etc.
- C-PHASE 65-85c shipped per existing C_PHASE doc files (`docs/C_PHASE65_*.md` through `docs/C_PHASE85C_*.md`).

### Residual (on-deck before this session's work)
- **C-PHASE 85d** — Autonomous Operations Promotion (blocked on user soak)
- **USER SOAK** — 7-day wall-clock window (front-loaded per S-11d)

### New phases this design session adds

#### Sprite track (rungs as phases)
- **Sprite Rung 1** — Golden-seed UI + heuristic prefilter (S-S5 implementation)
- **Sprite Rung 2** — Image embedding scoring (CLIP-image ONNX)
- **Sprite Rung 3** — Palette conformer + variant declaration + color propagation (S-P5 implementation, uses C-PHASE 99 grammars)
- **Sprite Rung 4** — Remaining 7-animation expansion

#### H4+ foundation phases
- **C-PHASE 86** — Pre-approved Scope Service (H3 foundation)
- **C-PHASE 87** — Sandboxed Experiment Runner + Kind Registry
- **C-PHASE 88** — Experiment Kind: sprite-template-candidate-generation
- **C-PHASE 89** — Experiment Kind: lora-hyperparameter-search
- **C-PHASE 90** — Constitutional Decision Service
- **C-PHASE 91** — Approved Research Connector Expansion (R3 allowlist)
- **C-PHASE 92** — Self-Improvement Promotion Gate
- **C-PHASE 93** — Composite Fitness Scoreboard
- **C-PHASE 94** — Maturity Promotion Service
- **C-PHASE 95** — Build Hot-Swap Wrapper (W-B3 + W-14a..d implementation)
- **C-PHASE 96** — Codex Loop Runner (W-Q18-runner implementation)

#### Gap phases (written to `CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md`)
- **C-PHASE 97** — Image LoRA Training Pipeline
- **C-PHASE 98** — Local Image Generation Runtime
- **C-PHASE 99** — Multi-Domain Palette Grammar Registry
- **C-PHASE 100** — General "Simpler Pixel Art" Fallback Grammar
- **C-PHASE 101** — Always-On Household Maintenance
- **C-PHASE 102** — User Feedback Ingestion
- **C-PHASE 103** — Memory Consolidation
- **C-PHASE 104** — Strategic Planner
- **C-PHASE 105** — Daily/Weekly User-Facing Digest UI

#### Reframe phases (still to be authored, renumbered to 106-116 to avoid collision with shipped C-PHASE 53-85c)
- **C-PHASE 106** — Default-enabled reasoning LLM (Qwen 2.5 7B via Ollama + ONNX-Phi fallback adapter wiring) — Q15-L2
- **C-PHASE 107** — Chat UI replacing pet-task report area (Q15-Layout-2 + streaming + tool-use + history + interrupt + search + title)
- **C-PHASE 108** — Agent-slot redesign (Q16-A4 + CM3 + NM3 + RS3 + DA1+DA3 + VS3); deletes hardcoded Scout/Inspector/Builder
- **C-PHASE 109** — Tool registry first-class for AI calling (schema discovery + mid-chat invocation + tool-result streaming)
- **C-PHASE 110** — Pet/AI isolation contract (Q18: F3 + P4 + R4 + G3+G4 + C4 + I3)
- **C-PHASE 111** — User-PC coexistence policy (Q19: T5 + D5 + L3 + B4 + R4 + V3)
- **C-PHASE 112** — Benchmark Suite v1 (Q17 implementation)
- **C-PHASE 113** — Benchmark case curation UI (bookmark-from-chat + draft-approval surface)
- **C-PHASE 114** — Codex loop reliability hardening (Q20: T4-modified + S2 + F4 + M4 + L3 + I4 + H4)
- **C-PHASE 115** — Identity rename + UX language pass (Q21: N4 + N-3 + U-2 + D-4 + O-2)
- **C-PHASE 116** — Chat-context-window management at year-scale (Q22: C-5 + A-1 + S-3 + R-4 + P-3 + T-4 + F-4)

#### Sprite-rung phases (renumbered to 117-120)
- **C-PHASE 117** — Sprite Rung 1: Golden-seed UI + heuristic prefilter (S-S5 implementation)
- **C-PHASE 118** — Sprite Rung 2: Image embedding scoring (CLIP-image ONNX, lights up after C-PHASE 99)
- **C-PHASE 119** — Sprite Rung 3: Palette conformer + variant declaration + color propagation (folds into C-PHASE 99 partially; the propagation engine is a separate phase)
- **C-PHASE 120** — Sprite Rung 4: Remaining 7-animation expansion (uses everything above)

Total new phases: **35** (4 sprite rungs + 11 H4+ foundation + 9 gap + 11 reframe).

### Sequencing summary (rough; final order in master plan synthesis)
```text
0.  USER SOAK START          (front-loaded, runs in parallel)
1.  C-PHASE 53               Default-enabled reasoning LLM
2.  C-PHASE 54               Chat UI
3.  C-PHASE 55               Agent-slot redesign
4.  C-PHASE 56               Tool registry first-class
5.  Sprite Rung 1            Golden-seed UI
6.  C-PHASE 101              Household maintenance       (so wevito has day-1 work)
7.  C-PHASE 102              User feedback ingestion
8.  C-PHASE 57               Benchmark suite v1
9.  C-PHASE 58               Benchmark curation UI
10. C-PHASE 99               Palette grammar registry
11. C-PHASE 100              General fallback grammar
12. C-PHASE 98               Image generation runtime
13. Sprite Rung 2            Image embedding scoring
14. Sprite Rung 3            Palette conformer + propagation
15. SOAK ENDS (~day 7)
16. C-PHASE 85d              Autonomous operations promotion
17. C-PHASE 86               Pre-approved scope service
18. C-PHASE 87               Sandboxed experiment runner
19. C-PHASE 88               Experiment kind: sprite-template
20. C-PHASE 93               Composite fitness scoreboard
21. C-PHASE 94               Maturity promotion service
22. C-PHASE 89               Experiment kind: LoRA hyperparam search
23. C-PHASE 97               Image LoRA training pipeline
24. C-PHASE 90               Constitutional decision service
25. C-PHASE 91               Approved research connector (R3)
26. C-PHASE 92               Self-improvement promotion gate
27. C-PHASE 103              Memory consolidation
28. C-PHASE 104              Strategic planner
29. C-PHASE 105              Activity digest UI
30. Sprite Rung 4            Remaining animations
31. C-PHASE 59               Pet/AI isolation contract       (Q18 pending)
32. C-PHASE 60               User-PC coexistence policy       (Q19 pending)
33. C-PHASE 61               Codex loop reliability           (Q20 pending)
34. C-PHASE 62               Identity rename + UX pass        (Q21 pending)
35. C-PHASE 63R              Chat-context-window management   (Q22 pending)
36. C-PHASE 95               Build hot-swap wrapper           (could move earlier)
37. C-PHASE 96               Codex loop runner                (could move earlier)
```

Phases 36-37 (build pipeline + loop runner) might want to land *earlier* in execution order to actually enable the autonomous loop for everything that follows them; final synthesis will resolve.

---

## 12. Q18-Q22 Decisions (Closed)

All grilling rounds closed 2026-05-15. Decisions below complete the design.

### Q18 — Pet/AI Isolation Contract (C-PHASE 110)

- **Q18-F3** — 60fps target for Godot pet game with strict 30fps floor; AI throttles when below 60, halts when below 30. `PetFpsMonitorService` writes `pet_fps_snapshot` hourly + `pet_fps_violation` on breach.
- **Q18-P4** — Tiered AI priority: BelowNormal when pet has focus + suspend background during 5s of active pet interaction + chat-foreground preempts background.
- **Q18-R4** — RAM pressure cascade: pet game has 1 GB reserved minimum; free RAM < 3 GB suspends experiments, < 2 GB unloads image-gen sidecar, < 1.5 GB unloads Ollama (pet keeps running), < 1 GB emergency stop.
- **Q18-G3+G4** — Image-gen runs only when pet idle for autonomous mode (G3); always runs immediately on explicit user trigger (G4); CPU-only fallback (G5) on VRAM pressure.
- **Q18-C4** — Cross-process state-survival: pet game persists every 30s to `pet-state.json`; chat history owned by wevito shell (not Ollama); image-gen stateless. Watchdog in Broker restarts crashed children with max 3 retries in 5 min then halt + Stop card.
- **Q18-I3** — Pet input fully preempts AI on separate threads; UI thread services pet input independently.

### Q19 — User-PC Coexistence Policy (C-PHASE 111)

- **Q19-T5** — Auto-pause triggers: fullscreen-app + user-configurable app list (pre-seeded with `zoom.exe`, `teams.exe`, `obs64.exe`, `discord.exe`) + CPU > 80% from non-wevito + network saturation. Each individually toggle-able in Settings.
- **Q19-D5** — Do-not-disturb: user-configurable schedule + quick-toggle in overlay ("DnD now / 1h / until tomorrow"). Smart auto-detection deferred until that itself earns M5 maturity.
- **Q19-L3** — Three workload tiers: **User-Foreground** (chat reply, user-clicks) > **Maintenance** (chores, evidence, daily eval) > **Experimentation** (LoRA, image gen, autonomous research). Maps onto existing `RuntimeSupervisorMode`.
- **Q19-B4** — Budget allocation: reserved minimums per tier (foreground always has its share) + adaptive borrowing above the floor. Implementation extends `RuntimeBudgetMeter.TryReserve`.
- **Q19-R4** — Tiered resume after triggers clear: maintenance 1 min grace; experimentation 5 min grace. Pet game runs through; never paused.
- **Q19-V3** — Visible pause via pet animation: pet plays `sleep` instead of `idle` when wevito is yielding. Free reuse of existing sprite asset.

### Q20 — Codex Loop Reliability (C-PHASE 114)

- **Q20-T4-modified** — Per-phase timeout: 2h baseline. Auto-extend indefinitely when user idle > 10 min (Windows `GetLastInputInfo`); snap back to remaining budget on user input. Combined with S2 prevents runaway.
- **Q20-S2** — Stuck detection: no commit in 30 min → halt with reason.
- **Q20-F4** — Test/build failure: retry once with failure context attached; second failure → auto-open remediation task card describing the failure for human inspection.
- **Q20-M4** — Merge conflicts: auto-rebase if clean fast-forward; halt for human if semantic conflict.
- **Q20-L3** — Three-file state: `codex-phase-queue.json` (pending), `codex-loop-status.json` (in flight), `codex-phase-history.jsonl` (append-only audit).
- **Q20-I4** — Human-inject: edit queue files (routine) + `tools/codex-inject.ps1` (priority insertion) + `tools/codex-loop-pause.ps1` / `resume.ps1` (emergency).
- **Q20-H4** — Heartbeat every 5 min + watchdog in Broker restarts loop runner + ambient status badge in HomePanelWindow.

### Q21 — Identity Rename + UX Language Pass (C-PHASE 115)

- **Q21-N4** — Rename misleading internals (`PetCommandBarService` → `ChatInputBarService`, `PetTaskCard` → `AgentTaskCard`, `PetTaskAdapterPreviewDispatcher` → `AgentToolDispatcher`, `HelperPet` → `AgentSlot`, etc.); keep accurate ones (`PetMemoryStore`, `PetSimulationEngine`). Contracts namespace unchanged.
- **Q21-N-3** — AI identity: first-run wizard asks "what should I call me?" defaulting to "Wevito"; user can rename anytime in Settings. New `AiIdentityService`.
- **Q21-U-2** — Settings UI restructure: chat owns body; top tab strip = `Chat (default) | Activity | Agents | Tools | Benchmarks | Creative Lab | Settings`.
- **Q21-D-4** — Doc rewrite: `README.md` + new `WHAT_IS_WEVITO.md` + new `docs/INDEX.md`. Historical phase docs untouched (frozen audit trail).
- **Q21-O-2** — Onboarding: 4-step first-launch wizard (name AI, name agent slots, initial scope, first greeting). Writes `first_launch_completed`; never runs again.

### Q22 — Chat-Context-Window Management (C-PHASE 116)

- **Q22-C-5** — Session boundaries: user explicitly starts new conversations via "New chat"; AI can retrieve from past sessions via memory store. Same as Claude.ai / ChatGPT.
- **Q22-A-1** — Static context allocation for Qwen 2.5 7B (128K tokens): 4K system + 70K current turns + 30K retrieval + 16K tool buffer + 8K reply headroom.
- **Q22-S-3** — Rolling summarization: when current-turns budget hits 80%, background summarizer compresses oldest 20K → 3K summary. Originals stay in `ChatHistoryStore` (FTS searchable). Summary written to `PetMemoryStore`.
- **Q22-R-4** — Mid-chat retrieval hybrid: automatic embedding-based retrieve per user turn (top-3, low budget) + explicit `retrieve_from_memory` tool call for deep dives.
- **Q22-P-3** — Pinning: user pins key messages; pinned content has dedicated ~4K budget separate from system prompt; survives summarization.
- **Q22-T-4** — Tool result budget: truncate at 4K + inline summary + "[truncated; full result at <path>]" marker so AI knows to retrieve more if needed.
- **Q22-F-4** — Archival: chat sessions inactive 6 months migrate to `chat-history-cold.sqlite`. Memory summaries + pins migrate too. Active store stays lean; cold store FTS-searchable lazily. Append-only invariant preserved.

---

## 13. Reverse Index: "Which decision affects X?"

If you want to know what's locked for a given topic, here's the reverse map:

### Sprite work
S-A · S-A3 · S-B5 · S-F3 · S-G6 · S-S5 · S-P5 (P5a, P5b, palette conformer) · S-11a · S-11b · S-11c · S-11d

### Autonomy
A-IG5 · A-H4+ · A-R3-now · A-M5 · W-C3

### Image generation
A-IG5 · S-P5 · Q15-L2 (model can do tool-use; not image) · S-11b (natural canonical) · Hardware (SD 1.5 only)

### Chat / UI
I-Reframe · Q15-Layout-2 · Q15-Streaming · Q15-Tool-Use · Q15-History · Q15-Interrupt · Q15-Search · Q15-Title

### Agent slots
Q16-A4 · Q16-CM3 · Q16-NM3 · Q16-RS3 · Q16-DA1+DA3 · Q16-VS3

### Codex loop / workflow
W-A2 · W-B3 · W-C3 · W-D2 · W-M3 · W-14a-ii · W-14b-ii · W-14c-iii · W-14d-iii · W-Q18-runner · Q20-pending

### Benchmarks
Q17-V1-Suite · Q17-CA6 · Q17-GI1+GI4-reinterpreted · Q17-FR4 · Q17-UI5 · Q17-V1-Immutable · Q17-Option-C

### Maturity / capability earning
A-M5 · W-Q15-registry · W-Q16-allowlist · A-R3-now

### Identity / product framing
I-Reframe · I-Single-Effort · I-Analogy · Q21-pending

---

## 14. Push-Back Instructions

If anything in this ledger doesn't feel right anymore, the easiest path is:

- Tell Claude "I want to change <ID>" with the new choice, and we'll re-derive any downstream decisions that depended on it.
- If a decision wasn't recorded that should have been, tell Claude "I think we also agreed on <X>" and we'll add it to the ledger.
- If a decision was recorded but the rationale feels wrong, tell Claude "the rationale for <ID> is incomplete" and we'll either fix the rationale or revisit the decision.

The ledger is the source of truth for downstream plan documents. Phase prompts will reference decision IDs directly so Codex can look them up here when implementing.
