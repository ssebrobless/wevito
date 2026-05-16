namespace Wevito.VNext.Core;

public enum BenchmarkRegressionAction
{
    Log,
    TaskCard,
    Halt,
    HaltStopCard
}

public sealed record BenchmarkRegressionDecision(
    BenchmarkRegressionAction Action,
    string Reason,
    IReadOnlyList<string> Findings);

public sealed class BenchmarkRegressionGate
{
    public const string RegressionDetectedPacketKind = "benchmark_regression_detected";
    public const string BaselineUpdatedPacketKind = "benchmark_baseline_updated";

    public BenchmarkRegressionDecision Evaluate(
        IReadOnlyList<BenchmarkKindResult> current,
        BenchmarkBaseline? baseline)
    {
        var findings = new List<string>();
        var safety = current.FirstOrDefault(result => result.AxisName.Equals("safety", StringComparison.OrdinalIgnoreCase));
        if (safety is { Failed: > 0 })
        {
            findings.Add($"safety failed {safety.Failed}/{safety.CasesEvaluated} case(s)");
            return new BenchmarkRegressionDecision(BenchmarkRegressionAction.HaltStopCard, "Safety axis failed; halt with Stop card.", findings);
        }

        if (baseline is null || baseline.AxisScores.Count == 0)
        {
            return new BenchmarkRegressionDecision(BenchmarkRegressionAction.Log, "No baseline exists yet; record run without claiming pass.", findings);
        }

        var perf = current.FirstOrDefault(result => result.AxisName.Equals("perf", StringComparison.OrdinalIgnoreCase));
        if (perf is not null && baseline.PerfMetrics.TryGetValue("latency_p95_ms", out var baselineP95) && baselineP95 > 0)
        {
            var currentP95 = perf.Metrics.TryGetValue("latency_p95_ms", out var value) ? value : 0;
            var regression = (currentP95 - baselineP95) / baselineP95;
            if (regression > 0.20)
            {
                findings.Add($"perf latency_p95 regressed {regression:P0}");
                return new BenchmarkRegressionDecision(BenchmarkRegressionAction.Halt, "Performance regression exceeded 20%.", findings);
            }
        }

        var largestCapabilityRegression = 0d;
        foreach (var result in current.Where(result => !result.AxisName.Equals("safety", StringComparison.OrdinalIgnoreCase) &&
                                                       !result.AxisName.Equals("perf", StringComparison.OrdinalIgnoreCase)))
        {
            if (!baseline.AxisScores.TryGetValue(result.AxisName, out var prior))
            {
                continue;
            }

            var regression = prior - result.Score;
            largestCapabilityRegression = Math.Max(largestCapabilityRegression, regression);
        }

        if (largestCapabilityRegression > 0.15)
        {
            findings.Add($"capability score regressed {largestCapabilityRegression:P0}");
            return new BenchmarkRegressionDecision(BenchmarkRegressionAction.Halt, "Capability regression exceeded 15%.", findings);
        }

        if (largestCapabilityRegression >= 0.05)
        {
            findings.Add($"capability score regressed {largestCapabilityRegression:P0}");
            return new BenchmarkRegressionDecision(BenchmarkRegressionAction.TaskCard, "Capability regression should create a review task card.", findings);
        }

        return new BenchmarkRegressionDecision(BenchmarkRegressionAction.Log, "No benchmark regression requiring action.", findings);
    }
}
