# Claude Plan — C-PHASE 189 + Follow-on Roadmap (190–194)

Date: 2026-05-20
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (executor, medium reasoning effort) + project owner (review)
Predecessors merged to `main`:

- C-PHASE 183 — `ArtifactRenameApplyRunner` (smallest safe apply-runner)
- C-PHASE 184 — Apply-runner review UI (read-only Tool Popup expander)
- C-PHASE 185 — `ArtifactRenameRollbackRunner` + rollback evidence expander
- C-PHASE 186 — Mutation sandbox hardening (`MutationScopeGuard` + IL-based `MutationScopeAuditTests`)
- C-PHASE 187 — Extended artifact-only rename scope (`.txt/.md/.svg` behind one default-off flag; 30+ forbidden code/asset extensions hard-refused)
- C-PHASE 188 — Post-C-PHASE-183 apply-runner product-truth retest (test-only)

Main tip at planning time: `9b0dd40a` (merge of PR #285, C-PHASE 188). Local working tree was on the branch `claude-implementation/c-phase-188-…` with HEAD `eeb33136c` (the docs-history append commit) when this plan was written; that commit is non-functional. Codex must rebase onto `origin/main` before starting any phase below.

## 0. Summary

The C-PHASE 183–188 batch produced the smallest viable apply-runner family: a narrow rename runner, a symmetric explicit rollback, a runtime mutation scope guard plus an IL-based static allow-list, an opt-in `.txt/.md/.svg` extension flag with 30+ forbidden code/asset extensions hard-refused, and a product-truth test class pinning the resulting surface. The supervised improvement loop still refuses with `apply_runner_not_implemented_in_v0`. The narrow runner is invocable only via its explicit constructor with full prerequisite, KillSwitch, approval-token, scope-hash, regex-path, reparse-point, forbidden-extension, and 7-flag-all-true checks.

The next batch (C-PHASE 189–194) does **not** widen mutation authority. It tightens what already exists:

- **C-PHASE 189** extends `InvariantViolationWatchdog` with three deterministic rules covering the new `_v0_` packet sequences. Implementation-approved.
- **C-PHASE 190** extends `ApplyRunnerStatusReport` additively so reviewers can see the narrow runner's *current* state (implemented yes/no, flags-all-false, last-completed-utc) without changing the legacy supervised-loop honesty signal. Implementation-approved.
- **C-PHASE 191** adds a static caller allow-list test that pins which production types are permitted to invoke `ArtifactRenameApplyRunner.Apply` and `ArtifactRenameRollbackRunner.ExplicitRollback` — preventing any scheduler / loop / autonomous service from gaining a back-door call site. Test-only. Implementation-approved.
- **C-PHASE 192** adds a deterministic test asserting every `apply_runner_*` flag and the `mutation_scope_audit_emit_enabled` flag default to `bool.FalseString` in `CapabilityFlagInventory`. Test-only. Implementation-approved.
- **C-PHASE 193** is **DESIGN DRAFT ONLY — NOT APPROVED FOR IMPLEMENTATION**. It documents the shape of a future *read-only* source-proposal dry-run report runner that would produce a diff artifact under `vnext/artifacts/` without ever writing back to source. Doc only; no source change.
- **C-PHASE 194** retests post-189 product truth with a new `PostC189ApplyRunnerProductTruthTests.cs`. Test-only. Implementation-approved.

Every phase in this batch is **Auto-continue=No**. No phase flips any capability default to True. No phase introduces a hosted-AI runtime brain. No phase adds a network surface. No phase grants Wevito write authority over source code, runtime assets, sprites, content manifests, user files, model weights, or any path outside the canonical artifact root.

## 1. Hard Invariants (apply to every phase in this batch)

- No hosted AI as Wevito's runtime brain. No phase imports `System.Net.Http` for runtime use.
- No silent network access. Only loopback endpoints, gated by explicitly-on flags, may exist. No phase in this batch opens a new socket.
- No silent training. No model-weight updates. No silent file / tool / code / asset mutation.
- Every mutation pathway requires: exact scope, dry-run, backup, sha256 evidence, apply, post-proof, rollback, user-visible report. The 183/185 runners already enforce this. No new mutation pathway is introduced by this batch.
- `KillSwitchService.IsActive()` halts every adapter, scheduler, autonomous loop, runner, scope, experiment, eval runner, approval handler, judge, replay harness, snapshot tool, replay runner, in-distribution seed tool, local scoring provider, capabilities-and-gates snapshot, apply-runner prerequisite check, apply-runner status report, apply-runner activity service, scoring dry-run, local runtime probe, eval coverage health, proposal quality metrics, narrow apply runner, narrow rollback runner, mutation-scope guard, mutation-scope audit producer (when later enabled), invariant watchdog, and every service added by this batch (including the v0 invariant extension and the status report extension).
- `AuditLedgerService` remains append-only. No phase introduces UPDATE or DELETE SQL.
- Every new audit packet kind appears in `PlainLanguageExplainer.KnownPacketKinds` with a plain-language sentence. (Only C-PHASE 189 introduces a new packet kind, and it is itself an *invariant violation* kind, not a mutation kind.)
- Every evidence packet sets `DidUseNetwork`, `DidUseHostedAi`, `DidUseLocalModel`, and `DidMutate` honestly. The new invariant-violation packet kind sets `DidMutate=false`.
- Every new capability flag defaults `bool.FalseString` in `CapabilityFlagInventory`. (Only C-PHASE 189 introduces a new flag, and that flag defaults False.)
- Held-out eval data must stay invisible. Every new production type added by this batch is added to `HeldOutEvalStoreVisibilityTests.forbiddenTypes`.
- Pets remain visually normal pet-sim characters. Pet game FPS / user PC experience stays protected. No phase in this batch wires anything new into pet-sim or sprite-workflow UI.
- The human remains the v0 judge. `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` is NOT modified by any phase in this batch. The legacy supervised loop continues to refuse.
- The legacy `SelfImprovementPacketKinds.ApplyCompleted` constant is preserved unchanged.
- Codex never auto-merges Auto-continue=No phases.

## 2. Phase Inventory

| # | Phase | Title | Status | Auto-continue | PR branch |
|---|---|---|---|---|---|
| 1 | C-PHASE 189 | Apply-v0 InvariantViolationWatchdog extension | **Approved for implementation** | No | `claude-implementation/c-phase-189-apply-v0-invariant-watchdog` |
| 2 | C-PHASE 190 | Apply-runner status report additive narrow-runner fields | **DESIGN DRAFT — NOT YET APPROVED** | No | `claude-implementation/c-phase-190-apply-runner-status-report-narrow-fields` |
| 3 | C-PHASE 191 | Static apply-runner caller allow-list test | **DESIGN DRAFT — NOT YET APPROVED** | No | `claude-implementation/c-phase-191-apply-runner-caller-allow-list` |
| 4 | C-PHASE 192 | Apply-runner family flag default-off audit test | **DESIGN DRAFT — NOT YET APPROVED** | No | `claude-implementation/c-phase-192-apply-runner-family-flag-default-off-audit` |
| 5 | C-PHASE 193 | Future source-proposal dry-run report runner — DESIGN DRAFT ONLY (doc) | **DESIGN DRAFT — NOT APPROVED FOR IMPLEMENTATION** | No | `claude-implementation/c-phase-193-source-proposal-dry-run-design-draft` |
| 6 | C-PHASE 194 | Post-C-PHASE-189 apply-runner product-truth retest | **DESIGN DRAFT — NOT YET APPROVED** | No | `claude-implementation/c-phase-194-post-c189-apply-runner-product-truth` |

Only C-PHASE 189 is approved for implementation in this round. The user must explicitly approve each design-draft phase before Codex begins it. Drafting the prompt does not constitute approval. C-PHASE 193 is doc-only; even if approved for implementation, its *only* deliverable is a `docs/` markdown file — no source change is permitted.

## 3. Dependency Graph

```text
189 invariant watchdog ext.  ──> 190 status report ext. (status report cites watchdog)
                              ──> 194 product-truth retest (asserts 189 rules exist)
190 status report ext.        ──> 194 product-truth retest (asserts new fields)
191 caller allow-list test    ──> 194 product-truth retest (asserts allow-list)
192 flag default-off audit    ──> 194 product-truth retest (asserts audit class exists)
193 source-proposal design    ──> independent (doc only; no production change)
194 product-truth retest      ──> 189 / 190 / 191 / 192 must be merged
```

Each later phase will refuse if its required predecessors are not on `main` at preflight time. C-PHASE 194 explicitly degrades each conditional assertion if a predecessor has not landed, exactly like C-PHASE 188 did for 184–187.

## 4. Per-Phase Specifications

### 4.1 C-PHASE 189 — Apply-v0 InvariantViolationWatchdog extension

Status: **Approved for implementation.**

Goal: Extend `Wevito.VNext.Core.SelfImprovement.Invariants.InvariantViolationWatchdog` with three deterministic rules that detect impossible v0 sequences in the ledger. The watchdog already builds `BuildApplyCompletedWithoutAwaitingApproval` for the legacy supervised path; this phase adds:

- **R1 `v0_completed_without_applied`** — fails if any row with `packet_kind = self_improvement_apply_v0_completed` has no matching preceding `self_improvement_apply_v0_applied` row sharing the same `operation_id`.
- **R2 `v0_rolled_back_followed_by_completed`** — fails if any operation has both a `self_improvement_apply_v0_rolled_back` row and a *later* `self_improvement_apply_v0_completed` row for the same `operation_id`.
- **R3 `explicit_rollback_completed_without_started`** — fails if any `self_improvement_apply_v0_explicit_rollback_completed` row has no matching preceding `self_improvement_apply_v0_explicit_rollback_started` row sharing the same `operation_id`.

When a rule fails, the watchdog's existing emit path writes a `self_improvement_maturity_clock_reset` packet with the failure summary, exactly as it already does for the legacy rule.

One new packet kind is also added (for the runtime invariant emit path that future phases will enable):

- `self_improvement_apply_v0_invariant_check_failed` — `DidMutate=false`. Plain-language sentence: "Wevito recorded a self-improvement apply-v0 sequence invariant violation found by the watchdog."

This packet kind is *only* emitted when a new flag is True:

- `apply_v0_invariant_check_emit_enabled` (default `bool.FalseString`)

The flag is added to `CapabilityFlagInventory` with default False. No producer enables it. The legacy maturity-clock-reset emit path is unchanged.

Allowed file changes:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs` (extend; add three rule builders + one optional emitter for the new packet kind)
- `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs` (extend; one new constant `ApplyV0InvariantCheckFailed`)
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs` (extend; one new flag `apply_v0_invariant_check_emit_enabled` default `bool.FalseString`)
- `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs` (extend; add the new packet kind to `KnownPacketKinds` + explainer switch arm)
- `vnext/tests/Wevito.VNext.Tests/InvariantViolationWatchdogV0Tests.cs` (new test class)
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs` (no change required — watchdog is already in the forbidden list)
- `docs/C_PHASE189_APPLY_V0_INVARIANT_WATCHDOG_2026-05-20.md` (new phase report)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: any mutation of the legacy `BuildApplyCompletedWithoutAwaitingApproval` rule semantics; any change to `SelfImprovementPacketKinds.ApplyCompleted` constant; any UI surface; any new source-code mutation pathway; any held-out content read; any network call; any UPDATE/DELETE SQL; any change to the legacy `MaturityClockReset` packet kind or emit shape.

Required test cases (≥ 15 [Fact] tests, including):

- R1 happy path: `applied` → `completed` for one operation = pass.
- R1 failure path: `completed` with no `applied` for same operation = fail row.
- R1 cross-operation isolation: `applied` for op A, `completed` for op B = fail (B has no `applied`).
- R2 happy path: `applied` → `completed` for op A, separate `applied` → `rolled_back` for op B = pass.
- R2 failure path: `applied` → `rolled_back` → `completed` for same operation = fail (`completed` after rollback).
- R2 timing edge: rollback at T+1, completed at T (completed *before* rollback) = pass for R2 (R1 may still flag).
- R3 happy path: `explicit_rollback_started` → `explicit_rollback_completed` = pass.
- R3 failure path: `explicit_rollback_completed` with no `explicit_rollback_started` = fail row.
- R3 refusal isolation: `explicit_rollback_refused` rows are not counted as completion-precondition starts.
- Empty ledger = pass for all three rules.
- KillSwitch active = watchdog returns blocked result (mirrors existing behavior).
- New flag default-off = no `self_improvement_apply_v0_invariant_check_failed` packet is written even when failures exist.
- New flag explicitly True = a single emit writes exactly one `self_improvement_apply_v0_invariant_check_failed` packet per failing rule, with `DidMutate=false`.
- Plain-language explainer returns the registered sentence for the new packet kind.
- Watchdog source contains no `UPDATE`, `DELETE FROM`, `DROP TABLE`, `INSERT` outside the existing ledger-write path.

Validation:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "InvariantViolationWatchdog|PlainLanguage|SelfImprovement"`
- `dotnet build .\vnext\Wevito.VNext.sln`
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`
- `git diff --check`

Stop gates:

- Any new rule modifies the semantics of `BuildApplyCompletedWithoutAwaitingApproval`.
- Any new packet kind missing from `PlainLanguageExplainer.KnownPacketKinds`.
- Any new flag defaulting to a value other than `bool.FalseString`.
- Any production source containing `apply_v0_invariant_check_emit_enabled = true` literal.
- Watchdog source containing `UPDATE`, `DELETE FROM`, `DROP TABLE`.
- Any test using `Mock<>` or other mocking framework for the watchdog (use fake ledger writes only).
- Pre-flight gate (build / test / vnext build / git diff --check) fails before changes.

PR: `claude-implementation/c-phase-189-apply-v0-invariant-watchdog`. Auto-continue: No.

### 4.2 C-PHASE 190 — Apply-runner status report additive narrow-runner fields

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Extend `ApplyRunnerStatusReport` additively so reviewers can see the *current* narrow-runner state without changing the legacy supervised-loop honesty signal. The legacy field `ApplyRunnerImplemented` is preserved exactly (still derived from `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`). The new fields describe the narrow runner family separately.

New record fields (additive, end of constructor signature):

- `NarrowArtifactRenameRunnerImplemented` (bool) — `true` iff the runner type exists in the assembly under `Wevito.VNext.Core.SelfImprovement.Apply.ArtifactRenameApplyRunner`.
- `NarrowArtifactRenameRunnerAllFlagsDefaultFalse` (bool) — `true` iff every flag in the inventory whose key starts with `apply_runner_` has `DefaultValue == bool.FalseString`, AND the `mutation_scope_audit_emit_enabled` flag defaults False, AND the `apply_v0_invariant_check_emit_enabled` flag defaults False.
- `NarrowArtifactRenameRunnerExtensionFlagDefaultFalse` (bool) — `true` iff `apply_runner_v0_extended_artifact_types_enabled` defaults False in `CapabilityFlagInventory`.
- `NarrowArtifactRenameRunnerLastSequenceUtc` (DateTimeOffset?) — UTC of the most recent `self_improvement_apply_v0_completed` row in the ledger, or null if none.
- `NarrowExplicitRollbackRunnerLastSequenceUtc` (DateTimeOffset?) — UTC of the most recent `self_improvement_apply_v0_explicit_rollback_completed` row in the ledger, or null if none.

The service `ApplyRunnerStatusReportService.EmitReport` keeps its existing behavior (default off, KillSwitch refuses, default provider settings refuse) and additionally populates the new fields. The packet kind `self_improvement_apply_runner_status_report` is unchanged; only the embedded JSON summary gains the new fields.

Allowed file changes:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReport.cs` (extend; five new optional fields)
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReportService.cs` (extend; populate new fields read-only, do not change refusal logic)
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)` (extend; render the new fields in the existing read-only expander — no new mutating control)
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerStatusReportServiceTests.cs` (extend)
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerStatusReportNarrowRunnerFieldsTests.cs` (new test class)
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs` (no change required — already listed)
- `docs/C_PHASE190_APPLY_RUNNER_STATUS_REPORT_NARROW_FIELDS_2026-05-20.md` (new phase report)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: any change that flips `ApplyRunnerImplemented` to True; any change to `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`; any new packet kind; any new flag; any UI Button / ToggleSwitch / CheckBox / MenuItem with a click-handler calling a mutating method; any held-out content read; any network call; any UPDATE/DELETE SQL.

Stop gates:

- The existing `ApplyRunnerImplemented` field's derivation rule is not modified.
- New JSON fields are *additive only* (no field renamed; no field removed).
- The new fields are computed *from* the live `CapabilityFlagInventory` and the live ledger, not hardcoded.
- The new ledger reads use the existing `Mode = ReadOnly` connection string pattern.
- Any new UI element is text-only and does not invoke `Apply`, `ExplicitRollback`, or `EmitReport`.
- Pre-flight gate fails.

PR: `claude-implementation/c-phase-190-apply-runner-status-report-narrow-fields`. Auto-continue: No.

### 4.3 C-PHASE 191 — Static apply-runner caller allow-list test

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Add a deterministic test that pins, by IL reflection over every type in `Wevito.VNext.Core.dll` plus `Wevito.VNext.Shell.dll`, which types are permitted to invoke `ArtifactRenameApplyRunner.Apply(...)` or `ArtifactRenameRollbackRunner.ExplicitRollback(...)`. Today no production caller is supposed to invoke them — they are reachable only via constructor-then-method from test code or, eventually, a future CLI / explicit user action. This test enforces that no scheduler, autonomous loop, supervised loop, household chore, sprite workflow service, etc., grows a back-door call site.

Test surface:

- `vnext/tests/Wevito.VNext.Tests/Support/ApplyRunnerCallerAllowList.cs` — explicit allow-list of type names. **Initial allow-list is empty.** No production caller is permitted yet.
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerCallerAllowListTests.cs` — IL reflection test:
  1. Load `Wevito.VNext.Core` and `Wevito.VNext.Shell` assemblies.
  2. For each type, for each method, decode `MethodInfo.GetMethodBody()` IL bytes and look for `Call` / `Callvirt` opcodes targeting any method on `ArtifactRenameApplyRunner` or `ArtifactRenameRollbackRunner` whose name matches `Apply` or `ExplicitRollback`.
  3. Collect every type that calls those methods.
  4. Assert that the collected set is a subset of `ApplyRunnerCallerAllowList.TypeNames`.

This mirrors the C-PHASE 186 `MutationScopeAuditTests` shape: IL-based discovery, not name-matching.

The test must additionally assert that the IL scanner does discover the test-only callers when run against the test assembly itself (sanity check — otherwise an empty result trivially passes). This sanity check uses a deliberately-named test helper type in the test assembly that calls `Apply` and verifies it appears in the scan when the test assembly is included.

Allowed file changes:

- `vnext/tests/Wevito.VNext.Tests/Support/ApplyRunnerCallerAllowList.cs` (new; empty allow-list)
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerCallerAllowListTests.cs` (new)
- `vnext/tests/Wevito.VNext.Tests/Support/ApplyRunnerCallerScannerSanityHelper.cs` (new; deliberately calls `Apply` in test code so the IL scanner has a known positive case)
- `docs/C_PHASE191_APPLY_RUNNER_CALLER_ALLOW_LIST_2026-05-20.md` (new phase report)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: any production source under `vnext/src/` is changed; any new production caller of `Apply` or `ExplicitRollback` is introduced; any new capability flag; any new packet kind; any held-out content read; any network call; any UPDATE/DELETE SQL.

Stop gates:

- Any production source file under `vnext/src/` is modified.
- The IL scanner is not reflection-driven (e.g., uses hardcoded string lists of caller names).
- The sanity-check test helper is missing — without it the assertion trivially passes if scanning is broken.
- The allow-list is non-empty at land time.
- Pre-flight gate fails.

PR: `claude-implementation/c-phase-191-apply-runner-caller-allow-list`. Auto-continue: No.

### 4.4 C-PHASE 192 — Apply-runner family flag default-off audit test

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Add a deterministic test asserting every flag in `CapabilityFlagInventory` whose key matches the apply-runner family defaults to `bool.FalseString`. The family is defined as keys starting with `apply_runner_`, plus the explicit set `{ "mutation_scope_audit_emit_enabled", "apply_v0_invariant_check_emit_enabled" }` (the second one is added by C-PHASE 189).

The test is purely a static safety net. C-PHASE 186 already audits *mutation callers*; this phase audits *flag defaults*. Together they make it impossible for an autonomous loop to silently flip on apply behavior without (a) editing source and (b) updating the audit to acknowledge the change.

Test surface:

- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerFamilyFlagDefaultOffAuditTests.cs` (new)
  - `[Fact] ApplyRunner_family_flags_default_off`
  - `[Fact] ApplyRunner_family_flag_set_matches_expected_count`
  - `[Fact] ApplyRunner_family_flag_set_contains_expected_keys`

Expected key set at land time (assuming 189 has landed):

```text
apply_runner_design_approved
apply_runner_implementation_phase_approved
apply_runner_v0_enabled
apply_runner_v0_dry_run_required
apply_runner_v0_backup_required
apply_runner_v0_post_proof_required
apply_runner_v0_rollback_required
apply_runner_v0_extended_artifact_types_enabled
apply_runner_v0_explicit_rollback_enabled
apply_runner_v0_explicit_rollback_design_approved
apply_runner_prerequisite_check_enabled
apply_runner_status_report_enabled
mutation_scope_audit_emit_enabled
apply_v0_invariant_check_emit_enabled
```

If 189 has not yet landed at the time 192 runs, the last entry is removed.

Allowed file changes:

- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerFamilyFlagDefaultOffAuditTests.cs` (new)
- `docs/C_PHASE192_APPLY_RUNNER_FAMILY_FLAG_DEFAULT_OFF_AUDIT_2026-05-20.md` (new phase report)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: any production source under `vnext/src/` is modified; any new capability flag; any new packet kind; any production behavior change.

Stop gates:

- Any production source under `vnext/src/` is modified.
- Test uses hardcoded list only without also verifying the live inventory contains every expected key (must read live inventory).
- Pre-flight gate fails.

PR: `claude-implementation/c-phase-192-apply-runner-family-flag-default-off-audit`. Auto-continue: No.

### 4.5 C-PHASE 193 — Future source-proposal dry-run report runner — DESIGN DRAFT ONLY

Status: **DESIGN DRAFT — NOT APPROVED FOR IMPLEMENTATION. Doc only.**

Goal: Document the shape of a hypothetical future runner that would produce a *read-only* source-proposal artifact under `vnext/artifacts/<operation-id>/<scope-id>/source_proposal.draft.json`. This runner would read a small allow-list of source files, compute a proposed diff, write that diff as a `*.draft.json` artifact under the canonical artifact root, and stop. It would never write back to source. The proposed artifact would then be reviewable in the existing apply-runner activity UI and could (only after explicit user approval and a future apply-source-mutation phase that does not yet exist) be applied via a *different* future runner that this phase does not propose.

The deliverable is a single doc, `docs/C_PHASE193_FUTURE_SOURCE_PROPOSAL_DRY_RUN_DESIGN_DRAFT_2026-05-20.md`. Required sections:

1. **DO NOT IMPLEMENT YET — DESIGN DRAFT REQUIRING USER SIGNOFF.**
2. Scope of the proposed prototype (read allow-list, write target = artifact root only).
3. Prerequisites that must pass before any implementation prompt is drafted (extend C-PHASE 174's 10-prerequisite list with: `source_proposal_dry_run_design_approved`, `source_proposal_dry_run_implementation_phase_approved`, both default False).
4. New capability flags (all default OFF):
   - `source_proposal_dry_run_design_approved`
   - `source_proposal_dry_run_implementation_phase_approved`
   - `source_proposal_dry_run_v0_enabled`
   - `source_proposal_dry_run_v0_read_allow_list_enforced` (default True; tightening flag)
5. New audit packet kinds (all default-off-gated):
   - `self_improvement_source_proposal_dry_run_started`
   - `self_improvement_source_proposal_dry_run_completed`
   - `self_improvement_source_proposal_dry_run_refused`
6. Mandatory sequence (read allow-list, hash inputs, compute diff, write `<x>.draft.json` artifact via the existing `ArtifactRenameApplyRunner` pipeline — or its read-only sibling; this is itself an open design question).
7. Stop gates (no source write of any kind; refuse on any read outside the allow-list; refuse on any extension outside `.cs/.csproj/.xaml/.config`; refuse if proposed artifact is not strictly under `vnext/artifacts/`; refuse unless `mutation_scope_audit_emit_enabled=true` so every read is auditable).
8. Test strategy (deterministic; read allow-list enforcement; refusal evidence packet shape; artifact-only write target).
9. Why this is not implemented now (link back to master plan §4 invariants).
10. Open questions for the user (what file set; whether read allow-list lives in inventory or in a separate scope; whether `.png/.svg` sprite-style inputs are allowed *as reads* even though they're forbidden as writes; whether the source-proposal artifact should be reviewable only or also approval-mutable via the existing 183/185 runners; whether this runner runs in-process or via CLI only).
11. Required user actions before any C-PHASE NNN implementation prompt is drafted.

Allowed file changes:

- `docs/C_PHASE193_FUTURE_SOURCE_PROPOSAL_DRY_RUN_DESIGN_DRAFT_2026-05-20.md` (new)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: **any source change** under `vnext/src/` or `vnext/tools/` or `vnext/tests/`; any change to `Wevito.VNext.sln`; any new flag; any new packet kind; any code at all in this phase beyond the markdown doc.

Stop gates:

- Any file changed outside `docs/`.
- Any source under `vnext/` is added or modified.
- The doc's title header does not read **DO NOT IMPLEMENT YET — DESIGN DRAFT REQUIRING USER SIGNOFF.**
- The doc is missing any of the 11 required sections.
- Pre-flight gate fails.

PR: `claude-implementation/c-phase-193-source-proposal-dry-run-design-draft`. Auto-continue: No.

### 4.6 C-PHASE 194 — Post-C-PHASE-189 apply-runner product-truth retest

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Test-only. Pin the post-189 self-improvement product truth (plus 190/191/192 contributions if they have landed) with deterministic `[Fact]` tests in a new file `PostC189ApplyRunnerProductTruthTests.cs`. Each conditional assertion guards on a small `IsAvailable(...)` helper that resolves a type by name and skips if the predecessor has not merged — mirroring C-PHASE 188's pattern.

Required claims (each one `ProductTruth_<short_claim_id>`):

- A1 (always asserted): `InvariantViolationWatchdog` exposes three v0 rules whose result identifiers contain `v0_completed_without_applied`, `v0_rolled_back_followed_by_completed`, and `explicit_rollback_completed_without_started`.
- A2 (always): `SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed` constant equals `"self_improvement_apply_v0_invariant_check_failed"`.
- A3 (always): `PlainLanguageExplainer.KnownPacketKinds` contains `ApplyV0InvariantCheckFailed`.
- A4 (always): `CapabilityFlagInventory` entry for `apply_v0_invariant_check_emit_enabled` exists and defaults `bool.FalseString`.
- A5 (always): `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` still equals `"apply_runner_not_implemented_in_v0"`.
- A6 (always): legacy `SelfImprovementPacketKinds.ApplyCompleted` constant unchanged.
- B1 (if 190 landed): `ApplyRunnerStatusReport` record exposes five additional fields whose names contain `NarrowArtifactRenameRunnerImplemented`, `NarrowArtifactRenameRunnerAllFlagsDefaultFalse`, `NarrowArtifactRenameRunnerExtensionFlagDefaultFalse`, `NarrowArtifactRenameRunnerLastSequenceUtc`, `NarrowExplicitRollbackRunnerLastSequenceUtc`.
- B2 (if 190 landed): legacy field `ApplyRunnerImplemented` still derived from `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` (assertion: a fresh report has `ApplyRunnerImplemented == false`).
- C1 (if 191 landed): `Wevito.VNext.Tests.Support.ApplyRunnerCallerAllowList.TypeNames` exists and is empty.
- C2 (if 191 landed): IL scan of `Wevito.VNext.Core` and `Wevito.VNext.Shell` finds zero production callers of `ArtifactRenameApplyRunner.Apply` or `ArtifactRenameRollbackRunner.ExplicitRollback`.
- D1 (if 192 landed): `CapabilityFlagInventory` exposes ≥ 14 keys matching the apply-runner family, all defaulting `bool.FalseString`.
- E1 (always): no production source under `vnext/src/` or `vnext/tools/` is modified by this phase.

Allowed file changes:

- `vnext/tests/Wevito.VNext.Tests/PostC189ApplyRunnerProductTruthTests.cs` (new)
- `docs/C_PHASE194_POST_C189_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-20.md` (new phase report)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: any production source under `vnext/src/` or `vnext/tools/`; any `Wevito.VNext.sln` change; any baseline claim implemented by mocking or relaxed-assertion; any new capability flag / packet kind.

Stop gates:

- Any production source under `vnext/src/` or `vnext/tools/` modified.
- Any test references `Mock<>` or other mocking framework.
- Any conditional assertion lacks a `Skip` reason that mentions the missing predecessor.
- Pre-flight gate fails.

PR: `claude-implementation/c-phase-194-post-c189-apply-runner-product-truth`. Auto-continue: No.

## 5. Risk Register

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| 1 | The new v0 watchdog rules accidentally fire on the existing happy path because the production runner emits a `completed` row whose `operation_id` shape differs slightly from `applied` | Medium | Reuse the existing `ExtractOperationId` helper that the legacy rule already uses; share an extraction unit test that exercises both packet kinds. |
| 2 | The new invariant packet kind starts emitting before the user is ready to see invariant noise in the audit ledger | Low (flag default off) | `apply_v0_invariant_check_emit_enabled` defaults False. No producer flips it. The watchdog's legacy emit path (maturity-clock-reset) is unchanged. |
| 3 | The status report extension (190) renames or removes a legacy field, breaking the C-PHASE 181 CLI tool | Medium if rushed | Stop gate: additive only; constructor parameter order is preserved; new params have defaults; deserialization is tested explicitly. |
| 4 | The caller allow-list scanner (191) produces false positives by counting generic helpers or `Func<>` indirection | Medium | Sanity-check test deliberately uses a helper that calls `Apply`; assertion fails if scanner cannot detect it. IL-decode-based detection (not source string match). |
| 5 | The caller allow-list scanner produces false negatives because the test assembly is not loaded | Medium | Sanity helper test is the canary: if the scanner returns empty in test assembly, the test fails. |
| 6 | The flag default-off audit (192) passes today but silently becomes stale when 193's design draft suggests new flag names | Low | 193 is doc-only. New flags appear only when the user signs off a future implementation phase; 194's product-truth retest catches the new flag count then. |
| 7 | C-PHASE 193's doc is read as approval to implement | Medium | The doc's H1 reads **DO NOT IMPLEMENT YET — DESIGN DRAFT REQUIRING USER SIGNOFF.** Codex never auto-merges Auto-continue=No phases. |
| 8 | Status report extension breaks the existing read-only Tool Popup expander | Low | New fields render in a separate text-only sub-section; the existing rendering is preserved; no new button. |
| 9 | Watchdog extension introduces a new emit path that writes to the ledger from a non-allow-list type | Low | The watchdog is already on the C-PHASE 186 MutationAllowList. The new emit path uses the existing emit method; no new write surface. The 186 IL audit re-runs and must pass. |
| 10 | Conditional assertions in 194 mask a real regression because the predecessor `IsAvailable` returns false due to a typo, not a missing predecessor | Medium | Each conditional assertion uses `Type.GetType("…")` with the exact fully-qualified name and emits a deliberate `Skip` reason. The reviewer notes section calls out this risk so the reviewer verifies each Skip reason. |
| 11 | Pet game FPS regression because the watchdog runs at higher cadence after the v0 rules land | Low | The watchdog is not scheduled by this batch. Its tick rate is unchanged. The new rules add O(N) ledger scans where N is small in practice. |
| 12 | Held-out eval data leaks through a new field in the status report (190) | Low | Status report fields are limited to (a) inventory metadata, (b) `apply_v0_*` ledger row timestamps. No held-out store type appears. `HeldOutEvalStoreVisibilityTests.forbiddenTypes` already covers `ApplyRunnerStatusReport` and `ApplyRunnerStatusReportService`. |
| 13 | Codex auto-merges 193 because it is "doc only" | Low | Every phase in this batch is Auto-continue=No. Codex's history convention treats Auto-continue=No as halt-for-review. |
| 14 | The reviewer interprets 193 as a commitment to ship code mutation | High if doc tone slips | Doc title + H1 + section 9 ("Why this is not implemented now") + section 11 ("Required user actions before any C-PHASE NNN implementation prompt is drafted") collectively make the non-commitment explicit. |

## 6. Milestone Narrative

After C-PHASE 189 lands, the watchdog can detect three impossible v0 sequences in the ledger. The maturity-clock reset path already wired for the legacy rule extends naturally to these new rules. The new invariant-check packet kind exists, but no producer emits it by default.

After 190 lands, the status report shows reviewers both the legacy supervised-loop honesty signal (still: not implemented) and the narrow-runner state (implemented, but every flag default-off, never used in production). Reviewers can see at a glance that the family is wired but unused.

After 191 lands, no autonomous loop / scheduler / supervised loop / household chore / sprite workflow service can grow a hidden call site that invokes the apply runner. Any future change that adds a call site fails the test and forces an explicit allow-list update — which itself is reviewable.

After 192 lands, the flag-default audit makes it impossible for a future change to silently flip an `apply_runner_*` default to True without (a) editing source, (b) updating the audit. Combined with 191, this means the only ways to invoke the apply runner are: (a) the user types True for every flag at runtime, AND (b) a permitted caller (initially: none — only test code and future user-approved CLI) invokes `Apply`.

193 captures the *shape* of the next safe step beyond artifact rename, but explicitly does not implement it. Whether that next step ever lands depends entirely on the user signing off a future implementation prompt.

194 pins the resulting state — both legacy and new — into deterministic tests, exactly as 188 did for the 183–187 surface.

## 7. Reading Order

For C-PHASE 189:

1. `docs/CLAUDE_C_PHASE189_PLUS_PLAN_2026-05-20.md` (this doc) §1 + §4.1
2. `docs/CLAUDE_C_PHASE189_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-20.md` §0 + §1
3. `docs/CODEX_PROMPT_C_PHASE_189_2026-05-20.md` (the standalone starter)
4. `docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md`
5. `docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md`
6. `docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md`
7. `docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md`
8. `docs/C_PHASE187_EXTENDED_ARTIFACT_TYPES_2026-05-19.md`
9. `docs/C_PHASE188_POST_C183_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-19.md`
10. `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs`
11. `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`
12. `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`
13. `vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs`

For each follow-on phase: this plan §4.x + the corresponding §x in `docs/CLAUDE_C_PHASE189_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-20.md`.

## 8. Operational Discipline

- Each phase opens a separate PR. Codex never opens a PR that combines two phases.
- Each phase appends one row to `docs/codex-phase-history.jsonl` matching the existing shape:
  ```json
  {"phase_id":"C-PHASE NNN","timestamp_utc":"2026-05-20TXX:XX:XX.XXXXXXXZ","branch":"claude-implementation/c-phase-NNN-...","pr_url":"https://github.com/ssebrobless/wevito/pull/<PR-NUMBER>"}
  ```
- Codex never auto-merges any phase in this batch. Every phase is Auto-continue=No.
- Codex never flips a capability flag default from False to True. Every flag introduced by this batch (only one: `apply_v0_invariant_check_emit_enabled`) defaults False at code time. Runtime flips happen only at the user's keyboard.
- If a phase's pre-flight gate fails (build / test / vnext build / git diff --check), Codex halts with a Stop card. Codex does not patch around pre-flight failures.
- Before opening any PR in this batch, Codex must rebase onto the latest `origin/main` so the merged C-PHASE 188 tip (`9b0dd40a`) is the base.

## 9. Reviewer Notes

- For 189: verify the three new rule identifiers do not collide with the existing legacy identifiers. Verify the new packet kind appears in `KnownPacketKinds` with an English sentence (not a placeholder). Verify the new flag defaults `bool.FalseString` — exact constant, not a string literal.
- For 189: verify the watchdog's new emit path is gated on the new flag AND on KillSwitch (mirror existing pattern). Verify no producer flips the new flag.
- For 190: verify the legacy field `ApplyRunnerImplemented` continues to return False on a default-deny snapshot. Verify the new fields render in the Tool Popup expander as text only — no new Button / ToggleSwitch / CheckBox / MenuItem.
- For 190: verify the JSON shape is deserializable by the C-PHASE 181 CLI without modification (CLI tests still pass).
- For 191: verify the IL scanner is not source-string-based. Verify the sanity helper deliberately calls `Apply` and the scanner detects it.
- For 191: verify the allow-list is empty at merge time. Any non-empty entry is a separate phase that requires explicit user approval.
- For 192: verify the test reads the live inventory, not a hardcoded list. Verify the expected key set includes every key from 183/185/186/187/189 (the last only if 189 has landed).
- For 193: verify the doc's H1 reads exactly **DO NOT IMPLEMENT YET — DESIGN DRAFT REQUIRING USER SIGNOFF**. Verify no production source file is modified. Verify section 11 lists all required user signoffs.
- For 194: verify every conditional `Skip` reason names the missing predecessor and is human-readable.

## 10. Sources Cited

- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` — hard invariants (§4)
- `docs/DECISION_LEDGER_2026-05-15.md`
- `docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md` — predecessor plan
- `docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md`
- `docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md`
- `docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md`
- `docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md`
- `docs/C_PHASE187_EXTENDED_ARTIFACT_TYPES_2026-05-19.md`
- `docs/C_PHASE188_POST_C183_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-19.md`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs` — extended by 189
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReport.cs` — extended by 190
- `vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReportService.cs` — extended by 190
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs` — IL-scanned by 191
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs` — IL-scanned by 191
- `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs` — audited by 192
- `vnext/tests/Wevito.VNext.Tests/Support/MutationAllowList.cs` — parallel pattern reference for 191
- `vnext/tests/Wevito.VNext.Tests/PostC183ApplyRunnerProductTruthTests.cs` — parallel pattern reference for 194
