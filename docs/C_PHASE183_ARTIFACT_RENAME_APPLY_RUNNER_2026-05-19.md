# C-PHASE 183 — ArtifactRenameApplyRunner

## Goal

Implement the smallest safe apply-runner prototype: a default-off, flag-gated service that can rename exactly one artifact JSON file from `<operation>/<scope>/<name>.draft.json` to `<operation>/<scope>/<name>.approved.json` under the canonical artifact root.

## Scope

- Added `Wevito.VNext.Core.SelfImprovement.Apply`.
- Added `ApplyRequest`, `ApplyResult`, and `ArtifactRenameApplyRunner`.
- Added 7 `_v0_` packet kinds.
- Added 7 default-False capability flags.
- Added one `ShellCompositionRoot` factory.
- Added apply-runner safety tests and held-out/in-distribution visibility tests.
- Updated the existing prerequisite-check producer guard so the newly approved C-PHASE 183 runner is the only additional allowed caller.
- Did not wire the runner into `SupervisedImprovementLoop`.
- Did not add UI, model calls, network calls, source-code mutation, sprite mutation, or broader file mutation.

## The Seven Packet Kinds

| Packet kind | DidMutate | Plain-language sentence |
|---|---:|---|
| `self_improvement_apply_v0_dry_run_started` | `true` | Wevito wrote a self-improvement v0 rename dry-run record before any mutation. |
| `self_improvement_apply_v0_dry_run_completed` | `true` | Wevito finished writing the self-improvement v0 rename dry-run record without mutating the target file. |
| `self_improvement_apply_v0_backup_written` | `true` | Wevito copied the self-improvement draft artifact to a backup path and recorded its sha256. |
| `self_improvement_apply_v0_applied` | `true` | Wevito atomically renamed a self-improvement artifact draft to its approved counterpart. |
| `self_improvement_apply_v0_post_proof_completed` | `false` | Wevito verified the self-improvement renamed artifact's sha256 and confirmed the original draft no longer exists. |
| `self_improvement_apply_v0_rolled_back` | `true` | Wevito restored the original self-improvement draft artifact from its backup after a v0 rename failure. |
| `self_improvement_apply_v0_completed` | `true` | Wevito finished the self-improvement v0 rename apply sequence after every prior step succeeded. |

## The Seven Capability Flags

All default to `False`. The runner refuses unless every flag is explicitly `True` at apply time.

| Flag | Default |
|---|---:|
| `apply_runner_design_approved` | `False` |
| `apply_runner_implementation_phase_approved` | `False` |
| `apply_runner_v0_enabled` | `False` |
| `apply_runner_v0_dry_run_required` | `False` |
| `apply_runner_v0_backup_required` | `False` |
| `apply_runner_v0_post_proof_required` | `False` |
| `apply_runner_v0_rollback_required` | `False` |

## Mandatory Sequence

```text
ApplyRequest
    │
    ▼
13 pre-mutation checks
    │
    ├─▶ refused: no apply-v0 packet, no target mutation
    │
    ▼
dry_run_started ─▶ dry_run_completed ─▶ backup_written
    │                                      │
    │                                      ▼
    │                                  applied
    │                                      │
    ▼                                      ▼
rolled_back ◀──────────── failure ─── post_proof_completed ─▶ completed
```

Rollback paths are automatic for mid-flight failure after backup. Backup sha256 mismatch, rename failure, post-proof mismatch, cancellation, and mid-flight KillSwitch activation all avoid `ApplyV0Completed`.

## Safety Boundaries

- Artifact-root confinement is enforced through canonical full-path checks.
- Relative paths must match `<operation>/<scope>/<name>.draft.json` and reject `..`, absolute paths, backslashes, drive letters, and reparse-point source files.
- The runner writes no legacy `self_improvement_apply_completed` packet.
- `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` remains unchanged.
- Refused pre-mutation paths do not write apply-v0 packets and do not mutate target files.
- No hosted AI, no network, no model call, no held-out content, no in-distribution content, no source-code mutation, no sprite mutation, and no UI surface were added.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameApplyRunner"`: passed, 27/27.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ArtifactRenameApplyRunner|ApplyRunnerPrerequisite|HeldOutEvalStoreVisibility|InDistributionVersusHeldOutTypeSeparation|PlainLanguage|SelfImprovement|PostC174SelfImprovementBaseline"`: passed, 344/344.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 1442/1442.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed.

## Stop-Gate Checklist

- [x] No required flag defaults to `True`.
- [x] All new packet kinds are in `PlainLanguageExplainer.KnownPacketKinds`.
- [x] Runner writes only under constructor-provided artifact root.
- [x] Refused paths write no apply-v0 packet.
- [x] `ApplyV0Completed` only emits after the preceding sequence succeeds.
- [x] Legacy `ApplyCompleted` is untouched.
- [x] `SupervisedImprovementLoop.ApplyRunnerNotImplementedReason` is unchanged.
- [x] Tests do not import network namespaces.
- [x] Runner and tests do not reference held-out or in-distribution eval content.
- [x] Runner source contains no `UPDATE`, `DELETE FROM`, or `DROP TABLE` SQL.

## Next Phase

C-PHASE 184 — Apply-runner review UI is NOT YET APPROVED FOR IMPLEMENTATION. It requires explicit user signoff before Codex begins it.
