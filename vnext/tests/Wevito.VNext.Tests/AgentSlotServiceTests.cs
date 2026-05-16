using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AgentSlotServiceTests
{
    [Fact]
    public void BuildRoster_UsesActivePetNamesAndFallbackAgentNames()
    {
        var service = new AgentSlotService();
        var pet = new PetActor(Guid.NewGuid(), "Noodle", "snake");

        var roster = service.BuildRoster([pet], nowUtc: DateTimeOffset.Parse("2026-05-15T12:00:00Z"));

        Assert.Collection(
            roster.Slots,
            slot =>
            {
                Assert.Equal(0, slot.SlotIndex);
                Assert.Equal("Noodle", slot.Name);
                Assert.Equal(pet.Id, slot.PetId);
            },
            slot => Assert.Equal("Agent 2", slot.Name),
            slot => Assert.Equal("Agent 3", slot.Name));
    }

    [Fact]
    public void BuildRoster_EmitsRenamePacketWhenPetSlotNameChanges()
    {
        var service = new AgentSlotService();
        var previous = new[]
        {
            new AgentSlot(AgentSlotService.BuildSlotId(0), 0, "Old Name")
        };

        var roster = service.BuildRoster(
            [new PetActor(Guid.NewGuid(), "New Name", "fox")],
            previous,
            DateTimeOffset.Parse("2026-05-15T12:05:00Z"));

        Assert.Single(roster.RenamePackets);
        Assert.Equal(AgentSlotService.SlotRenamedPacketKind, roster.RenamePackets[0].PacketKind);
        Assert.Contains("Old Name", roster.RenamePackets[0].Summary);
        Assert.Contains("New Name", roster.RenamePackets[0].Summary);
    }

    [Fact]
    public async Task Dispatcher_UserVisibleTaskConsultsModelAdapter()
    {
        var adapter = new RecordingModelAdapter("2");
        var dispatcher = new AgentSlotDispatcher(adapter);
        var slots = new[]
        {
            new AgentSlot(AgentSlotService.BuildSlotId(0), 0, "goose 1"),
            new AgentSlot(AgentSlotService.BuildSlotId(1), 1, "fox 1"),
            new AgentSlot(AgentSlotService.BuildSlotId(2), 2, "frog 1")
        };

        var selected = await dispatcher.PickAgentForTaskAsync(TaskKind.ReviewCode, TaskOrigin.UserVisible, slots, "review code");

        Assert.True(adapter.WasCalled);
        Assert.Equal("fox 1", selected?.Name);
    }

    [Fact]
    public void Dispatcher_BackgroundTaskUsesFirstIdleWithoutModel()
    {
        var adapter = new RecordingModelAdapter("3");
        var dispatcher = new AgentSlotDispatcher(adapter);
        var slots = new[]
        {
            new AgentSlot(AgentSlotService.BuildSlotId(0), 0, "goose 1", AgentSlotStatus.RunningTool, Guid.NewGuid()),
            new AgentSlot(AgentSlotService.BuildSlotId(1), 1, "fox 1"),
            new AgentSlot(AgentSlotService.BuildSlotId(2), 2, "frog 1")
        };

        var selected = dispatcher.PickAgentForTask(TaskKind.SearchLocalDocs, TaskOrigin.Background, slots);

        Assert.False(adapter.WasCalled);
        Assert.Equal("fox 1", selected?.Name);
    }

    private sealed class RecordingModelAdapter : IModelAdapter
    {
        private readonly string _summary;

        public RecordingModelAdapter(string summary)
        {
            _summary = summary;
        }

        public bool WasCalled { get; private set; }

        public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new ModelResponse("fake", "fake", _summary, DidCallProvider: false));
        }
    }
}
