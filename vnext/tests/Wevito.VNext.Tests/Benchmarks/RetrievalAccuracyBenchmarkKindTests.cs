using Wevito.VNext.Core;
using Wevito.VNext.Core.Benchmarks;

namespace Wevito.VNext.Tests.Benchmarks;

public sealed class RetrievalAccuracyBenchmarkKindTests
{
    [Fact]
    public void ComputesRecallAt1And3AndMrr()
    {
        var result = new RetrievalAccuracyBenchmarkKind().RunCases([
            new BenchmarkCase("retrieval-1", "retrieval", "find local ai", ExpectedChunkIds: ["chunk-b"], RetrievedChunkIds: ["chunk-a", "chunk-b", "chunk-c"])
        ], Context());

        Assert.Equal(0, result.Metrics["recall_at_1"]);
        Assert.Equal(1, result.Metrics["recall_at_3"]);
        Assert.Equal(0.5, result.Metrics["mrr"]);
    }

    private static BenchmarkContext Context() => new(BenchmarkSuiteVersion.V1, GraderTriad.Default, DateTimeOffset.UtcNow);
}
