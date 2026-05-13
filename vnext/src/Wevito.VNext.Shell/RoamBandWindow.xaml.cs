using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class RoamBandWindow : Window
{
    private const int WmActivate = 0x0006;
    private bool _closingSilently;
    private FocusStealCounter? _focusStealCounter;
    private Func<bool>? _isForegroundFullscreenOther;
    private HwndSource? _hwndSource;

    public RoamBandWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            OverlayWindowStyler.Apply(this, clickThrough: true, noActivate: true);
            AttachFocusStealHook();
        };
    }

    public long WindowHandle => new WindowInteropHelper(this).Handle.ToInt64();

    public void RegisterFocusStealCounter(FocusStealCounter counter, Func<bool> isForegroundFullscreenOther)
    {
        _focusStealCounter = counter;
        _isForegroundFullscreenOther = isForegroundFullscreenOther;
        AttachFocusStealHook();
    }

    internal void Render(
        CompanionState state,
        SpriteAssetService assetService,
        OverlayStatusSnapshot? statusSnapshot = null,
        string statusText = "",
        RuntimeSupervisorStatus? runtimeStatus = null,
        bool killSwitchActive = false,
        EvidenceCollectionStatus? evidenceStatus = null,
        double assetOpacity = ShellPresentationRules.ActiveAssetOpacity)
    {
        RoamCanvas.Children.Clear();
        RoamCanvas.Opacity = Math.Clamp(assetOpacity, 0.0, 1.0);
        var now = DateTimeOffset.UtcNow;
        StatusBanner.Render(
            FormatStatusText(statusText, evidenceStatus),
            runtimeStatus ?? new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            killSwitchActive,
            now,
            statusSnapshot?.LastAtUtc);
        if (state.Mode != CompanionMode.Passive)
        {
            return;
        }

        foreach (var pet in state.ActivePets)
        {
            var source = assetService.GetPetFrame(pet, now);
            if (source is null)
            {
                continue;
            }

            var scale = assetService.GetPetScale(pet);
            var bitmap = source as BitmapSource;
            var width = (bitmap?.PixelWidth ?? 28) * scale;
            var height = (bitmap?.PixelHeight ?? 24) * scale;
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
            if (pet.FacingDirection == PetFacingDirection.Left)
            {
                image.RenderTransform = new ScaleTransform(-1, 1);
            }

            var localX = Math.Round(pet.CurrentX - Left - width / 2);
            var localY = ResolvePetTopInBand(pet.CurrentY, Top, height, ActualHeight);
            Canvas.SetLeft(image, localX);
            Canvas.SetTop(image, localY);
            RoamCanvas.Children.Add(image);
        }
    }

    internal static double ResolvePetTopInBand(double petScreenY, double windowTop, double height, double actualHeight)
    {
        var localY = Math.Round(petScreenY - windowTop - height);
        if (double.IsNaN(localY) || double.IsInfinity(localY))
        {
            localY = actualHeight - height - 8;
        }

        return Math.Clamp(localY, 0, Math.Max(0, actualHeight - height - 8));
    }

    private static string FormatStatusText(string statusText, EvidenceCollectionStatus? evidenceStatus)
    {
        var baseText = string.IsNullOrWhiteSpace(statusText)
            ? "Last action: none yet - today: 0 previews, 0 approvals, 0 mutations"
            : statusText;
        return evidenceStatus?.Active == true
            ? $"{baseText} - soak day {evidenceStatus.DayN} of {evidenceStatus.DayMax}"
            : baseText;
    }

    internal void CloseSilently()
    {
        _closingSilently = true;
        Close();
    }

    private void AttachFocusStealHook()
    {
        if (_hwndSource is not null || PresentationSource.FromVisual(this) is not HwndSource source)
        {
            return;
        }

        _hwndSource = source;
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmActivate && wParam != IntPtr.Zero)
        {
            _focusStealCounter?.RecordActivation(_isForegroundFullscreenOther?.Invoke() == true, DateTimeOffset.UtcNow);
        }

        return IntPtr.Zero;
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
