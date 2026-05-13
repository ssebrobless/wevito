using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal static class ShellPresentationRules
{
    public const double ActiveAssetOpacity = 1.0;
    public const double BackgroundAssetOpacity = 0.5;

    public static bool ShouldShowMainPanel(CompanionMode mode)
    {
        return mode is CompanionMode.Focused or CompanionMode.Pinned;
    }

    public static double ResolveDesktopAssetOpacity(
        CompanionMode mode,
        DesktopContext? desktopContext,
        IReadOnlyCollection<long> shellWindowHandles)
    {
        _ = mode;
        if (desktopContext is null)
        {
            return ActiveAssetOpacity;
        }

        var foreground = desktopContext.ForegroundWindow;
        var foregroundIsWevito = foreground.IsShellSurface || shellWindowHandles.Contains(foreground.Hwnd);
        return foregroundIsWevito ? ActiveAssetOpacity : BackgroundAssetOpacity;
    }
}
