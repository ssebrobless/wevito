# Wevito Visual QA Cockpit

Date: 2026-05-14
Branch: feature/visual-qa-cockpit

## Goal

Create a dev-only controller for testing the pet-simulator side of Wevito without guessing through the normal UI. The cockpit gives us a visible way to select a pet slot, spawn or replace pets, delete pets without ghosts, trigger care/actions, force animation loops, and inspect the runtime asset source feeding the visible pet.

## Current Shape

```text
╔══════════════════════════════╗
║ Wevito Shell                 ║
║  ┌────────────┐              ║
║  │ pet state  │              ║
║  └─────┬──────┘              ║
║        ▼                     ║
║  DevControlServer            ║
║  named pipe:                 ║
║  wevito-vnext-dev-control    ║
╚════════╦═════════════════════╝
         ║ JSON request/response
╔════════▼═════════════════════╗
║ Wevito Visual QA Cockpit     ║
║  slot 1 │ slot 2 │ slot 3    ║
║  spawn/delete/action/roam    ║
║  force animation/source info ║
╚══════════════════════════════╝
```

## Implemented

- Added `DevControlContracts.cs` for snapshot, slot, spawn/replace, delete, selected-action, and roam commands.
- Added `VisualQaContracts.cs` for animation forcing and future QA expansion commands.
- Added `DevControlServer` as a dev-only named-pipe command surface owned by the Shell.
- Added ShellCoordinator dev-control handlers for snapshot, delete, spawn/replace, selected action, roam, forced animation, clear forced animation, and asset source lookup.
- Added `Wevito.VNext.DevController`, a WPF cockpit window with three selectable pet slots and direct controls.
- Updated `tools/build-vnext.ps1` so debug publish emits both the Shell and Dev Controller.
- Added `tools/export_pet_runtime_contact_sheets.py` for broad visual review of all runtime pet rows.

## Safety Boundaries

- The cockpit does not click Wevito UI.
- The cockpit does not edit sprite PNGs, prop anchors, runtime manifests, or source boards.
- Shell remains the only owner of pet state and persistence.
- Delete removes a selected active pet without creating a ghost, memorial, or visual remnant.
- Spawn/replace checks that runtime idle sprites exist before creating a pet.
- The dev-control server starts only when dev tools are enabled.

## Validation

```text
dotnet build .\vnext\Wevito.VNext.sln
Result: passed

dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
Result: passed, 603 / 603

powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
Result: passed
```

Published outputs:

- `vnext/artifacts/shell/Wevito.VNext.Shell.exe`
- `vnext/artifacts/dev-controller/Wevito.VNext.DevController.exe`

Live smoke:

- Launched Shell from the new artifact build.
- Launched Dev Controller from the new artifact build.
- Sent `GetSnapshot` to `wevito-vnext-dev-control`.
- Response returned `success=True` with `slots=3`.

## Known Limitations

- Empty slots are represented through the current compact active-pet list. Deleting slot 1 shifts later pets forward rather than preserving a stable empty hole.
- Roam currently starts a selected-pet roam state but is not yet a full ping-pong frame-scrub proof runner.
- Animation forcing pins a family loop but does not yet expose exact frame stepping in the Shell render path.
- Visual overlays, batch matrix runner, issue tagging from inside the cockpit, save sandbox reset buttons, and soak observer are planned but not fully implemented in this slice.

## Next Cockpit Improvements

- Add stable three-slot identity so empty slots can remain empty until explicitly filled.
- Add exact frame scrubber and playback speed control.
- Add debug overlays for sprite bounds, alpha bounds, ground line, taskbar line, and anchor points.
- Add in-cockpit issue tagging that writes JSON and Markdown evidence packets.
- Add save sandbox reset modes, including fresh egg-choice flow.
- Add batch matrix runner for selected species/color/stage/gender/action combinations.
