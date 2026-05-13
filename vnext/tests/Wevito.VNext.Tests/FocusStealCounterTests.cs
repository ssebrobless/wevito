using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class FocusStealCounterTests
{
    [Fact]
    public void RecordActivation_IncrementsOnlyWhenForegroundIsFullscreenOther()
    {
        var counter = new FocusStealCounter(CreateStatePath());
        var now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var ignored = counter.RecordActivation(foregroundIsFullscreenOther: false, now);
        var counted = counter.RecordActivation(foregroundIsFullscreenOther: true, now.AddSeconds(1));

        Assert.Equal(0, ignored.Count);
        Assert.Equal(1, counted.Count);
        Assert.Equal(now.AddSeconds(1), counted.LastRecordedAtUtc);
    }

    [Fact]
    public void RecordActivation_PersistsCountAcrossInstances()
    {
        var path = CreateStatePath();
        var first = new FocusStealCounter(path);
        first.RecordActivation(foregroundIsFullscreenOther: true, DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        var second = new FocusStealCounter(path);

        Assert.Equal(1, second.Read().Count);
        Assert.True(File.Exists(path));
    }

    private static string CreateStatePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-focus-steal-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "focus-steal.json");
    }
}
