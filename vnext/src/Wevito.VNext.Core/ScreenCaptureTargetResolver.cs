using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public static class ScreenCaptureTargetResolver
{
    public static CaptureRequest ResolveRequest(
        string rawText,
        Guid taskCardId,
        DateTimeOffset createdAtUtc,
        CaptureRegion? region = null)
    {
        var target = ResolveTarget(rawText);
        return new CaptureRequest(
            Guid.NewGuid(),
            target.Preset,
            target.TargetKind,
            CaptureOutputKind.ScreenshotPng,
            target.PrivacyLevel,
            taskCardId,
            region,
            IncludeCursor: false,
            IncludeOverlayMetadata: true,
            IsRecording: false,
            IsExternalShareRequested: false,
            CreatedAtUtc: createdAtUtc);
    }

    public static CaptureTargetResolution ResolveTarget(string rawText)
    {
        var normalized = (rawText ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Contains("last region"))
        {
            return new CaptureTargetResolution(
                CapturePreset.LastRegion,
                CaptureTargetKind.LastRegion,
                CapturePrivacyLevel.SelectedRegion);
        }

        if (normalized.Contains("selected region") ||
            normalized.Contains("screenshot a region") ||
            normalized.Contains("capture a region") ||
            normalized.Contains("screen shot a region") ||
            normalized.Contains("region") ||
            normalized.Contains("selected area") ||
            normalized.Contains("screenshot an area") ||
            normalized.Contains("capture an area"))
        {
            return new CaptureTargetResolution(
                CapturePreset.SelectedRegion,
                CaptureTargetKind.SelectedRegion,
                CapturePrivacyLevel.SelectedRegion);
        }

        return new CaptureTargetResolution(
            CapturePreset.WevitoWindow,
            CaptureTargetKind.WevitoWindow,
            CapturePrivacyLevel.WevitoOnly);
    }
}

public sealed record CaptureTargetResolution(
    CapturePreset Preset,
    CaptureTargetKind TargetKind,
    CapturePrivacyLevel PrivacyLevel);
