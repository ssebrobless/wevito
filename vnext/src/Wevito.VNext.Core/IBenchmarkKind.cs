namespace Wevito.VNext.Core;

public interface IBenchmarkKind
{
    string AxisName { get; }

    BenchmarkKindResult RunCases(IReadOnlyList<BenchmarkCase> cases, BenchmarkContext context);
}
