# Claude Plan — C-PHASE 183 + Follow-on Roadmap (184–188)

Date: 2026-05-19
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (executor, medium reasoning effort) + project owner (review)
Predecessor: `docs/C_PHASE182_FUTURE_APPLY_RUNNER_DESIGN_DRAFT_2026-05-19.md`
Current main tip at planning time: `9e91e8cd5bfc3deb30b3d82d7d4b80ea59f2596c` — C-PHASE 182: future apply-runner prototype design draft (DOC ONLY)

## 0. Summary

C-PHASE 182 produced a design draft for the first real apply-runner. C-PHASE 183 implements the smallest safe version of that design: a single-purpose service that atomically renames one JSON artifact under the canonical artifact root from `*.draft.json` to `*.approved.json`. Every other phase in this roadmap (184–188) is **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION**.

The user has explicitly approved drafting and implementing C-PHASE 183. The user has NOT yet approved 184, 185, 186, 187, or 188. Each of those phases is included as a copy-paste Codex prompt only so that, when the user signs off on each in turn, no further planning round-trip is required.

Every phase in this batch is Auto-continue=No. No phase flips a capability default to On. No phase introduces a hosted-AI runtime brain. No phase adds a network surface. No phase grants Wevito write authority over source code, runtime assets, sprites, content manifests, user files, model weights, or any path outside the canonical artifact root.

## 1. Hard Invariants (apply to every phase in this batch)

- No hosted AI as Wevito's runtime brain.
- No silent network access. Only loopback endpoints, gated by explicitly-on flags, may exist. No phase in this batch opens a new socket.
- No silent training. No model-weight updates. No silent file / tool / code / asset mutation.
- Every mutation pathway requires: exact scope, dry-run, backup, sha256 evidence, apply, post-proof, rollback, user-visible report. The new apply runner enforces this for every successful apply.
- KillSwitchService.IsActive() halts every adapter, scheduler, autonomous loop, runner, scope, experiment, eval runner, approval handler, judge, replay harness, snapshot tool, replay runner, in-distribution seed tool, local scoring provider, capabilities-and-gates snapshot, apply-runner prerequisite check, apply-runner status report, scoring dry-run, local runtime probe, eval coverage health, proposal quality metrics, and every service added by this batch (including the new apply runner, rollback runner, review UI, mutation sandbox, and extended-artifact runner).
- AuditLedgerService remains append-only. No phase introduces UPDATE or DELETE SQL.
- Every new audit packet kind appears in `PlainLanguageExplainer.KnownPacketKinds` with a plain-language sentence.
- Every evidence packet sets `DidUseNetwork`, `DidUseHostedAi`, `DidUseLocalModel`, and `DidMutate` honestly. Mutation packets set `DidMutate=true`. Non-mutating packets set it `false`.
- Every new capability flag defaults OFF. The runner refuses unless every required flag is True.
- Held-out eval data must stay invisible. Every new service added by this batch is added to `HeldOutEvalStoreVisibilityTests.forbiddenTypes`.
- Pets remain visually normal pet-sim characters. Pet game FPS / user PC experience stays protected. No phase in this batch wires the apply runner into pet-sim or sprite-workflow UI.
- The human remains the v0 judge. `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` is NOT modified by any phase in this batch. The new `ArtifactRenameApplyRunner` is a separate narrow service; it is not called by the supervised loop.
- Codex never auto-merges Auto-continue=No phases.

## 2. Phase Inventory

| # | Phase | Title | Status | Auto-continue |
|---|---|---|---|---|
| 1 | C-PHASE 183 | Smallest safe apply-runner prototype (ArtifactRenameApplyRunner) | **Approved for implementation** | No |
| 2 | C-PHASE 184 | Apply-runner review UI (read-only Tool Popup expander) | **DESIGN DRAFT — NOT YET APPROVED** | No |
| 3 | C-PHASE 185 | Rollback evidence viewer + symmetric narrow rollback runner | **DESIGN DRAFT — NOT YET APPROVED** | No |
| 4 | C-PHASE 186 | Mutation sandbox hardening (runtime + static scope guard) | **DESIGN DRAFT — NOT YET APPROVED** | No |
| 5 | C-PHASE 187 | Expanded but artifact-only rename scope (.draft.txt/.md/.svg) | **DESIGN DRAFT — NOT YET APPROVED** | No |
| 6 | C-PHASE 188 | Product-truth retest after first apply-runner prototype | **DESIGN DRAFT — NOT YET APPROVED** | No |

The user must explicitly approve each design-draft phase before Codex begins it. Drafting the prompt does not constitute approval.

## 3. Dependency Graph

- 183 introduces the narrow runner, the 7 packets, and the 7 flags.
- 184 reads 183's packets (no apply trigger from UI; read-only).
- 185 depends on 183's packet schema; reuses path constraints; introduces a symmetric `*.approved.json → *.draft.json` rollback runner.
- 186 depends on 183 + 185 existing so the mutation-scope guard has full surface coverage.
- 187 extends 183 (and 185 if landed) to additional artifact-only file extensions; same path constraints; same packet schema.
- 188 retests the post-183 (and 184–187 if landed) self-improvement product truth.

Each later phase will refuse if its required predecessors are not on `main`.

## 4. Per-Phase Specifications

### 4.1 C-PHASE 183 — Smallest safe apply-runner prototype (ArtifactRenameApplyRunner)

Status: **Approved for implementation.**

Goal: Introduce a single-purpose narrow apply runner that can perform exactly one filesystem operation — rename `<x>.draft.json` to `<x>.approved.json` under `<artifact-root>\<operation-id>\<scope-id>\` — gated behind 7 default-off capability flags, the existing 10-prerequisite check, KillSwitch, and approval-token verification.

Allowed file changes: see C-PHASE 183 prompt §"Allowed file changes for this phase" in `docs/CLAUDE_C_PHASE183_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md` §1.

Forbidden surfaces: SupervisedImprovementLoop wiring, UI surface, source-code mutation, sprite mutation, additional file extensions, symmetric rollback runner, status report schema change.

Validation: standard four-command gate (filtered test, build, full test, vnext build) + `git diff --check`.

Stop gates: any required flag default flips to True, any packet kind missing from explainer, any mutation outside artifact root, any Refused path that wrote a packet, ApplyV0Completed without preceding 5 packets, ApplyCompleted (legacy) constant changed, `ApplyRunnerNotImplementedReason` changed, network import, held-out type reference, UPDATE/DELETE SQL.

PR: `claude-implementation/c-phase-183-artifact-rename-apply-runner`. Auto-continue: No.

### 4.2 C-PHASE 184 — Apply-runner review UI (read-only)

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Add a read-only Tool Popup expander labeled "Apply-runner activity (read-only)" that surfaces the last N apply-v0 sequences (their 7 packets) per operation, including the pre/post sha256, the relative paths, the success/refuse/rollback disposition, and the responsible flag-checklist. No button, no toggle, no click-handler that mutates. The expander does NOT invoke `Apply`. The expander does NOT modify any capability flag. The expander does NOT call `EmitReport`.

Allowed file changes:
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityService.cs` (new; read-only ledger reader)
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityEntry.cs` (new; immutable DTO)
- `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs` (extend; one factory)
- `vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)` (extend; one expander)
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityServiceTests.cs` (new)
- `vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityHeldOutAccessTests.cs` (new)
- `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs` (extend)
- `docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md` (new)
- `docs/codex-phase-history.jsonl` (one append)

Forbidden surfaces: any UI element with a click-handler that mutates state; any reference to `ArtifactRenameApplyRunner.Apply`; any new capability flag (the service reads existing ones); any new packet kind; any held-out content read; any network call.

Stop gates: the new expander contains any Button / ToggleSwitch / CheckBox / MenuItem with a click-handler that calls a non-`ReadOnly*` method; the activity service writes any packet; the activity service references a held-out or in-distribution type.

PR: `claude-implementation/c-phase-184-apply-runner-review-ui`. Auto-continue: No.

### 4.3 C-PHASE 185 — Rollback evidence viewer + symmetric narrow rollback runner

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: (a) Add `ArtifactRenameRollbackRunner` — the symmetric narrow operation: rename `<x>.approved.json` back to `<x>.draft.json` under the same path constraints (artifact-root, operation-id/scope-id segments, regex-validated relative path, approval token re-verification). All flags default OFF. Auto-rollback is the same shape as 183's auto-rollback, but this phase introduces an *explicit* user-initiated rollback. (b) Add a read-only "Rollback evidence" expander showing prior `ApplyV0RolledBack` packets and any new explicit-rollback packets emitted by the new runner.

New packet kinds (all default-off-gated):
- `self_improvement_apply_v0_explicit_rollback_started`
- `self_improvement_apply_v0_explicit_rollback_completed`
- `self_improvement_apply_v0_explicit_rollback_refused`

New flags (all default OFF):
- `apply_runner_v0_explicit_rollback_enabled`
- `apply_runner_v0_explicit_rollback_design_approved`

Stop gates: the rollback runner refuses unless the file at the source path is exactly `<x>.approved.json`; the rollback refuses if a destination `<x>.draft.json` already exists; the rollback writes no file outside the artifact root; the rollback never calls into `ArtifactRenameApplyRunner.Apply`.

PR: `claude-implementation/c-phase-185-artifact-rename-rollback-runner`. Auto-continue: No.

### 4.4 C-PHASE 186 — Mutation sandbox hardening

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Two complementary defenses against the runner ever mutating outside its declared scope.

(a) Static defense: `MutationScopeAuditTests.cs` enumerates every type in `Wevito.VNext.Core` that calls `File.Move`, `File.Copy`, `File.Delete`, `File.WriteAllText`, `File.WriteAllBytes`, `Directory.Delete`, or `FileStream` with a `FileMode.Create*` / `FileMode.Truncate`, and asserts the type is on an explicit `MutationAllowList`. The allow-list, by C-PHASE 186 land, must contain only: `ArtifactRenameApplyRunner`, `ArtifactRenameRollbackRunner` (if 185 has landed), the existing snapshot/replay/in-distribution-seed tool projects, and `AuditLedgerService` (append-only ledger). Anything else fails the test.

(b) Runtime defense: a `MutationScopeGuard` static utility used by every mutation-emitting service. The guard takes `(intendedPath, scopeName)` and throws if `Path.GetFullPath(intendedPath)` is not under the expected scope root for that scope. The guard records every guarded call to a new packet kind `self_improvement_mutation_scope_audit_event`, default-off-gated. (No guard call writes a packet by default; the audit packet kind exists only so future opt-in audits emit honest evidence.)

New packet kind: `self_improvement_mutation_scope_audit_event` (default-off-gated).
New flag: `mutation_scope_audit_emit_enabled` (default False).

Stop gates: the static test must enumerate types via reflection, not hardcoded; the runtime guard must throw, not silently swallow; the guard's source contains no try/catch that hides scope violations.

PR: `claude-implementation/c-phase-186-mutation-sandbox-hardening`. Auto-continue: No.

### 4.5 C-PHASE 187 — Expanded but artifact-only rename scope

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Extend `ArtifactRenameApplyRunner` to accept additional artifact-only file extensions, all still under `<artifact-root>\<operation-id>\<scope-id>\`. Allowed pairs:
- `*.draft.json` → `*.approved.json` (already in 183)
- `*.draft.txt`  → `*.approved.txt`  (new)
- `*.draft.md`   → `*.approved.md`   (new)
- `*.draft.svg`  → `*.approved.svg`  (new)

The runner's regex relaxes to `^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+/[a-zA-Z0-9._-]+\.draft\.(json|txt|md|svg)$`. Every other constraint stays identical. No new packet kinds. No new flags except one default-off opt-in:
- `apply_runner_v0_extended_artifact_types_enabled` (default False)

The runner refuses non-json extensions unless this flag is True. Even with the flag True, the runner still refuses paths outside artifact root, mismatched approval tokens, missing prerequisites, etc.

Stop gates: the runner must NOT accept any extension outside the 4-tuple list. Code-mutation extensions (`.cs`, `.csproj`, `.xaml`, `.config`, `.exe`, `.dll`, `.png` for sprites, etc.) must all refuse.

PR: `claude-implementation/c-phase-187-extended-artifact-types`. Auto-continue: No.

### 4.6 C-PHASE 188 — Product-truth retest after first apply-runner prototype

Status: **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION.**

Goal: Test-only. Pin the post-183 self-improvement product truth (plus 184–187 contributions if they have landed) with deterministic `[Fact]` tests in a new file `PostC183ApplyRunnerProductTruthTests.cs`. Claims include:
- `ArtifactRenameApplyRunner` exists in `Wevito.VNext.Core/SelfImprovement/Apply/`.
- The 7 apply-v0 packet kinds are present in `KnownPacketKinds`.
- All 7 apply-v0 capability flags default `bool.FalseString`.
- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` still equals `"apply_runner_not_implemented_in_v0"`.
- The legacy `SelfImprovementPacketKinds.ApplyCompleted` constant is unchanged.
- `HeldOutEvalStoreVisibilityTests.forbiddenTypes` contains `ArtifactRenameApplyRunner` and the 184/185/186/187 services if those have landed.
- If 185 has landed, `ArtifactRenameRollbackRunner` exists with the symmetric path constraints.
- If 186 has landed, `MutationScopeAuditTests` exists and the allow-list does not contain types it should not contain (verified by reflection).
- If 187 has landed, the runner accepts `.draft.txt|.md|.svg` only when `apply_runner_v0_extended_artifact_types_enabled=True`; refuses otherwise.
- No production source file changed in 188.

Stop gates: any [Fact] is implemented by mocking or relaxed-assertion; any production source file change; any new packet kind / capability flag.

PR: `claude-implementation/c-phase-188-post-c183-apply-runner-product-truth`. Auto-continue: No.

## 5. Risk Register

| # | Risk | Mitigation |
|---|---|---|
| 1 | The narrow runner is interpreted as broad approval to mutate source code | Hard invariant: artifact-root confinement, regex validation, scope-guard tests. 183 stop gate: source-code extensions refuse. |
| 2 | Default-off flags get flipped silently | Every flag defaults `bool.FalseString` in inventory. `CapabilityFlagInventory` tests assert the default. Watchdog reads flags from the ledger; flipping a flag is itself an auditable event. |
| 3 | Watchdog `BuildApplyCompletedWithoutAwaitingApproval` fires when the rename runner emits `ApplyV0Completed` | Mitigated by using a separate `_v0_` packet namespace. The watchdog only inspects the legacy `ApplyCompleted` constant. 183 stop gate explicitly preserves this. |
| 4 | Rename succeeds but rollback fails (corrupt backup) | Step C verifies backup sha256 before proceeding. Step E rollback re-verifies. Backup hash mismatch refuses without continuing. |
| 5 | Approval-token reuse across operations | Step (xii) checks the token against the *latest* awaiting-approval packet for *this* operation id. Double-apply attempts refuse on Step (xi) (destination_already_exists). |
| 6 | Held-out data leaks via the new runner | The runner does not reference held-out types. 183 adds `ArtifactRenameApplyRunner` to `HeldOutEvalStoreVisibilityTests.forbiddenTypes`. |
| 7 | Future UI inadvertently calls `Apply` | 184 is read-only by stop gate: no Button / ToggleSwitch / CheckBox / MenuItem with a click-handler that calls `Apply`. 186 adds a static mutation allow-list test that catches any non-allowed caller. |
| 8 | Pet game FPS regression because the runner spawns I/O | The runner is invoked only on explicit user / Codex / CLI command. No scheduler invokes it in this batch. |
| 9 | Codex auto-merges an apply-runner PR | Every phase in this batch is Auto-continue=No. Codex's history convention treats Auto-continue=No as halt-for-review. |
| 10 | Status report claims `apply_runner_implemented=false` after the rename runner exists | C-PHASE 181 derives that field from `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`. The supervised loop is still not implemented. The narrow rename runner is separate. Honesty preserved. A later phase (after the user signs off) can extend the status report to surface `narrow_artifact_rename_runner_implemented=true`. |
| 11 | Path traversal via Unicode normalization | Refuse on any non-ASCII character in the relative path (regex enforces `[a-zA-Z0-9._-]`). |
| 12 | Symlink attacks on the source / destination paths | `File.GetAttributes` check for `FileAttributes.ReparsePoint` on source. If set, refuse. (Codex must add this to step (x) of 183.) |

## 6. Milestone Narrative

After C-PHASE 183 lands, Wevito for the first time can mutate one specific class of file on disk — but only:
- under the artifact root,
- only `.draft.json` → `.approved.json`,
- only with all 7 default-off flags explicitly True,
- only after the 10-prerequisite check passes,
- only with a matching approval token,
- only with full evidence trail (dry-run, backup, applied, post-proof, completed).
- KillSwitch halts at every step.
- Rollback runs automatically on every mid-flight failure.

Even with all of that, the supervised improvement loop continues to refuse with `apply_runner_not_implemented_in_v0`. The narrow rename runner is a separate path, not a delegate of the supervised loop. This separation lets us validate the apply pipeline shape end-to-end without granting Wevito authority over real product source.

Phases 184–188 then add: a review UI for inspecting what the runner did, an explicit rollback runner (the user-initiated mirror of auto-rollback), a static + runtime mutation sandbox, an opt-in extension to artifact-only `.txt`/`.md`/`.svg` files, and a product-truth retest pinning the resulting state. None of those phases extend mutation authority to source code, sprites, or model weights.

## 7. Reading Order

For C-PHASE 183:
1. `docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md` (this doc) §1 + §4.1
2. `docs/CLAUDE_C_PHASE183_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md` §0 + §1
3. `docs/CODEX_PROMPT_C_PHASE_183_2026-05-19.md` (the standalone starter)
4. `docs/C_PHASE182_FUTURE_APPLY_RUNNER_DESIGN_DRAFT_2026-05-19.md`
5. `docs/C_PHASE174_APPLY_RUNNER_PREREQUISITE_CHECK_2026-05-19.md`
6. `docs/C_PHASE181_APPLY_RUNNER_STATUS_REPORT_2026-05-19.md`

For each follow-on phase: this plan §4.x + the corresponding prompts §x in `docs/CLAUDE_C_PHASE183_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md`.

## 8. Operational Discipline

- Each phase opens a separate PR. Codex never opens a PR that combines two phases.
- Each phase appends one row to `docs/codex-phase-history.jsonl` with `status="merged_pending_user_review"` at PR-open time and is updated to `status="merged"` after merge.
- Codex never auto-merges any phase in this batch. Every phase is Auto-continue=No.
- Codex never flips a capability flag default from False to True. Every flag introduced by this batch defaults False at code time. Runtime flips happen only at the user's keyboard.
- If a phase's pre-flight gate fails (build / test / vnext build / git diff --check), Codex halts with a Stop card. Codex does not patch around pre-flight failures.

## 9. Reviewer Notes

- Verify the 7 apply-v0 packet kinds appear in `PlainLanguageExplainer.KnownPacketKinds` with English sentences — not placeholders.
- Verify the 7 capability flags default `bool.FalseString` — exact constant, not a string literal.
- Verify the runner refuses on every Refused path with NO packet written (audit ledger row count before/after assertions in the tests).
- Verify the happy-path test asserts the destination file exists, the source file does NOT exist, and post sha256 equals pre sha256.
- Verify the rollback tests restore the original draft content byte-for-byte (sha256 comparison, not just file existence).
- Verify the static text-scan tests catch `UPDATE`, `DELETE FROM`, `DROP TABLE`, `MyDocuments`, `Desktop`, `System`, `Path.GetTempPath()`, `AppContext.BaseDirectory` substrings absent from the runner.
- Verify the constructor exposes the artifact root explicitly — no implicit resolution inside the runner.
- Verify `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` is unchanged.

## 10. Sources Cited

- `docs/CLAUDE_MASTER_PLAN_2026-05-15.md` — hard invariants (§4)
- `docs/DECISION_LEDGER_2026-05-15.md`
- `docs/C_PHASE182_FUTURE_APPLY_RUNNER_DESIGN_DRAFT_2026-05-19.md` — the design draft this batch implements
- `docs/C_PHASE174_APPLY_RUNNER_PREREQUISITE_CHECK_2026-05-19.md` — the 10-prereq check the runner re-runs
- `docs/C_PHASE181_APPLY_RUNNER_STATUS_REPORT_2026-05-19.md` — the status report contract (preserved unchanged in 183)
- `vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs` — the constant preserved unchanged in 183
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs` — the watchdog whose rules govern why the new packets use the `_v0_` namespace
