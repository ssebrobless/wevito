using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal static class OverlayWindowStyler
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpNoRedraw = 0x0008;
    private const uint SwpNoSendChanging = 0x0400;
    private const uint SwpNoActivate = 0x0010;

    private static readonly HashSet<IntPtr> HookedWindows = [];
    private static readonly Dictionary<IntPtr, WindowStyleState> StyleStateByHandle = [];
    private static readonly IntPtr HwndTopMost = new(-1);

    public static void Apply(Window window, bool clickThrough, bool noActivate, bool hideFromTaskbar = true)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        HookWindow(handle, window);

        var desiredState = new WindowStyleState(clickThrough, noActivate, hideFromTaskbar);
        if (StyleStateByHandle.TryGetValue(handle, out var existingState) && existingState.Equals(desiredState))
        {
            return;
        }

        var exStyle = NativeMethods.GetWindowLong(handle, GwlExStyle);
        exStyle = hideFromTaskbar ? exStyle | WsExToolWindow : exStyle & ~WsExToolWindow;
        exStyle = clickThrough ? exStyle | WsExTransparent : exStyle & ~WsExTransparent;
        exStyle = noActivate ? exStyle | WsExNoActivate : exStyle & ~WsExNoActivate;
        NativeMethods.SetWindowLong(handle, GwlExStyle, exStyle);
        NativeMethods.SetWindowPos(handle, HwndTopMost, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoOwnerZOrder | SwpNoRedraw | SwpNoSendChanging | (noActivate ? SwpNoActivate : 0));
        StyleStateByHandle[handle] = desiredState;
        TraceLog.Write("overlay-style", $"window={window.Title} clickThrough={clickThrough} noActivate={noActivate} hideFromTaskbar={hideFromTaskbar} handle={handle}");
    }

    private static void HookWindow(IntPtr handle, Window window)
    {
        if (HookedWindows.Contains(handle))
        {
            return;
        }

        if (PresentationSource.FromVisual(window) is HwndSource source)
        {
            source.AddHook(WndProc);
            HookedWindows.Add(handle);
        }
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmMouseActivate)
        {
            var exStyle = NativeMethods.GetWindowLong(hwnd, GwlExStyle);
            if ((exStyle & WsExNoActivate) != 0)
            {
                handled = true;
                return new IntPtr(MaNoActivate);
            }
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);
    }

    private readonly record struct WindowStyleState(bool ClickThrough, bool NoActivate, bool HideFromTaskbar);
}
