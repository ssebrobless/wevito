```text
You are Codex, executing the next Wevito phase at medium reasoning effort.

Working repo:
  C:\Users\fishe\Documents\projects\wevito

Latest merged commit on origin/main at planning time:
  9e91e8cd5bfc3deb30b3d82d7d4b80ea59f2596c — C-PHASE 182: future apply-runner prototype design draft (DOC ONLY).

Your next phase is C-PHASE 183 — Smallest safe apply-runner prototype:
ArtifactRenameApplyRunner. This is the first phase in the Wevito project history
that introduces a real mutation surface for the supervised self-improvement
pipeline. The scope is intentionally tiny. The runner is allowed to perform
exactly ONE class of filesystem operation: atomically rename a single
artifact-only JSON file under
  <artifact-root>\<scope>\<operation-id>\<name>.draft.json
to
  <artifact-root>\<scope>\<operation-id>\<name>.approved.json
where <artifact-root> equals the value resolved by
`ShellCompositionRoot` (`%LocalAppData%\WevitoVNext\artifacts`).

The runner must not touch any other file on disk for any reason. The runner
must not edit source code, runtime assets, sprites, content manifests, user
files, model weights, or any path outside the canonical artifact root. The
runner must not call hosted AI, fetch network resources, train, embed, or
mutate audit ledger rows. The runner appends evidence packets only.

The user has explicitly approved drafting this prompt. The user has NOT
flipped any of the capability flags introduced by this phase to True. Every
flag introduced by this phase defaults False. The runner refuses unless
every required flag is explicitly True at runtime. The user remains the
judge.

Authoritative plan + per-phase prompt set you must follow:
  docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md                 (master plan; §4.1 = this phase; §1 hard invariants; §7 reading order)
  docs/CLAUDE_C_PHASE183_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md (§0 common header + §1 = this phase)
  docs/C_PHASE182_FUTURE_APPLY_RUNNER_DESIGN_DRAFT_2026-05-19.md (predecessor design draft)
  docs/C_PHASE174_APPLY_RUNNER_PREREQUISITE_CHECK_2026-05-19.md  (prerequisite check service)
  docs/C_PHASE181_APPLY_RUNNER_STATUS_REPORT_2026-05-19.md       (status report contract; NOT modified by 183)

Hard invariants you must preserve at all times:
  - No hosted AI as Wevito's runtime brain.
  - No silent network access. No phase opens a socket in 183.
  - No silent training. No silent file / tool / code / asset mutation
    outside the declared artifact-rename scope.
  - Every mutation pathway in 183 requires: explicit operation id +
    approved scope hash + flag-checklist + prerequisite check pass +
    dry-run record + backup + sha256 evidence + apply + post-proof +
    rollback-on-failure + user-visible audit packets.
  - KillSwitchService.IsActive() halts every adapter, scheduler,
    autonomous loop, runner, scope, experiment, eval runner, approval
    handler, judge, replay harness, snapshot tool, replay runner,
    in-distribution seed tool, local scoring provider,
    capabilities-and-gates snapshot, apply-runner prerequisite check,
    apply-runner status report, scoring dry-run, local runtime probe,
    eval coverage health, proposal quality metrics, AND the new
    ArtifactRenameApplyRunner.
  - AuditLedgerService remains append-only. The new runner appends only.
  - Every new audit packet kind appears in
    `PlainLanguageExplainer.KnownPacketKinds` with a plain-language
    sentence.
  - Every evidence packet sets DidUseNetwork / DidUseHostedAi /
    DidUseLocalModel / DidMutate honestly. The four mutation-emitting
    packets (`backup_written`, `applied`, `rolled_back`, `completed`)
    set DidMutate=true. The two non-mutating packets
    (`dry_run_started`, `dry_run_completed`) set DidMutate=true only if
    the dry-run JSON file is written (default Yes, since the dry-run
    file lives under the operation artifact folder). `post_proof_completed`
    sets DidMutate=false (it only reads).
  - Every new capability flag defaults OFF. The runner refuses unless
    every required flag is True.
  - Held-out eval data must stay invisible. The new runner type and its
    helpers must be on the `HeldOutEvalStoreVisibilityTests.forbiddenTypes`
    list.
  - Pets remain visually normal pet-sim characters. Pet game FPS / user
    PC experience stays protected. The runner is not invoked from any
    pet-sim or sprite-workflow UI path in this phase.
  - The supervised improvement loop continues to refuse with
    `apply_runner_not_implemented_in_v0`. The constant
    `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` is NOT
    modified in 183. The narrow ArtifactRenameApplyRunner is a SEPARATE
    service; it is not wired into the supervised loop in this phase.
  - Codex never auto-merges Auto-continue=No phases. This phase is
    Auto-continue=No.

Read FIRST (do not skip):
  1. docs/CLAUDE_C_PHASE183_PLUS_PLAN_2026-05-19.md (§1 + §4.1)
  2. docs/CLAUDE_C_PHASE183_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md (§0 + §1)
  3. docs/C_PHASE182_FUTURE_APPLY_RUNNER_DESIGN_DRAFT_2026-05-19.md
  4. docs/C_PHASE174_APPLY_RUNNER_PREREQUISITE_CHECK_2026-05-19.md
  5. docs/C_PHASE181_APPLY_RUNNER_STATUS_REPORT_2026-05-19.md
  6. vnext/src/Wevito.VNext.Core/SelfImprovement/ApplyRunnerPrerequisiteCheckService.cs
  7. vnext/src/Wevito.VNext.Core/SelfImprovement/SupervisedImprovementLoop.cs
  8. vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs
  9. vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs
  10. vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs
  11. vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs
  12. vnext/src/Wevito.VNext.Core/SelfImprovement/Invariants/InvariantViolationWatchdog.cs
  13. vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs
  14. vnext/tests/Wevito.VNext.Tests/Support/KillSwitchCoverageAllowList.cs

Step 0 — Sync working tree to origin/main (REQUIRED first action):
  Set-Location 'C:\Users\fishe\Documents\projects\wevito'
  git status
  git checkout main
  git pull origin main
  git rev-parse HEAD   # must descend from 9e91e8cd5bfc3deb30b3d82d7d4b80ea59f2596c
  git checkout -b claude-implementation/c-phase-183-artifact-rename-apply-runner

Pre-flight gate (after sync, before any code change):
  - git status (clean)
  - dotnet build .\vnext\Wevito.VNext.sln
  - dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  - powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  - git diff --check
  If any fail, HALT and open a Stop card describing the failure.

C-PHASE 183 work:

1. Add 7 new constants to
   `vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs`,
   each prefixed `self_improvement_apply_v0_` to keep the narrow rename
   runner's packets segregated from the existing `ApplyCompleted`
   constant and its watchdog rules:
     - `ApplyV0DryRunStarted        = "self_improvement_apply_v0_dry_run_started"`
     - `ApplyV0DryRunCompleted      = "self_improvement_apply_v0_dry_run_completed"`
     - `ApplyV0BackupWritten        = "self_improvement_apply_v0_backup_written"`
     - `ApplyV0Applied              = "self_improvement_apply_v0_applied"`
     - `ApplyV0PostProofCompleted   = "self_improvement_apply_v0_post_proof_completed"`
     - `ApplyV0RolledBack           = "self_improvement_apply_v0_rolled_back"`
     - `ApplyV0Completed            = "self_improvement_apply_v0_completed"`
   Do NOT touch the existing `ApplyCompleted` constant or its watchdog.

2. Register all 7 in `PlainLanguageExplainer.KnownPacketKinds` and add
   a one-sentence explainer for each in `Explain(...)`:
     - dry_run_started        → "Wevito wrote a v0 rename dry-run record before any mutation."
     - dry_run_completed      → "Wevito finished writing the v0 rename dry-run record without mutating the target file."
     - backup_written         → "Wevito copied the draft artifact to a backup path and recorded its sha256."
     - applied                → "Wevito atomically renamed an artifact draft to its approved counterpart."
     - post_proof_completed   → "Wevito verified the renamed artifact's sha256 and confirmed the original draft no longer exists."
     - rolled_back            → "Wevito restored the original draft artifact from its backup after a v0 rename failure."
     - completed              → "Wevito finished the v0 rename apply sequence after every prior step succeeded."

3. Add 7 new capability flags to
   `vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs`,
   each with `DefaultValue = bool.FalseString`:
     - `apply_runner_design_approved`
     - `apply_runner_implementation_phase_approved`
     - `apply_runner_v0_enabled`
     - `apply_runner_v0_dry_run_required`
     - `apply_runner_v0_backup_required`
     - `apply_runner_v0_post_proof_required`
     - `apply_runner_v0_rollback_required`

4. Add new folder
   `vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/` and three files:

   a. `ApplyRequest.cs` — readonly record with fields:
        - `OperationId : string` (must be non-empty, match scope's awaiting-approval packet)
        - `ScopeId : string` (must be non-empty, must equal the second path segment of `DraftRelativePath`)
        - `ScopeHash : string` (must be lowercase hex, length 64)
        - `DraftRelativePath : string` (path relative to artifact root; must match regex `^[a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+/[a-zA-Z0-9._-]+\.draft\.json$` after normalizing to forward slashes)
        - `ApprovalToken : string` (must match the latest awaiting-approval packet's stored approval token for OperationId)

   b. `ApplyResult.cs` — closed discriminated union (abstract record with three nested sealed records):
        - `Refused(string Reason)` — emitted when any pre-mutation check fails. No packets written.
        - `RolledBack(string Reason, string BackupRelativePath)` — emitted when a mid-flight check fails after backup but before completed.
        - `Succeeded(string ApprovedRelativePath, string PostHashSha256)` — emitted only when every step in the mandatory sequence passed.

   c. `ArtifactRenameApplyRunner.cs` — public sealed class:
        - Constructor takes: `AuditLedgerService ledger`, `Func<string, string?> settings`, `KillSwitchService killSwitch`, `ApplyRunnerPrerequisiteCheckService prereqCheck`, `string artifactRoot` (fully resolved).
        - Single public method:
          ```
          ApplyResult Apply(ApplyRequest request, CancellationToken cancellationToken)
          ```
        - Required pre-mutation checks (in order; first failure short-circuits to `Refused`, no packets written):
            (i)   `cancellationToken.ThrowIfCancellationRequested()`.
            (ii)  KillSwitch active → Refused("kill_switch_active").
            (iii) Any of the 7 new flags not equal to `bool.TrueString` → Refused("flag_<name>_not_true").
            (iv)  `ApplyRunnerPrerequisiteCheckService.Check(operationId, ...)` returns any entry with `Passed=false` → Refused("prerequisite_check_failed:<entry_name>").
            (v)   `DraftRelativePath` regex mismatch, or contains `..`, or path separators other than `/`, or starts with `/`, or has a drive letter, or backslash → Refused("invalid_relative_path").
            (vi)  ScopeId mismatch against `DraftRelativePath`'s second segment → Refused("scope_id_mismatch").
            (vii) `OperationId` mismatch against `DraftRelativePath`'s first segment → Refused("operation_id_mismatch").
            (viii)Resolved source path (Path.Combine + Path.GetFullPath) does not start with `artifactRoot + Path.DirectorySeparatorChar` (case-insensitive on Windows) → Refused("source_outside_artifact_root").
            (ix)  Resolved destination path (same prefix-check) → Refused("destination_outside_artifact_root").
            (x)   Source file does not exist → Refused("source_missing").
            (xi)  Destination file already exists → Refused("destination_already_exists").
            (xii) Approval token does not match latest `ApplyAwaitingApproval` packet's approval_token for `OperationId` → Refused("approval_token_mismatch").
            (xiii)Latest `ApplyAwaitingApproval` packet's `scope_hash` does not equal `ScopeHash` → Refused("scope_hash_mismatch").
        - On every Refused path: write NO packets. Do not mutate any file.
        - On all pre-mutation checks pass, execute the mandatory sequence
          (every packet uses the same OperationId / ScopeId / ScopeHash;
           every packet sets DidUseNetwork=false, DidUseHostedAi=false,
           DidUseLocalModel=false; DidMutate per rule above):

            Step A. Compute pre-hash sha256 of source file (UTF-8 bytes
                    as on disk; full file). Append
                    `ApplyV0DryRunStarted` packet with summary
                    `operation_id=<id> scope_id=<scope> scope_hash=<hash>
                    source=<draftRelativePath> pre_sha256=<hex>`
                    and DidMutate=true (because step B will write a file).

            Step B. Construct dry-run record path:
                    `<artifactRoot>\<operationId>\<scopeId>\apply-v0-dry-run.json`
                    (NOTE: artifact layout is `<artifactRoot>\<operationId>\<scopeId>\…`;
                    if the layout differs from this in the existing
                    artifact tree, re-derive the layout from
                    ApplyRunnerPrerequisiteCheckService and match it.
                    Halt and Stop-card if you cannot determine a single
                    canonical layout.)
                    Write JSON `{ operationId, scopeId, scopeHash,
                    source: draftRelativePath, destination:
                    approvedRelativePath, expectedPreSha256, plannedAt:
                    DateTimeOffset.UtcNow }`. Compute its sha256.
                    Append `ApplyV0DryRunCompleted` packet with
                    `dry_run_path=<path> dry_run_sha256=<hex>` and
                    DidMutate=true.

            Step C. Backup. Copy source file to
                    `<sourcePath>.backup-<UTC-timestamp>`. Verify the
                    backup file's sha256 equals the source pre-hash. If
                    mismatch → roll back (no backup to restore yet;
                    simply delete the partial backup if present) and emit
                    `ApplyV0RolledBack` with reason
                    `backup_sha256_mismatch`. Otherwise append
                    `ApplyV0BackupWritten` packet with
                    `backup_path=<relative> backup_sha256=<hex>` and
                    DidMutate=true.

            Step D. Atomic rename source → destination. Use
                    `File.Move(source, destination, overwrite: false)`.
                    Append `ApplyV0Applied` packet with
                    `source=<draftRelativePath>
                    destination=<approvedRelativePath>` and
                    DidMutate=true. If File.Move throws, restore the
                    backup over the original source path (use File.Move
                    with overwrite=true), verify sha256 equals
                    pre-hash, then append `ApplyV0RolledBack` with reason
                    `rename_threw:<exception_type_name>`.

            Step E. Post-proof. Compute sha256 of the destination file.
                    Confirm it equals the pre-hash. Confirm the source
                    file no longer exists. If either check fails, rename
                    destination back to source (File.Move(destination,
                    source, overwrite: false)), verify the restored
                    source sha256 equals pre-hash, then append
                    `ApplyV0RolledBack` with reason
                    `post_proof_mismatch`. Otherwise append
                    `ApplyV0PostProofCompleted` packet with
                    `post_sha256=<hex>` and DidMutate=false.

            Step F. Append `ApplyV0Completed` packet with
                    `approved_path=<relative> post_sha256=<hex>` and
                    DidMutate=true (terminal evidence of the sequence).
                    Return `ApplyResult.Succeeded(approved_path,
                    post_sha256)`.

        - If `CancellationToken` is cancelled between Step C and Step F,
          treat as a mid-flight failure: restore the backup if backup was
          written and destination either exists or the source no longer
          exists, then emit `ApplyV0RolledBack` with reason
          `cancelled`. Do NOT emit `ApplyV0Completed`.
        - If KillSwitch becomes active between Step C and Step F, treat
          as a mid-flight failure: restore the backup, then emit
          `ApplyV0RolledBack` with reason `kill_switch_activated`. Do
          NOT emit `ApplyV0Completed`.

5. Add factory in
   `vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs`:
   `public static ArtifactRenameApplyRunner CreateArtifactRenameApplyRunner(
     AuditLedgerService ledger,
     Func<string, string?> settings,
     KillSwitchService? killSwitchService = null)`
   The factory resolves `artifactRoot` to
   `Path.Combine(Environment.GetFolderPath(LocalApplicationData),
   "WevitoVNext", "artifacts")` (matching the existing convention) and
   constructs an `ApplyRunnerPrerequisiteCheckService` from the same
   ledger, settings, and artifactRoot. The factory does NOT call
   `Apply` and does NOT enable any flag.

6. Add new test file
   `vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs`.
   The file uses an in-memory SQLite AuditLedgerService and a temporary
   artifact root under `Path.GetTempPath()`. Each [Fact] method is
   named `Apply_<short_claim_id>`. The class must NOT import
   `System.Net.Http` or `System.Net.Sockets`. The class must NOT
   reference `IHeldOutEvalStore`, `HeldOutEvalCase`,
   `IInDistributionEvalStore`, or `InDistributionEvalCase`.

   Required [Fact] tests (every test sets default flags to False unless
   the test explicitly sets one to True; ALL 7 flags must be True for
   the happy path):

   a. Apply_refuses_when_v0_enabled_flag_false_writes_no_packet
   b. Apply_refuses_when_design_approved_flag_false_writes_no_packet
   c. Apply_refuses_when_implementation_phase_approved_flag_false_writes_no_packet
   d. Apply_refuses_when_dry_run_required_flag_false_writes_no_packet
   e. Apply_refuses_when_backup_required_flag_false_writes_no_packet
   f. Apply_refuses_when_post_proof_required_flag_false_writes_no_packet
   g. Apply_refuses_when_rollback_required_flag_false_writes_no_packet
   h. Apply_refuses_when_kill_switch_active_writes_no_packet
   i. Apply_refuses_when_prerequisite_check_returns_any_failed_entry
   j. Apply_refuses_when_draft_relative_path_contains_double_dot
   k. Apply_refuses_when_draft_relative_path_is_absolute
   l. Apply_refuses_when_draft_relative_path_contains_backslash
   m. Apply_refuses_when_draft_relative_path_does_not_end_with_draft_json
   n. Apply_refuses_when_scope_id_segment_mismatches_request
   o. Apply_refuses_when_operation_id_segment_mismatches_request
   p. Apply_refuses_when_source_file_does_not_exist
   q. Apply_refuses_when_destination_file_already_exists
   r. Apply_refuses_when_approval_token_mismatches_awaiting_approval_packet
   s. Apply_refuses_when_scope_hash_mismatches_awaiting_approval_packet
   t. Apply_happy_path_writes_six_packets_in_order
        Assert: packet sequence kinds are exactly
        [dry_run_started, dry_run_completed, backup_written, applied,
         post_proof_completed, completed]; destination file exists;
         source file no longer exists; post sha256 equals pre sha256.
   u. Apply_backup_sha256_mismatch_rolls_back_emits_no_completed
        Force a mid-flight backup mismatch (test seam: an
        `IFileHasher` override that returns a wrong hash for the backup
        file the first time it is hashed). Assert: packets are
        [dry_run_started, dry_run_completed, rolled_back]; source file
        still exists with original content.
   v. Apply_post_proof_mismatch_rolls_back_destination_back_to_source
        Force a mid-flight post-proof mismatch. Assert: packets are
        [dry_run_started, dry_run_completed, backup_written, applied,
         rolled_back]; source file restored with original content;
        destination file does not exist.
   w. Apply_double_apply_against_same_artifact_returns_destination_already_exists
        Run Apply twice; second call returns Refused
        ("destination_already_exists") and writes no additional
        packets.
   x. Apply_kill_switch_activated_between_backup_and_post_proof_rolls_back
        Use a KillSwitch test stub that flips after backup_written.
        Assert: packets are [dry_run_started, dry_run_completed,
         backup_written, rolled_back] (no `applied`, no `completed`);
        source file restored.
   y. Apply_runner_class_source_contains_no_update_or_delete_sql
        Read the runner source as text. Assert.DoesNotContain
        "UPDATE", "DELETE FROM", "DROP TABLE" (case-insensitive).
   z. Apply_runner_class_source_only_writes_under_artifact_root
        Read the runner source as text. Assert.DoesNotContain
        "Environment.SpecialFolder.MyDocuments",
        "Environment.SpecialFolder.Desktop",
        "Environment.SpecialFolder.System",
        "Path.GetTempPath()" (the runner must never reach into temp
        directly), and `"AppContext.BaseDirectory"`. The runner accepts
        an artifact root via constructor only.

   Test infrastructure: provide a small `FakeHasher` test seam exposed
   through a constructor overload only used by tests. Production code
   must not depend on a hashing interface — production uses
   `SHA256.HashData` directly. The test seam is internal and accessed
   via `InternalsVisibleTo("Wevito.VNext.Tests")` if needed, or via a
   `protected internal virtual` `ComputeSha256(Stream)` method on the
   runner. Choose whichever pattern matches existing code in
   `ApplyRunnerPrerequisiteCheckService` / `OllamaLoopbackScoringProvider`.

7. Add new test file
   `vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerHeldOutAccessTests.cs`
   asserting that the runner type does not reference any held-out or
   in-distribution eval store type. Use reflection over the runner's
   constructor parameter types and field types, plus a static text scan
   of the source file for substrings `HeldOut`, `InDistribution`,
   `EvalCase` and assert none appear.

8. Extend `vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs`
   `forbiddenTypes` list to include `typeof(ArtifactRenameApplyRunner)`,
   `typeof(ApplyRequest)`, and `typeof(ApplyResult)`. Document the
   exception in a comment matching the C-PHASE 174 / 178 comment style.

9. Extend `vnext/tests/Wevito.VNext.Tests/Support/KillSwitchCoverageAllowList.cs`
   so the new runner type is in the expected-to-honor-KillSwitch list.

10. Update `docs/codex-phase-history.jsonl` with one new row:
    `{"phase_id":"C-PHASE 183","status":"merged_pending_user_review",
      "branch":"claude-implementation/c-phase-183-artifact-rename-apply-runner",
      "pr_url":"<gh-pr-url>","utc":"<UTC>","summary":"smallest safe
      apply-runner prototype: ArtifactRenameApplyRunner (default off,
      flag-gated, rename .draft.json → .approved.json only)"}`

DO NOT in this phase:
  - Add an apply-runner UI surface, button, or click handler.
  - Wire the runner into SupervisedImprovementLoop.
  - Modify `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason`.
  - Modify `ApplyCompleted` constant or the
    BuildApplyCompletedWithoutAwaitingApproval watchdog.
  - Modify ApplyRunnerStatusReport schema (the broad
    `apply_runner_implemented` field stays as-is — it tracks the
    supervised loop, which is still not implemented).
  - Add a rollback runner separate from the auto-rollback-on-failure
    inside Apply (a symmetric explicit rollback runner is C-PHASE 185;
    DO NOT add it here).
  - Add a UI review panel (that is C-PHASE 184).
  - Extend the runner to accept any extension other than `.draft.json`
    (extending to `.draft.txt`, `.draft.md`, etc. is C-PHASE 187; DO
    NOT add it here).
  - Add hosted-AI, network, model, training, or sprite mutation paths.
  - Read held-out or in-distribution eval content at runtime.

Allowed file changes for this phase:
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyRequest.cs                       (new)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ApplyResult.cs                        (new)
  - vnext/src/Wevito.VNext.Core/SelfImprovement/Apply/ArtifactRenameApplyRunner.cs          (new)
  - vnext/src/Wevito.VNext.Core/Audit/SelfImprovementPacketKinds.cs                         (extend; 7 new constants)
  - vnext/src/Wevito.VNext.Core/Audit/CapabilityFlagInventory.cs                            (extend; 7 new flags, all default False)
  - vnext/src/Wevito.VNext.Core/PlainLanguageExplainer.cs                                   (extend; 7 new KnownPacketKinds + 7 Explain cases)
  - vnext/src/Wevito.VNext.Core/ShellCompositionRoot.cs                                     (extend; one new factory)
  - vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerTests.cs                        (new)
  - vnext/tests/Wevito.VNext.Tests/ArtifactRenameApplyRunnerHeldOutAccessTests.cs           (new)
  - vnext/tests/Wevito.VNext.Tests/HeldOutEvalStoreVisibilityTests.cs                       (extend; forbiddenTypes additions)
  - vnext/tests/Wevito.VNext.Tests/Support/KillSwitchCoverageAllowList.cs                   (extend)
  - docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md                              (new)
  - docs/codex-phase-history.jsonl                                                          (one append)

NO other files may change.

Stop gates (HALT and open the PR as Draft if any is TRUE):
  - Any required flag is changed to default True.
  - Any new packet kind is missing from `PlainLanguageExplainer.KnownPacketKinds`.
  - The runner writes any file outside the artifact root.
  - The runner writes any packet on a Refused path.
  - The runner emits ApplyV0Completed without first emitting all five preceding packets.
  - The runner emits ApplyCompleted (legacy supervised-loop packet) — that constant remains untouched.
  - `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` changes.
  - Any test imports `System.Net.Http` or `System.Net.Sockets`.
  - Any test references a held-out or in-distribution eval type.
  - The runner introduces any UPDATE or DELETE SQL.
  - Pre-flight commands fail.

Validation (run before opening PR):
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameApplyRunner|ApplyRunnerPrerequisite|HeldOutEvalStoreVisibility|InDistributionVersusHeldOutTypeSeparation|PlainLanguage|SelfImprovement|PostC174SelfImprovementBaseline"
  dotnet build .\vnext\Wevito.VNext.sln
  dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
  powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
  git diff --check

Write the phase report at:
  docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md
With sections:
  - Goal
  - Scope (one new namespace; 7 new flags; 7 new packet kinds; one new factory; one new runner; two new test files)
  - The Seven Packet Kinds (table mapping kind → DidMutate value → plain-language sentence)
  - The Seven Capability Flags (table; all default False; runner refuses unless every flag is True at apply time)
  - Mandatory Sequence (the 6 step ladder; rollback paths)
  - Safety Boundaries (artifact-root confinement; no source-code/sprite/asset mutation; no network; no model call; no held-out content; no auto-merge; no UI surface in this phase)
  - Validation (the five commands and outcomes)
  - Stop-Gate Checklist (all FALSE)
  - Next Phase (C-PHASE 184 — Apply-runner review UI — NOT YET APPROVED FOR IMPLEMENTATION; awaiting user signoff)

PR and commit:
  - Branch: claude-implementation/c-phase-183-artifact-rename-apply-runner
  - Commit: "C-PHASE 183: smallest safe apply-runner prototype (ArtifactRenameApplyRunner; default off; rename .draft.json → .approved.json only)"
  - PR title: "C-PHASE 183: Smallest safe apply-runner prototype (artifact rename only; default off)"
  - PR body: link to docs/C_PHASE183_ARTIFACT_RENAME_APPLY_RUNNER_2026-05-19.md.
  - Auto-continue: NO. Halt for user review after the PR opens.

When you finish C-PHASE 183, append one row to docs/codex-phase-history.jsonl
with phase_id="C-PHASE 183", UTC timestamp, branch, and PR URL.

If you complete C-PHASE 183 and the user explicitly approves continuing, the
next phase is C-PHASE 184 (Apply-runner review UI). C-PHASE 184 is NOT YET
APPROVED FOR IMPLEMENTATION. Do NOT start 184 until the user signs off and
pastes its prompt from
docs/CLAUDE_C_PHASE183_PLUS_CODEX_MEDIUM_PROMPTS_2026-05-19.md §2.

Begin.
```
