using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

internal sealed class ShellCoordinator : IAsyncDisposable
{
    private const double HomeWindowWidth = 360;
    private const double HomeWindowHeight = 300;
    private const double ToolWindowWidth = 320;
    private const double ToolWindowHeight = 240;
    private const double RoamBandHeight = 118;
    private const double HomeMargin = 28;

    private readonly Application _application;
    private readonly string _pipeName = $"wevito-vnext-{Environment.ProcessId}";
    private readonly PetSimulationEngine _petSimulationEngine = new();
    private readonly BasketService _basketService = new(5);
    private readonly DispatcherTimer _tickTimer;

    private readonly HomePanelWindow _homeWindow = new();
    private readonly RoamBandWindow _roamBandWindow = new();
    private readonly ToolPopupWindow _toolPopupWindow = new();

    private BrokerClient? _brokerClient;
    private Process? _brokerProcess;
    private ContentRepository? _contentRepository;
    private AppRepository? _repository;
    private GameContent? _content;
    private CompanionState? _state;
    private DesktopContext? _desktopContext;
    private DateTimeOffset _lastTickAtUtc;
    private string _feedbackText = "";
    private CompanionMode? _lastLoggedMode;
    private string _lastPublishedRegionsKey = "";

    public ShellCoordinator(Application application)
    {
        _application = application;
        _tickTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _tickTimer.Tick += OnTick;

        _homeWindow.TogglePinnedRequested += async () => await TogglePinnedAsync();
        _homeWindow.ToggleBasketRequested += async () => await ToggleBasketAsync();
        _homeWindow.CaptureClipboardRequested += async () => await RequestClipboardCaptureAsync();
        _homeWindow.ActionRequested += HandleAction;
        _homeWindow.Closed += (_, _) => _application.Shutdown();

        _toolPopupWindow.CloseRequested += async () =>
        {
            if (_state is null)
            {
                return;
            }

            _state = _state with { ActiveTool = _state.ActiveTool with { IsOpen = false } };
            await PersistAndRenderAsync();
        };
        _toolPopupWindow.CopyRequested += CopyBasketItem;
        _toolPopupWindow.OpenRequested += async id => await OpenBasketItemAsync(id);
        _toolPopupWindow.DeleteRequested += async id => await DeleteBasketItemAsync(id);
        _toolPopupWindow.LinksDropped += async urls => await AddLinksAsync(urls, "drop");
    }

    public async Task StartAsync()
    {
        TraceLog.Write("shell", "startup-begin");
        var contentRoot = BrokerProcessManager.ResolveContentRoot();
        _contentRepository = new ContentRepository(contentRoot);
        _content = await _contentRepository.LoadAsync();
        TraceLog.Write("shell", $"content-loaded path={contentRoot} species={_content.Species.Count} environments={_content.Environments.Count}");

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "wevito-vnext.db");
        _repository = new AppRepository(appDataPath);
        await _repository.InitializeAsync();

        var defaultStateFactory = new DefaultStateFactory(_petSimulationEngine);
        _state = await _repository.LoadAsync() ?? defaultStateFactory.Create(_content);
        TraceLog.Write("shell", $"state-ready pinned={_state.IsPinned} pets={_state.ActivePets.Count} basket={_state.BasketItems.Count}");

        _homeWindow.Show();
        _roamBandWindow.Show();
        _toolPopupWindow.Show();

        _brokerProcess = BrokerProcessManager.Start(_pipeName);
        TraceLog.Write("shell", $"broker-started pid={_brokerProcess.Id} pipe={_pipeName}");
        _brokerClient = new BrokerClient(_pipeName);
        _brokerClient.EventReceived += OnBrokerEvent;
        await _brokerClient.ConnectAsync();
        TraceLog.Write("shell", "broker-connected");
        await _brokerClient.SendCommandAsync(ShellCommandTypes.RequestDesktopContext, new RequestDesktopContextCommand());
        await _brokerClient.SendCommandAsync(ShellCommandTypes.RegisterDropTarget, new RegisterDropTargetCommand(WindowRole.HomePanel));
        await _brokerClient.SendCommandAsync(ShellCommandTypes.RegisterDropTarget, new RegisterDropTargetCommand(WindowRole.ToolPopup));
        await _brokerClient.SendCommandAsync(ShellCommandTypes.SetPinned, new SetPinnedCommand(_state.IsPinned));

        _lastTickAtUtc = DateTimeOffset.UtcNow;
        ApplyModeAndLayout();
        _tickTimer.Start();
        TraceLog.Write("shell", "startup-complete");
    }

    private void OnBrokerEvent(ShellEventEnvelope envelope)
    {
        _ = _application.Dispatcher.InvokeAsync(async () => await HandleBrokerEventAsync(envelope));
    }

    private async Task HandleBrokerEventAsync(ShellEventEnvelope envelope)
    {
        TraceLog.Write("broker-event", envelope.EventType);
        switch (envelope.EventType)
        {
            case ShellEventTypes.DesktopContextChanged:
                _desktopContext = PipeMessage.DeserializePayload<DesktopContext>(envelope.Payload);
                TraceLog.Write("desktop-context", $"foreground={_desktopContext.ForegroundWindow.ProcessName} title={_desktopContext.ForegroundWindow.Title} shell={_desktopContext.ForegroundWindow.IsShellSurface} fullscreen={_desktopContext.ForegroundWindow.IsFullscreenApp}");
                ApplyModeAndLayout();
                break;

            case ShellEventTypes.ClipboardUrlAvailable:
                var clipboard = PipeMessage.DeserializePayload<ClipboardPayload>(envelope.Payload);
                await AddLinksAsync([clipboard.Url], clipboard.Source);
                break;

            case ShellEventTypes.HotkeyPressed:
                var hotkey = PipeMessage.DeserializePayload<HotkeyPressedEvent>(envelope.Payload);
                await HandleHotkeyAsync(hotkey.ActionId);
                break;

            case ShellEventTypes.ShellActionFailed:
                var error = PipeMessage.DeserializePayload<ShellActionFailedEvent>(envelope.Payload);
                SetFeedback(error.Message);
                break;
        }
    }

    private async Task HandleHotkeyAsync(string actionId)
    {
        switch (actionId)
        {
            case "toggle-pinned":
                await TogglePinnedAsync();
                break;
            case "capture-basket":
                await RequestClipboardCaptureAsync();
                break;
            case "open-basket":
                await ToggleBasketAsync();
                break;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_state is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var deltaSeconds = Math.Max(0.016, (now - _lastTickAtUtc).TotalSeconds);
        _lastTickAtUtc = now;

        var homeStageRect = _homeWindow.GetStageRect();
        _state = _state with
        {
            ActivePets = _petSimulationEngine.ApplyLayout(
                _state.ActivePets,
                homeStageRect.X + 24,
                homeStageRect.Y + 16,
                homeStageRect.Width - 48,
                homeStageRect.Height - 28)
        };

        var roamBandRect = _desktopContext?.WorkArea is { } workArea
            ? new RectInt(workArea.X, workArea.Bottom - (int)RoamBandHeight, workArea.Width, (int)RoamBandHeight)
            : new RectInt(0, 0, 1280, (int)RoamBandHeight);

        _state = _state with
        {
            ActivePets = _petSimulationEngine.Tick(_state.ActivePets, _state.Mode, roamBandRect, now, deltaSeconds)
        };

        Render();
    }

    private void ApplyModeAndLayout()
    {
        if (_state is null || _content is null)
        {
            return;
        }

        var workArea = _desktopContext?.WorkArea ?? new RectInt(0, 0, (int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height);
        var homeLeft = workArea.Right - HomeWindowWidth - HomeMargin;
        var homeTop = workArea.Bottom - HomeWindowHeight - 20;
        _homeWindow.Left = homeLeft;
        _homeWindow.Top = homeTop;
        _homeWindow.Width = HomeWindowWidth;
        _homeWindow.Height = HomeWindowHeight;

        _toolPopupWindow.Left = homeLeft + (HomeWindowWidth - ToolWindowWidth);
        _toolPopupWindow.Top = homeTop - ToolWindowHeight - 10;
        _toolPopupWindow.Width = ToolWindowWidth;
        _toolPopupWindow.Height = ToolWindowHeight;

        _roamBandWindow.Left = workArea.X;
        _roamBandWindow.Top = workArea.Bottom - RoamBandHeight;
        _roamBandWindow.Width = workArea.Width;
        _roamBandWindow.Height = RoamBandHeight;

        var handles = new List<long>
        {
            _homeWindow.WindowHandle,
            _roamBandWindow.WindowHandle,
            _toolPopupWindow.WindowHandle
        }.Where(handle => handle != 0).ToList();

        var nextMode = ModeReducer.Reduce(_state.IsPinned, _desktopContext, handles);
        if (nextMode != _state.Mode)
        {
            _state = _state with { Mode = nextMode };
        }
        if (_lastLoggedMode != _state.Mode)
        {
            TraceLog.Write("mode", $"mode={_state.Mode} pinned={_state.IsPinned}");
            _lastLoggedMode = _state.Mode;
        }

        TraceLog.Write("layout", $"home={homeLeft:0},{homeTop:0} {HomeWindowWidth:0}x{HomeWindowHeight:0} tool={_toolPopupWindow.Left:0},{_toolPopupWindow.Top:0} {ToolWindowWidth:0}x{ToolWindowHeight:0} roam={_roamBandWindow.Left:0},{_roamBandWindow.Top:0} {_roamBandWindow.Width:0}x{_roamBandWindow.Height:0}");

        ApplyWindowStyles();
        Render();
        _ = PublishOverlayRegionsAsync(workArea);
    }

    private void ApplyWindowStyles()
    {
        if (_state is null)
        {
            return;
        }

        var passive = _state.Mode == CompanionMode.Passive;
        var pinned = _state.Mode == CompanionMode.Pinned;

        OverlayWindowStyler.Apply(_homeWindow, passive, passive || pinned);
        OverlayWindowStyler.Apply(_roamBandWindow, true, true);
        OverlayWindowStyler.Apply(_toolPopupWindow, passive || !_state.ActiveTool.IsOpen, passive || pinned);

        _homeWindow.SetHudVisible(!passive);
        SetWindowVisibility(_roamBandWindow, Visibility.Visible, "RoamBand");
        SetWindowVisibility(_toolPopupWindow, _state.ActiveTool.IsOpen && !passive ? Visibility.Visible : Visibility.Hidden, "ToolPopup");
    }

    private void Render()
    {
        if (_state is null || _content is null)
        {
            return;
        }

        var environment = _content.Environments.First(environment => environment.Id == _state.ActiveEnvironmentId);
        _homeWindow.Render(_state, environment, _feedbackText);
        _roamBandWindow.Render(_state);
        _toolPopupWindow.Render(_state);
    }

    private async Task PublishOverlayRegionsAsync(RectInt workArea)
    {
        if (_brokerClient is null || _state is null)
        {
            return;
        }

        var regions = new List<OverlayRegion>
        {
            new(WindowRole.HomePanel, new RectInt((int)_homeWindow.Left, (int)_homeWindow.Top, (int)_homeWindow.Width, (int)_homeWindow.Height), _state.Mode != CompanionMode.Passive),
            new(WindowRole.RoamBand, new RectInt(workArea.X, workArea.Bottom - (int)RoamBandHeight, workArea.Width, (int)RoamBandHeight), false),
            new(WindowRole.ToolPopup, new RectInt((int)_toolPopupWindow.Left, (int)_toolPopupWindow.Top, (int)_toolPopupWindow.Width, (int)_toolPopupWindow.Height), _state.Mode != CompanionMode.Passive && _state.ActiveTool.IsOpen)
        };

        var regionKey = string.Join("|", regions.Select(region => $"{region.Role}:{region.Bounds.X},{region.Bounds.Y},{region.Bounds.Width},{region.Bounds.Height}:{region.Interactive}"));
        if (string.Equals(regionKey, _lastPublishedRegionsKey, StringComparison.Ordinal))
        {
            return;
        }

        await _brokerClient.SendCommandAsync(ShellCommandTypes.SetOverlayRegions, new SetOverlayRegionsCommand(regions));
        _lastPublishedRegionsKey = regionKey;
        TraceLog.Write("overlay-regions", regionKey);
    }

    private async Task TogglePinnedAsync()
    {
        if (_state is null || _brokerClient is null)
        {
            return;
        }

        _state = _state with { IsPinned = !_state.IsPinned };
        TraceLog.Write("ui-command", $"toggle-pinned next={_state.IsPinned}");
        await _brokerClient.SendCommandAsync(ShellCommandTypes.SetPinned, new SetPinnedCommand(_state.IsPinned));
        ApplyModeAndLayout();
        await PersistAsync();
    }

    private async Task ToggleBasketAsync()
    {
        if (_state is null)
        {
            return;
        }

        var nextToolState = !_state.ActiveTool.IsOpen;
        var nextPinned = _state.IsPinned;
        if (!_state.IsPinned && _state.Mode == CompanionMode.Passive && nextToolState)
        {
            nextPinned = true;
        }

        _state = _state with
        {
            IsPinned = nextPinned,
            ActiveTool = _state.ActiveTool with { IsOpen = nextToolState }
        };
        TraceLog.Write("ui-command", $"toggle-basket open={nextToolState} pinned={nextPinned}");

        if (_brokerClient is not null)
        {
            await _brokerClient.SendCommandAsync(ShellCommandTypes.SetPinned, new SetPinnedCommand(_state.IsPinned));
        }

        ApplyModeAndLayout();
        await PersistAsync();
    }

    private async Task RequestClipboardCaptureAsync()
    {
        if (_brokerClient is null)
        {
            return;
        }

        await _brokerClient.SendCommandAsync(ShellCommandTypes.CaptureClipboard, new CaptureClipboardCommand());
        TraceLog.Write("ui-command", "capture-clipboard");
    }

    private void HandleAction(string actionId)
    {
        if (_state is null)
        {
            return;
        }

        _feedbackText = actionId switch
        {
            "feed" => "A snack appears in the corner habitat.",
            "pet" => "The pets settle and lean into the attention.",
            "rest" => "Everyone is encouraged back to the environment.",
            _ => "Action applied."
        };
        TraceLog.Write("action", actionId);

        if (actionId == "rest")
        {
            _state = _state with
            {
                ActivePets = _state.ActivePets
                    .Select(pet => pet with
                    {
                        TargetX = pet.HomeX,
                        TargetY = pet.HomeY,
                        BehaviorState = PetBehaviorState.Recalling
                    })
                    .ToList()
            };
        }

        Render();
        _ = PersistAsync();
    }

    private async Task AddLinksAsync(IEnumerable<string> urls, string source)
    {
        if (_state is null)
        {
            return;
        }

        var basketItems = _state.BasketItems.ToList();
        foreach (var url in urls)
        {
            if (!BasketService.TryCreate(url, url, source, out var item))
            {
                continue;
            }

            basketItems = _basketService.Add(item, basketItems).ToList();
        }

        _state = _state with { BasketItems = basketItems };
        SetFeedback(basketItems.Count == 0 ? "No valid links found." : $"Basket now holds {basketItems.Count} link(s).");
        TraceLog.Write("basket", $"source={source} count={basketItems.Count}");
        await PersistAndRenderAsync();
    }

    private void CopyBasketItem(Guid id)
    {
        if (_state is null)
        {
            return;
        }

        var item = _basketService.Get(id, _state.BasketItems);
        if (item is null)
        {
            return;
        }

        Clipboard.SetText(item.Url);
        SetFeedback($"Copied {item.Label}.");
        TraceLog.Write("basket", $"copy id={id} url={item.Url}");
        Render();
    }

    private async Task OpenBasketItemAsync(Guid id)
    {
        if (_state is null || _brokerClient is null)
        {
            return;
        }

        var item = _basketService.Get(id, _state.BasketItems);
        if (item is null)
        {
            return;
        }

        Clipboard.SetText(item.Url);
        await _brokerClient.SendCommandAsync(ShellCommandTypes.OpenUrl, new OpenUrlCommand(item.Url));
        SetFeedback($"Opened {item.Label}.");
        TraceLog.Write("basket", $"open id={id} url={item.Url}");
        Render();
    }

    private async Task DeleteBasketItemAsync(Guid id)
    {
        if (_state is null)
        {
            return;
        }

        _state = _state with { BasketItems = _basketService.Remove(id, _state.BasketItems) };
        TraceLog.Write("basket", $"delete id={id} count={_state.BasketItems.Count}");
        await PersistAndRenderAsync();
    }

    private void SetFeedback(string message)
    {
        _feedbackText = message;
        TraceLog.Write("feedback", message);
        Render();
    }

    private async Task PersistAndRenderAsync()
    {
        Render();
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        if (_repository is not null && _state is not null)
        {
            await _repository.SaveAsync(_state);
            TraceLog.Write("persistence", $"saved pets={_state.ActivePets.Count} basket={_state.BasketItems.Count} pinned={_state.IsPinned}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        TraceLog.Write("shell", "shutdown-begin");
        _tickTimer.Stop();
        if (_state is not null)
        {
            await PersistAsync();
        }

        if (_brokerClient is not null)
        {
            try
            {
                await _brokerClient.SendCommandAsync(ShellCommandTypes.Shutdown, new ShutdownBrokerCommand());
            }
            catch
            {
                // Ignore shutdown race.
            }
            await _brokerClient.DisposeAsync();
        }

        if (_brokerProcess is not null && !_brokerProcess.HasExited)
        {
            _brokerProcess.WaitForExit(1000);
        }

        _homeWindow.CloseSilently();
        _roamBandWindow.CloseSilently();
        _toolPopupWindow.CloseSilently();
        TraceLog.Write("shell", "shutdown-complete");
    }

    private static void SetWindowVisibility(Window window, Visibility visibility, string name)
    {
        if (window.Visibility == visibility)
        {
            return;
        }

        window.Visibility = visibility;
        TraceLog.Write("visibility", $"window={name} visibility={visibility}");
    }
}
