using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class WindowsForegroundFullscreenMonitorTests
{
    [Fact]
    public void Observe_EntersFullscreenOnlyAfterSustainedThreshold()
    {
        var monitor = new WindowsForegroundFullscreenMonitor();
        var start = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        var first = monitor.Observe(Context(isFullscreen: true, isShell: false), start);
        var early = monitor.Observe(Context(isFullscreen: true, isShell: false), start.AddSeconds(4));
        var transitioned = monitor.Observe(Context(isFullscreen: true, isShell: false), start.AddSeconds(5));

        Assert.False(first.DidTransition);
        Assert.False(early.DidTransition);
        Assert.True(transitioned.DidTransition);
        Assert.True(transitioned.IsFullscreenOther);
    }

    [Fact]
    public void Observe_ExitsFullscreenOnlyAfterLongerSustainedThreshold()
    {
        var monitor = new WindowsForegroundFullscreenMonitor();
        var start = DateTimeOffset.Parse("2026-05-13T12:00:00Z");
        monitor.Observe(Context(isFullscreen: true, isShell: false), start);
        monitor.Observe(Context(isFullscreen: true, isShell: false), start.AddSeconds(5));

        var early = monitor.Observe(Context(isFullscreen: false, isShell: false), start.AddSeconds(10));
        var transitioned = monitor.Observe(Context(isFullscreen: false, isShell: false), start.AddSeconds(25));

        Assert.False(early.DidTransition);
        Assert.True(transitioned.DidTransition);
        Assert.False(transitioned.IsFullscreenOther);
    }

    [Fact]
    public void Observe_IgnoresShellFullscreenSurfaces()
    {
        var monitor = new WindowsForegroundFullscreenMonitor();
        var start = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

        monitor.Observe(Context(isFullscreen: true, isShell: true), start);
        var result = monitor.Observe(Context(isFullscreen: true, isShell: true), start.AddSeconds(20));

        Assert.False(result.DidTransition);
        Assert.False(monitor.IsFullscreenOther);
    }

    private static DesktopContext Context(bool isFullscreen, bool isShell)
    {
        return new DesktopContext(
            new ForegroundWindowInfo(100, 101, "game", "Game", "Window", isShell, isFullscreen),
            new RectInt(0, 0, 1920, 1080),
            new RectInt(0, 0, 1920, 1080),
            new PointInt(0, 0),
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
    }
}
