using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public partial class RoamBandWindow : Window
{
    private bool _closingSilently;

    public RoamBandWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => OverlayWindowStyler.Apply(this, clickThrough: true, noActivate: true);
    }

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public void Render(CompanionState state)
    {
        RoamCanvas.Children.Clear();
        if (state.Mode != CompanionMode.Passive)
        {
            return;
        }

        foreach (var pet in state.ActivePets)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(pet.AccentColor));
            var ellipse = new Ellipse
            {
                Width = 28,
                Height = 18,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, pet.CurrentX - Left - 14);
            Canvas.SetTop(ellipse, ActualHeight - 36);
            RoamCanvas.Children.Add(ellipse);
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
}
