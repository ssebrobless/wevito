namespace Wevito.VNext.Contracts;

public static class VisualQaCommandTypes
{
    public const string ForceAnimation = "VisualQa.ForceAnimation";
    public const string ClearForcedAnimation = "VisualQa.ClearForcedAnimation";
    public const string SetOverlayOptions = "VisualQa.SetOverlayOptions";
    public const string RunBatchMatrix = "VisualQa.RunBatchMatrix";
    public const string CaptureEvidence = "VisualQa.CaptureEvidence";
    public const string TagIssue = "VisualQa.TagIssue";
    public const string ResetSaveSandbox = "VisualQa.ResetSaveSandbox";
    public const string GetAssetSource = "VisualQa.GetAssetSource";
    public const string GetSoakObserver = "VisualQa.GetSoakObserver";
}

public sealed record VisualQaForceAnimationRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    string AnimationFamily,
    int? FrameIndex,
    double PlaybackSpeed,
    bool Loop);

public sealed record VisualQaClearForcedAnimationRequest(
    int SlotIndex,
    Guid? ExpectedPetId);

public sealed record VisualQaOverlayOptions(
    bool ShowSpriteBounds,
    bool ShowAlphaBounds,
    bool ShowAnchorPoints,
    bool ShowGroundLine,
    bool ShowTaskbarLine,
    bool ShowEnvironmentZones,
    bool ShowAssetSourceLabels)
{
    public static VisualQaOverlayOptions Off { get; } = new(false, false, false, false, false, false, false);
}

public sealed record VisualQaRunBatchMatrixRequest(
    IReadOnlyList<string> SpeciesIds,
    IReadOnlyList<string> LifeStages,
    IReadOnlyList<string> Genders,
    IReadOnlyList<string> ColorVariants,
    IReadOnlyList<string> AnimationFamilies,
    bool CaptureContactSheets,
    bool CaptureScreenshots,
    bool IncludeActions);

public sealed record VisualQaCaptureEvidenceRequest(
    string CaptureKind,
    int? SlotIndex,
    Guid? ExpectedPetId,
    string Label);

public sealed record VisualQaIssueTagRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    IReadOnlyList<string> Tags,
    string Notes,
    bool AttachCurrentScreenshot);

public sealed record VisualQaAssetSourceSnapshot(
    int SlotIndex,
    Guid? PetId,
    string? SpeciesId,
    string? LifeStage,
    string? Gender,
    string? ColorVariant,
    string? AnimationFamily,
    string SourceKind,
    string? SourcePath,
    bool IsFallback,
    IReadOnlyList<string> Warnings);

public sealed record VisualQaGetAssetSourceRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    string AnimationFamily);

public sealed record VisualQaResetSaveSandboxRequest(string Mode);

public sealed record VisualQaSoakObserverSnapshot(
    bool IsActive,
    int DayNumber,
    DateTimeOffset? LatestHeartbeatUtc,
    int PreviewCount,
    int ApprovalCount,
    int MutationCount,
    int FlaggedRows,
    string? LatestArtifactPath);
