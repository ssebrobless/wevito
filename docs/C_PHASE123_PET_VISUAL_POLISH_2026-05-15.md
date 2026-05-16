# C-PHASE 123: Pet Visual Polish

## Goal

Make the pet surface feel more alive after the sprite cleanup rungs without changing pet simulation truth, sprite PNGs, runtime asset folders, AI/tool routing, or autonomous behavior.

## Scope

- Added lightweight Godot pet visual polish in `scripts/pet.gd`:
  - animation transition cross-fade via a temporary previous-frame sprite,
  - idle micro-behavior hooks for Blink, LookAround, EarTwitch, and TailWag,
  - visual-only particle effects for sleep, happy, eat, bathe, sad, and walk,
  - user window-shake reaction that plays sad + tears for 2 seconds,
  - fixed an existing wandering-branch indentation issue so wandering movement remains valid.
- Added `scripts/particle_effects_layer.gd` and a scene-level `ParticleEffectsLayer` node.
- Added `Camera2D` smoothing setup and real-user window-shake detection in `scripts/main_scene.gd`.
- Added `PetMicroBehavior` and `PetMicroBehaviorTriggered` contracts.
- Added `PetVisualPolishLogger` with one-per-minute aggregate audit flushing.
- Added settings defaults and Tool Popup checkboxes for:
  - animation blending,
  - position interpolation,
  - idle micro-behaviors,
  - particle effects,
  - window shake reaction.
- Added plain-language coverage for:
  - `pet_micro_behavior`,
  - `pet_particle_effect_played`.

## Safety Boundaries

- No sprite PNGs were mutated.
- No runtime/source art folders were mutated.
- No model calls, web calls, training, asset generation, or tool execution behavior was added.
- Pet polish is visual-only and does not change hunger, thirst, health, personality, action outcomes, or AI workload.
- Window-shake reaction is only triggered from focused real window-position deltas; it is not invoked by AI work.
- Audit packets use `did_use_network=false`, `did_use_hosted_ai=false`, `did_use_local_model=false`, and `did_mutate=false`.

## Validation

- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetVisualPolishLogger|PlainLanguage"` passed: 169 / 169.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetFps" --no-build` passed: 4 / 4.
- `dotnet build .\vnext\Wevito.VNext.sln` passed with 0 warnings and 0 errors.
- `dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed: 900 / 900.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests` passed.
- `git diff --check` passed with line-ending warnings only.

## Godot Validation Note

The `godot` executable was not available in PATH and no `GODOT_*` environment override was present in this worktree session, so `godot --headless --path . --script scripts/tests/pet_test.gd` could not be run here. The edited GDScript was kept narrow, ASCII-only, and limited to runtime visual state; .NET and publish validation remained green.

## Stop-Gate Review

- Pet FPS gate: no failing FPS evidence; existing PetFps tests passed. Live Godot FPS soak was not available because Godot CLI was unavailable.
- Particle GPU gate: no PNG or heavy shader assets were added; particles are capped at 18 active nodes per pet layer.
- Micro-behavior audit gate: `PetVisualPolishLogger` rate-limits aggregate ledger writes to once per minute.
- Position interpolation/collision gate: no collision or world-position ownership was changed; current movement bounds remain authoritative.
- Window-shake gate: reaction requires focused user window movement and is disabled during startup guard, automation, and automation scenarios.
- Plain-language gate: both new packet kinds are in `PlainLanguageExplainer.KnownPacketKinds` and have tests.

## Next Phase

C-PHASE 124 can proceed after PR merge. It should keep pet sim behavior and AI awareness separate: pets stay visually normal, while AI receives bounded pet-state context through explicit tools/loggers.
