namespace Wevito.VNext.Core.Chores;

public sealed class BenchmarkDailyChore
{
    private readonly BenchmarkSuiteService _benchmarkSuiteService;

    public BenchmarkDailyChore(BenchmarkSuiteService benchmarkSuiteService)
    {
        _benchmarkSuiteService = benchmarkSuiteService;
    }

    public BenchmarkRunResult RunDaily(string approvedCaseRoot, string artifactRoot, string modelVersion, DateTimeOffset nowUtc)
    {
        return _benchmarkSuiteService.Run(
            BenchmarkSuiteVersion.V1,
            new BenchmarkRunRequest(
                approvedCaseRoot,
                artifactRoot,
                modelVersion,
                nowUtc,
                EnabledAxes: ["chat", "tool-use", "retrieval"]));
    }
}
