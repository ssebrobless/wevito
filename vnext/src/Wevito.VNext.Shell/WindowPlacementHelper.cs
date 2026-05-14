using System.Windows;

namespace Wevito.VNext.Shell;

internal static class WindowPlacementHelper
{
    public static void FitInsideWorkArea(Window window, double margin = 16)
    {
        var workArea = SystemParameters.WorkArea;
        var maxWidth = Math.Max(320, workArea.Width - (margin * 2));
        var maxHeight = Math.Max(320, workArea.Height - (margin * 2));

        window.Width = Math.Min(window.Width, maxWidth);
        window.Height = Math.Min(window.Height, maxHeight);
        window.MaxWidth = maxWidth;
        window.MaxHeight = maxHeight;

        if (window.Left + window.Width > workArea.Right - margin)
        {
            window.Left = workArea.Right - window.Width - margin;
        }

        if (window.Top + window.Height > workArea.Bottom - margin)
        {
            window.Top = workArea.Bottom - window.Height - margin;
        }

        window.Left = Math.Max(workArea.Left + margin, window.Left);
        window.Top = Math.Max(workArea.Top + margin, window.Top);
    }
}
