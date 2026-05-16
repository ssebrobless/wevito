using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AgentToolConcurrencyCoordinatorTests
{
    [Fact]
    public async Task RunToolAsync_AllowsParallelToolsForDifferentAgents()
    {
        var coordinator = new AgentToolConcurrencyCoordinator();
        var slotA = new AgentSlot(AgentSlotService.BuildSlotId(0), 0, "goose 1");
        var slotB = new AgentSlot(AgentSlotService.BuildSlotId(1), 1, "fox 1");
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = coordinator.RunToolAsync(slotA, "spriteAudit", async token =>
        {
            firstEntered.SetResult();
            await release.Task.WaitAsync(token);
            return "first";
        });
        await firstEntered.Task;

        var second = await coordinator.RunToolAsync(slotB, "localDocs", _ => Task.FromResult("second"));
        release.SetResult();

        Assert.Equal("second", second);
        Assert.Equal("first", await first);
    }

    [Fact]
    public async Task RunModelGenerationAsync_SerializesLocalLlmGeneration()
    {
        var coordinator = new AgentToolConcurrencyCoordinator();
        var slotA = new AgentSlot(AgentSlotService.BuildSlotId(0), 0, "goose 1");
        var slotB = new AgentSlot(AgentSlotService.BuildSlotId(1), 1, "fox 1");
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = false;

        var first = coordinator.RunModelGenerationAsync(slotA, "chat", async token =>
        {
            firstEntered.SetResult();
            await release.Task.WaitAsync(token);
            return "first";
        });
        await firstEntered.Task;
        var second = coordinator.RunModelGenerationAsync(slotB, "chat", _ =>
        {
            secondStarted = true;
            return Task.FromResult("second");
        });

        await Task.Delay(100);
        Assert.False(secondStarted);

        release.SetResult();
        Assert.Equal("first", await first);
        Assert.Equal("second", await second);
        Assert.True(secondStarted);
    }
}
