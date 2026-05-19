# C-PHASE 158 - Deterministic-Replay Verification

Date: 2026-05-18
Branch: `claude-implementation/c-phase-158-deterministic-replay-verification`
Base: `8bd0ba5024e8f4ebaeb86711518694d994291623`

## Goal

Add a test-only deterministic replay harness for the `sprite-repair-batch-proposal` review-only scope. The harness replays a captured seeded scope tick and compares the evidence packet stream after redacting only packet ids, task-card ids, and timestamps.

## Scope

Files added:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Replay/ReplayCapture.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Replay/ReplayComparisonResult.cs`
- `vnext/src/Wevito.VNext.Core/SelfImprovement/Replay/ReplayHarness.cs`
- `vnext/tests/Wevito.VNext.Tests/ReplayHarnessTests.cs`
- `docs/C_PHASE158_DETERMINISTIC_REPLAY_VERIFICATION_2026-05-18.md`

Files extended:

- `vnext/src/Wevito.VNext.Core/SelfImprovement/Experiments/SpriteRepairBatchProposalScope.cs`
- `docs/codex-phase-history.jsonl`

Not touched:

- No sprite assets.
- No content JSON.
- No model adapter.
- No held-out eval store.
- No capability flag.
- No apply runner.
- No production audit-ledger replay writer.

## Implemented

- Added `ReplayCapture` to carry the replay scope id, operation id, seed, original timestamp, and captured packet stream.
- Added `ReplayComparisonResult` with `Identical`, `Diverged`, and `NotApplicable` results.
- Added `ReplayHarness` with `Replay(ReplayCapture)` that:
  - returns `NotApplicable("kill_switch=true")` when the kill switch is active;
  - replays only the supplied `SpriteRepairBatchProposalScope`;
  - derives the artifact root from the captured packet artifacts;
  - writes replay packets to an in-memory packet list only;
  - compares packets after redacting `PacketId`, `TaskCardId`, and `CreatedAtUtc`;
  - reports one diff line per non-redacted mismatch.
- Extended `SpriteRepairBatchProposalScope` with a non-breaking seeded replay overload:
  - existing `TryRun(request)` behavior remains unchanged;
  - `TryRun(request, seed, packetSink)` uses deterministic ids only when a seed is supplied;
  - the packet sink path bypasses `AuditLedgerService.Record` and captures the constitutional review packet in memory.
- Added tests for:
  - identical seeded replay;
  - divergence when a non-redacted packet field changes;
  - kill-switch `NotApplicable` behavior;
  - zero writes to the production audit ledger during replay.

## Safety Boundaries

- Replay does not instantiate or call a model adapter.
- Replay does not read `IHeldOutEvalStore` or any held-out eval path.
- Replay does not add any apply or mutation pathway.
- Replay does not introduce a capability flag.
- Replay writes no production audit rows; the replay packet stream is captured through an in-memory sink.
- The only scope change is opt-in through the new overload; the normal autonomous scope path still writes exactly as before.

## Validation

Passed:

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "ReplayHarness|SpriteRepairBatchProposal|PlainLanguage|SelfImprovement"`: 251/251 passed.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: 1177/1177 passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.
- `git diff --check`: passed with Git's existing LF-to-CRLF working-copy warning only.

## Stop-Gate Checklist

- [x] Replay does not write to a production ledger.
- [x] Replay does not read held-out content.
- [x] Replay does not use a model adapter.
- [x] Tests fail the replay when non-redacted packet fields differ.
- [x] Pre-flight commands passed before implementation.
- [x] No new production capability flag was added.

## Next Phase

C-PHASE 158 is Auto-continue=No. After this PR is reviewed, the next batch can consider C-PHASE 159+ planning, such as held-out eval seeding, a second review-only scope, or the v1 Proposer/Solver/Judge split described in the C-PHASE 151+ plan narrative.
