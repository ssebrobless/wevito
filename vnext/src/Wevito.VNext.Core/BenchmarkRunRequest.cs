namespace Wevito.VNext.Core;

public sealed record BenchmarkRunRequest(
    string ApprovedCaseRoot,
    string ArtifactRoot,
    string ModelVersion,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string>? EnabledAxes = null,
    BenchmarkBaseline? Baseline = null);
