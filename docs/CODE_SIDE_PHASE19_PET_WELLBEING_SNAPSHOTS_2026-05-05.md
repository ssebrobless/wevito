# Code-Side Phase 19: Pet Wellbeing Snapshots

Date: 2026-05-05

## Scope

Add a derived, non-persistent wellbeing snapshot layer for future helper-agent context, debug views, and simulation readiness.

```text
PetActor durable state
   |
   +-- needs/vitals
   +-- habits
   +-- personality
   +-- conditions
   +-- statuses
   |
   v
PetWellbeingInterpreter
   |
   v
PetWellbeingSnapshot
```

This phase does not change gameplay behavior or persistence.

## Files Changed

- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Contracts\Models.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\src\Wevito.VNext.Core\PetWellbeingInterpreter.cs`
- `C:\Users\fishe\.codex\worktrees\36d6\wevito\vnext\tests\Wevito.VNext.Tests\PetWellbeingInterpreterTests.cs`

## Ownership Map

```text
PetSimulationEngine
   owns durable pet state changes

PetWellbeingInterpreter
   owns derived readable interpretation only

Shell / PET TASKS / future agents
   may read snapshots
   must not mutate pet truth through snapshots
```

## New Contracts

- `PetWellbeingUrgency`
  - `Stable`
  - `Watch`
  - `NeedsCare`
  - `Critical`
- `PetDriveFamily`
  - `SelfMaintenance`
  - `SafetyAvoidance`
  - `SocialConnection`
  - `Rest`
  - `Exploration`
- `PetEmotionChannel`
  - `Relief`
  - `Attachment`
  - `Curiosity`
  - `Agitation`
  - `Exhaustion`
  - `Threat`
- `PetWellbeingSnapshot`

## Snapshot Contents

- pet identity
- species / age / gender / color
- urgency
- dominant drive
- dominant emotion channel
- human-readable summary
- need pressure map
- personality descriptors
- active condition IDs
- current statuses

## Persistence Boundary

Persist:

- `PetActor` durable truth already stored by app state

Rebuild:

- `PetWellbeingSnapshot`
- need pressure map
- dominant drive/emotion
- summary text
- descriptors

This follows the life-sim rule: persist truth, rebuild cheap debug/context views.

## AI Integration Point

Future helper pets / tool agents can use `PetWellbeingSnapshot` as safe context:

- which pet needs attention
- why a care action may be useful
- how personality may color communication
- whether a condition is driving urgency
- whether current status and displayed animation agree with debug truth

## Tests Added

- stable pet reports low urgency and relief
- low-energy pet maps to rest/exhaustion or critical threat
- active conditions can override dominant drive to safety
- personality traits expose concise descriptors for agent context

## Validation

- `dotnet build vnext\Wevito.VNext.sln` passed with `0` warnings.
- `dotnet test vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build` passed `83 / 83`.

## Next Safe Step

Expose the wellbeing snapshot in the Shell as read-only debug/helper context. Do not make it interactive yet.
