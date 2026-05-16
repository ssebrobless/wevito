using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class RuntimeBudgetMeterUserProtectionFloorTests
{
    [Fact]
    public void RefusesReservationBelowFloor()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var meter = new RuntimeBudgetMeter(
            Path.Combine(BuildTempRoot(), "budget.json"),
            () => now,
            () => new RuntimeResourceSnapshot(0, 128, now, WevitoCpuPercent: 55, GpuUtilizationPercent: 0, AvailableMemoryMb: 8192));

        var result = meter.TryReserve(new RuntimeBudgetSnapshot(4, 90, 2048));

        Assert.False(result.Allowed);
        Assert.Contains("User protection floor", result.Reason);
    }

    [Fact]
    public void AllowsForegroundEvenBelowFloor()
    {
        var now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");
        var meter = new RuntimeBudgetMeter(
            Path.Combine(BuildTempRoot(), "budget.json"),
            () => now,
            () => new RuntimeResourceSnapshot(0, 128, now, WevitoCpuPercent: 55, GpuUtilizationPercent: 90, AvailableMemoryMb: 256));

        var result = meter.TryReserve(new RuntimeBudgetSnapshot(4, 90, 2048), isForeground: true);

        Assert.True(result.Allowed, result.Reason);
    }

    private static string BuildTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-budget-floor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
