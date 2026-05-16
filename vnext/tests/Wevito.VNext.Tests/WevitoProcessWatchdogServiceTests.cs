using Wevito.VNext.Broker;

namespace Wevito.VNext.Tests;

public sealed class WevitoProcessWatchdogServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void RestartsCrashedChildWithBackoff()
    {
        var service = new WevitoProcessWatchdogService();
        service.Observe(new WatchedProcessState("godot", true, Now));

        var first = service.Observe(new WatchedProcessState("godot", false, Now.AddSeconds(1)));
        var second = service.Observe(new WatchedProcessState("godot", false, Now.AddSeconds(2)));

        Assert.True(first.RestartRequested);
        Assert.Equal(TimeSpan.FromSeconds(1), first.Backoff);
        Assert.True(second.RestartRequested);
        Assert.Equal(TimeSpan.FromSeconds(5), second.Backoff);
    }

    [Fact]
    public void RespectsMaxRestartsCap()
    {
        var service = new WevitoProcessWatchdogService();
        service.Observe(new WatchedProcessState("ollama", true, Now));
        service.Observe(new WatchedProcessState("ollama", false, Now.AddSeconds(1)));
        service.Observe(new WatchedProcessState("ollama", false, Now.AddSeconds(2)));
        service.Observe(new WatchedProcessState("ollama", false, Now.AddSeconds(3)));

        var capped = service.Observe(new WatchedProcessState("ollama", false, Now.AddSeconds(4)));

        Assert.False(capped.RestartRequested);
        Assert.True(capped.RestartCapReached);
        Assert.Contains("restart_cap_reached=true", capped.Summary);
    }

    [Fact]
    public void EmitsPacketOnRecovery()
    {
        var events = new List<ProcessCrashRecoveryEvent>();
        var service = new WevitoProcessWatchdogService(recordRecovery: events.Add);
        service.Observe(new WatchedProcessState("shell", true, Now));

        var result = service.Observe(new WatchedProcessState("shell", false, Now.AddSeconds(1)));

        Assert.Equal(WevitoProcessWatchdogService.PacketKind, result.PacketKind);
        var recorded = Assert.Single(events);
        Assert.Equal(WevitoProcessWatchdogService.PacketKind, recorded.PacketKind);
        Assert.Contains("restart_requested=true", recorded.Summary);
    }

    [Fact]
    public void KillSwitchBlocksRestartRequest()
    {
        var service = new WevitoProcessWatchdogService(killSwitchActive: () => true);
        service.Observe(new WatchedProcessState("python-image-gen", true, Now));

        var result = service.Observe(new WatchedProcessState("python-image-gen", false, Now.AddSeconds(1)));

        Assert.False(result.RestartRequested);
        Assert.False(result.RestartCapReached);
        Assert.Contains("kill_switch=true", result.Summary);
    }
}
