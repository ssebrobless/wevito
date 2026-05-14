using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.DevController;

public partial class MainWindow : Window
{
    private readonly DevControlClient _client = new();
    private readonly DispatcherTimer _refreshTimer;
    private DevControlSnapshot _snapshot = DevControlSnapshot.Empty(DateTimeOffset.UtcNow);
    private int _selectedSlotIndex;

    public MainWindow()
    {
        InitializeComponent();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _refreshTimer.Tick += async (_, _) => await RefreshSnapshotAsync(silent: true);
        Loaded += async (_, _) =>
        {
            SelectSlot(0);
            await RefreshSnapshotAsync(silent: false);
            _refreshTimer.Start();
        };
        Closed += (_, _) => _refreshTimer.Stop();
    }

    private async Task RefreshSnapshotAsync(bool silent)
    {
        try
        {
            var response = await _client.GetSnapshotAsync();
            ApplyResponse(response);
            ConnectionText.Text = "Connected";
            ConnectionText.Foreground = Brushes.LightGreen;
        }
        catch (Exception ex)
        {
            ConnectionText.Text = "Wevito not connected";
            ConnectionText.Foreground = Brushes.Orange;
            if (!silent)
            {
                StatusText.Text = $"Launch Wevito first. {ex.Message}";
            }
        }
    }

    private void ApplyResponse(DevControlResponseEnvelope response)
    {
        _snapshot = response.Snapshot;
        RenderSlots();
        PopulateOptions();
        PopulateAnimationOptions();
        PopulateIssueTagOptions();
        StatusText.Text = response.Message;
        StatusText.Foreground = response.Success ? Brushes.LightGreen : Brushes.Orange;
    }

    private void RenderSlots()
    {
        RenderSlotButton(Slot0Button, 0);
        RenderSlotButton(Slot1Button, 1);
        RenderSlotButton(Slot2Button, 2);
    }

    private void RenderSlotButton(Button button, int slotIndex)
    {
        var slot = _snapshot.Slots.FirstOrDefault(item => item.SlotIndex == slotIndex) ?? DevControlPetSlotSnapshot.Empty(slotIndex);
        button.Content = slot.DisplayText;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.VerticalContentAlignment = VerticalAlignment.Top;
        button.Background = slotIndex == _selectedSlotIndex ? new SolidColorBrush(Color.FromRgb(47, 70, 94)) : new SolidColorBrush(Color.FromRgb(31, 43, 55));
        button.BorderBrush = slotIndex == _selectedSlotIndex ? Brushes.LightSkyBlue : new SolidColorBrush(Color.FromRgb(95, 136, 161));
    }

    private void PopulateOptions()
    {
        SetItemsIfChanged(SpeciesCombo, _snapshot.Options.SpeciesIds, "goose");
        SetItemsIfChanged(StageCombo, _snapshot.Options.LifeStages, "baby");
        SetItemsIfChanged(GenderCombo, _snapshot.Options.Genders, "female");
        SetItemsIfChanged(ColorCombo, _snapshot.Options.ColorVariants, "blue");
    }

    private static void SetItemsIfChanged(ComboBox comboBox, IReadOnlyList<string> values, string fallback)
    {
        var selected = comboBox.SelectedItem as string ?? fallback;
        if (comboBox.Items.Count == values.Count && comboBox.Items.Cast<string>().SequenceEqual(values, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        comboBox.ItemsSource = values.Count > 0 ? values : [fallback];
        comboBox.SelectedItem = values.Contains(selected, StringComparer.OrdinalIgnoreCase) ? selected : comboBox.Items[0];
    }

    private void PopulateAnimationOptions()
    {
        var values = new[] { "idle", "walk", "eat", "happy", "sad", "sleep", "sick", "bathe", "waving", "jumping", "waiting", "review" };
        SetItemsIfChanged(AnimationCombo, values, "idle");
    }

    private void PopulateIssueTagOptions()
    {
        var values = new[] { "white_box", "cropped", "blurry", "wrong_scale", "static_animation", "bad_motion", "wrong_baseline", "duplicate_sprite", "bad_opacity", "missing_frame", "wrong_asset_source", "needs_redraw" };
        SetItemsIfChanged(IssueTagCombo, values, "needs_redraw");
    }

    private void SlotButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && int.TryParse(value, out var slotIndex))
        {
            SelectSlot(slotIndex);
        }
    }

    private void SelectSlot(int slotIndex)
    {
        _selectedSlotIndex = Math.Clamp(slotIndex, 0, 2);
        RenderSlots();
    }

    private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        if (slot.IsEmpty)
        {
            StatusText.Text = "Selected slot is already empty.";
            return;
        }

        var response = await _client.DeletePetAsync(new DevControlDeletePetRequest(_selectedSlotIndex, slot.PetId));
        ApplyResponse(response);
    }

    private async void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        var replace = false;
        if (!slot.IsEmpty)
        {
            replace = MessageBox.Show(
                this,
                "Replace this pet? This deletes the selected pet without creating a ghost, then spawns the selected test pet.",
                "Replace pet",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
            if (!replace)
            {
                return;
            }
        }

        var response = await _client.SpawnOrReplacePetAsync(new DevControlSpawnOrReplacePetRequest(
            _selectedSlotIndex,
            SelectedComboValue(SpeciesCombo, "goose"),
            SelectedComboValue(StageCombo, "baby"),
            SelectedComboValue(GenderCombo, "female"),
            SelectedComboValue(ColorCombo, "blue"),
            replace));
        ApplyResponse(response);
    }

    private async void ActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionId })
        {
            return;
        }

        var slot = SelectedSlot();
        var response = await _client.ApplyActionAsync(new DevControlApplyActionRequest(_selectedSlotIndex, slot.PetId, actionId));
        ApplyResponse(response);
    }

    private async void RoamButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        var response = await _client.RoamAsync(new DevControlRoamRequest(_selectedSlotIndex, slot.PetId, 10));
        ApplyResponse(response);
    }

    private async void ForceAnimationButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        var response = await _client.ForceAnimationAsync(new VisualQaForceAnimationRequest(
            _selectedSlotIndex,
            slot.PetId,
            SelectedComboValue(AnimationCombo, "idle"),
            ParseOptionalFrameIndex(),
            ParsePlaybackSpeed(),
            true));
        ApplyResponse(response);
    }

    private async void ClearAnimationButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        var response = await _client.ClearForcedAnimationAsync(new VisualQaClearForcedAnimationRequest(_selectedSlotIndex, slot.PetId));
        ApplyResponse(response);
    }

    private async void AssetSourceButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        var response = await _client.GetAssetSourceAsync(new VisualQaGetAssetSourceRequest(
            _selectedSlotIndex,
            slot.PetId,
            SelectedComboValue(AnimationCombo, "idle")));
        ApplyResponse(response);
    }

    private async void TagIssueButton_OnClick(object sender, RoutedEventArgs e)
    {
        var slot = SelectedSlot();
        var tag = SelectedComboValue(IssueTagCombo, "needs_redraw");
        var notes = string.IsNullOrWhiteSpace(IssueNotesTextBox.Text)
            ? $"Visual QA issue tagged as {tag}."
            : IssueNotesTextBox.Text.Trim();
        var response = await _client.TagIssueAsync(new VisualQaIssueTagRequest(
            _selectedSlotIndex,
            slot.PetId,
            [tag],
            notes,
            AttachCurrentScreenshot: false));
        ApplyResponse(response);
    }

    private async void ResetEggChoiceButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ResetSaveSandboxAsync("fresh_start_egg_choice", "Reset save to the fresh egg-choice screen?");
    }

    private async void EmptySaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ResetSaveSandboxAsync("empty_three_slots", "Clear all pets without creating ghosts?");
    }

    private async void SeedPetsButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ResetSaveSandboxAsync("three_random_alive_pets", "Replace the save with three visual QA test pets?");
    }

    private async Task ResetSaveSandboxAsync(string mode, string prompt)
    {
        var confirmed = MessageBox.Show(this, prompt, "Visual QA save sandbox", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        if (!confirmed)
        {
            return;
        }

        var response = await _client.ResetSaveSandboxAsync(new VisualQaResetSaveSandboxRequest(mode));
        ApplyResponse(response);
    }

    private DevControlPetSlotSnapshot SelectedSlot()
    {
        return _snapshot.Slots.FirstOrDefault(slot => slot.SlotIndex == _selectedSlotIndex)
            ?? DevControlPetSlotSnapshot.Empty(_selectedSlotIndex);
    }

    private static string SelectedComboValue(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem as string ?? fallback;
    }

    private int? ParseOptionalFrameIndex()
    {
        if (string.IsNullOrWhiteSpace(FrameIndexTextBox.Text))
        {
            return null;
        }

        return int.TryParse(FrameIndexTextBox.Text.Trim(), out var frameIndex)
            ? Math.Max(0, frameIndex)
            : null;
    }

    private double ParsePlaybackSpeed()
    {
        return double.TryParse(PlaybackSpeedTextBox.Text.Trim(), out var speed) && speed > 0
            ? speed
            : 1;
    }
}
