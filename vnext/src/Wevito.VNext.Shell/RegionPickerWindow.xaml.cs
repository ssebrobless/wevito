using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

public partial class RegionPickerWindow : Window
{
    private Point? _dragStart;

    public RegionPickerWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            SelectionCanvas.Width = Width;
            SelectionCanvas.Height = Height;
            Activate();
            Focus();
        };
    }

    public CaptureRegion? SelectedRegion { get; private set; }

    public static CaptureRegion? Pick(Window? owner = null)
    {
        var picker = new RegionPickerWindow
        {
            Owner = owner
        };
        return picker.ShowDialog() == true ? picker.SelectedRegion : null;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _dragStart = e.GetPosition(this);
        SelectionRectangle.Visibility = Visibility.Visible;
        UpdateSelectionRectangle(_dragStart.Value, _dragStart.Value);
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        UpdateSelectionRectangle(_dragStart.Value, e.GetPosition(this));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _dragStart is null)
        {
            return;
        }

        ReleaseMouseCapture();
        var end = e.GetPosition(this);
        var rect = Normalize(_dragStart.Value, end);
        _dragStart = null;

        if (rect.Width < 4 || rect.Height < 4)
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            return;
        }

        SelectedRegion = ToScreenRegion(rect);
        DialogResult = true;
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        SelectedRegion = null;
        DialogResult = false;
        Close();
    }

    private void UpdateSelectionRectangle(Point start, Point end)
    {
        var rect = Normalize(start, end);
        Canvas.SetLeft(SelectionRectangle, rect.X);
        Canvas.SetTop(SelectionRectangle, rect.Y);
        SelectionRectangle.Width = rect.Width;
        SelectionRectangle.Height = rect.Height;
    }

    private CaptureRegion ToScreenRegion(Rect rect)
    {
        var topLeft = PointToScreen(new Point(rect.X, rect.Y));
        var bottomRight = PointToScreen(new Point(rect.Right, rect.Bottom));
        var deviceRect = Normalize(topLeft, bottomRight);
        return new CaptureRegion(
            (int)Math.Round(deviceRect.X),
            (int)Math.Round(deviceRect.Y),
            Math.Max(1, (int)Math.Round(deviceRect.Width)),
            Math.Max(1, (int)Math.Round(deviceRect.Height)));
    }

    private static Rect Normalize(Point start, Point end)
    {
        return new Rect(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));
    }
}
