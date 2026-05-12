using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        Canvas.SetZIndex(StageBackdropCanvas, HabitatDepthOrder.GetZIndex(DepthBand.Backdrop));
        Canvas.SetZIndex(StagePropCanvas, HabitatDepthOrder.GetZIndex(DepthBand.GroundContact));
        Canvas.SetZIndex(HomePetCanvas, HabitatDepthOrder.GetZIndex(DepthBand.PetBody));
        Canvas.SetZIndex(StageDecorationCanvas, HabitatDepthOrder.GetZIndex(DepthBand.NearOccluder));
        SourceInitialized += (_, _) => OverlayWindowStyler.Apply(this, clickThrough: false, noActivate: false);
    }

    public event Func<Task>? TogglePinnedRequested;

    public event Func<Task>? ToggleActionsRequested;

    public event Func<Task>? ToggleWebToolsRequested;

    public event Func<Task>? ToggleBasketRequested;

    public event Func<Task>? ToggleHelpersRequested;

    public event Func<Task>? OpenSpriteWorkflowV2Requested;

    public event Func<Task>? OpenCreativeLearningLabRequested;

    public event Func<Task>? SaveRequested;

    public event Func<Task>? OpenSettingsRequested;

    public event Func<Task>? ToggleCompactRequested;

    public event Func<Task>? ToggleDevRequested;

    public event Action<string>? ActionRequested;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public RectInt GetStageRect()
    {
        if (!IsLoaded)
        {
        return new RectInt(12, 292, 612, 300);
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
        StatusCard.Visibility = chromeVisibility;
        FeedbackText.Visibility = chromeVisibility;
        HudCard.Visibility = isVisible && !isCompact ? Visibility.Visible : Visibility.Collapsed;
        CompactButton.Content = isCompact ? "FULL" : "MIN";
        PanelBorder.Background = isVisible ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F61B2228")) : Brushes.Transparent;
        PanelBorder.BorderBrush = isVisible ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF72858B")) : Brushes.Transparent;
        PanelBorder.BorderThickness = isVisible ? new Thickness(1) : new Thickness(0);
        PanelBorder.Padding = isVisible ? new Thickness(12) : new Thickness(0);
        PanelBorder.CornerRadius = isVisible ? new CornerRadius(18) : new CornerRadius(0);

        TraceLog.Write("visibility", $"window=HomeChrome visibility={chromeVisibility} compact={isCompact}");
    }

    public void SetDevToolsVisible(bool isVisible)
    {
        DevButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    internal void Render(
        CompanionState state,
        EnvironmentDefinition environment,
        string feedbackText,
        SpriteAssetService assetService,
        IReadOnlyDictionary<string, double> needSnapshot,
        IReadOnlyList<PetStatusType> aggregateStatuses,
        IReadOnlyDictionary<string, bool> actionEnabled,
        HabitatLoadout habitatLoadout)
    {
        var compactHud = state.SettingsSnapshot.TryGetValue("compact_hud", out var compactValue) &&
                         bool.TryParse(compactValue, out var compactFlag) &&
                         compactFlag;
        var webToolsVisible = state.SettingsSnapshot.TryGetValue("webtools_visible", out var webToolsValue) &&
                              bool.TryParse(webToolsValue, out var webToolsFlag) &&
                              webToolsFlag;
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
        ActionsButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "actions", StringComparison.OrdinalIgnoreCase) ? "ACTIONS ON" : "ACTIONS";
        WebToolsButton.Content = webToolsVisible ? "HIDE" : "TOOLS";
        SettingsButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "settings", StringComparison.OrdinalIgnoreCase) ? "CLOSE" : "SETTINGS";
        DevButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "dev", StringComparison.OrdinalIgnoreCase) ? "CLOSE" : "DEV";
        WebToolsBar.Visibility = _isHudVisible && webToolsVisible ? Visibility.Visible : Visibility.Collapsed;
        LinkBinTabButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "basket", StringComparison.OrdinalIgnoreCase) ? "LINK BIN ACTIVE" : "LINK BIN";
        LinkBinTabButton.FontWeight = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "basket", StringComparison.OrdinalIgnoreCase)
            ? FontWeights.SemiBold
            : FontWeights.Normal;
        HelperTabButton.Content = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "helpers", StringComparison.OrdinalIgnoreCase) ? "PET TASKS ACTIVE" : "PET TASKS";
        HelperTabButton.FontWeight = state.ActiveTool.IsOpen && string.Equals(state.ActiveTool.ToolId, "helpers", StringComparison.OrdinalIgnoreCase)
            ? FontWeights.SemiBold
            : FontWeights.Normal;
        WebToolSlot3Button.Content = "SPRITES";
        WebToolSlot4Button.Content = "LAB";
        WebToolSlot5Button.Content = "EMPTY";
        WebToolsHintText.Text = state.BasketItems.Count switch
        {
            0 => "Link Bin, Pet Tasks, Sprites, and Lab are ready. One slot is reserved for future tools.",
            1 => "Link Bin has 1 saved link. Pet Tasks can draft helper assignments.",
            _ => $"Link Bin has {state.BasketItems.Count} saved links. Pet Tasks can draft helper assignments."
        };
        var focusPet = state.ActivePets.FirstOrDefault();
        StatusText.Text = showStatusSummary
            ? BuildLeadPetSummary(focusPet, state.BasketItems.Count)
            : $"{environment.DisplayName} - {state.ActivePets.Count} companions";
        FeedbackText.Text = feedbackText;

        StageBackgroundImage.Source = null;
        StageGradient.Visibility = Visibility.Collapsed;
        CelestialImage.Source = assetService.GetCelestial(DateTimeOffset.Now, environment.IsNightEnvironment);

        HungerBar.Value = GetNeed(needSnapshot, "hunger");
        ThirstBar.Value = GetNeed(needSnapshot, "thirst");
        EnergyBar.Value = GetNeed(needSnapshot, "energy");
        CleanlinessBar.Value = GetNeed(needSnapshot, "cleanliness");

        RenderStatusIcons(aggregateStatuses, assetService);
        RenderRecommendedItems(habitatLoadout.RecommendedItems, assetService);
        ApplyButtonState(FeedButton, FeedIcon, assetService.GetIcon("feed"), actionEnabled, "feed");
        ApplyButtonState(WaterButton, WaterIcon, assetService.GetIcon("water"), actionEnabled, "water");
        ApplyButtonState(RestButton, RestIcon, assetService.GetIcon("rest"), actionEnabled, "rest");
        ApplyButtonState(PlayButton, PlayIcon, assetService.GetIcon("exercise"), actionEnabled, "play");
        ApplyButtonState(GroomButton, GroomIcon, assetService.GetIcon("groom"), actionEnabled, "groom");
        ApplyButtonState(BathButton, BathIcon, assetService.GetIcon("bathe"), actionEnabled, "bath");
        ApplyButtonState(MedicineButton, MedicineIcon, assetService.GetIcon("medicine"), actionEnabled, "medicine");
        ApplyButtonState(DoctorButton, DoctorIcon, assetService.GetIcon("doctor"), actionEnabled, "doctor");
        ApplyButtonState(HomeButton, HomeIcon, assetService.GetIcon("callback"), actionEnabled, "home");

        var stageSpecs = RenderStageProps(environment, habitatLoadout, focusPet, assetService);
        HomePetCanvas.Children.Clear();
        var stageRect = GetStageRect();
        var now = DateTimeOffset.UtcNow;
        var renderedInteractionCue = false;
        foreach (var pet in state.ActivePets.Where(pet => state.Mode != CompanionMode.Passive || pet.BehaviorState == PetBehaviorState.Roaming))
        {
            RenderMemorialIfActive(pet, now, stageRect, assetService);
            var ghostFrame = pet.IsGhost ? assetService.GetGhostPetFrame(pet, now) : null;
            var source = ghostFrame ?? assetService.GetPetFrame(pet, now);
            if (source is null)
            {
                continue;
            }

            var usesGenericGhostOverlay = pet.IsGhost && ghostFrame is null;
            var scale = assetService.GetPetScale(pet);
            var bitmap = source as BitmapSource;
            var width = (bitmap?.PixelWidth ?? 28) * scale;
            var height = (bitmap?.PixelHeight ?? 24) * scale;
            var image = CreateSpriteImage(source, width, height, pet.FacingDirection == PetFacingDirection.Left);
            image.Opacity = pet.IsGhost ? (usesGenericGhostOverlay ? 0.44 : 0.82) : pet.IsDead ? 0.58 : 1.0;
            var localX = Math.Round(pet.CurrentX - Left - stageRect.X - width / 2);
            var localY = Math.Round(pet.CurrentY - Top - stageRect.Y - height);
            if (focusPet is not null &&
                pet.Id == focusPet.Id &&
                TryResolveInteractionVisual(pet, stageSpecs, now, width, height, out var interactionX, out var interactionY, out var cueText, out var cuePoint))
            {
                localX = interactionX;
                localY = interactionY;
                if (!renderedInteractionCue && !string.IsNullOrWhiteSpace(cueText))
                {
                    RenderInteractionCue(cueText!, cuePoint);
                    renderedInteractionCue = true;
                }
            }
            var shadow = CreatePetContactShadow(pet, width, height);
            Canvas.SetLeft(shadow, Math.Round(localX + width * 0.12));
            Canvas.SetTop(shadow, Math.Round(localY + height - shadow.Height * 0.58));
            Canvas.SetZIndex(shadow, HabitatDepthOrder.GetShadowZIndex(ContactShadowMode.Soft));
            HomePetCanvas.Children.Add(shadow);

            Canvas.SetLeft(image, localX);
            Canvas.SetTop(image, localY);
            Canvas.SetZIndex(image, HabitatDepthOrder.GetZIndex(DepthBand.PetBody));
            HomePetCanvas.Children.Add(image);

            if (pet.AgeStage == PetAgeStage.Senior && !pet.IsGhost)
            {
                var seniorTint = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8F9AA0")),
                    Opacity = 0.16,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(seniorTint, localX);
                Canvas.SetTop(seniorTint, localY);
                Canvas.SetZIndex(seniorTint, HabitatDepthOrder.GetZIndex(DepthBand.PetBody) + 1);
                HomePetCanvas.Children.Add(seniorTint);
            }

            if (usesGenericGhostOverlay)
            {
                RenderGenericGhostOverlay(assetService, localX, localY, width, height);
            }

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
            Canvas.SetLeft(label, Math.Round(localX - 2));
            Canvas.SetTop(label, Math.Round(localY - 14));
            Canvas.SetZIndex(label, HabitatDepthOrder.GetZIndex(DepthBand.UiOverlay));
            HomePetCanvas.Children.Add(label);
        }
    }

    private void RenderMemorialIfActive(PetActor pet, DateTimeOffset now, RectInt stageRect, SpriteAssetService assetService)
    {
        if (string.IsNullOrWhiteSpace(pet.MemorialObjectId) ||
            pet.MemorialExpiresAtUtc is null ||
            pet.MemorialExpiresAtUtc <= now)
        {
            return;
        }

        var source = assetService.GetItem("toys_b", pet.MemorialObjectId);
        if (source is null)
        {
            return;
        }

        const double size = 28;
        var image = CreateSpriteImage(source, size, size, flipX: false);
        image.Opacity = 0.92;
        var x = Math.Round(pet.MemorialX - Left - stageRect.X - size / 2);
        var y = Math.Round(pet.MemorialY - Top - stageRect.Y - size);
        Canvas.SetLeft(image, x);
        Canvas.SetTop(image, y);
        Canvas.SetZIndex(image, HabitatDepthOrder.GetZIndex(DepthBand.GroundContact) + 1);
        HomePetCanvas.Children.Add(image);
    }

    private void RenderGenericGhostOverlay(SpriteAssetService assetService, double localX, double localY, double width, double height)
    {
        var icon = assetService.GetIcon("ghost");
        if (icon is not null)
        {
            var iconSize = Math.Max(18, Math.Min(32, width * 0.46));
            var ghost = CreateSpriteImage(icon, iconSize, iconSize, flipX: false);
            ghost.Opacity = 0.78;
            Canvas.SetLeft(ghost, Math.Round(localX + (width - iconSize) / 2));
            Canvas.SetTop(ghost, Math.Round(localY - iconSize * 0.35));
            Canvas.SetZIndex(ghost, HabitatDepthOrder.GetZIndex(DepthBand.PetBody) + 2);
            HomePetCanvas.Children.Add(ghost);
            return;
        }

        var halo = new Ellipse
        {
            Width = Math.Max(20, width * 0.58),
            Height = Math.Max(18, height * 0.38),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDDF7")),
            StrokeThickness = 2,
            Opacity = 0.62,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(halo, Math.Round(localX + (width - halo.Width) / 2));
        Canvas.SetTop(halo, Math.Round(localY + height * 0.08));
        Canvas.SetZIndex(halo, HabitatDepthOrder.GetZIndex(DepthBand.PetBody) + 2);
        HomePetCanvas.Children.Add(halo);
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

        if (await TryInvokeButtonAsync(ActionsButton, localPoint, ToggleActionsRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(WebToolsButton, localPoint, ToggleWebToolsRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(LinkBinTabButton, localPoint, ToggleBasketRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(HelperTabButton, localPoint, ToggleHelpersRequested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(WebToolSlot3Button, localPoint, OpenSpriteWorkflowV2Requested))
        {
            return true;
        }

        if (await TryInvokeButtonAsync(WebToolSlot4Button, localPoint, OpenCreativeLearningLabRequested))
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

        if (await TryInvokeButtonAsync(DevButton, localPoint, ToggleDevRequested))
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
        _ = assetService;
        StatusIconPanel.Children.Clear();
        foreach (var status in aggregateStatuses.Take(6))
        {
            var badge = new Border
            {
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(6, 2, 6, 2),
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetStatusBackgroundColor(status))),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(GetStatusBorderColor(status))),
                BorderThickness = new Thickness(1),
                ToolTip = status.ToString()
            };

            badge.Child = new TextBlock
            {
                Text = GetStatusLabel(status),
                Foreground = Brushes.White,
                FontSize = 9,
                FontWeight = FontWeights.SemiBold
            };

            StatusIconPanel.Children.Add(badge);
        }
    }

    private void RenderRecommendedItems(IReadOnlyList<HabitatDisplayItem> recommendedItems, SpriteAssetService assetService)
    {
        RecommendedItemPanel.Children.Clear();
        foreach (var item in recommendedItems.Take(6))
        {
            var source = ResolveRecommendationIcon(assetService, item);
            if (source is null)
            {
                continue;
            }

            var stack = new Border
            {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(6, 4, 6, 4),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.IsUrgent ? "#AAE3B05F" : "#554D5961")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.IsUrgent ? "#4037271A" : "#28141B20"))
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var image = new Image
            {
                Source = source,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = item.Label
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            panel.Children.Add(image);
            var textPanel = new StackPanel();
            textPanel.Children.Add(new TextBlock
            {
                Text = item.Label,
                Foreground = Brushes.Gainsboro,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(item.PreferenceHint) ? item.Purpose : $"{item.Purpose} - {item.PreferenceHint}",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.IsUrgent ? "#FFF3C969" : "#FFB8C4C8")),
                FontSize = 9
            });
            panel.Children.Add(textPanel);
            stack.Child = panel;
            RecommendedItemPanel.Children.Add(stack);
        }
    }

    private static string GetStatusLabel(PetStatusType status)
    {
        return status switch
        {
            PetStatusType.Hungry => "Hungry",
            PetStatusType.Thirsty => "Thirsty",
            PetStatusType.Sleepy => "Sleepy",
            PetStatusType.Sick => "Sick",
            PetStatusType.Happy => "Happy",
            PetStatusType.Dirty => "Dirty",
            PetStatusType.Lonely => "Lonely",
            PetStatusType.Comforted => "Safe",
            PetStatusType.Dead => "Passed",
            PetStatusType.Ghost => "Ghost",
            _ => status.ToString()
        };
    }

    private static string GetStatusBackgroundColor(PetStatusType status)
    {
        return status switch
        {
            PetStatusType.Hungry => "#403A2914",
            PetStatusType.Thirsty => "#1F2F4760",
            PetStatusType.Sleepy => "#2B314A61",
            PetStatusType.Sick => "#4A3A2323",
            PetStatusType.Happy => "#243E4A2A",
            PetStatusType.Dirty => "#45352E22",
            PetStatusType.Lonely => "#3F3B2C46",
            PetStatusType.Comforted => "#27403A2E",
            PetStatusType.Dead => "#34252E38",
            PetStatusType.Ghost => "#26364F5A",
            _ => "#20313A"
        };
    }

    private static string GetStatusBorderColor(PetStatusType status)
    {
        return status switch
        {
            PetStatusType.Hungry => "#AAE3B05F",
            PetStatusType.Thirsty => "#7AA9D4",
            PetStatusType.Sleepy => "#8C9EC4",
            PetStatusType.Sick => "#C98686",
            PetStatusType.Happy => "#7FCB8A",
            PetStatusType.Dirty => "#A8906B",
            PetStatusType.Lonely => "#B08CC4",
            PetStatusType.Comforted => "#7FB892",
            PetStatusType.Dead => "#C4A7B6",
            PetStatusType.Ghost => "#A8D2FF",
            _ => "#556872"
        };
    }

    private static ImageSource? ResolveRecommendationIcon(SpriteAssetService assetService, HabitatDisplayItem item)
    {
        var iconId = item.ActionId switch
        {
            "feed" => "feed",
            "water" => "water",
            "rest" => "rest",
            "play" => "exercise",
            "groom" => "groom",
            "bath" => "bathe",
            "medicine" => "medicine",
            "doctor" => "doctor",
            "home" => "callback",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(iconId))
        {
            var icon = assetService.GetIcon(iconId);
            if (icon is not null)
            {
                return icon;
            }
        }

        return assetService.GetItem(item.CategoryFolder, item.AssetId);
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
            RenderTransformOrigin = new Point(0.5, 0.5),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);
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

    private IReadOnlyList<StagePropSpec> RenderStageProps(
        EnvironmentDefinition environment,
        HabitatLoadout habitatLoadout,
        PetActor? focusPet,
        SpriteAssetService assetService)
    {
        StagePropCanvas.Children.Clear();
        StageBackdropCanvas.Children.Clear();
        StageDecorationCanvas.Children.Clear();
        RenderStageBackdrop(focusPet);
        AddGroundShade(focusPet);

        var specs = GetStagePropSpecs(environment, habitatLoadout.DynamicStageProps, focusPet);
        TraceLog.Write("stage-props", $"{focusPet?.SpeciesId ?? "none"}:{string.Join(",", specs.Select(spec => $"{spec.CategoryFolder}/{spec.AssetId}"))}");
        var scaledSpecs = new List<StagePropSpec>(specs.Count);
        foreach (var spec in specs)
        {
            var scaledSpec = ScaleStagePropSpec(spec, focusPet);
            scaledSpecs.Add(scaledSpec);
            UIElement? element = null;
            if (string.Equals(spec.CategoryFolder, "containers", StringComparison.OrdinalIgnoreCase))
            {
                element = CreateContainerVisual(scaledSpec);
            }
            else if (TryCreateStageSafePropVisual(scaledSpec, out var stageSafeProp))
            {
                element = stageSafeProp;
            }
            else
            {
                var source = assetService.GetItem(spec.CategoryFolder, spec.AssetId);
                if (source is not null)
                {
                    element = CreateSpriteImage(source, scaledSpec.Width, scaledSpec.Height, false);
                }
            }

            if (element is null)
            {
                continue;
            }

            if (string.Equals(scaledSpec.SlotId, "primary", StringComparison.OrdinalIgnoreCase) &&
                scaledSpec.ContactShadowMode != ContactShadowMode.None)
            {
                var shadow = CreateAnchorContactShadow(scaledSpec);
                Canvas.SetLeft(shadow, Math.Round(scaledSpec.Left + (scaledSpec.Width - shadow.Width) / 2.0));
                Canvas.SetTop(shadow, Math.Round(scaledSpec.Top + scaledSpec.Height - shadow.Height * 0.62));
                Canvas.SetZIndex(shadow, HabitatDepthOrder.GetShadowZIndex(scaledSpec.ContactShadowMode));
                StagePropCanvas.Children.Add(shadow);
            }

            element.Opacity = scaledSpec.Opacity;
            Canvas.SetLeft(element, Math.Round(scaledSpec.Left));
            Canvas.SetTop(element, Math.Round(scaledSpec.Top));
            Canvas.SetZIndex(element, HabitatDepthOrder.GetZIndex(scaledSpec.DepthBand));
            if (scaledSpec.DepthBand == DepthBand.NearOccluder)
            {
                StageDecorationCanvas.Children.Add(element);
            }
            else
            {
                StagePropCanvas.Children.Add(element);
            }
        }

        return scaledSpecs;
    }

    private void RenderStageBackdrop(PetActor? focusPet)
    {
        if (focusPet is null)
        {
            return;
        }

        switch (focusPet.SpeciesId)
        {
            case "rat":
                AddBackdropElement(CreateBackdropPatch(300, 126, "#2E3123", "#516448", 0.34), 28, 128);
                AddBackdropElement(CreateBackdropPatch(222, 72, "#443521", "#6B5A40", 0.42), 72, 166);
                AddBackdropElement(CreateGrassCluster(84, 48, "#728948", "#8FA25A"), 68, 186);
                AddBackdropElement(CreateGrassCluster(70, 40, "#68813C", "#81994D"), 286, 194);
                break;
            case "crow":
                AddBackdropElement(CreateBackdropPatch(268, 86, "#2F353E", "#56606A", 0.32), 68, 166);
                AddBackdropElement(CreatePebbleCluster(92, 44, "#6E7882", "#4E565F", "#8C959C"), 74, 196);
                AddBackdropElement(CreateTwigScatter(108, 34, "#7A5A3D", "#8F6C48"), 250, 196);
                break;
            case "fox":
                AddBackdropElement(CreateBackdropPatch(308, 118, "#33442D", "#5E7356", 0.3), 32, 138);
                AddBackdropElement(CreateBackdropPatch(218, 70, "#5A4631", "#7D6446", 0.34), 138, 176);
                AddBackdropElement(CreateGrassCluster(96, 54, "#75904A", "#97AF61"), 54, 182);
                AddBackdropElement(CreateLeafScatter(96, 40, "#A88B4C", "#6A7636", "#8A6438"), 268, 194);
                break;
            case "snake":
                AddBackdropElement(CreateBackdropPatch(314, 108, "#3D4332", "#626A56", 0.28), 40, 146);
                AddBackdropElement(CreateBackdropPatch(184, 58, "#787169", "#9A9389", 0.38), 166, 182);
                AddBackdropElement(CreatePebbleCluster(110, 42, "#8B8479", "#6B665E", "#A29B90"), 70, 196);
                AddBackdropElement(CreateReedCluster(70, 54, "#7A8F4D", "#90A95C"), 300, 176);
                break;
            case "deer":
                AddBackdropElement(CreateBackdropPatch(324, 124, "#36502F", "#64845D", 0.28), 26, 134);
                AddBackdropElement(CreateGrassCluster(114, 58, "#7D9A4B", "#9FBA62"), 42, 182);
                AddBackdropElement(CreateGrassCluster(96, 52, "#769446", "#91AD57"), 268, 184);
                AddBackdropElement(CreateFlowerScatter(76, 34, "#D8D6CA", "#F1C989", "#E7E8F2"), 176, 202);
                break;
            case "frog":
                AddBackdropElement(CreateBackdropPatch(320, 112, "#355342", "#5E8E78", 0.28), 30, 142);
                AddBackdropElement(CreateRipplePool(164, 58, "#6EA7AD", "#90C5CB"), 84, 190);
                AddBackdropElement(CreateLilyCluster(106, 54, "#688D44", "#4E6E33"), 240, 176);
                AddBackdropElement(CreateReedCluster(78, 60, "#829F55", "#9FC26A"), 36, 172);
                break;
            case "pigeon":
                AddBackdropElement(CreateBackdropPatch(286, 90, "#3A4047", "#5E646C", 0.28), 52, 160);
                AddBackdropElement(CreatePebbleCluster(112, 42, "#7B838A", "#5C636A", "#9AA0A5"), 76, 196);
                AddBackdropElement(CreateTwigScatter(88, 28, "#8C6C49", "#6B5038"), 250, 202);
                break;
            case "raccoon":
                AddBackdropElement(CreateBackdropPatch(322, 116, "#324234", "#5F765E", 0.28), 24, 140);
                AddBackdropElement(CreateBackdropPatch(208, 68, "#4C3929", "#6F5944", 0.3), 148, 178);
                AddBackdropElement(CreateFernCluster(82, 56, "#6E8C47", "#8EA95C"), 44, 176);
                AddBackdropElement(CreatePebbleCluster(82, 34, "#7E786F", "#5C5750", "#A39B8F"), 278, 202);
                break;
            case "squirrel":
                AddBackdropElement(CreateBackdropPatch(308, 112, "#34452E", "#61755B", 0.28), 28, 142);
                AddBackdropElement(CreateGrassCluster(80, 44, "#739149", "#95AF5E"), 44, 190);
                AddBackdropElement(CreateLeafScatter(102, 42, "#A58347", "#6A7131", "#7D5B36"), 254, 194);
                AddBackdropElement(CreateTwigScatter(80, 28, "#7B5C40", "#966D4A"), 166, 204);
                break;
            case "goose":
                AddBackdropElement(CreateBackdropPatch(328, 116, "#3B594A", "#688C7A", 0.26), 22, 140);
                AddBackdropElement(CreateRipplePool(156, 54, "#78AEB7", "#9BD0D8"), 42, 192);
                AddBackdropElement(CreateReedCluster(86, 64, "#87A35A", "#A4C56E"), 244, 168);
                AddBackdropElement(CreateGrassCluster(82, 48, "#789349", "#98B55F"), 308, 186);
                break;
        }
    }

    private bool TryResolveInteractionVisual(
        PetActor pet,
        IReadOnlyList<StagePropSpec> stageSpecs,
        DateTimeOffset now,
        double petWidth,
        double petHeight,
        out double localX,
        out double localY,
        out string? cueText,
        out Point cuePoint)
    {
        localX = 0;
        localY = 0;
        cueText = null;
        cuePoint = default;

        if (pet.OverrideAnimationEndsAtUtc is null || pet.OverrideAnimationEndsAtUtc <= now || string.IsNullOrWhiteSpace(pet.LastActionId))
        {
            return false;
        }

        var actionId = pet.LastActionId;
        var anchor = actionId switch
        {
            "feed" or "water" or "bath" => stageSpecs.FirstOrDefault(spec => spec.CategoryFolder == "containers"),
            "rest" or "home" => stageSpecs.FirstOrDefault(spec => IsRestAnchorAsset(spec.AssetId)),
            "play" => stageSpecs.FirstOrDefault(spec => spec.AssetId is "branch_perch" or "stump_perch" or "rope_toy") ??
                      stageSpecs.FirstOrDefault(spec => IsRestAnchorAsset(spec.AssetId)),
            _ => null
        };

        if (anchor is null)
        {
            return false;
        }

        var anchorX = anchor.Left + (anchor.Width / 2.0);
        var anchorY = anchor.Top + anchor.Height;
        (localX, localY) = ResolveInteractionPlacement(anchor, actionId, petWidth, petHeight, anchorX, anchorY);

        cueText = actionId switch
        {
            "rest" or "home" => "zzz...",
            "feed" => "nom...",
            "water" => "sip...",
            "bath" => "splash",
            "play" => "!",
            _ => null
        };

        cuePoint = actionId switch
        {
            "rest" or "home" => new Point(
                Math.Round(anchor.Left + (anchor.Width * 0.24)),
                Math.Round(anchor.Top - 18)),
            "feed" or "water" or "bath" or "play" => new Point(
                Math.Round(localX + (petWidth * 0.3)),
                Math.Round(localY - 16)),
            _ => new Point(
                Math.Round(anchor.Left + (anchor.Width * 0.55)),
                Math.Round(anchor.Top - 14))
        };
        return true;
    }

    private static (double LocalX, double LocalY) ResolveInteractionPlacement(
        StagePropSpec anchor,
        string actionId,
        double petWidth,
        double petHeight,
        double anchorX,
        double anchorY)
    {
        if (actionId is "feed" or "water" or "bath")
        {
            return (
                Math.Round(anchor.Left + (anchor.Width * 0.1)),
                Math.Round(anchor.Top + (anchor.Height * 0.24) - petHeight));
        }

        if (actionId == "play")
        {
            if (anchor.AssetId == "branch_perch")
            {
                return (
                    Math.Round(anchor.Left + (anchor.Width * 0.26)),
                    Math.Round(anchor.Top + (anchor.Height * 0.1) - (petHeight * 0.58)));
            }

            if (anchor.AssetId == "stump_perch")
            {
                return (
                    Math.Round(anchor.Left + (anchor.Width * 0.18)),
                    Math.Round(anchor.Top + (anchor.Height * 0.18) - petHeight));
            }

            return (
                Math.Round(anchorX - (petWidth / 2.0)),
                Math.Round(anchorY - petHeight - 2));
        }

        if (actionId is "rest" or "home")
        {
            return anchor.AssetId switch
            {
                "crate_hideout" => (
                    Math.Round(anchor.Left + (anchor.Width * 0.14)),
                    Math.Round(anchor.Top + (anchor.Height * 0.5) - (petHeight * 0.84))),
                "log_shelter" => (
                    Math.Round(anchor.Left + (anchor.Width * 0.08)),
                    Math.Round(anchor.Top + (anchor.Height * 0.56) - (petHeight * 0.82))),
                "branch_perch" => (
                    Math.Round(anchor.Left + (anchor.Width * 0.28)),
                    Math.Round(anchor.Top + (anchor.Height * 0.12) - (petHeight * 0.58))),
                "stump_perch" => (
                    Math.Round(anchor.Left + (anchor.Width * 0.18)),
                    Math.Round(anchor.Top + (anchor.Height * 0.14) - petHeight)),
                "nest_bed" => (
                    Math.Round(anchorX - (petWidth / 2.0)),
                    Math.Round(anchor.Top + (anchor.Height * 0.52) - petHeight)),
                "hay_bed" or "moss_bed" or "rock_basking_spot" => (
                    Math.Round(anchorX - (petWidth / 2.0)),
                    Math.Round(anchor.Top + (anchor.Height * 0.62) - petHeight)),
                _ => (
                    Math.Round(anchorX - (petWidth / 2.0)),
                    Math.Round(anchorY - petHeight - 4))
            };
        }

        return (
            Math.Round(anchorX - (petWidth / 2.0)),
            Math.Round(anchorY - petHeight - 2));
    }

    private void RenderInteractionCue(string cueText, Point cuePoint)
    {
        var text = new TextBlock
        {
            Text = cueText,
            Foreground = Brushes.Gainsboro,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.92
        };
        Canvas.SetLeft(text, cuePoint.X);
        Canvas.SetTop(text, cuePoint.Y);
        StageDecorationCanvas.Children.Add(text);
    }

    private void AddBackdropElement(FrameworkElement element, double left, double top)
    {
        element.IsHitTestVisible = false;
        Canvas.SetLeft(element, Math.Round(left));
        Canvas.SetTop(element, Math.Round(top));
        StageBackdropCanvas.Children.Add(element);
    }

    private void AddGroundShade(PetActor? focusPet)
    {
        var fill = focusPet?.SpeciesId switch
        {
            "crow" or "pigeon" => "#3A454F",
            "frog" or "goose" => "#466A62",
            "snake" => "#504D47",
            _ => "#303B42"
        };
        var ground = new Ellipse
        {
            Width = 380,
            Height = 42,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fill)),
            Opacity = 0.9,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(ground, 10);
        Canvas.SetTop(ground, 210);
        Canvas.SetZIndex(ground, HabitatDepthOrder.GetZIndex(DepthBand.FarProp));
        StagePropCanvas.Children.Add(ground);
    }

    private static FrameworkElement CreateContainerVisual(StagePropSpec spec)
    {
        var root = new Grid
        {
            Width = spec.Width,
            Height = spec.Height,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            ClipToBounds = true
        };
        RenderOptions.SetBitmapScalingMode(root, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(root, EdgeMode.Aliased);

        var isShallow = spec.AssetId is "shallow_water_dish" or "pond_dish";
        var outer = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2C2AF")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#725E4F")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.08))),
            CornerRadius = new CornerRadius(isShallow ? spec.Height * 0.34 : spec.Height * 0.42),
            Width = spec.Width,
            Height = isShallow ? spec.Height * 0.72 : spec.Height * 0.9,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var inner = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8FB7C9")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFE0E8")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.05))),
            CornerRadius = new CornerRadius(isShallow ? spec.Height * 0.26 : spec.Height * 0.3),
            Width = spec.Width * (isShallow ? 0.72 : 0.66),
            Height = spec.Height * (isShallow ? 0.28 : 0.3),
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, spec.Height * (isShallow ? 0.22 : 0.2))
        };

        var shadow = new Ellipse
        {
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35242D34")),
            Width = spec.Width * 0.92,
            Height = spec.Height * 0.24,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, spec.Height * 0.02)
        };

        root.Children.Add(shadow);
        root.Children.Add(outer);
        root.Children.Add(inner);
        return root;
    }

    private static Ellipse CreateAnchorContactShadow(StagePropSpec spec)
    {
        var opacity = spec.ContactShadowMode switch
        {
            ContactShadowMode.Hard => 0.34,
            ContactShadowMode.Soft => 0.22,
            _ => 0
        };

        return new Ellipse
        {
            Width = Math.Max(18, spec.Width * 0.78),
            Height = Math.Max(7, spec.Height * 0.16),
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E111820")),
            Opacity = opacity,
            IsHitTestVisible = false
        };
    }

    private static Ellipse CreatePetContactShadow(PetActor pet, double width, double height)
    {
        var speciesScale = pet.SpeciesId switch
        {
            "snake" => (Width: 0.9, Height: 0.11),
            "frog" => (Width: 0.72, Height: 0.16),
            "crow" or "pigeon" => (Width: 0.64, Height: 0.12),
            "goose" => (Width: 0.76, Height: 0.14),
            _ => (Width: 0.7, Height: 0.14)
        };

        return new Ellipse
        {
            Width = Math.Max(12, width * speciesScale.Width),
            Height = Math.Max(5, height * speciesScale.Height),
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3810181D")),
            Opacity = 0.28,
            IsHitTestVisible = false
        };
    }

    private static bool TryCreateStageSafePropVisual(StagePropSpec spec, out FrameworkElement? element)
    {
        element = spec.AssetId switch
        {
            "hay_bed" => CreateHayBedVisual(spec),
            "moss_bed" => CreateMossBedVisual(spec),
            "nest_bed" => CreateNestBedVisual(spec),
            "crate_hideout" => CreateCrateHideoutVisual(spec),
            "log_shelter" => CreateLogShelterVisual(spec),
            "stump_perch" => CreateStumpPerchVisual(spec),
            "branch_perch" => CreateBranchPerchVisual(spec),
            "rock_basking_spot" => CreateRockBaskingSpotVisual(spec),
            "leaf_pile" => CreateLeafPileVisual(spec),
            "ball" => CreateBallVisual(spec),
            "bug_treat" => CreateBugTreatVisual(spec),
            "moss_patch" => CreateMossPatchVisual(spec),
            "pebble_cluster" => CreatePebbleClusterVisual(spec),
            "shiny_reward" => CreateShinyRewardVisual(spec),
            "snack_bowl" => CreateSnackBowlVisual(spec),
            "storage_basket" => CreateStorageBasketVisual(spec),
            _ => null
        };
        return element is not null;
    }

    private static Grid CreateStagePropRoot(StagePropSpec spec)
    {
        var root = new Grid
        {
            Width = spec.Width,
            Height = spec.Height,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };
        RenderOptions.SetBitmapScalingMode(root, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(root, EdgeMode.Aliased);
        return root;
    }

    private static Ellipse CreatePropShadow(double width, double height, double opacity = 0.9)
    {
        return new Ellipse
        {
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35242D34")),
            Width = width,
            Height = height,
            Opacity = opacity,
            VerticalAlignment = VerticalAlignment.Bottom
        };
    }

    private static FrameworkElement CreateHayBedVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.86, spec.Height * 0.22, 0.82));

        var body = new Border
        {
            Width = spec.Width,
            Height = spec.Height * 0.76,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C89B4E")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5D2C")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.05))),
            CornerRadius = new CornerRadius(spec.Height * 0.16)
        };
        root.Children.Add(body);

        var slatColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#876133"));
        foreach (var offset in new[] { 0.22, 0.48, 0.74 })
        {
            var band = new Border
            {
                Width = spec.Width * 0.06,
                Height = spec.Height * 0.72,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(spec.Width * offset, 0, 0, spec.Height * 0.02),
                Background = slatColor
            };
            root.Children.Add(band);
        }

        var strawBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5C46A"));
        for (var index = 0; index < 11; index++)
        {
            var strand = new Rectangle
            {
                Width = spec.Width * 0.2,
                Height = Math.Max(2, spec.Height * 0.05),
                Fill = strawBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * (0.04 + (index * 0.085)), 0, 0, spec.Height * (0.56 + ((index % 3) * 0.04))),
                RenderTransform = new RotateTransform(index % 2 == 0 ? -20 : 18)
            };
            root.Children.Add(strand);
        }

        return root;
    }

    private static FrameworkElement CreateMossBedVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.84, spec.Height * 0.2, 0.8));

        var baseBed = new Border
        {
            Width = spec.Width,
            Height = spec.Height * 0.66,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546A33")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#31431F")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.05))),
            CornerRadius = new CornerRadius(spec.Height * 0.28)
        };
        root.Children.Add(baseBed);

        foreach (var segment in new[]
                 {
                     (0.08, 0.18, "#6E8A3F"),
                     (0.24, 0.08, "#5D7A33"),
                     (0.43, 0.16, "#6A8B43"),
                     (0.63, 0.11, "#4D662F")
                 })
        {
            var tuft = new Ellipse
            {
                Width = spec.Width * 0.3,
                Height = spec.Height * 0.42,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(segment.Item3)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * segment.Item1, 0, 0, spec.Height * segment.Item2)
            };
            root.Children.Add(tuft);
        }

        return root;
    }

    private static FrameworkElement CreateNestBedVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.78, spec.Height * 0.18, 0.76));

        var nest = new Ellipse
        {
            Width = spec.Width,
            Height = spec.Height * 0.74,
            VerticalAlignment = VerticalAlignment.Bottom,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A6438")),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4126")),
            StrokeThickness = Math.Max(1, Math.Round(spec.Height * 0.05))
        };
        root.Children.Add(nest);

        var inner = new Ellipse
        {
            Width = spec.Width * 0.66,
            Height = spec.Height * 0.42,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, spec.Height * 0.12),
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5E4732"))
        };
        root.Children.Add(inner);

        foreach (var offset in new[] { 0.1, 0.28, 0.48, 0.68 })
        {
            var twig = new Rectangle
            {
                Width = spec.Width * 0.26,
                Height = Math.Max(2, spec.Height * 0.04),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B48B58")),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * offset, 0, 0, spec.Height * 0.48),
                RenderTransform = new RotateTransform(offset < 0.5 ? -22 : 18)
            };
            root.Children.Add(twig);
        }

        return root;
    }

    private static FrameworkElement CreateCrateHideoutVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.82, spec.Height * 0.18, 0.76));

        var body = new Border
        {
            Width = spec.Width,
            Height = spec.Height,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7A5A3C")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4E3825")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.05))),
            CornerRadius = new CornerRadius(spec.Height * 0.08)
        };
        root.Children.Add(body);

        var doorway = new Border
        {
            Width = spec.Width * 0.34,
            Height = spec.Height * 0.56,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(spec.Width * 0.12, 0, 0, spec.Height * 0.06),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1916")),
            CornerRadius = new CornerRadius(spec.Height * 0.18, spec.Height * 0.18, 0, 0)
        };
        root.Children.Add(doorway);

        foreach (var offset in new[] { 0.08, 0.34, 0.6, 0.82 })
        {
            var slat = new Border
            {
                Width = spec.Width * 0.07,
                Height = spec.Height * 0.92,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * offset, 0, 0, spec.Height * 0.02),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8E6A47"))
            };
            root.Children.Add(slat);
        }

        return root;
    }

    private static FrameworkElement CreateLogShelterVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.88, spec.Height * 0.2, 0.8));

        var log = new Border
        {
            Width = spec.Width,
            Height = spec.Height * 0.78,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B4A30")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F2B1D")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.05))),
            CornerRadius = new CornerRadius(spec.Height * 0.28)
        };
        root.Children.Add(log);

        var openingOuter = new Ellipse
        {
            Width = spec.Width * 0.42,
            Height = spec.Height * 0.54,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(spec.Width * 0.06, 0, 0, spec.Height * 0.08),
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B6441"))
        };
        root.Children.Add(openingOuter);

        var openingInner = new Ellipse
        {
            Width = spec.Width * 0.28,
            Height = spec.Height * 0.4,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(spec.Width * 0.13, 0, 0, spec.Height * 0.14),
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#211914"))
        };
        root.Children.Add(openingInner);

        foreach (var offset in new[] { 0.34, 0.54, 0.72 })
        {
            var grain = new Rectangle
            {
                Width = spec.Width * 0.18,
                Height = Math.Max(2, spec.Height * 0.05),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#876241")),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * offset, 0, 0, spec.Height * (0.42 + ((offset * 10) % 2 * 0.06))),
                RenderTransform = new RotateTransform(offset < 0.5 ? -12 : 9)
            };
            root.Children.Add(grain);
        }

        return root;
    }

    private static FrameworkElement CreateStumpPerchVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.76, spec.Height * 0.18, 0.78));

        var trunk = new Border
        {
            Width = spec.Width * 0.64,
            Height = spec.Height * 0.76,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#796047")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A392C")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.05))),
            CornerRadius = new CornerRadius(spec.Height * 0.08)
        };
        root.Children.Add(trunk);

        var top = new Ellipse
        {
            Width = spec.Width,
            Height = spec.Height * 0.34,
            VerticalAlignment = VerticalAlignment.Top,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A28A69")),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A4633")),
            StrokeThickness = Math.Max(1, Math.Round(spec.Height * 0.05))
        };
        root.Children.Add(top);

        var ring = new Ellipse
        {
            Width = spec.Width * 0.42,
            Height = spec.Height * 0.14,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, spec.Height * 0.1, 0, 0),
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#887153"))
        };
        root.Children.Add(ring);

        return root;
    }

    private static FrameworkElement CreateBranchPerchVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);

        var branchColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#765339"));
        foreach (var segment in new[]
                 {
                     (0.02, 0.48, 0.72, 0.12, -12.0),
                     (0.48, 0.34, 0.44, 0.08, 18.0),
                     (0.72, 0.18, 0.24, 0.06, -14.0)
                 })
        {
            var branch = new Border
            {
                Width = spec.Width * segment.Item3,
                Height = Math.Max(3, spec.Height * segment.Item4),
                Background = branchColor,
                CornerRadius = new CornerRadius(spec.Height * 0.06),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(spec.Width * segment.Item1, spec.Height * segment.Item2, 0, 0),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(segment.Item5)
            };
            root.Children.Add(branch);
        }

        return root;
    }

    private static FrameworkElement CreateBallVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.62, spec.Height * 0.18, 0.62));
        var ball = new Ellipse
        {
            Width = spec.Width * 0.48,
            Height = spec.Width * 0.48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D66A42")),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B2F23")),
            StrokeThickness = Math.Max(1, Math.Round(spec.Width * 0.035))
        };
        root.Children.Add(ball);
        root.Children.Add(new Border
        {
            Width = spec.Width * 0.14,
            Height = spec.Width * 0.48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0B05E")),
            Opacity = 0.75,
            CornerRadius = new CornerRadius(spec.Width * 0.04)
        });
        return root;
    }

    private static FrameworkElement CreateBugTreatVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.72, spec.Height * 0.16, 0.58));
        var body = new Ellipse
        {
            Width = spec.Width * 0.36,
            Height = spec.Height * 0.42,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8793E")),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6A3A24")),
            StrokeThickness = Math.Max(1, Math.Round(spec.Height * 0.04))
        };
        root.Children.Add(body);
        foreach (var x in new[] { 0.22, 0.34, 0.58, 0.7 })
        {
            root.Children.Add(new Border
            {
                Width = spec.Width * 0.16,
                Height = Math.Max(2, spec.Height * 0.05),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * x, 0, 0, spec.Height * 0.18),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5B3020")),
                CornerRadius = new CornerRadius(spec.Height * 0.03)
            });
        }
        return root;
    }

    private static FrameworkElement CreateMossPatchVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.86, spec.Height * 0.18, 0.54));
        foreach (var tuft in new[]
                 {
                     (0.04, 0.08, 0.22, 0.58, "#6FA657"),
                     (0.22, 0.0, 0.26, 0.72, "#8AC36D"),
                     (0.46, 0.06, 0.24, 0.62, "#5F974D"),
                     (0.66, 0.1, 0.24, 0.54, "#7DBB63")
                 })
        {
            root.Children.Add(new Ellipse
            {
                Width = spec.Width * tuft.Item3,
                Height = spec.Height * tuft.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * tuft.Item1, 0, 0, spec.Height * tuft.Item2),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tuft.Item5))
            });
        }
        return root;
    }

    private static FrameworkElement CreatePebbleClusterVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.82, spec.Height * 0.14, 0.5));
        foreach (var pebble in new[]
                 {
                     (0.08, 0.18, 0.22, 0.28, "#879090"),
                     (0.28, 0.08, 0.28, 0.38, "#A7AAA4"),
                     (0.54, 0.16, 0.2, 0.28, "#737B7D"),
                     (0.68, 0.22, 0.22, 0.22, "#B8B4AA")
                 })
        {
            root.Children.Add(new Ellipse
            {
                Width = spec.Width * pebble.Item3,
                Height = spec.Height * pebble.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * pebble.Item1, 0, 0, spec.Height * pebble.Item2),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pebble.Item5))
            });
        }
        return root;
    }

    private static FrameworkElement CreateShinyRewardVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.62, spec.Height * 0.12, 0.42));
        foreach (var sparkle in new[]
                 {
                     (0.18, 0.34, 0.16),
                     (0.48, 0.18, 0.22),
                     (0.72, 0.42, 0.14)
                 })
        {
            var gem = new Polygon
            {
                Points = new PointCollection([
                    new Point(spec.Width * sparkle.Item3 / 2, 0),
                    new Point(spec.Width * sparkle.Item3, spec.Height * sparkle.Item3 / 2),
                    new Point(spec.Width * sparkle.Item3 / 2, spec.Height * sparkle.Item3),
                    new Point(0, spec.Height * sparkle.Item3 / 2)
                ]),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8D66D")),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A86D2E")),
                StrokeThickness = Math.Max(1, Math.Round(spec.Height * 0.025))
            };
            Canvas.SetLeft(gem, spec.Width * sparkle.Item1);
            Canvas.SetTop(gem, spec.Height * sparkle.Item2);
            root.Children.Add(gem);
        }
        return root;
    }

    private static FrameworkElement CreateSnackBowlVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.78, spec.Height * 0.16, 0.58));
        var bowl = new Border
        {
            Width = spec.Width * 0.7,
            Height = spec.Height * 0.34,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B6A48")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#503223")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.04))),
            CornerRadius = new CornerRadius(spec.Height * 0.14)
        };
        root.Children.Add(bowl);
        foreach (var x in new[] { 0.34, 0.46, 0.58 })
        {
            root.Children.Add(new Ellipse
            {
                Width = spec.Width * 0.1,
                Height = spec.Width * 0.1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * x, 0, 0, spec.Height * 0.24),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9B06C"))
            });
        }
        return root;
    }

    private static FrameworkElement CreateStorageBasketVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.78, spec.Height * 0.16, 0.58));
        var basket = new Border
        {
            Width = spec.Width * 0.72,
            Height = spec.Height * 0.54,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9F7149")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A3B28")),
            BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.04))),
            CornerRadius = new CornerRadius(spec.Height * 0.08)
        };
        root.Children.Add(basket);
        for (var i = 1; i <= 3; i++)
        {
            root.Children.Add(new Border
            {
                Width = Math.Max(2, spec.Width * 0.035),
                Height = spec.Height * 0.5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * (0.28 + i * 0.12), 0, 0, spec.Height * 0.02),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C3915E"))
            });
        }
        return root;
    }

    private static FrameworkElement CreateRockBaskingSpotVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.8, spec.Height * 0.18, 0.78));

        foreach (var rock in new[]
                 {
                     (0.02, 0.24, 0.42, 0.36, "#7D7A76"),
                     (0.32, 0.08, 0.38, 0.42, "#8B8884"),
                     (0.58, 0.2, 0.34, 0.32, "#6E6B67")
                 })
        {
            var stone = new Border
            {
                Width = spec.Width * rock.Item3,
                Height = spec.Height * rock.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * rock.Item1, 0, 0, spec.Height * rock.Item2),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(rock.Item5)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F5052")),
                BorderThickness = new Thickness(Math.Max(1, Math.Round(spec.Height * 0.04))),
                CornerRadius = new CornerRadius(spec.Height * 0.18)
            };
            root.Children.Add(stone);
        }

        return root;
    }

    private static FrameworkElement CreateLeafPileVisual(StagePropSpec spec)
    {
        var root = CreateStagePropRoot(spec);
        root.Children.Add(CreatePropShadow(spec.Width * 0.74, spec.Height * 0.16, 0.72));

        foreach (var leaf in new[]
                 {
                     (0.08, 0.22, 0.26, 0.34, "#7B6D2E", -16.0),
                     (0.28, 0.1, 0.28, 0.4, "#5E7431", 12.0),
                     (0.52, 0.2, 0.22, 0.32, "#A98944", -10.0),
                     (0.7, 0.12, 0.18, 0.28, "#596628", 18.0)
                 })
        {
            var blade = new Border
            {
                Width = spec.Width * leaf.Item3,
                Height = spec.Height * leaf.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(spec.Width * leaf.Item1, 0, 0, spec.Height * leaf.Item2),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(leaf.Item5)),
                CornerRadius = new CornerRadius(spec.Height * 0.18),
                RenderTransform = new RotateTransform(leaf.Item6)
            };
            root.Children.Add(blade);
        }

        return root;
    }

    private static FrameworkElement CreateBackdropPatch(double width, double height, string fill, string stroke, double opacity)
    {
        var patch = new Ellipse
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fill)),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(stroke)),
            StrokeThickness = Math.Max(1, Math.Round(Math.Min(width, height) * 0.035)),
            Opacity = opacity,
            IsHitTestVisible = false
        };
        RenderOptions.SetEdgeMode(patch, EdgeMode.Aliased);
        return patch;
    }

    private static FrameworkElement CreateGrassCluster(double width, double height, string lowColor, string highColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        var colors = new[] { lowColor, highColor, lowColor, highColor, lowColor };
        var offsets = new[] { 0.04, 0.18, 0.38, 0.58, 0.76 };
        var rotations = new[] { -18.0, -8.0, 0.0, 10.0, 18.0 };
        for (var index = 0; index < colors.Length; index++)
        {
            var blade = new Border
            {
                Width = width * 0.16,
                Height = height * (0.72 + ((index % 2) * 0.14)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * offsets[index], 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[index])),
                CornerRadius = new CornerRadius(height * 0.16),
                RenderTransform = new RotateTransform(rotations[index])
            };
            root.Children.Add(blade);
        }

        return root;
    }

    private static FrameworkElement CreateReedCluster(double width, double height, string lowColor, string highColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        for (var index = 0; index < 6; index++)
        {
            var reed = new Border
            {
                Width = width * 0.08,
                Height = height * (0.72 + ((index % 3) * 0.12)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * (0.06 + (index * 0.14)), 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(index % 2 == 0 ? lowColor : highColor)),
                CornerRadius = new CornerRadius(height * 0.12),
                RenderTransform = new RotateTransform(index % 2 == 0 ? -6 : 7)
            };
            root.Children.Add(reed);
        }

        return root;
    }

    private static FrameworkElement CreatePebbleCluster(double width, double height, string darkColor, string midColor, string lightColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        foreach (var pebble in new[]
                 {
                     (0.02, 0.42, 0.22, 0.34, darkColor),
                     (0.18, 0.18, 0.2, 0.3, midColor),
                     (0.34, 0.38, 0.18, 0.28, lightColor),
                     (0.5, 0.16, 0.22, 0.34, midColor),
                     (0.68, 0.34, 0.2, 0.28, darkColor)
                 })
        {
            var stone = new Ellipse
            {
                Width = width * pebble.Item3,
                Height = height * pebble.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * pebble.Item1, 0, 0, height * pebble.Item2),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pebble.Item5)),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#43474C")),
                StrokeThickness = Math.Max(1, Math.Round(height * 0.04))
            };
            root.Children.Add(stone);
        }

        return root;
    }

    private static FrameworkElement CreateTwigScatter(double width, double height, string darkColor, string lightColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        foreach (var twig in new[]
                 {
                     (0.04, 0.44, 0.26, -18.0, darkColor),
                     (0.26, 0.24, 0.24, 12.0, lightColor),
                     (0.48, 0.42, 0.24, -10.0, darkColor),
                     (0.68, 0.22, 0.2, 16.0, lightColor)
                 })
        {
            var branch = new Border
            {
                Width = width * twig.Item3,
                Height = Math.Max(2, height * 0.1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * twig.Item1, 0, 0, height * twig.Item2),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(twig.Item5)),
                CornerRadius = new CornerRadius(height * 0.08),
                RenderTransform = new RotateTransform(twig.Item4)
            };
            root.Children.Add(branch);
        }

        return root;
    }

    private static FrameworkElement CreateLeafScatter(double width, double height, string warmColor, string greenColor, string barkColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        foreach (var leaf in new[]
                 {
                     (0.06, 0.36, 0.22, 0.3, warmColor, -18.0),
                     (0.26, 0.14, 0.22, 0.32, greenColor, 14.0),
                     (0.5, 0.3, 0.18, 0.28, barkColor, -12.0),
                     (0.7, 0.16, 0.18, 0.26, warmColor, 18.0)
                 })
        {
            var blade = new Border
            {
                Width = width * leaf.Item3,
                Height = height * leaf.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * leaf.Item1, 0, 0, height * leaf.Item2),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(leaf.Item5)),
                CornerRadius = new CornerRadius(height * 0.18),
                RenderTransform = new RotateTransform(leaf.Item6)
            };
            root.Children.Add(blade);
        }

        return root;
    }

    private static FrameworkElement CreateFlowerScatter(double width, double height, string paleColor, string warmColor, string coolColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        foreach (var blossom in new[]
                 {
                     (0.08, 0.18, paleColor),
                     (0.34, 0.28, warmColor),
                     (0.6, 0.14, coolColor)
                 })
        {
            var stem = new Border
            {
                Width = Math.Max(2, width * 0.02),
                Height = height * 0.42,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * blossom.Item1, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5D7A3A"))
            };
            root.Children.Add(stem);

            var bloom = new Ellipse
            {
                Width = width * 0.16,
                Height = height * 0.26,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * (blossom.Item1 - 0.06), 0, 0, height * blossom.Item2),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(blossom.Item3)),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7D3CB")),
                StrokeThickness = Math.Max(1, Math.Round(height * 0.04))
            };
            root.Children.Add(bloom);
        }

        return root;
    }

    private static FrameworkElement CreateRipplePool(double width, double height, string edgeColor, string innerColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));

        var pool = new Ellipse
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(edgeColor)),
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#547E86")),
            StrokeThickness = Math.Max(1, Math.Round(height * 0.06)),
            Opacity = 0.82
        };
        root.Children.Add(pool);

        var inner = new Ellipse
        {
            Width = width * 0.64,
            Height = height * 0.44,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(innerColor)),
            Opacity = 0.92,
            Margin = new Thickness(width * 0.18, height * 0.2, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        root.Children.Add(inner);

        return root;
    }

    private static FrameworkElement CreateLilyCluster(double width, double height, string darkColor, string lightColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        foreach (var pad in new[]
                 {
                     (0.02, 0.28, 0.34, 0.4, darkColor),
                     (0.3, 0.08, 0.3, 0.34, lightColor),
                     (0.56, 0.24, 0.28, 0.34, darkColor)
                 })
        {
            var lily = new Ellipse
            {
                Width = width * pad.Item3,
                Height = height * pad.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * pad.Item1, 0, 0, height * pad.Item2),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pad.Item5)),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#405C2D")),
                StrokeThickness = Math.Max(1, Math.Round(height * 0.04))
            };
            root.Children.Add(lily);
        }

        return root;
    }

    private static FrameworkElement CreateFernCluster(double width, double height, string darkColor, string lightColor)
    {
        var root = CreateStagePropRoot(new StagePropSpec(string.Empty, string.Empty, 0, 0, width, height, 1));
        foreach (var frond in new[]
                 {
                     (0.02, 0.0, 0.18, 0.78, -30.0, darkColor),
                     (0.18, 0.06, 0.18, 0.84, -14.0, lightColor),
                     (0.38, 0.02, 0.18, 0.88, 0.0, darkColor),
                     (0.58, 0.06, 0.18, 0.82, 12.0, lightColor),
                     (0.76, 0.02, 0.18, 0.76, 24.0, darkColor)
                 })
        {
            var blade = new Border
            {
                Width = width * frond.Item3,
                Height = height * frond.Item4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(width * frond.Item1, 0, 0, height * frond.Item2),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(frond.Item6)),
                CornerRadius = new CornerRadius(height * 0.16),
                RenderTransform = new RotateTransform(frond.Item5)
            };
            root.Children.Add(blade);
        }

        return root;
    }


    private static StagePropSpec ScaleStagePropSpec(StagePropSpec spec, PetActor? focusPet)
    {
        var scale = spec.CategoryFolder switch
        {
            "containers" => 2.35,
            "care" => 1.72,
            "food_birds" => 1.72,
            "food_herbivore" => 1.82,
            "food_omnivore" => 1.78,
            "food_predator" => 1.82,
            "toys_a" => 2.45,
            "toys_b" => 2.8,
            "utility" => 1.92,
            _ => 1.8
        };

        var speciesMultiplier = focusPet?.SpeciesId switch
        {
            "deer" => 1.18,
            "goose" => 1.14,
            "fox" => 1.1,
            "raccoon" => 1.1,
            "snake" => 1.08,
            "crow" => 1.04,
            "pigeon" => 1.04,
            "frog" => 0.96,
            "rat" => 0.96,
            _ => 1.0
        };

        scale *= speciesMultiplier;

        scale *= spec.AssetId switch
        {
            "ball" => 0.42,
            "bug_treat" => 0.72,
            "branch_perch" => 0.42,
            "pebble_cluster" or "shiny_reward" => 0.7,
            "stump_perch" => 0.58,
            _ => 1.0
        };

        if (IsRestAnchorAsset(spec.AssetId))
        {
            scale = Math.Max(scale, focusPet?.AgeStage switch
            {
                PetAgeStage.Baby => 3.15,
                PetAgeStage.Teen => 3.55,
                _ => 3.95
            });
        }

        var scaledWidth = spec.Width * scale;
        var scaledHeight = spec.Height * scale;
        var left = spec.Left - ((scaledWidth - spec.Width) / 2.0);
        var top = spec.Top - (scaledHeight - spec.Height);
        return spec with
        {
            Left = left,
            Top = top,
            Width = scaledWidth,
            Height = scaledHeight
        };
    }

    private static bool IsRestAnchorAsset(string assetId)
    {
        return assetId switch
        {
            "blanket_mat" or
            "crate_hideout" or
            "hay_bed" or
            "log_shelter" or
            "moss_bed" or
            "nest_bed" or
            "rock_basking_spot" or
            "stump_perch" or
            "branch_perch" or
            "tunnel_hide" => true,
            _ => false
        };
    }

    private static IReadOnlyList<StagePropSpec> GetStagePropSpecs(
        EnvironmentDefinition environment,
        IReadOnlyList<StagePropSpec> dynamicStageProps,
        PetActor? focusPet)
    {
        _ = environment;
        var speciesId = focusPet?.SpeciesId ?? string.Empty;
        if (UsesManifestHabitatPilot(speciesId) && dynamicStageProps.Count > 0)
        {
            return dynamicStageProps;
        }

        var template = speciesId switch
        {
            "rat" => new[]
            {
                new StagePropSpec("toys_b", "crate_hideout", 90, 132, 70, 64, 0.94),
                new StagePropSpec("toys_b", "hay_bed", 286, 182, 88, 42, 0.98),
                new StagePropSpec("containers", "shallow_water_dish", 176, 192, 68, 24, 0.94)
            },
            "crow" => new[]
            {
                new StagePropSpec("toys_a", "branch_perch", 200, 54, 176, 50, 0.94),
                new StagePropSpec("toys_b", "nest_bed", 286, 176, 72, 38, 0.9)
            },
            "fox" => new[]
            {
                new StagePropSpec("toys_b", "moss_bed", 244, 176, 112, 48, 0.98),
                new StagePropSpec("toys_a", "leaf_pile", 108, 190, 56, 34, 0.9),
                new StagePropSpec("containers", "shallow_water_dish", 154, 192, 70, 24, 0.94)
            },
            "snake" => new[]
            {
                new StagePropSpec("toys_b", "rock_basking_spot", 250, 176, 106, 44, 0.96),
                new StagePropSpec("containers", "shallow_water_dish", 188, 192, 70, 24, 0.92)
            },
            "deer" => new[]
            {
                new StagePropSpec("toys_b", "hay_bed", 282, 182, 92, 42, 0.98),
                new StagePropSpec("toys_a", "leaf_pile", 108, 190, 58, 36, 0.9),
                new StagePropSpec("containers", "shallow_water_dish", 174, 192, 72, 26, 0.94)
            },
            "frog" => new[]
            {
                new StagePropSpec("toys_b", "moss_bed", 274, 176, 90, 40, 0.98),
                new StagePropSpec("toys_b", "rock_basking_spot", 116, 188, 66, 30, 0.92),
                new StagePropSpec("containers", "pond_dish", 166, 190, 74, 26, 0.92)
            },
            "pigeon" => new[]
            {
                new StagePropSpec("toys_a", "branch_perch", 196, 56, 174, 48, 0.94),
                new StagePropSpec("toys_b", "nest_bed", 284, 178, 70, 36, 0.9)
            },
            "raccoon" => new[]
            {
                new StagePropSpec("toys_b", "stump_perch", 88, 158, 70, 58, 0.92),
                new StagePropSpec("toys_b", "log_shelter", 272, 154, 98, 60, 0.92),
                new StagePropSpec("containers", "shallow_water_dish", 184, 192, 68, 24, 0.92)
            },
            "squirrel" => new[]
            {
                new StagePropSpec("toys_a", "branch_perch", 194, 56, 184, 48, 0.94),
                new StagePropSpec("toys_b", "stump_perch", 330, 162, 62, 52, 0.90),
                new StagePropSpec("toys_b", "nest_bed", 122, 182, 66, 34, 0.9),
                new StagePropSpec("containers", "shallow_water_dish", 144, 192, 64, 24, 0.92)
            },
            "goose" => new[]
            {
                new StagePropSpec("toys_b", "hay_bed", 284, 182, 92, 42, 0.98),
                new StagePropSpec("toys_a", "leaf_pile", 112, 190, 56, 34, 0.88),
                new StagePropSpec("containers", "pond_dish", 170, 190, 74, 26, 0.92)
            },
            _ => Array.Empty<StagePropSpec>()
        };

        if (template.Length > 0)
        {
            return template;
        }

        return dynamicStageProps;
    }

    private static bool UsesManifestHabitatPilot(string speciesId)
    {
        return speciesId is "goose" or "rat" or "crow" or "snake" or "frog";
    }

    private static string BuildLeadPetSummary(PetActor? pet, int basketCount)
    {
        var basketText = basketCount switch
        {
            0 => "link bin empty",
            1 => "1 saved link",
            _ => $"{basketCount} saved links"
        };

        if (pet is null)
        {
            return $"No active companions - {basketText}";
        }

        var personalityBits = new List<string>();
        var personality = pet.Personality;
        if (personality is not null)
        {
            if (personality.Playfulness >= 25) { personalityBits.Add("playful"); }
            if (personality.Cheerfulness >= 25) { personalityBits.Add("bright"); }
            if (personality.CuddleNeed >= 25 || personality.SocialNeed >= 25) { personalityBits.Add("clingy"); }
            if (personality.Stubbornness >= 25) { personalityBits.Add("headstrong"); }
        }

        var agePhase = pet.IsGhost ? "ghost" : pet.IsDead ? "passed" : pet.AgeStage.ToString().ToLowerInvariant();
        var traitText = personalityBits.Count == 0 ? "settling" : string.Join(", ", personalityBits.Take(2));
        return $"{pet.Name} - {agePhase} - {traitText} - {basketText}";
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

    private async void WebToolsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleWebToolsRequested is not null)
        {
            await ToggleWebToolsRequested.Invoke();
        }
    }

    private async void ActionsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleActionsRequested is not null)
        {
            await ToggleActionsRequested.Invoke();
        }
    }

    private async void LinkBinTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleBasketRequested is not null)
        {
            await ToggleBasketRequested.Invoke();
        }
    }

    private async void HelperTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleHelpersRequested is not null)
        {
            await ToggleHelpersRequested.Invoke();
        }
    }

    private async void WebToolSlot3Button_OnClick(object sender, RoutedEventArgs e)
    {
        if (OpenSpriteWorkflowV2Requested is not null)
        {
            await OpenSpriteWorkflowV2Requested.Invoke();
        }
    }

    private async void WebToolSlot4Button_OnClick(object sender, RoutedEventArgs e)
    {
        if (OpenCreativeLearningLabRequested is not null)
        {
            await OpenCreativeLearningLabRequested.Invoke();
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

    private async void DevButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ToggleDevRequested is not null)
        {
            await ToggleDevRequested.Invoke();
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
