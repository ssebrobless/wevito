using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal static class ShellPresentationRules
{
    public const double ActiveAssetOpacity = 1.0;
    public const double BackgroundAssetOpacity = 0.5;
    private const int RoamTaskbarOverlapPixels = 10;

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

    public static bool IsActionsSurfaceOpen(ToolSession tool)
    {
        return tool.IsOpen &&
               (string.Equals(tool.ToolId, "actions", StringComparison.OrdinalIgnoreCase) ||
                tool.ToolId.StartsWith("action:", StringComparison.OrdinalIgnoreCase));
    }

    public static RectInt ResolveRoamMotionBounds(RectInt visibleBandBounds, RectInt workArea)
    {
        var targetBottom = Math.Min(visibleBandBounds.Bottom, workArea.Bottom + RoamTaskbarOverlapPixels);
        var height = Math.Max(1, targetBottom - visibleBandBounds.Y);
        return new RectInt(visibleBandBounds.X, visibleBandBounds.Y, visibleBandBounds.Width, height);
    }
}
