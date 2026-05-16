namespace Wevito.VNext.Core.Benchmarks;

public sealed class PerfRegressionBenchmarkKind : IBenchmarkKind
{
    public string AxisName => "perf";

    public BenchmarkKindResult RunCases(IReadOnlyList<BenchmarkCase> cases, BenchmarkContext context)
    {
        if (cases.Count == 0)
        {
            return BenchmarkKindResult.NoBaseline(AxisName);
        }

        var latencies = cases.Select(testCase => Math.Max(0, testCase.LatencyMs)).Order().ToList();
        var ramPeak = cases.Max(testCase => Math.Max(0, testCase.RamPeakMb));
        var vramPeak = cases.Max(testCase => Math.Max(0, testCase.VramPeakMb));
        var passed = cases.Count(testCase => testCase.LatencyMs <= 0 || testCase.LatencyMs <= 5000);
        var findings = cases
            .Where(testCase => testCase.LatencyMs > 5000)
            .Select(testCase => $"{testCase.Id}: latency {testCase.LatencyMs:0}ms exceeds v1 threshold")
            .ToList();

        return new BenchmarkKindResult(
            AxisName,
            cases.Count,
            passed,
            cases.Count - passed,
            cases.Count == 0 ? 0 : passed / (double)cases.Count,
            findings,
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["latency_p50_ms"] = Percentile(latencies, 0.50),
                ["latency_p95_ms"] = Percentile(latencies, 0.95),
                ["ram_peak_mb"] = ramPeak,
                ["vram_peak_mb"] = vramPeak
            });
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }
}
