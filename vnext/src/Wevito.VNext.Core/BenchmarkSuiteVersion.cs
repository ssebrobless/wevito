namespace Wevito.VNext.Core;

public sealed record BenchmarkSuiteVersion(string VersionId)
{
    public static BenchmarkSuiteVersion V1 { get; } = new("v1");
}
