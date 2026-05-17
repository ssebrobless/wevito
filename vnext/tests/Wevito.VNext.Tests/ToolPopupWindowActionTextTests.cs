using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class ToolPopupWindowActionTextTests
{
    [Fact]
    public void PetTasksPanelHasOwnScrollViewerForReportControls()
    {
        var xaml = File.ReadAllText(FindRepoFile("vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));

        Assert.Contains("x:Name=\"PetCommandPanel\"", xaml);
        Assert.Contains("AutomationId=\"PetCommandScrollViewer\"", xaml);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("PetTaskOpenReportButton", xaml);
        Assert.Contains("PetTaskExecuteButton", xaml);
    }

    [Theory]
    [InlineData("CreativeLearningLabWindow.xaml", "CreativeLearningLabRootScrollViewer")]
    [InlineData("SpriteWorkflowV2Window.xaml", "SpriteWorkflowV2RootScrollViewer")]
    public void DenseToolWindowsHaveRootScrollViewerSoBottomControlsRemainReachable(string fileName, string automationId)
    {
        var xaml = File.ReadAllText(FindRepoFile("vnext", "src", "Wevito.VNext.Shell", fileName));

        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", xaml);
        Assert.Contains($"AutomationId=\"{automationId}\"", xaml);
    }

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
    public void FormatActionSummaryIncludesClarityDescription()
    {
        var pets = new[]
        {
            new PetActor(Guid.NewGuid(), "Frog 1", "frog", ActiveStatuses: [])
        };

        var text = ToolPopupWindow.FormatActionSummary(
            "Water",
            "Drag a water item onto the selected pet to refill thirst.",
            2,
            pets);

        Assert.Contains("refill thirst", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Frog 1", text);
    }

    [Fact]
    public void ActionsJsonContainsDescriptionsForEveryAction()
    {
        var json = File.ReadAllText(FindRepoFile("vnext", "content", "actions.json"));

        Assert.Contains("\"description\"", json);
        Assert.Equal(9, json.Split("\"description\"", StringSplitOptions.None).Length - 1);
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

    private static string FindRepoFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {string.Join(Path.DirectorySeparatorChar, relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
