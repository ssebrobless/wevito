# C-PHASE 124: Pet Behavior + Pet AI Awareness

## Goal

Make the pet sim more behavior-aware while keeping Wevito's AI side reactive, local, and read-only for pet state.

## Scope

- Added goal-based pet wandering hooks in Godot:
  - `wander`
  - `seek_food_zone`
  - `seek_water_zone`
  - `rest_zone`
  - `follow_cursor`
- Added bounded cursor curiosity:
  - 80px personal-space trigger.
  - 40px close-range happy animation trigger.
  - 10 second cooldown to avoid packet spam.
- Added invisible goal-zone targets in `main_scene.gd` so pets can move toward food, water, rest, or cursor-interest areas without adding new visible habitat clutter.
- Added a Godot threshold signal for critical pet stats without changing stat math or care side effects.
- Added a local read-only `PetStateTool` that returns pet state only after passing through `UnifiedPolicyService`.
- Added append-only `PetInteractionLogger` for `%LOCALAPPDATA%/Wevito/audit/pet-interactions.jsonl`.
- Added `PetStateContextInjector` for next-turn pet-state FYI lines only. It does not auto-message the user.
- Added plain-language coverage for:
  - `pet_goal_changed`
  - `pet_interaction_logged`
  - `pet_cursor_curiosity_fired`
  - `pet_state_tool_invoked`

## Safety Boundaries

- No sprite PNGs, source boards, runtime assets, or prop anchors were changed.
- No hosted AI calls were added.
- No local model calls were added.
- No auto-messaging was added. Pet-state context is only available for a later user chat turn.
- `PetStateTool` calls `UnifiedPolicyService.EvaluatePetStateRead` before returning state.
- `PetStateContextInjector` rejects stale snapshots and respects `ai_mentions_pet_state=false`.
- Cursor curiosity is rate-limited to one trigger every 10 seconds per pet instance.
- Godot stat semantics were preserved: lower hunger/hydration/energy means greater need.

## Validation

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "PetStateTool|PetInteractionLogger|PetStateContextInjector|PlainLanguage"
```

Passed: 183 / 183.

```powershell
dotnet build .\vnext\Wevito.VNext.sln
```

Passed: 0 warnings, 0 errors.

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
```

Passed: 917 / 917.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Passed.

```powershell
git diff --check
```

Passed with expected line-ending warnings only.

Godot CLI validation is expected to remain unavailable in this environment unless `godot` is installed or exposed on `PATH`.

## Next Phase

C-PHASE 124 is auto-continue eligible when validation passes. If there is no next prompt after C-PHASE 124, the next safe step is a post-phase status check and updated handoff.
