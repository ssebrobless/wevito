# Wevito Master Plan — Autonomous AI Assistant with Pet Visuals

Date: 2026-05-15
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex (executor, medium reasoning effort) + project owner (review)

## 0. One-Paragraph Summary

Wevito is a local AI assistant. The desktop pet game is its cosmetic visual
surface. The AI is the product. This master plan covers 35 phases that
transform the current state (sprite-cleanup work blocked in a Gemini
feedback loop; AI capabilities scaffolded but not enabled by default; pet
visuals as primary interaction surface) into the target state (Qwen 2.5
7B local LLM running by default, chat is primary UI, three "pets" are
agent slots the AI dispatches to, AI improves itself autonomously within
a multi-layered safety sandbox, benchmark-gated regression detection,
pet visuals never affected by AI workload, Codex loop runs unattended
through every phase). The plan is sized for Codex medium reasoning to
execute phase-by-phase via `tools/run-codex-loop.ps1` without human
intervention except at architecturally significant stop gates.

## 1. Where Every Decision Is Documented

- **`docs/DECISION_LEDGER_2026-05-15.md`** — authoritative source for
  every architectural decision. Phase prompts reference decision IDs
  (e.g., `Q15-L2`, `P5`, `FR4`).
- **`docs/CLAUDE_GAP_PHASES_97_THROUGH_105_PLAN_2026-05-15.md`** — 9 gap
  phases (image LoRA, image runtime, palette grammars, fallback grammar,
  household maintenance, user feedback, memory consolidation, strategic
  planner, activity digest UI).
- **`docs/CLAUDE_REFRAME_PHASES_106_THROUGH_116_PLAN_2026-05-15.md`** —
  11 reframe phases (default LLM, chat UI, agent slots, tool registry,
  isolation, coexistence, benchmarks v1, curation UI, loop reliability,
  identity rename, context window mgmt).
- **`docs/CLAUDE_H4_FOUNDATION_PHASES_86_THROUGH_96_PLAN_2026-05-15.md`**
  — 11 autonomy-foundation phases (to be authored next).
- **`docs/CLAUDE_SPRITE_RUNG_PHASES_117_THROUGH_120_PLAN_2026-05-15.md`**
  — 4 sprite-track phases (to be authored).
- **`docs/CLAUDE_PHASE_PROMPTS_2026-05-15.md`** — copy-paste Codex prompts,
  one per phase (to be authored).

## 2. Hardware Reality (binding constraint)

```text
Machine                   DESKTOP-F3KB5SP, Windows 10 Home, x64
Processor                 i7-9700K @ 3.6 GHz, 8 cores / 8 threads
RAM                       16 GB (binding constraint)
GPU                       NVIDIA RTX 2070 SUPER, 8 GB VRAM
Storage                   954 GB SSD + 932 GB HDD
```

**Implications baked into every phase:**

- Qwen 2.5 7B Q4_K_M (~4.5 GB VRAM) is the default LLM; loads to GPU.
- Stable Diffusion 1.5 + LoRA is the image target; ~4 GB VRAM. **SDXL off the table.**
- LLM warm by default; image-gen cold-by-default with 5-min idle unload.
- LLM and image-gen cannot both be hot simultaneously; second one unloads
  the first.
- Background experiments suspend when free RAM < 3 GB.
- Pet game has 1 GB reserved minimum, never touched by AI cascade.
- Codex loop work + League of Legends + AI work coexist via tier-priority
  + coexistence triggers (see §6).

## 3. The Shape of Wevito (post-plan)

```text
                            +------------------------+
                            |  User                  |
                            +-----------+------------+
                                        |
        clicks pet            chat input             tool requests
        feed/groom               + thumbs              (mid-chat)
            |                        |                    |
            v                        v                    v
+-----------+--------+   +-----------+-----------+   +----+---------+
|  Godot pet game   |   |  WPF chat panel        |   |  Agent slots |
|  (always 60 fps   |   |  (multi-turn, streaming,|   |  (3 worker   |
|   target;          |   |   tool-use inline,      |   |   pool;      |
|   never affected   |   |   FTS5-searchable,      |   |   names from |
|   by AI workload)  |   |   bookmark-from-chat)   |   |   pets)      |
+-----+--------------+   +-----------+-------------+   +----+---------+
      |                              |                      |
      | pet state IPC                | streaming SSE        | tool calls
      v                              v                      v
+-----+------------------------------+----------------------+---------+
|              Wevito.VNext.Core (the AI's brain)                     |
|   - OllamaLocalModelAdapter (Qwen 2.5 7B, default-on, loopback only)|
|   - ChatStreamingService + ChatHistoryStore (sqlite + FTS5)          |
|   - ToolRegistry (every preview adapter exposed as AI-callable)      |
|   - AgentSlotService + AgentToolConcurrencyCoordinator                |
|   - HouseholdMaintenanceService (5 day-1 chores)                     |
|   - BenchmarkSuiteService (B1/B2/B4/B7/B8 + FR4 regression gate)    |
|   - ExperimentRunnerService + StrategicPlannerService                |
|   - GuardedMutationService (every mutation: dry-run/backup/apply/    |
|     post-proof/rollback)                                              |
|   - MaturityPromotionService (M5 capability gating)                  |
|   - CompositeFitnessScoreboard                                       |
|   - PetFpsMonitorService + RamPressureCascadeService                 |
|   - CoexistenceTriggerService + DoNotDisturbScheduleService          |
|   - ChatContextBudgetService + RollingSummarizerService              |
|   - RetrievalAutomaticInjector + PetMemoryStore (sqlite + sqlite-vec) |
|   - AuditLedgerService (append-only sqlite with triggers)             |
|   - UnifiedPolicyService (default-deny, denylist > allowlist)         |
|   - KillSwitchService (halts EVERY adapter, scheduler, loop, runner)  |
+-----+----------------------+---------------------------+-----------+
      |                      |                           |
      | localhost            | localhost                 | watchdog
      v                      v                           v
+-----+------------+  +------+------------+    +--------+--------+
|  Ollama          |  |  Python image-gen|    |  Broker         |
|  (Qwen 2.5 7B   |  |  sidecar         |    |  (pipes,        |
|   warm)          |  |  (SD 1.5 cold-   |    |   hotkeys,      |
|                  |  |   load on demand)|    |   watchdog)     |
+------------------+  +------------------+    +-----------------+
```

## 4. Hard Invariants (every phase must satisfy)

```text
- Wevito's reasoning runs LOCAL only; no hosted GPT/Claude/Codex/Gemini at runtime.
- No hidden web access, no hidden training, no hidden file access.
- Every risky capability stays default-off OR ships with empty initial state.
- Every learning step: reviewed data + eval gate + rollback.
- Every mutation: dry-run + backup hash + apply + post-proof + rollback?
- KillSwitch halts every adapter, scheduler, loop, tracker, runner.
- AuditLedgerService is append-only (sqlite UPDATE/DELETE triggers); never violated.
- Pets remain visually regular pet-sim characters; no AI-task animation overlay.
- Pet sim experience (visuals + gameplay) MUST NOT be affected by AI workload.
- Pet game has 1 GB reserved minimum; AI cascade never touches it.
- Every local model adapter must safely degrade when runtime absent.
- Only loopback endpoints permitted for local model calls.
- Every evidence packet sets `did_use_network/hosted_ai/local_model/mutate` honestly.
- Every new packet kind must appear in `PlainLanguageExplainer.KnownPacketKinds`.
- Promotion criteria are versioned and themselves mutation-gated.
- Reset on violation: any invariant violation resets the relevant maturity clock.
- Codex never auto-merges Auto-continue=No phases.
- Codex auto-rebases only on clean fast-forward; halts on semantic conflict.
- Loop runner respects coexistence triggers (gaming, video calls) and DnD.
```

## 5. Phase Inventory (35 new phases + 1 residual)

| # | ID | Title | Source doc | Auto-continue? |
|---|---|---|---|---|
| residual | 85d | Autonomous Operations Promotion | (existing post-85 plan) | No |
| 1 | 106 | Default-Enabled Reasoning LLM | reframe doc | No |
| 2 | 107 | Chat UI | reframe doc | No |
| 3 | 108 | Agent-Slot Redesign | reframe doc | No |
| 4 | 109 | Tool Registry First-Class | reframe doc | No |
| 5 | 110 | Pet/AI Isolation Contract | reframe doc | No |
| 6 | 111 | User-PC Coexistence Policy | reframe doc | Yes |
| 7 | 101 | Always-On Household Maintenance | gap doc | Yes |
| 8 | 102 | User Feedback Ingestion | gap doc | Yes |
| 9 | 112 | Benchmark Suite v1 | reframe doc | No |
| 10 | 113 | Benchmark Curation UI | reframe doc | Yes |
| 11 | 116 | Chat-Context-Window Management | reframe doc | Yes |
| 12 | 115 | Identity Rename + UX Pass | reframe doc | Yes |
| 13 | 99 | Multi-Domain Palette Grammars | gap doc | No |
| 14 | 100 | General Fallback Grammar | gap doc | Yes |
| 15 | 98 | Local Image Generation Runtime | gap doc | No |
| 16 | 117 | Sprite Rung 1: Golden-Seed UI | sprite doc (pending) | No |
| 17 | 118 | Sprite Rung 2: Image Embedding | sprite doc (pending) | No |
| 18 | 119 | Sprite Rung 3: Palette Conformer + Propagation | sprite doc (pending) | Yes |
| (parallel) | — | USER SOAK ENDS | wall-clock | — |
| 19 | 85d | Autonomous Operations Promotion | (existing) | No |
| 20 | 95 | Build Hot-Swap Wrapper | foundation (pending) | No |
| 21 | 96 | Codex Loop Runner (initial) | foundation (pending) | No |
| 22 | 114 | Codex Loop Reliability Hardening | reframe doc | No |
| 23 | 86 | Pre-Approved Scope Service (H3) | foundation (pending) | No |
| 24 | 87 | Sandboxed Experiment Runner | foundation (pending) | No |
| 25 | 88 | Experiment Kind: sprite-template | foundation (pending) | No |
| 26 | 93 | Composite Fitness Scoreboard | foundation (pending) | No |
| 27 | 94 | Maturity Promotion Service | foundation (pending) | No |
| 28 | 89 | Experiment Kind: LoRA hyperparam search | foundation (pending) | No |
| 29 | 97 | Image LoRA Training Pipeline | gap doc | No |
| 30 | 90 | Constitutional Decision Service | foundation (pending) | No |
| 31 | 91 | Approved Research Connector (R3) | foundation (pending) | No |
| 32 | 92 | Self-Improvement Promotion Gate | foundation (pending) | No |
| 33 | 103 | Memory Consolidation | gap doc | Yes |
| 34 | 104 | Strategic Planner | gap doc | No |
| 35 | 105 | Activity Digest UI | gap doc | Yes |
| 36 | 120 | Sprite Rung 4: Remaining Animations | sprite doc (pending) | Yes |

**Auto-continue=Yes count: 12.** Codex will land these without human nod.
**Auto-continue=No count: 24.** Each requires explicit user review of the PR.
**Total estimated time:** ~6-10 weeks if you actively review the Auto-continue=No
PRs as they land; ~3-5 months if you batch-review weekly. The user soak
window (7 days) runs in parallel with phases 1-18 so it's not on the
critical path.

## 6. Gaming Scenario: League of Legends + Wevito Coexistence

This section is explicit because the user asked: "can I play League while
wevito is building/working?"

**Answer: yes, under these specific behaviors.**

### 6.1 What happens when you launch League

```text
0s    LeagueClient.exe starts
3s    CoexistenceTriggerService detects fullscreen-app + app-list match
5s    RuntimeSupervisorService transitions to Quiet mode
10s   AutonomousOperationsLoop refuses next tick (block_reason=user_gaming_active)
10s   HouseholdMaintenanceService skips current tick
15s   ImageGenIdleGuardService blocks any background image-gen invocation
20s   RAM pressure check: if < 3 GB free, image-gen sidecar unloads
30s   Pet animation IPC: pet plays `sleep` instead of `idle`
30s   CodexLoopRunnerService consults coexistence triggers and pauses
      loop runner (does NOT halt; resumes when triggers clear)
40s   Ollama keep-alive set to 0 if VRAM pressure detected (LLM unloads;
      chat goes offline; pet sim continues unaffected)
```

### 6.2 What stays running

- **Pet game (Godot)** — 60fps target, 30fps floor enforced; 1 GB reserved RAM.
- **Wevito shell (WPF)** — minimal footprint; still receives pet input,
  audit ledger writes, settings changes.
- **Broker process** — pipes + watchdog still alive; restarts crashed
  children.

### 6.3 What pauses

- All background experiments (`ExperimentRunnerService`)
- All household chores (`HouseholdMaintenanceService`)
- Codex loop runner (advancing phases)
- R3 web research (no automatic fetches)
- Memory consolidation (any pending runs)
- Strategic planner (no new selections)

### 6.4 What unloads if RAM pressure crosses thresholds

- 3 GB free: experiments suspend (already covered above).
- 2 GB free: image-gen Python sidecar unloads (frees ~4 GB RAM + 4 GB VRAM).
- 1.5 GB free: Ollama unloads its model (frees ~5 GB RAM + 4.5 GB VRAM;
  chat goes offline; user notified in pet popup).
- 1 GB free: emergency stop + Stop card.

### 6.5 What happens when League closes

```text
0s    League process exits
10s   CoexistenceTriggerService detects clear
1min  Maintenance resume (R4 tier 1)
5min  Experimentation resume (R4 tier 2)
5min  Codex loop resume
~6min Image-gen sidecar can spin up on next request (cold-start)
~7min Ollama model warm-load on next chat invocation
```

### 6.6 Vanguard anti-cheat compatibility

Every wevito process is standard user-mode Windows. None inject into other
processes, read game memory, or touch kernel space. No known Vanguard
incompatibility risk.

Specifically:
- `wevito-vnext-shell.exe` — WPF UI, no DLL injection
- `wevito-vnext-broker.exe` — named pipes + hotkey registration via standard APIs
- Godot's pet game `.exe` — standard window
- Ollama — runs as separate localhost server, doesn't touch other processes
- Python image-gen sidecar — runs as separate process, GPU access via standard CUDA APIs

### 6.7 Pre-seeded app list for coexistence triggers

C-PHASE 111's `coexistence_app_list` setting ships with these defaults:

```json
[
  "LeagueClient.exe",
  "LeagueofLegends.exe",
  "RiotClientServices.exe",
  "zoom.exe",
  "Teams.exe",
  "obs64.exe",
  "obs32.exe",
  "Discord.exe"
]
```

User can edit via Settings → Coexistence → App list.

### 6.8 Practical Codex loop behavior during a 1-hour gaming session

- Codex was mid-phase 110 when League launched.
- Codex's CLI invocation completed before pause (it was waiting on test
  results).
- Pause sentinel file written to `tools/codex-loop-paused.sentinel`.
- Loop runner reads sentinel each iteration; doesn't advance.
- Tests + build complete in background (low-priority); results captured.
- 5 min after League closes, sentinel cleared; loop resumes from where
  it left off.

**Net effect: League gameplay is unaffected; Codex loses ~1 hour of
phase-advance time per gaming session.** Acceptable trade.

## 7. Codex Loop Operational Model

### 7.1 Files Codex reads on each iteration

```text
docs/codex-phase-queue.json        pending phases in order
docs/codex-loop-status.json        currently in-flight phase
docs/codex-phase-history.jsonl     append-only audit of every transition
tools/codex-loop-paused.sentinel   if exists, loop is paused (don't advance)
```

### 7.2 Files Codex writes on each phase

```text
vnext/artifacts/<phase-id>-<ts>/...   per-phase artifacts
docs/C_PHASE<NNN>_*.md                phase report
docs/codex-phase-history.jsonl        append-only transition
%LOCALAPPDATA%/Wevito/audit/...        evidence packets
```

### 7.3 Codex's iteration loop

```text
1. Read queue → pick next pending phase
2. Check coexistence triggers → if game/video call active, sleep 60s and retry
3. Check DnD → if active, sleep until DnD window ends
4. Check kill switch → if active, halt
5. Mark phase in_progress in status file
6. Invoke Codex CLI with the phase prompt (from prompts file)
7. Wait for Codex to finish (timeout per Q20-T4-modified)
   - If user idle > 10 min → timeout extends indefinitely
   - If user returns → timeout resumes
   - If no commit in 30 min → halt phase with reason
8. Run validation commands (dotnet build, dotnet test, etc.)
   - If fail → retry once with failure context
   - If second fail → open remediation task card + halt phase
9. Auto-rebase Codex's branch onto main
   - Fast-forward → continue
   - Semantic conflict → halt for human
10. Run benchmark gates (safety + perf per CA6)
    - Safety fail → auto-halt + Stop card
    - Perf regression > 20% → auto-halt
    - Capability regression > 15% → halt
    - Capability regression 5-15% → open task card + continue
11. Auto-merge if Auto-continue=Yes; else halt for human approval
12. Trigger build hot-swap (smoke test → swap at natural pause)
13. Append to history file
14. Mark phase completed; advance queue
15. Go to step 1
```

### 7.4 Failure recovery

| Failure | Response |
|---|---|
| Test fails first time | Retry with failure context |
| Test fails second time | Open remediation card; halt phase |
| Build fails | Same as test fail |
| Stuck (no commit 30 min) | Halt phase; user inspects |
| Merge conflict (semantic) | Halt for human |
| Smoke test fails post-merge | Abort swap; audit row; Stop card |
| Watchdog detects loop runner dead | Restart with backoff (1s, 5s, 30s); max 3 in 5 min then Stop |
| Kill switch activated | All processes halt immediately |
| Benchmark safety regression | Auto-halt + Stop card |
| Benchmark perf regression > 20% | Auto-halt |
| Coexistence trigger fires | Pause until triggers clear + grace period |

### 7.5 Human-inject points

- **Edit queue files** — routine reordering, adding/removing phases.
- **`tools/codex-inject.ps1 -PhaseId X`** — priority phase insertion.
- **`tools/codex-loop-pause.ps1`** — emergency pause.
- **`tools/codex-loop-resume.ps1`** — resume after pause.

## 8. What Wevito Looks Like At Each Milestone

### 8.1 After Phase 106-109 (chat surface lit up)

You open wevito. The pet appears. You click the overlay icon. The popup
opens with a chat panel front-and-center. You type "what's in my docs
folder?" The AI streams a response, calling the `localDocs` tool mid-reply.
Tool result inlined in chat. You bookmark the response. Pet keeps animating.

### 8.2 After Phase 110-111 (isolation + coexistence)

You launch League. Pet starts playing `sleep`. Chat unavailable
(LLM unloaded). Background work suspended. League runs at full frame
rate. You close League. ~5 min later, chat is back; pet wakes up; Codex
loop resumes.

### 8.3 After Phase 112-113 (benchmarks)

You open the Benchmarks tab. See line charts for chat correctness,
tool-use, retrieval, safety, perf. 30 days of trend. Latest run: composite
fitness 87%. Click 🔖 on a chat response to add it as a benchmark case.

### 8.4 After Phase 116 (context window mgmt)

Chat sessions can grow to hundreds of turns without choking. Old turns
get summarized; summary lives in memory store; AI retrieves relevant
past context per turn. Sessions inactive 6 months migrate to cold storage
but remain searchable.

### 8.5 After Phase 86-96 (autonomy infrastructure)

You declare a scope: "wevito, you can do sprite-cleanup work for rat,
fox, crow during your background time." Scope card is approved.
ExperimentRunner picks `sprite-template-candidate-generation` (the
only registered kind initially). Strategic planner reads composite
fitness, picks the weakest axis as the target. Wevito generates candidate
templates overnight. You wake up, see "10 candidate templates ready for
review" in Activity Digest.

### 8.6 After Phase 97-100 (image generation)

Wevito's tool list now includes `generate_image`. You ask in chat:
"draw me a small wizard hat in our pixel style." Wevito routes the
request via `GrammarRouterService` → general-pixel-art grammar. Image
LoRA generates a 32×32 wizard hat in the constrained palette. PaletteConformer
validates. Image appears in chat. You can save it, refine it, or use it
as a benchmark case for future requests.

### 8.7 After Phase 117-120 (sprite rungs complete)

All 60 canonical sprite templates (10 species × 3 ages × 2 genders)
are seeded with goldens, palette-conformed, and propagated to all 6
color variants. The original Gemini-loop problem is gone. Wevito can
also propose new sprite candidates via LoRA when you ask.

## 9. Dependency Graph (compressed)

```text
106 LLM enabled        ──> 107 Chat UI
107 Chat UI            ──> 108 Agent slots, 109 Tool registry, 113 Curation UI,
                            116 Context window mgmt
108 Agent slots        ──> 109 Tool registry
109 Tool registry      ──> 112 Benchmarks, 116 retrieve_from_memory tool
110 Isolation          ──> independent
111 Coexistence        ──> 114 Loop reliability
101 Household chores   ──> 103 Memory consolidation (registers chore),
                            116 cold storage (registers chore)
102 User feedback      ──> 112 Benchmarks (composite fitness signal)
112 Benchmarks         ──> 113 Curation, 93 Composite scoreboard, 114 Loop FR4
113 Curation           ──> downstream benchmark runs use approved cases
115 Identity rename    ──> independent (touches everything mechanically)
116 Context mgmt       ──> 107 (consumed), 101 (registers chore)
99  Palette grammars   ──> 100 Fallback, 98 image runtime usage, 117/118/119 sprite work
98  Image runtime      ──> 97 Image LoRA, 117-120 sprite work
97  Image LoRA         ──> 89 Experiment kind LoRA hyperparam search
85d Autonomy promotion ──> 86 Pre-approved scope, 87 Experiment runner
86  Pre-approved scope ──> 87 Experiment runner
87  Experiment runner  ──> 88 sprite kind, 89 LoRA kind, 104 Strategic planner
93  Composite scoreboard ──> 94 Maturity service, 104 Strategic planner
94  Maturity service   ──> 91 R3, 92 Self-improvement gate
95  Build hot-swap     ──> 96 Codex loop runner (which uses it)
96  Loop runner        ──> 114 Loop reliability
103 Memory consolidation ──> 104 Strategic planner (consumes patterns)
104 Strategic planner  ──> 87 Experiment runner (consulted)
105 Digest UI          ──> 83, 103, 87, 93 (read-only consumers)
```

Sprite rungs:

```text
117 Rung 1 Seeding UI       ──> 118 Rung 2 (consumes goldens)
118 Rung 2 Embedding score  ──> 119 Rung 3 (consumes scores)
119 Rung 3 Conformer + prop ──> 120 Rung 4 (uses all)
```

## 10. Execution Sequence (final)

The full order is in §5's table. Highlights of the path:

- **Days 0-3**: phases 106-111 land. Wevito has chat, agents, tools,
  isolation, coexistence. League works fine alongside Codex loop work.
- **Days 3-5**: phases 101-102, 112-113, 116 land. Wevito has household
  chores, user feedback, benchmark suite v1, curation UI, context mgmt.
- **Days 5-7**: phases 115, 99-100, 98, 117-119 land. Identity rename;
  palette grammars; image runtime; sprite rungs 1-3.
- **Day 7**: SOAK ENDS in parallel. 85d promotion PR opens with real
  evidence.
- **Days 7-10**: phases 95-96, 114 land. Build pipeline + Codex loop
  itself hardened.
- **Days 10-20**: phases 86-94, 97 land. Autonomy infrastructure complete.
  Image LoRA training pipeline ready.
- **Days 20-30**: phases 90-92, 103-105, 120 land. Constitutional decision,
  research connector, self-improvement gate, memory consolidation, strategic
  planner, activity digest UI, sprite rung 4.

Total wall-clock estimate: 4-6 weeks of Codex loop time if user reviews
Auto-continue=No PRs within 24h; 8-12 weeks if user reviews weekly.

## 11. Risk Register

| Risk | Likelihood | Mitigation |
|---|---|---|
| Codex loop produces silently-wrong code | Medium | Per-phase stop gates + benchmark regression gates + smoke tests |
| Phase prompt is ambiguous → Codex generates plausible-but-wrong | Medium | Phase prompts reference decision ledger + name exact files/methods/tests; explicit stop gates per phase |
| User PC becomes unusable | Low | Q19 coexistence triggers + Q18 isolation contract + tested via PetFpsSoakTest |
| Vanguard flags wevito | Low | All processes user-mode; no injection; no kernel touch |
| LoRA training degrades the model | Low | LearningEvalService 2% regression gate + auto-rollback |
| Web research causes prompt injection | Medium (when R3 enabled) | R3 fetches enter "research notes" channel, not instructions; allowlist starts empty |
| Hot-swap loses in-flight state | Low | W-14d-iii state preservation + sqlite persistence at exit |
| Loop runs unattended for days without user awareness | Low | Activity Digest UI surfaces last-N-phases + benchmark trend |
| Disk fills up with audit/artifact data | Medium | C-PHASE 103 memory consolidation archives old rows + cold storage |
| Codex gets stuck in retry loop on a fundamentally broken phase | Low | Q20-F4 retry-once-then-open-remediation-card |
| Image-gen breaks pet rendering | Low | C-PHASE 110 G3 idle-guard + RAM cascade |
| Pet sim crashes mid-AI-session | Low | C-PHASE 110 watchdog restarts; AI keeps running; pet state restored from disk |

## 12. Closing Instructions for Codex

When picking up a phase:

1. **Read `DECISION_LEDGER_2026-05-15.md`** for decision IDs in scope.
2. **Read the named Pattern file** end-to-end before writing the new file.
3. **Mirror the pattern's tests** one-for-one with new names.
4. **Use the same DI shape** (constructors with optional KillSwitchService,
   AuditLedgerService, HttpClient, etc.).
5. **Use the standard evidence-packet shape** — set
   `did_use_network/hosted_ai/local_model/mutate` honestly.
6. **Consult `KillSwitchService.IsActive()`** at the top of every public
   method that writes state.
7. **Add new packet kinds to `PlainLanguageExplainer.KnownPacketKinds`**
   with a plain-language sentence.
8. **Add a phase report doc** under `docs/C_PHASE<NNN>_*.md`.
9. **Verify all stop gates before opening the PR**; if any are true,
   open the PR in Draft and write a `phase_blocked` audit row.

When a phase has `Auto-continue=Yes` AND tests pass AND no stop gate
fires AND benchmark regressions are within tolerance → loop runner
auto-merges. Otherwise halt and surface a Stop card.

The user is reachable via PR comments + the chat panel in wevito itself.
If something is genuinely ambiguous in a phase prompt, halt the phase and
open a clarification card via `tools/codex-inject.ps1` mechanism.
