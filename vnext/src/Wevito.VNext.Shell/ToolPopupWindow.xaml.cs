using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wevito.VNext.Contracts;

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

    public ToolPopupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => OverlayWindowStyler.Apply(this, clickThrough: false, noActivate: false);
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

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    internal void Render(CompanionState state, GameContent content, HabitatLoadout habitatLoadout, SpriteAssetService assetService, bool devToolsEnabled)
    {
        var toolId = string.IsNullOrWhiteSpace(state.ActiveTool.ToolId) ? "basket" : state.ActiveTool.ToolId;
        var showingBasket = string.Equals(toolId, "basket", StringComparison.OrdinalIgnoreCase);
        var showingSettings = string.Equals(toolId, "settings", StringComparison.OrdinalIgnoreCase);
        var showingDev = devToolsEnabled && string.Equals(toolId, "dev", StringComparison.OrdinalIgnoreCase);
        var showingActionMenu = string.Equals(toolId, "actions", StringComparison.OrdinalIgnoreCase);
        var showingAction = toolId.StartsWith("action:", StringComparison.OrdinalIgnoreCase);
        var actionId = showingAction ? toolId["action:".Length..] : string.Empty;
        var actionDefinition = showingAction
            ? content.Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.OrdinalIgnoreCase))
            : null;

        Title = showingBasket ? "Wevito Basket" : showingDev ? "Wevito Dev Tools" : showingActionMenu ? "Wevito Actions" : showingAction ? $"Wevito {actionDefinition?.DisplayName ?? "Action"}" : "Wevito Settings";
        PopupTitle.Text = showingBasket ? "Basket" : showingDev ? "Dev Tools" : showingActionMenu ? "Actions" : showingAction ? (actionDefinition?.DisplayName ?? "Action") : "Settings";
        BasketPanel.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        BasketButtons.Visibility = showingBasket ? Visibility.Visible : Visibility.Collapsed;
        ActionMenuPanel.Visibility = showingActionMenu ? Visibility.Visible : Visibility.Collapsed;
        ActionPanel.Visibility = showingAction ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = showingSettings ? Visibility.Visible : Visibility.Collapsed;
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
        else
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

        var phase = pet.BiologicalAgeMinutes >= 480 ? "aging" : pet.AgeStage.ToString().ToLowerInvariant();
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
        DeleteButton.IsEnabled = markedCount > 0;
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

internal sealed record DevConditionOption(
    string Id,
    string Label)
{
    public override string ToString() => Label;
}
