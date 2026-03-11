using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public partial class HomePanelWindow : Window
{
    private bool _closingSilently;
    private bool _isHudVisible = true;
    private bool _isCompact;

    public HomePanelWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => OverlayWindowStyler.Apply(this, clickThrough: false, noActivate: false);
    }

    public event Func<Task>? TogglePinnedRequested;

    public event Func<Task>? ToggleBasketRequested;

    public event Func<Task>? CaptureClipboardRequested;

    public event Func<Task>? SaveRequested;

    public event Func<Task>? OpenSettingsRequested;

    public event Func<Task>? ToggleCompactRequested;

    public event Action<string>? ActionRequested;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public RectInt GetStageRect()
    {
        if (!IsLoaded)
        {
            return new RectInt(12, 76, 368, 152);
        }

        UpdateLayout();
        var topLeft = StageBorder.TranslatePoint(new Point(0, 0), this);
        return new RectInt(
            (int)Math.Round(topLeft.X),
            (int)Math.Round(topLeft.Y),
            (int)Math.Round(StageBorder.ActualWidth),
            (int)Math.Round(StageBorder.ActualHeight));
    }

    public void SetHudVisible(bool isVisible, bool isCompact)
    {
        if (_isHudVisible == isVisible && _isCompact == isCompact)
        {
            return;
        }

        _isHudVisible = isVisible;
        _isCompact = isCompact;

        var chromeVisibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        HeaderPanel.Visibility = chromeVisibility;
        ModeText.Visibility = chromeVisibility;
        StatusPanel.Visibility = chromeVisibility;
        FeedbackText.Visibility = chromeVisibility;
        HudPanel.Visibility = isVisible && !isCompact ? Visibility.Visible : Visibility.Collapsed;
        EnvironmentChrome.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        CompactButton.Content = isCompact ? "FULL" : "MIN";
        PanelBorder.Background = isVisible ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F61B2228")) : Brushes.Transparent;
        PanelBorder.BorderBrush = isVisible ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF72858B")) : Brushes.Transparent;
        PanelBorder.BorderThickness = isVisible ? new Thickness(1) : new Thickness(0);
        PanelBorder.Padding = isVisible ? new Thickness(12) : new Thickness(0);
        PanelBorder.CornerRadius = isVisible ? new CornerRadius(18) : new CornerRadius(0);

        TraceLog.Write("visibility", $"window=HomeChrome visibility={chromeVisibility} compact={isCompact}");
    }

    internal void Render(
        CompanionState state,
        EnvironmentDefinition environment,
        string feedbackText,
        SpriteAssetService assetService,
        IReadOnlyDictionary<string, double> needSnapshot,
        IReadOnlyList<PetStatusType> aggregateStatuses,
        IReadOnlyDictionary<string, bool> actionEnabled,
        IReadOnlyList<ItemDefinition> recommendedItems)
    {
        var compactHud = state.SettingsSnapshot.TryGetValue("compact_hud", out var compactValue) &&
                         bool.TryParse(compactValue, out var compactFlag) &&
                         compactFlag;
        var showPetNames = state.SettingsSnapshot.TryGetValue("show_pet_names", out var namesValue) &&
                           bool.TryParse(namesValue, out var namesFlag) &&
                           namesFlag;
        var showStatusSummary = !state.SettingsSnapshot.TryGetValue("show_status_summary", out var statusValue) ||
                                !bool.TryParse(statusValue, out var statusFlag) ||
                                statusFlag;

        ModeText.Text = state.Mode switch
        {
            CompanionMode.Focused => "Focused - HUD active",
            CompanionMode.Passive => "Passive - HUD hidden",
            CompanionMode.Pinned => "Pinned - interactive over desktop",
            _ => state.Mode.ToString()
        };
        SubtitleText.Text = state.Mode == CompanionMode.Passive
            ? "Pets are roaming the desktop band"
            : compactHud
                ? "Compact habitat controls"
                : "Pets are settled in the home environment";
        PinButton.Content = state.IsPinned ? "REL" : "PIN";
        BasketButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "basket", StringComparison.OrdinalIgnoreCase) ? "HIDE" : "BIN";
        SettingsButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "settings", StringComparison.OrdinalIgnoreCase) ? "DONE" : "SET";
        EnvironmentLabel.Text = environment.DisplayName;
        StatusText.Text = showStatusSummary
            ? $"{state.ActivePets.Count} pets - {state.BasketItems.Count} saved link(s)"
            : $"{environment.DisplayName} - {state.ActivePets.Count} companions";
        FeedbackText.Text = feedbackText;

        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(environment.PrimaryColor),
            (Color)ColorConverter.ConvertFromString(environment.SecondaryColor),
            90);
        StageGradient.Fill = brush;
        StageBackgroundImage.Source = assetService.GetEnvironment(string.IsNullOrWhiteSpace(environment.AssetId) ? environment.Id : environment.AssetId);
        CelestialImage.Source = assetService.GetCelestial(DateTimeOffset.Now, environment.IsNightEnvironment);

        HungerBar.Value = GetNeed(needSnapshot, "hunger");
        ThirstBar.Value = GetNeed(needSnapshot, "thirst");
        EnergyBar.Value = GetNeed(needSnapshot, "energy");
        CleanlinessBar.Value = GetNeed(needSnapshot, "cleanliness");

        RenderStatusIcons(aggregateStatuses, assetService);
        RenderRecommendedItems(recommendedItems, assetService);
        ApplyButtonState(FeedButton, FeedIcon, assetService.GetIcon("feed"), actionEnabled, "feed");
        ApplyButtonState(WaterButton, WaterIcon, assetService.GetIcon("water"), actionEnabled, "water");
        ApplyButtonState(RestButton, RestIcon, assetService.GetIcon("rest"), actionEnabled, "rest");
        ApplyButtonState(PlayButton, PlayIcon, assetService.GetIcon("exercise"), actionEnabled, "play");
        ApplyButtonState(GroomButton, GroomIcon, assetService.GetIcon("groom"), actionEnabled, "groom");
        ApplyButtonState(BathButton, BathIcon, assetService.GetIcon("bathe"), actionEnabled, "bath");
        ApplyButtonState(MedicineButton, MedicineIcon, assetService.GetIcon("medicine"), actionEnabled, "medicine");
        ApplyButtonState(DoctorButton, DoctorIcon, assetService.GetIcon("doctor"), actionEnabled, "doctor");
        ApplyButtonState(HomeButton, HomeIcon, assetService.GetIcon("callback"), actionEnabled, "home");

        HomePetCanvas.Children.Clear();
        var stageRect = GetStageRect();
        foreach (var pet in state.ActivePets.Where(pet => state.Mode != CompanionMode.Passive || pet.BehaviorState == PetBehaviorState.Roaming))
        {
            var source = assetService.GetPetFrame(pet, DateTimeOffset.UtcNow);
            if (source is null)
            {
                continue;
            }

            var scale = assetService.GetPetScale(pet);
            var width = 28 * scale;
            var height = 24 * scale;
            var image = CreateSpriteImage(source, width, height, pet.FacingDirection == PetFacingDirection.Left);
            var localX = pet.CurrentX - Left - stageRect.X - width / 2;
            var localY = pet.CurrentY - Top - stageRect.Y - height;
            Canvas.SetLeft(image, localX);
            Canvas.SetTop(image, localY);
            HomePetCanvas.Children.Add(image);

            if (!showPetNames || state.Mode == CompanionMode.Passive)
            {
                continue;
            }

            var label = new TextBlock
            {
                Text = $"{pet.Name} ({pet.AgeStage})",
                Foreground = Brushes.White,
                FontSize = 10
            };
            Canvas.SetLeft(label, localX - 2);
            Canvas.SetTop(label, localY - 14);
            HomePetCanvas.Children.Add(label);
        }
    }

    internal void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    internal async Task<bool> TryInvokeOverlayClickAsync(PointInt screenPosition)
    {
        if (Visibility != Visibility.Visible)
        {
            return false;
        }

        UpdateLayout();
        var localPoint = PointFromScreen(new Point(screenPosition.X, screenPosition.Y));

        if (await TryInvokeButtonAsync(PinButton, localPoint, TogglePinnedRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(BasketButton, localPoint, ToggleBasketRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(CaptureButton, localPoint, CaptureClipboardRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(SettingsButton, localPoint, OpenSettingsRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(CompactButton, localPoint, ToggleCompactRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(SaveButton, localPoint, SaveRequested))
        {
            return true;
        }

        if (TryInvokeActionButton(FeedButton, localPoint, "feed")) { return true; }
        if (TryInvokeActionButton(WaterButton, localPoint, "water")) { return true; }
        if (TryInvokeActionButton(RestButton, localPoint, "rest")) { return true; }
        if (TryInvokeActionButton(PlayButton, localPoint, "play")) { return true; }
        if (TryInvokeActionButton(GroomButton, localPoint, "groom")) { return true; }
        if (TryInvokeActionButton(BathButton, localPoint, "bath")) { return true; }
        if (TryInvokeActionButton(MedicineButton, localPoint, "medicine")) { return true; }
        if (TryInvokeActionButton(DoctorButton, localPoint, "doctor")) { return true; }
        if (TryInvokeActionButton(HomeButton, localPoint, "home")) { return true; }

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

    private void RenderStatusIcons(IReadOnlyList<PetStatusType> aggregateStatuses, SpriteAssetService assetService)
    {
        StatusIconPanel.Children.Clear();
        foreach (var status in aggregateStatuses.Take(6))
        {
            var source = assetService.GetStatusIcon(status.ToString().ToLowerInvariant());
            if (source is null)
            {
                continue;
            }

            var image = new Image
            {
                Source = source,
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = status.ToString()
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            StatusIconPanel.Children.Add(image);
        }
    }

    private void RenderRecommendedItems(IReadOnlyList<ItemDefinition> recommendedItems, SpriteAssetService assetService)
    {
        RecommendedItemPanel.Children.Clear();
        foreach (var item in recommendedItems.Take(4))
        {
            var source = assetService.GetIcon(item.IconId);
            if (source is null)
            {
                continue;
            }

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var image = new Image
            {
                Source = source,
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = item.DisplayName
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            stack.Children.Add(image);
            stack.Children.Add(new TextBlock
            {
                Text = item.DisplayName,
                Foreground = Brushes.Gainsboro,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            RecommendedItemPanel.Children.Add(stack);
        }
    }

    private static void ApplyButtonState(
        Button button,
        Image icon,
        ImageSource? iconSource,
        IReadOnlyDictionary<string, bool> actionEnabled,
        string actionId)
    {
        icon.Source = iconSource;
        button.IsEnabled = actionEnabled.TryGetValue(actionId, out var isEnabled) ? isEnabled : true;
    }

    private static Image CreateSpriteImage(ImageSource source, double width, double height, bool flipX)
    {
        var image = new Image
        {
            Source = source,
            Width = width,
            Height = height,
            Stretch = Stretch.Fill,
            RenderTransformOrigin = new Point(0.5, 0.5)
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        if (flipX)
        {
            image.RenderTransform = new ScaleTransform(-1, 1);
        }

        return image;
    }

    private static double GetNeed(IReadOnlyDictionary<string, double> needSnapshot, string key)
    {
        return needSnapshot.TryGetValue(key, out var value) ? value : 0;
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

    private bool TryInvokeActionButton(Button button, Point localPoint, string actionId)
    {
        if (!IsPointInside(button, localPoint))
        {
            return false;
        }

        ActionRequested?.Invoke(actionId);
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

    private async void PinButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (TogglePinnedRequested is not null)
        {
            await TogglePinnedRequested.Invoke();
        }
    }

    private async void BasketButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleBasketRequested is not null)
        {
            await ToggleBasketRequested.Invoke();
        }
    }

    private async void CaptureButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CaptureClipboardRequested is not null)
        {
            await CaptureClipboardRequested.Invoke();
        }
    }

    private async void SettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OpenSettingsRequested is not null)
        {
            await OpenSettingsRequested.Invoke();
        }
    }

    private async void CompactButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleCompactRequested is not null)
        {
            await ToggleCompactRequested.Invoke();
        }
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SaveRequested is not null)
        {
            await SaveRequested.Invoke();
        }
    }

    private void FeedButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("feed");

    private void WaterButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("water");

    private void RestButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("rest");

    private void PlayButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("play");

    private void GroomButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("groom");

    private void BathButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("bath");

    private void MedicineButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("medicine");

    private void DoctorButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("doctor");

    private void HomeButton_OnClick(object sender, RoutedEventArgs e) => ActionRequested?.Invoke("home");
}
