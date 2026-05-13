# C-PHASE 75 Autonomous Operations Beta

## Goal

Add the first full autonomous-operations loop while keeping Wevito local-first,
proposal-only, default-disabled, and unable to apply mutations directly.

## Scope

- Added `AutonomousOperationsConfig` for default-off beta settings.
- Added `AutonomousBetaDecisionService` to decide whether the audit ledger
  supports limited autonomy over the last seven days.
- Added `AutonomousOperationsLoop` to run at most one proposal-only iteration per
  tick after all gates pass.
- Wired the shell tick into the beta loop only through the runtime supervisor,
  KillSwitch, daily cap, interval, and ledger decision gates.
- Added settings UI status/toggle text for the autonomous beta loop.
- Added tests for decision refusal, successful enable decisions, hosted-AI
  refusal, unproofed mutation refusal, dormancy gates, daily cap, KillSwitch, and
  the no-direct-mutation guarantee.

## Safety Boundaries

- `runtime_autonomous_beta_enabled` defaults to `false`.
- The loop requires `RuntimeSupervisorMode.Active` and background work allowed.
- KillSwitch blocks before any iteration can write an activity packet.
- The beta decision refuses to compute without ledger history.
- Any hosted-AI use, focus-steal marker, policy/block row, resource-budget
  exceeded marker, or unproofed mutation prevents autonomous beta enablement.
- Iterations write only timestamped `activity_summary` evidence packets under
  `vnext/artifacts/pet-tasks/`.
- Iterations never apply mutations and record `DidMutate=false`.
- Web/file/model steps remain preview-labelled; no web fetch, hosted AI call, or
  mutation execution was added.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "AutonomousOperations|AutonomousBeta"` passed: 13 / 13.
- `dotnet build .\vnext\Wevito.VNext.sln` passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 405 / 405.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.

## Stop Gate

C-PHASE 75 is intentionally stop-gated. The PR should be reviewed before merge,
and enabling `runtime_autonomous_beta_enabled=true` should remain a deliberate
user decision after reviewing the ledger decision output.

## Next Phase

No automatic next phase is authorized by this plan. The next decision is whether
to merge the beta loop, keep it disabled while collecting more activity ledger
history, or pause for reliability work.
