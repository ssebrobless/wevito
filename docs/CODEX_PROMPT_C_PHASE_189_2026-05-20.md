# Codex Starter Prompt — C-PHASE 189 (Apply-v0 InvariantViolationWatchdog extension)

Date: 2026-05-20
Audience: Codex CLI (medium reasoning effort)
Status: **APPROVED FOR IMPLEMENTATION**
Auto-continue: **No**

Copy everything inside the fenced block below into Codex CLI. The prompt is self-contained.

```text
You are Codex CLI at medium reasoning effort, executing ONE phase: C-PHASE 189 — Apply-v0 InvariantViolationWatchdog extension.

Working repo: C:\Users\fishe\Documents\projects\wevito
Branch to create from main: claude-implementation/c-phase-189-apply-v0-invariant-watchdog
Auto-continue: No

==================== PRE-FLIGHT GATE ====================

Run these in order. HALT on any failure. Do NOT patch around failures.
1) git fetch origin && git checkout main && git pull --ff-only origin main
2) git status --porcelain (output must be empty)
3) git rev-parse HEAD (must equal origin/main and be the merge commit `9b0dd40a` of PR #285 OR a successor)
4) dotnet build .\vnext\Wevito.VNext.sln
5) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
6) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
7) git diff --check
If any of 1-7 fails: STOP. Write a Stop card describing the failure and exit.
After all 7 pass: git checkout -b claude-implementation/c-phase-189-apply-v0-invariant-watchdog

==================== HARD INVARIANTS ====================

You must satisfy ALL of these. Any violation: STOP and report.
- No hosted AI as Wevito's runtime brain. No System.Net.Http import for runtime use.
- No silent network access. Only loopback endpoints behind explicit-on flags. This phase opens no socket.
- No silent training. No model-weight updates. No silent file/tool/code/asset mutation.
- KillSwitchService.IsActive() halts every code path that emits or reads from the ledger.
- AuditLedgerService remains append-only. No UPDATE / DELETE / DROP TABLE SQL.
- Every new audit packet kind must appear in PlainLanguageExplainer.KnownPacketKinds with a plain-language sentence.
- Every new capability flag defaults bool.FalseString in CapabilityFlagInventory.
- Held-out eval data must stay invisible. InvariantViolationWatchdog is already in HeldOutEvalStoreVisibilityTests.forbiddenTypes; do not change that.
- Pets remain visually normal pet-sim characters. Pet game FPS / user PC experience stays protected. No UI change in this phase.
- SupervisedImprovementLoop.ApplyRunnerNotImplementedReason MUST remain "apply_runner_not_implemented_in_v0".
- Legacy SelfImprovementPacketKinds.ApplyCompleted MUST remain "self_improvement_apply_completed".
- The existing InvariantViolationWatchdog rule BuildApplyCompletedWithoutAwaitingApproval MUST remain semantically unchanged.
- Codex never auto-merges Auto-continue=No phases.

==================== READING LIST ====================

Read these files end-to-end BEFORE making any change:

1) docs/CLAUDE_C_PHASE189_PLUS_PLAN_2026-05-20.md (this phase's master plan; §1 + §4.1)
2) docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
3) docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md
4) docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md
5) docs/C_PHASE188_POST_C183_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-19.md
6) vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs (extended in this phase)
7) vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantCheck.cs
8) vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantCheckResult.cs
9) vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs (constant added here)
10) vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs (flag added here)
11) vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs (extended here)
12) vnext/src/Wevito.VNext.Core/Audit/AuditLedgerService.cs (to understand the existing Record() / row shape; do NOT modify)
13) vnext/src/Wevito.VNext.Core/Audit/EvidencePacket.cs (do NOT modify)
14) vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogTests.cs (existing pattern to mirror)
15) vnext/tests/Wevito.VNext.Tests/Support/MutationAllowList.cs (for the C-PHASE 186 audit; InvariantViolationWatchdog is NOT on this list because it does not call File.* — it only writes via AuditLedgerService)

After reading: do not start coding until you can name (a) the existing rule's identifier, (b) the existing `ExtractOperationId` helper signature, (c) the existing `BuildResult` helper signature, (d) the existing emit method's exact name and signature.

==================== ALLOWED FILE CHANGES ====================

Add or extend EXACTLY these files. No other file may be touched.

ADD:
- vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogV0Tests.cs
- docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md

EXTEND:
- vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs
- vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs
- vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs
- vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
- docs/codex-phase-history.jsonl (one append at the very end)

==================== STEP-BY-STEP WORK ====================

Work in this order. Run validation after each numbered step that changes source.

1) Read every file in the READING LIST above.

2) In vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs:
   Append this constant after MutationScopeAuditEvent:
       public const string ApplyV0InvariantCheckFailed = "self_improvement_apply_v0_invariant_check_failed";

3) In vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs:
   a) Add `SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed` to the `KnownPacketKinds` collection (preserve declaration order with other ApplyV0* / MutationScopeAuditEvent entries).
   b) Add the explainer switch arm:
       SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed => "Wevito recorded a self-improvement apply-v0 sequence invariant violation found by the watchdog.",

4) In vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs:
   Append this entry near the other invariant/audit emit flags (e.g., immediately after the mutation_scope_audit_emit_enabled entry on line ~63):
       new("apply_v0_invariant_check_emit_enabled", bool.FalseString, "Allows the invariant watchdog to emit a self-improvement apply-v0 invariant-violation packet per failing rule. Default off; the maturity-clock reset path is unchanged."),

5) In vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs:

   a) Add three new rule identifier private string constants near the existing ones, e.g.:
       private const string V0CompletedWithoutApplied = "v0_completed_without_applied";
       private const string V0RolledBackFollowedByCompleted = "v0_rolled_back_followed_by_completed";
       private const string ExplicitRollbackCompletedWithoutStarted = "explicit_rollback_completed_without_started";

   b) Add the three new `InvariantCheck` static fields beside the existing ones (e.g., `V0CompletedWithoutAppliedCheck`, `V0RolledBackFollowedByCompletedCheck`, `ExplicitRollbackCompletedWithoutStartedCheck`), each with `MaturityClockResetReason.InvariantViolation`, and add them to the `Checks` static collection so the existing maturity-clock-reset emitter knows about them.

   c) In the result-list assembly within the existing rule-building method (the same place that calls `BuildApplyCompletedWithoutAwaitingApproval` at line ~163), append three new entries:
       BuildV0CompletedWithoutApplied(rows),
       BuildV0RolledBackFollowedByCompleted(rows),
       BuildExplicitRollbackCompletedWithoutStarted(rows)

   d) Add three private static methods analogous to `BuildApplyCompletedWithoutAwaitingApproval`. Each one:
      - Filters `rows` by the appropriate `PacketKind` constants from `SelfImprovementPacketKinds`.
      - Uses the existing `ExtractOperationId` helper to read each row's operation id.
      - Returns failures as `"row_id={row.Id} operation_id={operationId}"` strings.
      - Calls `BuildResult(<rule identifier>, failures)`.

      Rule semantics:
      - R1 V0CompletedWithoutApplied: fail any `ApplyV0Completed` row whose `operation_id` does NOT appear in the set of `ApplyV0Applied` `operation_id` values.
      - R2 V0RolledBackFollowedByCompleted: fail any operation that has BOTH a `ApplyV0RolledBack` row AND a `ApplyV0Completed` row whose `CreatedAtUtc` is STRICTLY GREATER than the rolled-back row's `CreatedAtUtc`. (Tie-break by `Id` if timestamps are equal.)
      - R3 ExplicitRollbackCompletedWithoutStarted: fail any `ApplyV0ExplicitRollbackCompleted` row whose `operation_id` does NOT appear in the set of `ApplyV0ExplicitRollbackStarted` `operation_id` values. (Refused rows do NOT count as starts.)

   e) Add a new optional emit method, gated on the new flag AND KillSwitch:

       public const string V0InvariantCheckEmitEnabledSetting = "apply_v0_invariant_check_emit_enabled";

       public void EmitInvariantCheckFailedPackets(IReadOnlyList<InvariantCheckResult> results, DateTimeOffset nowUtc)
       {
           if (_killSwitchService?.IsActive() == true) return;
           if (_auditLedgerService is null) return;
           var settings = _settingsProvider?.Invoke();
           if (settings is null) return;
           if (!IsTrue(settings, V0InvariantCheckEmitEnabledSetting)) return;
           foreach (var result in results)
           {
               if (result.Passed) continue;
               if (!IsV0Rule(result.InvariantId)) continue;
               _auditLedgerService.Record(new EvidencePacket(
                   Guid.NewGuid(),
                   SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed,
                   null,
                   nowUtc,
                   DidUseNetwork: false,
                   DidUseHostedAi: false,
                   DidUseLocalModel: false,
                   DidMutate: false,
                   ArtifactPath: "",
                   Summary: System.Text.Json.JsonSerializer.Serialize(new { invariantId = result.InvariantId, failures = result.Failures }, JsonDefaults.Options),
                   Status: "Completed"));
           }
       }

       Use the existing watchdog field names verbatim: `_auditLedgerService`, `_killSwitchService`, `_settingsProvider`. Do NOT change the existing constructor signature; the new method reads the already-injected nullable dependencies and is a no-op when any required dependency is null.

   f) Add `IsV0Rule(string invariantId)` private static helper that returns true for the three new rule identifiers.

   g) If the existing watchdog source does not yet have a private static `IsTrue(IReadOnlyDictionary<string,string>, string)` helper, add one matching the shape used by `ApplyRunnerStatusReportService.IsTrue` (TryGetValue → bool.TryParse → parsed).

   h) Do NOT modify `BuildApplyCompletedWithoutAwaitingApproval`. Do NOT modify any existing rule's semantics. Do NOT change the existing maturity-clock-reset emit path; the three new checks flow through the existing `Checks` list so a future reviewer running the watchdog's existing emit path will see the new rules naturally.

6) Add vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogV0Tests.cs:
   - Mirror the existing `InvariantViolationWatchdogTests.cs` shape (use a fake `AuditLedgerService` against an in-memory or `Path.GetTempPath()` SQLite db following the existing test pattern).
   - Do NOT use Moq / NSubstitute / FakeItEasy. Construct real `AuditLedgerService` against a tmp db.
   - Tests required (≥ 15 [Fact] methods):
     a) V0_R1_happy_path: applied + completed for one operation = pass.
     b) V0_R1_fail_completed_without_applied: completed alone = fail row contains the completed row's id.
     c) V0_R1_cross_op_isolation: applied op A, completed op B = R1 fails for op B.
     d) V0_R1_empty_ledger: no rows = pass.
     e) V0_R2_happy_path_rollback_only: applied + rolled_back for one op (no completed) = pass.
     f) V0_R2_happy_path_two_ops: completed for A, rolled_back for B = pass.
     g) V0_R2_fail_completed_after_rollback: applied → rolled_back at T → completed at T+1 = fail row.
     h) V0_R2_completed_before_rollback_passes_R2: completed at T, rolled_back at T+1 = R2 passes (R1 may still pass too if applied present).
     i) V0_R3_happy_path: explicit_rollback_started + explicit_rollback_completed = pass.
     j) V0_R3_fail_completed_without_started: explicit_rollback_completed alone = fail row.
     k) V0_R3_refused_not_counted_as_start: explicit_rollback_refused + explicit_rollback_completed = fail (refused is NOT a start).
     l) Empty_ledger_passes_all_three_v0_rules.
     m) KillSwitch_active_returns_blocked_result (mirror existing pattern; assert build returns the existing blocked shape).
     n) Emit_default_off_writes_no_packet: Build returns 1+ failing v0 rule; EmitInvariantCheckFailedPackets writes 0 rows when the flag is False.
     o) Emit_flag_on_writes_one_packet_per_failing_rule: with the flag True and KillSwitch off, EmitInvariantCheckFailedPackets writes exactly one self_improvement_apply_v0_invariant_check_failed row per failing v0 rule, each with DidMutate=false, DidUseNetwork=false, DidUseHostedAi=false, DidUseLocalModel=false.
     p) Plain_language_explainer_returns_registered_sentence_for_new_kind.
     q) Watchdog_source_has_no_update_or_delete_sql: static text scan via File.ReadAllText against the watchdog source path asserts substrings "UPDATE ", "DELETE FROM", "DROP TABLE", "INSERT INTO audit_ledger" outside an existing whitelist of allowed write call sites; if the existing watchdog already uses `_ledger.Record(...)` only (no raw SQL writes), assert NO "UPDATE", "DELETE FROM", "DROP TABLE", "INSERT INTO" substrings appear in the file at all.

   Use `Path.Combine(Path.GetTempPath(), "wevito-watchdog-v0", Guid.NewGuid().ToString("N"))` for tmp dirs, mirroring existing tests.

7) Author the phase report: docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md
   Use this structure (mirror C_PHASE186 / C_PHASE188 style):
   - # C-PHASE 189 — Apply-v0 InvariantViolationWatchdog extension
   - ## Goal
   - ## Scope (Added / Extended / Not touched)
   - ## Implemented (the three rules + the optional emit method, table format)
   - ## Safety Boundaries
   - ## Validation (each command + outcome)
   - ## Stop-Gate Checklist (mark each [x] after verification)
   - ## Next Phase: "C-PHASE 190 — Apply-runner status report additive narrow-runner fields is NOT YET APPROVED FOR IMPLEMENTATION. It requires explicit user signoff before Codex begins it."

==================== VALIDATION ====================

Run all five and report each outcome:
A) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InvariantViolationWatchdog|InvariantViolationWatchdogV0|PlainLanguage|SelfImprovement"
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check

If any of A-E fails: STOP. Do not patch around failures.

==================== STOP-GATE CHECKLIST ====================

Before opening the PR, verify each:

[ ] The legacy rule `BuildApplyCompletedWithoutAwaitingApproval` is unchanged.
[ ] `SelfImprovementPacketKinds.ApplyCompleted` constant unchanged.
[ ] `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` unchanged.
[ ] New packet kind `self_improvement_apply_v0_invariant_check_failed` present in `PlainLanguageExplainer.KnownPacketKinds`.
[ ] New flag `apply_v0_invariant_check_emit_enabled` defaults `bool.FalseString` in `CapabilityFlagInventory`.
[ ] No new flag is set True by any producer in this PR.
[ ] No mocking framework imported in the new test class.
[ ] No `UPDATE`, `DELETE FROM`, `DROP TABLE`, or new `INSERT` SQL appears in the watchdog source.
[ ] Watchdog still passes the C-PHASE 186 `MutationScopeAuditTests` (the watchdog already only writes via `_ledger.Record(...)` which routes through `AuditLedgerService`; do not change this).
[ ] No new System.Net.Http import. No new socket open.
[ ] No UI surface changed. No pet-sim or sprite-workflow file changed.
[ ] No held-out or in-distribution eval store type referenced by the new code.
[ ] Phase report doc written at `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md`.

If any box cannot be checked: STOP and report.

==================== COMMIT & PR ====================

1) git add -A vnext/src vnext/tests docs
2) git status --short (verify only the allowed file paths appear)
3) git commit -m "C-PHASE 189: apply-v0 invariant watchdog extension (three new v0-sequence rules; default-off emit packet kind)"
4) git push -u origin claude-implementation/c-phase-189-apply-v0-invariant-watchdog
5) gh pr create \
     --base main \
     --head claude-implementation/c-phase-189-apply-v0-invariant-watchdog \
     --title "C-PHASE 189: apply-v0 invariant watchdog extension" \
     --body "Implements three deterministic v0-sequence invariant rules on InvariantViolationWatchdog. Adds one default-off-emit packet kind and one default-off capability flag. No source-code, UI, or asset mutation. SupervisedImprovementLoop.ApplyRunnerNotImplementedReason is preserved unchanged. Legacy SelfImprovementPacketKinds.ApplyCompleted is preserved unchanged. See docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md for the full report. Auto-continue: No."
6) Capture the PR URL from `gh pr create` output.

==================== HISTORY APPEND ====================

After the PR is created, append exactly one line to docs/codex-phase-history.jsonl matching the existing shape:

{"phase_id":"C-PHASE 189","timestamp_utc":"<UTC now with 7 fractional digits and Z suffix>","branch":"claude-implementation/c-phase-189-apply-v0-invariant-watchdog","pr_url":"<the PR URL from step 6>"}

Timestamp helper (PowerShell):
([DateTimeOffset]::UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")

Commit the history append:
git add docs/codex-phase-history.jsonl
git commit -m "docs: append C-PHASE 189 history row"
git push

==================== AUTO-CONTINUE ====================

Auto-continue: No. STOP after the PR and history append are pushed. Do not merge. Wait for explicit user review.
```

## End of starter
