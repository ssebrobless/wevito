# Codex-Medium Prompt Pack — C-PHASE 183 + Follow-on Roadmap (184–188)

Date: 2026-05-19
Author: Claude (Opus 4.7), commissioned by `ssebrobles@gmail.com`
Audience: Codex CLI (executor, medium reasoning effort)
Pairs with: `docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md`

Each prompt below is self-contained and copy-paste-ready. §1 (C-PHASE 183) is approved for implementation. §2–§6 (C-PHASE 184–188) are **DESIGN DRAFT — NOT YET APPROVED FOR IMPLEMENTATION**. Do NOT paste any of §2–§6 into Codex until the user explicitly signs off in writing.

---

## §0. Common Header (read once per phase)

Every phase below assumes:

- Working repo: `C:\Users\fishe\Documents\projects\wevito`.
- Latest merged commit on `origin/main` at planning time: `9e91e8cd5bfc3deb30b3d82d7d4b80ea59f2596c` (C-PHASE 182, doc-only).
- Codex CLI reasoning effort: medium.
- Every phase Auto-continue: No.
- Pre-flight gate (run after `git pull origin main` and `git checkout -b <branch>`, before any code change):
  ```
  git status
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check
  ```
  If any fail, HALT and open a Stop card.
- Post-implementation validation:
  ```
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "<phase-filter>"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check
  ```
- Hard invariants (apply to every phase; see plan §1 for the full list).
- After PR open, append a row to `docs/codex-phase-history.jsonl` with `phase_id`, UTC timestamp, branch, and PR URL.

---

## §1. C-PHASE 183 — Smallest safe apply-runner prototype (ArtifactRenameApplyRunner)

**Status: Approved for implementation.**

This prompt is the same as the standalone starter at `docs/CODEX_PROMPT_C_PHASE_183_2026-05-19.md`. Paste either copy into Codex.

```text
[See docs/CODEX_PROMPT_C_PHASE_183_2026-05-19.md for the full self-contained prompt.]
```

Filter for validation: `ArtifactRenameApplyRunner|ApplyRunnerPrerequisite|HeldOutEvalStoreVisibility|InDistributionVersusHeldOutTypeSeparation|PlainLanguage|SelfImprovement|PostC174SelfImprovementBaseline`.

Branch: `claude-implementation/c-phase-183-artifact-rename-apply-runner`.

---

## §2. C-PHASE 184 — Apply-runner review UI (read-only)

**DO NOT IMPLEMENT YET — NOT YET APPROVED FOR IMPLEMENTATION. AWAIT USER SIGNOFF BEFORE PASTING TO CODEX.**

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo: C:\Users\fishe\Documents\projects\wevito
Predecessor phase that must be merged before this one begins: C-PHASE 183.
This phase is C-PHASE 184 — Apply-runner review UI (read-only).

Goal: Add a read-only Tool Popup expander labeled "Apply-runner activity (read-only)"
that displays the most recent apply-v0 sequences from the audit ledger. The expander
displays packet kind, operation id, scope id, scope hash, pre/post sha256, source path,
destination path, success/refuse/rollback disposition, and the flag-checklist that was
in effect. The expander has NO Button, NO ToggleSwitch, NO CheckBox, NO MenuItem with a
click-handler that mutates state. It does NOT call ArtifactRenameApplyRunner.Apply. It
does NOT modify any capability flag. It does NOT call EmitReport.

Hard invariants (preserve all from the master plan):
  - KillSwitch halts the new activity service.
  - The activity service writes no packets (read-only).
  - The activity service references no held-out / in-distribution eval types.
  - No network. No model. No hosted AI. No mutation.
  - Auto-continue: No.

Read FIRST:
  1. docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md (§1 + §4.2)
  2. docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
  3. vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs
  4. vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerStatusReportService.cs   (read-only pattern reference)
  5. vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyPrerequisiteExplainerService.cs (read-only pattern reference)
  6. vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml(.cs)
  7. vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs

Sync + branch:
  git checkout main && git pull origin main
  git checkout -b claude-implementation/c-phase-184-apply-runner-review-ui

Pre-flight gate: see §0.

Work:

1. Add vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityEntry.cs:
   public sealed record ApplyRunnerActivityEntry(
     string OperationId, string ScopeId, string ScopeHash,
     IReadOnlyList<ApplyRunnerActivityPacket> Packets,
     ApplyRunnerActivityDisposition Disposition);
   public sealed record ApplyRunnerActivityPacket(
     string PacketKind, DateTimeOffset Timestamp, IReadOnlyDictionary<string,string> Summary,
     bool DidMutate);
   public enum ApplyRunnerActivityDisposition { Succeeded, RolledBack, InProgress }

2. Add vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityService.cs:
   public sealed class ApplyRunnerActivityService {
     public ApplyRunnerActivityService(string databasePath, KillSwitchService killSwitch);
     public IReadOnlyList<ApplyRunnerActivityEntry> ReadRecent(int maxEntries);
   }
   The service opens the SQLite ledger in ReadOnly mode (Microsoft.Data.Sqlite Mode=ReadOnly),
   queries rows whose PacketKind starts with "self_improvement_apply_v0_", groups by
   OperationId, orders by Timestamp ascending, and returns up to maxEntries groups (most
   recent operations first). If KillSwitch is active, the service returns an empty list.

3. Add factory in ShellCompositionRoot.cs:
   public static ApplyRunnerActivityService CreateApplyRunnerActivityService(
     AuditLedgerService ledger, KillSwitchService? killSwitch = null)
   { var ks = killSwitch ?? new KillSwitchService(() => null);
     return new ApplyRunnerActivityService(ledger.DatabasePath, ks); }

4. Extend ToolPopupWindow.xaml with one read-only Expander
   "Apply-runner activity (read-only)" that binds to a vm collection populated by
   CreateApplyRunnerActivityService(...).ReadRecent(20). No Button. No ToggleSwitch.
   No CheckBox. No MenuItem.Click handler.

5. Add vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityServiceTests.cs with
   [Fact] tests covering:
     - Returns empty list when ledger has no apply-v0 rows.
     - Returns one group per OperationId containing all related packets in timestamp order.
     - Marks Disposition=Succeeded when group ends with apply_v0_completed.
     - Marks Disposition=RolledBack when group ends with apply_v0_rolled_back.
     - Marks Disposition=InProgress when group ends with a non-terminal packet.
     - KillSwitch active returns empty list.
     - Service source contains no INSERT / UPDATE / DELETE SQL (text scan).
     - Service uses Mode=ReadOnly in its connection string (text scan).

6. Add vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityHeldOutAccessTests.cs.

7. Extend HeldOutEvalStoreVisibilityTests.cs forbiddenTypes with
   typeof(ApplyRunnerActivityService), typeof(ApplyRunnerActivityEntry),
   typeof(ApplyRunnerActivityPacket). Document the exception in the established
   comment style.

8. Add docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md and append one row to
   docs/codex-phase-history.jsonl.

Allowed file changes:
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityEntry.cs       (new)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityService.cs     (new)
  - vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs                                  (extend)
  - vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml                                    (extend)
  - vnext/src/Wevito.VNext.Shell/ToolPopupWindow.xaml.cs                                 (extend)
  - vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs                                     (extend if required for vm wiring)
  - vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityServiceTests.cs                    (new)
  - vnext/tests/Wevito.VNext.Tests/ApplyRunnerActivityHeldOutAccessTests.cs              (new)
  - vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs                    (extend)
  - docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md                                 (new)
  - docs/codex-phase-history.jsonl                                                       (one append)

NO other files may change. NO new capability flag. NO new packet kind. NO change to
ArtifactRenameApplyRunner.

Stop gates (HALT if any TRUE):
  - The expander contains a Button / ToggleSwitch / CheckBox / MenuItem with a
    click-handler that calls a non-Read* method.
  - The activity service writes any packet, opens a non-ReadOnly SQLite connection,
    or contains INSERT/UPDATE/DELETE SQL.
  - The activity service references IHeldOutEvalStore / HeldOutEvalCase /
    IInDistributionEvalStore / InDistributionEvalCase.
  - A new capability flag or packet kind is added.
  - The expander invokes Apply on the rename runner.
  - Pre-flight gate fails.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ApplyRunnerActivity|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

PR + commit:
  Branch: claude-implementation/c-phase-184-apply-runner-review-ui
  Commit: "C-PHASE 184: apply-runner review UI (read-only Tool Popup expander; no mutation surfaces)"
  PR title: "C-PHASE 184: Apply-runner review UI (read-only)"
  Auto-continue: NO.

Begin.
```

---

## §3. C-PHASE 185 — Rollback evidence viewer + symmetric narrow rollback runner

**DO NOT IMPLEMENT YET — NOT YET APPROVED FOR IMPLEMENTATION. AWAIT USER SIGNOFF BEFORE PASTING TO CODEX.**

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo: C:\Users\fishe\Documents\projects\wevito
Predecessor phases that must be merged before this one begins: C-PHASE 183 and C-PHASE 184.
This phase is C-PHASE 185 — Rollback evidence viewer + symmetric narrow rollback runner.

Goal: (a) Introduce ArtifactRenameRollbackRunner — the symmetric mirror of
ArtifactRenameApplyRunner. It renames <x>.approved.json back to <x>.draft.json under
the same path constraints (artifact-root, operation-id/scope-id segments, regex-
validated relative path, approval-token verification). All flags default OFF. The
runner refuses unless every required flag is True. (b) Add a read-only "Rollback
evidence" Tool Popup expander surfacing prior apply_v0_rolled_back packets plus any
new explicit-rollback packets emitted by this runner.

Hard invariants (preserve all from master plan):
  - KillSwitch halts the new runner and the new expander.
  - The rollback runner writes only inside the artifact root. Same regex enforcement
    as 183 but inverted extension pair: source ends with .approved.json, destination
    ends with .draft.json. Destination must not already exist.
  - The new expander has no Button / ToggleSwitch / CheckBox / MenuItem with a
    click-handler that mutates state. It does NOT invoke ExplicitRollback().
  - No network. No model. No hosted AI. No held-out content. No source/sprite mutation.
  - Auto-continue: No.

Read FIRST:
  1. docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md (§1 + §4.3)
  2. docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
  3. docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md
  4. vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs
  5. vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRunnerActivityService.cs

Sync + branch:
  git checkout main && git pull origin main
  git checkout -b claude-implementation/c-phase-185-artifact-rename-rollback-runner

Pre-flight gate: see §0.

Work:

1. Add 3 new packet kinds to SelfImprovementPacketKinds.cs (all gated by default-off
   flags; register all 3 in PlainLanguageExplainer.KnownPacketKinds with English
   sentences):
     - ApplyV0ExplicitRollbackStarted   = "self_improvement_apply_v0_explicit_rollback_started"
     - ApplyV0ExplicitRollbackCompleted = "self_improvement_apply_v0_explicit_rollback_completed"
     - ApplyV0ExplicitRollbackRefused   = "self_improvement_apply_v0_explicit_rollback_refused"

2. Add 2 new capability flags to CapabilityFlagInventory.cs (default
   bool.FalseString):
     - apply_runner_v0_explicit_rollback_enabled
     - apply_runner_v0_explicit_rollback_design_approved

3. Add vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/RollbackRequest.cs
   (immutable record analogous to ApplyRequest with ApprovedRelativePath in place
   of DraftRelativePath; regex enforces *.approved.json suffix).

4. Add vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/RollbackResult.cs (closed
   discriminated union: Refused / Succeeded).

5. Add vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs.
   The runner refuses unless:
     - KillSwitch inactive.
     - All 2 new flags True, AND apply_runner_design_approved=True, AND
       apply_runner_implementation_phase_approved=True (it reuses the 183 design
       approval gates).
     - ApplyRunnerPrerequisiteCheckService returns no failed entries.
     - Path is artifact-root-confined.
     - Source path ends with .approved.json.
     - Destination path ends with .draft.json.
     - Destination does not already exist.
     - Approval token + scope hash match latest awaiting-approval packet.
   On accept, emit ApplyV0ExplicitRollbackStarted, write a rollback-dry-run.json,
   compute pre-sha256, atomically rename approved → draft, verify post sha256 of
   the restored draft equals the pre-sha256, then emit
   ApplyV0ExplicitRollbackCompleted. On any failure mid-flight, emit
   ApplyV0ExplicitRollbackRefused with reason.
   No auto-restore loop; the rollback runner has nothing to restore (it IS the
   rollback). It must not re-call ArtifactRenameApplyRunner.Apply.

6. Add factory in ShellCompositionRoot.cs.

7. Add vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs with
   tests symmetric to 183's test suite (flag-off refusals; KillSwitch refusal;
   path-traversal refusals; happy-path packet sequence
   [explicit_rollback_started, explicit_rollback_completed]; double-rollback
   refuses on destination_already_exists; approval-token mismatch refuses;
   no UPDATE/DELETE SQL; only writes under artifact root).

8. Add vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerHeldOutAccessTests.cs.

9. Extend ToolPopupWindow.xaml(.cs) with one read-only Expander "Rollback
   evidence (read-only)" bound to ApplyRunnerActivityService.ReadRecent(20) filtered
   to entries whose Disposition is RolledBack OR whose packet sequence contains an
   ApplyV0ExplicitRollback* packet kind.

10. Extend HeldOutEvalStoreVisibilityTests forbiddenTypes with all new types.

11. Extend KillSwitchCoverageAllowList with ArtifactRenameRollbackRunner.

12. Add docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md.

Stop gates (HALT if any TRUE):
  - The rollback runner accepts a non-`.approved.json` source extension.
  - The rollback runner produces a non-`.draft.json` destination extension.
  - The rollback runner writes any file outside the artifact root.
  - The rollback runner calls ArtifactRenameApplyRunner.Apply.
  - Any new capability flag defaults True.
  - Any new packet kind missing from KnownPacketKinds.
  - The new expander contains a click-handler that calls ExplicitRollback.
  - Pre-flight gate fails.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameRollback|ArtifactRenameApply|ApplyRunnerActivity|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

PR + commit:
  Branch: claude-implementation/c-phase-185-artifact-rename-rollback-runner
  Commit: "C-PHASE 185: symmetric narrow rollback runner + rollback evidence expander (default off; .approved.json → .draft.json only)"
  PR title: "C-PHASE 185: Rollback evidence viewer + symmetric rollback runner"
  Auto-continue: NO.

Begin.
```

---

## §4. C-PHASE 186 — Mutation sandbox hardening

**DO NOT IMPLEMENT YET — NOT YET APPROVED FOR IMPLEMENTATION. AWAIT USER SIGNOFF BEFORE PASTING TO CODEX.**

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo: C:\Users\fishe\Documents\projects\wevito
Predecessor phases that must be merged before this one begins: C-PHASE 183, 184, 185.
This phase is C-PHASE 186 — Mutation sandbox hardening (static allow-list + runtime guard).

Goal: Two complementary defenses that catch any service that mutates outside its
declared scope.

(a) Static defense: a new test class MutationScopeAuditTests.cs in
vnext/tests/Wevito.VNext.Tests that uses reflection over every type in
Wevito.VNext.Core to discover any caller of File.Move, File.Copy, File.Delete,
File.WriteAllText, File.WriteAllBytes, File.Open(...FileMode.Create*...),
File.OpenWrite, FileStream(..., FileMode.Create*|Truncate, ...),
Directory.CreateDirectory, Directory.Delete, Directory.Move. Each discovered
type must be in an explicit MutationAllowList. The allow-list at land time
contains exactly:
  - ArtifactRenameApplyRunner
  - ArtifactRenameRollbackRunner
  - AuditLedgerService                         (append-only ledger writer)
  - SnapshotExportService                      (existing artifact-only writer)
  - ReplayResultStore                          (existing artifact-only writer)
  - HeldOutEvalSeedTool                        (existing tools-project writer)
  - InDistributionEvalSeedTool                 (existing tools-project writer)
  - SnapshotVerifyTool                         (existing tools-project writer)
Any other type discovered fails the test.

(b) Runtime defense: a new static helper
vnext/src/Wevito.VNext.Core/SelfImprovement/Sandbox/MutationScopeGuard.cs with
method:
  public static void ThrowIfOutsideScope(string intendedPath, string scopeRoot, string scopeName);
The guard resolves intendedPath via Path.GetFullPath, compares case-insensitively
to scopeRoot + DirectorySeparatorChar, and throws InvalidOperationException with
a message containing scopeName and intendedPath if the prefix does not match.
ArtifactRenameApplyRunner and ArtifactRenameRollbackRunner must be updated to
call MutationScopeGuard.ThrowIfOutsideScope on every File.Move / File.Copy
target path. No try/catch that hides the exception.

(c) Audit packet kind (default-off-gated):
self_improvement_mutation_scope_audit_event. The guard does NOT emit packets by
default. A future opt-in audit can emit them when flag
mutation_scope_audit_emit_enabled is True. Both the flag and the packet kind are
registered now to keep the surface honest.

Hard invariants (preserve all from master plan):
  - The runtime guard throws synchronously. No swallow.
  - The static test uses reflection over IL, not hardcoded type lists.
  - The allow-list lives in a single constant in the test class.
  - No network. No model. No hosted AI.
  - Auto-continue: No.

Read FIRST:
  1. docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md (§1 + §4.4)
  2. docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
  3. docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md
  4. every type currently in vnext/src/Wevito.VNext.Core that touches File.* or Directory.*

Sync + branch:
  git checkout main && git pull origin main
  git checkout -b claude-implementation/c-phase-186-mutation-sandbox-hardening

Pre-flight gate: see §0.

Work:

1. Add 1 new constant to SelfImprovementPacketKinds.cs:
     MutationScopeAuditEvent = "self_improvement_mutation_scope_audit_event"
   Register in PlainLanguageExplainer.KnownPacketKinds + Explain switch with
   an English sentence ("Wevito recorded a mutation-scope audit event for a
   file write under a declared scope.").

2. Add 1 new capability flag to CapabilityFlagInventory.cs (default
   bool.FalseString):
     mutation_scope_audit_emit_enabled

3. Add vnext/src/Wevito.VNext.Core/SelfImprovement/Sandbox/MutationScopeGuard.cs
   (static helper).

4. Update ArtifactRenameApplyRunner.cs and ArtifactRenameRollbackRunner.cs to
   call MutationScopeGuard.ThrowIfOutsideScope on every File.Move / File.Copy
   target path (source AND destination), with scopeName="artifact-root" and
   scopeRoot=artifactRoot.

5. Add vnext/tests/Wevito.VNext.Tests/MutationScopeGuardTests.cs ([Fact] tests:
   pass on legal path; throw on absolute path outside scope; throw on .. relative
   path that escapes scope; throw on alternate-case scope prefix only if Windows;
   throw on null/empty input).

6. Add vnext/tests/Wevito.VNext.Tests/MutationScopeAuditTests.cs:
   Use reflection over Wevito.VNext.Core's assembly to walk every MethodInfo
   in every TypeInfo and inspect its IL body (via System.Reflection.MethodInfo
   GetMethodBody()) for calls to the listed File.* / Directory.* methods.
   Assert each containing type is in the MutationAllowList constant.

7. Add vnext/tests/Wevito.VNext.Tests/Support/MutationAllowList.cs containing
   only the explicit 8-type list above.

8. Extend HeldOutEvalStoreVisibilityTests forbiddenTypes with
   typeof(MutationScopeGuard) (defensive; guard is invariant infrastructure).

9. Add docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md.

Allowed file changes:
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Sandbox/MutationScopeGuard.cs            (new)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs       (extend; call guard)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs    (extend; call guard)
  - vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs                       (extend; 1 new constant)
  - vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs                          (extend; 1 new flag)
  - vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs                                 (extend; 1 new entry)
  - vnext/tests/Wevito.VNext.Tests/MutationScopeGuardTests.cs                             (new)
  - vnext/tests/Wevito.VNext.Tests/MutationScopeAuditTests.cs                             (new)
  - vnext/tests/Wevito.VNext.Tests/Support/MutationAllowList.cs                           (new)
  - vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs                     (extend)
  - docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md                              (new)
  - docs/codex-phase-history.jsonl                                                        (one append)

NO other production type may be added to the MutationAllowList.

Stop gates (HALT if any TRUE):
  - MutationAllowList contains any type beyond the listed 8.
  - The runtime guard contains try/catch that hides scope violations.
  - The static test hardcodes the list of mutating callers (must be reflection-derived).
  - The new packet kind defaults emitted=true.
  - The new capability flag defaults True.
  - Any reflection scan misses a known mutator (e.g., AuditLedgerService).
  - Pre-flight gate fails.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "MutationScope|ArtifactRename|HeldOutEvalStoreVisibility|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

PR + commit:
  Branch: claude-implementation/c-phase-186-mutation-sandbox-hardening
  Commit: "C-PHASE 186: mutation sandbox hardening (static allow-list + runtime scope guard; default off)"
  PR title: "C-PHASE 186: Mutation sandbox hardening"
  Auto-continue: NO.

Begin.
```

---

## §5. C-PHASE 187 — Expanded but artifact-only rename scope

**DO NOT IMPLEMENT YET — NOT YET APPROVED FOR IMPLEMENTATION. AWAIT USER SIGNOFF BEFORE PASTING TO CODEX.**

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo: C:\Users\fishe\Documents\projects\wevito
Predecessor phases that must be merged before this one begins: C-PHASE 183, 184, 185, 186.
This phase is C-PHASE 187 — Expanded but artifact-only rename scope (.draft.txt/.md/.svg).

Goal: Extend ArtifactRenameApplyRunner (and ArtifactRenameRollbackRunner) to accept
additional artifact-only file extensions, all still under
<artifact-root>\<operation-id>\<scope-id>\. The allowed extension pairs are:
  - *.draft.json → *.approved.json (already accepted in 183)
  - *.draft.txt  → *.approved.txt  (new)
  - *.draft.md   → *.approved.md   (new)
  - *.draft.svg  → *.approved.svg  (new)

No new packet kinds. No change to the 7 existing apply-v0 packet kinds. One new
default-off opt-in flag:
  apply_runner_v0_extended_artifact_types_enabled (default False).

The runner refuses non-json extensions unless this flag is True. Even when the flag
is True, the runner refuses paths outside artifact root, mismatched approval
tokens, missing prerequisites, KillSwitch active, source missing, destination
existing, etc. Code-mutation extensions (.cs, .csproj, .xaml, .config, .exe, .dll,
.png, .jpg, .jpeg, .gif, .ico, .wav, .ogg, .ttf, .otf, .yaml, .yml, .toml, .ini,
.bat, .ps1, .sh, .py, .js, .ts, .java, .kt, .swift, .rs, .go) MUST refuse even
when the flag is True.

Hard invariants (preserve all from master plan):
  - The runner remains under MutationAllowList from C-PHASE 186; no new mutator
    is introduced.
  - No new packet kind. No new audit surface beyond extended-extension handling.
  - KillSwitch halts the runner unchanged.
  - The runner runtime MutationScopeGuard call is unchanged.
  - Auto-continue: No.

Read FIRST:
  1. docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md (§1 + §4.5)
  2. docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
  3. docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md
  4. docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md
  5. vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs
  6. vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs

Sync + branch:
  git checkout main && git pull origin main
  git checkout -b claude-implementation/c-phase-187-extended-artifact-types

Pre-flight gate: see §0.

Work:

1. Add 1 new capability flag to CapabilityFlagInventory.cs (default
   bool.FalseString):
     apply_runner_v0_extended_artifact_types_enabled

2. Update both runners' relative-path regex to
     ^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+/[a-zA-Z0-9._-]+\.draft\.(json|txt|md|svg)$
   for the apply runner; corresponding `.approved.(json|txt|md|svg)` for rollback.

3. Update both runners' refuse logic to gate the non-json extensions behind
   apply_runner_v0_extended_artifact_types_enabled=True. Json extensions remain
   accepted under the 183-era 7-flag check, no change.

4. Add explicit refuse paths for the forbidden extension list above. Implement
   as a constant readonly array `ForbiddenExtensions` checked before any other
   extension logic. Refuse reason: "forbidden_extension:<ext>".

5. Extend ArtifactRenameApplyRunnerTests.cs and ArtifactRenameRollbackRunnerTests.cs
   with:
     - happy-path test per non-json extension (txt/md/svg), gated by the new flag
     - refuse test when flag is False and a non-json extension is requested
     - refuse test per forbidden extension above (assert all 30+ refusals)
     - regex-mismatch test for unsupported extensions (e.g. .draft.html)

6. Add docs/C_PHASE187_EXTENDED_ARTIFACT_TYPES_2026-05-19.md.

Allowed file changes:
  - vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs                          (extend; 1 new flag)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs        (extend; regex + extension gate)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameRollbackRunner.cs     (extend; regex + extension gate)
  - vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs                      (extend; non-json + forbidden tests)
  - vnext/tests/Wevito.VNext.Tests/ArtifactRenameRollbackRunnerTests.cs                   (extend; non-json + forbidden tests)
  - docs/C_PHASE187_EXTENDED_ARTIFACT_TYPES_2026-05-19.md                                 (new)
  - docs/codex-phase-history.jsonl                                                        (one append)

NO new packet kind. NO new runner. NO new UI surface. NO change to MutationScopeGuard
or MutationAllowList.

Stop gates (HALT if any TRUE):
  - The runner accepts any extension outside the {json, txt, md, svg} tuple.
  - The runner accepts a non-json extension when the new flag is False.
  - The new flag defaults True.
  - The forbidden extension list is incomplete (must cover at least the 30 listed
    in the goal).
  - The runner gains a new MutationAllowList entry (it does not — it remains the
    same type).
  - Pre-flight gate fails.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameApply|ArtifactRenameRollback|MutationScope|PlainLanguage|SelfImprovement"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

PR + commit:
  Branch: claude-implementation/c-phase-187-extended-artifact-types
  Commit: "C-PHASE 187: extended artifact-only rename scope (.txt/.md/.svg via opt-in flag; forbidden code extensions enumerated)"
  PR title: "C-PHASE 187: Extended artifact-only rename scope"
  Auto-continue: NO.

Begin.
```

---

## §6. C-PHASE 188 — Product-truth retest after first apply-runner prototype

**DO NOT IMPLEMENT YET — NOT YET APPROVED FOR IMPLEMENTATION. AWAIT USER SIGNOFF BEFORE PASTING TO CODEX.**

```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo: C:\Users\fishe\Documents\projects\wevito
Predecessor phases that must be merged before this one begins: C-PHASE 183
(required). C-PHASE 184, 185, 186, 187 are conditional — the tests below adapt
to whichever predecessors have landed by gating each claim on the presence of
the relevant type via reflection. If only 183 has landed, only the 183 claims
are asserted.
This phase is C-PHASE 188 — Post-C-PHASE-183 apply-runner product-truth retest
(test-only).

Goal: Pin the post-183 self-improvement surface at HEAD with deterministic
[Fact] tests that cannot drift. NO production source file changes. NO new
capability flag. NO new audit packet kind. NO new runner. NO new UI surface.
NO model call. NO network call. NO held-out access at runtime.

Hard invariants (preserve all from master plan): see §0 + master plan §1.

Read FIRST:
  1. docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md (§1 + §4.6)
  2. docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
  3. docs/C_PHASE184_APPLY_RUNNER_REVIEW_UI_2026-05-19.md   (if landed)
  4. docs/C_PHASE185_ARTIFACT_RENAME_ROLLBACK_RUNNER_2026-05-19.md (if landed)
  5. docs/C_PHASE186_MUTATION_SANDBOX_HARDENING_2026-05-19.md (if landed)
  6. docs/C_PHASE187_EXTENDED_ARTIFACT_TYPES_2026-05-19.md (if landed)
  7. docs/C_PHASE175_POST_174_SELF_IMPROVEMENT_BASELINE_2026-05-19.md (test pattern reference)

Sync + branch:
  git checkout main && git pull origin main
  git checkout -b claude-implementation/c-phase-188-post-c183-apply-runner-product-truth

Pre-flight gate: see §0.

Work:

1. Add vnext/tests/Wevito.VNext.Tests/PostC183ApplyRunnerProductTruthTests.cs.
   The class must NOT import System.Net.Http / System.Net.Sockets. The class
   must NOT reference IHeldOutEvalStore / HeldOutEvalCase /
   IInDistributionEvalStore / InDistributionEvalCase.

   Required [Fact] tests (each named ProductTruth_<short_claim_id>):

   A. 183-only claims (always asserted):
     a. ProductTruth_artifact_rename_apply_runner_type_exists_in_apply_namespace
        Assert typeof(ArtifactRenameApplyRunner).Namespace ==
        "Wevito.VNext.Core.SelfImprovement.Apply"
     b. ProductTruth_seven_apply_v0_packet_kinds_are_in_known_packet_kinds
        Foreach kind in [ApplyV0DryRunStarted, ApplyV0DryRunCompleted,
        ApplyV0BackupWritten, ApplyV0Applied, ApplyV0PostProofCompleted,
        ApplyV0RolledBack, ApplyV0Completed]:
          Assert.Contains(kind, PlainLanguageExplainer.KnownPacketKinds)
     c. ProductTruth_seven_apply_v0_capability_flags_default_false
        Foreach flag in [apply_runner_design_approved,
        apply_runner_implementation_phase_approved, apply_runner_v0_enabled,
        apply_runner_v0_dry_run_required, apply_runner_v0_backup_required,
        apply_runner_v0_post_proof_required, apply_runner_v0_rollback_required]:
          var entry = CapabilityFlagInventory.Entries.Single(e => e.Name == flag);
          Assert.Equal(bool.FalseString, entry.DefaultValue);
     d. ProductTruth_supervised_loop_apply_runner_not_implemented_reason_unchanged
        Assert.Equal("apply_runner_not_implemented_in_v0",
                     SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
     e. ProductTruth_legacy_apply_completed_constant_unchanged
        Assert.Equal("self_improvement_apply_completed",
                     SelfImprovementPacketKinds.ApplyCompleted);
     f. ProductTruth_runner_type_appears_in_held_out_eval_store_visibility_forbidden_types
        Read HeldOutEvalStoreVisibilityTests.cs as text. Assert.Contains
        "ArtifactRenameApplyRunner" in the forbiddenTypes block.
     g. ProductTruth_runner_only_writes_under_artifact_root
        Read ArtifactRenameApplyRunner.cs as text. Assert.DoesNotContain
        "Environment.SpecialFolder.MyDocuments",
        "Environment.SpecialFolder.Desktop",
        "Environment.SpecialFolder.System",
        "AppContext.BaseDirectory",
        "Path.GetTempPath()" (constructor-injected only).

   B. 184 conditional claims (skip-when-type-not-found):
     a. ProductTruth_apply_runner_activity_service_type_exists
        Skip if typeof(ApplyRunnerActivityService) cannot be located; otherwise
        Assert it lives under SelfImprovement.Apply namespace.
     b. ProductTruth_apply_runner_activity_service_uses_readonly_sqlite
        Read service source as text; Assert.Contains "Mode=ReadOnly".
     c. ProductTruth_apply_runner_activity_service_no_insert_update_delete_sql
        Read source; Assert.DoesNotContain "INSERT", "UPDATE", "DELETE FROM",
        "DROP TABLE".

   C. 185 conditional claims:
     a. ProductTruth_artifact_rename_rollback_runner_exists_with_symmetric_path_constraints
     b. ProductTruth_three_explicit_rollback_packet_kinds_in_known_kinds
     c. ProductTruth_two_explicit_rollback_capability_flags_default_false

   D. 186 conditional claims:
     a. ProductTruth_mutation_scope_guard_throws_invalid_operation_on_out_of_scope_path
     b. ProductTruth_mutation_allow_list_contains_only_expected_eight_types
     c. ProductTruth_mutation_scope_audit_tests_pass_with_only_known_mutators

   E. 187 conditional claims:
     a. ProductTruth_extended_artifact_types_flag_default_false
     b. ProductTruth_runner_refuses_non_json_extension_when_flag_false
     c. ProductTruth_runner_accepts_txt_md_svg_when_flag_true
     d. ProductTruth_runner_refuses_each_forbidden_extension_unconditionally
        (Iterate through the 30+ forbidden extensions and assert each refuses.)

2. Add docs/C_PHASE188_POST_C183_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-19.md.

3. DO NOT modify any production source file.
4. DO NOT add a new capability flag.
5. DO NOT add a new audit packet kind.
6. DO NOT modify any runner, service, or UI surface.

Allowed file changes:
  - vnext/tests/Wevito.VNext.Tests/PostC183ApplyRunnerProductTruthTests.cs   (new)
  - docs/C_PHASE188_POST_C183_APPLY_RUNNER_PRODUCT_TRUTH_2026-05-19.md       (new)
  - docs/codex-phase-history.jsonl                                           (one append)

NO other files may change.

Stop gates (HALT if any TRUE):
  - A production source file changed.
  - A claim was masked by relaxing an assertion or skipping when the predecessor
    HAS landed (only skip when the type genuinely does not exist).
  - A new capability flag, packet kind, runner, or UI surface added.
  - The new test class imports System.Net.Http / System.Net.Sockets.
  - Pre-flight gate fails.

Validation:
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PostC183ApplyRunnerProductTruth|ArtifactRenameApply|ArtifactRenameRollback|MutationScope|PlainLanguage|SelfImprovement|PostC174SelfImprovementBaseline"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

PR + commit:
  Branch: claude-implementation/c-phase-188-post-c183-apply-runner-product-truth
  Commit: "C-PHASE 188: product-truth retest after first apply-runner prototype (test-only; no production changes)"
  PR title: "C-PHASE 188: Post-C-PHASE-183 apply-runner product-truth retest (test-only)"
  Auto-continue: NO.

Begin.
```

---

## Appendix A — Phase Reading List

| Phase | Plan section | Prompt section | Standalone starter file |
|---|---|---|---|
| 183 | §4.1 | §1 | `docs/CODEX_PROMPT_C_PHASE_183_2026-05-19.md` |
| 184 | §4.2 | §2 | — (not approved yet) |
| 185 | §4.3 | §3 | — (not approved yet) |
| 186 | §4.4 | §4 | — (not approved yet) |
| 187 | §4.5 | §5 | — (not approved yet) |
| 188 | §4.6 | §6 | — (not approved yet) |
