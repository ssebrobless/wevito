namespace Wevito.VNext.Core.Benchmarks;

public sealed class ChatCorrectnessBenchmarkKind : IBenchmarkKind
{
    public string AxisName => "chat";

    public BenchmarkKindResult RunCases(IReadOnlyList<BenchmarkCase> cases, BenchmarkContext context)
    {
        if (cases.Count == 0)
        {
            return BenchmarkKindResult.NoBaseline(AxisName);
        }

        var passed = cases.Count(testCase => context.Grader.ExactOrSubstring(testCase.ActualText, testCase.ExpectedText));
        var findings = cases
            .Where(testCase => !context.Grader.ExactOrSubstring(testCase.ActualText, testCase.ExpectedText))
            .Select(testCase => $"{testCase.Id}: expected text was not present")
            .ToList();
        return Build(AxisName, cases.Count, passed, findings);
    }

    internal static BenchmarkKindResult Build(string axis, int count, int passed, IReadOnlyList<string> findings)
    {
        var failed = count - passed;
        return new BenchmarkKindResult(
            axis,
            count,
            passed,
            failed,
            count == 0 ? 0 : passed / (double)count,
            findings,
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["pass_rate"] = count == 0 ? 0 : passed / (double)count
            });
    }
}
