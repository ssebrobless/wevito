using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class ToolPopupWindow : Window
{
    private bool _closingSilently;
    private bool _suppressSettingEvents;
    private Guid? _lastRenderedDevPetId;
    private int _lastRenderedPetCount;
    private int _lastRenderedSpeciesCount;
    private List<BasketRowItem> _basketRows = [];
    private List<ActionOptionRowItem> _actionRows = [];
    private List<PetTaskQueueRowItem> _taskQueueRows = [];
    private bool _suppressTaskQueueSelection;

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

    public event Action<string, bool>? SettingChanged;

    internal event Func<DevToolCommand, Task>? DevToolCommandRequested;

    public event Func<string, string, Task>? ActionOptionRequested;

    public event Func<string, Task>? ActionMenuRequested;

    public event Func<string, Task>? PetCommandSubmitted;

    public event Func<Guid, TaskCardStatus, Task>? PetTaskStatusChangeRequested;

    public event Func<Guid, Task>? PetTaskPreviewRequested;

    public event Func<Guid, Task>? PetTaskExecutionRequested;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    internal void Render(CompanionState state, GameContent content, HabitatLoadout habitatLoadout, SpriteAssetService assetService, bool devToolsEnabled, PetCommandBarState? petCommandState = null)
    {
        var toolId = string.IsNullOrWhiteSpace(state.ActiveTool.ToolId) ? "basket" : state.ActiveTool.ToolId;
        var showingBasket = string.Equals(toolId, "basket", StringComparison.OrdinalIgnoreCase);
        var showingSettings = string.Equals(toolId, "settings", StringComparison.OrdinalIgnoreCase);
        var showingDev = devToolsEnabled && string.Equals(toolId, "dev", StringComparison.OrdinalIgnoreCase);
        var showingPetCommand = string.Equals(toolId, "helpers", StringComparison.OrdinalIgnoreCase);
        var showingActionMenu = string.Equals(toolId, "actions", StringComparison.OrdinalIgnoreCase);
        var showingAction = toolId.StartsWith("action:", StringComparison.OrdinalIgnoreCase);
        var actionId = showingAction ? toolId["action:".Length..] : string.Empty;
        var actionDefinition = showingAction
            ? content.Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.OrdinalIgnoreCase))
            : null;

        Title = showingBasket ? "Wevito Basket" : showingDev ? "Wevito Dev Tools" : showingPetCommand ? "Wevito PET TASKS" : showingActionMenu ? "Wevito Actions" : showingAction ? $"Wevito {actionDefinition?.DisplayName ?? "Action"}" : "Wevito Settings";
        PopupTitle.Text = showingBasket ? "Basket" : showingDev ? "Dev Tools" : showingPetCommand ? "PET TASKS" : showingActionMenu ? "Actions" : showingAction ? (actionDefinition?.DisplayName ?? "Action") : "Settings";
        BasketPanel.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        BasketButtons.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        ActionMenuPanel.Visibility = showingActionMenu ? Visibility.Visible : Visibility.Collapsed;
        ActionPanel.Visibility = showingAction ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = showingSettings ? Visibility.Visible : Visibility.Collapsed;
        PetCommandPanel.Visibility = showingPetCommand ? Visibility.Visible : Visibility.Collapsed;
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
            _actionRows = optionItems
                .Select(item => ActionOptionRowItem.From(actionDefinition.Id, item, ResolveActionOptionPreview(assetService, actionDefinition.Id, item)))
                .ToList();
            ActionGrid.ItemsSource = _actionRows;
            ActionSummaryText.Text = _actionRows.Count switch
            {
                0 => $"No specific {actionDefinition.DisplayName.ToLowerInvariant()} options are ready right now.",
                1 => $"Use the prepared {actionDefinition.DisplayName.ToLowerInvariant()} option below.",
                _ => $"Choose how to {actionDefinition.DisplayName.ToLowerInvariant()} from {_actionRows.Count} prepared options."
            };
        }
        else if (showingPetCommand)
        {
            RenderPetCommandPanel(petCommandState);
        }

        _suppressSettingEvents = true;
        CompactHudCheckBox.IsChecked = GetSettingBool(state, "compact_hud");
        ShowPetNamesCheckBox.IsChecked = GetSettingBool(state, "show_pet_names");
        ShowStatusSummaryCheckBox.IsChecked = GetSettingBool(state, "show_status_summary", true);
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

    private void RenderPetCommandPanel(PetCommandBarState? state)
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

    private static string FormatHelper(IReadOnlyList<PetHelperProfile> helpers, int index)
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
        return $"{helper.PetNameSnapshot} ({species})\n{FormatRole(helper.Role)} | {state}\n{task}";
    }

    private static string FormatRole(PetHelperRole role)
    {
        return role switch
        {
            PetHelperRole.SpriteReviewHelper => "Inspector - sprite QA",
            PetHelperRole.ChecklistHelper => "Builder - plans",
            PetHelperRole.ResearchHelper => "Scout - research",
            PetHelperRole.FileOrganizerHelper => "File organizer",
            PetHelperRole.BuildProofHelper => "Build proof",
            PetHelperRole.ReminderHelper => "Reminder",
            _ => role.ToString()
        };
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

    private static string FormatHelperState(PetHelperAvailability availability)
    {
        return availability switch
        {
            PetHelperAvailability.Drafting => "drafting",
            PetHelperAvailability.WaitingForApproval => "waiting",
            PetHelperAvailability.Running => "running",
            PetHelperAvailability.Reviewing => "reviewing",
            PetHelperAvailability.Blocked => "blocked",
            PetHelperAvailability.Done => "done",
            PetHelperAvailability.Failed => "failed",
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
            var parts = key.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && ActionOptionRequested is not null)
            {
                await ActionOptionRequested.Invoke(parts[0], parts[1]);
            }
        }
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

        SettingChanged?.Invoke(key, value);
    }

    private static bool GetSettingBool(CompanionState state, string key, bool defaultValue = false)
    {
        if (state.SettingsSnapshot.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
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

    private PetTaskArtifactPathResolution ResolveSelectedPetTaskArtifactPath()
    {
        if (PetTaskQueueComboBox.SelectedItem is not PetTaskQueueRowItem selectedRow)
        {
            return new PetTaskArtifactPathResolution(false, "", "", "No task card is selected.");
        }

        return PetTaskCardQueueService.ResolveArtifactReportPath(selectedRow.AuditLogPath, ResolveRepoRoot());
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
    ImageSource? Preview)
{
    public static ActionOptionRowItem From(string actionId, HabitatDisplayItem item, ImageSource? preview)
    {
        return new ActionOptionRowItem(
            $"{actionId}|{item.Id}",
            actionId,
            item.Id,
            item.Label,
            string.IsNullOrWhiteSpace(item.PreferenceHint) ? item.Purpose : $"{item.Purpose} - {item.PreferenceHint}",
            BuildCategoryLabel(item.CategoryFolder),
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

internal sealed record DevConditionOption(
    string Id,
    string Label)
{
    public override string ToString() => Label;
}
