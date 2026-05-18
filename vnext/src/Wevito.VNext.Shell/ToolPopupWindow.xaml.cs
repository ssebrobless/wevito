using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.LocalRetrieval;
using Wevito.VNext.Core.Settings;
using Wevito.VNext.Core.Tools;

namespace Wevito.VNext.Shell;

public partial class ToolPopupWindow : Window
{
    internal const string ActionOptionDragFormat = "Wevito.ActionOption";
    private bool _closingSilently;
    private bool _suppressSettingEvents;
    private Guid? _lastRenderedDevPetId;
    private int _lastRenderedPetCount;
    private int _lastRenderedSpeciesCount;
    private List<BasketRowItem> _basketRows = [];
    private List<ActionOptionRowItem> _actionRows = [];
    private List<PetTaskQueueRowItem> _taskQueueRows = [];
    private List<LocalDocumentResultRowItem> _localDocumentRows = [];
    private List<EvidenceSummaryRowItem> _evidenceSummaryRows = [];
    private bool _suppressTaskQueueSelection;
    private string _autonomousScopePreviewText = "No autonomous scope preview has run in this session.";
    private string _localDocsStatusText = "Local document retrieval is disabled until Settings enables it.";

    public ToolPopupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            OverlayWindowStyler.Apply(this, clickThrough: false, noActivate: false);
            WindowDisplayAffinity.ExcludeFromCapture(this);
        };
        Visibility = Visibility.Hidden;

        HungerSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        ThirstSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        EnergySlider.ValueChanged += (_, _) => UpdateSliderLabels();
        CleanlinessSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        AffectionSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        ComfortSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        HealthSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        FitnessSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        BiologicalAgeSlider.ValueChanged += (_, _) => UpdateSliderLabels();
    }

    public event Func<Task>? CloseRequested;

    public event Func<Task>? PasteRequested;

    public event Func<Task>? SaveRequested;

    public event Func<Task>? OpenDevRequested;

    public event Func<Guid, Task>? OpenRequested;

    public event Func<IReadOnlyList<Guid>, Task>? DeleteRequested;

    public event Func<IReadOnlyList<string>, Task>? LinksDropped;

    public event Action<string, string>? SettingChanged;

    public event Func<Task>? AutonomousBetaConsentConfirmed;

    internal event Func<DevToolCommand, Task>? DevToolCommandRequested;

    public event Func<string, string, Task>? ActionOptionRequested;

    public event Func<string, Task>? ActionMenuRequested;

    public event Func<string, Task>? PetCommandSubmitted;

    public event Func<string, Task>? ToolTabRequested;

    public event Func<Task>? LocalDocsBuildRequested;

    public event Func<string, Task>? LocalDocsQueryRequested;

    public event Func<Task>? RunFirstLaunchWizardRequested;

    public event Func<string, Task>? AutonomousScopePreviewRequested;

    public event Func<Task>? EvidenceSummaryExportRequested;

    public event Func<Guid, TaskCardStatus, Task>? PetTaskStatusChangeRequested;

    public event Func<Guid, Task>? PetTaskPreviewRequested;

    public event Func<Guid, Task>? PetTaskExecutionRequested;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public void ConfigureChatServices(IModelAdapter modelAdapter, AuditLedgerService auditLedgerService, KillSwitchService killSwitchService, string repoRoot)
    {
        var historyStore = new ChatHistoryStore(killSwitchService: killSwitchService);
        var titleService = new ChatTitleService(historyStore, modelAdapter, auditLedgerService, killSwitchService);
        var sessionService = new ChatSessionService(historyStore, auditLedgerService, killSwitchService);
        var memoryStore = new PetMemoryStore();
        var contextBudgetService = new ChatContextBudgetService(historyStore, auditLedgerService, killSwitchService);
        var retrievalInjector = new RetrievalAutomaticInjector(memoryStore, auditLedgerService, killSwitchService);
        var summarizer = new RollingSummarizerService(historyStore, contextBudgetService, memoryStore, modelAdapter, auditLedgerService, killSwitchService);
        var streamingService = new ChatStreamingService(historyStore, modelAdapter, titleService, contextBudgetService, summarizer, retrievalInjector, auditLedgerService, killSwitchService);
        var pinnedContextStore = new PinnedContextStore(auditLedgerService: auditLedgerService, killSwitchService: killSwitchService);
        ChatPanel.Configure(sessionService, historyStore, streamingService);
        var draftRoot = Path.Combine(repoRoot, "vnext", "content", "benchmarks", "v1", "draft");
        var approvedRoot = Path.Combine(repoRoot, "vnext", "content", "benchmarks", "v1", "approved");
        var draftService = new BenchmarkCaseDraftService(auditLedgerService, killSwitchService);
        var curationStore = new BenchmarkCaseCurationStore(auditLedgerService: auditLedgerService, killSwitchService: killSwitchService);
        var curationViewModel = new BenchmarkCurationViewModel(draftRoot, approvedRoot, draftService, curationStore);
        BenchmarkCurationPanel.Configure(curationViewModel);
        ChatPanel.BenchmarkBookmarkRequested += text =>
        {
            curationViewModel.BookmarkFromChat(text);
            BenchmarkCurationPanel.Refresh();
            return Task.CompletedTask;
        };
        ChatPanel.PinMessageRequested += text =>
        {
            pinnedContextStore.Pin(text);
            return Task.CompletedTask;
        };
    }

    internal void Render(
        CompanionState state,
        GameContent content,
        HabitatLoadout habitatLoadout,
        SpriteAssetService assetService,
        bool devToolsEnabled,
        ChatInputBarState? petCommandState = null,
        RuntimeSupervisorStatus? runtimeSupervisorStatus = null,
        ActivitySummary? activitySummary = null,
        AutonomousBetaDecision? autonomousDecision = null,
        PromotionDecision? promotionDecision = null,
        IReadOnlyList<string>? activityRecentLines = null,
        EvidenceCollectionStatus? evidenceStatus = null,
        EvidenceSummary? evidenceSummary = null)
    {
        var toolId = string.IsNullOrWhiteSpace(state.ActiveTool.ToolId) ? "basket" : state.ActiveTool.ToolId;
        var showingBasket = string.Equals(toolId, "basket", StringComparison.OrdinalIgnoreCase);
        var showingSettings = string.Equals(toolId, "settings", StringComparison.OrdinalIgnoreCase);
        var showingActivity = string.Equals(toolId, "activity", StringComparison.OrdinalIgnoreCase);
        var showingDev = devToolsEnabled && string.Equals(toolId, "dev", StringComparison.OrdinalIgnoreCase);
        var showingPetCommand = string.Equals(toolId, "helpers", StringComparison.OrdinalIgnoreCase);
        var showingBenchmarks = string.Equals(toolId, "benchmarks", StringComparison.OrdinalIgnoreCase);
        var showingAutonomousScopes = string.Equals(toolId, "autonomous-scopes", StringComparison.OrdinalIgnoreCase);
        var showingEvidence = string.Equals(toolId, "evidence", StringComparison.OrdinalIgnoreCase);
        var localDocsEnabled = GetSettingBool(state, SettingKeys.LocalDocumentRetrievalEnabled);
        var advancedToolsVisible = GetSettingBool(state, ToolCatalog.AdvancedToolsVisibleSetting);
        var showingLocalDocs = localDocsEnabled && string.Equals(toolId, "local-docs", StringComparison.OrdinalIgnoreCase);
        var showingActionMenu = string.Equals(toolId, "actions", StringComparison.OrdinalIgnoreCase);
        var showingAction = toolId.StartsWith("action:", StringComparison.OrdinalIgnoreCase);
        var actionId = showingAction ? toolId["action:".Length..] : string.Empty;
        var actionDefinition = showingAction
            ? content.Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.OrdinalIgnoreCase))
            : null;

        Title = showingBasket ? "Wevito Tools" : showingDev ? "Wevito Dev Tools" : showingPetCommand ? "Wevito Chat" : showingBenchmarks ? "Wevito Benchmarks" : showingAutonomousScopes ? "Wevito Autonomy" : showingEvidence ? "Wevito Evidence" : showingLocalDocs ? "Wevito Local Docs" : showingActivity ? "Wevito Activity" : showingActionMenu ? "Wevito Actions" : showingAction ? $"Wevito {actionDefinition?.DisplayName ?? "Action"}" : "Wevito Settings";
        PopupTitle.Text = showingBasket ? "Tools" : showingDev ? "Dev Tools" : showingPetCommand ? "Chat" : showingBenchmarks ? "Benchmarks" : showingAutonomousScopes ? "Autonomy" : showingEvidence ? "Evidence" : showingLocalDocs ? "Local Docs" : showingActivity ? "Activity" : showingActionMenu ? "Actions" : showingAction ? (actionDefinition?.DisplayName ?? "Action") : "Settings";
        BasketPanel.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        BasketButtons.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        ActionMenuPanel.Visibility = showingActionMenu ? Visibility.Visible : Visibility.Collapsed;
        ActionPanel.Visibility = showingAction ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = showingSettings || showingActivity ? Visibility.Visible : Visibility.Collapsed;
        AutonomousScopesPanel.Visibility = showingAutonomousScopes ? Visibility.Visible : Visibility.Collapsed;
        EvidencePanel.Visibility = showingEvidence ? Visibility.Visible : Visibility.Collapsed;
        ApplyToolCatalogTabMetadata(localDocsEnabled, advancedToolsVisible);
        LocalDocsTabButton.Visibility = localDocsEnabled ? Visibility.Visible : Visibility.Collapsed;
        ActivityTabButton.Visibility = advancedToolsVisible ? Visibility.Visible : Visibility.Collapsed;
        BenchmarksTabButton.Visibility = advancedToolsVisible ? Visibility.Visible : Visibility.Collapsed;
        CreativeLabTabButton.Visibility = advancedToolsVisible ? Visibility.Visible : Visibility.Collapsed;
        LocalDocsPanel.Visibility = showingLocalDocs ? Visibility.Visible : Visibility.Collapsed;
        PetCommandPanel.Visibility = showingPetCommand ? Visibility.Visible : Visibility.Collapsed;
        BenchmarksPanel.Visibility = showingBenchmarks ? Visibility.Visible : Visibility.Collapsed;
        PetTaskReportOnlyBadge.Visibility = showingPetCommand ? Visibility.Visible : Visibility.Collapsed;
        DevPanel.Visibility = showingDev ? Visibility.Visible : Visibility.Collapsed;
        SettingsSaveButton.Visibility = showingSettings ? Visibility.Visible : Visibility.Collapsed;
        SettingsDevButton.Visibility = showingSettings && devToolsEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (showingBasket)
        {
            var existingMarks = _basketRows
                .Where(row => row.IsMarked)
                .Select(row => row.Id)
                .ToHashSet();
            _basketRows = state.BasketItems
                .Select(item => BasketRowItem.From(item, existingMarks.Contains(item.Id)))
                .ToList();
            foreach (var row in _basketRows)
            {
                row.PropertyChanged += BasketRow_OnPropertyChanged;
            }
            BasketGrid.ItemsSource = _basketRows;
            BasketSummaryText.Text = state.BasketItems.Count switch
            {
                0 => "Paste a valid clipboard link to start the bin.",
                1 => "1 saved link. Double-click a row or click the link text to open it.",
                _ => $"{state.BasketItems.Count} saved links. Double-click to open, or mark rows to delete together."
            };
            UpdateBasketDeleteButtonState();
        }
        else if (showingAction && actionDefinition is not null)
        {
            var optionItems = habitatLoadout.ActionOptions.TryGetValue(actionDefinition.Id, out var options)
                ? options
                : [];
            var buttonLabel = BuildActionOptionButtonLabel(state.ActivePets);
            _actionRows = optionItems
                .Select(item => ActionOptionRowItem.From(actionDefinition.Id, item, ResolveActionOptionPreview(assetService, actionDefinition.Id, item), buttonLabel))
                .ToList();
            ActionGrid.ItemsSource = _actionRows;
            ActionSummaryText.Text = FormatActionSummary(
                actionDefinition.DisplayName,
                actionDefinition.Description,
                _actionRows.Count,
                state.ActivePets);
        }
        else if (showingPetCommand)
        {
            RenderPetCommandPanel(petCommandState);
        }
        else if (showingBenchmarks)
        {
            RenderBenchmarksPanel(state);
        }
        else if (showingLocalDocs)
        {
            RenderLocalDocsPanel(state);
        }
        else if (showingEvidence)
        {
            RenderEvidencePanel(state, evidenceSummary);
        }

        _suppressSettingEvents = true;
        AdvancedToolsToggle.IsChecked = advancedToolsVisible;
        EvidenceDateRangeComboBox.SelectedValue = ResolveEvidenceDateRangeSetting(state);
        EvidenceMaxPacketsComboBox.SelectedValue = ResolveEvidenceMaxPacketsSetting(state).ToString();
        CompactHudCheckBox.IsChecked = GetSettingBool(state, "compact_hud");
        ShowPetNamesCheckBox.IsChecked = GetSettingBool(state, "show_pet_names");
        ShowStatusSummaryCheckBox.IsChecked = GetSettingBool(state, "show_status_summary", true);
        RuntimeQuietModeCheckBox.IsChecked = GetSettingBool(state, RuntimeSupervisorService.QuietModeSetting);
        RuntimeKillSwitchCheckBox.IsChecked = GetSettingBool(state, KillSwitchService.KillSwitchSetting);
        RuntimePetOnlyModeCheckBox.IsChecked = GetSettingBool(state, RuntimeSupervisorService.PetOnlyModeSetting);
        RuntimeBackgroundWorkAllowedCheckBox.IsChecked = GetSettingBool(state, RuntimeSupervisorService.BackgroundWorkAllowedSetting);
        SchedulerEnabledCheckBox.IsChecked = GetSettingBool(state, AutonomousTaskScheduler.SchedulerEnabledSetting);
        AutonomousBetaEnabledCheckBox.IsChecked = GetSettingBool(state, AutonomousOperationsConfig.EnabledSetting);
        AutonomousBetaEnabledCheckBox.IsEnabled = false;
        AutonomousBetaStatusText.Text = FormatAutonomousBetaStatus(state, autonomousDecision);
        PromotionSnapshotTableText.Text = FormatPromotionSnapshotTable(promotionDecision);
        AutonomousBetaTryButton.IsEnabled = PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(promotionDecision, state.SettingsSnapshot);
        AutonomousBetaTryHelpText.Text = FormatAutonomousBetaTryHelp(promotionDecision, state.SettingsSnapshot);
        SpriteRepairTriageScopeCheckBox.IsChecked = GetSettingBool(state, AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairTriageScopeId));
        SpriteRepairBatchProposalScopeCheckBox.IsChecked = GetSettingBool(state, AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId));
        AuditLedgerCleanupScopeCheckBox.IsChecked = GetSettingBool(state, AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.AuditLedgerCleanupScopeId));
        AutonomousSpriteRepairTriageScopeCheckBox.IsChecked = SpriteRepairTriageScopeCheckBox.IsChecked;
        AutonomousSpriteRepairBatchProposalScopeCheckBox.IsChecked = SpriteRepairBatchProposalScopeCheckBox.IsChecked;
        AutonomousAuditLedgerCleanupScopeCheckBox.IsChecked = AuditLedgerCleanupScopeCheckBox.IsChecked;
        AutonomousScopeStatusText.Text = FormatAutonomousScopeStatus(state.SettingsSnapshot);
        AutonomousScopePanelStatusText.Text = AutonomousScopeStatusText.Text;
        SpriteRepairScopeLastTickText.Text = FormatAutonomousScopeRecentLine(activityRecentLines, AutonomousScopeService.SpriteRepairTriageScopeId);
        SpriteRepairBatchProposalScopeLastTickText.Text = FormatAutonomousScopeRecentLine(activityRecentLines, AutonomousScopeService.SpriteRepairBatchProposalScopeId);
        AuditCleanupScopeLastTickText.Text = FormatAutonomousScopeRecentLine(activityRecentLines, AutonomousScopeService.AuditLedgerCleanupScopeId);
        AutonomousScopePreviewText.Text = _autonomousScopePreviewText;
        RuntimeNoFocusStealCheckBox.IsChecked = GetSettingBool(state, RuntimeSupervisorService.NoFocusStealSetting, true);
        RuntimeAutoQuietFullscreenCheckBox.IsChecked = GetSettingBool(state, RuntimeSupervisorService.AutoQuietFullscreenSetting, true);
        CoexistenceAppListCheckBox.IsChecked = GetSettingBool(state, CoexistenceTriggerService.AppListEnabledSetting, true);
        CoexistenceCpuCheckBox.IsChecked = GetSettingBool(state, CoexistenceTriggerService.CpuEnabledSetting, true);
        CoexistenceNetworkCheckBox.IsChecked = GetSettingBool(state, CoexistenceTriggerService.NetworkEnabledSetting, true);
        CoexistenceGameModeCheckBox.IsChecked = GetSettingBool(state, CoexistenceTriggerService.GameModeEnabledSetting, true);
        ResourcePriorityCheckBox.IsChecked = GetSettingBool(state, ProcessPriorityManagerService.EnabledSetting, true);
        DiskIoBudgetCheckBox.IsChecked = GetSettingBool(state, DiskIoBudgetService.EnabledSetting, true);
        CodexCompileCapComboBox.SelectedIndex = Math.Clamp(GetSettingInt(state, CodexCompileThrottleService.ActiveProcessorCapSetting, 2), 1, 4) - 1;
        DoNotDisturbScheduleCheckBox.IsChecked = GetSettingBool(state, DoNotDisturbScheduleService.EnabledSetting);
        NotificationDeferCheckBox.IsChecked = GetSettingBool(state, NotificationPolicyService.DeferDuringActivitySetting, true);
        AllowFocusTheftCheckBox.IsChecked = false;
        PetSoundsCheckBox.IsChecked = GetSettingBool(state, AudioOutputPolicyService.PetSoundEffectsEnabledSetting);
        PetCursorReactivityCheckBox.IsChecked = GetSettingBool(state, CursorReactivityService.EnabledSetting, true);
        TrayIconAnimationCheckBox.IsChecked = GetSettingBool(state, TrayIconDisciplineService.AnimationEnabledSetting);
        AnimationBlendingCheckBox.IsChecked = GetSettingBool(state, PetVisualPolishLogger.AnimationBlendingSetting, true);
        PositionInterpolationCheckBox.IsChecked = GetSettingBool(state, PetVisualPolishLogger.PositionInterpolationSetting, true);
        IdleMicroBehaviorsCheckBox.IsChecked = GetSettingBool(state, PetVisualPolishLogger.IdleMicroBehaviorsSetting, true);
        ParticleEffectsCheckBox.IsChecked = GetSettingBool(state, PetVisualPolishLogger.ParticleEffectsSetting, true);
        WindowShakeReactionCheckBox.IsChecked = GetSettingBool(state, PetVisualPolishLogger.WindowShakeReactionSetting, true);
        RuntimeSupervisorStatusText.Text = runtimeSupervisorStatus?.UserStatus ?? "Runtime supervisor: waiting for shell state.";
        var modelEnabled = GetSettingBool(state, "pet_model_adapter_enabled");
        var modelFirstCallApproved = GetSettingBool(state, "pet_model_first_call_approved");
        PetModelAdapterEnabledCheckBox.IsChecked = modelEnabled;
        PetModelCapabilityStatusText.Text = FormatPetModelCapabilityStatus(modelEnabled, modelFirstCallApproved);
        PetModelConsentText.Text = FormatPetModelConsentNotice();
        PetModelConsentStatusText.Text = modelFirstCallApproved
            ? "Consent acknowledged. Live calls still require a later explicit approval."
            : "Consent not acknowledged. No live model calls can run.";
        PetModelFirstCallConsentButton.IsEnabled = !modelFirstCallApproved;
        LocalAiRuntimeStatusText.Text = FormatLocalAiRuntimeStatus(state);
        ReasoningModelStatusText.Text = FormatReasoningModelStatus(state);
        LocalDocumentRetrievalEnabledCheckBox.IsChecked = localDocsEnabled;
        RenderToolRegistryList(state);
        var webSearchEnabled = GetSettingBool(state, WebResearchConnector.WebSearchEnabledSetting);
        var webBackend = state.SettingsSnapshot.TryGetValue(WebResearchConnector.WebBackendSetting, out var backend) ? backend : "offline";
        WebSearchEnabledCheckBox.IsChecked = webSearchEnabled;
        WebSearchStatusText.Text = $"Backend: {webBackend} · live fetches {(webSearchEnabled ? "allowed after approval" : "disabled")} · keys: Wevito/web-search/<backend>";
        ActivitySummaryText.Text = activitySummary is null
            ? "Activity ledger: waiting for shell state."
            : ActivitySummaryService.FormatOneLine(activitySummary);
        ActivityRecentText.Text = activityRecentLines is { Count: > 0 }
            ? string.Join(Environment.NewLine, activityRecentLines)
            : "Recent activity appears here after agents produce evidence packets.";
        SelfImprovementText.Text = FormatSelfImprovementPanel(activitySummary);
        EvidenceCollectionPanel.Visibility = evidenceStatus is { Active: true } or { HasManifest: true }
            ? Visibility.Visible
            : Visibility.Collapsed;
        EvidenceCollectionText.Text = FormatEvidenceCollectionPanel(evidenceStatus);
        GuardedMutationPilotTextBox.IsEnabled = false;
        GuardedMutationPilotButton.IsEnabled = false;
        GuardedMutationPilotButton.ToolTip = "guardedMutation pilotEnabled=false; proposals stay disabled until a later explicit approval.";
        if (showingDev)
        {
            RenderDevTools(state, content);
        }
        else
        {
            _lastRenderedDevPetId = null;
        }
        _suppressSettingEvents = false;
    }

    public void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    public void SetLocalDocsStatus(string status)
    {
        _localDocsStatusText = string.IsNullOrWhiteSpace(status) ? "Local Docs is waiting." : status;
        if (IsLoaded)
        {
            LocalDocsStatusText.Text = _localDocsStatusText;
        }
    }

    public void SetLocalDocsResults(IReadOnlyList<LocalDocumentSnippet> snippets)
    {
        _localDocumentRows = snippets.Select(LocalDocumentResultRowItem.From).ToList();
        if (IsLoaded)
        {
            LocalDocsResultsGrid.ItemsSource = _localDocumentRows;
        }
    }

    public async Task<bool> TryInvokeOverlayClickAsync(PointInt screenPosition)
    {
        if (Visibility != Visibility.Visible)
        {
            return false;
        }

        UpdateLayout();
        var localPoint = PointFromScreen(new Point(screenPosition.X, screenPosition.Y));

        if (await TryInvokeButtonAsync(CloseButton, localPoint, CloseRequested))
        {
            return true;
        }

        if (BasketPanel.Visibility == Visibility.Visible)
        {
            if (await TryInvokeButtonAsync(PasteButton, localPoint, PasteRequested))
            {
                return true;
            }

            if (await TryDeleteMarkedAsync(localPoint))
            {
                return true;
            }

            if (await TryToggleBasketMarkAsync(localPoint))
            {
                return true;
            }

            if (await TryOpenBasketRowAsync(localPoint))
            {
                return true;
            }
        }
        else if (ActionPanel.Visibility == Visibility.Visible)
        {
            if (await TryInvokeActionOptionAsync(localPoint))
            {
                return true;
            }
        }
        else if (ActionMenuPanel.Visibility == Visibility.Visible)
        {
            if (await TryInvokeActionMenuAsync(localPoint))
            {
                return true;
            }
        }
        else if (SettingsPanel.Visibility == Visibility.Visible)
        {
            if (TryToggleCheckBox(CompactHudCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(ShowPetNamesCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(ShowStatusSummaryCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(RuntimeKillSwitchCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(RuntimeQuietModeCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(RuntimePetOnlyModeCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(RuntimeBackgroundWorkAllowedCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(SchedulerEnabledCheckBox, localPoint)) { return true; }
            if (await TryInvokeButtonAsync(AutonomousBetaTryButton, localPoint, RequestAutonomousBetaConsentAsync)) { return true; }
            if (TryToggleCheckBox(SpriteRepairTriageScopeCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(SpriteRepairBatchProposalScopeCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(AuditLedgerCleanupScopeCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(RuntimeNoFocusStealCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(RuntimeAutoQuietFullscreenCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(CoexistenceAppListCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(CoexistenceCpuCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(CoexistenceNetworkCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(CoexistenceGameModeCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(ResourcePriorityCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(DiskIoBudgetCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(DoNotDisturbScheduleCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(NotificationDeferCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(PetSoundsCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(PetCursorReactivityCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(TrayIconAnimationCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(AnimationBlendingCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(PositionInterpolationCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(IdleMicroBehaviorsCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(ParticleEffectsCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(WindowShakeReactionCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(PetModelAdapterEnabledCheckBox, localPoint)) { return true; }
            if (TryToggleCheckBox(WebSearchEnabledCheckBox, localPoint)) { return true; }
            if (await TryInvokeButtonAsync(PullDefaultModelButton, localPoint, () =>
                {
                    PullDefaultModel();
                    return Task.CompletedTask;
                }))
            {
                return true;
            }
            if (await TryInvokeButtonAsync(PetModelFirstCallConsentButton, localPoint, () =>
                {
                    PublishSetting("pet_model_first_call_approved", true);
                    return Task.CompletedTask;
                }))
            {
                return true;
            }
            if (await TryInvokeButtonAsync(SettingsSaveButton, localPoint, SaveRequested)) { return true; }
            if (await TryInvokeButtonAsync(SettingsDevButton, localPoint, OpenDevRequested)) { return true; }
        }
        else if (PetCommandPanel.Visibility == Visibility.Visible)
        {
            if (await TryInvokeButtonAsync(PetCommandSubmitButton, localPoint, () => PetCommandSubmitted?.Invoke(PetCommandTextBox.Text ?? "") ?? Task.CompletedTask)) { return true; }
            if (await TryInvokeButtonAsync(PetTaskApproveButton, localPoint, () => PublishPetTaskStatusChangeAsync(TaskCardStatus.Approved))) { return true; }
            if (await TryInvokeButtonAsync(PetTaskPreviewButton, localPoint, PublishPetTaskPreviewAsync)) { return true; }
            if (await TryInvokeButtonAsync(PetTaskCancelButton, localPoint, () => PublishPetTaskStatusChangeAsync(TaskCardStatus.Cancelled))) { return true; }
            if (await TryInvokeButtonAsync(PetTaskExecuteButton, localPoint, PublishPetTaskExecutionAsync)) { return true; }
            if (await TryInvokeButtonAsync(PetTaskOpenReportButton, localPoint, OpenSelectedPetTaskReportAsync)) { return true; }
            if (await TryInvokeButtonAsync(PetTaskCopyPathButton, localPoint, CopySelectedPetTaskPathAsync)) { return true; }
            if (await TryInvokeButtonAsync(PetTaskOpenFolderButton, localPoint, OpenSelectedPetTaskFolderAsync)) { return true; }
        }
        else if (DevPanel.Visibility == Visibility.Visible)
        {
            if (await TryInvokeButtonAsync(AddPetButton, localPoint, () => PublishDevCommandAsync(BuildAppearanceCommand(DevToolCommandKind.AddPet)))) { return true; }
            if (await TryInvokeButtonAsync(RemovePetButton, localPoint, () => PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.RemovePet, GetSelectedPetId())))) { return true; }
            if (await TryInvokeButtonAsync(ClearPetsButton, localPoint, () => PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.RemoveAllPets)))) { return true; }
            if (await TryInvokeButtonAsync(ApplyAppearanceButton, localPoint, () => PublishDevCommandAsync(BuildAppearanceCommand(DevToolCommandKind.ApplyAppearance)))) { return true; }
            if (await TryInvokeButtonAsync(ApplyEnvironmentButton, localPoint, () => PublishDevCommandAsync(BuildEnvironmentCommand()))) { return true; }
            if (await TryInvokeButtonAsync(ApplyAnimationButton, localPoint, () => PublishDevCommandAsync(BuildAnimationCommand(DevToolCommandKind.ApplyAnimation)))) { return true; }
            if (await TryInvokeButtonAsync(ClearAnimationButton, localPoint, () => PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.ClearAnimation, GetSelectedPetId())))) { return true; }
            if (await TryInvokeButtonAsync(SpawnColorSetButton, localPoint, () => PublishDevCommandAsync(BuildAppearanceCommand(DevToolCommandKind.SpawnColorSet)))) { return true; }
            if (await TryInvokeButtonAsync(ApplyConditionButton, localPoint, () => PublishDevCommandAsync(BuildConditionCommand(DevToolCommandKind.SetCondition)))) { return true; }
            if (await TryInvokeButtonAsync(ClearConditionButton, localPoint, () => PublishDevCommandAsync(BuildConditionCommand(DevToolCommandKind.ClearCondition)))) { return true; }
            if (await TryInvokeButtonAsync(ApplyVitalsButton, localPoint, () => PublishDevCommandAsync(BuildVitalsCommand()))) { return true; }
            if (await TryInvokeButtonAsync(HungryPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("hungry")))) { return true; }
            if (await TryInvokeButtonAsync(ThirstyPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("thirsty")))) { return true; }
            if (await TryInvokeButtonAsync(TiredPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("tired")))) { return true; }
            if (await TryInvokeButtonAsync(DirtyPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("dirty")))) { return true; }
            if (await TryInvokeButtonAsync(LonelyPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("lonely")))) { return true; }
            if (await TryInvokeButtonAsync(SickPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("sick")))) { return true; }
            if (await TryInvokeButtonAsync(HealthyPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("healthy")))) { return true; }
            if (await TryInvokeButtonAsync(ComfortedPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("comforted")))) { return true; }
            if (await TryInvokeButtonAsync(RecallPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("recall")))) { return true; }
            if (await TryInvokeButtonAsync(ObesePresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("obese")))) { return true; }
            if (await TryInvokeButtonAsync(MalnourishedPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("malnourished")))) { return true; }
            if (await TryInvokeButtonAsync(AnxiousPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("anxious")))) { return true; }
            if (await TryInvokeButtonAsync(DepressedPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("depressed")))) { return true; }
            if (await TryInvokeButtonAsync(InjuredPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("injured")))) { return true; }
            if (await TryInvokeButtonAsync(ElderPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("elder")))) { return true; }
            if (await TryInvokeButtonAsync(FoodiePresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("foodie")))) { return true; }
            if (await TryInvokeButtonAsync(CuddlyPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("cuddly")))) { return true; }
            if (await TryInvokeButtonAsync(NeatPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("neat")))) { return true; }
            if (await TryInvokeButtonAsync(PlayfulPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("playful")))) { return true; }
            if (await TryInvokeButtonAsync(StubbornPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("stubborn")))) { return true; }
            if (await TryInvokeButtonAsync(ResilientPresetButton, localPoint, () => PublishDevCommandAsync(BuildPresetCommand("resilient")))) { return true; }
        }

        return false;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_closingSilently)
        {
            Hide();
        }

        base.OnClosed(e);
    }

    private void RenderPetCommandPanel(ChatInputBarState? state)
    {
        var helpers = state?.ActiveHelpers ?? [];
        PetHelperOneText.Text = FormatHelper(helpers, 0);
        PetHelperTwoText.Text = FormatHelper(helpers, 1);
        PetHelperThreeText.Text = FormatHelper(helpers, 2);
        PetWellbeingSnapshotText.Text = FormatWellbeingSnapshots(state?.WellbeingSnapshots);

        if (!PetCommandTextBox.IsKeyboardFocusWithin &&
            state is not null &&
            !string.IsNullOrWhiteSpace(state.InputText) &&
            !string.Equals(PetCommandTextBox.Text, state.InputText, StringComparison.Ordinal))
        {
            PetCommandTextBox.Text = state.InputText;
        }

        PetCommandStatusText.Text = string.IsNullOrWhiteSpace(state?.StatusMessage)
            ? "Ready for a helper pet task."
            : state.StatusMessage;
        PetCommandQueueText.Text = FormatTaskQueue(state?.QueuedTaskCards);
        RenderPetTaskQueue(state?.QueuedTaskCards, state?.LastTaskCard?.Id);

        if (state?.LastTaskCard is not { } card)
        {
            PetCommandResultPanel.Visibility = Visibility.Collapsed;
            PetTaskNextActionText.Text = "Next: prepare a card, then preview its report before anything can run.";
            PetTaskResultPathText.Text = "Report: path appears here after preview.";
            return;
        }

        PetCommandResultPanel.Visibility = Visibility.Visible;
        var helper = FormatQueuePetName(card);
        var reportPath = string.IsNullOrWhiteSpace(card.AuditLogPath) ? "not written yet" : card.AuditLogPath;
        PetCommandAssignedText.Text = $"Helper: {helper}";
        PetCommandTaskKindText.Text = $"Family: {card.ToolFamily} | status: {card.Status} | task: {card.Intent.TaskKind}";
        PetCommandPolicyText.Text = state.LastPolicyDecision is null
            ? $"Report: {reportPath} | policy: not evaluated"
            : $"Report: {reportPath} | policy: {state.LastPolicyDecision.Status} ({state.LastPolicyDecision.RiskLevel})";
        PetCommandTimelineText.Text = $"Latest event: {card.Timeline?.LastOrDefault() ?? "draft_created"}";
        PetTaskNextActionText.Text = FormatNextAction(card);
        PetTaskResultPathText.Text = FormatResultPath(card);
    }

    private void RenderBenchmarksPanel(CompanionState state)
    {
        var score = state.SettingsSnapshot.TryGetValue("benchmark_latest_score", out var latestScore) && !string.IsNullOrWhiteSpace(latestScore)
            ? latestScore
            : "--";
        BenchmarkScoreText.Text = $"Latest composite score: {score}";
        BenchmarkCadenceText.Text = "Cadence: safety + perf per phase, capability daily, on-demand when requested.";
        BenchmarkAxesText.Text = "v1 axes: chat, tool-use, retrieval, safety, perf.";
        BenchmarkStatusText.Text = "Approved case folder is immutable once committed. Empty approved cases report NoBaseline, not pass.";
    }

    private static string FormatTaskQueue(IReadOnlyList<TaskCard>? cards)
    {
        if (cards is null || cards.Count == 0)
        {
            return "Queue: no saved cards yet.";
        }

        var draftCount = cards.Count(card => card.Status == TaskCardStatus.Draft);
        var approvalCount = cards.Count(card => card.Status == TaskCardStatus.WaitingForApproval);
        var blockedCount = cards.Count(card => card.Status == TaskCardStatus.Blocked);
        var runnableCount = cards.Count(CanRunReviewedTask);
        var latest = cards
            .Take(3)
            .Select(card =>
            {
                var petName = FormatQueuePetName(card);
                var report = string.IsNullOrWhiteSpace(card.AuditLogPath) ? "no report" : "report ready";
                return $"{petName} | {card.ToolFamily} | {card.Status} | {report}";
            });

        return $"Queue: {cards.Count} saved | draft {draftCount} | approval {approvalCount} | runnable {runnableCount} | blocked {blockedCount}\nLatest: {string.Join(" / ", latest)}";
    }

    private static string FormatWellbeingSnapshots(IReadOnlyList<PetWellbeingSnapshot>? snapshots)
    {
        if (snapshots is null || snapshots.Count == 0)
        {
            return "Wellbeing: no pet snapshots available yet.";
        }

        var lines = snapshots
            .Take(3)
            .Select(snapshot =>
            {
                var traits = snapshot.PersonalityDescriptors is { Count: > 0 }
                    ? $" | {string.Join(", ", snapshot.PersonalityDescriptors.Take(2))}"
                    : "";
                var conditions = snapshot.ActiveConditionIds is { Count: > 0 }
                    ? $" | conditions: {string.Join(", ", snapshot.ActiveConditionIds.Take(2))}"
                    : "";
                return $"{snapshot.PetName}: {snapshot.Urgency} / {snapshot.DominantDrive} / {snapshot.DominantEmotion}{traits}{conditions}";
            })
            .ToList();

        if (snapshots.Count > lines.Count)
        {
            lines.Add($"+{snapshots.Count - lines.Count} more pet snapshot(s)");
        }

        return $"Wellbeing: {string.Join(" / ", lines)}";
    }

    private void RenderPetTaskQueue(IReadOnlyList<TaskCard>? cards, Guid? selectedCardId)
    {
        _taskQueueRows = (cards ?? [])
            .Select(card => new PetTaskQueueRowItem(
                card.Id,
                FormatQueueCardLabel(card),
                card.ToolFamily,
                card.Status,
                card.Intent.RawText,
                FormatNextAction(card),
                CanRunReviewedTask(card),
                card.AuditLogPath))
            .ToList();

        _suppressTaskQueueSelection = true;
        PetTaskQueueComboBox.ItemsSource = _taskQueueRows;
        PetTaskQueueComboBox.SelectedValue = selectedCardId is not null && _taskQueueRows.Any(row => row.Id == selectedCardId.Value)
            ? selectedCardId.Value
            : _taskQueueRows.FirstOrDefault()?.Id;
        _suppressTaskQueueSelection = false;
        UpdatePetTaskButtons();
    }

    private static string FormatQueuePetName(TaskCard card)
    {
        return string.IsNullOrWhiteSpace(card.AssignedPetNameSnapshot)
            ? "Unassigned"
            : card.AssignedPetNameSnapshot;
    }

    private static string FormatQueueCardLabel(TaskCard card)
    {
        var report = string.IsNullOrWhiteSpace(card.AuditLogPath) ? "report: pending" : "report: ready";
        return $"{FormatQueuePetName(card)} | {card.ToolFamily} | {card.Status} | {report} | {ShortNextAction(card)}";
    }

    private static string ShortNextAction(TaskCard card)
    {
        return card.Status switch
        {
            TaskCardStatus.Draft => "next: preview",
            TaskCardStatus.WaitingForApproval => "next: approve",
            TaskCardStatus.Approved => "next: preview",
            TaskCardStatus.Reviewing when CanRunReviewedTask(card) => "next: run approved",
            TaskCardStatus.Reviewing => "next: open report",
            TaskCardStatus.Running => "next: wait",
            TaskCardStatus.Done => "next: review result",
            TaskCardStatus.Blocked => "next: revise",
            TaskCardStatus.Cancelled => "next: new task",
            TaskCardStatus.Failed => "next: inspect failure",
            _ => $"next: {card.Status}"
        };
    }

    private static string FormatHelper(IReadOnlyList<AgentSlotProfile> helpers, int index)
    {
        if (index >= helpers.Count)
        {
            return $"{index + 1}. Waiting for pet";
        }

        var helper = helpers[index];
        var species = helper.PreferenceSnapshot is not null && helper.PreferenceSnapshot.TryGetValue("species", out var speciesValue)
            ? speciesValue
            : "pet";
        var state = FormatHelperState(helper.Availability);
        var task = helper.CurrentTaskCardId is Guid taskId
            ? $"task {taskId.ToString()[..8]}"
            : "no task";
        var tool = helper.PreferenceSnapshot is not null && helper.PreferenceSnapshot.TryGetValue("active_tool_family", out var toolValue) && !string.IsNullOrWhiteSpace(toolValue)
            ? toolValue
            : "no active tool";
        return $"{helper.PetNameSnapshot} ({species})\nAgent slot {helper.SlotIndex + 1} | {state}\n{tool} | {task}";
    }

    private static string FormatSelfImprovementPanel(ActivitySummary? summary)
    {
        if (summary is null)
        {
            return "Self-improvement reports appear here after the daily/weekly loop runs.";
        }

        var reportCount = summary.Buckets
            .FirstOrDefault(bucket => string.Equals(bucket.PacketKind, AuditLedgerService.SelfImprovementReportPacketKind, StringComparison.OrdinalIgnoreCase))
            ?.Count ?? 0;
        var proposalCount = summary.Buckets
            .FirstOrDefault(bucket => string.Equals(bucket.PacketKind, AuditLedgerService.RollbackProposalPacketKind, StringComparison.OrdinalIgnoreCase))
            ?.Count ?? 0;

        if (proposalCount > 0)
        {
            return $"Review needed: {proposalCount} rollback proposal(s). Reports: {reportCount}. Rollbacks are draft-only.";
        }

        if (reportCount > 0)
        {
            return $"Self-improvement reports: {reportCount}. No rollback proposals in this activity window.";
        }

        return "No self-improvement reports in this activity window yet.";
    }

    internal static string FormatEvidenceCollectionPanel(EvidenceCollectionStatus? status)
    {
        if (status is null || !status.HasManifest)
        {
            return "Soak window: not started";
        }

        var header = status.Active
            ? $"Soak window: Day {status.DayN} of {status.DayMax}"
            : $"Soak window: {status.LastReadinessLabel}";
        var lines = new List<string>
        {
            header,
            $"Readiness: {status.LastReadinessLabel}",
            $"Today: rows={status.RowsToday}, flagged={status.FlaggedRowsToday}, heartbeats={status.HeartbeatCountToday}, focus_delta={status.FocusStealDeltaToday}, budget_exceeded={status.BudgetExceededToday}"
        };

        foreach (var day in status.Days.Take(7))
        {
            lines.Add($"{day.DateUtc}: heartbeats={day.HeartbeatCount}, flagged={day.FlaggedRows}, focus_delta={day.FocusStealDelta}, budget_exceeded={day.BudgetExceeded}, report={(day.LastSelfImprovementReportAtUtc?.ToString("O") ?? "none")}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatNextAction(TaskCard card)
    {
        return card.Status switch
        {
            TaskCardStatus.Draft => "Next: preview this task. Preview is report-only and must not mutate files.",
            TaskCardStatus.WaitingForApproval => "Next: approve only if you want the queued safe action to continue.",
            TaskCardStatus.Approved => "Next: preview the approved card. RUN APPROVED stays disabled until a reviewed report exists.",
            TaskCardStatus.Running => "Next: wait for the adapter to finish and write its artifact report.",
            TaskCardStatus.Reviewing when string.Equals(card.ToolFamily, "translateText", StringComparison.OrdinalIgnoreCase) => "Next: open the preview report, then RUN only if you approve sending this text to the configured provider.",
            TaskCardStatus.Reviewing when string.Equals(card.ToolFamily, "screenCapture", StringComparison.OrdinalIgnoreCase) && CanRunReviewedTask(card) => FormatScreenCaptureRunMessage(card.Intent.RawText),
            TaskCardStatus.Reviewing when string.Equals(card.ToolFamily, "audioAssist", StringComparison.OrdinalIgnoreCase) && CanRunReviewedTask(card) => "Next: open the preview report, then RUN only if you approve changing normal Windows volume/mute state.",
            TaskCardStatus.Reviewing when string.Equals(card.ToolFamily, "audioAssist", StringComparison.OrdinalIgnoreCase) && IsAudioBoostHandoff(card.Intent.RawText) => "Next: open the audio boost setup guide. Wevito will not install software or edit enhancer/APO configs.",
            TaskCardStatus.Reviewing when string.Equals(card.ToolFamily, "audioAssist", StringComparison.OrdinalIgnoreCase) => "Next: open the audio status report. Execution is only for set volume, mute, or unmute requests.",
            TaskCardStatus.Reviewing => "Next: open the artifact/report. Most reviewed cards are report-only and cannot run.",
            TaskCardStatus.Blocked => "Next: revise the task or inspect the blocker report.",
            TaskCardStatus.Cancelled => "Next: prepare a new task if you still want help.",
            TaskCardStatus.Done => "Next: review the result artifact before starting another task.",
            TaskCardStatus.Failed => "Next: inspect the failure artifact and revise the task.",
            _ => $"Next: review status {card.Status} before continuing."
        };
    }

    private static string FormatHelperState(AgentSlotAvailability availability)
    {
        return availability switch
        {
            AgentSlotAvailability.Drafting => "drafting",
            AgentSlotAvailability.WaitingForApproval => "waiting",
            AgentSlotAvailability.Running => "running",
            AgentSlotAvailability.Reviewing => "reviewing",
            AgentSlotAvailability.Blocked => "blocked",
            AgentSlotAvailability.Done => "done",
            AgentSlotAvailability.Failed => "failed",
            _ => "available"
        };
    }

    private static string FormatScreenCaptureRunMessage(string rawText)
    {
        if (ScreenCaptureTargetResolver.IsRecordingRequest(rawText))
        {
            return "Next: open the preview report, then RUN only if you want a 5-10 second no-audio Wevito-window proof clip.";
        }

        var target = ScreenCaptureTargetResolver.ResolveTarget(rawText);
        return target.TargetKind switch
        {
            CaptureTargetKind.SelectedRegion => "Next: open the preview report, then RUN only if you want to drag-select a screenshot region.",
            CaptureTargetKind.LastRegion => "Next: open the preview report, then RUN only if you want to recapture the saved last region.",
            _ => "Next: open the preview report, then RUN only if you want a Wevito-window screenshot artifact."
        };
    }

    private static bool IsAudioBoostHandoff(string rawText)
    {
        return rawText.Contains("boost", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("equalizer", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("fxsound", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("apo", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("louder than", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("over 100", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatResultPath(TaskCard card)
    {
        if (!string.IsNullOrWhiteSpace(card.AuditLogPath))
        {
            return $"Report: {card.AuditLogPath}";
        }

        if (!string.IsNullOrWhiteSpace(card.ResultSummary))
        {
            return $"Result: {card.ResultSummary}";
        }

        return card.Status switch
        {
            TaskCardStatus.Draft => "Result: no report yet. PREVIEW will create a report-only artifact.",
            TaskCardStatus.Blocked => "Result: blocked before a report could be written.",
            TaskCardStatus.Cancelled => "Result: task cancelled.",
            _ => "Result: waiting for a report-only artifact."
        };
    }

    private void RenderDevTools(CompanionState state, GameContent content)
    {
        var selectedPetId = GetSelectedPetId(state);
        if (selectedPetId is null && state.ActivePets.Count > 0)
        {
            selectedPetId = state.ActivePets[0].Id;
        }

        var petOptions = state.ActivePets
            .Select((pet, index) => new DevPetOption(pet.Id, $"{index + 1}. {pet.Name} [{pet.SpeciesId}/{pet.AgeStage}/{pet.ColorVariant}]"))
            .ToList();

        SelectedPetComboBox.ItemsSource = petOptions;
        _lastRenderedPetCount = petOptions.Count;

        if (_lastRenderedSpeciesCount != content.Species.Count)
        {
            SpeciesComboBox.ItemsSource = content.Species.Select(species => species.Id).ToList();
            AgeComboBox.ItemsSource = Enum.GetValues<PetAgeStage>();
            GenderComboBox.ItemsSource = Enum.GetValues<PetGender>();
            AnimationComboBox.ItemsSource = Enum.GetValues<PetAnimationState>();
            AnimationDurationComboBox.ItemsSource = new[] { 1.5, 2.5, 4.0, 8.0, 20.0, 60.0 };
            EnvironmentComboBox.ItemsSource = content.Environments
                .Select(environment => new DevEnvironmentOption(environment.Id, environment.DisplayName))
                .ToList();
            EnvironmentComboBox.DisplayMemberPath = nameof(DevEnvironmentOption.Label);
            EnvironmentComboBox.SelectedValuePath = nameof(DevEnvironmentOption.Id);
            ConditionComboBox.ItemsSource = content.Conditions
                .OrderBy(condition => condition.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(condition => new DevConditionOption(condition.Id, condition.DisplayName))
                .ToList();
            ConditionComboBox.DisplayMemberPath = nameof(DevConditionOption.Label);
            ConditionComboBox.SelectedValuePath = nameof(DevConditionOption.Id);
            ConditionSeverityComboBox.ItemsSource = new[] { 1, 2, 3 };
            _lastRenderedSpeciesCount = content.Species.Count;
        }
        SpeciesComboBox.Tag = content;

        SelectedPetComboBox.SelectedValue = selectedPetId;

        if (_lastRenderedDevPetId != selectedPetId)
        {
            var selectedPet = selectedPetId is null
                ? null
                : state.ActivePets.FirstOrDefault(pet => pet.Id == selectedPetId.Value);
            var speciesId = selectedPet?.SpeciesId ?? content.Species.First().Id;
            var species = content.Species.First(species => string.Equals(species.Id, speciesId, StringComparison.OrdinalIgnoreCase));
            var colors = species.SupportedColors?.ToList() ?? ["blue"];
            SpeciesComboBox.SelectedItem = species.Id;
            ColorComboBox.ItemsSource = colors;
            ColorComboBox.SelectedItem = selectedPet?.ColorVariant ?? colors.First();
            AgeComboBox.SelectedItem = selectedPet?.AgeStage ?? species.SupportedAgeStages?.FirstOrDefault() ?? PetAgeStage.Adult;
            GenderComboBox.SelectedItem = selectedPet?.Gender ?? species.SupportedGenders?.FirstOrDefault() ?? PetGender.Female;
            EnvironmentComboBox.SelectedValue = state.ActiveEnvironmentId;
            AnimationComboBox.SelectedItem = selectedPet?.CurrentAnimationState ?? PetAnimationState.Idle;
            AnimationDurationComboBox.SelectedItem = 8.0;
            HungerSlider.Value = selectedPet?.Hunger ?? 84;
            ThirstSlider.Value = selectedPet?.Thirst ?? 82;
            EnergySlider.Value = selectedPet?.Energy ?? 76;
            CleanlinessSlider.Value = selectedPet?.Cleanliness ?? 78;
            AffectionSlider.Value = selectedPet?.Affection ?? 72;
            ComfortSlider.Value = selectedPet?.Comfort ?? 74;
            HealthSlider.Value = selectedPet?.Health ?? 88;
            FitnessSlider.Value = selectedPet?.Fitness ?? 68;
            BiologicalAgeSlider.Value = selectedPet?.BiologicalAgeMinutes ?? 0;
            var firstCondition = selectedPet?.ActiveConditions?.FirstOrDefault(condition => !condition.IsInnate);
            if (firstCondition is not null)
            {
                ConditionComboBox.SelectedValue = firstCondition.Id;
                ConditionSeverityComboBox.SelectedItem = firstCondition.Severity;
            }
            else
            {
                ConditionComboBox.SelectedIndex = ConditionComboBox.Items.Count > 0 ? 0 : -1;
                ConditionSeverityComboBox.SelectedItem = 1;
            }
            _lastRenderedDevPetId = selectedPetId;
        }

        var currentPet = selectedPetId is null
            ? null
            : state.ActivePets.FirstOrDefault(pet => pet.Id == selectedPetId.Value);
        PersonalitySummaryText.Text = BuildPersonalitySummary(currentPet);
        HabitSummaryText.Text = BuildHabitSummary(currentPet);
        AgingSummaryText.Text = BuildAgingSummary(currentPet);
        ConditionSummaryText.Text = BuildConditionSummary(currentPet, content);
        UpdateSliderLabels();
        RemovePetButton.IsEnabled = selectedPetId is not null;
        ClearPetsButton.IsEnabled = state.ActivePets.Count > 0;
        ApplyAnimationButton.IsEnabled = selectedPetId is not null;
        ClearAnimationButton.IsEnabled = selectedPetId is not null;
        SpawnColorSetButton.IsEnabled = true;
        ApplyEnvironmentButton.IsEnabled = EnvironmentComboBox.Items.Count > 0;
        ApplyConditionButton.IsEnabled = selectedPetId is not null;
        ClearConditionButton.IsEnabled = selectedPetId is not null;
        ApplyVitalsButton.IsEnabled = selectedPetId is not null;
    }

    private async void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CloseRequested is not null)
        {
            await CloseRequested.Invoke();
        }
    }

    private async void ToolTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string toolId } && ToolTabRequested is not null)
        {
            await ToolTabRequested.Invoke(toolId);
        }
    }

    private void AdvancedToolsToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(ToolCatalog.AdvancedToolsVisibleSetting, (AdvancedToolsToggle.IsChecked == true).ToString());
    }

    private async void LocalDocsBuildButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LocalDocsBuildRequested is not null)
        {
            await LocalDocsBuildRequested.Invoke();
        }
    }

    private async void LocalDocsQueryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (LocalDocsQueryRequested is not null)
        {
            await LocalDocsQueryRequested.Invoke(LocalDocsQueryTextBox.Text ?? string.Empty);
        }
    }

    private void EvidenceFilterComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingEvents)
        {
            return;
        }

        PublishSetting("evidence_dashboard_date_range", EvidenceDateRangeComboBox.SelectedValue as string ?? "24h");
        PublishSetting("evidence_dashboard_max_packets", EvidenceMaxPacketsComboBox.SelectedValue as string ?? EvidenceSummaryService.DefaultMaxPackets.ToString());
    }

    private async void EvidenceExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (EvidenceSummaryExportRequested is not null)
        {
            await EvidenceSummaryExportRequested.Invoke();
        }
    }

    private async void RunFirstLaunchWizardButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RunFirstLaunchWizardRequested is not null)
        {
            await RunFirstLaunchWizardRequested.Invoke();
        }
    }

    private async void PasteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PasteRequested is not null)
        {
            await PasteRequested.Invoke();
        }
    }

    private async void SettingsSaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SaveRequested is not null)
        {
            await SaveRequested.Invoke();
        }
    }

    private async void SettingsDevButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OpenDevRequested is not null)
        {
            await OpenDevRequested.Invoke();
        }
    }

    private async void BasketLinkButton_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Guid id && OpenRequested is not null)
        {
            await OpenRequested.Invoke(id);
        }
    }

    private async void BasketGrid_OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (BasketGrid.SelectedItem is BasketRowItem row && OpenRequested is not null)
        {
            await OpenRequested.Invoke(row.Id);
        }
    }

    private void BasketGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateBasketDeleteButtonState();
    }

    private async void ActionOptionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string key)
        {
            if (TryParseActionOptionDragPayload(key, out var actionId, out var itemId) && ActionOptionRequested is not null)
            {
                await ActionOptionRequested.Invoke(actionId, itemId);
            }
        }
    }

    private void ActionGrid_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || ActionGrid.SelectedItem is not ActionOptionRowItem row)
        {
            return;
        }

        var data = new DataObject();
        data.SetData(ActionOptionDragFormat, row.Key);
        data.SetData(DataFormats.Text, $"{row.Label} - {row.ButtonLabel}");
        DragDrop.DoDragDrop(ActionGrid, data, DragDropEffects.Copy);
    }

    private async void ActionMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string actionId && ActionMenuRequested is not null)
        {
            await ActionMenuRequested.Invoke(actionId);
        }
    }

    private async void PetCommandSubmitButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PetCommandSubmitted is not null)
        {
            await PetCommandSubmitted.Invoke(PetCommandTextBox.Text ?? "");
        }
    }

    private void PetTaskQueueComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressTaskQueueSelection)
        {
            UpdatePetTaskButtons();
        }
    }

    private async void PetTaskApproveButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishPetTaskStatusChangeAsync(TaskCardStatus.Approved);
    }

    private async void PetTaskPreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishPetTaskPreviewAsync();
    }

    private async void PetTaskCancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishPetTaskStatusChangeAsync(TaskCardStatus.Cancelled);
    }

    private async void PetTaskExecuteButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishPetTaskExecutionAsync();
    }

    private async void PetTaskOpenReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        await OpenSelectedPetTaskReportAsync();
    }

    private async void PetTaskCopyPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        await CopySelectedPetTaskPathAsync();
    }

    private async void PetTaskOpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        await OpenSelectedPetTaskFolderAsync();
    }

    private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var ids = _basketRows
            .Where(row => row.IsMarked)
            .Select(row => row.Id)
            .ToList();
        if (ids.Count == 0 && BasketGrid.SelectedItem is BasketRowItem selectedRow)
        {
            ids.Add(selectedRow.Id);
        }
        if (ids.Count == 0 && _basketRows.Count == 1)
        {
            ids.Add(_basketRows[0].Id);
        }
        if (ids.Count > 0 && DeleteRequested is not null)
        {
            await DeleteRequested.Invoke(ids);
        }
    }

    private void BasketRow_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(BasketRowItem.IsMarked), StringComparison.Ordinal))
        {
            UpdateBasketDeleteButtonState();
        }
    }

    private async void ToolPopupWindow_OnDrop(object sender, DragEventArgs e)
    {
        if (LinksDropped is null)
        {
            return;
        }

        var urls = DropPayloadReader.ExtractUrls(e.Data);
        if (urls.Count > 0)
        {
            await LinksDropped.Invoke(urls);
        }
    }

    private void CompactHudCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("compact_hud", CompactHudCheckBox.IsChecked == true);
    }

    private void ShowPetNamesCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("show_pet_names", ShowPetNamesCheckBox.IsChecked == true);
    }

    private void ShowStatusSummaryCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("show_status_summary", ShowStatusSummaryCheckBox.IsChecked != false);
    }

    private void RuntimeQuietModeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(RuntimeSupervisorService.QuietModeSetting, RuntimeQuietModeCheckBox.IsChecked == true);
    }

    private void RuntimePetOnlyModeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(RuntimeSupervisorService.PetOnlyModeSetting, RuntimePetOnlyModeCheckBox.IsChecked == true);
    }

    private void RuntimeBackgroundWorkAllowedCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(RuntimeSupervisorService.BackgroundWorkAllowedSetting, RuntimeBackgroundWorkAllowedCheckBox.IsChecked == true);
    }

    private void SchedulerEnabledCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(AutonomousTaskScheduler.SchedulerEnabledSetting, SchedulerEnabledCheckBox.IsChecked == true);
    }

    private void AutonomousBetaEnabledCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressSettingEvents || AutonomousBetaEnabledCheckBox.IsChecked != false)
        {
            return;
        }

        PublishSetting(AutonomousOperationsConfig.EnabledSetting, false);
    }

    private async void AutonomousBetaTryButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RequestAutonomousBetaConsentAsync();
    }

    private void RuntimeKillSwitchCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(KillSwitchService.KillSwitchSetting, RuntimeKillSwitchCheckBox.IsChecked == true);
    }

    private void RuntimeNoFocusStealCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(RuntimeSupervisorService.NoFocusStealSetting, RuntimeNoFocusStealCheckBox.IsChecked != false);
    }

    private void RuntimeAutoQuietFullscreenCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(RuntimeSupervisorService.AutoQuietFullscreenSetting, RuntimeAutoQuietFullscreenCheckBox.IsChecked != false);
    }

    private void CoexistenceAppListCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(CoexistenceTriggerService.AppListEnabledSetting, CoexistenceAppListCheckBox.IsChecked != false);
    }

    private void CoexistenceCpuCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(CoexistenceTriggerService.CpuEnabledSetting, CoexistenceCpuCheckBox.IsChecked != false);
    }

    private void CoexistenceNetworkCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(CoexistenceTriggerService.NetworkEnabledSetting, CoexistenceNetworkCheckBox.IsChecked != false);
    }

    private void CoexistenceGameModeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(CoexistenceTriggerService.GameModeEnabledSetting, CoexistenceGameModeCheckBox.IsChecked != false);
    }

    private void ResourcePriorityCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(ProcessPriorityManagerService.EnabledSetting, ResourcePriorityCheckBox.IsChecked != false);
    }

    private void DiskIoBudgetCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(DiskIoBudgetService.EnabledSetting, DiskIoBudgetCheckBox.IsChecked != false);
    }

    private void CodexCompileCapComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CodexCompileCapComboBox.SelectedItem is ComboBoxItem { Content: string value })
        {
            PublishSetting(CodexCompileThrottleService.ActiveProcessorCapSetting, value);
        }
    }

    private void DoNotDisturbScheduleCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(DoNotDisturbScheduleService.EnabledSetting, DoNotDisturbScheduleCheckBox.IsChecked == true);
    }

    private void NotificationDeferCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(NotificationPolicyService.DeferDuringActivitySetting, NotificationDeferCheckBox.IsChecked == true);
    }

    private void PetSoundsCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(AudioOutputPolicyService.PetSoundEffectsEnabledSetting, PetSoundsCheckBox.IsChecked == true);
    }

    private void PetCursorReactivityCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(CursorReactivityService.EnabledSetting, PetCursorReactivityCheckBox.IsChecked == true);
    }

    private void TrayIconAnimationCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(TrayIconDisciplineService.AnimationEnabledSetting, TrayIconAnimationCheckBox.IsChecked == true);
    }

    private void AnimationBlendingCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(PetVisualPolishLogger.AnimationBlendingSetting, AnimationBlendingCheckBox.IsChecked == true);
    }

    private void PositionInterpolationCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(PetVisualPolishLogger.PositionInterpolationSetting, PositionInterpolationCheckBox.IsChecked == true);
    }

    private void IdleMicroBehaviorsCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(PetVisualPolishLogger.IdleMicroBehaviorsSetting, IdleMicroBehaviorsCheckBox.IsChecked == true);
    }

    private void ParticleEffectsCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(PetVisualPolishLogger.ParticleEffectsSetting, ParticleEffectsCheckBox.IsChecked == true);
    }

    private void WindowShakeReactionCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(PetVisualPolishLogger.WindowShakeReactionSetting, WindowShakeReactionCheckBox.IsChecked == true);
    }

    private void WebSearchEnabledCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(WebResearchConnector.WebSearchEnabledSetting, WebSearchEnabledCheckBox.IsChecked == true);
    }

    private void PetModelAdapterEnabledCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting("pet_model_adapter_enabled", PetModelAdapterEnabledCheckBox.IsChecked == true);
    }

    private void LocalDocumentRetrievalEnabledCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(SettingKeys.LocalDocumentRetrievalEnabled, LocalDocumentRetrievalEnabledCheckBox.IsChecked == true);
    }

    private async void AutonomousScopePreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string scopeId } && AutonomousScopePreviewRequested is not null)
        {
            _autonomousScopePreviewText = $"Preview requested for {scopeId}.";
            AutonomousScopePreviewText.Text = _autonomousScopePreviewText;
            await AutonomousScopePreviewRequested.Invoke(scopeId);
        }
    }

    private void SpriteRepairTriageScopeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(
            AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairTriageScopeId),
            SpriteRepairTriageScopeCheckBox.IsChecked == true);
    }

    private void SpriteRepairBatchProposalScopeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(
            AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId),
            SpriteRepairBatchProposalScopeCheckBox.IsChecked == true);
    }

    private void AuditLedgerCleanupScopeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(
            AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.AuditLedgerCleanupScopeId),
            AuditLedgerCleanupScopeCheckBox.IsChecked == true);
    }

    private void AutonomousSpriteRepairTriageScopeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(
            AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairTriageScopeId),
            AutonomousSpriteRepairTriageScopeCheckBox.IsChecked == true);
    }

    private void AutonomousSpriteRepairBatchProposalScopeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(
            AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId),
            AutonomousSpriteRepairBatchProposalScopeCheckBox.IsChecked == true);
    }

    private void AutonomousAuditLedgerCleanupScopeCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        PublishSetting(
            AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.AuditLedgerCleanupScopeId),
            AutonomousAuditLedgerCleanupScopeCheckBox.IsChecked == true);
    }

    internal void SetAutonomousScopePreview(AutonomousScopePreview preview)
    {
        var lines = new List<string>
        {
            $"{preview.ScopeId}: {preview.Summary}",
            $"would-do action count: {preview.ActionCount}",
            $"flags: network={preview.EvidenceFlags.DidUseNetwork}, hosted_ai={preview.EvidenceFlags.DidUseHostedAi}, local_model={preview.EvidenceFlags.DidUseLocalModel}, mutate={preview.EvidenceFlags.DidMutate}"
        };
        if (!string.IsNullOrWhiteSpace(preview.BlockReason))
        {
            lines.Add($"blocked: {preview.BlockReason}");
        }

        foreach (var item in preview.PlannedItems.Take(8))
        {
            var details = new List<string> { item.Label };
            if (!string.IsNullOrWhiteSpace(item.SourcePath))
            {
                details.Add($"source={item.SourcePath}");
            }

            if (!string.IsNullOrWhiteSpace(item.DestinationPath))
            {
                details.Add($"dest={item.DestinationPath}");
            }

            if (!string.IsNullOrWhiteSpace(item.Sha256))
            {
                details.Add($"sha256={item.Sha256}");
            }

            if (item.AgeDays is not null)
            {
                details.Add($"age_days={item.AgeDays}");
            }

            lines.Add("- " + string.Join(" | ", details));
        }

        _autonomousScopePreviewText = string.Join(Environment.NewLine, lines);
        AutonomousScopePreviewText.Text = _autonomousScopePreviewText;
    }

    private void PetModelFirstCallConsentButton_OnClick(object sender, RoutedEventArgs e)
    {
        PublishSetting("pet_model_first_call_approved", true);
    }

    private void ToolRegistryCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { Tag: string toolFamily } checkBox)
        {
            PublishSetting(ToolRegistry.BuildToolEnabledSettingKey(toolFamily), checkBox.IsChecked == true);
        }
    }

    private void PullDefaultModelButton_OnClick(object sender, RoutedEventArgs e)
    {
        PullDefaultModel();
    }

    private async void SelectedPetComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingEvents || SelectedPetComboBox.SelectedValue is not Guid petId)
        {
            return;
        }

        _lastRenderedDevPetId = petId;
        await PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.SelectPet, petId));
    }

    private void SpeciesComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSettingEvents || SpeciesComboBox.SelectedItem is not string speciesId || SpeciesComboBox.Tag is not GameContent content)
        {
            return;
        }

        var species = content.Species.FirstOrDefault(item => string.Equals(item.Id, speciesId, StringComparison.OrdinalIgnoreCase));
        if (species is null)
        {
            return;
        }

        var colors = species.SupportedColors?.ToList() ?? ["blue"];
        var currentColor = ColorComboBox.SelectedItem as string;
        ColorComboBox.ItemsSource = colors;
        ColorComboBox.SelectedItem = colors.Any(color => string.Equals(color, currentColor, StringComparison.OrdinalIgnoreCase))
            ? currentColor
            : colors.First();
    }

    private async void AddPetButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildAppearanceCommand(DevToolCommandKind.AddPet));
    }

    private async void RemovePetButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.RemovePet, GetSelectedPetId()));
    }

    private async void ClearPetsButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.RemoveAllPets));
    }

    private async void ApplyAppearanceButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildAppearanceCommand(DevToolCommandKind.ApplyAppearance));
    }

    private async void ApplyEnvironmentButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildEnvironmentCommand());
    }

    private async void ApplyAnimationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildAnimationCommand(DevToolCommandKind.ApplyAnimation));
    }

    private async void ClearAnimationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(new DevToolCommand(DevToolCommandKind.ClearAnimation, GetSelectedPetId()));
    }

    private async void SpawnColorSetButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildAppearanceCommand(DevToolCommandKind.SpawnColorSet));
    }

    private async void ApplyConditionButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildConditionCommand(DevToolCommandKind.SetCondition));
    }

    private async void ClearConditionButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildConditionCommand(DevToolCommandKind.ClearCondition));
    }

    private async void ApplyVitalsButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PublishDevCommandAsync(BuildVitalsCommand());
    }

    private async void HungryPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("hungry"));

    private async void ThirstyPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("thirsty"));

    private async void TiredPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("tired"));

    private async void DirtyPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("dirty"));

    private async void LonelyPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("lonely"));

    private async void SickPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("sick"));

    private async void HealthyPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("healthy"));

    private async void ComfortedPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("comforted"));

    private async void RecallPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("recall"));

    private async void ObesePresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("obese"));

    private async void MalnourishedPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("malnourished"));

    private async void AnxiousPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("anxious"));

    private async void DepressedPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("depressed"));

    private async void InjuredPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("injured"));

    private async void ElderPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("elder"));

    private async void FoodiePresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("foodie"));

    private async void CuddlyPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("cuddly"));

    private async void NeatPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("neat"));

    private async void PlayfulPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("playful"));

    private async void StubbornPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("stubborn"));

    private async void ResilientPresetButton_OnClick(object sender, RoutedEventArgs e) => await PublishDevCommandAsync(BuildPresetCommand("resilient"));

    private void PublishSetting(string key, bool value)
    {
        if (_suppressSettingEvents)
        {
            return;
        }

        SettingChanged?.Invoke(key, value.ToString());
    }

    private void PublishSetting(string key, string value)
    {
        if (_suppressSettingEvents)
        {
            return;
        }

        SettingChanged?.Invoke(key, value);
    }

    private static bool GetSettingBool(CompanionState state, string key, bool defaultValue = false)
    {
        return GetSettingsBool(state.SettingsSnapshot, key, defaultValue);
    }

    private static int GetSettingInt(CompanionState state, string key, int defaultValue = 0)
    {
        return state.SettingsSnapshot.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static bool GetSettingsBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue = false)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    private static string FormatPetModelCapabilityStatus(bool modelEnabled, bool firstCallApproved)
    {
        if (!modelEnabled)
        {
            return "AI helper summaries: Disabled";
        }

        return firstCallApproved
            ? "AI helper summaries: Enabled, no live adapter wired in Shell"
            : "AI helper summaries: Enabled flag set, waiting for first-call consent";
    }

    private static string FormatAutonomousBetaStatus(CompanionState state, AutonomousBetaDecision? decision)
    {
        var enabled = GetSettingBool(state, AutonomousOperationsConfig.EnabledSetting);
        if (decision is null)
        {
            return $"Autonomous beta: {(enabled ? "enabled" : "off")} | decision not evaluated | proposal-only; mutation apply is blocked.";
        }

        var passed = decision.Checks.Count(check => check.Passed);
        return $"Autonomous beta: {(enabled ? "enabled" : "off")} | decision={decision.Decision} | checks={passed}/{decision.Checks.Count} | proposal-only; mutation apply is blocked.";
    }

    internal static string FormatPromotionSnapshotTable(PromotionDecision? decision)
    {
        if (decision is null)
        {
            return "Promotion snapshot: no reviewed decision packet yet.";
        }

        var lines = new List<string>
        {
            $"Promotion decision: {decision.Label} ({decision.Criteria.Count(criterion => criterion.Passed)}/{decision.Criteria.Count})"
        };
        lines.AddRange(decision.Criteria.Select(criterion =>
            $"{(criterion.Passed ? "PASS" : "FAIL")} {criterion.Id}: {criterion.ObservedValue} vs {criterion.Threshold}"));
        return string.Join(Environment.NewLine, lines);
    }

    internal static string FormatAutonomousBetaTryHelp(PromotionDecision? decision, IReadOnlyDictionary<string, string> settings)
    {
        if (PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(decision, settings))
        {
            return "Ready for explicit confirmation. The loop remains proposal-only, daily-capped, and KillSwitch-protected.";
        }

        if (KillSwitchService.IsActive(settings))
        {
            return "Disabled because Stop Everything is active.";
        }

        if (GetSettingsBool(settings, AutonomousOperationsConfig.EnabledSetting))
        {
            return "Autonomous beta is already enabled.";
        }

        return decision is null
            ? "Disabled until a 7-day promotion packet passes every safety and liveness criterion."
            : $"Disabled because latest promotion decision is {decision.Label}: {string.Join(", ", decision.Reasons)}.";
    }

    internal static bool ShouldWriteAutonomousBetaConsent(bool explicitConfirm, PromotionDecision? decision, IReadOnlyDictionary<string, string> settings)
    {
        return explicitConfirm && PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(decision, settings);
    }

    internal static string FormatAutonomousScopeStatus(IReadOnlyDictionary<string, string> settings)
    {
        var betaEnabled = GetSettingsBool(settings, AutonomousOperationsConfig.EnabledSetting);
        var spriteEnabled = AutonomousScopeService.IsEnabled(settings, AutonomousScopeService.SpriteRepairTriageScopeId);
        var proposalEnabled = AutonomousScopeService.IsEnabled(settings, AutonomousScopeService.SpriteRepairBatchProposalScopeId);
        var cleanupEnabled = AutonomousScopeService.IsEnabled(settings, AutonomousScopeService.AuditLedgerCleanupScopeId);
        return $"Scopes: sprite-repair-triage={(spriteEnabled ? "on" : "off")}, sprite-repair-batch-proposal={(proposalEnabled ? "on" : "off")}, audit-ledger-cleanup={(cleanupEnabled ? "on" : "off")} | autonomous beta={(betaEnabled ? "on" : "off")} | review-only scopes never mutate sprite art.";
    }

    private static string FormatAutonomousScopeRecentLine(IReadOnlyList<string>? recentLines, string scopeId)
    {
        var line = recentLines?
            .LastOrDefault(candidate => candidate.Contains(scopeId, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(line)
            ? "Last tick: none in recent activity."
            : $"Last tick/result: {line}";
    }

    private async Task RequestAutonomousBetaConsentAsync()
    {
        if (!AutonomousBetaTryButton.IsEnabled)
        {
            return;
        }

        var result = MessageBox.Show(
            "Enable the autonomous operations beta?\n\nThis lets Wevito run the reviewed proposal-only autonomous loop. It does not mutate files by itself, daily caps still apply, and Stop Everything still blocks helper work immediately.\n\nClick Yes only if you want to enable the beta now.",
            "Enable the autonomous operations beta?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes || AutonomousBetaConsentConfirmed is null)
        {
            return;
        }

        await AutonomousBetaConsentConfirmed.Invoke();
    }

    private static string FormatPetModelConsentNotice()
    {
        var notice = ModelConsentNoticeBuilder.BuildAnthropicNotice();
        return string.Join(Environment.NewLine, [
            $"Provider: {notice.Provider}",
            $"Credential target: {notice.CredentialStorage}",
            $"Data sent: {notice.WhatIsSent}",
            $"Local audit: {notice.LocalAuditPath}",
            $"Allowlist: {notice.AllowlistSummary}",
            notice.NoToolExecutionStatement
        ]);
    }

    private static string FormatLocalAiRuntimeStatus(CompanionState state)
    {
        var providerMode = state.SettingsSnapshot.TryGetValue(ModelProviderModeService.ProviderModeSetting, out var mode)
            ? mode
            : "disabled";
        var endpoint = state.SettingsSnapshot.TryGetValue(ModelProviderModeService.LocalRuntimeEndpointSetting, out var configuredEndpoint)
            ? configuredEndpoint
            : LocalRuntimeProbeService.DefaultOllamaEndpoint;
        var model = state.SettingsSnapshot.TryGetValue(ModelProviderModeService.LocalRuntimeModelSetting, out var configuredModel)
            ? configuredModel
            : LocalRuntimeProbeService.DefaultOllamaModel;
        var available = GetSettingBool(state, ModelProviderModeService.LocalProviderAvailableSetting);
        var inProcessEnabled = GetSettingBool(state, ModelProviderModeService.InProcessLocalRuntimeEnabledSetting);
        return $"Local AI runtime: mode={providerMode}; provider={(available ? "available" : "not probed/available")}; endpoint={endpoint}; model={model}; in-process fallback={(inProcessEnabled ? "enabled if weights exist" : "off")}; hosted AI remains disabled unless separately approved.";
    }

    private void RenderToolRegistryList(CompanionState state)
    {
        var registry = ToolRegistry.CreateDefault(settingsProvider: () => state.SettingsSnapshot);
        ToolRegistryListPanel.Children.Clear();
        foreach (var tool in registry.Descriptors)
        {
            var catalogEntry = ToolCatalog.FindFamily(tool.ToolFamily);
            var checkBox = new CheckBox
            {
                Margin = new Thickness(0, 4, 0, 0),
                Style = (Style)FindResource("PopupCheckBoxStyle"),
                Foreground = Brushes.WhiteSmoke,
                IsChecked = !registry.IsDisabled(tool.ToolFamily),
                Tag = tool.ToolFamily,
                Content = $"{tool.ToolFamily} - {tool.RiskLevel} - {(tool.RequiresApproval ? "approval required" : "no approval")} - {catalogEntry?.Description ?? tool.Description}"
            };
            checkBox.Checked += ToolRegistryCheckBox_OnChanged;
            checkBox.Unchecked += ToolRegistryCheckBox_OnChanged;
            ToolRegistryListPanel.Children.Add(checkBox);
        }

        if (ToolRegistryListPanel.Children.Count == 0)
        {
            ToolRegistryListPanel.Children.Add(new TextBlock
            {
                Foreground = Brushes.LightGray,
                FontSize = 10,
                Text = "No AI-callable tools are registered."
            });
        }
    }

    private void ApplyToolCatalogTabMetadata(bool localDocsEnabled, bool advancedToolsVisible)
    {
        ApplyTab(PetsTabButton);
        ApplyTab(TasksTabButton);
        ApplyTab(ToolsTabButton);
        ApplyTab(LocalAiTabButton);
        ApplyTab(EvidenceTabButton);
        ApplyTab(AutonomyTabButton);
        ApplyTab(LocalDocsTabButton);
        ApplyTab(ActivityTabButton);
        ApplyTab(BenchmarksTabButton);
        ApplyTab(CreativeLabTabButton);

        AdvancedToolsToggle.ToolTip = advancedToolsVisible
            ? "Hide advanced Tool Hub tabs."
            : "Show advanced Tool Hub tabs.";
        LocalDocsTabButton.Visibility = localDocsEnabled ? Visibility.Visible : Visibility.Collapsed;

        void ApplyTab(Button button)
        {
            if (button.Tag is not string toolId)
            {
                return;
            }

            var tab = ToolCatalog.FindTabByToolId(toolId);
            if (tab is null)
            {
                return;
            }

            button.Content = tab.DisplayName;
            button.ToolTip = tab.Description;
        }
    }

    private void RenderLocalDocsPanel(CompanionState state)
    {
        var root = state.SettingsSnapshot.TryGetValue(SettingKeys.LocalDocumentRetrievalRoot, out var configuredRoot)
            ? configuredRoot
            : SettingKeys.DefaultLocalDocumentRetrievalRoot();
        var maxBytes = state.SettingsSnapshot.TryGetValue(SettingKeys.LocalDocumentRetrievalMaxFileBytes, out var configuredMax)
            ? configuredMax
            : SettingKeys.LocalDocumentRetrievalDefaultMaxFileBytes;
        LocalDocsRootText.Text = $"Root: {root}";
        LocalDocsPolicyText.Text = $"Default-off local search. File types: .md, .txt, .json. Max file bytes: {maxBytes}. No model and no network.";
        LocalDocsStatusText.Text = _localDocsStatusText;
        LocalDocsResultsGrid.ItemsSource = _localDocumentRows;
    }

    private void RenderEvidencePanel(CompanionState state, EvidenceSummary? summary)
    {
        if (summary is null)
        {
            _evidenceSummaryRows = [];
            EvidenceSummaryGrid.ItemsSource = _evidenceSummaryRows;
            EvidenceStatusText.Text = "Evidence summary is waiting for shell state.";
            EvidenceExportButton.IsEnabled = false;
            return;
        }

        _evidenceSummaryRows = summary.Rows.Select(EvidenceSummaryRowItem.From).ToList();
        EvidenceSummaryGrid.ItemsSource = _evidenceSummaryRows;
        EvidenceExportButton.IsEnabled = !summary.IsBlocked;
        var unknownText = summary.UnknownPacketKinds.Count == 0
            ? "no unknown packet kinds"
            : $"{summary.UnknownPacketKinds.Count} unknown packet kind(s) hidden from dashboard claims";
        EvidenceStatusText.Text = summary.IsBlocked
            ? $"Blocked: {summary.StatusMessage}"
            : $"Range={ResolveEvidenceDateRangeSetting(state)}; max={summary.Query.MaxPackets}; rows={summary.Rows.Count}; {unknownText}. Export writes only to vnext/artifacts/c-phase-141-evidence-dashboard/.";
    }

    internal static EvidenceSummaryQuery BuildEvidenceSummaryQuery(IReadOnlyDictionary<string, string> settings, DateTimeOffset nowUtc)
    {
        var maxPackets = settings.TryGetValue("evidence_dashboard_max_packets", out var rawMax) &&
            int.TryParse(rawMax, out var parsedMax)
            ? parsedMax
            : EvidenceSummaryService.DefaultMaxPackets;
        var range = settings.TryGetValue("evidence_dashboard_date_range", out var rawRange)
            ? rawRange
            : "24h";
        DateTimeOffset? from = range switch
        {
            "7d" => nowUtc.AddDays(-7),
            "30d" => nowUtc.AddDays(-30),
            "all" => null,
            _ => nowUtc.AddHours(-24)
        };
        return new EvidenceSummaryQuery(from, nowUtc, maxPackets);
    }

    private static string ResolveEvidenceDateRangeSetting(CompanionState state)
    {
        return state.SettingsSnapshot.TryGetValue("evidence_dashboard_date_range", out var range) &&
            range is "24h" or "7d" or "30d" or "all"
            ? range
            : "24h";
    }

    private static int ResolveEvidenceMaxPacketsSetting(CompanionState state)
    {
        return state.SettingsSnapshot.TryGetValue("evidence_dashboard_max_packets", out var raw) &&
            int.TryParse(raw, out var maxPackets)
            ? Math.Clamp(maxPackets, 1, EvidenceSummaryService.MaxAllowedPackets)
            : EvidenceSummaryService.DefaultMaxPackets;
    }

    private static string FormatReasoningModelStatus(CompanionState state)
    {
        var model = state.SettingsSnapshot.TryGetValue(ModelProviderModeService.LocalRuntimeModelSetting, out var configuredModel)
            ? configuredModel
            : LocalRuntimeProbeService.DefaultOllamaModel;
        var endpoint = state.SettingsSnapshot.TryGetValue(ModelProviderModeService.LocalRuntimeEndpointSetting, out var configuredEndpoint)
            ? configuredEndpoint
            : LocalRuntimeProbeService.DefaultOllamaEndpoint;
        var available = GetSettingBool(state, ModelProviderModeService.LocalProviderAvailableSetting);
        var mode = state.SettingsSnapshot.TryGetValue(ModelProviderModeService.ProviderModeSetting, out var configuredMode)
            ? configuredMode
            : ModelProviderModeService.LocalOnlyModeValue;
        var latency = state.SettingsSnapshot.TryGetValue("local_runtime_last_latency_ms", out var rawLatency) && !string.IsNullOrWhiteSpace(rawLatency)
            ? $"{rawLatency} ms"
            : "not measured";
        var runtimeStatus = available ? "Available" : "Bootstrap required or unavailable";
        return $"Reasoning model status: model={model}; mode={mode}; runtime={runtimeStatus}; endpoint={endpoint}; last latency={latency}.";
    }

    private void PullDefaultModel()
    {
        var repoRoot = ResolveRepoRootOrBaseDirectory();
        var scriptPath = Path.Combine(repoRoot, "tools", "pull-default-model.ps1");
        if (!File.Exists(scriptPath))
        {
            ReasoningModelStatusText.Text = $"Reasoning model status: pull script not found at {scriptPath}.";
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            WorkingDirectory = repoRoot,
            UseShellExecute = true
        });
        ReasoningModelStatusText.Text = $"Reasoning model status: pull started for {LocalRuntimeProbeService.DefaultOllamaModel}.";
    }

    private static string ResolveRepoRootOrBaseDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                Directory.Exists(Path.Combine(current.FullName, "vnext")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private static ImageSource? ResolveActionOptionPreview(SpriteAssetService assetService, string actionId, HabitatDisplayItem item)
    {
        if (item.IsSmallIconSafe)
        {
            var itemArt = assetService.GetItem(item.CategoryFolder, item.AssetId);
            if (itemArt is not null)
            {
                return itemArt;
            }
        }

        ImageSource? preview = item.CategoryFolder switch
        {
            "containers" => assetService.GetIcon("water_bowl") ?? assetService.GetIcon("water"),
            "food_predator" => assetService.GetIcon("food_meat"),
            "food_herbivore" => assetService.GetIcon("food_plant"),
            "food_birds" => assetService.GetIcon("feed"),
            "food_omnivore" => assetService.GetIcon("feed"),
            "toys_a" => assetService.GetIcon("exercise"),
            "toys_b" => actionId is "rest" or "home"
                ? assetService.GetIcon("rest")
                : assetService.GetIcon("exercise"),
            "care" => actionId switch
            {
                "groom" => assetService.GetIcon("groom"),
                "bath" => assetService.GetIcon("bathe"),
                "medicine" => assetService.GetIcon("medicine"),
                "doctor" => assetService.GetIcon("doctor"),
                _ => assetService.GetIcon("medicine")
            },
            _ => null
        };

        preview ??= actionId switch
        {
            "feed" => assetService.GetIcon("feed"),
            "water" => assetService.GetIcon("water"),
            "rest" or "home" => assetService.GetIcon("rest"),
            "play" => assetService.GetIcon("exercise"),
            "groom" => assetService.GetIcon("groom"),
            "bath" => assetService.GetIcon("bathe"),
            "medicine" => assetService.GetIcon("medicine"),
            "doctor" => assetService.GetIcon("doctor"),
            _ => null
        };

        return preview ?? assetService.GetItem(item.CategoryFolder, item.AssetId);
    }

    internal static string FormatActionSummary(string actionDisplayName, int optionCount, IReadOnlyList<PetActor> pets)
    {
        return FormatActionSummary(actionDisplayName, "", optionCount, pets);
    }

    internal static string FormatActionSummary(string actionDisplayName, string actionDescription, int optionCount, IReadOnlyList<PetActor> pets)
    {
        var targetSummary = FormatLivingPetTargets(pets);
        var actionName = string.IsNullOrWhiteSpace(actionDisplayName)
            ? "action"
            : actionDisplayName.ToLowerInvariant();
        var prefix = optionCount switch
        {
            0 => $"No specific {actionName} options are ready right now.",
            1 => $"Use or drag the prepared {actionName} option below.",
            _ => $"Choose, drag, or use one of {optionCount} prepared {actionName} options."
        };
        var clarity = string.IsNullOrWhiteSpace(actionDescription)
            ? ""
            : $" {actionDescription.Trim()}";

        return $"{prefix}{clarity} Target: {targetSummary}. Drag and drop an item onto the pet, or click the target button.";
    }

    internal static string BuildActionOptionButtonLabel(IReadOnlyList<PetActor> pets)
    {
        var firstLivingPet = pets.FirstOrDefault(pet => !pet.IsDead);
        return firstLivingPet is null ? "Use" : $"Use on {firstLivingPet.Name}";
    }

    internal static bool TryParseActionOptionDragPayload(string? payload, out string actionId, out string itemId)
    {
        actionId = "";
        itemId = "";
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        actionId = parts[0];
        itemId = parts[1];
        return true;
    }

    private static string FormatLivingPetTargets(IReadOnlyList<PetActor> pets)
    {
        var names = pets
            .Where(pet => !pet.IsDead)
            .Select(pet => pet.Name)
            .Take(3)
            .ToList();
        return names.Count == 0
            ? "no living pets"
            : string.Join(", ", names);
    }

    private async Task<bool> TryInvokeButtonAsync(Button button, Point localPoint, Func<Task>? action)
    {
        if (!IsPointInside(button, localPoint) || action is null)
        {
            return false;
        }

        await action.Invoke();
        return true;
    }

    private async Task<bool> TryOpenBasketRowAsync(Point localPoint)
    {
        foreach (var item in _basketRows)
        {
            if (BasketGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow rowContainer)
            {
                continue;
            }

            if (!IsPointInside(rowContainer, localPoint))
            {
                continue;
            }

            var rowOrigin = rowContainer.TransformToAncestor(this).Transform(new Point(0, 0));
            var relativeX = localPoint.X - rowOrigin.X;
            if (relativeX < 42)
            {
                continue;
            }

            if (OpenRequested is not null)
            {
                await OpenRequested.Invoke(item.Id);
                return true;
            }
        }

        return false;
    }

    private async Task<bool> TryInvokeActionOptionAsync(Point localPoint)
    {
        foreach (var item in _actionRows)
        {
            if (ActionGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow rowContainer)
            {
                continue;
            }

            if (!IsPointInside(rowContainer, localPoint) || ActionOptionRequested is null)
            {
                continue;
            }

            await ActionOptionRequested.Invoke(item.ActionId, item.ItemId);
            return true;
        }

        return false;
    }

    private async Task<bool> TryInvokeActionMenuAsync(Point localPoint)
    {
        var buttons = new Button[]
        {
            ActionMenuFeedButton,
            ActionMenuWaterButton,
            ActionMenuRestButton,
            ActionMenuPlayButton,
            ActionMenuGroomButton,
            ActionMenuBathButton,
            ActionMenuMedicineButton,
            ActionMenuDoctorButton,
            ActionMenuHomeButton
        };

        foreach (var button in buttons)
        {
            if (!IsPointInside(button, localPoint) || button.Tag is not string actionId || ActionMenuRequested is null)
            {
                continue;
            }

            await ActionMenuRequested.Invoke(actionId);
            return true;
        }

        return false;
    }

    private async Task<bool> TryDeleteMarkedAsync(Point localPoint)
    {
        if (!IsPointInside(DeleteButton, localPoint))
        {
            return false;
        }

        var ids = _basketRows.Where(row => row.IsMarked).Select(row => row.Id).ToList();
        if (ids.Count == 0 && BasketGrid.SelectedItem is BasketRowItem selectedRow)
        {
            ids.Add(selectedRow.Id);
        }
        if (ids.Count == 0 && _basketRows.Count == 1)
        {
            ids.Add(_basketRows[0].Id);
        }
        if (ids.Count == 0 || DeleteRequested is null)
        {
            return false;
        }

        await DeleteRequested.Invoke(ids);
        return true;
    }

    private Task<bool> TryToggleBasketMarkAsync(Point localPoint)
    {
        foreach (var item in _basketRows)
        {
            if (BasketGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow rowContainer)
            {
                continue;
            }

            if (!IsPointInside(rowContainer, localPoint))
            {
                continue;
            }

            var rowOrigin = rowContainer.TransformToAncestor(this).Transform(new Point(0, 0));
            var relativeX = localPoint.X - rowOrigin.X;
            if (relativeX > 42)
            {
                continue;
            }

            item.IsMarked = !item.IsMarked;
            UpdateBasketDeleteButtonState();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private bool TryToggleCheckBox(CheckBox checkBox, Point localPoint)
    {
        if (!IsPointInside(checkBox, localPoint))
        {
            return false;
        }

        checkBox.IsChecked = !(checkBox.IsChecked ?? false);
        return true;
    }

    private bool IsPointInside(FrameworkElement element, Point localPoint)
    {
        if (element.Visibility != Visibility.Visible || !element.IsEnabled || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        var origin = element.TransformToAncestor(this).Transform(new Point(0, 0));
        var bounds = new Rect(origin.X, origin.Y, element.ActualWidth, element.ActualHeight);
        return bounds.Contains(localPoint);
    }

    private Guid? GetSelectedPetId()
    {
        return SelectedPetComboBox.SelectedValue is Guid petId ? petId : null;
    }

    private static Guid? GetSelectedPetId(CompanionState state)
    {
        if (state.SettingsSnapshot.TryGetValue("dev_selected_pet_id", out var raw) && Guid.TryParse(raw, out var petId))
        {
            return petId;
        }

        return null;
    }

    private DevToolCommand BuildAppearanceCommand(DevToolCommandKind kind)
    {
        var ageStage = AgeComboBox.SelectedItem is PetAgeStage selectedAge ? selectedAge : PetAgeStage.Adult;
        var gender = GenderComboBox.SelectedItem is PetGender selectedGender ? selectedGender : PetGender.Female;
        return new DevToolCommand(
            kind,
            GetSelectedPetId(),
            SpeciesId: SpeciesComboBox.SelectedItem as string ?? "",
            AgeStage: ageStage,
            Gender: gender,
            ColorVariant: ColorComboBox.SelectedItem as string ?? "blue");
    }

    private DevToolCommand BuildPresetCommand(string presetId)
    {
        return new DevToolCommand(DevToolCommandKind.ApplyPreset, GetSelectedPetId(), PresetId: presetId);
    }

    private DevToolCommand BuildEnvironmentCommand()
    {
        return new DevToolCommand(
            DevToolCommandKind.ApplyEnvironment,
            GetSelectedPetId(),
            EnvironmentId: EnvironmentComboBox.SelectedValue as string ?? "");
    }

    private DevToolCommand BuildAnimationCommand(DevToolCommandKind kind)
    {
        var animation = AnimationComboBox.SelectedItem is PetAnimationState selectedAnimation
            ? selectedAnimation
            : PetAnimationState.Idle;
        var seconds = AnimationDurationComboBox.SelectedItem is double selectedSeconds
            ? selectedSeconds
            : 8.0;
        return new DevToolCommand(
            kind,
            GetSelectedPetId(),
            AnimationState: animation,
            OverrideDurationSeconds: seconds);
    }

    private DevToolCommand BuildConditionCommand(DevToolCommandKind kind)
    {
        var conditionId = ConditionComboBox.SelectedValue as string ?? "";
        var severity = ConditionSeverityComboBox.SelectedItem is int selectedSeverity ? selectedSeverity : 1;
        return new DevToolCommand(
            kind,
            GetSelectedPetId(),
            ConditionId: conditionId,
            ConditionSeverity: severity);
    }

    private DevToolCommand BuildVitalsCommand()
    {
        return new DevToolCommand(
            DevToolCommandKind.ApplyVitals,
            GetSelectedPetId(),
            Hunger: HungerSlider.Value,
            Thirst: ThirstSlider.Value,
            Energy: EnergySlider.Value,
            Cleanliness: CleanlinessSlider.Value,
            Affection: AffectionSlider.Value,
            Comfort: ComfortSlider.Value,
            Health: HealthSlider.Value,
            Fitness: FitnessSlider.Value,
            BiologicalAgeMinutes: BiologicalAgeSlider.Value);
    }

    private async Task PublishDevCommandAsync(DevToolCommand command)
    {
        _lastRenderedDevPetId = null;
        if (DevToolCommandRequested is not null)
        {
            await DevToolCommandRequested.Invoke(command);
        }
    }

    private async Task PublishPetTaskStatusChangeAsync(TaskCardStatus nextStatus)
    {
        if (PetTaskQueueComboBox.SelectedValue is Guid cardId && PetTaskStatusChangeRequested is not null)
        {
            await PetTaskStatusChangeRequested.Invoke(cardId, nextStatus);
        }
    }

    private async Task PublishPetTaskPreviewAsync()
    {
        if (PetTaskQueueComboBox.SelectedValue is Guid cardId && PetTaskPreviewRequested is not null)
        {
            await PetTaskPreviewRequested.Invoke(cardId);
        }
    }

    private async Task PublishPetTaskExecutionAsync()
    {
        if (PetTaskQueueComboBox.SelectedValue is Guid cardId && PetTaskExecutionRequested is not null)
        {
            await PetTaskExecutionRequested.Invoke(cardId);
        }
    }

    private Task OpenSelectedPetTaskReportAsync()
    {
        var resolution = ResolveSelectedPetTaskArtifactPath();
        if (!resolution.IsAllowed)
        {
            SetPetTaskArtifactBlockedMessage(resolution.BlockReason);
            return Task.CompletedTask;
        }

        if (!File.Exists(resolution.ReportPath))
        {
            SetPetTaskArtifactBlockedMessage("blocked: report file does not exist");
            return Task.CompletedTask;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = resolution.ReportPath,
                UseShellExecute = true
            });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            SetPetTaskArtifactBlockedMessage("blocked: report could not be opened");
            return Task.CompletedTask;
        }

        PetTaskNextActionText.Text = "Opened the selected task report.";
        return Task.CompletedTask;
    }

    private Task CopySelectedPetTaskPathAsync()
    {
        var resolution = ResolveSelectedPetTaskArtifactPath();
        if (!resolution.IsAllowed)
        {
            SetPetTaskArtifactBlockedMessage(resolution.BlockReason);
            return Task.CompletedTask;
        }

        Clipboard.SetText(resolution.ReportPath);
        PetTaskNextActionText.Text = "Copied the selected report path.";
        return Task.CompletedTask;
    }

    private Task OpenSelectedPetTaskFolderAsync()
    {
        var resolution = ResolveSelectedPetTaskArtifactPath();
        if (!resolution.IsAllowed)
        {
            SetPetTaskArtifactBlockedMessage(resolution.BlockReason);
            return Task.CompletedTask;
        }

        var explorerArgument = File.Exists(resolution.ReportPath)
            ? $"/select,\"{resolution.ReportPath}\""
            : $"\"{resolution.ArtifactFolder}\"";
        try
        {
            Process.Start("explorer.exe", explorerArgument);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            SetPetTaskArtifactBlockedMessage("blocked: report folder could not be opened");
            return Task.CompletedTask;
        }

        PetTaskNextActionText.Text = "Opened the selected report folder.";
        return Task.CompletedTask;
    }

    private AgentTaskArtifactPathResolution ResolveSelectedPetTaskArtifactPath()
    {
        if (PetTaskQueueComboBox.SelectedItem is not PetTaskQueueRowItem selectedRow)
        {
            return new AgentTaskArtifactPathResolution(false, "", "", "No task card is selected.");
        }

        return AgentTaskCardQueueService.ResolveArtifactReportPath(selectedRow.AuditLogPath, ResolveRepoRoot());
    }

    private static string ResolveRepoRoot()
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

    private void SetPetTaskArtifactBlockedMessage(string reason)
    {
        PetTaskNextActionText.Text = string.IsNullOrWhiteSpace(reason)
            ? "Blocked: the selected artifact path could not be opened safely."
            : reason;
    }

    private void UpdatePetTaskButtons()
    {
        var selectedRow = PetTaskQueueComboBox.SelectedItem as PetTaskQueueRowItem;
        PetTaskApproveButton.IsEnabled = selectedRow?.Status == TaskCardStatus.WaitingForApproval;
        PetTaskPreviewButton.IsEnabled = selectedRow?.Status is TaskCardStatus.Draft or TaskCardStatus.Approved;
        PetTaskCancelButton.IsEnabled = selectedRow?.Status is TaskCardStatus.Draft or TaskCardStatus.WaitingForApproval or TaskCardStatus.Approved;
        PetTaskExecuteButton.IsEnabled = selectedRow is { Status: TaskCardStatus.Reviewing, CanExecute: true };
        var hasArtifactPath = !string.IsNullOrWhiteSpace(selectedRow?.AuditLogPath);
        PetTaskOpenReportButton.IsEnabled = hasArtifactPath;
        PetTaskCopyPathButton.IsEnabled = hasArtifactPath;
        PetTaskOpenFolderButton.IsEnabled = hasArtifactPath;
    }

    private static bool CanRunReviewedTask(TaskCard card)
    {
        if (card.Status != TaskCardStatus.Reviewing)
        {
            return false;
        }

        if (string.Equals(card.ToolFamily, "translateText", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(card.ToolFamily, "screenCapture", StringComparison.OrdinalIgnoreCase) &&
            IsExecutableScreenCaptureRequest(card.Intent.RawText))
        {
            return true;
        }

        if (string.Equals(card.ToolFamily, "buildProof", StringComparison.OrdinalIgnoreCase) &&
            card.Intent.TaskKind == TaskKind.BuildProof)
        {
            return true;
        }

        return string.Equals(card.ToolFamily, "audioAssist", StringComparison.OrdinalIgnoreCase) &&
               IsExecutableAudioAssistRequest(card.Intent.RawText);
    }

    private static bool IsWevitoWindowCaptureRequest(string rawText)
    {
        return rawText.Contains("wevito", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("this window", StringComparison.OrdinalIgnoreCase);
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

    private static bool IsExecutableAudioAssistRequest(string rawText)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            rawText,
            @"\b(unmute|mute|(?:set|change|turn|put|make)\b.*?\bvolume\b.*?\d{1,3}|volume\b.*?\b(?:to|at)\b\s*\d{1,3})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string BuildPersonalitySummary(PetActor? pet)
    {
        if (pet?.Personality is null)
        {
            return "Personality: no pet selected";
        }

        var personality = pet.Personality;
        var descriptors = new List<string>();
        if (personality.Playfulness >= 25) { descriptors.Add("playful"); }
        if (personality.Cheerfulness >= 25) { descriptors.Add("bright"); }
        if (personality.CuddleNeed >= 25 || personality.SocialNeed >= 25) { descriptors.Add("clingy"); }
        if (personality.CleanlinessPreference >= 25) { descriptors.Add("neat"); }
        if (personality.ActivityLevel >= 25) { descriptors.Add("active"); }
        if (personality.Stubbornness >= 25) { descriptors.Add("headstrong"); }
        if (descriptors.Count == 0) { descriptors.Add("settling"); }
        return $"Personality: {string.Join(", ", descriptors.Take(3))}";
    }

    private static string BuildHabitSummary(PetActor? pet)
    {
        if (pet?.HabitProfile is null)
        {
            return "Habits: no pet selected";
        }

        var habits = pet.HabitProfile;
        return $"Habits: nutrition {Math.Round(habits.Nutrition)}, hydration {Math.Round(habits.Hydration)}, exercise {Math.Round(habits.Exercise)}, hygiene {Math.Round(habits.Hygiene)}, stress {Math.Round(habits.Stress)}";
    }

    private static string BuildAgingSummary(PetActor? pet)
    {
        if (pet is null)
        {
            return "Aging: no pet selected";
        }

        var phase = pet.IsGhost ? "ghost" : pet.IsDead ? "passed" : pet.AgeStage.ToString().ToLowerInvariant();
        var rateHint = pet.HabitProfile?.Stress > 55 || (pet.ActiveConditions?.Count ?? 0) > 2
            ? "fast"
            : (pet.HabitProfile?.Nutrition ?? 0) > 75 && (pet.HabitProfile?.Exercise ?? 0) > 70
                ? "gentle"
                : "steady";
        return $"Aging: {phase}, bio age {Math.Round(pet.BiologicalAgeMinutes)}m, pace {rateHint}";
    }

    private static string BuildConditionSummary(PetActor? pet, GameContent content)
    {
        if (pet is null)
        {
            return "Conditions: no pet selected";
        }

        var conditions = pet.ActiveConditions ?? [];
        if (conditions.Count == 0)
        {
            return "Conditions: none";
        }

        var names = conditions
            .Select(condition =>
            {
                var definition = content.Conditions.FirstOrDefault(item => string.Equals(item.Id, condition.Id, StringComparison.OrdinalIgnoreCase));
                var name = definition?.DisplayName ?? condition.Id;
                return $"{name} {condition.Severity}";
            });
        return $"Conditions: {string.Join(" / ", names)}";
    }

    private void UpdateSliderLabels()
    {
        HungerValueText.Text = Math.Round(HungerSlider.Value).ToString();
        ThirstValueText.Text = Math.Round(ThirstSlider.Value).ToString();
        EnergyValueText.Text = Math.Round(EnergySlider.Value).ToString();
        CleanlinessValueText.Text = Math.Round(CleanlinessSlider.Value).ToString();
        AffectionValueText.Text = Math.Round(AffectionSlider.Value).ToString();
        ComfortValueText.Text = Math.Round(ComfortSlider.Value).ToString();
        HealthValueText.Text = Math.Round(HealthSlider.Value).ToString();
        FitnessValueText.Text = Math.Round(FitnessSlider.Value).ToString();
        BiologicalAgeValueText.Text = Math.Round(BiologicalAgeSlider.Value).ToString();
    }

    private void UpdateBasketDeleteButtonState()
    {
        var markedCount = _basketRows.Count(row => row.IsMarked);
        var hasSelectedRow = BasketGrid.SelectedItem is BasketRowItem;
        DeleteButton.IsEnabled = markedCount > 0 || hasSelectedRow || _basketRows.Count == 1;
        DeleteButton.Content = markedCount > 0 ? $"DELETE ({markedCount})" : "DELETE";
    }
}

internal sealed class BasketRowItem : INotifyPropertyChanged
{
    private bool _isMarked;

    public Guid Id { get; init; }

    public string Label { get; init; } = "";

    public string Host { get; init; } = "";

    public string CapturedLabel { get; init; } = "";

    public bool IsMarked
    {
        get => _isMarked;
        set
        {
            if (_isMarked == value)
            {
                return;
            }

            _isMarked = value;
            OnPropertyChanged();
        }
    }

    public static BasketRowItem From(BasketItem item, bool isMarked)
    {
        var host = Uri.TryCreate(item.Url, UriKind.Absolute, out var uri)
            ? uri.Host
            : "invalid";

        return new BasketRowItem
        {
            Id = item.Id,
            Label = string.IsNullOrWhiteSpace(item.Label) ? item.Url : item.Label,
            Host = host,
            CapturedLabel = item.CapturedAtUtc.ToLocalTime().ToString("MM/dd HH:mm"),
            IsMarked = isMarked
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal sealed record ActionOptionRowItem(
    string Key,
    string ActionId,
    string ItemId,
    string Label,
    string DetailLabel,
    string CategoryLabel,
    string ButtonLabel,
    ImageSource? Preview)
{
    public static ActionOptionRowItem From(string actionId, HabitatDisplayItem item, ImageSource? preview, string buttonLabel)
    {
        return new ActionOptionRowItem(
            $"{actionId}|{item.Id}",
            actionId,
            item.Id,
            item.Label,
            string.IsNullOrWhiteSpace(item.PreferenceHint) ? item.Purpose : $"{item.Purpose} - {item.PreferenceHint}",
            BuildCategoryLabel(item.CategoryFolder),
            buttonLabel,
            preview);
    }

    private static string BuildCategoryLabel(string categoryFolder)
    {
        return categoryFolder switch
        {
            "containers" => "Water",
            "care" => "Care",
            "toys_a" => "Play",
            "toys_b" => "Comfort",
            "food_herbivore" or "food_birds" or "food_predator" or "food_omnivore" => "Food",
            _ => categoryFolder.Replace('_', ' ')
        };
    }
}

internal sealed record PetTaskQueueRowItem(
    Guid Id,
    string Label,
    string ToolFamily,
    TaskCardStatus Status,
    string RawText,
    string NextAllowedAction,
    bool CanExecute,
    string AuditLogPath);

internal sealed record LocalDocumentResultRowItem(
    string Path,
    int Line,
    string Snippet,
    string Score)
{
    public static LocalDocumentResultRowItem From(LocalDocumentSnippet snippet)
    {
        return new LocalDocumentResultRowItem(
            snippet.RelativePath,
            snippet.LineNumber,
            snippet.SnippetText,
            snippet.Score.ToString("0.000000"));
    }
}

internal sealed record EvidenceSummaryRowItem(
    string PacketKind,
    int Count,
    string LastSeen,
    int MutationYesCount,
    int NetworkYesCount,
    int HostedAiYesCount,
    int LocalModelYesCount,
    int RefusalCount)
{
    public static EvidenceSummaryRowItem From(EvidenceSummaryKindRow row)
    {
        return new EvidenceSummaryRowItem(
            row.PacketKind,
            row.Count,
            row.LastSeenUtc.ToLocalTime().ToString("MM/dd HH:mm"),
            row.MutationYesCount,
            row.NetworkYesCount,
            row.HostedAiYesCount,
            row.LocalModelYesCount,
            row.RefusalCount);
    }
}

internal sealed record DevConditionOption(
    string Id,
    string Label)
{
    public override string ToString() => Label;
}
