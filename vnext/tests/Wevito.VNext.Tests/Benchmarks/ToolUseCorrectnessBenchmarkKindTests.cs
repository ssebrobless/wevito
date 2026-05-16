using Wevito.VNext.Core;
using Wevito.VNext.Core.Benchmarks;

namespace Wevito.VNext.Tests.Benchmarks;

public sealed class ToolUseCorrectnessBenchmarkKindTests
{
    [Fact]
    public void StructuralJsonCheck()
    {
        var result = new ToolUseCorrectnessBenchmarkKind().RunCases([
            new BenchmarkCase("tool-1", "tool-use", "review sprites", ExpectedToolFamily: "spriteAudit", ActualToolFamily: "spriteAudit", RequiredJsonFields: ["reportPath"], JsonPayload: "{\"reportPath\":\"report.md\"}")
        ], Context());

        Assert.Equal(1, result.Passed);
        Assert.Equal(0, result.Failed);
    }

    private static BenchmarkContext Context() => new(BenchmarkSuiteVersion.V1, GraderTriad.Default, DateTimeOffset.UtcNow);
}
