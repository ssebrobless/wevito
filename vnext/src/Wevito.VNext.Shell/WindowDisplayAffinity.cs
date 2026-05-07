using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Wevito.VNext.Shell;

internal static class WindowDisplayAffinity
{
    private const uint WdaExcludeFromCapture = 0x00000011;

    public static void ExcludeFromCapture(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _ = SetWindowDisplayAffinity(handle, WdaExcludeFromCapture);
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
}
