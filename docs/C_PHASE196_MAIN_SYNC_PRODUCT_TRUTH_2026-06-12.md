# C-PHASE 196: Main-Sync Product Truth + BUG-005 Disposition

Date: 2026-06-12
Branch: `claude-implementation/c-phase-196-main-sync-product-truth`
Plan: `docs/CLAUDE_C_PHASE196_PLUS_PET_SIM_COMPLETION_PLAN_2026-06-12.md` §6.1
Prompt: `docs/CLAUDE_C_PHASE196_PLUS_CODEX_MEDIUM_PROMPTS_2026-06-12.md` §1

## Goal

Re-establish trustworthy main after the PR #298 merge and disposition BUG-005
(Critical), as the entry gate for the pet-sim completion track (C-PHASE 196–205).
Test/docs/evidence only. No production source change.

## Main Truth

| Item | Value |
| --- | --- |
| C-PHASE 195 on main | `d7a5e916a` (squash merge of PR #298, 2026-06-12) |
| Merge method note | Squash, matching the PR #293–#297 convention. The branch SHA `ee4aa82f9` is therefore NOT an ancestor of main; later phases anchor on `d7a5e916a`. |
| Branch base | `f458a3629` (PR #299, pet-sim track planning docs) |
| Build | PASS, 0 warnings, 0 errors (cold cache) |
| Full test suite | 1715/1715 (expected 1715) |
| KnownPacketKinds | 160 (asserted by `KnownPacketKindCount` const in the C-195 wiring tests, passing in the full suite) |
| build-vnext (`-SkipAssetPrep -SkipTests`) | PASS |
| `git diff --check` | PASS |

## BUG-005 Disposition: CLOSED (GREEN)

The exact BUG-005 cold-cache recipe from `docs/BUG_BOARD.md` was re-run on merged
main:

1. `dotnet build-server shutdown` — compiler + MSBuild servers shut down.
2. Running `Wevito*`/`VNext*` processes before repro: NONE (none killed).
3. Deleted every `bin\` and `obj\` directory under `vnext\` — 26 directories.
4. Cold `dotnet build .\vnext\Wevito.VNext.sln` — PASS (0/0, ~40 s).
5. Filtered run `FullyQualifiedName~ArtifactRenameApplyRunnerTests|FullyQualifiedName~ArtifactRenameRollbackRunnerTests`
   — **123/123 PASS**. Derived minimum at anchor: 27 + 24 = 51 `[Fact]`s; the
   observed 123 includes theory expansions.
6. Full suite `--no-build` — **1715/1715 PASS**.

All five BUG-005-affected facts passed, including
`Apply_happy_path_writes_six_packets_in_order` and
`ExplicitRollback_happy_path_writes_started_and_completed_packets`. The
empty-packet-list failure observed 2026-05-21 at `9b0dd40aa` does not reproduce; the
C-PHASE 189–195 chain now on main resolved it. BUG-005 moved to Closed on the bug
board; Critical count is 0.

## Scope

Implemented:

- `vnext/artifacts/c-phase-196-main-truth/main_truth.json` (evidence packet)
- `docs/BUG_BOARD.md` — BUG-005 closed, summary counts updated, session note appended
- This report
- One `docs/codex-phase-history.jsonl` append

Not implemented (by design):

- No production or test source change.
- No sprite mutation. No asset prep. Stable release lock untouched.
- No SelfImprovement-surface change of any kind.
- No new packet kind, no new capability flag (KnownPacketKinds stays 160).

## Stop-Gate Checklist

- [x] No production or test source file modified.
- [x] Full suite count matches PR #298's reported 1715.
- [x] KnownPacketKinds == 160.
- [x] BUG-005 repro GREEN (had it been red: Draft PR + track halt).

## Track Status

C-PHASE 196 is GREEN. Next phase: C-PHASE 197 (pet-sim completion baseline), per the
plan's dependency graph. Owner decisions confirmed at go-ahead: D1 = remove loose
`play_*`, D2 = synthesized-first ball families, D3 = accept C-56 carry candidates.
