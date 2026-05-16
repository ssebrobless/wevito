using Wevito.VNext.Broker;

namespace Wevito.VNext.Tests;

public sealed class CodexLoopWatchdogServiceTests
{
    [Fact]
    public void RestartsLoopOnMissedHeartbeat()
    {
        var recorded = new List<CodexLoopWatchdogDecision>();
        var service = new CodexLoopWatchdogService(recordDecision: recorded.Add);

        var decision = service.Observe(new CodexLoopHeartbeatSnapshot(
            LoopRunning: true,
            LastHeartbeatUtc: DateTimeOffset.Parse("2026-05-15T12:00:00Z"),
            ObservedAtUtc: DateTimeOffset.Parse("2026-05-15T12:11:00Z")));

        Assert.True(decision.RestartRequested);
        Assert.Single(recorded);
    }

    [Fact]
    public void RespectsKillSwitch()
    {
        var service = new CodexLoopWatchdogService(killSwitchActive: () => true);

        var decision = service.Observe(new CodexLoopHeartbeatSnapshot(
            LoopRunning: true,
            LastHeartbeatUtc: DateTimeOffset.Parse("2026-05-15T12:00:00Z"),
            ObservedAtUtc: DateTimeOffset.Parse("2026-05-15T12:30:00Z")));

        Assert.False(decision.RestartRequested);
        Assert.Contains("kill_switch=true", decision.Summary);
    }
}
