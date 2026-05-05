# Code-Side Phase 33 Proof Execution Design - 2026-05-05

## Goal

Define how PET TASKS could eventually run approved build/test/proof commands without becoming a loose terminal.

No execution implementation was added in this phase.

```text
╔════════════════════════════════════════════════════════════╗
║ PET TASKS Proof Execution Gate                            ║
╠═══════════════╦═══════════════╦═══════════════╦════════════╣
║ 1. Plan       ║ 2. Approve    ║ 3. Execute    ║ 4. Review  ║
║ report-only   ║ user-visible  ║ allowlisted   ║ artifacts  ║
╠═══════════════╬═══════════════╬═══════════════╬════════════╣
║ buildProof    ║ exact command ║ no shell free ║ logs       ║
║ plan artifact ║ + risk text   ║ text entry    ║ summaries  ║
╚═══════════════╩═══════════════╩═══════════════╩════════════╝
```

## Proposed Execution Contract

Future execution should require all of these before any process starts:

- A `BuildProofPlanReport` exists from the `buildProof` preview adapter.
- The task card is explicitly `Approved`.
- The command selected for execution exactly matches an allowlisted command from the plan.
- The command has no unapproved arguments.
- The command writes logs to a new timestamped `vnext\artifacts\pet-tasks` folder.
- The command is cancellable.
- The command records stdout, stderr, exit code, start time, end time, working directory, environment overrides, and post-run hash checks.

## Allowlist Shape

```text
allowed command families
├── dotnet build .\vnext\Wevito.VNext.sln
├── dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
├── tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
└── tools\probe-vnext-pet-tasks.ps1 ... -SkipBuild
```

Rules:

- No arbitrary shell text.
- No redirection or shell composition.
- No `Remove-Item`, `git reset`, `git checkout`, package install, upload, login, browser automation, or asset prep.
- `build-vnext.ps1` must include `-SkipAssetPrep` while visual cleanup is active.
- Probe commands must preserve target sprite hash checks unless the user explicitly approves skipping them.

## Execution Result Contract

Recommended future contracts:

- `ProofExecutionCommand`
- `ProofExecutionRequest`
- `ProofExecutionManifest`
- `ProofExecutionResult`

Suggested fields:

- task card id
- command id
- executable
- arguments
- working directory
- environment overrides
- expected artifact root
- stdout path
- stderr path
- merged log path
- exit code
- started/finished UTC
- timeout
- cancellation state
- pre-run protected hashes
- post-run protected hashes
- did mutate code
- did mutate assets
- did run asset prep

## UI Flow

```text
PET TASKS card
  │
  ├─ PREVIEW ▶ buildProof plan report
  │
  ├─ APPROVE ▶ exact command list becomes selectable
  │
  ├─ RUN ONE ▶ command runs with logs + cancel button
  │
  └─ REVIEW ▶ pass/fail summary + artifact links
```

Keep the user-facing surface simple:

- one task card,
- one current command,
- one progress line,
- one cancel button,
- one result/report link.

Do not show raw terminal output by default; tuck it behind "View log".

## Required Guardrails

- Execution must not be available for visual generation/import, sprite mutation, prop anchors, browser automation, translation network calls, audio changes, or arbitrary external actions.
- Execution must stop if protected sprite rows change unexpectedly.
- Execution must stop if a build/proof command attempts asset preparation.
- Execution must stop if a command needs admin privileges.
- Execution must stop if output paths are outside the artifact root.
- Execution must never clean or revert unrelated dirty work automatically.

## Validation Plan For Future Implementation

- Unit tests for allowlist matching.
- Unit tests for blocked command composition and destructive commands.
- Unit tests for `-SkipAssetPrep` enforcement.
- Unit tests for manifest round-trip.
- Integration test with a harmless command stub.
- Live PET TASKS probe for approval -> run stub -> review result.
- Separate manual proof before running real build commands through PET TASKS.

## Recommended Sequencing

1. Add contracts only.
2. Add allowlist evaluator tests.
3. Add command-runner abstraction with a fake runner.
4. Add UI status model and cancellation model.
5. Add a stub execution probe.
6. Only then consider real build/test command execution.

## Current Decision

Hold implementation.

Phase 32 already provides the safe buildProof plan artifact. That is enough for now while visual-side sprite cleanup is active.
