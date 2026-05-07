using System.Windows;
using System.Windows.Interop;

namespace Wevito.VNext.Shell;

public partial class RecordingIndicatorWindow : Window
{
    public RecordingIndicatorWindow(Window? owner = null)
    {
        InitializeComponent();
        Owner = owner;
        SourceInitialized += (_, _) => WindowDisplayAffinity.ExcludeFromCapture(this);
    }

    public void ShowNear(Window window, TimeSpan duration)
    {
        UpdateRemaining(duration);
        Left = window.Left + Math.Max(16, window.ActualWidth - Width - 20);
        Top = window.Top + 20;
        Show();
        Activate();
    }

    public void UpdateRemaining(TimeSpan remaining)
    {
        var clamped = TimeSpan.FromMilliseconds(Math.Max(0, remaining.TotalMilliseconds));
        CountdownText.Text = $"{clamped:mm\\:ss} remaining";
    }

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();
}
