namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class ToolResultBudgetServiceTests
{
    [Fact]
    public void TruncatesAt4kTokens()
    {
        var service = new ToolResultBudgetService(Path.Combine(TestRoot(), "tool-results"), tokenBudget: 4);

        var result = service.FormatToolResult("localDocs", "one two three four five six");

        Assert.Equal(6, result.TotalTokens);
        Assert.Equal(4, result.TruncatedTokens);
    }

    [Fact]
    public void WritesFullResultToDisk()
    {
        var service = new ToolResultBudgetService(Path.Combine(TestRoot(), "tool-results"), tokenBudget: 4);

        var result = service.FormatToolResult("localDocs", "one two three four five six");

        Assert.True(File.Exists(result.FullPath));
    }

    [Fact]
    public void IncludesTruncationMarker()
    {
        var service = new ToolResultBudgetService(Path.Combine(TestRoot(), "tool-results"), tokenBudget: 4);

        var result = service.FormatToolResult("localDocs", "one two three four five six");

        Assert.Contains("[truncated; full result at", result.Truncated);
    }

    private static string TestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-tool-result-budget-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
