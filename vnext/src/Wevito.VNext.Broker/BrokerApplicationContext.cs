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
    private readonly System.Windows.Forms.Timer _desktopPollTimer;
    private readonly ToolStripMenuItem _pinMenuItem;

    private bool _isPinned;
    private DesktopContext? _lastDesktopContext;
    private string _lastDesktopContextKey = "";
    private ForegroundWindowInfo? _lastForegroundWindow;
    private RectInt? _lastWorkArea;

    public BrokerApplicationContext(string pipeName)
    {
        TraceLog.Write("broker", $"startup pipe={pipeName}");
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _pipeServer = new PipeServer(pipeName);
        _pipeServer.CommandReceived += OnCommandReceivedAsync;
        _pipeServer.ClientDisconnected += OnClientDisconnected;

        _desktopContextService = new DesktopContextService();
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.ActionPressed += PublishHotkey;

        _pinMenuItem = new ToolStripMenuItem("Pin HUD", null, (_, _) => PublishHotkey("toggle-pinned"));
        var captureMenuItem = new ToolStripMenuItem("Capture Clipboard Link", null, (_, _) => PublishHotkey("capture-basket"));
        var basketMenuItem = new ToolStripMenuItem("Open Basket", null, (_, _) => PublishHotkey("open-basket"));

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
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())
        ]);

        _desktopPollTimer = new System.Windows.Forms.Timer
        {
            Interval = 120
        };
        _desktopPollTimer.Tick += async (_, _) => await PublishDesktopContextAsync();
        _desktopPollTimer.Start();

        _ = _pipeServer.RunAsync();
        TraceLog.Write("broker", "startup-complete");
    }

    protected override void ExitThreadCore()
    {
        TraceLog.Write("broker", "shutdown-begin");
        _desktopPollTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _hotkeyWindow.Dispose();
        _pipeServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
        TraceLog.Write("broker", "shutdown-complete");
        base.ExitThreadCore();
    }

    private async Task PublishDesktopContextAsync()
    {
        var desktopContext = _desktopContextService.Capture();
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
            case ShellCommandTypes.SetOverlayRegions:
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
}
