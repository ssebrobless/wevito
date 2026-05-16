using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class BenchmarkRegressionGateTests
{
    [Fact]
    public void SafetyFailureReturnsHaltStopCard()
    {
        var decision = new BenchmarkRegressionGate().Evaluate([Result("safety", 0.5, failed: 1)], Baseline());

        Assert.Equal(BenchmarkRegressionAction.HaltStopCard, decision.Action);
    }

    [Fact]
    public void PerfRegressionAbove20PctReturnsHalt()
    {
        var decision = new BenchmarkRegressionGate().Evaluate([
            new BenchmarkKindResult("perf", 1, 1, 0, 1, [], new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["latency_p95_ms"] = 130
            })
        ], Baseline(perfP95: 100));

        Assert.Equal(BenchmarkRegressionAction.Halt, decision.Action);
    }

    [Fact]
    public void CapabilityRegressionAbove15PctReturnsHalt()
    {
        var decision = new BenchmarkRegressionGate().Evaluate([Result("chat", 0.70)], Baseline(chat: 0.90));

        Assert.Equal(BenchmarkRegressionAction.Halt, decision.Action);
    }

    [Fact]
    public void CapabilityRegression5To15PctReturnsTaskCard()
    {
        var decision = new BenchmarkRegressionGate().Evaluate([Result("chat", 0.80)], Baseline(chat: 0.90));

        Assert.Equal(BenchmarkRegressionAction.TaskCard, decision.Action);
    }

    [Fact]
    public void CapabilityRegressionBelow5PctReturnsLog()
    {
        var decision = new BenchmarkRegressionGate().Evaluate([Result("chat", 0.87)], Baseline(chat: 0.90));

        Assert.Equal(BenchmarkRegressionAction.Log, decision.Action);
    }

    private static BenchmarkKindResult Result(string axis, double score, int failed = 0) => new(
        axis,
        1,
        failed == 0 ? 1 : 0,
        failed,
        score,
        [],
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase));

    private static BenchmarkBaseline Baseline(double chat = 0.90, double perfP95 = 100) => new(
        "v1",
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["chat"] = chat
        },
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["latency_p95_ms"] = perfP95
        });
}
