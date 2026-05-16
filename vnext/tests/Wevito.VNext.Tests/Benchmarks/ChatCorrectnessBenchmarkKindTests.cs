using Wevito.VNext.Core;
using Wevito.VNext.Core.Benchmarks;

namespace Wevito.VNext.Tests.Benchmarks;

public sealed class ChatCorrectnessBenchmarkKindTests
{
    [Fact]
    public void DeterministicGradingAgainstExpected()
    {
        var result = new ChatCorrectnessBenchmarkKind().RunCases([
            new BenchmarkCase("chat-1", "chat", "say hello", ExpectedText: "hello goose", ActualText: "Hello goose!")
        ], Context());

        Assert.Equal(1, result.Passed);
        Assert.Equal(1, result.Score);
    }

    private static BenchmarkContext Context() => new(BenchmarkSuiteVersion.V1, GraderTriad.Default, DateTimeOffset.UtcNow);
}
