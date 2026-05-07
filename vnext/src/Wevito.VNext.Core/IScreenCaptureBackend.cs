using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public interface IScreenCaptureBackend
{
    Task<ScreenCaptureBackendResult> CaptureWevitoWindowAsync(string outputPath, CancellationToken cancellationToken = default);
}

public sealed record ScreenCaptureBackendResult(
    bool DidCapture,
    string TargetWindowTitle,
    CaptureRegion TargetWindowRect,
    bool IndicatorVisible,
    string RedactionState,
    IReadOnlyList<string>? Warnings = null);
