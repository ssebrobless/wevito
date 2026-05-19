using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.LocalRetrieval;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Invariants;
using Wevito.VNext.Core.SelfImprovement.Maturity;
using Wevito.VNext.Core.Settings;
using Wevito.VNext.Core.Tools;

namespace Wevito.VNext.Shell;

internal sealed class ShellCoordinator : IAsyncDisposable
{
    private const double HomeWindowWidth = 660;
    private const double HomeWindowFullHeight = 910;
    private const double HomeWindowCompactHeight = 560;
    private const double HomeWindowPassiveHeight = 340;
    private const double ToolWindowWidth = 520;
    private const double ToolWindowHeight = 720;
    private const double DevToolWindowWidth = 520;
    private const double DevToolWindowHeight = 760;
    private const double RoamBandHeight = 118;
    private const double HomeMargin = 28;

    private readonly Application _application;
    private readonly string _pipeName = $"wevito-vnext-{Environment.ProcessId}";
    private readonly PetSimulationEngine _petSimulationEngine = new();
    private readonly PetWellbeingInterpreter _petWellbeingInterpreter = new();
    private readonly BasketService _basketService = new(5);
    private readonly ChatInputBarService _petCommandBarService = new();
    private readonly AgentSlotService _agentSlotService = new();
    private readonly AgentTaskCardQueueService _petTaskCardQueueService = new();
    private readonly AuditLedgerService _auditLedgerService = new();
    private readonly ActivitySummaryService _activitySummaryService;
    private readonly EvidenceSummaryService _evidenceSummaryService;
    private readonly MaturityScoreboardService _maturityScoreboardService;
    private readonly LiveStatusFeed _liveStatusFeed;
    private readonly EvidenceCollectionStatusService _evidenceCollectionStatusService;
    private readonly KillSwitchService _killSwitchService;
    private readonly AiIdentityService _aiIdentityService;
    private readonly FirstLaunchWizardStateService _firstLaunchWizardStateService;
    private readonly OllamaModelBootstrapService _ollamaModelBootstrapService;
    private readonly LocalBrainHeartbeatService _localBrainHeartbeatService;
    private readonly LocalBrainStatusPanelService _localBrainStatusPanelService;
    private readonly LocalDocumentRetrievalService _localDocumentRetrievalService;
    private readonly AgentToolDispatcher _petTaskAdapterPreviewDispatcher;
    private readonly RuntimeSupervisorService _runtimeSupervisorService = new();
    private readonly RuntimeBudgetMeter _runtimeBudgetMeter = new();
    private readonly CoexistenceTriggerService _coexistenceTriggerService;
    private readonly DoNotDisturbScheduleService _doNotDisturbScheduleService;
    private readonly ProcessPriorityManagerService _processPriorityManagerService;
    private readonly GameModeDetectorService _gameModeDetectorService;
    private readonly NotificationPolicyService _notificationPolicyService;
    private readonly FocusDisciplineService _focusDisciplineService;
    private readonly AudioOutputPolicyService _audioOutputPolicyService;
    private readonly MultiMonitorService _multiMonitorService;
    private readonly CursorReactivityService _cursorReactivityService;
    private readonly TrayIconDisciplineService _trayIconDisciplineService = new();
    private readonly WindowsForegroundFullscreenMonitor _fullscreenMonitor;
    private readonly WindowsPowerHandler _powerHandler;
    private readonly FocusStealCounter _focusStealCounter = new();
    private readonly RuntimeSessionTracker _runtimeSessionTracker;
    private readonly DailyEvidenceSnapshotService _dailyEvidenceSnapshotService;
    private readonly AutonomousTaskScheduler _autonomousTaskScheduler;
    private readonly AutonomousBetaDecisionService _autonomousBetaDecisionService;
    private readonly AutonomousScopeService _autonomousScopeService;
    private readonly IReadOnlyList<IAutonomousScope> _autonomousScopes;
    private readonly SupervisedImprovementLoop _supervisedImprovementLoop;
    private readonly InvariantViolationWatchdog _invariantViolationWatchdog;
    private readonly AutonomousOperationsLoop _autonomousOperationsLoop;
    private readonly TranslationExecutionAdapter _translationExecutionAdapter = new();
    private readonly AudioAssistExecutionAdapter _audioAssistExecutionAdapter = new();
    private readonly BuildProofExecutionAdapter _buildProofExecutionAdapter = new();
    private readonly ScreenCaptureExecutionAdapter _screenCaptureExecutionAdapter;
    private readonly RegionSelectionStore _regionSelectionStore = new();
    private readonly DispatcherTimer _tickTimer;
    private readonly bool _devToolsEnabled = BrokerProcessManager.IsDevelopmentBuild();

    private readonly HomePanelWindow _homeWindow = new();
    private readonly RoamBandWindow _roamBandWindow = new();
    private readonly ToolPopupWindow _toolPopupWindow = new();
    private SpriteWorkflowV2Window? _spriteWorkflowV2Window;
    private CreativeLearningLabWindow? _creativeLearningLabWindow;
    private LocalBrainStatusPanelWindow? _localBrainStatusPanelWindow;
    private DevControlServer? _devControlServer;

    private BrokerClient? _brokerClient;
    private Process? _brokerProcess;
    private ContentRepository? _contentRepository;
    private AppRepository? _repository;
    private SpriteAssetService? _assetService;
    private GameContent? _content;
    private CompanionState? _state;
    private DesktopContext? _desktopContext;
    private DateTimeOffset _lastTickAtUtc;
    private DateTimeOffset _lastSchedulerPollAtUtc;
    private DateTimeOffset _lastForegroundPollAtUtc;
    private DateTimeOffset _lastForcedPetAnimationAtUtc;
    private string _feedbackText = "";
    private string _lastForcedPetAnimationReason = "";
    private CompanionMode? _lastLoggedMode;
    private string _lastPublishedRegionsKey = "";
    private ChatInputBarState? _petCommandBarState;
    private LocalBrainStatus _localBrainStatus = LocalBrainStatus.Starting(DateTimeOffset.UtcNow);
    private bool _localBrainHeartbeatInFlight;

    public ShellCoordinator(Application application)
    {
        _application = application;
        var plainLanguageExplainer = new PlainLanguageExplainer(kind => TraceLog.Write("activity", $"WarnUnknownPacketKind kind={kind}"));
        _activitySummaryService = new ActivitySummaryService(_auditLedgerService, plainLanguageExplainer);
        _liveStatusFeed = new LiveStatusFeed(_auditLedgerService, plainLanguageExplainer);
        _killSwitchService = new KillSwitchService(() => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), _auditLedgerService);
        _evidenceSummaryService = new EvidenceSummaryService(_auditLedgerService.DatabasePath, _auditLedgerService, _killSwitchService);
        _maturityScoreboardService = new MaturityScoreboardService(_auditLedgerService.DatabasePath, _auditLedgerService, _killSwitchService);
        _aiIdentityService = new AiIdentityService(_auditLedgerService, _killSwitchService);
        _firstLaunchWizardStateService = new FirstLaunchWizardStateService(_aiIdentityService, _auditLedgerService, _killSwitchService);
        _coexistenceTriggerService = new CoexistenceTriggerService(_auditLedgerService, _killSwitchService);
        _doNotDisturbScheduleService = new DoNotDisturbScheduleService(_auditLedgerService, _killSwitchService);
        _processPriorityManagerService = new ProcessPriorityManagerService(_auditLedgerService, _killSwitchService);
        _gameModeDetectorService = new GameModeDetectorService(auditLedgerService: _auditLedgerService, killSwitchService: _killSwitchService);
        _notificationPolicyService = new NotificationPolicyService(_auditLedgerService, _killSwitchService);
        _focusDisciplineService = new FocusDisciplineService(_auditLedgerService, _killSwitchService);
        _audioOutputPolicyService = new AudioOutputPolicyService(_auditLedgerService, _killSwitchService);
        _multiMonitorService = new MultiMonitorService(_auditLedgerService, _killSwitchService);
        _cursorReactivityService = new CursorReactivityService(_auditLedgerService, _killSwitchService);
        _ollamaModelBootstrapService = new OllamaModelBootstrapService(
            probeService: new LocalRuntimeProbeService(killSwitchService: _killSwitchService),
            auditLedgerService: _auditLedgerService,
            killSwitchService: _killSwitchService,
            artifactRoot: Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "model-bootstrap"));
        _localBrainHeartbeatService = new LocalBrainHeartbeatService(
            probeService: new LocalRuntimeProbeService(killSwitchService: _killSwitchService),
            auditLedgerService: _auditLedgerService,
            killSwitchService: _killSwitchService);
        _localBrainStatusPanelService = new LocalBrainStatusPanelService(
            _localBrainHeartbeatService,
            new LocalRuntimeProbeService(killSwitchService: _killSwitchService),
            _auditLedgerService,
            _killSwitchService);
        _localDocumentRetrievalService = new LocalDocumentRetrievalService(
            settingsProvider: () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            auditLedgerService: _auditLedgerService,
            killSwitchService: _killSwitchService);
        _evidenceCollectionStatusService = new EvidenceCollectionStatusService(
            _auditLedgerService,
            Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "soak"),
            killSwitchService: _killSwitchService);
        _runtimeSessionTracker = new RuntimeSessionTracker(_auditLedgerService, killSwitchService: _killSwitchService);
        _dailyEvidenceSnapshotService = new DailyEvidenceSnapshotService(_auditLedgerService, _runtimeBudgetMeter, _focusStealCounter, killSwitchService: _killSwitchService);
        _fullscreenMonitor = new WindowsForegroundFullscreenMonitor(_auditLedgerService);
        _powerHandler = new WindowsPowerHandler(_auditLedgerService);
        var onnxPhiAdapter = new OnnxPhiLocalModelAdapter(
            settingsProvider: () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            killSwitchService: _killSwitchService);
        var activeLocalModelAdapter = new LocalModelProviderRouterAdapter(
            () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            probeService: new LocalRuntimeProbeService(killSwitchService: _killSwitchService),
            ollamaAdapter: new OllamaLocalModelAdapter(
                settingsProvider: () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                toolProvider: () => ToolRegistry.CreateDefault(
                    settingsProvider: () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).LlmVisibleDescriptors,
                killSwitchService: _killSwitchService),
            onnxPhiAdapter: onnxPhiAdapter,
            deterministicAdapter: new LocalModelAdapter(_killSwitchService),
            onnxPhiWeightsPresent: () => onnxPhiAdapter.HasWeights);
        _toolPopupWindow.ConfigureChatServices(activeLocalModelAdapter, _auditLedgerService, _killSwitchService, ResolveRepoRootOrBaseDirectory());
        _petTaskAdapterPreviewDispatcher = new AgentToolDispatcher(activeLocalModelAdapter: activeLocalModelAdapter, auditLedgerService: _auditLedgerService, killSwitchService: _killSwitchService);
        _autonomousTaskScheduler = new AutonomousTaskScheduler(_runtimeSupervisorService, _runtimeBudgetMeter, _auditLedgerService, _killSwitchService);
        _autonomousBetaDecisionService = new AutonomousBetaDecisionService(_auditLedgerService);
        _autonomousScopeService = new AutonomousScopeService(_auditLedgerService, _killSwitchService);
        var repoRoot = ResolveRepoRootOrBaseDirectory();
        _autonomousScopes =
        [
            new SpriteRepairTriageScope(Path.Combine(repoRoot, "vnext", "artifacts", "c-phase-128-sprite-repair-queue", "repair_queue.json"), _auditLedgerService, _petTaskCardQueueService),
            new SpriteRepairBatchProposalScope(Path.Combine(repoRoot, "vnext", "artifacts", "c-phase-128-sprite-repair-queue", "repair_queue.json"), _auditLedgerService),
            new AuditLedgerCleanupScope(Path.GetDirectoryName(_auditLedgerService.DatabasePath) ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wevito", "audit"), _auditLedgerService)
        ];
        var autonomousScopeRegistry = new AutonomousScopeRegistry(_autonomousScopeService, _autonomousScopes);
        _supervisedImprovementLoop = ShellCompositionRoot.CreateSupervisedImprovementLoop(_auditLedgerService, _killSwitchService);
        _invariantViolationWatchdog = ShellCompositionRoot.CreateInvariantViolationWatchdog(
            _auditLedgerService,
            _killSwitchService,
            () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        _autonomousOperationsLoop = new AutonomousOperationsLoop(_autonomousBetaDecisionService, _auditLedgerService, _killSwitchService, scopeRegistry: autonomousScopeRegistry, supervisedImprovementLoop: _supervisedImprovementLoop);
        _screenCaptureExecutionAdapter = new ScreenCaptureExecutionAdapter(new WindowsGraphicsCaptureBackend(() => _homeWindow));
        _tickTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _tickTimer.Tick += OnTick;

        _homeWindow.TogglePinnedRequested += async () => await TogglePinnedAsync();
        _homeWindow.ToggleActionsRequested += async () => await ToggleActionsAsync();
        _homeWindow.ToggleWebToolsRequested += async () => await ToggleWebToolsAsync();
        _homeWindow.ToggleBasketRequested += async () => await ToggleBasketAsync();
        _homeWindow.ToggleHelpersRequested += async () => await ToggleHelpersAsync();
        _homeWindow.OpenSpriteWorkflowV2Requested += async () => await OpenSpriteWorkflowV2Async();
        _homeWindow.OpenCreativeLearningLabRequested += async () => await OpenCreativeLearningLabAsync();
        _homeWindow.OpenBenchmarksRequested += async () => await ToggleBenchmarksAsync();
        _homeWindow.OpenLocalBrainStatusRequested += async () => await OpenLocalBrainStatusPanelAsync();
        _homeWindow.OpenSettingsRequested += async () => await ToggleSettingsAsync();
        _homeWindow.ToggleCompactRequested += async () => await ToggleCompactHudAsync();
        _homeWindow.SaveRequested += async () => await SaveAsync();
        _homeWindow.ToggleDevRequested += async () => await ToggleDevAsync();
        _homeWindow.StopEverythingRequested += async () => await ActivateKillSwitchAsync();
        _homeWindow.ToggleDoNotDisturbRequested += async () => await ToggleDoNotDisturbAsync();
        _homeWindow.StarterEggRequested += async colorVariant => await AddStarterEggAsync(colorVariant);
        _homeWindow.FirstRunChoiceRequested += async choice => await HandleFirstRunChoiceAsync(choice);
        _homeWindow.ActionRequested += HandleAction;
        _homeWindow.ActionOptionRequested += async (actionId, itemId) => await ApplyActionSelectionAsync(actionId, itemId);
        _homeWindow.Closed += (_, _) => _application.Shutdown();
        _homeWindow.RegisterFocusStealCounter(_focusStealCounter, () => _fullscreenMonitor.IsFullscreenOther);
        _roamBandWindow.RegisterFocusStealCounter(_focusStealCounter, () => _fullscreenMonitor.IsFullscreenOther);
        _powerHandler.RuntimeEventObserved += runtimeEvent =>
        {
            _ = _application.Dispatcher.InvokeAsync(async () => await HandlePowerRuntimeEventAsync(runtimeEvent));
        };

        _toolPopupWindow.CloseRequested += async () =>
        {
            if (_state is null)
            {
                return;
            }

            _state = CloseToolSession(_state);
            await SyncPinnedAsync();
            await PersistAndRenderAsync();
        };
        _toolPopupWindow.PasteRequested += async () => await RequestClipboardCaptureAsync();
        _toolPopupWindow.SaveRequested += async () => await SaveAsync();
        _toolPopupWindow.OpenDevRequested += async () => await ToggleDevAsync();
        _toolPopupWindow.OpenRequested += async id => await OpenBasketItemAsync(id);
        _toolPopupWindow.DeleteRequested += async ids => await DeleteBasketItemsAsync(ids);
        _toolPopupWindow.LinksDropped += async urls => await AddLinksAsync(urls, "drop");
        _toolPopupWindow.SettingChanged += OnSettingChanged;
        _toolPopupWindow.ToolTabRequested += async toolId => await OpenToolTabAsync(toolId);
        _toolPopupWindow.LocalDocsBuildRequested += async () => await HandleLocalDocsBuildAsync();
        _toolPopupWindow.LocalDocsQueryRequested += async query => await HandleLocalDocsQueryAsync(query);
        _toolPopupWindow.RunFirstLaunchWizardRequested += async () => await RunFirstLaunchWizardAsync(force: true);
        _toolPopupWindow.AutonomousBetaConsentConfirmed += async () => await EnableAutonomousBetaAfterConsentAsync();
        _toolPopupWindow.AutonomousScopePreviewRequested += async scopeId => await PreviewAutonomousScopeAsync(scopeId);
        _toolPopupWindow.EvidenceSummaryExportRequested += async () => await ExportEvidenceSummaryAsync();
        _toolPopupWindow.DevToolCommandRequested += async command => await HandleDevToolCommandAsync(command);
        _toolPopupWindow.ActionMenuRequested += actionId =>
        {
            HandleAction(actionId);
            return Task.CompletedTask;
        };
        _toolPopupWindow.ActionOptionRequested += async (actionId, itemId) => await ApplyActionSelectionAsync(actionId, itemId);
        _toolPopupWindow.PetCommandSubmitted += async input => await HandlePetCommandSubmittedAsync(input);
        _toolPopupWindow.PetTaskStatusChangeRequested += async (cardId, status) => await HandlePetTaskStatusChangeAsync(cardId, status);
        _toolPopupWindow.PetTaskPreviewRequested += async cardId => await HandlePetTaskPreviewAsync(cardId);
        _toolPopupWindow.PetTaskExecutionRequested += async cardId => await HandlePetTaskExecutionAsync(cardId);
        _toolPopupWindow.SupervisedApplyApprovalRequested += async (cardId, approval) => await HandleSupervisedApplyApprovalAsync(cardId, approval);
    }

    public async Task StartAsync()
    {
        TraceLog.Write("shell", "startup-begin");
        var contentRoot = BrokerProcessManager.ResolveContentRoot();
        _contentRepository = new ContentRepository(contentRoot);
        var preferAuthored = string.Equals(Environment.GetEnvironmentVariable("WEVITO_VNEXT_PREFER_AUTHORED"), "1", StringComparison.OrdinalIgnoreCase);
        var preferAuthoredLocomotion = preferAuthored ||
                                       string.Equals(Environment.GetEnvironmentVariable("WEVITO_VNEXT_PREFER_AUTHORED_LOCOMOTION"), "1", StringComparison.OrdinalIgnoreCase);
        var disableVerifiedLocomotion = string.Equals(Environment.GetEnvironmentVariable("WEVITO_VNEXT_DISABLE_AUTHORED_LOCOMOTION"), "1", StringComparison.OrdinalIgnoreCase);
        var preferVerifiedLocomotion = preferAuthoredLocomotion && !disableVerifiedLocomotion;
        TraceLog.Write("sprite-source", $"preferAuthored={preferAuthored} preferVerifiedLocomotion={preferVerifiedLocomotion}");
        _assetService = new SpriteAssetService(
            BrokerProcessManager.ResolveSpriteAuthoredRoot(),
            BrokerProcessManager.ResolveSpriteRuntimeRoot(),
            BrokerProcessManager.ResolveSharedSpriteRuntimeRoot(),
            BrokerProcessManager.ResolveSpriteRoot(),
            preferVerifiedLocomotion,
            preferAuthored);
        _content = await _contentRepository.LoadAsync();
        TraceLog.Write("shell", $"content-loaded path={contentRoot} species={_content.Species.Count} environments={_content.Environments.Count}");

        var dataRoot = ResolveDataRoot();
        var appDataPath = Path.Combine(dataRoot, AppRepository.DefaultDatabaseFileName);
        _repository = new AppRepository(appDataPath);
        await _repository.InitializeAsync();
        _runtimeBudgetMeter.EnsureStateFile();
        _powerHandler.Subscribe();

        var defaultStateFactory = new DefaultStateFactory(_petSimulationEngine);
        _state = await _repository.LoadAsync() ?? defaultStateFactory.Create(_content);
        _state = HydrateLoadedState(_state, _content);
        _state = ApplyAuditScenarioOverride(_state, _content);
        _state = RecordToolHubLayoutChangedOnce(_state);
        _processPriorityManagerService.ApplyBelowNormalToCurrentProcess(_state.SettingsSnapshot);
        _coexistenceTriggerService.EnsureDefaultAppListFile();
        _doNotDisturbScheduleService.EnsureDefaultScheduleFile();
        var bootstrapStatus = await _ollamaModelBootstrapService.ProbeStartupAsync(
            _state.SettingsSnapshot,
            EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: false),
            DateTimeOffset.UtcNow);
        TraceLog.Write("local-ai", $"ollama-bootstrap kind={bootstrapStatus.PacketKind} summary={bootstrapStatus.Summary}");
        _localBrainStatus = await _localBrainHeartbeatService.TickAsync(
            _state.SettingsSnapshot,
            EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: false),
            DateTimeOffset.UtcNow);
        TraceLog.Write("local-ai", $"local-brain status={_localBrainStatus.Availability} reason={_localBrainStatus.Reason}");
        TraceLog.Write("shell", $"state-ready pinned={_state.IsPinned} pets={_state.ActivePets.Count} basket={_state.BasketItems.Count}");
        TraceDisciplinePolicySnapshot(_state.SettingsSnapshot);

        ShowWindowWithFocusDiscipline(_homeWindow, "HomePanel", userInitiated: false);
        ShowWindowWithFocusDiscipline(_roamBandWindow, "RoamBand", userInitiated: false);
        ShowWindowWithFocusDiscipline(_toolPopupWindow, "ToolPopup", userInitiated: false);
        await RunFirstLaunchWizardAsync(force: false);

        _brokerProcess = BrokerProcessManager.Start(_pipeName);
        _processPriorityManagerService.ApplyBelowNormal(ProcessPriorityManagerService.FromProcess(_brokerProcess), _state.SettingsSnapshot);
        TraceLog.Write("shell", $"broker-started pid={_brokerProcess.Id} pipe={_pipeName}");
        _brokerClient = new BrokerClient(_pipeName);
        _brokerClient.EventReceived += OnBrokerEvent;
        await _brokerClient.ConnectAsync();
        TraceLog.Write("shell", "broker-connected");
        await _brokerClient.SendCommandAsync(ShellCommandTypes.RequestDesktopContext, new RequestDesktopContextCommand());
        await _brokerClient.SendCommandAsync(ShellCommandTypes.RegisterDropTarget, new RegisterDropTargetCommand(WindowRole.HomePanel));
        await _brokerClient.SendCommandAsync(ShellCommandTypes.RegisterDropTarget, new RegisterDropTargetCommand(WindowRole.ToolPopup));
        await _brokerClient.SendCommandAsync(ShellCommandTypes.SetPinned, new SetPinnedCommand(_state.IsPinned));

        if (_devToolsEnabled)
        {
            _devControlServer = new DevControlServer(
                DevControlServer.ResolvePipeName(),
                _application.Dispatcher,
                HandleDevControlCommandAsync);
            _devControlServer.Start();
            TraceLog.Write("dev-control", $"server-started pipe={DevControlServer.ResolvePipeName()}");
        }

        _lastTickAtUtc = DateTimeOffset.UtcNow;
        ApplyModeAndLayout();
        if (!IsVisualQaFastMode())
        {
            _tickTimer.Start();
        }
        else
        {
            TraceLog.Write("visual-qa", "tick-timer-skipped-for-fast-mode");
        }
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
                _fullscreenMonitor.Observe(_desktopContext, DateTimeOffset.UtcNow);
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

            case ShellEventTypes.OverlayClickReceived:
                var overlayClick = PipeMessage.DeserializePayload<OverlayClickEvent>(envelope.Payload);
                await HandleOverlayClickAsync(overlayClick);
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
            case "open-helpers":
                await ToggleHelpersAsync();
                break;
            case "open-dev-tools":
                if (_devToolsEnabled)
                {
                    await ToggleDevAsync();
                }
                break;
        }
    }

    private async Task HandleOverlayClickAsync(OverlayClickEvent overlayClick)
    {
        var handled = overlayClick.Role switch
        {
            WindowRole.HomePanel => await _homeWindow.TryInvokeOverlayClickAsync(overlayClick.ScreenPosition),
            WindowRole.ToolPopup => await _toolPopupWindow.TryInvokeOverlayClickAsync(overlayClick.ScreenPosition),
            _ => false
        };

        if (handled)
        {
            TraceLog.Write("overlay-click", $"handled role={overlayClick.Role} x={overlayClick.ScreenPosition.X} y={overlayClick.ScreenPosition.Y}");
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
                _homeWindow.Left + homeStageRect.X + 24,
                _homeWindow.Top + homeStageRect.Y + 20,
                homeStageRect.Width - 48,
                homeStageRect.Height - 36)
        };

        TryPollForegroundContext(now);
        _runtimeBudgetMeter.FlushIfDue();
        _runtimeSessionTracker.Tick(now, _state.SettingsSnapshot);
        _dailyEvidenceSnapshotService.Tick(now, _runtimeSupervisorService.ReadBudgetSnapshot(_state.SettingsSnapshot), _state.SettingsSnapshot);
        TryScanInvariantViolations(now);
        TryPollAutonomousScheduler(now);
        TryRunAutonomousOperationsBeta(now);

        var roamBandRect = ResolveRoamBandRect();
        var petMotionBounds = ResolvePetMotionBounds(roamBandRect, homeStageRect);
        var petMotionMode = ShellPresentationRules.IsActionsSurfaceOpen(_state.ActiveTool)
            ? CompanionMode.Passive
            : _state.Mode;

        _state = _state with
        {
            ActivePets = _petSimulationEngine.Tick(_state.ActivePets, petMotionMode, petMotionBounds, now, deltaSeconds)
        };
        _state = ApplyAmbientWorkCompanionState(_state, now);
        TryPollLocalBrainHeartbeat(now);

        Render();
    }

    private void TryScanInvariantViolations(DateTimeOffset now)
    {
        if (_state is null || !GetSettingBool(_state.SettingsSnapshot, InvariantViolationWatchdog.EnabledSetting))
        {
            return;
        }

        try
        {
            var results = _invariantViolationWatchdog.Scan(now);
            var triggered = results.Count(result => result.Triggered);
            if (triggered > 0)
            {
                TraceLog.Write("self-improvement", $"invariant-watchdog triggered={triggered}");
            }
        }
        catch (Exception ex)
        {
            TraceLog.Write("self-improvement", $"invariant-watchdog failed={ex.GetType().Name}");
        }
    }

    private void TryPollLocalBrainHeartbeat(DateTimeOffset now)
    {
        if (_state is null || _localBrainHeartbeatInFlight)
        {
            return;
        }

        _localBrainHeartbeatInFlight = true;
        var settings = _state.SettingsSnapshot;
        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: _state.ActiveTool.IsOpen);
        _ = _localBrainHeartbeatService.TickAsync(settings, supervisorStatus, now)
            .ContinueWith(task =>
            {
                _localBrainHeartbeatInFlight = false;
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    _localBrainStatus = task.Result;
                    TraceLog.Write("local-ai", $"local-brain heartbeat status={_localBrainStatus.Availability} reason={_localBrainStatus.Reason}");
                }
                else if (task.Exception is not null)
                {
                    TraceLog.Write("local-ai", $"local-brain heartbeat failed={task.Exception.GetBaseException().GetType().Name}");
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void TryPollForegroundContext(DateTimeOffset now)
    {
        if (_brokerClient is null)
        {
            return;
        }

        if (_lastForegroundPollAtUtc != default && now - _lastForegroundPollAtUtc < WindowsForegroundFullscreenMonitor.PollInterval)
        {
            return;
        }

        _lastForegroundPollAtUtc = now;
        _ = _brokerClient.SendCommandAsync(ShellCommandTypes.RequestDesktopContext, new RequestDesktopContextCommand());
    }

    private void TryPollAutonomousScheduler(DateTimeOffset now)
    {
        if (_state is null)
        {
            return;
        }

        if (_lastSchedulerPollAtUtc != default && now - _lastSchedulerPollAtUtc < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _lastSchedulerPollAtUtc = now;
        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: false);
        var triggers = AutonomousTaskScheduler.BuildShellTriggers(_state.TaskCards, _state.SettingsSnapshot, now);
        var request = new AutonomousSchedulerRequest(
            _state.SettingsSnapshot,
            supervisorStatus,
            _runtimeSupervisorService.ReadBudgetSnapshot(_state.SettingsSnapshot),
            triggers,
            _state.TaskCards ?? [],
            Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "pet-tasks"),
            now);
        var result = _autonomousTaskScheduler.TryCreateProposal(request);
        if (!result.Created || result.TaskCard is null)
        {
            return;
        }

        var taskCards = _petTaskCardQueueService.AppendDraft(_state.TaskCards, result.TaskCard);
        _state = _state with { TaskCards = taskCards };
        var helpers = BuildHelperProfiles(_state.ActivePets);
        _petCommandBarState = new ChatInputBarState(
            helpers,
            result.TaskCard.Intent.RawText,
            result.TaskCard.Intent,
            result.TaskCard,
            LastPolicyDecision: null,
            StatusMessage: "Wevito proposed a background draft. No preview or execution has started.",
            UpdatedAtUtc: now,
            QueuedTaskCards: taskCards,
            WellbeingSnapshots: _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets));
        SetFeedback("Scheduler proposed a draft task card. Preview it manually if useful.");
        TraceLog.Write("scheduler", $"proposal card={result.TaskCard.Id} family={result.TaskCard.ToolFamily} artifact={result.SummaryPath}");
        _ = PersistAsync();
    }

    private void TryRunAutonomousOperationsBeta(DateTimeOffset now)
    {
        if (_state is null)
        {
            return;
        }

        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: false);
        var result = _autonomousOperationsLoop.TryRunIteration(new AutonomousOperationsRequest(
            _state.SettingsSnapshot,
            supervisorStatus,
            Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "pet-tasks"),
            now,
            _state.TaskCards ?? []));
        if (!result.Ran)
        {
            return;
        }

        if (result.TaskCards is not null)
        {
            _state = _state with { TaskCards = result.TaskCards };
            _petCommandBarState = BuildChatInputBarState(
                BuildHelperProfiles(_state.ActivePets),
                result.TaskCards,
                _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets));
            _ = PersistAsync();
        }

        TraceLog.Write("autonomous-beta", $"iteration artifact={result.ArtifactFolder} mutate={result.DidMutate}");
        SetFeedback(result.DidMutate
            ? "Autonomous beta ran an enabled scope and recorded an audit-safe file move."
            : "Autonomous beta wrote a proposal-only activity packet. No sprite mutation was applied.");
    }

    private void ApplyModeAndLayout()
    {
        if (_state is null || _content is null)
        {
            return;
        }

        var workArea = _desktopContext?.WorkArea ?? new RectInt(0, 0, (int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height);
        var roamBandRect = ResolveRoamBandRect();
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

        var homeHeight = GetHomeWindowHeight();
        var homeLeft = workArea.Right - HomeWindowWidth - HomeMargin;
        var homeTop = workArea.Bottom - homeHeight - 20;
        _homeWindow.Left = homeLeft;
        _homeWindow.Top = homeTop;
        _homeWindow.Width = HomeWindowWidth;
        _homeWindow.Height = homeHeight;

        var (toolWidth, toolHeight) = GetToolWindowSize(workArea);
        var toolRect = ResolveToolPopupRect(workArea, homeLeft, homeTop, HomeWindowWidth, homeHeight, toolWidth, toolHeight);
        _toolPopupWindow.Left = toolRect.X;
        _toolPopupWindow.Top = toolRect.Y;
        _toolPopupWindow.Width = toolRect.Width;
        _toolPopupWindow.Height = toolRect.Height;

        _roamBandWindow.Left = roamBandRect.X;
        _roamBandWindow.Top = roamBandRect.Y;
        _roamBandWindow.Width = roamBandRect.Width;
        _roamBandWindow.Height = roamBandRect.Height;

        if (_lastLoggedMode != _state.Mode)
        {
            TraceLog.Write("mode", $"mode={_state.Mode} pinned={_state.IsPinned}");
            _lastLoggedMode = _state.Mode;
        }

        TraceLog.Write("layout", $"home={homeLeft:0},{homeTop:0} {HomeWindowWidth:0}x{homeHeight:0} tool={_toolPopupWindow.Left:0},{_toolPopupWindow.Top:0} {toolWidth:0}x{toolHeight:0} roam={_roamBandWindow.Left:0},{_roamBandWindow.Top:0} {_roamBandWindow.Width:0}x{_roamBandWindow.Height:0}");

        var desktopAssetOpacity = ShellPresentationRules.ResolveDesktopAssetOpacity(_state.Mode, _desktopContext, handles);
        ApplyWindowStyles(desktopAssetOpacity);
        Render(desktopAssetOpacity);
        _ = PublishOverlayRegionsAsync(roamBandRect);
    }

    private RectInt ResolveRoamBandRect()
    {
        var monitor = _desktopContext?.PrimaryMonitorBounds
            ?? new RectInt(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
        return new RectInt(monitor.X, monitor.Bottom - (int)RoamBandHeight, monitor.Width, (int)RoamBandHeight);
    }

    internal static RectInt ResolveToolPopupRect(
        RectInt workArea,
        double homeLeft,
        double homeTop,
        double homeWidth,
        double homeHeight,
        double toolWidth,
        double toolHeight)
    {
        const double margin = 10;
        var alignedRight = homeLeft + homeWidth - toolWidth;
        var aboveTop = homeTop - toolHeight - margin;
        if (aboveTop >= workArea.Y + margin)
        {
            return new RectInt(
                (int)Math.Round(Math.Clamp(alignedRight, workArea.X + margin, workArea.Right - toolWidth - margin)),
                (int)Math.Round(aboveTop),
                (int)Math.Round(toolWidth),
                (int)Math.Round(toolHeight));
        }

        var leftOfHome = homeLeft - toolWidth - margin;
        var sideLeft = leftOfHome >= workArea.X + margin
            ? leftOfHome
            : workArea.X + margin;
        var sideTop = Math.Clamp(homeTop, workArea.Y + margin, workArea.Bottom - toolHeight - margin);
        return new RectInt(
            (int)Math.Round(sideLeft),
            (int)Math.Round(sideTop),
            (int)Math.Round(toolWidth),
            (int)Math.Round(toolHeight));
    }

    private RectInt ResolvePetMotionBounds(RectInt roamBandRect, RectInt homeStageRect)
    {
        if (_state is not null && ShellPresentationRules.IsActionsSurfaceOpen(_state.ActiveTool))
        {
            return new RectInt(
                (int)Math.Round(_homeWindow.Left + homeStageRect.X + 24),
                (int)Math.Round(_homeWindow.Top + homeStageRect.Y + 24),
                Math.Max(1, homeStageRect.Width - 48),
                Math.Max(1, homeStageRect.Height - 54));
        }

        var workArea = _desktopContext?.WorkArea
            ?? new RectInt(0, 0, (int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height);
        return ShellPresentationRules.ResolveRoamMotionBounds(roamBandRect, workArea);
    }

    private void ApplyWindowStyles(double desktopAssetOpacity)
    {
        if (_state is null)
        {
            return;
        }

        var passive = _state.Mode == CompanionMode.Passive;
        OverlayWindowStyler.Apply(_homeWindow, passive, passive, hideFromTaskbar: false);
        OverlayWindowStyler.Apply(_roamBandWindow, true, true);
        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: _state.ActiveTool.IsOpen);

        OverlayWindowStyler.Apply(_toolPopupWindow, passive || !_state.ActiveTool.IsOpen, passive);

        _homeWindow.SetHudVisible(!passive, GetSettingBool("compact_hud"));
        _homeWindow.SetMainPanelVisible(ShellPresentationRules.ShouldShowMainPanel(_state.Mode));
        _homeWindow.SetDesktopAssetOpacity(desktopAssetOpacity);
        _homeWindow.SetDevToolsVisible(_devToolsEnabled && !passive);
        SetWindowVisibility(_homeWindow, Visibility.Visible, "HomePanel");
        SetWindowVisibility(_roamBandWindow, Visibility.Visible, "RoamBand");
        var settingsToolOpen = string.Equals(_state.ActiveTool.ToolId, "settings", StringComparison.OrdinalIgnoreCase);
        var toolWindowAllowed = supervisorStatus.ToolWindowAllowed || settingsToolOpen;
        SetWindowVisibility(_toolPopupWindow, _state.ActiveTool.IsOpen && !passive && toolWindowAllowed ? Visibility.Visible : Visibility.Hidden, "ToolPopup");
    }

    private void Render(double desktopAssetOpacity = ShellPresentationRules.ActiveAssetOpacity)
    {
        if (_state is null || _content is null || _assetService is null)
        {
            return;
        }

        var environment = ResolveEnvironment(_state);
        var needSnapshot = _petSimulationEngine.BuildAverageNeedSnapshot(_state.ActivePets);
        var aggregateStatuses = _petSimulationEngine.BuildAggregateStatuses(_state.ActivePets);
        var actionEnabled = _content.Actions
            .Where(action => action.IsPrimaryAction)
            .ToDictionary(action => action.Id, action => _petSimulationEngine.IsActionEnabled(action.Id, _state.ActivePets), StringComparer.OrdinalIgnoreCase);
        var habitatLoadout = HabitatLoadoutResolver.Resolve(_state, _content);
        var petCommandBarState = EnsureChatInputBarState(_state.ActivePets, _state.TaskCards);
        var now = DateTimeOffset.UtcNow;
        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: _state.ActiveTool.IsOpen);
        var activitySummary = _activitySummaryService.BuildDaily(now);
        var liveStatus = _liveStatusFeed.BuildDaily(now, _state.SettingsSnapshot);
        var liveBannerText = _liveStatusFeed.FormatBanner(liveStatus);
        var liveRecentLines = _activitySummaryService.FormatRecentRows(activitySummary);
        var promotionDecision = PromotionCriteriaSnapshot.TryReadLatestDecision(Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "promotion"));
        var autonomousDecision = _autonomousBetaDecisionService.Decide(now, promotionDecision);
        var killSwitchActive = KillSwitchService.IsActive(_state.SettingsSnapshot);
        var evidenceStatus = _evidenceCollectionStatusService.Read();
        var evidenceSummary = _evidenceSummaryService.GetSummary(ToolPopupWindow.BuildEvidenceSummaryQuery(_state.SettingsSnapshot, now));
        var maturityClock = _maturityScoreboardService.BuildScoreboard(now);

        _homeWindow.Render(_state, environment, _feedbackText, _assetService, needSnapshot, aggregateStatuses, actionEnabled, habitatLoadout, evidenceStatus, _localBrainStatus);
        _roamBandWindow.Render(_state, _assetService, liveStatus, liveBannerText, supervisorStatus, killSwitchActive, evidenceStatus, desktopAssetOpacity);
        _toolPopupWindow.Render(_state, _content, habitatLoadout, _assetService, _devToolsEnabled, petCommandBarState, supervisorStatus, activitySummary, autonomousDecision, promotionDecision, liveRecentLines, evidenceStatus, evidenceSummary, maturityClock);
    }

    private async Task EnableAutonomousBetaAfterConsentAsync()
    {
        if (_state is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var promotionDecision = PromotionCriteriaSnapshot.TryReadLatestDecision(Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "promotion"));
        if (!PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(promotionDecision, _state.SettingsSnapshot))
        {
            SetFeedback("Autonomous beta still needs a passing promotion decision before it can be enabled.");
            return;
        }

        _auditLedgerService.Record(new EvidencePacket(
            Guid.NewGuid(),
            PromotionCriteriaSnapshot.UserConsentPacketKind,
            TaskCardId: null,
            now,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"user_consent_at={now:O}",
            Status: "Completed"));
        _state = _state with { SettingsSnapshot = WithSetting(_state.SettingsSnapshot, AutonomousOperationsConfig.EnabledSetting, bool.TrueString) };
        _feedbackText = "Autonomous beta enabled after explicit consent. Stop Everything can still pause it immediately.";
        await PersistAndRenderAsync();
    }

    private async Task PreviewAutonomousScopeAsync(string scopeId)
    {
        if (_state is null)
        {
            return;
        }

        var preview = await _autonomousScopeService.PreviewAsync(scopeId, _autonomousScopes);
        _toolPopupWindow.SetAutonomousScopePreview(preview);
        _feedbackText = string.IsNullOrWhiteSpace(preview.BlockReason)
            ? $"Previewed {scopeId}: {preview.ActionCount} planned action(s), no mutation."
            : $"Preview blocked for {scopeId}: {preview.BlockReason}";
        await PersistAndRenderAsync();
    }

    private async Task HandleLocalDocsBuildAsync()
    {
        if (_state is null)
        {
            return;
        }

        try
        {
            var result = await _localDocumentRetrievalService.BuildIndexAsync(CancellationToken.None);
            _toolPopupWindow.SetLocalDocsStatus(
                result.Success
                    ? $"Index ready: {result.IndexedFiles} file(s), {result.IndexedLines} line(s), {result.SkippedFiles} skipped."
                    : $"Index refused: {result.Reason}.");
            _feedbackText = result.Success ? "Local Docs index built safely." : $"Local Docs refused: {result.Reason}.";
        }
        catch (Exception ex)
        {
            _toolPopupWindow.SetLocalDocsStatus($"Index failed: {ex.Message}");
            _feedbackText = "Local Docs index failed. Check the Local Docs tab for details.";
            TraceLog.Write("local-docs", $"build-failed {ex}");
        }

        Render();
    }

    private async Task HandleLocalDocsQueryAsync(string query)
    {
        if (_state is null)
        {
            return;
        }

        try
        {
            var results = await _localDocumentRetrievalService.QueryAsync(query, 12, CancellationToken.None);
            _toolPopupWindow.SetLocalDocsResults(results);
            _toolPopupWindow.SetLocalDocsStatus($"Query complete: {results.Count} result(s).");
            _feedbackText = $"Local Docs returned {results.Count} result(s).";
        }
        catch (Exception ex)
        {
            _toolPopupWindow.SetLocalDocsResults([]);
            _toolPopupWindow.SetLocalDocsStatus($"Query failed: {ex.Message}");
            _feedbackText = "Local Docs query failed. Check the query text.";
            TraceLog.Write("local-docs", $"query-failed {ex}");
        }

        Render();
    }

    private async Task ToggleDoNotDisturbAsync()
    {
        if (_state is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var current = _doNotDisturbScheduleService.Evaluate(_state.SettingsSnapshot, now);
        var next = _doNotDisturbScheduleService.ApplyQuickToggle(
            _state.SettingsSnapshot,
            current.IsActive ? DoNotDisturbQuickToggle.Off : DoNotDisturbQuickToggle.OneHour,
            now);
        _state = _state with { SettingsSnapshot = next.SettingsSnapshot };
        _feedbackText = next.IsActive
            ? "Do Not Disturb is on. Helpers pause while pets stay visible."
            : "Do Not Disturb is off. Helpers may resume when other gates allow.";
        await PersistAndRenderAsync();
    }

    private async Task ExportEvidenceSummaryAsync()
    {
        if (_state is null)
        {
            return;
        }

        var artifactRoot = Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "c-phase-141-evidence-dashboard");
        var result = _evidenceSummaryService.ExportSummary(
            ToolPopupWindow.BuildEvidenceSummaryQuery(_state.SettingsSnapshot, DateTimeOffset.UtcNow),
            artifactRoot,
            DateTimeOffset.UtcNow);
        _feedbackText = result.Exported
            ? $"Evidence summary exported: {result.Path}"
            : $"Evidence export blocked: {result.BlockReason}";
        await PersistAndRenderAsync();
    }

    private RuntimeSupervisorStatus EvaluateRuntimeSupervisor(bool isUserInitiatedToolOpen)
    {
        var now = DateTimeOffset.UtcNow;
        var gameMode = _gameModeDetectorService.Evaluate(_state?.SettingsSnapshot);
        var coexistence = _coexistenceTriggerService.Evaluate(
            _state?.SettingsSnapshot,
            _desktopContext,
            new CoexistenceResourceSnapshot(GameModeActive: gameMode.IsGameModeActive),
            now);
        var dnd = _doNotDisturbScheduleService.Evaluate(_state?.SettingsSnapshot, now);
        return _runtimeSupervisorService.Evaluate(
            _state?.SettingsSnapshot,
            _desktopContext,
            isUserInitiatedToolOpen,
            fullscreenOtherOverride: _fullscreenMonitor.IsFullscreenOther,
            coexistenceTriggers: coexistence,
            doNotDisturbState: dnd);
    }

    private ChatInputBarState EnsureChatInputBarState(IReadOnlyList<PetActor> pets, IReadOnlyList<TaskCard>? taskCards)
    {
        var helpers = BuildHelperProfiles(pets);
        var snapshots = _petWellbeingInterpreter.BuildSnapshots(pets);
        if (_petCommandBarState is null || !SameHelperRoster(_petCommandBarState.ActiveHelpers, helpers))
        {
            _petCommandBarState = BuildChatInputBarState(helpers, taskCards, snapshots);
        }

        return _petCommandBarState with { WellbeingSnapshots = snapshots };
    }

    private ChatInputBarState BuildChatInputBarState(IReadOnlyList<AgentSlotProfile> helpers, IReadOnlyList<TaskCard>? taskCards, IReadOnlyList<PetWellbeingSnapshot> snapshots)
    {
        var cards = (taskCards ?? [])
            .OrderByDescending(card => card.UpdatedAtUtc == default ? card.CreatedAtUtc : card.UpdatedAtUtc)
            .ThenByDescending(card => card.CreatedAtUtc)
            .ToList();
        if (cards.Count == 0)
        {
            return _petCommandBarService.BuildInitialState(helpers, DateTimeOffset.UtcNow) with { WellbeingSnapshots = snapshots };
        }

        var latest = cards[0];
        return new ChatInputBarState(
            helpers,
            latest.Intent.RawText,
            latest.Intent,
            latest,
            LastPolicyDecision: null,
            StatusMessage: $"{cards.Count} draft task card(s) saved locally. Tools are still disabled.",
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            QueuedTaskCards: cards,
            WellbeingSnapshots: snapshots);
    }

    private static bool SameHelperRoster(IReadOnlyList<AgentSlotProfile> current, IReadOnlyList<AgentSlotProfile> next)
    {
        if (current.Count != next.Count)
        {
            return false;
        }

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i].PetId != next[i].PetId || !string.Equals(current[i].PetNameSnapshot, next[i].PetNameSnapshot, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private IReadOnlyList<AgentSlotProfile> BuildHelperProfiles(IReadOnlyList<PetActor> pets)
    {
        var previousSlots = _petCommandBarState?.ActiveHelpers
            .Select(helper => new AgentSlot(
                helper.PetId,
                helper.SlotIndex,
                helper.PetNameSnapshot,
                helper.AgentStatus,
                helper.CurrentTaskCardId,
                DateTimeOffset.UtcNow,
                helper.PetId,
                helper.PreferenceSnapshot is not null && helper.PreferenceSnapshot.TryGetValue("species", out var species) ? species : "pet",
                helper.PreferenceSnapshot is not null && helper.PreferenceSnapshot.TryGetValue("tool_icon", out var icon) ? icon : "",
                helper.PreferenceSnapshot is not null && helper.PreferenceSnapshot.TryGetValue("active_tool_family", out var family) ? family : ""))
            .ToList();
        var roster = _agentSlotService.BuildRoster(pets, previousSlots, DateTimeOffset.UtcNow);
        foreach (var packet in roster.RenamePackets)
        {
            _auditLedgerService.Record(packet);
        }

        return roster.Slots.Select(AgentSlotService.ToProfile).ToList();
    }

    private static IReadOnlyList<ToolPolicy> BuildPetCommandPolicies()
    {
        return
        [
            new ToolPolicy("local-docs-readonly", "localDocs", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("local-research-readonly", "localResearch", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("sprite-audit-readonly", "spriteAudit", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("pet-state-readonly", "petState", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("asset-inventory-readonly", "assetInventory", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("code-review-readonly", "codeReview", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("code-patch-plan-readonly", "codePatchPlan", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("checklist-readonly", "checklist", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("translate-text-readonly", "translateText", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("audio-assist-readonly", "audioAssist", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("screen-capture-approval", "screenCapture", ToolAccessMode.ReadOnly, ToolRiskLevel.Medium, ApprovalRequirement.BeforeExecution),
            new ToolPolicy("proof-capture-readonly", "proofCapture", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("basket-readonly", "basket", ToolAccessMode.ReadOnly, ToolRiskLevel.Low, ApprovalRequirement.None),
            new ToolPolicy("build-proof-approval", "buildProof", ToolAccessMode.Write, ToolRiskLevel.Medium, ApprovalRequirement.BeforeExecution)
        ];
    }

    private async Task PublishOverlayRegionsAsync(RectInt roamBandRect)
    {
        if (_brokerClient is null || _state is null)
        {
            return;
        }

        var regions = new List<OverlayRegion>
        {
            new(WindowRole.HomePanel, new RectInt((int)_homeWindow.Left, (int)_homeWindow.Top, (int)_homeWindow.Width, (int)_homeWindow.Height), _state.Mode != CompanionMode.Passive),
            new(WindowRole.RoamBand, roamBandRect, false),
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

    private async Task ToggleWebToolsAsync()
    {
        if (_state is null)
        {
            return;
        }

        var nextVisible = !GetSettingBool("webtools_visible");
        var nextState = _state with
        {
            SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "webtools_visible", nextVisible.ToString())
        };

        if (!nextVisible && nextState.ActiveTool.IsOpen)
        {
            nextState = nextState with
            {
                ActiveTool = nextState.ActiveTool with { IsOpen = false }
            };
        }

        _state = nextState;
        TraceLog.Write("ui-command", $"toggle-webtools visible={nextVisible}");
        ApplyModeAndLayout();
        await PersistAsync();
    }

    private async Task ToggleBasketAsync()
    {
        await ToggleToolAsync("basket");
    }

    private async Task ToggleActionsAsync()
    {
        await ToggleToolAsync("actions");
    }

    private async Task ToggleHelpersAsync()
    {
        await ToggleToolAsync("helpers");
    }

    private async Task ToggleBenchmarksAsync()
    {
        await ToggleToolAsync("benchmarks");
    }

    private Task OpenSpriteWorkflowV2Async()
    {
        if (_spriteWorkflowV2Window is null)
        {
            _spriteWorkflowV2Window = new SpriteWorkflowV2Window
            {
                Owner = _homeWindow
            };
            _spriteWorkflowV2Window.Closed += (_, _) => _spriteWorkflowV2Window = null;
        }

        _spriteWorkflowV2Window.LoadProject(ResolveRepoRootOrBaseDirectory());
        ShowWindowWithFocusDiscipline(_spriteWorkflowV2Window, "SpriteWorkflowV2", userInitiated: true);
        TraceLog.Write("sprite-workflow-v2", "opened read-only workbench");
        return Task.CompletedTask;
    }

    private Task OpenCreativeLearningLabAsync()
    {
        if (_creativeLearningLabWindow is null)
        {
            _creativeLearningLabWindow = new CreativeLearningLabWindow
            {
                Owner = _homeWindow
            };
            _creativeLearningLabWindow.Closed += (_, _) => _creativeLearningLabWindow = null;
        }

        _creativeLearningLabWindow.LoadProject(ResolveRepoRootOrBaseDirectory());
        ShowWindowWithFocusDiscipline(_creativeLearningLabWindow, "CreativeLearningLab", userInitiated: true);
        TraceLog.Write("creative-learning-lab", "opened read-only artifact index");
        return Task.CompletedTask;
    }

    private Task OpenLocalBrainStatusPanelAsync()
    {
        if (_localBrainStatusPanelWindow is null)
        {
            _localBrainStatusPanelWindow = new LocalBrainStatusPanelWindow(
                _localBrainStatusPanelService,
                () => _state?.SettingsSnapshot ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                () => EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: true))
            {
                Owner = _homeWindow
            };
            _localBrainStatusPanelWindow.Closed += (_, _) => _localBrainStatusPanelWindow = null;
        }
        else
        {
            _localBrainStatusPanelWindow.RefreshFromCurrentState();
        }

        ShowWindowWithFocusDiscipline(_localBrainStatusPanelWindow, "LocalBrainStatusPanel", userInitiated: true);
        TraceLog.Write("local-ai", "local-brain-status-panel-opened");
        return Task.CompletedTask;
    }

    private async Task ToggleSettingsAsync()
    {
        await ToggleToolAsync("settings");
    }

    private async Task OpenToolTabAsync(string toolId)
    {
        if (_state is null)
        {
            return;
        }

        if (string.Equals(toolId, "creative-lab", StringComparison.OrdinalIgnoreCase))
        {
            await OpenCreativeLearningLabAsync();
            return;
        }

        _state = _state with
        {
            ActiveTool = new ToolSession(toolId, true),
            SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "webtools_visible", bool.TrueString)
        };
        TraceLog.Write("ui-command", $"open-tool-tab tool={toolId}");
        ApplyModeAndLayout();
        await PersistAndRenderAsync();
    }

    private async Task RunFirstLaunchWizardAsync(bool force)
    {
        if (_state is null)
        {
            return;
        }

        if (!force && string.Equals(Environment.GetEnvironmentVariable("WEVITO_SKIP_FIRST_LAUNCH_WIZARD"), "1", StringComparison.Ordinal))
        {
            TraceLog.Write("first-launch", "wizard-skipped-for-automation");
            _state = _state with
            {
                SettingsSnapshot = WithSetting(_state.SettingsSnapshot, FirstLaunchWizardStateService.CompletedSetting, bool.TrueString)
            };
            await PersistAndRenderAsync();
            return;
        }

        if (!force && !_firstLaunchWizardStateService.ShouldRun(_state.SettingsSnapshot))
        {
            return;
        }

        var wizard = new FirstLaunchWizardWindow(_firstLaunchWizardStateService)
        {
            Owner = _homeWindow
        };
        var focusDecision = _focusDisciplineService.Decide(
            new WindowShowRequest("FirstLaunchWizard", UserInitiated: force, IsFirstLaunchWizard: true),
            DateTimeOffset.UtcNow);
        wizard.ShowActivated = focusDecision.ShowActivated;
        wizard.LoadSettings(_state.SettingsSnapshot, _state.ActivePets);
        var completed = wizard.ShowDialog() == true;
        if (!completed && !force)
        {
            return;
        }

        _state = _state with
        {
            SettingsSnapshot = wizard.ResultSettings,
            ActiveTool = new ToolSession("helpers", true)
        };
        if (TryParseFirstLaunchChoice(wizard.ResultSettings, out var choice))
        {
            ApplyFirstRunChoiceResult(choice, DateTimeOffset.UtcNow, openHelpers: choice == FirstLaunchBackgroundChoice.HelpWithSpriteCleanup);
        }

        SetFeedback($"{_aiIdentityService.GetAiName(_state.SettingsSnapshot)} is ready. Chat is the primary surface; pets stay visually normal.");
        await PersistAndRenderAsync();
    }

    private async Task HandleFirstRunChoiceAsync(FirstLaunchBackgroundChoice choice)
    {
        if (_state is null)
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var result = ApplyFirstRunChoiceResult(choice, timestamp, openHelpers: choice == FirstLaunchBackgroundChoice.HelpWithSpriteCleanup);
        SetFeedback(result.Message);
        await PersistAndRenderAsync();
    }

    private FirstLaunchChoiceResult ApplyFirstRunChoiceResult(FirstLaunchBackgroundChoice choice, DateTimeOffset timestamp, bool openHelpers)
    {
        if (_state is null)
        {
            return new FirstLaunchChoiceResult(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), [], null, false, "State is not ready.");
        }

        var result = _firstLaunchWizardStateService.ApplyInlineChoice(
            _state.SettingsSnapshot,
            _state.TaskCards,
            _petTaskCardQueueService,
            choice,
            timestamp);
        _state = _state with
        {
            SettingsSnapshot = result.Settings,
            TaskCards = result.TaskCards,
            ActiveTool = openHelpers
                ? new ToolSession("helpers", true)
                : _state.ActiveTool
        };
        var helpers = BuildHelperProfiles(_state.ActivePets);
        _petCommandBarState = new ChatInputBarState(
            helpers,
            result.DraftedCard?.Intent.RawText ?? "",
            result.DraftedCard?.Intent,
            result.DraftedCard,
            LastPolicyDecision: null,
            StatusMessage: result.Message,
            UpdatedAtUtc: timestamp,
            QueuedTaskCards: result.TaskCards,
            WellbeingSnapshots: _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets));
        return result;
    }

    private static bool TryParseFirstLaunchChoice(IReadOnlyDictionary<string, string> settings, out FirstLaunchBackgroundChoice choice)
    {
        choice = FirstLaunchBackgroundChoice.JustChat;
        return settings.TryGetValue(FirstLaunchWizardStateService.BackgroundChoiceSetting, out var raw) &&
               Enum.TryParse(raw, ignoreCase: true, out choice);
    }

    private async Task ActivateKillSwitchAsync()
    {
        if (_state is null)
        {
            return;
        }

        if (KillSwitchService.IsActive(_state.SettingsSnapshot))
        {
            _feedbackText = "Stop Everything is already active. Re-enable helpers from Settings after confirming.";
            Render();
            return;
        }

        _state = _state with { SettingsSnapshot = WithSetting(_state.SettingsSnapshot, KillSwitchService.KillSwitchSetting, bool.TrueString) };
        _killSwitchService.Record("kill_switch", null, DateTimeOffset.UtcNow, "Stop Everything activated from the home overlay.", "Blocked");
        _feedbackText = "Stop Everything is active. Helper work is blocked until you turn it off in Settings.";
        TraceLog.Write("settings", $"{KillSwitchService.KillSwitchSetting}=True source=home-overlay");
        ApplyModeAndLayout();
        await PersistAndRenderAsync();
    }

    private async Task HandlePowerRuntimeEventAsync(WindowsPowerRuntimeEvent runtimeEvent)
    {
        if (_state is null)
        {
            return;
        }

        var result = _powerHandler.ApplyRuntimeEvent(_state.SettingsSnapshot, runtimeEvent, DateTimeOffset.UtcNow);
        _state = _state with { SettingsSnapshot = result.SettingsSnapshot };
        _feedbackText = result.ForcedQuiet
            ? "Quiet mode is active because Windows is sleeping, locking, or switching sessions."
            : "Windows resumed. Wevito did not auto-return to Active; review Settings when ready.";
        TraceLog.Write("power", $"event={runtimeEvent} forcedQuiet={result.ForcedQuiet}");
        ApplyModeAndLayout();
        await PersistAndRenderAsync();
    }

    private async Task ToggleDevAsync()
    {
        if (!_devToolsEnabled)
        {
            return;
        }

        await ToggleToolAsync("dev");
    }

    private async Task ToggleCompactHudAsync()
    {
        if (_state is null)
        {
            return;
        }

        var nextSettings = new Dictionary<string, string>(_state.SettingsSnapshot, StringComparer.OrdinalIgnoreCase)
        {
            ["compact_hud"] = (!GetSettingBool("compact_hud")).ToString()
        };
        _state = _state with { SettingsSnapshot = nextSettings };
        TraceLog.Write("ui-command", $"toggle-compact next={nextSettings["compact_hud"]}");
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

    private async Task HandlePetCommandSubmittedAsync(string inputText)
    {
        if (_state is null)
        {
            return;
        }

        var helpers = BuildHelperProfiles(_state.ActivePets);
        _petCommandBarState = _petCommandBarService.SubmitDraft(
            inputText,
            helpers,
            BuildPetCommandPolicies(),
            selectedPetId: null,
            DateTimeOffset.UtcNow);
        if (_petCommandBarState.LastTaskCard is { } taskCard)
        {
            var taskCards = _petTaskCardQueueService.AppendDraft(_state.TaskCards, taskCard);
            _state = _state with
            {
                TaskCards = taskCards
            };
            _petCommandBarState = _petCommandBarState with
            {
                QueuedTaskCards = taskCards,
                WellbeingSnapshots = _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets)
            };
        }

        SetFeedback(_petCommandBarState.StatusMessage);
        TraceLog.Write(
            "pet-command",
            $"status={_petCommandBarState.LastTaskCard?.Status} kind={_petCommandBarState.LastIntent?.TaskKind} policy={_petCommandBarState.LastPolicyDecision?.Status}");
        await PersistAndRenderAsync();
    }

    private async Task HandlePetTaskStatusChangeAsync(Guid cardId, TaskCardStatus nextStatus)
    {
        if (_state is null)
        {
            return;
        }

        if (!_petTaskCardQueueService.TryTransitionStatus(
                _state.TaskCards,
                cardId,
                nextStatus,
                DateTimeOffset.UtcNow,
                out var taskCards,
                out var updatedCard,
                out var reason))
        {
            SetFeedback(reason);
            TraceLog.Write("pet-command", $"status-transition-blocked id={cardId} next={nextStatus} reason={reason}");
            Render();
            return;
        }

        _state = _state with { TaskCards = taskCards };
        var helpers = BuildHelperProfiles(_state.ActivePets);
        var snapshots = _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets);
        _petCommandBarState = new ChatInputBarState(
            helpers,
            updatedCard?.Intent.RawText ?? "",
            updatedCard?.Intent,
            updatedCard,
            LastPolicyDecision: null,
            StatusMessage: nextStatus == TaskCardStatus.Approved
                ? "Task card approved locally. Preview the approval-gated report next."
                : "Task card cancelled locally. No execution was started.",
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            QueuedTaskCards: taskCards,
            WellbeingSnapshots: snapshots);
        SetFeedback(_petCommandBarState.StatusMessage);
        TraceLog.Write("pet-command", $"status-transition id={cardId} next={nextStatus}");
        await PersistAndRenderAsync();
    }

    private async Task HandlePetTaskPreviewAsync(Guid cardId)
    {
        if (_state is null)
        {
            return;
        }

        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: true);
        if (!_runtimeSupervisorService.CanStartUserInitiatedWork(supervisorStatus, out var supervisorReason))
        {
            SetFeedback(supervisorReason);
            TraceLog.Write("runtime-supervisor", $"preview-blocked id={cardId} reason={supervisorReason}");
            Render();
            return;
        }

        var card = (_state.TaskCards ?? []).FirstOrDefault(candidate => candidate.Id == cardId);
        if (card is null)
        {
            SetFeedback("Task card was not found.");
            Render();
            return;
        }

        if (card.PolicySnapshot is null)
        {
            SetFeedback("Task card has no policy snapshot to preview safely.");
            Render();
            return;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var request = new TaskAdapterRequest(
            card.Id,
            card.Intent,
            BuildPreviewPolicySnapshot(card.PolicySnapshot),
            TaskAdapterRunMode.DryRunPreview,
            ResolvePetTaskArtifactRoot(card, timestamp),
            timestamp);
        var result = _petTaskAdapterPreviewDispatcher.BuildPreview(request, timestamp, _content, _state.ActivePets, _state.Mode);

        if (!_petTaskCardQueueService.TryApplyAdapterResult(
                _state.TaskCards,
                result,
                timestamp,
                out var taskCards,
                out var updatedCard,
                out var reason))
        {
            SetFeedback(reason);
            TraceLog.Write("pet-command", $"preview-blocked id={cardId} reason={reason}");
            Render();
            return;
        }

        _state = _state with { TaskCards = taskCards };
        var helpers = BuildHelperProfiles(_state.ActivePets);
        var snapshots = _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets);
        _petCommandBarState = new ChatInputBarState(
            helpers,
            updatedCard?.Intent.RawText ?? "",
            updatedCard?.Intent,
            updatedCard,
            LastPolicyDecision: null,
            StatusMessage: BuildPreviewStatusMessage(result),
            UpdatedAtUtc: timestamp,
            QueuedTaskCards: taskCards,
            WellbeingSnapshots: snapshots);
        SetFeedback(_petCommandBarState.StatusMessage);
        TraceLog.Write("pet-command", $"preview id={cardId} family={result.ToolFamily} status={result.Status} mutate={result.DidMutate} audit={result.AuditLogPath}");
        await PersistAndRenderAsync();
    }

    private async Task HandlePetTaskExecutionAsync(Guid cardId)
    {
        if (_state is null)
        {
            return;
        }

        var supervisorStatus = EvaluateRuntimeSupervisor(isUserInitiatedToolOpen: true);
        if (!_runtimeSupervisorService.CanStartUserInitiatedWork(supervisorStatus, out var supervisorReason))
        {
            SetFeedback(supervisorReason);
            TraceLog.Write("runtime-supervisor", $"execute-blocked id={cardId} reason={supervisorReason}");
            Render();
            return;
        }

        var card = (_state.TaskCards ?? []).FirstOrDefault(candidate => candidate.Id == cardId);
        if (card is null)
        {
            SetFeedback("Task card was not found.");
            Render();
            return;
        }

        if (card.Status != TaskCardStatus.Reviewing)
        {
            SetFeedback("Preview the task first, then run only after reviewing the report.");
            Render();
            return;
        }

        if (!CanExecuteReviewedPetTask(card))
        {
            SetFeedback("Only reviewed translateText, screenCapture, audioAssist, and buildProof tasks can run right now.");
            Render();
            return;
        }

        var timestamp = DateTimeOffset.UtcNow;
        var request = new TaskAdapterRequest(
            card.Id,
            card.Intent,
            BuildExecutionPolicySnapshot(card),
            TaskAdapterRunMode.Execute,
            ResolvePetTaskArtifactRoot(card, timestamp),
            timestamp);
        var result = await ExecutePetTaskAdapterAsync(card, request, timestamp);

        if (!_petTaskCardQueueService.TryApplyAdapterResult(
                _state.TaskCards,
                result,
                timestamp,
                out var taskCards,
                out var updatedCard,
                out var reason))
        {
            SetFeedback(reason);
            TraceLog.Write("pet-command", $"execution-blocked id={cardId} reason={reason}");
            Render();
            return;
        }

        _state = _state with { TaskCards = taskCards };
        var helpers = BuildHelperProfiles(_state.ActivePets);
        var snapshots = _petWellbeingInterpreter.BuildSnapshots(_state.ActivePets);
        _petCommandBarState = new ChatInputBarState(
            helpers,
            updatedCard?.Intent.RawText ?? "",
            updatedCard?.Intent,
            updatedCard,
            LastPolicyDecision: null,
            StatusMessage: BuildExecutionStatusMessage(result),
            UpdatedAtUtc: timestamp,
            QueuedTaskCards: taskCards,
            WellbeingSnapshots: snapshots);
        SetFeedback(_petCommandBarState.StatusMessage);
        TraceLog.Write("pet-command", $"execute id={cardId} family={result.ToolFamily} status={result.Status} mutate={result.DidMutate} audit={result.AuditLogPath}");
        await PersistAndRenderAsync();
    }

    private async Task HandleSupervisedApplyApprovalAsync(Guid cardId, UserApplyApproval approval)
    {
        if (_state is null)
        {
            return;
        }

        var card = (_state.TaskCards ?? []).FirstOrDefault(candidate => candidate.Id == cardId);
        if (card is null)
        {
            SetFeedback("Supervised apply card was not found.");
            Render();
            return;
        }

        var payload = card.ReviewPayload ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var expectedScopeId = payload.TryGetValue("scope_id", out var scopeId) ? scopeId : "";
        var expectedOperationId = payload.TryGetValue("operation_id", out var operationId) ? operationId : "";
        var result = _supervisedImprovementLoop.HandleApplyApproval(
            approval,
            expectedScopeId,
            expectedOperationId,
            card.Id,
            DateTimeOffset.UtcNow,
            _state.TaskCards ?? []);
        SetFeedback(result.RefusalReason.Equals(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason, StringComparison.Ordinal)
            ? "Approval accepted, but v0 safely refused because the apply runner is not implemented yet."
            : $"Supervised apply refused: {result.RefusalReason}");
        _state = _state with { TaskCards = result.TaskCards };
        await PersistAndRenderAsync();
    }

    private static ToolPolicy BuildPreviewPolicySnapshot(ToolPolicy policy)
    {
        var roots = ResolvePreviewApprovedRoots(policy.ToolFamily);
        return policy with
        {
            AccessMode = ToolAccessMode.ReadOnly,
            RiskLevel = ToolRiskLevel.Low,
            ApprovedRootPaths = roots
        };
    }

    private static ToolPolicy BuildTranslationExecutionPolicySnapshot()
    {
        return new ToolPolicy(
            "translate-text-network-approval",
            "translateText",
            ToolAccessMode.Network,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);
    }

    private static ToolPolicy BuildAudioAssistExecutionPolicySnapshot()
    {
        return new ToolPolicy(
            "audio-assist-write-approval",
            "audioAssist",
            ToolAccessMode.Write,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);
    }

    private static ToolPolicy BuildScreenCaptureExecutionPolicySnapshot()
    {
        return new ToolPolicy(
            "screen-capture-wevito-window-approval",
            "screenCapture",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);
    }

    private static ToolPolicy BuildProofExecutionPolicySnapshot()
    {
        return new ToolPolicy(
            "build-proof-execution-approval",
            "buildProof",
            ToolAccessMode.Write,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);
    }

    private static ToolPolicy BuildExecutionPolicySnapshot(TaskCard card)
    {
        if (string.Equals(card.ToolFamily, "translateText", StringComparison.OrdinalIgnoreCase))
        {
            return BuildTranslationExecutionPolicySnapshot();
        }

        if (string.Equals(card.ToolFamily, "screenCapture", StringComparison.OrdinalIgnoreCase))
        {
            return BuildScreenCaptureExecutionPolicySnapshot();
        }

        if (string.Equals(card.ToolFamily, "buildProof", StringComparison.OrdinalIgnoreCase))
        {
            return BuildProofExecutionPolicySnapshot();
        }

        return BuildAudioAssistExecutionPolicySnapshot();
    }

    private static bool CanExecuteReviewedPetTask(TaskCard card)
    {
        if (card.Status != TaskCardStatus.Reviewing)
        {
            return false;
        }

        if (card.Intent.TaskKind == TaskKind.TranslateText &&
            string.Equals(card.ToolFamily, "translateText", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (card.Intent.TaskKind == TaskKind.ScreenCapture &&
            string.Equals(card.ToolFamily, "screenCapture", StringComparison.OrdinalIgnoreCase) &&
            IsExecutableScreenCaptureRequest(card.Intent.RawText))
        {
            return true;
        }

        if (card.Intent.TaskKind == TaskKind.BuildProof &&
            string.Equals(card.ToolFamily, "buildProof", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return card.Intent.TaskKind == TaskKind.AudioAssist &&
               string.Equals(card.ToolFamily, "audioAssist", StringComparison.OrdinalIgnoreCase) &&
               IsExecutableAudioAssistRequest(card.Intent.RawText);
    }

    private async Task<TaskAdapterResult> ExecutePetTaskAdapterAsync(TaskCard card, TaskAdapterRequest request, DateTimeOffset timestamp)
    {
        if (string.Equals(card.ToolFamily, "translateText", StringComparison.OrdinalIgnoreCase))
        {
            return await _translationExecutionAdapter.ExecuteAsync(request, timestamp);
        }

        if (string.Equals(card.ToolFamily, "screenCapture", StringComparison.OrdinalIgnoreCase))
        {
            var region = ResolveScreenCaptureRegion(card.Intent.RawText);
            if (region.Status == ScreenCaptureRegionStatus.Cancelled)
            {
                return BuildBlockedScreenCaptureResult(request, "Selected-region capture was cancelled.", timestamp);
            }

            if (region.Status == ScreenCaptureRegionStatus.MissingLastRegion)
            {
                return BuildBlockedScreenCaptureResult(request, "No last screenshot region has been saved yet.", timestamp);
            }

            if (ScreenCaptureTargetResolver.IsRecordingRequest(card.Intent.RawText))
            {
                return await ExecuteScreenRecordingAsync(request, timestamp, region.Region);
            }

            return await _screenCaptureExecutionAdapter.ExecuteAsync(request, timestamp, region.Region);
        }

        if (string.Equals(card.ToolFamily, "buildProof", StringComparison.OrdinalIgnoreCase))
        {
            return await _buildProofExecutionAdapter.ExecuteAsync(request, timestamp);
        }

        return _audioAssistExecutionAdapter.Execute(request, timestamp);
    }

    private static bool IsExecutableAudioAssistRequest(string rawText)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            rawText,
            @"\b(unmute|mute|(?:set|change|turn|put|make)\b.*?\bvolume\b.*?\d{1,3}|volume\b.*?\b(?:to|at)\b\s*\d{1,3})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool IsWevitoWindowCaptureRequest(string rawText)
    {
        return rawText.Contains("wevito", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("this window", StringComparison.OrdinalIgnoreCase);
    }

    private ScreenCaptureRegionResolution ResolveScreenCaptureRegion(string rawText)
    {
        var target = ScreenCaptureTargetResolver.ResolveTarget(rawText);
        if (target.TargetKind == CaptureTargetKind.SelectedRegion)
        {
            var selected = RegionPickerWindow.Pick(_homeWindow);
            if (selected is null)
            {
                return new ScreenCaptureRegionResolution(ScreenCaptureRegionStatus.Cancelled, null);
            }

            _regionSelectionStore.Save(selected);
            return new ScreenCaptureRegionResolution(ScreenCaptureRegionStatus.Ready, selected);
        }

        if (target.TargetKind == CaptureTargetKind.LastRegion)
        {
            return _regionSelectionStore.TryLoad(out var lastRegion)
                ? new ScreenCaptureRegionResolution(ScreenCaptureRegionStatus.Ready, lastRegion)
                : new ScreenCaptureRegionResolution(ScreenCaptureRegionStatus.MissingLastRegion, null);
        }

        return new ScreenCaptureRegionResolution(ScreenCaptureRegionStatus.Ready, null);
    }

    private async Task<TaskAdapterResult> ExecuteScreenRecordingAsync(TaskAdapterRequest request, DateTimeOffset timestamp, CaptureRegion? region)
    {
        var indicator = new RecordingIndicatorWindow(_homeWindow);
        var progress = new Progress<TimeSpan>(indicator.UpdateRemaining);
        try
        {
            indicator.ShowNear(_homeWindow, TimeSpan.FromSeconds(5));
            return await _screenCaptureExecutionAdapter.ExecuteAsync(request, timestamp, region, progress);
        }
        finally
        {
            indicator.Close();
        }
    }

    private static TaskAdapterResult BuildBlockedScreenCaptureResult(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            "screenCapture",
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }

    private static bool IsExecutableScreenCaptureRequest(string rawText)
    {
        if (ScreenCaptureTargetResolver.IsRecordingRequest(rawText))
        {
            return IsWevitoWindowCaptureRequest(rawText) && !IsRegionCaptureRequest(rawText);
        }

        return IsWevitoWindowCaptureRequest(rawText) ||
               IsRegionCaptureRequest(rawText);
    }

    private static bool IsRegionCaptureRequest(string rawText)
    {
        var target = ScreenCaptureTargetResolver.ResolveTarget(rawText);
        return target.TargetKind is CaptureTargetKind.SelectedRegion or CaptureTargetKind.LastRegion;
    }

    private static IReadOnlyList<string> ResolvePreviewApprovedRoots(string toolFamily)
    {
        if (string.Equals(toolFamily, "spriteAudit", StringComparison.OrdinalIgnoreCase))
        {
            return [BrokerProcessManager.ResolveSpriteRuntimeRoot()];
        }

        var repoRoot = ResolveRepoRootOrBaseDirectory();
        if (string.Equals(toolFamily, "assetInventory", StringComparison.OrdinalIgnoreCase))
        {
            var roots = new[]
                {
                    Path.Combine(repoRoot, "sprites_runtime"),
                    Path.Combine(repoRoot, "sprites_shared_runtime")
                }
                .Where(Directory.Exists)
                .ToList();
            return roots.Count > 0 ? roots : [repoRoot];
        }

        if (string.Equals(toolFamily, "localDocs", StringComparison.OrdinalIgnoreCase))
        {
            var docsRoot = Path.Combine(repoRoot, "docs");
            return Directory.Exists(docsRoot)
                ? [docsRoot]
                : [repoRoot];
        }

        if (string.Equals(toolFamily, "codeReview", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(toolFamily, "codePatchPlan", StringComparison.OrdinalIgnoreCase))
        {
            var roots = new[]
                {
                    Path.Combine(repoRoot, "vnext", "src"),
                    Path.Combine(repoRoot, "vnext", "tests"),
                    Path.Combine(repoRoot, "tools"),
                    Path.Combine(repoRoot, "scripts")
                }
                .Where(Directory.Exists)
                .ToList();
            return roots.Count > 0 ? roots : [repoRoot];
        }

        return [repoRoot];
    }

    private static string ResolvePetTaskArtifactRoot(TaskCard card, DateTimeOffset timestamp)
    {
        var repoRoot = ResolveRepoRootOrBaseDirectory();
        var taskSlug = string.Equals(card.ToolFamily, "screenCapture", StringComparison.OrdinalIgnoreCase) &&
            ScreenCaptureTargetResolver.IsRecordingRequest(card.Intent.RawText)
                ? "clip"
                : SanitizeArtifactSlug(card.Intent.TaskKind.ToString());
        var slug = $"{timestamp:yyyyMMdd-HHmmss}-{SanitizeArtifactSlug(card.ToolFamily)}-{taskSlug}";
        return Path.Combine(repoRoot, "vnext", "artifacts", "pet-tasks", slug);
    }

    private static string ResolveRepoRootOrBaseDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "vnext")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private static string SanitizeArtifactSlug(string value)
    {
        var chars = (value ?? string.Empty)
            .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "task" : slug;
    }

    private static string BuildPreviewStatusMessage(TaskAdapterResult result)
    {
        return result.Status switch
        {
            TaskAdapterResultStatus.PreviewReady => string.IsNullOrWhiteSpace(result.AuditLogPath)
                ? result.PreviewSummary
                : $"Preview report ready: {result.AuditLogPath}",
            TaskAdapterResultStatus.Blocked => $"Preview blocked: {result.BlockReason}",
            TaskAdapterResultStatus.Failed => $"Preview failed: {result.BlockReason}",
            _ => result.ResultSummary
        };
    }

    private static string BuildExecutionStatusMessage(TaskAdapterResult result)
    {
        return result.Status switch
        {
            TaskAdapterResultStatus.Completed => string.IsNullOrWhiteSpace(result.AuditLogPath)
                ? result.ResultSummary
                : $"Execution report ready: {result.AuditLogPath}",
            TaskAdapterResultStatus.Blocked => $"Execution blocked: {result.BlockReason}",
            TaskAdapterResultStatus.Failed => $"Execution failed: {result.BlockReason}",
            _ => result.ResultSummary
        };
    }

    private void HandleAction(string actionId)
    {
        if (_state is null || _content is null)
        {
            return;
        }

        var actionDefinition = _content.Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.OrdinalIgnoreCase));
        if (actionDefinition is null)
        {
            return;
        }

        if (!_petSimulationEngine.IsActionEnabled(actionId, _state.ActivePets))
        {
            SetFeedback($"{actionDefinition.DisplayName} is not needed right now.");
            return;
        }

        if (string.Equals(actionId, "home", StringComparison.OrdinalIgnoreCase))
        {
            _ = ApplyActionSelectionAsync(actionId, null);
            return;
        }

        var habitatLoadout = HabitatLoadoutResolver.Resolve(_state, _content);
        if (!habitatLoadout.ActionOptions.TryGetValue(actionId, out var options) || options.Count <= 1)
        {
            _ = ApplyActionSelectionAsync(actionId, options?.FirstOrDefault()?.Id);
            return;
        }

        var nextSettings = _state.SettingsSnapshot;
        var autoPinned = !_state.IsPinned;
        nextSettings = WithSetting(nextSettings, "actions_auto_pinned", autoPinned.ToString());
        _state = _state with
        {
            IsPinned = true,
            ActiveTool = new ToolSession($"action:{actionId}", true),
            SettingsSnapshot = nextSettings
        };
        TraceLog.Write("ui-command", $"open-action action={actionId} count={options.Count}");
        _ = SyncPinnedAsync();
        ApplyModeAndLayout();
        _ = PersistAsync();
    }

    private async Task ApplyActionSelectionAsync(string actionId, string? itemId)
    {
        if (_state is null || _content is null)
        {
            return;
        }

        var actionDefinition = _content.Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.OrdinalIgnoreCase));
        if (actionDefinition is null)
        {
            return;
        }

        if (!_petSimulationEngine.IsActionEnabled(actionId, _state.ActivePets))
        {
            SetFeedback($"{actionDefinition.DisplayName} is not needed right now.");
            return;
        }

        var targetNames = _state.ActivePets
            .Where(pet => !pet.IsDead)
            .Select(pet => pet.Name)
            .Take(3)
            .ToList();
        var targetSummary = targetNames.Count == 0 ? "no living pets" : string.Join(", ", targetNames);

        _state = _state with
        {
            ActivePets = _petSimulationEngine.ApplyAction(actionDefinition, _state.ActivePets, DateTimeOffset.UtcNow),
        };
        _state = CloseToolSession(_state);
        await SyncPinnedAsync();

        if (string.Equals(actionId, "home", StringComparison.OrdinalIgnoreCase))
        {
            _state = _state with { ActiveEnvironmentId = _state.ActivePets.FirstOrDefault()?.SpeciesId ?? _state.ActiveEnvironmentId };
        }

        var habitatLoadout = HabitatLoadoutResolver.Resolve(_state, _content);
        var selectedItem = !string.IsNullOrWhiteSpace(itemId) &&
                           habitatLoadout.ActionOptions.TryGetValue(actionId, out var options)
            ? options.FirstOrDefault(option => string.Equals(option.Id, itemId, StringComparison.OrdinalIgnoreCase))
            : null;

        var baseFeedback = selectedItem is null
            ? HabitatLoadoutResolver.BuildActionFeedback(actionId, actionDefinition, habitatLoadout)
            : BuildSelectedActionFeedback(actionId, actionDefinition.DisplayName, selectedItem);
        _feedbackText = $"{baseFeedback} Target: {targetSummary}.";
        TraceLog.Write("action", $"{actionId}:{selectedItem?.Id ?? "default"}");
        await PersistAndRenderAsync();
    }

    private static string BuildSelectedActionFeedback(string actionId, string actionLabel, HabitatDisplayItem selectedItem)
    {
        return actionId switch
        {
            "feed" => $"Served {selectedItem.Label.ToLowerInvariant()} for feeding time.",
            "water" => $"Set out {selectedItem.Label.ToLowerInvariant()} for fresh water.",
            "rest" => $"Settled everyone into the {selectedItem.Label.ToLowerInvariant()}.",
            "play" => $"Brought out the {selectedItem.Label.ToLowerInvariant()} for play.",
            "groom" => $"Used the {selectedItem.Label.ToLowerInvariant()} for grooming.",
            "bath" => $"Set up bath time with the {selectedItem.Label.ToLowerInvariant()}.",
            "medicine" => $"Applied the {selectedItem.Label.ToLowerInvariant()} for treatment.",
            "doctor" => $"Checked everyone over with the {selectedItem.Label.ToLowerInvariant()}.",
            "home" => $"Called everyone back toward the {selectedItem.Label.ToLowerInvariant()}.",
            _ => $"{actionLabel} used {selectedItem.Label.ToLowerInvariant()}."
        };
    }

    private async Task AddLinksAsync(IEnumerable<string> urls, string source)
    {
        if (_state is null)
        {
            return;
        }

        var basketItems = _state.BasketItems.ToList();
        var addedCount = 0;
        foreach (var url in urls)
        {
            if (!BasketService.TryCreate(url, url, source, out var item))
            {
                continue;
            }

            basketItems = _basketService.Add(item, basketItems).ToList();
            addedCount++;
        }

        _state = _state with
        {
            BasketItems = basketItems,
            SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "webtools_visible", bool.TrueString)
        };
        SetFeedback(addedCount == 0 ? "No valid links found." : $"Basket now holds {basketItems.Count} link(s).");
        TraceLog.Write("basket", $"source={source} added={addedCount} count={basketItems.Count}");
        await PersistAndRenderAsync();
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

        var copiedToClipboard = TrySetClipboardText(item.Url);
        await _brokerClient.SendCommandAsync(ShellCommandTypes.OpenUrl, new OpenUrlCommand(item.Url));
        SetFeedback(copiedToClipboard
            ? $"Opened {item.Label}."
            : $"Opened {item.Label}. Clipboard stayed unchanged.");
        TraceLog.Write("basket", $"open id={id} url={item.Url}");
        Render();
    }

    private async Task DeleteBasketItemsAsync(IReadOnlyList<Guid> ids)
    {
        if (_state is null || ids.Count == 0)
        {
            return;
        }

        var basketItems = _state.BasketItems;
        foreach (var id in ids)
        {
            basketItems = _basketService.Remove(id, basketItems);
        }

        _state = _state with { BasketItems = basketItems };
        SetFeedback(ids.Count == 1 ? "Deleted 1 saved link." : $"Deleted {ids.Count} saved links.");
        TraceLog.Write("basket", $"delete count={ids.Count} remaining={_state.BasketItems.Count}");
        await PersistAndRenderAsync();
    }

    private void OnSettingChanged(string key, string value)
    {
        if (_state is null)
        {
            return;
        }

        var nextSettings = new Dictionary<string, string>(_state.SettingsSnapshot, StringComparer.OrdinalIgnoreCase)
        {
            [key] = value
        };
        _state = _state with { SettingsSnapshot = nextSettings };
        TraceLog.Write("settings", $"{key}={value}");
        if (TryParseAutonomousScopeSetting(key, out var scopeId) && bool.TryParse(value, out var enabled))
        {
            _autonomousScopeService.RecordEnabledChanged(scopeId, enabled, DateTimeOffset.UtcNow);
        }

        ApplyModeAndLayout();
        _ = PersistAsync();
    }

    private static bool TryParseAutonomousScopeSetting(string key, out string scopeId)
    {
        const string prefix = "autonomous_scope_";
        const string suffix = "_enabled";
        scopeId = "";
        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parsedScopeId = key[prefix.Length..^suffix.Length];
        scopeId = parsedScopeId;
        return AutonomousScopeService.KnownScopes.Any(scope =>
            scope.ScopeId.Equals(parsedScopeId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SaveAsync()
    {
        await PersistAsync();
        SetFeedback("Saved current companion state.");
    }

    private void SetFeedback(string message)
    {
        _feedbackText = message;
        TraceLog.Write("feedback", message);
        Render();
    }

    private CompanionState ApplyAmbientWorkCompanionState(CompanionState state, DateTimeOffset now)
    {
        var gameMode = _gameModeDetectorService.Evaluate(state.SettingsSnapshot);
        var coexistence = _coexistenceTriggerService.Evaluate(
            state.SettingsSnapshot,
            _desktopContext,
            new CoexistenceResourceSnapshot(GameModeActive: gameMode.IsGameModeActive),
            now);
        var dnd = _doNotDisturbScheduleService.Evaluate(state.SettingsSnapshot, now);
        if (!coexistence.IsQuieting && !dnd.IsActive)
        {
            return state;
        }

        var pets = state.ActivePets
            .Select(pet => pet with
            {
                CurrentAnimationState = PetAnimationState.Sleep,
                OverrideAnimationState = PetAnimationState.Sleep,
                OverrideAnimationEndsAtUtc = now.AddSeconds(3)
            })
            .ToList();
        var reason = coexistence.IsQuieting ? coexistence.Reason : dnd.Reason;
        if (!string.Equals(reason, _lastForcedPetAnimationReason, StringComparison.OrdinalIgnoreCase) ||
            now - _lastForcedPetAnimationAtUtc > TimeSpan.FromMinutes(5))
        {
            _coexistenceTriggerService.RecordPetAnimationForced(now, reason);
            _lastForcedPetAnimationReason = reason;
            _lastForcedPetAnimationAtUtc = now;
        }

        return state with { ActivePets = pets };
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

    private static bool TrySetClipboardText(string value)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(value, true);
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(75);
            }
            catch (ExternalException)
            {
                Thread.Sleep(75);
            }
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        TraceLog.Write("shell", "shutdown-begin");
        _tickTimer.Stop();
        _runtimeSessionTracker.End(DateTimeOffset.UtcNow);
        _powerHandler.Dispose();
        if (_state is not null)
        {
            await PersistAsync();
        }

        if (_devControlServer is not null)
        {
            await _devControlServer.DisposeAsync();
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

    private EnvironmentDefinition ResolveEnvironment(CompanionState state)
    {
        var environmentId = state.ActiveEnvironmentId;
        if (string.IsNullOrWhiteSpace(environmentId) && state.ActivePets.Count > 0)
        {
            environmentId = state.ActivePets[0].SelectedEnvironmentId;
        }

        return _content!.Environments.FirstOrDefault(environment => string.Equals(environment.Id, environmentId, StringComparison.OrdinalIgnoreCase))
            ?? _content.Environments.First();
    }

    private CompanionState HydrateLoadedState(CompanionState state, GameContent content)
    {
        var speciesMap = content.Species.ToDictionary(species => species.Id, StringComparer.OrdinalIgnoreCase);
        var hydratedPets = state.ActivePets
            .Select((pet, index) =>
            {
                if (!speciesMap.TryGetValue(pet.SpeciesId, out var species))
                {
                    species = content.Species[Math.Min(index, content.Species.Count - 1)];
                }

                var color = string.IsNullOrWhiteSpace(pet.ColorVariant)
                    ? (species.SupportedColors ?? ["blue"])[index % (species.SupportedColors ?? ["blue"]).Count]
                    : pet.ColorVariant;

                return pet with
                {
                    AccentColor = string.IsNullOrWhiteSpace(pet.AccentColor) ? species.AccentColor : pet.AccentColor,
                    ColorVariant = color,
                    BaseSpeed = pet.BaseSpeed <= 0 ? species.BaseSpeed : pet.BaseSpeed,
                    Fitness = pet.Fitness <= 0 ? 68 : pet.Fitness,
                    BiologicalAgeMinutes = pet.BiologicalAgeMinutes <= 0 && pet.AgeStage != PetAgeStage.Baby
                        ? pet.AgeStage switch
                        {
                            PetAgeStage.Teen => 72,
                            PetAgeStage.Adult => 258,
                            PetAgeStage.Senior => 498,
                            _ => 0
                        }
                        : pet.BiologicalAgeMinutes,
                    Personality = pet.Personality ?? species.PersonalitySeed ?? new PetPersonalityProfile(),
                    HabitProfile = pet.HabitProfile ?? new PetHabitProfile(),
                    ActiveConditions = pet.ActiveConditions ?? (
                        string.IsNullOrWhiteSpace(species.InnateConditionId)
                            ? []
                            : [new PetConditionRecord(species.InnateConditionId, 1, true)]),
                    SelectedEnvironmentId = string.IsNullOrWhiteSpace(pet.SelectedEnvironmentId) ? species.DefaultEnvironmentId : pet.SelectedEnvironmentId,
                    AnimationStartedAtUtc = pet.AnimationStartedAtUtc == default ? DateTimeOffset.UtcNow : pet.AnimationStartedAtUtc,
                    AgeStageStartedAtUtc = pet.AgeStageStartedAtUtc == default ? DateTimeOffset.UtcNow : pet.AgeStageStartedAtUtc,
                    ActiveStatuses = pet.ActiveStatuses ?? []
                };
            })
            .ToList();

        var environmentId = string.IsNullOrWhiteSpace(state.ActiveEnvironmentId)
            ? hydratedPets.FirstOrDefault()?.SelectedEnvironmentId ?? content.Environments.First().Id
            : state.ActiveEnvironmentId;

        return state with
        {
            ActiveEnvironmentId = environmentId,
            ActivePets = hydratedPets,
            TaskCards = state.TaskCards ?? [],
            ActiveTool = string.IsNullOrWhiteSpace(state.ActiveTool.ToolId)
                ? new ToolSession("basket", state.ActiveTool.IsOpen)
                : state.ActiveTool,
            SettingsSnapshot = ApplyDefaultSettings(state.SettingsSnapshot)
        };
    }

    private CompanionState ApplyAuditScenarioOverride(CompanionState state, GameContent content)
    {
        var scenarioPath = Environment.GetEnvironmentVariable("WEVITO_VNEXT_SCENARIO_PATH");
        if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
        {
            return state;
        }

        try
        {
            var scenario = JsonSerializer.Deserialize<SpriteAuditScenario>(
                File.ReadAllText(scenarioPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (scenario is null)
            {
                return state;
            }

            var now = DateTimeOffset.UtcNow;
            var pets = new List<PetActor>();
            var speciesMap = content.Species.ToDictionary(species => species.Id, StringComparer.OrdinalIgnoreCase);
            var nextIndex = 1;

            foreach (var petScenario in scenario.Pets ?? [])
            {
                if (string.IsNullOrWhiteSpace(petScenario.SpeciesId) || !speciesMap.TryGetValue(petScenario.SpeciesId, out var species))
                {
                    continue;
                }

                var age = Enum.TryParse<PetAgeStage>(petScenario.AgeStage, true, out var parsedAge)
                    ? parsedAge
                    : species.SupportedAgeStages?.FirstOrDefault() ?? PetAgeStage.Adult;
                var gender = Enum.TryParse<PetGender>(petScenario.Gender, true, out var parsedGender)
                    ? parsedGender
                    : species.SupportedGenders?.FirstOrDefault() ?? PetGender.Female;
                var color = ResolveColor(species, string.IsNullOrWhiteSpace(petScenario.ColorVariant) ? "blue" : petScenario.ColorVariant);
                var pet = _petSimulationEngine.CreatePet(
                    species,
                    age,
                    gender,
                    color,
                    string.IsNullOrWhiteSpace(petScenario.Name) ? $"{species.DisplayName} {nextIndex}" : petScenario.Name!,
                    now,
                    activeStatuses: [PetStatusType.Comforted],
                    nextDecisionOffsetSeconds: 0.2 * nextIndex);

                if (Enum.TryParse<PetAnimationState>(petScenario.AnimationState, true, out var parsedAnimation))
                {
                    pet = pet with
                    {
                        CurrentAnimationState = parsedAnimation,
                        AnimationStartedAtUtc = now,
                        OverrideAnimationState = parsedAnimation,
                        OverrideAnimationEndsAtUtc = now.AddMinutes(10)
                    };
                }

                if (!string.IsNullOrWhiteSpace(petScenario.LastActionId))
                {
                    var lastActionAtUtc = now.AddSeconds(-Math.Max(0, petScenario.LastActionSecondsAgo ?? 0));
                    pet = pet with
                    {
                        LastActionId = petScenario.LastActionId!,
                        LastActionAtUtc = lastActionAtUtc,
                        OverrideAnimationEndsAtUtc = now.AddMinutes(10)
                    };
                }

                if (TryParseFacingDirection(petScenario.FacingDirection, out var parsedFacing))
                {
                    pet = pet with { FacingDirection = parsedFacing };
                }

                if (!string.IsNullOrWhiteSpace(petScenario.EnvironmentId))
                {
                    pet = pet with { SelectedEnvironmentId = petScenario.EnvironmentId! };
                }

                pets.Add(pet);
                nextIndex++;
            }

            var mode = Enum.TryParse<CompanionMode>(scenario.Mode, true, out var parsedMode)
                ? parsedMode
                : state.Mode;
            var environmentId = !string.IsNullOrWhiteSpace(scenario.ActiveEnvironmentId)
                ? scenario.ActiveEnvironmentId!
                : pets.FirstOrDefault()?.SelectedEnvironmentId
                  ?? pets.FirstOrDefault()?.SpeciesId
                  ?? state.ActiveEnvironmentId;
            var settings = state.SettingsSnapshot.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
            if (scenario.SettingsSnapshot is not null)
            {
                foreach (var entry in scenario.SettingsSnapshot)
                {
                    settings[entry.Key] = entry.Value;
                }
            }

            TraceLog.Write("audit-scenario", $"path={scenarioPath} pets={pets.Count} mode={mode} env={environmentId}");
            return state with
            {
                Mode = mode,
                IsPinned = mode == CompanionMode.Pinned,
                ActiveEnvironmentId = environmentId,
                ActivePets = pets.Count > 0 ? pets : state.ActivePets,
                BasketItems = scenario.ClearBasket ? [] : state.BasketItems,
                ActiveTool = string.IsNullOrWhiteSpace(scenario.ToolId)
                    ? state.ActiveTool with { IsOpen = scenario.ToolIsOpen ?? state.ActiveTool.IsOpen }
                    : new ToolSession(scenario.ToolId!, scenario.ToolIsOpen ?? true),
                SettingsSnapshot = settings
            };
        }
        catch (Exception ex)
        {
            TraceLog.Write("audit-scenario", $"failed path={scenarioPath} error={ex.Message}");
            return state;
        }
    }

    private async Task ToggleToolAsync(string toolId)
    {
        if (_state is null)
        {
            return;
        }

        var currentlyOpen = _state.ActiveTool.IsOpen && string.Equals(_state.ActiveTool.ToolId, toolId, StringComparison.OrdinalIgnoreCase);
        var nextToolState = !currentlyOpen;
        var nextPinned = _state.IsPinned;
        var nextSettings = WithSetting(_state.SettingsSnapshot, "webtools_visible", bool.TrueString);
        if (string.Equals(toolId, "actions", StringComparison.OrdinalIgnoreCase))
        {
            if (nextToolState)
            {
                var autoPinned = !_state.IsPinned;
                nextPinned = true;
                nextSettings = WithSetting(nextSettings, "actions_auto_pinned", autoPinned.ToString());
            }
            else if (GetSettingBool(nextSettings, "actions_auto_pinned") && _state.IsPinned)
            {
                nextPinned = false;
                nextSettings = WithSetting(nextSettings, "actions_auto_pinned", string.Empty);
            }
        }
        else if (!_state.IsPinned && _state.Mode == CompanionMode.Passive && nextToolState)
        {
            nextPinned = true;
        }

        _state = _state with
        {
            IsPinned = nextPinned,
            ActiveTool = new ToolSession(toolId, nextToolState),
            SettingsSnapshot = nextSettings
        };
        TraceLog.Write("ui-command", $"toggle-tool tool={toolId} open={nextToolState} pinned={nextPinned}");

        if (_brokerClient is not null)
        {
            await SyncPinnedAsync();
        }

        ApplyModeAndLayout();
        await PersistAsync();
    }

    private async Task HandleDevToolCommandAsync(DevToolCommand command)
    {
        if (!_devToolsEnabled || _state is null || _content is null)
        {
            return;
        }

        switch (command.Kind)
        {
            case DevToolCommandKind.SelectPet:
                _state = _state with { SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "dev_selected_pet_id", command.PetId?.ToString() ?? string.Empty) };
                break;

            case DevToolCommandKind.AddPet:
                _state = AddDevPet(_state, command);
                break;

            case DevToolCommandKind.RemovePet:
                _state = RemoveDevPet(_state, command.PetId);
                break;

            case DevToolCommandKind.RemoveAllPets:
                _state = RemoveAllDevPets(_state);
                break;

            case DevToolCommandKind.SpawnColorSet:
                _state = SpawnDevColorSet(_state, command);
                break;

            case DevToolCommandKind.ApplyAppearance:
                _state = ApplyPetAppearance(_state, command);
                break;

            case DevToolCommandKind.ApplyEnvironment:
                _state = ApplyDevEnvironment(_state, command);
                break;

            case DevToolCommandKind.ApplyPreset:
                _state = ApplyDevPreset(_state, command);
                break;

            case DevToolCommandKind.ApplyVitals:
                _state = ApplyDevVitals(_state, command);
                break;

            case DevToolCommandKind.ApplyAnimation:
                _state = ApplyDevAnimation(_state, command);
                break;

            case DevToolCommandKind.ClearAnimation:
                _state = ClearDevAnimation(_state, command.PetId);
                break;

            case DevToolCommandKind.SetCondition:
                _state = SetDevCondition(_state, command);
                break;

            case DevToolCommandKind.ClearCondition:
                _state = ClearDevCondition(_state, command);
                break;
        }

        _state = _state with
        {
            ActivePets = _petSimulationEngine.ApplyLayout(
                _state.ActivePets,
                _homeWindow.Left + _homeWindow.GetStageRect().X + 24,
                _homeWindow.Top + _homeWindow.GetStageRect().Y + 20,
                _homeWindow.GetStageRect().Width - 48,
                _homeWindow.GetStageRect().Height - 36)
        };
        var workArea = _desktopContext?.WorkArea ?? new RectInt(0, 0, (int)SystemParameters.WorkArea.Width, (int)SystemParameters.WorkArea.Height);
        var roamBandRect = new RectInt(workArea.X, workArea.Bottom - (int)RoamBandHeight, workArea.Width, (int)RoamBandHeight);
        _state = _state with
        {
            ActivePets = _petSimulationEngine.Tick(_state.ActivePets, _state.Mode, roamBandRect, DateTimeOffset.UtcNow, 0)
        };
        SetFeedback($"Dev command applied: {command.Kind}");
        await PersistAndRenderAsync();
    }

    internal async Task<DevControlResponseEnvelope> HandleDevControlCommandAsync(DevControlCommandEnvelope envelope)
    {
        if (!_devToolsEnabled || _state is null || _content is null)
        {
            return DevControlFailure("Dev-control is available only after the dev Shell is ready.");
        }

        return envelope.CommandType switch
        {
            DevControlCommandTypes.GetSnapshot => DevControlSuccess("Snapshot refreshed."),
            DevControlCommandTypes.DeletePet => await DeletePetForDevControlAsync(DevControlPipeMessage.DeserializePayload<DevControlDeletePetRequest>(envelope.Payload)),
            DevControlCommandTypes.SpawnOrReplacePet => await SpawnOrReplacePetForDevControlAsync(DevControlPipeMessage.DeserializePayload<DevControlSpawnOrReplacePetRequest>(envelope.Payload)),
            DevControlCommandTypes.ApplyAction => await ApplyActionForDevControlAsync(DevControlPipeMessage.DeserializePayload<DevControlApplyActionRequest>(envelope.Payload)),
            DevControlCommandTypes.Roam => await StartRoamForDevControlAsync(DevControlPipeMessage.DeserializePayload<DevControlRoamRequest>(envelope.Payload)),
            VisualQaCommandTypes.ForceAnimation => await ForceAnimationForVisualQaAsync(DevControlPipeMessage.DeserializePayload<VisualQaForceAnimationRequest>(envelope.Payload)),
            VisualQaCommandTypes.ClearForcedAnimation => await ClearAnimationForVisualQaAsync(DevControlPipeMessage.DeserializePayload<VisualQaClearForcedAnimationRequest>(envelope.Payload)),
            VisualQaCommandTypes.GetAssetSource => GetAssetSourceForVisualQa(DevControlPipeMessage.DeserializePayload<VisualQaGetAssetSourceRequest>(envelope.Payload)),
            VisualQaCommandTypes.TagIssue => TagIssueForVisualQa(DevControlPipeMessage.DeserializePayload<VisualQaIssueTagRequest>(envelope.Payload)),
            VisualQaCommandTypes.ResetSaveSandbox => await ResetSaveSandboxForVisualQaAsync(DevControlPipeMessage.DeserializePayload<VisualQaResetSaveSandboxRequest>(envelope.Payload)),
            _ => DevControlFailure($"Unsupported dev-control command: {envelope.CommandType}.")
        };
    }

    internal DevControlSnapshot GetDevControlSnapshot()
    {
        if (_state is null || _content is null)
        {
            return DevControlSnapshot.Empty(DateTimeOffset.UtcNow);
        }

        return DevControlSnapshotBuilder.Build(_state.ActivePets, _content, DateTimeOffset.UtcNow);
    }

    internal async Task<DevControlResponseEnvelope> DeletePetForDevControlAsync(DevControlDeletePetRequest request)
    {
        if (_state is null || _content is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out var pet, out var message) || pet is null)
        {
            return DevControlFailure(message);
        }

        _state = RemoveDevPet(_state, pet.Id);
        _state = ApplyCurrentLayout(_state);
        SetFeedback($"Dev controller removed {pet.Name} without creating a ghost.");
        await PersistAndRenderAsync();
        return DevControlSuccess($"Removed {pet.Name}.");
    }

    internal async Task<DevControlResponseEnvelope> SpawnOrReplacePetForDevControlAsync(DevControlSpawnOrReplacePetRequest request)
    {
        if (_state is null || _content is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (request.SlotIndex < 0 || request.SlotIndex > 2)
        {
            return DevControlFailure("Slot must be 1, 2, or 3.");
        }

        var occupied = request.SlotIndex < _state.ActivePets.Count;
        if (occupied && !request.ReplaceIfOccupied)
        {
            return DevControlFailure("Replacement confirmation required.");
        }

        if (!TryParseEnum(request.LifeStage, out PetAgeStage ageStage))
        {
            return DevControlFailure($"Unknown life stage: {request.LifeStage}.");
        }

        if (!TryParseEnum(request.Gender, out PetGender gender))
        {
            return DevControlFailure($"Unknown gender: {request.Gender}.");
        }

        var species = ResolveSpecies(request.SpeciesId);
        var color = ResolveColor(species, request.ColorVariant);
        var newPet = _petSimulationEngine.CreatePet(
            species,
            ageStage,
            gender,
            color,
            $"{species.DisplayName} {request.SlotIndex + 1}",
            DateTimeOffset.UtcNow,
            activeStatuses: [PetStatusType.Comforted]);

        if (!HasRuntimeSprite(newPet))
        {
            return DevControlFailure($"No runtime idle sprites found for {species.Id}/{ageStage}/{gender}/{color}.");
        }

        var pets = _state.ActivePets.Take(3).ToList();
        if (!occupied && request.SlotIndex > pets.Count)
        {
            return DevControlFailure("Add pets to earlier empty slots first.");
        }

        if (occupied)
        {
            pets[request.SlotIndex] = newPet;
        }
        else
        {
            pets.Add(newPet);
        }

        _state = _state with
        {
            ActivePets = pets.Take(3).ToList(),
            ActiveEnvironmentId = string.IsNullOrWhiteSpace(_state.ActiveEnvironmentId) ? species.DefaultEnvironmentId : _state.ActiveEnvironmentId,
            SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "dev_selected_pet_id", newPet.Id.ToString())
        };
        _state = ApplyCurrentLayout(_state);
        SetFeedback($"Dev controller spawned {newPet.Name}: {species.Id} {ageStage} {gender} {color}.");
        await PersistAndRenderForDevControlAsync();
        return DevControlSuccess($"Spawned {newPet.Name}.");
    }

    internal async Task<DevControlResponseEnvelope> ApplyActionForDevControlAsync(DevControlApplyActionRequest request)
    {
        if (_state is null || _content is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out var pet, out var message) || pet is null)
        {
            return DevControlFailure(message);
        }

        var actionDefinition = _content.Actions.FirstOrDefault(action => string.Equals(action.Id, request.ActionId, StringComparison.OrdinalIgnoreCase));
        if (actionDefinition is null)
        {
            return DevControlFailure($"Unknown action: {request.ActionId}.");
        }

        var updatedPet = _petSimulationEngine.ApplyAction(actionDefinition, [pet], DateTimeOffset.UtcNow)[0];
        var pets = _state.ActivePets.Select(existing => existing.Id == pet.Id ? updatedPet : existing).ToList();
        _state = _state with { ActivePets = pets };
        SetFeedback($"Dev controller applied {actionDefinition.DisplayName} to {pet.Name}.");
        await PersistAndRenderAsync();
        return DevControlSuccess($"Applied {actionDefinition.DisplayName} to {pet.Name}.");
    }

    internal async Task<DevControlResponseEnvelope> StartRoamForDevControlAsync(DevControlRoamRequest request)
    {
        if (_state is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out var pet, out var message) || pet is null)
        {
            return DevControlFailure(message);
        }

        var now = DateTimeOffset.UtcNow;
        var durationSeconds = Math.Clamp(request.DurationSeconds <= 0 ? 10 : request.DurationSeconds, 1, 30);
        var targetX = pet.CurrentX <= pet.HomeX ? pet.HomeX + 180 : pet.HomeX - 180;
        var pets = _state.ActivePets.Select(existing => existing.Id == pet.Id
            ? existing with
            {
                BehaviorState = PetBehaviorState.Roaming,
                TargetX = targetX,
                TargetY = pet.HomeY,
                CurrentAnimationState = PetAnimationState.Walk,
                OverrideAnimationState = PetAnimationState.Walk,
                OverrideAnimationEndsAtUtc = now.AddSeconds(durationSeconds),
                AnimationStartedAtUtc = now
            }
            : existing).ToList();

        _state = _state with { ActivePets = pets };
        SetFeedback($"Dev controller started a {durationSeconds}s roam for {pet.Name}.");
        await PersistAndRenderAsync();
        return DevControlSuccess($"Roaming {pet.Name} for {durationSeconds}s.");
    }

    internal async Task<DevControlResponseEnvelope> ForceAnimationForVisualQaAsync(VisualQaForceAnimationRequest request)
    {
        if (_state is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out var pet, out var message) || pet is null)
        {
            return DevControlFailure(message);
        }

        if (!TryParseEnum(request.AnimationFamily, out PetAnimationState animationState))
        {
            return DevControlFailure($"Unknown animation: {request.AnimationFamily}.");
        }

        var now = DateTimeOffset.UtcNow;
        var durationSeconds = request.Loop ? 600 : 8;
        var playbackSpeed = request.PlaybackSpeed > 0 ? request.PlaybackSpeed : 1;
        var pets = _state.ActivePets.Select(existing => existing.Id == pet.Id
            ? existing with
            {
                CurrentAnimationState = animationState,
                OverrideAnimationState = animationState,
                OverrideAnimationEndsAtUtc = now.AddSeconds(durationSeconds),
                AnimationStartedAtUtc = now,
                VisualQaForcedFrameIndex = request.FrameIndex,
                VisualQaPlaybackSpeed = playbackSpeed
            }
            : existing).ToList();

        _state = _state with { ActivePets = pets };
        var frameLabel = request.FrameIndex is { } frameIndex ? $" frame {frameIndex}" : " loop";
        SetFeedback($"Visual QA forced {animationState}{frameLabel} on {pet.Name}.");
        await PersistAndRenderForDevControlAsync();
        return DevControlSuccess($"Forced {animationState}{frameLabel} on {pet.Name}.");
    }

    internal async Task<DevControlResponseEnvelope> ClearAnimationForVisualQaAsync(VisualQaClearForcedAnimationRequest request)
    {
        if (_state is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out var pet, out var message) || pet is null)
        {
            return DevControlFailure(message);
        }

        _state = ClearDevAnimation(_state, pet.Id);
        SetFeedback($"Visual QA cleared forced animation on {pet.Name}.");
        await PersistAndRenderForDevControlAsync();
        return DevControlSuccess($"Cleared forced animation on {pet.Name}.");
    }

    internal DevControlResponseEnvelope GetAssetSourceForVisualQa(VisualQaGetAssetSourceRequest request)
    {
        if (_state is null || _assetService is null)
        {
            return DevControlFailure("Shell state or asset service is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out var pet, out var message) || pet is null)
        {
            return DevControlFailure(message);
        }

        var animationId = string.IsNullOrWhiteSpace(request.AnimationFamily)
            ? pet.CurrentAnimationState.ToString().ToLowerInvariant()
            : request.AnimationFamily.ToLowerInvariant();
        var frames = _assetService.GetAnimationFramePaths(pet, animationId);
        if (frames.Count == 0)
        {
            return DevControlFailure($"No frames found for {pet.SpeciesId}/{pet.AgeStage}/{pet.Gender}/{pet.ColorVariant}/{animationId}.");
        }

        return DevControlSuccess($"Asset source for {pet.Name}: {frames[0]} ({frames.Count} frame(s)).");
    }

    internal DevControlResponseEnvelope TagIssueForVisualQa(VisualQaIssueTagRequest request)
    {
        if (_state is null || _content is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        if (!DevControlSnapshotBuilder.TryResolveSlot(_state.ActivePets, request.SlotIndex, request.ExpectedPetId, out _, out var message) &&
            request.ExpectedPetId is not null)
        {
            return DevControlFailure(message);
        }

        var writer = new VisualQaIssueReportWriter(Path.Combine(ResolveRepoRootOrBaseDirectory(), "vnext", "artifacts", "visual-qa-cockpit"));
        var result = writer.WriteIssue(request, GetDevControlSnapshot());
        return DevControlSuccess($"Tagged visual QA issue: {result.PacketPath}");
    }

    internal async Task<DevControlResponseEnvelope> ResetSaveSandboxForVisualQaAsync(VisualQaResetSaveSandboxRequest request)
    {
        if (_state is null || _content is null)
        {
            return DevControlFailure("Shell state is not ready.");
        }

        var mode = request.Mode.Trim().ToLowerInvariant();
        _state = mode switch
        {
            "fresh_start_egg_choice" => new DefaultStateFactory(_petSimulationEngine).Create(_content),
            "empty_three_slots" => _state with
            {
                ActivePets = [],
                ActiveTool = new ToolSession("settings", false),
                SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "starter_egg_pending", bool.FalseString)
            },
            "three_random_alive_pets" => SeedThreeVisualQaPets(_state),
            _ => _state
        };

        if (mode is not ("fresh_start_egg_choice" or "empty_three_slots" or "three_random_alive_pets"))
        {
            return DevControlFailure($"Unknown save sandbox mode: {request.Mode}.");
        }

        _state = HydrateLoadedState(_state, _content);
        _state = ApplyCurrentLayout(_state);
        SetFeedback($"Visual QA reset save sandbox: {mode}.");
        await PersistAndRenderForDevControlAsync();
        return DevControlSuccess($"Reset save sandbox: {mode}.");
    }

    private Task PersistAndRenderForDevControlAsync()
    {
        if (IsVisualQaFastMode())
        {
            return Task.CompletedTask;
        }

        return PersistAndRenderAsync();
    }

    private static bool IsVisualQaFastMode()
    {
        return string.Equals(Environment.GetEnvironmentVariable("WEVITO_VISUAL_QA_FAST_MODE"), "1", StringComparison.Ordinal);
    }

    private DevControlResponseEnvelope DevControlSuccess(string message)
    {
        return new DevControlResponseEnvelope(true, message, GetDevControlSnapshot());
    }

    private DevControlResponseEnvelope DevControlFailure(string message)
    {
        return new DevControlResponseEnvelope(false, message, GetDevControlSnapshot());
    }

    private CompanionState ApplyCurrentLayout(CompanionState state)
    {
        var stageRect = _homeWindow.GetStageRect();
        return state with
        {
            ActivePets = _petSimulationEngine.ApplyLayout(
                state.ActivePets,
                _homeWindow.Left + stageRect.X + 24,
                _homeWindow.Top + stageRect.Y + 20,
                stageRect.Width - 48,
                stageRect.Height - 36)
        };
    }

    private bool HasRuntimeSprite(PetActor pet)
    {
        if (_assetService is null)
        {
            return true;
        }

        return _assetService.GetAnimationFramePaths(pet, "idle").Count > 0;
    }

    private CompanionState SeedThreeVisualQaPets(CompanionState state)
    {
        if (_content is null)
        {
            return state;
        }

        var speciesIds = new[] { "goose", "fox", "frog" };
        var pets = new List<PetActor>();
        for (var index = 0; index < speciesIds.Length; index++)
        {
            var species = ResolveSpecies(speciesIds[index]);
            var color = ResolveColor(species, index switch
            {
                0 => "yellow",
                1 => "red",
                _ => "blue"
            });
            pets.Add(_petSimulationEngine.CreatePet(
                species,
                PetAgeStage.Baby,
                index == 1 ? PetGender.Male : PetGender.Female,
                color,
                $"{species.DisplayName} {index + 1}",
                DateTimeOffset.UtcNow,
                activeStatuses: [PetStatusType.Comforted]));
        }

        return state with
        {
            ActivePets = pets,
            ActiveTool = new ToolSession("settings", false),
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, "starter_egg_pending", bool.FalseString)
        };
    }

    private static bool TryParseEnum<TEnum>(string value, out TEnum result)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, true, out result);
    }

    private async Task AddStarterEggAsync(string colorVariant)
    {
        if (_state is null || _content is null)
        {
            return;
        }

        var egg = StarterEggCatalog.Resolve(colorVariant);
        if (egg is null)
        {
            SetFeedback("That egg is not available.");
            return;
        }

        if (!egg.IsEnabled)
        {
            SetFeedback(egg.DisabledReason);
            return;
        }

        var species = _content.Species.FirstOrDefault(candidate => string.Equals(candidate.Id, egg.SpeciesId, StringComparison.OrdinalIgnoreCase))
            ?? _content.Species.FirstOrDefault();
        if (species is null)
        {
            return;
        }

        var ageStage = species.SupportedAgeStages?.Contains(PetAgeStage.Baby) == true
            ? PetAgeStage.Baby
            : species.SupportedAgeStages?.FirstOrDefault() ?? PetAgeStage.Baby;
        var gender = species.SupportedGenders?.Contains(PetGender.Female) == true
            ? PetGender.Female
            : species.SupportedGenders?.FirstOrDefault() ?? PetGender.Female;
        var color = ResolveColor(species, egg.ColorVariant);
        _state = AddDevPet(_state, new DevToolCommand(
            DevToolCommandKind.AddPet,
            SpeciesId: species.Id,
            AgeStage: ageStage,
            Gender: gender,
            ColorVariant: color));
        _state = _state with
        {
            ActivePets = _petSimulationEngine.ApplyLayout(
                _state.ActivePets,
                _homeWindow.Left + _homeWindow.GetStageRect().X + 24,
                _homeWindow.Top + _homeWindow.GetStageRect().Y + 20,
                _homeWindow.GetStageRect().Width - 48,
                _homeWindow.GetStageRect().Height - 36)
        };
        _state = _state with
        {
            ActiveTool = new ToolSession("basket", false),
            SettingsSnapshot = WithSetting(_state.SettingsSnapshot, "starter_egg_pending", string.Empty)
        };
        SetFeedback($"{egg.Label} hatched. Open ACTIONS when you want to feed, water, or play.");
        await PersistAndRenderAsync();
    }

    private double GetHomeWindowHeight()
    {
        if (_state is null)
        {
            return HomeWindowFullHeight;
        }

        if (_state.Mode == CompanionMode.Passive)
        {
            return HomeWindowPassiveHeight;
        }

        return GetSettingBool("compact_hud") ? HomeWindowCompactHeight : HomeWindowFullHeight;
    }

    private (double Width, double Height) GetToolWindowSize(RectInt workArea)
    {
        if (_state is not null && string.Equals(_state.ActiveTool.ToolId, "dev", StringComparison.OrdinalIgnoreCase) && _devToolsEnabled)
        {
            return (DevToolWindowWidth, ResolveToolWindowHeight(workArea, DevToolWindowHeight));
        }

        return (ToolWindowWidth, ResolveToolWindowHeight(workArea, ToolWindowHeight));
    }

    internal static double ResolveToolWindowHeight(RectInt workArea, double requestedHeight)
    {
        const double margin = 20;
        var available = Math.Max(320, workArea.Height - margin);
        return Math.Min(requestedHeight, available);
    }

    private bool GetSettingBool(string key, bool defaultValue = false)
    {
        if (_state is null)
        {
            return defaultValue;
        }

        return GetSettingBool(_state.SettingsSnapshot, key, defaultValue);
    }

    private static bool GetSettingBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue = false)
    {
        if (settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private CompanionState CloseToolSession(CompanionState state)
    {
        var nextPinned = state.IsPinned;
        var nextSettings = state.SettingsSnapshot;
        if (GetSettingBool(nextSettings, "actions_auto_pinned") && state.IsPinned)
        {
            nextPinned = false;
            nextSettings = WithSetting(nextSettings, "actions_auto_pinned", string.Empty);
        }

        return state with
        {
            IsPinned = nextPinned,
            ActiveTool = new ToolSession("basket", false),
            SettingsSnapshot = nextSettings
        };
    }

    private async Task SyncPinnedAsync()
    {
        if (_state is not null && _brokerClient is not null)
        {
            await _brokerClient.SendCommandAsync(ShellCommandTypes.SetPinned, new SetPinnedCommand(_state.IsPinned));
        }
    }

    internal static IReadOnlyDictionary<string, string> ApplyDefaultSettings(IReadOnlyDictionary<string, string> settings)
    {
        var hydrated = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        hydrated.TryAdd("compact_hud", bool.FalseString);
        hydrated.TryAdd("show_pet_names", bool.FalseString);
        hydrated.TryAdd("show_status_summary", bool.TrueString);
        hydrated.TryAdd("webtools_visible", bool.FalseString);
        hydrated.TryAdd(AiIdentityService.AiIdentityNameSetting, AiIdentityService.DefaultAiName);
        hydrated.TryAdd(FirstLaunchWizardStateService.CompletedSetting, bool.FalseString);
        hydrated.TryAdd("pet_model_adapter_enabled", bool.FalseString);
        hydrated.TryAdd("pet_model_first_call_approved", bool.FalseString);
        hydrated.TryAdd(KillSwitchService.KillSwitchSetting, bool.FalseString);
        hydrated.TryAdd(ModelProviderModeService.ProviderModeSetting, ModelProviderModeService.LocalOnlyModeValue);
        hydrated.TryAdd(ModelProviderModeService.LocalProviderIdSetting, ModelProviderModeService.DefaultLocalProviderId);
        hydrated.TryAdd(ModelProviderModeService.LocalProviderAvailableSetting, bool.FalseString);
        hydrated.TryAdd(ModelProviderModeService.InProcessLocalRuntimeEnabledSetting, bool.FalseString);
        hydrated.TryAdd(ModelProviderModeService.LocalRuntimeEndpointSetting, LocalRuntimeProbeService.DefaultOllamaEndpoint);
        hydrated.TryAdd(ModelProviderModeService.LocalRuntimeModelSetting, LocalRuntimeProbeService.DefaultOllamaModel);
        hydrated.TryAdd(ModelProviderModeService.HostedProviderIdSetting, "none");
        hydrated.TryAdd(ModelProviderModeService.HostedProviderApprovedSetting, bool.FalseString);
        hydrated.TryAdd(RuntimeSupervisorService.QuietModeSetting, bool.FalseString);
        hydrated.TryAdd(RuntimeSupervisorService.PetOnlyModeSetting, bool.FalseString);
        hydrated.TryAdd(RuntimeSupervisorService.BackgroundWorkAllowedSetting, bool.FalseString);
        hydrated.TryAdd(RuntimeSupervisorService.NoFocusStealSetting, bool.TrueString);
        hydrated.TryAdd(RuntimeSupervisorService.AutoQuietFullscreenSetting, bool.TrueString);
        hydrated.TryAdd(RuntimeSupervisorService.MaxBackgroundTasksPerHourSetting, "4");
        hydrated.TryAdd(RuntimeSupervisorService.CpuBudgetPercentSetting, "20");
        hydrated.TryAdd(RuntimeSupervisorService.MemoryBudgetMbSetting, "512");
        hydrated.TryAdd(CoexistenceTriggerService.FullscreenEnabledSetting, bool.TrueString);
        hydrated.TryAdd(CoexistenceTriggerService.AppListEnabledSetting, bool.TrueString);
        hydrated.TryAdd(CoexistenceTriggerService.CpuEnabledSetting, bool.TrueString);
        hydrated.TryAdd(CoexistenceTriggerService.NetworkEnabledSetting, bool.TrueString);
        hydrated.TryAdd(CoexistenceTriggerService.GameModeEnabledSetting, bool.TrueString);
        hydrated.TryAdd(ProcessPriorityManagerService.EnabledSetting, bool.TrueString);
        hydrated.TryAdd(DiskIoBudgetService.EnabledSetting, bool.TrueString);
        hydrated.TryAdd(CodexCompileThrottleService.ActiveProcessorCapSetting, "2");
        hydrated.TryAdd(NotificationPolicyService.DeferDuringActivitySetting, bool.TrueString);
        hydrated.TryAdd(AudioOutputPolicyService.PetSoundEffectsEnabledSetting, bool.FalseString);
        hydrated.TryAdd(CursorReactivityService.EnabledSetting, bool.TrueString);
        hydrated.TryAdd(TrayIconDisciplineService.AnimationEnabledSetting, bool.FalseString);
        hydrated.TryAdd(PetVisualPolishLogger.AnimationBlendingSetting, bool.TrueString);
        hydrated.TryAdd(PetVisualPolishLogger.PositionInterpolationSetting, bool.TrueString);
        hydrated.TryAdd(PetVisualPolishLogger.IdleMicroBehaviorsSetting, bool.TrueString);
        hydrated.TryAdd(PetVisualPolishLogger.ParticleEffectsSetting, bool.TrueString);
        hydrated.TryAdd(PetVisualPolishLogger.WindowShakeReactionSetting, bool.TrueString);
        hydrated.TryAdd(PetStateContextInjector.AiMentionsPetStateSetting, bool.TrueString);
        hydrated.TryAdd(MultiMonitorService.PreferredMonitorSetting, string.Empty);
        hydrated.TryAdd(CoexistenceTriggerService.CpuThresholdSetting, "80");
        hydrated.TryAdd(CoexistenceTriggerService.NetworkThresholdSetting, "80");
        hydrated.TryAdd(DoNotDisturbScheduleService.EnabledSetting, bool.FalseString);
        hydrated.TryAdd(DoNotDisturbScheduleService.ScheduleSetting, "[]");
        hydrated.TryAdd(DoNotDisturbScheduleService.QuickToggleUntilUtcSetting, string.Empty);
        hydrated.TryAdd(AutonomousTaskScheduler.SchedulerEnabledSetting, bool.FalseString);
        hydrated.TryAdd(AutonomousTaskScheduler.SchedulerPreviewDispatchApprovedSetting, bool.FalseString);
        hydrated.TryAdd(AutonomousOperationsConfig.EnabledSetting, bool.FalseString);
        hydrated.TryAdd(AutonomousOperationsConfig.DailyCapSetting, "3");
        hydrated.TryAdd(AutonomousOperationsConfig.IntervalMinutesSetting, "10");
        hydrated.TryAdd(SupervisedImprovementLoopSettings.EnabledSetting, bool.FalseString);
        hydrated.TryAdd(InvariantViolationWatchdog.EnabledSetting, bool.FalseString);
        hydrated.TryAdd(LiveStatusFeed.PollSecondsSetting, "10");
        hydrated.TryAdd(WebResearchConnector.WebSearchEnabledSetting, bool.FalseString);
        hydrated.TryAdd(WebResearchConnector.WebBackendSetting, "offline");
        hydrated.TryAdd(WebResearchConnector.MaxFetchesPerHourSetting, "30");
        hydrated.TryAdd(WebResearchConnector.MaxFetchesPerTaskSetting, "5");
        hydrated.TryAdd(SettingKeys.LocalDocumentRetrievalEnabled, bool.FalseString);
        hydrated.TryAdd(SettingKeys.LocalDocumentRetrievalRoot, SettingKeys.DefaultLocalDocumentRetrievalRoot());
        hydrated.TryAdd(SettingKeys.LocalDocumentRetrievalMaxFileBytes, SettingKeys.LocalDocumentRetrievalDefaultMaxFileBytes);
        hydrated.TryAdd(ToolCatalog.AdvancedToolsVisibleSetting, bool.FalseString);
        hydrated.TryAdd("evidence_dashboard_date_range", "24h");
        hydrated.TryAdd("evidence_dashboard_max_packets", EvidenceSummaryService.DefaultMaxPackets.ToString());
        return hydrated;
    }

    private CompanionState RecordToolHubLayoutChangedOnce(CompanionState state)
    {
        if (GetSettingBool(state.SettingsSnapshot, ToolCatalog.LayoutAnnouncementSetting) ||
            KillSwitchService.IsActive(state.SettingsSnapshot))
        {
            return state;
        }

        _auditLedgerService.Record(new EvidencePacket(
            Guid.NewGuid(),
            ToolCatalog.LayoutChangedPacketKind,
            TaskCardId: null,
            DateTimeOffset.UtcNow,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: "Tool Hub layout v1 loaded: primary tabs stay visible, advanced tabs are hidden behind the Advanced toggle.",
            Status: "Completed"));

        return state with
        {
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, ToolCatalog.LayoutAnnouncementSetting, bool.TrueString)
        };
    }

    private static string ResolveDataRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable("WEVITO_VNEXT_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            return Path.GetFullPath(overrideRoot);
        }

        return AppRepository.ResolveDefaultDataRoot();
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

    private void TraceDisciplinePolicySnapshot(IReadOnlyDictionary<string, string> settings)
    {
        var audioDecision = _audioOutputPolicyService.Evaluate(
            new AudioOutputRequest("startup", UserTriggered: false, IsTextToSpeech: false),
            settings,
            DateTimeOffset.UtcNow);
        var trayDecision = _trayIconDisciplineService.DecideAnimation(settings);
        var monitorDecision = _multiMonitorService.ResolvePreferredMonitor(
            settings,
            [new MonitorDescriptor("primary", IsPrimary: true)]);
        var cursorDecision = _cursorReactivityService.Evaluate(
            new CursorReactivityRequest("startup", 0, 0, 999, 999, DateTimeOffset.UtcNow),
            settings);
        var notificationDefers = NotificationPolicyService.ShouldDefer(
            new NotificationContext(UserTypingRecently: true, ForegroundFullscreen: false, CoexistenceActive: false, DoNotDisturbActive: false, DateTimeOffset.UtcNow),
            settings);
        TraceLog.Write(
            "discipline-policy",
            $"audio={audioDecision.Reason} tray={trayDecision.Reason} monitor={monitorDecision.Reason} cursor={cursorDecision.Reason} notificationDefers={notificationDefers}");
    }

    private void ShowWindowWithFocusDiscipline(Window window, string name, bool userInitiated)
    {
        var decision = _focusDisciplineService.Decide(
            new WindowShowRequest(name, userInitiated, IsFirstLaunchWizard: false),
            DateTimeOffset.UtcNow);
        window.ShowActivated = decision.ShowActivated;
        window.Show();
        if (decision.ShowActivated)
        {
            window.Activate();
        }

        TraceLog.Write("focus-discipline", $"window={name} activated={decision.ShowActivated} reason={decision.Reason}");
    }

    private IReadOnlyDictionary<string, string> WithSetting(IReadOnlyDictionary<string, string> settings, string key, string value)
    {
        var next = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
        {
            next.Remove(key);
        }
        else
        {
            next[key] = value;
        }

        return next;
    }

    private CompanionState AddDevPet(CompanionState state, DevToolCommand command)
    {
        var species = ResolveSpecies(command.SpeciesId);
        var ageStage = command.AgeStage ?? species.SupportedAgeStages?.FirstOrDefault() ?? PetAgeStage.Adult;
        var gender = command.Gender ?? species.SupportedGenders?.FirstOrDefault() ?? PetGender.Female;
        var color = ResolveColor(species, command.ColorVariant);
        var existingCount = state.ActivePets.Count(pet => string.Equals(pet.SpeciesId, species.Id, StringComparison.OrdinalIgnoreCase));
        var pet = _petSimulationEngine.CreatePet(
            species,
            ageStage,
            gender,
            color,
            $"{species.DisplayName} {existingCount + 1}",
            DateTimeOffset.UtcNow,
            activeStatuses: [PetStatusType.Comforted]);
        var pets = state.ActivePets.Concat([pet]).ToList();
        return state with
        {
            ActivePets = pets,
            ActiveEnvironmentId = string.IsNullOrWhiteSpace(state.ActiveEnvironmentId) ? species.DefaultEnvironmentId : state.ActiveEnvironmentId,
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, "dev_selected_pet_id", pet.Id.ToString())
        };
    }

    private CompanionState RemoveDevPet(CompanionState state, Guid? petId)
    {
        if (petId is null)
        {
            return state;
        }

        var pets = state.ActivePets.Where(pet => pet.Id != petId.Value).ToList();
        var nextSelected = pets.FirstOrDefault()?.Id.ToString() ?? string.Empty;
        return state with
        {
            ActivePets = pets,
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, "dev_selected_pet_id", nextSelected)
        };
    }

    private CompanionState RemoveAllDevPets(CompanionState state)
    {
        return state with
        {
            ActivePets = [],
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, "dev_selected_pet_id", string.Empty)
        };
    }

    private CompanionState SpawnDevColorSet(CompanionState state, DevToolCommand command)
    {
        var species = ResolveSpecies(command.SpeciesId);
        var ageStage = command.AgeStage ?? species.SupportedAgeStages?.FirstOrDefault() ?? PetAgeStage.Adult;
        var gender = command.Gender ?? species.SupportedGenders?.FirstOrDefault() ?? PetGender.Female;
        var colors = species.SupportedColors?.ToList() ?? ["blue"];
        var now = DateTimeOffset.UtcNow;
        var pets = state.ActivePets.ToList();

        foreach (var color in colors)
        {
            if (pets.Any(pet =>
                string.Equals(pet.SpeciesId, species.Id, StringComparison.OrdinalIgnoreCase) &&
                pet.AgeStage == ageStage &&
                pet.Gender == gender &&
                string.Equals(pet.ColorVariant, color, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var variantIndex = pets.Count(pet => string.Equals(pet.SpeciesId, species.Id, StringComparison.OrdinalIgnoreCase)) + 1;
            pets.Add(_petSimulationEngine.CreatePet(
                species,
                ageStage,
                gender,
                color,
                $"{species.DisplayName} {variantIndex}",
                now,
                activeStatuses: [PetStatusType.Comforted]));
        }

        var selectedPetId = pets.LastOrDefault()?.Id.ToString() ?? string.Empty;
        return state with
        {
            ActivePets = pets,
            ActiveEnvironmentId = species.DefaultEnvironmentId,
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, "dev_selected_pet_id", selectedPetId)
        };
    }

    private CompanionState ApplyPetAppearance(CompanionState state, DevToolCommand command)
    {
        if (command.PetId is null)
        {
            return state;
        }

        var species = ResolveSpecies(command.SpeciesId);
        var ageStage = command.AgeStage ?? PetAgeStage.Adult;
        var gender = command.Gender ?? PetGender.Female;
        var color = ResolveColor(species, command.ColorVariant);
        var now = DateTimeOffset.UtcNow;
        var pets = state.ActivePets
            .Select(pet => pet.Id != command.PetId.Value
                ? pet
                : _petSimulationEngine.ReconfigurePet(pet, species, ageStage, gender, color, now))
            .ToList();
        return state with
        {
            ActivePets = pets,
            ActiveEnvironmentId = species.DefaultEnvironmentId,
            SettingsSnapshot = WithSetting(state.SettingsSnapshot, "dev_selected_pet_id", command.PetId.Value.ToString())
        };
    }

    private CompanionState ApplyDevEnvironment(CompanionState state, DevToolCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EnvironmentId) || _content is null)
        {
            return state;
        }

        var environment = _content.Environments.FirstOrDefault(item => string.Equals(item.Id, command.EnvironmentId, StringComparison.OrdinalIgnoreCase));
        if (environment is null)
        {
            return state;
        }

        var pets = state.ActivePets
            .Select(pet => command.PetId is null || pet.Id == command.PetId.Value
                ? pet with { SelectedEnvironmentId = environment.Id }
                : pet)
            .ToList();

        return state with
        {
            ActiveEnvironmentId = environment.Id,
            ActivePets = pets
        };
    }

    private CompanionState ApplyDevPreset(CompanionState state, DevToolCommand command)
    {
        if (command.PetId is null)
        {
            return state;
        }

        var now = DateTimeOffset.UtcNow;
        var pets = state.ActivePets.Select(pet =>
        {
            if (pet.Id != command.PetId.Value)
            {
                return pet;
            }

            return command.PresetId switch
            {
                "hungry" => pet with { Hunger = 8, Health = Math.Max(pet.Health, 72) },
                "thirsty" => pet with { Thirst = 8, Health = Math.Max(pet.Health, 72) },
                "tired" => pet with { Energy = 8 },
                "dirty" => pet with { Cleanliness = 8 },
                "lonely" => pet with { Affection = 8, Comfort = 18 },
                "sick" => pet with { Health = 26, Comfort = 30 },
                "healthy" => pet with
                {
                    Health = 100,
                    Hunger = Math.Max(pet.Hunger, 70),
                    Thirst = Math.Max(pet.Thirst, 70),
                    Energy = Math.Max(pet.Energy, 70),
                    Cleanliness = Math.Max(pet.Cleanliness, 70),
                    Fitness = Math.Max(pet.Fitness, 64),
                    HabitProfile = (pet.HabitProfile ?? new PetHabitProfile()) with { Nutrition = 78, Hydration = 78, Exercise = 72, Hygiene = 76, Affection = 76, Rest = 76, Medical = 82, Stress = 16 },
                    ActiveConditions = (pet.ActiveConditions ?? []).Where(condition => condition.IsInnate).ToList()
                },
                "comforted" => pet with { Comfort = 96, Affection = 96, Health = Math.Max(pet.Health, 80) },
                "obese" => pet with
                {
                    Hunger = 98,
                    Fitness = 18,
                    ActiveConditions = ReplaceCondition(pet.ActiveConditions, new PetConditionRecord("obesity", 2, false))
                },
                "malnourished" => pet with
                {
                    Hunger = 6,
                    Health = Math.Min(pet.Health, 42),
                    ActiveConditions = ReplaceCondition(pet.ActiveConditions, new PetConditionRecord("malnutrition", 2, false))
                },
                "anxious" => pet with
                {
                    Affection = 10,
                    Comfort = 12,
                    ActiveConditions = ReplaceCondition(pet.ActiveConditions, new PetConditionRecord("anxiety", 2, false))
                },
                "depressed" => pet with
                {
                    Affection = 12,
                    Comfort = 16,
                    Health = Math.Min(pet.Health, 56),
                    ActiveConditions = ReplaceCondition(pet.ActiveConditions, new PetConditionRecord("depression", 2, false))
                },
                "injured" => pet with
                {
                    Fitness = 14,
                    Energy = 18,
                    Health = Math.Min(pet.Health, 44),
                    ActiveConditions = ReplaceCondition(pet.ActiveConditions, new PetConditionRecord("injury", 2, false))
                },
                "elder" => pet with
                {
                    BiologicalAgeMinutes = 520,
                    Energy = Math.Min(pet.Energy, 48),
                    Fitness = Math.Min(pet.Fitness, 40)
                },
                "foodie" => pet with { Personality = (pet.Personality ?? new PetPersonalityProfile()) with { FoodLove = 68, Cheerfulness = 18 } },
                "cuddly" => pet with { Personality = (pet.Personality ?? new PetPersonalityProfile()) with { CuddleNeed = 70, SocialNeed = 56 } },
                "neat" => pet with { Personality = (pet.Personality ?? new PetPersonalityProfile()) with { CleanlinessPreference = 72, Stubbornness = -12 } },
                "playful" => pet with { Personality = (pet.Personality ?? new PetPersonalityProfile()) with { Playfulness = 72, ActivityLevel = 64, Cheerfulness = 32 } },
                "stubborn" => pet with { Personality = (pet.Personality ?? new PetPersonalityProfile()) with { Stubbornness = 76, Cheerfulness = -10 } },
                "resilient" => pet with
                {
                    Personality = (pet.Personality ?? new PetPersonalityProfile()) with { Cheerfulness = 60, Stubbornness = -18 },
                    HabitProfile = (pet.HabitProfile ?? new PetHabitProfile()) with { Nutrition = 82, Hydration = 82, Exercise = 78, Hygiene = 80, Affection = 82, Rest = 80, Medical = 84, Stress = 14 }
                },
                "recall" => pet with
                {
                    TargetX = pet.HomeX,
                    TargetY = pet.HomeY,
                    BehaviorState = PetBehaviorState.Recalling,
                    OverrideAnimationState = PetAnimationState.Idle,
                    OverrideAnimationEndsAtUtc = now.AddSeconds(1.0),
                    AnimationStartedAtUtc = now
                },
                _ => pet
            };
        }).ToList();

        return state with { ActivePets = pets };
    }

    private CompanionState ApplyDevVitals(CompanionState state, DevToolCommand command)
    {
        if (command.PetId is null)
        {
            return state;
        }

        var pets = state.ActivePets.Select(pet =>
        {
            if (pet.Id != command.PetId.Value)
            {
                return pet;
            }

            return pet with
            {
                Hunger = command.Hunger ?? pet.Hunger,
                Thirst = command.Thirst ?? pet.Thirst,
                Energy = command.Energy ?? pet.Energy,
                Cleanliness = command.Cleanliness ?? pet.Cleanliness,
                Affection = command.Affection ?? pet.Affection,
                Comfort = command.Comfort ?? pet.Comfort,
                Health = command.Health ?? pet.Health,
                Fitness = command.Fitness ?? pet.Fitness,
                BiologicalAgeMinutes = command.BiologicalAgeMinutes ?? pet.BiologicalAgeMinutes
            };
        }).ToList();

        return state with { ActivePets = pets };
    }

    private CompanionState ApplyDevAnimation(CompanionState state, DevToolCommand command)
    {
        if (command.PetId is null || command.AnimationState is null)
        {
            return state;
        }

        var now = DateTimeOffset.UtcNow;
        var durationSeconds = Math.Clamp(command.OverrideDurationSeconds ?? 8.0, 0.5, 600);
        var pets = state.ActivePets.Select(pet =>
        {
            if (pet.Id != command.PetId.Value)
            {
                return pet;
            }

            return pet with
            {
                CurrentAnimationState = command.AnimationState.Value,
                OverrideAnimationState = command.AnimationState.Value,
                OverrideAnimationEndsAtUtc = now.AddSeconds(durationSeconds),
                AnimationStartedAtUtc = now
            };
        }).ToList();

        return state with { ActivePets = pets };
    }

    private CompanionState ClearDevAnimation(CompanionState state, Guid? petId)
    {
        if (petId is null)
        {
            return state;
        }

        var pets = state.ActivePets.Select(pet =>
        {
            if (pet.Id != petId.Value)
            {
                return pet;
            }

            return pet with
            {
                OverrideAnimationState = null,
                OverrideAnimationEndsAtUtc = null,
                VisualQaForcedFrameIndex = null,
                VisualQaPlaybackSpeed = 1,
                AnimationStartedAtUtc = DateTimeOffset.UtcNow
            };
        }).ToList();

        return state with { ActivePets = pets };
    }

    private CompanionState SetDevCondition(CompanionState state, DevToolCommand command)
    {
        if (command.PetId is null || string.IsNullOrWhiteSpace(command.ConditionId))
        {
            return state;
        }

        var severity = Math.Clamp(command.ConditionSeverity ?? 1, 1, 3);
        var pets = state.ActivePets.Select(pet =>
        {
            if (pet.Id != command.PetId.Value)
            {
                return pet;
            }

            var isInnate = (pet.ActiveConditions ?? []).Any(condition =>
                string.Equals(condition.Id, command.ConditionId, StringComparison.OrdinalIgnoreCase) &&
                condition.IsInnate);

            return pet with
            {
                ActiveConditions = ReplaceCondition(pet.ActiveConditions, new PetConditionRecord(command.ConditionId, severity, isInnate))
            };
        }).ToList();

        return state with { ActivePets = pets };
    }

    private CompanionState ClearDevCondition(CompanionState state, DevToolCommand command)
    {
        if (command.PetId is null || string.IsNullOrWhiteSpace(command.ConditionId))
        {
            return state;
        }

        var pets = state.ActivePets.Select(pet =>
        {
            if (pet.Id != command.PetId.Value)
            {
                return pet;
            }

            var nextConditions = (pet.ActiveConditions ?? [])
                .Where(condition =>
                    !string.Equals(condition.Id, command.ConditionId, StringComparison.OrdinalIgnoreCase) ||
                    condition.IsInnate)
                .ToList();

            return pet with
            {
                ActiveConditions = nextConditions
            };
        }).ToList();

        return state with { ActivePets = pets };
    }

    private static IReadOnlyList<PetConditionRecord> ReplaceCondition(IReadOnlyList<PetConditionRecord>? source, PetConditionRecord replacement)
    {
        var next = (source ?? []).Where(condition => !string.Equals(condition.Id, replacement.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        next.Add(replacement);
        return next.OrderBy(condition => condition.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private SpeciesDefinition ResolveSpecies(string speciesId)
    {
        return _content!.Species.FirstOrDefault(species => string.Equals(species.Id, speciesId, StringComparison.OrdinalIgnoreCase))
            ?? _content.Species.First();
    }

    private static string ResolveColor(SpeciesDefinition species, string requestedColor)
    {
        var supported = species.SupportedColors?.ToList() ?? ["blue"];
        return supported.FirstOrDefault(color => string.Equals(color, requestedColor, StringComparison.OrdinalIgnoreCase))
            ?? supported.First();
    }

    private static bool TryParseFacingDirection(JsonElement? value, out PetFacingDirection direction)
    {
        direction = PetFacingDirection.Right;
        if (value is null)
        {
            return false;
        }

        var element = value.Value;
        if (element.ValueKind == JsonValueKind.String)
        {
            return Enum.TryParse(element.GetString(), true, out direction);
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numeric))
        {
            direction = numeric < 0 ? PetFacingDirection.Left : PetFacingDirection.Right;
            return true;
        }

        return false;
    }

    private sealed record SpriteAuditScenario(
        string? Mode,
        string? ActiveEnvironmentId,
        bool ClearBasket = false,
        string? ToolId = null,
        bool? ToolIsOpen = null,
        IReadOnlyDictionary<string, string>? SettingsSnapshot = null,
        IReadOnlyList<SpriteAuditPetScenario>? Pets = null);

    private sealed record SpriteAuditPetScenario(
        string? SpeciesId,
        string? AgeStage,
        string? Gender,
        string? ColorVariant,
        string? AnimationState,
        string? LastActionId,
        double? LastActionSecondsAgo,
        JsonElement? FacingDirection,
        string? EnvironmentId,
        string? Name);

    private sealed record ScreenCaptureRegionResolution(
        ScreenCaptureRegionStatus Status,
        CaptureRegion? Region);

    private enum ScreenCaptureRegionStatus
    {
        Ready,
        Cancelled,
        MissingLastRegion
    }
}
