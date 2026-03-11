using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public static class ModeReducer
{
    public static CompanionMode Reduce(
        bool isPinned,
        DesktopContext? desktopContext,
        IReadOnlyCollection<long> shellWindowHandles)
    {
        if (isPinned)
        {
            return CompanionMode.Pinned;
        }

        if (desktopContext is null)
        {
            return CompanionMode.Focused;
        }

        var foreground = desktopContext.ForegroundWindow;
        if (foreground.IsShellSurface || shellWindowHandles.Contains(foreground.Hwnd))
        {
            return CompanionMode.Focused;
        }

        return CompanionMode.Passive;
    }
}
