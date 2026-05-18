# Self-Improvement Eval Gate Manifest

Date: 2026-05-18
Phase: C-PHASE 147

## Purpose

This manifest defines the eval gates that supervised self-improvement proposals must eventually satisfy before any apply path can be considered. C-PHASE 147 only defines the contract. It does not run experiments, seed held-out content, approve mutations, or expose held-out eval data to proposal loops.

## Gate List

1. Build
2. Unit tests
3. Benchmark suite
4. In-distribution eval
5. Held-out eval
6. Performance
7. Scope hash
8. Dry-run
9. Backup
10. Post-proof
11. Rollback

## Result Contract

Each gate must return exactly one of:

- `Passed`
- `Failed(reason)`
- `NotApplicable(reason)`

C-PHASE 147's runner returns `NotApplicable("no_eval_run_wired_v0")` for all gates because no eval execution is wired yet. If the kill switch is active, the runner returns `NotApplicable("kill_switch=true")`.

## Held-Out Eval Storage Contract

- Held-out eval content is isolated behind `IHeldOutEvalStore`.
- Held-out content is not seeded in this phase.
- Held-out content must not be exposed through PET TASKS, shell views, autonomous scopes, tool registry surfaces, evidence dashboards, or proposal producers.
- `HeldOutEvalStore` refuses list/read operations while `KillSwitchService.IsActive()` is true.
- Future proposal loops must not see held-out eval content; only the eval gate runner may read it for scoring.

## Current Status

- Contract exists.
- Default manifest lists all eleven gates.
- Runner exists as a non-executing preview stub.
- No experiments consume the manifest yet.
- No held-out eval content exists in this phase.
