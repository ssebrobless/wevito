namespace Wevito.VNext.Core;

public sealed record LearningDatasetManifest(
    string SchemaVersion,
    string DatasetVersion,
    Guid TaskCardId,
    Guid PetId,
    string PetName,
    int ExamplesCount,
    int ExcludedCount,
    IReadOnlyList<string> SourceBundlePaths,
    string ExamplesSha256,
    string Reviewer,
    bool AutomaticTrainingEnabled,
    bool AutomaticMemoryPromotionEnabled,
    DateTimeOffset CreatedAtUtc);

public sealed record LearningPromotionExampleRow(
    string SourcePath,
    string RelativePath,
    string ArtifactKind,
    string Target,
    string Label,
    string Reviewer,
    string Notes,
    string Content);
