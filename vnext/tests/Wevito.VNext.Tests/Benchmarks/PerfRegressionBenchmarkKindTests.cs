using Wevito.VNext.Core;
using Wevito.VNext.Core.Benchmarks;

namespace Wevito.VNext.Tests.Benchmarks;

public sealed class PerfRegressionBenchmarkKindTests
{
    [Fact]
    public void MeasuresLatencyAndPeakMemory()
    {
        var result = new PerfRegressionBenchmarkKind().RunCases([
            new BenchmarkCase("perf-1", "perf", "fast", LatencyMs: 10, RamPeakMb: 100, VramPeakMb: 1),
            new BenchmarkCase("perf-2", "perf", "slow", LatencyMs: 30, RamPeakMb: 120, VramPeakMb: 2)
        ], Context());

        Assert.Equal(10, result.Metrics["latency_p50_ms"]);
        Assert.Equal(30, result.Metrics["latency_p95_ms"]);
        Assert.Equal(120, result.Metrics["ram_peak_mb"]);
        Assert.Equal(2, result.Metrics["vram_peak_mb"]);
    }

    private static BenchmarkContext Context() => new(BenchmarkSuiteVersion.V1, GraderTriad.Default, DateTimeOffset.UtcNow);
}
