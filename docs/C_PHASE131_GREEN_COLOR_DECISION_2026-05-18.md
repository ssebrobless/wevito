# C-PHASE 131 - Green Color Decision

## Goal

Lock the v1 starter egg surface to the six cleaned runtime colors and reserve green for a later phase.

## Scope

- Implemented the precision addendum's Branch B decision for C-PHASE 131.
- Kept `species.json` and all sprite/runtime PNGs unchanged.
- Updated WPF starter egg catalog fallback, shared starter egg manifest, and Godot fallback data to agree on six visible choices.

## Implemented

- `vnext/content/starter_eggs.json` now emits six enabled entries: red, orange, yellow, blue, indigo, violet.
- `StarterEggCatalog` fallback now matches the six-entry v1 manifest.
- `HomePanelWindow` starter egg copy no longer promises a full ROYGBIV prompt and clearly says green returns in a future phase.
- Godot's `FALLBACK_STARTER_EGGS` now matches the six-entry manifest path in case the shared manifest is unavailable.
- Starter egg tests now assert the six-color v1 contract and that green is omitted rather than rendered disabled.

## Safety Boundaries

- No sprite PNGs were changed.
- No `species.json` changes were made.
- No hosted AI calls, model calls, network fetches, training, or asset prep were introduced.
- Green is not enabled without installed green runtime sprites.

## Stop-Gate Checklist

- [x] Green is not enabled while `sprites_runtime/.../green/` is absent.
- [x] `StarterEggCatalog` and `starter_eggs.json` agree on the six v1 starter choices.
- [x] Godot fallback starter egg data agrees with the shared six-color manifest.
- [x] WPF prompt renders from the six-entry `StarterEggCatalog.Eggs` collection.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "StarterEgg"`: passed, 11/11.
- Manifest stop-gate check: passed, six colors and `hasGreen=False`.
- `dotnet build .\vnext\Wevito.VNext.sln`: passed.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build`: passed, 966/966.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests`: passed.

## Next Phase

C-PHASE 132 should handle the local AI/runtime truth surface and must keep the same local-only/no-hosted-AI invariants.
