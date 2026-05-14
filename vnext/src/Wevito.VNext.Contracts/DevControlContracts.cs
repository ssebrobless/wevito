using System.Text.Json;

namespace Wevito.VNext.Contracts;

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
    DateTimeOffset CapturedAtUtc)
{
    public static DevControlSnapshot Empty(DateTimeOffset now) => new(
        [
            DevControlPetSlotSnapshot.Empty(0),
            DevControlPetSlotSnapshot.Empty(1),
            DevControlPetSlotSnapshot.Empty(2)
        ],
        DevControlOptions.Empty,
        now);
}

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
    string? AnimationState)
{
    public static DevControlPetSlotSnapshot Empty(int slotIndex) => new(
        slotIndex,
        null,
        "N/A",
        null,
        null,
        null,
        null,
        null,
        true,
        false,
        null,
        null);
}

public sealed record DevControlOptions(
    IReadOnlyList<string> SpeciesIds,
    IReadOnlyList<string> LifeStages,
    IReadOnlyList<string> Genders,
    IReadOnlyList<string> ColorVariants,
    IReadOnlyList<DevControlActionOption> Actions)
{
    public static DevControlOptions Empty { get; } = new([], [], [], [], []);
}

public sealed record DevControlActionOption(string ActionId, string Label);

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

public static class DevControlPipeMessage
{
    public static string SerializeCommand<TPayload>(string commandType, TPayload payload)
    {
        var envelope = new DevControlCommandEnvelope(commandType, JsonSerializer.SerializeToElement(payload, JsonDefaults.Options));
        return JsonSerializer.Serialize(envelope, JsonDefaults.Options);
    }

    public static string SerializeResponse(DevControlResponseEnvelope response)
    {
        return JsonSerializer.Serialize(response, JsonDefaults.Options);
    }

    public static DevControlCommandEnvelope DeserializeCommand(string line)
    {
        return JsonSerializer.Deserialize<DevControlCommandEnvelope>(line, JsonDefaults.Options)
            ?? throw new InvalidOperationException("Failed to deserialize dev-control command envelope.");
    }

    public static DevControlResponseEnvelope DeserializeResponse(string line)
    {
        return JsonSerializer.Deserialize<DevControlResponseEnvelope>(line, JsonDefaults.Options)
            ?? throw new InvalidOperationException("Failed to deserialize dev-control response envelope.");
    }

    public static TPayload DeserializePayload<TPayload>(JsonElement payload)
    {
        return payload.Deserialize<TPayload>(JsonDefaults.Options)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TPayload).Name} payload.");
    }
}
