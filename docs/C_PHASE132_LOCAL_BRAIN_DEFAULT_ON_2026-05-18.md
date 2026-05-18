# C-PHASE 132 - Local-Brain Default-On

Date: 2026-05-18
Branch: `claude-implementation/c-phase-132-local-brain-default-on`
Decision basis: `I-Reframe` and the C-PHASE 132 precision addendum.

## Goal

Move Wevito closer to being a local-first runtime AI assistant by making the product shell visibly and audibly accountable for its local brain status. This phase does not download model weights, does not call hosted AI, and does not make Ollama availability a merge gate.

## Implemented

- Added `LocalBrainHeartbeatService`.
  - Probes the local Ollama endpoint through `LocalRuntimeProbeService`.
  - Respects the KillSwitch before probing or writing state.
  - Respects Quiet/PetOnly runtime supervisor status by reporting dormant instead of probing.
  - Writes `local_brain_heartbeat` audit-ledger packets at most once per 10 minutes.
  - Sets `DidUseLocalModel=true` only when the local runtime probe succeeds.

- Added `LocalBrainBenchmarkSmokeTest`.
  - Runs five deterministic prompts through an injected `IModelAdapter`.
  - Refuses hosted provider IDs via `ModelProviderModeService.IsHostedProviderId`.
  - Reports degraded deterministic fallback when no real local model is used.
  - Writes `local_brain_smoke_test_completed` evidence with honest flags.

- Wired shell startup and runtime heartbeat.
  - `ShellCoordinator` creates the heartbeat service with loopback-only runtime probe wiring.
  - Startup records one local-brain heartbeat after the existing Ollama bootstrap probe.
  - Timer ticks refresh the local-brain status without hammering Ollama or the ledger.

- Added a home-panel local brain badge.
  - `BRAIN ready` when the local runtime is reachable.
  - `BRAIN offline` when Ollama is unavailable and deterministic fallback is active.
  - `BRAIN quiet` when runtime supervisor mode makes background probing dormant.
  - `BRAIN paused` when Stop Everything/KillSwitch blocks the probe path.

- Added plain-language support for:
  - `local_brain_heartbeat`
  - `local_brain_smoke_test_completed`
  - `local_brain_onboarding_panel_shown`

- Added hosted-adapter production composition guard.
  - `ShellRefusesHostedAdapterAtRuntimeTests.CompositionRootHasNoHostedAdapter` verifies the shell composition root does not instantiate Anthropic/OpenAI/Gemini adapters and does route through local model infrastructure.

## Local Runtime Observation

Ollama loopback probe on this machine timed out:

```text
OLLAMA_UNREACHABLE: WebException: The operation has timed out.
```

That is acceptable for this phase. The shipped behavior is degraded/offline local-brain status, not a false-ready state.

Live shell startup proof wrote this audit row after launch:

```text
packet_kind=local_brain_heartbeat
status=Offline
did_use_network=0
did_use_hosted_ai=0
did_use_local_model=0
did_mutate=0
summary=Local brain offline: Ollama probe failed: TaskCanceledException.
```

## Safety Boundaries

- No hosted AI call was added.
- No model weights were downloaded.
- No automatic Ollama install or `ollama pull` was run.
- No sprite/runtime assets were mutated.
- No asset prep was run.
- KillSwitch blocks public write-capable local-brain paths before probing/writing.
- Evidence flags distinguish local runtime use from deterministic fallback.

## Validation

```text
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalBrain|PlainLanguage|ShellRefusesHosted"
Passed: 196/196

dotnet build .\vnext\Wevito.VNext.sln
Passed

dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
Passed: 981/981

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
Passed

git diff --check
Passed
```

## Stop-Gate Checklist

- [x] No composition-root path instantiates a hosted adapter.
- [x] Heartbeat packet is rate-limited to one packet per 10 minutes.
- [x] Badge/status does not report ready when the local probe fails.
- [x] Smoke test can report degraded deterministic fallback instead of silently pretending a local model answered.
- [x] All new packet kinds are known to `PlainLanguageExplainer`.
- [x] All validation commands passed.

## Next Phase

C-PHASE 133 should wire the autonomous loop to bounded, default-off scopes: sprite-repair triage and audit-ledger cleanup. It must keep mutation behind review, backup, hash, post-proof, and rollback gates.
