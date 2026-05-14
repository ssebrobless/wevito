# Wevito Dev Control Viewer Plan

## Goal

Build a small dev-only companion program that lets us visually QA every Wevito pet slot, species, gender, color, life stage, and core interaction without hand-driving the normal UI. The tool must make sprite/gameplay testing faster while keeping Wevito Shell as the only owner of pet state.

## Shape

```text
╔════════════════════════════════════════════════════════════════════╗
║ Wevito Shell                                                       ║
║                                                                    ║
║  ┌───────────────┐      ┌──────────────────────┐                  ║
║  │ Pet simulator │◀────▶│ ShellCoordinator     │                  ║
║  │ state/render  │      │ dev-control methods  │                  ║
║  └───────────────┘      └──────────┬───────────┘                  ║
║                                     │ WPF Dispatcher only           ║
║                          ┌──────────▼───────────┐                  ║
║                          │ DevControlServer     │ dev builds only  ║
║                          │ named pipe JSON API  │                  ║
║                          └──────────┬───────────┘                  ║
╚═════════════════════════════════════│══════════════════════════════╝
                                      │ request/response snapshots
╔═════════════════════════════════════▼══════════════════════════════╗
║ Wevito Dev Controller                                               ║
║                                                                    ║
║  ┌────────────┐ ┌────────────┐ ┌────────────┐                     ║
║  │ Pet slot 1 │ │ Pet slot 2 │ │ Pet slot 3 │  selected target     ║
║  └────────────┘ └────────────┘ └────────────┘                     ║
║                                                                    ║
║  Delete | Add/Replace | Stage ▼ | Color ▼ | Species ▼ | Gender ▼   ║
║                                                                    ║
║  Feed | Water | Rest | Play | Groom | Bath | Medicine | Doctor      ║
║  Home | Roam 10s                                                    ║
╚════════════════════════════════════════════════════════════════════╝
```

## Non-Negotiable Safety Rules

- The controller must not click the Wevito UI.
- The controller must not edit save files, sprite PNGs, runtime manifests, or databases directly.
- The controller must not create ghost pets when deleting a slot.
- The controller must not bypass Wevito validation for species, color, gender, or life stage.
- The controller must be disabled in non-dev/release builds unless explicitly enabled by a local dev flag.
- Wevito Shell remains the single source of truth; the controller only displays snapshots and sends commands.
- Every command returns the latest snapshot so the UI cannot drift from Wevito state.

## Phase 1: Contracts

Add dev-control contracts to `vnext/src/Wevito.VNext.Contracts/`.

Preferred file:

```text
vnext/src/Wevito.VNext.Contracts/DevControlContracts.cs
```

Define message types:

```csharp
public static class DevControlCommandTypes
{
    public const string GetSnapshot = "GetSnapshot";
    public const string DeletePet = "DeletePet";
    public const string SpawnOrReplacePet = "SpawnOrReplacePet";
    public const string ApplyAction = "ApplyAction";
    public const string Roam = "Roam";
}

public sealed record DevControlCommandEnvelope(
    string CommandType,
    JsonElement Payload);

public sealed record DevControlResponseEnvelope(
    bool Success,
    string Message,
    DevControlSnapshot Snapshot);

public sealed record DevControlSnapshot(
    IReadOnlyList<DevControlPetSlotSnapshot> Slots,
    DevControlOptions Options,
    DateTimeOffset CapturedAtUtc);

public sealed record DevControlPetSlotSnapshot(
    int SlotIndex,
    Guid? PetId,
    string DisplayText,
    string? Name,
    string? SpeciesId,
    string? Gender,
    string? ColorVariant,
    string? LifeStage,
    bool IsEmpty,
    bool IsDead,
    string? BehaviorState,
    string? AnimationState);

public sealed record DevControlOptions(
    IReadOnlyList<string> SpeciesIds,
    IReadOnlyList<string> LifeStages,
    IReadOnlyList<string> Genders,
    IReadOnlyList<string> ColorVariants,
    IReadOnlyList<DevControlActionOption> Actions);

public sealed record DevControlActionOption(string ActionId, string Label);
```

Define command payloads:

```csharp
public sealed record DevControlGetSnapshotRequest();

public sealed record DevControlDeletePetRequest(int SlotIndex, Guid? ExpectedPetId);

public sealed record DevControlSpawnOrReplacePetRequest(
    int SlotIndex,
    string SpeciesId,
    string LifeStage,
    string Gender,
    string ColorVariant,
    bool ReplaceIfOccupied);

public sealed record DevControlApplyActionRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    string ActionId);

public sealed record DevControlRoamRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    int DurationSeconds);
```

Acceptance checks:

- Contract serialization round-trips with `JsonDefaults.Options`.
- Empty slots render as `DisplayText = "N/A"`.
- Slot indices are always `0..2`.

## Phase 2: Shell Dev-Control Server

Add a dedicated server in Shell:

```text
vnext/src/Wevito.VNext.Shell/DevControlServer.cs
```

Use a separate named pipe from the broker:

```text
wevito-vnext-dev-control
```

Server behavior:

- Starts only when the Shell is running as a development build.
- Accepts one request per line as JSON.
- Dispatches all state-changing work onto the WPF Dispatcher.
- Never mutates state directly; calls `ShellCoordinator` dev-control methods.
- Returns `DevControlResponseEnvelope` for every request.
- Handles malformed requests with `Success = false` and a current snapshot when possible.
- Logs failures without crashing Shell.

Integration point:

```text
vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs
```

Start server during Shell initialization, near existing broker/client startup. Stop it during shutdown/dispose.

## Phase 3: ShellCoordinator Dev API

Add internal methods to `ShellCoordinator`:

```csharp
internal DevControlSnapshot GetDevControlSnapshot();

internal Task<DevControlResponseEnvelope> DeletePetForDevControlAsync(
    DevControlDeletePetRequest request);

internal Task<DevControlResponseEnvelope> SpawnOrReplacePetForDevControlAsync(
    DevControlSpawnOrReplacePetRequest request);

internal Task<DevControlResponseEnvelope> ApplyActionForDevControlAsync(
    DevControlApplyActionRequest request);

internal Task<DevControlResponseEnvelope> StartRoamForDevControlAsync(
    DevControlRoamRequest request);
```

Snapshot rules:

- Always return exactly three slots.
- Filled slot text:

```text
<name>
<species> | <gender> | <color> | <life stage>
```

- Empty slot text:

```text
N/A
```

Delete rules:

- Remove only the selected slot.
- Do not create a ghost.
- Do not create a memorial/remnant.
- Persist and re-render after deletion.
- If `ExpectedPetId` is supplied and does not match the selected slot, reject the command.

Spawn/replace rules:

- Empty slot: create the requested pet.
- Occupied slot with `ReplaceIfOccupied = false`: reject with message `Replacement confirmation required.`
- Occupied slot with `ReplaceIfOccupied = true`: delete the current pet without ghost, then spawn requested pet.
- Enforce a maximum of three active pets.
- Validate that the requested runtime sprite folder exists for species/life stage/gender/color before spawn.
- If the requested color is structurally unavailable, reject the command instead of spawning an invisible pet.

Action rules:

- Actions apply only to the selected pet, not all active pets.
- Supported action IDs:

```text
feed
water
rest
play
groom
bath
medicine
doctor
home
```

- Add or expose a single-pet action path in `PetSimulationEngine` instead of reusing the current all-pets action path.
- `home` sends the selected pet back to its assigned idle/home position.
- Action command persists and re-renders after applying.

Roam rules:

- `roam` applies only to the selected pet.
- Duration defaults to 10 seconds and clamps to a safe range, for example `1..30`.
- During roam, the selected pet walks back and forth inside the visible stage at normal animation speed.
- After roam expires, the pet returns to its home/idle position.
- Roam state is transient and must not persist into saves.

Implementation hint:

Add a small dev motion override dictionary:

```csharp
private readonly Dictionary<Guid, DevRoamOverride> _devRoamOverrides = new();
```

Use it during tick/render calculation to force the selected pet into a visible walk cycle, then remove it when expired.

## Phase 4: Dev Controller App

Add a new WPF project:

```text
vnext/src/Wevito.VNext.DevController/Wevito.VNext.DevController.csproj
vnext/src/Wevito.VNext.DevController/App.xaml
vnext/src/Wevito.VNext.DevController/App.xaml.cs
vnext/src/Wevito.VNext.DevController/MainWindow.xaml
vnext/src/Wevito.VNext.DevController/MainWindow.xaml.cs
vnext/src/Wevito.VNext.DevController/DevControlClient.cs
vnext/src/Wevito.VNext.DevController/DevControllerViewModel.cs
```

UI layout:

```text
┌──────────────────────────────────────────────────────────────┐
│ Wevito Dev Controller                         Connected/Retry │
├────────────────┬────────────────┬────────────────────────────┤
│ Slot 1          │ Slot 2          │ Slot 3                     │
│ name/species... │ N/A             │ name/species...            │
└────────────────┴────────────────┴────────────────────────────┘

Target controls:
Delete | Add/Replace | Stage ▼ | Color ▼ | Species ▼ | Gender ▼

Actions:
Feed | Water | Rest | Play | Groom | Bath | Medicine | Doctor | Home | Roam

Status:
Last command result / validation message
```

View model behavior:

- Poll snapshot every `500ms` while connected.
- Also refresh immediately after each command response.
- Selecting a slot makes it the command target.
- Add on empty slot sends `ReplaceIfOccupied = false`.
- Add on occupied slot shows confirmation dialog:

```text
Replace this pet?
This will delete the selected pet without creating a ghost, then spawn the selected test pet.
```

- If confirmed, send `ReplaceIfOccupied = true`.
- If Wevito is not running, show disconnected status and keep retrying.
- Never launch Wevito automatically in the first version.

## Phase 5: Build and Packaging

Add the project to:

```text
vnext/Wevito.VNext.sln
```

Update:

```text
tools/build-vnext.ps1
```

Publish outputs:

```text
vnext/artifacts/shell/Wevito.VNext.Shell.exe
vnext/artifacts/dev-controller/Wevito.VNext.DevController.exe
```

The existing `-SkipAssetPrep` path must remain safe and must not mutate sprites.

## Phase 6: Tests

Add tests under:

```text
vnext/tests/Wevito.VNext.Tests/
```

Suggested files:

```text
DevControlContractsTests.cs
DevControlCommandHandlerTests.cs
DevControllerViewModelTests.cs
PetSimulationSingleActionTests.cs
```

Required test cases:

- Snapshot always has exactly three slots.
- Empty slot displays `N/A`.
- Delete selected pet removes it without creating ghost state.
- Spawn into empty slot creates requested species/life stage/gender/color.
- Spawn rejects unavailable runtime color combinations.
- Replace occupied slot is rejected without confirmation.
- Replace occupied slot succeeds with confirmation.
- Single-pet action changes only the selected pet.
- Roam sets a temporary selected-pet walking override.
- Roam expires and returns pet to home/idle behavior.
- Controller view model does not send replace command until confirmation succeeds.
- Controller reconnect behavior does not crash when Wevito is not running.

## Phase 7: Manual Visual QA Script

Add a short report doc after implementation:

```text
docs/WEVITO_DEV_CONTROLLER_VISUAL_QA_2026-05-14.md
```

Manual pass:

1. Build and launch Wevito Shell.
2. Launch Dev Controller.
3. Confirm all three slot boxes match the Wevito pets.
4. Delete slot 1; confirm no ghost appears and slot reads `N/A`.
5. Add a goose/baby/female/red into slot 1.
6. Replace slot 1 with snake/adult/male/blue after confirmation.
7. Trigger each action and visually verify the selected pet responds.
8. Trigger roam and confirm the selected pet walks back and forth for about 10 seconds, then returns.
9. Repeat a quick sample for at least one bird, one mammal, one reptile/amphibian.

## Validation Commands

Run focused tests first:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "DevControl|DevController|SingleAction"
```

Run full validation:

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Manual launch:

```powershell
Start-Process .\vnext\artifacts\shell\Wevito.VNext.Shell.exe
Start-Process .\vnext\artifacts\dev-controller\Wevito.VNext.DevController.exe
```

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Controller and Shell disagree about state | Shell returns snapshot after every command and controller polls periodically. |
| Dev tool corrupts saves | Controller never writes saves directly; Shell validates and persists. |
| A command targets the wrong pet after reorder | Include `ExpectedPetId` with slot commands and reject mismatches. |
| Controller exposes cheat functions in release | Start pipe server only in development builds or behind explicit local flag. |
| Roam becomes normal gameplay state | Keep roam overrides transient and excluded from persistence. |
| Action buttons affect every pet | Add single-pet action path and tests proving other pets are unchanged. |
| Missing sprite color spawns invisible pet | Validate runtime sprite folder before spawn; reject unavailable combinations. |

## Implementation Order

```text
1. Contracts
2. ShellCoordinator dev methods
3. DevControlServer
4. Single-pet action and roam override
5. DevController WPF app
6. Build/publish integration
7. Tests
8. Manual visual QA doc
```

## Stop Conditions

Stop and ask before continuing if:

- Dev-control pipe cannot be restricted to dev builds.
- Single-pet actions require rewriting the whole care/action system.
- Runtime sprite validation shows many expected combinations are missing.
- Roam requires broad movement-system changes beyond a small transient override.
- The controller would need direct database/save-file writes to work.

