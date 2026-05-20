using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class EvalCoverageHealthServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Snapshot_KillSwitchActive_ReturnsZeroCounts()
    {
        var service = new EvalCoverageHealthService(
            new StaticHeldOutStore(["held-out-a"]),
            new StaticInDistributionStore(["in-dist-a"]),
            new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = bool.TrueString
            }));

        var snapshot = service.Snapshot(Now);

        Assert.Equal(0, snapshot.HeldOutCount);
        Assert.Equal(0, snapshot.InDistributionCount);
        Assert.False(snapshot.AllMet);
        Assert.Equal("kill_switch=true", snapshot.Reason);
    }

    [Fact]
    public void Snapshot_EmptyStores_AreBelowMinimum()
    {
        var service = new EvalCoverageHealthService(
            new StaticHeldOutStore([]),
            new StaticInDistributionStore([]));

        var snapshot = service.Snapshot(Now);

        Assert.Equal(0, snapshot.HeldOutCount);
        Assert.Equal(0, snapshot.InDistributionCount);
        Assert.False(snapshot.HeldOutMeetsMinimum);
        Assert.False(snapshot.InDistributionMeetsMinimum);
        Assert.False(snapshot.AllMet);
        Assert.Equal("below_minimum", snapshot.Reason);
    }

    [Fact]
    public void Snapshot_WhenBothStoresMeetMinimum_AllMetTrue()
    {
        var service = new EvalCoverageHealthService(
            new StaticHeldOutStore(Ids("held", 5)),
            new StaticInDistributionStore(Ids("dist", 10)));

        var snapshot = service.Snapshot(Now);

        Assert.Equal(5, snapshot.HeldOutCount);
        Assert.Equal(10, snapshot.InDistributionCount);
        Assert.True(snapshot.HeldOutMeetsMinimum);
        Assert.True(snapshot.InDistributionMeetsMinimum);
        Assert.True(snapshot.AllMet);
        Assert.Equal("ok", snapshot.Reason);
    }

    [Fact]
    public void Snapshot_WhenInDistributionIsBelowMinimum_AllMetFalse()
    {
        var service = new EvalCoverageHealthService(
            new StaticHeldOutStore(Ids("held", 5)),
            new StaticInDistributionStore(Ids("dist", 9)));

        var snapshot = service.Snapshot(Now);

        Assert.True(snapshot.HeldOutMeetsMinimum);
        Assert.False(snapshot.InDistributionMeetsMinimum);
        Assert.False(snapshot.AllMet);
        Assert.Equal("below_minimum", snapshot.Reason);
    }

    private static IReadOnlyList<string> Ids(string prefix, int count)
    {
        return Enumerable.Range(1, count).Select(index => $"{prefix}-{index:00}").ToArray();
    }

    private sealed class StaticHeldOutStore(IReadOnlyList<string> ids) : IHeldOutEvalStore
    {
        public IReadOnlyList<string> ListCaseIds() => ids;

        public string? ReadCase(string caseId) => throw new InvalidOperationException("EvalCoverageHealthService must not read held-out case contents.");
    }

    private sealed class StaticInDistributionStore(IReadOnlyList<string> ids) : IInDistributionEvalStore
    {
        public override IReadOnlyList<string> ListCaseIds() => ids;

        public override string? ReadCase(string caseId) => throw new InvalidOperationException("EvalCoverageHealthService must not read in-distribution case contents.");
    }
}
