# C-PHASE 110 - Pet/AI Isolation Contract

## Goal

Implement the Q18 isolation contract so the desktop pet game remains the protected foreground experience and AI/background work yields under FPS pressure, RAM pressure, pet interaction, image-generation idle rules, and child-process crash recovery.

Decision IDs:

- Q18-F3: 60 FPS target, strict 30 FPS floor.
- Q18-P4: tiered AI priority and 5-second pet-interaction preemption.
- Q18-R4: RAM pressure cascade with sacred 1 GB pet-game reserve.
- Q18-G3+G4: autonomous image generation only when pets are idle; explicit user trigger bypasses.
- Q18-C4: child-process watchdog with 1s/5s/30s backoff and 3-restarts-in-5-min cap.
- Q18-I3: pet input preempts AI background work.
- I-Reframe: Wevito is a local AI assistant; pet visuals remain the cosmetic surface.

## Implemented

- Added `PetFpsSample` to the pet-agent contracts and `IdleSince` to `PetActor`.
- Added `PetFpsMonitorService` with 250ms sampling, hourly snapshot summaries, below-30 FPS violation packets, and a 50% throttle window for 5 minutes.
- Added `RamPressureCascadeService` with `<3 GB`, `<2 GB`, `<1.5 GB`, and `<1 GB` tiers. The result model always reports `pet_reserved_min_touched=false`.
- Added `ImageGenIdleGuardService` to block background image generation until the selected pet has been idle for at least 10 seconds, while preserving explicit user-triggered bypass.
- Added `UserInteractingWithPetState` and wired `AutonomousOperationsLoop` to block with `user_interacting_with_pet=true` during the 5-second interaction window.
- Added `WevitoProcessWatchdogService` in the Broker project with crash observations, restart backoff, max-restart cap, and kill-switch-safe behavior.
- Added all new packet kinds to `PlainLanguageExplainer.KnownPacketKinds`.

## Safety Boundaries

- No hosted AI calls.
- No model downloads.
- No sprite, asset, source-board, or runtime PNG mutation.
- No live process killing or restarting during tests.
- No hidden RAM/model unload calls are performed in this phase; cascade results are explicit, testable decisions that later process/model adapters can execute.
- Godot FPS/input emission is not faked. This phase adds the C# receiver/enforcement seam; a later bridge phase should connect actual Godot IPC samples.

## Validation

Commands run:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetFps|RamPressure|WevitoProcessWatchdog|ImageGenIdleGuard|PlainLanguage"
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetFps|RamPressure|WevitoProcessWatchdog|ImageGenIdleGuard|PlainLanguage|RuntimeSupervisor|AutonomousOperationsLoop"
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Results:

- Required focused filter passed: 69/69.
- Expanded isolation/supervisor focused filter passed: 87/87.
- `dotnet build` passed with 0 warnings and 0 errors.
- Full tests passed: 634/634.
- `build-vnext.ps1 -SkipAssetPrep -SkipTests` passed.

## Stop Gates

- Pet game can be RAM-cascaded: false. The cascade service never marks the pet reserve as touched.
- Watchdog restart bypasses kill switch: false. Kill switch prevents restart requests.
- Soak test fails below 30 FPS: false. Simulated 10-minute 100% loop stays above the floor and never emits a violation.
- User interaction can be set by non-Godot path: false in the public contract. The only state-entering method is `EnterFromGodotPetInput`.
- Background image-gen can bypass idle guard: false. Background calls require 10 seconds of pet idle time.
- Packet kinds missing from `PlainLanguageExplainer`: false. All new packet kinds are mapped.

## Next Phase Notes

C-PHASE 111 can build on this contract for broader user-PC coexistence. The most important remaining bridge seam is live Godot IPC emission for FPS samples and pet input events; this phase intentionally avoided fabricating that bridge.
