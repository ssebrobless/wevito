using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ModeReducerTests
{
    [Fact]
    public void Reduce_ReturnsPinned_WhenPinnedOverrideEnabled()
    {
        var desktopContext = BuildDesktopContext(999, 22, false);

        var mode = ModeReducer.Reduce(true, desktopContext, [11]);

        Assert.Equal(CompanionMode.Pinned, mode);
    }

    [Fact]
    public void Reduce_ReturnsFocused_WhenForegroundIsShellWindow()
    {
        var desktopContext = BuildDesktopContext(999, 11, false);

        var mode = ModeReducer.Reduce(false, desktopContext, [11]);

        Assert.Equal(CompanionMode.Focused, mode);
    }

    [Fact]
    public void Reduce_ReturnsPassive_WhenAnotherWindowOwnsForeground()
    {
        var desktopContext = BuildDesktopContext(999, 22, false);

        var mode = ModeReducer.Reduce(false, desktopContext, [11]);

        Assert.Equal(CompanionMode.Passive, mode);
    }

    private static DesktopContext BuildDesktopContext(int processId, long hwnd, bool isShellSurface)
    {
        return new DesktopContext(
            new ForegroundWindowInfo(processId, hwnd, "notepad", "Untitled - Notepad", "Notepad", isShellSurface, false),
            new RectInt(0, 0, 1920, 1040),
            new RectInt(0, 0, 1920, 1080),
            new PointInt(100, 100),
            DateTimeOffset.UtcNow);
    }
}
