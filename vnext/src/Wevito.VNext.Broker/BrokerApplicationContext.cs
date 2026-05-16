using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Broker;

internal sealed class BrokerApplicationContext : ApplicationContext
{
    private readonly SynchronizationContext _uiContext;
    private readonly NotifyIcon _notifyIcon;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly PipeServer _pipeServer;
    private readonly DesktopContextService _desktopContextService;
    private readonly CodexLoopWatchdogService _codexLoopWatchdogService;
    private readonly System.Windows.Forms.Timer _desktopPollTimer;
    private readonly System.Windows.Forms.Timer _overlayClickTimer;
    private readonly ToolStripMenuItem _pinMenuItem;

    private bool _isPinned;
    private bool _wasLeftButtonDown;
    private DesktopContext? _lastDesktopContext;
    private string _lastDesktopContextKey = "";
    private ForegroundWindowInfo? _lastForegroundWindow;
    private RectInt? _lastWorkArea;
    private IReadOnlyList<OverlayRegion> _overlayRegions = [];

    public BrokerApplicationContext(string pipeName)
    {
        TraceLog.Write("broker", $"startup pipe={pipeName}");
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _pipeServer = new PipeServer(pipeName);
        _pipeServer.CommandReceived += OnCommandReceivedAsync;
        _pipeServer.ClientDisconnected += OnClientDisconnected;

        _desktopContextService = new DesktopContextService();
        _codexLoopWatchdogService = new CodexLoopWatchdogService();
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.ActionPressed += PublishHotkey;

        _pinMenuItem = new ToolStripMenuItem("Pin HUD", null, (_, _) => PublishHotkey("toggle-pinned"));
        var captureMenuItem = new ToolStripMenuItem("Capture Clipboard Link", null, (_, _) => PublishHotkey("capture-basket"));
        var basketMenuItem = new ToolStripMenuItem("Open Basket", null, (_, _) => PublishHotkey("open-basket"));
        var devMenuItem = new ToolStripMenuItem("Open Dev Tools", null, (_, _) => PublishHotkey("open-dev-tools"));

        _notifyIcon = new NotifyIcon
        {
            Text = "Wevito Broker",
            Visible = true,
            Icon = SystemIcons.Information,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.AddRange(
        [
            _pinMenuItem,
            captureMenuItem,
            basketMenuItem,
            devMenuItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())
        ]);

        _desktopPollTimer = new System.Windows.Forms.Timer
        {
            Interval = 120
        };
        _desktopPollTimer.Tick += async (_, _) => await OnDesktopTickAsync();
        _desktopPollTimer.Start();

        _overlayClickTimer = new System.Windows.Forms.Timer
        {
            Interval = 16
        };
        _overlayClickTimer.Tick += async (_, _) => await PublishOverlayClickAsync();
        _overlayClickTimer.Start();

        _ = _pipeServer.RunAsync();
        TraceLog.Write("broker", "startup-complete");
    }

    protected override void ExitThreadCore()
    {
        TraceLog.Write("broker", "shutdown-begin");
        _desktopPollTimer.Stop();
        _overlayClickTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _hotkeyWindow.Dispose();
        _pipeServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
        TraceLog.Write("broker", "shutdown-complete");
        base.ExitThreadCore();
    }

    private async Task OnDesktopTickAsync()
    {
        var desktopContext = _desktopContextService.Capture();
        await PublishDesktopContextAsync(desktopContext);
    }

    private async Task PublishDesktopContextAsync()
    {
        await PublishDesktopContextAsync(_desktopContextService.Capture());
    }

    private async Task PublishDesktopContextAsync(DesktopContext desktopContext)
    {
        var desktopContextKey = CreateDesktopContextKey(desktopContext);
        if (string.Equals(_lastDesktopContextKey, desktopContextKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastDesktopContext = desktopContext;
        _lastDesktopContextKey = desktopContextKey;
        TraceLog.Write("desktop-context", $"foreground={desktopContext.ForegroundWindow.ProcessName} title={desktopContext.ForegroundWindow.Title} shell={desktopContext.ForegroundWindow.IsShellSurface} fullscreen={desktopContext.ForegroundWindow.IsFullscreenApp}");
        await _pipeServer.SendEventAsync(ShellEventTypes.DesktopContextChanged, desktopContext);

        if (_lastForegroundWindow is null || !_lastForegroundWindow.Equals(desktopContext.ForegroundWindow))
        {
            _lastForegroundWindow = desktopContext.ForegroundWindow;
            await _pipeServer.SendEventAsync(ShellEventTypes.ForegroundChanged, desktopContext.ForegroundWindow);
        }

        if (_lastWorkArea is null || _lastWorkArea.Value != desktopContext.WorkArea)
        {
            _lastWorkArea = desktopContext.WorkArea;
            await _pipeServer.SendEventAsync(ShellEventTypes.WorkAreaChanged, desktopContext.WorkArea);
        }
    }

    private async Task OnCommandReceivedAsync(ShellCommandEnvelope envelope)
    {
        TraceLog.Write("shell-command", envelope.CommandType);
        switch (envelope.CommandType)
        {
            case ShellCommandTypes.SetPinned:
                var setPinned = PipeMessage.DeserializePayload<SetPinnedCommand>(envelope.Payload);
                _isPinned = setPinned.IsPinned;
                _pinMenuItem.Text = _isPinned ? "Release HUD" : "Pin HUD";
                TraceLog.Write("pinned", $"value={_isPinned}");
                break;

            case ShellCommandTypes.CaptureClipboard:
                await PublishClipboardAsync("command");
                break;

            case ShellCommandTypes.OpenUrl:
                var openUrl = PipeMessage.DeserializePayload<OpenUrlCommand>(envelope.Payload);
                await OpenUrlAsync(openUrl.Url);
                break;

            case ShellCommandTypes.RequestDesktopContext:
                await PublishDesktopContextAsync();
                break;

            case ShellCommandTypes.RegisterDropTarget:
                break;

            case ShellCommandTypes.SetOverlayRegions:
                var overlayRegions = PipeMessage.DeserializePayload<SetOverlayRegionsCommand>(envelope.Payload);
                _overlayRegions = overlayRegions.Regions;
                break;

            case ShellCommandTypes.Shutdown:
                ExitThread();
                break;
        }
    }

    private void OnClientDisconnected()
    {
        TraceLog.Write("broker", "client-disconnected");
        _uiContext.Post(_ => ExitThread(), null);
    }

    private void PublishHotkey(string actionId)
    {
        TraceLog.Write("hotkey", actionId);
        _ = _pipeServer.SendEventAsync(ShellEventTypes.HotkeyPressed, new HotkeyPressedEvent(actionId));
    }

    private async Task PublishClipboardAsync(string source)
    {
        var url = ClipboardService.TryGetClipboardUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            TraceLog.Write("clipboard", "capture-failed");
            await _pipeServer.SendEventAsync(
                ShellEventTypes.ShellActionFailed,
                new ShellActionFailedEvent("capture-clipboard", "Clipboard does not contain a valid http(s) URL."));
            return;
        }

        TraceLog.Write("clipboard", $"capture-success url={url}");
        await _pipeServer.SendEventAsync(
            ShellEventTypes.ClipboardUrlAvailable,
            new ClipboardPayload(url, source, DateTimeOffset.UtcNow));
    }

    private async Task OpenUrlAsync(string url)
    {
        try
        {
            TraceLog.Write("open-url", url);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await _pipeServer.SendEventAsync(
                ShellEventTypes.ShellActionFailed,
                new ShellActionFailedEvent("open-url", ex.Message));
        }
    }

    private async Task PublishOverlayClickAsync()
    {
        var isLeftButtonDown = (NativeMethods.GetAsyncKeyState(0x01) & 0x8000) != 0;
        if (!isLeftButtonDown || _wasLeftButtonDown || !_isPinned)
        {
            _wasLeftButtonDown = isLeftButtonDown;
            return;
        }

        _wasLeftButtonDown = true;

        if (_overlayRegions.Count == 0)
        {
            return;
        }

        if (!NativeMethods.GetCursorPos(out var position))
        {
            return;
        }

        OverlayRegion? region = null;
        for (var index = _overlayRegions.Count - 1; index >= 0; index--)
        {
            var candidate = _overlayRegions[index];
            if (candidate.Interactive && Contains(candidate.Bounds, position))
            {
                region = candidate;
                break;
            }
        }

        if (region is null)
        {
            return;
        }

        TraceLog.Write("overlay-click", $"role={region.Role} x={position.X} y={position.Y}");
        await _pipeServer.SendEventAsync(
            ShellEventTypes.OverlayClickReceived,
            new OverlayClickEvent(region.Role, position, DateTimeOffset.UtcNow));
    }

    private static bool Contains(RectInt bounds, PointInt point)
    {
        return point.X >= bounds.X &&
               point.X <= bounds.Right &&
               point.Y >= bounds.Y &&
               point.Y <= bounds.Bottom;
    }

    private static string CreateDesktopContextKey(DesktopContext desktopContext)
    {
        var foreground = desktopContext.ForegroundWindow;
        return string.Join(
            "|",
            foreground.ProcessId,
            foreground.Hwnd,
            foreground.ProcessName,
            foreground.Title,
            foreground.ClassName,
            foreground.IsShellSurface,
            foreground.IsFullscreenApp,
            desktopContext.WorkArea.X,
            desktopContext.WorkArea.Y,
            desktopContext.WorkArea.Width,
            desktopContext.WorkArea.Height,
            desktopContext.PrimaryMonitorBounds.X,
            desktopContext.PrimaryMonitorBounds.Y,
            desktopContext.PrimaryMonitorBounds.Width,
            desktopContext.PrimaryMonitorBounds.Height);
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int virtualKey);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out NativePoint point);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;

        public static implicit operator PointInt(NativePoint point) => new(point.X, point.Y);
    }
}
