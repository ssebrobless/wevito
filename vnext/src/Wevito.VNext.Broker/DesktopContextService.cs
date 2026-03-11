using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Broker;

internal sealed class DesktopContextService
{
    public DesktopContext Capture()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        var processId = 0;
        var processName = string.Empty;
        var title = string.Empty;
        var className = string.Empty;
        var isShellSurface = false;
        var isFullscreen = false;

        if (hwnd != IntPtr.Zero)
        {
            _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            processId = unchecked((int)pid);
            processName = TryGetProcessName(processId);
            title = GetWindowText(hwnd);
            className = GetClassName(hwnd);
            isShellSurface = className is "Progman" or "WorkerW" or "Shell_TrayWnd";
            isFullscreen = IsFullscreenWindow(hwnd);
        }

        NativeMethods.GetCursorPos(out var nativePoint);
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        var workArea = screen.WorkingArea;
        var bounds = screen.Bounds;

        return new DesktopContext(
            new ForegroundWindowInfo(processId, hwnd.ToInt64(), processName, title, className, isShellSurface, isFullscreen),
            new RectInt(workArea.X, workArea.Y, workArea.Width, workArea.Height),
            new RectInt(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            new PointInt(nativePoint.X, nativePoint.Y),
            DateTimeOffset.UtcNow);
    }

    private static string TryGetProcessName(int processId)
    {
        if (processId <= 0)
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsFullscreenWindow(IntPtr hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return false;
        }

        var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        var bounds = screen.Bounds;
        return Math.Abs(rect.Left - bounds.Left) <= 2 &&
               Math.Abs(rect.Top - bounds.Top) <= 2 &&
               Math.Abs(rect.Right - bounds.Right) <= 2 &&
               Math.Abs(rect.Bottom - bounds.Bottom) <= 2;
    }

    private static string GetWindowText(IntPtr hwnd)
    {
        var builder = new StringBuilder(512);
        _ = NativeMethods.GetWindowTextW(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(128);
        _ = NativeMethods.GetClassNameW(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    }
}
