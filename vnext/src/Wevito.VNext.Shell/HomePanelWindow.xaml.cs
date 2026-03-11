using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public partial class HomePanelWindow : Window
{
    private bool _closingSilently;

    public HomePanelWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => OverlayWindowStyler.Apply(this, clickThrough: false, noActivate: false);
    }

    public event Func<Task>? TogglePinnedRequested;

    public event Func<Task>? ToggleBasketRequested;

    public event Func<Task>? CaptureClipboardRequested;

    public event Action<string>? ActionRequested;

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public RectInt GetStageRect()
    {
        return new RectInt(12, 78, 336, 132);
    }

    public void SetHudVisible(bool isVisible)
    {
        var next = isVisible ? Visibility.Visible : Visibility.Collapsed;
        if (HudPanel.Visibility == next)
        {
            return;
        }

        HudPanel.Visibility = next;
        TraceLog.Write("visibility", $"window=HomeHud visibility={next}");
    }

    public void Render(CompanionState state, EnvironmentDefinition environment, string feedbackText)
    {
        ModeText.Text = state.Mode switch
        {
            CompanionMode.Focused => "Focused · HUD active",
            CompanionMode.Passive => "Passive · HUD hidden",
            CompanionMode.Pinned => "Pinned · interactive over desktop",
            _ => state.Mode.ToString()
        };
        SubtitleText.Text = state.Mode == CompanionMode.Passive
            ? "Pets are roaming the desktop band"
            : "Pets are settled in the home environment";
        PinButton.Content = state.IsPinned ? "REL" : "PIN";
        BasketButton.Content = state.ActiveTool.IsOpen ? "HIDE" : "BIN";
        EnvironmentLabel.Text = environment.DisplayName;
        StatusText.Text = $"{state.ActivePets.Count} pets • {state.BasketItems.Count} saved link(s)";
        FeedbackText.Text = feedbackText;

        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(environment.PrimaryColor),
            (Color)ColorConverter.ConvertFromString(environment.SecondaryColor),
            90);
        StageGradient.Fill = brush;

        HomePetCanvas.Children.Clear();
        if (state.Mode == CompanionMode.Passive)
        {
            return;
        }

        foreach (var pet in state.ActivePets)
        {
            var color = (Color)ColorConverter.ConvertFromString(pet.AccentColor);
            var ellipse = new Ellipse
            {
                Width = 26,
                Height = 18,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, pet.CurrentX - 13);
            Canvas.SetTop(ellipse, pet.CurrentY - 18);
            HomePetCanvas.Children.Add(ellipse);

            var label = new System.Windows.Controls.TextBlock
            {
                Text = pet.Name,
                Foreground = Brushes.White,
                FontSize = 10
            };
            Canvas.SetLeft(label, pet.CurrentX - 18);
            Canvas.SetTop(label, pet.CurrentY - 34);
            HomePetCanvas.Children.Add(label);
        }
    }

    public void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_closingSilently)
        {
            Hide();
        }

        base.OnClosed(e);
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

    private void FeedButton_OnClick(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke("feed");
    }

    private void PetButton_OnClick(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke("pet");
    }

    private void RestButton_OnClick(object sender, RoutedEventArgs e)
    {
        ActionRequested?.Invoke("rest");
    }
}
