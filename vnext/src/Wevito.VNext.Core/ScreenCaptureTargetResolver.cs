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
        var isRecording = IsRecordingRequest(rawText);
        return new CaptureRequest(
            Guid.NewGuid(),
            isRecording ? CapturePreset.ShortRecording : target.Preset,
            target.TargetKind,
            isRecording ? CaptureOutputKind.ClipMp4 : CaptureOutputKind.ScreenshotPng,
            target.PrivacyLevel,
            taskCardId,
            region,
            IncludeCursor: false,
            IncludeOverlayMetadata: true,
            IsRecording: isRecording,
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

    public static bool IsRecordingRequest(string rawText)
    {
        var normalized = (rawText ?? string.Empty).Trim().ToLowerInvariant();
        return normalized.Contains("record") ||
               normalized.Contains("recording") ||
               normalized.Contains("proof clip") ||
               normalized.Contains("video clip") ||
               normalized.Contains("clip.mp4") ||
               normalized.Contains("mp4");
    }
}

public sealed record CaptureTargetResolution(
    CapturePreset Preset,
    CaptureTargetKind TargetKind,
    CapturePrivacyLevel PrivacyLevel);
