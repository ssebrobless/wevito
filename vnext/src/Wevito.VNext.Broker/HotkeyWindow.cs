using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Broker;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotKey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const int TogglePinnedId = 1;
    private const int CaptureBasketId = 2;
    private const int OpenBasketId = 3;
    private const int OpenDevToolsId = 4;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
        Register();
    }

    public event Action<string>? ActionPressed;

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotKey)
        {
            var action = m.WParam.ToInt32() switch
            {
                TogglePinnedId => "toggle-pinned",
                CaptureBasketId => "capture-basket",
                OpenBasketId => "open-basket",
                OpenDevToolsId => "open-dev-tools",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(action))
            {
                ActionPressed?.Invoke(action);
            }
        }

        base.WndProc(ref m);
    }

    private void Register()
    {
        RegisterHotkeyWithFallback(TogglePinnedId, "toggle-pinned", Keys.P);
        RegisterHotkeyWithFallback(CaptureBasketId, "capture-basket", Keys.B);
        RegisterHotkeyWithFallback(OpenBasketId, "open-basket", Keys.O);
        RegisterHotkeyWithFallback(OpenDevToolsId, "open-dev-tools", Keys.D);
    }

    private void RegisterHotkeyWithFallback(int id, string actionId, Keys key)
    {
        if (TryRegisterHotkey(id, actionId, "Ctrl+Shift", ModControl | ModShift, key))
        {
            return;
        }

        _ = TryRegisterHotkey(id, actionId, "Ctrl+Alt", ModControl | ModAlt, key);
    }

    private bool TryRegisterHotkey(int id, string actionId, string label, uint modifiers, Keys key)
    {
        var registered = NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)key);
        var error = registered ? 0 : Marshal.GetLastWin32Error();
        TraceLog.Write("hotkey-register", $"action={actionId} keys={label}+{key} success={registered} error={error}");
        return registered;
    }

    private void Unregister()
    {
        _ = NativeMethods.UnregisterHotKey(Handle, TogglePinnedId);
        _ = NativeMethods.UnregisterHotKey(Handle, CaptureBasketId);
        _ = NativeMethods.UnregisterHotKey(Handle, OpenBasketId);
        _ = NativeMethods.UnregisterHotKey(Handle, OpenDevToolsId);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
