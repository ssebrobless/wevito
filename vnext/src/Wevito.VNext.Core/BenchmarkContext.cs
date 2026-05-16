namespace Wevito.VNext.Core;

public sealed record BenchmarkContext(
    BenchmarkSuiteVersion Version,
    GraderTriad Grader,
    DateTimeOffset CreatedAtUtc);
