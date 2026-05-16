# C-PHASE 114: Codex Loop Reliability Hardening

## Goal

Add the reliability shell for the Codex phase loop: durable queue files, heartbeat/status history, pause/resume/inject controls, timeout/stuck/retry policy, an idle detector, and a broker watchdog policy.

## Shape

```text
docs/codex-phase-queue.json ──▶ CodexPhaseQueueService ──▶ CodexLoopRunnerService
                                         │                           │
                                         ▼                           ▼
docs/codex-loop-status.json       history jsonl              heartbeat packet
docs/codex-phase-history.jsonl          │                           │
                                         ▼                           ▼
                              pause/resume/inject scripts    broker watchdog
```

## Implemented

- Added `CodexPhaseQueueService` for queue/status/history files and immutable completed-phase protection.
- Added `CodexLoopRunnerService` policy decisions for:
  - 2-hour timeout while user is present.
  - indefinite extension once the user is idle for more than 10 minutes.
  - 30-minute no-commit stuck detection.
  - retry once, then remediation-card halt.
  - trivial auto-rebase decision and semantic-conflict halt.
  - refusal to mark a phase complete when tests failed.
- Added `CodexLoopWatchdogService` in Broker to request restart after missed heartbeat while honoring kill switch.
- Added `tools/run-codex-loop.ps1`, `tools/codex-inject.ps1`, `tools/codex-loop-pause.ps1`, and `tools/codex-loop-resume.ps1`.
- Added `tools/idle-detector`, which reads user idle time through `GetLastInputInfo`.
- Added initial queue/status/history files under `docs/`.
- Added a small Home HUD loop badge.
- Added `codex_loop` to `tool_definitions.json`.
- Added plain-language coverage for all new loop packet kinds.

## Safety Boundaries

- The runner scaffold does not silently invoke hosted AI or mutate source beyond the explicit queue/status/history files.
- Auto-continue false is treated as a halt/review condition.
- Completed phases cannot be re-injected through the queue service.
- Kill switch blocks runner service decisions and watchdog restarts.
- The idle helper uses only `GetLastInputInfo`.

## Validation

Run before merge:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "CodexPhaseQueue|CodexLoopRunner|CodexLoopWatchdog|PlainLanguage"
dotnet build .\vnext\Wevito.VNext.sln
dotnet build .\tools\idle-detector\idle-detector.csproj
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

## Next Phase

Auto-continue is off for this phase. After review, C-PHASE 115 can handle identity rename and UX language cleanup.
