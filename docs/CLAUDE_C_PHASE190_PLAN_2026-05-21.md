# C-PHASE 190 Plan - Pin Watchdog Product-Truth Surface

## Goal

C-PHASE 190 should pin the post-C-PHASE 189 watchdog surface with deterministic product-truth tests before any new runtime wiring is considered. C-PHASE 189 added three apply-v0 invariant rules plus a default-off optional emit method, but it deliberately left the watchdog inert by default: no producer invokes the new emit path, no scheduler was added, and `apply_v0_invariant_check_emit_enabled` defaults `False`. The next safest step is to freeze those truths in tests so future phases cannot quietly widen authority.

## Constraints carried forward

- No hosted AI as runtime brain. No silent network. No silent mutation.
- Every new capability flag defaults False.
- Every new audit packet kind registered in `PlainLanguageExplainer.KnownPacketKinds` with a plain-language sentence.
- KillSwitch must halt every surface this batch adds.
- `AuditLedgerService` remains append-only (no UPDATE/DELETE/DROP TABLE SQL).
- Held-out eval data invisible to all surfaces.
- Human is v0 judge. `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason == "apply_runner_not_implemented_in_v0"` unchanged.
- `SelfImprovementPacketKinds.ApplyCompleted == "self_improvement_apply_completed"` unchanged.
- Auto-continue: No on every PR.
- Codex HALTs on any failure.

## Options considered

### Option A — Pin watchdog product-truth surface

Add one test-only file, `vnext/tests/Wevito.VNext.Tests/PostC189WatchdogProductTruthTests.cs`, analogous to the C-PHASE 188 product-truth retest. The test file should pin the watchdog type location, the three v0 invariant identifiers, the new packet kind registration, the new flag defaulting false, absence of raw `INSERT`/`UPDATE`/`DELETE` SQL in watchdog source, absence of `System.Net.*` imports, absence of held-out or in-distribution eval references, the supervised-loop refusal constant, and the legacy apply-completed packet constant.

Files touched: one new test file under `vnext/tests/Wevito.VNext.Tests/`.

New packet kinds: none.

New flags: none.

New runtime behavior: none.

Blast radius: S.

Single biggest risk: a product-truth test can become too source-string-heavy if it checks implementation formatting rather than durable safety facts.

### Option B — Read-only observer wiring

Wire `InvariantViolationWatchdog` as a passive observer into exactly one existing read-only surface, such as `ApplyRunnerActivityService` or the capabilities-and-gates snapshot, behind a new default-false flag. The observer would read ledger state and emit the C-PHASE 189 invariant-check-failed packet only when violations are detected and the flag is explicitly enabled. It would not call apply, rollback, scheduler, judge, replay, snapshot export, scoring, eval, or mutation surfaces.

Files touched: likely one production read-only service, the watchdog or composition root if construction changes are required, tests for the new read-only integration, `CapabilityFlagInventory`, and a phase report.

New packet kinds: none if it reuses `self_improvement_apply_v0_invariant_check_failed`.

New flags: one default-false observer/wiring flag.

New runtime behavior: a read-only observer path exists but is dormant unless explicitly enabled.

Blast radius: M.

Single biggest risk: even read-only wiring creates a new runtime path and could accidentally run too often or too early if the flag gate is misplaced.

### Option C — Narrow scheduler tick

Add a fixed-interval tick that invokes the watchdog behind a default-false flag. This would make invariant scanning a scheduled runtime behavior rather than a passive or explicitly invoked surface. It is not recommended for C-PHASE 190 because the C-PHASE 189 watchdog should first be pinned and then soaked through a narrower read-only observer path.

Files touched: scheduler or runtime loop code, watchdog composition, capability inventory, tests, and a phase report.

New packet kinds: none if it reuses `self_improvement_apply_v0_invariant_check_failed`.

New flags: at least one default-false scheduler/tick flag.

New runtime behavior: a dormant scheduled watchdog invocation path exists until enabled.

Blast radius: M-L.

Single biggest risk: scheduled execution changes the runtime cadence and could create background ledger reads or emits outside the user's expected review flow.

## Recommendation

Recommend Option A as C-PHASE 190, Option B as C-PHASE 191, and Option C deferred. Option A is fully reversible, test-only, and has the smallest blast radius: it changes no production code, adds no runtime path, emits no packets, flips no flags, and does not touch the solution or tools. It gives reviewers a hard safety net around C-PHASE 189 before any later prompt tries to wire the watchdog into an observable surface.

This also matches the product-truth-first cadence that worked after C-PHASE 187: C-PHASE 188 pinned the apply-runner family before the next batch widened thinking. Repeating that pattern after C-PHASE 189 keeps the project honest: first lock what just landed, then consider a read-only integration, and only much later consider any scheduled behavior.

## C-PHASE 190 acceptance criteria

- Exactly one new test file under `vnext/tests/Wevito.VNext.Tests/`.
- Zero files under `vnext/src/`, `vnext/tools/`, repo-root `tools/` changed.
- `vnext/Wevito.VNext.sln` unchanged.
- No new packet kind. No new flag. No flag flipped.
- Full test count after = 1648 + (new test count), all passing.
- Preflight gate (build, full test, build-vnext.ps1, `git diff --check`) all pass.
- Auto-continue: No.

## Stop-gates the C-190 implementation prompt must enforce

- Test class does not import `System.Net.Http` or `System.Net.Sockets`.
- Test class does not reference held-out or in-distribution eval stores or cases.
- Test class does not invoke any apply runner, rollback runner, scheduler, judge, replay, snapshot, or scoring surface that would mutate state.
- Branch created from `origin/main`; never `git checkout main`.

## What this plan does NOT do

- Does not change watchdog source.
- Does not enable any capability flag.
- Does not register any new packet kind.
- Does not wire the watchdog into any producer.
- Does not change UI, sprite, pet, or asset surfaces.
- Does not touch held-out eval data.

## References

- `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md`
- `docs/C_PHASE188_POST_C183_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-19.md`
- `docs/C_PHASE187_EXTENDED_ARTIFACT_TYPES_2026-05-19.md`
