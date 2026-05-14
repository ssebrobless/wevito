using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Tests;

public sealed class DevControlContractsTests
{
    [Fact]
    public void DevControlSnapshotAlwaysCarriesThreeReadableSlots()
    {
        var snapshot = new DevControlSnapshot(
            [
                DevControlPetSlotSnapshot.Empty(0),
                new DevControlPetSlotSnapshot(
                    1,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Scout\nfox | female | blue | baby",
                    "Scout",
                    "fox",
                    "female",
                    "blue",
                    "baby",
                    false,
                    false,
                    "Home",
                    "Idle"),
                DevControlPetSlotSnapshot.Empty(2)
            ],
            DevControlOptions.Empty,
            DateTimeOffset.Parse("2026-05-14T12:00:00Z"));

        Assert.Equal(3, snapshot.Slots.Count);
        Assert.Equal("N/A", snapshot.Slots[0].DisplayText);
        Assert.Equal("Scout", snapshot.Slots[1].Name);
        Assert.Equal("N/A", snapshot.Slots[2].DisplayText);
    }

    [Fact]
    public void DevControlCommandEnvelopeRoundTrips()
    {
        var payload = new DevControlSpawnOrReplacePetRequest(
            2,
            "goose",
            "adult",
            "male",
            "red",
            true);

        var line = DevControlPipeMessage.SerializeCommand(DevControlCommandTypes.SpawnOrReplacePet, payload);
        var envelope = DevControlPipeMessage.DeserializeCommand(line);
        var roundTrip = DevControlPipeMessage.DeserializePayload<DevControlSpawnOrReplacePetRequest>(envelope.Payload);

        Assert.Equal(DevControlCommandTypes.SpawnOrReplacePet, envelope.CommandType);
        Assert.Equal(payload, roundTrip);
    }

    [Fact]
    public void VisualQaCommandEnvelopeRoundTrips()
    {
        var command = new VisualQaForceAnimationRequest(
            0,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "walk",
            2,
            0.25,
            true);

        var json = JsonSerializer.Serialize(command, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<VisualQaForceAnimationRequest>(json, JsonDefaults.Options);

        Assert.Equal(command, roundTrip);
    }

    [Fact]
    public void IssueTagRequestKeepsEvidenceScopeExplicit()
    {
        var request = new VisualQaIssueTagRequest(
            1,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ["cropped", "white_box"],
            "Crow head is flattened on frame 3.",
            true);

        Assert.Equal(1, request.SlotIndex);
        Assert.Contains("cropped", request.Tags);
        Assert.True(request.AttachCurrentScreenshot);
    }
}
