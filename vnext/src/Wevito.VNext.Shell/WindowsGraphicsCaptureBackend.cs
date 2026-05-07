using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

    public Task<ScreenCaptureBackendResult> CaptureRegionAsync(CaptureRegion region, string outputPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Selected region must have positive width and height.");
        }

        var parent = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not acquire desktop device context.");
        }

        var memoryDc = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previous = IntPtr.Zero;
        try
        {
            memoryDc = CreateCompatibleDC(screenDc);
            if (memoryDc == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not create compatible device context.");
            }

            bitmap = CreateCompatibleBitmap(screenDc, region.Width, region.Height);
            if (bitmap == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not create region capture bitmap.");
            }

            previous = SelectObject(memoryDc, bitmap);
            if (!BitBlt(memoryDc, 0, 0, region.Width, region.Height, screenDc, region.X, region.Y, CopyPixelOperationSourceCopy))
            {
                throw new InvalidOperationException("Could not copy selected screen region.");
            }

            var source = Imaging.CreateBitmapSourceFromHBitmap(
                bitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using (var stream = File.Create(outputPath))
            {
                encoder.Save(stream);
            }
        }
        finally
        {
            if (previous != IntPtr.Zero && memoryDc != IntPtr.Zero)
            {
                _ = SelectObject(memoryDc, previous);
            }

            if (bitmap != IntPtr.Zero)
            {
                _ = DeleteObject(bitmap);
            }

            if (memoryDc != IntPtr.Zero)
            {
                _ = DeleteDC(memoryDc);
            }

            _ = ReleaseDC(IntPtr.Zero, screenDc);
        }

        return Task.FromResult(new ScreenCaptureBackendResult(
            true,
            "Selected Region",
            region,
            IndicatorVisible: false,
            RedactionState: "Selected-region capture uses explicit user approval and geometry-only last-region recall.",
            Warnings:
            [
                "Selected-region capture may include non-Wevito screen content. It is approval-gated and writes local artifacts only.",
                "The OS yellow capture border is not expected for this GDI region capture implementation."
            ]));
    }

    private const int CopyPixelOperationSourceCopy = 0x00CC0020;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDc, IntPtr hGdiObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDc);
}
