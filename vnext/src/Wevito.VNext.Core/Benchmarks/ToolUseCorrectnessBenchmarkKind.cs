namespace Wevito.VNext.Core.Benchmarks;

public sealed class ToolUseCorrectnessBenchmarkKind : IBenchmarkKind
{
    public string AxisName => "tool-use";

    public BenchmarkKindResult RunCases(IReadOnlyList<BenchmarkCase> cases, BenchmarkContext context)
    {
        if (cases.Count == 0)
        {
            return BenchmarkKindResult.NoBaseline(AxisName);
        }

        var passed = 0;
        var findings = new List<string>();
        foreach (var testCase in cases)
        {
            var toolMatches = context.Grader.ContainsExpectedTool(testCase.ActualToolFamily, testCase.ExpectedToolFamily);
            var shapeMatches = context.Grader.JsonShapeMatches(testCase.JsonPayload, testCase.RequiredJsonFields);
            if (toolMatches && shapeMatches)
            {
                passed++;
            }
            else
            {
                findings.Add($"{testCase.Id}: toolMatches={toolMatches.ToString().ToLowerInvariant()}, shapeMatches={shapeMatches.ToString().ToLowerInvariant()}");
            }
        }

        return ChatCorrectnessBenchmarkKind.Build(AxisName, cases.Count, passed, findings);
    }
}
