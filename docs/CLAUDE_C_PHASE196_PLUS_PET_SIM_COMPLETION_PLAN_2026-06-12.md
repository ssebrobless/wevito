# Claude Plan — Pet-Sim Completion Track: C-PHASE 196 through 205

Date: 2026-06-12
Author: Claude (Fable 5), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (executor, medium reasoning effort) + project owner (review)
Companion prompts: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md`

Predecessor state at planning time:

- C-PHASE 195 GREEN on branch `c-phase-195-watchdog-observer-in-eval-coverage-health`, PR #298 **open, not merged**. 1715/1715 tests. KnownPacketKinds=160.
- Local working tree is still on the C-PHASE 195 branch. Main has not been pulled.
- BUG-005 (Critical) open on the bug board: five apply/rollback runner tests observed empty packet lists on origin/main as of 2026-05-21. The same suites are green on the C-195 branch; disposition must be re-verified on main after #298 merges.
- C-PHASEs 142–195 were all self-improvement guardrail infrastructure. The last pet-sim-focused phases were C-121–130 (mid-May).

## 0. Summary

The owner's directive (2026-06-12): **first make the pet simulator fully playable as
intended — including clean, high-quality sprites with all animations and all
variations — then regroup on Wevito's local AI capabilities.** This plan is the
pet-sim track. It does not advance, widen, or wire any self-improvement, apply-runner,
watchdog, or autonomy capability. The AI track resumes afterward from
`docs/CLAUDE_MASTER_PLAN_2026-05-15.md` (phases 86–105 remain the backlog; the safety
substrate they need is already built and default-off).

### 0.1 Ground truth this plan is built on (measured 2026-06-12)

```text
Required sprite contract        COMPLETE  360/360 variants (10 species x 3 ages x
                                          2 genders x 6 colors) hold the core families:
                                          idle4 walk6 eat4 happy4 sad2 sleep2 sick4
                                          bathe4 + drink4 + groom4 (38 frames each).
                                          C-130 drained its 60-batch repair queue;
                                          contract + canvas audits pass at error_count=0.

Optional family readiness       2,520 targets = 360 variants x 7 optional families.
(audit_optional_animation_       - drink:        360/360 runtime_only_complete
 readiness.py)                   - play_ball, hold_ball, pickup_ball, drop_ball,
                                   carry_ball_walk, carry_ball_run:
                                   4/2,160 present (goose/baby/female/blue pilot has
                                   play_ball6 + hold_ball4 + pickup_ball4 + drop_ball4;
                                   NO variant anywhere has carry_ball_walk or
                                   carry_ball_run).
                                 - 2,156 targets runtime_fallback_only.
                                 - runtime prop-anchor metadata: absent
                                   (sprites_runtime/_metadata/ does not exist).

Fetch/play engine               ALREADY BUILT. PetSimulationEngine has the full fetch
                                state machine (MoveToBall -> Pickup -> Hold ->
                                CarryWalk -> CarryRun -> Drop -> ReturnIdle) with
                                graceful per-family fallbacks. Ball is a RUNTIME
                                OVERLAY (PropOverlayKind.Ball, rendered via
                                HabitatLoadoutResolver) — never baked into frames
                                (C-PHASE 56 contract). actions.json Play already sets
                                optionalAnimationFamily=play_ball.

Dead assets                     504 loose play_00..03 PNGs across 126 variants
                                (frog 36, goose 36, snake 36, rat 12, deer 6). "play"
                                is in neither EXPECTED_ANIMATIONS nor
                                OPTIONAL_EXPANDED_ANIMATIONS and no action or engine
                                path renders it. Pure leftovers.

Authored-quality coverage       sprites_authored_verified holds locomotion (idle+walk)
(sprites_authored_verified/)    only: complete for crow, deer, fox, frog, pigeon, rat,
                                snake (360 files per species); raccoon partial (144);
                                goose and squirrel ZERO. No care/expression family has
                                authored-verified art anywhere — those frames are
                                pose-board synthesis from C-130.

Guarded mutation machinery      PROVEN AT MATRIX SCALE. AutomationRunner
                                --sprite-repair-batch + audit_sprite_contract.py +
                                report_runtime_canvas_mismatches.py + run-matrix-sweep
                                + contact-sheet export ran 60 batches in C-130 with
                                backup/sha256/post-proof/rollback per batch.

Stable release lock             vnext/content/stable_release_lock.json present.
                                Asset prep is forbidden (would resynthesize the runtime
                                tree from pose boards and destroy C-130 repairs).
                                ALL sprite mutation in this plan goes through targeted
                                guarded apply, never asset prep.
```

### 0.2 What "fully playable as intended" means (exit criteria)

1. Every action a player can take has real, on-contract art on every variant —
   including the fetch/play sequence end-to-end with the ball overlay.
2. No dead or off-contract sprite files in `sprites_runtime/`, and a pinned test so
   stray families fail fast in the future.
3. Sprite quality triaged honestly: synthesized frames are acceptable only where the
   quality audit says so; the worst rows get authored refresh through the existing
   Gemini handoff lane.
4. The 30-item RC Checklist v1 passes (1/2/3 pets, focused/unfocused), bug board
   updated, with a written verdict report.

## 1. Hard Invariants (every phase in this batch)

- This is the pet-sim track. **No phase touches** the apply-runner family, supervised
  improvement loop, InvariantViolationWatchdog, watchdog observer wiring,
  caller-allowlist pins, maturity clocks, or any `SelfImprovement` namespace type.
  (Therefore the C-189/C-191 caller-allowlist extension rule is intentionally out of
  scope: nothing in this batch may add a `ScanAndEmit` host.)
- No hosted AI as Wevito's runtime brain. The Gemini handoff lane (C-PHASE 202/203) is
  an *authoring-time* workflow operated by the owner/driver scripts; its outputs enter
  the repo only through the existing import + verification tools. No runtime code may
  call any hosted endpoint.
- No silent network access. No new sockets. No new System.Net.Http runtime use.
- Every sprite mutation: exact scope declared up front, dry-run, backup, sha256
  evidence, apply, post-proof (contract + canvas audits), rollback path, user-visible
  report. The C-130 batch pattern is the template.
- **Never run asset prep.** Never pass `-AllowAssetPrepAfterStable`. Build with
  `-SkipAssetPrep` always. The stable release lock stays in place.
- `KillSwitchService.IsActive()` is honored by every new or touched stateful service.
- AuditLedgerService remains append-only. No UPDATE / DELETE / DROP TABLE SQL.
- Every new audit packet kind appears in `PlainLanguageExplainer.KnownPacketKinds`
  with a plain-language sentence. (Expected new kinds in this batch are sprite-workflow
  kinds only, e.g. ball-family apply/rollback evidence.)
- Every new capability flag defaults `bool.FalseString` in `CapabilityFlagInventory`.
- Pets remain visually normal pet-sim characters. No AI-task animation overlays.
- Where a prompt pins a focused test filter with an expected minimum count, the
  minimum is derived from the actual `[Fact]`/`[Theory]` count in the file at the
  anchor SHA — never copied from a prior phase report.
- Codex never auto-merges Auto-continue=No phases.
- Codex never touches `.codex\worktrees`, never kills IDE/foreign processes.

## 2. User pre-step (before C-PHASE 196 starts)

**The owner must merge PR #298 (C-PHASE 195) first.** Every phase preflight requires
`main == origin/main` with a clean tree; C-196 additionally asserts that main contains
the C-195 commit. If #298 is rejected instead, C-196's anchor changes and this plan's
preflight SHAs must be re-pinned — halt and ask.

## 3. Phase Inventory

| # | Phase | Title | Kind | Auto-continue |
|---|---|---|---|---|
| 1 | C-196 | Main-sync product truth + BUG-005 disposition | test/docs only | No |
| 2 | C-197 | Pet-sim completion baseline (sprite truth report) | report only | No |
| 3 | C-198 | Ball-family pilot completion (goose/baby/female/blue) | guarded sprite apply | No |
| 4 | C-199 | Ball-family synthesis tool + full-matrix dry-run candidates | tooling + report only | No |
| 5 | C-200 | Guarded matrix apply of six ball families (10 species batches) | guarded sprite apply | No |
| 6 | C-201 | Loose `play_*` removal + contract stray-family pin | guarded sprite delete + test | No |
| 7 | C-202 | Authored locomotion completion: goose, squirrel, raccoon | handoff round-trip + guarded apply | No |
| 8 | C-203 | Quality triage queue + targeted authored refresh batches | report, then guarded apply | No |
| 9 | C-204 | Play/fetch wiring verification + all-action visual proof matrix | test/evidence (code fix only if gap found) | No |
| 10 | C-205 | RC Checklist v1 full sweep + pet-sim playability verdict | evidence/docs | No |

Every phase is Auto-continue=No. The batch has three deliberate human review points
beyond PR review: §5 decision D1 (before C-201), the C-199 candidate contact-sheet
review (before C-200), and the C-203 triage queue approval (before its apply batches).

## 4. Dependency Graph

```text
user merges PR #298 ──> 196 main truth ──> 197 baseline ──> 198 ball pilot
                                                       ├──> 201 play_* removal (needs D1)
198 ball pilot ──> 199 synthesis tool + candidates (pilot proves the 6-family loop in-app)
199 candidates ──[user reviews contact sheets per species]──> 200 matrix apply
197 baseline ──> 202 authored locomotion (goose/squirrel/raccoon gaps pinned by 197)
197 + 202   ──> 203 quality triage + refresh batches
200 + 201   ──> 204 play/fetch wiring proof (needs full ball coverage + clean tree)
202 + 203 + 204 ──> 205 RC sweep + verdict
```

C-201 and C-202 are parallel-safe with the 198→200 ball track (disjoint file scopes:
201 touches only `play_*` files, 202 touches only goose/squirrel/raccoon
locomotion rows). Codex still runs them as separate sequential phases — one PR each.

> **SUPERSEDED for the 199→200 edge by Amendment R2 (§10):** the C-199 review
> returned 0/10 approved; C-203 (split A/B) now runs BEFORE C-200, followed by
> C-199R candidate regeneration and a second review gate. See §10.3 for the live
> graph.

## 5. Decisions needed from the owner (resolve at go-ahead)

- **D1 — Loose `play_*` frames: remove or register?** Recommendation: **remove**
  (guarded, backed-up, rollback-able). They are unreachable by any action or engine
  path; registering a generic `play` family would add a 13th family that nothing
  plays. C-201 implements whichever is chosen.
- **D2 — Ball-family production strategy.** Recommendation: **synthesized-first**:
  derive body-pose frames procedurally from each variant's existing walk/happy/idle
  frames (ball stays a runtime overlay, so no ball pixels are needed in-frame), review
  per-species contact sheets, then apply matrix-wide; authored refresh for weak species
  flows through C-203 later. Authoring ~10,800 frames through the Gemini lane up front
  would stall the track for weeks.
- **D3 — C-PHASE 56 pilot decision label.** The held pilot decision becomes
  `accept_candidate_for_apply_plan` for the six hash-verified `carry_ball_walk`
  candidate frames (re-reviewed against current runtime in C-198 before apply).

## 6. Per-Phase Specifications

### 6.1 C-PHASE 196 — Main-sync product truth + BUG-005 disposition

Goal: return the repo to a trustworthy main and close out the only Critical bug.
After #298 merges: sync main, full build + 1715-expected test run, then run the five
BUG-005-affected `ArtifactRenameApplyRunnerTests`/`ArtifactRenameRollbackRunnerTests`
facts cold-cache (delete `bin`/`obj` first, exactly the BUG-005 repro recipe). If
green: close BUG-005 on the bug board with evidence paths. If red: reopen with fresh
evidence and **halt the batch** (the plan resumes only after a dedicated diagnostic).
Also verifies KnownPacketKinds count is 160 on main. No source changes. Docs +
evidence only.

### 6.2 C-PHASE 197 — Pet-sim completion baseline

Goal: one authoritative, machine-readable baseline that later phases consume, in the
C-128 mold. Runs (read-only): `audit_sprite_contract.py`,
`audit_optional_animation_readiness.py`, `audit_source_to_runtime_quality.py`
(full matrix), `export_pet_runtime_contact_sheets.py`, plus a loose-file inventory
(any `sprites_runtime` file whose family is in neither EXPECTED nor OPTIONAL lists)
and an authored-verified coverage matrix. Output:
`vnext/artifacts/c-phase-197-pet-sim-baseline/` with `baseline.json` (per-variant
rows: optional-family gaps, loose files, quality scores, authored coverage) +
`baseline.md` + contact sheets, and a phase report doc. Stop gate: required-contract
errors > 0 (would mean C-130 regressed — halt, don't continue the batch on a broken
base).

### 6.3 C-PHASE 198 — Ball-family pilot completion (goose/baby/female/blue)

Goal: make ONE variant fully fetch-complete and prove the sequence in the running app
before any matrix-scale work. Scope: resolve D3; verify the six C-56
`carry_ball_walk` candidate sha256es against the C-56 packet; synthesize
`carry_ball_run` (6 frames) from the pilot's existing walk + carry candidates;
guarded apply of both carry families to
`sprites_runtime/goose/baby/female/blue/` (backup + sha256 + post-proof contract &
canvas + rollback script); then a DevControl-driven in-app proof: spawn the pilot
variant, trigger Play/fetch, capture every fetch stage via the existing capture
tooling into the artifact folder. Update the C-56 `decision-needed.md` with the chosen
label. Mutation scope is exactly 12 new PNGs in one directory. Stop gates: any hash
mismatch vs the C-56 manifest; contract/canvas post-proof failure; fetch sequence does
not visually advance through Pickup/Hold/CarryWalk/CarryRun/Drop in the capture.

### 6.4 C-PHASE 199 — Ball-family synthesis tool + full-matrix dry-run candidates

Goal: the production tool + reviewable candidates, zero runtime mutation. New
`tools/generate_optional_ball_families.py`: for a given variant, synthesize body-pose
frames for all six ball families from that variant's existing on-contract frames
(deterministic transforms; frame counts per OPTIONAL_EXPANDED_ANIMATIONS: play_ball 6,
hold_ball 4, pickup_ball 4, drop_ball 4, carry_ball_walk 6, carry_ball_run 6; 28x24
RGBA; no ball pixels — overlay-only contract). Honors `--dry-run` and writes
candidates ONLY under `vnext/artifacts/c-phase-199-ball-candidates/<species>/...`.
Runs the full 360-variant dry-run and exports one review contact sheet per species
(10 sheets). The pilot variant's applied frames from C-198 are the visual reference
row on the goose sheet. Stop gates: any write outside the artifact root; any frame
failing 28x24 RGBA; tool nondeterminism (two runs must hash identically).
**User reviews the 10 contact sheets and approves species (all or a subset) before
C-200 starts.**

### 6.5 C-PHASE 200 — Guarded matrix apply of six ball families

Goal: apply approved candidates matrix-wide, one species batch at a time (10 batches,
C-130 pattern). Per batch: declared scope `sprites_runtime/<species>/**` ball-family
files only; backup manifest + sha256 of any overwritten file (expected: none —
these are additive); apply ~1,080 PNGs per species (36 variants x 30 frames); post-proof
= contract audit error_count 0, canvas mismatch 0, optional-readiness audit shows the
species' 216 ball targets present, matrix sweep rows green; per-batch summary
artifact + rollback script. Also in this phase: extend the sprite runtime coverage
test expectations (vnext tests) so ball families are asserted present going forward,
and add prop-anchor metadata `sprites_runtime/_metadata/prop_anchors.json` with
per-species default anchors (the readiness audit already reads this path; runtime
overlay keeps working regardless). Stop gates per batch: any post-proof failure rolls
back that batch and halts; the phase never proceeds to the next species past a red
batch. Only species the user approved in the C-199 review are in scope.

### 6.6 C-PHASE 201 — Loose `play_*` removal + contract stray-family pin

Goal (assuming D1=remove): guarded deletion of the 504 dead `play_*` PNGs across the
126 variants pinned by C-197's loose-file inventory (backup zip + sha256 manifest +
rollback script + post-proof: contract audit still error_count 0, file count delta
exactly matches the inventory). Then pin the contract: a new test (and/or an
`audit_sprite_contract.py` strictness flag) that fails when any `sprites_runtime`
animation file's family is outside EXPECTED + OPTIONAL — so dead families can never
silently accumulate again. Stop gates: deletion list diverges from the C-197
inventory by even one path; post-proof contract failure; any non-`play_*` file
touched.

### 6.7 C-PHASE 202 — Authored locomotion completion: goose, squirrel, raccoon

Goal: close the authored-verified locomotion gap (goose 0, squirrel 0, raccoon
partial — exact rows pinned by C-197). Cooperative phase: Codex prepares the
handoff packs (`prepare_motion_gemini_handoff.py --family locomotion --species goose
squirrel raccoon`), the owner (or the existing `batch-drive-live-gemini.ps1` driver)
round-trips the boards through Gemini, then Codex imports
(`import_authored_family_board.py`), verifies, propagates colors
(`propagate_authored_colors.py`), and guarded-applies into `sprites_authored_verified`
+ `sprites_runtime` for exactly those species' idle/walk rows, with the standard
backup/sha256/post-proof/rollback evidence. The phase halts cleanly at the round-trip
point if boards aren't returned — partial progress is committed as
prepared-packs-only with a `phase_blocked` audit row. Stop gates: any redesign of pet
identity vs canonical source sheets (import tool's verification), contract/canvas
post-proof failure, mutation outside the three species' locomotion rows.

### 6.8 C-PHASE 203 — Quality triage queue + targeted authored refresh batches

Goal: honest quality, not blanket re-authoring. Build a triage queue from C-197's
source→runtime quality scores + C-199/200 ball candidates: every family row below the
quality bar (threshold + rubric written into the queue builder, in the C-128
P0/P1/P2/P3 style) becomes a queue row with its repair lane (authored refresh via
handoff vs regenerate via tooling). **Owner approves the queue before any batch
runs.** Then drain it in guarded batches exactly like C-130 (one batch = one queue
row group, full evidence per batch). Expected volume is unknown until C-197 runs;
the phase is sized to halt-and-resume across sessions.

### 6.9 C-PHASE 204 — Play/fetch wiring verification + all-action visual proof matrix

Goal: prove the game plays as designed now that art is complete. Test-side: unit
coverage that `ResolveFetchStageIntent` selects the real families (no fallbacks) when
the full optional set is available, and that the Play action path triggers the fetch
sequence (StartFetchSequence wiring from the action handler — if a wiring gap is
found, fixing it is in scope; that is the only allowed production-code change in this
phase). Evidence-side: DevControl matrix proof — for a sample grid (each species x
one variant), drive all nine actions + full fetch sequence and capture
before/after animation states + screenshots into
`vnext/artifacts/c-phase-204-action-proof/`. Stop gates: any action returning success
without its mapped animation-state transition (the BUG-004 class of failure); any
fetch stage rendering a fallback family on a fully-covered variant.

### 6.10 C-PHASE 205 — RC Checklist v1 full sweep + playability verdict

Goal: the exit gate. Run all 30 RC items per the checklist's own matrix (1/2/3 pets,
focused/unfocused, the four contexts), using the C-134 evidence pattern (DevControl
snapshots + captures per item; manual-judgment items get captures + a written
PASS/FAIL with reasoning). File bugs for failures on the bug board; Critical/Major
failures halt the batch into a fix round (new mini-phases) before re-sweep. Output:
`docs/C_PHASE205_PET_SIM_PLAYABILITY_VERDICT_2026-MM-DD.md` with the full item table,
bug links, and the explicit verdict line `PET SIM FULLY PLAYABLE: YES/NO`. The
pet-sim track is done when this is YES with zero Critical/Major bugs open — that is
the regroup trigger for the local-AI track.

## 7. What this plan does NOT do (parked for the AI-track regroup)

- No Ollama/local-LLM runtime work, no image-gen runtime (SD 1.5 / LoRA), no
  experiment-runner kinds, no strategic planner, no memory consolidation — master-plan
  phases 86–105 stay parked; their safety substrate (apply runners, watchdog, scope
  hashes, eval gates, maturity clocks) is already merged and default-off.
- No new watchdog observer hosts (so no caller-allowlist pin extensions needed).
- No local-AI-generated sprite art. The future "Wevito proposes sprites via its own
  image LoRA" capability (master plan §8.6) builds on the C-199 tool's contracts +
  C-203's quality rubric — both are designed as stable interfaces the AI track can
  later target through the existing guarded-apply machinery.

## 8. Risk Register

| Risk | Likelihood | Mitigation |
|---|---|---|
| BUG-005 red on merged main | Low-Med | C-196 runs the exact cold-cache repro first; batch halts before any sprite work |
| Synthesized ball poses look bad for some species | Medium | C-199 is dry-run + per-species contact-sheet review; user approves species individually; weak species go authored via C-203 |
| Matrix apply corrupts a row | Low | Additive-only writes, per-batch backup + sha256 + contract/canvas post-proof + rollback script; red batch halts phase |
| Gemini round-trip stalls C-202 | Medium | Phase commits prepared-packs-only and blocks cleanly; 198–201 and 203's queue build don't depend on it |
| Coverage-test expectations drift vs new ball families | Medium | C-200 updates the runtime coverage tests in the same PR as the first batch; full suite must be green per batch |
| `play_*` removal deletes a needed file | Low | Deletion list is pinned to C-197's inventory; full backup zip + rollback script; contract post-proof |
| RC sweep finds Major gameplay bugs | Medium | Expected and fine — C-205 files them and the batch loops a fix round; verdict stays NO until clean |

## 9. Closing instructions for Codex

Per phase: read this plan section + the matching prompt section end-to-end; follow the
C-130/C-128 artifact patterns exactly; never widen a mutation scope mid-phase; verify
all stop gates before opening the PR (any true → Draft PR + `phase_blocked` audit
row); one PR per phase; append `docs/codex-phase-history.jsonl` after the PR opens;
end every phase with the standard GREEN/RED report block (see prompts §0).

## 10. Amendment R2 (2026-06-12, post C-199 owner review) — quality before apply

### 10.1 Trigger

The C-199 review gate returned **0/10 species approved**. Owner verdict:

- **Larger issues** (missing chunks of sprites, very blurry, art inconsistencies):
  deer, fox, frog, pigeon, snake.
- **Smaller issues** (small cutoffs, silhouette cleanup, sizing issues, cleaning
  between the legs of animals): crow, goose, raccoon, rat, squirrel.

Because C-199 candidates are synthesized verbatim from each variant's existing
happy/idle/eat/walk frames, every reported defect lives in the **source required
families**, not in the synthesis transforms. The C-197 machine audit corroborates but
under-detects (25/60 rows flagged; frog 6/6, snake 6/6, goose 6/6, pigeon 4/6, deer
2/6, rat 1/6 — but fox/raccoon/squirrel/crow 0/6 despite owner-visible issues).
**Owner review supersedes machine flags.** Triage queue (owner-seeded):
`vnext/artifacts/c-phase-203-quality-triage/triage-queue.json`.

### 10.2 Restructure

C-PHASE 203 is **pulled forward ahead of C-200** and split into two lanes:

- **C-203A — Procedural cleanup sweep** (smaller bucket: crow, goose, raccoon, rat,
  squirrel). Deterministic local repairs in the `repair_*_motion_rows.py` lineage:
  silhouette/halo cleanup (incl. stray pixels between legs), edge-cutoff fixes
  (edge-touch padding), per-species sizing normalization. One guarded batch per
  species (C-130 pattern: declared scope, backup+sha256, post-proof, rollback),
  before/after contact sheet per batch. No Gemini dependency. Repairs land at the
  source-row level and propagate to all 6 colors via the existing tint pipeline;
  BUG-007 color-conformance is checked in every batch post-proof and fixed for any
  in-scope species (e.g. goose/blue).
- **C-203B — Gemini regeneration sweep** (larger bucket: deer, fox, frog, pigeon,
  snake). Handoff packs via `prepare_gemini_handoff.py` / family-focused packs for
  affected families; owner (or `batch-drive-live-gemini.ps1`) round-trips boards;
  guarded import (`import_gemini_sprite_block.py` lineage) + color propagation +
  post-proof per species. BUG-007 fix for fox/blue rides this lane. The original
  C-202 locomotion boards (goose, squirrel, raccoon) join the **same round-trip
  session** so the owner drives Gemini once, not twice. **Anti-blur policy
  (binding, owner 2026-06-12):** ≤4 frames per Gemini prompt (small-board splits
  only, never the 10-frame locomotion / 14-frame expression boards), enlarged
  board cells for ≤4-cell boards, and a deterministic sharpness gate at import
  that auto-rejects blurry returns — volume is absorbed by more small prompts via
  the batch driver, never by packing frames. Full spec in prompts §8 amendment.

Then:

- **C-199R — Candidate regeneration + second review round.** Re-run
  `generate_optional_ball_families.py` against repaired sources (deterministic; prior
  candidate set discarded), rebuild the 10 review sheets, present for per-species
  approval. This is the **second user gate**; C-200's scope is the species approved
  here.
- **C-200 / C-204 / C-205** specs unchanged; C-200 consumes C-199R approvals instead
  of C-199's.

### 10.3 Revised dependency graph

```text
C-199 review (0/10) ──> 203A procedural cleanup (smaller bucket) ─────────────┐
                   ──> 203B gemini regen (larger bucket) + 202 boards (same trip) ─┤
[owner approves triage queue before any 203A/203B batch runs — standing C-203 gate]
203A + 203B + 202 import ──> 199R re-synthesis + new sheets ──[owner re-review]──> 200 matrix apply
200 + 201 (done) ──> 204 play/fetch wiring proof ──> 205 RC sweep + verdict
```

### 10.4 Gates introduced/kept

1. Owner approves the triage queue (lanes + species scope) before any repair batch.
2. Owner drives (or authorizes the automated driver for) the single Gemini
   round-trip session covering C-203B + C-202 boards.
3. Owner re-reviews the regenerated C-199R sheets before C-200.
