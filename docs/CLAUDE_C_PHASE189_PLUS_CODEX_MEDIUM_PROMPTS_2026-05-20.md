# Codex Medium-Reasoning Phase Prompts — C-PHASE 189 + 190–194

Date: 2026-05-20
Audience: Codex CLI (medium reasoning effort)
Predecessor plan: `docs/CLAUDE_C_PHASE189_PLUS_PLAN_2026-05-20.md`

This file contains six copy-paste prompts. Only §1 (C-PHASE 189) is approved for implementation. §§2–6 (C-PHASE 190–194) are **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION**. Paste those into Codex CLI only after explicit user signoff.

---

## §0 — Common Header (paste into every phase)

```text
You are Codex CLI at medium reasoning effort, executing one C-PHASE for the Wevito project.

Working repo: C:\Users\fishe\Documents\projects\wevito

Pre-flight gate (run before any source change; halt on any failure):
1) Sync: git fetch origin && git checkout main && git pull --ff-only origin main
2) Verify clean tree: git status --porcelain (must be empty)
3) Verify HEAD: git rev-parse HEAD matches origin/main (post-C-PHASE 188 merge `9b0dd40a` or its successor)
4) Build: dotnet build .\vnext\Wevito.VNext.sln
5) Tests: dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
6) Vnext build: powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
7) Diff check: git diff --check
If any pre-flight step fails: STOP. Do not patch around it. Open a Stop card and exit.

Hard invariants (every phase in this batch must satisfy):
- No hosted AI as Wevito's runtime brain. No System.Net.Http import for runtime use.
- No silent network access. Only loopback endpoints behind explicit-on flags.
- No silent training. No model-weight updates. No silent file/tool/code/asset mutation.
- KillSwitchService.IsActive() halts every adapter, scheduler, autonomous loop, runner, scope, experiment, eval runner, approval handler, judge, replay harness, snapshot tool, replay runner, in-distribution seed tool, local scoring provider, capabilities-and-gates snapshot, apply-runner prerequisite check, apply-runner status report, apply-runner activity service, scoring dry-run, local runtime probe, eval coverage health, proposal quality metrics, narrow apply runner, narrow rollback runner, mutation-scope guard, mutation-scope audit producer, invariant watchdog, and every service this batch adds.
- AuditLedgerService remains append-only. No UPDATE / DELETE / DROP TABLE SQL.
- Every new audit packet kind must appear in PlainLanguageExplainer.KnownPacketKinds with a plain-language sentence.
- Every new capability flag defaults bool.FalseString in CapabilityFlagInventory.
- Held-out eval data must stay invisible. Every new production type added by this batch is added to HeldOutEvalStoreVisibilityTests.forbiddenTypes when applicable.
- Pets remain visually normal pet-sim characters. Pet game FPS / user PC experience stays protected. No phase wires anything into pet-sim or sprite-workflow UI.
- SupervisedImprovementLoop.ApplyRunnerNotImplementedReason MUST remain unchanged at the constant string "apply_runner_not_implemented_in_v0".
- Legacy SelfImprovementPacketKinds.ApplyCompleted MUST remain unchanged at the constant string "self_improvement_apply_completed".
- Codex never auto-merges Auto-continue=No phases.

Validation (run before opening the PR; halt on any failure):
A) Focused test filter (see per-phase prompt below)
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check
If any of A-E fails: STOP and report the failure. Do not patch around it.

History append (every phase, after PR opens):
Append one line to docs/codex-phase-history.jsonl matching the existing shape:
{"phase_id":"C-PHASE NNN","timestamp_utc":"<ISO8601 UTC with 7 fractional digits and Z suffix>","branch":"claude-implementation/c-phase-NNN-<slug>","pr_url":"https://github.com/ssebrobless/wevito/pull/<PR-NUMBER>"}

PR convention:
- One PR per phase. Branch name in this file's per-phase section.
- PR title equals the phase title in this file's per-phase section.
- PR body: link to the per-phase prompt section and the per-phase report doc.
- Auto-continue=No for every phase in this batch.
```

---

## §1 — C-PHASE 189: Apply-v0 InvariantViolationWatchdog extension (APPROVED FOR IMPLEMENTATION)

For C-PHASE 189 use the standalone starter file: `docs/CODEX_PROMPT_C_PHASE_189_2026-05-20.md`. The starter is self-contained and supersedes this section. The summary below exists only so the prompts pack is readable end-to-end.

**Branch:** `claude-implementation/c-phase-189-apply-v0-invariant-watchdog`

**Goal:** Add three deterministic v0-sequence rules to `InvariantViolationWatchdog` plus one new default-off-emit packet kind and one new default-off flag.

**Files in scope:**

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs` (extend)
- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs` (extend; one constant `ApplyV0InvariantCheckFailed = "self_improvement_apply_v0_invariant_check_failed"`)
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs` (extend; one flag `apply_v0_invariant_check_emit_enabled` default `bool.FalseString` with descriptive sentence)
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs` (extend; add `ApplyV0InvariantCheckFailed` to `KnownPacketKinds` and the explainer switch)
- `vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogV0Tests.cs` (new)
- `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md` (new phase report)
- `docs/codex-phase-history.jsonl` (one append)

**Allowed changes:** add three rules (`v0_completed_without_applied`, `v0_rolled_back_followed_by_completed`, `explicit_rollback_completed_without_started`), each constructed by a new private `BuildXxx` method analogous to the existing `BuildApplyCompletedWithoutAwaitingApproval`. Reuse `ExtractOperationId`. The watchdog's existing `Build` method appends the three new results to its result list. Add one *optional* emitter method `EmitInvariantCheckFailedPackets` that, when called with the new flag True and KillSwitch inactive, writes one `self_improvement_apply_v0_invariant_check_failed` packet per failing rule (`DidMutate=false`, `DidUseNetwork=false`, `DidUseHostedAi=false`, `DidUseLocalModel=false`). No producer in this phase calls `EmitInvariantCheckFailedPackets`.

**Stop gates:**

- The legacy rule `BuildApplyCompletedWithoutAwaitingApproval` is modified in any semantic way.
- `SelfImprovementPacketKinds.ApplyCompleted` constant changed.
- New packet kind missing from `PlainLanguageExplainer.KnownPacketKinds`.
- New flag default is anything other than `bool.FalseString`.
- Any test uses a mocking framework instead of fake ledger rows.
- Watchdog source contains `UPDATE`, `DELETE FROM`, `DROP TABLE`, or any new `INSERT` outside the existing ledger-write helper.
- Pre-flight gate fails.

**Validation:**

```text
A) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InvariantViolationWatchdog|PlainLanguage|SelfImprovement"
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check
```

**Commit message:**

```text
C-PHASE 189: apply-v0 invariant watchdog extension (three new v0-sequence rules; default-off emit packet kind)
```

**PR title:** `C-PHASE 189: apply-v0 invariant watchdog extension`

**Auto-continue:** No

---

## §2 — C-PHASE 190: Apply-runner status report additive narrow-runner fields

**DO NOT IMPLEMENT YET — DESIGN DRAFT, NOT APPROVED FOR IMPLEMENTATION.**

Wait for explicit user signoff before pasting this prompt into Codex.

```text
Paste only after C-PHASE 189 has merged AND the user has explicitly approved C-PHASE 190.

Start with the §0 common header above.

Phase: C-PHASE 190 — Apply-runner status report additive narrow-runner fields
Branch: claude-implementation/c-phase-190-apply-runner-status-report-narrow-fields

Goal:
Extend the `ApplyRunnerStatusReport` record additively so reviewers can see the narrow-runner family state alongside the legacy supervised-loop honesty signal. Do not change the legacy field `ApplyRunnerImplemented` semantics. Add five new fields. Populate them from the live `CapabilityFlagInventory` and live ledger reads (ReadOnly). Render them in the existing read-only Tool Popup expander as text only.

Files in scope:
- vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReport.cs (extend; five new fields with defaults)
- vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReportService.cs (extend; populate the new fields read-only)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml (extend; new TextBlock entries in the existing expander)
- vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs (extend; render the new fields)
- vnext/tests/Wevito.VNext.Tests/ApplyRunnerStatusReportServiceTests.cs (extend; assert legacy field unchanged + new fields populated)
- vnext/tests/Wevito.VNext.Tests/ApplyRunnerStatusReportNarrowRunnerFieldsTests.cs (new; per-field [Fact] coverage)
- docs/C_PHASE190_APPLY_RUNNER_STATUS_REPORT_NARROW_FIELDS_2026-05-20.md (new)
- docs/codex-phase-history.jsonl (one append)

Allowed changes:
1) Append five new fields to the `ApplyRunnerStatusReport` record constructor (all optional with safe defaults):
   - NarrowArtifactRenameRunnerImplemented (bool)
   - NarrowArtifactRenameRunnerAllFlagsDefaultFalse (bool)
   - NarrowArtifactRenameRunnerExtensionFlagDefaultFalse (bool)
   - NarrowArtifactRenameRunnerLastSequenceUtc (DateTimeOffset?)
   - NarrowExplicitRollbackRunnerLastSequenceUtc (DateTimeOffset?)
2) In `ApplyRunnerStatusReportService.BuildReport`:
   - Compute `NarrowArtifactRenameRunnerImplemented` by `Type.GetType("Wevito.VNext.Core.SelfImprovement.Apply.ArtifactRenameApplyRunner, Wevito.VNext.Core") is not null`.
   - Compute `NarrowArtifactRenameRunnerAllFlagsDefaultFalse` by enumerating `CapabilityFlagInventory.Flags`, filtering keys that start with `apply_runner_` or equal `mutation_scope_audit_emit_enabled` or equal `apply_v0_invariant_check_emit_enabled`, and asserting every `DefaultValue == bool.FalseString`. Treat absence of `apply_v0_invariant_check_emit_enabled` as False (the flag may not have landed yet on older builds).
   - Compute `NarrowArtifactRenameRunnerExtensionFlagDefaultFalse` by reading the `apply_runner_v0_extended_artifact_types_enabled` entry; if absent, set to False.
   - Compute `NarrowArtifactRenameRunnerLastSequenceUtc` by reading the most recent `self_improvement_apply_v0_completed` row (ReadOnly).
   - Compute `NarrowExplicitRollbackRunnerLastSequenceUtc` by reading the most recent `self_improvement_apply_v0_explicit_rollback_completed` row (ReadOnly).
3) In `ToolPopupWindow.xaml` add five new TextBlock entries in the existing `ApplyRunnerStatusExpander` (text-only; no Button / ToggleSwitch / CheckBox / MenuItem).
4) In `ToolPopupWindow.xaml.cs` populate the new TextBlock contents in the existing render method.

Stop gates:
- The legacy `ApplyRunnerImplemented` field's derivation rule is modified.
- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` changed.
- The status report JSON shape is non-additive (any field renamed or removed).
- New fields use cached or hardcoded values instead of reading live inventory / ledger.
- New ledger reads do not use SqliteOpenMode.ReadOnly.
- Any new UI element is a Button / ToggleSwitch / CheckBox / MenuItem that calls a mutating method.
- New service or UI imports System.Net.Http.
- Pre-flight gate fails.

Validation:
A) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerStatusReport|ApplyRunnerStatusReportCli|PlainLanguage|SelfImprovement"
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check

Commit message:
C-PHASE 190: apply-runner status report additive narrow-runner fields (legacy field unchanged)

PR title: C-PHASE 190: apply-runner status report additive narrow-runner fields
Auto-continue: No
```

---

## §3 — C-PHASE 191: Static apply-runner caller allow-list test

**DO NOT IMPLEMENT YET — DESIGN DRAFT, NOT APPROVED FOR IMPLEMENTATION.**

Wait for explicit user signoff before pasting this prompt into Codex.

```text
Paste only after C-PHASE 189 has merged AND the user has explicitly approved C-PHASE 191.

Start with the §0 common header above.

Phase: C-PHASE 191 — Static apply-runner caller allow-list test
Branch: claude-implementation/c-phase-191-apply-runner-caller-allow-list

Goal:
Add a deterministic IL-reflection test that pins which types may invoke `ArtifactRenameApplyRunner.Apply(...)` and `ArtifactRenameRollbackRunner.ExplicitRollback(...)`. The initial allow-list is empty: today no production caller is permitted, mirroring the current narrow-runner-isolation posture.

Files in scope:
- vnext/tests/Wevito.VNext.Tests/Support/ApplyRunnerCallerAllowList.cs (new; empty HashSet<string>)
- vnext/tests/Wevito.VNext.Tests/Support/ApplyRunnerCallerScannerSanityHelper.cs (new; test-only helper that deliberately calls Apply and ExplicitRollback so the IL scanner has known positive cases)
- vnext/tests/Wevito.VNext.Tests/ApplyRunnerCallerAllowListTests.cs (new; ≥ 5 [Fact] tests)
- docs/C_PHASE191_APPLY_RUNNER_CALLER_ALLOW_LIST_2026-05-20.md (new)
- docs/codex-phase-history.jsonl (one append)

Allowed changes (NO production source under vnext/src/ or vnext/tools/ is modified):
1) `ApplyRunnerCallerAllowList.TypeNames` is `IReadOnlySet<string>` initialized empty (using `StringComparer.Ordinal`).
2) `ApplyRunnerCallerScannerSanityHelper` is a test-internal type with two methods:
   - `void CallApply(...)` which constructs an `ArtifactRenameApplyRunner` with a stub ledger / fake settings / fake KillSwitch / no-op prereq override / tmp artifact root, then invokes `Apply` once with a synthetic request (cancellation token = default). The body may also throw immediately after the call to short-circuit.
   - `void CallExplicitRollback(...)` similarly invokes `ExplicitRollback`.
3) `ApplyRunnerCallerAllowListTests` covers:
   - [Fact] Production_assemblies_contain_no_unallowed_apply_callers — load Wevito.VNext.Core and Wevito.VNext.Shell, IL-scan every method, collect every type whose IL emits a Call/Callvirt opcode targeting `ArtifactRenameApplyRunner.Apply` or `ArtifactRenameRollbackRunner.ExplicitRollback`. Assert result is subset of `ApplyRunnerCallerAllowList.TypeNames`.
   - [Fact] Test_assembly_scanner_finds_the_sanity_helper_calling_apply — load `Wevito.VNext.Tests` assembly, IL-scan, assert `ApplyRunnerCallerScannerSanityHelper` appears in the discovered caller set for `Apply`.
   - [Fact] Test_assembly_scanner_finds_the_sanity_helper_calling_explicit_rollback — same for `ExplicitRollback`.
   - [Fact] AllowList_is_empty_at_merge_time — `ApplyRunnerCallerAllowList.TypeNames.Count == 0`.
   - [Fact] Scanner_uses_il_decode_not_string_match — internal assertion that the scanner inspects `MethodBody.GetILAsByteArray()` and decodes opcodes, not source text.

Stop gates:
- Any production source under vnext/src/ or vnext/tools/ is modified.
- The IL scanner uses source-string matching (e.g., reads `.cs` files and greps).
- The sanity helper does not actually call `Apply` and `ExplicitRollback` (any short-circuit must happen AFTER the call instruction is emitted).
- The allow-list is non-empty at merge time.
- The IL scanner ignores generics, lambdas, or interface dispatch and would miss a delegate-based call. (Use Callvirt + Call opcode scan against the metadata token for the target method.)
- Pre-flight gate fails.

Validation:
A) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerCallerAllowList"
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check

Commit message:
C-PHASE 191: static apply-runner caller allow-list test (empty allow-list; IL-decode-based; sanity helper canary)

PR title: C-PHASE 191: static apply-runner caller allow-list test
Auto-continue: No
```

---

## §4 — C-PHASE 192: Apply-runner family flag default-off audit test

**DO NOT IMPLEMENT YET — DESIGN DRAFT, NOT APPROVED FOR IMPLEMENTATION.**

Wait for explicit user signoff before pasting this prompt into Codex.

```text
Paste only after C-PHASE 189 has merged AND the user has explicitly approved C-PHASE 192.

Start with the §0 common header above.

Phase: C-PHASE 192 — Apply-runner family flag default-off audit test
Branch: claude-implementation/c-phase-192-apply-runner-family-flag-default-off-audit

Goal:
Add a deterministic test asserting every flag in `CapabilityFlagInventory` belonging to the apply-runner family defaults to bool.FalseString. The family is: every key starting with `apply_runner_` PLUS the explicit set `{ "mutation_scope_audit_emit_enabled", "apply_v0_invariant_check_emit_enabled" }`. The second explicit key is conditional on C-PHASE 189 having merged.

Files in scope:
- vnext/tests/Wevito.VNext.Tests/ApplyRunnerFamilyFlagDefaultOffAuditTests.cs (new)
- docs/C_PHASE192_APPLY_RUNNER_FAMILY_FLAG_DEFAULT_OFF_AUDIT_2026-05-20.md (new)
- docs/codex-phase-history.jsonl (one append)

Allowed changes (NO production source under vnext/src/ or vnext/tools/ is modified):
1) Test reads the live `CapabilityFlagInventory.Flags` IReadOnlyList.
2) [Fact] ApplyRunner_family_flags_default_off — every flag in the family has `DefaultValue == bool.FalseString`.
3) [Fact] ApplyRunner_family_flag_set_matches_expected_count — the family count is ≥ 13 (and ≥ 14 if C-PHASE 189 has landed; the test conditionally probes for `apply_v0_invariant_check_emit_enabled` via `inventory.Any(f => f.Key == ...)` and adjusts the expected count).
4) [Fact] ApplyRunner_family_flag_set_contains_expected_keys — every key in this list is present in the live inventory:
   - apply_runner_design_approved
   - apply_runner_implementation_phase_approved
   - apply_runner_v0_enabled
   - apply_runner_v0_dry_run_required
   - apply_runner_v0_backup_required
   - apply_runner_v0_post_proof_required
   - apply_runner_v0_rollback_required
   - apply_runner_v0_extended_artifact_types_enabled
   - apply_runner_v0_explicit_rollback_enabled
   - apply_runner_v0_explicit_rollback_design_approved
   - apply_runner_prerequisite_check_enabled
   - apply_runner_status_report_enabled
   - mutation_scope_audit_emit_enabled

Stop gates:
- Any production source under vnext/src/ or vnext/tools/ is modified.
- The test hardcodes the inventory list instead of reading live `CapabilityFlagInventory.Flags`.
- The test depends on string-matching `DefaultValue` against the literal "False"; it must use `bool.FalseString`.
- Pre-flight gate fails.

Validation:
A) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerFamilyFlagDefaultOffAudit"
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check

Commit message:
C-PHASE 192: apply-runner family flag default-off audit test (live-inventory-driven; covers 13–14 keys)

PR title: C-PHASE 192: apply-runner family flag default-off audit test
Auto-continue: No
```

---

## §5 — C-PHASE 193: Future source-proposal dry-run report runner — DESIGN DRAFT ONLY (doc)

**DO NOT IMPLEMENT YET — DESIGN DRAFT, NOT APPROVED FOR IMPLEMENTATION. EVEN AFTER USER SIGNOFF, THIS PHASE PRODUCES ONLY A MARKDOWN DOC AND NO SOURCE CHANGE.**

```text
Paste only after the user has explicitly approved drafting the C-PHASE 193 design doc.

Start with the §0 common header above.

Phase: C-PHASE 193 — Future source-proposal dry-run report runner — DESIGN DRAFT ONLY
Branch: claude-implementation/c-phase-193-source-proposal-dry-run-design-draft

Goal:
Author a single markdown design draft for a hypothetical future runner that reads a small allow-list of source files and writes a proposed source diff as a `*.draft.json` artifact under `vnext/artifacts/<operation-id>/<scope-id>/source_proposal.draft.json`. This runner does NOT exist and is NOT being implemented in this phase. The doc describes its shape so that, if the user later approves an implementation prompt, no further design round-trip is required.

Files in scope:
- docs/C_PHASE193_FUTURE_SOURCE_PROPOSAL_DRY_RUN_DESIGN_DRAFT_2026-05-20.md (new)
- docs/codex-phase-history.jsonl (one append)

ABSOLUTE PROHIBITION:
NO file under vnext/, NO file in any tool project, NO .csproj change, NO .sln change.
Only the markdown doc and the history append are allowed.

Required doc structure (this is the only deliverable):
0. H1 header MUST read exactly: "DO NOT IMPLEMENT YET — DESIGN DRAFT REQUIRING USER SIGNOFF"
1. Scope of the proposed prototype (read allow-list; write target = artifact root only; never source tree).
2. Prerequisites that must pass before any implementation prompt is drafted.
3. New capability flags (all default OFF):
   - source_proposal_dry_run_design_approved
   - source_proposal_dry_run_implementation_phase_approved
   - source_proposal_dry_run_v0_enabled
   - source_proposal_dry_run_v0_read_allow_list_enforced (tightening flag; default True when its parent flag is True)
4. New audit packet kinds (all default-off-gated):
   - self_improvement_source_proposal_dry_run_started
   - self_improvement_source_proposal_dry_run_completed
   - self_improvement_source_proposal_dry_run_refused
5. Mandatory sequence (read allow-list → hash inputs → compute proposed diff → write `<x>.draft.json` artifact via the existing 183 pipeline OR a sibling reader-then-writer; OPEN QUESTION).
6. Stop gates (no source write; refuse on read outside the allow-list; refuse on extension outside `.cs/.csproj/.xaml/.config`; refuse if proposed artifact is not strictly under `vnext/artifacts/`; refuse unless `mutation_scope_audit_emit_enabled=true`).
7. Test strategy (deterministic; read allow-list enforcement; refusal evidence packet shape; artifact-only write target).
8. Why this is not implemented now (link back to master plan §4 invariants).
9. Open questions for the user (file set; allow-list location; whether `.png/.svg` are allowed AS reads; whether the source-proposal artifact is reviewable only or also approval-mutable via the existing 183/185 runners; whether this runner runs in-process or via CLI only).
10. Required user actions before any C-PHASE NNN implementation prompt is drafted.

Stop gates:
- Any file changed outside `docs/`.
- Any source under `vnext/` added or modified.
- H1 differs from the exact required string.
- Any of the 10 required sections missing.
- Pre-flight gate fails.

Validation (doc-only phase; the build/test gates still run to prove nothing else changed):
A) dotnet build .\vnext\Wevito.VNext.sln
B) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
C) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
D) git diff --check
E) git diff --stat origin/main..HEAD must touch only `docs/C_PHASE193_FUTURE_SOURCE_PROPOSAL_DRY_RUN_DESIGN_DRAFT_2026-05-20.md` and `docs/codex-phase-history.jsonl`.

Commit message:
C-PHASE 193: future source-proposal dry-run runner — DESIGN DRAFT ONLY (doc, no source change)

PR title: C-PHASE 193: future source-proposal dry-run design draft (doc only)
Auto-continue: No
```

---

## §6 — C-PHASE 194: Post-C-PHASE-189 apply-runner product-truth retest

**DO NOT IMPLEMENT YET — DESIGN DRAFT, NOT APPROVED FOR IMPLEMENTATION.**

Wait for explicit user signoff before pasting this prompt into Codex.

```text
Paste only after C-PHASE 189 has merged AND every other predecessor whose conditional you want asserted has also merged. The test class adapts to which predecessors landed.

Start with the §0 common header above.

Phase: C-PHASE 194 — Post-C-PHASE-189 apply-runner product-truth retest
Branch: claude-implementation/c-phase-194-post-c189-apply-runner-product-truth

Goal:
Pin the post-189 surface (and post-190/191/192 contributions if landed) into deterministic [Fact] tests in a single new test class `PostC189ApplyRunnerProductTruthTests.cs`. Mirror the conditional-skip pattern from `PostC183ApplyRunnerProductTruthTests`. No production source change.

Files in scope:
- vnext/tests/Wevito.VNext.Tests/PostC189ApplyRunnerProductTruthTests.cs (new)
- docs/C_PHASE194_POST_C189_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-20.md (new)
- docs/codex-phase-history.jsonl (one append)

ABSOLUTE PROHIBITION:
No file under vnext/src/ or vnext/tools/ may be modified. No Wevito.VNext.sln change.

Allowed changes (test class only):
1) Section A (always asserted):
   - ProductTruth_invariant_watchdog_exposes_three_v0_rules
   - ProductTruth_apply_v0_invariant_check_failed_packet_kind_constant
   - ProductTruth_apply_v0_invariant_check_failed_in_known_packet_kinds
   - ProductTruth_apply_v0_invariant_check_emit_enabled_flag_default_false
   - ProductTruth_supervised_loop_apply_runner_not_implemented_reason_unchanged
   - ProductTruth_legacy_apply_completed_constant_unchanged
2) Section B (conditional on C-PHASE 190 landed):
   - ProductTruth_status_report_record_has_five_new_narrow_fields
   - ProductTruth_status_report_legacy_apply_runner_implemented_still_false_for_default_deny
3) Section C (conditional on C-PHASE 191 landed):
   - ProductTruth_caller_allow_list_type_exists_and_is_empty
   - ProductTruth_no_production_caller_of_apply_or_explicit_rollback
4) Section D (conditional on C-PHASE 192 landed):
   - ProductTruth_apply_runner_family_flag_count_at_least_thirteen_or_fourteen_with_189
   - ProductTruth_apply_runner_family_flags_all_default_false
5) Section E (always asserted):
   - ProductTruth_no_production_source_file_changed_under_vnext_src
     (this is enforced procedurally by the phase itself, not by a runtime test; the [Fact] documents the constraint as a comment with the literal assertion that the test class's own filename starts with `PostC189ApplyRunnerProductTruth`).

Conditional pattern (mirror C-PHASE 188):
- Each conditional Section B/C/D test uses `Type.GetType("FullyQualifiedTypeName, AssemblyName")` to detect availability.
- If type is null, the test calls `Assert.True(true, "Skipped: C-PHASE NNN predecessor has not merged.")` — make the Skip reason explicit and human-readable.

Stop gates:
- Any production source under vnext/src/ or vnext/tools/ modified.
- Any [Fact] implemented via mocking or relaxed-assertion.
- Any conditional assertion's Skip reason omits the predecessor identifier.
- Test class imports System.Net.Http or System.Net.Sockets.
- Test class references held-out or in-distribution eval store types.
- Pre-flight gate fails.

Validation:
A) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PostC189ApplyRunnerProductTruth|PostC183ApplyRunnerProductTruth|InvariantViolationWatchdog|PlainLanguage|SelfImprovement"
B) dotnet build .\vnext\Wevito.VNext.sln
C) dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
D) powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
E) git diff --check

Commit message:
C-PHASE 194: post-C-PHASE-189 apply-runner product-truth retest (test-only; conditional skips for 190/191/192)

PR title: C-PHASE 194: post-C-PHASE-189 apply-runner product-truth retest
Auto-continue: No
```

---

## Appendix A — Reading order (for any phase in this batch)

1. `docs/CLAUDE_C_PHASE189_PLUS_PLAN_2026-05-20.md`
2. This prompts pack §0 common header + the per-phase section
3. The standalone starter for C-PHASE 189: `docs/CODEX_PROMPT_C_PHASE_189_2026-05-20.md`
4. The relevant predecessor phase report (`docs/C_PHASE183_*.md` through `docs/C_PHASE188_*.md`)
5. The source / test files named in the phase's "Files in scope" section
6. `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` §4 (hard invariants)

## Appendix B — How to choose the timestamp for the history append

Append immediately after `gh pr create` returns. Use the current UTC time with 7-fractional-digit precision and `Z` suffix to match existing rows. Example PowerShell:

```powershell
([DateTimeOffset]::UtcNow).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
```
