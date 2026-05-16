using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetFpsMonitorServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void SnapshotEmittedHourly()
    {
        var harness = BuildHarness();
        harness.Service.Observe(new PetFpsSample(61, Now));
        var result = harness.Service.Observe(new PetFpsSample(62, Now.AddHours(1).AddMilliseconds(250)));

        Assert.True(result.SnapshotEmitted);
        Assert.Equal(PetFpsMonitorService.SnapshotPacketKind, result.PacketKind);
        Assert.Contains("p50", result.Summary);
        Assert.Contains("p95", result.Summary);

        var rows = harness.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddHours(2));
        Assert.Contains(rows, row => row.PacketKind == PetFpsMonitorService.SnapshotPacketKind);
    }

    [Fact]
    public void ViolationEmittedOnBelow30()
    {
        var harness = BuildHarness();

        var result = harness.Service.Observe(new PetFpsSample(28.5, Now));

        Assert.True(result.ViolationEmitted);
        Assert.Equal(PetFpsMonitorService.ViolationPacketKind, result.PacketKind);
        Assert.True(result.AutonomousThrottleActive);
        Assert.Equal(0.5, result.ThrottleMultiplier);
        var row = Assert.Single(harness.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
        Assert.Equal(PetFpsMonitorService.ViolationPacketKind, row.PacketKind);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public void ThrottlesAutonomousLoopOnViolation()
    {
        var harness = BuildHarness();

        harness.Service.Observe(new PetFpsSample(20, Now));

        Assert.True(harness.Service.IsAutonomousThrottleActive(Now.AddMinutes(4)));
        Assert.Equal(0.5, harness.Service.GetThrottleMultiplier(Now.AddMinutes(4)));
        Assert.False(harness.Service.IsAutonomousThrottleActive(Now.AddMinutes(6)));
        Assert.Equal(1.0, harness.Service.GetThrottleMultiplier(Now.AddMinutes(6)));
    }

    private static Harness BuildHarness()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-fps-monitor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        return new Harness(new PetFpsMonitorService(ledger), ledger);
    }

    private sealed record Harness(PetFpsMonitorService Service, AuditLedgerService Ledger);
}
