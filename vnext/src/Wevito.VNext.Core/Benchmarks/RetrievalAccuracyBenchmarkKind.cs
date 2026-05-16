namespace Wevito.VNext.Core.Benchmarks;

public sealed class RetrievalAccuracyBenchmarkKind : IBenchmarkKind
{
    public string AxisName => "retrieval";

    public BenchmarkKindResult RunCases(IReadOnlyList<BenchmarkCase> cases, BenchmarkContext context)
    {
        if (cases.Count == 0)
        {
            return BenchmarkKindResult.NoBaseline(AxisName);
        }

        var recallAt1 = new List<double>();
        var recallAt3 = new List<double>();
        var mrr = new List<double>();
        var findings = new List<string>();
        foreach (var testCase in cases)
        {
            var expected = testCase.ExpectedChunkIds ?? [];
            var actual = testCase.RetrievedChunkIds ?? [];
            var r1 = GraderTriad.RecallAt(expected, actual, 1);
            var r3 = GraderTriad.RecallAt(expected, actual, 3);
            var rank = GraderTriad.MeanReciprocalRank(expected, actual);
            recallAt1.Add(r1);
            recallAt3.Add(r3);
            mrr.Add(rank);
            if (r3 <= 0)
            {
                findings.Add($"{testCase.Id}: expected chunk not found in top 3");
            }
        }

        var score = mrr.Average();
        return new BenchmarkKindResult(
            AxisName,
            cases.Count,
            mrr.Count(value => value > 0),
            mrr.Count(value => value <= 0),
            score,
            findings,
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["recall_at_1"] = recallAt1.Average(),
                ["recall_at_3"] = recallAt3.Average(),
                ["mrr"] = score
            });
    }
}
