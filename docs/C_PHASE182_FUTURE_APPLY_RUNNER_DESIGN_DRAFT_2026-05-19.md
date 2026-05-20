## 0. DO NOT IMPLEMENT YET — DESIGN DRAFT, REQUIRES USER SIGNOFF

This is a design draft only. It does not approve, schedule, or implement C-PHASE 183 or any future apply runner. C-PHASE 183 is NOT drafted, NOT approved, and NOT scheduled. No source code, capability flag, packet kind, or runtime behavior should be created from this document until the user explicitly signs off on the scope, flags, packet taxonomy, and implementation shape.

```text
Future apply-runner prototype shape

╔══════════════════════════════════════════════════════════════════════╗
║ Human approval remains the judge                                    ║
╠══════════════════════════════════════════════════════════════════════╣
║ Approval token                                                      ║
║   ▼                                                                  ║
║ Preflight: re-run prerequisite check, all gates must pass            ║
║   ▼                                                                  ║
║ Dry-run: write apply-dry-run.json under vnext/artifacts/             ║
║   ▼                                                                  ║
║ Backup: copy current draft artifact + sha256 proof                   ║
║   ▼                                                                  ║
║ Apply: atomic rename <x>.draft.json → <x>.approved.json              ║
║   ▼                                                                  ║
║ Post-proof: sha256 verify final bytes                                ║
║   ▼                                                                  ║
║ Completed packet only if every prior step passed                     ║
║                                                                      ║
║ Any failure at any point ──▶ rollback ──▶ rolled_back packet          ║
╚══════════════════════════════════════════════════════════════════════╝
```

## 1. Scope of the proposed prototype

The proposed first prototype scope is intentionally tiny: rename one existing JSON artifact under `vnext/artifacts/<scope>/<operation-id>/` from `<x>.draft.json` to `<x>.approved.json`.

The prototype would touch nothing outside `vnext/artifacts/`. It would not edit source code, runtime assets, sprites, content manifests, user files, local model weights, audit ledger rows other than append-only evidence packets, or any path outside the canonical artifact root.

This scope is useful only as a safety exercise. It proves the apply pipeline shape without granting Wevito authority over real product source, real assets, or real user data.

## 2. Prerequisites that must pass

The future runner cannot start unless the existing `ApplyRunnerPrerequisiteCheckService` reports all ten prerequisites as passed:

- `KillSwitch armed`
- `EvalGateRunner v1 enabled`
- `Heuristic judge enabled`
- `Snapshot signed and verified recently`
- `Held-out store contains >= 1 case`
- `In-distribution store contains >= 1 case`
- `Scope hash matches latest awaiting-approval artifact`
- `Replay run within window`
- `Capability default-off audit`
- `Apply runner declared not implemented`

Two additional user-signoff prerequisites must also remain false until the user approves a future implementation phase:

- `apply_runner_design_approved=False`
- `apply_runner_implementation_phase_approved=False`

The future C-PHASE 183 prompt must not be drafted until both of those are explicitly approved by the user in writing.

## 3. New capability flags (all default OFF)

Any future implementation would need these capability flags, all defaulting to `False`:

- `apply_runner_design_approved`
- `apply_runner_implementation_phase_approved`
- `apply_runner_v0_enabled`
- `apply_runner_v0_dry_run_required`
- `apply_runner_v0_backup_required`
- `apply_runner_v0_post_proof_required`
- `apply_runner_v0_rollback_required`

No phase should flip these flags to `True` automatically. Any true value must be user-visible, auditable, and reversible.

## 4. New audit packet kinds

Any future implementation would need these packet kinds registered in `SelfImprovementPacketKinds` and `PlainLanguageExplainer.KnownPacketKinds` before any producer can emit them:

- `self_improvement_apply_dry_run_started`
- `self_improvement_apply_dry_run_completed`
- `self_improvement_apply_backup_written`
- `self_improvement_apply_applied`
- `self_improvement_apply_post_proof_completed`
- `self_improvement_apply_rolled_back`
- `self_improvement_apply_completed`

Every packet must set `DidUseNetwork`, `DidUseHostedAi`, `DidUseLocalModel`, and `DidMutate` honestly. For the proposed artifact rename, `DidMutate=true` is expected only on the actual apply, rollback, and any backup-writing packet that writes files.

## 5. Mandatory sequence

The future prototype sequence must be exactly:

1. Approval token: verify the user approval payload still matches the operation id, scope id, approved scope hash, and typed confirmation.
2. Preflight: re-run the prerequisite check, and halt unless every prerequisite passes.
3. Dry-run: write `apply-dry-run.json` under the operation artifact folder, describing the exact source path, destination path, expected pre-hash, expected post-state, and rollback plan.
4. Backup: copy the current `<x>.draft.json` to a backup path under the same operation artifact folder and record sha256.
5. Apply: atomically rename `<x>.draft.json` to `<x>.approved.json`.
6. Post-proof: compute sha256 of `<x>.approved.json`, confirm the expected final state, and confirm `<x>.draft.json` no longer exists.
7. Complete: append `self_improvement_apply_completed` only after every previous step passed.

Any failure must automatically roll back by restoring the backup, verifying sha256, and appending `self_improvement_apply_rolled_back`.

## 6. Stop gates at every step

The future runner must halt before every step if any of these are true:

- `KillSwitchService.IsActive()` is true.
- Any prerequisite check entry is false.
- Any required future flag is false.
- Backup hash mismatches.
- Any source, destination, backup, proof, or report path resolves outside `vnext/artifacts/`.
- The target file is not a JSON artifact.
- The destination already exists and is not byte-identical to the expected result.
- The current operation id, scope id, or scope hash differs from the approved token.
- Any runtime code attempts to call hosted AI, fetch network resources, train, tune weights, or mutate outside the declared artifact scope.

## 7. Test strategy

A future implementation must include deterministic tests for:

- Identical path: dry-run, backup, apply, post-proof, and completed packet succeed.
- Diverged path: source hash differs from dry-run expectation and apply refuses.
- KillSwitch midway: KillSwitch activates after backup but before apply, and rollback restores the original artifact.
- Backup mismatch: backup sha256 mismatch halts and emits rollback/refusal evidence.
- Path traversal: `../`, symlink-like, absolute path, alternate separator, and mixed-case artifact-root attempts all refuse.
- Double-apply idempotency: a second apply against the same approved artifact refuses or returns an idempotent no-op with no duplicate mutation.
- Stop-Everything mid-run: the same halt contract works from every sequence boundary.
- Packet honesty: mutation packets set `DidMutate=true`; non-mutating preflight/dry-run packets set it false unless they write a dry-run file.
- Append-only ledger: no UPDATE or DELETE SQL appears anywhere in the future runner path.

## 8. Why this is not implemented now

The master plan hard invariants say:

```text
- Wevito's reasoning runs LOCAL only; no hosted GPT/Claude/Codex/Gemini at runtime.
- No hidden web access, no hidden training, no hidden file access.
- Every risky capability stays default-off OR ships with empty initial state.
- Every learning step: reviewed data + eval gate + rollback.
- Every mutation: dry-run + backup hash + apply + post-proof + rollback?
- KillSwitch halts every adapter, scheduler, loop, tracker, runner.
- AuditLedgerService is append-only (sqlite UPDATE/DELETE triggers); never violated.
- Pets remain visually regular pet-sim characters; no AI-task animation overlay.
- Pet sim experience (visuals + gameplay) MUST NOT be affected by AI workload.
- Only loopback endpoints permitted for local model calls.
- Every evidence packet sets did_use_network/hosted_ai/local_model/mutate honestly.
- Every new packet kind must appear in PlainLanguageExplainer.KnownPacketKinds.
- Codex never auto-merges Auto-continue=No phases.
```

A real apply runner is the first explicit self-improvement mutation surface. That makes it qualitatively riskier than the read-only reports, dry-runs, score probes, and dashboards built so far. The design must receive user signoff before any C-PHASE 183 prompt is drafted. C-PHASE 183 is not approved, not scheduled, and must not be inferred from this document alone.

## 9. Open questions for the user

- Is renaming under `vnext/artifacts/` the right first apply scope, or is it too artificial to teach us anything useful?
- Would rotating or approving a snapshot artifact be a better first mutation target?
- Should the future apply step live in an in-process service, a standalone CLI, or both?
- Does Codex draft the dry-run JSON, or does the service generate it from a constrained request object?
- Should the first prototype require a manual UI approval click even after a typed approval token exists?
- How long should backup artifacts be retained before any cleanup proposal can mention them?
- Should the future runner be allowed to operate while the pet simulator is visible, or only while Wevito is in a quiet/tooling mode?

## 10. Required user actions before C-PHASE 183 is drafted

Before any C-PHASE 183 prompt exists, the user must:

- Approve every new capability flag listed in this document.
- Approve the first mutation scope.
- Sign off on the packet taxonomy.
- Decide whether apply happens in-process or via a CLI.
- Decide whether a UI approval step is required in addition to typed confirmation.
- Confirm that the prototype may mutate only `vnext/artifacts/` and nothing else.
- Confirm whether the future apply runner should remain separate from pet-game UI and sprite workflow UI.

Until those decisions are made, Wevito remains in the current safe state: the supervised self-improvement apply runner is still not implemented.
