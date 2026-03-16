using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    internal void Render(CompanionState state, SpriteAssetService assetService)
    {
        RoamCanvas.Children.Clear();
        if (state.Mode != CompanionMode.Passive)
        {
            return;
        }

        foreach (var pet in state.ActivePets)
        {
            var source = assetService.GetPetFrame(pet, DateTimeOffset.UtcNow);
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

            Canvas.SetLeft(image, Math.Round(pet.CurrentX - Left - width / 2));
            Canvas.SetTop(image, Math.Round(ActualHeight - height - 8));
            RoamCanvas.Children.Add(image);
        }
    }

    internal void CloseSilently()
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
