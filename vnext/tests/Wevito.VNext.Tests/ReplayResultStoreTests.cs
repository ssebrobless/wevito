using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement.Replay;

namespace Wevito.VNext.Tests;

public sealed class ReplayResultStoreTests
{
    [Fact]
    public void GetLatest_ReturnsMostRecentResultForOperation()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-replay-result-store-tests", Guid.NewGuid().ToString("N"));
        var operationRoot = Path.Combine(root, "operation-alpha");
        Directory.CreateDirectory(Path.Combine(operationRoot, "older"));
        Directory.CreateDirectory(Path.Combine(operationRoot, "newer"));
        WriteSummary(Path.Combine(operationRoot, "older", "replay-result.json"), new ReplayResultSummary("operation-alpha", "Diverged", 2, DateTimeOffset.Parse("2026-05-19T10:00:00Z"), ["old"]));
        WriteSummary(Path.Combine(operationRoot, "newer", "replay-result.json"), new ReplayResultSummary("operation-alpha", "Identical", 0, DateTimeOffset.Parse("2026-05-19T11:00:00Z"), []));

        var store = new ReplayResultStore(root, new KillSwitchService(() => new Dictionary<string, string>()));

        var latest = store.GetLatest("operation-alpha");

        Assert.NotNull(latest);
        Assert.Equal("Identical", latest!.ResultKind);
        Assert.Equal(DateTimeOffset.Parse("2026-05-19T11:00:00Z"), latest.ReplayedAtUtc);
    }

    [Fact]
    public void GetLatest_KillSwitchActive_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-replay-result-store-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "operation-alpha"));
        WriteSummary(Path.Combine(root, "operation-alpha", "replay-result.json"), new ReplayResultSummary("operation-alpha", "Identical", 0, DateTimeOffset.Parse("2026-05-19T11:00:00Z"), []));
        var settings = new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = bool.TrueString };
        var store = new ReplayResultStore(root, new KillSwitchService(() => settings));

        var latest = store.GetLatest("operation-alpha");

        Assert.Null(latest);
    }

    [Fact]
    public void GetLatest_MissingRoot_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-replay-result-store-tests", Guid.NewGuid().ToString("N"), "missing");
        var store = new ReplayResultStore(root, new KillSwitchService(() => new Dictionary<string, string>()));

        var latest = store.GetLatest("operation-alpha");

        Assert.Null(latest);
    }

    [Fact]
    public void ToolPopupReplayLogExpander_IsReadOnly()
    {
        var xaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("SupervisedReplayLogExpander", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("</Expander>", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var segment = xaml[start..end];

        Assert.DoesNotContain("<Button", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ToggleSwitch", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<CheckBox", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Click=", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Checked=", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unchecked=", segment, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteSummary(string path, ReplayResultSummary summary)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(summary, JsonDefaults.Options));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
