using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests.Fakes;

internal sealed class FakeScreenCaptureBackend : IScreenCaptureBackend
{
    private static readonly byte[] OnePixelPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

    public Task<ScreenCaptureBackendResult> CaptureWevitoWindowAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        File.WriteAllBytes(outputPath, OnePixelPng);
        return Task.FromResult(new ScreenCaptureBackendResult(
            true,
            "Wevito Home Panel",
            new CaptureRegion(10, 20, 320, 240),
            IndicatorVisible: true,
            RedactionState: "fake backend",
            Warnings: []));
    }

    public Task<ScreenCaptureBackendResult> CaptureRegionAsync(CaptureRegion region, string outputPath, CancellationToken cancellationToken = default)
    {
        File.WriteAllBytes(outputPath, OnePixelPng);
        return Task.FromResult(new ScreenCaptureBackendResult(
            true,
            "Selected Region",
            region,
            IndicatorVisible: false,
            RedactionState: "fake region backend",
            Warnings: []));
    }

    public Task<ScreenCaptureBackendResult> CaptureWevitoWindowClipAsync(
        string outputPath,
        TimeSpan duration,
        IProgress<TimeSpan>? remainingProgress = null,
        CancellationToken cancellationToken = default)
    {
        File.WriteAllBytes(outputPath, [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6F, 0x6D]);
        remainingProgress?.Report(TimeSpan.Zero);
        return Task.FromResult(new ScreenCaptureBackendResult(
            true,
            "Wevito Home Panel",
            new CaptureRegion(10, 20, 320, 240),
            IndicatorVisible: true,
            RedactionState: "fake clip backend",
            Warnings: ["fake deterministic clip"]));
    }
}
