# C-PHASE 138 - Local Brain Runtime Setup Drilldown

## Goal

Make the existing BRAIN badge actionable without changing Wevito's local-first safety posture. The badge now opens a read-only local brain status panel that explains current runtime state, loopback probe details, recommended Ollama setup commands, keep-alive guidance, and fallback installer text.

## Scope

Implemented:
- Made the BRAIN badge clickable from the home panel.
- Added `LocalBrainStatusPanelWindow` as a text-guidance panel.
- Added `LocalBrainStatusPanelService` to own status snapshot, refresh rate-limiting, setup command metadata, and audit packet writes.
- Exposed `LocalBrainHeartbeatService.LastObserved` so the panel can read the last heartbeat without mutating runtime state.
- Added `local_brain_status_panel_shown` and `local_brain_setup_instruction_copied` to `PlainLanguageExplainer.KnownPacketKinds`.
- Added tests for kill switch behavior, refresh rate limiting, copy packet honesty, command-copy-only behavior, and plain-language coverage.

Not implemented by design:
- No Ollama, winget, curl, installer, or model process is launched by Wevito.
- No model weights are downloaded.
- No hosted AI calls are added.
- No capability flags are changed.
- The fallback download URL is displayed as selectable text only.

## UX Contract

The BRAIN badge opens a panel with:
- current state: ready, offline, quiet, paused, or starting
- last heartbeat timestamp, endpoint, model, and reason
- last refresh/probe detail when manually refreshed
- recommended runtime: `Ollama loopback http://127.0.0.1:11434`
- recommended model: `qwen3:8b`
- keep-alive guidance: Ollama unloads models after about 5 minutes by default; use `OLLAMA_KEEP_ALIVE=30m` or `-1` if intentionally keeping the model loaded
- copy-only setup commands:
  - `winget install -e --id Ollama.Ollama`
  - `ollama serve`
  - `ollama pull qwen3:8b`
  - `setx OLLAMA_KEEP_ALIVE 30m`

## Safety Boundaries

- `KillSwitchService.IsActive()` is checked before every panel action that writes audit state.
- Copy buttons only write to clipboard after the service returns a successful copy result.
- Copy audit packets use honest flags: `did_use_network=false`, `did_use_hosted_ai=false`, `did_use_local_model=false`, `did_mutate=false`.
- Refresh uses the existing loopback-only probe path and is rate-limited to once per 10 seconds.
- The panel does not call `Process.Start` and does not execute the copied commands.

## Validation

Passed:
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "LocalBrain|PlainLanguage"` - 219/219
- `dotnet build .\vnext\Wevito.VNext.sln` - passed
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` - 1022/1022
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` - passed
- `git diff --check` - passed

## Stop Gates

- No hosted AI call added or observed in this phase.
- No silent network access added; refresh remains limited to the existing loopback Ollama probe.
- No silent file/tool/code/asset mutation added.
- No capability flags changed.
- KillSwitch blocks panel audit writes.
- No process launch path for `ollama`, `winget`, or `curl` was added.

## Next Phase

Auto-continue is disabled for C-PHASE 138. Wait for user review before starting any later local brain or autonomous-operation phase.
