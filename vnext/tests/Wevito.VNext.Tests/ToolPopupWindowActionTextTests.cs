using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ToolPopupWindowActionTextTests
{
    [Fact]
    public void FormatActionSummaryNamesPrimaryTargetAndDragPath()
    {
        var pets = new[]
        {
            new PetActor(Guid.NewGuid(), "Frog 1", "frog", ActiveStatuses: []),
            new PetActor(Guid.NewGuid(), "Snake 1", "snake", ActiveStatuses: [])
        };

        var text = ToolPopupWindow.FormatActionSummary("Water", 2, pets);

        Assert.Contains("Frog 1", text);
        Assert.Contains("drag", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("drop", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildActionOptionButtonLabelNamesTheTargetPet()
    {
        var pets = new[]
        {
            new PetActor(Guid.NewGuid(), "Frog 1", "frog", ActiveStatuses: []),
            new PetActor(Guid.NewGuid(), "Snake 1", "snake", ActiveStatuses: [])
        };

        var label = ToolPopupWindow.BuildActionOptionButtonLabel(pets);

        Assert.Equal("Use on Frog 1", label);
    }

    [Fact]
    public void TryParseActionOptionDragPayloadSplitsActionAndItem()
    {
        var parsed = ToolPopupWindow.TryParseActionOptionDragPayload("water|pond_dish", out var actionId, out var itemId);

        Assert.True(parsed);
        Assert.Equal("water", actionId);
        Assert.Equal("pond_dish", itemId);
    }
}
