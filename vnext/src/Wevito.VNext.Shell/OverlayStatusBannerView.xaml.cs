using System.Windows;
using System.Windows.Controls;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class OverlayStatusBannerView : UserControl
{
    public OverlayStatusBannerView()
    {
        InitializeComponent();
    }

    public bool IsBannerVisible => BannerBorder.Visibility == Visibility.Visible;

    public void Render(string text, RuntimeSupervisorStatus status, bool killSwitchActive, DateTimeOffset nowUtc, DateTimeOffset? lastActivityAtUtc)
    {
        var visible = ShouldShow(status, killSwitchActive, nowUtc, lastActivityAtUtc);
        BannerBorder.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        BannerText.Text = killSwitchActive
            ? "Stop Everything is active · helpers are blocked"
            : text;
    }

    public static bool ShouldShow(RuntimeSupervisorStatus status, bool killSwitchActive, DateTimeOffset nowUtc, DateTimeOffset? lastActivityAtUtc)
    {
        if (killSwitchActive)
        {
            return true;
        }

        if (status.Mode != RuntimeSupervisorMode.Active || status.IsQuietedForFullscreen)
        {
            return false;
        }

        if (lastActivityAtUtc is null)
        {
            return true;
        }

        return nowUtc - lastActivityAtUtc <= TimeSpan.FromSeconds(60);
    }
}
