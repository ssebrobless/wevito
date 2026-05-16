namespace Wevito.VNext.Core;

public sealed record BenchmarkRunResult(
    bool Succeeded,
    string Status,
    string BenchmarkVersion,
    string ModelVersion,
    DateTimeOffset CreatedAtUtc,
    string ArtifactFolder,
    string ResultPath,
    string SummaryPath,
    double CompositeScore,
    IReadOnlyList<BenchmarkKindResult> KindResults,
    BenchmarkRegressionDecision RegressionDecision,
    string Message);
