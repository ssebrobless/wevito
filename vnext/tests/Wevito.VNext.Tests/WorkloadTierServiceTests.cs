using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class WorkloadTierServiceTests
{
    [Fact]
    public void ForegroundReservesMinimum60Pct()
    {
        var service = new WorkloadTierService();

        var result = service.TryReserve(WorkloadTier.UserForeground, 60, new WorkloadTierSnapshot());

        Assert.True(result.Allowed, result.Reason);
        Assert.Equal(60, result.ReservedPercent);
    }

    [Fact]
    public void AdaptiveBorrowingAboveFloor()
    {
        var service = new WorkloadTierService();
        var snapshot = new WorkloadTierSnapshot(UserForegroundCpuInUsePercent: 20);

        var result = service.TryReserve(WorkloadTier.Maintenance, 35, snapshot);

        Assert.True(result.Allowed, result.Reason);
    }

    [Fact]
    public void ExperimentationYieldsToMaintenance()
    {
        var service = new WorkloadTierService();
        var snapshot = new WorkloadTierSnapshot(UserForegroundCpuInUsePercent: 60, MaintenanceCpuInUsePercent: 0, ExperimentationCpuInUsePercent: 30);

        var result = service.TryReserve(WorkloadTier.Experimentation, 5, snapshot);

        Assert.False(result.Allowed);
        Assert.Contains("maintenance", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MaintenanceYieldsToForeground()
    {
        var service = new WorkloadTierService();
        var snapshot = new WorkloadTierSnapshot(UserForegroundCpuInUsePercent: 10, MaintenanceCpuInUsePercent: 30, ExperimentationCpuInUsePercent: 5);

        var result = service.TryReserve(WorkloadTier.Maintenance, 10, snapshot);

        Assert.False(result.Allowed);
        Assert.Contains("Foreground", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
