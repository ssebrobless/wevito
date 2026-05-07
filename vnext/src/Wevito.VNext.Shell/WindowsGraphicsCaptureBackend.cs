using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public sealed class WindowsGraphicsCaptureBackend : IScreenCaptureBackend
{
    private readonly Func<Window?> _targetWindowFactory;

    public WindowsGraphicsCaptureBackend(Func<Window?> targetWindowFactory)
    {
        _targetWindowFactory = targetWindowFactory;
    }

    public Task<ScreenCaptureBackendResult> CaptureWevitoWindowAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = _targetWindowFactory();
        if (window is null || !window.IsVisible)
        {
            throw new InvalidOperationException("Wevito target window is not visible.");
        }

        window.Dispatcher.VerifyAccess();
        window.UpdateLayout();

        var width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight));
        var screenTopLeft = window.PointToScreen(new Point(0, 0));
        var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(window);

        var parent = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));
        using (var stream = File.Create(outputPath))
        {
            encoder.Save(stream);
        }

        return Task.FromResult(new ScreenCaptureBackendResult(
            true,
            window.Title,
            new CaptureRegion((int)Math.Round(screenTopLeft.X), (int)Math.Round(screenTopLeft.Y), width, height),
            IndicatorVisible: false,
            RedactionState: "Tool popup excluded from capture with WDA_EXCLUDEFROMCAPTURE when available; no credential surfaces exist yet.",
            Warnings:
            [
                "C-PHASE 11 uses a Wevito-window-only WPF render capture path in the WindowsGraphicsCaptureBackend seam. Windows.Graphics.Capture can replace this backend without changing Core artifacts.",
                "The OS yellow capture border is not expected for this backend implementation."
            ]));
    }
}
