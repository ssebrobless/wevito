namespace Wevito.VNext.Contracts;

public sealed record VisualQaMatrixManifest(
    DateTimeOffset GeneratedAtUtc,
    string RepoRoot,
    int ExpectedCellCount,
    IReadOnlyList<string> SpeciesIds,
    IReadOnlyList<string> LifeStages,
    IReadOnlyList<string> Genders,
    IReadOnlyList<string> ColorVariants,
    IReadOnlyList<string> AnimationFamilies,
    IReadOnlyList<VisualQaMatrixCell> Rows);

public sealed record VisualQaMatrixCell(
    string SpeciesId,
    string LifeStage,
    string Gender,
    string ColorVariant,
    IReadOnlyList<VisualQaAnimationObservation> Animations,
    IReadOnlyList<string> Tags,
    string Notes);

public sealed record VisualQaAnimationObservation(
    string AnimationFamily,
    bool ForceSucceeded,
    bool AssetSourceSucceeded,
    string? SourcePath,
    int FrameCount,
    string? CapturePath,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Warnings);
