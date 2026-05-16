namespace Wevito.VNext.Core;

public sealed record BenchmarkKindResult(
    string AxisName,
    int CasesEvaluated,
    int Passed,
    int Failed,
    double Score,
    IReadOnlyList<string> Findings,
    IReadOnlyDictionary<string, double> Metrics)
{
    public static BenchmarkKindResult NoBaseline(string axisName) => new(
        axisName,
        CasesEvaluated: 0,
        Passed: 0,
        Failed: 0,
        Score: 0,
        Findings: ["no_baseline: approved benchmark cases are empty"],
        Metrics: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase));
}
