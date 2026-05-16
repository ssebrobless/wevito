using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class DiskIoBudgetServiceTests
{
    [Fact]
    public void CapsAt20MbpsWhenUserActive()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new DiskIoBudgetService(
            clock: () => now,
            lastInputReader: () => now.AddSeconds(-5),
            diskSnapshotReader: () => new DiskIoBudgetSnapshot(32, now));

        var decision = service.Evaluate();

        Assert.True(decision.ShouldThrottle);
        Assert.Equal(20, decision.CapMegabytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(1), decision.SuggestedDelay);
    }

    [Fact]
    public void NoLimitWhenUserIdle5min()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new DiskIoBudgetService(
            clock: () => now,
            lastInputReader: () => now.AddMinutes(-5),
            diskSnapshotReader: () => new DiskIoBudgetSnapshot(200, now));

        var decision = service.Evaluate();

        Assert.False(decision.ShouldThrottle);
        Assert.Contains("idle", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmitsThrottlePacketOnLimitHit()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-disk-io-tests", Guid.NewGuid().ToString("N"));
        var jsonl = Path.Combine(root, "disk.jsonl");
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var service = new DiskIoBudgetService(
            clock: () => now,
            lastInputReader: () => now,
            diskSnapshotReader: () => new DiskIoBudgetSnapshot(22, now),
            jsonlPath: jsonl);

        service.Evaluate();

        var text = File.ReadAllText(jsonl);
        Assert.Contains(DiskIoBudgetService.DiskIoBudgetThrottledPacketKind, text);
    }
}
