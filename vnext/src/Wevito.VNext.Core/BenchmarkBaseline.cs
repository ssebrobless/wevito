namespace Wevito.VNext.Core;

public sealed record BenchmarkBaseline(
    string BenchmarkVersion,
    IReadOnlyDictionary<string, double> AxisScores,
    IReadOnlyDictionary<string, double> PerfMetrics);
