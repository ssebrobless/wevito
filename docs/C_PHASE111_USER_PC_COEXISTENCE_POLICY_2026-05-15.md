# C-PHASE 111 — User-PC Coexistence Policy

## Goal

Implement the Q19 coexistence layer so Wevito can stay launched while yielding helper/AI work to the user's active PC session.

## Scope

- Added configurable coexistence triggers for fullscreen apps, meeting/recording app list, non-Wevito CPU pressure, and network saturation.
- Added Do Not Disturb schedule state plus a one-click overlay quick toggle.
- Added workload tiers: `UserForeground`, `Maintenance`, and `Experimentation`.
- Extended budget reservation to honor foreground reserve and adaptive borrowing.
- Forced pets into calm `Sleep` visuals while helper work is yielded by DND/coexistence, then clears naturally when gates clear.
- Added plain-language coverage and audit packet kinds for coexistence events.

## Implemented

- `CoexistenceTriggerService`
  - Default app list: `zoom.exe`, `teams.exe`, `obs64.exe`, `discord.exe`.
  - Trigger packet kinds: `coexistence_trigger_fired`, `coexistence_trigger_cleared`, `pet_animation_forced`.
  - Writes append-only coexistence JSONL evidence at `%LOCALAPPDATA%/Wevito/audit/coexistence-events.jsonl`.
  - Seeds default app-list settings at `%LOCALAPPDATA%/Wevito/settings/coexistence-app-list.json`.
- `DoNotDisturbScheduleService`
  - Schedule setting, quick-toggle setting, and DND state evaluation.
  - Seeds `%LOCALAPPDATA%/Wevito/settings/dnd-schedule.json`.
  - Overlay header now has a `DND` quick toggle that turns one-hour DND on/off.
- `WorkloadTierService`
  - Foreground reserve: 60%.
  - Maintenance reserve: 30%.
  - Experimentation reserve: 10%.
  - Experimentation yields to maintenance and foreground.
- Runtime wiring
  - `RuntimeSupervisorService.Evaluate` accepts coexistence and DND states.
  - `ShellCoordinator` applies coexistence/DND quieting without stopping the pet game.
  - Settings panel exposes individual toggles for app-list, CPU, network, DND schedule, and existing fullscreen quieting.

## Safety Boundaries

- No hosted AI calls.
- No model downloads.
- No sprite or asset mutation.
- KillSwitch blocks coexistence/DND evidence writes and default settings-file writes.
- Pet-game simulation remains active; only helper/background work yields.
- Forced sleep visuals are transient and rate-limited to avoid audit spam.

## Stop Gates

- Trigger bypasses KillSwitch: false.
- Pet animation force-state leaks beyond DND/quiet: false; it is transient and recomputed per tick.
- Default app list includes hosted-AI clients: false.
- Foreground tier can be starved: false; workload-tier tests reserve foreground first.
- New packet kind missing from `PlainLanguageExplainer`: false.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "Coexistence|DoNotDisturb|WorkloadTier|PlainLanguage"`: passed, 101/101.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 719/719.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 112 should continue only after this PR is reviewed/merged. The next likely step is the plan's next approved local-first AI capability phase; do not start it from this branch.

